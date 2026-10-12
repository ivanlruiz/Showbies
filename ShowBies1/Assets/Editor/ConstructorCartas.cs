using System.IO;
using UnityEditor;
using UnityEngine;

// Arma las cartas de la partida (1.6.0, ver CARTAS.md) y su catalogo en Resources. Crea solo las
// que faltan: los numeros de las que ya estan se balancean en el asset y no se pisan. El orden del
// catalogo es el de esta lista (el sorteo no depende de el).
public static class ConstructorCartas
{
    public const string Carpeta = "Assets/Cartas";
    public const string CarpetaRecursos = Carpeta + "/Resources";

    private struct Molde
    {
        public string id;
        public int tope;
        public float valor;
        public Carta.Requisito requiere;
        public Color color;
        public string simbolo;

        public Molde(string id, int tope, float valor, Carta.Requisito requiere, Color color, string simbolo)
        {
            this.id = id; this.tope = tope; this.valor = valor; this.requiere = requiere; this.color = color; this.simbolo = simbolo;
        }
    }

    // Etapa 1: las de numeros. El simbolo queda de respaldo: lo que se ve es el icono. Los colores
    // los eligio Gemini (12/10), por familia: las estadisticas en naranja, amarillo y rojo, la
    // supervivencia en verdes, la tactica en naranja y rojo oscuros, la curacion blanca.
    private static readonly Molde[] Moldes =
    {
        new Molde(PoderDeLaPartida.Dano, 4, 1.25f, Carta.Requisito.Ninguno, new Color(1f, 0.55f, 0.15f), "+"),
        new Molde(PoderDeLaPartida.Cadencia, 4, 1.2f, Carta.Requisito.Ninguno, new Color(1f, 0.86f, 0.2f), ">>"),
        new Molde(PoderDeLaPartida.Critico, 3, 0.1f, Carta.Requisito.Ninguno, new Color(1f, 0.27f, 0.33f), "!"),
        new Molde(PoderDeLaPartida.Vida, 3, 1.2f, Carta.Requisito.Ninguno, new Color(0.62f, 1f, 0.2f), "V"),
        new Molde(PoderDeLaPartida.Velocidad, 2, 0.1f, Carta.Requisito.Ninguno, new Color(0.45f, 1f, 0.62f), ">"),
        new Molde(PoderDeLaPartida.Iman, 2, 2f, Carta.Requisito.Ninguno, new Color(0.32f, 0.58f, 1f), "U"),
        new Molde(PoderDeLaPartida.GranadaRapida, 2, 0.7f, Carta.Requisito.Granada, new Color(0.95f, 0.42f, 0.08f), "G"),
        new Molde(PoderDeLaPartida.FuriaLarga, 2, 2f, Carta.Requisito.Furia, new Color(0.82f, 0.13f, 0.2f), "F"),
        // Etapa 2: las que cambian las reglas. Alteracion en violeta, elementales en cian, el dron
        // rosa, el vampiro rojo sangre y el escudo azul claro.
        new Molde(PoderDeLaPartida.Perforante, 2, 1f, Carta.Requisito.Ninguno, new Color(0.66f, 0.42f, 1f), "P"),
        new Molde(PoderDeLaPartida.DisparoDoble, 2, 1f, Carta.Requisito.Ninguno, new Color(0.66f, 0.42f, 1f), "2"),
        new Molde(PoderDeLaPartida.HaciaAtras, 1, 1f, Carta.Requisito.Ninguno, new Color(0.66f, 0.42f, 1f), "<"),
        new Molde(PoderDeLaPartida.Explosiva, 2, 0.3f, Carta.Requisito.Ninguno, new Color(0.2f, 0.9f, 1f), "*"),
        new Molde(PoderDeLaPartida.Hielo, 2, 0.4f, Carta.Requisito.Ninguno, new Color(0.2f, 0.9f, 1f), "H"),
        new Molde(PoderDeLaPartida.Quemadura, 2, 0.25f, Carta.Requisito.Ninguno, new Color(0.2f, 0.9f, 1f), "Q"),
        new Molde(PoderDeLaPartida.Dron, 2, 1f, Carta.Requisito.Ninguno, new Color(1f, 0.38f, 0.78f), "D"),
        new Molde(PoderDeLaPartida.Vampiro, 2, 0.05f, Carta.Requisito.Ninguno, new Color(0.72f, 0.06f, 0.14f), "V"),
        new Molde(PoderDeLaPartida.Escudo, 1, 15f, Carta.Requisito.Ninguno, new Color(0.55f, 0.8f, 1f), "E"),
        new Molde(PoderDeLaPartida.Curacion, 0, 1f, Carta.Requisito.Ninguno, new Color(0.95f, 0.96f, 1f), "+"),
    };

    [MenuItem("ShowBies/Cartas/Armar las cartas")]
    public static void Armar()
    {
        if (!AssetDatabase.IsValidFolder(Carpeta)) AssetDatabase.CreateFolder("Assets", "Cartas");
        if (!AssetDatabase.IsValidFolder(CarpetaRecursos)) AssetDatabase.CreateFolder(Carpeta, "Resources");

        string rutaCatalogo = CarpetaRecursos + "/" + CatalogoCartas.Ruta + ".asset";
        var catalogo = AssetDatabase.LoadAssetAtPath<CatalogoCartas>(rutaCatalogo);
        if (catalogo == null)
        {
            catalogo = ScriptableObject.CreateInstance<CatalogoCartas>();
            AssetDatabase.CreateAsset(catalogo, rutaCatalogo);
        }

        int creadas = 0, conIcono = 0;
        catalogo.cartas.Clear();
        foreach (var m in Moldes)
        {
            string ruta = Carpeta + "/Carta_" + m.id + ".asset";
            var carta = AssetDatabase.LoadAssetAtPath<Carta>(ruta);
            if (carta == null)
            {
                carta = ScriptableObject.CreateInstance<Carta>();
                carta.id = m.id;
                carta.tope = m.tope;
                carta.valor = m.valor;
                carta.requiere = m.requiere;
                carta.color = m.color;
                carta.simbolo = m.simbolo;
                AssetDatabase.CreateAsset(carta, ruta);
                creadas++;
            }
            // El icono y el color no son balance: se ponen siempre.
            var icono = Icono(m.id);
            if (icono != null && carta.icono != icono)
            {
                carta.icono = icono;
                EditorUtility.SetDirty(carta);
                conIcono++;
            }
            if (carta.color != m.color)
            {
                carta.color = m.color;
                EditorUtility.SetDirty(carta);
            }
            catalogo.cartas.Add(carta);
        }
        EditorUtility.SetDirty(catalogo);
        AssetDatabase.SaveAssets();
        Debug.Log("ConstructorCartas: " + catalogo.cartas.Count + " cartas en el catalogo, " + creadas + " nuevas, " + conIcono + " iconos puestos");
    }

    // Los iconos los genero Gemini en una hoja y los recorta Marketing/cartas/recortar_iconos.py:
    // blancos sobre transparente, de 256 px. Se importan como los del menu, con mipmaps porque en
    // la fila del HUD se ven a unos 26 px.
    public const string CarpetaIconos = Carpeta + "/Iconos";

    private static Sprite Icono(string id)
    {
        string ruta = CarpetaIconos + "/IconoCarta_" + id + ".png";
        var importador = AssetImporter.GetAtPath(ruta) as TextureImporter;
        if (importador == null) return null;
        bool cambio = importador.textureType != TextureImporterType.Sprite || importador.spriteImportMode != SpriteImportMode.Single
                      || !importador.alphaIsTransparency || !importador.mipmapEnabled;
        if (cambio)
        {
            importador.textureType = TextureImporterType.Sprite;
            importador.spriteImportMode = SpriteImportMode.Single;
            importador.alphaIsTransparency = true;
            importador.mipmapEnabled = true;
            importador.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(ruta);
    }
}
