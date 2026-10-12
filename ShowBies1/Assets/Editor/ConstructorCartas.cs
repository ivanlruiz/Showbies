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

    // Etapa 1: las de numeros. El simbolo es provisorio, hasta que tengan dibujo.
    private static readonly Molde[] Moldes =
    {
        new Molde(PoderDeLaPartida.Dano, 4, 1.25f, Carta.Requisito.Ninguno, new Color(1f, 0.42f, 0.25f), "+"),
        new Molde(PoderDeLaPartida.Cadencia, 4, 1.2f, Carta.Requisito.Ninguno, new Color(0.3f, 0.75f, 1f), ">>"),
        new Molde(PoderDeLaPartida.Critico, 3, 0.1f, Carta.Requisito.Ninguno, new Color(1f, 0.23f, 0.36f), "!"),
        new Molde(PoderDeLaPartida.Vida, 3, 1.2f, Carta.Requisito.Ninguno, new Color(0.22f, 1f, 0.53f), "V"),
        new Molde(PoderDeLaPartida.Velocidad, 2, 0.1f, Carta.Requisito.Ninguno, new Color(0.75f, 1f, 0.3f), ">"),
        new Molde(PoderDeLaPartida.Iman, 2, 2f, Carta.Requisito.Ninguno, new Color(1f, 0.88f, 0.3f), "U"),
        new Molde(PoderDeLaPartida.GranadaRapida, 2, 0.7f, Carta.Requisito.Granada, new Color(1f, 0.62f, 0.11f), "G"),
        new Molde(PoderDeLaPartida.FuriaLarga, 2, 2f, Carta.Requisito.Furia, new Color(1f, 0.17f, 0.84f), "F"),
        new Molde(PoderDeLaPartida.Curacion, 0, 1f, Carta.Requisito.Ninguno, new Color(0.22f, 1f, 0.53f), "+"),
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

        int creadas = 0;
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
            catalogo.cartas.Add(carta);
        }
        EditorUtility.SetDirty(catalogo);
        AssetDatabase.SaveAssets();
        Debug.Log("ConstructorCartas: " + catalogo.cartas.Count + " cartas en el catalogo, " + creadas + " nuevas");
    }
}
