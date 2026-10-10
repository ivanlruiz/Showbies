using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Maquetas para elegir lo visual de la 1.5.2 (10/10, de la revision de Gemini), de noche en los
// tres capitulos, con la camara del juego y la calidad del telefono. Sin play y sin tocar la
// escena abierta: cada capitulo se arma en una escena de vista previa con FotosDeLaHorda.
//  - heroe_<capitulo>: el heroe con la remera de siempre (negra), blanca, amarilla y naranja,
//    arriba sin aura y abajo con un charco de luz de su color bajo los pies (celeste el negro).
//    La remera se pinta en una copia en memoria de la textura del pack (TT_demo.mat no se toca).
//  - borde_<capitulo>: los cinco zombis sin borde, con un borde de luz de su color y con uno
//    blanco (ShowBies/PielDeZombi, _Borde), en copias de sus materiales.
//  - sombras_<capitulo>: a la izquierda con las sombras de verdad de la luna, a la derecha sin
//    ellas y con la sombra falsa (ShowBies/SombraFalsa).
// Escribe Builds/maquetas_152/.
public static class MaquetasDelHeroe
{
    const string Escena = "Assets/Escenas/WaveMode.unity";
    const string Carpeta = "../Builds/maquetas_152";
    const int Ancho = 1920, Alto = 1080;
    const float EscalaDelModelo = 0.6f;   // 1,2 dentro del jugador, que esta a 0,5

    // La remera en la textura del pack (en pixeles de la de 512, con y hacia abajo): el bloque
    // que pintan los vertices del hueso Bip001 Spine, medido de la malla de TT_demo_male_A.
    const int RemeraX0 = 245, RemeraX1 = 273, RemeraY0 = 166, RemeraY1 = 198;
    static readonly Color32 ColorRemera = new Color32(42, 35, 31, 255);

    static readonly string[] Zombis = { "Zombi", "ZombiRapido", "ZombiFASTER", "ZombiTanque", "ZombiBOSS" };

    struct Variante
    {
        public string nombre;
        public Color remera;      // alfa 0: la de siempre
        public Color aura;
    }

    static readonly Variante[] Remeras =
    {
        new Variante { nombre = "negra", remera = new Color(0, 0, 0, 0), aura = new Color(0.25f, 0.85f, 1f) },
        new Variante { nombre = "blanca", remera = new Color(0.94f, 0.94f, 0.94f), aura = new Color(0.9f, 0.95f, 1f) },
        new Variante { nombre = "amarilla", remera = new Color(1f, 0.82f, 0.12f), aura = new Color(1f, 0.82f, 0.2f) },
        new Variante { nombre = "naranja", remera = new Color(1f, 0.45f, 0.1f), aura = new Color(1f, 0.5f, 0.15f) },
    };

    [MenuItem("ShowBies/Arte/Maquetas de la 1.5.2 (heroe, borde, sombras)")]
    public static void Sacar()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("MaquetasDelHeroe: no en play");
            return;
        }
        Scene waveMode = SceneManager.GetSceneByPath(Escena);
        bool abriYo = false;
        if (!waveMode.isLoaded)
        {
            waveMode = EditorSceneManager.OpenScene(Escena, OpenSceneMode.Additive);
            abriYo = true;
        }

        int calidadDelEditor = QualitySettings.GetQualityLevel();
        var rt = new RenderTexture(Ancho, Alto, 24);
        var creados = new List<Object>();
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
                Debug.LogError("MaquetasDelHeroe: WaveMode no tiene CapitulosDeEscenario, camara principal o Relleno");
                return;
            }

            int telefono = CalidadDeAndroid.Guardada() >= 0 ? CalidadDeAndroid.Guardada() : CalidadDeAndroid.Medium;
            QualitySettings.SetQualityLevel(telefono, false);
            Directory.CreateDirectory(Path.GetFullPath(Carpeta));

            var texturas = TexturasDelHeroe(creados);
            for (int i = 0; i < capitulos.escenarios.Length; i++)
            {
                var escenario = capitulos.escenarios[i];
                if (escenario.decorado == null) continue;
                if (i == 0) escenario = FotosDeLaHorda.LoQueTraeLaEscena(escenario, capitulos, camaraDelJuego, waveMode);
                Fotografiar(escenario, camaraDelJuego, relleno, rt, "heroe", (escena) => PonerHeroes(escena, texturas, creados));
                Fotografiar(escenario, camaraDelJuego, relleno, rt, "borde", (escena) => PonerBordes(escena, creados));
                Fotografiar(escenario, camaraDelJuego, relleno, rt, "sombras", (escena) => PonerSombras(escena, texturas, creados));
            }
        }
        finally
        {
            if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
            QualitySettings.SetQualityLevel(calidadDelEditor, false);
            rt.Release();
            Object.DestroyImmediate(rt);
            foreach (var o in creados) if (o != null) Object.DestroyImmediate(o);
            if (abriYo) EditorSceneManager.CloseScene(waveMode, true);
        }
        Debug.Log("MaquetasDelHeroe: listas en " + Path.GetFullPath(Carpeta));
    }

    // La textura del pack con la remera de cada color, en memoria. Se lee por una RenderTexture
    // porque la del pack no esta marcada como legible.
    static Texture2D[] TexturasDelHeroe(List<Object> creados)
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/ToonyTinyPeople/TT_demo/models/Materials/TT_demo.mat");
        var original = mat != null ? mat.mainTexture as Texture2D : null;
        var resultado = new Texture2D[Remeras.Length];
        if (original == null) return resultado;
        int w = original.width, h = original.height;
        var rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        Graphics.Blit(original, rt);
        var antes = RenderTexture.active;
        RenderTexture.active = rt;
        var leida = new Texture2D(w, h, TextureFormat.RGBA32, false);
        leida.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        leida.Apply();
        RenderTexture.active = antes;
        RenderTexture.ReleaseTemporary(rt);
        creados.Add(leida);

        float escala = w / 512f;
        for (int v = 0; v < Remeras.Length; v++)
        {
            if (Remeras[v].remera.a <= 0f) { resultado[v] = original; continue; }
            var t = new Texture2D(w, h, TextureFormat.RGBA32, true);
            t.SetPixels32(leida.GetPixels32());
            Color32 nuevo = Remeras[v].remera;
            for (int y = Mathf.FloorToInt(RemeraY0 * escala); y <= Mathf.CeilToInt(RemeraY1 * escala); y++)
            {
                for (int x = Mathf.FloorToInt(RemeraX0 * escala); x <= Mathf.CeilToInt(RemeraX1 * escala); x++)
                {
                    int yu = h - 1 - y;   // en la textura de Unity la y va hacia arriba
                    Color32 c = t.GetPixel(x, yu);
                    int d = Mathf.Abs(c.r - ColorRemera.r) + Mathf.Abs(c.g - ColorRemera.g) + Mathf.Abs(c.b - ColorRemera.b);
                    if (d < 40) t.SetPixel(x, yu, nuevo);
                }
            }
            t.Apply(true);
            creados.Add(t);
            resultado[v] = t;
        }
        return resultado;
    }

    static void Fotografiar(EscenarioDeCapitulo escenario, Camera camaraDelJuego, Light rellenoDeLaEscena, RenderTexture rt,
                            string que, System.Action<Scene> poner)
    {
        var escena = EditorSceneManager.NewPreviewScene();
        try
        {
            Light relleno;
            var camara = FotosDeLaHorda.ArmarNoche(escena, escenario, camaraDelJuego, rellenoDeLaEscena, rt, out relleno);
            poses.Clear();
            poner(escena);
            AnimationMode.StartAnimationMode();
            AnimationMode.BeginSampling();
            foreach (var p in poses)
                if (p.clip != null) AnimationMode.SampleAnimationClip(p.objeto, p.clip, p.momento * p.clip.length);
            AnimationMode.EndSampling();
            FotosDeLaHorda.PonerLaLuz(escena, escenario);
            try
            {
                var foto = FotosDeLaHorda.Foto(camara, rt);
                File.WriteAllBytes(Path.GetFullPath(Carpeta + "/" + que + "_" + escenario.idTexto + ".png"), foto.EncodeToPNG());
                Object.DestroyImmediate(foto);
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

    struct Pose
    {
        public GameObject objeto;
        public AnimationClip clip;
        public float momento;
    }

    static readonly List<Pose> poses = new List<Pose>();

    // Los cuatro colores en una fila sin aura (arriba) y otra con aura (abajo), y una fila de
    // zombis detras para comparar.
    static void PonerHeroes(Scene escena, Texture2D[] texturas, List<Object> creados)
    {
        float[] xs = { -7.5f, -2.5f, 2.5f, 7.5f };
        for (int v = 0; v < Remeras.Length; v++)
        {
            var material = MaterialDelHeroe(texturas[v], creados);
            Heroe(escena, new Vector3(xs[v], 0f, 2.5f), material, true);
            Heroe(escena, new Vector3(xs[v], 0f, -3f), material, true);
            Aura(escena, new Vector3(xs[v], 0f, -3f), Remeras[v].aura, creados);
        }
        for (int i = 0; i < Zombis.Length; i++) Zombi(escena, Zombis[i], new Vector3(-8f + 4f * i, 0f, 7f), null, true);
    }

    // Los cinco: sin borde (arriba), con borde de su color (al medio) y blanco (abajo).
    static void PonerBordes(Scene escena, List<Object> creados)
    {
        float[] zs = { 5.5f, 1f, -3.5f };
        for (int fila = 0; fila < 3; fila++)
        {
            for (int i = 0; i < Zombis.Length; i++)
            {
                Zombi(escena, Zombis[i], new Vector3(-8f + 4f * i, 0f, zs[fila]), (r) =>
                {
                    if (fila == 0) return;
                    var mats = r.sharedMaterials;
                    for (int m = 0; m < mats.Length; m++)
                    {
                        if (mats[m] == null || mats[m].shader == null || mats[m].shader.name != "ShowBies/PielDeZombi") continue;
                        var copia = new Material(mats[m]);
                        creados.Add(copia);
                        Color piel = mats[m].GetColor("_Color");
                        copia.SetFloat("_Borde", fila == 1 ? 1.6f : 1.2f);
                        copia.SetFloat("_FinuraBorde", 2.5f);
                        copia.SetColor("_ColorBorde", fila == 1 ? Color.Lerp(piel, Color.white, 0.35f) : Color.white);
                        mats[m] = copia;
                    }
                    r.sharedMaterials = mats;
                }, true);
            }
        }
    }

    // A la izquierda con las sombras de la luna; a la derecha sin ellas y con la falsa.
    static void PonerSombras(Scene escena, Texture2D[] texturas, List<Object> creados)
    {
        var material = MaterialDelHeroe(texturas[0], creados);
        var sombra = new Material(Shader.Find("ShowBies/SombraFalsa"));
        creados.Add(sombra);
        foreach (int lado in new[] { -1, 1 })
        {
            bool real = lado < 0;
            float x = lado * 5f;
            Heroe(escena, new Vector3(x, 0f, 1f), material, real);
            if (!real) Sombra(escena, new Vector3(x, 0f, 1f), 1.1f, sombra);
            Vector3[] zs = { new Vector3(x - 2.5f, 0f, 4f), new Vector3(x + 2f, 0f, 4.5f), new Vector3(x + 1.5f, 0f, -2.5f) };
            string[] tipos = { "Zombi", "ZombiTanque", "ZombiRapido" };
            for (int i = 0; i < zs.Length; i++)
            {
                var z = Zombi(escena, tipos[i], zs[i], null, real);
                if (!real && z != null) Sombra(escena, zs[i], RadioDeSombra(z), sombra);
            }
        }
    }

    static float RadioDeSombra(GameObject zombi)
    {
        var capsula = zombi.GetComponent<CapsuleCollider>();
        if (capsula == null) return 1f;
        return capsula.radius * Mathf.Abs(zombi.transform.lossyScale.x) * 1.6f;
    }

    static Material MaterialDelHeroe(Texture2D textura, List<Object> creados)
    {
        var mat = new Material(Shader.Find("Legacy Shaders/Diffuse"));
        mat.mainTexture = textura;
        creados.Add(mat);
        return mat;
    }

    static void Heroe(Scene escena, Vector3 donde, Material material, bool conSombra)
    {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ToonyTinyPeople/TT_demo/models/TT_demo_male_A.FBX");
        if (asset == null) return;
        // El clip pisa el giro de la raiz al muestrearlo: lo que gira es un pivote.
        var pivote = new GameObject("Heroe");
        SceneManager.MoveGameObjectToScene(pivote, escena);
        pivote.transform.SetPositionAndRotation(donde, Quaternion.Euler(0f, 160f, 0f));
        pivote.transform.localScale = Vector3.one * EscalaDelModelo;
        var modelo = (GameObject)PrefabUtility.InstantiatePrefab(asset, escena);
        modelo.transform.SetParent(pivote.transform, false);
        modelo.transform.localPosition = Vector3.zero;
        modelo.transform.localRotation = Quaternion.identity;
        Personajes.PonerEnLaCapa(modelo);
        foreach (var r in modelo.GetComponentsInChildren<Renderer>(true))
        {
            if (r.name.Contains("baseballbat")) { r.enabled = false; continue; }
            r.sharedMaterial = material;
            r.shadowCastingMode = conSombra ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        var animador = modelo.GetComponent<Animator>();
        if (animador != null)
        {
            animador.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            poses.Add(new Pose { objeto = animador.gameObject, clip = Clip("m_pistol_shoot"), momento = 0.35f });
            var mano = animador.isHuman ? animador.GetBoneTransform(HumanBodyBones.RightHand) : null;
            var contenedor = mano != null ? mano.Find("R_hand_container") : null;
            if (contenedor == null) contenedor = mano;
            var pistola = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Pistola.prefab");
            if (contenedor != null && pistola != null)
            {
                foreach (Transform hijo in contenedor) hijo.gameObject.SetActive(false);
                var p = (GameObject)PrefabUtility.InstantiatePrefab(pistola, escena);
                p.transform.SetParent(contenedor, false);
                p.transform.localPosition = Vector3.zero;
                p.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                p.transform.localScale = Vector3.one;
                Personajes.PonerEnLaCapa(p);
            }
        }
    }

    static GameObject Zombi(Scene escena, string nombre, Vector3 donde, System.Action<Renderer> vestir, bool conSombra)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Personajes/" + nombre + ".prefab");
        if (prefab == null) return null;
        var zombi = (GameObject)PrefabUtility.InstantiatePrefab(prefab, escena);
        var capsula = zombi.GetComponent<CapsuleCollider>();
        float y = capsula != null ? (capsula.height * 0.5f - capsula.center.y) * zombi.transform.lossyScale.y : 1f;
        zombi.transform.SetPositionAndRotation(donde + Vector3.up * y, Quaternion.Euler(0f, 180f, 0f));
        Personajes.PonerEnLaCapa(zombi);
        foreach (var r in zombi.GetComponentsInChildren<Renderer>(true))
        {
            if (r.GetComponent<Collider>() != null) continue;
            r.shadowCastingMode = conSombra ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
            if (vestir != null) vestir(r);
        }
        var controlador = zombi.GetComponent<EnemyController>();
        var ritmo = controlador != null ? new SerializedObject(controlador).FindProperty("ritmoDeAndar") : null;
        bool camina = ritmo != null && ritmo.floatValue < 0.5f;
        foreach (var a in zombi.GetComponentsInChildren<Animator>(true))
        {
            if (a.runtimeAnimatorController == null) continue;
            a.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            poses.Add(new Pose { objeto = a.gameObject, clip = Clip(camina ? "Z_walk" : "Z_run"), momento = 0.3f });
        }
        return zombi;
    }

    static void Aura(Scene escena, Vector3 donde, Color color, List<Object> creados)
    {
        var mat = new Material(Shader.Find("ShowBies/CharcoDeLuz"));
        mat.SetColor("_Color", color);
        mat.SetFloat("_Intensidad", 1.6f);
        creados.Add(mat);
        Cuadrado(escena, "Aura", donde + Vector3.up * 0.04f, 2.6f, mat);
    }

    static void Sombra(Scene escena, Vector3 donde, float radio, Material mat)
    {
        Cuadrado(escena, "Sombra", donde + Vector3.up * 0.03f, radio * 2f, mat);
    }

    static void Cuadrado(Scene escena, string nombre, Vector3 donde, float lado, Material mat)
    {
        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = nombre;
        Object.DestroyImmediate(quad.GetComponent<Collider>());
        SceneManager.MoveGameObjectToScene(quad, escena);
        quad.transform.SetPositionAndRotation(donde, Quaternion.Euler(90f, 0f, 0f));
        quad.transform.localScale = Vector3.one * lado;
        var r = quad.GetComponent<Renderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
    }

    static AnimationClip Clip(string nombre)
    {
        foreach (var guid in AssetDatabase.FindAssets(nombre + " t:AnimationClip"))
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(guid)))
            {
                var clip = o as AnimationClip;
                if (clip != null && clip.name == nombre) return clip;
            }
        return null;
    }
}
