using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// El boton HALLOWEEN del menu y la ventana que abre (EventoHalloween): los caramelos juntados,
// cuantos dias quedan y la fila de cinco hitos, cada uno con lo que pide y lo que paga (las
// monedas, o el sombrero de calabaza en el ultimo), COBRAR en el primero alcanzado sin
// cobrar y la tilde en los cobrados.
//
// El boton va abajo a la izquierda del panel principal, en espejo con PLAY (a la misma
// distancia del borde, que sigue el borde real de la pantalla en 20:9), naranja y con la
// insignia de MEJORAS: los hitos por cobrar, o "!" mientras no se abrio la ventana en esta
// edicion. Nada de esto esta en la escena: lo instala InstaladorHalloween durante el evento,
// en la raiz del canvas "Main Menu", y toma de VentanaMisiones la fuente, la pildora, los
// iconos y los sonidos. El atras de Android la cierra (BotonAtrasMenu).
public class VentanaHalloween : MonoBehaviour
{
    public Button botonMejoras;
    public TMP_FontAsset fuente;
    public Material materialContorno;
    public Sprite pildora;
    public Sprite tilde;
    public Sprite iconoAtras;
    public AudioClip nota;
    public AudioClip sonidoFestejo;
    public AudioClip sonidoClick;
    public RectTransform panelPrincipal;     // el MainMenu, donde va el boton

    public static readonly Color Naranja = new Color(1f, 0.55f, 0.1f, 1f);
    public static readonly Color NaranjaTexto = new Color(0.17f, 0.07f, 0f, 1f);
    public static readonly Color Violeta = new Color(0.72f, 0.36f, 1f, 1f);
    public static readonly Color Dorado = new Color(1f, 0.76f, 0.12f, 1f);

    // El boton, como PLAY pero del otro lado y mas chico: PLAY esta a 110 del borde y 90 del piso.
    public const float DesdeElBorde = 110f;
    public const float DesdeElPiso = 90f;
    public static readonly Vector2 TamanioDelBoton = new Vector2(440f, 140f);

    private static readonly int[] SemitonosFestejo = { -12, -8, -5, 0, 4, 7, 12 };

    public static bool Abierta { get; private set; }
    private static VentanaHalloween instancia;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reiniciar()
    {
        Abierta = false;
        instancia = null;
    }

    private class Hito
    {
        public RectTransform raiz;
        public Image circulo;
        public Image icono;
        public TMP_Text umbral;
        public TMP_Text premio;
        public GameObject cobrar;
        public Image tilde;
        public float golpe = -1f;
    }

    private GameObject panel;
    private RectTransform ventana;
    private TMP_Text textoCaramelos;
    private TMP_Text textoQuedan;
    private TMP_Text textoSombrero;
    private RectTransform relleno;
    private readonly Hito[] hitos = new Hito[EventoHalloween.Hitos];
    private Sprite circuloSprite;
    private Texture2D circuloTextura;
    private Sprite iconoCalabaza;
    private Sprite iconoCaramelo;
    private RectTransform insignia;
    private TMP_Text numeroInsignia;
    private int revisionVista = -1;
    private int idiomaArmado = -1;
    private float reloj = -1f;
    private float relojInsignia;
    private float relojDias;
    private float golpeCaramelos = -1f;

    // Lo instala InstaladorHalloween en el menu: al lado de VentanaMisiones, con sus piezas.
    public static VentanaHalloween Instalar(Scene escena)
    {
        VentanaMisiones misiones = null;
        BotonAtrasMenu atras = null;
        foreach (var raiz in escena.GetRootGameObjects())
        {
            if (misiones == null) misiones = raiz.GetComponentInChildren<VentanaMisiones>(true);
            if (atras == null) atras = raiz.GetComponentInChildren<BotonAtrasMenu>(true);
        }
        if (misiones == null || atras == null || atras.menuPrincipal == null) return null;

        var ventana = misiones.GetComponent<VentanaHalloween>();
        if (ventana != null) return ventana;
        ventana = misiones.gameObject.AddComponent<VentanaHalloween>();
        ventana.botonMejoras = misiones.botonMejoras;
        ventana.fuente = misiones.fuente;
        ventana.materialContorno = misiones.materialContorno;
        ventana.pildora = misiones.pildora;
        ventana.tilde = misiones.tilde;
        ventana.iconoAtras = misiones.iconoAtras;
        ventana.nota = misiones.nota;
        ventana.sonidoFestejo = misiones.sonidoFestejo;
        ventana.sonidoClick = misiones.sonidoClick;
        ventana.panelPrincipal = atras.menuPrincipal.transform as RectTransform;
        return ventana;
    }

    // Para el atras de Android.
    public static void CerrarLaAbierta()
    {
        if (instancia != null) instancia.Cerrar();
    }

    private void Start()
    {
        instancia = this;
        circuloTextura = TexturasUI.Circulo(64);
        circuloSprite = Sprite.Create(circuloTextura, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));
        iconoCalabaza = Resources.Load<Sprite>("IconoCalabaza");
        iconoCaramelo = Resources.Load<Sprite>("IconoCaramelo");
        CrearBoton();
        Armar();
        panel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (instancia == this) { instancia = null; Abierta = false; }
        if (circuloSprite != null) Destroy(circuloSprite);
        if (circuloTextura != null) Destroy(circuloTextura);
    }

    // --- El boton del menu ---------------------------------------------------------

    private void CrearBoton()
    {
        if (panelPrincipal == null) return;
        var boton = ConstructorUI.Boton(panelPrincipal, "BotonHalloween", Vector2.zero, TamanioDelBoton, Naranja, NaranjaTexto,
                                        iconoCalabaza, Textos.De("halloween_boton"), 54f, fuente, pildora, sonidoClick);
        var rt = (RectTransform)boton.transform;
        rt.anchorMin = rt.anchorMax = Vector2.zero;
        rt.pivot = Vector2.zero;
        rt.anchoredPosition = new Vector2(DesdeElBorde, DesdeElPiso);
        boton.GetComponent<BotonJugoso>().respirar = true;
        // El texto cambia con el idioma sin volver a armar el boton.
        var texto = boton.transform.Find("Visual/Texto");
        if (texto != null)
        {
            texto.gameObject.SetActive(false);
            texto.gameObject.AddComponent<TextoTraducido>().id = "halloween_boton";
            texto.gameObject.SetActive(true);
        }
        boton.onClick.AddListener(Abrir);
        insignia = ConstructorUI.Insignia(botonMejoras, rt, out numeroInsignia);
    }

    private void RefrescarInsignia(float dt)
    {
        relojInsignia += dt;
        if (insignia == null) return;
        int porCobrar = EventoHalloween.PorCobrar;
        bool nuevo = !EventoHalloween.Visto;
        int cuenta = porCobrar > 0 ? porCobrar : nuevo ? 1 : 0;
        if (numeroInsignia != null && cuenta > 0)
        {
            string texto = porCobrar > 0 ? porCobrar.ToString() : "!";
            if (numeroInsignia.text != texto) numeroInsignia.text = texto;
        }
        ConstructorUI.Latir(insignia, null, cuenta, relojInsignia);
    }

    // --- La ventana ----------------------------------------------------------------

    public void Abrir()
    {
        if (panel == null) return;
        if (Idioma.Revision != idiomaArmado)
        {
            Destroy(panel);
            Armar();
        }
        EventoHalloween.Asegurar();
        EventoHalloween.MarcarVisto();
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

    private void Update()
    {
        float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
        RefrescarInsignia(dt);
        if (!Abierta) return;

        reloj += dt;
        ventana.localScale = Vector3.one * (EscalaQueEntra() * CurvasUI.SalidaAtras(Mathf.Clamp01(reloj / 0.45f)));
        if (Progreso.Revision != revisionVista) Refrescar();

        relojDias -= dt;
        if (relojDias <= 0f)
        {
            relojDias = 5f;
            textoQuedan.text = TextoQuedan();
        }

        if (golpeCaramelos >= 0f)
        {
            golpeCaramelos += dt;
            float s = 0.12f * CurvasUI.Campana(Mathf.Clamp01(golpeCaramelos / 0.4f));
            textoCaramelos.rectTransform.localScale = Vector3.one * (1f + s);
            if (golpeCaramelos >= 0.4f) golpeCaramelos = -1f;
        }
        foreach (var h in hitos)
        {
            if (h == null || h.golpe < 0f) continue;
            h.golpe += dt;
            float s = 0.3f * CurvasUI.Campana(Mathf.Clamp01(h.golpe / 0.5f));
            h.raiz.localScale = Vector3.one * (1f + s);
            if (h.golpe >= 0.5f) { h.golpe = -1f; h.raiz.localScale = Vector3.one; }
        }
    }

    private void Cobrar()
    {
        int hito = EventoHalloween.Cobrados;
        bool sombrero = EventoHalloween.EsElSombrero(hito);
        if (hito >= EventoHalloween.Alcanzados) return;
        EventoHalloween.CobrarSiguiente();

        for (int k = 0; k < SemitonosFestejo.Length; k++)
            Sonidos.Programar(nota, 0.05 * k, 0.8f, Sonidos.PitchDe(SemitonosFestejo[k]));
        Sonidos.Tocar(sonidoFestejo, 0.7f);
        if (sombrero)
        {
            // El premio grande: el arpegio doble, como el cofre, y un temblor.
            for (int k = 0; k < SemitonosFestejo.Length; k++)
                Sonidos.Programar(nota, 0.4 + 0.05 * k, 0.8f, Sonidos.PitchDe(SemitonosFestejo[k] + 12));
            CamaraJugador.Temblar(0.25f);
        }
        if (hito < hitos.Length && hitos[hito] != null) hitos[hito].golpe = 0f;
        Refrescar();
    }

    private void Refrescar()
    {
        revisionVista = Progreso.Revision;
        double caramelos = EventoHalloween.Caramelos;
        int alcanzados = EventoHalloween.Alcanzados;
        int cobrados = EventoHalloween.Cobrados;
        textoCaramelos.text = FormatoNumeros.Compacto(System.Math.Floor(caramelos + 1e-6));

        for (int i = 0; i < hitos.Length; i++)
        {
            var h = hitos[i];
            if (h == null) continue;
            bool alcanzado = i < alcanzados;
            bool cobrado = i < cobrados;
            bool sombrero = EventoHalloween.EsElSombrero(i) || (i == hitos.Length - 1 && cobrado && Progreso.Halloween.conSombrero);
            h.umbral.text = FormatoNumeros.Compacto(EventoHalloween.Umbral(i));
            h.premio.text = sombrero ? Textos.De("halloween_sombrero") : "+" + FormatoNumeros.Compacto(EventoHalloween.Premio(i));
            h.premio.color = sombrero ? Naranja : Dorado;
            h.circulo.color = cobrado ? ConstructorUI.Verde : alcanzado ? Naranja : Tema.Elegir(new Color(0f, 0f, 0f, 0.15f), RolDeTema.Hueco);
            h.icono.sprite = sombrero ? iconoCalabaza : circuloSprite;
            h.icono.color = cobrado ? ConstructorUI.VerdeTexto : alcanzado ? NaranjaTexto : sombrero ? Naranja : Dorado;
            h.icono.rectTransform.sizeDelta = sombrero ? new Vector2(64f, 64f) : new Vector2(40f, 40f);
            h.cobrar.SetActive(alcanzado && !cobrado && i == cobrados);
            h.tilde.enabled = cobrado && tilde != null;
        }

        // La barra: del principio al primer hito y de un hito al siguiente, cada tramo con su parte.
        float avance = 0f;
        double anterior = 0;
        for (int i = 0; i < hitos.Length; i++)
        {
            double umbral = EventoHalloween.Umbral(i);
            float tramo = umbral > anterior ? Mathf.Clamp01((float)((caramelos - anterior) / (umbral - anterior))) : 1f;
            avance += tramo;
            if (tramo < 1f) break;
            anterior = umbral;
        }
        relleno.anchorMax = new Vector2(Mathf.Clamp01(avance / hitos.Length), 1f);

        textoSombrero.text = EventoHalloween.TieneSombrero ? Textos.De("halloween_sombrero_puesto") : Textos.De("halloween_como");
    }

    private static string TextoQuedan()
    {
        int dias = EventoHalloween.DiasQueFaltan(EventoHalloween.Hoy());
        return dias <= 1 ? Textos.De("halloween_ultimo_dia") : Textos.Formato("halloween_quedan", dias);
    }

    // Como las otras ventanas del menu: en las pantallas mas largas se achica lo justo.
    private float EscalaQueEntra()
    {
        float alto = ((RectTransform)panel.transform).rect.height;
        if (alto <= 0f) return 1f;
        float lugar = alto * 0.5f - Mathf.Abs(ventana.anchoredPosition.y) - 10f;
        float mitad = ventana.rect.height * 0.5f + 40f;
        return Mathf.Clamp(lugar / mitad, 0.5f, 1f);
    }

    // La fila de hitos: la barra arranca en cero caramelos a la izquierda y cada hito esta a
    // un quinto de ella. Corrida a la izquierda para que el texto del ultimo entre en la
    // ventana (1180 de ancho).
    public const float AnchoDeLaFila = 1000f;
    public const float CentroDeLaFila = -30f;
    public static float XDelHito(int i)
    {
        return CentroDeLaFila - AnchoDeLaFila * 0.5f + AnchoDeLaFila * (i + 1) / EventoHalloween.Hitos;
    }

    private void Armar()
    {
        panel = new GameObject("VentanaHalloween", typeof(RectTransform));
        var rtPanel = (RectTransform)panel.transform;
        rtPanel.SetParent(transform, false);
        rtPanel.anchorMin = Vector2.zero;
        rtPanel.anchorMax = Vector2.one;
        rtPanel.offsetMin = rtPanel.offsetMax = Vector2.zero;
        panel.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.45f);   // tapa los toques del menu
        idiomaArmado = Idioma.Revision;

        ventana = ConstructorUI.Rect(rtPanel, "Ventana", new Vector2(0f, 10f), new Vector2(1180f, 820f));
        var fondo = ventana.gameObject.AddComponent<Image>();
        ConstructorUI.VentanaNeon(ventana, fondo, pildora);
        fondo.color = Tema.Elegir(new Color(1f, 0.96f, 0.86f, 1f), RolDeTema.Panel);
        Color texto = Tema.Elegir(new Color(0.16f, 0.14f, 0.2f, 1f), RolDeTema.Texto);
        Color suave = Tema.Elegir(new Color(0.35f, 0.32f, 0.4f, 1f), RolDeTema.TextoSuave);

        var titulo = Texto(ventana, "Titulo", Textos.De("halloween_titulo"), 84f, Naranja, new Vector2(0f, 330f), new Vector2(1100f, 100f));
        if (materialContorno != null) titulo.fontSharedMaterial = materialContorno;
        textoQuedan = Texto(ventana, "Quedan", TextoQuedan(), 36f, Violeta, new Vector2(0f, 262f), new Vector2(1100f, 50f));
        relojDias = 5f;

        // Los caramelos: el icono y el numero grande, centrados juntos.
        var fila = ConstructorUI.Rect(ventana, "Caramelos", new Vector2(0f, 186f), new Vector2(700f, 90f));
        var icono = ConstructorUI.Imagen(fila, "Icono", new Vector2(-130f, 0f), new Vector2(84f, 84f), iconoCaramelo, Naranja);
        textoCaramelos = Texto(fila, "Numero", "0", 76f, Color.white, new Vector2(10f, 0f), new Vector2(180f, 90f));
        var etiqueta = Texto(fila, "Etiqueta", Textos.De("halloween_caramelos"), 40f, texto, new Vector2(190f, -6f), new Vector2(260f, 60f));
        etiqueta.alignment = TextAlignmentOptions.Left;
        if (icono.sprite == null) icono.enabled = false;

        var como = Texto(ventana, "Como", Textos.De("halloween_como"), 32f, suave, new Vector2(0f, 126f), new Vector2(1100f, 50f));
        textoSombrero = como;

        // La barra y los hitos.
        float yFila = -28f;
        var barra = ConstructorUI.Rect(ventana, "Barra", new Vector2(CentroDeLaFila, yFila), new Vector2(AnchoDeLaFila, 22f));
        var imgBarra = barra.gameObject.AddComponent<Image>();
        imgBarra.sprite = pildora;
        ConstructorUI.RedondearPildora(imgBarra);
        imgBarra.color = Tema.Elegir(new Color(0f, 0f, 0f, 0.12f), RolDeTema.Surco);
        imgBarra.raycastTarget = false;
        relleno = ConstructorUI.Estirar(barra, "Relleno");
        relleno.anchorMax = new Vector2(0f, 1f);
        var imgRelleno = relleno.gameObject.AddComponent<Image>();
        imgRelleno.sprite = pildora;
        ConstructorUI.RedondearPildora(imgRelleno);
        imgRelleno.color = Naranja;
        imgRelleno.raycastTarget = false;

        for (int i = 0; i < hitos.Length; i++) hitos[i] = ArmarHito(i, yFila, texto);

        var volver = ConstructorUI.Boton(ventana, "Volver", new Vector2(0f, -340f), new Vector2(340f, 100f),
                                         Tema.Elegir(new Color(0.06f, 0.12f, 0.05f, 0.45f), RolDeTema.Vidrio), Color.white,
                                         iconoAtras, Textos.De("comun_volver"), 46f, fuente, pildora, sonidoClick);
        volver.onClick.AddListener(Cerrar);
    }

    private Hito ArmarHito(int i, float y, Color texto)
    {
        var h = new Hito();
        // La raiz es el punto de la fila: lo de arriba y lo de abajo cuelgan de ella.
        h.raiz = ConstructorUI.Rect(ventana, "Hito" + i, new Vector2(XDelHito(i), y), new Vector2(100f, 100f));
        h.circulo = h.raiz.gameObject.AddComponent<Image>();
        h.circulo.sprite = circuloSprite;
        h.circulo.raycastTarget = false;
        h.icono = ConstructorUI.Imagen(h.raiz, "Icono", Vector2.zero, new Vector2(40f, 40f), circuloSprite, Dorado);
        h.umbral = Texto(h.raiz, "Umbral", "", 34f, texto, new Vector2(0f, 82f), new Vector2(170f, 44f));
        h.premio = Texto(h.raiz, "Premio", "", 34f, Dorado, new Vector2(0f, -80f), new Vector2(176f, 44f));
        h.premio.enableAutoSizing = true;
        h.premio.fontSizeMax = 34f;
        h.premio.fontSizeMin = 20f;

        var cobrar = ConstructorUI.Boton(h.raiz, "Cobrar", new Vector2(0f, -150f), new Vector2(170f, 76f),
                                         ConstructorUI.Verde, ConstructorUI.VerdeTexto, null,
                                         Textos.De("mision_cobrar"), 34f, fuente, pildora, sonidoClick);
        cobrar.onClick.AddListener(Cobrar);
        cobrar.GetComponent<BotonJugoso>().respirar = true;
        h.cobrar = cobrar.gameObject;

        var rtTilde = ConstructorUI.Rect(h.raiz, "Tilde", new Vector2(0f, -150f), new Vector2(60f, 60f));
        h.tilde = rtTilde.gameObject.AddComponent<Image>();
        h.tilde.sprite = tilde;
        h.tilde.color = ConstructorUI.Verde;
        h.tilde.raycastTarget = false;
        h.tilde.enabled = false;
        return h;
    }

    private TMP_Text Texto(RectTransform padre, string nombre, string texto, float tamanio, Color color, Vector2 posicion, Vector2 caja)
    {
        return ConstructorUI.Texto(padre, nombre, texto, tamanio, color, posicion, caja, fuente);
    }
}
