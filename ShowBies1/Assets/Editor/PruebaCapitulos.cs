using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Banco de empezar las oleadas desde un capitulo (1.7.0, de Argon, 21/10), en play, con los caminos de verdad:
//
//  0. ¡CAPITULO 1 SUPERADO!: con la 9 como mejor oleada, una partida retomada en la 10 (el jefe y los demas, muertos
//     apenas salen): al terminarla, el cartel del capitulo con el punto de control y +250 monedas, el capitulo 2 queda
//     para volver y no cura. Otra vez en la 10, ya con la 10 completada: el cartel sin el punto de control, y cura la
//     mitad de la vida maxima.
//  1. Con la 20 completada, WaveMode cargada con el capitulo 2 pedido (lo que hacen el selector y OTRA VEZ) arranca en la
//     11: una oleada nueva (sin retomas, se puede retomar), con el cartel de la 11 y no el de una superada, el cartel del
//     capitulo del cementerio, sin puntos, con la vida llena y sin tocar las monedas. El capitulo queda para volver.
//  2. La derrota: OTRA VEZ dice "DESDE LA OLEADA 11", y tocarlo arranca de nuevo en la 11.
//  3. REINICIAR de la pausa tambien vuelve a la 11.
//  4. Salir dos veces en la misma oleada pierde la partida, pero la siguiente arranca en el principio de ese capitulo,
//     no en la 1.
//
// El progreso y los PlayerPrefs del editor vuelven como estaban al salir de play (RespaldoDelBanco). Escribe
// Builds/prueba_capitulos.txt.
//
// OJO: entrar y salir de play hace que Unity vuelva a serializar ProjectSettings (ver la trampa en CLAUDE.md): despues
// de correrlo, revertir lo que no se toco a proposito.
[InitializeOnLoad]
public static class PruebaCapitulos
{
    const string Clave = "ShowBies.PruebaCapitulos";
    const string Ruta = "../Builds/prueba_capitulos.txt";
    const string Escena = "Assets/Escenas/WaveMode.unity";
    const int Capitulo = 2;
    const int Inicial = 11;
    const double TopePorPaso = 25.0;
    const double TopeTotal = 220.0;
    const int VidaDejada = 30;

    enum Paso
    {
        DiezPrimera,        // retomada en la 10, con la 9 como mejor oleada
        TerminarDiezPrimera,
        FotoDelCapitulo,    // el cartel, cuando termino de entrar
        SalirDeLaDiez,
        DiezSegunda,        // otra vez en la 10, con la 10 completada
        TerminarDiezSegunda,
        EnLaUno,            // la partida del editor, en la 1: pide el capitulo 2 y recarga
        EnLaOnce,           // lo que hay al empezar en la 11, y muere
        Derrota,            // el texto de OTRA VEZ, y tocarlo
        OtraVez,            // de nuevo en la 11, y REINICIAR
        Reiniciada,         // de nuevo en la 11, y al menu
        PrimeraRetoma,      // retomada (gasta la retoma), y al menu otra vez
        SegundaSalida,      // la siguiente arranca en la 11, nueva
        Listo,
    }

    static Paso paso;
    static double inicio, pasoDesde;
    static int cuadroDelPaso;
    static bool terminado;
    static WaveManager oleadasVistas;
    // El de la escena que se esta dejando: LoadScene carga en el cuadro siguiente, y mientras tanto sigue ahi.
    static WaveManager anterior;
    static int cuadroDeLaEscena;
    static double vistasDesde;

    // Lo medido.
    static int oleadaDiez1 = -1, oleadaDiez2 = -1, volverDiez1 = -1, vidaDiez1 = -1, vidaDiez2 = -1, vidaMaxDiez = -1;
    static string cartelDiez1 = "", cartelDiez2 = "";
    static double monedasDiez1Antes = -1, monedasDiez1 = -1;
    static double monedasAntes = -1;
    static int oleada1 = -1, oleada11 = -1, retomas11 = -1, puntos11 = -1, vida11 = -1, vidaMax11 = -1, volver11 = -1;
    static bool hay11, cartelDeLaOleada, cartelDelCapitulo;
    static string cartel11 = "";
    static double monedas11 = -1;
    static string textoOtraVez = "";
    static bool dejoDeContar;
    static int oleadaOtraVez = -1, oleadaReiniciada = -1, oleadaRetomada = -1, retomasRetomada = -1;
    static int oleadaDespues = -1, retomasDespues = -1;

    static readonly StringBuilder excepciones = new StringBuilder();
    static int cuantasExcepciones;

    static PruebaCapitulos()
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

    [MenuItem("ShowBies/Pruebas/Empezar en un capitulo (play)")]
    // Publico para poder correrlo por codigo (ver la trampa del registro de menus).
    public static void Arrancar()
    {
        if (!RespaldoDelBanco.PuedeArrancar("PruebaCapitulos")) return;
        RespaldoDelBanco.Guardar("PruebaCapitulos");
        PlayerSettings.runInBackground = true;
        PlayerPrefs.SetInt("AutoApuntar", 0);
        PlayerPrefs.SetInt("UltimoModo", TiendaMejoras.EscenaOleadas);
        // Una partida retomada en la 10, con la 9 como mejor oleada (la 10 todavia no se completo nunca).
        Progreso.OlvidarOleadaEnCurso();
        Progreso.DepurarFijarMejorOleada(9);
        Progreso.GuardarOleadaEnCurso(10, 0);
        Progreso.CapituloParaVolver = 0;
        Progreso.Guardar();
        EditorSceneManager.OpenScene(Escena);
        SessionState.SetBool(Clave, true);
        SessionState.SetBool(Clave + ".empezo", false);
        EditorApplication.EnterPlaymode();
    }

    static void Tick()
    {
        if (!SessionState.GetBool(Clave, false)) return;
        if (!EditorApplication.isPlaying) return;
        if (!RespaldoDelBanco.SigueArmado("PruebaCapitulos", Clave)) return;

        double ahora = EditorApplication.timeSinceStartup;
        if (!SessionState.GetBool(Clave + ".empezo", false))
        {
            SessionState.SetBool(Clave + ".empezo", true);
            inicio = ahora;
            terminado = false;
            oleadasVistas = null;
            anterior = null;
            Pasar(Paso.DiezPrimera, ahora);
            return;
        }
        if (ahora - inicio > TopeTotal) { Terminar("se paso del tope total de " + TopeTotal + " s, en el paso " + paso); return; }
        if (ahora - pasoDesde > TopePorPaso) { Terminar("se trabo en el paso " + paso); return; }

        try
        {
            Avanzar(ahora);
        }
        catch (System.Exception e)
        {
            Terminar("excepcion del banco en el paso " + paso + ": " + e);
        }
    }

    // El WaveManager de la escena cargada, cuando ya lleva tres cuadros y medio segundo. Null mientras tanto.
    static WaveManager OleadasListas(double ahora)
    {
        if (SceneManager.GetActiveScene().buildIndex != TiendaMejoras.EscenaOleadas) return null;
        var wm = Object.FindAnyObjectByType<WaveManager>();
        if (wm == null || wm == anterior || PlayerHealth.instance == null) return null;
        if (wm != oleadasVistas)
        {
            oleadasVistas = wm;
            cuadroDeLaEscena = Time.frameCount;
            vistasDesde = ahora;
            return null;
        }
        if (Time.frameCount - cuadroDeLaEscena < 3 || ahora - vistasDesde < 0.5) return null;
        return wm;
    }

    static void Avanzar(double ahora)
    {
        var vida = PlayerHealth.instance;
        switch (paso)
        {
            case Paso.DiezPrimera:
            {
                var wm = OleadasListas(ahora);
                if (wm == null) return;
                oleadaDiez1 = wm.OleadaActual;
                monedasDiez1Antes = Progreso.Monedas;
                vida.health = VidaDejada;
                Pasar(Paso.TerminarDiezPrimera, ahora);
                return;
            }

            case Paso.TerminarDiezPrimera:
            {
                var wm = Object.FindAnyObjectByType<WaveManager>();
                if (wm == null || vida == null) return;
                if (wm.OleadaActual < 11)
                {
                    MatarATodos(vida);
                    return;
                }
                cartelDiez1 = wm.cartelOleada != null ? wm.cartelOleada.text : "";
                monedasDiez1 = Progreso.Monedas;
                volverDiez1 = Progreso.CapituloParaVolver;
                vidaDiez1 = vida.health;
                Pasar(Paso.FotoDelCapitulo, ahora);
                return;
            }

            case Paso.FotoDelCapitulo:
            {
                if (Espero(ahora, 0.7)) return;
                string ruta = Path.GetFullPath("../Builds/capitulo_superado.png");
                ScreenCapture.CaptureScreenshot(ruta);
                Pasar(Paso.SalirDeLaDiez, ahora);
                return;
            }

            case Paso.SalirDeLaDiez:
            {
                // La captura se escribe al final del cuadro.
                if (Espero(ahora, 0.3)) return;
                // Otra vez en la 10, ya con la 10 completada.
                Progreso.GuardarOleadaEnCurso(10, 0);
                anterior = Object.FindAnyObjectByType<WaveManager>();
                SceneManager.LoadScene(TiendaMejoras.EscenaOleadas);
                Pasar(Paso.DiezSegunda, ahora);
                return;
            }

            case Paso.DiezSegunda:
            {
                var wm = OleadasListas(ahora);
                if (wm == null) return;
                oleadaDiez2 = wm.OleadaActual;
                vidaMaxDiez = vida.maxHealth;
                vida.health = VidaDejada;
                Pasar(Paso.TerminarDiezSegunda, ahora);
                return;
            }

            case Paso.TerminarDiezSegunda:
            {
                var wm = Object.FindAnyObjectByType<WaveManager>();
                if (wm == null || vida == null) return;
                if (wm.OleadaActual < 11)
                {
                    MatarATodos(vida);
                    return;
                }
                cartelDiez2 = wm.cartelOleada != null ? wm.cartelOleada.text : "";
                vidaDiez2 = vida.health;
                // Lo de siempre: sin partida guardada y con los tres capitulos destrabados.
                Progreso.OlvidarOleadaEnCurso();
                Progreso.DepurarFijarMejorOleada(20);
                anterior = wm;
                SceneManager.LoadScene(TiendaMejoras.EscenaOleadas);
                Pasar(Paso.EnLaUno, ahora);
                return;
            }

            case Paso.EnLaUno:
            {
                var wm = OleadasListas(ahora);
                if (wm == null) return;
                oleada1 = wm.OleadaActual;
                // Lo que hacen el selector y OTRA VEZ: el capitulo, y la escena.
                Progreso.OlvidarOleadaEnCurso();
                monedasAntes = Progreso.Monedas;
                anterior = wm;
                Capitulos.ParaEmpezar = Capitulo;
                SceneManager.LoadScene(TiendaMejoras.EscenaOleadas);
                Pasar(Paso.EnLaOnce, ahora);
                return;
            }

            case Paso.EnLaOnce:
            {
                var wm = OleadasListas(ahora);
                if (wm == null) return;
                oleada11 = wm.OleadaActual;
                retomas11 = Progreso.RetomasEnCurso;
                hay11 = Progreso.HayOleadaParaRetomar;
                puntos11 = Puntaje.instance != null ? Puntaje.instance.contadorKill : -1;
                vida11 = vida.health;
                vidaMax11 = vida.maxHealth;
                volver11 = Progreso.CapituloParaVolver;
                monedas11 = Progreso.Monedas;
                cartel11 = wm.cartelOleada != null && wm.cartelOleada.gameObject.activeInHierarchy ? wm.cartelOleada.text : "";
                cartelDeLaOleada = cartel11.StartsWith(Textos.Formato("cartel_oleada", Inicial))
                                   && !cartel11.Contains(Textos.Formato("cartel_oleada_superada", Inicial - 1));
                var escenario = Object.FindAnyObjectByType<CapitulosDeEscenario>();
                cartelDelCapitulo = escenario != null && escenario.CartelEnPantalla;
                // Muere: sin oferta de revivir (en el editor no hay videos), la derrota encima.
                vida.TakeDamage(100000f);
                Pasar(Paso.Derrota, ahora);
                return;
            }

            case Paso.Derrota:
            {
                if (Espero(ahora, 1.0)) return;
                var derrota = Object.FindAnyObjectByType<MenuPerdiste>();
                if (derrota == null) return;
                foreach (var traducido in Object.FindObjectsByType<TextoTraducido>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (traducido.id != "derrota_otra_vez") continue;
                    var texto = traducido.GetComponent<TMPro.TMP_Text>();
                    if (texto != null) textoOtraVez = texto.text;
                    dejoDeContar = !traducido.enabled;
                }
                anterior = Object.FindAnyObjectByType<WaveManager>();
                derrota.Retry();
                Pasar(Paso.OtraVez, ahora);
                return;
            }

            case Paso.OtraVez:
            {
                var wm = OleadasListas(ahora);
                if (wm == null) return;
                oleadaOtraVez = wm.OleadaActual;
                var menu = Object.FindAnyObjectByType<MenuPausa>();
                if (menu == null) throw new System.Exception("no hay MenuPausa");
                anterior = wm;
                menu.ReiniciarYa();
                Pasar(Paso.Reiniciada, ahora);
                return;
            }

            case Paso.Reiniciada:
            {
                var wm = OleadasListas(ahora);
                if (wm == null) return;
                oleadaReiniciada = wm.OleadaActual;
                anterior = wm;
                SalirAlMenuYVolver();
                Pasar(Paso.PrimeraRetoma, ahora);
                return;
            }

            case Paso.PrimeraRetoma:
            {
                var wm = OleadasListas(ahora);
                if (wm == null) return;
                oleadaRetomada = wm.OleadaActual;
                retomasRetomada = Progreso.RetomasEnCurso;
                anterior = wm;
                SalirAlMenuYVolver();
                Pasar(Paso.SegundaSalida, ahora);
                return;
            }

            case Paso.SegundaSalida:
            {
                var wm = OleadasListas(ahora);
                if (wm == null) return;
                oleadaDespues = wm.OleadaActual;
                retomasDespues = Progreso.RetomasEnCurso;
                Pasar(Paso.Listo, ahora);
                Terminar(null);
                return;
            }
        }
    }

    // Los zombis, apenas salen: los comunes se despejan (cuentan como muertos) y el jefe, que no se despeja, muere.
    static void MatarATodos(PlayerHealth vida)
    {
        EnemyController.DespejarAlrededor(vida.transform.position, 500f);
        var jefes = new System.Collections.Generic.List<EnemyController>(EnemyController.Jefes);
        foreach (var jefe in jefes)
            if (jefe != null && jefe.Vivo) jefe.DanoZombi(1e9f);
    }

    // MENU PRINCIPAL de la pausa y, como OLEADAS sin selector, WaveMode otra vez (sin capitulo pedido).
    static void SalirAlMenuYVolver()
    {
        var menu = Object.FindAnyObjectByType<MenuPausa>();
        if (menu == null) throw new System.Exception("no hay MenuPausa");
        menu.Pausar();
        Progreso.Guardar();
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene(TiendaMejoras.EscenaOleadas);
    }

    static void Pasar(Paso siguiente, double ahora)
    {
        paso = siguiente;
        pasoDesde = ahora;
        cuadroDelPaso = Time.frameCount;
    }

    static bool Espero(double ahora, double segundos)
    {
        return ahora - pasoDesde < segundos || Time.frameCount - cuadroDelPaso < 2;
    }

    static void Terminar(string error)
    {
        if (terminado) return;
        terminado = true;

        var inf = new StringBuilder();
        inf.AppendLine("Prueba de empezar en un capitulo (WaveMode, con la 20 completada, capitulo " + Capitulo + ")");
        inf.AppendLine();
        if (error != null) inf.AppendLine("ERROR: " + error);
        inf.AppendLine("La 10 por primera vez: retomada en la " + oleadaDiez1 + ", cartel \"" + cartelDiez1.Replace("\n", " / ")
                       + "\", monedas " + monedasDiez1Antes.ToString("0.##") + " -> " + monedasDiez1.ToString("0.##")
                       + ", capitulo para volver " + volverDiez1 + ", vida " + VidaDejada + " -> " + vidaDiez1);
        inf.AppendLine("La 10 otra vez: retomada en la " + oleadaDiez2 + ", cartel \"" + cartelDiez2.Replace("\n", " / ")
                       + "\", vida " + VidaDejada + " -> " + vidaDiez2 + " de " + vidaMaxDiez);
        inf.AppendLine("Sin capitulo pedido: oleada " + oleada1);
        inf.AppendLine("Con el capitulo " + Capitulo + ": oleada " + oleada11 + ", retomas " + retomas11 + ", se puede retomar " + hay11
                       + ", puntos " + puntos11 + ", vida " + vida11 + "/" + vidaMax11 + ", capitulo para volver " + volver11
                       + ", monedas " + monedasAntes.ToString("0.##") + " -> " + monedas11.ToString("0.##"));
        inf.AppendLine("  cartel de la oleada \"" + cartel11.Replace("\n", " / ") + "\"; cartel del capitulo " + cartelDelCapitulo);
        inf.AppendLine("Derrota: OTRA VEZ dice \"" + textoOtraVez.Replace("\n", " / ") + "\" (traduccion apagada " + dejoDeContar + ")");
        inf.AppendLine("OTRA VEZ: oleada " + oleadaOtraVez + "; REINICIAR: oleada " + oleadaReiniciada);
        inf.AppendLine("Retomada: oleada " + oleadaRetomada + ", retomas " + retomasRetomada + "; despues de la segunda salida: oleada "
                       + oleadaDespues + ", retomas " + retomasDespues);
        inf.AppendLine("Errores y excepciones durante la prueba: " + cuantasExcepciones);
        if (cuantasExcepciones > 0) inf.Append(excepciones);
        inf.AppendLine();

        string desde = Textos.Formato("derrota_desde_la_oleada", Inicial);
        string superado = Textos.Formato("cartel_capitulo_superado", 1);
        string puntoDeControl = Textos.Formato("cartel_punto_de_control", 250).Trim();
        int curada = Mathf.Min(vidaMaxDiez, VidaDejada + Mathf.Max(1, Mathf.RoundToInt(vidaMaxDiez * Capitulos.CuraAlCruzar)));
        bool[] ok =
        {
            error == null && cuantasExcepciones == 0,
            oleadaDiez1 == 10 && cartelDiez1.StartsWith(superado) && cartelDiez1.Contains(puntoDeControl),
            monedasDiez1 - monedasDiez1Antes >= 250 - 1e-6 && volverDiez1 == 2 && vidaDiez1 == VidaDejada,
            oleadaDiez2 == 10 && cartelDiez2.StartsWith(superado) && !cartelDiez2.Contains(puntoDeControl),
            vidaDiez2 == curada && vidaDiez2 > VidaDejada,
            oleada1 == 1,
            oleada11 == Inicial && retomas11 == 0 && hay11,
            cartelDeLaOleada && cartelDelCapitulo,
            puntos11 == 0 && vida11 == vidaMax11 && vidaMax11 > 0 && System.Math.Abs(monedas11 - monedasAntes) < 1e-6,
            volver11 == Capitulo,
            textoOtraVez.StartsWith(Textos.De("derrota_otra_vez")) && textoOtraVez.Contains(desde) && dejoDeContar,
            oleadaOtraVez == Inicial,
            oleadaReiniciada == Inicial,
            oleadaRetomada == Inicial && retomasRetomada == 1,
            oleadaDespues == Inicial && retomasDespues == 0,
        };
        string[] que =
        {
            "el banco llego hasta el final, sin errores ni excepciones",
            "la 10 por primera vez cierra con ¡CAPITULO 1 SUPERADO! y el punto de control",
            "paga 250 monedas, deja el capitulo 2 para volver y no cura",
            "la 10 otra vez cierra con el capitulo superado, sin punto de control",
            "y cura la mitad de la vida maxima",
            "sin capitulo pedido, las oleadas arrancan en la 1",
            "con el capitulo 2 pedido arrancan en la 11, como una oleada nueva",
            "con el cartel de la 11 (no el de una superada) y el del capitulo",
            "sin puntos, con la vida llena y sin tocar las monedas",
            "el capitulo queda para volver",
            "OTRA VEZ dice desde que oleada",
            "OTRA VEZ arranca en la 11",
            "REINICIAR tambien",
            "salir al menu y volver retoma la 11, y gasta la retoma",
            "despues de la segunda salida, la partida nueva arranca en el principio del capitulo, no en la 1",
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
        Debug.Log(inf.ToString());
    }
}
