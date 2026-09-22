using UnityEditor;
using UnityEngine;

// Los botones de la prueba de red desde el editor (fase 0 de COOP.md). Se borra con el
// resto de Assets/PruebaRed cuando se decida el co-op.
//
// La PC hace de anfitrion desde el editor y el telefono se conecta como cliente: asi no
// hace falta cambiar el target a Windows, que reimporta el proyecto entero.
public static class PruebaRedEditor
{
    const string Clave = "PruebaRedModo";
    const string Escena = "Assets/PruebaRed/PruebaRed.unity";

    [MenuItem("ShowBies/Prueba de red/Ser anfitrion (la PC)")]
    public static void Anfitrion() { Arrancar("anfitrion"); }

    [MenuItem("ShowBies/Prueba de red/Solo, sin red (el piso)")]
    public static void Solo() { Arrancar("solo"); }

    [MenuItem("ShowBies/Prueba de red/Decime la IP de esta PC")]
    public static void Ip()
    {
        string salida = "IPs de esta maquina (probá la de 192.168.x.x):\n";
        foreach (var d in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
        {
            if (d.OperationalStatus != System.Net.NetworkInformation.OperationalStatus.Up) continue;
            foreach (var a in d.GetIPProperties().UnicastAddresses)
            {
                if (a.Address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork) continue;
                if (System.Net.IPAddress.IsLoopback(a.Address)) continue;
                salida += "  " + a.Address + "   (" + d.Name + ")\n";
            }
        }
        // Solo al log: un EditorUtility.DisplayDialog abre un modal que bloquea el editor
        // entero hasta que alguien lo cierra a mano. Las herramientas de esta sesion no
        // sacan carteles.
        Debug.Log("PRUEBARED " + salida);
    }

    [MenuItem("ShowBies/Prueba de red/Armar la APK de la prueba")]
    public static void Apk()
    {
        // Se construye SOLO la escena de la prueba, pasandola explicita: asi no hay que
        // tocar el Build Settings del juego, donde el orden esta hardcodeado en el codigo
        // y reordenarlo rompe la navegacion en silencio.
        string salida = System.IO.Path.GetFullPath(
            System.IO.Path.Combine(Application.dataPath, "..", "..", "Builds", "PruebaRed.apk"));
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(salida));

        string paqueteAntes = PlayerSettings.GetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android);
        string nombreAntes = PlayerSettings.productName;
        try
        {
            // Paquete propio: asi convive con el juego y con la APK de prueba del juego.
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android,
                                                    "com.ivanruiz.showbies.pruebared");
            PlayerSettings.productName = "PruebaRed";

            var opciones = new UnityEditor.BuildPlayerOptions
            {
                scenes = new[] { Escena },
                locationPathName = salida,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None
            };
            var informe = UnityEditor.BuildPipeline.BuildPlayer(opciones);
            Debug.Log("PRUEBARED build: " + informe.summary.result + "  ->  " + salida);
        }
        finally
        {
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, paqueteAntes);
            PlayerSettings.productName = nombreAntes;
            // Sin esto Unity no persiste la restauracion y ProjectSettings.asset queda con
            // el nombre de la prueba, que despues aparece sucio en git.
            AssetDatabase.SaveAssets();
            EditorApplication.ExecuteMenuItem("File/Save Project");
        }
    }

    static void Arrancar(string modo)
    {
        // Si ya estamos en play, NO se puede abrir la escena: EditorSceneManager.OpenScene
        // tira InvalidOperationException en play mode, y la excepcion se comia el
        // EnterPlaymode de abajo, asi que seguia corriendo la sesion anterior como si nada.
        // Estando en play la escena ya es la correcta: se le pide directo al componente.
        if (EditorApplication.isPlaying)
        {
            var vivo = Object.FindAnyObjectByType<PruebaRed>();
            if (vivo != null)
            {
                vivo.ArrancarDesdeElEditor(modo);
                Debug.Log("PRUEBARED arrancado en caliente: " + modo);
            }
            else
            {
                Debug.LogWarning("PRUEBARED no hay componente en esta escena; pará el play y probá de nuevo");
            }
            return;
        }

        SessionState.SetString(Clave, modo);
        SessionState.SetBool("PruebaRedFondo", PlayerSettings.runInBackground);
        SessionState.SetBool("PruebaRedTocado", true);
        // Si no, con la ventana de Unity detras el anfitrion deja de simular y el telefono
        // ve los bichos congelados.
        PlayerSettings.runInBackground = true;
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene(Escena);
        EditorApplication.EnterPlaymode();
    }

    [InitializeOnLoadMethod]
    static void Enganchar()
    {
        // Ya no arranca el modo: de eso se encarga PruebaRed.Start leyendo el SessionState,
        // porque este evento llega antes de que esta clase se suscriba. Aca solo se
        // devuelve runInBackground a como estaba.
        EditorApplication.playModeStateChanged += estado =>
        {
            if (estado == PlayModeStateChange.EnteredEditMode && SessionState.GetBool("PruebaRedTocado", false))
            {
                SessionState.SetBool("PruebaRedTocado", false);
                PlayerSettings.runInBackground = SessionState.GetBool("PruebaRedFondo", false);
            }
        };
    }
}
