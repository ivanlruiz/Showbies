using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Las piezas con que se arman en codigo las ventanas del menu (misiones, bestiario):
// rectangulos, textos, fondos redondeados, botones pildora con el molde de siempre y
// los botones redondos de las esquinas, copiados del globo del idioma. Asi se ven
// todas iguales sin repetir el mismo codigo en cada una.
public static class ConstructorUI
{
    // --- Carbón neón (ver Tema): la paleta de los botones y los bordes, la misma de la
    // tienda (ConstructorTienda) y de las escenas (ConstructorNeon).
    public static readonly Color Celeste = new Color(0f, 0.898f, 1f, 1f);            // #00E5FF, los bordes
    public static readonly Color Magenta = new Color(1f, 0.169f, 0.839f, 1f);        // #FF2BD6, el halo de los titulos
    public static readonly Color TituloNeon = new Color(1f, 0.702f, 0.949f, 1f);     // #FFB3F2, la cara de los titulos
    public static readonly Color Verde = new Color(0.224f, 1f, 0.533f, 1f);          // #39FF88: jugar, cobrar
    public static readonly Color VerdeTexto = new Color(0.012f, 0.125f, 0.059f, 1f);
    public static readonly Color Amarillo = new Color(1f, 0.882f, 0.302f, 1f);       // #FFE14D: la tienda, el tope
    public static readonly Color AmarilloTexto = new Color(0.102f, 0.086f, 0f, 1f);
    public static readonly Color Naranja = new Color(1f, 0.624f, 0.11f, 1f);         // #FF9F1C: las oleadas, el video
    public static readonly Color NaranjaTexto = new Color(0.165f, 0.078f, 0f, 1f);
    public static readonly Color Rojo = new Color(1f, 0.231f, 0.361f, 1f);           // #FF3B5C

    // El radio de las puntas de la linea de NeonBorde: las ventanas se redondean igual
    // para que la linea les quede justa.
    public const float RadioNeon = 26f;

    // Una ventana de carbón neón: el fondo con las puntas del radio del neón y el borde
    // celeste que brilla, que sobresale 40 de cada lado (el margen que tiene el dibujo).
    // Los brillos estan en Sprites/UI/Resources para que lo armado en codigo los tenga
    // sin cablear nada.
    public static void VentanaNeon(RectTransform ventana, Image fondo, Sprite pildora)
    {
        if (fondo != null && pildora != null) Redondear(fondo, pildora, 127f / RadioNeon);
        var borde = Resources.Load<Sprite>("NeonBorde");
        if (ventana == null || borde == null) return;
        var halo = Estirar(ventana, "Neon");
        halo.SetAsFirstSibling();
        halo.offsetMin = new Vector2(-40f, -40f);
        halo.offsetMax = new Vector2(40f, 40f);
        var img = halo.gameObject.AddComponent<Image>();
        img.sprite = borde;
        img.type = Image.Type.Sliced;
        img.color = new Color(Celeste.r, Celeste.g, Celeste.b, 0.85f);
        img.raycastTarget = false;
    }

    // El halo que va detras de un boton del color del boton; uno de vidrio (translucido o
    // gris) lo lleva celeste y tenue.
    public static Color ColorDeHalo(Color boton)
    {
        float h, s, v;
        Color.RGBToHSV(boton, out h, out s, out v);
        bool vidrio = boton.a < 0.95f || s < 0.35f;
        return vidrio ? new Color(Celeste.r, Celeste.g, Celeste.b, 0.3f) : new Color(boton.r, boton.g, boton.b, 0.5f);
    }

    // La sombra de un boton pasa a ser su halo de neón: la pildora borrosa, 34 mas grande
    // de cada lado y sin correrse. Sin el sprite, la sombra oscura de antes.
    public static void HaloDeBoton(Image sombra, Color colorDelBoton)
    {
        if (sombra == null) return;
        var halo = Resources.Load<Sprite>("NeonPildora");
        if (halo == null) return;
        var rt = sombra.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(-34f, -34f);
        rt.offsetMax = new Vector2(34f, 34f);
        sombra.sprite = halo;
        sombra.type = Image.Type.Sliced;
        sombra.pixelsPerUnitMultiplier = 1f;
        sombra.color = ColorDeHalo(colorDelBoton);
    }

    // Para un boton que cambia de color (el cofre de las misiones): el halo lo sigue.
    public static void PintarHalo(Button boton, Color colorDelBoton)
    {
        var sombra = boton != null ? boton.transform.Find("Sombra") : null;
        var img = sombra != null ? sombra.GetComponent<Image>() : null;
        if (img != null && img.sprite != null && img.sprite.name == "NeonPildora") img.color = ColorDeHalo(colorDelBoton);
    }

    public static RectTransform Rect(RectTransform padre, string nombre, Vector2 posicion, Vector2 tamanio)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(padre, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = tamanio;
        rt.anchoredPosition = posicion;
        return rt;
    }

    public static RectTransform Estirar(RectTransform padre, string nombre)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(padre, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return rt;
    }

    public static TMP_Text Texto(RectTransform padre, string nombre, string texto, float tamanio, Color color,
                                 Vector2 posicion, Vector2 caja, TMP_FontAsset fuente)
    {
        var rt = Rect(padre, nombre, posicion, caja);
        var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (fuente != null) tmp.font = fuente;
        tmp.text = texto;
        tmp.fontSize = tamanio;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.raycastTarget = false;
        return tmp;
    }

    public static Image Imagen(RectTransform padre, string nombre, Vector2 posicion, Vector2 tamanio, Sprite sprite, Color color)
    {
        var rt = Rect(padre, nombre, posicion, tamanio);
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.preserveAspect = sprite != null;
        img.raycastTarget = false;
        return img;
    }

    // Un fondo con las puntas redondeadas: la pildora en Sliced, con bordes mas chicos
    // cuanto mayor el multiplicador.
    public static void Redondear(Image img, Sprite pildora, float multiplicador)
    {
        img.sprite = pildora;
        img.type = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = multiplicador;
    }

    // Una barra de avance sin sprite: el relleno crece con el ancla derecha (un Image
    // Filled sin sprite no llena). Devuelve el relleno.
    public static RectTransform Barra(RectTransform padre, string nombre, Vector2 posicion, Vector2 tamanio,
                                      Sprite pildora, Color fondo, Color color)
    {
        var barra = Rect(padre, nombre, posicion, tamanio);
        var img = barra.gameObject.AddComponent<Image>();
        Redondear(img, pildora, 6f);
        img.color = fondo;
        img.raycastTarget = false;
        var relleno = Estirar(barra, "Relleno");
        relleno.anchorMax = new Vector2(0f, 1f);
        var imgRelleno = relleno.gameObject.AddComponent<Image>();
        Redondear(imgRelleno, pildora, 6f);
        imgRelleno.color = color;
        imgRelleno.raycastTarget = false;
        return relleno;
    }

    // Un boton pildora con el molde de siempre: la raiz recibe el toque, Visual se anima
    // (BotonJugoso lo toma como primer hijo), Sombra corrida abajo, Fondo, Icono y Texto.
    public static Button Boton(RectTransform padre, string nombre, Vector2 posicion, Vector2 tamanio, Color color,
                               Color colorTexto, Sprite dibujo, string texto, float tamanioTexto,
                               TMP_FontAsset fuente, Sprite pildora, AudioClip sonidoClick)
    {
        var raiz = Rect(padre, nombre, posicion, tamanio);
        var toque = raiz.gameObject.AddComponent<Image>();
        toque.color = new Color(1f, 1f, 1f, 0f);
        var button = raiz.gameObject.AddComponent<Button>();
        button.targetGraphic = toque;

        var visual = Rect(raiz, "Visual", Vector2.zero, tamanio);
        var fondo = Rect(visual, "Fondo", Vector2.zero, tamanio);
        var imgFondo = fondo.gameObject.AddComponent<Image>();
        imgFondo.sprite = pildora;
        imgFondo.type = Image.Type.Sliced;
        imgFondo.color = color;
        imgFondo.raycastTarget = false;

        var tmp = Texto(visual, "Texto", texto, tamanioTexto, colorTexto, Vector2.zero, tamanio, fuente);
        if (dibujo != null)
        {
            var icono = Rect(visual, "Icono", Vector2.zero, new Vector2(tamanio.y * 0.42f, tamanio.y * 0.42f));
            icono.anchorMin = icono.anchorMax = new Vector2(0f, 0.5f);
            var imgIcono = icono.gameObject.AddComponent<Image>();
            imgIcono.sprite = dibujo;
            imgIcono.color = colorTexto;
            imgIcono.preserveAspect = true;
            imgIcono.raycastTarget = false;
            icono.gameObject.AddComponent<IconoDeBoton>().texto = tmp;
        }

        var jugoso = raiz.gameObject.AddComponent<BotonJugoso>();
        jugoso.sonidoClick = sonidoClick;

        var sombra = Rect(raiz, "Sombra", new Vector2(0f, -8f), tamanio);
        sombra.SetAsFirstSibling();
        var imgSombra = sombra.gameObject.AddComponent<Image>();
        imgSombra.sprite = pildora;
        imgSombra.type = Image.Type.Sliced;
        imgSombra.color = new Color(0f, 0f, 0f, 0.3f);
        imgSombra.raycastTarget = false;
        HaloDeBoton(imgSombra, color);
        return button;
    }

    // Un boton redondo de las esquinas del menu: una copia del globo del idioma, con otro
    // dibujo, anclada arriba a la derecha a 'desdeLaDerecha' del borde (el globo esta a la
    // izquierda). Se llama en Start, cuando SelectorIdioma ya le puso sus dibujos.
    public static Button BotonDeEsquina(SelectorIdioma selector, string nombre, float desdeLaDerecha, Sprite dibujo,
                                        UnityEngine.Events.UnityAction alTocar)
    {
        if (selector == null || selector.botonGlobo == null) return null;
        var globo = (RectTransform)selector.botonGlobo.transform;
        var copia = Object.Instantiate(globo.gameObject, globo.parent);
        copia.name = nombre;
        var rt = (RectTransform)copia.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-desdeLaDerecha, globo.anchoredPosition.y);

        if (selector.iconoGlobo != null && dibujo != null)
        {
            var icono = copia.transform.Find(Ruta(selector.iconoGlobo.transform, globo));
            if (icono != null) icono.GetComponent<Image>().sprite = dibujo;
        }

        var boton = copia.GetComponent<Button>();
        boton.onClick = new Button.ButtonClickedEvent();
        boton.onClick.AddListener(alTocar);
        return boton;
    }

    // La insignia roja con un numero, copiada de la del boton MEJORAS, en la esquina de
    // arriba a la derecha de 'boton'.
    public static RectTransform Insignia(Button botonMejoras, RectTransform boton, out TMP_Text numero)
    {
        numero = null;
        var molde = botonMejoras != null ? botonMejoras.transform.Find("Insignia") : null;
        if (molde == null || boton == null) return null;
        var insignia = (RectTransform)Object.Instantiate(molde.gameObject, boton).transform;
        insignia.name = "Insignia";
        insignia.anchorMin = insignia.anchorMax = new Vector2(1f, 1f);
        insignia.pivot = new Vector2(0.5f, 0.5f);
        insignia.anchoredPosition = new Vector2(-10f, -10f);
        insignia.sizeDelta = new Vector2(48f, 48f);
        numero = insignia.GetComponentInChildren<TMP_Text>(true);
        if (numero != null) numero.fontSize = 30f;
        return insignia;
    }

    // La insignia late cuando hay algo (como la de MEJORAS) y se esconde cuando no.
    public static void Latir(RectTransform insignia, TMP_Text numero, int cuenta, float reloj)
    {
        if (insignia == null) return;
        insignia.gameObject.SetActive(cuenta > 0);
        if (cuenta <= 0) return;
        if (numero != null) numero.text = cuenta.ToString();
        float latido = Mathf.Abs(Mathf.Sin(reloj * Mathf.PI / 1.4f));
        insignia.localScale = Vector3.one * (1f + 0.25f * latido * latido);
    }

    // La ruta de un hijo desde un ancestro, para encontrar lo mismo en una copia.
    public static string Ruta(Transform hijo, Transform ancestro)
    {
        string ruta = hijo.name;
        var t = hijo.parent;
        while (t != null && t != ancestro)
        {
            ruta = t.name + "/" + ruta;
            t = t.parent;
        }
        return ruta;
    }
}
