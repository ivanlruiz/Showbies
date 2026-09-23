using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Banco de la muerte animada. Matar dejo de ser "apagar el objeto": ahora el zombi
// sale de la cuenta en el acto y el GameObject se queda prendido mientras se
// desploma. Eso toca las dos cosas mas faciles de romper en silencio de todo el
// juego: el contador ZombisVivos (si queda alto, el generador queda tapado para
// siempre y la partida se vacia) y la cuenta de muertos de la oleada (si no baja,
// la oleada no termina nunca).
//
// Entra en play en WaveMode y mata un zombi cada tanto llamando a DanoZombi, que
// es el mismo camino que una bala. Cada frame compara ZombisVivos contra los que
// de verdad estan vivos. Escribe Builds/prueba_muerte.txt.
//
// OJO: entrar y salir de play hace que Unity vuelva a serializar ProjectSettings.
// Al menos una vez, QualitySettings perdio el bloque m_PerPlatformDefaultQuality,
// que es el que pone Android en Medium (ver Rendimiento en movil). Despues de
// correr esto, mirar el git status de ProjectSettings/ y revertir lo que no se
// haya tocado a proposito.
[InitializeOnLoad]
public static class PruebaMuerteAnimada
{
    const string Clave = "ShowBies.PruebaMuerteAnimada";
    const string Ruta = "../Builds/prueba_muerte.txt";
    const float Segundos = 60f;
    const float SegundosSinMatar = 5f;      // al final, para ver que los cadaveres se van
    const float EntreMuertes = 0.35f;

    static readonly int IdMorir = Animator.StringToHash("Morir");

    static int muertos, framesMuriendo, desajusteMaximo, cadaveresMaximos;
    static int oleadaAlEmpezar, oleadaAlTerminar;
    static int cadaveresAlFinal = -1;
    static double proximaMuerte;

    static PruebaMuerteAnimada()
    {
        EditorApplication.update += Tick;
    }

    [MenuItem("ShowBies/Pruebas/Muerte animada (play)")]
    // Publico para poder correrlo por codigo: despues de una sesion de play el
    // registro de menus tarda en rehacerse (ver la trampa en CLAUDE.md).
    public static void Arrancar()
    {
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
            muertos = framesMuriendo = desajusteMaximo = cadaveresMaximos = 0;
            cadaveresAlFinal = -1;
            oleadaAlEmpezar = OleadaDeAhora();
            proximaMuerte = 0;
            return;
        }

        double corrido = ahora - desde;
        Muestrear(ahora, corrido < Segundos - SegundosSinMatar);

        if (corrido < Segundos) return;
        Terminar();
    }

    static void Muestrear(double ahora, bool matando)
    {
        var jugador = PlayerHealth.instance;
        if (jugador != null && jugador.health < 40) jugador.health = 80;   // que llegue al final

        var todos = Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
        int vivosDeVerdad = 0, cadaveres = 0;
        EnemyController paraMatar = null;

        foreach (var zombi in todos)
        {
            if (zombi == null || !zombi.isActiveAndEnabled) continue;

            // SigueVivo mira enUso: un cadaver ya dijo que no.
            if (EnemyController.SigueVivo(zombi, zombi.NumeroDeAparicion))
            {
                vivosDeVerdad++;
                if (!zombi.EsJefe) paraMatar = zombi;       // el jefe no, que la oleada lo espera
                continue;
            }

            cadaveres++;
            foreach (var animador in zombi.GetComponentsInChildren<Animator>(true))
            {
                if (animador.runtimeAnimatorController == null) continue;
                if (animador.GetCurrentAnimatorStateInfo(0).shortNameHash == IdMorir) framesMuriendo++;
            }
        }

        desajusteMaximo = Mathf.Max(desajusteMaximo, Mathf.Abs(EnemyController.ZombisVivos - vivosDeVerdad));
        cadaveresMaximos = Mathf.Max(cadaveresMaximos, cadaveres);
        if (!matando) cadaveresAlFinal = cadaveres;

        if (matando && paraMatar != null && ahora >= proximaMuerte)
        {
            proximaMuerte = ahora + EntreMuertes;
            paraMatar.DanoZombi(99999f);      // el mismo camino que una bala
            muertos++;
        }
    }

    static int OleadaDeAhora()
    {
        var wm = Object.FindFirstObjectByType<WaveManager>();
        return wm != null ? wm.OleadaActual : -1;
    }

    static void Terminar()
    {
        oleadaAlTerminar = OleadaDeAhora();

        var inf = new StringBuilder();
        inf.AppendLine("Prueba de la muerte animada (" + Segundos + " s en WaveMode)");
        inf.AppendLine();
        inf.AppendLine("Zombis matados a mano: " + muertos);
        inf.AppendLine("Frames vistos en el estado Morir: " + framesMuriendo);
        inf.AppendLine("Cadaveres a la vez, maximo: " + cadaveresMaximos);
        inf.AppendLine("Cadaveres tras " + SegundosSinMatar + " s sin matar: " + cadaveresAlFinal);
        inf.AppendLine("Desajuste maximo entre ZombisVivos y los vivos de verdad: " + desajusteMaximo);
        inf.AppendLine("Oleada: " + oleadaAlEmpezar + " -> " + oleadaAlTerminar);
        inf.AppendLine();

        bool murieron = muertos >= 20;
        bool seVio = framesMuriendo > 0;
        bool cuentaSana = desajusteMaximo == 0;
        bool seVan = cadaveresAlFinal == 0;
        bool avanzo = oleadaAlTerminar > oleadaAlEmpezar;

        inf.AppendLine(murieron ? "OK  murieron suficientes zombis para medir" : "FALLA  casi no murio nadie: el banco no midio");
        inf.AppendLine(seVio ? "OK  el estado Morir se reproduce" : "FALLA  nunca entro en Morir");
        inf.AppendLine(cuentaSana ? "OK  ZombisVivos cuenta exactamente los vivos (el cadaver no cuenta)"
                                  : "FALLA  ZombisVivos se despego " + desajusteMaximo + ": el generador queda tapado");
        inf.AppendLine(seVan ? "OK  los cadaveres se van solos" : "FALLA  quedaron " + cadaveresAlFinal + " cadaveres colgados");
        inf.AppendLine(avanzo ? "OK  la oleada sigue terminando y avanzando" : "FALLA  la oleada no avanzo: los muertos no se cuentan");
        inf.AppendLine();
        inf.AppendLine("RESULTADO: " + (murieron && seVio && cuentaSana && seVan && avanzo ? "TODO OK" : "HAY FALLAS"));

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(Ruta)));
        File.WriteAllText(Ruta, inf.ToString());
        Debug.Log(inf.ToString());

        SessionState.SetBool(Clave, false);
        EditorApplication.ExitPlaymode();
        PlayerSettings.runInBackground = false;
        AssetDatabase.SaveAssets();
    }
}
