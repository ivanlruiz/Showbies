using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Lo que tira cada arma de la mano (Proyectiles, 12/10) con el color de su tramo, y la critica
// (BulletController.Vestir), de noche, en cada capitulo, con la calidad del telefono y la camara
// del juego (FotosDeLaHorda.ArmarNoche): de izquierda a derecha, un chorro corto de la pistola,
// el revolver, la escopeta (sus cuatro perdigones), el rifle y la de energia, y al final la
// critica de la pistola. Sin play y sin tocar la escena abierta. Escribe
// Builds/balas/<capitulo>.png.
public static class FotosDeLasBalas
{
    const string Escena = "Assets/Escenas/WaveMode.unity";
    const string Carpeta = "../Builds/balas";
    const int Ancho = 1920, Alto = 1080;

    [MenuItem("ShowBies/Armas/Fotos de las balas")]
    public static void Sacar()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogError("FotosDeLasBalas: no en play"); return; }
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
            if (capitulos == null || camaraDelJuego == null || relleno == null) { Debug.LogError("FotosDeLasBalas: a WaveMode le falta algo"); return; }
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
        Debug.Log("FotosDeLasBalas: listas en " + Path.GetFullPath(Carpeta));
    }

    static void Fotografiar(EscenarioDeCapitulo escenario, Camera camaraDelJuego, Light rellenoDeLaEscena, RenderTexture rt)
    {
        var escena = EditorSceneManager.NewPreviewScene();
        try
        {
            Light relleno;
            var camara = FotosDeLaHorda.ArmarNoche(escena, escenario, camaraDelJuego, rellenoDeLaEscena, rt, out relleno);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Bullet.prefab");
            int columnas = ArmaEnLaMano.Armas + 1;
            for (int c = 0; c < columnas; c++)
            {
                bool critica = c == columnas - 1;
                int arma = critica ? Proyectiles.Pistola : c;
                float dano = critica ? 1f : Mathf.Max(1f, BulletController.DesdeDano[c]);
                float x = (c - (columnas - 1) * 0.5f) * 2.6f;
                for (int k = 0; k < 4; k++)
                {
                    float z = -1.5f + 1.1f * k;
                    if (arma == Proyectiles.Escopeta)
                    {
                        // Los cuatro perdigones de un tiro, abiertos como salen.
                        var angulos = Proyectiles.AngulosDeLaEscopeta;
                        for (int p = 0; p < angulos.Length; p++)
                        {
                            var giro = Quaternion.AngleAxis(angulos[p], Vector3.up);
                            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, escena);
                            go.transform.SetPositionAndRotation(new Vector3(x, 1.1f, z) + giro * Vector3.forward * 0.4f - Vector3.forward * 0.4f + Vector3.right * Mathf.Sin(angulos[p] * Mathf.Deg2Rad) * (0.6f + 0.25f * k), giro);
                            go.GetComponent<BulletController>().Vestir(dano, critica, arma, Proyectiles.LlevaLuz(arma, p), 1f);
                        }
                        continue;
                    }
                    var una = (GameObject)PrefabUtility.InstantiatePrefab(prefab, escena);
                    una.transform.SetPositionAndRotation(new Vector3(x, 1.1f, z), Quaternion.identity);
                    una.GetComponent<BulletController>().Vestir(dano, critica, arma, Proyectiles.LlevaLuz(arma, 0), 1f);
                }
            }

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
}
