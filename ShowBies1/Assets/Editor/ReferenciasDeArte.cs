using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Las referencias de los personajes para hacer arte afuera del juego (la tarjeta del evento con
// Gemini, 9/10): cada cosa sola, sobre un fondo gris parejo y con luz de estudio, sin la noche,
// para que se lean las formas y los colores de verdad. El jugador de perfil apuntando, con la
// pistola y el sombrero de calabaza; cada zombi con su disfraz; la calabaza farol, la chica, el
// caramelo y la moneda. Todo en una escena vacia, posado con AnimationMode como ArteDeHalloween,
// y cada foto encuadrada sobre lo que mide. Va a Builds/referencias/.
public static class ReferenciasDeArte
{
    const string Carpeta = "../Builds/referencias";
    const string Zombi = "Assets/Prefabs/Personajes/Zombi.prefab";
    const string Rapido = "Assets/Prefabs/Personajes/ZombiRapido.prefab";
    const string Faster = "Assets/Prefabs/Personajes/ZombiFASTER.prefab";
    const string Tanque = "Assets/Prefabs/Personajes/ZombiTanque.prefab";
    const string Jefe = "Assets/Prefabs/Personajes/ZombiBOSS.prefab";

    static readonly Color Fondo = new Color(0.46f, 0.47f, 0.5f);

    struct Pose
    {
        public GameObject objeto;
        public AnimationClip clip;
        public float momento;
    }

    static readonly List<Pose> poses = new List<Pose>();

    // Las hojas de modelo: cada personaje de frente, de tres cuartos, de perfil y de espaldas,
    // quieto, para que una IA los copie tal cual (la tarjeta del evento, 9/10).
    struct Vista
    {
        public GameObject grupo;
        public string archivo;
    }

    static readonly List<Vista> vistas = new List<Vista>();
    static int lugares;

    static readonly string[] NombresDeVistas = { "frente", "tres_cuartos", "perfil", "espalda" };
    static readonly float[] RumbosDeVistas = { 180f, 135f, 90f, 0f };

    static void Hoja(string nombre, System.Action<Transform, Vector3, float> poner)
    {
        for (int i = 0; i < NombresDeVistas.Length; i++)
        {
            var g = Grupo(nombre + "_" + NombresDeVistas[i], 20f + lugares++);
            poner(g.transform, g.transform.position, RumbosDeVistas[i]);
            vistas.Add(new Vista { grupo = g, archivo = "modelo_" + nombre + "_" + NombresDeVistas[i] });
        }
    }

    [MenuItem("ShowBies/Halloween/Referencias para arte")]
    public static void Sacar()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EscenasSinGuardar.Hay("Referencias de arte")) return;
        var abiertas = EscenasSinGuardar.Recordar();
        int calidad = QualitySettings.GetQualityLevel();
        Directory.CreateDirectory(Path.GetFullPath(Carpeta));
        try
        {
            QualitySettings.SetQualityLevel(QualitySettings.names.Length - 1, true);
            poses.Clear();
            vistas.Clear();
            lugares = 0;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Estudio();

            // Cada uno lejos de los otros, para que ninguna foto vea al vecino. El jugador mira
            // a la derecha de la foto y los zombis a la izquierda, como se van a enfrentar.
            var perfil = Grupo("Perfil", 0f);
            Jugador(perfil.transform, perfil.transform.position, 90f, "m_pistol_shoot", 0.35f);
            var tresCuartos = Grupo("TresCuartos", 1f);
            Jugador(tresCuartos.transform, tresCuartos.transform.position, 135f, "m_pistol_shoot", 0.35f);
            var frente = Grupo("Frente", 2f);
            Jugador(frente.transform, frente.transform.position, 180f, "m_pistol_idle_A", 0.3f);
            var normal = Grupo("Normal", 3f);
            Horda(normal.transform, Zombi, normal.transform.position.x, 0.1f, 0.2f, false, -125f);
            var rapido = Grupo("Rapido", 4f);
            Horda(rapido.transform, Rapido, rapido.transform.position.x, 0.45f, 0.55f, false, -125f);
            var veloz = Grupo("Veloz", 5f);
            Horda(veloz.transform, Faster, veloz.transform.position.x, 0.9f, 0.35f, false, -125f);
            var tanque = Grupo("Tanque", 6f);
            Horda(tanque.transform, Tanque, tanque.transform.position.x, 0.1f, 0.6f, false, -125f);
            var jefe = Grupo("Jefe", 7f);
            Horda(jefe.transform, Jefe, jefe.transform.position.x, 0f, 0.3f, true, -125f);

            var tesoro = Grupo("Tesoro", 8f);
            var prefabTesoro = Resources.Load<GameObject>(WaveManager.RutaTesoro);
            if (prefabTesoro != null)
                Horda(tesoro.transform, AssetDatabase.GetAssetPath(prefabTesoro), tesoro.transform.position.x, 0.9f, 0.4f, false, -125f);

            // Las tres cajas, como se ven en la partida: el dibujo esta de frente a la camara del
            // juego, que mira desde arriba, asi que se sacan desde ahi.
            var cajas = Grupo("Cajas", 9f);
            float xc = cajas.transform.position.x;
            foreach (var ruta in new[] { "Assets/Prefabs/PUVida.prefab", "Assets/Prefabs/PUBalas.prefab", "Assets/Prefabs/PUArma.prefab" })
            {
                var caja = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
                if (caja == null) continue;
                var c = (GameObject)PrefabUtility.InstantiatePrefab(caja, cajas.transform);
                c.transform.SetPositionAndRotation(new Vector3(xc, 0.6f, 0f), Quaternion.identity);
                foreach (var r in c.GetComponentsInChildren<Renderer>(true))
                    if (r.name == "Charco") r.enabled = false;
                xc += 1.8f;
            }

            var cosas = new GameObject("Cosas");
            cosas.transform.position = new Vector3(400f, 0f, 0f);
            Suelto(cosas.transform, DecoradoHalloween.RutaCalabaza, new Vector3(400f, 0f, 0f), 180f, 1.2f);
            Suelto(cosas.transform, DecoradoHalloween.RutaCalabazaChica, new Vector3(401.6f, 0f, 0.2f), 160f, 1.3f);
            Suelto(cosas.transform, "Halloween/Caramelo", new Vector3(402.9f, 0.35f, 0f), 180f, 2f);
            var moneda = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Moneda.prefab");
            if (moneda != null)
            {
                var m = (GameObject)PrefabUtility.InstantiatePrefab(moneda, cosas.transform);
                m.transform.SetPositionAndRotation(new Vector3(404.1f, 0.35f, 0f), Quaternion.Euler(0f, 180f, 0f));
                m.transform.localScale = Vector3.one * 2f;
            }

            Hoja("heroe", (p, donde, rumbo) => Jugador(p, donde, rumbo, "m_pistol_idle_A", 0.3f));
            Hoja("heroe_disparando", (p, donde, rumbo) => Jugador(p, donde, rumbo, "m_pistol_shoot", 0.35f));
            Hoja("zombi_calabaza", (p, donde, rumbo) => Horda(p, Zombi, donde.x, 0.1f, 0.3f, false, rumbo, "Z_idle_A"));
            Hoja("zombi_bruja", (p, donde, rumbo) => Horda(p, Rapido, donde.x, 0.45f, 0.3f, false, rumbo, "Z_idle_A"));
            Hoja("zombi_veloz", (p, donde, rumbo) => Horda(p, Faster, donde.x, 0.9f, 0.3f, false, rumbo, "Z_idle_A"));
            Hoja("zombi_grandote_calabaza", (p, donde, rumbo) => Horda(p, Tanque, donde.x, 0.1f, 0.3f, false, rumbo, "Z_idle_A"));
            Hoja("zombi_grandote", (p, donde, rumbo) => Horda(p, Tanque, donde.x, 0.9f, 0.3f, false, rumbo, "Z_idle_A"));
            Hoja("jefe", (p, donde, rumbo) => Horda(p, Jefe, donde.x, 0f, 0.3f, true, rumbo, "Z_idle_A"));
            if (prefabTesoro != null)
                Hoja("tesoro", (p, donde, rumbo) => Horda(p, AssetDatabase.GetAssetPath(prefabTesoro), donde.x, 0.9f, 0.3f, false, rumbo, "Z_idle_A"));

            Posar();

            var camara = new GameObject("Camara").AddComponent<Camera>();
            camara.clearFlags = CameraClearFlags.SolidColor;
            camara.backgroundColor = Fondo;
            camara.allowHDR = false;
            camara.fieldOfView = 24f;
            camara.farClipPlane = 300f;

            Foto(camara, perfil, Vector3.back, 0.1f, 1200, 1200, "jugador_perfil");
            Foto(camara, tresCuartos, Vector3.back, 0.1f, 1200, 1200, "jugador_tres_cuartos");
            Foto(camara, frente, Vector3.back, 0.1f, 1200, 1200, "jugador_frente");
            Foto(camara, normal, Vector3.back, 0.1f, 1200, 1200, "zombi_normal");
            Foto(camara, rapido, Vector3.back, 0.1f, 1200, 1200, "zombi_rapido");
            Foto(camara, veloz, Vector3.back, 0.1f, 1200, 1200, "zombi_veloz");
            Foto(camara, tanque, Vector3.back, 0.1f, 1200, 1200, "zombi_tanque");
            Foto(camara, jefe, Vector3.back, 0.1f, 1200, 1200, "zombi_jefe");
            Foto(camara, tesoro, Vector3.back, 0.1f, 1200, 1200, "zombi_tesoro");
            Foto(camara, cajas, -(ConstructorEscenarios.GiroDeLaCamara * Vector3.forward), 0.15f, 1800, 900, "cajas");
            foreach (var v in vistas) Foto(camara, v.grupo, Vector3.back, 0.08f, 900, 900, v.archivo);
            Foto(camara, cosas, (Vector3.back + Vector3.up * 0.35f).normalized, 0.1f, 2000, 1000, "calabazas_caramelo_moneda");
        }
        finally
        {
            if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
            QualitySettings.SetQualityLevel(calidad, true);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EscenasSinGuardar.Volver(abiertas);
        }
        Debug.Log("ReferenciasDeArte: listas en " + Path.GetFullPath(Carpeta));
    }

    static GameObject Grupo(string nombre, float lugar)
    {
        var g = new GameObject(nombre);
        g.transform.position = new Vector3(lugar * 40f, 0f, 0f);
        return g;
    }

    // Luz de estudio: una principal de adelante y arriba, un relleno del otro lado y un
    // contraluz, con luz ambiente pareja. Sin niebla.
    static void Estudio()
    {
        RenderSettings.fog = false;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.58f);
        Luz(new Vector3(35f, 30f, 0f), 1.05f, Color.white, true);
        Luz(new Vector3(20f, -40f, 0f), 0.45f, new Color(0.9f, 0.93f, 1f), false);
        Luz(new Vector3(25f, 160f, 0f), 0.6f, Color.white, false);
    }

    static void Luz(Vector3 giro, float intensidad, Color color, bool sombras)
    {
        var luz = new GameObject("Luz").AddComponent<Light>();
        luz.type = LightType.Directional;
        luz.transform.rotation = Quaternion.Euler(giro);
        luz.intensity = intensidad;
        luz.color = color;
        luz.shadows = sombras ? LightShadows.Soft : LightShadows.None;
        luz.renderMode = LightRenderMode.ForcePixel;
    }

    static void Jugador(Transform padre, Vector3 donde, float rumbo, string clip, float momento)
    {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ToonyTinyPeople/TT_demo/models/TT_demo_male_A.FBX");
        // El clip pisa el giro de la raiz del modelo al muestrearlo (por eso en la foto vieja
        // salia de espaldas): lo que gira es un pivote del que cuelga.
        var pivote = new GameObject("Pivote").transform;
        pivote.SetParent(padre, false);
        pivote.SetPositionAndRotation(donde, Quaternion.Euler(0f, rumbo, 0f));
        var modelo = (GameObject)PrefabUtility.InstantiatePrefab(asset, pivote);
        modelo.transform.localPosition = Vector3.zero;
        modelo.transform.localRotation = Quaternion.identity;
        modelo.transform.localScale = Vector3.one * 1.2f;
        var animador = modelo.GetComponent<Animator>();
        animador.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        poses.Add(new Pose { objeto = animador.gameObject, clip = Clip(clip), momento = momento });

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
        }
        Disfraces.PonerSombrero(Disfraces.Cabeza(modelo.transform));
    }

    // Un zombi en el piso, mirando hacia la izquierda de la foto. Devuelve donde termina.
    static float Horda(Transform padre, string ruta, float x, float disfraz, float momento, bool jefe = false, float rumbo = -110f, string clip = null)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
        if (prefab == null) return x;
        var pivote = new GameObject("Pivote").transform;
        pivote.SetParent(padre, false);
        pivote.SetPositionAndRotation(new Vector3(x, 0f, 0f), Quaternion.Euler(0f, rumbo, 0f));
        var zombi = (GameObject)PrefabUtility.InstantiatePrefab(prefab, pivote);
        zombi.transform.localPosition = Vector3.zero;
        zombi.transform.localRotation = Quaternion.identity;
        var capsula = zombi.GetComponent<CapsuleCollider>();
        float radio = 0.5f;
        if (capsula != null)
        {
            float alto = capsula.direction == 1 ? Mathf.Max(capsula.height * 0.5f, capsula.radius) : capsula.radius;
            float fondo = zombi.transform.TransformPoint(capsula.center).y - alto * Mathf.Abs(zombi.transform.lossyScale.y);
            zombi.transform.position += Vector3.up * -fondo;
            radio = capsula.radius * Mathf.Abs(zombi.transform.lossyScale.x);
        }

        Disfraces.Vestir(zombi, jefe, disfraz);
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
            poses.Add(new Pose { objeto = a.gameObject, clip = Clip(clip ?? (ritmo < 0.5f ? "Z_walk" : "Z_run")), momento = momento });
        }
        return zombi.transform.position.x + radio * 2f;
    }

    static void Suelto(Transform padre, string ruta, Vector3 donde, float giro, float escala)
    {
        var prefab = Resources.Load<GameObject>(ruta);
        if (prefab == null) return;
        var c = Object.Instantiate(prefab, padre);
        c.transform.SetPositionAndRotation(donde, Quaternion.Euler(0f, giro, 0f));
        c.transform.localScale = Vector3.one * escala;
        // El charco del piso y el halo son para la noche: en el estudio solo ensucian.
        foreach (var r in c.GetComponentsInChildren<Renderer>(true))
            if (r.name == "Charco" || r.name == "Halo") r.enabled = false;
    }

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
        Debug.LogWarning("ReferenciasDeArte: no esta el clip " + nombre);
        return null;
    }

    // Encuadra el grupo desde una direccion (la camara mira hacia -desde) con un margen, y saca
    // la foto. Lo que se mide son los renderers prendidos, sin los colliders apagados.
    static void Foto(Camera camara, GameObject grupo, Vector3 desde, float margen, int ancho, int alto, string nombre)
    {
        Bounds caja = default;
        bool hay = false;
        foreach (var r in grupo.GetComponentsInChildren<Renderer>())
        {
            if (!r.enabled || r is ParticleSystemRenderer) continue;
            if (hay) caja.Encapsulate(r.bounds); else { caja = r.bounds; hay = true; }
        }
        if (!hay) return;

        float aspecto = (float)ancho / alto;
        camara.aspect = aspecto;
        float tan = Mathf.Tan(camara.fieldOfView * 0.5f * Mathf.Deg2Rad);
        var giro = Quaternion.LookRotation(-desde);
        // El tamaño visto desde la camara: la caja llevada a sus ejes.
        var derecha = giro * Vector3.right;
        var arriba = giro * Vector3.up;
        float medioAncho = 0f, medioAlto = 0f, fondo = 0f;
        for (int i = 0; i < 8; i++)
        {
            var esquina = caja.center + Vector3.Scale(caja.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1)) - caja.center;
            medioAncho = Mathf.Max(medioAncho, Mathf.Abs(Vector3.Dot(esquina, derecha)));
            medioAlto = Mathf.Max(medioAlto, Mathf.Abs(Vector3.Dot(esquina, arriba)));
            fondo = Mathf.Max(fondo, Mathf.Abs(Vector3.Dot(esquina, desde)));
        }
        float distancia = Mathf.Max(medioAlto, medioAncho / aspecto) * (1f + margen * 2f) / tan + fondo;
        camara.transform.SetPositionAndRotation(caja.center + desde * distancia, giro);

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
