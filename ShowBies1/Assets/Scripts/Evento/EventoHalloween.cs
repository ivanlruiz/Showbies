using System;
using UnityEngine;

// El evento de Halloween (pedido de Ivan, 8/10: algo para atraer jugadores, con su ficha en
// Play). Del 24/10 al 9/11, ambos incluidos, el mundo se llena de calabazas, los zombis salen
// disfrazados y sueltan caramelos ademas de monedas. Los caramelos llenan una fila de cinco
// hitos (VentanaHalloween, en el menu): los cuatro primeros pagan monedas y el ultimo es el
// SOMBRERO DE CALABAZA, que el jugador lleva puesto para siempre (se apaga en OPCIONES).
//
// Esto es la logica, sin escena: las fechas, los caramelos, los hitos y sus premios. Lo
// que se ve lo arma InstaladorHalloween al cargar cada escena, asi el evento no toca las
// escenas y sacarlo es borrar la carpeta Evento y sus llamadas.
//
// Las fechas son de cada anio (mmdd), y el estado guarda de que edicion son los caramelos:
// si el juego sigue instalado en 2027, el evento vuelve solo, con la fila de cero y, para
// quien ya tiene el sombrero, monedas en su lugar.
//
// Los hitos y los premios se miden con la vara de Economia y la mejor oleada del dia en que
// el jugador vio el evento por primera vez (congelada, como las misiones): con numeros
// fijos, uno de la oleada 40 llenaba la fila en una partida y uno nuevo no llegaba nunca.
public static class EventoHalloween
{
    public const int Inicio = 1024;   // mmdd, el primer dia
    public const int Fin = 1109;      // mmdd, el ultimo dia (incluido)

    // Cuantos caramelos suelta cada zombi que muere: uno con esta probabilidad, y el jefe
    // una lluvia (lluviaDesde de Moneda: sale entera aunque el piso este lleno).
    public const float ProbabilidadDeCaramelo = 0.3f;
    public const int CaramelosDelJefe = 10;

    // Lo que cuesta llegar a cada hito, en partidas que llegan a la oleada congelada
    // (acumulado), y lo que paga cada uno, en partidas de monedas (Economia.MonedasPorPartida).
    // El ultimo es el sombrero: para quien ya lo tiene, paga PremioSinSombrero.
    public static readonly double[] PartidasPorHito = { 0.5, 1.5, 3, 5, 8 };
    public static readonly double[] PartidasDePremio = { 0.25, 0.5, 0.75, 1.0, 0 };
    public const double PremioSinSombrero = 1.5;
    public static int Hitos { get { return PartidasPorHito.Length; } }

    // Con menos, la vara mide partidas de un par de minutos y la fila se llenaba en dos.
    public const int OleadaMinima = 5;

    // El sombrero puesto es una preferencia del telefono, como MOSTRAR FPS; tenerlo es
    // progreso y va en el JSON.
    public const string ClaveSombreroPuesto = "SombreroCalabaza";

#if UNITY_EDITOR
    // ShowBies > Halloween > Forzar en el editor: el evento prendido fuera de las fechas,
    // para probarlo. Es de esta maquina (EditorPrefs), no del proyecto.
    public const string ClaveForzarEnElEditor = "ShowBies.HalloweenEnElEditor";
#endif

    // Activo se pregunta en cada muerte y el dia confiable cuesta una llamada por JNI en
    // Android (RelojConfiable): se mira cada tanto. A medianoche del 9/11 se apaga solo.
    private const float SegundosEntreRevisiones = 20f;
    private static float revisadoEn = float.NegativeInfinity;
    private static bool activo;
    private static int edicion;
    private static Moneda carameloPrefab;
    private static bool carameloBuscado;

    // Para las pruebas: el dia de hoy y si esta forzado. -1 = el de verdad.
    private static int diaParaPruebas = -1;
    private static int forzadoParaPruebas = -1;

    // Lo que se junto en la partida en curso (la derrota lo muestra). No se guarda.
    public static double CaramelosDeLaPartida { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetearEstadoCompartido()
    {
        revisadoEn = float.NegativeInfinity;
        activo = false;
        edicion = 0;
        carameloPrefab = null;
        carameloBuscado = false;
        diaParaPruebas = -1;
        forzadoParaPruebas = -1;
        CaramelosDeLaPartida = 0;
    }

    public static void UsarParaPruebas(int dia, int forzado)
    {
        diaParaPruebas = dia;
        forzadoParaPruebas = forzado;
        revisadoEn = float.NegativeInfinity;
    }

    // --- Fechas ---------------------------------------------------------------------

    public static bool EnFechas(int hoy)
    {
        int mmdd = hoy % 10000;
        return mmdd >= Inicio && mmdd <= Fin;
    }

    public static int EdicionDe(int hoy)
    {
        return hoy / 10000;
    }

    public static int Hoy()
    {
        return diaParaPruebas > 0 ? diaParaPruebas : Progreso.DiaDeHoy();
    }

    // Prendido en la APK de prueba (el paquete .prueba) y, en el editor, con el menu.
    public static bool Forzado
    {
        get
        {
            if (forzadoParaPruebas >= 0) return forzadoParaPruebas == 1;
#if UNITY_EDITOR
            return UnityEditor.EditorPrefs.GetBool(ClaveForzarEnElEditor, false);
#else
            return ConfigAnuncios.EsPaqueteDePrueba(Application.identifier);
#endif
        }
    }

    public static bool Activo
    {
        get { Revisar(); return activo; }
    }

    // La edicion en curso: la del anio de hoy. Forzado fuera de las fechas, tambien.
    public static int EdicionActual
    {
        get { Revisar(); return edicion; }
    }

    private static void Revisar()
    {
        // En las pruebas se pregunta sin escena y con el dia cambiando: sin cache.
        bool deprueba = diaParaPruebas > 0 || forzadoParaPruebas >= 0;
        float ahora = Time.unscaledTime;
        if (!deprueba && ahora - revisadoEn < SegundosEntreRevisiones && ahora >= revisadoEn) return;
        revisadoEn = ahora;
        int hoy = Hoy();
        activo = Forzado || EnFechas(hoy);
        edicion = EdicionDe(hoy);
    }

    // Los dias que quedan contando hoy: 1 el 9/11 ("¡ULTIMO DIA!"). Forzado fuera de las
    // fechas cuenta hasta el 9/11 de este anio, o 1 si ya paso.
    public static int DiasQueFaltan(int hoy)
    {
        try
        {
            var dia = new DateTime(hoy / 10000, hoy / 100 % 100, hoy % 100);
            var fin = new DateTime(hoy / 10000, Fin / 100, Fin % 100);
            return Math.Max(1, (int)(fin - dia).TotalDays + 1);
        }
        catch (ArgumentException)
        {
            return 1;
        }
    }

    // --- Caramelos ------------------------------------------------------------------

    public static Moneda CarameloPrefab
    {
        get
        {
            if (!carameloBuscado)
            {
                carameloBuscado = true;
                carameloPrefab = Resources.Load<Moneda>("Halloween/Caramelo");
                if (carameloPrefab == null) Debug.LogError("EventoHalloween: falta Resources/Halloween/Caramelo");
            }
            return carameloPrefab;
        }
    }

    // Cuantos suelta un zombi al morir.
    public static int CaramelosAlMorir(bool jefe, float azar)
    {
        if (jefe) return CaramelosDelJefe;
        return azar < ProbabilidadDeCaramelo ? 1 : 0;
    }

    public static double Caramelos
    {
        get { Asegurar(); return Progreso.Halloween.caramelos; }
    }

    // Al agarrar un caramelo (Moneda.Cobrar). Fuera del evento no cuenta: los que
    // quedaron en el piso a medianoche del ultimo dia se pierden, como si vencieran.
    public static void Sumar(double cantidad)
    {
        if (!(cantidad > 0) || double.IsInfinity(cantidad) || !Activo) return;
        Asegurar();
        Progreso.Halloween.caramelos += cantidad;
        CaramelosDeLaPartida += cantidad;
        Progreso.AvisarCambio();
    }

    public static void EmpezarPartida()
    {
        CaramelosDeLaPartida = 0;
    }

    // --- La fila de hitos -----------------------------------------------------------

    // Arranca la edicion de hoy la primera vez que se la ve, y antes cierra la anterior si
    // quedo algo sin cobrar. Se puede llamar seguido: si no hay nada que hacer, no hace nada.
    public static void Asegurar()
    {
        var estado = Progreso.Halloween;
        CerrarSiTermino();
        if (!Activo || estado.edicion == EdicionActual) return;

        estado.edicion = EdicionActual;
        estado.caramelos = 0;
        estado.oleada = Progreso.MejorOleada;
        estado.cobrados = 0;
        estado.cerrado = false;
        estado.visto = false;
        estado.conSombrero = !estado.sombrero;
        Progreso.AvisarCambio();
    }

    // Terminado el evento, lo que quedo sin cobrar se cobra solo (como el cierre de
    // medianoche de las misiones), sombrero incluido. Devuelve las monedas que pago.
    public static double CerrarSiTermino()
    {
        var estado = Progreso.Halloween;
        if (estado.edicion == 0 || estado.cerrado) return 0;
        if (Activo && estado.edicion == EdicionActual) return 0;

        double pagado = 0;
        while (estado.cobrados < Alcanzados) pagado += CobrarSiguiente();
        estado.cerrado = true;
        Progreso.AvisarCambio();
        Progreso.Guardar();
        return pagado;
    }

    // La oleada con que se mide esta edicion.
    public static int OleadaDeLaVara
    {
        get
        {
            var estado = Progreso.Halloween;
            return Math.Max(OleadaMinima, estado.oleada >= 0 ? estado.oleada : Progreso.MejorOleada);
        }
    }

    public static double Umbral(int hito)
    {
        return UmbralCon(hito, OleadaDeLaVara);
    }

    public static double UmbralCon(int hito, int oleada)
    {
        hito = Mathf.Clamp(hito, 0, Hitos - 1);
        return Economia.Redondo(PartidasPorHito[hito] * Economia.ZombisPorPartida(Math.Max(OleadaMinima, oleada)) * ProbabilidadDeCaramelo);
    }

    // Las monedas del hito (0 si es el sombrero).
    public static double Premio(int hito)
    {
        return PremioCon(hito, OleadaDeLaVara, Progreso.Halloween.conSombrero);
    }

    public static double PremioCon(int hito, int oleada, bool conSombrero)
    {
        hito = Mathf.Clamp(hito, 0, Hitos - 1);
        double partidas = hito == Hitos - 1 ? (conSombrero ? 0 : PremioSinSombrero) : PartidasDePremio[hito];
        if (partidas <= 0) return 0;
        return Economia.Redondo(partidas * Economia.MonedasPorPartida(Math.Max(OleadaMinima, oleada)));
    }

    public static bool EsElSombrero(int hito)
    {
        return hito == Hitos - 1 && Progreso.Halloween.conSombrero;
    }

    // Cuantos hitos alcanzan los caramelos de esta edicion.
    public static int Alcanzados
    {
        get
        {
            var estado = Progreso.Halloween;
            if (estado.edicion == 0) return 0;
            int n = 0;
            while (n < Hitos && estado.caramelos + 1e-6 >= Umbral(n)) n++;
            return n;
        }
    }

    public static int Cobrados
    {
        get { return Progreso.Halloween.cobrados; }
    }

    public static int PorCobrar
    {
        get { return Math.Max(0, Alcanzados - Cobrados); }
    }

    // Cobra el primer hito alcanzado sin cobrar: monedas, o el sombrero. Devuelve las
    // monedas (0 con el sombrero o si no habia nada).
    public static double CobrarSiguiente()
    {
        var estado = Progreso.Halloween;
        int hito = estado.cobrados;
        if (hito >= Alcanzados) return 0;

        double monto = Premio(hito);
        estado.cobrados++;
        if (EsElSombrero(hito))
        {
            estado.sombrero = true;
            Progreso.AvisarCambio();
            Progreso.Guardar();
            return 0;
        }
        // Un premio, no monedas ganadas jugando (ver Progreso.CobrarPremio), que guarda.
        Progreso.CobrarPremio("halloween_" + (hito + 1), monto, false);
        return monto;
    }

    // --- El sombrero ----------------------------------------------------------------

    public static bool TieneSombrero
    {
        get { return Progreso.Halloween.sombrero; }
    }

    public static bool SombreroPuesto
    {
        get { return PlayerPrefs.GetInt(ClaveSombreroPuesto, 1) == 1; }
    }

    public static void PonerSombrero(bool puesto)
    {
        PlayerPrefs.SetInt(ClaveSombreroPuesto, puesto ? 1 : 0);
    }

    public static bool LlevaSombrero
    {
        get { return TieneSombrero && SombreroPuesto; }
    }

    // La insignia "!" del boton: la ventana todavia no se abrio en esta edicion.
    public static bool Visto
    {
        get { return Progreso.Halloween.visto; }
    }

    public static void MarcarVisto()
    {
        var estado = Progreso.Halloween;
        if (estado.visto) return;
        estado.visto = true;
        Progreso.AvisarCambio();
    }
}

// Lo que se guarda del evento en el progreso (v7). Los nombres van al JSON: no se renombran.
[Serializable]
public class EstadoHalloween
{
    public int edicion;              // el anio de los caramelos de abajo; 0 = nunca se vio el evento
    public double caramelos;
    public int oleada = -1;          // la mejor oleada al empezar la edicion: mide hitos y premios (-1: sin marca)
    public int cobrados;             // los primeros N hitos ya cobrados
    public bool cerrado;             // la edicion termino y se cobro lo que faltaba
    public bool conSombrero;         // el ultimo hito de esta edicion es el sombrero (no lo tenia al empezar)
    public bool visto;               // ya se abrio la ventana en esta edicion
    public bool sombrero;            // el sombrero de calabaza: ganado una vez, es para siempre
}
