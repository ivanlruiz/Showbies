using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// La ventana de LOGROS y el nivel del jugador en el menu, pedido de Ivan. Arriba, la
// tarjeta del nivel: la moneda con el numero, la barra de experiencia y el premio (COBRAR
// cuando hay uno esperando, y si no, lo que va a dar el siguiente). Abajo, una lista que
// se desplaza con las doce familias de Logros: la moneda del color de la mas alta ganada,
// lo que pide la siguiente, su barra, las tres monedas (bronce, plata y oro) y COBRAR con
// la experiencia de la ganada. Al cobrar, la barra se llena de a poco y cada nivel que
// cruza hace saltar la moneda con una nota.
//
// Se entra por dos lados: el boton del nivel, arriba a la izquierda al lado del engranaje
// (la moneda con el numero y su barra, que tambien se llena sola al volver de jugar), y
// una medalla redonda arriba a la derecha, al lado del bestiario, con la insignia de lo
// que hay para cobrar.
//
// Nada de esto esta en la escena: se arma en codigo con ConstructorUI, como las misiones
// y el bestiario. Vive en la raiz del canvas "Main Menu"; el atras de Android la cierra
// (BotonAtrasMenu).
public class VentanaLogros : MonoBehaviour
{
    public SelectorIdioma selectorIdioma;
    public Button botonMejoras;             // de donde sale la insignia
    public float separacion = 24f;
    public Sprite iconoBoton;               // IconoMedalla
    public TMP_FontAsset fuente;
    public Material materialContorno;
    public Sprite pildora;
    public Sprite iconoAtras;
    public Sprite tilde;
    public AudioClip nota;                  // moneda.wav
    public AudioClip sonidoFestejo;         // cartel.wav
    public AudioClip sonidoClick;

    public Color colorVentana = new Color(1f, 0.96f, 0.86f, 1f);
    public Color colorTextoOscuro = new Color(0.16f, 0.14f, 0.2f, 1f);
    public Color colorTitulo = new Color(0.97f, 0.79f, 0.28f, 1f);
    public Color colorFila = new Color(0f, 0f, 0f, 0.06f);
    public Color colorApagado = new Color(0f, 0f, 0f, 0.15f);
    public Color colorBarra = new Color(0.3f, 0.75f, 0.2f, 1f);
    public Color colorExperiencia = new Color(0.25f, 0.6f, 1f, 1f);
    public Color colorVidrio = new Color(0.06f, 0.12f, 0.05f, 0.45f);
    [Tooltip("Cuanto tarda la barra en llenarse: mas alto, mas rapido.")]
    public float llenado = 3.5f;

    // Los colores de arriba son los del tema claro, que es como se ve el juego desde
    // siempre; con el oscuro cada uno pasa a su papel (ver Tema).
    private Color ColorDeVentana { get { return Tema.Elegir(colorVentana, RolDeTema.Panel); } }
    private Color ColorDeTexto { get { return Tema.Elegir(colorTextoOscuro, RolDeTema.Texto); } }
    private Color ColorDeFila { get { return Tema.Elegir(colorFila, RolDeTema.Hueco); } }
    private Color ColorApagado { get { return Tema.Elegir(colorApagado, RolDeTema.Surco); } }
    private Color ColorDeVidrio { get { return Tema.Elegir(colorVidrio, RolDeTema.Vidrio); } }

    private static readonly Color ColorVerde = new Color32(0x7d, 0xe0, 0x4a, 255);
    private static readonly Color ColorTextoVerde = new Color32(0x10, 0x24, 0x0e, 255);
    private static readonly int[] SemitonosFestejo = { -12, -8, -5, 0, 4, 7, 12 };

    // Las medidas de la lista: alto de cada fila y de la vista que se desplaza.
    private const float AltoFila = 112f, PasoFila = 124f, AltoVista = 410f;

    public static bool Abierta { get; private set; }

    // La experiencia que muestra la barra, que va hacia la de verdad de a poco. Estatica
    // para que sobreviva a la partida: al volver al menu, la barra se llena con lo jugado.
    private static double experienciaMostrada = -1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reiniciar()
    {
        Abierta = false;
        experienciaMostrada = -1;
    }

    private class Fila
    {
        public Logros.Familia familia;
        public RectTransform raiz;
        public Image cara, borde;
        public TMP_Text inicial;
        public TMP_Text descripcion;
        public RectTransform relleno;
        public TMP_Text cuenta;
        public Image[] caras, bordes;
        public GameObject cobrar;
        public TMP_Text textoCobrar;
        public Image tilde;
        public float golpe = -1f;
    }

    // La moneda con el numero del nivel y su barra: una en el boton y otra en la ventana.
    private class Medidor
    {
        public RectTransform moneda;
        public TMP_Text numero;
        public RectTransform relleno;
        public TMP_Text titulo;
        public TMP_Text experiencia;
        public float golpe = -1f;
        public long pintada = -1;       // la experiencia que muestra, para no repintar lo mismo
        public int idioma = -1;
    }

    private GameObject panel;
    private RectTransform ventana;
    private ScrollRect lista;
    private RectTransform contenido;
    private readonly Fila[] filas = new Fila[Logros.Familias.Length];
    private Medidor enLaVentana, enElBoton;
    private TMP_Text textoTotal;
    private RectTransform tarjetaNivel;
    private Button cobrarNivel;
    private TMP_Text textoCobrarNivel;
    private TMP_Text textoProximo;
    private float golpeNivel = -1f;
    private Texture2D texturaCirculo;
    private Sprite circulo;
    private RectTransform insignia;
    private TMP_Text numeroInsignia;
    private int revisionVista = -1;
    private float reloj = -1f;
    private int idiomaArmado = -1;
    private int temaArmado = -1;
    private float relojInsignia, proximaRevision;
    private int nivelMostrado;

    private void Start()
    {
        texturaCirculo = TexturasUI.Circulo(64);
        circulo = Sprite.Create(texturaCirculo, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));
        Logros.Revisar();
        if (experienciaMostrada < 0 || experienciaMostrada > NivelJugador.Experiencia) experienciaMostrada = NivelJugador.Experiencia;
        nivelMostrado = NivelJugador.NivelCon(experienciaMostrada);

        CrearBotones();
        Armar();
        panel.SetActive(false);
        PintarMedidor(enElBoton, false);
    }

    private void OnDestroy()
    {
        if (panel != null) Abierta = false;
        if (circulo != null) Destroy(circulo);
        if (texturaCirculo != null) Destroy(texturaCirculo);
    }

    // --- Los botones del menu -----------------------------------------------------

    private void CrearBotones()
    {
        if (selectorIdioma == null || selectorIdioma.botonGlobo == null) return;
        var globo = (RectTransform)selectorIdioma.botonGlobo.transform;
        float ancho = globo.rect.width, alto = globo.rect.height;

        // La medalla, arriba a la derecha: despues de las misiones y el bestiario.
        var medalla = ConstructorUI.BotonDeEsquina(selectorIdioma, "BotonLogros",
                                                   globo.anchoredPosition.x + 2f * (ancho + separacion), iconoBoton, Abrir);
        if (medalla != null) insignia = ConstructorUI.Insignia(botonMejoras, (RectTransform)medalla.transform, out numeroInsignia);

        // El nivel, arriba a la izquierda: despues del globo y el engranaje (que es una copia
        // del globo corrida su ancho y la separacion). Una pildora de vidrio como el fondo
        // del globo, con la moneda del nivel y la barra. Con las anclas del globo y el borde
        // izquierdo medido desde el suyo, sea cual sea su pivote.
        var boton = ConstructorUI.Boton((RectTransform)globo.parent, "BotonNivel", Vector2.zero, new Vector2(300f, alto),
                                        Color.white, Color.white, null, "", 10f, fuente, pildora, sonidoClick);
        var rt = (RectTransform)boton.transform;
        rt.anchorMin = globo.anchorMin;
        rt.anchorMax = globo.anchorMax;
        rt.pivot = new Vector2(0f, globo.pivot.y);
        float bordeDelGlobo = globo.anchoredPosition.x - globo.pivot.x * ancho;
        rt.anchoredPosition = new Vector2(bordeDelGlobo + 2f * (ancho + separacion), globo.anchoredPosition.y);
        boton.onClick.AddListener(Abrir);

        var fondo = boton.transform.Find("Visual/Fondo").GetComponent<Image>();
        var delGlobo = selectorIdioma.fondoGlobo != null ? selectorIdioma.fondoGlobo.GetComponent<PintarConTema>() : null;
        Color claro = delGlobo != null ? delGlobo.colorClaro : (selectorIdioma.fondoGlobo != null ? selectorIdioma.fondoGlobo.color : colorVidrio);
        Tema.Pintar(fondo, RolDeTema.Vidrio, claro);

        var visual = (RectTransform)boton.transform.Find("Visual");
        enElBoton = new Medidor();
        enElBoton.moneda = MonedaDeNivel(visual, new Vector2(-150f + alto * 0.5f, 0f), alto - 16f, 40f, out enElBoton.numero);
        enElBoton.titulo = ConstructorUI.Texto(visual, "Titulo", "", 30f, Color.white, new Vector2(50f, 16f), new Vector2(180f, 40f), fuente);
        enElBoton.titulo.alignment = TextAlignmentOptions.Left;
        enElBoton.relleno = ConstructorUI.Barra(visual, "Barra", new Vector2(50f, -18f), new Vector2(180f, 14f), pildora,
                                                new Color(1f, 1f, 1f, 0.2f), colorExperiencia);
    }

    // Una moneda dorada con un numero: el borde, la cara y el texto. Devuelve la raiz.
    private RectTransform MonedaDeNivel(RectTransform padre, Vector2 posicion, float lado, float letra, out TMP_Text numero)
    {
        var raiz = ConstructorUI.Rect(padre, "Moneda", posicion, new Vector2(lado, lado));
        ConstructorUI.Imagen(raiz, "Borde", Vector2.zero, new Vector2(lado, lado), circulo, ColoresDeLogro.Borde(ColoresDeLogro.Oro));
        ConstructorUI.Imagen(raiz, "Cara", Vector2.zero, new Vector2(lado * 0.84f, lado * 0.84f), circulo, ColoresDeLogro.Oro);
        numero = ConstructorUI.Texto(raiz, "Numero", "1", letra, ColoresDeLogro.TextoSobreMetal, Vector2.zero, new Vector2(lado, lado), fuente);
        numero.enableAutoSizing = true;
        numero.fontSizeMin = letra * 0.5f;
        numero.fontSizeMax = letra;
        return raiz;
    }

    // --- La ventana -----------------------------------------------------------------

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
        Logros.Revisar();
        Refrescar();
        panel.SetActive(true);
        panel.transform.SetAsLastSibling();
        Abierta = true;
        reloj = 0f;
        ventana.localScale = Vector3.zero;
        IrALaPrimeraParaCobrar();

        // Como en la tienda: con el umbral de arrastre por defecto, un dedo que se mueve
        // apenas al tocar COBRAR en un telefono de muchos dpi se toma como arrastre.
        if (EventSystem.current != null)
        {
            float dpi = Screen.dpi > 0 ? Screen.dpi : 160f;
            EventSystem.current.pixelDragThreshold = Mathf.Max(EventSystem.current.pixelDragThreshold, Mathf.RoundToInt(dpi * 0.06f));
        }
    }

    public void Cerrar()
    {
        if (panel != null) panel.SetActive(false);
        Abierta = false;
        reloj = -1f;
    }

    // La lista arranca en la primera familia con algo para cobrar, si hay. Se corre el
    // contenido directo, en unidades de la lista: verticalNormalizedPosition pasa por la
    // matriz de mundo, y con la ventana en escala 0 (entrando) la cuenta daba cualquier
    // cosa y la lista quedaba corrida una fila.
    private void IrALaPrimeraParaCobrar()
    {
        if (lista == null) return;
        float sobra = Mathf.Max(0f, contenido.sizeDelta.y - AltoVista);
        float y = 0f;
        for (int i = 0; i < filas.Length; i++)
        {
            if (Logros.ParaCobrar(filas[i].familia.id) == null) continue;
            y = Mathf.Min(i * PasoFila, sobra);
            break;
        }
        lista.StopMovement();
        contenido.anchoredPosition = new Vector2(0f, y);
    }

    private void CobrarLogro(int indice)
    {
        if (Logros.Cobrar(filas[indice].familia.id) <= 0) return;
        Festejar(0);
        filas[indice].golpe = 0f;
        Refrescar();
    }

    private void CobrarNivel()
    {
        if (NivelJugador.Cobrar() <= 0) return;
        Festejar(5);
        CamaraJugador.Temblar(0.2f);
        golpeNivel = 0f;
        Refrescar();
    }

    private void Festejar(int semitonos)
    {
        for (int k = 0; k < SemitonosFestejo.Length; k++)
            Sonidos.Programar(nota, 0.05 * k, 0.8f, Sonidos.PitchDe(SemitonosFestejo[k] + semitonos));
        Sonidos.Tocar(sonidoFestejo, 0.7f);
    }

    private void Update()
    {
        float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);

        // Lo que se gana en el menu (el critico se compra en la tienda) aparece en la
        // insignia sin tener que abrir la ventana.
        relojInsignia += dt;
        if (relojInsignia >= proximaRevision)
        {
            proximaRevision = relojInsignia + 0.5f;
            Logros.Revisar();
        }
        ConstructorUI.Latir(insignia, numeroInsignia, Logros.PorCobrar + NivelJugador.PorCobrar, relojInsignia);

        AvanzarExperiencia(dt);
        Saltar(enElBoton, dt);
        if (!Abierta) return;

        reloj += dt;
        ventana.localScale = Vector3.one * CurvasUI.SalidaAtras(Mathf.Clamp01(reloj / 0.45f));
        if (Progreso.Revision != revisionVista) Refrescar();
        Saltar(enLaVentana, dt);

        if (golpeNivel >= 0f) golpeNivel += dt;
        float saltoNivel = golpeNivel >= 0f ? 0.06f * CurvasUI.Campana(Mathf.Clamp01(golpeNivel / 0.4f)) : 0f;
        tarjetaNivel.localScale = Vector3.one * (1f + saltoNivel);

        float latido = 1f + 0.18f * Mathf.Abs(Mathf.Sin(reloj * 4f));
        foreach (var fila in filas)
        {
            if (fila.golpe >= 0f) fila.golpe += dt;
            float salto = fila.golpe >= 0f ? 0.06f * CurvasUI.Campana(Mathf.Clamp01(fila.golpe / 0.4f)) : 0f;
            fila.raiz.localScale = Vector3.one * (1f + salto);

            // Las monedas ganadas sin cobrar laten.
            int ganadas = Logros.Ganadas(fila.familia.id);
            for (int e = 0; e < Logros.Escalones; e++)
            {
                var ganada = e < ganadas ? Logros.Ganado(fila.familia.id, e) : null;
                float escala = ganada != null && !ganada.cobrado ? latido : 1f;
                fila.bordes[e].rectTransform.localScale = Vector3.one * escala;
            }
        }
    }

    // La barra va hacia la experiencia de verdad de a poco; cada nivel que cruza hace
    // saltar las monedas con una nota, un grado mas arriba cada vez.
    private void AvanzarExperiencia(float dt)
    {
        double real = NivelJugador.Experiencia;
        if (experienciaMostrada > real) experienciaMostrada = real;
        if (experienciaMostrada < real)
        {
            double falta = real - experienciaMostrada;
            double paso = falta * (1.0 - System.Math.Exp(-llenado * dt));
            // Un minimo para que el final no se arrastre: un vigesimo del nivel por cuadro.
            paso = System.Math.Max(paso, NivelJugador.CostoDelNivel(NivelJugador.NivelCon(experienciaMostrada)) * 0.02);
            experienciaMostrada = System.Math.Min(real, experienciaMostrada + paso);

            int nivel = NivelJugador.NivelCon(experienciaMostrada);
            if (nivel > nivelMostrado)
            {
                Sonidos.Tocar(nota, 0.8f, Sonidos.PitchDe(Mathf.Min(24, 12 + (nivel - nivelMostrado) * 2)));
                nivelMostrado = nivel;
                if (enElBoton != null) enElBoton.golpe = 0f;
                if (enLaVentana != null) enLaVentana.golpe = 0f;
            }
        }
        PintarMedidor(enElBoton, false);
        if (Abierta) PintarMedidor(enLaVentana, true);
    }

    // Solo cuando cambia lo que se ve: se llama en cada cuadro, y armar los textos aloca.
    private void PintarMedidor(Medidor medidor, bool conNumeros)
    {
        if (medidor == null) return;
        long entera = (long)System.Math.Floor(experienciaMostrada);
        if (entera == medidor.pintada && Idioma.Revision == medidor.idioma) return;
        medidor.pintada = entera;
        medidor.idioma = Idioma.Revision;

        int nivel = NivelJugador.NivelCon(experienciaMostrada);
        double dentro = experienciaMostrada - NivelJugador.ExperienciaDelNivel(nivel);
        int costo = NivelJugador.CostoDelNivel(nivel);
        medidor.numero.SetText("{0}", nivel);
        medidor.titulo.SetText(Textos.De("nivel_titulo"), nivel);
        medidor.relleno.anchorMax = new Vector2(Mathf.Clamp01((float)(dentro / costo)), 1f);
        if (conNumeros && medidor.experiencia != null)
            medidor.experiencia.text = Textos.Formato("nivel_experiencia", FormatoNumeros.Compacto(dentro), FormatoNumeros.Compacto(costo));
    }

    private static void Saltar(Medidor medidor, float dt)
    {
        if (medidor == null) return;
        if (medidor.golpe >= 0f) medidor.golpe += dt;
        float salto = medidor.golpe >= 0f ? 0.25f * CurvasUI.Campana(Mathf.Clamp01(medidor.golpe / 0.35f)) : 0f;
        medidor.moneda.localScale = Vector3.one * (1f + salto);
    }

    private void Refrescar()
    {
        revisionVista = Progreso.Revision;
        textoTotal.text = Textos.Formato("logros_total", Logros.GanadasEnTotal, Logros.Total);

        var premio = NivelJugador.Pendiente;
        cobrarNivel.gameObject.SetActive(premio != null);
        textoProximo.gameObject.SetActive(premio == null);
        if (premio != null)
            textoCobrarNivel.text = Textos.Formato("nivel_cobrar", premio.nivel, FormatoNumeros.Compacto(NivelJugador.Monto(premio)));
        else
            textoProximo.text = Textos.Formato("nivel_proximo", FormatoNumeros.Compacto(NivelJugador.PremioDelSiguiente));

        foreach (var fila in filas) PintarFila(fila);
    }

    private void PintarFila(Fila fila)
    {
        string id = fila.familia.id;
        int ganadas = Logros.Ganadas(id);
        double valor = fila.familia.Valor;

        // La moneda grande es de la mas alta ganada; sin ninguna, apagada.
        Color cara = ganadas > 0 ? ColoresDeLogro.De(ganadas - 1) : ColorApagado;
        fila.cara.color = cara;
        fila.borde.color = ganadas > 0 ? ColoresDeLogro.Borde(cara) : ColorApagado;
        fila.inicial.color = ganadas > 0 ? ColoresDeLogro.TextoSobreMetal : ColorDeTexto;

        float fraccion;
        if (ganadas < Logros.Escalones)
        {
            double meta = fila.familia.metas[ganadas];
            fila.descripcion.text = Logros.Descripcion(id, meta);
            fila.cuenta.text = FormatoNumeros.Compacto(System.Math.Min(valor, meta)) + " / " + FormatoNumeros.Compacto(meta);
            fraccion = meta > 0 ? Mathf.Clamp01((float)(valor / meta)) : 1f;
        }
        else
        {
            fila.descripcion.text = Logros.Descripcion(id, fila.familia.metas[Logros.Escalones - 1]);
            fila.cuenta.text = Textos.De("bestiario_completo");
            fraccion = 1f;
        }
        fila.relleno.anchorMax = new Vector2(fraccion, 1f);

        for (int e = 0; e < Logros.Escalones; e++)
        {
            Color c = e < ganadas ? ColoresDeLogro.De(e) : ColorApagado;
            fila.caras[e].color = c;
            fila.bordes[e].color = e < ganadas ? ColoresDeLogro.Borde(c) : ColorApagado;
        }

        var paraCobrar = Logros.ParaCobrar(id);
        fila.cobrar.SetActive(paraCobrar != null);
        if (paraCobrar != null)
            fila.textoCobrar.text = Textos.Formato("logro_cobrar", FormatoNumeros.Compacto(Logros.Experiencia(paraCobrar)));
        fila.tilde.enabled = tilde != null && Logros.Cobradas(id) >= Logros.Escalones;
    }

    private void Armar()
    {
        var rtPanel = ConstructorUI.Estirar((RectTransform)transform, "VentanaLogros");
        panel = rtPanel.gameObject;
        panel.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.45f);   // tapa los toques del menu
        idiomaArmado = Idioma.Revision;
        temaArmado = Tema.Revision;

        ventana = ConstructorUI.Rect(rtPanel, "Ventana", new Vector2(0f, 10f), new Vector2(1180f, 820f));
        var fondo = ventana.gameObject.AddComponent<Image>();
        ConstructorUI.Redondear(fondo, pildora, 3f);
        fondo.color = ColorDeVentana;

        var titulo = ConstructorUI.Texto(ventana, "Titulo", Textos.De("logros_titulo"), 80f, colorTitulo,
                                         new Vector2(0f, 340f), new Vector2(1100f, 100f), fuente);
        if (materialContorno != null) titulo.fontSharedMaterial = materialContorno;
        textoTotal = ConstructorUI.Texto(ventana, "Total", "", 36f, ColorDeTexto, new Vector2(430f, 338f), new Vector2(200f, 50f), fuente);
        textoTotal.alignment = TextAlignmentOptions.Right;

        ArmarTarjetaDelNivel();
        ArmarLista();

        var volver = ConstructorUI.Boton(ventana, "Volver", new Vector2(0f, -345f), new Vector2(340f, 100f), ColorDeVidrio,
                                         Color.white, iconoAtras, Textos.De("comun_volver"), 46f, fuente, pildora, sonidoClick);
        volver.onClick.AddListener(Cerrar);
    }

    private void ArmarTarjetaDelNivel()
    {
        tarjetaNivel = ConstructorUI.Rect(ventana, "Nivel", new Vector2(0f, 210f), new Vector2(1080f, 140f));
        var fondo = tarjetaNivel.gameObject.AddComponent<Image>();
        ConstructorUI.Redondear(fondo, pildora, 3f);
        fondo.color = ColorDeFila;

        enLaVentana = new Medidor();
        enLaVentana.moneda = MonedaDeNivel(tarjetaNivel, new Vector2(-462f, 0f), 120f, 58f, out enLaVentana.numero);
        enLaVentana.titulo = ConstructorUI.Texto(tarjetaNivel, "Titulo", "", 50f, ColorDeTexto, new Vector2(-150f, 36f), new Vector2(420f, 60f), fuente);
        enLaVentana.titulo.alignment = TextAlignmentOptions.Left;
        enLaVentana.relleno = ConstructorUI.Barra(tarjetaNivel, "Barra", new Vector2(-130f, -6f), new Vector2(460f, 24f), pildora,
                                                  Tema.Elegir(new Color(0f, 0f, 0f, 0.12f), RolDeTema.Surco), colorExperiencia);
        enLaVentana.experiencia = ConstructorUI.Texto(tarjetaNivel, "Experiencia", "", 30f, ColorDeTexto, new Vector2(-150f, -44f), new Vector2(420f, 40f), fuente);
        enLaVentana.experiencia.alignment = TextAlignmentOptions.Left;

        cobrarNivel = ConstructorUI.Boton(tarjetaNivel, "Cobrar", new Vector2(372f, 0f), new Vector2(300f, 96f), ColorVerde,
                                          ColorTextoVerde, null, "", 38f, fuente, pildora, sonidoClick);
        cobrarNivel.onClick.AddListener(CobrarNivel);
        cobrarNivel.GetComponent<BotonJugoso>().respirar = true;
        textoCobrarNivel = cobrarNivel.transform.Find("Visual/Texto").GetComponent<TMP_Text>();
        textoCobrarNivel.enableAutoSizing = true;
        textoCobrarNivel.fontSizeMin = 24f;
        textoCobrarNivel.fontSizeMax = 38f;

        textoProximo = ConstructorUI.Texto(tarjetaNivel, "Proximo", "", 32f, ColorDeTexto, new Vector2(372f, 0f), new Vector2(300f, 96f), fuente);
        textoProximo.textWrappingMode = TextWrappingModes.Normal;
        PintarMedidor(enLaVentana, true);
    }

    private void ArmarLista()
    {
        var vista = ConstructorUI.Rect(ventana, "Lista", new Vector2(0f, -79f), new Vector2(1100f, AltoVista));
        vista.gameObject.AddComponent<RectMask2D>();
        // Transparente, pero recibe el arrastre entre fila y fila.
        vista.gameObject.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0f);

        contenido = ConstructorUI.Rect(vista, "Contenido", Vector2.zero, new Vector2(1080f, filas.Length * PasoFila + 8f));
        contenido.anchorMin = contenido.anchorMax = new Vector2(0.5f, 1f);
        contenido.pivot = new Vector2(0.5f, 1f);
        contenido.anchoredPosition = Vector2.zero;

        lista = vista.gameObject.AddComponent<ScrollRect>();
        lista.content = contenido;
        lista.viewport = vista;
        lista.horizontal = false;
        lista.vertical = true;
        lista.movementType = ScrollRect.MovementType.Elastic;
        lista.scrollSensitivity = 40f;

        float mitad = contenido.sizeDelta.y * 0.5f;
        for (int i = 0; i < filas.Length; i++) filas[i] = ArmarFila(i, mitad - 4f - AltoFila * 0.5f - i * PasoFila);
    }

    private Fila ArmarFila(int indice, float y)
    {
        var fila = new Fila { familia = Logros.Familias[indice] };
        fila.raiz = ConstructorUI.Rect(contenido, fila.familia.id, new Vector2(0f, y), new Vector2(1080f, AltoFila));
        var fondo = fila.raiz.gameObject.AddComponent<Image>();
        ConstructorUI.Redondear(fondo, pildora, 3f);
        fondo.color = ColorDeFila;
        // Para que el arrastre que empieza sobre una fila llegue a la lista.
        fondo.raycastTarget = true;

        string nombre = Logros.Nombre(fila.familia.id);
        fila.borde = ConstructorUI.Imagen(fila.raiz, "Borde", new Vector2(-478f, 0f), new Vector2(86f, 86f), circulo, ColorApagado);
        fila.cara = ConstructorUI.Imagen(fila.raiz, "Cara", new Vector2(-478f, 0f), new Vector2(72f, 72f), circulo, ColorApagado);
        fila.inicial = ConstructorUI.Texto(fila.raiz, "Inicial", nombre.Substring(0, 1), 44f, ColorDeTexto,
                                           new Vector2(-478f, 0f), new Vector2(86f, 86f), fuente);

        var rotulo = ConstructorUI.Texto(fila.raiz, "Nombre", nombre, 40f, ColorDeTexto, new Vector2(-150f, 30f), new Vector2(560f, 48f), fuente);
        rotulo.alignment = TextAlignmentOptions.Left;
        fila.descripcion = ConstructorUI.Texto(fila.raiz, "Descripcion", "", 30f, ColorDeTexto, new Vector2(-150f, -4f), new Vector2(560f, 40f), fuente);
        fila.descripcion.alignment = TextAlignmentOptions.Left;
        fila.descripcion.enableAutoSizing = true;
        fila.descripcion.fontSizeMin = 20f;
        fila.descripcion.fontSizeMax = 30f;

        fila.relleno = ConstructorUI.Barra(fila.raiz, "Barra", new Vector2(-255f, -36f), new Vector2(350f, 14f), pildora,
                                           Tema.Elegir(new Color(0f, 0f, 0f, 0.12f), RolDeTema.Surco), colorBarra);
        fila.cuenta = ConstructorUI.Texto(fila.raiz, "Cuenta", "", 28f, ColorDeTexto, new Vector2(50f, -36f), new Vector2(230f, 36f), fuente);
        fila.cuenta.alignment = TextAlignmentOptions.Left;

        fila.caras = new Image[Logros.Escalones];
        fila.bordes = new Image[Logros.Escalones];
        for (int e = 0; e < Logros.Escalones; e++)
        {
            var x = 200f + e * 46f;
            fila.bordes[e] = ConstructorUI.Imagen(fila.raiz, "Moneda" + e, new Vector2(x, 0f), new Vector2(38f, 38f), circulo, ColorApagado);
            fila.caras[e] = ConstructorUI.Imagen(fila.bordes[e].rectTransform, "Cara", Vector2.zero, new Vector2(30f, 30f), circulo, ColorApagado);
        }

        var cobrar = ConstructorUI.Boton(fila.raiz, "Cobrar", new Vector2(445f, 0f), new Vector2(190f, 80f), ColorVerde,
                                         ColorTextoVerde, null, "", 34f, fuente, pildora, sonidoClick);
        cobrar.onClick.AddListener(() => CobrarLogro(indice));
        cobrar.GetComponent<BotonJugoso>().respirar = true;
        fila.cobrar = cobrar.gameObject;
        fila.textoCobrar = cobrar.transform.Find("Visual/Texto").GetComponent<TMP_Text>();
        fila.textoCobrar.enableAutoSizing = true;
        fila.textoCobrar.fontSizeMin = 20f;
        fila.textoCobrar.fontSizeMax = 34f;

        fila.tilde = ConstructorUI.Imagen(fila.raiz, "Tilde", new Vector2(445f, 0f), new Vector2(64f, 64f), tilde, new Color(0.1f, 0.3f, 0.05f, 1f));
        fila.tilde.enabled = false;
        return fila;
    }
}

// Los colores de las monedas de los logros, de la ventana y del aviso en la partida.
public static class ColoresDeLogro
{
    public static readonly Color Bronce = new Color(0.86f, 0.55f, 0.3f, 1f);
    public static readonly Color Plata = new Color(0.8f, 0.82f, 0.88f, 1f);
    public static readonly Color Oro = new Color(1f, 0.79f, 0.2f, 1f);

    // El texto que va encima de una moneda: oscuro siempre, que el metal no cambia con el tema.
    public static readonly Color TextoSobreMetal = new Color(0.23f, 0.15f, 0.05f, 1f);

    public static Color De(int escalon)
    {
        return escalon <= 0 ? Bronce : escalon == 1 ? Plata : Oro;
    }

    // El canto de la moneda, mas oscuro que la cara.
    public static Color Borde(Color cara)
    {
        var c = Color.Lerp(cara, Color.black, 0.3f);
        c.a = 1f;
        return c;
    }
}
