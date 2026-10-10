using System.IO;
using UnityEditor;
using UnityEngine;

// Arma lo visual que Ivan eligio entre maquetas el 10/10 (MaquetasDelHeroe, de la revision de
// Gemini), y se vuelve a correr para cambiarlo:
//  - Materiales/HeroeRemeraBlanca.png: la textura del pack (TT_demo_texture, que no se toca) con
//    la remera del heroe pintada de blanco, y Materiales/Resources/HeroeRemeraBlanca.mat, que la
//    usa (AspectoDelHeroe lo pone al modelo al arrancar).
//  - Materiales/Resources/AuraHeroe.mat: el charco de luz bajo los pies (ShowBies/CharcoDeLuz).
//  - Materiales/Resources/SombraFalsa.mat: el disco oscuro de las sombras falsas del telefono.
//  - El borde de luz (_Borde de ShowBies/PielDeZombi) de su color en la piel de cada zombi.
public static class ConstructorHeroe
{
    const string TexturaDelPack = "Assets/ToonyTinyPeople/TT_demo/models/Materials/TT_demo_texture.tga";
    const string Textura = "Assets/Materiales/HeroeRemeraBlanca.png";
    const string Recursos = "Assets/Materiales/Resources";
    const string Remera = Recursos + "/HeroeRemeraBlanca.mat";
    const string Aura = Recursos + "/AuraHeroe.mat";
    const string Sombra = Recursos + "/SombraFalsa.mat";

    // La remera en la textura del pack (en pixeles de la de 512, con la y hacia abajo): el bloque
    // que pintan los vertices del hueso Bip001 Spine de TT_demo_male_A, y su color.
    public const int RemeraX0 = 245, RemeraX1 = 273, RemeraY0 = 166, RemeraY1 = 198;
    public static readonly Color32 ColorDeLaRemera = new Color32(42, 35, 31, 255);
    public static readonly Color32 RemeraBlanca = new Color32(240, 240, 240, 255);

    public static readonly Color ColorDelAura = new Color(0.9f, 0.95f, 1f, 1f);
    public const float IntensidadDelAura = 1.6f;
    public const float OpacidadDeLaSombra = 0.55f;

    // El borde de los zombis: de su color, aclarado.
    public const float Borde = 1.6f;
    public const float FinuraDelBorde = 2.5f;
    public const float AclaradoDelBorde = 0.35f;

    public static readonly string[] Pieles =
    {
        "Assets/Materiales/ZombiNormalPiel.mat", "Assets/Materiales/ZombiRapidoPiel.mat", "Assets/Materiales/ZombiFasterPiel.mat",
        "Assets/Materiales/ZombiTanquePiel.mat", "Assets/Materiales/ZombiJefePiel.mat", "Assets/Materiales/ZombiTesoroPiel.mat",
    };

    [MenuItem("ShowBies/Arte/Armar el heroe, el borde y las sombras falsas")]
    public static void Armar()
    {
        if (!AssetDatabase.IsValidFolder(Recursos)) AssetDatabase.CreateFolder("Assets/Materiales", "Resources");
        ArmarTextura();
        ArmarRemera();
        ArmarAura();
        ArmarSombra();
        PonerBordes();
        AssetDatabase.SaveAssets();
        Debug.Log("ConstructorHeroe: listo");
    }

    static void ArmarTextura()
    {
        var original = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturaDelPack);
        if (original == null) { Debug.LogError("ConstructorHeroe: no esta " + TexturaDelPack); return; }
        int w = original.width, h = original.height;
        // Por una RenderTexture: la del pack no esta marcada como legible.
        var rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        Graphics.Blit(original, rt);
        var antes = RenderTexture.active;
        RenderTexture.active = rt;
        var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
        t.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        RenderTexture.active = antes;
        RenderTexture.ReleaseTemporary(rt);

        float escala = w / 512f;
        for (int y = Mathf.FloorToInt(RemeraY0 * escala); y <= Mathf.CeilToInt(RemeraY1 * escala); y++)
        {
            for (int x = Mathf.FloorToInt(RemeraX0 * escala); x <= Mathf.CeilToInt(RemeraX1 * escala); x++)
            {
                int yu = h - 1 - y;   // en la textura de Unity la y va hacia arriba
                Color32 c = t.GetPixel(x, yu);
                int d = Mathf.Abs(c.r - ColorDeLaRemera.r) + Mathf.Abs(c.g - ColorDeLaRemera.g) + Mathf.Abs(c.b - ColorDeLaRemera.b);
                if (d < 40) t.SetPixel(x, yu, RemeraBlanca);
            }
        }
        t.Apply();
        File.WriteAllBytes(Path.GetFullPath(Textura), t.EncodeToPNG());
        Object.DestroyImmediate(t);
        AssetDatabase.ImportAsset(Textura, ImportAssetOptions.ForceUpdate);

        // Que se importe como la del pack.
        var delPack = AssetImporter.GetAtPath(TexturaDelPack) as TextureImporter;
        var nueva = AssetImporter.GetAtPath(Textura) as TextureImporter;
        if (delPack != null && nueva != null)
        {
            nueva.textureType = delPack.textureType;
            nueva.sRGBTexture = delPack.sRGBTexture;
            nueva.mipmapEnabled = delPack.mipmapEnabled;
            nueva.filterMode = delPack.filterMode;
            nueva.wrapMode = delPack.wrapMode;
            nueva.maxTextureSize = delPack.maxTextureSize;
            nueva.textureCompression = delPack.textureCompression;
            nueva.SaveAndReimport();
        }
    }

    static void ArmarRemera()
    {
        var mat = Cargar(Remera, "Legacy Shaders/Diffuse");
        if (mat == null) return;
        mat.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(Textura);
        mat.color = Color.white;
        EditorUtility.SetDirty(mat);
    }

    static void ArmarAura()
    {
        var mat = Cargar(Aura, "ShowBies/CharcoDeLuz");
        if (mat == null) return;
        mat.SetColor("_Color", ColorDelAura);
        mat.SetFloat("_Intensidad", IntensidadDelAura);
        EditorUtility.SetDirty(mat);
    }

    static void ArmarSombra()
    {
        var mat = Cargar(Sombra, "ShowBies/SombraFalsa");
        if (mat == null) return;
        mat.SetFloat("_Opacidad", OpacidadDeLaSombra);
        EditorUtility.SetDirty(mat);
    }

    static void PonerBordes()
    {
        foreach (var ruta in Pieles)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (mat == null || !mat.HasProperty("_Borde")) { Debug.LogWarning("ConstructorHeroe: sin borde " + ruta); continue; }
            Color piel = mat.GetColor("_Color");
            mat.SetFloat("_Borde", Borde);
            mat.SetFloat("_FinuraBorde", FinuraDelBorde);
            mat.SetColor("_ColorBorde", Color.Lerp(piel, Color.white, AclaradoDelBorde));
            EditorUtility.SetDirty(mat);
        }
    }

    static Material Cargar(string ruta, string shader)
    {
        var s = Shader.Find(shader);
        if (s == null) { Debug.LogError("ConstructorHeroe: no esta el shader " + shader); return null; }
        var mat = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (mat == null)
        {
            mat = new Material(s) { name = Path.GetFileNameWithoutExtension(ruta) };
            AssetDatabase.CreateAsset(mat, ruta);
        }
        else if (mat.shader != s) mat.shader = s;
        return mat;
    }
}
