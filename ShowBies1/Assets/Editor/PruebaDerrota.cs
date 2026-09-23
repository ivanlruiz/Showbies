using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

// Banco de la derrota encima de la partida (DerrotaEnLaPartida). Entra en play en
// WaveMode, espera a que el jugador tenga horda encima, lo mata y mira, cuadro a
// cuadro, lo que se rompe facil en silencio: que no cambie la escena, que la partida
// SIGA ANDANDO detras (timeScale en 1, los zombis moviendose: "que no se freeze el
// juego", pidio Ivan) pero que nada la cambie (el jugador muerto quieto, ni puntos ni
// monedas ni oleadas despues de morir), que la pantalla salga enseguida y el mundo se
// ponga gris DETRAS de ella ("la pantalla y el gris"), que el fondo no tape el mundo,
// que no queden dos EventSystem ni dos AudioListener (se pelean y avisan en cada
// cuadro) ni la luz de la derrota prendida, y que al tocar OTRA VEZ todo vuelva.
//
// Saca dos capturas en Builds/: a mitad del gris y con el gris completo. Con "Grabar
// la derrota" ademas graba la muerte entera en Builds/derrota_video/.
// Escribe Builds/prueba_derrota.txt.
//
// OJO: entrar y salir de play hace que Unity vuelva a serializar ProjectSettings (ver
// la trampa en CLAUDE.md): despues de correrlo, revertir lo que no se toco a proposito.
[InitializeOnLoad]
public static class PruebaDerrota
{
    const string Clave = "ShowBies.PruebaDerrota";
    const string Ruta = "../Builds/prueba_derrota.txt";
    const string CarpetaVideo = "../Builds/derrota_video";

    // Muere cuando ya tiene horda encima, que es como se ve de verdad: a los tres
    // segundos de partida el campo esta vacio y la captura no muestra nada.
    const float SegundosAntesDeMorir = 3f;
    const float EsperaMaxima = 40f;
    const int ZombisCerca = 6;
    const float DistanciaCerca = 7f;

    // Cuando mirar, contado desde el golpe.
    const double CapturaAMitad = 2.5;
    const double CapturaGris = 6.0;
    const double TocarOtraVez = 6.8;
    const double CadaCuanto = 0.1;   // la grabacion: un cuadro cada decimo de segundo real

    enum Paso { Esperando, Muerto, Saliendo }

    static Paso paso;
    static double murioEn, aparecioEn, grisCompletoEn, salioEn, proximoCuadro;
    static int cuadro, escenaDelJuego;
    static bool cambioDeEscena, sePauso, capturoMitad, capturoGris;
    static Vector3 jugadorAlMorir, zombisA, zombisB;
    static int puntosAlMorir;
    static bool puntosQuietos = true, jugadorQuieto = true, midioZombisA, midioZombisB;
    static int zarpazosAlMorir, festejandoA4, vivosA4 = -1, rugidos = -1;
    static bool zarpazosQuietos = true, midioFestejo;
    static bool derrotaAMitadDelGris;
    static double monedasAlMorir;
    static int oleadaAlMorir, partidasAntes;
    static bool progresoQuieto = true;
    static int oidos, sistemas, camarasDeLaDerrota = -1, canvasDelJuegoPrendidos = -1, lucesDeLaDerrota = -1;
    static float opacidadDelFondo = -1f;
    static bool congeladoEncima;
    static float timeScaleAlSalir = -1f;
    static int escenasAlSalir = -1;
    static bool activaAlSalir = true, sobreLaPartidaAlSalir = true;
    static bool midioElCuerpo, corriendoMuerto, disparandoMuerto;
    static int vidaTrasElGolpe;
    static string vidaEnElHud;

    // Las excepciones que salten mientras corre: un banco que falla sin decir por que
    // obliga a ir a buscar la consola, que con dos editores abiertos ni siquiera es la
    // de este proyecto.
    static readonly StringBuilder excepciones = new StringBuilder();
    static int cuantasExcepciones;

    static PruebaDerrota()
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

    [MenuItem("ShowBies/Pruebas/Derrota encima de la partida (play)")]
    // Publico para poder correrlo por codigo (ver la trampa del registro de menus).
    public static void Arrancar()
    {
        Arrancar(false);
    }

    [MenuItem("ShowBies/Pruebas/Grabar la derrota (play)")]
    public static void Grabar()
    {
        string carpeta = Path.GetFullPath(CarpetaVideo);
        if (Directory.Exists(carpeta)) Directory.Delete(carpeta, true);
        Directory.CreateDirectory(carpeta);
        Arrancar(true);
    }

    static void Arrancar(bool grabando)
    {
        SessionState.SetBool(Clave + ".grabar", grabando);
        PlayerSettings.runInBackground = true;
        EditorSceneManager.OpenScene("Assets/Escenas/WaveMode.unity");
        SessionState.SetBool(Clave, true);
        SessionState.SetFloat(Clave + ".desde", -1f);
        EditorApplication.EnterPlaymode();
    }

    static void Tick()
    {
        if (!SessionState.GetBool(Clave, false)) return;
        if (!EditorApplication.isPlaying) return;

        double ahora = EditorApplication.timeSinceStartup;
        float desde = SessionState.GetFloat(Clave + ".desde", -1f);
        if (desde < 0f)
        {
            SessionState.SetFloat(Clave + ".desde", (float)ahora);
            paso = Paso.Esperando;
            cuadro = 0;
            proximoCuadro = 0;
            aparecioEn = grisCompletoEn = -1;
            cambioDeEscena = sePauso = capturoMitad = capturoGris = derrotaAMitadDelGris = false;
            puntosQuietos = jugadorQuieto = zarpazosQuietos = true;
            midioZombisA = midioZombisB = midioFestejo = false;
            vivosA4 = rugidos = -1;
            progresoQuieto = true;
            camarasDeLaDerrota = canvasDelJuegoPrendidos = lucesDeLaDerrota = -1;
            opacidadDelFondo = timeScaleAlSalir = -1f;
            escenasAlSalir = -1;
            activaAlSalir = sobreLaPartidaAlSalir = true;
            midioElCuerpo = corriendoMuerto = disparandoMuerto = false;
            vidaTrasElGolpe = int.MinValue;
            vidaEnElHud = null;
            partidasAntes = Progreso.PartidasTerminadas;
            excepciones.Length = 0;
            cuantasExcepciones = 0;
            return;
        }

        switch (paso)
        {
            case Paso.Esperando:
                if (ahora - desde < SegundosAntesDeMorir || PlayerHealth.instance == null) return;
                if (ahora - desde < EsperaMaxima && ZombisAlrededor() < ZombisCerca)
                {
                    PlayerHealth.instance.health = Mathf.Max(PlayerHealth.instance.health, 60);
                    if (ZombisAlrededor() >= ZombisCerca - 2) GrabarCuadro(ahora);   // un poco de antes
                    return;
                }
                escenaDelJuego = SceneManager.GetActiveScene().buildIndex;
                // Muere corriendo y disparando, que es como se muere de verdad: el cuerpo
                // tiene que quedar quieto tambien en la animacion. Y de un golpe que saca
                // mucho mas de lo que le queda: la vida no puede quedar en negativo.
                var control = PlayerHealth.instance.GetComponent<PlayerController>();
                if (control != null)
                {
                    control.Move(new Vector2(1f, 0f));
                    control.FijarDisparo(true);
                }
                PlayerHealth.instance.TakeDamage(999999f);
                murioEn = ahora;
                jugadorAlMorir = PlayerHealth.instance.transform.position;
                puntosAlMorir = Puntaje.instance != null ? Puntaje.instance.contadorKill : 0;
                zarpazosAlMorir = EnemyController.ZarpazosEmpezados;
                monedasAlMorir = Progreso.Monedas;
                oleadaAlMorir = Progreso.MejorOleada;
                paso = Paso.Muerto;
                return;

            case Paso.Muerto:
            {
                double pasado = ahora - murioEn;
                GrabarCuadro(ahora);
                Vigilar();
                if (aparecioEn < 0 && DerrotaCargada()) aparecioEn = ahora;
                if (!midioElCuerpo && pasado >= 0.5) MedirElCuerpo();
                // La horda sigue: la suma de las posiciones de los zombis cambia.
                if (!midioZombisA && pasado >= 1.5) { midioZombisA = true; zombisA = SumaDeZombis(); }
                if (!midioZombisB && pasado >= 4.0) { midioZombisB = true; zombisB = SumaDeZombis(); }
                // El festejo: a los 5,5 s la mayoria ya llego a su costado y festeja (el
                // tanque, a 3 m/s, tarda unos 4 s en abrirse 12 m).
                if (!midioFestejo && pasado >= 5.5)
                {
                    midioFestejo = true;
                    festejandoA4 = vivosA4 = 0;
                    foreach (var z in Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None))
                    {
                        if (!z.Vivo) continue;
                        vivosA4++;
                        if (z.Festejando) festejandoA4++;
                    }
                }
                if (grisCompletoEn < 0 && DerrotaEnLaPartida.Avance >= 0.999f) grisCompletoEn = ahora;

                // A mitad del gris: la pantalla ya tiene que estar encima.
                if (!capturoMitad && pasado >= CapturaAMitad)
                {
                    capturoMitad = true;
                    float gris = DerrotaEnLaPartida.Avance;
                    derrotaAMitadDelGris = DerrotaCargada() && gris > 0.2f && gris < 0.8f;
                    ScreenCapture.CaptureScreenshot(Path.GetFullPath("../Builds/derrota_gris.png"));
                }
                if (!capturoGris && pasado >= CapturaGris)
                {
                    capturoGris = true;
                    ScreenCapture.CaptureScreenshot(Path.GetFullPath("../Builds/derrota_encima.png"));
                    MedirLaDerrota();
                }
                if (pasado < TocarOtraVez) return;

                var menu = Object.FindFirstObjectByType<MenuPerdiste>();
                if (menu == null) { Terminar("no aparecio MenuPerdiste"); return; }
                menu.Retry();
                salioEn = ahora;
                paso = Paso.Saliendo;
                return;
            }

            case Paso.Saliendo:
                if (ahora - salioEn < 1.5) return;
                timeScaleAlSalir = Time.timeScale;
                escenasAlSalir = SceneManager.sceneCount;
                activaAlSalir = DerrotaEnLaPartida.Activa;
                sobreLaPartidaAlSalir = MenuPerdiste.SobreLaPartida;
                Terminar(null);
                return;
        }
    }

    static void GrabarCuadro(double ahora)
    {
        if (!SessionState.GetBool(Clave + ".grabar", false) || ahora < proximoCuadro) return;
        proximoCuadro = ahora + CadaCuanto;
        ScreenCapture.CaptureScreenshot(Path.Combine(Path.GetFullPath(CarpetaVideo), "f" + (cuadro++).ToString("0000") + ".png"));
    }

    static Vector3 SumaDeZombis()
    {
        Vector3 suma = Vector3.zero;
        foreach (var z in Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None))
            if (z.Vivo) suma += z.transform.position;
        return suma;
    }

    static int ZombisAlrededor()
    {
        Vector3 jugador = PlayerHealth.instance.transform.position;
        int n = 0;
        foreach (var z in Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None))
            if (z.Vivo && (z.transform.position - jugador).sqrMagnitude <= DistanciaCerca * DistanciaCerca) n++;
        return n;
    }

    // Mientras esta muerto: la escena no cambia, el tiempo sigue en 1 y nada de la
    // partida cambia por eso: ni el progreso, ni los puntos, ni el jugador de lugar.
    static void Vigilar()
    {
        if (SceneManager.GetActiveScene().buildIndex != escenaDelJuego) cambioDeEscena = true;
        if (Time.timeScale != 1f) sePauso = true;
        if (Progreso.Monedas != monedasAlMorir || Progreso.MejorOleada != oleadaAlMorir) progresoQuieto = false;
        if (Puntaje.instance != null && Puntaje.instance.contadorKill != puntosAlMorir) puntosQuietos = false;
        // A un muerto no le tiran zarpazos: festejan.
        if (EnemyController.ZarpazosEmpezados != zarpazosAlMorir) zarpazosQuietos = false;
        if (PlayerHealth.instance != null &&
            (PlayerHealth.instance.transform.position - jugadorAlMorir).sqrMagnitude > 0.05f * 0.05f)
            jugadorQuieto = false;
    }

    // Medio segundo despues del golpe: la vida y lo que dice el HUD (que durante el ¡HAS
    // MUERTO! se ve), y si el cuerpo sigue corriendo o disparando en el lugar.
    static void MedirElCuerpo()
    {
        midioElCuerpo = true;
        var vida = PlayerHealth.instance;
        if (vida == null) return;
        vidaTrasElGolpe = vida.health;
        vidaEnElHud = vida.healthTMP != null ? vida.healthTMP.text : null;
        var control = vida.GetComponent<PlayerController>();
        var animador = control != null && control.trans != null ? control.trans.GetComponent<Animator>() : null;
        if (animador == null) return;
        corriendoMuerto = animador.GetBool("run");
        disparandoMuerto = animador.GetBool("shoot");
    }

    static bool DerrotaCargada()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene s = SceneManager.GetSceneAt(i);
            if (s.buildIndex == DerrotaEnLaPartida.EscenaDerrota && s.isLoaded) return true;
        }
        return false;
    }

    // Con la derrota encima: uno solo de cada cosa, la camara de la derrota apagada, el
    // HUD del juego apagado y el fondo sin tapar el mundo.
    static void MedirLaDerrota()
    {
        oidos = 0;
        foreach (var o in Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
            if (o.isActiveAndEnabled) oidos++;
        sistemas = 0;
        foreach (var e in Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None))
            if (e.isActiveAndEnabled) sistemas++;

        camarasDeLaDerrota = 0;
        canvasDelJuegoPrendidos = 0;
        foreach (var c in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            if (c.gameObject.scene.buildIndex == DerrotaEnLaPartida.EscenaDerrota && c.enabled) camarasDeLaDerrota++;
        foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            if (c.isRootCanvas && c.enabled && c.gameObject.scene.buildIndex == escenaDelJuego) canvasDelJuegoPrendidos++;
        // Una luz de la derrota encendida es un segundo sol sobre la partida: el mundo
        // quedaba casi blanco en vez de gris.
        lucesDeLaDerrota = 0;
        foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (l.gameObject.scene.buildIndex == DerrotaEnLaPartida.EscenaDerrota && l.isActiveAndEnabled) lucesDeLaDerrota++;
        foreach (var p in Object.FindObjectsByType<PintarConTema>(FindObjectsSortMode.None))
        {
            if (p.rol != RolDeTema.Fondo || p.gameObject.scene.buildIndex != DerrotaEnLaPartida.EscenaDerrota) continue;
            var g = p.GetComponent<UnityEngine.UI.Graphic>();
            if (g != null) opacidadDelFondo = g.color.a;
        }
        // Nadie juega (el input esta cortado y la pausa no se mete), pero el tiempo corre.
        congeladoEncima = MenuPausa.JuegoCongelado && Time.timeScale == 1f;
        rugidos = EnemyController.RugidosDelFestejo;
    }

    static void Terminar(string error)
    {
        double enAparecer = aparecioEn < 0 ? -1 : aparecioEn - murioEn;
        double enQuedarGris = grisCompletoEn < 0 ? -1 : grisCompletoEn - murioEn;
        int partidasDeMas = Progreso.PartidasTerminadas - partidasAntes;

        var inf = new StringBuilder();
        inf.AppendLine("Prueba de la derrota encima de la partida (WaveMode)");
        inf.AppendLine();
        if (error != null) inf.AppendLine("ERROR: " + error);
        inf.AppendLine("Del golpe a la pantalla: " + enAparecer.ToString("0.00") + " s");
        inf.AppendLine("Del golpe al gris completo: " + enQuedarGris.ToString("0.00") + " s");
        inf.AppendLine("AudioListener prendidos: " + oidos + ", EventSystem prendidos: " + sistemas);
        inf.AppendLine("Camaras de la derrota prendidas: " + camarasDeLaDerrota + ", luces de la derrota prendidas: " + lucesDeLaDerrota
                       + ", canvas raiz del juego prendidos: " + canvasDelJuegoPrendidos);
        inf.AppendLine("Opacidad del fondo de la derrota: " + opacidadDelFondo.ToString("0.00"));
        inf.AppendLine("Partidas contadas al morir: " + partidasDeMas);
        inf.AppendLine("Vida despues del golpe: " + vidaTrasElGolpe + " (el HUD dice \"" + vidaEnElHud + "\"); el cuerpo corre: "
                       + corriendoMuerto + ", dispara: " + disparandoMuerto);
        inf.AppendLine("Festejando a los 5,5 s: " + festejandoA4 + " de " + vivosA4 + " zombis vivos; rugidos hasta los 6 s: " + rugidos);
        inf.AppendLine("Al salir con OTRA VEZ: timeScale " + timeScaleAlSalir + ", escenas cargadas " + escenasAlSalir);
        inf.AppendLine("Errores y excepciones durante la prueba: " + cuantasExcepciones);
        if (cuantasExcepciones > 0) inf.Append(excepciones);
        inf.AppendLine();

        float t = DerrotaEnLaPartida.SegundosDeTransicion;
        bool[] ok =
        {
            error == null && cuantasExcepciones == 0,
            !cambioDeEscena,
            !sePauso,
            midioZombisB && (zombisB - zombisA).sqrMagnitude > 1f,
            jugadorQuieto,
            midioElCuerpo && vidaTrasElGolpe == 0 && vidaEnElHud == "0",
            midioElCuerpo && !corriendoMuerto && !disparandoMuerto,
            puntosQuietos,
            vivosA4 > 0 && festejandoA4 * 2 >= vivosA4,
            rugidos >= 2,
            zarpazosQuietos,
            enAparecer >= 0 && enAparecer <= 0.6,
            enQuedarGris >= t - 0.3 && enQuedarGris <= t + 1.0,
            derrotaAMitadDelGris,
            oidos == 1 && sistemas == 1,
            camarasDeLaDerrota == 0,
            lucesDeLaDerrota == 0,
            canvasDelJuegoPrendidos == 0,
            opacidadDelFondo >= 0f && opacidadDelFondo <= 0.2f,
            progresoQuieto,
            partidasDeMas == 1,
            congeladoEncima,
            timeScaleAlSalir == 1f && escenasAlSalir == 1,
            !activaAlSalir && !sobreLaPartidaAlSalir,
        };
        string[] que =
        {
            "el banco llego hasta el final, sin errores ni excepciones",
            "morir no cambia la escena: la derrota va encima",
            "la partida sigue andando detras (timeScale 1 todo el tiempo)",
            "los zombis se siguen moviendo detras de la derrota",
            "el jugador muerto no se mueve (ni camina ni lo arrastra la horda)",
            "un golpe que se pasa deja la vida en 0 y el HUD no muestra negativos",
            "el cuerpo no queda corriendo ni disparando en el lugar",
            "los puntos no cambian despues de morir (lo que quedo en el aire no mata)",
            "la horda festeja: a los 5,5 s festejan la mitad o mas",
            "rugen en las primeras oleadas del festejo",
            "nadie le tira zarpazos al cuerpo",
            "la pantalla sale enseguida al morir",
            "el mundo queda gris del todo en " + t + " s",
            "la pantalla y el gris van a la vez: a mitad del gris la pantalla ya esta encima",
            "un solo AudioListener y un solo EventSystem",
            "la camara de la derrota esta apagada (no tapa el mundo con el cielo)",
            "la luz de la derrota esta apagada (no sobreexpone el mundo de la partida)",
            "el HUD del juego esta apagado (nada queda a color encima del gris)",
            "el fondo de la derrota no tapa el mundo gris",
            "el progreso no se toca despues de morir (la oleada no se cierra sola)",
            "la partida se cuenta una sola vez",
            "con la derrota encima el input esta cortado y la pausa no se mete",
            "OTRA VEZ vuelve el timeScale a 1 y deja una sola escena",
            "al salir no queda nada marcado como congelado",
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
        Debug.Log(inf.ToString());

        SessionState.SetBool(Clave, false);
        EditorApplication.ExitPlaymode();
        PlayerSettings.runInBackground = false;
        AssetDatabase.SaveAssets();
    }
}
