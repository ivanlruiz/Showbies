using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Banco para ver, sin mirar la pantalla, que los zombis pegan con la animacion de
// pegar. Entra en play en WaveMode, deja que los zombis lleguen solos al jugador
// (que no se mueve ni dispara) y cuenta, frame por frame, en que estado esta el
// Animator de cada zombi que lo esta tocando. Escribe Builds/prueba_golpe.txt.
//
// Va con [InitializeOnLoad] y SessionState, y no con un playModeStateChanged
// suscrito desde el menu, porque entrar en play recarga el dominio: lo que se
// suscribe antes no sobrevive, y ya paso de creer que el arreglo fallaba cuando
// lo que fallaba era el banco.
//
// OJO: entrar y salir de play hace que Unity vuelva a serializar ProjectSettings.
// Al menos una vez, QualitySettings perdio el bloque m_PerPlatformDefaultQuality,
// que es el que pone Android en Medium (ver Rendimiento en movil). Despues de
// correr esto, mirar el git status de ProjectSettings/ y revertir lo que no se
// haya tocado a proposito.
[InitializeOnLoad]
public static class PruebaGolpeAnimado
{
    const string Clave = "ShowBies.PruebaGolpeAnimado";
    const string Ruta = "../Builds/prueba_golpe.txt";
    const float Segundos = 45f;          // lo que tarda un zombi en llegar y pegar varias veces
    // Lo que dura la ventana en la que se mira el estado despues de un golpe. El
    // clip de atacar dura 1,33 s y el intervalo entre golpes es 0,8: en medio
    // segundo tiene que estar atacando, sin margen para discutir.
    const float VentanaDelGolpe = 0.5f;

    static readonly int IdCorrer = Animator.StringToHash("Correr");
    static readonly int IdAtacar = Animator.StringToHash("Atacar");
    static readonly int IdMorir = Animator.StringToHash("Morir");

    static int framesCorriendo, framesAtacando, framesMuriendo, framesEnOtra;
    static int golpesVistos, zombisQuePegaron;
    static int vidaMinima = int.MaxValue;

    // Por zombi: cuantos golpes llevaba dados la ultima vez que se lo miro, y
    // cuando pego por ultima vez. Se mide por zombi y no por distancia porque en
    // una horda hay zombis a un metro y medio que todavia no llegaron a tocar a
    // nadie, y corriendo estan bien.
    static readonly Dictionary<EnemyController, int> golpesDeCadaUno = new Dictionary<EnemyController, int>();
    static readonly Dictionary<EnemyController, double> ultimoGolpeDe = new Dictionary<EnemyController, double>();

    static PruebaGolpeAnimado()
    {
        EditorApplication.update += Tick;
    }

    [MenuItem("ShowBies/Pruebas/Golpe animado (play)")]
    static void Arrancar()
    {
        // Sin esto, con la ventana de Unity atras el juego en play no corre y el
        // banco parece andar mientras no pasa nada.
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

        float desde = SessionState.GetFloat(Clave + ".desde", -1f);
        if (desde < 0f)
        {
            SessionState.SetFloat(Clave + ".desde", (float)EditorApplication.timeSinceStartup);
            framesCorriendo = framesAtacando = framesMuriendo = framesEnOtra = 0;
            golpesVistos = zombisQuePegaron = 0;
            vidaMinima = int.MaxValue;
            golpesDeCadaUno.Clear();
            ultimoGolpeDe.Clear();
            return;
        }

        Muestrear();

        if (EditorApplication.timeSinceStartup - desde < Segundos) return;
        Terminar();
    }

    static void Muestrear()
    {
        var jugador = PlayerHealth.instance;
        if (jugador == null) return;                 // la escena cambio: lo dice el informe
        vidaMinima = Mathf.Min(vidaMinima, jugador.health);
        // Que aguante los 45 s: sin esto muere a la mitad y la muestra se corta.
        if (jugador.health < 30) jugador.health = 80;

        double ahora = EditorApplication.timeSinceStartup;

        foreach (var zombi in Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None))
        {
            if (zombi == null || !zombi.isActiveAndEnabled) continue;
            golpesVistos = Mathf.Max(golpesVistos, zombi.GolpesDados);

            int antes;
            if (golpesDeCadaUno.TryGetValue(zombi, out antes) && zombi.GolpesDados > antes)
            {
                if (!ultimoGolpeDe.ContainsKey(zombi)) zombisQuePegaron++;
                ultimoGolpeDe[zombi] = ahora;
            }
            golpesDeCadaUno[zombi] = zombi.GolpesDados;

            double cuando;
            if (!ultimoGolpeDe.TryGetValue(zombi, out cuando)) continue;
            if (ahora - cuando > VentanaDelGolpe) continue;

            foreach (var animador in zombi.GetComponentsInChildren<Animator>(true))
            {
                if (animador.runtimeAnimatorController == null) continue;
                int estado = animador.GetCurrentAnimatorStateInfo(0).shortNameHash;
                if (estado == IdAtacar) framesAtacando++;
                else if (estado == IdCorrer) framesCorriendo++;
                else if (estado == IdMorir) framesMuriendo++;
                else framesEnOtra++;
            }
        }
    }

    static void Terminar()
    {
        var inf = new StringBuilder();
        inf.AppendLine("Prueba del golpe animado (" + Segundos + " s en WaveMode, sin tocar nada)");
        inf.AppendLine();
        inf.AppendLine("Golpes que dio el zombi que mas pego: " + golpesVistos);
        inf.AppendLine("Zombis distintos que llegaron a pegar: " + zombisQuePegaron);
        inf.AppendLine("Vida mas baja del jugador: " + (vidaMinima == int.MaxValue ? "no se midio" : vidaMinima.ToString()));
        inf.AppendLine();
        inf.AppendLine("Frames en el medio segundo POSTERIOR a cada golpe, por estado:");
        inf.AppendLine("  Atacar  " + framesAtacando);
        inf.AppendLine("  Correr  " + framesCorriendo);
        inf.AppendLine("  Morir   " + framesMuriendo);
        inf.AppendLine("  otro    " + framesEnOtra);
        inf.AppendLine();

        int total = framesAtacando + framesCorriendo + framesMuriendo + framesEnOtra;
        bool pego = golpesVistos > 0 && zombisQuePegaron > 0;
        bool ataco = framesAtacando > 0;
        // El arranque de la transicion (0,06 s) y los que mueren de un tiro justo
        // despues de pegar caen fuera, asi que no se pide el 100 %.
        bool casiSiempre = total > 0 && framesAtacando >= total * 0.9f;
        inf.AppendLine(pego ? "OK  los zombis llegaron y pegaron" : "FALLA  ningun zombi llego a pegar: el banco no midio nada");
        inf.AppendLine(ataco ? "OK  el estado Atacar se reproduce" : "FALLA  nunca entro en Atacar");
        inf.AppendLine(casiSiempre ? "OK  despues de pegar esta atacando (" + (total == 0 ? 0 : framesAtacando * 100 / total) + " % de los frames)"
                                   : "FALLA  despues de pegar no esta atacando (" + (total == 0 ? 0 : framesAtacando * 100 / total) + " % de los frames)");
        inf.AppendLine();
        inf.AppendLine("RESULTADO: " + (pego && ataco && casiSiempre ? "TODO OK" : "HAY FALLAS"));

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(Ruta)));
        File.WriteAllText(Ruta, inf.ToString());
        Debug.Log(inf.ToString());

        SessionState.SetBool(Clave, false);
        EditorApplication.ExitPlaymode();
        PlayerSettings.runInBackground = false;
        AssetDatabase.SaveAssets();
    }
}
