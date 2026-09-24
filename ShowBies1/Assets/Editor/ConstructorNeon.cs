using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Viste de carbón neón lo que está en las escenas, pedido de Ivan (24/9): eligió esa paleta
// para la tienda y "la idea es que todo el juego tenga esa temática". Lo que se arma en
// código lo viste ConstructorUI (el borde de las ventanas, el halo de los botones) y los
// paneles y textos los pinta el tema, que desde entonces es uno solo (Tema); acá va lo que
// está guardado en las escenas:
//
//  - Cada botón del molde de siempre (Sombra, Visual/Fondo, Icono, Texto) pasa a su neón
//    según el color que tenía: el verde de jugar a verde neón, el dorado de la tienda a
//    amarillo, el naranja de las oleadas a naranja, el azul a celeste, cada uno con el texto
//    oscuro; la sombra pasa a ser su halo. Los de vidrio siguen al tema (vidrio oscuro) con un
//    halo celeste tenue.
//  - El globo del idioma lleva un anillo celeste: el engranaje, las misiones, el bestiario y
//    la medalla son copias suyas y lo heredan.
//  - La ventana del idioma lleva el borde celeste y el título rosa con halo magenta (el
//    material Bangers SDF - Neon); las de opciones y de salir son copias suyas.
//  - Las ventanas que se arman en código reciben por el inspector el material del título y
//    sus colores de neón.
//
// No se edita a mano: ShowBies > Neón > Vestir el menú, y se vuelve a correr (correrlo dos
// veces deja lo mismo). La tienda la viste ConstructorTienda.
public static class ConstructorNeon
{
    const string RutaMenu = "Assets/Escenas/Menu.unity";
    const string RutaPildora = "Assets/Sprites/UI/Pildora.png";
    const string RutaMaterialNeon = "Assets/Fuentes/Bangers SDF - Neon.mat";
    public const string RutaAnillo = "Assets/Sprites/UI/Resources/NeonAnillo.png";
    public const string RutaBorde = "Assets/Sprites/UI/Resources/NeonBorde.png";

    static readonly Color CelesteTexto = new Color(0f, 0.165f, 0.188f, 1f);

    [MenuItem("ShowBies/Neón/Vestir el menú")]
    public static void VestirMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("ConstructorNeon: en play no");
            return;
        }
        var activa = SceneManager.GetActiveScene();
        if (activa.isDirty && activa.path != RutaMenu)
        {
            Debug.LogError("ConstructorNeon: la escena abierta (" + activa.path + ") tiene cambios sin guardar");
            return;
        }
        ImportarAnillo();
        var escena = EditorSceneManager.OpenScene(RutaMenu, OpenSceneMode.Single);
        var neon = AssetDatabase.LoadAssetAtPath<Material>(RutaMaterialNeon);
        var pildora = AssetDatabase.LoadAssetAtPath<Sprite>(RutaPildora);

        var menu = Object.FindFirstObjectByType<SelectorIdioma>(FindObjectsInactive.Include);
        if (menu == null)
        {
            Debug.LogError("ConstructorNeon: el menú no tiene su SelectorIdioma");
            return;
        }
        var canvas = menu.transform;

        foreach (var ruta in new[] { "MainMenu/Play", "MainMenu/Mejoras", "MainMenu/Quit", "GameModesMenu/FreeMode",
                                     "GameModesMenu/WaveMode", "GameModesMenu/Back", "GameModesMenu/Tutorial" })
            VestirBoton(canvas.Find(ruta));

        // El globo y su anillo, que heredan sus copias.
        if (menu.botonGlobo != null) Anillo(menu.botonGlobo.transform);

        // La ventana del idioma: el borde, el titulo, el VOLVER y los dos idiomas (el elegido
        // en amarillo, el otro en gris azulado; los pinta SelectorIdioma).
        if (menu.ventana != null)
        {
            VentanaNeon(menu.ventana, pildora);
            var titulo = menu.ventana.Find("Titulo");
            if (titulo != null) TituloNeon(titulo.GetComponent<TMP_Text>(), neon);
        }
        if (menu.botonVolver != null) VestirBoton(menu.botonVolver.transform);
        foreach (var boton in menu.botonesIdioma)
        {
            if (boton == null) continue;
            var sombra = boton.transform.Find("Sombra");
            if (sombra != null) ConstructorUI.HaloDeBoton(sombra.GetComponent<Image>(), Tema.TextoSuaveClaro);
        }
        menu.colorElegido = ConstructorUI.Amarillo;
        menu.colorOtro = Tema.TextoSuaveClaro;
        EditorUtility.SetDirty(menu);

        // Las ventanas que se arman en codigo: el material del titulo y sus colores.
        var misiones = canvas.GetComponent<VentanaMisiones>();
        if (misiones != null)
        {
            misiones.materialContorno = neon;
            misiones.colorTitulo = ConstructorUI.TituloNeon;
            misiones.colorCumplida = ConstructorUI.Verde;
            misiones.colorBarra = ConstructorUI.Verde;
            misiones.colorCofre = ConstructorUI.Amarillo;
            misiones.coloresDificultad = new[] { ConstructorUI.Verde, ConstructorUI.Amarillo, ConstructorUI.Rojo };
            EditorUtility.SetDirty(misiones);
        }
        var bestiario = canvas.GetComponent<VentanaBestiario>();
        if (bestiario != null)
        {
            bestiario.materialContorno = neon;
            bestiario.colorTitulo = ConstructorUI.TituloNeon;
            bestiario.colorBarra = ConstructorUI.Verde;
            bestiario.colorEstrella = ConstructorUI.Amarillo;
            EditorUtility.SetDirty(bestiario);
        }
        var logros = canvas.GetComponent<VentanaLogros>();
        if (logros != null)
        {
            logros.materialContorno = neon;
            logros.colorTitulo = ConstructorUI.TituloNeon;
            logros.colorBarra = ConstructorUI.Verde;
            logros.colorExperiencia = ConstructorUI.Celeste;
            EditorUtility.SetDirty(logros);
        }
        var diaria = canvas.GetComponent<VentanaRecompensaDiaria>();
        if (diaria != null)
        {
            diaria.materialContorno = neon;
            diaria.colorHoy = ConstructorUI.Amarillo;
            diaria.colorCobrado = ConstructorUI.Verde;
            diaria.colorVideo = ConstructorUI.Naranja;
            EditorUtility.SetDirty(diaria);
        }
        var salir = canvas.GetComponent<ConfirmarSalir>();
        if (salir != null)
        {
            salir.colorSeguir = ConstructorUI.Verde;
            salir.colorTextoSeguir = ConstructorUI.VerdeTexto;
            EditorUtility.SetDirty(salir);
        }

        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);
        Debug.Log("ConstructorNeon: el menú quedó de carbón neón");
    }

    // --- Piezas, tambien para las otras escenas ------------------------------------

    // Un boton del molde de siempre a su neon, segun el color que tenia.
    public static void VestirBoton(Transform boton)
    {
        if (boton == null) return;
        var fondo = boton.Find("Visual/Fondo") != null ? boton.Find("Visual/Fondo").GetComponent<Image>() : null;
        if (fondo == null) return;

        var pintor = fondo.GetComponent<PintarConTema>();
        bool vidrio = (pintor != null && pintor.rol == RolDeTema.Vidrio) || fondo.color.a < 0.95f;
        Color relleno = fondo.color, texto = Color.white;
        if (!vidrio)
        {
            NeonDe(fondo.color, out relleno, out texto);
            fondo.color = relleno;
            foreach (var grafico in boton.Find("Visual").GetComponentsInChildren<Graphic>(true))
            {
                if (grafico == fondo) continue;
                grafico.color = texto;
            }
        }
        var sombra = boton.Find("Sombra");
        if (sombra != null) ConstructorUI.HaloDeBoton(sombra.GetComponent<Image>(), vidrio ? Tema.VidrioClaro : relleno);
        EditorUtility.SetDirty(boton.gameObject);
    }

    // El neon que le toca a un color de boton de antes, por su tono.
    public static void NeonDe(Color viejo, out Color relleno, out Color texto)
    {
        float h, s, v;
        Color.RGBToHSV(viejo, out h, out s, out v);
        float grados = h * 360f;
        if (grados >= 70f && grados < 165f) { relleno = ConstructorUI.Verde; texto = ConstructorUI.VerdeTexto; }
        else if (grados >= 38f && grados < 70f) { relleno = ConstructorUI.Amarillo; texto = ConstructorUI.AmarilloTexto; }
        else if (grados >= 15f && grados < 38f) { relleno = ConstructorUI.Naranja; texto = ConstructorUI.NaranjaTexto; }
        else if (grados >= 165f && grados < 270f) { relleno = ConstructorUI.Celeste; texto = CelesteTexto; }
        else { relleno = ConstructorUI.Rojo; texto = Color.white; }
    }

    // Un anillo celeste detras de un boton redondo, un poco mas grande que el.
    public static void Anillo(Transform boton)
    {
        var anillo = AssetDatabase.LoadAssetAtPath<Sprite>(RutaAnillo);
        var rtBoton = boton as RectTransform;
        if (anillo == null || rtBoton == null) return;
        var hijo = boton.Find("Neon");
        if (hijo == null)
        {
            var go = new GameObject("Neon", typeof(RectTransform));
            go.layer = boton.gameObject.layer;
            go.transform.SetParent(boton, false);
            hijo = go.transform;
        }
        hijo.SetAsFirstSibling();
        var rt = (RectTransform)hijo;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        // La linea del dibujo esta a 58 de 160 del centro: queda justa en el borde del boton.
        rt.sizeDelta = rtBoton.sizeDelta * (160f / 116f);
        var img = hijo.GetComponent<Image>();
        if (img == null) img = hijo.gameObject.AddComponent<Image>();
        img.sprite = anillo;
        img.type = Image.Type.Simple;
        img.raycastTarget = false;
        img.color = new Color(ConstructorUI.Celeste.r, ConstructorUI.Celeste.g, ConstructorUI.Celeste.b, 0.85f);
    }

    // Una ventana de la escena: las puntas del radio del neon y el borde celeste.
    public static void VentanaNeon(RectTransform ventana, Sprite pildora)
    {
        var fondo = ventana.GetComponent<Image>();
        if (fondo != null && pildora != null)
        {
            fondo.sprite = pildora;
            fondo.type = Image.Type.Sliced;
            fondo.pixelsPerUnitMultiplier = 127f / ConstructorUI.RadioNeon;
        }
        var borde = AssetDatabase.LoadAssetAtPath<Sprite>(RutaBorde);
        if (borde == null) return;
        var hijo = ventana.Find("Neon");
        if (hijo == null)
        {
            var go = new GameObject("Neon", typeof(RectTransform));
            go.layer = ventana.gameObject.layer;
            go.transform.SetParent(ventana, false);
            hijo = go.transform;
        }
        hijo.SetAsFirstSibling();
        var rt = (RectTransform)hijo;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(-40f, -40f);
        rt.offsetMax = new Vector2(40f, 40f);
        var img = hijo.GetComponent<Image>();
        if (img == null) img = hijo.gameObject.AddComponent<Image>();
        img.sprite = borde;
        img.type = Image.Type.Sliced;
        img.raycastTarget = false;
        img.color = new Color(ConstructorUI.Celeste.r, ConstructorUI.Celeste.g, ConstructorUI.Celeste.b, 0.85f);
    }

    // Un titulo: la cara rosa y el halo magenta del material Neon, sin el degrade de antes.
    public static void TituloNeon(TMP_Text titulo, Material neon)
    {
        if (titulo == null || neon == null) return;
        titulo.fontSharedMaterial = neon;
        titulo.enableVertexGradient = false;
        titulo.colorGradientPreset = null;
        titulo.color = ConstructorUI.TituloNeon;
        EditorUtility.SetDirty(titulo);
    }

    static void ImportarAnillo()
    {
        AssetDatabase.ImportAsset(RutaAnillo, ImportAssetOptions.ForceSynchronousImport);
        var importador = AssetImporter.GetAtPath(RutaAnillo) as TextureImporter;
        if (importador == null) return;
        if (importador.textureType == TextureImporterType.Sprite && !importador.mipmapEnabled) return;
        importador.textureType = TextureImporterType.Sprite;
        importador.spriteImportMode = SpriteImportMode.Single;
        importador.alphaIsTransparency = true;
        importador.mipmapEnabled = false;
        importador.wrapMode = TextureWrapMode.Clamp;
        importador.SaveAndReimport();
    }
}
