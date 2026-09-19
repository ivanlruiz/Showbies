using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// El boton MISIONES del menu y la ventana que abre, con las tres misiones del dia
// (MisionesDiarias): cada una con su dificultad (verde, amarilla, roja), lo que pide,
// una barra de avance, el premio y COBRAR cuando esta cumplida. Al cobrar, el arpegio
// de la diaria y la fila se pone verde con su tilde. Abajo, VOLVER y el cofre del dia:
// gris con la cuenta ("COFRE 1/3") hasta cobrar las tres, dorado y latiendo cuando se
// puede abrir, y al tocarlo tiembla, estalla con el arpegio doble y queda verde.
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
    public Sprite iconoCofre;
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
    public Color colorCofre = new Color(0.97f, 0.79f, 0.28f, 1f);
    public Color colorTextoCofre = new Color(0.23f, 0.15f, 0f, 1f);
    public Color colorVidrio = new Color(0.06f, 0.12f, 0.05f, 0.45f);
    public Color colorTextoAbierto = new Color(0.06f, 0.14f, 0.05f, 1f);
    public float esperaDelCofre = 0.6f;
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
    private int idiomaArmado = -1;
    private float relojInsignia;
    private Button cofre;
    private Image fondoCofre;
    private Image iconoDelCofre;
    private TMP_Text textoCofre;
    private BotonJugoso jugoCofre;
    private float abriendo = -1f;
    private double montoAbierto;

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
        // Una copia del globo, en espejo arriba a la derecha (pedido de Ivan), con el
        // portapapeles en vez del globo.
        var boton = ConstructorUI.BotonDeEsquina(selectorIdioma, "BotonMisiones", DesdeLaDerecha, iconoBoton, Abrir);
        if (boton != null) insignia = ConstructorUI.Insignia(botonMejoras, (RectTransform)boton.transform, out numeroInsignia);
    }

    // A la misma distancia del borde derecho que el globo del izquierdo.
    private float DesdeLaDerecha
    {
        get { return selectorIdioma != null && selectorIdioma.botonGlobo != null ? ((RectTransform)selectorIdioma.botonGlobo.transform).anchoredPosition.x : 36f; }
    }

    private void RefrescarInsignia(float dt)
    {
        relojInsignia += dt;
        ConstructorUI.Latir(insignia, numeroInsignia, MisionesDiarias.PorCobrar, relojInsignia);
    }

    // --- La ventana ----------------------------------------------------------------

    public void Abrir()
    {
        if (panel == null) return;
        // Los textos fijos se escriben al armarla: si cambio el idioma desde el globo, se
        // vuelve a armar.
        if (Idioma.Revision != idiomaArmado)
        {
            Destroy(panel);
            Armar();
        }
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
        Festejar();
        filas[indice].golpe = 0f;
        Refrescar();
    }

    private void Festejar()
    {
        for (int k = 0; k < SemitonosFestejo.Length; k++)
            Sonidos.Programar(nota, 0.05 * k, 0.8f, Sonidos.PitchDe(SemitonosFestejo[k]));
        Sonidos.Tocar(sonidoFestejo, 0.7f);
    }

    private void TocarCofre()
    {
        if (abriendo >= 0f) return;
        if (!MisionesDiarias.CofreDisponible)
        {
            // Todavia no: tiembla y no hace nada, como una tarjeta sin monedas.
            if (jugoCofre != null && !MisionesDiarias.CofreCobrado) jugoCofre.Sacudir();
            return;
        }
        abriendo = 0f;
        if (jugoCofre != null) jugoCofre.Sacudir(22f, esperaDelCofre);
    }

    private void Update()
    {
        float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
        RefrescarInsignia(dt);
        if (!Abierta) return;

        reloj += dt;
        ventana.localScale = Vector3.one * CurvasUI.SalidaAtras(Mathf.Clamp01(reloj / 0.45f));

        // El cofre tiembla un rato antes de abrirse.
        if (abriendo >= 0f)
        {
            abriendo += dt;
            if (abriendo >= esperaDelCofre)
            {
                abriendo = -1f;
                montoAbierto = MisionesDiarias.CobrarCofre();
                if (montoAbierto > 0)
                {
                    Festejar();
                    for (int k = 0; k < SemitonosFestejo.Length; k++)
                        Sonidos.Programar(nota, 0.4 + 0.05 * k, 0.8f, Sonidos.PitchDe(SemitonosFestejo[k] + 12));
                    if (jugoCofre != null) jugoCofre.Golpe(1.35f, 0.4f);
                    CamaraJugador.Temblar(0.2f);
                }
                Refrescar();
            }
        }
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
        PintarCofre(mejor);
    }

    private void PintarCofre(int mejor)
    {
        if (cofre == null) return;
        Color fondo, texto;
        if (MisionesDiarias.CofreCobrado)
        {
            fondo = colorCumplida;
            texto = colorTextoAbierto;
            textoCofre.text = Textos.Formato("misiones_cofre_abierto", FormatoNumeros.Compacto(MisionesDiarias.MontoCofre(mejor)));
        }
        else if (MisionesDiarias.CofreDisponible)
        {
            fondo = colorCofre;
            texto = colorTextoCofre;
            textoCofre.text = Textos.Formato("misiones_cofre", FormatoNumeros.Compacto(MisionesDiarias.MontoCofre(mejor)));
        }
        else
        {
            fondo = colorVidrio;
            texto = Color.white;
            textoCofre.text = Textos.Formato("misiones_cofre_falta", MisionesDiarias.Cobradas);
        }
        fondoCofre.color = fondo;
        textoCofre.color = texto;
        if (iconoDelCofre != null) iconoDelCofre.color = texto;
        if (jugoCofre != null) jugoCofre.respirar = MisionesDiarias.CofreDisponible;
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
        idiomaArmado = Idioma.Revision;

        ventana = Rect(rtPanel, "Ventana", new Vector2(0f, 10f), new Vector2(1180f, 660f));
        var fondo = ventana.gameObject.AddComponent<Image>();
        Redondear(fondo, 3f);
        fondo.color = colorVentana;

        var titulo = Texto(ventana, "Titulo", Textos.De("misiones_titulo"), 80f, colorTitulo, new Vector2(0f, 250f), new Vector2(1100f, 100f));
        if (materialContorno != null) titulo.fontSharedMaterial = materialContorno;
        textoNuevas = Texto(ventana, "Nuevas", TextoNuevas(), 36f, colorTextoOscuro, new Vector2(0f, 182f), new Vector2(1100f, 50f));

        for (int i = 0; i < filas.Length; i++) filas[i] = ArmarFila(i, 85f - 125f * i);

        var volver = ArmarBoton(ventana, "Volver", new Vector2(-300f, -262f), new Vector2(340f, 100f),
                                colorVidrio, Color.white, iconoAtras, Textos.De("comun_volver"), 46f);
        volver.onClick.AddListener(Cerrar);

        cofre = ArmarBoton(ventana, "Cofre", new Vector2(200f, -262f), new Vector2(480f, 100f),
                           colorVidrio, Color.white, iconoCofre, Textos.Formato("misiones_cofre_falta", 0), 46f);
        cofre.onClick.AddListener(TocarCofre);
        fondoCofre = cofre.transform.Find("Visual/Fondo").GetComponent<Image>();
        textoCofre = cofre.transform.Find("Visual/Texto").GetComponent<TMP_Text>();
        var icono = cofre.transform.Find("Visual/Icono");
        if (icono != null) iconoDelCofre = icono.GetComponent<Image>();
        jugoCofre = cofre.GetComponent<BotonJugoso>();
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
        return ConstructorUI.Boton(padre, nombre, posicion, tamanio, color, colorTexto, dibujo, texto, tamanioTexto,
                                   fuente, pildora, sonidoClick);
    }

    private void Redondear(Image img, float multiplicador)
    {
        ConstructorUI.Redondear(img, pildora, multiplicador);
    }

    private TMP_Text Texto(RectTransform padre, string nombre, string texto, float tamanio, Color color, Vector2 posicion, Vector2 caja)
    {
        return ConstructorUI.Texto(padre, nombre, texto, tamanio, color, posicion, caja, fuente);
    }

    private static RectTransform Rect(RectTransform padre, string nombre, Vector2 posicion, Vector2 tamanio)
    {
        return ConstructorUI.Rect(padre, nombre, posicion, tamanio);
    }
}
