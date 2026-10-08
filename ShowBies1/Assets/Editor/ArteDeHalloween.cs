using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Las imagenes del evento de Halloween para afuera del juego: la de la tarjeta de contenido
// promocional de Play (1920 x 1080, sin ningun texto, lo importante en el centro) y las de la
// campania de Google Ads (1200 x 628 y 1200 x 1200). Es una escena armada a mano sobre la noche
// de WaveMode, con lo que arma ConstructorHalloween: el jugador con el sombrero de calabaza,
// corriendo y disparando, y la horda disfrazada que viene, entre calabazas farol, con caramelos
// y monedas en el piso. Los modelos se posan muestreando un clip con AnimationMode (sin play el
// Animator de los zombis no mueve huesos: esta en Cull Update Transforms y nadie los ve). Va a
// Builds/halloween/arte_*.png. WaveMode se abre y se descarta sin guardar.
public static class ArteDeHalloween
{
    const string Carpeta = "../Builds/halloween";
    const string Zombi = "Assets/Prefabs/Personajes/Zombi.prefab";
    const string Rapido = "Assets/Prefabs/Personajes/ZombiRapido.prefab";
    const string Faster = "Assets/Prefabs/Personajes/ZombiFASTER.prefab";
    const string Tanque = "Assets/Prefabs/Personajes/ZombiTanque.prefab";
    const string Jefe = "Assets/Prefabs/Personajes/ZombiBOSS.prefab";

    // Lo que se posa: el objeto del Animator, el clip y en que momento (de 0 a 1).
    struct Pose
    {
        public GameObject objeto;
        public AnimationClip clip;
        public float momento;
    }

    static readonly List<Pose> poses = new List<Pose>();

    const float RumboDelJugador = 72f;

    [MenuItem("ShowBies/Halloween/Arte para Play y Google Ads")]
    public static void Sacar()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EscenasSinGuardar.Hay("Arte de Halloween")) return;
        var abiertas = EscenasSinGuardar.Recordar();
        int calidad = QualitySettings.GetQualityLevel();
        Directory.CreateDirectory(Path.GetFullPath(Carpeta));
        try
        {
            QualitySettings.SetQualityLevel(QualitySettings.names.Length - 1, true);
            poses.Clear();
            ArmarEscena();
            Posar();
            var camara = new GameObject("CamaraDelArte").AddComponent<Camera>();
            camara.clearFlags = CameraClearFlags.SolidColor;
            camara.backgroundColor = RenderSettings.fogColor;
            camara.allowHDR = false;
            camara.farClipPlane = 200f;

            Encuadrar(camara, new Vector3(3.3f, 4.2f, -8.2f), new Vector3(4.0f, 1.3f, 1.4f), 44f);
            Foto(camara, 1920, 1080, "arte_tarjeta_1920x1080");
            Foto(camara, 1200, 628, "arte_ads_1200x628");
            Encuadrar(camara, new Vector3(3.6f, 7.6f, -8.5f), new Vector3(4.0f, 0.9f, 1.8f), 54f);
            Foto(camara, 1200, 1200, "arte_ads_1200x1200");
        }
        finally
        {
            if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
            QualitySettings.SetQualityLevel(calidad, true);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EscenasSinGuardar.Volver(abiertas);
        }
        Debug.Log("ArteDeHalloween: listas en " + Path.GetFullPath(Carpeta));
    }

    static void Encuadrar(Camera camara, Vector3 desde, Vector3 hacia, float fov)
    {
        camara.transform.SetPositionAndRotation(desde, Quaternion.LookRotation(hacia - desde));
        camara.fieldOfView = fov;
        // Los halos de las calabazas y el fogonazo de la pistola, de frente a esta camara.
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
            if (t.name == "Halo" || t.name == "Fogonazo") t.rotation = camara.transform.rotation;
    }

    static void ArmarEscena()
    {
        EditorSceneManager.OpenScene("Assets/Escenas/WaveMode.unity", OpenSceneMode.Single);
        // El jugador de la escena y su modelo se esconden: va uno posado a mano.
        foreach (var jugador in Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None)) jugador.gameObject.SetActive(false);
        foreach (var raiz in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            if (raiz.name.StartsWith("TT_demo")) raiz.SetActive(false);

        // La pradera de noche, como en las oleadas 1 a 10.
        var pradera = AssetDatabase.LoadAssetAtPath<GameObject>(ConstructorEscenarios.RutaPrefabPradera);
        if (pradera != null) PrefabUtility.InstantiatePrefab(pradera);

        // De tres cuartos: un poco hacia la camara, para que se le vea la cara y el sombrero.
        var adelante = Quaternion.Euler(0f, RumboDelJugador, 0f) * Vector3.forward;
        Jugador(new Vector3(0f, 0f, 0.2f), RumboDelJugador);

        // La horda: viene desde la derecha hacia el jugador. Tipo, lugar, disfraz (0,1 calabaza,
        // 0,45 sombrero, 0,9 nada) y en que momento del paso.
        Horda(Rapido, new Vector3(3.1f, 0f, 1.3f), 0.1f, 0.15f);
        Horda(Zombi, new Vector3(3.6f, 0f, -1.0f), 0.45f, 0.55f);
        Horda(Faster, new Vector3(4.6f, 0f, 2.6f), 0.45f, 0.35f);
        Horda(Zombi, new Vector3(5.1f, 0f, 0.2f), 0.1f, 0.8f);
        Horda(Rapido, new Vector3(5.7f, 0f, -2.2f), 0.9f, 0.25f);
        Horda(Tanque, new Vector3(6.9f, 0f, 1.4f), 0.45f, 0.6f);
        Horda(Zombi, new Vector3(7.3f, 0f, -0.9f), 0.1f, 0.05f);
        Horda(Faster, new Vector3(7.9f, 0f, 3.6f), 0.1f, 0.7f);
        Horda(Rapido, new Vector3(8.6f, 0f, -2.6f), 0.45f, 0.45f);
        Horda(Zombi, new Vector3(9.2f, 0f, 0.6f), 0.9f, 0.9f);
        Horda(Jefe, new Vector3(9.2f, 0f, 5.4f), 0f, 0.3f, true);

        // Las calabazas farol y las chicas, alrededor de la accion.
        Calabaza(DecoradoHalloween.RutaCalabaza, new Vector3(-2.2f, 0f, 2.4f), 25f, 1.15f);
        Calabaza(DecoradoHalloween.RutaCalabaza, new Vector3(1.4f, 0f, -3.4f), -10f, 1f);
        Calabaza(DecoradoHalloween.RutaCalabazaChica, new Vector3(2.4f, 0f, -3.9f), 70f, 1f);
        Calabaza(DecoradoHalloween.RutaCalabaza, new Vector3(6.2f, 0f, 5.2f), 0f, 1.2f);
        Calabaza(DecoradoHalloween.RutaCalabaza, new Vector3(10.6f, 0f, -3.6f), -20f, 1.1f);
        Calabaza(DecoradoHalloween.RutaCalabazaChica, new Vector3(-1.2f, 0f, 3.6f), 140f, 1.1f);
        Calabaza(DecoradoHalloween.RutaCalabaza, new Vector3(-3.6f, 0f, -1.6f), 30f, 1f);
        Calabaza(DecoradoHalloween.RutaCalabazaChica, new Vector3(12.8f, 0f, 0.2f), 10f, 1.2f);

        // El chorro de balas, de la pistola hacia la horda.
        var bala = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Bullet.prefab");
        if (bala != null)
            for (int i = 0; i < 6; i++)
            {
                var b = (GameObject)PrefabUtility.InstantiatePrefab(bala);
                b.transform.SetPositionAndRotation(new Vector3(0f, 1.05f, 0.2f) + adelante * (0.9f + i * 0.75f), Quaternion.Euler(0f, RumboDelJugador, 0f));
            }

        // Caramelos y monedas en el piso y en el aire, donde cayeron los primeros.
        Botin("Halloween/Caramelo", new[] { new Vector3(2.1f, 0.3f, 0.6f), new Vector3(2.6f, 0.95f, -0.4f), new Vector3(1.6f, 0.3f, -1.3f), new Vector3(3.0f, 0.3f, -1.9f) });
        var moneda = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Moneda.prefab");
        foreach (var p in new[] { new Vector3(1.9f, 0.3f, 1.5f), new Vector3(2.4f, 1.3f, 0.9f), new Vector3(3.4f, 0.3f, -0.4f), new Vector3(1.2f, 0.3f, -2.0f) })
        {
            if (moneda == null) break;
            var m = (GameObject)PrefabUtility.InstantiatePrefab(moneda);
            m.transform.SetPositionAndRotation(p, Quaternion.Euler(0f, Random.Range(-40f, 40f), 0f));
            Personajes.PonerEnLaCapa(m);
        }
    }

    // El jugador corriendo y apuntando, con la pistola en la mano y el sombrero de calabaza.
    static void Jugador(Vector3 donde, float rumbo)
    {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ToonyTinyPeople/TT_demo/models/TT_demo_male_A.FBX");
        var modelo = (GameObject)PrefabUtility.InstantiatePrefab(asset);
        modelo.transform.SetPositionAndRotation(donde, Quaternion.Euler(0f, rumbo, 0f));
        modelo.transform.localScale = Vector3.one * 1.2f;
        var animador = modelo.GetComponent<Animator>();
        animador.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        poses.Add(new Pose { objeto = animador.gameObject, clip = Clip("m_pistol_run"), momento = 0.3f });

        var mano = animador.isHuman ? animador.GetBoneTransform(HumanBodyBones.RightHand) : null;
        var contenedor = mano != null ? mano.Find("R_hand_container") : null;
        if (contenedor == null) contenedor = mano;
        var pistola = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Pistola.prefab");
        if (contenedor != null && pistola != null)
        {
            foreach (Transform hijo in contenedor) hijo.gameObject.SetActive(false);
            var p = (GameObject)PrefabUtility.InstantiatePrefab(pistola, contenedor);
            p.transform.localPosition = Vector3.zero;
            p.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            p.transform.localScale = Vector3.one;
            var fogonazo = p.transform.Find("Boca/Fogonazo");
            if (fogonazo != null) fogonazo.gameObject.SetActive(true);
        }
        Disfraces.PonerSombrero(Disfraces.Cabeza(modelo.transform));
        Personajes.PonerEnLaCapa(modelo);
    }

    // Un zombi de la horda mirando al jugador, a mitad del paso, con su disfraz.
    static void Horda(string ruta, Vector3 donde, float disfraz, float momento, bool jefe = false)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
        if (prefab == null) return;
        var zombi = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        var hacia = new Vector3(0f, 0f, 0.2f) - donde;
        zombi.transform.SetPositionAndRotation(donde, Quaternion.LookRotation(new Vector3(hacia.x, 0f, hacia.z)));
        var capsula = zombi.GetComponent<CapsuleCollider>();
        if (capsula != null)
        {
            float alto = capsula.direction == 1 ? Mathf.Max(capsula.height * 0.5f, capsula.radius) : capsula.radius;
            float fondo = zombi.transform.TransformPoint(capsula.center).y - alto * Mathf.Abs(zombi.transform.lossyScale.y);
            zombi.transform.position += Vector3.up * -fondo;
        }
        Disfraces.Vestir(zombi, jefe, disfraz);
        // El ritmo de cada tipo (el tanque y el jefe caminan) es un campo privado del prefab.
        var enemigo = zombi.GetComponent<EnemyController>();
        float ritmo = 1f;
        if (enemigo != null)
        {
            var campo = new SerializedObject(enemigo).FindProperty("ritmoDeAndar");
            if (campo != null) ritmo = campo.floatValue;
        }
        foreach (var a in zombi.GetComponentsInChildren<Animator>(true))
        {
            if (a.runtimeAnimatorController == null) continue;
            a.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            // Los que caminan (el tanque y el jefe) con el clip de caminar; el resto corre. Los
            // clips sin root motion: con el _rm, el modelo se correria de su lugar.
            poses.Add(new Pose { objeto = a.gameObject, clip = Clip(ritmo < 0.5f ? "Z_walk" : "Z_run"), momento = momento });
        }
        Personajes.PonerEnLaCapa(zombi);
    }

    // Todos juntos, en un solo muestreo: el pose queda mientras dure el modo de animacion.
    static void Posar()
    {
        AnimationMode.StartAnimationMode();
        AnimationMode.BeginSampling();
        foreach (var p in poses)
            if (p.objeto != null && p.clip != null) AnimationMode.SampleAnimationClip(p.objeto, p.clip, p.momento * p.clip.length);
        AnimationMode.EndSampling();
    }

    static AnimationClip Clip(string nombre)
    {
        foreach (var guid in AssetDatabase.FindAssets(nombre + " t:AnimationClip"))
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(guid)))
            {
                var clip = o as AnimationClip;
                if (clip != null && clip.name == nombre) return clip;
            }
        Debug.LogWarning("ArteDeHalloween: no esta el clip " + nombre);
        return null;
    }

    static void Calabaza(string ruta, Vector3 donde, float giro, float escala)
    {
        var prefab = Resources.Load<GameObject>(ruta);
        if (prefab == null) return;
        var c = Object.Instantiate(prefab);
        c.transform.SetPositionAndRotation(donde, Quaternion.Euler(0f, giro, 0f));
        c.transform.localScale = Vector3.one * escala;
        Personajes.PonerEnLaCapa(c);
    }

    static void Botin(string ruta, Vector3[] lugares)
    {
        var prefab = Resources.Load<GameObject>(ruta);
        if (prefab == null) return;
        foreach (var p in lugares)
        {
            var c = Object.Instantiate(prefab);
            c.transform.SetPositionAndRotation(p, Quaternion.Euler(-20f, Random.Range(-30f, 30f), 0f));
            Personajes.PonerEnLaCapa(c);
        }
    }

    static void Foto(Camera camara, int ancho, int alto, string nombre)
    {
        var rt = new RenderTexture(ancho, alto, 24) { antiAliasing = 8, hideFlags = HideFlags.HideAndDontSave };
        camara.targetTexture = rt;
        camara.Render();
        camara.Render();
        var antes = RenderTexture.active;
        RenderTexture.active = rt;
        var foto = new Texture2D(ancho, alto, TextureFormat.RGB24, false);
        foto.ReadPixels(new Rect(0, 0, ancho, alto), 0, 0);
        foto.Apply();
        RenderTexture.active = antes;
        camara.targetTexture = null;
        rt.Release();
        Object.DestroyImmediate(rt);
        File.WriteAllBytes(Path.GetFullPath(Carpeta + "/" + nombre + ".png"), foto.EncodeToPNG());
        Object.DestroyImmediate(foto);
    }
}
