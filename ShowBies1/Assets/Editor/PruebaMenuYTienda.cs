using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Banco del menu y la tienda: que abrir y cerrar la tienda no deje ningun cuadro de color
// liso ni de escena vacia, y que el menu se vea igual sin HDR.
//
// La tienda, con su fundido de entrada terminado, apaga la camara del menu (cullingMask en
// 0: su fondo es opaco y tapa todo) y la vuelve a prender al cerrarse o al apagarse
// (TiendaMejoras.DejarDeDibujarLaEscena / VolverADibujarLaEscena). Si eso se corriera un
// cuadro, se veria un cuadro con el color liso del cielo detras del menu. FondoMenu apaga el
// HDR y el MSAA de la camara del menu: el menu tiene que verse igual que con ellos.
//
// Entra en play en Menu.unity y recorre, capturando CADA cuadro de la transicion (con
// ScreenCapture.CaptureScreenshotAsTexture al final del cuadro, que es lo unico que trae la
// UI en overlay), cinco caminos:
//   1. abrir con MEJORAS y cerrar con VOLVER (los botones de verdad, con los eventos de un dedo),
//   2. cerrar con el atras (BotonAtrasMenu.Atras, que es lo que hace el Escape),
//   3. apagar la tienda de golpe (su OnDisable tiene que devolver la camara),
//   4. A JUGAR, que carga una partida (se mira el ultimo cuadro del menu antes de cargar),
//   5. MEJORAS desde la derrota (muere en la partida del camino 4 y toca MEJORAS en la derrota:
//      MenuPerdiste.AbrirMejoras -> TiendaMejoras.AbrirEnMenu, el menu carga con la tienda abierta).
// De cada cuadro mide el desvio de la luminancia (un cuadro liso casi no tiene) y la fraccion
// de pixeles del color de fondo de la camara (una escena vacia con el menu encima es casi toda
// de ese color); los umbrales salen de dos referencias sacadas al empezar: el menu quieto y el
// mismo menu con la camara apagada a proposito. Ademas mira en cada cuadro que la camara no
// este apagada sin la tienda opaca encima.
//
// El HDR: ocho cuadros seguidos del menu quieto, sin, sin, con, con, sin, sin, con, con. Los
// zombis del fondo se mueven entre cuadro y cuadro, asi que lo que cuenta es cuanto MAS cambian
// dos cuadros seguidos cuando cambia el HDR que cuando no.
//
// Las capturas van a Builds/menu_tienda/ (a la mitad del tamanio) y el informe a
// Builds/prueba_menutienda.txt.
//
// Cambia el progreso (lo reinicia: una partida terminada, la oleada 3, el danio en 1, 3000
// monedas y la diaria de hoy cobrada, para que no salgan la guia de la primera compra ni la
// recompensa diaria) y PlayerPrefs["UltimoModo"]; la muerte del camino 5 escribe el record y el
// ultimo modo. RespaldoDelBanco guarda y devuelve el progreso y los PlayerPrefs.
//
// OJO: entrar y salir de play hace que Unity vuelva a serializar ProjectSettings (ver la
// trampa en CLAUDE.md): despues de correrlo, revertir lo que no se toco a proposito.
[InitializeOnLoad]
public static class PruebaMenuYTienda
{
    const string Clave = "ShowBies.PruebaMenuYTienda";
    const string Ruta = "../Builds/prueba_menutienda.txt";
    const string Carpeta = "../Builds/menu_tienda";

    const string CaminoAbrir = "1_abrir_con_mejoras";
    const string CaminoVolver = "1_cerrar_con_volver";
    const string CaminoAtras = "2_cerrar_con_el_atras";
    const string CaminoApagar = "3_apagar_de_golpe";
    const string CaminoJugar = "4_a_jugar";
    const string CaminoDerrota = "5_mejoras_desde_la_derrota";
    static readonly string[] Caminos = { CaminoAbrir, CaminoVolver, CaminoAtras, CaminoApagar, CaminoJugar, CaminoDerrota };

    // Cuadros que se capturan antes de cada accion, y despues como minimo (en cuadros y en
    // segundos reales: codificar cada PNG baja los cuadros por segundo).
    const int CuadrosAntes = 3;
    const int CuadrosMinimos = 12;
    const float SegundosMinimos = 0.6f;

    // Un cuadro es liso si el desvio de su luminancia (de 0 a 1) no llega a esto.
    const double UmbralLiso = 0.02;
    // Cuanto se puede alejar un pixel del color de fondo de la camara (de 0 a 255 por canal)
    // para contar como "fondo".
    const int ToleranciaFondo = 8;
    // Las dos referencias tienen que separarse al menos esto en la fraccion de fondo para que
    // la medicion de "escena vacia" sirva.
    const double DistanciaMinimaDeReferencias = 0.1;
    // Lo que el HDR puede sumarle a la diferencia media entre dos cuadros seguidos (de 0 a 255).
    const double UmbralHdr = 2.0;

    const double TopeTotal = 240.0;
    const int SinCamara = int.MinValue;

    enum Paso
    {
        Esperando, Referencias, Hdr,
        AbrirConMejoras, AbiertaParaVolver, CerrarConVolver,
        AbrirParaElAtras, CerrarConElAtras,
        AbrirParaApagar, Apagar, Reprender,
        AbrirParaJugar, AJugar,
        EnLaPartida, EsperandoLaDerrota, DesdeLaDerrota,
        AbiertaAlLlegar, CerrarAlFinal, Listo
    }

    // Lo que se anoto de cada cuadro capturado.
    class Cuadro
    {
        public string camino;
        public int indice;
        public bool despues;      // despues de la accion del camino
        public int escena;        // buildIndex de la escena activa
        public int mascara;       // cullingMask de Camera.main (SinCamara si no hay)
        public bool abierta;      // TiendaMejoras.Abierta
        public bool panel;        // el panel de la tienda prendido
        public float alfa;        // el alfa del CanvasGroup del panel
        public bool conFoto;
        public double desvio;     // desvio de la luminancia, de 0 a 1
        public double fondo;      // fraccion de pixeles del color de fondo de la camara del menu
    }

    static Paso paso;
    static double ahora, inicio, pasoDesde, desdeTapando, desdeDerrota;
    static bool tocoAbrir, abrioDirecto, cerroLaDiaria, rechazoRevivir;
    static int aperturasDirectas;

    // La corrutina de captura: corre en un objeto propio que sobrevive a los cambios de escena.
    static MonoBehaviour anfitrion;
    static bool secuenciaTerminada, ultimaLlego;
    static TiendaMejoras tiendaApagada;

    static readonly List<Cuadro> cuadros = new List<Cuadro>();
    static int cuadrosSinFoto;
    static int anchoPantalla, altoPantalla;
    static Color32 colorFondo;

    // La camara del menu como la dejo FondoMenu.
    static bool midioLaCamara, hdrAlEmpezar, msaaAlEmpezar;
    static int mascaraOriginal = SinCamara;

    // Las referencias: el menu quieto y el menu con la camara apagada a proposito.
    static bool midioReferencias;
    static double desvioMenu = -1, fondoMenu = -1, desvioVacia = -1, fondoVacia = -1;

    // El HDR de cada cuadro de la comparacion, lo que cambio cada uno respecto del anterior y
    // su luminancia media.
    static readonly bool[] SecuenciaHdr = { false, false, true, true, false, false, true, true };
    static readonly bool[] hdrLeido = new bool[8];
    static readonly double[] diferenciasHdr = new double[8];
    static readonly double[] luminanciasHdr = new double[8];
    static bool midioHdr;
    static double diferenciaIgual = -1, diferenciaCambio = -1;

    // Lo que dejo cada camino.
    static bool abrioConMejoras, cerroConVolver, cerroConAtras, abrioAlLlegar, cerroAlFinal, menuTrasReprender;
    static int mascaraAntesDeVolver, mascaraTrasVolver, mascaraAntesDelAtras, mascaraTrasAtras,
               mascaraAntesDeApagar, mascaraAlApagar, mascaraApagada, mascaraAlReprender,
               mascaraAntesDelFinal, mascaraAlFinal;
    static int escenaTrasJugar = -1;

    static readonly StringBuilder excepciones = new StringBuilder();
    static int cuantasExcepciones;

    static PruebaMenuYTienda()
    {
        EditorApplication.update += Tick;
        Application.logMessageReceived += AnotarExcepcion;
    }

    static void AnotarExcepcion(string mensaje, string pila, LogType tipo)
    {
        if (!SessionState.GetBool(Clave, false)) return;
        if (tipo != LogType.Exception && tipo != LogType.Error) return;
        cuantasExcepciones++;
        if (cuantasExcepciones <= 3) excepciones.AppendLine("  " + mensaje + System.Environment.NewLine + pila);
    }

    [MenuItem("ShowBies/Pruebas/Menu y tienda sin parpadeo (play)")]
    // Publico para poder correrlo por codigo (ver la trampa del registro de menus).
    public static void Arrancar()
    {
        if (!RespaldoDelBanco.PuedeArrancar("PruebaMenuYTienda")) return;
        RespaldoDelBanco.Guardar("PruebaMenuYTienda");

        // Con la recarga de dominio al entrar en play (EnterPlayModeOptions apagado) lo de
        // Progreso vuelve a leerse del archivo: tiene que quedar guardado en disco antes.
        PrepararProgreso();
        // A JUGAR va a las oleadas (el libre sigue bloqueado con la oleada 3, igual).
        PlayerPrefs.SetInt("UltimoModo", TiendaMejoras.EscenaOleadas);
        PlayerPrefs.Save();

        string carpeta = Path.GetFullPath(Carpeta);
        if (Directory.Exists(carpeta)) Directory.Delete(carpeta, true);
        Directory.CreateDirectory(carpeta);

        PlayerSettings.runInBackground = true;
        EditorSceneManager.OpenScene("Assets/Escenas/Menu.unity");
        SessionState.SetBool(Clave, true);
        SessionState.SetBool(Clave + ".empezo", false);
        EditorApplication.EnterPlaymode();
    }

    // Alguien que ya jugo y ya compro, con monedas para comprar: sin la guia de la primera
    // compra (PrimeraVez.NuncaCompro), sin la recompensa diaria (cobrada hoy; si saliera, la
    // tienda que se abre al llegar la esperaria) y sin el pedido de resenia (oleada < 10).
    static void PrepararProgreso()
    {
        Progreso.ReiniciarTodo();
        Progreso.TerminarPartida(60f);
        Progreso.RegistrarOleadaCompletada(3);
        var catalogo = CatalogoMejoras.Instancia;
        if (catalogo != null && catalogo.danoBala != null && !string.IsNullOrEmpty(catalogo.danoBala.id))
            Progreso.DepurarFijarNivel(catalogo.danoBala.id, 1);
        else
            Debug.LogWarning("PruebaMenuYTienda: no hay catalogo o danoBala; la guia de la primera compra puede salir");
        Progreso.DepurarFijarMonedas(3000);
        Progreso.RegistrarRecompensaDiaria(Progreso.DiaDeHoy(), 1);
        Progreso.Guardar();
    }

    // Todo lo de la prueba vuelve a cero al entrar en play (la recarga de dominio ya lo hizo,
    // pero asi no depende de eso). Las excepciones no: las del arranque de la escena cuentan.
    static void Reiniciar()
    {
        paso = Paso.Esperando;
        inicio = pasoDesde = ahora;
        desdeTapando = desdeDerrota = -1;
        tocoAbrir = abrioDirecto = cerroLaDiaria = rechazoRevivir = false;
        aperturasDirectas = 0;
        anfitrion = null;
        secuenciaTerminada = ultimaLlego = false;
        tiendaApagada = null;
        cuadros.Clear();
        cuadrosSinFoto = 0;
        anchoPantalla = altoPantalla = 0;
        midioLaCamara = hdrAlEmpezar = msaaAlEmpezar = false;
        mascaraOriginal = SinCamara;
        midioReferencias = false;
        desvioMenu = fondoMenu = desvioVacia = fondoVacia = -1;
        for (int i = 0; i < SecuenciaHdr.Length; i++)
        {
            hdrLeido[i] = false;
            diferenciasHdr[i] = -1;
            luminanciasHdr[i] = -1;
        }
        midioHdr = false;
        diferenciaIgual = diferenciaCambio = -1;
        abrioConMejoras = cerroConVolver = cerroConAtras = abrioAlLlegar = cerroAlFinal = menuTrasReprender = false;
        mascaraAntesDeVolver = mascaraTrasVolver = mascaraAntesDelAtras = mascaraTrasAtras = SinCamara;
        mascaraAntesDeApagar = mascaraAlApagar = mascaraApagada = mascaraAlReprender = SinCamara;
        mascaraAntesDelFinal = mascaraAlFinal = SinCamara;
        escenaTrasJugar = -1;
    }

    static double Tope(Paso p)
    {
        switch (p)
        {
            case Paso.Esperando: return 40;
            case Paso.Referencias:
            case Paso.Hdr: return 30;
            case Paso.AbrirConMejoras:
            case Paso.CerrarConVolver:
            case Paso.CerrarConElAtras:
            case Paso.Apagar:
            case Paso.AJugar:
            case Paso.DesdeLaDerrota: return 60;
            case Paso.EnLaPartida:
            case Paso.EsperandoLaDerrota: return 30;
            default: return 20;
        }
    }

    static void Pasar(Paso siguiente)
    {
        paso = siguiente;
        pasoDesde = ahora;
        tocoAbrir = abrioDirecto = false;
        desdeTapando = -1;
        desdeDerrota = -1;
    }

    static void Tick()
    {
        if (!SessionState.GetBool(Clave, false)) return;
        if (!EditorApplication.isPlaying) return;
        if (!RespaldoDelBanco.SigueArmado("PruebaMenuYTienda", Clave)) return;

        ahora = EditorApplication.timeSinceStartup;
        if (!SessionState.GetBool(Clave + ".empezo", false))
        {
            SessionState.SetBool(Clave + ".empezo", true);
            Reiniciar();
            return;
        }
        if (ahora - inicio > TopeTotal) { Terminar("se paso del tope total de " + TopeTotal + " s, en el paso " + paso); return; }
        if (ahora - pasoDesde > Tope(paso)) { Terminar("se trabo en el paso " + paso + " (tope de " + Tope(paso) + " s)"); return; }

        double enElPaso = ahora - pasoDesde;
        Camera camara = Camera.main;
        TiendaMejoras tienda = Object.FindFirstObjectByType<TiendaMejoras>();

        switch (paso)
        {
            case Paso.Esperando:
            {
                // Que el menu termine de entrar (los botones rebotan al aparecer).
                if (enElPaso < 3.0) return;
                if (camara == null || tienda == null || SceneManager.GetActiveScene().buildIndex != TiendaMejoras.EscenaMenu) return;
                // No tendria que salir (se cobro hoy al preparar el progreso); si sale igual,
                // se cierra sin cobrar, como el atras.
                if (VentanaRecompensaDiaria.Ocupada)
                {
                    if (VentanaRecompensaDiaria.Abierta && !cerroLaDiaria)
                    {
                        var atras = Object.FindFirstObjectByType<BotonAtrasMenu>();
                        if (atras != null) atras.Atras();
                        cerroLaDiaria = true;
                    }
                    return;
                }
                midioLaCamara = true;
                hdrAlEmpezar = camara.allowHDR;
                msaaAlEmpezar = camara.allowMSAA;
                mascaraOriginal = camara.cullingMask;
                colorFondo = camara.backgroundColor;
                anchoPantalla = Screen.width;
                altoPantalla = Screen.height;
                Pasar(Paso.Referencias);
                Lanzar(Referencias());
                return;
            }

            case Paso.Referencias:
                if (!secuenciaTerminada) return;
                Pasar(Paso.Hdr);
                Lanzar(CompararHdr());
                return;

            case Paso.Hdr:
            {
                if (!secuenciaTerminada) return;
                if (tienda == null) return;
                // Camino 1: MEJORAS, hasta que el fundido termina y la camara deja de dibujar.
                var boton = tienda.botonAbrir;
                Pasar(Paso.AbrirConMejoras);
                Lanzar(Secuencia(CaminoAbrir, () => Tocar(boton), CuadrosMinimos, SegundosMinimos, TiendaTapando, 3, 150));
                return;
            }

            case Paso.AbrirConMejoras:
                if (!secuenciaTerminada) return;
                abrioConMejoras = ultimaLlego;
                Pasar(Paso.AbiertaParaVolver);
                return;

            case Paso.AbiertaParaVolver:
            {
                if (!AbrirYEsperar(tienda, enElPaso)) return;
                mascaraAntesDeVolver = camara != null ? camara.cullingMask : SinCamara;
                var boton = tienda.botonVolver;
                Pasar(Paso.CerrarConVolver);
                Lanzar(Secuencia(CaminoVolver, () => Tocar(boton), CuadrosMinimos, SegundosMinimos, null, 0, 150));
                return;
            }

            case Paso.CerrarConVolver:
                if (!secuenciaTerminada) return;
                cerroConVolver = tienda != null && !tienda.Abierta;
                mascaraTrasVolver = camara != null ? camara.cullingMask : SinCamara;
                Pasar(Paso.AbrirParaElAtras);
                return;

            case Paso.AbrirParaElAtras:
            {
                if (!AbrirYEsperar(tienda, enElPaso)) return;
                mascaraAntesDelAtras = camara != null ? camara.cullingMask : SinCamara;
                // El Escape (y el atras de Android) llega a BotonAtrasMenu.Update, que llama a Atras.
                var atras = Object.FindFirstObjectByType<BotonAtrasMenu>();
                Pasar(Paso.CerrarConElAtras);
                Lanzar(Secuencia(CaminoAtras, () =>
                {
                    if (atras != null) atras.Atras();
                    else Debug.LogError("PruebaMenuYTienda: no hay BotonAtrasMenu en el menu");
                }, CuadrosMinimos, SegundosMinimos, null, 0, 150));
                return;
            }

            case Paso.CerrarConElAtras:
                if (!secuenciaTerminada) return;
                cerroConAtras = tienda != null && !tienda.Abierta;
                mascaraTrasAtras = camara != null ? camara.cullingMask : SinCamara;
                Pasar(Paso.AbrirParaApagar);
                return;

            case Paso.AbrirParaApagar:
                if (!AbrirYEsperar(tienda, enElPaso)) return;
                mascaraAntesDeApagar = camara != null ? camara.cullingMask : SinCamara;
                tiendaApagada = tienda;
                Pasar(Paso.Apagar);
                // La tienda entera (su canvas y su componente) se apaga de golpe, abierta: su
                // OnDisable tiene que devolverle la camara a la escena en el acto.
                Lanzar(Secuencia(CaminoApagar, () =>
                {
                    tiendaApagada.gameObject.SetActive(false);
                    var c = Camera.main;
                    mascaraAlApagar = c != null ? c.cullingMask : SinCamara;
                }, 6, 0.3f, null, 0, 90));
                return;

            case Paso.Apagar:
                if (!secuenciaTerminada) return;
                mascaraApagada = camara != null ? camara.cullingMask : SinCamara;
                // Se vuelve a prender y se cierra, para dejar el menu como estaba.
                if (tiendaApagada != null)
                {
                    tiendaApagada.gameObject.SetActive(true);
                    tiendaApagada.Cerrar();
                }
                Pasar(Paso.Reprender);
                return;

            case Paso.Reprender:
                if (enElPaso < 0.5) return;
                mascaraAlReprender = camara != null ? camara.cullingMask : SinCamara;
                menuTrasReprender = tienda != null && !tienda.Abierta && tienda.menuPrincipal != null && tienda.menuPrincipal.activeInHierarchy;
                Pasar(Paso.AbrirParaJugar);
                return;

            case Paso.AbrirParaJugar:
            {
                if (!AbrirYEsperar(tienda, enElPaso)) return;
                var boton = tienda.botonJugar;
                Pasar(Paso.AJugar);
                // Hasta que la escena activa deja de ser el menu, y tres cuadros mas.
                Lanzar(Secuencia(CaminoJugar, () => Tocar(boton), 1, 0f,
                                 () => SceneManager.GetActiveScene().buildIndex != TiendaMejoras.EscenaMenu, 3, 150));
                return;
            }

            case Paso.AJugar:
                if (!secuenciaTerminada) return;
                escenaTrasJugar = SceneManager.GetActiveScene().buildIndex;
                Pasar(Paso.EnLaPartida);
                return;

            case Paso.EnLaPartida:
            {
                // Un rato de partida y se muere, como en PruebaDerrota. En el editor no hay
                // oferta de revivir (proveedor Nulo, y con una sola partida terminada tampoco).
                var vida = PlayerHealth.instance;
                if (vida == null || enElPaso < 2.0) return;
                vida.TakeDamage(999999f);
                Pasar(Paso.EsperandoLaDerrota);
                return;
            }

            case Paso.EsperandoLaDerrota:
            {
                var vida = PlayerHealth.instance;
                if (vida != null && !vida.EstaMuerto) vida.TakeDamage(999999f);
                // Por si igual sale la oferta de revivir: NO, GRACIAS.
                if (OfertaDeRevivir.Activa && !rechazoRevivir)
                {
                    var oferta = Object.FindFirstObjectByType<OfertaDeRevivir>();
                    if (oferta != null) Tocar(oferta.botonNo);
                    rechazoRevivir = true;
                }
                var perdiste = Object.FindFirstObjectByType<MenuPerdiste>();
                if (perdiste == null) { desdeDerrota = -1; return; }
                if (desdeDerrota < 0) desdeDerrota = ahora;
                if (ahora - desdeDerrota < 1.2) return;
                Pasar(Paso.DesdeLaDerrota);
                // El boton MEJORAS de la derrota llama a MenuPerdiste.AbrirMejoras, que llama a
                // TiendaMejoras.AbrirEnMenu. Hasta que el menu esta con la tienda tapando todo.
                Lanzar(Secuencia(CaminoDerrota, () => perdiste.AbrirMejoras(), CuadrosMinimos, SegundosMinimos,
                                 () => SceneManager.GetActiveScene().buildIndex == TiendaMejoras.EscenaMenu && TiendaTapando(), 3, 200));
                return;
            }

            case Paso.DesdeLaDerrota:
                if (!secuenciaTerminada) return;
                abrioAlLlegar = ultimaLlego;
                Pasar(Paso.AbiertaAlLlegar);
                return;

            case Paso.AbiertaAlLlegar:
                if (enElPaso < 0.5 || tienda == null) return;
                mascaraAntesDelFinal = camara != null ? camara.cullingMask : SinCamara;
                if (tienda.Abierta) Tocar(tienda.botonVolver);
                Pasar(Paso.CerrarAlFinal);
                return;

            case Paso.CerrarAlFinal:
                if (enElPaso < 0.4) return;
                cerroAlFinal = tienda != null && !tienda.Abierta;
                mascaraAlFinal = camara != null ? camara.cullingMask : SinCamara;
                Pasar(Paso.Listo);
                return;

            case Paso.Listo:
                Terminar(null);
                return;
        }
    }

    // Abre la tienda con MEJORAS si no esta abierta y espera a que tape todo (la camara sin
    // dibujar) y un poco mas. Si MEJORAS no la abrio en un segundo y medio, la abre con
    // Abrir() para seguir (se anota: el camino 1 es el que mira el boton).
    static bool AbrirYEsperar(TiendaMejoras tienda, double enElPaso)
    {
        if (tienda == null) return false;
        if (!tienda.Abierta)
        {
            if (!tocoAbrir)
            {
                tocoAbrir = true;
                Tocar(tienda.botonAbrir);
            }
            else if (enElPaso > 1.5 && !abrioDirecto)
            {
                abrioDirecto = true;
                aperturasDirectas++;
                tienda.Abrir();
            }
            return false;
        }
        if (!TiendaTapando()) { desdeTapando = -1; return false; }
        if (desdeTapando < 0) desdeTapando = enElPaso;
        return enElPaso - desdeTapando >= 0.4;
    }

    // La tienda abierta con el fundido terminado: la camara del menu ya no dibuja.
    static bool TiendaTapando()
    {
        var tienda = Object.FindFirstObjectByType<TiendaMejoras>();
        var camara = Camera.main;
        return tienda != null && tienda.Abierta && camara != null && camara.cullingMask == 0;
    }

    // Un dedo que toca el boton: los mismos eventos que manda el EventSystem (apoyar, soltar,
    // click). Button.OnPointerClick llama a su onClick, y BotonJugoso hace su clic al apoyar.
    static void Tocar(Button boton)
    {
        if (boton == null)
        {
            Debug.LogError("PruebaMenuYTienda: falta el boton a tocar");
            return;
        }
        var dedo = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
        ExecuteEvents.Execute(boton.gameObject, dedo, ExecuteEvents.pointerDownHandler);
        ExecuteEvents.Execute(boton.gameObject, dedo, ExecuteEvents.pointerUpHandler);
        ExecuteEvents.Execute(boton.gameObject, dedo, ExecuteEvents.pointerClickHandler);
    }

    // La corrutina corre en un objeto del juego propio y no en uno de la escena: tiene que
    // seguir viva cuando A JUGAR y MEJORAS cambian de escena. El componente es un EventTrigger
    // (un MonoBehaviour de UnityEngine.UI sin nada que hacer por su cuenta), porque un
    // MonoBehaviour de este ensamblado de editor no se puede agregar a un objeto.
    static void Lanzar(IEnumerator rutina)
    {
        secuenciaTerminada = false;
        if (anfitrion == null)
        {
            var objeto = new GameObject("BancoMenuYTienda");
            Object.DontDestroyOnLoad(objeto);
            anfitrion = objeto.AddComponent<EventTrigger>();
        }
        anfitrion.StartCoroutine(rutina);
    }

    static void Hacer(System.Action accion)
    {
        try
        {
            if (accion != null) accion();
        }
        catch (System.Exception e)
        {
            Debug.LogException(e);
        }
    }

    static bool Cumple(System.Func<bool> listo)
    {
        if (listo == null) return true;
        try
        {
            return listo();
        }
        catch (System.Exception e)
        {
            Debug.LogException(e);
            return false;
        }
    }

    // Unos cuadros antes, la accion al final del ultimo (como si llegara en el cuadro
    // siguiente, que es cuando llega un toque), y los cuadros de despues: por lo menos
    // 'minimo' y 'segundos', hasta que se cumple 'listo', y 'extra' mas. 'maximo' corta.
    static IEnumerator Secuencia(string camino, System.Action accion, int minimo, float segundos,
                                 System.Func<bool> listo, int extra, int maximo)
    {
        for (int i = 0; i < CuadrosAntes; i++)
        {
            yield return new WaitForEndOfFrame();
            Capturar(camino, i, false);
        }
        Hacer(accion);

        float desde = Time.realtimeSinceStartup;
        int n = 0, quedan = -1;
        bool llego = false;
        while (n < maximo)
        {
            yield return new WaitForEndOfFrame();
            Capturar(camino, CuadrosAntes + n, true);
            n++;
            if (quedan < 0)
            {
                if (n >= minimo && Time.realtimeSinceStartup - desde >= segundos && Cumple(listo)) quedan = extra;
            }
            else quedan--;
            if (quedan == 0)
            {
                llego = true;
                break;
            }
        }
        ultimaLlego = llego;
        secuenciaTerminada = true;
    }

    // Las dos referencias: el menu quieto y, en el cuadro siguiente, el mismo menu con la
    // camara apagada a proposito (lo que se veria si la tienda la apagara antes de tiempo o no
    // la devolviera). Se vuelve a prender enseguida.
    static IEnumerator Referencias()
    {
        var camara = Camera.main;
        int ancho, alto;
        yield return new WaitForEndOfFrame();
        Color32[] menu = Foto(out ancho, out alto);
        int mascara = camara != null ? camara.cullingMask : 0;
        if (camara != null) camara.cullingMask = 0;
        yield return new WaitForEndOfFrame();
        Color32[] vacia = Foto(out ancho, out alto);
        if (camara != null) camara.cullingMask = mascara;

        try
        {
            double media;
            if (menu != null)
            {
                Medir(menu, out desvioMenu, out fondoMenu, out media);
                GuardarPng(menu, ancho, alto, "0_referencia_menu");
            }
            if (vacia != null)
            {
                Medir(vacia, out desvioVacia, out fondoVacia, out media);
                GuardarPng(vacia, ancho, alto, "0_referencia_escena_vacia");
            }
            midioReferencias = menu != null && vacia != null;
        }
        catch (System.Exception e)
        {
            Debug.LogException(e);
        }
        secuenciaTerminada = true;
    }

    // Ocho cuadros seguidos del menu quieto cambiando el HDR de la camara de a pares. El HDR de
    // cada cuadro se fija al final del anterior (el primero, al lanzar la corrutina, entre
    // cuadros). Al terminar vuelve al HDR con que la dejo FondoMenu.
    static IEnumerator CompararHdr()
    {
        var camara = Camera.main;
        var fotos = new Color32[SecuenciaHdr.Length][];
        int ancho = 0, alto = 0;
        for (int i = 0; i < SecuenciaHdr.Length; i++)
        {
            if (camara != null) camara.allowHDR = SecuenciaHdr[i];
            yield return new WaitForEndOfFrame();
            hdrLeido[i] = camara != null && camara.allowHDR;
            fotos[i] = Foto(out ancho, out alto);
        }
        if (camara != null) camara.allowHDR = hdrAlEmpezar;

        try
        {
            double igual = 0, cambio = 0;
            int nIgual = 0, nCambio = 0;
            for (int i = 0; i < fotos.Length; i++)
            {
                if (fotos[i] == null) continue;
                double desvio, fondo, media;
                Medir(fotos[i], out desvio, out fondo, out media);
                luminanciasHdr[i] = media;
                if (i == 0 || fotos[i - 1] == null || fotos[i - 1].Length != fotos[i].Length) continue;
                double d = Diferencia(fotos[i - 1], fotos[i]);
                diferenciasHdr[i] = d;
                if (SecuenciaHdr[i] == SecuenciaHdr[i - 1]) { igual += d; nIgual++; }
                else { cambio += d; nCambio++; }
            }
            if (nIgual > 0) diferenciaIgual = igual / nIgual;
            if (nCambio > 0) diferenciaCambio = cambio / nCambio;
            midioHdr = nIgual == 4 && nCambio == 3;

            // El cuadro 1 es sin HDR y el 2 con HDR: las dos fotos y la diferencia, aumentada.
            if (fotos[1] != null) GuardarPng(fotos[1], ancho, alto, "0_menu_sin_hdr");
            if (fotos[2] != null) GuardarPng(fotos[2], ancho, alto, "0_menu_con_hdr");
            if (fotos[1] != null && fotos[2] != null && fotos[1].Length == fotos[2].Length)
                GuardarPng(ImagenDeDiferencia(fotos[1], fotos[2], 8), ancho, alto, "0_menu_diferencia_hdr_x8");
        }
        catch (System.Exception e)
        {
            Debug.LogException(e);
        }
        secuenciaTerminada = true;
    }

    // Lo que se ve en la pantalla al final de este cuadro (con la UI en overlay), a la mitad
    // del tamanio: un pixel de cada dos en cada eje. Se llama despues de WaitForEndOfFrame.
    static Color32[] Foto(out int ancho, out int alto)
    {
        ancho = alto = 0;
        Texture2D foto = null;
        try
        {
            foto = ScreenCapture.CaptureScreenshotAsTexture();
            if (foto == null) return null;
            int w = foto.width, h = foto.height;
            Color32[] pixeles = foto.GetPixels32();
            ancho = Mathf.Max(1, w / 2);
            alto = Mathf.Max(1, h / 2);
            var chica = new Color32[ancho * alto];
            for (int y = 0; y < alto; y++)
            {
                int origen = Mathf.Min(y * 2, h - 1) * w;
                int destino = y * ancho;
                for (int x = 0; x < ancho; x++)
                {
                    Color32 c = pixeles[origen + Mathf.Min(x * 2, w - 1)];
                    c.a = 255;
                    chica[destino + x] = c;
                }
            }
            return chica;
        }
        catch (System.Exception e)
        {
            Debug.LogException(e);
            return null;
        }
        finally
        {
            if (foto != null) Object.Destroy(foto);
        }
    }

    // El desvio de la luminancia (Rec. 709, de 0 a 1), la fraccion de pixeles del color de
    // fondo de la camara del menu y la luminancia media.
    static void Medir(Color32[] pixeles, out double desvio, out double fondo, out double media)
    {
        double suma = 0, suma2 = 0;
        int enFondo = 0;
        for (int i = 0; i < pixeles.Length; i++)
        {
            Color32 c = pixeles[i];
            double l = (0.2126 * c.r + 0.7152 * c.g + 0.0722 * c.b) / 255.0;
            suma += l;
            suma2 += l * l;
            if (System.Math.Abs(c.r - colorFondo.r) <= ToleranciaFondo &&
                System.Math.Abs(c.g - colorFondo.g) <= ToleranciaFondo &&
                System.Math.Abs(c.b - colorFondo.b) <= ToleranciaFondo) enFondo++;
        }
        int total = System.Math.Max(1, pixeles.Length);
        media = suma / total;
        desvio = System.Math.Sqrt(System.Math.Max(0.0, suma2 / total - media * media));
        fondo = (double)enFondo / total;
    }

    // La diferencia media por canal entre dos fotos del mismo tamanio, de 0 a 255.
    static double Diferencia(Color32[] a, Color32[] b)
    {
        long suma = 0;
        for (int i = 0; i < a.Length; i++)
            suma += System.Math.Abs(a[i].r - b[i].r) + System.Math.Abs(a[i].g - b[i].g) + System.Math.Abs(a[i].b - b[i].b);
        return suma / (3.0 * System.Math.Max(1, a.Length));
    }

    static Color32[] ImagenDeDiferencia(Color32[] a, Color32[] b, int aumento)
    {
        var d = new Color32[a.Length];
        for (int i = 0; i < a.Length; i++)
        {
            int r = System.Math.Min(255, System.Math.Abs(a[i].r - b[i].r) * aumento);
            int g = System.Math.Min(255, System.Math.Abs(a[i].g - b[i].g) * aumento);
            int bl = System.Math.Min(255, System.Math.Abs(a[i].b - b[i].b) * aumento);
            d[i] = new Color32((byte)r, (byte)g, (byte)bl, 255);
        }
        return d;
    }

    static void GuardarPng(Color32[] pixeles, int ancho, int alto, string nombre)
    {
        var textura = new Texture2D(ancho, alto, TextureFormat.RGBA32, false);
        try
        {
            textura.SetPixels32(pixeles);
            textura.Apply(false);
            File.WriteAllBytes(Path.Combine(Path.GetFullPath(Carpeta), nombre + ".png"), textura.EncodeToPNG());
        }
        finally
        {
            Object.Destroy(textura);
        }
    }

    // Captura el cuadro que se acaba de dibujar y anota como estaban la escena, la camara y la
    // tienda al dibujarlo (despues de WaitForEndOfFrame ya no cambia nada hasta el cuadro
    // siguiente).
    static void Capturar(string camino, int indice, bool despues)
    {
        var c = new Cuadro { camino = camino, indice = indice, despues = despues, mascara = SinCamara, alfa = -1f };
        try
        {
            c.escena = SceneManager.GetActiveScene().buildIndex;
            var camara = Camera.main;
            c.mascara = camara != null ? camara.cullingMask : SinCamara;
            var tienda = Object.FindFirstObjectByType<TiendaMejoras>();
            c.abierta = tienda != null && tienda.Abierta;
            c.panel = tienda != null && tienda.panel != null && tienda.panel.activeInHierarchy;
            c.alfa = tienda != null && tienda.grupoPanel != null ? tienda.grupoPanel.alpha : -1f;

            int ancho, alto;
            Color32[] pixeles = Foto(out ancho, out alto);
            if (pixeles == null)
            {
                cuadrosSinFoto++;
                Debug.LogError("PruebaMenuYTienda: no salio la captura del cuadro " + camino + " " + indice);
            }
            else
            {
                double media;
                Medir(pixeles, out c.desvio, out c.fondo, out media);
                c.conFoto = true;
                GuardarPng(pixeles, ancho, alto, camino + "_" + indice.ToString("00"));
            }
        }
        catch (System.Exception e)
        {
            Debug.LogException(e);
        }
        cuadros.Add(c);
    }

    // Las referencias sirven si la escena vacia se separa del menu en la fraccion de fondo
    // y el menu no es liso. Si no sirven, no se juzga "escena vacia" (y falla su chequeo).
    static bool ReferenciasSirven()
    {
        return midioReferencias && fondoVacia - fondoMenu >= DistanciaMinimaDeReferencias && desvioMenu >= UmbralLiso;
    }

    // A mitad de camino entre las dos referencias.
    static double UmbralVacio()
    {
        return ReferenciasSirven() ? (fondoMenu + fondoVacia) * 0.5 : 2.0;
    }

    static bool EsLiso(Cuadro c)
    {
        return c.conFoto && c.desvio < UmbralLiso;
    }

    // Solo en el menu: el color de fondo es el de su camara.
    static bool EsVacio(Cuadro c, double umbral)
    {
        return c.conFoto && c.escena == TiendaMejoras.EscenaMenu && c.fondo >= umbral;
    }

    // La camara del menu sin dibujar y sin la tienda opaca encima: el cuadro que no tiene que
    // existir nunca.
    static bool CamaraSinTienda(Cuadro c)
    {
        return c.escena == TiendaMejoras.EscenaMenu && c.mascara == 0 && !(c.panel && c.alfa >= 0.999f);
    }

    // En A JUGAR solo cuentan los cuadros del menu (los de la partida que carga son otra cosa).
    static bool Juzgado(Cuadro c)
    {
        return c.camino != CaminoJugar || c.escena == TiendaMejoras.EscenaMenu;
    }

    static void Contar(string camino, double umbral, out int total, out int juzgados, out int lisos, out int vacios, out int sinFoto)
    {
        total = juzgados = lisos = vacios = sinFoto = 0;
        foreach (var c in cuadros)
        {
            if (c.camino != camino) continue;
            total++;
            if (!Juzgado(c)) continue;
            juzgados++;
            if (!c.conFoto) sinFoto++;
            if (EsLiso(c)) lisos++;
            if (EsVacio(c, umbral)) vacios++;
        }
    }

    static bool SinParpadeo(string camino, double umbral)
    {
        int total, juzgados, lisos, vacios, sinFoto;
        Contar(camino, umbral, out total, out juzgados, out lisos, out vacios, out sinFoto);
        return juzgados > 0 && lisos == 0 && vacios == 0 && sinFoto == 0;
    }

    static Cuadro PrimeroDespues(string camino)
    {
        foreach (var c in cuadros)
            if (c.camino == camino && c.despues) return c;
        return null;
    }

    static int MascaraDe(Cuadro c)
    {
        return c != null ? c.mascara : SinCamara;
    }

    static string Mascara(int m)
    {
        return m == SinCamara ? "sin camara" : m.ToString();
    }

    static bool EsLaOriginal(int m)
    {
        return m != SinCamara && m != 0 && m == mascaraOriginal;
    }

    static string Linea(Cuadro c, double umbral)
    {
        var s = new StringBuilder();
        s.Append("  ").Append(c.indice.ToString("00")).Append(c.despues ? " despues" : " antes  ");
        s.Append("  escena ").Append(c.escena);
        if (c.conFoto) s.Append(": desvio ").Append(c.desvio.ToString("0.000")).Append(", fondo ").Append(c.fondo.ToString("0.00"));
        else s.Append(": SIN FOTO");
        s.Append(", cullingMask ").Append(Mascara(c.mascara));
        s.Append(", tienda ").Append(c.abierta ? "abierta" : "cerrada");
        s.Append(" (panel ").Append(c.panel ? "prendido" : "apagado").Append(", alfa ").Append(c.alfa.ToString("0.00")).Append(')');
        if (!Juzgado(c)) s.Append("  [no se juzga: ya es la partida]");
        if (EsLiso(c) && Juzgado(c)) s.Append("  <- LISO");
        if (EsVacio(c, umbral) && Juzgado(c)) s.Append("  <- ESCENA VACIA");
        if (CamaraSinTienda(c)) s.Append("  <- CAMARA SIN DIBUJAR Y SIN LA TIENDA ENCIMA");
        return s.ToString();
    }

    static void Terminar(string error)
    {
        // Lo que la partida y la derrota tocan y el juego devuelve solo; por las dudas, si el
        // banco se corto a mitad de camino.
        Time.timeScale = 1f;
        AudioListener.pause = false;
        if (anfitrion != null)
        {
            anfitrion.StopAllCoroutines();
            Object.Destroy(anfitrion.gameObject);
        }
        anfitrion = null;

        double umbral = UmbralVacio();
        var inf = new StringBuilder();
        inf.AppendLine("Prueba del menu y la tienda: abrir y cerrar sin parpadeo, y el menu sin HDR (Menu.unity)");
        inf.AppendLine();
        if (error != null) inf.AppendLine("ERROR: " + error);
        inf.AppendLine("Pantalla: " + anchoPantalla + " x " + altoPantalla + "; capturas a la mitad en Builds/menu_tienda/");
        inf.AppendLine("Camara del menu como la dejo FondoMenu: allowHDR " + hdrAlEmpezar + ", allowMSAA " + msaaAlEmpezar
                       + ", cullingMask " + Mascara(mascaraOriginal) + ", color de fondo " + colorFondo);
        inf.AppendLine("Referencia del menu quieto: desvio " + desvioMenu.ToString("0.000") + ", fondo " + fondoMenu.ToString("0.00")
                       + "; con la camara apagada a proposito (escena vacia): desvio " + desvioVacia.ToString("0.000")
                       + ", fondo " + fondoVacia.ToString("0.00"));
        inf.AppendLine("Umbrales: liso con desvio de la luminancia menor a " + UmbralLiso.ToString("0.000")
                       + "; escena vacia con la fraccion de fondo (tolerancia " + ToleranciaFondo + "/255) desde "
                       + (ReferenciasSirven() ? umbral.ToString("0.00") + " (a mitad de camino entre las referencias)" : "- (las referencias no sirven: no se juzga)"));

        inf.AppendLine("HDR, ocho cuadros seguidos del menu quieto (diferencia media con el anterior, de 0 a 255, y luminancia media):");
        for (int i = 0; i < SecuenciaHdr.Length; i++)
            inf.AppendLine("  cuadro " + i + ": allowHDR " + hdrLeido[i] + ", diferencia " + (diferenciasHdr[i] < 0 ? "-" : diferenciasHdr[i].ToString("0.00"))
                           + ", luminancia " + (luminanciasHdr[i] < 0 ? "-" : luminanciasHdr[i].ToString("0.000")));
        double agrega = diferenciaCambio - diferenciaIgual;
        inf.AppendLine("  entre cuadros con el mismo HDR: " + diferenciaIgual.ToString("0.00") + "; cambiando el HDR: " + diferenciaCambio.ToString("0.00")
                       + "; lo que agrega el HDR: " + agrega.ToString("0.00") + " (tope " + UmbralHdr.ToString("0.0") + ")");

        foreach (string camino in Caminos)
        {
            int total, juzgados, lisos, vacios, sinFoto;
            Contar(camino, umbral, out total, out juzgados, out lisos, out vacios, out sinFoto);
            inf.AppendLine("Camino " + camino + ": " + total + " cuadros (" + juzgados + " juzgados), lisos " + lisos
                           + ", escena vacia " + vacios + ", sin foto " + sinFoto);
            foreach (var c in cuadros)
                if (c.camino == camino) inf.AppendLine(Linea(c, umbral));
        }

        Cuadro ultimoDelMenu = null, primeroDeLaPartida = null;
        foreach (var c in cuadros)
        {
            if (c.camino != CaminoJugar) continue;
            if (c.escena == TiendaMejoras.EscenaMenu) ultimoDelMenu = c;
            else if (primeroDeLaPartida == null) primeroDeLaPartida = c;
        }
        inf.AppendLine("A JUGAR: ultimo cuadro del menu antes de cargar: "
                       + (ultimoDelMenu != null ? ultimoDelMenu.indice.ToString("00") + " (desvio " + ultimoDelMenu.desvio.ToString("0.000") + ", fondo " + ultimoDelMenu.fondo.ToString("0.00") + ")" : "ninguno")
                       + "; primero de la partida: "
                       + (primeroDeLaPartida != null ? primeroDeLaPartida.indice.ToString("00") + " (escena " + primeroDeLaPartida.escena + ", desvio " + primeroDeLaPartida.desvio.ToString("0.000") + ")" : "ninguno")
                       + "; escena al terminar: " + escenaTrasJugar);

        int camaraSinTienda = 0;
        foreach (var c in cuadros)
            if (CamaraSinTienda(c)) camaraSinTienda++;

        Cuadro trasVolver = PrimeroDespues(CaminoVolver), trasAtras = PrimeroDespues(CaminoAtras), trasApagar = PrimeroDespues(CaminoApagar);
        inf.AppendLine("cullingMask de la camara del menu (original " + Mascara(mascaraOriginal) + "):");
        inf.AppendLine("  VOLVER: antes " + Mascara(mascaraAntesDeVolver) + ", primer cuadro despues " + Mascara(MascaraDe(trasVolver))
                       + ", al terminar " + Mascara(mascaraTrasVolver) + "; la tienda se cerro: " + cerroConVolver);
        inf.AppendLine("  atras: antes " + Mascara(mascaraAntesDelAtras) + ", primer cuadro despues " + Mascara(MascaraDe(trasAtras))
                       + ", al terminar " + Mascara(mascaraTrasAtras) + "; la tienda se cerro: " + cerroConAtras);
        inf.AppendLine("  apagada de golpe: antes " + Mascara(mascaraAntesDeApagar) + ", en el acto " + Mascara(mascaraAlApagar)
                       + ", primer cuadro despues " + Mascara(MascaraDe(trasApagar)) + ", un rato despues " + Mascara(mascaraApagada)
                       + "; prendida otra vez y cerrada " + Mascara(mascaraAlReprender) + " (menu a la vista: " + menuTrasReprender + ")");
        inf.AppendLine("  abierta al llegar desde la derrota: se abrio sola " + abrioAlLlegar + "; antes de cerrarla " + Mascara(mascaraAntesDelFinal)
                       + ", despues " + Mascara(mascaraAlFinal) + "; se cerro: " + cerroAlFinal);
        inf.AppendLine("Cuadros con la camara del menu sin dibujar y sin la tienda opaca encima: " + camaraSinTienda + " de " + cuadros.Count);
        inf.AppendLine("Veces que MEJORAS no abrio la tienda y hubo que llamar a Abrir(): " + aperturasDirectas);
        inf.AppendLine("Errores y excepciones durante la prueba: " + cuantasExcepciones);
        if (cuantasExcepciones > 0) inf.Append(excepciones);
        inf.AppendLine();

        bool[] ok =
        {
            error == null && cuantasExcepciones == 0,
            cuadros.Count > 0 && cuadrosSinFoto == 0,
            ReferenciasSirven(),
            midioLaCamara && !hdrAlEmpezar && !msaaAlEmpezar,
            midioHdr && agrega <= UmbralHdr,
            abrioConMejoras,
            SinParpadeo(CaminoAbrir, umbral),
            SinParpadeo(CaminoVolver, umbral),
            cerroConVolver && mascaraAntesDeVolver == 0 && EsLaOriginal(MascaraDe(trasVolver)) && EsLaOriginal(mascaraTrasVolver),
            SinParpadeo(CaminoAtras, umbral),
            cerroConAtras && mascaraAntesDelAtras == 0 && EsLaOriginal(MascaraDe(trasAtras)) && EsLaOriginal(mascaraTrasAtras),
            mascaraAntesDeApagar == 0 && EsLaOriginal(mascaraAlApagar) && EsLaOriginal(MascaraDe(trasApagar)) && EsLaOriginal(mascaraApagada),
            SinParpadeo(CaminoApagar, umbral),
            escenaTrasJugar == TiendaMejoras.EscenaOleadas || escenaTrasJugar == TiendaMejoras.EscenaModoLibre,
            ultimoDelMenu != null && ultimoDelMenu.conFoto && !EsLiso(ultimoDelMenu) && !EsVacio(ultimoDelMenu, umbral) && SinParpadeo(CaminoJugar, umbral),
            abrioAlLlegar,
            SinParpadeo(CaminoDerrota, umbral),
            cerroAlFinal && mascaraAntesDelFinal == 0 && EsLaOriginal(mascaraAlFinal),
            cuadros.Count > 0 && camaraSinTienda == 0,
        };
        string[] que =
        {
            "el banco llego hasta el final, sin errores ni excepciones",
            "salieron todas las capturas de pantalla",
            "la medicion distingue el menu de una escena vacia (las dos referencias se separan)",
            "FondoMenu deja la camara del menu sin HDR ni MSAA",
            "el menu se ve igual sin HDR (lo que el HDR agrega a la diferencia entre cuadros no pasa del tope)",
            "MEJORAS abre la tienda y, terminado el fundido, la camara del menu deja de dibujar",
            "abrir con MEJORAS: ningun cuadro liso ni de escena vacia",
            "cerrar con VOLVER: ningun cuadro liso ni de escena vacia",
            "VOLVER cierra la tienda y la camara vuelve a dibujar en el mismo cuadro (cullingMask como antes)",
            "cerrar con el atras (BotonAtrasMenu, el Escape): ningun cuadro liso ni de escena vacia",
            "el atras cierra la tienda y la camara vuelve a dibujar en el mismo cuadro",
            "si la tienda se apaga de golpe abierta, la camara vuelve a dibujar en el acto",
            "con la tienda apagada de golpe, ningun cuadro liso ni de escena vacia",
            "A JUGAR carga una partida",
            "A JUGAR: ni el ultimo cuadro del menu antes de cargar ni los de antes son lisos o de escena vacia",
            "MEJORAS desde la derrota carga el menu con la tienda abierta, tapando todo",
            "MEJORAS desde la derrota: ningun cuadro liso ni de escena vacia",
            "cerrar la tienda que se abrio al llegar tambien le devuelve la camara a la escena",
            "en ningun cuadro la camara del menu deja de dibujar sin la tienda opaca encima",
        };
        bool todo = true;
        for (int i = 0; i < ok.Length; i++)
        {
            inf.AppendLine((ok[i] ? "OK  " : "FALLA  ") + que[i]);
            todo &= ok[i];
        }
        inf.AppendLine();
        inf.AppendLine("RESULTADO: " + (todo ? "TODO OK" : "HAY FALLAS"));

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(Ruta)));
        File.WriteAllText(Ruta, inf.ToString());

        SessionState.SetBool(Clave, false);
        EditorApplication.ExitPlaymode();
        PlayerSettings.runInBackground = false;
        AssetDatabase.SaveAssets();
        Debug.Log(inf.ToString());
    }
}
