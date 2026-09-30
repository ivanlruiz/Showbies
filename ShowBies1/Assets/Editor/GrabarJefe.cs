using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Graba al jefe haciendo sus patrones, para ver la pose de cada uno (agazaparse,
// embestir, tambalearse aturdido, arquearse al invocar) sin tener que llegar a la
// oleada 10 jugando. Saca un jefe cerca del jugador, lo deja actuar y escribe
// los PNG en Builds/jefe.
//
// El jugador no dispara: aca lo que hay que ver es el jefe, no matarlo. Y queda fijo
// (kinematic): con el control apagado nadie le pisa la velocidad, y hasta el 27/9 el
// choque de la embestida lo mandaba deslizando fuera de cuadro; con el jugador lejos,
// el jefe quedaba fuera de pantalla y no volvia a atacar, y la invocacion no se grababa.
// La oleada se para y se despeja: los zombis de la oleada le pegaban fuera de cuadro y
// la pantalla titilaba de rojo.
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
    // 16 s: la espera del primer ataque (3 s), la carga entera con su aturdimiento, los
    // 5 s hasta el ataque siguiente y la invocacion.
    const int Frames = 400;
    // A 13 m y no a 7: dentro de los 15 m a los que ataca, pero lejos para que llegue
    // caminando cuando ya puede atacar y la carga se vea entera (a 7 m llegaba antes,
    // le tiraba un zarpazo y la embestida duraba cuatro cuadros).
    const float DistanciaDelJefe = 13f;

    static int frame;
    static EnemyController jefeGrabado;

    static GrabarJefe()
    {
        EditorApplication.update += Tick;
    }

    [MenuItem("ShowBies/Pruebas/Grabar al jefe")]
    // Publico para poder correrlo por codigo y no solo por el menu: despues de
    // una sesion de play el registro de menus de Unity tarda en rehacerse y la
    // entrada no se encuentra, aunque la clase este cargada.
    public static void Arrancar()
    {
        if (!RespaldoDelBanco.PuedeArrancar("GrabarJefe")) return;
        // El progreso y los PlayerPrefs del editor vuelven a como estaban al volver a modo
        // edicion (jugar los cambia: una partida mas, la oleada en curso, el record).
        RespaldoDelBanco.Guardar("GrabarJefe");
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
        if (!RespaldoDelBanco.SigueArmado("GrabarJefe", Clave)) return;

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
        var cuerpo = jugador.GetComponent<Rigidbody>();
        if (cuerpo != null && !cuerpo.isKinematic)
        {
            cuerpo.linearVelocity = Vector3.zero;
            cuerpo.isKinematic = true;
        }
        var oleadas = Object.FindFirstObjectByType<WaveManager>();
        if (oleadas != null && oleadas.enabled)
        {
            oleadas.StopAllCoroutines();
            oleadas.enabled = false;
            // El cartel de la oleada lo esconde la corrutina que se acaba de cortar: si no,
            // queda fijo en el medio, encima del jefe.
            if (oleadas.cartelOleada != null) oleadas.cartelOleada.gameObject.SetActive(false);
        }

        if (Time.timeSinceLevelLoad < SegundosDeEspera) return;

        if (!SessionState.GetBool(Clave + ".jefe", false))
        {
            SessionState.SetBool(Clave + ".jefe", true);
            // Sin los zombis que alcanzo a sacar la oleada (el jefe no se despeja).
            EnemyController.DespejarAlrededor(jugador.transform.position, 1000f);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabJefe);
            if (prefab != null)
            {
                Vector3 donde = jugador.transform.position + new Vector3(0f, 0f, DistanciaDelJefe);
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
            // Entre los dos y un poco mas lejos que antes, para que entren los dos y la
            // linea de la carga: el jefe tiene que quedar en pantalla para atacar.
            Vector3 centro = jefeGrabado != null && jefeGrabado.isActiveAndEnabled
                ? Vector3.Lerp(jefeGrabado.transform.position, jugador.transform.position, 0.5f)
                : jugador.transform.position;
            camara.transform.SetPositionAndRotation(centro + new Vector3(0f, 13f, -10.4f),
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
