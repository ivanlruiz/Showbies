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
    const string CarpetaMateriales = "Assets/Materiales";

    // Clara y con la empuñadura marron: sobre la remera negra del muñeco, una pistola
    // oscura no se ve, y a la distancia de la camara mide unos pocos pixeles.
    static readonly Color ColorMetal = new Color(0.62f, 0.64f, 0.7f);
    static readonly Color ColorMango = new Color(0.5f, 0.27f, 0.12f);

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
