using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Graba al jefe haciendo sus patrones, para ver la pose de cada uno (agazaparse,
// embestir, tambalearse aturdido, arquearse al invocar) sin tener que llegar a la
// oleada 10 jugando. Saca un jefe al lado del jugador, lo deja actuar y escribe
// los PNG en Builds/jefe.
//
// El jugador no dispara: aca lo que hay que ver es el jefe, no matarlo.
//
// OJO: entrar y salir de play hace que Unity vuelva a serializar ProjectSettings.
// Al menos una vez, QualitySettings perdio el bloque m_PerPlatformDefaultQuality,
// que es el que pone Android en Medium (ver Rendimiento en movil). Despues de
// correr esto, mirar el git status de ProjectSettings/ y revertir lo que no se
// haya tocado a proposito.
[InitializeOnLoad]
public static class GrabarJefe
{
    const string Clave = "ShowBies.GrabarJefe";
    const string Carpeta = "../Builds/jefe";
    const string PrefabJefe = "Assets/Prefabs/Personajes/ZombiBOSS.prefab";
    const int Fps = 25;
    const float SegundosDeEspera = 2f;
    const int Frames = 300;                 // 12 s: entran la carga entera y una invocacion

    static int frame;
    static EnemyController jefeGrabado;

    static GrabarJefe()
    {
        EditorApplication.update += Tick;
    }

    [MenuItem("ShowBies/Pruebas/Grabar al jefe")]
    static void Arrancar()
    {
        PlayerSettings.runInBackground = true;
        string carpeta = Path.GetFullPath(Carpeta);
        if (Directory.Exists(carpeta)) Directory.Delete(carpeta, true);
        Directory.CreateDirectory(carpeta);

        EditorSceneManager.OpenScene("Assets/Escenas/WaveMode.unity");
        SessionState.SetBool(Clave, true);
        SessionState.SetBool(Clave + ".listo", false);
        SessionState.SetBool(Clave + ".jefe", false);
        EditorApplication.EnterPlaymode();
    }

    static void Tick()
    {
        if (!SessionState.GetBool(Clave, false)) return;
        if (!EditorApplication.isPlaying) return;

        if (!SessionState.GetBool(Clave + ".listo", false))
        {
            SessionState.SetBool(Clave + ".listo", true);
            Time.captureFramerate = Fps;
            frame = 0;
        }

        var jugador = PlayerHealth.instance;
        if (jugador == null) return;
        jugador.health = 80;                          // que aguante los doce segundos

        var control = jugador.GetComponent<PlayerController>();
        if (control != null) control.enabled = false;
        var joysticks = jugador.GetComponent<PlayerJS>();
        if (joysticks != null) joysticks.enabled = false;

        if (Time.timeSinceLevelLoad < SegundosDeEspera) return;

        if (!SessionState.GetBool(Clave + ".jefe", false))
        {
            SessionState.SetBool(Clave + ".jefe", true);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabJefe);
            if (prefab != null)
            {
                Vector3 donde = jugador.transform.position + new Vector3(0f, 0f, 7f);
                var jefe = EnemyController.Aparecer(prefab, donde);
                if (jefe != null)
                {
                    jefe.EsJefe = true;
                    jefe.multiplicadorVida = 20f;     // que no lo maten los otros zombis
                    jefe.multiplicadorDano = 0.2f;
                    jefeGrabado = jefe;
                }
            }
        }

        // La camara mira al jefe y no al jugador: embistiendo a 16 m/s se sale de
        // cuadro en medio segundo, que es justo lo que hay que ver.
        var seguidor = Object.FindFirstObjectByType<CamaraJugador>();
        if (seguidor != null) seguidor.enabled = false;
        var camara = Camera.main;
        if (camara != null)
        {
            Vector3 centro = jefeGrabado != null && jefeGrabado.isActiveAndEnabled
                ? Vector3.Lerp(jefeGrabado.transform.position, jugador.transform.position, 0.35f)
                : jugador.transform.position;
            camara.transform.SetPositionAndRotation(centro + new Vector3(0f, 10f, -8f),
                                                    Quaternion.Euler(52f, 0f, 0f));
        }

        ScreenCapture.CaptureScreenshot(Path.Combine(Path.GetFullPath(Carpeta), "f" + frame.ToString("0000") + ".png"));
        frame++;
        if (frame < Frames) return;

        Time.captureFramerate = 0;
        SessionState.SetBool(Clave, false);
        EditorApplication.ExitPlaymode();
        PlayerSettings.runInBackground = false;
        AssetDatabase.SaveAssets();
        Debug.Log("Grabados " + frame + " frames del jefe en " + Path.GetFullPath(Carpeta));
    }
}
