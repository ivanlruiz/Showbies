using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Los retratos del bestiario (de Argon, 19/10): cada zombi de tres cuartos, cabeza y hombros, recortado en un circulo,
// para la tarjeta de VentanaBestiario, y su silueta (blanca, con el mismo recorte) para la tarjeta tapada del que
// todavia no se mato. Sin disfraz (el bestiario no cambia con Halloween), con la luz de estudio de ReferenciasDeArte y
// posados con el clip de quieto, como las hojas de modelo, en una escena vacia. El fondo transparente sale de sacar cada
// foto dos veces, sobre negro y sobre blanco: la piel de los zombis no escribe un alfa parejo. Van a
// Sprites/UI/Resources/Retratos, de donde VentanaBestiario los carga por el nombre del tipo.
public static class RetratosDelBestiario
{
    public const string Carpeta = "Assets/Sprites/UI/Resources/Retratos";
    public const int Lado = 192;

    // De tres cuartos: el zombi mira hacia la derecha de la foto y un poco hacia la camara.
    const float Rumbo = 145f;
    // Lo que entra, en altos de la cabeza (la malla Z_head del pack, la misma en los diez) desde su centro: hasta los
    // hombros abajo, y arriba lo que lleve colgado de la cabeza (las crestas del saltador, el casco del blindado, el
    // totem del chaman), sin pasar de un alto total; si lo pasa, se cortan los hombros (el chaman: el cristal del totem
    // es lo suyo).
    const float HastaLosHombros = 0.85f;
    const float ArribaSinNada = 0.7f;
    const float AltoMaximo = 2.4f;

    static readonly Dictionary<string, string> Prefabs = new Dictionary<string, string>
    {
        { Bestiario.Normal, "Assets/Prefabs/Personajes/Zombi.prefab" },
        { Bestiario.Rapido, "Assets/Prefabs/Personajes/ZombiRapido.prefab" },
        { Bestiario.Faster, "Assets/Prefabs/Personajes/ZombiFASTER.prefab" },
        { Bestiario.Tanque, "Assets/Prefabs/Personajes/ZombiTanque.prefab" },
        { Bestiario.Jefe, "Assets/Prefabs/Personajes/ZombiBOSS.prefab" },
        { Bestiario.Escupidor, ConstructorZombis.RutaEscupidor },
        { Bestiario.Saltador, ConstructorZombis.RutaSaltador },
        { Bestiario.Chaman, ConstructorZombis.RutaChaman },
        { Bestiario.Blindado, ConstructorZombis.RutaBlindado },
        { Bestiario.Hinchado, ConstructorZombis.RutaHinchado },
    };

    public static string RutaDelPrefab(string tipo)
    {
        string ruta;
        return Prefabs.TryGetValue(tipo, out ruta) ? ruta : null;
    }

    struct Puesto
    {
        public string tipo;
        public GameObject zombi;
        public ZombiHinchado hinchado;
        public Vector3 piezasDesdeElTorso;
    }

    [MenuItem("ShowBies/Zombis/Retratos del bestiario")]
    public static void Sacar()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EscenasSinGuardar.Hay("Retratos del bestiario")) return;
        var abiertas = EscenasSinGuardar.Recordar();
        int calidad = QualitySettings.GetQualityLevel();
        Directory.CreateDirectory(Path.GetFullPath(Carpeta));
        var escritos = new List<string>();
        try
        {
            QualitySettings.SetQualityLevel(QualitySettings.names.Length - 1, true);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Estudio();

            var puestos = new List<Puesto>();
            var animadores = new List<Animator>();
            var quieto = Clip("Z_idle_A");
            for (int i = 0; i < Bestiario.Tipos.Length; i++)
            {
                string tipo = Bestiario.Tipos[i];
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaDelPrefab(tipo));
                if (prefab == null) { Debug.LogError("RetratosDelBestiario: no esta el prefab de " + tipo); continue; }
                var zombi = Poner(prefab, new Vector3(i * 40f, 0f, 0f));
                var puesto = new Puesto { tipo = tipo, zombi = zombi, hinchado = zombi.GetComponent<ZombiHinchado>() };
                // El tacho del hinchado sigue al torso en LateUpdate, que aca no corre: se lleva a mano despues de posar.
                if (puesto.hinchado != null && puesto.hinchado.piezas != null && puesto.hinchado.torso != null)
                    puesto.piezasDesdeElTorso = zombi.transform.InverseTransformVector(puesto.hinchado.piezas.position - puesto.hinchado.torso.position);
                puestos.Add(puesto);
                foreach (var a in zombi.GetComponentsInChildren<Animator>(true))
                    if (a.runtimeAnimatorController != null) { a.cullingMode = AnimatorCullingMode.AlwaysAnimate; animadores.Add(a); }
            }

            AnimationMode.StartAnimationMode();
            AnimationMode.BeginSampling();
            foreach (var a in animadores)
                if (quieto != null) AnimationMode.SampleAnimationClip(a.gameObject, quieto, 0.3f * quieto.length);
            AnimationMode.EndSampling();
            foreach (var p in puestos)
                if (p.hinchado != null && p.hinchado.piezas != null && p.hinchado.torso != null)
                    p.hinchado.piezas.position = p.hinchado.torso.position + p.zombi.transform.TransformVector(p.piezasDesdeElTorso);

            var camara = new GameObject("Camara").AddComponent<Camera>();
            camara.clearFlags = CameraClearFlags.SolidColor;
            camara.orthographic = true;
            camara.allowHDR = false;
            camara.allowMSAA = true;
            camara.nearClipPlane = 0.1f;
            camara.farClipPlane = 60f;
            foreach (var p in puestos)
            {
                string retrato = Carpeta + "/Retrato_" + p.tipo + ".png";
                string silueta = Carpeta + "/Silueta_" + p.tipo + ".png";
                if (Foto(camara, p.zombi, retrato, silueta))
                {
                    escritos.Add(retrato);
                    escritos.Add(silueta);
                }
            }
        }
        finally
        {
            if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
            QualitySettings.SetQualityLevel(calidad, true);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EscenasSinGuardar.Volver(abiertas);
        }

        AssetDatabase.Refresh();
        foreach (var ruta in escritos) Importar(ruta);
        Debug.Log("RetratosDelBestiario: " + escritos.Count / 2 + " retratos en " + Carpeta);
    }

    // Como un sprite de la interfaz, con mipmaps (en la tarjeta se ve a unos 90 px) y sin el limite de mipmaps del
    // telefono (globalTextureMipmapLimit 1: se veria el de 96 px estirado).
    static void Importar(string ruta)
    {
        AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceSynchronousImport);
        var importador = AssetImporter.GetAtPath(ruta) as TextureImporter;
        if (importador == null) return;
        importador.textureType = TextureImporterType.Sprite;
        importador.spriteImportMode = SpriteImportMode.Single;
        importador.mipmapEnabled = true;
        importador.alphaIsTransparency = true;
        importador.wrapMode = TextureWrapMode.Clamp;
        importador.maxTextureSize = 256;
        importador.ignoreMipmapLimit = true;
        importador.SaveAndReimport();
    }

    // Luz de estudio, la de ReferenciasDeArte un poco mas baja (la piel de los zombis tiene brillo propio y a esa luz se
    // quemaba): una principal de adelante y arriba, un relleno y un contraluz.
    static void Estudio()
    {
        RenderSettings.fog = false;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.42f, 0.42f, 0.45f);
        Luz(new Vector3(35f, 30f, 0f), 0.85f, Color.white);
        Luz(new Vector3(20f, -40f, 0f), 0.35f, new Color(0.9f, 0.93f, 1f));
        Luz(new Vector3(25f, 160f, 0f), 0.45f, Color.white);
    }

    static void Luz(Vector3 giro, float intensidad, Color color)
    {
        var luz = new GameObject("Luz").AddComponent<Light>();
        luz.type = LightType.Directional;
        luz.transform.rotation = Quaternion.Euler(giro);
        luz.intensity = intensidad;
        luz.color = color;
        luz.shadows = LightShadows.None;
        luz.renderMode = LightRenderMode.ForcePixel;
    }

    // El zombi parado en el piso, de un pivote girado (el clip pisa el giro de la raiz del modelo al muestrearlo), sin
    // los brillos del piso (el charco del blindado y del hinchado) ni las particulas.
    static GameObject Poner(GameObject prefab, Vector3 donde)
    {
        var pivote = new GameObject("Pivote").transform;
        pivote.SetPositionAndRotation(donde, Quaternion.Euler(0f, Rumbo, 0f));
        var zombi = (GameObject)PrefabUtility.InstantiatePrefab(prefab, pivote);
        zombi.transform.localPosition = Vector3.zero;
        zombi.transform.localRotation = Quaternion.identity;
        var capsula = zombi.GetComponent<CapsuleCollider>();
        if (capsula != null)
        {
            float alto = capsula.direction == 1 ? Mathf.Max(capsula.height * 0.5f, capsula.radius) : capsula.radius;
            float fondo = zombi.transform.TransformPoint(capsula.center).y - alto * Mathf.Abs(zombi.transform.lossyScale.y);
            zombi.transform.position += Vector3.up * -fondo;
        }
        foreach (var r in zombi.GetComponentsInChildren<Renderer>(true))
            if (!SeVe(r)) r.enabled = false;
        return zombi;
    }

    static bool SeVe(Renderer r)
    {
        if (r is ParticleSystemRenderer) return false;
        if (r.name == "Charco" || r.name == "Halo") return false;
        var m = r.sharedMaterial;
        return m == null || m.shader == null || m.shader.name != "ShowBies/CharcoDeLuz";
    }

    // La cabeza y los hombros, con la camara ortografica de frente y un poco de arriba, sobre negro y sobre blanco.
    static bool Foto(Camera camara, GameObject zombi, string rutaRetrato, string rutaSilueta)
    {
        Bounds caja = default;
        bool hay = false;
        foreach (var r in zombi.GetComponentsInChildren<Renderer>())
        {
            if (!r.enabled || !SeVe(r)) continue;
            if (hay) caja.Encapsulate(r.bounds); else { caja = r.bounds; hay = true; }
        }
        if (!hay) return false;

        // La cabeza y lo que cuelga de ella.
        var hueso = Disfraces.Cabeza(zombi.transform);
        Renderer malla = null;
        float piezas = float.MinValue;
        if (hueso != null)
            foreach (var r in hueso.GetComponentsInChildren<Renderer>())
            {
                if (!r.enabled || !SeVe(r)) continue;
                if (malla == null && r.name.StartsWith("Z_head")) malla = r;
                else piezas = Mathf.Max(piezas, r.bounds.max.y);
            }
        float arriba, abajo;
        Vector3 centro;
        if (malla != null)
        {
            var cabeza = malla.bounds;
            float h = cabeza.size.y;
            arriba = Mathf.Max(cabeza.center.y + h * ArribaSinNada, piezas + h * 0.05f);
            abajo = Mathf.Max(cabeza.center.y - h * HastaLosHombros, arriba - h * AltoMaximo);
            centro = new Vector3(cabeza.center.x, (arriba + abajo) * 0.5f, cabeza.center.z);
        }
        else
        {
            // Sin la malla del pack, de lo mas alto a poco mas de la mitad del cuerpo.
            arriba = caja.max.y + caja.size.y * 0.04f;
            abajo = caja.max.y - caja.size.y * 0.56f;
            centro = new Vector3(caja.center.x, (arriba + abajo) * 0.5f, caja.center.z);
        }
        Debug.Log("RetratosDelBestiario: " + zombi.name + (malla != null ? " cabeza de " + malla.bounds.size.y.ToString("0.00") + " m en " + malla.bounds.center.y.ToString("0.00") : " sin la malla de la cabeza")
                  + ", foto de " + abajo.ToString("0.00") + " a " + arriba.ToString("0.00"));
        var desde = new Vector3(0f, 0.18f, -1f).normalized;
        camara.transform.SetPositionAndRotation(centro + desde * 20f, Quaternion.LookRotation(-desde));
        camara.orthographicSize = (arriba - abajo) * 0.5f;
        camara.aspect = 1f;

        var negro = Render(camara, Color.black);
        var blanco = Render(camara, Color.white);
        var retrato = new Texture2D(Lado, Lado, TextureFormat.RGBA32, false);
        var silueta = new Texture2D(Lado, Lado, TextureFormat.RGBA32, false);
        var colores = new Color[Lado * Lado];
        var mascara = new Color[Lado * Lado];
        float radio = Lado * 0.5f;
        for (int y = 0; y < Lado; y++)
        {
            for (int x = 0; x < Lado; x++)
            {
                int i = y * Lado + x;
                Color n = negro[i], b = blanco[i];
                // Sobre negro sale color * alfa; sobre blanco, color * alfa + (1 - alfa).
                float alfa = Mathf.Clamp01(1f - ((b.r - n.r) + (b.g - n.g) + (b.b - n.b)) / 3f);
                // El recorte en circulo, con un borde de un pixel y medio.
                float dx = x + 0.5f - radio, dy = y + 0.5f - radio;
                float circulo = Mathf.Clamp01((radio - Mathf.Sqrt(dx * dx + dy * dy)) / 1.5f);
                Color c = alfa > 0.001f ? new Color(n.r / alfa, n.g / alfa, n.b / alfa) : Color.clear;
                c.a = alfa * circulo;
                colores[i] = c;
                mascara[i] = new Color(1f, 1f, 1f, c.a);
            }
        }
        retrato.SetPixels(colores);
        retrato.Apply();
        silueta.SetPixels(mascara);
        silueta.Apply();
        File.WriteAllBytes(Path.GetFullPath(rutaRetrato), retrato.EncodeToPNG());
        File.WriteAllBytes(Path.GetFullPath(rutaSilueta), silueta.EncodeToPNG());
        Object.DestroyImmediate(retrato);
        Object.DestroyImmediate(silueta);
        return true;
    }

    // Cuatro veces mas grande y achicado, para que los bordes salgan suaves.
    static Color[] Render(Camera camara, Color fondo)
    {
        const int Grande = Lado * 4;
        camara.backgroundColor = fondo;
        var rt = new RenderTexture(Grande, Grande, 24, RenderTextureFormat.ARGB32) { antiAliasing = 8, hideFlags = HideFlags.HideAndDontSave };
        camara.targetTexture = rt;
        camara.Render();
        camara.Render();
        var antes = RenderTexture.active;
        RenderTexture.active = rt;
        var foto = new Texture2D(Grande, Grande, TextureFormat.RGBA32, false);
        foto.ReadPixels(new Rect(0, 0, Grande, Grande), 0, 0);
        foto.Apply();
        RenderTexture.active = antes;
        camara.targetTexture = null;
        rt.Release();
        Object.DestroyImmediate(rt);

        var grande = foto.GetPixels();
        Object.DestroyImmediate(foto);
        var chico = new Color[Lado * Lado];
        for (int y = 0; y < Lado; y++)
            for (int x = 0; x < Lado; x++)
            {
                Color suma = Color.clear;
                for (int j = 0; j < 4; j++)
                    for (int k = 0; k < 4; k++)
                        suma += grande[(y * 4 + j) * Grande + x * 4 + k];
                chico[y * Lado + x] = suma / 16f;
            }
        return chico;
    }

    static AnimationClip Clip(string nombre)
    {
        foreach (var guid in AssetDatabase.FindAssets(nombre + " t:AnimationClip"))
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(guid)))
            {
                var clip = o as AnimationClip;
                if (clip != null && clip.name == nombre) return clip;
            }
        Debug.LogWarning("RetratosDelBestiario: no esta el clip " + nombre);
        return null;
    }
}
