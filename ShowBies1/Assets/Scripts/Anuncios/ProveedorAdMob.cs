using System;
using System.Collections.Generic;
using UnityEngine;

// Los videos de verdad: AdMob, sin el plugin de Unity. Del lado de Android esta el puente
// (Plugins/Android/ShowBiesAnuncios.androidlib, PuenteAnuncios.java), que maneja el SDK y
// el consentimiento de Europa (UMP) y avisa con eventos de texto; aca se escuchan y se
// traducen a lo que espera ServicioAnuncios. El plugin oficial depende de EDM4U, que Google
// archiva el 26/10/2026 y que ya se peleo con Unity 6 en este proyecto.
//
// Tres reglas:
// - **Listo no cruza a Java**: se pregunta en el golpe que mata al jugador. Sale de lo que
//   avisa el puente (cargado, no_cargado, vencido).
// - **Un solo resultado por video**: el premio (ganado) llega antes del cierre y se avisa
//   recien al cerrarse, con Recompensado si llego. Avisar antes reanudaria el juego detras
//   del anuncio.
// - **Siempre se resuelve**: si el SDK no avisa nunca (volver a la app desde el icono con el
//   video abierto, o un anuncio que no llega a abrirse), un vigia lo da por terminado.
//
// Los avisos llegan en el hilo de Android: AlEvento solo los encola, y Atender los aplica en
// el de Unity (lo llama ServicioAnuncios.AtenderAvisos, desde VigiaAplicacion). El puente es
// una interfaz y el reloj se puede cambiar para probarlo entero sin telefono
// (PruebasMejoras).
public class ProveedorAdMob : IProveedorAnuncios, IConsentimientoAnuncios
{
    // Lo que el proveedor necesita de Android.
    public interface IPuente
    {
        void Iniciar(Action<string, string, string> alEvento, string[] lugares, string[] bloques,
                     string clasificacion, bool simularEuropa);
        void Mostrar(string lugar);
        void OlvidarElQueSeMuestra();
        void PedirConsentimiento();
        void MostrarPrivacidad();
    }

    // Lo que se espera, con la app delante, a que el video se abra: el anuncio ya esta
    // cargado y abrirlo es casi inmediato, asi que si no se abrio, no se va a abrir.
    public const float SegundosParaAbrir = 6f;
    // Lo que se espera el aviso del cierre despues de volver a la app.
    public const float SegundosParaCerrar = 1.5f;

    private readonly IPuente puente;
    private readonly string[] lugares;
    private readonly string[] bloques;
    private readonly string clasificacion;
    private readonly bool simularEuropa;
    private readonly Func<float> reloj;

    private readonly object candado = new object();
    private readonly Queue<string[]> avisos = new Queue<string[]>();
    private readonly Dictionary<string, bool> cargados = new Dictionary<string, bool>();

    private bool iniciado;
    private Action<ResultadoAnuncio> alTerminar;
    private string lugarEnPantalla;
    private bool abrio;
    private bool ganado;
    private bool conFoco = true;
    private float esperandoQueAbraDesde = -1f;
    private float volvioEn = -1f;

    public bool ConsentimientoRequerido { get; private set; }
    public bool ConsentimientoEnPantalla { get; private set; }
    public bool PrivacidadRequerida { get; private set; }

    // Lo que muestra el diagnostico de la APK de prueba (DiagnosticoAnuncios): que avisó el
    // puente, sin conectar el telefono a la PC.
    private readonly Dictionary<string, string> estados = new Dictionary<string, string>();
    private int avisosRecibidos;
    public bool SdkListo { get; private set; }
    public string EstadoConsentimiento { get; private set; }
    public string PuedePedir { get; private set; }
    public string UltimoError { get; private set; }
    public string UltimoAviso { get; private set; }

    public int AvisosRecibidos
    {
        get { lock (candado) return avisosRecibidos; }
    }

    public string EstadoDe(string lugar)
    {
        string estado;
        return lugar != null && estados.TryGetValue(lugar, out estado) ? estado : "sin aviso";
    }

    public ProveedorAdMob(IPuente puente, string[] lugares, string[] bloques, string clasificacion,
                          bool simularEuropa, Func<float> reloj = null)
    {
        this.puente = puente;
        this.lugares = lugares ?? new string[0];
        this.bloques = bloques ?? new string[0];
        this.clasificacion = clasificacion;
        this.simularEuropa = simularEuropa;
        this.reloj = reloj ?? (() => Time.realtimeSinceStartup);
        EstadoConsentimiento = "sin respuesta";
        PuedePedir = "sin respuesta";
        UltimoError = "";
        UltimoAviso = "";
    }

    public string Nombre { get { return "admob"; } }

    public void Inicializar()
    {
        if (iniciado || puente == null) return;
        iniciado = true;
        puente.Iniciar(AlEvento, lugares, bloques, clasificacion, simularEuropa);
    }

    public bool Listo(string lugar)
    {
        bool listo;
        return alTerminar == null && lugar != null && cargados.TryGetValue(lugar, out listo) && listo;
    }

    public void Mostrar(string lugar, Action<ResultadoAnuncio> fin)
    {
        // ServicioAnuncios no pide uno mientras hay otro, pero si pasara, no se pisan.
        if (alTerminar != null || puente == null)
        {
            if (fin != null) fin(ResultadoAnuncio.NoDisponible);
            return;
        }
        alTerminar = fin;
        lugarEnPantalla = lugar;
        abrio = false;
        ganado = false;
        esperandoQueAbraDesde = -1f;
        volvioEn = -1f;
        // Ese anuncio se gasta: el puente pide otro y avisa cuando esta.
        cargados[lugar] = false;
        puente.Mostrar(lugar);
    }

    // Desde el hilo de Android: solo se encola.
    public void AlEvento(string lugar, string evento, string dato)
    {
        lock (candado)
        {
            avisos.Enqueue(new[] { lugar ?? "", evento ?? "", dato ?? "" });
            avisosRecibidos++;
        }
    }

    // En el hilo de Unity, una vez por cuadro.
    public void Atender()
    {
        while (true)
        {
            string[] aviso;
            lock (candado)
            {
                if (avisos.Count == 0) break;
                aviso = avisos.Dequeue();
            }
            Aplicar(aviso[0], aviso[1], aviso[2]);
        }
        if (alTerminar == null) return;

        float ahora = reloj();
        // No se abrio nunca, con la app delante todo el tiempo: el SDK no lo va a mostrar.
        if (!abrio && conFoco && volvioEn < 0f)
        {
            if (esperandoQueAbraDesde < 0f) esperandoQueAbraDesde = ahora;
            else if (ahora - esperandoQueAbraDesde >= SegundosParaAbrir)
            {
                puente.OlvidarElQueSeMuestra();
                Terminar(ResultadoAnuncio.NoDisponible);
                return;
            }
        }
        // De vuelta en la app y el cierre no llego: cuenta lo que se vio.
        if (volvioEn >= 0f && ahora - volvioEn >= SegundosParaCerrar)
        {
            puente.OlvidarElQueSeMuestra();
            Terminar(ganado ? ResultadoAnuncio.Recompensado : ResultadoAnuncio.Cerrado);
        }
    }

    // El video saca el foco a la app; al volver, si el cierre no llega, el vigia de Atender
    // lo resuelve. Los avisos del video llegan antes de esto o en el mismo cuadro, porque
    // Unity no corre mientras el anuncio esta delante.
    public void CambioElFoco(bool foco)
    {
        conFoco = foco;
        if (!foco)
        {
            esperandoQueAbraDesde = -1f;
            return;
        }
        if (alTerminar != null) volvioEn = reloj();
    }

    private void Aplicar(string lugar, string evento, string dato)
    {
        UltimoAviso = evento + (lugar != "" ? " [" + lugar + "]" : "") + (dato != "" ? " " + dato : "");
        switch (evento)
        {
            case "cargado":
                cargados[lugar] = true;
                estados[lugar] = "cargado";
                break;
            case "no_cargado":
                cargados[lugar] = false;
                estados[lugar] = "no cargo (" + dato + ")";
                break;
            case "vencido":
                cargados[lugar] = false;
                estados[lugar] = "vencido";
                break;
            case "abierto":
                if (lugar == lugarEnPantalla) abrio = true;
                estados[lugar] = "en pantalla";
                break;
            case "ganado":
                if (lugar == lugarEnPantalla) ganado = true;
                break;
            case "terminado":
                estados[lugar] = "termino: " + dato;
                if (alTerminar != null && lugar == lugarEnPantalla) Terminar(Traducir(dato, ganado));
                break;
            case "consentimiento":
                ConsentimientoRequerido = dato == "requerido";
                EstadoConsentimiento = dato;
                break;
            case "privacidad":
                PrivacidadRequerida = dato == "requerida";
                break;
            case "puede_pedir":
                PuedePedir = dato;
                break;
            case "consentimiento_cerrado":
                ConsentimientoEnPantalla = false;
                break;
            case "sdk_listo":
                SdkListo = true;
                break;
            case "error":
                UltimoError = dato;
                Debug.LogWarning("AdMob: " + dato);
                break;
        }
    }

    // Lo que dice el puente al cerrarse, en lo que espera ServicioAnuncios. Si el premio ya
    // habia llegado, cuenta aunque despues se cierre o falle.
    public static ResultadoAnuncio Traducir(string dato, bool ganado)
    {
        if (ganado || dato == "recompensado") return ResultadoAnuncio.Recompensado;
        switch (dato)
        {
            case "cerrado": return ResultadoAnuncio.Cerrado;
            case "falla": return ResultadoAnuncio.FallaAlMostrar;
            default: return ResultadoAnuncio.NoDisponible;
        }
    }

    private void Terminar(ResultadoAnuncio resultado)
    {
        Action<ResultadoAnuncio> fin = alTerminar;
        alTerminar = null;
        lugarEnPantalla = null;
        abrio = false;
        ganado = false;
        esperandoQueAbraDesde = -1f;
        volvioEn = -1f;
        if (fin != null) fin(resultado);
    }

    public void PedirConsentimiento()
    {
        if (ConsentimientoEnPantalla || puente == null) return;
        ConsentimientoEnPantalla = true;
        puente.PedirConsentimiento();
    }

    public void MostrarPrivacidad()
    {
        if (ConsentimientoEnPantalla || puente == null) return;
        ConsentimientoEnPantalla = true;
        puente.MostrarPrivacidad();
    }
}
