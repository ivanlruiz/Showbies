using TMPro;
using UnityEngine;
using UnityEngine.UI;

// El boton del bestiario (un trofeo, arriba a la derecha al lado de las misiones) y la
// ventana que abre (Bestiario): una tarjeta por tipo de zombi con su retrato, su nombre,
// cuantos lleva matados, sus tres estrellas, la barra hasta la siguiente y un boton
// para cobrar la estrella ganada. Las estrellas ganadas sin cobrar laten; al cobrar,
// el arpegio de la diaria y la tarjeta salta.
//
// Con los cinco zombis con habilidad son diez, en dos pestanas (de Argon, 19/10): COMUNES y ESPECIALES, cinco
// tarjetas cada una, como antes, y cada pestana con su insignia de estrellas por cobrar. El retrato (de tres cuartos,
// cabeza y hombros, que saca RetratosDelBestiario) va en un disco oscuro con un anillo del color de cada zombi; el que
// todavia no se mato sale tapado: su silueta, "???" y la oleada en que aparece, como un adelanto.
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

    // El anillo de cada tipo, en el orden de Bestiario.Tipos (de Argon, 19/10): el acento brillante de cada uno, que se
    // recorta sobre el disco oscuro aunque la piel sea oscura (el saltador, el hinchado). En codigo y no en el inspector:
    // la escena guardaba los cinco de antes y le habria pintado el ultimo a todos los nuevos.
    public static readonly Color[] Acentos =
    {
        new Color(0.463f, 1f, 0.012f),    // #76FF03, caminante
        new Color(0.776f, 1f, 0f),        // #C6FF00, corredor
        new Color(0f, 0.898f, 1f),        // #00E5FF, veloz
        new Color(1f, 0.239f, 0f),        // #FF3D00, tanque
        new Color(0.835f, 0f, 0.976f),    // #D500F9, jefe
        new Color(0.831f, 1f, 0f),        // #D4FF00, escupidor
        new Color(0.396f, 0.451f, 1f),    // #6573FF, saltador
        new Color(1f, 0.09f, 0.267f),     // #FF1744, chaman
        new Color(1f, 0.769f, 0f),        // #FFC400, blindado
        new Color(0f, 1f, 0.4f),          // #00FF66, hinchado
    };
    public static readonly Color FondoDelRetrato = new Color(0.078f, 0.094f, 0.125f);   // #141820
    public static readonly Color ColorDeLaSilueta = new Color(0.27f, 0.3f, 0.37f);
    public const float AlfaDelAnilloTapado = 0.35f;
    static readonly Color CelesteTexto = new Color(0f, 0.165f, 0.188f, 1f);

    // Lo que mide cada cosa: la ventana crecio 60 para las pestanas.
    public const float AltoDeLaVentana = 720f;
    public const float LadoDelRetrato = 96f;
    public const float GrosorDelAnillo = 4f;

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
        public int indice;
        public RectTransform raiz;
        public Image retrato;
        public Image anillo;
        public Sprite fotoDelRetrato;
        public Sprite fotoDeLaSilueta;
        public TMP_Text nombre;
        public TMP_Text cuenta;
        public GameObject barra;
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
    private Texture2D texturaAnillo;
    private Sprite anillo;

    // Las dos pestanas: 0 los comunes, 1 los especiales.
    private int pestana;
    private readonly Button[] pestanas = new Button[2];
    private readonly RectTransform[] grupos = new RectTransform[2];
    private readonly RectTransform[] insigniasDePestana = new RectTransform[2];
    private readonly TMP_Text[] numerosDePestana = new TMP_Text[2];
    private readonly int[] cuentasDePestana = { -1, -1 };
    private RectTransform insignia;
    private TMP_Text numeroInsignia;
    private int revisionVista = -1;
    private float reloj = -1f;
    private int idiomaArmado = -1;
    private int temaArmado = -1;
    private float relojInsignia;
    private int cuentaEscrita = -1;

    private void Start()
    {
        texturaCirculo = TexturasUI.Circulo(128);
        circulo = Sprite.Create(texturaCirculo, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f));
        texturaAnillo = TexturasUI.Anillo(128, GrosorDelAnillo / (LadoDelRetrato * 0.5f));
        anillo = Sprite.Create(texturaAnillo, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f));

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
        botonEsquina = boton;

        Armar();
        panel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (panel != null) Abierta = false;
        if (circulo != null) Destroy(circulo);
        if (texturaCirculo != null) Destroy(texturaCirculo);
        if (anillo != null) Destroy(anillo);
        if (texturaAnillo != null) Destroy(texturaAnillo);
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
        // Abre en la pestana que tiene algo para cobrar, si la otra no tiene.
        int otra = 1 - pestana;
        if (PorCobrarEn(pestana) == 0 && PorCobrarEn(otra) > 0) pestana = otra;
        MostrarPestana(pestana);
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

    private static int PorCobrarEn(int cual)
    {
        return cual == 0 ? Bestiario.PorCobrarEntre(0, Bestiario.Comunes) : Bestiario.PorCobrarEntre(Bestiario.Comunes, Bestiario.Tipos.Length);
    }

    // Publico para las fotos (FotosDelBestiario).
    public void MostrarPestana(int cual)
    {
        pestana = cual;
        for (int i = 0; i < 2; i++)
        {
            if (grupos[i] != null) grupos[i].gameObject.SetActive(i == cual);
            PintarPestana(pestanas[i], i == cual);
        }
    }

    // La elegida, rellena de celeste; la otra, de vidrio, como los botones secundarios.
    private void PintarPestana(Button boton, bool elegida)
    {
        if (boton == null) return;
        Color color = elegida ? ConstructorUI.Celeste : ColorDeVidrio;
        var fondo = boton.transform.Find("Visual/Fondo");
        if (fondo != null) fondo.GetComponent<Image>().color = color;
        var texto = boton.transform.Find("Visual/Texto");
        if (texto != null) texto.GetComponent<TMP_Text>().color = elegida ? CelesteTexto : Color.white;
        ConstructorUI.PintarHalo(boton, color);
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

    // El trofeo de arriba a la derecha: hasta el nivel 5 no se ve (Desbloqueo).
    private Button botonEsquina;

    private void Update()
    {
        float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
        if (botonEsquina != null && botonEsquina.gameObject.activeSelf != Desbloqueo.Todo)
            botonEsquina.gameObject.SetActive(Desbloqueo.Todo);
        relojInsignia += dt;
        // El numero se escribe solo cuando cambia, como en las misiones y los logros:
        // escribirlo en cada cuadro armaba una cadena por cuadro. Latir sin texto sigue
        // prendiendo, apagando y latiendo.
        int cuenta = Bestiario.PorCobrar;
        ConstructorUI.Latir(insignia, cuenta != cuentaEscrita ? numeroInsignia : null, cuenta, relojInsignia);
        cuentaEscrita = cuenta;
        if (!Abierta) return;

        for (int i = 0; i < 2; i++)
        {
            int deLaPestana = PorCobrarEn(i);
            ConstructorUI.Latir(insigniasDePestana[i], deLaPestana != cuentasDePestana[i] ? numerosDePestana[i] : null, deLaPestana, relojInsignia);
            cuentasDePestana[i] = deLaPestana;
        }

        reloj += dt;
        ventana.localScale = Vector3.one * (EscalaQueEntra() * CurvasUI.SalidaAtras(Mathf.Clamp01(reloj / 0.45f)));
        if (Progreso.Revision != revisionVista) Refrescar();

        float latido = 1f + 0.18f * Mathf.Abs(Mathf.Sin(reloj * 4f));
        foreach (var tarjeta in tarjetas)
        {
            if (!tarjeta.raiz.gameObject.activeInHierarchy) continue;
            if (tarjeta.golpe >= 0f) tarjeta.golpe += dt;
            float salto = tarjeta.golpe >= 0f ? 0.08f * CurvasUI.Campana(Mathf.Clamp01(tarjeta.golpe / 0.4f)) : 0f;
            tarjeta.raiz.localScale = Vector3.one * (1f + salto);

            // Las estrellas ganadas sin cobrar laten.
            int cobradas = Bestiario.Cobradas(tarjeta.tipo), alcanzadas = Bestiario.Alcanzadas(tarjeta.tipo);
            for (int e = 0; e < tarjeta.estrellas.Length; e++)
                tarjeta.estrellas[e].rectTransform.localScale = Vector3.one * (e >= cobradas && e < alcanzadas ? latido : 1f);
        }
    }

    // Si la ventana no entra en la pantalla (en 21:9 el menu mide 823 de alto), se achica, como las otras del menu.
    // Publico para las fotos.
    public float EscalaQueEntra()
    {
        float alto = ((RectTransform)panel.transform).rect.height;
        if (alto <= 0f) return 1f;
        float lugar = alto * 0.5f - Mathf.Abs(ventana.anchoredPosition.y) - 10f;
        float mitad = ventana.rect.height * 0.5f + 20f;
        return Mathf.Clamp(lugar / mitad, 0.5f, 1f);
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

            // El que nunca se mato sale tapado: la silueta, "???", sin la cuenta ni la barra, y la oleada en que sale.
            bool visto = muertes > 0;
            Color acento = Acentos[Mathf.Min(tarjeta.indice, Acentos.Length - 1)];
            var foto = visto ? tarjeta.fotoDelRetrato : tarjeta.fotoDeLaSilueta;
            tarjeta.retrato.sprite = foto;
            tarjeta.retrato.enabled = foto != null;
            tarjeta.retrato.color = visto ? Color.white : ColorDeLaSilueta;
            tarjeta.anillo.color = visto ? acento : new Color(acento.r, acento.g, acento.b, AlfaDelAnilloTapado);
            tarjeta.nombre.text = visto ? Bestiario.Nombre(tipo) : Textos.De("bestiario_desconocido");
            tarjeta.cuenta.text = visto ? FormatoNumeros.Compacto(muertes) : "";
            tarjeta.barra.SetActive(visto);
            for (int e = 0; e < tarjeta.estrellas.Length; e++)
            {
                var color = ColorDeEstrellaApagada;
                if (e < cobradas) color = colorEstrella;
                else if (e < alcanzadas) color = Color.Lerp(colorEstrella, Color.white, 0.35f);
                tarjeta.estrellas[e].color = color;
            }

            int siguiente = Bestiario.Siguiente(tipo);
            if (!visto)
                tarjeta.siguiente.text = Textos.Formato("bestiario_oleada", Bestiario.OleadaDeAparicion(tipo));
            else if (siguiente > 0)
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

        ventana = ConstructorUI.Rect(rtPanel, "Ventana", new Vector2(0f, 10f), new Vector2(1180f, AltoDeLaVentana));
        var fondo = ventana.gameObject.AddComponent<Image>();
        ConstructorUI.VentanaNeon(ventana, fondo, pildora);
        fondo.color = ColorDeVentana;

        var titulo = ConstructorUI.Texto(ventana, "Titulo", Textos.De("bestiario_titulo"), 80f, colorTitulo,
                                         new Vector2(0f, 290f), new Vector2(1100f, 100f), fuente);
        if (materialContorno != null) titulo.fontSharedMaterial = materialContorno;

        // Las pestanas van donde iba la bajada.
        string[] nombres = { Textos.De("bestiario_comunes"), Textos.De("bestiario_especiales") };
        for (int i = 0; i < 2; i++)
        {
            int cual = i;
            pestanas[i] = ConstructorUI.Boton(ventana, i == 0 ? "PestanaComunes" : "PestanaEspeciales", new Vector2(i == 0 ? -175f : 175f, 216f),
                                              new Vector2(320f, 64f), ColorDeVidrio, Color.white, null, nombres[i], 36f, fuente, pildora, sonidoClick);
            pestanas[i].onClick.AddListener(() => MostrarPestana(cual));
            insigniasDePestana[i] = ConstructorUI.Insignia(botonMejoras, (RectTransform)pestanas[i].transform, out numerosDePestana[i]);
            cuentasDePestana[i] = -1;
            grupos[i] = ConstructorUI.Rect(ventana, i == 0 ? "Comunes" : "Especiales", Vector2.zero, new Vector2(1180f, AltoDeLaVentana));
        }

        for (int i = 0; i < tarjetas.Length; i++)
        {
            bool especial = i >= Bestiario.Comunes;
            int lugar = especial ? i - Bestiario.Comunes : i;
            int cuantos = especial ? tarjetas.Length - Bestiario.Comunes : Bestiario.Comunes;
            tarjetas[i] = ArmarTarjeta(i, grupos[especial ? 1 : 0], (lugar - (cuantos - 1) * 0.5f) * 216f);
        }
        MostrarPestana(pestana);

        var volver = ConstructorUI.Boton(ventana, "Volver", new Vector2(0f, -290f), new Vector2(340f, 100f), ColorDeVidrio,
                                         Color.white, iconoAtras, Textos.De("comun_volver"), 46f, fuente, pildora, sonidoClick);
        volver.onClick.AddListener(Cerrar);
    }

    private Tarjeta ArmarTarjeta(int indice, RectTransform grupo, float x)
    {
        var tarjeta = new Tarjeta { tipo = Bestiario.Tipos[indice], indice = indice };
        tarjeta.raiz = ConstructorUI.Rect(grupo, tarjeta.tipo, new Vector2(x, -18f), new Vector2(200f, 390f));
        var fondo = tarjeta.raiz.gameObject.AddComponent<Image>();
        ConstructorUI.Redondear(fondo, pildora, 3f);
        fondo.color = ColorDeTarjeta;

        // El retrato: el disco oscuro, la foto (ya recortada en circulo) y el anillo del color del zombi encima.
        var posicion = new Vector2(0f, 128f);
        ConstructorUI.Imagen(tarjeta.raiz, "Disco", posicion, new Vector2(LadoDelRetrato, LadoDelRetrato), circulo, FondoDelRetrato);
        float adentro = LadoDelRetrato - GrosorDelAnillo * 2f;
        tarjeta.fotoDelRetrato = Resources.Load<Sprite>(RutaDelRetrato(tarjeta.tipo));
        tarjeta.fotoDeLaSilueta = Resources.Load<Sprite>(RutaDeLaSilueta(tarjeta.tipo));
        tarjeta.retrato = ConstructorUI.Imagen(tarjeta.raiz, "Retrato", posicion, new Vector2(adentro, adentro), tarjeta.fotoDelRetrato, Color.white);
        tarjeta.retrato.preserveAspect = true;
        tarjeta.anillo = ConstructorUI.Imagen(tarjeta.raiz, "Anillo", posicion, new Vector2(LadoDelRetrato, LadoDelRetrato), anillo,
                                              Acentos[Mathf.Min(indice, Acentos.Length - 1)]);

        tarjeta.nombre = ConstructorUI.Texto(tarjeta.raiz, "Nombre", Bestiario.Nombre(tarjeta.tipo), 34f, ColorDeTexto,
                                             new Vector2(0f, 60f), new Vector2(190f, 44f), fuente);
        tarjeta.cuenta = ConstructorUI.Texto(tarjeta.raiz, "Cuenta", "", 48f, ColorDeTexto, new Vector2(0f, 20f), new Vector2(190f, 56f), fuente);

        tarjeta.estrellas = new Image[Bestiario.Estrellas];
        for (int e = 0; e < tarjeta.estrellas.Length; e++)
            tarjeta.estrellas[e] = ConstructorUI.Imagen(tarjeta.raiz, "Estrella" + e, new Vector2((e - 1) * 54f, -34f),
                                                        new Vector2(46f, 46f), iconoEstrella, ColorDeEstrellaApagada);

        tarjeta.siguiente = ConstructorUI.Texto(tarjeta.raiz, "Siguiente", "", 28f, ColorDeTexto,
                                                new Vector2(0f, -78f), new Vector2(190f, 36f), fuente);
        tarjeta.relleno = ConstructorUI.Barra(tarjeta.raiz, "Barra", new Vector2(0f, -104f), new Vector2(160f, 10f), pildora,
                                              Tema.Elegir(new Color(0f, 0f, 0f, 0.12f), RolDeTema.Surco), colorBarra);
        tarjeta.barra = tarjeta.relleno.parent.gameObject;

        var cobrar = ConstructorUI.Boton(tarjeta.raiz, "Cobrar", new Vector2(0f, -152f), new Vector2(170f, 70f),
                                         ConstructorUI.Verde, ConstructorUI.VerdeTexto, null,
                                         "", 38f, fuente, pildora, sonidoClick);
        cobrar.onClick.AddListener(() => Cobrar(indice));
        cobrar.GetComponent<BotonJugoso>().respirar = true;
        tarjeta.cobrar = cobrar.gameObject;
        tarjeta.textoCobrar = cobrar.transform.Find("Visual/Texto").GetComponent<TMP_Text>();
        return tarjeta;
    }

    // Donde estan los retratos (los saca RetratosDelBestiario), en Resources.
    public static string RutaDelRetrato(string tipo)
    {
        return "Retratos/Retrato_" + tipo;
    }

    public static string RutaDeLaSilueta(string tipo)
    {
        return "Retratos/Silueta_" + tipo;
    }
}
