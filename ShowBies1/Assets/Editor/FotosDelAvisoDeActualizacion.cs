using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// El aviso de la version nueva (AvisoDeActualizacion), fotografiado sin entrar en play sobre el
// menu, con sus tres caras: disponible, bajando (al 45 %) y lista. Se arma como en el juego (los
// Start del menu y el Armar del aviso) y los canvas se pasan un instante a una camara para poder
// sacarles la foto: la UI en overlay no pasa por ninguna. En 16:9, 20:9 y 21:9, para ver que entre
// entre los botones redondos de las esquinas y MEJORAS. Menu.unity se abre y se descarta sin
// guardar. Las fotos van a Builds/actualizacion.
public static class FotosDelAvisoDeActualizacion
{
    const string Carpeta = "../Builds/actualizacion";

    [MenuItem("ShowBies/Actualización/Fotos del aviso")]
    public static void Sacar()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EscenasSinGuardar.Hay("Fotos del aviso de actualización")) return;
        var abiertas = EscenasSinGuardar.Recordar();
        Directory.CreateDirectory(Path.GetFullPath(Carpeta));
        string carpetaProgreso = Path.Combine(Path.GetTempPath(), "ShowBiesFotosActualizacion");
        if (Directory.Exists(carpetaProgreso)) Directory.Delete(carpetaProgreso, true);
        Directory.CreateDirectory(carpetaProgreso);
        File.WriteAllText(Path.Combine(carpetaProgreso, "progreso.json"), "{\"version\":" + Progreso.VersionActual + ",\"monedas\":0,\"mejorOleada\":12}");
        Idioma.UsarParaPruebas(Lengua.Espanol);
        Progreso.UsarCarpetaDePruebas(carpetaProgreso);
        try
        {
            foreach (var ancho in new[] { 1920, 2400, 2520 }) Fotos(ancho, 1080);
        }
        finally
        {
            Progreso.UsarCarpetaDePruebas(null);
            Idioma.UsarParaPruebas(null);
            EscenasSinGuardar.Volver(abiertas);
        }
        Debug.Log("FotosDelAvisoDeActualizacion: listas en " + Path.GetFullPath(Carpeta));
    }

    static void Fotos(int ancho, int alto)
    {
        var escena = EditorSceneManager.OpenScene("Assets/Escenas/Menu.unity", OpenSceneMode.Single);
        // Los botones de las esquinas los arman en codigo estas ventanas, en su Awake o Start.
        foreach (var tipo in new[] { typeof(SelectorIdioma), typeof(VentanaMisiones), typeof(VentanaBestiario),
                                     typeof(VentanaLogros), typeof(BotonDiscord) })
        {
            var componente = Object.FindFirstObjectByType(tipo, FindObjectsInactive.Include);
            Llamar(componente, "Awake");
            Llamar(componente, "Start");
        }

        var go = new GameObject("AvisoDeActualizacion");
        SceneManager_Mover(go, escena);
        var aviso = go.AddComponent<AvisoDeActualizacion>();
        Llamar(aviso, "Start");

        var camara = new GameObject("CamaraUI").AddComponent<Camera>();
        camara.clearFlags = CameraClearFlags.SolidColor;
        camara.backgroundColor = new Color(0.05f, 0.06f, 0.12f);
        camara.transform.position = new Vector3(0f, -1000f, -10f);
        var rt = new RenderTexture(ancho, alto, 24) { hideFlags = HideFlags.HideAndDontSave };
        camara.targetTexture = rt;
        string sufijo = "_" + ancho + "x" + alto;

        var tipoVista = typeof(AvisoDeActualizacion).GetNestedType("Vista", BindingFlags.NonPublic);
        var armar = typeof(AvisoDeActualizacion).GetMethod("Armar", BindingFlags.Instance | BindingFlags.NonPublic);
        var quitar = typeof(AvisoDeActualizacion).GetMethod("Quitar", BindingFlags.Instance | BindingFlags.NonPublic);
        var campoCartel = typeof(AvisoDeActualizacion).GetField("cartel", BindingFlags.Instance | BindingFlags.NonPublic);
        var campoAvance = typeof(ActualizacionDePlay).GetField("avance", BindingFlags.Static | BindingFlags.NonPublic);
        // Sin play no hay sonidos: se le dice que ya sono esa cara.
        var campoSono = typeof(AvisoDeActualizacion).GetField("yaSono", BindingFlags.Instance | BindingFlags.NonPublic);
        foreach (string cara in new[] { "Disponible", "Descargando", "Lista" })
        {
            object vista = Enum.Parse(tipoVista, cara);
            if (campoAvance != null) campoAvance.SetValue(null, cara == "Descargando" ? 0.45f : 0f);
            if (campoSono != null) campoSono.SetValue(aviso, vista);
            armar.Invoke(aviso, new[] { vista });
            var cartel = (RectTransform)campoCartel.GetValue(aviso);
            if (cartel != null) cartel.localScale = Vector3.one;
            Foto(camara, rt, "aviso_" + cara.ToLowerInvariant() + sufijo);
            // Sin play, Destroy no anda: el lienzo se va en el acto y Quitar ya no lo encuentra.
            var lienzo = go.transform.Find("Lienzo");
            if (lienzo != null) Object.DestroyImmediate(lienzo.gameObject);
            quitar.Invoke(aviso, null);
        }
        if (campoAvance != null) campoAvance.SetValue(null, 0f);

        camara.targetTexture = null;
        rt.Release();
        Object.DestroyImmediate(rt);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }

    static void SceneManager_Mover(GameObject go, UnityEngine.SceneManagement.Scene escena)
    {
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, escena);
    }

    static void Llamar(Object objeto, string metodo)
    {
        if (objeto == null) return;
        var m = objeto.GetType().GetMethod(metodo, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (m == null) return;
        try
        {
            m.Invoke(objeto, null);
        }
        catch (Exception e)
        {
            Debug.LogWarning("FotosDelAvisoDeActualizacion: " + objeto.GetType().Name + "." + metodo + ": " + e.InnerException?.Message);
        }
    }

    // Todos los canvas raiz a la camara, cada uno con su orden, y la foto.
    static void Foto(Camera camara, RenderTexture rt, string nombre)
    {
        var raices = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var canvas in raices)
        {
            if (!canvas.isRootCanvas) continue;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camara;
            canvas.planeDistance = 5f;
            var escala = canvas.GetComponent<CanvasScaler>();
            if (escala != null) Llamar(escala, "Handle");
        }
        camara.cullingMask = ~0;
        Canvas.ForceUpdateCanvases();
        foreach (var t in Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)) t.ForceMeshUpdate();
        // El icono de cada boton lo pega a su texto IconoDeBoton, en su LateUpdate.
        foreach (var icono in Object.FindObjectsByType<IconoDeBoton>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            Llamar(icono, "Acomodar");
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
