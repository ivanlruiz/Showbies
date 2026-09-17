using TMPro;
using UnityEngine;
using UnityEngine.UI;

// La ventana de la recompensa diaria (RecompensaDiaria): aparece sola al abrir el menu
// si hoy hay algo para cobrar. Siete casilleros con los dias de la racha (los cobrados
// en verde con su tilde, el de hoy dorado y latiendo), y un boton COBRAR +N. Al cobrar
// suena el arpegio de la tienda, el casillero de hoy se pone verde y la ventana se va.
//
// Se arma entera en codigo, como OpcionesSonido: en la escena solo esta este componente
// (en la raiz del canvas "Main Menu") con los sprites, la fuente y los sonidos. El
// atras de Android la cierra sin cobrar (BotonAtrasMenu): vuelve a salir la proxima vez
// que se abra el menu en el dia.
public class VentanaRecompensaDiaria : MonoBehaviour
{
    public TMP_FontAsset fuente;
    public Material materialContorno;       // titulo sobre el crema
    public Sprite pildora;
    public Sprite tilde;
    public AudioClip nota;                  // moneda.wav
    public AudioClip sonidoFestejo;         // cartel.wav
    public AudioClip sonidoClick;           // el de los demas botones

    public Color colorVentana = new Color(1f, 0.96f, 0.86f, 1f);
    public Color colorTextoOscuro = new Color(0.16f, 0.14f, 0.2f, 1f);
    public Color colorCobrado = new Color(0.49f, 0.88f, 0.29f, 1f);
    public Color colorHoy = new Color(0.97f, 0.79f, 0.28f, 1f);
    public Color colorFuturo = new Color(0f, 0f, 0f, 0.08f);
    public Color colorMoneda = new Color(1f, 0.76f, 0.12f, 1f);
    public Color colorBordeMoneda = new Color(0.72f, 0.42f, 0.02f, 1f);
    public Color colorTilde = new Color(0.1f, 0.3f, 0.05f, 1f);

    private static readonly int[] SemitonosFestejo = { -12, -8, -5, 0, 4, 7, 12 };
    private const float DuracionEntrada = 0.45f;
    private const float DuracionSalida = 0.25f;
    private const float EsperaTrasCobrar = 1.3f;

    public static bool Abierta { get; private set; }

    private GameObject panel;
    private RectTransform ventana;
    private RectTransform casilleroHoy;
    private Image fondoHoy;
    private Image tildeHoy;
    private TMP_Text titulo;
    private GameObject boton;
    private Texture2D texturaMoneda;
    private Texture2D texturaBorde;
    private Sprite bordeMoneda;

    private int racha;
    private float reloj = -1f;              // tiempo desde que se abrio
    private float relojSalida = -1f;        // tiempo desde que empezo a irse
    private float esperaParaIrse = -1f;     // tras cobrar, cuanto falta para empezar a irse

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reiniciar()
    {
        Abierta = false;
    }

    private void Start()
    {
        racha = RecompensaDiaria.RachaDeHoy;
        if (racha <= 0) return;
        Armar();
        Abrir();
    }

    private void OnDestroy()
    {
        if (panel != null) Abierta = false;
        if (texturaMoneda != null) Destroy(texturaMoneda);
        if (texturaBorde != null) Destroy(texturaBorde);
    }

    private void Abrir()
    {
        panel.SetActive(true);
        panel.transform.SetAsLastSibling();
        Abierta = true;
        reloj = 0f;
        ventana.localScale = Vector3.zero;
    }

    public void Cerrar()
    {
        if (!Abierta || relojSalida >= 0f) return;
        relojSalida = 0f;
    }

    private void Cobrar()
    {
        if (esperaParaIrse >= 0f || relojSalida >= 0f) return;
        double monto = RecompensaDiaria.Cobrar();
        if (monto <= 0) { Cerrar(); return; }

        for (int k = 0; k < SemitonosFestejo.Length; k++)
            Sonidos.Programar(nota, 0.05 * k, 0.8f, Sonidos.PitchDe(SemitonosFestejo[k]));
        Sonidos.Tocar(sonidoFestejo, 0.7f);

        if (fondoHoy != null) fondoHoy.color = colorCobrado;
        if (tildeHoy != null) tildeHoy.enabled = true;
        titulo.text = Textos.Formato("diaria_cobrado", FormatoNumeros.Compacto(monto));
        boton.SetActive(false);
        esperaParaIrse = EsperaTrasCobrar;
    }

    private void Update()
    {
        if (!Abierta) return;
        float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);

        if (relojSalida >= 0f)
        {
            relojSalida += dt;
            float s = Mathf.Clamp01(relojSalida / DuracionSalida);
            ventana.localScale = Vector3.one * (1f - CurvasUI.SalidaCubica(s));
            if (s >= 1f)
            {
                panel.SetActive(false);
                Abierta = false;
                relojSalida = -1f;
            }
            return;
        }

        reloj += dt;
        float e = Mathf.Clamp01(reloj / DuracionEntrada);
        ventana.localScale = Vector3.one * CurvasUI.SalidaAtras(e);

        if (casilleroHoy != null)
        {
            // Late mientras espera el cobro; al cobrar da un salto y se queda quieto.
            float latido = esperaParaIrse >= 0f
                ? 0.25f * CurvasUI.Campana(Mathf.Clamp01((EsperaTrasCobrar - esperaParaIrse) / 0.4f))
                : 0.06f * Mathf.Sin(reloj * 6f);
            casilleroHoy.localScale = Vector3.one * (1.12f + latido);
        }

        if (esperaParaIrse >= 0f)
        {
            esperaParaIrse -= dt;
            if (esperaParaIrse <= 0f) { esperaParaIrse = -1f; Cerrar(); }
        }
    }

    private void Armar()
    {
        texturaMoneda = TexturasUI.Circulo(64);
        var moneda = Sprite.Create(texturaMoneda, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));
        texturaBorde = TexturasUI.Anillo(64, 0.2f);
        var borde = Sprite.Create(texturaBorde, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));

        panel = new GameObject("RecompensaDiaria", typeof(RectTransform));
        var rtPanel = Estirar((RectTransform)panel.transform, (RectTransform)transform);
        var velo = panel.AddComponent<Image>();
        velo.color = new Color(0f, 0f, 0f, 0.45f);          // tapa los toques del menu

        ventana = Rect(rtPanel, "Ventana", new Vector2(0f, 10f), new Vector2(1180f, 640f));
        var fondo = ventana.gameObject.AddComponent<Image>();
        Redondear(fondo, 3f);
        fondo.color = colorVentana;

        titulo = Texto(ventana, "Titulo", Textos.De("diaria_titulo"), 84f, colorHoy, new Vector2(0f, 235f), new Vector2(1100f, 110f));
        if (materialContorno != null) titulo.fontSharedMaterial = materialContorno;

        string textoBajada = racha > 1 ? Textos.Formato("diaria_racha", racha) : Textos.De("diaria_volve");
        Texto(ventana, "Bajada", textoBajada, 40f, colorTextoOscuro, new Vector2(0f, 158f), new Vector2(1100f, 60f));

        int hoy = RecompensaDiaria.Casillero(racha);
        int mejor = Progreso.MejorOleada;
        const float Ancho = 138f, Paso = 154f;
        for (int dia = 1; dia <= RecompensaDiaria.DiasDelCiclo; dia++)
        {
            float x = (dia - (RecompensaDiaria.DiasDelCiclo + 1) * 0.5f) * Paso;
            var casillero = Rect(ventana, "Dia" + dia, new Vector2(x, 10f), new Vector2(Ancho, 190f));
            var img = casillero.gameObject.AddComponent<Image>();
            Redondear(img, 3f);
            img.color = dia < hoy ? colorCobrado : dia == hoy ? colorHoy : colorFuturo;

            Texto(casillero, "Dia", Textos.Formato("diaria_dia", dia), 30f, colorTextoOscuro, new Vector2(0f, 62f), new Vector2(Ancho, 40f));
            var circulo = Rect(casillero, "Moneda", new Vector2(0f, 8f), new Vector2(54f, 54f));
            var imgMoneda = circulo.gameObject.AddComponent<Image>();
            imgMoneda.sprite = moneda;
            imgMoneda.color = colorMoneda;
            ConBorde(circulo, borde);
            // A partir del dia de hoy se muestra lo que se cobraria con la racha intacta.
            int rachaDelCasillero = racha - hoy + dia;
            string monto = FormatoNumeros.Compacto(RecompensaDiaria.Monto(rachaDelCasillero, mejor));
            Texto(casillero, "Monto", monto, 36f, colorTextoOscuro, new Vector2(0f, -52f), new Vector2(Ancho, 46f));

            var rtTilde = Rect(casillero, "Tilde", new Vector2(0f, 8f), new Vector2(44f, 44f));
            var imgTilde = rtTilde.gameObject.AddComponent<Image>();
            imgTilde.sprite = tilde;
            imgTilde.color = colorTilde;
            imgTilde.raycastTarget = false;
            imgTilde.enabled = dia < hoy && tilde != null;

            if (dia == hoy)
            {
                casilleroHoy = casillero;
                fondoHoy = img;
                tildeHoy = imgTilde;
                casillero.SetAsLastSibling();
            }
        }

        bordeMoneda = borde;
        boton = ArmarBoton(ventana, moneda, Textos.Formato("diaria_cobrar",
                           FormatoNumeros.Compacto(RecompensaDiaria.Monto(racha, mejor))));
    }

    private GameObject ArmarBoton(RectTransform padre, Sprite moneda, string texto)
    {
        var raiz = Rect(padre, "BotonCobrar", new Vector2(0f, -218f), new Vector2(560f, 130f));
        var toque = raiz.gameObject.AddComponent<Image>();
        toque.color = new Color(1f, 1f, 1f, 0f);
        var button = raiz.gameObject.AddComponent<Button>();
        button.targetGraphic = toque;
        button.onClick.AddListener(Cobrar);

        // Visual antes que la sombra: BotonJugoso toma en su Awake el primer hijo como visual.
        var visual = Rect(raiz, "Visual", Vector2.zero, new Vector2(560f, 130f));
        var fondo = Rect(visual, "Fondo", Vector2.zero, new Vector2(560f, 130f));
        var imgFondo = fondo.gameObject.AddComponent<Image>();
        imgFondo.sprite = pildora; imgFondo.type = Image.Type.Sliced;
        imgFondo.color = new Color32(0x7d, 0xe0, 0x4a, 255);
        imgFondo.raycastTarget = false;

        var icono = Rect(visual, "Icono", Vector2.zero, new Vector2(58f, 58f));
        icono.anchorMin = icono.anchorMax = new Vector2(0f, 0.5f);
        var imgIcono = icono.gameObject.AddComponent<Image>();
        imgIcono.sprite = moneda;
        imgIcono.color = colorMoneda;
        imgIcono.raycastTarget = false;
        ConBorde(icono, bordeMoneda);

        var tmp = Texto(visual, "Texto", texto, 62f, new Color32(0x10, 0x24, 0x0e, 255), Vector2.zero, new Vector2(560f, 130f));
        var junto = icono.gameObject.AddComponent<IconoDeBoton>();
        junto.texto = tmp;
        junto.separacion = 12f;

        var jugoso = raiz.gameObject.AddComponent<BotonJugoso>();
        jugoso.respirar = true;
        jugoso.sonidoClick = sonidoClick;

        var sombra = Rect(raiz, "Sombra", new Vector2(0f, -8f), new Vector2(560f, 130f));
        sombra.SetAsFirstSibling();
        var imgSombra = sombra.gameObject.AddComponent<Image>();
        imgSombra.sprite = pildora; imgSombra.type = Image.Type.Sliced;
        imgSombra.color = new Color(0f, 0f, 0f, 0.3f);
        imgSombra.raycastTarget = false;
        return raiz.gameObject;
    }

    private void ConBorde(RectTransform moneda, Sprite borde)
    {
        var rt = Rect(moneda, "Borde", Vector2.zero, Vector2.zero);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = borde;
        img.color = colorBordeMoneda;
        img.raycastTarget = false;
    }

    private void Redondear(Image img, float multiplicador)
    {
        img.sprite = pildora;
        img.type = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = multiplicador;   // bordes mas chicos que una pildora
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

    private static RectTransform Estirar(RectTransform rt, RectTransform padre)
    {
        rt.SetParent(padre, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return rt;
    }
}
