using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// El boton MISIONES del menu y la ventana que abre, con las tres misiones del dia
// (MisionesDiarias): cada una con su dificultad (verde, amarilla, roja), lo que pide,
// una barra de avance, el premio y COBRAR cuando esta cumplida. Al cobrar, el arpegio
// de la diaria y la fila se pone verde con su tilde. Abajo, cuanto falta para las
// nuevas y VOLVER.
//
// Nada de esto esta en la escena: el boton es una copia redonda del globo del idioma,
// arriba a la derecha y con el portapapeles (pedido de Ivan), con una insignia contando
// las misiones para cobrar, y la ventana se arma en codigo como la de la diaria. Vive en la raiz del canvas
// "Main Menu"; el atras de Android la cierra (BotonAtrasMenu).
public class VentanaMisiones : MonoBehaviour
{
    public SelectorIdioma selectorIdioma;   // el globo, que se copia para el boton
    public Button botonMejoras;             // de donde sale la insignia
    public Sprite iconoBoton;               // IconoMisiones, el portapapeles
    public TMP_FontAsset fuente;
    public Material materialContorno;
    public Sprite pildora;
    public Sprite tilde;
    public Sprite iconoAtras;
    public AudioClip nota;                  // moneda.wav
    public AudioClip sonidoFestejo;         // cartel.wav
    public AudioClip sonidoClick;

    public Color colorVentana = new Color(1f, 0.96f, 0.86f, 1f);
    public Color colorTextoOscuro = new Color(0.16f, 0.14f, 0.2f, 1f);
    public Color colorTitulo = new Color(0.97f, 0.79f, 0.28f, 1f);
    public Color colorFila = new Color(0f, 0f, 0f, 0.06f);
    public Color colorCumplida = new Color(0.49f, 0.88f, 0.29f, 1f);
    public Color colorBarra = new Color(0.3f, 0.75f, 0.2f, 1f);
    public Color colorMoneda = new Color(1f, 0.76f, 0.12f, 1f);
    public Color[] coloresDificultad =
    {
        new Color(0.49f, 0.88f, 0.29f, 1f),
        new Color(1f, 0.79f, 0.2f, 1f),
        new Color(1f, 0.42f, 0.35f, 1f),
    };

    private static readonly int[] SemitonosFestejo = { -12, -8, -5, 0, 4, 7, 12 };

    public static bool Abierta { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reiniciar()
    {
        Abierta = false;
    }

    private class Fila
    {
        public Image fondo;
        public TMP_Text descripcion;
        public RectTransform relleno;
        public TMP_Text cuenta;
        public TMP_Text premio;
        public GameObject cobrar;
        public Image tilde;
        public float golpe = -1f;
        public RectTransform raiz;
    }

    private GameObject panel;
    private RectTransform ventana;
    private TMP_Text textoNuevas;
    private readonly Fila[] filas = new Fila[MisionesDiarias.Cantidad];
    private Texture2D texturaCirculo;
    private Sprite circulo;
    private RectTransform insignia;
    private TMP_Text numeroInsignia;
    private int revisionVista = -1;
    private float reloj = -1f;
    private float relojInsignia;

    private void Start()
    {
        texturaCirculo = TexturasUI.Circulo(64);
        circulo = Sprite.Create(texturaCirculo, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));
        CrearBoton();
        Armar();
        panel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (panel != null) Abierta = false;
        if (texturaCirculo != null) Destroy(texturaCirculo);
    }

    // --- El boton del menu ---------------------------------------------------------

    private void CrearBoton()
    {
        if (selectorIdioma == null || selectorIdioma.botonGlobo == null) return;

        // Una copia del globo, en espejo arriba a la derecha (pedido de Ivan), con el
        // portapapeles en vez del globo: se hace en Start, cuando SelectorIdioma ya le
        // puso sus dibujos, como el engranaje de OpcionesSonido.
        var globo = (RectTransform)selectorIdioma.botonGlobo.transform;
        var copia = Instantiate(globo.gameObject, globo.parent);
        copia.name = "BotonMisiones";
        var rt = (RectTransform)copia.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-globo.anchoredPosition.x, globo.anchoredPosition.y);

        if (selectorIdioma.iconoGlobo != null && iconoBoton != null)
        {
            var icono = copia.transform.Find(Ruta(selectorIdioma.iconoGlobo.transform, globo));
            if (icono != null) icono.GetComponent<Image>().sprite = iconoBoton;
        }

        var boton = copia.GetComponent<Button>();
        boton.onClick = new Button.ButtonClickedEvent();
        boton.onClick.AddListener(Abrir);

        // La insignia es una copia de la de MEJORAS, en la esquina del circulo.
        var molde = botonMejoras != null ? botonMejoras.transform.Find("Insignia") : null;
        if (molde != null)
        {
            insignia = (RectTransform)Instantiate(molde.gameObject, rt).transform;
            insignia.name = "Insignia";
            insignia.anchorMin = insignia.anchorMax = new Vector2(1f, 1f);
            insignia.pivot = new Vector2(0.5f, 0.5f);
            insignia.anchoredPosition = new Vector2(-10f, -10f);
            insignia.sizeDelta = new Vector2(48f, 48f);
            numeroInsignia = insignia.GetComponentInChildren<TMP_Text>(true);
            if (numeroInsignia != null) numeroInsignia.fontSize = 30f;
        }
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

    private void RefrescarInsignia(float dt)
    {
        if (insignia == null) return;
        int n = MisionesDiarias.PorCobrar;
        insignia.gameObject.SetActive(n > 0);
        if (n <= 0) return;
        if (numeroInsignia != null) numeroInsignia.text = n.ToString();
        // Late como la de MEJORAS: hay algo para cobrar.
        relojInsignia += dt;
        float latido = Mathf.Abs(Mathf.Sin(relojInsignia * Mathf.PI / 1.4f));
        insignia.localScale = Vector3.one * (1f + 0.25f * latido * latido);
    }

    // --- La ventana ----------------------------------------------------------------

    public void Abrir()
    {
        if (panel == null) return;
        MisionesDiarias.Asegurar();
        Refrescar();
        panel.SetActive(true);
        panel.transform.SetAsLastSibling();
        Abierta = true;
        reloj = 0f;
        ventana.localScale = Vector3.zero;
    }

    public void Cerrar()
    {
        if (panel != null) panel.SetActive(false);
        Abierta = false;
        reloj = -1f;
    }

    private void Cobrar(int indice)
    {
        double monto = MisionesDiarias.Cobrar(indice);
        if (monto <= 0) return;
        for (int k = 0; k < SemitonosFestejo.Length; k++)
            Sonidos.Programar(nota, 0.05 * k, 0.8f, Sonidos.PitchDe(SemitonosFestejo[k]));
        Sonidos.Tocar(sonidoFestejo, 0.7f);
        filas[indice].golpe = 0f;
        Refrescar();
    }

    private void Update()
    {
        float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
        RefrescarInsignia(dt);
        if (!Abierta) return;

        reloj += dt;
        ventana.localScale = Vector3.one * CurvasUI.SalidaAtras(Mathf.Clamp01(reloj / 0.45f));
        if (Progreso.Revision != revisionVista) Refrescar();
        textoNuevas.text = TextoNuevas();

        foreach (var fila in filas)
        {
            if (fila == null) continue;
            if (fila.golpe >= 0f) fila.golpe += dt;
            float salto = fila.golpe >= 0f ? 0.08f * CurvasUI.Campana(Mathf.Clamp01(fila.golpe / 0.4f)) : 0f;
            fila.raiz.localScale = Vector3.one * (1f + salto);
        }
    }

    private void Refrescar()
    {
        revisionVista = Progreso.Revision;
        var misiones = MisionesDiarias.DeHoy;
        int mejor = Progreso.MejorOleada;
        for (int i = 0; i < filas.Length; i++)
        {
            var fila = filas[i];
            if (fila == null) continue;
            bool hay = i < misiones.Count;
            fila.raiz.gameObject.SetActive(hay);
            if (!hay) continue;

            var mision = misiones[i];
            bool cumplida = MisionesDiarias.Cumplida(mision);
            double avance = Math.Min(MisionesDiarias.Avance(mision), mision.objetivo);
            fila.descripcion.text = MisionesDiarias.Descripcion(mision);
            fila.cuenta.text = FormatoNumeros.Compacto(avance) + " / " + FormatoNumeros.Compacto(mision.objetivo);
            fila.premio.text = FormatoNumeros.Compacto(MisionesDiarias.Monto(mision.dificultad, mejor));
            float fraccion = mision.objetivo > 0 ? Mathf.Clamp01((float)(avance / mision.objetivo)) : 1f;
            fila.relleno.anchorMax = new Vector2(fraccion, 1f);
            fila.fondo.color = mision.cobrada ? colorCumplida : colorFila;
            fila.cobrar.SetActive(cumplida && !mision.cobrada);
            fila.tilde.enabled = mision.cobrada && tilde != null;
        }
    }

    private static string TextoNuevas()
    {
        TimeSpan falta = DateTime.Today.AddDays(1) - DateTime.Now;
        if (falta < TimeSpan.Zero) falta = TimeSpan.Zero;
        return Textos.Formato("misiones_nuevas", (int)falta.TotalHours, falta.Minutes);
    }

    private void Armar()
    {
        panel = new GameObject("VentanaMisiones", typeof(RectTransform));
        var rtPanel = (RectTransform)panel.transform;
        rtPanel.SetParent(transform, false);
        rtPanel.anchorMin = Vector2.zero;
        rtPanel.anchorMax = Vector2.one;
        rtPanel.offsetMin = rtPanel.offsetMax = Vector2.zero;
        panel.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.45f);   // tapa los toques del menu

        ventana = Rect(rtPanel, "Ventana", new Vector2(0f, 10f), new Vector2(1180f, 660f));
        var fondo = ventana.gameObject.AddComponent<Image>();
        Redondear(fondo, 3f);
        fondo.color = colorVentana;

        var titulo = Texto(ventana, "Titulo", Textos.De("misiones_titulo"), 80f, colorTitulo, new Vector2(0f, 250f), new Vector2(1100f, 100f));
        if (materialContorno != null) titulo.fontSharedMaterial = materialContorno;
        textoNuevas = Texto(ventana, "Nuevas", TextoNuevas(), 36f, colorTextoOscuro, new Vector2(0f, 182f), new Vector2(1100f, 50f));

        for (int i = 0; i < filas.Length; i++) filas[i] = ArmarFila(i, 85f - 125f * i);

        var volver = ArmarBoton(ventana, "Volver", new Vector2(0f, -262f), new Vector2(340f, 100f),
                                new Color(0.06f, 0.12f, 0.05f, 0.45f), Color.white, iconoAtras, Textos.De("comun_volver"), 46f);
        volver.onClick.AddListener(Cerrar);
    }

    private Fila ArmarFila(int indice, float y)
    {
        var fila = new Fila();
        fila.raiz = Rect(ventana, "Mision" + indice, new Vector2(0f, y), new Vector2(1080f, 110f));
        fila.fondo = fila.raiz.gameObject.AddComponent<Image>();
        Redondear(fila.fondo, 3f);
        fila.fondo.color = colorFila;

        var punto = Rect(fila.raiz, "Dificultad", new Vector2(-490f, 0f), new Vector2(46f, 46f));
        var imgPunto = punto.gameObject.AddComponent<Image>();
        imgPunto.sprite = circulo;
        imgPunto.color = coloresDificultad[Mathf.Min(indice, coloresDificultad.Length - 1)];

        fila.descripcion = Texto(fila.raiz, "Descripcion", "", 42f, colorTextoOscuro, new Vector2(-150f, 20f), new Vector2(620f, 56f));
        fila.descripcion.alignment = TextAlignmentOptions.Left;

        // La barra: un fondo y un relleno que crece con el ancla derecha (sin sprite, un
        // Image Filled no llena; ver las trampas de CLAUDE.md).
        var barra = Rect(fila.raiz, "Barra", new Vector2(-230f, -24f), new Vector2(460f, 18f));
        var imgBarra = barra.gameObject.AddComponent<Image>();
        Redondear(imgBarra, 6f);
        imgBarra.color = new Color(0f, 0f, 0f, 0.12f);
        fila.relleno = Rect(barra, "Relleno", Vector2.zero, Vector2.zero);
        fila.relleno.anchorMin = Vector2.zero;
        fila.relleno.anchorMax = new Vector2(0f, 1f);
        fila.relleno.pivot = new Vector2(0f, 0.5f);
        fila.relleno.offsetMin = fila.relleno.offsetMax = Vector2.zero;
        var imgRelleno = fila.relleno.gameObject.AddComponent<Image>();
        Redondear(imgRelleno, 6f);
        imgRelleno.color = colorBarra;

        fila.cuenta = Texto(fila.raiz, "Cuenta", "", 32f, colorTextoOscuro, new Vector2(100f, -24f), new Vector2(160f, 40f));
        fila.cuenta.alignment = TextAlignmentOptions.Left;

        var moneda = Rect(fila.raiz, "Moneda", new Vector2(215f, 0f), new Vector2(44f, 44f));
        var imgMoneda = moneda.gameObject.AddComponent<Image>();
        imgMoneda.sprite = circulo;
        imgMoneda.color = colorMoneda;
        fila.premio = Texto(fila.raiz, "Premio", "", 42f, colorTextoOscuro, new Vector2(305f, 0f), new Vector2(110f, 56f));
        fila.premio.alignment = TextAlignmentOptions.Left;

        var cobrar = ArmarBoton(fila.raiz, "Cobrar", new Vector2(440f, 0f), new Vector2(200f, 84f),
                                new Color32(0x7d, 0xe0, 0x4a, 255), new Color32(0x10, 0x24, 0x0e, 255), null,
                                Textos.De("mision_cobrar"), 40f);
        cobrar.onClick.AddListener(() => Cobrar(indice));
        cobrar.GetComponent<BotonJugoso>().respirar = true;
        fila.cobrar = cobrar.gameObject;

        var rtTilde = Rect(fila.raiz, "Tilde", new Vector2(440f, 0f), new Vector2(64f, 64f));
        fila.tilde = rtTilde.gameObject.AddComponent<Image>();
        fila.tilde.sprite = tilde;
        fila.tilde.color = new Color(0.1f, 0.3f, 0.05f, 1f);
        fila.tilde.raycastTarget = false;
        fila.tilde.enabled = false;
        return fila;
    }

    private Button ArmarBoton(RectTransform padre, string nombre, Vector2 posicion, Vector2 tamanio, Color color,
                              Color colorTexto, Sprite dibujo, string texto, float tamanioTexto)
    {
        var raiz = Rect(padre, nombre, posicion, tamanio);
        var toque = raiz.gameObject.AddComponent<Image>();
        toque.color = new Color(1f, 1f, 1f, 0f);
        var button = raiz.gameObject.AddComponent<Button>();
        button.targetGraphic = toque;

        // Visual antes que la sombra: BotonJugoso toma en su Awake el primer hijo como visual.
        var visual = Rect(raiz, "Visual", Vector2.zero, tamanio);
        var fondo = Rect(visual, "Fondo", Vector2.zero, tamanio);
        var imgFondo = fondo.gameObject.AddComponent<Image>();
        imgFondo.sprite = pildora;
        imgFondo.type = Image.Type.Sliced;
        imgFondo.color = color;
        imgFondo.raycastTarget = false;

        var tmp = Texto(visual, "Texto", texto, tamanioTexto, colorTexto, Vector2.zero, tamanio);
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

    private void Redondear(Image img, float multiplicador)
    {
        img.sprite = pildora;
        img.type = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = multiplicador;
    }

    private TMP_Text Texto(RectTransform padre, string nombre, string texto, float tamanio, Color color, Vector2 posicion, Vector2 caja)
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

    private static RectTransform Rect(RectTransform padre, string nombre, Vector2 posicion, Vector2 tamanio)
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
}
