using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Banco del selector de capitulos (1.7.0, de Argon, 21/10), en play, en el menu:
//
//  1. Con la 25 completada y sin partida para retomar, OLEADAS abre el selector (sin cargar nada): tres tarjetas con
//     ¡JUGAR!, la del capitulo 3 respirando (la mas alta), y lo que trae cada una. Foto en Builds/selector_capitulos.png.
//     El atras de Android lo cierra.
//  2. Con la 12: el capitulo 3 bloqueado (sin ¡JUGAR!, con "COMPLETA LA OLEADA 20" y el candado) y el 2 respirando. Foto
//     en Builds/selector_capitulos_bloqueado.png. ¡JUGAR! del 2 carga las oleadas en la 11, con la preparacion pendiente.
//  3. Con la 5 (nada destrabado), OLEADAS no abre el selector y entra a la 1.
//
// El progreso y los PlayerPrefs del editor vuelven como estaban al salir de play (RespaldoDelBanco). Escribe
// Builds/prueba_selector_capitulos.txt.
//
// OJO: entrar y salir de play hace que Unity vuelva a serializar ProjectSettings (ver la trampa en CLAUDE.md): despues
// de correrlo, revertir lo que no se toco a proposito.
[InitializeOnLoad]
public static class PruebaSelectorDeCapitulos
{
    const string Clave = "ShowBies.PruebaSelectorDeCapitulos";
    const string Ruta = "../Builds/prueba_selector_capitulos.txt";
    const string Escena = "Assets/Escenas/Menu.unity";
    const double TopePorPaso = 25.0;
    const double TopeTotal = 120.0;

    enum Paso
    {
        EnElMenu,           // con la 25: OLEADAS
        Abierto,            // las tarjetas, la foto, y el atras
        ConLaDoce,          // con la 12: OLEADAS otra vez
        AbiertoConLaDoce,   // el 3 bloqueado, la foto, y ¡JUGAR! del 2
        EnLaOnce,           // las oleadas, en la 11
        MenuConLaCinco,     // con la 5: OLEADAS entra derecho
        EnLaUno,
        Listo,
    }

    static Paso paso;
    static double inicio, pasoDesde;
    static int cuadroDelPaso;
    static bool terminado, foto1, foto2;

    // Lo medido.
    static bool abrio25, sigueEnElMenu25, atrasLoCierra;
    static int jugar25 = -1;
    static string respira25 = "", ventajas25 = "";
    static bool abrio12, tresBloqueado, candado3, bloqueadoTexto3, dosRespira;
    static int oleada11 = -1, pendientes11 = -1;
    static bool abrio5 = true;
    static int oleada1 = -1;

    static readonly StringBuilder excepciones = new StringBuilder();
    static int cuantasExcepciones;

    static PruebaSelectorDeCapitulos()
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

    [MenuItem("ShowBies/Pruebas/Selector de capitulos (play)")]
    // Publico para poder correrlo por codigo (ver la trampa del registro de menus).
    public static void Arrancar()
    {
        if (!RespaldoDelBanco.PuedeArrancar("PruebaSelectorDeCapitulos")) return;
        RespaldoDelBanco.Guardar("PruebaSelectorDeCapitulos");
        PlayerSettings.runInBackground = true;
        Progreso.OlvidarOleadaEnCurso();
        Progreso.DepurarFijarMejorOleada(25);
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
        if (!RespaldoDelBanco.SigueArmado("PruebaSelectorDeCapitulos", Clave)) return;

        double ahora = EditorApplication.timeSinceStartup;
        if (!SessionState.GetBool(Clave + ".empezo", false))
        {
            SessionState.SetBool(Clave + ".empezo", true);
            inicio = ahora;
            terminado = false;
            foto1 = foto2 = false;
            Pasar(Paso.EnElMenu, ahora);
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

    static void Avanzar(double ahora)
    {
        switch (paso)
        {
            case Paso.EnElMenu:
            {
                if (!EnElMenuListo(ahora)) return;
                Oleadas();
                abrio25 = SelectorDeCapitulos.Abierto;
                Pasar(Paso.Abierto, ahora);
                return;
            }

            case Paso.Abierto:
            {
                if (Espero(ahora, 0.7)) return;
                sigueEnElMenu25 = SceneManager.GetActiveScene().buildIndex == TiendaMejoras.EscenaMenu;
                jugar25 = 0;
                respira25 = "";
                var ventajas = new List<string>();
                for (int c = 1; c <= 3; c++)
                {
                    var tarjeta = Tarjeta(c);
                    if (tarjeta == null) continue;
                    var jugar = tarjeta.Find("Jugar");
                    if (jugar != null)
                    {
                        jugar25++;
                        if (jugar.GetComponent<BotonJugoso>().respirar) respira25 += c;
                    }
                    var ventaja = tarjeta.Find("Ventaja/Texto");
                    ventajas.Add(ventaja != null ? ventaja.GetComponent<TMPro.TMP_Text>().text : "");
                }
                ventajas25 = string.Join(" | ", ventajas);
                if (!foto1)
                {
                    foto1 = true;
                    ScreenCapture.CaptureScreenshot(Path.GetFullPath("../Builds/selector_capitulos.png"));
                    pasoDesde = ahora;
                    return;
                }
                if (Espero(ahora, 0.3)) return;
                var atras = Object.FindAnyObjectByType<BotonAtrasMenu>();
                if (atras != null) atras.Atras();
                atrasLoCierra = !SelectorDeCapitulos.Abierto;
                Progreso.DepurarFijarMejorOleada(12);
                Pasar(Paso.ConLaDoce, ahora);
                return;
            }

            case Paso.ConLaDoce:
            {
                if (Espero(ahora, 0.3)) return;
                Oleadas();
                abrio12 = SelectorDeCapitulos.Abierto;
                Pasar(Paso.AbiertoConLaDoce, ahora);
                return;
            }

            case Paso.AbiertoConLaDoce:
            {
                if (Espero(ahora, 0.7)) return;
                var tres = Tarjeta(3);
                var dos = Tarjeta(2);
                if (tres == null || dos == null) return;
                tresBloqueado = tres.Find("Jugar") == null;
                candado3 = tres.Find("CandadoCuerpo") != null;
                var bloqueado = tres.Find("Bloqueado");
                bloqueadoTexto3 = bloqueado != null && bloqueado.GetComponent<TMPro.TMP_Text>().text == Textos.Formato("capitulos_bloqueado", 20);
                var jugarDos = dos.Find("Jugar");
                dosRespira = jugarDos != null && jugarDos.GetComponent<BotonJugoso>().respirar;
                if (!foto2)
                {
                    foto2 = true;
                    ScreenCapture.CaptureScreenshot(Path.GetFullPath("../Builds/selector_capitulos_bloqueado.png"));
                    pasoDesde = ahora;
                    return;
                }
                if (Espero(ahora, 0.3)) return;
                if (jugarDos != null) jugarDos.GetComponent<Button>().onClick.Invoke();
                Pasar(Paso.EnLaOnce, ahora);
                return;
            }

            case Paso.EnLaOnce:
            {
                if (SceneManager.GetActiveScene().buildIndex != TiendaMejoras.EscenaOleadas || Espero(ahora, 1.0)) return;
                var wm = Object.FindAnyObjectByType<WaveManager>();
                if (wm == null) return;
                oleada11 = wm.OleadaActual;
                pendientes11 = PoderDeLaPartida.Pendientes;
                Progreso.OlvidarOleadaEnCurso();
                Progreso.DepurarFijarMejorOleada(5);
                SceneManager.LoadScene(TiendaMejoras.EscenaMenu);
                Pasar(Paso.MenuConLaCinco, ahora);
                return;
            }

            case Paso.MenuConLaCinco:
            {
                if (!EnElMenuListo(ahora)) return;
                Oleadas();
                abrio5 = SelectorDeCapitulos.Abierto;
                Pasar(Paso.EnLaUno, ahora);
                return;
            }

            case Paso.EnLaUno:
            {
                if (SceneManager.GetActiveScene().buildIndex != TiendaMejoras.EscenaOleadas || Espero(ahora, 1.0)) return;
                var wm = Object.FindAnyObjectByType<WaveManager>();
                if (wm == null) return;
                oleada1 = wm.OleadaActual;
                Pasar(Paso.Listo, ahora);
                Terminar(null);
                return;
            }
        }
    }

    // El menu cargado y, si salio la recompensa diaria, cerrada (taparia la foto).
    static bool EnElMenuListo(double ahora)
    {
        if (SceneManager.GetActiveScene().buildIndex != TiendaMejoras.EscenaMenu || Espero(ahora, 1.5)) return false;
        if (VentanaRecompensaDiaria.Abierta)
        {
            var diaria = Object.FindAnyObjectByType<VentanaRecompensaDiaria>();
            if (diaria != null) diaria.Cerrar();
            return false;
        }
        return Object.FindAnyObjectByType<SelectorDeCapitulos>() != null;
    }

    // Lo que hace el boton OLEADAS del panel de modos.
    static void Oleadas()
    {
        var menu = Object.FindAnyObjectByType<MainMenu>(FindObjectsInactive.Include);
        if (menu == null) throw new System.Exception("no hay MainMenu en el menu");
        menu.GameModes();
    }

    static Transform Tarjeta(int capitulo)
    {
        var selector = Object.FindAnyObjectByType<SelectorDeCapitulos>();
        return selector != null ? selector.transform.Find("Selector/Ventana/Capitulo" + capitulo) : null;
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
        inf.AppendLine("Prueba del selector de capitulos (el menu)");
        inf.AppendLine();
        if (error != null) inf.AppendLine("ERROR: " + error);
        inf.AppendLine("Con la 25: se abrio " + abrio25 + ", sigue en el menu " + sigueEnElMenu25 + ", con ¡JUGAR! " + jugar25
                       + ", respira el " + respira25 + "; trae: " + ventajas25 + "; el atras lo cierra " + atrasLoCierra);
        inf.AppendLine("Con la 12: se abrio " + abrio12 + ", el 3 sin ¡JUGAR! " + tresBloqueado + ", candado " + candado3
                       + ", dice la oleada 20 " + bloqueadoTexto3 + ", respira el 2 " + dosRespira);
        inf.AppendLine("¡JUGAR! del 2: oleada " + oleada11 + ", elecciones pendientes " + pendientes11);
        inf.AppendLine("Con la 5: se abrio " + abrio5 + ", oleada " + oleada1);
        inf.AppendLine("Errores y excepciones durante la prueba: " + cuantasExcepciones);
        if (cuantasExcepciones > 0) inf.Append(excepciones);
        inf.AppendLine();

        string doce = Textos.Formato("capitulos_cartas_doradas", 12, 2);
        string seis = Textos.Formato("capitulos_cartas_una_dorada", 6);
        bool[] ok =
        {
            error == null && cuantasExcepciones == 0,
            abrio25 && sigueEnElMenu25,
            jugar25 == 3 && respira25 == "3",
            ventajas25 == Textos.De("capitulos_sin_ventajas") + " | " + seis + " | " + doce,
            atrasLoCierra,
            abrio12 && tresBloqueado && candado3 && bloqueadoTexto3 && dosRespira,
            oleada11 == 11 && pendientes11 == 3,
            !abrio5 && oleada1 == 1,
        };
        string[] que =
        {
            "el banco llego hasta el final, sin errores ni excepciones",
            "con la 25, OLEADAS abre el selector sin cargar nada",
            "los tres capitulos con ¡JUGAR!, y respira el mas alto",
            "cada uno dice lo que trae (nada, 6 cartas con una dorada, 12 con dos)",
            "el atras de Android lo cierra",
            "con la 12, el 3 bloqueado (sin ¡JUGAR!, con candado y la oleada 20) y respira el 2",
            "¡JUGAR! del 2 carga las oleadas en la 11, con las tres elecciones de la preparacion",
            "con la 5, OLEADAS entra derecho a la 1",
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
