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

        // El globo y su anillo, que heredan sus copias. Su sombra de antes, un circulo negro
        // corrido abajo, se apaga: con el anillo se veia como un borde doble.
        if (menu.botonGlobo != null) Anillo(menu.botonGlobo.transform);
        if (menu.sombraGlobo != null)
        {
            menu.sombraGlobo.enabled = false;
            EditorUtility.SetDirty(menu.sombraGlobo);
        }

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

        foreach (var raizDeLaEscena in escena.GetRootGameObjects()) RedondearBotones(raizDeLaEscena);
        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);
        Debug.Log("ConstructorNeon: el menú quedó de carbón neón");
    }

    // --- La partida: la derrota, la pausa, el revivir, la furia y el HUD -------------

    const string RutaMaterialHud = "Assets/Fuentes/Bangers SDF - Neon HUD.mat";
    const string RutaMaterialContorno = "Assets/Fuentes/Bangers SDF - Outline.mat";
    static readonly string[] EscenasDeJuego = { "Assets/Escenas/ShowBies1.unity", "Assets/Escenas/WaveMode.unity", "Assets/Escenas/Tutorial.unity" };

    [MenuItem("ShowBies/Neón/Vestir la partida")]
    public static void VestirPartida()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("ConstructorNeon: en play no");
            return;
        }
        if (SceneManager.GetActiveScene().isDirty)
        {
            Debug.LogError("ConstructorNeon: la escena abierta tiene cambios sin guardar");
            return;
        }
        ImportarAnillo();
        var neon = AssetDatabase.LoadAssetAtPath<Material>(RutaMaterialNeon);
        var hud = MaterialHud();
        var pildora = AssetDatabase.LoadAssetAtPath<Sprite>(RutaPildora);
        var rojoAnillo = new Color(ConstructorUI.Rojo.r, ConstructorUI.Rojo.g, ConstructorUI.Rojo.b, 0.85f);
        var naranjaAnillo = new Color(ConstructorUI.Naranja.r, ConstructorUI.Naranja.g, ConstructorUI.Naranja.b, 0.85f);

        // La pausa: el boton redondo con su anillo, el titulo y los tres botones.
        VestirPrefab("Assets/Prefabs/UI/MenuPausa.prefab", raiz =>
        {
            var pausa = raiz.transform.Find("AreaSegura/BotonPausa");
            if (pausa != null)
            {
                pausa.GetComponent<Image>().color = Tema.VidrioClaro;
                Anillo(pausa);
            }
            var panel = raiz.transform.Find("Panel");
            panel.GetComponent<Image>().color = new Color(Tema.FondoOscuro.r, Tema.FondoOscuro.g, Tema.FondoOscuro.b, 0.8f);
            TituloNeon(panel.Find("Titulo").GetComponent<TMP_Text>(), neon);
            foreach (var nombre in new[] { "BotonContinuar", "BotonReiniciar", "BotonMenu" }) VestirBoton(panel.Find(nombre));
        });

        // El revivir: la ventanita de neon, el titulo rojo con halo y el boton del video naranja.
        VestirPrefab("Assets/Prefabs/UI/OfertaRevivir.prefab", raiz =>
        {
            var ventana = (RectTransform)raiz.transform.Find("Panel/Ventana");
            ventana.GetComponent<Image>().color = new Color(Tema.PanelOscuro.r, Tema.PanelOscuro.g, Tema.PanelOscuro.b, 0.92f);
            // 20 mas alta que antes: con la linea del borde, NO, GRACIAS quedaba pegado abajo.
            ventana.sizeDelta = new Vector2(ventana.sizeDelta.x, 360f);
            VentanaNeon(ventana, pildora);
            var titulo = ventana.Find("Titulo").GetComponent<TMP_Text>();
            TituloNeon(titulo, neon);
            titulo.color = ConstructorUI.Rojo;
            ventana.Find("BotonVideo/Circulo").GetComponent<Image>().color = ConstructorUI.Naranja;
            // Una pildora de verdad: en Simple, la Pildora estirada a 250 x 52 era un ovalo.
            var no = ventana.Find("BotonNo").GetComponent<Image>();
            no.color = Tema.VidrioClaro;
            ConstructorUI.RedondearPildora(no);
        });

        // La furia: el boton redondo con su anillo rojo, sus tres colores y el cartel.
        VestirPrefab("Assets/Prefabs/UI/BotonFuria.prefab", raiz =>
        {
            var furia = raiz.GetComponent<BotonFuria>();
            furia.colorListo = ConstructorUI.Rojo;
            furia.colorActivo = ConstructorUI.Amarillo;
            furia.colorEnfriando = new Color(0.23f, 0.1f, 0.14f, 1f);
            Anillo(raiz.transform.Find("Boton"), rojoAnillo);
            var cartel = raiz.transform.Find("Cartel").GetComponent<TMP_Text>();
            TituloNeon(cartel, neon);
            cartel.color = ConstructorUI.Rojo;
        });

        foreach (var ruta in EscenasDeJuego) VestirEscenaDeJuego(ruta, hud, neon, pildora, naranjaAnillo);
        VestirDerrota(neon);
        Debug.Log("ConstructorNeon: la partida quedó de carbón neón");
    }

    // Un cambio hecho por codigo sobre una instancia de prefab de la escena no se guarda si no se
    // anota como override: la escena guardaria el valor del prefab.
    static void Anotar(Object objeto)
    {
        if (PrefabUtility.IsPartOfPrefabInstance(objeto)) PrefabUtility.RecordPrefabInstancePropertyModifications(objeto);
        EditorUtility.SetDirty(objeto);
    }

    static void VestirPrefab(string ruta, System.Action<GameObject> vestir)
    {
        var raiz = PrefabUtility.LoadPrefabContents(ruta);
        try
        {
            vestir(raiz);
            RedondearBotones(raiz);
            PrefabUtility.SaveAsPrefabAsset(raiz, ruta);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(raiz);
        }
    }

    // El HUD: los textos con el material de neon (contorno oscuro fino y halo celeste), las
    // monedas en amarillo, los joysticks celestes, el boton de la granada naranja con su
    // anillo, y en el tutorial sus dos paneles de neon.
    static void VestirEscenaDeJuego(string ruta, Material hud, Material neon, Sprite pildora, Color naranjaAnillo)
    {
        var escena = EditorSceneManager.OpenScene(ruta, OpenSceneMode.Single);
        GameObject canvas = null;
        foreach (var raiz in escena.GetRootGameObjects()) if (raiz.name == "Canvas") canvas = raiz;
        if (canvas == null)
        {
            Debug.LogError("ConstructorNeon: " + ruta + " no tiene su Canvas");
            return;
        }
        var helper = canvas.transform.Find("CanvasHelper");
        var textos = helper.Find("Textos");

        foreach (var texto in textos.GetComponentsInChildren<TMP_Text>(true))
        {
            var material = texto.fontSharedMaterial;
            if (material == null || material.name != "Bangers SDF - Outline") continue;
            texto.fontSharedMaterial = texto.name == "CartelOleada" ? neon : hud;
            if (texto.name == "Monedas") texto.color = ConstructorUI.Amarillo;
            EditorUtility.SetDirty(texto);
        }

        // Los joysticks son instancias del prefab del Joystick Pack: sus colores se anotan como override.
        foreach (var nombre in new[] { "MoveJoystick", "ShootJoystick" })
        {
            var joystick = helper.Find(nombre);
            if (joystick == null) continue;
            var aro = joystick.GetComponent<Image>();
            aro.color = new Color(ConstructorUI.Celeste.r, ConstructorUI.Celeste.g, ConstructorUI.Celeste.b, 0.75f);
            Anotar(aro);
            var mango = joystick.Find("Handle");
            if (mango == null) continue;
            var imagenMango = mango.GetComponent<Image>();
            imagenMango.color = new Color(0.75f, 0.97f, 1f, 0.9f);
            Anotar(imagenMango);
        }

        var granada = helper.Find("BotonGranada");
        if (granada != null)
        {
            granada.GetComponent<Image>().color = ConstructorUI.Naranja;
            var etiqueta = granada.Find("Etiqueta");
            if (etiqueta != null) etiqueta.GetComponent<TMP_Text>().color = ConstructorUI.NaranjaTexto;
            Anillo(granada, naranjaAnillo);
        }
        var relleno = textos.Find("MejoraCadencia/Contenido/Relleno");
        if (relleno != null) relleno.GetComponent<Image>().color = ConstructorUI.Amarillo;

        // El tutorial: la instruccion en una franja de neon y el panel del final como ventana.
        var instruccion = textos.Find("PanelInstruccion");
        if (instruccion != null)
        {
            instruccion.GetComponent<Image>().color = new Color(Tema.PanelOscuro.r, Tema.PanelOscuro.g, Tema.PanelOscuro.b, 0.9f);
            VentanaNeon((RectTransform)instruccion, pildora);
        }
        var final = textos.Find("PanelFinal");
        if (final != null)
        {
            final.GetComponent<Image>().color = new Color(Tema.PanelOscuro.r, Tema.PanelOscuro.g, Tema.PanelOscuro.b, 0.96f);
            VentanaNeon((RectTransform)final, pildora);
            var titulo = final.Find("Titulo");
            if (titulo != null) TituloNeon(titulo.GetComponent<TMP_Text>(), neon);
            VestirBoton(final.Find("BotonJugar"));
            VestirBoton(final.Find("BotonMenu"));
        }

        // Los avisos de la partida ("¡MISION CUMPLIDA!", "¡NIVEL 13!") con el halo del HUD.
        var avisos = Object.FindFirstObjectByType<AvisoDeMisiones>(FindObjectsInactive.Include);
        if (avisos != null)
        {
            avisos.materialContorno = hud;
            EditorUtility.SetDirty(avisos);
        }

        foreach (var raizDeLaEscena in escena.GetRootGameObjects()) RedondearBotones(raizDeLaEscena);
        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);
    }

    // La derrota: GAME OVER rojo con halo, las monedas en amarillo, los botones de neon y
    // la barra del proximo objetivo en verde.
    static void VestirDerrota(Material neon)
    {
        var escena = EditorSceneManager.OpenScene("Assets/Escenas/Perdiste.unity", OpenSceneMode.Single);
        var menu = Object.FindFirstObjectByType<MenuPerdiste>(FindObjectsInactive.Include);
        var bg = menu != null ? menu.transform.Find("Canvas/BG") : null;
        if (bg == null)
        {
            Debug.LogError("ConstructorNeon: la derrota no tiene su BG");
            return;
        }
        var titulo = bg.Find("Perdiste").GetComponent<TMP_Text>();
        TituloNeon(titulo, neon);
        titulo.color = ConstructorUI.Rojo;
        var monedas = bg.Find("Monedas").GetComponent<TMP_Text>();
        monedas.color = ConstructorUI.Amarillo;
        EditorUtility.SetDirty(monedas);
        foreach (var nombre in new[] { "Jugar", "Mejoras", "AlMenu", "OfertaVideo" }) VestirBoton(bg.Find(nombre));

        var objetivo = Object.FindFirstObjectByType<ProximoObjetivo>(FindObjectsInactive.Include);
        if (objetivo != null)
        {
            objetivo.colorBarra = ConstructorUI.Verde;
            objetivo.colorFondoBarra = new Color(1f, 1f, 1f, 0.14f);
            EditorUtility.SetDirty(objetivo);
        }
        foreach (var raizDeLaEscena in escena.GetRootGameObjects()) RedondearBotones(raizDeLaEscena);
        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);
    }

    // El material de los textos del HUD: el contorno oscuro de siempre, mas fino, para que se
    // lean sobre el mundo, y un halo celeste alrededor.
    static Material MaterialHud()
    {
        var baseMat = AssetDatabase.LoadAssetAtPath<Material>(RutaMaterialContorno);
        if (baseMat == null) return null;
        var hud = AssetDatabase.LoadAssetAtPath<Material>(RutaMaterialHud);
        if (hud == null)
        {
            hud = new Material(baseMat);
            AssetDatabase.CreateAsset(hud, RutaMaterialHud);
        }
        hud.shader = baseMat.shader;
        hud.CopyPropertiesFromMaterial(baseMat);
        hud.shaderKeywords = baseMat.shaderKeywords;
        hud.SetFloat("_OutlineWidth", 0.16f);
        hud.EnableKeyword("UNDERLAY_ON");
        hud.SetColor("_UnderlayColor", new Color(ConstructorUI.Celeste.r, ConstructorUI.Celeste.g, ConstructorUI.Celeste.b, 0.6f));
        hud.SetFloat("_UnderlayOffsetX", 0f);
        hud.SetFloat("_UnderlayOffsetY", 0f);
        hud.SetFloat("_UnderlayDilate", 0.45f);
        hud.SetFloat("_UnderlaySoftness", 0.7f);
        EditorUtility.SetDirty(hud);
        AssetDatabase.SaveAssets();
        return hud;
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
        // Un vidrio sin pintor (los de los prefabs de la partida) toma el vidrio del neón fijo.
        if (vidrio && pintor == null) fondo.color = Tema.VidrioClaro;
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
        ConstructorUI.RedondearPildora(fondo);
        EditorUtility.SetDirty(boton.gameObject);
    }

    // Las pildoras de los botones (el Fondo del molde de siempre) y sus halos, con las puntas
    // redondas de verdad (ver ConstructorUI.RedondearPildora). Tambien las que no pasan por
    // VestirBoton: los idiomas, que los pinta SelectorIdioma, y los de los prefabs.
    public static void RedondearBotones(GameObject raiz)
    {
        foreach (var img in raiz.GetComponentsInChildren<Image>(true))
        {
            if (img.sprite == null) continue;
            bool pildora = img.sprite.name == "Pildora" && img.name == "Fondo" && img.transform.parent != null && img.transform.parent.name == "Visual";
            bool halo = img.sprite.name == "NeonPildora";
            if (!pildora && !halo) continue;
            ConstructorUI.RedondearPildora(img);
            Anotar(img);
        }
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
        Anillo(boton, new Color(ConstructorUI.Celeste.r, ConstructorUI.Celeste.g, ConstructorUI.Celeste.b, 0.85f));
    }

    public static void Anillo(Transform boton, Color color)
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
        img.color = color;
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
