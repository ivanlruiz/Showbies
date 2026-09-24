using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Viste la tienda de mejoras de carbón neón, pedido de Ivan (24/9): la quiso más
// minimalista y negra, y eligió esta entre seis paletas (las maquetas están en
// Builds/temas_tienda). Fondo casi negro, tarjetas oscuras con un borde celeste que
// brilla, el título en magenta con halo, y el verde neón para comprar, para el número
// nuevo y para ¡A JUGAR!. Saca lo que sobraba: la franja y el círculo con la letra de
// cada mejora, la barra de nivel, el sello de "¡MÁXIMO!", los rayos del fondo y la
// sombra del título.
//
// La paleta es propia: la tienda deja de seguir el tema claro u oscuro (se le sacan los
// PintarConTema), porque es igual de negra en los dos.
//
// No se edita a mano: se vuelve a correr (ShowBies > Tienda > Vestir de carbón neón),
// y correrlo dos veces deja lo mismo. Arregla también la importación de los dos brillos
// (Sprites/UI/NeonBorde y NeonPildora) y arma el material del título.
public static class ConstructorTienda
{
    const string RutaTienda = "Assets/Prefabs/UI/Tienda.prefab";
    const string RutaTarjeta = "Assets/Prefabs/UI/TarjetaMejora.prefab";
    const string RutaBorde = "Assets/Sprites/UI/NeonBorde.png";
    const string RutaPildora = "Assets/Sprites/UI/NeonPildora.png";
    const string RutaPildoraRedonda = "Assets/Sprites/UI/Pildora.png";
    const string RutaMaterialBase = "Assets/Fuentes/Bangers SDF - Outline.mat";
    const string RutaMaterialNeon = "Assets/Fuentes/Bangers SDF - Neon.mat";

    // La paleta, la de la maqueta 5.
    // Opaco: con 0,97 se trasluce el logo del menu detras del titulo.
    static readonly Color Fondo = Hex("06070B");
    static readonly Color Tarjeta = Hex("0E1017");
    static readonly Color Vidrio = Hex("11141D");
    static readonly Color Texto = Color.white;
    static readonly Color Suave = Hex("8A94AD");
    static readonly Color Celeste = Hex("00E5FF");
    static readonly Color Magenta = Hex("FF2BD6");
    static readonly Color TituloCara = Hex("FFB3F2");
    static readonly Color Verde = Hex("39FF88");
    static readonly Color VerdeTexto = Hex("03200F");
    static readonly Color Apagado = Hex("161A24");
    static readonly Color Amarillo = Hex("FFE14D");
    static readonly Color AmarilloClaro = Hex("FFF3A6");
    static readonly Color AmarilloTexto = Hex("1A1600");

    [MenuItem("ShowBies/Tienda/Vestir de carbón neón")]
    public static void Vestir()
    {
        ImportarBrillo(RutaBorde, new Vector4(72, 72, 72, 72));
        ImportarBrillo(RutaPildora, new Vector4(70, 59, 70, 59));
        var borde = AssetDatabase.LoadAssetAtPath<Sprite>(RutaBorde);
        var pildora = AssetDatabase.LoadAssetAtPath<Sprite>(RutaPildora);
        var neon = MaterialNeon();
        if (borde == null || pildora == null || neon == null)
        {
            Debug.LogError("ConstructorTienda: faltan los brillos o el material");
            return;
        }

        VestirTarjeta(borde, pildora);
        VestirTienda(pildora, neon);
        AssetDatabase.SaveAssets();
        Debug.Log("ConstructorTienda: la tienda quedó de carbón neón");
    }

    static void ImportarBrillo(string ruta, Vector4 bordes)
    {
        AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceSynchronousImport);
        var importador = AssetImporter.GetAtPath(ruta) as TextureImporter;
        if (importador == null) return;
        importador.textureType = TextureImporterType.Sprite;
        importador.spriteImportMode = SpriteImportMode.Single;
        importador.alphaIsTransparency = true;
        importador.mipmapEnabled = false;
        importador.wrapMode = TextureWrapMode.Clamp;
        importador.spriteBorder = bordes;
        importador.SaveAndReimport();
    }

    // El título: la cara clara y un halo magenta, con el underlay del shader móvil de
    // siempre (el de escritorio tiene glow, pero el de Bangers es el móvil).
    static Material MaterialNeon()
    {
        var baseMat = AssetDatabase.LoadAssetAtPath<Material>(RutaMaterialBase);
        if (baseMat == null) return null;
        var neon = AssetDatabase.LoadAssetAtPath<Material>(RutaMaterialNeon);
        if (neon == null)
        {
            neon = new Material(baseMat);
            AssetDatabase.CreateAsset(neon, RutaMaterialNeon);
        }
        neon.shader = baseMat.shader;
        neon.CopyPropertiesFromMaterial(baseMat);
        neon.shaderKeywords = baseMat.shaderKeywords;
        neon.SetFloat("_OutlineWidth", 0.08f);
        neon.SetColor("_OutlineColor", Magenta);
        neon.EnableKeyword("UNDERLAY_ON");
        neon.SetColor("_UnderlayColor", new Color(Magenta.r, Magenta.g, Magenta.b, 0.95f));
        neon.SetFloat("_UnderlayOffsetX", 0f);
        neon.SetFloat("_UnderlayOffsetY", 0f);
        neon.SetFloat("_UnderlayDilate", 0.6f);
        neon.SetFloat("_UnderlaySoftness", 0.8f);
        EditorUtility.SetDirty(neon);
        return neon;
    }

    static void VestirTarjeta(Sprite borde, Sprite pildora)
    {
        var raiz = PrefabUtility.LoadPrefabContents(RutaTarjeta);
        try
        {
            var tarjeta = raiz.GetComponent<TarjetaMejora>();
            var contenido = raiz.transform.Find("Contenido");

            // El borde que brilla, detras de todo lo de la tarjeta: sobresale 40 de cada lado,
            // que es el margen que tiene el dibujo afuera de la linea.
            var halo = Hijo(contenido, "Halo");
            halo.SetSiblingIndex(0);
            Estirar(halo, 40f);
            var imagenHalo = Imagen(halo, borde, Color.clear);
            imagenHalo.color = new Color(Celeste.r, Celeste.g, Celeste.b, 0.4f);
            tarjeta.haloTarjeta = imagenHalo;

            // Lo que sobraba: el borde de color que latia, la franja, el circulo con la
            // letra y la barra de nivel. Apagados y sin referencia, que Refrescar los prende.
            Apagar(contenido, "Borde");
            tarjeta.borde = null;
            Apagar(contenido, "Franja");
            tarjeta.franja = null;
            Apagar(contenido, "Icono");
            tarjeta.icono = null;
            tarjeta.imagenIcono = null;
            tarjeta.simbolo = null;
            Apagar(contenido, "BarraNivel");
            tarjeta.barraNivel = null;
            tarjeta.rellenoNivel = null;

            // El fondo y el destello de la compra, con las puntas del mismo radio que la linea
            // de neon (26): con el UISprite de antes las esquinas asomaban afuera de la linea.
            var pildoraRedonda = AssetDatabase.LoadAssetAtPath<Sprite>(RutaPildoraRedonda);
            SinTema(contenido.Find("Fondo"), Tarjeta);
            Redondear(contenido.Find("Fondo"), pildoraRedonda);
            Redondear(contenido.Find("Destello"), pildoraRedonda);

            // Sin el circulo arriba, todo sube: nombre, nivel, valores, unidad y "faltan".
            SinTema(contenido.Find("Nombre"), Texto);
            Mover(contenido, "Nombre", -70f);
            SinTema(contenido.Find("Nivel"), Suave);
            Mover(contenido, "Nivel", -128f);
            Mover(contenido, "Valores", -205f);
            SinTema(contenido.Find("Valores/ValorActual"), Texto);
            SinTema(contenido.Find("Valores/Flecha"), Suave);
            SinTema(contenido.Find("Valores/ValorSiguiente"), Verde);
            SinTema(contenido.Find("Descripcion"), Suave);
            Mover(contenido, "Descripcion", -282f);
            SinTema(contenido.Find("Faltan"), Suave);
            Mover(contenido, "Faltan", -330f);

            // El boton: la sombra pasa a ser su halo, un poco mas grande que el.
            var sombra = contenido.Find("Boton/Sombra");
            Estirar(sombra, 34f);
            var imagenSombra = Imagen(sombra, pildora, Color.clear);
            imagenSombra.color = new Color(Verde.r, Verde.g, Verde.b, 0.4f);
            tarjeta.haloBoton = imagenSombra;
            SinTema(contenido.Find("Boton/Visual/GrupoPrecio/Moneda"), Amarillo);
            SinTema(contenido.Find("Boton/Visual/GrupoPrecio/Moneda/Brillo"), AmarilloClaro);
            SinTema(contenido.Find("Boton/Visual/TextoTope"), AmarilloTexto);
            // El sello "¡MAXIMO!" iba encima del valor: con el borde amarillo y el MAX del boton
            // el tope ya se lee, y el sello era lo menos minimalista de la tarjeta.
            Apagar(contenido, "Estampa");
            tarjeta.estampa = null;

            tarjeta.colorComprable = Verde;
            tarjeta.colorSinMonedas = Apagado;
            tarjeta.colorTope = Amarillo;
            tarjeta.colorPrecioFalta = Suave;
            tarjeta.colorValorSiguiente = Verde;
            tarjeta.colorPrecioComprable = VerdeTexto;
            tarjeta.colorNeon = Celeste;

            // Ningun PintarConTema: la tarjeta es igual en los dos temas.
            foreach (var pintor in raiz.GetComponentsInChildren<PintarConTema>(true)) Object.DestroyImmediate(pintor);
            PrefabUtility.SaveAsPrefabAsset(raiz, RutaTarjeta);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(raiz);
        }
    }

    static void VestirTienda(Sprite pildora, Material neon)
    {
        var raiz = PrefabUtility.LoadPrefabContents(RutaTienda);
        try
        {
            var tienda = raiz.GetComponent<TiendaMejoras>();
            tienda.colorTope = Amarillo;
            var guia = raiz.GetComponent<GuiaPrimeraCompra>();
            if (guia != null) guia.colorFlecha = Amarillo;

            var panel = raiz.transform.Find("Panel");
            SinTema(panel.Find("Fondo"), Fondo);
            // Los rayos que giraban detras: fuera. El resplandor queda, apenas celeste.
            Apagar(panel, "Rayos");
            var resplandor = panel.Find("Resplandor").GetComponent<RawImage>();
            resplandor.color = new Color(Celeste.r, Celeste.g, Celeste.b, 0.07f);

            var encabezado = panel.Find("AreaSegura/Encabezado");
            SinTema(encabezado.Find("BotonVolver/Visual/Fondo"), Vidrio);
            Apagar(encabezado, "Titulo/Sombra");
            var titulo = encabezado.Find("Titulo/Texto").GetComponent<TMP_Text>();
            titulo.fontSharedMaterial = neon;
            // Traia un degrade de dorado a naranja, que le ganaba al color.
            titulo.enableVertexGradient = false;
            titulo.colorGradientPreset = null;
            titulo.color = TituloCara;
            SinTema(encabezado.Find("Monedas/Fondo"), Vidrio);
            SinTema(encabezado.Find("Monedas/Icono"), Amarillo);
            SinTema(encabezado.Find("Monedas/Icono/Brillo"), AmarilloClaro);
            SinTema(encabezado.Find("Monedas/Texto"), Amarillo);

            var pie = panel.Find("AreaSegura/Pie");
            SinTema(pie.Find("Pista"), Suave);
            var sombraJugar = pie.Find("BotonJugar/Sombra");
            Estirar(sombraJugar, 34f);
            var imagenSombra = Imagen(sombraJugar, pildora, Color.clear);
            imagenSombra.color = new Color(Verde.r, Verde.g, Verde.b, 0.55f);
            SinTema(pie.Find("BotonJugar/Visual/Fondo"), Verde);
            SinTema(pie.Find("BotonJugar/Visual/Icono"), VerdeTexto);
            SinTema(pie.Find("BotonJugar/Visual/Texto"), VerdeTexto);

            foreach (var pintor in raiz.GetComponentsInChildren<PintarConTema>(true)) Object.DestroyImmediate(pintor);
            PrefabUtility.SaveAsPrefabAsset(raiz, RutaTienda);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(raiz);
        }
    }

    // --- Piezas -------------------------------------------------------------------

    static Transform Hijo(Transform padre, string nombre)
    {
        var hijo = padre.Find(nombre);
        if (hijo != null) return hijo;
        var go = new GameObject(nombre, typeof(RectTransform));
        go.layer = padre.gameObject.layer;
        go.transform.SetParent(padre, false);
        return go.transform;
    }

    // Estirado al padre y sobresaliendo 'margen' de cada lado.
    static void Estirar(Transform t, float margen)
    {
        var rt = (RectTransform)t;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(-margen, -margen);
        rt.offsetMax = new Vector2(margen, margen);
    }

    static Image Imagen(Transform t, Sprite sprite, Color color)
    {
        var imagen = t.GetComponent<Image>();
        if (imagen == null) imagen = t.gameObject.AddComponent<Image>();
        imagen.sprite = sprite;
        imagen.type = Image.Type.Sliced;
        imagen.pixelsPerUnitMultiplier = 1f;
        imagen.fillCenter = true;
        imagen.raycastTarget = false;
        imagen.color = color;
        return imagen;
    }

    // La pildora (un circulo con bordes de 127) en Sliced: con el multiplicador en 4,9 las
    // puntas quedan de 26 de radio, como la linea de NeonBorde.
    static void Redondear(Transform t, Sprite pildora)
    {
        var imagen = t != null ? t.GetComponent<Image>() : null;
        if (imagen == null || pildora == null) return;
        imagen.sprite = pildora;
        imagen.type = Image.Type.Sliced;
        imagen.pixelsPerUnitMultiplier = 127f / 26f;
    }

    static void Apagar(Transform padre, string ruta)
    {
        var t = padre.Find(ruta);
        if (t != null) t.gameObject.SetActive(false);
    }

    static void Mover(Transform padre, string ruta, float y)
    {
        var rt = padre.Find(ruta) as RectTransform;
        if (rt != null) rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, y);
    }

    // El color fijo, sin el componente que lo cambiaba con el tema.
    static void SinTema(Transform t, Color color)
    {
        if (t == null) return;
        var pintor = t.GetComponent<PintarConTema>();
        if (pintor != null) Object.DestroyImmediate(pintor);
        var grafico = t.GetComponent<Graphic>();
        if (grafico != null) grafico.color = color;
    }

    static Color Hex(string hex, float alfa = 1f)
    {
        Color c;
        ColorUtility.TryParseHtmlString("#" + hex, out c);
        c.a = alfa;
        return c;
    }
}
