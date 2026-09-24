using TMPro;
using UnityEngine;
using UnityEngine.UI;

// El boton del bestiario (un trofeo, arriba a la derecha al lado de las misiones) y la
// ventana que abre (Bestiario): una tarjeta por tipo de zombi con su color, su nombre,
// cuantos lleva matados, sus tres estrellas, la barra hasta la siguiente y un boton
// para cobrar la estrella ganada. Las estrellas ganadas sin cobrar laten; al cobrar,
// el arpegio de la diaria y la tarjeta salta.
//
// Nada de esto esta en la escena: se arma en codigo con ConstructorUI, como las
// misiones. Vive en la raiz del canvas "Main Menu"; el atras de Android la cierra
// (BotonAtrasMenu).
public class VentanaBestiario : MonoBehaviour
{
    public SelectorIdioma selectorIdioma;
    public Button botonMejoras;             // de donde sale la insignia
    public float separacionDeMisiones = 24f;
    public Sprite iconoBoton;               // IconoTrofeo
    public Sprite iconoEstrella;
    public TMP_FontAsset fuente;
    public Material materialContorno;
    public Sprite pildora;
    public Sprite iconoAtras;
    public AudioClip nota;                  // moneda.wav
    public AudioClip sonidoFestejo;         // cartel.wav
    public AudioClip sonidoClick;

    public Color colorVentana = new Color(1f, 0.96f, 0.86f, 1f);
    public Color colorTextoOscuro = new Color(0.16f, 0.14f, 0.2f, 1f);
    public Color colorTitulo = new Color(0.97f, 0.79f, 0.28f, 1f);
    public Color colorTarjeta = new Color(0f, 0f, 0f, 0.06f);
    public Color colorEstrella = new Color(1f, 0.78f, 0.15f, 1f);
    public Color colorEstrellaApagada = new Color(0f, 0f, 0f, 0.15f);
    public Color colorBarra = new Color(0.3f, 0.75f, 0.2f, 1f);
    public Color colorVidrio = new Color(0.06f, 0.12f, 0.05f, 0.45f);
    public Color[] coloresTipo =
    {
        new Color(0.55f, 0.72f, 0.45f, 1f),   // normal
        new Color(0.72f, 0.95f, 0.2f, 1f),    // rapido, lima
        new Color(0.4f, 0.8f, 1f, 1f),        // FASTER, celeste
        new Color(0.95f, 0.35f, 0.3f, 1f),    // tanque, rojo
        new Color(0.65f, 0.4f, 0.95f, 1f),    // jefe, violeta
    };

    // Los colores de arriba son los del tema claro, que es como se ve el juego desde
    // siempre; con el oscuro cada uno pasa a su papel (ver Tema).
    private Color ColorDeVentana { get { return Tema.Elegir(colorVentana, RolDeTema.Panel); } }
    private Color ColorDeTexto { get { return Tema.Elegir(colorTextoOscuro, RolDeTema.Texto); } }
    private Color ColorDeTarjeta { get { return Tema.Elegir(colorTarjeta, RolDeTema.Hueco); } }
    private Color ColorDeEstrellaApagada { get { return Tema.Elegir(colorEstrellaApagada, RolDeTema.Surco); } }
    private Color ColorDeVidrio { get { return Tema.Elegir(colorVidrio, RolDeTema.Vidrio); } }

    private static readonly int[] SemitonosFestejo = { -12, -8, -5, 0, 4, 7, 12 };

    public static bool Abierta { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reiniciar()
    {
        Abierta = false;
    }

    private class Tarjeta
    {
        public string tipo;
        public RectTransform raiz;
        public TMP_Text cuenta;
        public Image[] estrellas;
        public TMP_Text siguiente;
        public RectTransform relleno;
        public GameObject cobrar;
        public TMP_Text textoCobrar;
        public float golpe = -1f;
    }

    private GameObject panel;
    private RectTransform ventana;
    private readonly Tarjeta[] tarjetas = new Tarjeta[Bestiario.Tipos.Length];
    private Texture2D texturaCirculo;
    private Sprite circulo;
    private RectTransform insignia;
    private TMP_Text numeroInsignia;
    private int revisionVista = -1;
    private float reloj = -1f;
    private int idiomaArmado = -1;
    private int temaArmado = -1;
    private float relojInsignia;

    private void Start()
    {
        texturaCirculo = TexturasUI.Circulo(64);
        circulo = Sprite.Create(texturaCirculo, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));

        // Al lado del de las misiones: el borde, el ancho de un boton y la separacion.
        float desdeLaDerecha = 36f, ancho = 96f;
        if (selectorIdioma != null && selectorIdioma.botonGlobo != null)
        {
            var globo = (RectTransform)selectorIdioma.botonGlobo.transform;
            desdeLaDerecha = globo.anchoredPosition.x;
            ancho = globo.rect.width;
        }
        var boton = ConstructorUI.BotonDeEsquina(selectorIdioma, "BotonBestiario", desdeLaDerecha + ancho + separacionDeMisiones,
                                                 iconoBoton, Abrir);
        if (boton != null) insignia = ConstructorUI.Insignia(botonMejoras, (RectTransform)boton.transform, out numeroInsignia);

        Armar();
        panel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (panel != null) Abierta = false;
        if (circulo != null) Destroy(circulo);
        if (texturaCirculo != null) Destroy(texturaCirculo);
    }

    public void Abrir()
    {
        if (panel == null) return;
        // Los textos y los colores se escriben al armarla: si cambio el idioma o el tema
        // desde el menu, se vuelve a armar.
        if (Idioma.Revision != idiomaArmado || Tema.Revision != temaArmado)
        {
            Destroy(panel);
            Armar();
        }
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
        if (Bestiario.Cobrar(tarjetas[indice].tipo) <= 0) return;
        for (int k = 0; k < SemitonosFestejo.Length; k++)
            Sonidos.Programar(nota, 0.05 * k, 0.8f, Sonidos.PitchDe(SemitonosFestejo[k]));
        Sonidos.Tocar(sonidoFestejo, 0.7f);
        tarjetas[indice].golpe = 0f;
        Refrescar();
    }

    private void Update()
    {
        float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
        relojInsignia += dt;
        ConstructorUI.Latir(insignia, numeroInsignia, Bestiario.PorCobrar, relojInsignia);
        if (!Abierta) return;

        reloj += dt;
        ventana.localScale = Vector3.one * CurvasUI.SalidaAtras(Mathf.Clamp01(reloj / 0.45f));
        if (Progreso.Revision != revisionVista) Refrescar();

        float latido = 1f + 0.18f * Mathf.Abs(Mathf.Sin(reloj * 4f));
        foreach (var tarjeta in tarjetas)
        {
            if (tarjeta.golpe >= 0f) tarjeta.golpe += dt;
            float salto = tarjeta.golpe >= 0f ? 0.08f * CurvasUI.Campana(Mathf.Clamp01(tarjeta.golpe / 0.4f)) : 0f;
            tarjeta.raiz.localScale = Vector3.one * (1f + salto);

            // Las estrellas ganadas sin cobrar laten.
            int cobradas = Bestiario.Cobradas(tarjeta.tipo), alcanzadas = Bestiario.Alcanzadas(tarjeta.tipo);
            for (int e = 0; e < tarjeta.estrellas.Length; e++)
                tarjeta.estrellas[e].rectTransform.localScale = Vector3.one * (e >= cobradas && e < alcanzadas ? latido : 1f);
        }
    }

    private void Refrescar()
    {
        revisionVista = Progreso.Revision;
        int mejor = Progreso.MejorOleada;
        foreach (var tarjeta in tarjetas)
        {
            string tipo = tarjeta.tipo;
            int muertes = Progreso.Matados(tipo);
            int cobradas = Bestiario.Cobradas(tipo), alcanzadas = Bestiario.Alcanzadas(tipo);
            tarjeta.cuenta.text = FormatoNumeros.Compacto(muertes);
            for (int e = 0; e < tarjeta.estrellas.Length; e++)
            {
                var color = ColorDeEstrellaApagada;
                if (e < cobradas) color = colorEstrella;
                else if (e < alcanzadas) color = Color.Lerp(colorEstrella, Color.white, 0.35f);
                tarjeta.estrellas[e].color = color;
            }

            int siguiente = Bestiario.Siguiente(tipo);
            if (siguiente > 0)
            {
                // La barra va del escalon anterior al siguiente.
                int anterior = 0;
                foreach (int escalon in Bestiario.Escalones(tipo)) if (escalon < siguiente) anterior = escalon;
                float fraccion = Mathf.Clamp01((muertes - anterior) / (float)Mathf.Max(1, siguiente - anterior));
                tarjeta.siguiente.text = Textos.Formato("bestiario_siguiente", FormatoNumeros.Compacto(siguiente));
                tarjeta.relleno.anchorMax = new Vector2(fraccion, 1f);
            }
            else
            {
                tarjeta.siguiente.text = Textos.De("bestiario_completo");
                tarjeta.relleno.anchorMax = Vector2.one;
            }

            bool hayParaCobrar = alcanzadas > cobradas;
            tarjeta.cobrar.SetActive(hayParaCobrar);
            if (hayParaCobrar) tarjeta.textoCobrar.text = "+" + FormatoNumeros.Compacto(Bestiario.Premio(tipo, cobradas, mejor));
        }
    }

    private void Armar()
    {
        var rtPanel = ConstructorUI.Estirar((RectTransform)transform, "VentanaBestiario");
        panel = rtPanel.gameObject;
        panel.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.45f);   // tapa los toques del menu
        idiomaArmado = Idioma.Revision;
        temaArmado = Tema.Revision;

        ventana = ConstructorUI.Rect(rtPanel, "Ventana", new Vector2(0f, 10f), new Vector2(1180f, 660f));
        var fondo = ventana.gameObject.AddComponent<Image>();
        ConstructorUI.VentanaNeon(ventana, fondo, pildora);
        fondo.color = ColorDeVentana;

        var titulo = ConstructorUI.Texto(ventana, "Titulo", Textos.De("bestiario_titulo"), 80f, colorTitulo,
                                         new Vector2(0f, 250f), new Vector2(1100f, 100f), fuente);
        if (materialContorno != null) titulo.fontSharedMaterial = materialContorno;
        ConstructorUI.Texto(ventana, "Bajada", Textos.De("bestiario_bajada"), 36f, ColorDeTexto,
                            new Vector2(0f, 186f), new Vector2(1100f, 50f), fuente);

        for (int i = 0; i < tarjetas.Length; i++) tarjetas[i] = ArmarTarjeta(i, (i - (tarjetas.Length - 1) * 0.5f) * 216f);

        var volver = ConstructorUI.Boton(ventana, "Volver", new Vector2(0f, -266f), new Vector2(340f, 100f), ColorDeVidrio,
                                         Color.white, iconoAtras, Textos.De("comun_volver"), 46f, fuente, pildora, sonidoClick);
        volver.onClick.AddListener(Cerrar);
    }

    private Tarjeta ArmarTarjeta(int indice, float x)
    {
        var tarjeta = new Tarjeta { tipo = Bestiario.Tipos[indice] };
        tarjeta.raiz = ConstructorUI.Rect(ventana, tarjeta.tipo, new Vector2(x, -20f), new Vector2(200f, 390f));
        var fondo = tarjeta.raiz.gameObject.AddComponent<Image>();
        ConstructorUI.Redondear(fondo, pildora, 3f);
        fondo.color = ColorDeTarjeta;

        string nombre = Bestiario.Nombre(tarjeta.tipo);
        ConstructorUI.Imagen(tarjeta.raiz, "Color", new Vector2(0f, 130f), new Vector2(84f, 84f), circulo,
                             coloresTipo[Mathf.Min(indice, coloresTipo.Length - 1)]);
        // La inicial va encima del circulo del color del zombi, que no cambia con el
        // tema: oscura siempre, o en el oscuro quedaba blanca sobre lima.
        ConstructorUI.Texto(tarjeta.raiz, "Inicial", nombre.Substring(0, 1), 52f, colorTextoOscuro,
                            new Vector2(0f, 130f), new Vector2(84f, 84f), fuente);
        ConstructorUI.Texto(tarjeta.raiz, "Nombre", nombre, 34f, ColorDeTexto, new Vector2(0f, 64f), new Vector2(190f, 44f), fuente);
        tarjeta.cuenta = ConstructorUI.Texto(tarjeta.raiz, "Cuenta", "", 48f, ColorDeTexto, new Vector2(0f, 20f), new Vector2(190f, 56f), fuente);

        tarjeta.estrellas = new Image[Bestiario.Estrellas];
        for (int e = 0; e < tarjeta.estrellas.Length; e++)
            tarjeta.estrellas[e] = ConstructorUI.Imagen(tarjeta.raiz, "Estrella" + e, new Vector2((e - 1) * 54f, -34f),
                                                        new Vector2(46f, 46f), iconoEstrella, ColorDeEstrellaApagada);

        tarjeta.siguiente = ConstructorUI.Texto(tarjeta.raiz, "Siguiente", "", 28f, ColorDeTexto,
                                                new Vector2(0f, -78f), new Vector2(190f, 36f), fuente);
        tarjeta.relleno = ConstructorUI.Barra(tarjeta.raiz, "Barra", new Vector2(0f, -104f), new Vector2(160f, 10f), pildora,
                                              Tema.Elegir(new Color(0f, 0f, 0f, 0.12f), RolDeTema.Surco), colorBarra);

        var cobrar = ConstructorUI.Boton(tarjeta.raiz, "Cobrar", new Vector2(0f, -152f), new Vector2(170f, 70f),
                                         ConstructorUI.Verde, ConstructorUI.VerdeTexto, null,
                                         "", 38f, fuente, pildora, sonidoClick);
        cobrar.onClick.AddListener(() => Cobrar(indice));
        cobrar.GetComponent<BotonJugoso>().respirar = true;
        tarjeta.cobrar = cobrar.gameObject;
        tarjeta.textoCobrar = cobrar.transform.Find("Visual/Texto").GetComponent<TMP_Text>();
        return tarjeta;
    }
}
