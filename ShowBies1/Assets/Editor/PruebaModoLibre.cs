using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Banco del desbloqueo del modo libre (ver "Menu y modos" en CLAUDE.md). El libre se
// gana llegando a la oleada 12 de las oleadas, o sea completando la 11 (ModoLibre), y
// hasta el 25/9 llegaba en silencio. Mira las tres cosas que lo anuncian:
//
//  1. En WaveMode, con la mejor oleada en 10 y la partida retomada en la 11 (el libre
//     bloqueado), mata a todos los zombis de la 11 por el mismo camino que una bala
//     (EnemyController.DanoZombi, como PruebaMuerteAnimada). Al completarla: que el libre
//     quede desbloqueado y guardado, que AvisoDeMisiones saque "MODO LIBRE DESBLOQUEADO"
//     (el texto de aviso_libre) una sola vez, que no se repita en la oleada 12, y que en la
//     partida siguiente, que ya arranca desbloqueada, no vuelva a salir. La cola de avisos
//     y el cartel se leen por reflexion (son privados).
//  2. En el menu, en el panel de modos: desbloqueado y sin record del libre (HighScore_1
//     borrado) BotonModoLibre dice NUEVO abajo; con record no; bloqueado dice
//     REACH WAVE 12 y tocarlo no carga nada.
//  3. Lo mismo en espaniol y en ingles, cambiando el idioma con Idioma.Cambiar (lo mismo
//     que hace SelectorIdioma), en vivo y sin recargar el menu.
//
// Saca capturas del aviso y del boton en cada estado en Builds/libre_*.png.
// Escribe Builds/prueba_libre.txt.
//
// Pisa el progreso real del editor (progreso.json): lo reinicia para armar la mejor oleada
// en 10. RespaldoDelBanco lo copia antes y lo devuelve al volver a modo edicion (al salir de
// play, Progreso guarda lo que tenga en memoria). Los PlayerPrefs que toca (Idioma y
// HighScore_1) los devuelve el mismo banco, y tambien RespaldoDelBanco.
//
// OJO: entrar y salir de play hace que Unity vuelva a serializar ProjectSettings (ver la
// trampa en CLAUDE.md): despues de correrlo, revertir lo que no se toco a proposito.
[InitializeOnLoad]
public static class PruebaModoLibre
{
    const string Clave = "ShowBies.PruebaModoLibre";
    const string Ruta = "../Builds/prueba_libre.txt";
    const string Capturas = "../Builds/libre_";

    const int RecordDePrueba = 1234;
    // Solo para que el banco no tarde: los zombis salen de a uno cada 0,1 s en vez de 0,35.
    const float IntervaloRapido = 0.1f;
    const float DanoQueMata = 99999f;

    // Topes por paso y total, en segundos reales.
    const double TopeArranque = 30, TopeOleada = 150, TopeSiguiente = 150, TopeOtraPartida = 30,
                 TopeMenu = 30, TopeCaso = 10, TopeJugar = 10, TopeTotal = 600;
    // Cuanto se espera, desde que se completa la oleada, a que el aviso del libre salga en
    // pantalla: con otros avisos antes en la cola (niveles, logros de combo, misiones, de
    // 2,6 s cada uno) puede tardar.
    const double EsperaDelAviso = 40;
    // Cuanto se mira la partida siguiente buscando un aviso repetido.
    const double MirarLaOtraPartida = 4;

    // La oleada que hay que completar: llegar a la 12 es haber completado la 11.
    static int OleadaQueDesbloquea { get { return ModoLibre.OleadaParaDesbloquear - 1; } }
    static string ClaveRecordLibre { get { return PlayerHealth.ClaveRecord(TiendaMejoras.EscenaModoLibre); } }

    enum Paso { Arrancando, Oleada11, Oleada12, OtraPartida, Menu, Caso, Jugar }

    // Los estados del boton que se miran, en este orden. El primero es tal cual queda el
    // menu al volver de la partida en que se desbloqueo (sin record del libre). Los cambios
    // de idioma van en vivo (el boton mira Idioma.Revision); los del record y del progreso
    // se ven al reabrir el panel de modos (VOLVER y PLAY), que es cuando el boton se pinta.
    // El ultimo tiene que ser bloqueado: ahi se toca el boton, y no tiene que cargar nada.
    struct Caso
    {
        public Lengua lengua;
        public bool libre;
        public bool record;
        public string nombre;
        public string que;
    }

    static readonly Caso[] Casos =
    {
        new Caso { lengua = Lengua.Espanol, libre = true, record = false, nombre = "1_nuevo_es",
                   que = "en espaniol, recien desbloqueado y sin record del libre: el nombre con NUEVO abajo (tal cual quedo al volver de la partida)" },
        new Caso { lengua = Lengua.Espanol, libre = true, record = true, nombre = "2_record_es",
                   que = "en espaniol, con record del libre: solo el nombre, sin NUEVO" },
        new Caso { lengua = Lengua.Ingles, libre = true, record = true, nombre = "3_record_en",
                   que = "en ingles, con record: solo el nombre (el idioma cambiado en vivo, sin reabrir el panel)" },
        new Caso { lengua = Lengua.Ingles, libre = true, record = false, nombre = "4_nuevo_en",
                   que = "en ingles, sin record: el nombre con NEW abajo" },
        new Caso { lengua = Lengua.Ingles, libre = false, record = false, nombre = "5_bloqueado_en",
                   que = "en ingles, bloqueado (mejor oleada 10): el nombre con REACH WAVE 12 abajo, sin NEW" },
        new Caso { lengua = Lengua.Espanol, libre = false, record = false, nombre = "6_bloqueado_es",
                   que = "en espaniol, bloqueado: el nombre con LLEGA A LA OLEADA 12 abajo (el idioma cambiado en vivo)" },
    };

    static Paso paso;
    static double inicio, pasoDesde;
    static bool jugadorMurio;

    // AvisoDeMisiones por reflexion: el cartel que se ve, la cola de pendientes y si ya
    // aviso el libre.
    static FieldInfo campoCartel, campoPendientes, campoLibreVisto;

    // La partida en que se desbloquea.
    static AvisoDeMisiones aviso, avisoViejo;
    static string idiomaDeLaPartida;
    static int oleadaAlEmpezar, mejorAlEmpezar;
    static bool libreAlEmpezar, libreVistoAlEmpezar;
    static int muertosEnLaOleada, muertosDespues;
    static double completoEn, completoSiguienteEn, segundosDeLaOleada;
    static bool libreAlCompletar;
    static int mejorAlCompletar, oleadaTrasCompletar, colaAlCompletar;
    static bool leyoDisco;
    static int mejorEnDisco;
    static TMP_Text ultimoCartel;
    static readonly List<string> titulosMostrados = new List<string>();
    static int libreMostrados, colaMaxima;
    static double libreVistoEn, libreMostradoEn, capturarAvisoEn;
    static bool capturoAviso;
    static string textoDelAvisoLibre;
    static Color colorDelAvisoLibre, colorEsperadoDelAviso;
    static int libreEnColaAlFinal;
    static bool libreEnPantallaAlFinal;

    // La partida siguiente.
    static double otraDesde;
    static bool libreVistoEnLaOtra, libreEnLaOtra;
    static int oleadaDeLaOtra;

    // El menu.
    static GameObject panelModos;
    static BotonModoLibre boton;
    static bool panelAbierto, nuncaJugoEnElMenu, primerCasoForzado;
    static int caso, sub;
    static double subDesde;
    static string[] textos;
    static bool[] textoOk, fondoGris, medido;
    static int escenaTrasJugar, escenasTrasJugar;
    static string textoTrasJugar;

    static readonly StringBuilder excepciones = new StringBuilder();
    static int cuantasExcepciones;

    static PruebaModoLibre()
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

    [MenuItem("ShowBies/Pruebas/Desbloqueo del modo libre (play)")]
    // Publico para poder correrlo por codigo (ver la trampa del registro de menus).
    public static void Arrancar()
    {
        if (!RespaldoDelBanco.PuedeArrancar("PruebaModoLibre")) return;
        RespaldoDelBanco.Guardar("PruebaModoLibre");

        // Lo que se toca de los PlayerPrefs se anota para devolverlo al terminar.
        string claveRecord = ClaveRecordLibre;
        SessionState.SetBool(Clave + ".recordHabia", PlayerPrefs.HasKey(claveRecord));
        SessionState.SetInt(Clave + ".record", PlayerPrefs.GetInt(claveRecord, 0));
        SessionState.SetBool(Clave + ".idiomaHabia", PlayerPrefs.HasKey(Idioma.ClavePreferencia));
        SessionState.SetString(Clave + ".idioma", PlayerPrefs.GetString(Idioma.ClavePreferencia, ""));

        PrepararProgreso();

        // La partida en espaniol: el aviso se compara igual con la tabla, pero asi la
        // captura dice lo que pide la prueba ("MODO LIBRE DESBLOQUEADO").
        PlayerPrefs.SetString(Idioma.ClavePreferencia, Idioma.Codigo(Lengua.Espanol));
        PlayerPrefs.Save();

        PlayerSettings.runInBackground = true;
        EditorSceneManager.OpenScene("Assets/Escenas/WaveMode.unity");
        SessionState.SetBool(Clave, true);
        SessionState.SetBool(Clave + ".empezo", false);
        EditorApplication.EnterPlaymode();
    }

    // La mejor oleada completada en 10 y la partida de oleadas a medias en la 11: el libre
    // bloqueado y a una oleada de desbloquearse. Se guarda en disco porque al entrar en
    // play se recarga el dominio y Progreso vuelve a leer el archivo.
    static void PrepararProgreso()
    {
        // Por si una prueba de logica quedo a medias con la carpeta temporal puesta.
        Progreso.UsarCarpetaDePruebas(null);
        Progreso.ReiniciarTodo();
        Progreso.RegistrarOleadaCompletada(OleadaQueDesbloquea - 1);
        Progreso.GuardarOleadaEnCurso(OleadaQueDesbloquea, 0);
        // La recompensa de hoy ya cobrada: si no, su ventana sale sola al abrir el menu y
        // tapa el panel de modos en las capturas (con la mejor oleada en 10, ya no espera a
        // la primera partida terminada).
        Progreso.RegistrarRecompensaDiaria(Progreso.DiaDeHoy(), 1);
        Progreso.Guardar();
    }

    static void Tick()
    {
        if (!SessionState.GetBool(Clave, false)) return;
        if (!EditorApplication.isPlaying) return;
        if (!RespaldoDelBanco.SigueArmado("PruebaModoLibre", Clave)) return;

        double ahora = EditorApplication.timeSinceStartup;
        if (!SessionState.GetBool(Clave + ".empezo", false))
        {
            SessionState.SetBool(Clave + ".empezo", true);
            Reiniciar(ahora);
            return;
        }

        // Una excepcion del banco mismo no tiene que dejarlo colgado repitiendose en
        // cada cuadro: termina con el error en el informe.
        try
        {
            Avanzar(ahora);
        }
        catch (System.Exception e)
        {
            Terminar("excepcion en el banco: " + e.GetType().Name + ": " + e.Message + System.Environment.NewLine + e.StackTrace);
        }
    }

    static void Reiniciar(double ahora)
    {
        inicio = ahora;
        paso = Paso.Arrancando;
        pasoDesde = ahora;
        jugadorMurio = false;
        campoCartel = campoPendientes = campoLibreVisto = null;

        aviso = avisoViejo = null;
        idiomaDeLaPartida = "";
        oleadaAlEmpezar = mejorAlEmpezar = -1;
        libreAlEmpezar = libreVistoAlEmpezar = true;
        muertosEnLaOleada = muertosDespues = 0;
        completoEn = completoSiguienteEn = segundosDeLaOleada = -1;
        libreAlCompletar = false;
        mejorAlCompletar = oleadaTrasCompletar = colaAlCompletar = -1;
        leyoDisco = false;
        mejorEnDisco = -1;
        ultimoCartel = null;
        titulosMostrados.Clear();
        libreMostrados = colaMaxima = 0;
        libreVistoEn = libreMostradoEn = capturarAvisoEn = -1;
        capturoAviso = false;
        textoDelAvisoLibre = null;
        colorDelAvisoLibre = colorEsperadoDelAviso = Color.clear;
        libreEnColaAlFinal = -1;
        libreEnPantallaAlFinal = false;

        otraDesde = -1;
        libreVistoEnLaOtra = libreEnLaOtra = false;
        oleadaDeLaOtra = -1;

        panelModos = null;
        boton = null;
        panelAbierto = nuncaJugoEnElMenu = primerCasoForzado = false;
        caso = sub = 0;
        subDesde = 0;
        textos = new string[Casos.Length];
        textoOk = new bool[Casos.Length];
        fondoGris = new bool[Casos.Length];
        medido = new bool[Casos.Length];
        escenaTrasJugar = escenasTrasJugar = -1;
        textoTrasJugar = null;

        excepciones.Length = 0;
        cuantasExcepciones = 0;
    }

    static void Pasar(Paso siguiente, double ahora)
    {
        paso = siguiente;
        pasoDesde = ahora;
    }

    static double TopeDe(Paso p)
    {
        switch (p)
        {
            case Paso.Arrancando: return TopeArranque;
            case Paso.Oleada11: return TopeOleada;
            case Paso.Oleada12: return TopeSiguiente;
            case Paso.OtraPartida: return TopeOtraPartida;
            case Paso.Menu: return TopeMenu;
            case Paso.Caso: return TopeCaso;
            default: return TopeJugar;
        }
    }

    static void Avanzar(double ahora)
    {
        if (ahora - inicio > TopeTotal)
        {
            Terminar("se paso el tope total de " + TopeTotal + " s (iba en el paso " + paso + ")");
            return;
        }
        if (ahora - pasoDesde > TopeDe(paso))
        {
            Terminar("se trabo en el paso " + paso + (paso == Paso.Caso ? " " + Casos[caso].nombre : "")
                     + " (tope de " + TopeDe(paso) + " s)");
            return;
        }
        if (jugadorMurio)
        {
            Terminar("el jugador murio en la partida (no deberia: los zombis se matan apenas salen)");
            return;
        }

        switch (paso)
        {
            case Paso.Arrancando:
            {
                if (ahora - pasoDesde < 1.0) return;
                var oleadas = Object.FindFirstObjectByType<WaveManager>();
                var a = Object.FindFirstObjectByType<AvisoDeMisiones>();
                if (PlayerHealth.instance == null || oleadas == null || a == null) return;
                if (!ResolverCampos())
                {
                    Terminar("no se encontraron por reflexion los campos cartel, pendientes o libreVisto de AvisoDeMisiones");
                    return;
                }
                aviso = a;
                idiomaDeLaPartida = Idioma.Codigo(Idioma.Actual);
                oleadaAlEmpezar = oleadas.OleadaActual;
                mejorAlEmpezar = Progreso.MejorOleada;
                libreAlEmpezar = ModoLibre.Desbloqueado;
                libreVistoAlEmpezar = (bool)campoLibreVisto.GetValue(a);
                if (oleadaAlEmpezar != OleadaQueDesbloquea)
                {
                    Terminar("la partida no se retomo en la oleada " + OleadaQueDesbloquea + ": arranco en la " + oleadaAlEmpezar);
                    return;
                }
                // Todavia esta en el descanso del cartel: el cambio vale desde el primer zombi.
                oleadas.intervaloEntreApariciones = IntervaloRapido;
                Pasar(Paso.Oleada11, ahora);
                return;
            }

            case Paso.Oleada11:
            {
                MantenerVivo();
                muertosEnLaOleada += MatarTodos();
                VigilarAvisos(ahora);
                if (Progreso.MejorOleada < OleadaQueDesbloquea) return;

                // WaveManager registra la oleada y en el mismo cuadro arranca la siguiente
                // (y la guarda en disco), sin ningun yield en el medio.
                var oleadas = Object.FindFirstObjectByType<WaveManager>();
                completoEn = ahora;
                segundosDeLaOleada = ahora - pasoDesde;
                libreAlCompletar = ModoLibre.Desbloqueado;
                mejorAlCompletar = Progreso.MejorOleada;
                oleadaTrasCompletar = oleadas != null ? oleadas.OleadaActual : -1;
                colaAlCompletar = LargoDeLaCola(aviso);
                Pasar(Paso.Oleada12, ahora);
                return;
            }

            case Paso.Oleada12:
            {
                MantenerVivo();
                muertosDespues += MatarTodos();
                VigilarAvisos(ahora);
                // Lo que quedo en disco: WaveManager guarda al empezar la oleada siguiente.
                if (!leyoDisco && ahora - completoEn >= 0.5)
                {
                    leyoDisco = true;
                    mejorEnDisco = LeerMejorOleadaDelDisco();
                }
                if (completoSiguienteEn < 0 && Progreso.MejorOleada >= OleadaQueDesbloquea + 1) completoSiguienteEn = ahora;

                // Sigue hasta completar tambien la oleada siguiente y, si el aviso todavia
                // esperaba en la cola, hasta que salga y se vaya.
                bool siguienteTerminada = completoSiguienteEn > 0 && ahora - completoSiguienteEn >= 1.5;
                bool avisoListo = libreMostradoEn > 0 && !LibreEnPantalla(aviso) && ContarLibreEnCola(aviso) == 0;
                if (!siguienteTerminada) return;
                if (!avisoListo && ahora - completoEn < EsperaDelAviso) return;

                libreEnColaAlFinal = ContarLibreEnCola(aviso);
                libreEnPantallaAlFinal = LibreEnPantalla(aviso);

                // La partida siguiente: WaveMode de nuevo, que retoma la oleada en curso.
                avisoViejo = aviso;
                aviso = null;
                Time.timeScale = 1f;   // por si justo habia una pausa de impacto
                SceneManager.LoadScene(TiendaMejoras.EscenaOleadas);
                Pasar(Paso.OtraPartida, ahora);
                return;
            }

            case Paso.OtraPartida:
            {
                MantenerVivo();
                MatarTodos();
                if (ahora - pasoDesde < 1.0) return;
                if (SceneManager.GetActiveScene().buildIndex != TiendaMejoras.EscenaOleadas) return;
                var a = Object.FindFirstObjectByType<AvisoDeMisiones>();
                if (a == null || object.ReferenceEquals(a, avisoViejo)) return;
                if (otraDesde < 0)
                {
                    otraDesde = ahora;
                    libreVistoEnLaOtra = (bool)campoLibreVisto.GetValue(a);
                    var oleadas = Object.FindFirstObjectByType<WaveManager>();
                    oleadaDeLaOtra = oleadas != null ? oleadas.OleadaActual : -1;
                }
                if (LibreEnPantalla(a) || ContarLibreEnCola(a) > 0) libreEnLaOtra = true;
                if (ahora - otraDesde < MirarLaOtraPartida) return;

                // Al menu por el boton MENU de la pausa, sin record del libre: es lo que
                // encuentra el que acaba de desbloquearlo.
                PlayerPrefs.DeleteKey(ClaveRecordLibre);
                PlayerPrefs.Save();
                var pausa = Object.FindFirstObjectByType<MenuPausa>();
                if (pausa != null) pausa.IrAlMenu();
                else
                {
                    Time.timeScale = 1f;
                    SceneManager.LoadScene(TiendaMejoras.EscenaMenu);
                }
                Pasar(Paso.Menu, ahora);
                return;
            }

            case Paso.Menu:
            {
                if (ahora - pasoDesde < 2.5) return;
                if (SceneManager.GetActiveScene().buildIndex != TiendaMejoras.EscenaMenu) return;

                // El panel principal es el MainMenu que tiene menuModos (el de modos lo
                // tiene vacio).
                MainMenu principal = null;
                foreach (var m in Object.FindObjectsByType<MainMenu>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    if (m.menuModos != null) principal = m;
                if (principal == null)
                {
                    Terminar("no se encontro el MainMenu del panel principal (el que tiene menuModos)");
                    return;
                }
                panelModos = principal.menuModos;
                // PLAY, el mismo metodo del boton. La primera vez manda derecho a la oleada 1
                // en vez de abrir el panel: aca no tendria que pasar (la mejor oleada es 12),
                // pero si pasa se abre el panel a mano y se anota.
                nuncaJugoEnElMenu = PrimeraVez.NuncaJugo;
                if (!nuncaJugoEnElMenu) principal.TocarJugar();
                else
                {
                    panelModos.SetActive(true);
                    principal.gameObject.SetActive(false);
                }
                boton = Object.FindFirstObjectByType<BotonModoLibre>(FindObjectsInactive.Include);
                if (boton == null)
                {
                    Terminar("no se encontro BotonModoLibre en el menu");
                    return;
                }
                panelAbierto = panelModos.activeInHierarchy && boton.isActiveAndEnabled;
                caso = 0;
                sub = 0;
                primerCasoForzado = AplicarCaso(0);
                Pasar(Paso.Caso, ahora);
                return;
            }

            case Paso.Caso:
            {
                if (sub == 0)
                {
                    // Un rato para que el texto se dibuje antes de la captura.
                    if (ahora - pasoDesde < 0.8) return;
                    MedirCaso(caso);
                    ScreenCapture.CaptureScreenshot(Path.GetFullPath(Capturas + "boton_" + Casos[caso].nombre + ".png"));
                    sub = 1;
                    subDesde = ahora;
                    return;
                }
                // Y otro para que la captura salga antes de cambiar el estado.
                if (ahora - subDesde < 0.6) return;
                caso++;
                if (caso < Casos.Length)
                {
                    sub = 0;
                    AplicarCaso(caso);
                    Pasar(Paso.Caso, ahora);
                    return;
                }
                // El ultimo caso es bloqueado: tocarlo tiene que temblar y no cargar nada.
                // Es lo que llama el onClick del boton.
                boton.Jugar();
                Pasar(Paso.Jugar, ahora);
                return;
            }

            case Paso.Jugar:
            {
                if (ahora - pasoDesde < 1.5) return;
                escenaTrasJugar = SceneManager.GetActiveScene().buildIndex;
                escenasTrasJugar = SceneManager.sceneCount;
                textoTrasJugar = boton != null && boton.texto != null ? boton.texto.text : null;
                Terminar(null);
                return;
            }
        }
    }

    // --- La partida -----------------------------------------------------------------

    static bool ResolverCampos()
    {
        const BindingFlags privado = BindingFlags.NonPublic | BindingFlags.Instance;
        var tipo = typeof(AvisoDeMisiones);
        campoCartel = tipo.GetField("cartel", privado);
        campoPendientes = tipo.GetField("pendientes", privado);
        campoLibreVisto = tipo.GetField("libreVisto", privado);
        return campoCartel != null && campoPendientes != null && campoLibreVisto != null
               && typeof(TMP_Text).IsAssignableFrom(campoCartel.FieldType)
               && typeof(IEnumerable).IsAssignableFrom(campoPendientes.FieldType)
               && campoLibreVisto.FieldType == typeof(bool);
    }

    // Que la horda no lo mate a mitad de la prueba (igual los zombis mueren al salir).
    static void MantenerVivo()
    {
        var vida = PlayerHealth.instance;
        if (vida == null) return;
        if (vida.EstaMuerto)
        {
            jugadorMurio = true;
            return;
        }
        vida.health = vida.maxHealth;
    }

    // Todos los zombis vivos, por el mismo camino que una bala. Devuelve cuantos.
    static int MatarTodos()
    {
        int n = 0;
        foreach (var zombi in Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None))
        {
            if (zombi == null || !zombi.Vivo) continue;
            zombi.DanoZombi(DanoQueMata);
            n++;
        }
        return n;
    }

    // Mira el cartel que se esta mostrando (uno nuevo por aviso) y la cola de pendientes.
    static void VigilarAvisos(double ahora)
    {
        if (aviso == null) aviso = Object.FindFirstObjectByType<AvisoDeMisiones>();
        if (aviso == null) return;

        colaMaxima = Mathf.Max(colaMaxima, LargoDeLaCola(aviso));
        var cartel = campoCartel.GetValue(aviso) as TMP_Text;
        if (cartel != null && !object.ReferenceEquals(cartel, ultimoCartel))
        {
            ultimoCartel = cartel;
            string texto = cartel.text ?? "";
            titulosMostrados.Add(PrimeraLinea(texto));
            if (EsAvisoLibre(texto))
            {
                libreMostrados++;
                if (libreMostradoEn < 0)
                {
                    libreMostradoEn = ahora;
                    textoDelAvisoLibre = texto;
                    colorDelAvisoLibre = cartel.color;
                    colorEsperadoDelAviso = aviso.colorEstrella;
                    // Pasada la entrada con rebote (0,35 s).
                    capturarAvisoEn = ahora + 0.7;
                }
            }
        }

        if (libreVistoEn < 0 && (ContarLibreEnCola(aviso) > 0 || LibreEnPantalla(aviso))) libreVistoEn = ahora;

        if (!capturoAviso && capturarAvisoEn > 0 && ahora >= capturarAvisoEn)
        {
            capturoAviso = true;
            ScreenCapture.CaptureScreenshot(Path.GetFullPath(Capturas + "aviso.png"));
        }
    }

    static bool EsAvisoLibre(string texto)
    {
        string titulo = Textos.De("aviso_libre");
        return !string.IsNullOrEmpty(texto) && !string.IsNullOrEmpty(titulo)
               && texto.StartsWith(titulo, System.StringComparison.Ordinal);
    }

    static bool LibreEnPantalla(AvisoDeMisiones a)
    {
        if (a == null) return false;
        var cartel = campoCartel.GetValue(a) as TMP_Text;
        return cartel != null && EsAvisoLibre(cartel.text);
    }

    // Los avisos del libre que esperan en la cola. Aviso es un struct privado con campos
    // publicos (titulo, detalle, color).
    static int ContarLibreEnCola(AvisoDeMisiones a)
    {
        if (a == null) return 0;
        var cola = campoPendientes.GetValue(a) as IEnumerable;
        if (cola == null) return 0;
        string titulo = Textos.De("aviso_libre");
        int n = 0;
        foreach (object item in cola)
        {
            if (item == null) continue;
            var campo = item.GetType().GetField("titulo");
            if (campo != null && campo.GetValue(item) as string == titulo) n++;
        }
        return n;
    }

    static int LargoDeLaCola(AvisoDeMisiones a)
    {
        if (a == null) return 0;
        var cola = campoPendientes.GetValue(a) as ICollection;
        return cola != null ? cola.Count : 0;
    }

    static string PrimeraLinea(string texto)
    {
        if (string.IsNullOrEmpty(texto)) return "";
        int corte = texto.IndexOf('\n');
        return corte < 0 ? texto : texto.Substring(0, corte);
    }

    static int LeerMejorOleadaDelDisco()
    {
        try
        {
            string json = File.ReadAllText(Progreso.RutaArchivo);
            var m = Regex.Match(json, "\"mejorOleada\"\\s*:\\s*(-?\\d+)");
            return m.Success ? int.Parse(m.Groups[1].Value) : -2;
        }
        catch (System.Exception)
        {
            return -3;
        }
    }

    // --- El menu --------------------------------------------------------------------

    // Arma el estado de un caso. Devuelve si tuvo que cambiar algo.
    static bool AplicarCaso(int i)
    {
        var c = Casos[i];
        bool cambio = false, reabrir = false;

        // El idioma, como lo cambia SelectorIdioma: en vivo, sin recargar.
        if (Idioma.Actual != c.lengua)
        {
            Idioma.Cambiar(c.lengua);
            cambio = true;
        }

        if (c.record != PlayerPrefs.HasKey(ClaveRecordLibre))
        {
            if (c.record) PlayerPrefs.SetInt(ClaveRecordLibre, RecordDePrueba);
            else PlayerPrefs.DeleteKey(ClaveRecordLibre);
            PlayerPrefs.Save();
            cambio = reabrir = true;
        }

        if (c.libre != ModoLibre.Desbloqueado)
        {
            // La mejor oleada solo sube: para bajarla hay que reiniciar el progreso.
            if (c.libre) Progreso.RegistrarOleadaCompletada(OleadaQueDesbloquea);
            else
            {
                Progreso.ReiniciarTodo();
                Progreso.RegistrarOleadaCompletada(OleadaQueDesbloquea - 1);
            }
            Progreso.Guardar();
            cambio = reabrir = true;
        }

        // VOLVER y PLAY: el boton se pinta al prenderse (OnEnable). El record no tiene
        // Revision que mirar, y en el juego cambia en otra escena.
        if (reabrir && panelModos != null)
        {
            panelModos.SetActive(false);
            panelModos.SetActive(true);
        }
        return cambio;
    }

    static bool Contiene(string texto, string parte)
    {
        return !string.IsNullOrEmpty(texto) && !string.IsNullOrEmpty(parte) && texto.Contains(parte);
    }

    static string Formatear(string plantilla, object valor)
    {
        return plantilla == null ? null : string.Format(plantilla, valor);
    }

    // Lo que dice el boton contra la tabla de textos, en el idioma del caso y sin nada del
    // otro idioma; el color del fondo; y que el estado armado sea el que se queria.
    static void MedirCaso(int i)
    {
        var c = Casos[i];
        Lengua otra = c.lengua == Lengua.Espanol ? Lengua.Ingles : Lengua.Espanol;
        int oleada = ModoLibre.OleadaParaDesbloquear;

        string texto = boton != null && boton.texto != null ? boton.texto.text : null;
        textos[i] = texto;

        string nombre = Textos.Crudo("modo_libre", c.lengua);
        string nuevo = Textos.Crudo("modo_libre_nuevo", c.lengua);
        string bloqueado = Formatear(Textos.Crudo("modo_libre_bloqueado", c.lengua), oleada);
        string nombreOtro = Textos.Crudo("modo_libre", otra);
        string nuevoOtro = Textos.Crudo("modo_libre_nuevo", otra);
        string bloqueadoOtro = Formatear(Textos.Crudo("modo_libre_bloqueado", otra), oleada);

        bool ok = texto != null && Contiene(texto, nombre)
                  && !Contiene(texto, nombreOtro) && !Contiene(texto, nuevoOtro) && !Contiene(texto, bloqueadoOtro);
        if (ok)
        {
            if (c.libre && !c.record) ok = Contiene(texto, nuevo) && !Contiene(texto, bloqueado);
            else if (c.libre) ok = texto.Trim() == nombre;
            else ok = Contiene(texto, bloqueado) && !Contiene(texto, nuevo);
        }
        ok = ok && Idioma.Actual == c.lengua
             && PlayerPrefs.GetString(Idioma.ClavePreferencia, "") == Idioma.Codigo(c.lengua)
             && ModoLibre.Desbloqueado == c.libre
             && PlayerPrefs.HasKey(ClaveRecordLibre) == c.record;
        textoOk[i] = ok;
        fondoGris[i] = boton != null && boton.fondo != null && boton.fondo.color == boton.colorBloqueado;
        medido[i] = true;
    }

    // --- El final -------------------------------------------------------------------

    // Los PlayerPrefs que toco el banco, como estaban. El idioma, tambien en la clase:
    // al salir de play no se recarga el dominio y el editor se quedaria con el de la prueba.
    static void DevolverPrefs()
    {
        string clave = ClaveRecordLibre;
        if (SessionState.GetBool(Clave + ".recordHabia", false)) PlayerPrefs.SetInt(clave, SessionState.GetInt(Clave + ".record", 0));
        else PlayerPrefs.DeleteKey(clave);

        string idioma = SessionState.GetString(Clave + ".idioma", "");
        Idioma.Cambiar(Idioma.DesdeCodigo(idioma));
        if (SessionState.GetBool(Clave + ".idiomaHabia", false)) PlayerPrefs.SetString(Idioma.ClavePreferencia, idioma);
        else PlayerPrefs.DeleteKey(Idioma.ClavePreferencia);
        PlayerPrefs.Save();
    }

    static string UnaLinea(string texto)
    {
        return texto == null ? "(nada)" : texto.Replace("\n", " / ");
    }

    static string Informe(string error)
    {
        var inf = new StringBuilder();
        inf.AppendLine("Prueba del desbloqueo del modo libre (WaveMode y el panel de modos del menu)");
        inf.AppendLine();
        if (error != null) inf.AppendLine("ERROR: " + error);
        inf.AppendLine("Idioma de la partida: " + idiomaDeLaPartida);
        inf.AppendLine("Al empezar: oleada " + oleadaAlEmpezar + ", mejor oleada " + mejorAlEmpezar + ", libre desbloqueado "
                       + libreAlEmpezar + ", aviso del libre ya visto " + libreVistoAlEmpezar);
        inf.AppendLine("Zombis matados: " + muertosEnLaOleada + " en la oleada " + OleadaQueDesbloquea + ", " + muertosDespues
                       + " despues; la oleada se completo en " + segundosDeLaOleada.ToString("0.0") + " s");
        inf.AppendLine("Al completarla: mejor oleada " + mejorAlCompletar + " (en disco " + mejorEnDisco + "), libre desbloqueado "
                       + libreAlCompletar + ", sigue la oleada " + oleadaTrasCompletar + ", avisos esperando en la cola " + colaAlCompletar);
        inf.AppendLine("Aviso del libre: en la cola o en pantalla a los "
                       + (libreVistoEn < 0 || completoEn < 0 ? "-1" : (libreVistoEn - completoEn).ToString("0.00"))
                       + " s de completar, en pantalla a los "
                       + (libreMostradoEn < 0 || completoEn < 0 ? "-1" : (libreMostradoEn - completoEn).ToString("0.00"))
                       + " s; mostrado " + libreMostrados + " veces; texto \"" + UnaLinea(textoDelAvisoLibre) + "\"");
        inf.AppendLine("Color del aviso: " + colorDelAvisoLibre + " (el dorado de las estrellas: " + colorEsperadoDelAviso + ")");
        inf.AppendLine("Avisos que salieron (" + titulosMostrados.Count + ", cola maxima " + colaMaxima + "): "
                       + string.Join(" | ", titulosMostrados.ToArray()));
        inf.AppendLine("Oleada siguiente completada: " + (completoSiguienteEn > 0) + "; al irse, avisos del libre en la cola "
                       + libreEnColaAlFinal + ", en pantalla " + libreEnPantallaAlFinal);
        inf.AppendLine("Partida siguiente: retomada en la oleada " + oleadaDeLaOtra + ", aviso ya visto al empezar "
                       + libreVistoEnLaOtra + ", aviso del libre en " + MirarLaOtraPartida + " s: " + libreEnLaOtra);
        inf.AppendLine("Menu: panel de modos abierto " + panelAbierto + " (primera vez: " + nuncaJugoEnElMenu
                       + "); el primer estado hubo que forzarlo: " + primerCasoForzado);
        for (int i = 0; i < Casos.Length; i++)
        {
            if (medido == null || !medido[i]) { inf.AppendLine("Boton " + Casos[i].nombre + ": sin medir"); continue; }
            inf.AppendLine("Boton " + Casos[i].nombre + ": \"" + UnaLinea(textos[i]) + "\", fondo gris " + fondoGris[i]);
        }
        inf.AppendLine("Tocarlo bloqueado: escena activa " + escenaTrasJugar + ", escenas cargadas " + escenasTrasJugar
                       + ", el boton dice \"" + UnaLinea(textoTrasJugar) + "\"");
        inf.AppendLine("Errores y excepciones durante la prueba: " + cuantasExcepciones);
        if (cuantasExcepciones > 0) inf.Append(excepciones);
        inf.AppendLine();

        var ok = new List<bool>();
        var que = new List<string>();

        ok.Add(error == null && cuantasExcepciones == 0);
        que.Add("el banco llego hasta el final, sin errores ni excepciones");

        ok.Add(oleadaAlEmpezar == OleadaQueDesbloquea && mejorAlEmpezar == OleadaQueDesbloquea - 1 && !libreAlEmpezar && !libreVistoAlEmpezar);
        que.Add("la partida se retoma en la oleada " + OleadaQueDesbloquea + " con la mejor en " + (OleadaQueDesbloquea - 1)
                + " y el modo libre bloqueado");

        ok.Add(completoEn > 0 && libreAlCompletar && mejorAlCompletar == OleadaQueDesbloquea && oleadaTrasCompletar == OleadaQueDesbloquea + 1);
        que.Add("al completar la oleada " + OleadaQueDesbloquea + " el modo libre queda desbloqueado y sigue la " + (OleadaQueDesbloquea + 1));

        ok.Add(mejorEnDisco >= OleadaQueDesbloquea);
        que.Add("el desbloqueo queda guardado en el progreso en disco");

        double enCola = libreVistoEn < 0 || completoEn < 0 ? -1 : libreVistoEn - completoEn;
        ok.Add(enCola >= 0 && enCola <= 1.0);
        que.Add("el aviso del libre entra a la cola enseguida al completarla, y no antes");

        // Contra la tabla en el idioma de la partida: al terminar el menu puede estar en otro.
        Lengua lenguaDeLaPartida = Idioma.DesdeCodigo(idiomaDeLaPartida);
        string esperado = Textos.Crudo("aviso_libre", lenguaDeLaPartida);
        bool textoDelAviso = Contiene(textoDelAvisoLibre, esperado) && PrimeraLinea(textoDelAvisoLibre) == esperado
                             && Contiene(textoDelAvisoLibre, Textos.Crudo("aviso_libre_detalle", lenguaDeLaPartida));
        ok.Add(libreMostradoEn > 0 && textoDelAviso && colorDelAvisoLibre == colorEsperadoDelAviso);
        que.Add("el aviso sale en pantalla con el titulo y el detalle de la tabla (aviso_libre), en dorado");

        ok.Add(libreMostrados == 1 && completoSiguienteEn > 0 && libreEnColaAlFinal == 0 && !libreEnPantallaAlFinal);
        que.Add("el aviso sale una sola vez: no se repite al completar la oleada siguiente");

        ok.Add(otraDesde > 0 && libreVistoEnLaOtra && !libreEnLaOtra);
        que.Add("en la partida siguiente, que ya arranca desbloqueada, el aviso no vuelve a salir");

        ok.Add(panelAbierto && !nuncaJugoEnElMenu);
        que.Add("PLAY abre el panel de modos con el boton del libre a la vista");

        for (int i = 0; i < Casos.Length; i++)
        {
            bool bien = medido != null && medido[i] && textoOk[i];
            if (i == 0) bien = bien && !primerCasoForzado;
            ok.Add(bien);
            que.Add("boton " + Casos[i].que);
        }

        bool colores = medido != null;
        for (int i = 0; colores && i < Casos.Length; i++) colores = medido[i] && fondoGris[i] == !Casos[i].libre;
        ok.Add(colores);
        que.Add("el fondo del boton es gris solo con el libre bloqueado");

        ok.Add(escenaTrasJugar == TiendaMejoras.EscenaMenu && escenasTrasJugar == 1);
        que.Add("bloqueado, tocarlo no carga ninguna escena (sigue el menu)");

        bool todo = true;
        for (int i = 0; i < ok.Count; i++)
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
        string informe;
        try
        {
            informe = Informe(error);
        }
        catch (System.Exception e)
        {
            informe = "Prueba del desbloqueo del modo libre" + System.Environment.NewLine + System.Environment.NewLine
                      + "ERROR: no se pudo armar el informe: " + e + System.Environment.NewLine
                      + (error != null ? "ERROR anterior: " + error + System.Environment.NewLine : "")
                      + System.Environment.NewLine + "FALLA  el banco llego hasta el final, sin errores ni excepciones"
                      + System.Environment.NewLine + System.Environment.NewLine + "RESULTADO: HAY FALLAS" + System.Environment.NewLine;
        }

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(Ruta)));
            File.WriteAllText(Ruta, informe);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("PruebaModoLibre: no se pudo escribir " + Ruta + ": " + e.Message);
        }

        try
        {
            DevolverPrefs();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("PruebaModoLibre: no se pudieron devolver los PlayerPrefs: " + e.Message);
        }

        SessionState.SetBool(Clave, false);
        Time.timeScale = 1f;
        EditorApplication.ExitPlaymode();
        PlayerSettings.runInBackground = false;
        AssetDatabase.SaveAssets();
        Debug.Log(informe);
    }
}
