using UnityEngine;
using UnityEngine.UI;
using TMPro;

// El globo del menu y la ventanita que abre: un boton por idioma, cada uno escrito
// en si mismo ("English", "Español"), con el elegido en dorado. Tocar uno lo aplica
// en el acto (todo lo que esta en pantalla se reescribe solo) y cierra la ventana.
//
// El globo es la convencion para "idioma" y no dice nada en ningun idioma: lo
// encuentra alguien que no lee ingles, que es justo quien lo necesita, porque el
// juego arranca en ingles.
//
// Vive en la raiz del canvas "Main Menu" de Menu.unity. El atras de Android lo
// cierra desde BotonAtrasMenu, que es el unico lector de Escape del menu.
public class SelectorIdioma : MonoBehaviour
{
    [Header("El globo")]
    public Button botonGlobo;
    [Tooltip("El circulo de fondo y su sombra, y el dibujo del globo: los arma TexturasUI.")]
    public Image fondoGlobo;
    public Image sombraGlobo;
    public Image iconoGlobo;

    [Header("La ventana")]
    [Tooltip("Todo lo que se ve al abrir: arranca apagado.")]
    public GameObject panel;
    public RectTransform ventana;
    public Button botonVolver;

    [Tooltip("Un boton por idioma, en el orden de Idioma.Todas.")]
    public Button[] botonesIdioma;
    public Image[] fondosIdioma;
    public TMP_Text[] textosIdioma;

    public Color colorElegido = new Color(1f, 0.78f, 0.22f, 1f);
    public Color colorOtro = new Color(0.55f, 0.55f, 0.65f, 1f);
    public float duracionRebote = 0.3f;

    private Texture2D texturaCirculo;
    private Texture2D texturaGlobo;
    private float abiertaDesde = -1f;

    public bool Abierto
    {
        get { return panel != null && panel.activeSelf; }
    }

    private void Awake()
    {
        if (panel != null) panel.SetActive(false);

        // Los dibujos se hacen en codigo para no sumar imagenes: esta clase es duenia
        // de las texturas y las destruye.
        texturaCirculo = TexturasUI.Circulo(128);
        texturaGlobo = TexturasUI.Globo(128);
        Vestir(fondoGlobo, texturaCirculo);
        Vestir(sombraGlobo, texturaCirculo);
        Vestir(iconoGlobo, texturaGlobo);

        if (botonGlobo != null) botonGlobo.onClick.AddListener(Abrir);
        if (botonVolver != null) botonVolver.onClick.AddListener(Cerrar);

        for (int i = 0; i < botonesIdioma.Length && i < Idioma.Todas.Length; i++)
        {
            if (botonesIdioma[i] == null) continue;
            int indice = i;
            botonesIdioma[i].onClick.AddListener(() => Elegir(indice));
        }
    }

    private void OnDestroy()
    {
        if (texturaCirculo != null) Destroy(texturaCirculo);
        if (texturaGlobo != null) Destroy(texturaGlobo);
    }

    private static void Vestir(Image imagen, Texture2D textura)
    {
        if (imagen == null || textura == null) return;
        imagen.sprite = Sprite.Create(textura, new Rect(0f, 0f, textura.width, textura.height),
                                      new Vector2(0.5f, 0.5f), 100f, 0u, SpriteMeshType.FullRect);
    }

    public void Abrir()
    {
        if (panel == null) return;
        Pintar();
        panel.SetActive(true);
        abiertaDesde = Time.unscaledTime;
        if (ventana != null) ventana.localScale = Vector3.zero;
    }

    public void Cerrar()
    {
        if (panel != null) panel.SetActive(false);
        abiertaDesde = -1f;
    }

    private void Elegir(int indice)
    {
        if (indice < 0 || indice >= Idioma.Todas.Length) return;
        Idioma.Cambiar(Idioma.Todas[indice]);
        Pintar();
        Cerrar();
    }

    // Los nombres no se traducen: cada idioma escrito en si mismo.
    private void Pintar()
    {
        for (int i = 0; i < Idioma.Todas.Length; i++)
        {
            Lengua lengua = Idioma.Todas[i];
            bool elegido = lengua == Idioma.Actual;
            if (i < textosIdioma.Length && textosIdioma[i] != null) textosIdioma[i].text = Idioma.NombrePropio(lengua);
            if (i < fondosIdioma.Length && fondosIdioma[i] != null) fondosIdioma[i].color = elegido ? colorElegido : colorOtro;
        }
    }

    private void Update()
    {
        if (abiertaDesde < 0f || ventana == null) return;

        float t = duracionRebote > 0f ? Mathf.Clamp01((Time.unscaledTime - abiertaDesde) / duracionRebote) : 1f;
        ventana.localScale = Vector3.one * CurvasUI.SalidaAtras(t);
        if (t >= 1f) abiertaDesde = -1f;
    }
}
