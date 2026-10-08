using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Las ventanas del evento, fotografiadas sin entrar en play: el boton HALLOWEEN del menu y la
// ventana con la fila de hitos (con un hito cobrado, uno para cobrar y el resto por delante).
// OPCIONES no: arma su ventana destruyendo los botones del idioma, y sin play no se puede. Se
// arman como en el juego (InstaladorHalloween, su Start), con el progreso en una carpeta de
// pruebas y el evento forzado, y el
// canvas del menu se pasa un instante a una camara para poder sacarle la foto: la UI en overlay
// no pasa por ninguna. En 16:9 y en 20:9. Menu.unity se abre y se descarta sin guardar.
public static class FotosDeHalloweenVentanas
{
    const string Carpeta = "../Builds/halloween";

    [MenuItem("ShowBies/Halloween/Fotos de las ventanas")]
    public static void Sacar()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EscenasSinGuardar.Hay("Fotos de las ventanas de Halloween")) return;
        var abiertas = EscenasSinGuardar.Recordar();
        Directory.CreateDirectory(Path.GetFullPath(Carpeta));
        string carpetaProgreso = Path.Combine(Path.GetTempPath(), "ShowBiesFotosHalloween");
        if (Directory.Exists(carpetaProgreso)) Directory.Delete(carpetaProgreso, true);
        Directory.CreateDirectory(carpetaProgreso);
        File.WriteAllText(Path.Combine(carpetaProgreso, "progreso.json"), "{\"version\":" + Progreso.VersionActual + ",\"monedas\":0,\"mejorOleada\":12}");
        Idioma.UsarParaPruebas(Lengua.Espanol);
        Progreso.UsarCarpetaDePruebas(carpetaProgreso);
        EventoHalloween.UsarParaPruebas(20261030, 1);
        try
        {
            EventoHalloween.Asegurar();
            EventoHalloween.Sumar(EventoHalloween.Umbral(1) + 37);
            EventoHalloween.CobrarSiguiente();
            Progreso.Halloween.sombrero = true;
            foreach (var ancho in new[] { 1600, 2000 }) Fotos(ancho, 900);
        }
        finally
        {
            EventoHalloween.UsarParaPruebas(-1, -1);
            Progreso.UsarCarpetaDePruebas(null);
            Idioma.UsarParaPruebas(null);
            EscenasSinGuardar.Volver(abiertas);
        }
        Debug.Log("FotosDeHalloweenVentanas: listas en " + Path.GetFullPath(Carpeta));
    }

    static void Fotos(int ancho, int alto)
    {
        var escena = EditorSceneManager.OpenScene("Assets/Escenas/Menu.unity", OpenSceneMode.Single);
        var ventana = VentanaHalloween.Instalar(escena);
        var selector = Object.FindFirstObjectByType<SelectorIdioma>();
        if (ventana == null)
        {
            Debug.LogError("FotosDeHalloweenVentanas: no se pudo armar el menu");
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

        Foto(camara, rt, canvas, "menu_boton" + sufijo);

        ventana.Abrir();
        Escalar(ventana, "ventana", "EscalaQueEntra");
        Foto(camara, rt, canvas, "ventana_halloween" + sufijo);
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
    static void Escalar(Object objeto, string campo, string metodo)
    {
        var tipo = objeto.GetType();
        var rt = tipo.GetField(campo, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(objeto) as RectTransform;
        Canvas.ForceUpdateCanvases();
        float escala = (float)tipo.GetMethod(metodo, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(objeto, null);
        if (rt != null) rt.localScale = Vector3.one * escala;
    }

    static void Foto(Camera camara, RenderTexture rt, Canvas canvas, string nombre)
    {
        Canvas.ForceUpdateCanvases();
        foreach (var t in canvas.GetComponentsInChildren<TMPro.TMP_Text>(true)) t.ForceMeshUpdate();
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
