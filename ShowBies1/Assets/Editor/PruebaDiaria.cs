using System.IO;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Banco de la recompensa diaria antes que la tienda. Lo que se arreglo: MEJORAS de la
// derrota carga el menu con la tienda abierta (TiendaMejoras.AbrirAlCargarMenu), y con la
// diaria disponible la tienda salia primero y la diaria no salia nunca (el circuito de la
// primera vez, MEJORAS, comprar, A JUGAR, carga la partida sin cerrar la tienda). Ahora la
// diaria sale primero, prende VentanaRecompensaDiaria.Ocupada, y la tienda espera a que
// termine de irse para abrirse sola.
//
// Entra en play en la derrota (Perdiste, abierta sola) y recorre cinco casos, cada uno
// cargando el menu de nuevo (SceneManager.LoadScene(0), por TiendaMejoras.AbrirEnMenu):
//  1. MEJORAS de la derrota (MenuPerdiste.AbrirMejoras, el boton de verdad) con la diaria
//     disponible: sale la diaria y la tienda no se abre mientras esta.
//  2. COBRAR (el onClick del boton que arma la ventana): suben las monedas lo que dice el
//     boton, y cuando la ventana termina de irse la tienda se abre sola.
//  3. El atras de Android (BotonAtrasMenu.Atras, lo que corre con Escape): cierra la diaria
//     sin cobrar y la tienda se abre despues.
//  4. Con video (un proveedor del banco, por ServicioAnuncios.UsarParaPruebas): despues de
//     cobrar la ventana ofrece VIDEO: +N MAS y espera; al premiarlo cobra el duplicado una
//     sola vez y la tienda se abre despues.
//  5. Sin diaria (ya cobrada hoy): la tienda abre enseguida, como siempre.
//
// Lo que pasa cuadro a cuadro (que la tienda y la diaria esten a la vez un solo cuadro) se
// mira con Canvas.willRenderCanvases, que corre en cada cuadro del juego, ademas de en cada
// Tick del editor.
//
// El progreso se prepara EN PLAY, con la API publica de Progreso y guardando en disco: cada
// caso necesita otro estado (disponible, cobrada hoy) y el cobro lo cambia. En modo edicion
// no se toca: Progreso puede tener en memoria lo que dejo otro banco (al salir de play no se
// recarga el dominio), y un Guardar ahi lo escribiria encima del archivo. RespaldoDelBanco
// guarda y devuelve el progreso y los PlayerPrefs.
//
// Escribe Builds/prueba_diaria.txt y dos capturas: Builds/diaria_antes_que_la_tienda.png y
// Builds/diaria_oferta_video.png.
//
// OJO: entrar y salir de play hace que Unity vuelva a serializar ProjectSettings (ver la
// trampa en CLAUDE.md): despues de correrlo, revertir lo que no se toco a proposito.
[InitializeOnLoad]
public static class PruebaDiaria
{
    const string Clave = "ShowBies.PruebaDiaria";
    const string Ruta = "../Builds/prueba_diaria.txt";

    const double SegundosParaAsentar = 1.5;     // la derrota del arranque, que no se mide
    const double SegundosParaCapturar = 0.7;    // la entrada de la diaria dura 0,45 s
    const double SegundosMirando = 1.2;         // con la diaria abierta, antes de tocarla
    const double SegundosConLaOferta = 2.0;     // mas que los 1,3 + 0,25 s en que se iria sola
    const double SegundosSinDiaria = 1.0;
    const double TopeEsperando = 8.0;           // cargar el menu, que salga algo
    const double TopeTotal = 120.0;

    // El ultimo cobro fue ayer con esta racha: hoy toca el dia 3.
    const int RachaDeAyer = 2;

    // "Enseguida": la tienda mira Ocupada en su Update, que puede correr antes o despues del
    // de la diaria en el mismo cuadro.
    const int CuadrosDeMargen = 2;
    const float SegundosDeMargen = 0.25f;

    enum Paso
    {
        Asentar,
        CobrarCargar, CobrarEsperarDiaria, CobrarMirar, CobrarEsperarTienda,
        AtrasCargar, AtrasEsperarDiaria, AtrasMirar, AtrasEsperarTienda,
        VideoCargar, VideoEsperarDiaria, VideoMirar, VideoOferta, VideoEsperarTienda,
        SinDiariaCargar, SinDiariaEsperarTienda, SinDiariaMirar,
        Listo
    }

    // Lo que se mide de cada carga del menu. Los cuadros son Time.frameCount; las horas,
    // Time.realtimeSinceStartup.
    class Carga
    {
        public readonly string nombre;
        public bool cargado;
        public TiendaMejoras tienda;
        public VentanaRecompensaDiaria diaria;
        public BotonAtrasMenu atras;
        public bool disponibleAlPreparar;
        public int cuadroCarga = -1, cuadroDiaria = -1, cuadroDiariaSeFue = -1, cuadroTienda = -1;
        public float horaCarga = -1f, horaDiaria = -1f, horaDiariaSeFue = -1f, horaTienda = -1f;
        public bool vioLaDiaria;              // abierta u ocupada en algun cuadro
        public int cuadrosEncimados;          // cuadros con la tienda abierta y la diaria ocupada
        public int ultimoEncimado = -1;

        public Carga(string nombre)
        {
            this.nombre = nombre;
        }
    }

    // El proveedor de anuncios del banco: con video o sin, y si se le pide, lo "muestra"
    // entero en el acto. El aviso se encola y lo resuelve VigiaAplicacion en su Update, como
    // con uno de verdad. No es el ProveedorFalso: ese espera 5 s y un toque en LISTO.
    class ProveedorDelBanco : IProveedorAnuncios
    {
        public bool hayVideo;
        public int veces;

        public string Nombre { get { return "banco"; } }
        public void Inicializar() { }
        public bool Listo(string lugar) { return hayVideo; }

        public void Mostrar(string lugar, System.Action<ResultadoAnuncio> alTerminar)
        {
            veces++;
            if (alTerminar != null) alTerminar(ResultadoAnuncio.Recompensado);
        }
    }

    static Paso paso;
    static double inicio, pasoDesde;
    static bool llegoAlFinal, capturo;
    static int muestras;
    static Carga actual, cargaCobrar, cargaAtras, cargaVideo, cargaSinDiaria;
    static ProveedorDelBanco proveedor;
    static ConfigAnuncios config;
    static string proveedorDelAsset = "?";
    static bool porElBotonDeLaDerrota, soloLectura;
    static int partidas;

    // 1 y 2: MEJORAS de la derrota y COBRAR.
    static bool tiendaCerradaConLaDiaria, cobroHecho, videoSinProveedor, disponibleTrasCobrar;
    static int rachaCobrar;
    static double montoCobrar, monedasAntesCobrar, monedasDespuesCobrar;
    static string textoCobrar;
    static float horaCobro;

    // 3: el atras de Android.
    static bool tiendaCerradaAntesDelAtras, atrasHecho, disponibleTrasAtras;
    static double monedasAntesAtras, monedasDespuesAtras;

    // 4: con video.
    static bool tiendaCerradaAntesDelVideo, ofertaVisible, volverVisible, diariaEsperaLaEleccion, videoTerminado;
    static bool audioPausadoTrasVideo, mostrandoTrasVideo;
    static int rachaVideo, videosAntes, videosPedidos;
    static double montoVideo, monedasAntesVideo, monedasTrasCobrarVideo, monedasTrasVideo, monedasTrasSegundo;
    static double paraDuplicarTrasVideo, segundoDuplicado;
    static string textoVideo;

    // 5: sin diaria.
    static bool sinDiariaMirado;

    // Las excepciones y errores que salten mientras corre.
    static readonly StringBuilder excepciones = new StringBuilder();
    static int cuantasExcepciones;

    static PruebaDiaria()
    {
        EditorApplication.update += Tick;
        Application.logMessageReceived += AnotarExcepcion;
        EditorApplication.playModeStateChanged += AlCambiarDeModo;
    }

    static void AnotarExcepcion(string mensaje, string pila, LogType tipo)
    {
        if (!SessionState.GetBool(Clave, false)) return;
        if (tipo != LogType.Exception && tipo != LogType.Error) return;
        cuantasExcepciones++;
        if (cuantasExcepciones <= 3) excepciones.AppendLine("  " + mensaje + System.Environment.NewLine + pila);
    }

    [MenuItem("ShowBies/Pruebas/Diaria antes que la tienda (play)")]
    // Publico para poder correrlo por codigo (ver la trampa del registro de menus).
    public static void Arrancar()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("PruebaDiaria: el editor ya esta en play; se arranca desde el modo edicion.");
            return;
        }
        RespaldoDelBanco.Guardar("PruebaDiaria");
        excepciones.Length = 0;
        cuantasExcepciones = 0;
        PlayerSettings.runInBackground = true;
        // Arranca en la derrota, abierta sola: el primer caso sale del boton MEJORAS de
        // verdad, desde una escena sin diaria, que es el camino que se arreglo.
        EditorSceneManager.OpenScene("Assets/Escenas/Perdiste.unity");
        SessionState.SetBool(Clave, true);
        SessionState.SetBool(Clave + ".empezo", false);
        SessionState.SetBool(Clave + ".olvidarProgreso", false);
        EditorApplication.EnterPlaymode();
    }

    static void Tick()
    {
        if (!SessionState.GetBool(Clave, false)) return;
        if (!EditorApplication.isPlaying) return;

        double ahora = EditorApplication.timeSinceStartup;
        if (!SessionState.GetBool(Clave + ".empezo", false))
        {
            SessionState.SetBool(Clave + ".empezo", true);
            Empezar(ahora);
            return;
        }

        try
        {
            if (ahora - inicio > TopeTotal)
            {
                Terminar("se paso del tope total de " + TopeTotal + " s, en el paso " + paso);
                return;
            }
            if (ahora - pasoDesde > Tope(paso))
            {
                Terminar("se trabo en el paso " + paso + " (tope de " + Tope(paso) + " s)");
                return;
            }
            Vigilar();
            Avanzar(ahora);
        }
        catch (System.Exception e)
        {
            Terminar("el banco tiro una excepcion en el paso " + paso + ": " + e.Message);
        }
    }

    static void Empezar(double ahora)
    {
        inicio = ahora;
        paso = Paso.Asentar;
        pasoDesde = ahora;
        llegoAlFinal = capturo = false;
        muestras = 0;
        cargaCobrar = new Carga("cobrar");
        cargaAtras = new Carga("atras");
        cargaVideo = new Carga("video");
        cargaSinDiaria = new Carga("sin diaria");
        actual = null;
        porElBotonDeLaDerrota = soloLectura = false;
        partidas = -1;

        tiendaCerradaConLaDiaria = cobroHecho = false;
        videoSinProveedor = disponibleTrasCobrar = true;
        rachaCobrar = -1;
        montoCobrar = monedasAntesCobrar = monedasDespuesCobrar = -1;
        textoCobrar = null;
        horaCobro = -1f;

        tiendaCerradaAntesDelAtras = atrasHecho = disponibleTrasAtras = false;
        monedasAntesAtras = monedasDespuesAtras = -1;

        tiendaCerradaAntesDelVideo = ofertaVisible = volverVisible = diariaEsperaLaEleccion = videoTerminado = false;
        audioPausadoTrasVideo = mostrandoTrasVideo = true;
        rachaVideo = -1;
        videosAntes = 0;
        videosPedidos = -1;
        montoVideo = monedasAntesVideo = monedasTrasCobrarVideo = monedasTrasVideo = monedasTrasSegundo = -1;
        paraDuplicarTrasVideo = segundoDuplicado = -1;
        textoVideo = null;

        sinDiariaMirado = false;
        // Los errores no se borran aca: los de la carga de la derrota del arranque, antes del
        // primer Tick, tambien cuentan. Se borran en Arrancar (y al entrar en play se recarga
        // el dominio, que los deja en cero de todos modos).

        // El proveedor y la config del banco: con la del asset, el x2 de la diaria pide 2
        // partidas, 180 s jugados, menos de 3 videos hoy y 60 s entre videos, y el proveedor
        // del asset puede ser el falso (5 s y un toque) o el nulo (nunca hay video).
        proveedorDelAsset = ConfigAnuncios.Instancia != null ? ConfigAnuncios.Instancia.proveedor.ToString() : "sin asset";
        proveedor = new ProveedorDelBanco();
        config = ScriptableObject.CreateInstance<ConfigAnuncios>();
        config.hideFlags = HideFlags.DontSave;
        config.partidasTerminadasMinimas = 0;
        config.segundosJugadosMinimos = 0f;
        config.vecesPorDia = 1000;
        config.vecesPorPartida = 1000;
        config.segundosEntreAnuncios = 0f;
        config.fallasPremiadasPorDia = 0;

        SceneManager.sceneLoaded -= AlCargarEscena;
        SceneManager.sceneLoaded += AlCargarEscena;
        Canvas.willRenderCanvases -= Muestrear;
        Canvas.willRenderCanvases += Muestrear;
    }

    static double Tope(Paso p)
    {
        switch (p)
        {
            case Paso.Asentar: return SegundosParaAsentar + 10.0;
            case Paso.CobrarMirar:
            case Paso.AtrasMirar:
            case Paso.VideoMirar: return SegundosMirando + 5.0;
            case Paso.VideoOferta: return SegundosConLaOferta + 5.0;
            case Paso.SinDiariaMirar: return SegundosSinDiaria + 5.0;
            default: return TopeEsperando;
        }
    }

    static void Pasar(Paso siguiente, double ahora)
    {
        paso = siguiente;
        pasoDesde = ahora;
        capturo = false;
    }

    static void Avanzar(double ahora)
    {
        double pasado = ahora - pasoDesde;
        switch (paso)
        {
            case Paso.Asentar:
                if (pasado < SegundosParaAsentar) return;
                Pasar(Paso.CobrarCargar, ahora);
                return;

            // --- 1 y 2: MEJORAS de la derrota con la diaria disponible, y COBRAR ------
            case Paso.CobrarCargar:
            {
                Preparar(cargaCobrar, true, false);
                soloLectura = Progreso.SoloLectura;
                partidas = Progreso.PartidasTerminadas;
                // El boton MEJORAS de la derrota; si la escena no lo tiene, lo mismo que llama.
                var derrota = Object.FindFirstObjectByType<MenuPerdiste>();
                porElBotonDeLaDerrota = derrota != null;
                if (derrota != null) derrota.AbrirMejoras();
                else TiendaMejoras.AbrirEnMenu();
                Pasar(Paso.CobrarEsperarDiaria, ahora);
                return;
            }

            case Paso.CobrarEsperarDiaria:
                if (!DiariaALaVista(cargaCobrar)) return;
                Pasar(Paso.CobrarMirar, ahora);
                return;

            case Paso.CobrarMirar:
            {
                if (!capturo && pasado >= SegundosParaCapturar)
                {
                    capturo = true;
                    Capturar("diaria_antes_que_la_tienda");
                }
                if (pasado < SegundosMirando) return;
                tiendaCerradaConLaDiaria = TiendaCerradaConLaDiaria(cargaCobrar);
                montoCobrar = MontoDeHoy(out rachaCobrar);
                var cobrar = BotonDeLaDiaria(cargaCobrar.diaria, "BotonCobrar");
                if (cobrar == null) { Terminar("no aparecio el boton COBRAR de la diaria"); return; }
                textoCobrar = TextoDelBoton(cobrar);
                monedasAntesCobrar = Progreso.Monedas;
                horaCobro = Time.realtimeSinceStartup;
                cobrar.onClick.Invoke();
                cobroHecho = true;
                monedasDespuesCobrar = Progreso.Monedas;
                // Sin video la ventana no ofrece nada: el boton del video sigue apagado.
                var video = BotonDeLaDiaria(cargaCobrar.diaria, "BotonVideo");
                videoSinProveedor = video != null && video.gameObject.activeSelf;
                disponibleTrasCobrar = RecompensaDiaria.Disponible;
                Pasar(Paso.CobrarEsperarTienda, ahora);
                return;
            }

            case Paso.CobrarEsperarTienda:
                if (!TiendaAbierta(cargaCobrar)) return;
                Pasar(Paso.AtrasCargar, ahora);
                return;

            // --- 3: el atras de Android con la diaria abierta ------------------------
            case Paso.AtrasCargar:
                Preparar(cargaAtras, true, false);
                TiendaMejoras.AbrirEnMenu();
                Pasar(Paso.AtrasEsperarDiaria, ahora);
                return;

            case Paso.AtrasEsperarDiaria:
                if (!DiariaALaVista(cargaAtras)) return;
                Pasar(Paso.AtrasMirar, ahora);
                return;

            case Paso.AtrasMirar:
                if (pasado < SegundosMirando) return;
                tiendaCerradaAntesDelAtras = TiendaCerradaConLaDiaria(cargaAtras);
                if (cargaAtras.atras == null) { Terminar("no aparecio BotonAtrasMenu en el menu"); return; }
                monedasAntesAtras = Progreso.Monedas;
                // Lo que corre BotonAtrasMenu.Update con Escape, que es el atras de Android.
                cargaAtras.atras.Atras();
                atrasHecho = true;
                Pasar(Paso.AtrasEsperarTienda, ahora);
                return;

            case Paso.AtrasEsperarTienda:
                if (!TiendaAbierta(cargaAtras)) return;
                monedasDespuesAtras = Progreso.Monedas;
                disponibleTrasAtras = RecompensaDiaria.Disponible;
                Pasar(Paso.VideoCargar, ahora);
                return;

            // --- 4: con video --------------------------------------------------------
            case Paso.VideoCargar:
                Preparar(cargaVideo, true, true);
                TiendaMejoras.AbrirEnMenu();
                Pasar(Paso.VideoEsperarDiaria, ahora);
                return;

            case Paso.VideoEsperarDiaria:
                if (!DiariaALaVista(cargaVideo)) return;
                Pasar(Paso.VideoMirar, ahora);
                return;

            case Paso.VideoMirar:
            {
                if (pasado < SegundosMirando) return;
                tiendaCerradaAntesDelVideo = TiendaCerradaConLaDiaria(cargaVideo);
                montoVideo = MontoDeHoy(out rachaVideo);
                var cobrar = BotonDeLaDiaria(cargaVideo.diaria, "BotonCobrar");
                if (cobrar == null) { Terminar("no aparecio el boton COBRAR de la diaria (caso con video)"); return; }
                monedasAntesVideo = Progreso.Monedas;
                cobrar.onClick.Invoke();
                monedasTrasCobrarVideo = Progreso.Monedas;
                Pasar(Paso.VideoOferta, ahora);
                return;
            }

            case Paso.VideoOferta:
            {
                if (!capturo && pasado >= SegundosParaCapturar)
                {
                    capturo = true;
                    Capturar("diaria_oferta_video");
                }
                if (pasado < SegundosConLaOferta) return;
                var botonVideo = BotonDeLaDiaria(cargaVideo.diaria, "BotonVideo");
                var botonVolver = BotonDeLaDiaria(cargaVideo.diaria, "BotonAtras");
                ofertaVisible = botonVideo != null && botonVideo.gameObject.activeSelf;
                volverVisible = botonVolver != null && botonVolver.gameObject.activeSelf;
                textoVideo = TextoDelBoton(botonVideo);
                // Con la oferta en pie la ventana no se va sola: espera que se elija.
                diariaEsperaLaEleccion = VentanaRecompensaDiaria.Abierta && VentanaRecompensaDiaria.Ocupada && !TiendaAbierta(cargaVideo);
                if (!ofertaVisible)
                {
                    // Sin oferta no hay video que mirar: el caso queda en falla y se sigue con el 5.
                    Pasar(Paso.SinDiariaCargar, ahora);
                    return;
                }
                videosAntes = proveedor.veces;
                botonVideo.onClick.Invoke();
                Pasar(Paso.VideoEsperarTienda, ahora);
                return;
            }

            case Paso.VideoEsperarTienda:
                if (!TiendaAbierta(cargaVideo)) return;
                videoTerminado = true;
                videosPedidos = proveedor.veces - videosAntes;
                monedasTrasVideo = Progreso.Monedas;
                paraDuplicarTrasVideo = RecompensaDiaria.ParaDuplicar;
                // Otra vez el premio del video: no tiene que dar nada.
                segundoDuplicado = RecompensaDiaria.CobrarDuplicado();
                monedasTrasSegundo = Progreso.Monedas;
                audioPausadoTrasVideo = AudioListener.pause;
                mostrandoTrasVideo = ServicioAnuncios.MostrandoAnuncio;
                Pasar(Paso.SinDiariaCargar, ahora);
                return;

            // --- 5: sin diaria, ya cobrada hoy ---------------------------------------
            case Paso.SinDiariaCargar:
                Preparar(cargaSinDiaria, false, false);
                TiendaMejoras.AbrirEnMenu();
                Pasar(Paso.SinDiariaEsperarTienda, ahora);
                return;

            case Paso.SinDiariaEsperarTienda:
                if (!TiendaAbierta(cargaSinDiaria)) return;
                Pasar(Paso.SinDiariaMirar, ahora);
                return;

            case Paso.SinDiariaMirar:
                // Un rato con la tienda abierta: la diaria no tiene que aparecer despues.
                if (pasado < SegundosSinDiaria) return;
                sinDiariaMirado = true;
                llegoAlFinal = true;
                Pasar(Paso.Listo, ahora);
                Terminar(null);
                return;
        }
    }

    // Deja el progreso como lo pide el caso, pone el proveedor del banco y marca la carga
    // que se va a vigilar. Lo que carga el menu viene despues, en el cuadro siguiente.
    static void Preparar(Carga carga, bool conDiaria, bool conVideo)
    {
        // La diaria espera a la primera partida terminada (PrimeraVez.NoTerminoPartidas).
        if (Progreso.PartidasTerminadas < 1) Progreso.TerminarPartida(120f);
        // Los videos sin apagar (el setter guarda si cambia).
        Progreso.OfrecerVideos = true;
        if (conDiaria)
        {
            // El ultimo cobro fue ayer: hoy hay diaria, con la racha de ayer mas uno.
            System.DateTime ayer = Progreso.AhoraConfiable().AddDays(-1);
            Progreso.RegistrarRecompensaDiaria(ayer.Year * 10000 + ayer.Month * 100 + ayer.Day, RachaDeAyer);
        }
        else
        {
            // Cobrada hoy: RachaParaHoy da 0.
            Progreso.RegistrarRecompensaDiaria(Progreso.DiaDeHoy(), RachaDeAyer + 1);
        }
        Progreso.Guardar();
        carga.disponibleAlPreparar = RecompensaDiaria.Disponible;

        proveedor.hayVideo = conVideo;
        ServicioAnuncios.UsarParaPruebas(proveedor, config);
        actual = carga;
    }

    // El menu del caso termino de cargar: de aca en mas se vigila. Los componentes se buscan
    // en SUS raices, no con Find: al cargar, la escena de antes puede seguir ahi un momento.
    static void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        if (!SessionState.GetBool(Clave, false)) return;
        var c = actual;
        if (c == null || c.cargado || escena.buildIndex != TiendaMejoras.EscenaMenu) return;
        c.cargado = true;
        c.cuadroCarga = Time.frameCount;
        c.horaCarga = Time.realtimeSinceStartup;
        foreach (var raiz in escena.GetRootGameObjects())
        {
            if (c.tienda == null) c.tienda = raiz.GetComponentInChildren<TiendaMejoras>(true);
            if (c.diaria == null) c.diaria = raiz.GetComponentInChildren<VentanaRecompensaDiaria>(true);
            if (c.atras == null) c.atras = raiz.GetComponentInChildren<BotonAtrasMenu>(true);
        }
    }

    // Una vez por cuadro del juego, antes de dibujar los canvas.
    static void Muestrear()
    {
        if (!EditorApplication.isPlaying || !SessionState.GetBool(Clave, false)) return;
        muestras++;
        try
        {
            Vigilar();
        }
        catch (System.Exception e)
        {
            // Que no se corte el dibujo de la UI por el banco; queda en el informe.
            cuantasExcepciones++;
            excepciones.AppendLine("  Muestrear: " + e.Message);
        }
    }

    // Anota el primer cuadro en que se vio cada cosa y los cuadros en que la tienda estuvo
    // abierta con la diaria abierta u ocupada. Se puede llamar de mas en el mismo cuadro.
    static void Vigilar()
    {
        var c = actual;
        if (c == null || !c.cargado) return;
        int cuadro = Time.frameCount;
        float hora = Time.realtimeSinceStartup;
        bool abierta = VentanaRecompensaDiaria.Abierta;
        bool ocupada = VentanaRecompensaDiaria.Ocupada;
        bool tienda = c.tienda != null && c.tienda.Abierta;

        if (abierta || ocupada) c.vioLaDiaria = true;
        if (abierta && c.cuadroDiaria < 0) { c.cuadroDiaria = cuadro; c.horaDiaria = hora; }
        if (c.cuadroDiaria >= 0 && !abierta && !ocupada && c.cuadroDiariaSeFue < 0) { c.cuadroDiariaSeFue = cuadro; c.horaDiariaSeFue = hora; }
        if (tienda && c.cuadroTienda < 0) { c.cuadroTienda = cuadro; c.horaTienda = hora; }
        if (tienda && (abierta || ocupada) && c.ultimoEncimado != cuadro)
        {
            c.cuadrosEncimados++;
            c.ultimoEncimado = cuadro;
        }
    }

    static bool DiariaALaVista(Carga c)
    {
        return c.cargado && VentanaRecompensaDiaria.Abierta && PanelDeLaDiaria(c.diaria) != null;
    }

    static bool TiendaAbierta(Carga c)
    {
        return c.cargado && c.tienda != null && c.tienda.Abierta;
    }

    static bool TiendaCerradaConLaDiaria(Carga c)
    {
        return VentanaRecompensaDiaria.Abierta && c.tienda != null && !c.tienda.Abierta;
    }

    // Lo que va a pagar la ventana: la racha de hoy y la mejor oleada congelada del dia.
    static double MontoDeHoy(out int racha)
    {
        racha = RecompensaDiaria.RachaDeHoy;
        return racha > 0 ? RecompensaDiaria.Monto(racha, RecompensaDiaria.OleadaDeHoy) : 0;
    }

    // La ventana la arma VentanaRecompensaDiaria en codigo, como hijo "RecompensaDiaria" del
    // canvas donde esta el componente. La que esta prendida: si la volvio a armar (cambio de
    // idioma o de tema), la vieja se destruye al final del cuadro.
    static Transform PanelDeLaDiaria(VentanaRecompensaDiaria diaria)
    {
        if (diaria == null) return null;
        foreach (Transform hijo in diaria.transform)
            if (hijo.name == "RecompensaDiaria" && hijo.gameObject.activeSelf) return hijo;
        return null;
    }

    // BotonCobrar, BotonVideo o BotonAtras (el VOLVER de la oferta), prendido o no.
    static Button BotonDeLaDiaria(VentanaRecompensaDiaria diaria, string nombre)
    {
        var panel = PanelDeLaDiaria(diaria);
        if (panel == null) return null;
        foreach (var boton in panel.GetComponentsInChildren<Button>(true))
            if (boton.name == nombre) return boton;
        return null;
    }

    static string TextoDelBoton(Button boton)
    {
        if (boton == null) return null;
        foreach (var texto in boton.GetComponentsInChildren<TMP_Text>(true))
            if (texto.name == "Texto") return texto.text;
        return null;
    }

    static void Capturar(string nombre)
    {
        string carpeta = Path.GetFullPath("../Builds");
        Directory.CreateDirectory(carpeta);
        ScreenCapture.CaptureScreenshot(Path.Combine(carpeta, nombre + ".png"));
    }

    static bool Cerca(double a, double b)
    {
        return System.Math.Abs(a - b) <= 1e-6 * System.Math.Max(1.0, System.Math.Abs(b));
    }

    // La tienda se abrio despues de que la diaria termino de irse, enseguida, y nunca las dos a la vez.
    static bool SeAbrioDespues(Carga c)
    {
        if (c.cuadroDiariaSeFue < 0 || c.cuadroTienda < c.cuadroDiariaSeFue || c.cuadrosEncimados > 0) return false;
        return c.cuadroTienda - c.cuadroDiariaSeFue <= CuadrosDeMargen || c.horaTienda - c.horaDiariaSeFue <= SegundosDeMargen;
    }

    static string Relativo(Carga c, int cuadro)
    {
        return c.cuadroCarga < 0 || cuadro < 0 ? "nunca" : "+" + (cuadro - c.cuadroCarga);
    }

    static string Cuadros(Carga c)
    {
        if (!c.cargado) return "el menu no llego a cargar";
        return "cuadros desde la carga del menu: diaria " + Relativo(c, c.cuadroDiaria) + ", la diaria termino de irse "
               + Relativo(c, c.cuadroDiariaSeFue) + ", tienda " + Relativo(c, c.cuadroTienda)
               + "; cuadros con la tienda y la diaria a la vez: " + c.cuadrosEncimados;
    }

    static string Segundos(float despues, float antes)
    {
        return despues < 0f || antes < 0f ? "?" : (despues - antes).ToString("0.00");
    }

    static string N(double valor)
    {
        return valor.ToString("0.##");
    }

    static string ArmarInforme(string error)
    {
        var inf = new StringBuilder();
        inf.AppendLine("Prueba de la recompensa diaria antes que la tienda (MEJORAS de la derrota, Menu)");
        inf.AppendLine();
        if (error != null) inf.AppendLine("ERROR: " + error);
        inf.AppendLine("Proveedor de anuncios del asset: " + proveedorDelAsset
                       + " (el banco pone el suyo: sin video en los casos 1, 2, 3 y 5, con video en el 4)");
        inf.AppendLine("Progreso en solo lectura: " + soloLectura + "; partidas terminadas: " + partidas
                       + "; cuadros vigilados con Canvas.willRenderCanvases: " + muestras);
        inf.AppendLine();

        inf.AppendLine("1 y 2. MEJORAS de la derrota con la diaria disponible (por el boton de la derrota: " + porElBotonDeLaDerrota + ")");
        inf.AppendLine("  disponible al preparar: " + cargaCobrar.disponibleAlPreparar + "; racha de hoy: " + rachaCobrar + "; monto: " + N(montoCobrar));
        inf.AppendLine("  " + Cuadros(cargaCobrar));
        inf.AppendLine("  con la diaria abierta " + SegundosMirando + " s, la tienda cerrada: " + tiendaCerradaConLaDiaria);
        inf.AppendLine("  el boton decia \"" + textoCobrar + "\"; monedas " + N(monedasAntesCobrar) + " -> " + N(monedasDespuesCobrar)
                       + " (+" + N(monedasDespuesCobrar - monedasAntesCobrar) + ")");
        inf.AppendLine("  sin video: el boton del video a la vista despues de cobrar: " + videoSinProveedor
                       + "; la diaria sigue disponible: " + disponibleTrasCobrar);
        inf.AppendLine("  la tienda se abrio " + Segundos(cargaCobrar.horaTienda, horaCobro) + " s despues de COBRAR y "
                       + Segundos(cargaCobrar.horaTienda, cargaCobrar.horaDiariaSeFue) + " s despues de irse la diaria");
        inf.AppendLine();

        inf.AppendLine("3. El atras de Android con la diaria abierta");
        inf.AppendLine("  disponible al preparar: " + cargaAtras.disponibleAlPreparar + "; con la diaria abierta, la tienda cerrada: " + tiendaCerradaAntesDelAtras);
        inf.AppendLine("  " + Cuadros(cargaAtras));
        inf.AppendLine("  monedas " + N(monedasAntesAtras) + " -> " + N(monedasDespuesAtras) + "; la diaria sigue disponible: " + disponibleTrasAtras);
        inf.AppendLine("  la tienda se abrio " + Segundos(cargaAtras.horaTienda, cargaAtras.horaDiariaSeFue) + " s despues de irse la diaria");
        inf.AppendLine();

        inf.AppendLine("4. Con video");
        inf.AppendLine("  disponible al preparar: " + cargaVideo.disponibleAlPreparar + "; racha de hoy: " + rachaVideo + "; monto: " + N(montoVideo)
                       + "; con la diaria abierta, la tienda cerrada: " + tiendaCerradaAntesDelVideo);
        inf.AppendLine("  " + Cuadros(cargaVideo));
        inf.AppendLine("  a los " + SegundosConLaOferta + " s de cobrar: video a la vista " + ofertaVisible + " (\"" + textoVideo + "\"), VOLVER a la vista "
                       + volverVisible + ", la ventana espera la eleccion: " + diariaEsperaLaEleccion);
        inf.AppendLine("  monedas " + N(monedasAntesVideo) + " -> " + N(monedasTrasCobrarVideo) + " al cobrar -> " + N(monedasTrasVideo)
                       + " con el video (+" + N(monedasTrasVideo - monedasAntesVideo) + " en total)");
        inf.AppendLine("  videos pedidos: " + videosPedidos + "; lo que quedaba para duplicar: " + N(paraDuplicarTrasVideo)
                       + "; el premio otra vez dio " + N(segundoDuplicado) + " (monedas " + N(monedasTrasSegundo) + ")");
        inf.AppendLine("  despues del video: audio en pausa " + audioPausadoTrasVideo + ", video en pantalla " + mostrandoTrasVideo);
        inf.AppendLine();

        inf.AppendLine("5. Sin diaria (cobrada hoy)");
        inf.AppendLine("  disponible al preparar: " + cargaSinDiaria.disponibleAlPreparar + "; la diaria aparecio u ocupo: " + cargaSinDiaria.vioLaDiaria);
        inf.AppendLine("  la tienda se abrio en el cuadro " + Relativo(cargaSinDiaria, cargaSinDiaria.cuadroTienda) + " de la carga, "
                       + Segundos(cargaSinDiaria.horaTienda, cargaSinDiaria.horaCarga) + " s despues");
        inf.AppendLine();
        inf.AppendLine("Errores y excepciones durante la prueba: " + cuantasExcepciones);
        if (cuantasExcepciones > 0) inf.Append(excepciones);
        inf.AppendLine();

        double deltaCobrar = monedasDespuesCobrar - monedasAntesCobrar;
        double deltaCobroVideo = monedasTrasCobrarVideo - monedasAntesVideo;
        double deltaVideo = monedasTrasVideo - monedasAntesVideo;
        var sinDiaria = cargaSinDiaria;

        bool[] ok =
        {
            error == null && cuantasExcepciones == 0 && llegoAlFinal,
            cargaCobrar.disponibleAlPreparar && cargaCobrar.cuadroDiaria >= 0,
            cargaCobrar.cuadroDiaria >= 0 && tiendaCerradaConLaDiaria && cargaCobrar.cuadrosEncimados == 0,
            cobroHecho && montoCobrar > 0 && Cerca(deltaCobrar, montoCobrar)
                && textoCobrar == Textos.Formato("diaria_cobrar", FormatoNumeros.Compacto(deltaCobrar)),
            cobroHecho && !videoSinProveedor && !disponibleTrasCobrar,
            cobroHecho && SeAbrioDespues(cargaCobrar),
            atrasHecho && tiendaCerradaAntesDelAtras && cargaAtras.cuadroDiariaSeFue >= 0
                && Cerca(monedasDespuesAtras, monedasAntesAtras) && disponibleTrasAtras,
            atrasHecho && SeAbrioDespues(cargaAtras),
            tiendaCerradaAntesDelVideo && montoVideo > 0 && Cerca(deltaCobroVideo, montoVideo) && ofertaVisible && volverVisible
                && diariaEsperaLaEleccion && textoVideo == Textos.Formato("diaria_video", FormatoNumeros.Compacto(deltaCobroVideo)),
            videoTerminado && videosPedidos == 1 && Cerca(deltaVideo, 2 * montoVideo) && paraDuplicarTrasVideo == 0
                && segundoDuplicado == 0 && Cerca(monedasTrasSegundo, monedasTrasVideo),
            videoTerminado && !audioPausadoTrasVideo && !mostrandoTrasVideo,
            videoTerminado && SeAbrioDespues(cargaVideo),
            sinDiariaMirado && !sinDiaria.disponibleAlPreparar && !sinDiaria.vioLaDiaria && sinDiaria.cuadroDiaria < 0,
            sinDiaria.cuadroTienda >= 0 && (sinDiaria.cuadroTienda - sinDiaria.cuadroCarga <= CuadrosDeMargen
                                            || sinDiaria.horaTienda - sinDiaria.horaCarga <= SegundosDeMargen),
        };
        string[] que =
        {
            "el banco llego hasta el final, sin errores ni excepciones",
            "MEJORAS de la derrota con la diaria disponible: al cargar el menu sale la diaria",
            "la tienda no se abre mientras la diaria esta (ni un cuadro las dos a la vez)",
            "COBRAR suma las monedas que dice el boton de la ventana",
            "sin video la ventana no ofrece el video, y la diaria queda cobrada por hoy",
            "cuando la diaria termina de irse, la tienda se abre sola, enseguida",
            "el atras de Android cierra la diaria sin cobrar (sigue disponible, las monedas no cambian)",
            "y la tienda se abre despues de irse la diaria, enseguida",
            "con video, despues de cobrar la ventana ofrece VIDEO: +N MAS con el monto cobrado y VOLVER, y espera",
            "el video cobra el duplicado una sola vez (un video, el doble en total, y otra vez no da nada)",
            "despues del video vuelve el audio y no queda un video en pantalla",
            "y la tienda se abre despues de irse la diaria, enseguida",
            "sin diaria disponible, la ventana no sale ni hace esperar a la tienda",
            "y la tienda abre enseguida al cargar el menu, como siempre",
        };
        bool todo = true;
        for (int i = 0; i < ok.Length; i++)
        {
            inf.AppendLine((ok[i] ? "OK  " : "FALLA  ") + que[i]);
            todo &= ok[i];
        }
        inf.AppendLine();
        inf.AppendLine("RESULTADO: " + (todo ? "TODO OK" : "HAY FALLAS"));
        return inf.ToString();
    }

    static void Terminar(string error)
    {
        // Lo del banco se va antes de escribir: los eventos, el proveedor y la config.
        SceneManager.sceneLoaded -= AlCargarEscena;
        Canvas.willRenderCanvases -= Muestrear;

        // Si armar el informe revienta, igual se sale: si no, el Tick volveria a intentarlo
        // en cada cuadro y el editor quedaria en play para siempre.
        string informe;
        try
        {
            informe = ArmarInforme(error);
        }
        catch (System.Exception e)
        {
            informe = "Prueba de la recompensa diaria antes que la tienda (MEJORAS de la derrota, Menu)" + System.Environment.NewLine
                      + System.Environment.NewLine + "ERROR: " + (error ?? "") + " / no se pudo armar el informe: " + e
                      + System.Environment.NewLine + System.Environment.NewLine
                      + "FALLA  el banco llego hasta el final, sin errores ni excepciones" + System.Environment.NewLine
                      + System.Environment.NewLine + "RESULTADO: HAY FALLAS" + System.Environment.NewLine;
        }

        ServicioAnuncios.UsarParaPruebas(null, null);
        if (config != null)
        {
            Object.DestroyImmediate(config);
            config = null;
        }
        // Si se corto a mitad de un video, el audio quedo en pausa: es global y cruza escenas.
        AudioListener.pause = false;

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(Ruta)));
            File.WriteAllText(Ruta, informe);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("PruebaDiaria: no se pudo escribir " + Ruta + ": " + e.Message);
        }

        SessionState.SetBool(Clave, false);
        SessionState.SetBool(Clave + ".olvidarProgreso", true);
        EditorApplication.ExitPlaymode();
        PlayerSettings.runInBackground = false;
        AssetDatabase.SaveAssets();
        Debug.Log(informe);
    }

    // Al volver al modo edicion. Si el banco seguia prendido es que se salio de play sin que
    // terminara (a mano, o se cayo): se apaga, o se meteria en la proxima partida del editor.
    // Y en los dos casos se olvida el progreso en memoria: al salir de play no se recarga el
    // dominio, asi que Progreso se queda con lo que dejo el banco (las monedas cobradas, la
    // racha). RespaldoDelBanco devuelve el archivo; sin esto, un Guardar en modo edicion (una
    // herramienta, las pruebas de logica) escribiria encima lo del banco.
    static void AlCambiarDeModo(PlayModeStateChange cambio)
    {
        if (cambio != PlayModeStateChange.EnteredEditMode) return;
        bool cortado = SessionState.GetBool(Clave, false);
        bool olvidar = SessionState.GetBool(Clave + ".olvidarProgreso", false);
        if (!cortado && !olvidar) return;

        if (cortado)
        {
            SessionState.SetBool(Clave, false);
            SceneManager.sceneLoaded -= AlCargarEscena;
            Canvas.willRenderCanvases -= Muestrear;
            PlayerSettings.runInBackground = false;
            if (config != null)
            {
                Object.DestroyImmediate(config);
                config = null;
            }
            Debug.LogWarning("PruebaDiaria: se salio de play antes de terminar; no se escribio el informe.");
        }
        SessionState.SetBool(Clave + ".olvidarProgreso", false);

        // Progreso.datos es privado: con null, lo proximo que pregunte vuelve a leer el archivo.
        var campo = typeof(Progreso).GetField("datos", BindingFlags.NonPublic | BindingFlags.Static);
        if (campo != null) campo.SetValue(null, null);
        else Debug.LogWarning("PruebaDiaria: no se encontro Progreso.datos; el progreso en memoria queda como lo dejo el banco.");
    }
}
