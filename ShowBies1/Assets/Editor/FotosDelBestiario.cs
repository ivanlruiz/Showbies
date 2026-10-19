using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// La ventana del bestiario, fotografiada sin entrar en play, como las de Halloween (FotosDeHalloweenVentanas): las dos
// pestanas, con un progreso de prueba en el que hay estrellas para cobrar en las dos, cobradas, a medias y zombis que
// todavia no se mataron (tapados). En 16:9, 20:9 y 21:9, en Builds/bestiario/. Menu.unity se abre y se descarta sin
// guardar.
public static class FotosDelBestiario
{
    const string Carpeta = "../Builds/bestiario";

    // Muertes de cada tipo en el progreso de las fotos: el chaman, el blindado y el hinchado, sin ninguna.
    const string Matados = "[{\"id\":\"ZombiNormal\",\"cantidad\":620},{\"id\":\"ZombiRapido\",\"cantidad\":130},{\"id\":\"ZombiFASTER\",\"cantidad\":48},"
                         + "{\"id\":\"ZombiTanque\",\"cantidad\":75},{\"id\":\"ZombiBOSS\",\"cantidad\":3},{\"id\":\"ZombiEscupidor\",\"cantidad\":31},"
                         + "{\"id\":\"ZombiSaltador\",\"cantidad\":9}]";
    const string Cobradas = "[{\"id\":\"ZombiNormal\",\"cantidad\":1},{\"id\":\"ZombiRapido\",\"cantidad\":1},{\"id\":\"ZombiBOSS\",\"cantidad\":1}]";

    [MenuItem("ShowBies/Zombis/Fotos del bestiario")]
    public static void Sacar()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EscenasSinGuardar.Hay("Fotos del bestiario")) return;
        var abiertas = EscenasSinGuardar.Recordar();
        Directory.CreateDirectory(Path.GetFullPath(Carpeta));
        string carpetaProgreso = Path.Combine(Path.GetTempPath(), "ShowBiesFotosBestiario");
        if (Directory.Exists(carpetaProgreso)) Directory.Delete(carpetaProgreso, true);
        Directory.CreateDirectory(carpetaProgreso);
        File.WriteAllText(Path.Combine(carpetaProgreso, "progreso.json"),
                          "{\"version\":" + Progreso.VersionActual + ",\"monedas\":0,\"mejorOleada\":16,\"estadisticas\":{\"matados\":" + Matados
                          + "},\"estrellasCobradas\":" + Cobradas + "}");
        Idioma.UsarParaPruebas(Lengua.Espanol);
        Progreso.UsarCarpetaDePruebas(carpetaProgreso);
        try
        {
            foreach (var ancho in new[] { 1600, 2000, 2100 }) Fotos(ancho, 900);
        }
        finally
        {
            Progreso.UsarCarpetaDePruebas(null);
            Idioma.UsarParaPruebas(null);
            EscenasSinGuardar.Volver(abiertas);
        }
        Debug.Log("FotosDelBestiario: listas en " + Path.GetFullPath(Carpeta));
    }

    static void Fotos(int ancho, int alto)
    {
        EditorSceneManager.OpenScene("Assets/Escenas/Menu.unity", OpenSceneMode.Single);
        var ventana = Object.FindFirstObjectByType<VentanaBestiario>(FindObjectsInactive.Include);
        var selector = Object.FindFirstObjectByType<SelectorIdioma>(FindObjectsInactive.Include);
        if (ventana == null)
        {
            Debug.LogError("FotosDelBestiario: el menu no tiene VentanaBestiario");
            return;
        }
        Llamar(selector, "Awake");
        Llamar(ventana, "Start");

        var canvas = ventana.GetComponentInParent<Canvas>().rootCanvas;
        var camara = new GameObject("CamaraUI").AddComponent<Camera>();
        camara.clearFlags = CameraClearFlags.SolidColor;
        camara.backgroundColor = new Color(0.05f, 0.06f, 0.12f);
        camara.cullingMask = 1 << canvas.gameObject.layer;
        camara.transform.position = new Vector3(0f, -1000f, -10f);
        var rt = new RenderTexture(ancho, alto, 24) { hideFlags = HideFlags.HideAndDontSave };
        camara.targetTexture = rt;
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camara;
        canvas.planeDistance = 5f;
        string sufijo = "_" + ancho + "x" + alto;

        ventana.Abrir();
        for (int pestana = 0; pestana < 2; pestana++)
        {
            ventana.MostrarPestana(pestana);
            Llamar(ventana, "Update");   // las insignias de las pestanas
            Escalar(ventana);
            Foto(camara, rt, (pestana == 0 ? "bestiario_comunes" : "bestiario_especiales") + sufijo);
        }
        ventana.Cerrar();

        camara.targetTexture = null;
        rt.Release();
        Object.DestroyImmediate(rt);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }

    static void Llamar(Object objeto, string metodo)
    {
        if (objeto == null) return;
        var m = objeto.GetType().GetMethod(metodo, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (m != null) m.Invoke(objeto, null);
    }

    // La escala que la ventana tendria al terminar de entrar (en el juego la anima su Update).
    static void Escalar(VentanaBestiario ventana)
    {
        var campo = typeof(VentanaBestiario).GetField("ventana", BindingFlags.Instance | BindingFlags.NonPublic);
        var rt = campo != null ? campo.GetValue(ventana) as RectTransform : null;
        Canvas.ForceUpdateCanvases();
        if (rt != null) rt.localScale = Vector3.one * ventana.EscalaQueEntra();
    }

    static void Foto(Camera camara, RenderTexture rt, string nombre)
    {
        Canvas.ForceUpdateCanvases();
        foreach (var t in Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None)) t.ForceMeshUpdate();
        Canvas.ForceUpdateCanvases();
        camara.Render();
        camara.Render();
        var antes = RenderTexture.active;
        RenderTexture.active = rt;
        var foto = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        foto.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        foto.Apply();
        RenderTexture.active = antes;
        File.WriteAllBytes(Path.GetFullPath(Carpeta + "/" + nombre + ".png"), foto.EncodeToPNG());
        Object.DestroyImmediate(foto);
    }
}
