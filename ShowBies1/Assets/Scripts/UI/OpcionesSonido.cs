using TMPro;
using UnityEngine;
using UnityEngine.UI;

// El engranaje del menu y la ventana de opciones que abre: los volumenes de efectos y
// de musica y el modo oscuro. No tiene objetos propios en la escena: al arrancar copia
// el globo y la ventana del idioma (SelectorIdioma) y los adapta, asi se ven igual sin
// mantener dos copias a mano. El engranaje queda a la derecha del globo.
//
// Se llamaba "sonido" cuando solo tenia los dos volumenes; el nombre de la clase quedo
// porque es lo que esta cableado en la escena.
//
// Vive en la raiz del canvas "Main Menu", junto a SelectorIdioma. El atras de
// Android la cierra desde BotonAtrasMenu.
public class OpcionesSonido : MonoBehaviour
{
    public SelectorIdioma selectorIdioma;
    public float separacionDelGlobo = 24f;
    public float duracionRebote = 0.3f;

    // La del idioma mide 620 x 480 y tiene dos botones; esta tiene tres controles.
    private const float AltoVentana = 640f;
    private const float AnchoControl = 500f;

    private GameObject panel;
    private RectTransform ventana;
    private Texture2D texturaEngranaje;
    private Texture2D texturaPerilla;
    private float abiertaDesde = -1f;

    public bool Abierto
    {
        get { return panel != null && panel.activeSelf; }
    }

    // Start y no Awake: SelectorIdioma pone los dibujos del globo en su Awake, y la
    // copia los tiene que traer puestos.
    private void Start()
    {
        if (selectorIdioma == null || selectorIdioma.botonGlobo == null || selectorIdioma.panel == null) return;

        texturaEngranaje = TexturasUI.Engranaje(128);
        texturaPerilla = TexturasUI.Circulo(64);

        CrearEngranaje();
        CrearVentana();
    }

    private void OnDestroy()
    {
        if (texturaEngranaje != null) Destroy(texturaEngranaje);
        if (texturaPerilla != null) Destroy(texturaPerilla);
    }

    private void CrearEngranaje()
    {
        var globo = (RectTransform)selectorIdioma.botonGlobo.transform;
        var copia = Instantiate(globo.gameObject, globo.parent);
        copia.name = "BotonSonido";
        var rt = (RectTransform)copia.transform;
        rt.anchoredPosition = globo.anchoredPosition + new Vector2(globo.rect.width + separacionDelGlobo, 0f);

        if (selectorIdioma.iconoGlobo != null)
        {
            var icono = copia.transform.Find(Ruta(selectorIdioma.iconoGlobo.transform, globo));
            var imagen = icono != null ? icono.GetComponent<Image>() : null;
            if (imagen != null) imagen.sprite = Sprite.Create(texturaEngranaje, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f));
        }

        var boton = copia.GetComponent<Button>();
        boton.onClick.RemoveAllListeners();
        boton.onClick.AddListener(Abrir);
    }

    private void CrearVentana()
    {
        var original = selectorIdioma.panel;
        panel = Instantiate(original, original.transform.parent);
        panel.name = "PanelSonido";
        panel.SetActive(false);

        var ventanaOriginal = selectorIdioma.ventana;
        ventana = (RectTransform)panel.transform.Find(Ruta(ventanaOriginal, (RectTransform)original.transform));
        // Mas alta que la del idioma: entran tres controles en vez de dos botones.
        ventana.sizeDelta = new Vector2(ventana.sizeDelta.x, AltoVentana);

        // El titulo pasa a OPCIONES y sube, que la ventana creció.
        foreach (var t in panel.GetComponentsInChildren<TextoTraducido>(true))
        {
            if (t.id != "idioma_titulo") continue;
            t.id = "opciones_titulo";
            ((RectTransform)t.transform).anchoredPosition = new Vector2(0f, 255f);
        }

        // Los botones de idioma se van; en su lugar, los volumenes y el modo oscuro. De
        // paso, de uno salen la pildora y la fuente con que se arman los controles.
        TMP_FontAsset fuente = null;
        Sprite pildora = null;
        var botones = selectorIdioma.botonesIdioma;
        for (int i = 0; i < botones.Length; i++)
        {
            if (botones[i] == null) continue;
            var copiaBoton = panel.transform.Find(Ruta(botones[i].transform, (RectTransform)original.transform));
            if (copiaBoton == null) continue;
            var texto = copiaBoton.GetComponentInChildren<TMP_Text>(true);
            if (fuente == null && texto != null) fuente = texto.font;
            var fondo = copiaBoton.Find("Visual/Fondo");
            var imagen = fondo != null ? fondo.GetComponent<Image>() : null;
            if (pildora == null && imagen != null) pildora = imagen.sprite;
            Destroy(copiaBoton.gameObject);
        }

        var volver = selectorIdioma.botonVolver != null
            ? panel.transform.Find(Ruta(selectorIdioma.botonVolver.transform, (RectTransform)original.transform))
            : null;
        AudioClip sonidoClick = null;
        if (volver != null)
        {
            var jugoso = volver.GetComponent<BotonJugoso>();
            if (jugoso != null) sonidoClick = jugoso.sonidoClick;
            var boton = volver.GetComponent<Button>();
            if (boton != null)
            {
                boton.onClick.RemoveAllListeners();
                boton.onClick.AddListener(Cerrar);
            }
            ((RectTransform)volver).anchoredPosition = new Vector2(0f, -255f);
        }

        var perilla = Sprite.Create(texturaPerilla, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));
        SliderVolumen.Crear(ventana, "sonido_efectos", new Vector2(0f, 140f), AnchoControl, fuente, Volumen.Efectos, Volumen.FijarEfectos, perilla);
        SliderVolumen.Crear(ventana, "sonido_musica", new Vector2(0f, 25f), AnchoControl, fuente, Volumen.Musica, Volumen.FijarMusica, perilla);
        Interruptor.Crear(ventana, "opciones_tema", new Vector2(0f, -95f), AnchoControl, fuente, Tema.Oscuro, Tema.Fijar,
                          pildora, perilla, sonidoClick);
    }

    public void Abrir()
    {
        if (panel == null) return;
        if (selectorIdioma != null) selectorIdioma.Cerrar();
        panel.SetActive(true);
        abiertaDesde = Time.unscaledTime;
        if (ventana != null) ventana.localScale = Vector3.zero;
    }

    public void Cerrar()
    {
        if (panel != null) panel.SetActive(false);
        abiertaDesde = -1f;
        Volumen.Guardar();
    }

    private void Update()
    {
        if (abiertaDesde < 0f || ventana == null) return;
        float t = duracionRebote > 0f ? Mathf.Clamp01((Time.unscaledTime - abiertaDesde) / duracionRebote) : 1f;
        ventana.localScale = Vector3.one * CurvasUI.SalidaAtras(t);
        if (t >= 1f) abiertaDesde = -1f;
    }

    // La ruta de un hijo desde un ancestro, para encontrar lo mismo en la copia.
    private static string Ruta(Transform hijo, Transform ancestro)
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
