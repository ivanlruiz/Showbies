using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Fotos de lo que arma ConstructorHalloween, sin entrar en play: los zombis disfrazados y el
// jugador con el sombrero, con la camara del juego y de cerca; las calabazas y la horda del
// menu con su camara; una calabaza farol y un caramelo de cerca; y WaveMode con las calabazas
// por el mapa. Van a Builds/halloween/. Sirve para mirar a ojo que nada flote, se hunda o
// quede de espaldas, que es lo que no ve una prueba. Los modelos salen en su pose de reposo:
// sin play el Animator no corre, y el disfraz va colgado del hueso de la cabeza igual.
//
// Abre escenas en modo Single: se niega si hay alguna sin guardar (EscenasSinGuardar), y al
// terminar vuelve a lo que estaba abierto. WaveMode se abre, se le ponen las calabazas y se
// descarta sin guardar.
public static class FotosDeHalloween
{
    const int Ancho = 1600, Alto = 900;
    const string Carpeta = "../Builds/halloween";

    static readonly string[] Zombis =
    {
        "Assets/Prefabs/Personajes/Zombi.prefab",
        "Assets/Prefabs/Personajes/ZombiRapido.prefab",
        "Assets/Prefabs/Personajes/ZombiFASTER.prefab",
        "Assets/Prefabs/Personajes/ZombiTanque.prefab",
        "Assets/Prefabs/Personajes/ZombiBOSS.prefab",
    };

    // Para la linea de comandos: arma todo y saca las fotos en una sola corrida.
    public static void ArmarYSacar()
    {
        ConstructorHalloween.Armar();
        AssetDatabase.Refresh();
        Sacar();
    }

    [MenuItem("ShowBies/Halloween/Fotos")]
    public static void Sacar()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EscenasSinGuardar.Hay("Fotos de Halloween")) return;
        var abiertas = EscenasSinGuardar.Recordar();
        Directory.CreateDirectory(Path.GetFullPath(Carpeta));
        // Sin esto, abrir otra escena la descarga como un asset sin usar.
        var rt = new RenderTexture(Ancho, Alto, 24) { hideFlags = HideFlags.HideAndDontSave };
        try
        {
            Disfrazados(rt);
            Menu(rt);
            Partida(rt);
        }
        finally
        {
            rt.Release();
            Object.DestroyImmediate(rt);
            EscenasSinGuardar.Volver(abiertas);
        }
        Debug.Log("FotosDeHalloween: listas en " + Path.GetFullPath(Carpeta));
    }

    // Una escena vacia de noche: el piso de la pradera, la luna, la luz de relleno de los
    // personajes (capa 8) y la luz ambiente de la pradera.
    static void Noche()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var piso = GameObject.CreatePrimitive(PrimitiveType.Plane);
        piso.transform.localScale = new Vector3(10f, 1f, 10f);
        var material = AssetDatabase.LoadAssetAtPath<Material>(ConstructorEscenarios.RutaPisoPradera);
        if (material != null) piso.GetComponent<Renderer>().sharedMaterial = material;

        var luna = new GameObject("Luna").AddComponent<Light>();
        luna.type = LightType.Directional;
        luna.color = new Color(0.55f, 0.66f, 1f);
        luna.intensity = 0.5f;
        luna.shadows = LightShadows.Soft;
        luna.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        var relleno = new GameObject("Relleno").AddComponent<Light>();
        relleno.type = LightType.Directional;
        relleno.color = new Color(0.8f, 0.85f, 1f);
        relleno.intensity = 0.95f;
        relleno.cullingMask = 1 << Personajes.Capa;
        relleno.renderMode = LightRenderMode.ForceVertex;
        relleno.transform.rotation = ConstructorEscenarios.GiroDeLaCamara;

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.15f, 0.19f, 0.3f);
        RenderSettings.fog = false;
    }

    static Camera Camara(Vector3 posicion, Quaternion giro, float fov = 60f)
    {
        var camara = new GameObject("Camara").AddComponent<Camera>();
        camara.transform.SetPositionAndRotation(posicion, giro);
        camara.fieldOfView = fov;
        camara.clearFlags = CameraClearFlags.SolidColor;
        camara.backgroundColor = new Color(0.02f, 0.04f, 0.1f);
        camara.nearClipPlane = 0.1f;
        camara.farClipPlane = 200f;
        return camara;
    }

    // Un zombi de la partida, con los pies en el piso como en FondoMenu.
    static GameObject Zombi(string ruta, Vector3 donde, float rumbo)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
        if (prefab == null) return null;
        var zombi = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        zombi.transform.SetPositionAndRotation(donde, Quaternion.Euler(0f, rumbo, 0f));
        var capsula = zombi.GetComponent<CapsuleCollider>();
        if (capsula != null)
        {
            float alto = capsula.direction == 1 ? Mathf.Max(capsula.height * 0.5f, capsula.radius) : capsula.radius;
            float fondo = zombi.transform.TransformPoint(capsula.center).y - alto * Mathf.Abs(zombi.transform.lossyScale.y);
            zombi.transform.position += Vector3.up * -fondo;
        }
        Personajes.PonerEnLaCapa(zombi);
        return zombi;
    }

    // Una fila de zombis (cada tipo con calabaza, sombrero y sin nada) y el jugador con su
    // sombrero.
    static void Disfrazados(RenderTexture rt)
    {
        Noche();

        var jugadorAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ToonyTinyPeople/TT_demo/models/TT_demo_male_A.FBX");
        var jugador = (GameObject)PrefabUtility.InstantiatePrefab(jugadorAsset);
        jugador.transform.SetPositionAndRotation(new Vector3(0f, 0f, -1.5f), Quaternion.Euler(0f, 160f, 0f));
        jugador.transform.localScale = Vector3.one * 1.2f;
        var sombrero = Disfraces.PonerSombrero(Disfraces.Cabeza(jugador.transform));
        Personajes.PonerEnLaCapa(jugador);

        float[] azares = { 0.1f, 0.45f, 0.9f };
        for (int t = 0; t < Zombis.Length; t++)
        {
            for (int v = 0; v < azares.Length; v++)
            {
                bool jefe = t == Zombis.Length - 1;
                if (jefe && v > 0) continue;
                var donde = new Vector3(-6f + t * 3f, 0f, 1.5f + v * 2.6f);
                var zombi = Zombi(Zombis[t], donde, 180f);
                if (zombi == null) continue;
                Disfraces.Vestir(zombi, jefe, azares[v]);
                Personajes.PonerEnLaCapa(zombi);
            }
        }

        const string sufijo = "";
        var juego = Camara(new Vector3(0f, 12.28f, -3.9f - 1.5f + 3f), ConstructorEscenarios.GiroDeLaCamara);
        Foto(juego, rt, "juego" + sufijo);
        Object.DestroyImmediate(juego.gameObject);
        var cerca = Camara(new Vector3(0f, 4.5f, -7.5f), Quaternion.LookRotation(new Vector3(0f, 1f, 2.5f) - new Vector3(0f, 4.5f, -7.5f)), 45f);
        Foto(cerca, rt, "cerca" + sufijo);
        Object.DestroyImmediate(cerca.gameObject);
        // El sombrero del jugador, bien de cerca y de frente.
        if (sombrero != null)
        {
            var cabeza = sombrero.transform.parent.position;
            var retrato = Camara(cabeza + new Vector3(0.6f, 0.5f, -1.8f), Quaternion.LookRotation(cabeza - (cabeza + new Vector3(0.6f, 0.5f, -1.8f))), 35f);
            Foto(retrato, rt, "sombrero" + sufijo);
            Object.DestroyImmediate(retrato.gameObject);
        }
    }

    // El menu: su camara, las calabazas de los costados y unos zombis disfrazados en el camino.
    static void Menu(RenderTexture rt)
    {
        Noche();
        DecoradoHalloween.EnElMenu(default(UnityEngine.SceneManagement.Scene), Quaternion.Euler(17f, 0f, 0f));
        for (int i = 0; i < 5; i++)
        {
            var zombi = Zombi(Zombis[i % 4], new Vector3(-6f + i * 3f, 0f, 1f + (i % 3) * 2.2f), i % 2 == 0 ? 90f : -90f);
            if (zombi != null) Disfraces.Vestir(zombi, false, i % 2 == 0 ? 0.1f : 0.45f);
        }
        var camara = Camara(new Vector3(0f, 3.4f, -6.5f), Quaternion.Euler(17f, 0f, 0f));
        Foto(camara, rt, "menu");

        // Una calabaza farol de cerca y de frente, un poco desde arriba.
        var calabaza = Object.Instantiate(Resources.Load<GameObject>(DecoradoHalloween.RutaCalabaza));
        calabaza.transform.position = new Vector3(0f, 0f, -4.5f);
        camara.transform.SetPositionAndRotation(new Vector3(0f, 2.2f, -6.6f), Quaternion.LookRotation(new Vector3(0f, -1.6f, 2.1f)));
        camara.fieldOfView = 30f;
        var caramelo = Object.Instantiate(Resources.Load<GameObject>("Halloween/Caramelo"));
        caramelo.transform.SetPositionAndRotation(new Vector3(0.95f, 0.3f, -4.9f), camara.transform.rotation * Quaternion.Euler(0f, 30f, 0f));
        Foto(camara, rt, "calabaza_frente");
    }

    // WaveMode de verdad, con la noche de la escena, y las calabazas por el mapa: con la camara
    // del juego y desde muy arriba, para ver como se reparten.
    static void Partida(RenderTexture rt)
    {
        var escena = EditorSceneManager.OpenScene("Assets/Escenas/WaveMode.unity", OpenSceneMode.Single);
        DecoradoHalloween.EnLaPartida(escena);
        var camara = Camara(new Vector3(0f, 12.28f, -3.9f), ConstructorEscenarios.GiroDeLaCamara);
        camara.backgroundColor = RenderSettings.fogColor;
        Foto(camara, rt, "partida");
        camara.transform.SetPositionAndRotation(new Vector3(0f, 95f, -35f), Quaternion.Euler(70f, 0f, 0f));
        camara.farClipPlane = 400f;
        RenderSettings.fog = false;
        Foto(camara, rt, "partida_mapa");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }

    // Dos veces: la primera evaluacion no mueve los huesos hasta que algo renderiza.
    static void Foto(Camera camara, RenderTexture rt, string nombre)
    {
        camara.targetTexture = rt;
        camara.Render();
        camara.Render();
        var antes = RenderTexture.active;
        RenderTexture.active = rt;
        var foto = new Texture2D(Ancho, Alto, TextureFormat.RGB24, false);
        foto.ReadPixels(new Rect(0, 0, Ancho, Alto), 0, 0);
        foto.Apply();
        RenderTexture.active = antes;
        camara.targetTexture = null;
        File.WriteAllBytes(Path.GetFullPath(Carpeta + "/" + nombre + ".png"), foto.EncodeToPNG());
        Object.DestroyImmediate(foto);
    }
}
