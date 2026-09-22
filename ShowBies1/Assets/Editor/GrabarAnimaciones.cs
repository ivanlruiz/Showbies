using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Graba unos segundos de WaveMode en PNG numerados, para armar un gif con ffmpeg y
// ver las animaciones nuevas (pegar y morir) sin mirar la pantalla en vivo. Mata un
// zombi cada tanto para que en el clip haya muertes, y deja que los que llegan
// peguen solos.
//
// Con Time.captureFramerate el juego avanza a pasos fijos, asi que la grabacion sale
// pareja aunque el editor vaya a tirones. Y con runInBackground apagado no se
// renderiza ningun frame con la ventana atras: sin prenderlo, CaptureScreenshot no
// escribe nada y parece que todo esta roto.
//
// OJO: entrar y salir de play hace que Unity vuelva a serializar ProjectSettings.
// Al menos una vez, QualitySettings perdio el bloque m_PerPlatformDefaultQuality,
// que es el que pone Android en Medium (ver Rendimiento en movil). Despues de
// correr esto, mirar el git status de ProjectSettings/ y revertir lo que no se
// haya tocado a proposito.
[InitializeOnLoad]
public static class GrabarAnimaciones
{
    const string Clave = "ShowBies.GrabarAnimaciones";
    const string Carpeta = "../Builds/animaciones";
    const int Fps = 25;
    const float SegundosDeEspera = 9f;      // que los zombis lleguen al jugador
    const int Frames = 200;                 // 8 s
    const float RafagaDispara = 1.2f, RafagaDescansa = 1.3f;

    static int frame;

    static GrabarAnimaciones()
    {
        EditorApplication.update += Tick;
    }

    [MenuItem("ShowBies/Pruebas/Grabar las animaciones")]
    // Publico para poder correrlo por codigo y no solo por el menu: despues de
    // una sesion de play el registro de menus de Unity tarda en rehacerse y la
    // entrada no se encuentra, aunque la clase este cargada.
    public static void Arrancar()
    {
        PlayerSettings.runInBackground = true;
        string carpeta = Path.GetFullPath(Carpeta);
        if (Directory.Exists(carpeta)) Directory.Delete(carpeta, true);
        Directory.CreateDirectory(carpeta);

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
            frame = 0;
        }

        var jugador = PlayerHealth.instance;
        if (jugador != null && jugador.health < 40) jugador.health = 80;

        if (Time.timeSinceLevelLoad < SegundosDeEspera) return;

        // Dispara de verdad al mas cercano, en vez de matarlos a mano: los numeros
        // de danio, las monedas y el combo que salen en el clip son los del juego.
        var control = jugador != null ? jugador.GetComponent<PlayerController>() : null;
        if (control != null && control.theGun != null)
        {
            // El control del jugador se apaga: si no, su Update (o PlayerJS en
            // movil) pone isFiring en false cada frame antes de que el arma lo
            // mire, y desde aca nunca llega a disparar.
            control.enabled = false;
            var joysticks = jugador.GetComponent<PlayerJS>();
            if (joysticks != null) joysticks.enabled = false;
            control.cantBalas = 9999;

            // La camara del juego mira de muy arriba: a esa distancia un zombi son
            // veinte pixeles y no se le ve el brazo. Para el clip se la maneja desde
            // aca, cerca y un poco de costado.
            //
            // Se apaga el seguidor (que puede estar en un objeto padre y no en la
            // camara) y se mueve la camara de verdad: moviendo el padre, la camara
            // se queda a su distancia y la escena se ve igual de lejos, que es lo
            // que pasaba antes.
            var seguidor = Object.FindFirstObjectByType<CamaraJugador>();
            if (seguidor != null) seguidor.enabled = false;
            var camara = Camera.main;
            if (camara != null)
            {
                Vector3 donde = jugador.transform.position + new Vector3(0f, 7.5f, -5.5f);
                camara.transform.SetPositionAndRotation(donde, Quaternion.Euler(54f, 0f, 0f));
            }

            EnemyController elegido = null;
            float mejor = float.MaxValue;
            foreach (var z in Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None))
            {
                if (z == null || !z.isActiveAndEnabled || !z.Vivo) continue;
                float d = (z.transform.position - jugador.transform.position).sqrMagnitude;
                if (d < mejor) { mejor = d; elegido = z; }
            }
            if (elegido != null)
            {
                Vector3 hacia = elegido.transform.position - jugador.transform.position;
                hacia.y = 0f;
                if (hacia.sqrMagnitude > 0.001f) jugador.transform.rotation = Quaternion.LookRotation(hacia);
            }
            // A rafagas: disparando sin parar los mata a diez metros y nunca se ve
            // uno pegando.
            float ciclo = Mathf.Repeat(Time.timeSinceLevelLoad - SegundosDeEspera, RafagaDispara + RafagaDescansa);
            control.theGun.isFiring = elegido != null && ciclo < RafagaDispara;
        }

        ScreenCapture.CaptureScreenshot(Path.Combine(Path.GetFullPath(Carpeta), "f" + frame.ToString("0000") + ".png"));
        frame++;
        if (frame < Frames) return;

        Time.captureFramerate = 0;
        SessionState.SetBool(Clave, false);
        EditorApplication.ExitPlaymode();
        PlayerSettings.runInBackground = false;
        AssetDatabase.SaveAssets();
        Debug.Log("Grabados " + frame + " frames en " + Path.GetFullPath(Carpeta));
    }
}
