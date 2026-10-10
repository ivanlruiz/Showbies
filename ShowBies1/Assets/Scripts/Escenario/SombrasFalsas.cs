using UnityEngine;
using UnityEngine.SceneManagement;

// Las sombras en el telefono (10/10: Ivan eligio entre maquetas las falsas, de la revision de
// Gemini). Las de verdad son una pasada mas de toda la escena desde la luna, y en el Redmi Note 8
// pesaban; desde la camara de arriba lo que se lee es la mancha bajo los pies, no la forma. En el
// telefono, al cargar cada escena, ninguna luz proyecta sombra, y cada personaje lleva debajo un
// disco oscuro (ShowBies/SombraFalsa, en Materiales/Resources/SombraFalsa): los zombis desde su
// Awake y el heroe desde AspectoDelHeroe. En PC quedan las de verdad y no hay discos.
//
// Se apagan las luces y no QualitySettings.shadows: cambiado en play en el editor, el ajuste de
// calidad queda escrito en ProjectSettings (ver la trampa de QualitySettings en CLAUDE.md).
public static class SombrasFalsas
{
    // El disco de un zombi mide esto por el radio de su capsula, de lado a lado: con 2, solo
    // el cuerpo; con mas, se lee como sombra.
    public const float DiametroPorRadio = 3.2f;
    // Arriba del piso, en metros: por encima de la vereda y el cordon de la ciudad no hace
    // falta (no se pisa la vereda), y a ras del piso titilaba con el plano.
    public const float Altura = 0.03f;

    private static Mesh malla;
    private static Material material;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetearEstadoCompartido()
    {
        malla = null;
        material = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Instalar()
    {
        SceneManager.sceneLoaded -= AlCargar;
        SceneManager.sceneLoaded += AlCargar;
    }

    public static bool EnUso
    {
        get { return Plataforma.EsMovil; }
    }

    private static void AlCargar(Scene escena, LoadSceneMode modo)
    {
        if (!EnUso) return;
        foreach (var raiz in escena.GetRootGameObjects())
            foreach (var luz in raiz.GetComponentsInChildren<Light>(true))
                luz.shadows = LightShadows.None;
    }

    // Un disco de 'diametro' metros acostado bajo 'padre', con su centro en 'pies' (en el
    // espacio del padre). Devuelve el objeto, o null si no hay material.
    public static Transform Poner(Transform padre, Vector3 pies, float diametro)
    {
        if (material == null) material = Resources.Load<Material>("SombraFalsa");
        if (material == null) return null;
        if (malla == null) malla = Cuadrado();

        var go = new GameObject("SombraFalsa", typeof(MeshFilter), typeof(MeshRenderer));
        var t = go.transform;
        t.SetParent(padre, false);
        Vector3 escala = padre.lossyScale;
        float ex = Mathf.Max(Mathf.Abs(escala.x), 1e-4f), ey = Mathf.Max(Mathf.Abs(escala.y), 1e-4f), ez = Mathf.Max(Mathf.Abs(escala.z), 1e-4f);
        t.localPosition = pies + new Vector3(0f, Altura / ey, 0f);
        t.localRotation = Quaternion.Euler(90f, 0f, 0f);
        t.localScale = new Vector3(diametro / ex, diametro / ez, 1f);
        go.GetComponent<MeshFilter>().sharedMesh = malla;
        var r = go.GetComponent<MeshRenderer>();
        r.sharedMaterial = material;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        r.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        r.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        return t;
    }

    // Un cuadrado de 1 x 1 en el plano XY, con las UV de 0 a 1: acostado con el giro de 90 en X.
    private static Mesh Cuadrado()
    {
        var m = new Mesh { name = "SombraFalsa" };
        m.vertices = new[]
        {
            new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f),
            new Vector3(-0.5f, 0.5f, 0f), new Vector3(0.5f, 0.5f, 0f),
        };
        m.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) };
        m.triangles = new[] { 0, 2, 1, 2, 3, 1 };
        m.RecalculateNormals();
        m.RecalculateBounds();
        return m;
    }
}
