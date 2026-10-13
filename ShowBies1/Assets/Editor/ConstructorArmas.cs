using System.IO;
using UnityEditor;
using UnityEngine;

// Arma la pistola que el muñeco tiene en la mano (ver ArmaEnLaMano), con formas simples
// y sin modelos, como los decorados. Pedido de Ivan: el modelo del pack traia un bate en
// la mano, y con las poses de pistola lo levantaba delante de la cara; las balas salian de
// un cubito que flotaba a la altura del pecho, y corriendo y disparando no se leia que
// estuviera disparando.
//
// Los ejes son los de la mano del modelo en las poses de pistola: +Z es el cañon (hacia
// adelante), +Y es arriba y el origen es el puño, donde va la empuñadura. La "Boca" es el
// punto del que salen las balas y el fogonazo.
//
// Se guarda como prefab con sus materiales; volver a correrlo la rehace igual.
public static class ConstructorArmas
{
    public const string RutaPistola = "Assets/Prefabs/Pistola.prefab";
    // El lomo de la corredera mide 0,07: la luz de arriba lo cubre entero (lo mira la prueba).
    public const float AnchoDelLedArriba = 0.072f;
    const string CarpetaMateriales = "Assets/Materiales";

    // Gris medio (#666666), de la segunda revision de arte de Gemini (12/10): el gris oscuro
    // (#333333) de la primera se fundia con el piso de noche, y el cañon largo de la escopeta y
    // del rifle no se veia. Hasta la etapa 3 era plateada con la empuñadura marron, porque la
    // remera era negra.
    static readonly Color ColorMetal = new Color(0.4f, 0.4f, 0.4f);
    static readonly Color ColorMango = new Color(0.3f, 0.3f, 0.3f);

    // La luz de la bala (BulletController.materialLuz, pedido de Ivan, 9/10): el material del
    // charco de los faroles, blanco (cada tramo lo tiñe con su color en runtime), puesto en el
    // prefab de la bala. La malla redonda de la onda del arma de energia (mallaRedonda: la
    // esfera baja de Halloween, de 150 vertices; la de Unity tiene 515, y con la cadencia alta
    // hay decenas en pantalla); las demas son el cubo del prefab. Y la base de los materiales de
    // cada tramo (materialPlano: ShowBies/ColorPlano, con instancing, 13/10). Se vuelve a correr
    // si se cambian.
    public const string RutaLuzDeLaBala = "Assets/Materiales/BalaLuz.mat";
    public const string RutaBalaPlana = "Assets/Materiales/BalaPlana.mat";
    public const string RutaEsferaBaja = "Assets/Halloween/EsferaBaja.asset";

    [MenuItem("ShowBies/Armas/Armar la luz de la bala")]
    public static void ArmarLuzDeLaBala()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogError("ConstructorArmas: no en play"); return; }
        var material = ConstructorEscenarios.MaterialDeBrillo("Assets/Materiales", "BalaLuz", Color.white, 1.1f, 0.35f, 4.5f);
        const string rutaBala = "Assets/Prefabs/Bullet.prefab";
        var contenido = PrefabUtility.LoadPrefabContents(rutaBala);
        try
        {
            var bala = contenido.GetComponent<BulletController>();
            if (bala == null) { Debug.LogError("ConstructorArmas: el prefab de la bala no tiene BulletController"); return; }
            bala.materialLuz = material;
            var esfera = AssetDatabase.LoadAssetAtPath<Mesh>(RutaEsferaBaja);
            if (esfera == null) { Debug.LogError("ConstructorArmas: falta " + RutaEsferaBaja + " (la arma ShowBies > Halloween)"); return; }
            bala.mallaRedonda = esfera;
            bala.materialPlano = MaterialPlanoDeLaBala();
            PrefabUtility.SaveAsPrefabAsset(contenido, rutaBala);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contenido);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("ConstructorArmas: la bala tiene su luz (" + RutaLuzDeLaBala + ") y su material plano (" + RutaBalaPlana + ")");
    }

    // Blanco: cada tramo lo tiñe con su color en runtime (BulletController.ArmarMateriales).
    static Material MaterialPlanoDeLaBala()
    {
        var shader = Shader.Find("ShowBies/ColorPlano");
        var mat = AssetDatabase.LoadAssetAtPath<Material>(RutaBalaPlana);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, RutaBalaPlana);
        }
        mat.shader = shader;
        mat.color = Color.white;
        mat.enableInstancing = true;
        EditorUtility.SetDirty(mat);
        return mat;
    }

    [MenuItem("ShowBies/Armas/Armar la pistola")]
    public static void ArmarPistola()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(RutaPistola));
        var metal = Material("Pistola", ColorMetal, 0.55f);
        var mango = Material("PistolaMango", ColorMango, 0.2f);

        var raiz = new GameObject("Pistola");
        Pieza(raiz, "Corredera", PrimitiveType.Cube, new Vector3(0f, 0.075f, 0.075f), Quaternion.identity, new Vector3(0.07f, 0.07f, 0.3f), metal);
        Pieza(raiz, "Canon", PrimitiveType.Cylinder, new Vector3(0f, 0.075f, 0.245f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.045f, 0.02f, 0.045f), metal);
        Pieza(raiz, "Mira", PrimitiveType.Cube, new Vector3(0f, 0.12f, 0.2f), Quaternion.identity, new Vector3(0.015f, 0.025f, 0.02f), metal);
        Pieza(raiz, "Mango", PrimitiveType.Cube, new Vector3(0f, -0.035f, -0.03f), Quaternion.Euler(-14f, 0f, 0f), new Vector3(0.06f, 0.17f, 0.085f), mango);
        Pieza(raiz, "Guardamonte", PrimitiveType.Cube, new Vector3(0f, -0.005f, 0.05f), Quaternion.identity, new Vector3(0.03f, 0.015f, 0.09f), metal);

        // La luz de neon (el "LED"): el lomo entero de la corredera y una franja ancha a cada
        // costado, asi se ve gire como gire la mano. Tres veces mas ancha que la primera (de la
        // segunda revision de Gemini: a la distancia de la camara era una linea de un pixel).
        // Blanca: ArmaEnLaMano la tiñe del color del tramo, la hace latir en cada tiro y la
        // pone roja con la furia.
        var luz = MaterialDeLaLuz();
        var led = new GameObject("LED");
        led.transform.SetParent(raiz.transform, false);
        Pieza(led, "Arriba", PrimitiveType.Cube, new Vector3(0f, 0.112f, 0.08f), Quaternion.identity, new Vector3(AnchoDelLedArriba, 0.006f, 0.22f), luz);
        Pieza(led, "Izquierda", PrimitiveType.Cube, new Vector3(-0.037f, 0.078f, 0.08f), Quaternion.identity, new Vector3(0.006f, 0.05f, 0.22f), luz);
        Pieza(led, "Derecha", PrimitiveType.Cube, new Vector3(0.037f, 0.078f, 0.08f), Quaternion.identity, new Vector3(0.006f, 0.05f, 0.22f), luz);
        foreach (var r in led.GetComponentsInChildren<MeshRenderer>())
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        var boca = new GameObject("Boca");
        boca.transform.SetParent(raiz.transform, false);
        boca.transform.localPosition = new Vector3(0f, 0.075f, 0.27f);

        // El fogonazo: apagado, lo prende ArmaEnLaMano en cada tiro y lo pone de frente a
        // la camara, estirado hacia donde apunta el cañon.
        var fogonazo = GameObject.CreatePrimitive(PrimitiveType.Quad);
        fogonazo.name = "Fogonazo";
        Object.DestroyImmediate(fogonazo.GetComponent<Collider>());
        fogonazo.transform.SetParent(boca.transform, false);
        var render = fogonazo.GetComponent<MeshRenderer>();
        render.sharedMaterial = MaterialDelFogonazo();
        render.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        render.receiveShadows = false;
        render.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        render.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        fogonazo.SetActive(false);

        PrefabUtility.SaveAsPrefabAsset(raiz, RutaPistola);
        Object.DestroyImmediate(raiz);
        AssetDatabase.SaveAssets();
        Debug.Log("ConstructorArmas: pistola armada en " + RutaPistola);
    }

    static void Pieza(GameObject padre, string nombre, PrimitiveType tipo, Vector3 posicion, Quaternion rotacion, Vector3 escala, Material material)
    {
        var go = GameObject.CreatePrimitive(tipo);
        go.name = nombre;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(padre.transform, false);
        go.transform.localPosition = posicion;
        go.transform.localRotation = rotacion;
        go.transform.localScale = escala;
        go.GetComponent<MeshRenderer>().sharedMaterial = material;
    }

    // Unlit/Color, que ya usan las escenas (el neon de los decorados): la luz no depende de la
    // luna ni del relleno.
    static Material MaterialDeLaLuz()
    {
        string ruta = CarpetaMateriales + "/PistolaLuz.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Unlit/Color"));
            AssetDatabase.CreateAsset(mat, ruta);
        }
        mat.shader = Shader.Find("Unlit/Color");
        mat.color = Color.white;
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static Material MaterialDelFogonazo()
    {
        var shader = Shader.Find("ShowBies/Fogonazo");
        string ruta = CarpetaMateriales + "/Fogonazo.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, ruta);
        }
        mat.shader = shader;
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static Material Material(string nombre, Color color, float brillo)
    {
        string ruta = CarpetaMateriales + "/" + nombre + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Standard"));
            AssetDatabase.CreateAsset(mat, ruta);
        }
        mat.color = color;
        mat.SetFloat("_Glossiness", brillo);
        mat.SetFloat("_Metallic", 0f);
        EditorUtility.SetDirty(mat);
        return mat;
    }
}
