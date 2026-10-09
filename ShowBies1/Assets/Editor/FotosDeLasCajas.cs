using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Las tres cajas (los power-ups) de noche, en cada capitulo, con la calidad del telefono y la
// camara del juego (FotosDeLaHorda.ArmarNoche), tal como las dejo ConstructorPowerUps en los
// prefabs. Con esta herramienta salieron las maquetas entre las que Ivan eligio los dibujos (9/10).
// Sin play y sin tocar la escena abierta. Escribe Builds/cajas/<capitulo>.png.
public static class FotosDeLasCajas
{
    const string Escena = "Assets/Escenas/WaveMode.unity";
    const string Carpeta = "../Builds/cajas";
    const int Ancho = 1920, Alto = 1080;

    [MenuItem("ShowBies/Power-ups/Fotos de las cajas")]
    public static void Sacar()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogError("FotosDeLasCajas: no en play"); return; }
        Scene waveMode = SceneManager.GetSceneByPath(Escena);
        bool abriYo = false;
        if (!waveMode.isLoaded)
        {
            waveMode = EditorSceneManager.OpenScene(Escena, OpenSceneMode.Additive);
            abriYo = true;
        }
        int calidadDelEditor = QualitySettings.GetQualityLevel();
        var rt = new RenderTexture(Ancho, Alto, 24);
        try
        {
            CapitulosDeEscenario capitulos = null;
            Camera camaraDelJuego = null;
            Light relleno = null;
            foreach (var raiz in waveMode.GetRootGameObjects())
            {
                if (capitulos == null) capitulos = raiz.GetComponentInChildren<CapitulosDeEscenario>(true);
                foreach (var c in raiz.GetComponentsInChildren<Camera>(true))
                    if (c.CompareTag("MainCamera")) camaraDelJuego = c;
                if (raiz.name == "Relleno") relleno = raiz.GetComponent<Light>();
            }
            if (capitulos == null || camaraDelJuego == null || relleno == null) { Debug.LogError("FotosDeLasCajas: a WaveMode le falta algo"); return; }
            int telefono = CalidadDeAndroid.Guardada() >= 0 ? CalidadDeAndroid.Guardada() : CalidadDeAndroid.Medium;
            QualitySettings.SetQualityLevel(telefono, false);
            Directory.CreateDirectory(Path.GetFullPath(Carpeta));
            for (int i = 0; i < capitulos.escenarios.Length; i++)
            {
                var escenario = capitulos.escenarios[i];
                if (escenario.decorado == null) continue;
                if (i == 0) escenario = FotosDeLaHorda.LoQueTraeLaEscena(escenario, capitulos, camaraDelJuego, waveMode);
                Fotografiar(escenario, camaraDelJuego, relleno, rt);
            }
        }
        finally
        {
            QualitySettings.SetQualityLevel(calidadDelEditor, false);
            rt.Release();
            Object.DestroyImmediate(rt);
            if (abriYo) EditorSceneManager.CloseScene(waveMode, true);
        }
        Debug.Log("FotosDeLasCajas: listas en " + Path.GetFullPath(Carpeta));
    }

    static void Fotografiar(EscenarioDeCapitulo escenario, Camera camaraDelJuego, Light rellenoDeLaEscena, RenderTexture rt)
    {
        var escena = EditorSceneManager.NewPreviewScene();
        try
        {
            Light relleno;
            var camara = FotosDeLaHorda.ArmarNoche(escena, escenario, camaraDelJuego, rellenoDeLaEscena, rt, out relleno);

            Caja(escena, "PUVida", new Vector3(-3.5f, 0.5f, 1.5f));
            Caja(escena, "PUBalas", new Vector3(0f, 0.5f, 1.5f));
            Caja(escena, "PUArma", new Vector3(3.5f, 0.5f, 1.5f));

            FotosDeLaHorda.PonerLaLuz(escena, escenario);
            try
            {
                var foto = FotosDeLaHorda.Foto(camara, rt);
                File.WriteAllBytes(Path.GetFullPath(Carpeta + "/" + escenario.idTexto + ".png"), foto.EncodeToPNG());
                Object.DestroyImmediate(foto);
            }
            finally
            {
                Unsupported.RestoreOverrideLightingSettings();
            }
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(escena);
        }
    }

    static void Caja(Scene escena, string prefab, Vector3 donde)
    {
        var p = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/" + prefab + ".prefab");
        if (p == null) return;
        var go = (GameObject)PrefabUtility.InstantiatePrefab(p, escena);
        go.transform.position = donde;
    }
}
