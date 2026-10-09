using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Cuanto se distingue cada zombi del piso que tiene detras, de noche, en cada capitulo y con
// la calidad del telefono. Sin play y sin tocar la escena abierta: como FotosDeLosFaroles,
// arma cada escenario en una escena de vista previa (el piso, la luna, la luz ambiente, la
// niebla y el decorado) con la camara del juego, y le suma la luz de relleno de WaveMode y
// los cinco zombis corriendo hacia la camara.
//
// Existe porque en Discord (8/10) dijeron que a algunos zombis no se los ve de noche. El
// contraste de cada uno es el de la WCAG entre el brillo medio de sus pixeles y el del piso
// en esos mismos pixeles sin el (una foto con cada zombi solo y otra sin ninguno). Con la piel de
// antes (Legacy Diffuse) daba entre 1,0 y 2,4, y subiendo la luz de relleno no mejoraba en
// ningun capitulo: el piso de la ciudad es mas claro que los zombis y el del cementerio mas
// oscuro. Se compararon un borde de neon y la piel con brillo propio (ShowBies/PielDeZombi), y
// Ivan eligio esa, con el brillo en 0,5. El promedio no mide el contorno, que es lo que hace leer
// una figura de 40 px: para mirar estan las fotos, a 1920 x 1080 y con un recorte de cada
// zombi. Escribe Builds/prueba_horda.txt y Builds/horda/<capitulo>.png (y _<n> los recortes).
public static class FotosDeLaHorda
{
    const string Ruta = "../Builds/prueba_horda.txt";
    const string Escena = "Assets/Escenas/WaveMode.unity";
    const string Carpeta = "../Builds/horda";
    const int Ancho = 1920, Alto = 1080;
    const int Recorte = 220;   // el cuadrado de cada zombi, en pixeles de la foto

    static readonly string[] Zombis = { "Zombi", "ZombiRapido", "ZombiFASTER", "ZombiTanque", "ZombiBOSS" };

    // Donde corre cada uno, alrededor del jugador (en el centro), dentro de cuadro.
    static readonly Vector3[] Lugares =
    {
        new Vector3(-7f, 0f, 3f), new Vector3(-3.5f, 0f, -2f), new Vector3(0f, 0f, 5f),
        new Vector3(3.5f, 0f, -2f), new Vector3(7.5f, 0f, 4f),
    };

    [MenuItem("ShowBies/Escenarios/Fotos de la horda de noche")]
    public static void Sacar()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("FotosDeLaHorda: no en play");
            return;
        }
        var inf = new StringBuilder();
        inf.AppendLine("Los zombis de noche contra el piso, con la calidad del telefono (contraste WCAG; 3 es el minimo de un texto grande)");

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
            if (capitulos == null || camaraDelJuego == null || relleno == null)
            {
                inf.AppendLine("ERROR: WaveMode no tiene CapitulosDeEscenario, camara principal o Relleno");
            }
            else
            {
                int telefono = CalidadDeAndroid.Guardada() >= 0 ? CalidadDeAndroid.Guardada() : CalidadDeAndroid.Medium;
                QualitySettings.SetQualityLevel(telefono, false);
                inf.AppendLine("Calidad: " + QualitySettings.names[telefono] + "; relleno de la escena: " + relleno.intensity);
                Directory.CreateDirectory(Path.GetFullPath(Carpeta));
                for (int i = 0; i < capitulos.escenarios.Length; i++)
                {
                    var escenario = capitulos.escenarios[i];
                    if (escenario.decorado == null) continue;
                    if (i == 0) escenario = LoQueTraeLaEscena(escenario, capitulos, camaraDelJuego, waveMode);
                    Fotografiar(escenario, camaraDelJuego, relleno, rt, inf);
                }
            }
        }
        finally
        {
            if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
            QualitySettings.SetQualityLevel(calidadDelEditor, false);
            rt.Release();
            Object.DestroyImmediate(rt);
            if (abriYo) EditorSceneManager.CloseScene(waveMode, true);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(Ruta)));
        File.WriteAllText(Ruta, inf.ToString());
        Debug.Log(inf.ToString());
    }

    // Lo mismo que FotosDeLosFaroles.LoQueTraeLaEscena: la pradera se lee de WaveMode.
    static EscenarioDeCapitulo LoQueTraeLaEscena(EscenarioDeCapitulo primero, CapitulosDeEscenario capitulos, Camera camara, Scene escena)
    {
        var e = new EscenarioDeCapitulo { idTexto = primero.idTexto, decorado = primero.decorado };
        e.piso = capitulos.piso != null ? capitulos.piso.sharedMaterial : primero.piso;
        e.cielo = camara.backgroundColor;
        if (capitulos.sol != null)
        {
            e.luz = capitulos.sol.color;
            e.intensidadLuz = capitulos.sol.intensity;
            e.rotacionLuz = capitulos.sol.transform.rotation.eulerAngles;
        }
        var activa = SceneManager.GetActiveScene();
        SceneManager.SetActiveScene(escena);
        e.ambiente = RenderSettings.ambientLight;
        e.conNiebla = RenderSettings.fog;
        e.nieblaInicio = RenderSettings.fogStartDistance;
        e.nieblaFin = RenderSettings.fogEndDistance;
        SceneManager.SetActiveScene(activa);
        return e;
    }

    static void Fotografiar(EscenarioDeCapitulo escenario, Camera camaraDelJuego, Light rellenoDeLaEscena,
                            RenderTexture rt, StringBuilder inf)
    {
        var escena = EditorSceneManager.NewPreviewScene();
        try
        {
            var piso = GameObject.CreatePrimitive(PrimitiveType.Plane);
            piso.transform.localScale = new Vector3(10f, 1f, 10f);
            piso.GetComponent<Renderer>().sharedMaterial = escenario.piso;
            SceneManager.MoveGameObjectToScene(piso, escena);

            var luna = new GameObject("Luna").AddComponent<Light>();
            luna.type = LightType.Directional;
            luna.color = escenario.luz;
            luna.intensity = escenario.intensidadLuz;
            luna.transform.rotation = Quaternion.Euler(escenario.rotacionLuz);
            luna.shadows = LightShadows.Hard;
            SceneManager.MoveGameObjectToScene(luna.gameObject, escena);

            var relleno = new GameObject("Relleno").AddComponent<Light>();
            relleno.type = LightType.Directional;
            relleno.color = rellenoDeLaEscena.color;
            relleno.intensity = rellenoDeLaEscena.intensity;
            relleno.transform.rotation = rellenoDeLaEscena.transform.rotation;
            relleno.cullingMask = rellenoDeLaEscena.cullingMask;
            relleno.renderMode = rellenoDeLaEscena.renderMode;
            relleno.shadows = LightShadows.None;
            SceneManager.MoveGameObjectToScene(relleno.gameObject, escena);

            var decorado = Object.Instantiate(escenario.decorado);
            SceneManager.MoveGameObjectToScene(decorado, escena);

            // Los cinco, corriendo hacia la camara, parados en el piso y en la capa que alumbra
            // el relleno (en el juego los pone ahi su Awake). Sin sombra: la suya cambiaria
            // pixeles del piso y ensuciaria la medida.
            var zombis = new List<GameObject>();
            var poses = new List<KeyValuePair<GameObject, AnimationClip>>();
            var correr = Clip("Z_run");
            var caminar = Clip("Z_walk");
            for (int i = 0; i < Zombis.Length; i++)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Personajes/" + Zombis[i] + ".prefab");
                if (prefab == null) continue;
                var zombi = (GameObject)PrefabUtility.InstantiatePrefab(prefab, escena);
                var capsula = zombi.GetComponent<CapsuleCollider>();
                float y = capsula != null ? (capsula.height * 0.5f - capsula.center.y) * zombi.transform.lossyScale.y : 1f;
                zombi.transform.SetPositionAndRotation(Lugares[i] + Vector3.up * y, Quaternion.Euler(0f, 180f, 0f));
                Personajes.PonerEnLaCapa(zombi);
                foreach (var r in zombi.GetComponentsInChildren<Renderer>(true))
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                var controlador = zombi.GetComponent<EnemyController>();
                var ritmo = controlador != null ? new SerializedObject(controlador).FindProperty("ritmoDeAndar") : null;
                bool camina = ritmo != null && ritmo.floatValue < 0.5f;
                foreach (var a in zombi.GetComponentsInChildren<Animator>(true))
                {
                    if (a.runtimeAnimatorController == null) continue;
                    a.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                    poses.Add(new KeyValuePair<GameObject, AnimationClip>(a.gameObject, camina ? caminar : correr));
                }
                zombis.Add(zombi);
            }
            AnimationMode.StartAnimationMode();
            AnimationMode.BeginSampling();
            foreach (var p in poses)
                if (p.Value != null) AnimationMode.SampleAnimationClip(p.Key, p.Value, 0.3f * p.Value.length);
            AnimationMode.EndSampling();

            var camara = new GameObject("Camara").AddComponent<Camera>();
            SceneManager.MoveGameObjectToScene(camara.gameObject, escena);
            camara.scene = escena;
            camara.transform.SetPositionAndRotation(camaraDelJuego.transform.position, camaraDelJuego.transform.rotation);
            camara.fieldOfView = camaraDelJuego.fieldOfView;
            camara.nearClipPlane = camaraDelJuego.nearClipPlane;
            camara.farClipPlane = camaraDelJuego.farClipPlane;
            camara.clearFlags = CameraClearFlags.SolidColor;
            camara.backgroundColor = escenario.cielo;
            camara.allowHDR = false;
            camara.allowMSAA = false;
            camara.renderingPath = RenderingPath.Forward;
            camara.targetTexture = rt;
            camara.aspect = (float)Ancho / Alto;
            camara.enabled = false;

            Unsupported.SetOverrideLightingSettings(escena);
            try
            {
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.ambientLight = escenario.ambiente;
                RenderSettings.fog = escenario.conNiebla;
                RenderSettings.fogMode = FogMode.Linear;
                RenderSettings.fogColor = escenario.cielo;
                RenderSettings.fogStartDistance = escenario.nieblaInicio;
                RenderSettings.fogEndDistance = escenario.nieblaFin;

                inf.AppendLine();
                inf.AppendLine(escenario.idTexto + ":");
                {
                    Mostrar(zombis, -1);
                    var sin = Foto(camara, rt);
                    var linea = new StringBuilder("  relleno " + relleno.intensity.ToString("0.00") + ":");
                    float peor = float.MaxValue;
                    for (int i = 0; i < zombis.Count; i++)
                    {
                        Mostrar(zombis, i);
                        var con = Foto(camara, rt);
                        float brilloZombi, brilloPiso;
                        float contraste = Contraste(con, sin, out brilloZombi, out brilloPiso);
                        peor = Mathf.Min(peor, contraste);
                        linea.Append("  " + zombis[i].name + " " + contraste.ToString("0.0") + " (" + brilloZombi.ToString("0.000") + " contra " + brilloPiso.ToString("0.000") + ")");
                        Object.DestroyImmediate(con);
                    }
                    linea.Append("  | el peor: " + peor.ToString("0.0"));
                    inf.AppendLine(linea.ToString());
                    Mostrar(zombis, -2);
                    var todos = Foto(camara, rt);
                    string nombre = Path.GetFullPath(Carpeta + "/" + escenario.idTexto);
                    File.WriteAllBytes(nombre + ".png", todos.EncodeToPNG());
                    // El recorte de cada uno, alrededor del centro de su figura en la foto.
                    for (int i = 0; i < zombis.Count; i++)
                    {
                        Bounds caja = default;
                        bool hay = false;
                        foreach (var r in zombis[i].GetComponentsInChildren<Renderer>(true))
                        {
                            if (r.GetComponent<Collider>() != null) continue;
                            if (hay) caja.Encapsulate(r.bounds); else { caja = r.bounds; hay = true; }
                        }
                        Vector3 centro = camara.WorldToScreenPoint(hay ? caja.center : zombis[i].transform.position);
                        int x0 = Mathf.Clamp((int)centro.x - Recorte / 2, 0, Ancho - Recorte);
                        int y0 = Mathf.Clamp((int)centro.y - Recorte / 2, 0, Alto - Recorte);
                        var recorte = new Texture2D(Recorte, Recorte, TextureFormat.RGB24, false);
                        recorte.SetPixels(todos.GetPixels(x0, y0, Recorte, Recorte));
                        recorte.Apply();
                        File.WriteAllBytes(nombre + "_" + i + ".png", recorte.EncodeToPNG());
                        Object.DestroyImmediate(recorte);
                    }
                    Object.DestroyImmediate(todos);
                    Object.DestroyImmediate(sin);
                }
            }
            finally
            {
                Unsupported.RestoreOverrideLightingSettings();
                AnimationMode.StopAnimationMode();
            }
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(escena);
        }
    }

    // -1: ninguno; -2: todos; si no, solo ese.
    static void Mostrar(List<GameObject> zombis, int cual)
    {
        for (int i = 0; i < zombis.Count; i++)
        {
            bool prendido = cual == -2 || cual == i;
            foreach (var r in zombis[i].GetComponentsInChildren<Renderer>(true))
            {
                // Los colliders de la capsula y las hitboxes tienen su renderer apagado de fabrica.
                if (r.GetComponent<Collider>() != null) continue;
                r.forceRenderingOff = !prendido;
            }
        }
    }

    // El contraste WCAG entre los pixeles que cambian con el zombi (su silueta) y esos mismos
    // pixeles sin el. Devuelve tambien los dos brillos (luminancia relativa, lineal).
    static float Contraste(Texture2D con, Texture2D sin, out float zombi, out float piso)
    {
        var a = con.GetPixels();
        var b = sin.GetPixels();
        double sumaZombi = 0, sumaPiso = 0;
        int n = 0;
        for (int i = 0; i < a.Length; i++)
        {
            Color c = a[i], d = b[i];
            if (Mathf.Abs(c.r - d.r) + Mathf.Abs(c.g - d.g) + Mathf.Abs(c.b - d.b) < 0.03f) continue;
            sumaZombi += Luminancia(c);
            sumaPiso += Luminancia(d);
            n++;
        }
        zombi = n > 0 ? (float)(sumaZombi / n) : 0f;
        piso = n > 0 ? (float)(sumaPiso / n) : 0f;
        float claro = Mathf.Max(zombi, piso), oscuro = Mathf.Min(zombi, piso);
        return (claro + 0.05f) / (oscuro + 0.05f);
    }

    static float Luminancia(Color c)
    {
        return 0.2126f * Lineal(c.r) + 0.7152f * Lineal(c.g) + 0.0722f * Lineal(c.b);
    }

    static float Lineal(float v)
    {
        return v <= 0.04045f ? v / 12.92f : Mathf.Pow((v + 0.055f) / 1.055f, 2.4f);
    }

    static Texture2D Foto(Camera camara, RenderTexture rt)
    {
        camara.Render();
        var antes = RenderTexture.active;
        RenderTexture.active = rt;
        var foto = new Texture2D(Ancho, Alto, TextureFormat.RGB24, false);
        foto.ReadPixels(new Rect(0, 0, Ancho, Alto), 0, 0);
        foto.Apply();
        RenderTexture.active = antes;
        return foto;
    }

    static AnimationClip Clip(string nombre)
    {
        foreach (var guid in AssetDatabase.FindAssets(nombre + " t:AnimationClip"))
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(guid)))
            {
                var clip = o as AnimationClip;
                if (clip != null && clip.name == nombre) return clip;
            }
        Debug.LogWarning("FotosDeLaHorda: no esta el clip " + nombre);
        return null;
    }
}
