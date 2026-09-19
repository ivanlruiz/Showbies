using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Las piezas con que se arman en codigo las ventanas del menu (misiones, bestiario):
// rectangulos, textos, fondos redondeados, botones pildora con el molde de siempre y
// los botones redondos de las esquinas, copiados del globo del idioma. Asi se ven
// todas iguales sin repetir el mismo codigo en cada una.
public static class ConstructorUI
{
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
