using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Banco de salir al menu en las oleadas y retomar (superauditoria del 29/9, H04; lo confirmo Argon el 21/10), en play,
// con el camino de verdad: MENU PRINCIPAL de la pausa y despues WaveMode cargada como la carga OLEADAS.
//
//  1. Una partida guardada en la 5 por un banco (sin vida ni balas anotadas, con PODER 3) se retoma en la 5, con sus
//     puntos y sus cartas y con la vida llena, y gasta su retoma. Se la deja en 33 de vida y 211 balas y se la termina
//     sin un golpe: sale perfecta (no habia golpe que heredar).
//  2. La 6 es nueva: anota la vida, la vida maxima, las balas y el cargador con que empieza, sin retomas ni golpe. Se
//     cambia la vida y las balas (lo que se gasto jugando) y, pasado el descanso, el jugador recibe un golpe: queda
//     anotado en el progreso.
//  3. MENU PRINCIPAL: el boton OLEADAS dice que se sigue en la 6. Retomarla devuelve la vida y las balas con que se la
//     empezo (no las de cuando se salio, ni la vida llena), gasta la retoma y hereda el golpe: terminada sin un golpe
//     nuevo, ya no sale perfecta, ni sube el logro INTOCABLE.
//  4. La 7 es nueva otra vez: se puede retomar. Se sale, se retoma (gasta la retoma) y se sale de nuevo: el boton
//     OLEADAS ya no dice que se sigue, y entrar arranca de la 1, sin puntos ni cartas.
//
// Los zombis se despejan apenas salen (cuentan como muertos, sin tocar al jugador), asi las oleadas terminan solas. El
// progreso y los PlayerPrefs del editor vuelven como estaban al salir de play (RespaldoDelBanco). Escribe
// Builds/prueba_retomar.txt.
//
// OJO: entrar y salir de play hace que Unity vuelva a serializar ProjectSettings (ver la trampa en CLAUDE.md): despues
// de correrlo, revertir lo que no se toco a proposito.
[InitializeOnLoad]
public static class PruebaRetomar
{
    const string Clave = "ShowBies.PruebaRetomar";
    const string Ruta = "../Builds/prueba_retomar.txt";
    const string Escena = "Assets/Escenas/WaveMode.unity";
    const int OleadaGuardada = 5;
    const int PuntosGuardados = 77;
    const int PoderGuardado = 3;
    const int VidaDejada = 33;
    const int BalasDejadas = 211;
    const double TopePorPaso = 25.0;
    const double TopeTotal = 180.0;

    enum Paso
    {
        EnLaCinco,          // la partida retomada en la 5: deja la vida y las balas
        TerminarLaCinco,    // despeja hasta que empieza la 6
        GolpeEnLaSeis,      // pasado el descanso, un golpe
        MarcaDelGolpe,      // que quede anotado, y MENU PRINCIPAL
        MenuDeLaSeis,       // el boton OLEADAS y a retomar
        RetomaLaSeis,       // la vida y las balas devueltas
        TerminarLaSeis,     // despeja hasta que empieza la 7
        MenuDeLaSiete,
        RetomaLaSiete,      // gasta la retoma y sale otra vez
        MenuSinRetomar,     // el boton ya no dice que se sigue
        DeCero,             // entrar arranca de la 1
        Listo,
    }

    static Paso paso;
    static double inicio, pasoDesde;
    static int cuadroDelPaso;
    static bool terminado;
    static WaveManager oleadasVistas;
    static int cuadroDeLaEscena;
    static double vistasDesde;

    // Lo medido.
    static int oleada5 = -1, puntos5 = -1, poder5 = -1, retomas5 = -1, vidaMax5 = -1, vida5 = -1;
    static bool hay5 = true, perfecta5;
    static int intacta5 = -1;
    static int vida6Guardada = -2, vidaMax6Guardada = -2, balas6Guardadas = -2, maxBalas6Guardado = -2, retomas6 = -1;
    static int vidaAl6 = -1, vidaMaxAl6 = -1, balasAl6 = -1, maxBalasAl6 = -1;
    static bool golpe6Al6 = true, hay6 = false;
    static bool marcaDelGolpe;
    static string textoMenu6 = "";
    static bool sigue6;
    static int oleadaRetomada6 = -1, vidaRetomada6 = -1, balasRetomadas6 = -1, maxBalasRetomado6 = -1, retomas6Al = -1;
    static bool golpeHeredado6, hayRetomada6 = true;
    static bool perfecta6 = true;
    static int golpesAlRetomar6 = -1, golpesAlTerminar6 = -1, intacta6 = -1;
    static int retomas7 = -1;
    static bool golpe7 = true, hay7 = false;
    static string textoMenu7 = "";
    static bool sigue7;
    static int oleadaRetomada7 = -1, retomas7Al = -1;
    static string textoSinRetomar = "";
    static bool sinRetomarBoton, sinRetomarProgreso;
    static int oleadaDeCero = -1, guardadaDeCero = -1, puntosDeCero = -1, poderDeCero = -1, vidaDeCero = -1, vidaMaxDeCero = -1;

    static readonly StringBuilder excepciones = new StringBuilder();
    static int cuantasExcepciones;

    static PruebaRetomar()
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

    [MenuItem("ShowBies/Pruebas/Salir al menu y retomar (play)")]
    // Publico para poder correrlo por codigo (ver la trampa del registro de menus).
    public static void Arrancar()
    {
        if (!RespaldoDelBanco.PuedeArrancar("PruebaRetomar")) return;
        RespaldoDelBanco.Guardar("PruebaRetomar");
        PlayerSettings.runInBackground = true;
        // Sin auto-apuntado: las balas tienen que quedar como se las deja.
        PlayerPrefs.SetInt("AutoApuntar", 0);
        // Como la arma un banco: sin la vida ni las balas anotadas.
        Progreso.GuardarOleadaEnCurso(OleadaGuardada, PuntosGuardados);
        Progreso.GuardarPoderEnCurso(new EstadoPoder { poder = PoderGuardado, semilla = 1234 });
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
        if (!RespaldoDelBanco.SigueArmado("PruebaRetomar", Clave)) return;

        double ahora = EditorApplication.timeSinceStartup;
        if (!SessionState.GetBool(Clave + ".empezo", false))
        {
            SessionState.SetBool(Clave + ".empezo", true);
            inicio = ahora;
            terminado = false;
            oleadasVistas = null;
            Pasar(Paso.EnLaCinco, ahora);
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

    // El WaveManager de la escena cargada, cuando ya lleva tres cuadros y medio segundo (el que retoma devuelve la vida
    // y las balas en su segundo cuadro). Null mientras tanto.
    static WaveManager OleadasListas(double ahora)
    {
        var wm = Object.FindAnyObjectByType<WaveManager>();
        if (wm == null || PlayerHealth.instance == null) return null;
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
        var control = vida != null ? vida.GetComponent<PlayerController>() : null;
        switch (paso)
        {
            case Paso.EnLaCinco:
            {
                var wm = OleadasListas(ahora);
                if (wm == null || control == null) return;
                oleada5 = wm.OleadaActual;
                puntos5 = Puntaje.instance != null ? Puntaje.instance.contadorKill : -1;
                poder5 = PoderDeLaPartida.Poder;
                retomas5 = Progreso.RetomasEnCurso;
                hay5 = Progreso.HayOleadaParaRetomar;
                vida5 = vida.health;
                vidaMax5 = vida.maxHealth;
                vida.health = Mathf.Min(VidaDejada, vida.maxHealth);
                control.cantBalas = BalasDejadas;
                Pasar(Paso.TerminarLaCinco, ahora);
                return;
            }

            case Paso.TerminarLaCinco:
            {
                var wm = Object.FindAnyObjectByType<WaveManager>();
                if (wm == null || vida == null || control == null) return;
                if (wm.OleadaActual < OleadaGuardada + 1)
                {
                    EnemyController.DespejarAlrededor(vida.transform.position, 500f);
                    return;
                }
                perfecta5 = wm.UltimaFuePerfecta;
                intacta5 = Progreso.MejorOleadaIntacta;
                vida6Guardada = Progreso.VidaEnCurso;
                vidaMax6Guardada = Progreso.VidaMaximaEnCurso;
                balas6Guardadas = Progreso.BalasEnCurso;
                maxBalas6Guardado = Progreso.MaxBalasEnCurso;
                retomas6 = Progreso.RetomasEnCurso;
                golpe6Al6 = Progreso.GolpeEnOleadaEnCurso;
                hay6 = Progreso.HayOleadaParaRetomar;
                vidaAl6 = vida.health;
                vidaMaxAl6 = vida.maxHealth;
                balasAl6 = control.cantBalas;
                maxBalasAl6 = control.maxBalas;
                // Lo que se gasta jugando la 6 antes de salir: retomarla no lo tiene que devolver.
                vida.health = Mathf.Min(VidaDejada + 30, vida.maxHealth);
                control.cantBalas = BalasDejadas + 288;
                Pasar(Paso.GolpeEnLaSeis, ahora);
                return;
            }

            case Paso.GolpeEnLaSeis:
            {
                var wm = Object.FindAnyObjectByType<WaveManager>();
                if (wm == null || vida == null || wm.EnDescanso || Espero(ahora, 0.3)) return;
                vida.TakeDamage(1f);
                Pasar(Paso.MarcaDelGolpe, ahora);
                return;
            }

            case Paso.MarcaDelGolpe:
            {
                if (Espero(ahora, 0.2)) return;
                marcaDelGolpe = Progreso.GolpeEnOleadaEnCurso;
                SalirAlMenu();
                Pasar(Paso.MenuDeLaSeis, ahora);
                return;
            }

            case Paso.MenuDeLaSeis:
            {
                string texto = TextoDeOleadas(ahora);
                if (texto == null) return;
                textoMenu6 = texto;
                sigue6 = texto.Contains(Textos.Formato("modo_oleadas_continuar", OleadaGuardada + 1));
                SceneManager.LoadScene(TiendaMejoras.EscenaOleadas);
                Pasar(Paso.RetomaLaSeis, ahora);
                return;
            }

            case Paso.RetomaLaSeis:
            {
                var wm = OleadasListas(ahora);
                if (wm == null || control == null) return;
                oleadaRetomada6 = wm.OleadaActual;
                vidaRetomada6 = vida.health;
                balasRetomadas6 = control.cantBalas;
                maxBalasRetomado6 = control.maxBalas;
                retomas6Al = Progreso.RetomasEnCurso;
                hayRetomada6 = Progreso.HayOleadaParaRetomar;
                golpeHeredado6 = Progreso.GolpeEnOleadaEnCurso;
                golpesAlRetomar6 = vida.GolpesRecibidos;
                Pasar(Paso.TerminarLaSeis, ahora);
                return;
            }

            case Paso.TerminarLaSeis:
            {
                var wm = Object.FindAnyObjectByType<WaveManager>();
                if (wm == null || vida == null) return;
                if (wm.OleadaActual < OleadaGuardada + 2)
                {
                    EnemyController.DespejarAlrededor(vida.transform.position, 500f);
                    return;
                }
                perfecta6 = wm.UltimaFuePerfecta;
                golpesAlTerminar6 = vida.GolpesRecibidos;
                intacta6 = Progreso.MejorOleadaIntacta;
                retomas7 = Progreso.RetomasEnCurso;
                golpe7 = Progreso.GolpeEnOleadaEnCurso;
                hay7 = Progreso.HayOleadaParaRetomar;
                SalirAlMenu();
                Pasar(Paso.MenuDeLaSiete, ahora);
                return;
            }

            case Paso.MenuDeLaSiete:
            {
                string texto = TextoDeOleadas(ahora);
                if (texto == null) return;
                textoMenu7 = texto;
                sigue7 = texto.Contains(Textos.Formato("modo_oleadas_continuar", OleadaGuardada + 2));
                SceneManager.LoadScene(TiendaMejoras.EscenaOleadas);
                Pasar(Paso.RetomaLaSiete, ahora);
                return;
            }

            case Paso.RetomaLaSiete:
            {
                var wm = OleadasListas(ahora);
                if (wm == null) return;
                oleadaRetomada7 = wm.OleadaActual;
                retomas7Al = Progreso.RetomasEnCurso;
                SalirAlMenu();
                Pasar(Paso.MenuSinRetomar, ahora);
                return;
            }

            case Paso.MenuSinRetomar:
            {
                string texto = TextoDeOleadas(ahora);
                if (texto == null) return;
                textoSinRetomar = texto;
                sinRetomarBoton = texto == Textos.De("modo_oleadas");
                sinRetomarProgreso = !Progreso.HayOleadaParaRetomar;
                SceneManager.LoadScene(TiendaMejoras.EscenaOleadas);
                Pasar(Paso.DeCero, ahora);
                return;
            }

            case Paso.DeCero:
            {
                var wm = OleadasListas(ahora);
                if (wm == null) return;
                oleadaDeCero = wm.OleadaActual;
                guardadaDeCero = Progreso.OleadaEnCurso;
                puntosDeCero = Puntaje.instance != null ? Puntaje.instance.contadorKill : -1;
                poderDeCero = PoderDeLaPartida.Poder;
                vidaDeCero = vida.health;
                vidaMaxDeCero = vida.maxHealth;
                Pasar(Paso.Listo, ahora);
                Terminar(null);
                return;
            }
        }
    }

    // Lo que hace MENU PRINCIPAL de la pausa: pausar (que guarda) e ir al menu.
    static void SalirAlMenu()
    {
        var menu = Object.FindAnyObjectByType<MenuPausa>();
        if (menu == null) throw new System.Exception("no hay MenuPausa en la escena");
        menu.Pausar();
        menu.IrAlMenu();
    }

    // El texto del boton OLEADAS del menu, con el panel de modos abierto (asi se pinta). Null mientras no esta.
    static string TextoDeOleadas(double ahora)
    {
        if (SceneManager.GetActiveScene().buildIndex != TiendaMejoras.EscenaMenu || Espero(ahora, 0.5)) return null;
        var boton = Object.FindAnyObjectByType<BotonOleadas>(FindObjectsInactive.Include);
        if (boton == null || boton.texto == null) return null;
        if (!boton.gameObject.activeInHierarchy)
        {
            for (var t = boton.transform; t != null; t = t.parent) t.gameObject.SetActive(true);
            cuadroDelPaso = Time.frameCount;
            return null;
        }
        if (Time.frameCount - cuadroDelPaso < 2) return null;
        return boton.texto.text;
    }

    static void Pasar(Paso siguiente, double ahora)
    {
        paso = siguiente;
        pasoDesde = ahora;
        cuadroDelPaso = Time.frameCount;
    }

    // Si todavia no pasaron esos segundos reales, o dos cuadros del juego, desde que empezo el paso.
    static bool Espero(double ahora, double segundos)
    {
        return ahora - pasoDesde < segundos || Time.frameCount - cuadroDelPaso < 2;
    }

    static string Una(string texto)
    {
        return texto.Replace("\n", " / ");
    }

    static void Terminar(string error)
    {
        if (terminado) return;
        terminado = true;

        var inf = new StringBuilder();
        inf.AppendLine("Prueba de salir al menu y retomar (WaveMode, partida guardada en la " + OleadaGuardada + " con "
                       + PuntosGuardados + " puntos y PODER " + PoderGuardado + ")");
        inf.AppendLine();
        if (error != null) inf.AppendLine("ERROR: " + error);
        inf.AppendLine("La 5 retomada: oleada " + oleada5 + ", puntos " + puntos5 + ", PODER " + poder5 + ", vida " + vida5 + "/" + vidaMax5
                       + ", retomas " + retomas5 + ", se puede retomar " + hay5 + "; terminada perfecta " + perfecta5 + ", INTOCABLE " + intacta5);
        inf.AppendLine("La 6 al empezar: en el jugador vida " + vidaAl6 + "/" + vidaMaxAl6 + ", balas " + balasAl6 + "/" + maxBalasAl6
                       + "; anotado vida " + vida6Guardada + "/" + vidaMax6Guardada + ", balas " + balas6Guardadas + "/" + maxBalas6Guardado
                       + ", retomas " + retomas6 + ", golpe " + golpe6Al6 + ", se puede retomar " + hay6);
        inf.AppendLine("Golpe pasado el descanso: anotado " + marcaDelGolpe);
        inf.AppendLine("Menu: OLEADAS dice \"" + Una(textoMenu6) + "\"");
        inf.AppendLine("La 6 retomada: oleada " + oleadaRetomada6 + ", vida " + vidaRetomada6 + ", balas " + balasRetomadas6 + "/" + maxBalasRetomado6
                       + ", retomas " + retomas6Al + ", se puede retomar " + hayRetomada6 + ", golpe heredado " + golpeHeredado6);
        inf.AppendLine("  terminada: golpes " + golpesAlRetomar6 + " -> " + golpesAlTerminar6 + ", perfecta " + perfecta6 + ", INTOCABLE " + intacta6);
        inf.AppendLine("La 7 al empezar: retomas " + retomas7 + ", golpe " + golpe7 + ", se puede retomar " + hay7);
        inf.AppendLine("Menu: OLEADAS dice \"" + Una(textoMenu7) + "\"");
        inf.AppendLine("La 7 retomada: oleada " + oleadaRetomada7 + ", retomas " + retomas7Al);
        inf.AppendLine("Menu despues de la segunda salida: OLEADAS dice \"" + Una(textoSinRetomar) + "\", se puede retomar " + !sinRetomarProgreso);
        inf.AppendLine("Entrar de nuevo: oleada " + oleadaDeCero + ", guardada " + guardadaDeCero + ", puntos " + puntosDeCero + ", PODER " + poderDeCero
                       + ", vida " + vidaDeCero + "/" + vidaMaxDeCero);
        inf.AppendLine("Errores y excepciones durante la prueba: " + cuantasExcepciones);
        if (cuantasExcepciones > 0) inf.Append(excepciones);
        inf.AppendLine();

        int vidaEsperada = Mathf.Min(VidaDejada, vidaMax5);
        bool[] ok =
        {
            error == null && cuantasExcepciones == 0,
            oleada5 == OleadaGuardada && puntos5 == PuntosGuardados && poder5 == PoderGuardado,
            vida5 == vidaMax5 && vidaMax5 > 0,
            retomas5 == 1 && !hay5,
            perfecta5 && intacta5 >= OleadaGuardada,
            vida6Guardada == vidaAl6 && vidaAl6 == vidaEsperada && vidaMax6Guardada == vidaMaxAl6
                && balas6Guardadas == balasAl6 && balasAl6 >= BalasDejadas && maxBalas6Guardado == maxBalasAl6,
            retomas6 == 0 && !golpe6Al6 && hay6,
            marcaDelGolpe,
            sigue6,
            oleadaRetomada6 == OleadaGuardada + 1 && vidaRetomada6 == vida6Guardada
                && balasRetomadas6 == balas6Guardadas && maxBalasRetomado6 >= maxBalas6Guardado,
            retomas6Al == 1 && !hayRetomada6 && golpeHeredado6,
            golpesAlTerminar6 == golpesAlRetomar6 && !perfecta6 && intacta6 == intacta5,
            retomas7 == 0 && !golpe7 && hay7 && sigue7,
            oleadaRetomada7 == OleadaGuardada + 2 && retomas7Al == 1,
            sinRetomarBoton && sinRetomarProgreso,
            oleadaDeCero == 1 && guardadaDeCero == 1 && puntosDeCero == 0 && poderDeCero == 0 && vidaDeCero == vidaMaxDeCero,
        };
        string[] que =
        {
            "el banco llego hasta el final, sin errores ni excepciones",
            "la partida de un banco se retoma en su oleada, con sus puntos y sus cartas",
            "sin vida anotada, se retoma con la vida llena",
            "retomar gasta la retoma de la oleada",
            "una oleada retomada sin golpe que heredar sale perfecta",
            "la oleada nueva anota la vida, la vida maxima, las balas y el cargador con que empieza",
            "la oleada nueva no tiene retomas ni golpe, y se puede retomar",
            "el primer golpe pasado el descanso queda anotado",
            "el boton OLEADAS dice en que oleada se sigue",
            "retomar devuelve la vida y las balas con que se empezo la oleada, no las de cuando se salio",
            "retomarla gasta la retoma y hereda el golpe",
            "la oleada retomada con un golpe heredado no sale perfecta, aunque no reciba ninguno nuevo, ni sube INTOCABLE",
            "la oleada siguiente es nueva: sin golpe y se puede retomar, y el boton lo dice",
            "la siguiente tambien se retoma, y gasta su retoma",
            "despues de la segunda salida, el boton OLEADAS ya no dice que se sigue",
            "y entrar arranca de la 1, sin puntos, sin cartas y con la vida llena",
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
