using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Graba al muñeco disparando, quieto y corriendo, con la camara mas cerca que la del
// juego: para ver la pistola, el brazo que apunta, el tiron de cada tiro y el cuerpo que
// mira hacia donde se apunta. Maneja los joysticks del telefono como PruebaDisparo y
// escribe los PNG en Builds/disparo_video.
//
// OJO: entrar y salir de play hace que Unity vuelva a serializar ProjectSettings (ver la
// trampa en CLAUDE.md): despues de correrlo, revertir lo que no se toco a proposito.
[InitializeOnLoad]
public static class GrabarDisparo
{
    const string Clave = "ShowBies.GrabarDisparo";
    const string Carpeta = "../Builds/disparo_video";
    const int Fps = 25;
    const float SegundosDeEspera = 2f;

    // Que se hace en cada tramo, en segundos desde que empieza a grabar: hacia donde
    // camina y hacia donde apunta (cero es soltar el joystick).
    static readonly (float hasta, Vector2 camina, Vector2 apunta)[] Guion =
    {
        (1.0f, Vector2.zero, Vector2.zero),                  // quieto
        (2.6f, Vector2.zero, new Vector2(1f, 0f)),           // quieto, disparando a la derecha
        (5.0f, new Vector2(0f, 1f), new Vector2(1f, 0f)),    // corre hacia arriba y dispara a la derecha
        (7.4f, new Vector2(-1f, 0f), new Vector2(-1f, 0f)),  // corre hacia la izquierda disparando al frente
        (9.0f, new Vector2(0f, -1f), Vector2.zero),          // corre sin disparar
        (10.0f, Vector2.zero, Vector2.zero),                 // quieto
    };

    static int cuadro, tramo = -1;

    static GrabarDisparo()
    {
        EditorApplication.update += Tick;
    }

    [MenuItem("ShowBies/Pruebas/Grabar el disparo (play)")]
    // Publico para poder correrlo por codigo (ver la trampa del registro de menus).
    public static void Arrancar()
    {
        if (SessionState.GetBool(Clave, false) || EditorApplication.isPlayingOrWillChangePlaymode) return;
        string carpeta = Path.GetFullPath(Carpeta);
        if (Directory.Exists(carpeta)) Directory.Delete(carpeta, true);
        Directory.CreateDirectory(carpeta);

        PruebaDisparo.ModoTelefono(Clave);
        PlayerSettings.runInBackground = true;
        EditorSceneManager.OpenScene("Assets/Escenas/WaveMode.unity");
        SessionState.SetBool(Clave, true);
        SessionState.SetBool(Clave + ".listo", false);
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
            cuadro = 0;
            tramo = -1;
        }

        var vida = PlayerHealth.instance;
        if (vida == null) return;
        vida.health = Mathf.Max(vida.health, 60);
        var control = vida.GetComponent<PlayerController>();
        var joysticks = vida.GetComponent<PlayerJS>();
        if (control == null || joysticks == null) return;
        control.cantBalas = Mathf.Max(control.cantBalas, 100);
        if (Time.timeSinceLevelLoad < SegundosDeEspera) return;

        float t = cuadro / (float)Fps;
        int ahora = 0;
        while (ahora < Guion.Length - 1 && t >= Guion[ahora].hasta) ahora++;
        if (ahora != tramo)
        {
            tramo = ahora;
            Mover(joysticks.moveJoystick, Guion[ahora].camina);
            Mover(joysticks.lookJoystick, Guion[ahora].apunta);
        }

        // Mas cerca que la camara del juego, siguiendo al muñeco.
        var seguidor = Object.FindFirstObjectByType<CamaraJugador>();
        if (seguidor != null) seguidor.enabled = false;
        var camara = Camera.main;
        if (camara != null)
        {
            Vector3 centro = vida.transform.position;
            camara.transform.SetPositionAndRotation(centro + new Vector3(0f, 4.2f, -3.4f), Quaternion.Euler(50f, 0f, 0f));
        }

        ScreenCapture.CaptureScreenshot(Path.Combine(Path.GetFullPath(Carpeta), "f" + cuadro.ToString("0000") + ".png"));
        cuadro++;
        if (t < Guion[Guion.Length - 1].hasta) return;

        Mover(joysticks.moveJoystick, Vector2.zero);
        Mover(joysticks.lookJoystick, Vector2.zero);
        Time.captureFramerate = 0;
        SessionState.SetBool(Clave, false);
        EditorApplication.ExitPlaymode();
        PlayerSettings.runInBackground = false;
        PruebaDisparo.DevolverElTeclado(Clave);
        AssetDatabase.SaveAssets();
        Debug.Log("Grabados " + cuadro + " cuadros del disparo en " + Path.GetFullPath(Carpeta));
    }

    static void Mover(Joystick joystick, Vector2 direccion)
    {
        if (direccion == Vector2.zero) PruebaDisparo.Soltar(joystick);
        else PruebaDisparo.Apretar(joystick, direccion);
    }
}
