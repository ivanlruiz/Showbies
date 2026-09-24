using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// El boton MISIONES del menu y la ventana que abre, con el desafio de la semana arriba
// (DesafioSemanal, pedido de Ivan: un objetivo grande que dura de lunes a domingo, con su
// propio premio) y las tres misiones del dia
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
    [Tooltip("El fondo de la fila del desafio semanal: dorado translucido, para que se lea como el especial.")]
    public Color colorSemanal = new Color(1f, 0.79f, 0.2f, 0.18f);
    public float esperaDelCofre = 0.6f;
    public Color[] coloresDificultad =
    {
        new Color(0.49f, 0.88f, 0.29f, 1f),
        new Color(1f, 0.79f, 0.2f, 1f),
        new Color(1f, 0.42f, 0.35f, 1f),
    };

    // Los colores de arriba son los del tema claro, que es como se ve el juego desde
    // siempre; con el oscuro cada uno pasa a su papel (ver Tema).
    private Color ColorDeVentana { get { return Tema.Elegir(colorVentana, RolDeTema.Panel); } }
    private Color ColorDeTexto { get { return Tema.Elegir(colorTextoOscuro, RolDeTema.Texto); } }
    private Color ColorDeFila { get { return Tema.Elegir(colorFila, RolDeTema.Hueco); } }
    private Color ColorDeVidrio { get { return Tema.Elegir(colorVidrio, RolDeTema.Vidrio); } }

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

    // La fila del desafio de la semana, arriba de las tres del dia.
    private class Semana
    {
        public RectTransform raiz;
        public Image fondo;
        public TMP_Text etiqueta;
        public TMP_Text descripcion;
        public TMP_Text cuenta;
        public TMP_Text premio;
        public TMP_Text termina;
        public RectTransform relleno;
        public GameObject cobrar;
        public Image tilde;
        public float golpe = -1f;
    }

    private GameObject panel;
    private RectTransform ventana;
    private TMP_Text textoNuevas;
    private Semana semana;
    private readonly Fila[] filas = new Fila[MisionesDiarias.Cantidad];
    private Texture2D texturaCirculo;
    private Sprite circulo;
    private RectTransform insignia;
    private TMP_Text numeroInsignia;
    private int revisionVista = -1;
    private float reloj = -1f;
    private int idiomaArmado = -1;
    private int minutoMostrado = -1;
    private float relojCuenta;
    private int temaArmado = -1;
    private float relojInsignia;
    private float proximaCuenta;
    private int revisionContada = -1;
    private int cuentaInsignia;
    private int cuentaEscrita = -1;
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
        if (circulo != null) Destroy(circulo);
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

        // Contar lo que hay para cobrar pregunta el dia (Asegurar): con la hora confiable,
        // en Android es una llamada por JNI (RelojConfiable), mas fechas armadas y parseadas,
        // y corria en cada cuadro con el menu abierto para mostrar casi siempre el mismo
        // numero. Se cuenta dos veces por segundo, y en el acto cuando cambia el progreso
        // (cobrar, las misiones nuevas del dia): se ve igual que antes.
        if (relojInsignia >= proximaCuenta || Progreso.Revision != revisionContada)
        {
            proximaCuenta = relojInsignia + 0.5f;
            cuentaInsignia = MisionesDiarias.PorCobrar + (DesafioSemanal.PorCobrar ? 1 : 0);
            // Despues de contar: armar las misiones del dia nuevo tambien sube la revision.
            revisionContada = Progreso.Revision;
        }

        // El numero se escribe solo cuando cambia: escribirlo en cada cuadro armaba una
        // cadena por cuadro. Latir sin texto sigue prendiendo, apagando y latiendo.
        bool otroNumero = cuentaInsignia != cuentaEscrita;
        ConstructorUI.Latir(insignia, otroNumero ? numeroInsignia : null, cuentaInsignia, relojInsignia);
        cuentaEscrita = cuentaInsignia;
    }

    // --- La ventana ----------------------------------------------------------------

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
        MisionesDiarias.Asegurar();
        DesafioSemanal.Asegurar();
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
        ventana.localScale = Vector3.one * (EscalaQueEntra() * CurvasUI.SalidaAtras(Mathf.Clamp01(reloj / 0.45f)));

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

        // La hora se pregunta una vez por segundo y el texto se arma solo cuando cambia
        // el minuto: en Android preguntarla es una llamada por JNI, y armar el texto,
        // una cadena; las dos cosas en cada frame para mostrar lo mismo.
        relojCuenta -= dt;
        if (relojCuenta <= 0f)
        {
            relojCuenta = 1f;
            int minutos = MinutosParaLasNuevas();
            if (minutos != minutoMostrado)
            {
                minutoMostrado = minutos;
                textoNuevas.text = TextoNuevas(minutos);
            }
        }

        if (semana != null && semana.golpe >= 0f)
        {
            semana.golpe += dt;
            float saltoSemanal = 0.08f * CurvasUI.Campana(Mathf.Clamp01(semana.golpe / 0.4f));
            semana.raiz.localScale = Vector3.one * (1f + saltoSemanal);
        }

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
            fila.premio.text = FormatoNumeros.Compacto(MisionesDiarias.Monto(mision.dificultad, MisionesDiarias.OleadaDeHoy));
            float fraccion = mision.objetivo > 0 ? Mathf.Clamp01((float)(avance / mision.objetivo)) : 1f;
            fila.relleno.anchorMax = new Vector2(fraccion, 1f);
            // Una mision cobrada pinta su fila de verde: ahi el texto va oscuro con
            // cualquier tema, que el verde no cambia. Con el claro del tema oscuro
            // quedaba blanco sobre verde.
            fila.fondo.color = mision.cobrada ? colorCumplida : ColorDeFila;
            Color textoDeLaFila = mision.cobrada ? colorTextoOscuro : ColorDeTexto;
            fila.descripcion.color = textoDeLaFila;
            fila.cuenta.color = textoDeLaFila;
            fila.premio.color = textoDeLaFila;
            fila.cobrar.SetActive(cumplida && !mision.cobrada);
            fila.tilde.enabled = mision.cobrada && tilde != null;
        }
        PintarSemana();
        PintarCofre(mejor);
    }

    private void PintarSemana()
    {
        if (semana == null) return;
        var estado = DesafioSemanal.Estado;
        bool cumplido = DesafioSemanal.Cumplido;
        double avance = Math.Min(DesafioSemanal.Avance, estado.objetivo);

        semana.descripcion.text = DesafioSemanal.Descripcion(estado);
        semana.cuenta.text = FormatoNumeros.Compacto(avance) + " / " + FormatoNumeros.Compacto(estado.objetivo);
        semana.premio.text = FormatoNumeros.Compacto(DesafioSemanal.MontoDeEstaSemana);
        float fraccion = estado.objetivo > 0 ? Mathf.Clamp01((float)(avance / estado.objetivo)) : 1f;
        semana.relleno.anchorMax = new Vector2(fraccion, 1f);

        int dias = DesafioSemanal.DiasQueFaltan();
        semana.termina.text = dias <= 1 ? Textos.De("semanal_termina_hoy") : Textos.Formato("semanal_termina", dias);

        semana.fondo.color = estado.cobrado ? colorCumplida : colorSemanal;
        Color textoDeLaFila = estado.cobrado ? colorTextoOscuro : ColorDeTexto;
        semana.descripcion.color = textoDeLaFila;
        semana.cuenta.color = textoDeLaFila;
        semana.premio.color = textoDeLaFila;
        semana.termina.color = textoDeLaFila;
        semana.etiqueta.color = estado.cobrado ? colorTextoOscuro : colorTitulo;
        semana.cobrar.SetActive(cumplido && !estado.cobrado);
        semana.tilde.enabled = estado.cobrado && tilde != null;
    }

    private void PintarCofre(int mejor)
    {
        if (cofre == null) return;
        Color fondo, texto;
        if (MisionesDiarias.CofreCobrado)
        {
            fondo = colorCumplida;
            texto = colorTextoAbierto;
            textoCofre.text = Textos.Formato("misiones_cofre_abierto", FormatoNumeros.Compacto(MisionesDiarias.MontoCofre(MisionesDiarias.OleadaDeHoy)));
        }
        else if (MisionesDiarias.CofreDisponible)
        {
            fondo = colorCofre;
            texto = colorTextoCofre;
            textoCofre.text = Textos.Formato("misiones_cofre", FormatoNumeros.Compacto(MisionesDiarias.MontoCofre(MisionesDiarias.OleadaDeHoy)));
        }
        else
        {
            fondo = ColorDeVidrio;
            texto = Color.white;
            textoCofre.text = Textos.Formato("misiones_cofre_falta", MisionesDiarias.Cobradas);
        }
        fondoCofre.color = fondo;
        ConstructorUI.PintarHalo(cofre, fondo);
        textoCofre.color = texto;
        if (iconoDelCofre != null) iconoDelCofre.color = texto;
        if (jugoCofre != null) jugoCofre.respirar = MisionesDiarias.CofreDisponible;
    }

    // La ventana, con el halo de neon que sobresale 40 de cada lado, tiene que entrar en el
    // alto del canvas "Main Menu", que escala por el ancho (match 0): mide 1080 en 16:9, 864
    // en 20:9 y 823 en 21:9. Con sus 820 de alto, en 20:9 se cortaba el halo de arriba y en
    // 21:9 hasta la linea y las puntas. En las pantallas mas largas se achica lo justo, y
    // nunca se agranda. Se mide en cada cuadro: en PC la ventana del juego cambia de tamanio.
    private const float HaloDeLaVentana = 40f;
    private const float MargenAlBorde = 10f;

    private float EscalaQueEntra()
    {
        float alto = ((RectTransform)panel.transform).rect.height;
        if (alto <= 0f) return 1f;
        float lugar = alto * 0.5f - Mathf.Abs(ventana.anchoredPosition.y) - MargenAlBorde;
        float mitad = ventana.rect.height * 0.5f + HaloDeLaVentana;
        return Mathf.Clamp(lugar / mitad, 0.5f, 1f);
    }

    // Con la hora confiable, la misma con que cambian las misiones: con DateTime.Now, un
    // reloj movido mostraba una cuenta que no se correspondia con nada.
    private static int MinutosParaLasNuevas()
    {
        DateTime ahora = Progreso.AhoraConfiable();
        TimeSpan falta = ahora.Date.AddDays(1) - ahora;
        return falta > TimeSpan.Zero ? (int)falta.TotalMinutes : 0;
    }

    private static string TextoNuevas(int minutos)
    {
        return Textos.Formato("misiones_nuevas", minutos / 60, minutos % 60);
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
        temaArmado = Tema.Revision;

        ventana = Rect(rtPanel, "Ventana", new Vector2(0f, 10f), new Vector2(1180f, 820f));
        var fondo = ventana.gameObject.AddComponent<Image>();
        ConstructorUI.VentanaNeon(ventana, fondo, pildora);
        fondo.color = ColorDeVentana;

        var titulo = Texto(ventana, "Titulo", Textos.De("misiones_titulo"), 80f, colorTitulo, new Vector2(0f, 330f), new Vector2(1100f, 100f));
        if (materialContorno != null) titulo.fontSharedMaterial = materialContorno;
        minutoMostrado = MinutosParaLasNuevas();
        textoNuevas = Texto(ventana, "Nuevas", TextoNuevas(minutoMostrado), 36f, ColorDeTexto, new Vector2(0f, 262f), new Vector2(1100f, 50f));

        semana = ArmarSemana(160f);
        for (int i = 0; i < filas.Length; i++) filas[i] = ArmarFila(i, 20f - 125f * i);

        var volver = ArmarBoton(ventana, "Volver", new Vector2(-300f, -340f), new Vector2(340f, 100f),
                                ColorDeVidrio, Color.white, iconoAtras, Textos.De("comun_volver"), 46f);
        volver.onClick.AddListener(Cerrar);

        cofre = ArmarBoton(ventana, "Cofre", new Vector2(200f, -340f), new Vector2(480f, 100f),
                           ColorDeVidrio, Color.white, iconoCofre, Textos.Formato("misiones_cofre_falta", 0), 46f);
        cofre.onClick.AddListener(TocarCofre);
        fondoCofre = cofre.transform.Find("Visual/Fondo").GetComponent<Image>();
        textoCofre = cofre.transform.Find("Visual/Texto").GetComponent<TMP_Text>();
        var icono = cofre.transform.Find("Visual/Icono");
        if (icono != null) iconoDelCofre = icono.GetComponent<Image>();
        jugoCofre = cofre.GetComponent<BotonJugoso>();
    }

    // La fila del desafio de la semana: como las del dia pero mas alta, dorada y con su
    // etiqueta y los dias que faltan.
    private Semana ArmarSemana(float y)
    {
        var s = new Semana();
        s.raiz = Rect(ventana, "Semanal", new Vector2(0f, y), new Vector2(1080f, 140f));
        s.fondo = s.raiz.gameObject.AddComponent<Image>();
        Redondear(s.fondo, 3f);
        s.fondo.color = colorSemanal;

        s.etiqueta = Texto(s.raiz, "Etiqueta", Textos.De("semanal_titulo"), 30f, colorTitulo,
                           new Vector2(-235f, 46f), new Vector2(620f, 40f));
        s.etiqueta.alignment = TextAlignmentOptions.Left;

        s.descripcion = Texto(s.raiz, "Descripcion", "", 44f, ColorDeTexto, new Vector2(-150f, 6f), new Vector2(620f, 56f));
        s.descripcion.alignment = TextAlignmentOptions.Left;

        var barra = Rect(s.raiz, "Barra", new Vector2(-230f, -44f), new Vector2(460f, 18f));
        var imgBarra = barra.gameObject.AddComponent<Image>();
        Redondear(imgBarra, 6f);
        imgBarra.color = Tema.Elegir(new Color(0f, 0f, 0f, 0.12f), RolDeTema.Surco);
        s.relleno = Rect(barra, "Relleno", Vector2.zero, Vector2.zero);
        s.relleno.anchorMin = Vector2.zero;
        s.relleno.anchorMax = new Vector2(0f, 1f);
        s.relleno.pivot = new Vector2(0f, 0.5f);
        s.relleno.offsetMin = s.relleno.offsetMax = Vector2.zero;
        var imgRelleno = s.relleno.gameObject.AddComponent<Image>();
        Redondear(imgRelleno, 6f);
        imgRelleno.color = colorMoneda;

        s.cuenta = Texto(s.raiz, "Cuenta", "", 32f, ColorDeTexto, new Vector2(100f, -44f), new Vector2(200f, 40f));
        s.cuenta.alignment = TextAlignmentOptions.Left;

        s.termina = Texto(s.raiz, "Termina", "", 28f, ColorDeTexto, new Vector2(300f, 46f), new Vector2(300f, 40f));
        s.termina.alignment = TextAlignmentOptions.Right;

        var moneda = Rect(s.raiz, "Moneda", new Vector2(190f, 4f), new Vector2(48f, 48f));
        var imgMoneda = moneda.gameObject.AddComponent<Image>();
        imgMoneda.sprite = circulo;
        imgMoneda.color = colorMoneda;
        // El premio de la semana es un numero grande: mas lugar y separado de la moneda. La
        // caja termina donde empieza COBRAR (x = 340), que se dibuja encima: desde la oleada
        // 12 paga cinco cifras y el boton tapaba parte del ultimo digito. Se achica.
        s.premio = Texto(s.raiz, "Premio", "", 44f, ColorDeTexto, new Vector2(287.5f, 4f), new Vector2(105f, 56f));
        s.premio.alignment = TextAlignmentOptions.Left;
        Achicar(s.premio, 30f);

        var cobrar = ArmarBoton(s.raiz, "Cobrar", new Vector2(440f, 0f), new Vector2(200f, 84f),
                                ConstructorUI.Verde, ConstructorUI.VerdeTexto, null,
                                Textos.De("mision_cobrar"), 40f);
        cobrar.onClick.AddListener(CobrarSemana);
        cobrar.GetComponent<BotonJugoso>().respirar = true;
        s.cobrar = cobrar.gameObject;

        var rtTilde = Rect(s.raiz, "Tilde", new Vector2(440f, 0f), new Vector2(64f, 64f));
        s.tilde = rtTilde.gameObject.AddComponent<Image>();
        s.tilde.sprite = tilde;
        s.tilde.color = new Color(0.1f, 0.3f, 0.05f, 1f);
        s.tilde.raycastTarget = false;
        s.tilde.enabled = false;
        return s;
    }

    private void CobrarSemana()
    {
        double monto = DesafioSemanal.Cobrar();
        if (monto <= 0) return;
        for (int k = 0; k < SemitonosFestejo.Length; k++)
            Sonidos.Programar(nota, 0.05 * k, 0.8f, Sonidos.PitchDe(SemitonosFestejo[k] + 5));
        Sonidos.Tocar(sonidoFestejo, 0.8f);
        CamaraJugador.Temblar(0.25f);
        semana.golpe = 0f;
        Refrescar();
    }

    private Fila ArmarFila(int indice, float y)
    {
        var fila = new Fila();
        fila.raiz = Rect(ventana, "Mision" + indice, new Vector2(0f, y), new Vector2(1080f, 110f));
        fila.fondo = fila.raiz.gameObject.AddComponent<Image>();
        Redondear(fila.fondo, 3f);
        fila.fondo.color = ColorDeFila;

        var punto = Rect(fila.raiz, "Dificultad", new Vector2(-490f, 0f), new Vector2(46f, 46f));
        var imgPunto = punto.gameObject.AddComponent<Image>();
        imgPunto.sprite = circulo;
        imgPunto.color = coloresDificultad[Mathf.Min(indice, coloresDificultad.Length - 1)];

        fila.descripcion = Texto(fila.raiz, "Descripcion", "", 42f, ColorDeTexto, new Vector2(-150f, 20f), new Vector2(620f, 56f));
        fila.descripcion.alignment = TextAlignmentOptions.Left;

        // La barra: un fondo y un relleno que crece con el ancla derecha (sin sprite, un
        // Image Filled no llena; ver las trampas de CLAUDE.md).
        var barra = Rect(fila.raiz, "Barra", new Vector2(-230f, -24f), new Vector2(460f, 18f));
        var imgBarra = barra.gameObject.AddComponent<Image>();
        Redondear(imgBarra, 6f);
        imgBarra.color = Tema.Elegir(new Color(0f, 0f, 0f, 0.12f), RolDeTema.Surco);
        fila.relleno = Rect(barra, "Relleno", Vector2.zero, Vector2.zero);
        fila.relleno.anchorMin = Vector2.zero;
        fila.relleno.anchorMax = new Vector2(0f, 1f);
        fila.relleno.pivot = new Vector2(0f, 0.5f);
        fila.relleno.offsetMin = fila.relleno.offsetMax = Vector2.zero;
        var imgRelleno = fila.relleno.gameObject.AddComponent<Image>();
        Redondear(imgRelleno, 6f);
        imgRelleno.color = colorBarra;

        // Con cinco cifras de cada lado ("12.180 / 20.300") se metia debajo de la moneda: se
        // achica para no salirse de su caja, que termina antes.
        fila.cuenta = Texto(fila.raiz, "Cuenta", "", 32f, ColorDeTexto, new Vector2(100f, -24f), new Vector2(160f, 40f));
        fila.cuenta.alignment = TextAlignmentOptions.Left;
        Achicar(fila.cuenta, 22f);

        var moneda = Rect(fila.raiz, "Moneda", new Vector2(215f, 0f), new Vector2(44f, 44f));
        var imgMoneda = moneda.gameObject.AddComponent<Image>();
        imgMoneda.sprite = circulo;
        imgMoneda.color = colorMoneda;
        // La caja del premio termina donde empieza COBRAR (x = 340), que se crea despues y se
        // dibuja encima: desde la oleada ~25 la dificil paga cinco cifras ("20.550") y el
        // boton tapaba el ultimo digito. Se achica en vez de salirse.
        fila.premio = Texto(fila.raiz, "Premio", "", 42f, ColorDeTexto, new Vector2(295f, 0f), new Vector2(90f, 56f));
        fila.premio.alignment = TextAlignmentOptions.Left;
        Achicar(fila.premio, 28f);

        var cobrar = ArmarBoton(fila.raiz, "Cobrar", new Vector2(440f, 0f), new Vector2(200f, 84f),
                                ConstructorUI.Verde, ConstructorUI.VerdeTexto, null,
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

    // Un numero que se achica hasta 'minimo' para no salirse de su caja (en una sola linea):
    // el que entra queda del tamanio de siempre.
    private static void Achicar(TMP_Text texto, float minimo)
    {
        texto.fontSizeMax = texto.fontSize;
        texto.fontSizeMin = minimo;
        texto.enableAutoSizing = true;
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
