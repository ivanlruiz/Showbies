using TMPro;
using UnityEngine;
using UnityEngine.UI;

// El engranaje del menu y la ventanita de sonido que abre, con los volumenes de
// efectos y de musica. No tiene objetos propios en la escena: al arrancar copia el
// globo y la ventana del idioma (SelectorIdioma) y los adapta, asi se ven igual sin
// mantener dos copias a mano. El engranaje queda a la derecha del globo.
//
// Vive en la raiz del canvas "Main Menu", junto a SelectorIdioma. El atras de
// Android la cierra desde BotonAtrasMenu.
public class OpcionesSonido : MonoBehaviour
{
    public SelectorIdioma selectorIdioma;
    public float separacionDelGlobo = 24f;
    public float duracionRebote = 0.3f;

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

        // El titulo pasa a SONIDO.
        foreach (var t in panel.GetComponentsInChildren<TextoTraducido>(true))
        {
            if (t.id == "idioma_titulo") t.id = "sonido_titulo";
        }

        // Los botones de idioma se van; en su lugar, los dos volumenes.
        TMP_FontAsset fuente = null;
        Vector2 posicionEfectos = new Vector2(0f, 60f), posicionMusica = new Vector2(0f, -70f);
        float ancho = 560f;
        var botones = selectorIdioma.botonesIdioma;
        for (int i = 0; i < botones.Length; i++)
        {
            if (botones[i] == null) continue;
            var copiaBoton = panel.transform.Find(Ruta(botones[i].transform, (RectTransform)original.transform));
            if (copiaBoton == null) continue;
            var rtBoton = (RectTransform)copiaBoton;
            if (i == 0) { posicionEfectos = rtBoton.anchoredPosition; ancho = rtBoton.rect.width; }
            if (i == 1) posicionMusica = rtBoton.anchoredPosition;
            var texto = copiaBoton.GetComponentInChildren<TMP_Text>(true);
            if (fuente == null && texto != null) fuente = texto.font;
            Destroy(copiaBoton.gameObject);
        }

        var perilla = Sprite.Create(texturaPerilla, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));
        SliderVolumen.Crear(ventana, "sonido_efectos", posicionEfectos, ancho, fuente, Volumen.Efectos, Volumen.FijarEfectos, perilla);
        SliderVolumen.Crear(ventana, "sonido_musica", posicionMusica, ancho, fuente, Volumen.Musica, Volumen.FijarMusica, perilla);

        if (selectorIdioma.botonVolver != null)
        {
            var volver = panel.transform.Find(Ruta(selectorIdioma.botonVolver.transform, (RectTransform)original.transform));
            var boton = volver != null ? volver.GetComponent<Button>() : null;
            if (boton != null)
            {
                boton.onClick.RemoveAllListeners();
                boton.onClick.AddListener(Cerrar);
            }
        }
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
