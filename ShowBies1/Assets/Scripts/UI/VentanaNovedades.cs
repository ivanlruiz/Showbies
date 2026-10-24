using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// "¿QUE HAY DE NUEVO?" (1.7.1; pedido de Ivan, que cada actualizacion llegue a la gente, y diseño de Argon, 24/10): la
// primera vez que alguien que ya jugaba entra al menu con una version nueva, una ventana de carbon neon con cuatro
// novedades (sin scroll: "apilarle carteles al que abre la app es el error clasico"), DISCORD y ¡A JUGAR!. Al que
// instala de cero no le sale nunca (todo el juego es nuevo): se anota la version en silencio. El que se salteo
// versiones ve la ultima, que junta lo grande de las anteriores. Se vuelve a abrir desde NOVEDADES en OPCIONES.
//
// Va primera en el menu: la recompensa diaria, el cartel de los premios cobrados solos, la tienda que se abre sola y el
// aviso de actualizacion esperan mientras esta Ocupada; y en la visita en que salio no se piden ni la reseña ni el
// permiso de las notificaciones (PedidoDeResena): "como mucho, novedades y la diaria, y a jugar".
//
// No esta en la escena: se instala al cargar el menu en el canvas "Main Menu", como el selector de capitulos, y toma del
// bestiario la fuente y la pildora, de las misiones el cofre y el sonido, y de Discord su icono. Las novedades son las
// filas novedades_1 a novedades_4 de la tabla: se cambian ahi con cada version.
public class VentanaNovedades : MonoBehaviour
{
    public const string ClaveVersionVista = "NovedadesVistas";
    public static readonly Vector2 TamVentana = new Vector2(980f, 620f);
    public static readonly Vector2 TamFila = new Vector2(880f, 88f);
    public const float EntreFilas = 10f;
    public const float LadoDelDisco = 68f;
    public const float DuracionEntrada = 0.28f;
    public const float CascadaDeFilas = 0.05f;
    public const float SinCerrarAlAbrir = 0.8f;   // tocar afuera recien despues: el dedo que venia de otra cosa no la cierra

    // Las cuatro filas: su texto (los ids, enteros para la prueba de idiomas), su acento y su dibujo (un retrato del
    // bestiario o, la ultima, el cofre).
    private static readonly string[] Titulos = { "novedades_1_titulo", "novedades_2_titulo", "novedades_3_titulo", "novedades_4_titulo" };
    private static readonly string[] Descripciones = { "novedades_1_texto", "novedades_2_texto", "novedades_3_texto", "novedades_4_texto" };
    private static readonly Color[] Acentos =
    {
        new Color(0.835f, 0f, 0.976f),    // #D500F9, los capitulos
        new Color(0f, 1f, 0.4f),          // #00FF66, los zombis con habilidad
        new Color(1f, 0.769f, 0f),        // #FFC400, el bate y las estrellas
        new Color(0f, 0.898f, 1f),        // #00E5FF, los avisos
    };
    private static readonly string[] Retratos = { Bestiario.Escupidor, Bestiario.Blindado, Bestiario.Hinchado, null };
    private static readonly Color AmarilloTitulo = new Color(1f, 0.839f, 0f);   // #FFD600
    private static readonly Color BlancoHielo = new Color(0.886f, 0.918f, 0.957f);   // #E2EAF4
    private static readonly Color FondoFila = new Color(0.086f, 0.11f, 0.157f, 1f);   // #161C28
    private static readonly Color BordeFila = new Color(0.149f, 0.188f, 0.259f, 1f);  // #263042
    private static readonly Color VioletaDiscord = new Color(0.345f, 0.396f, 0.949f); // #5865F2

    public static bool Abierta { get; private set; }
    // Desde que se carga el menu con novedades para mostrar hasta que se cierran: lo demas espera.
    public static bool Ocupada { get; private set; }
    // Si salio en esta visita al menu: entonces no se piden la reseña ni el permiso de las notificaciones.
    public static bool SalioEnEstaVisita { get; private set; }
    private static VentanaNovedades instancia;

    private VentanaBestiario bestiario;
    private VentanaMisiones misiones;
    private BotonDiscord discord;
    private GameObject panel;
    private Image fondo;
    private RectTransform ventana;
    private RectTransform[] filas;
    private CanvasGroup[] grupos;
    private float reloj = -1f;
    private int idiomaArmado = -1;
    private bool abrirAlEmpezar;
    private Texture2D texturaCirculo, texturaAnillo;
    private Sprite circulo, anillo;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetearEstadoCompartido()
    {
        Abierta = false;
        Ocupada = false;
        SalioEnEstaVisita = false;
        instancia = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Engancharse()
    {
        SceneManager.sceneLoaded -= AlCargar;
        SceneManager.sceneLoaded += AlCargar;
    }

    // La cuenta, sin PlayerPrefs, para la prueba: sale si ya jugaba y la version vista no es esta.
    public static bool HayQueMostrar(string vista, string actual, bool yaJugaba)
    {
        return yaJugaba && vista != actual;
    }

    private static void AlCargar(Scene escena, LoadSceneMode modo)
    {
        if (escena.buildIndex != TiendaMejoras.EscenaMenu) return;
        SalioEnEstaVisita = false;
        var bestiario = FindAnyObjectByType<VentanaBestiario>(FindObjectsInactive.Include);
        if (bestiario == null) return;
        var go = new GameObject("VentanaNovedades", typeof(RectTransform));
        go.transform.SetParent(bestiario.transform, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var ventanaNueva = go.AddComponent<VentanaNovedades>();
        ventanaNueva.bestiario = bestiario;
        ventanaNueva.misiones = FindAnyObjectByType<VentanaMisiones>(FindObjectsInactive.Include);
        ventanaNueva.discord = FindAnyObjectByType<BotonDiscord>(FindObjectsInactive.Include);

        // El que nunca termino una partida no la ve: todo el juego es nuevo. Se anota la version y listo.
        string actual = Application.version;
        string vista = PlayerPrefs.GetString(ClaveVersionVista, "");
        if (PrimeraVez.NoTerminoPartidas)
        {
            if (vista != actual) { PlayerPrefs.SetString(ClaveVersionVista, actual); PlayerPrefs.Save(); }
            return;
        }
        if (!HayQueMostrar(vista, actual, true)) return;
        Ocupada = true;
        ventanaNueva.abrirAlEmpezar = true;
    }

    // NOVEDADES en OPCIONES.
    public static void AbrirLaUltima()
    {
        if (instancia != null) instancia.Abrir();
    }

    public static void CerrarLaAbierta()
    {
        if (instancia != null) instancia.Cerrar();
    }

    private void Awake()
    {
        instancia = this;
    }

    private void Start()
    {
        if (!abrirAlEmpezar) return;
        abrirAlEmpezar = false;
        PlayerPrefs.SetString(ClaveVersionVista, Application.version);
        PlayerPrefs.Save();
        SalioEnEstaVisita = true;
        Analitica.NovedadesVistas(Application.version);
        Abrir();
    }

    private void OnDestroy()
    {
        if (instancia == this) instancia = null;
        if (panel != null) Abierta = false;
        Ocupada = false;
        foreach (var s in new[] { circulo, anillo }) if (s != null) Destroy(s);
        foreach (var t in new[] { texturaCirculo, texturaAnillo }) if (t != null) Destroy(t);
    }

    public void Abrir()
    {
        if (panel != null && Idioma.Revision != idiomaArmado)
        {
            Destroy(panel);
            panel = null;
        }
        if (panel == null) Armar();
        panel.SetActive(true);
        transform.SetAsLastSibling();
        Abierta = true;
        reloj = 0f;
        Animar(0f);
        if (misiones != null && misiones.sonidoFestejo != null) Sonidos.TocarUI(misiones.sonidoFestejo, 0.8f);
    }

    public void Cerrar()
    {
        if (panel != null) panel.SetActive(false);
        Abierta = false;
        Ocupada = false;
        reloj = -1f;
    }

    private void TocarAfuera()
    {
        if (reloj >= SinCerrarAlAbrir) Cerrar();
    }

    private void AbrirDiscord()
    {
        Application.OpenURL(BotonDiscord.Invitacion);
        Cerrar();
    }

    private void Update()
    {
        if (!Abierta || ventana == null) return;
        reloj += Mathf.Min(Time.unscaledDeltaTime, 0.1f);
        Animar(reloj);
    }

    // La entrada: el fondo oscurece en 0,15 s, la ventana va de 0,8 a 1 con rebote y las filas entran en cascada desde la
    // izquierda.
    private void Animar(float t)
    {
        if (fondo != null) fondo.color = new Color(0f, 0f, 0f, 0.55f * Mathf.Clamp01(t / 0.15f));
        float s = CurvasUI.SalidaAtras(Mathf.Clamp01(t / DuracionEntrada));
        ventana.localScale = Vector3.one * (EscalaQueEntra() * Mathf.LerpUnclamped(0.8f, 1f, s));
        for (int i = 0; i < filas.Length; i++)
        {
            float f = Mathf.Clamp01((t - 0.1f - i * CascadaDeFilas) / 0.2f);
            float suave = 1f - (1f - f) * (1f - f);
            filas[i].anchoredPosition = new Vector2(-30f * (1f - suave), filas[i].anchoredPosition.y);
            grupos[i].alpha = suave;
        }
    }

    public float EscalaQueEntra()
    {
        float alto = ((RectTransform)panel.transform).rect.height;
        if (alto <= 0f) return 1f;
        float lugar = alto * 0.5f - Mathf.Abs(ventana.anchoredPosition.y) - 10f;
        float mitad = ventana.rect.height * 0.5f + 20f;
        return Mathf.Clamp(lugar / mitad, 0.5f, 1f);
    }

    private void Armar()
    {
        idiomaArmado = Idioma.Revision;
        if (texturaCirculo == null)
        {
            texturaCirculo = TexturasUI.Circulo(128);
            circulo = Sprite.Create(texturaCirculo, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f));
            texturaAnillo = TexturasUI.Anillo(128, 3f / (LadoDelDisco * 0.5f));
            anillo = Sprite.Create(texturaAnillo, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f));
        }
        var fuente = bestiario != null ? bestiario.fuente : null;
        var contorno = bestiario != null ? bestiario.materialContorno : null;
        var pildora = bestiario != null ? bestiario.pildora : null;
        var click = bestiario != null ? bestiario.sonidoClick : null;

        var rtPanel = ConstructorUI.Estirar((RectTransform)transform, "Novedades");
        panel = rtPanel.gameObject;
        fondo = panel.AddComponent<Image>();
        fondo.color = new Color(0f, 0f, 0f, 0f);
        // Tocar afuera (despues de 0,8 s) es lo mismo que ¡A JUGAR!.
        var afuera = panel.AddComponent<Button>();
        afuera.transition = Selectable.Transition.None;
        afuera.onClick.AddListener(TocarAfuera);

        ventana = ConstructorUI.Rect(rtPanel, "Ventana", new Vector2(0f, 6f), TamVentana);
        var imgVentana = ventana.gameObject.AddComponent<Image>();
        ConstructorUI.VentanaNeon(ventana, imgVentana, pildora);
        imgVentana.color = Tema.Elegir(Color.white, RolDeTema.Panel);
        // Un toque adentro de la ventana se queda ahi: si no, subiria hasta el boton de afuera y la cerraria.
        ventana.gameObject.AddComponent<Button>().transition = Selectable.Transition.None;

        var titulo = ConstructorUI.Texto(ventana, "Titulo", Textos.De("novedades_titulo"), 56f, ConstructorUI.TituloNeon,
                                         new Vector2(0f, 258f), new Vector2(900f, 70f), fuente);
        var neon = MaterialDelTitulo();
        if (neon != null) titulo.fontSharedMaterial = neon;
        else if (contorno != null) titulo.fontSharedMaterial = contorno;

        // La version, en una pildora celeste debajo del titulo.
        var pastilla = ConstructorUI.Rect(ventana, "Version", new Vector2(0f, 212f), new Vector2(160f, 34f));
        var imgPastilla = pastilla.gameObject.AddComponent<Image>();
        imgPastilla.sprite = pildora;
        ConstructorUI.RedondearPildora(imgPastilla);
        imgPastilla.color = ConstructorUI.Celeste;
        imgPastilla.raycastTarget = false;
        ConstructorUI.Texto(pastilla, "Texto", Textos.Formato("novedades_version", Application.version), 22f,
                            new Color(0.043f, 0.024f, 0.094f), Vector2.zero, new Vector2(156f, 34f), fuente);

        filas = new RectTransform[Titulos.Length];
        grupos = new CanvasGroup[Titulos.Length];
        float y = 188f - TamFila.y * 0.5f;
        for (int i = 0; i < Titulos.Length; i++)
        {
            ArmarFila(i, y, fuente);
            y -= TamFila.y + EntreFilas;
        }

        var botonDiscord = ConstructorUI.Boton(ventana, "Discord", new Vector2(-205f, -250f), new Vector2(280f, 68f), VioletaDiscord,
                                               Color.white, discord != null ? discord.icono : null, Textos.De("novedades_discord"),
                                               32f, fuente, pildora, click);
        botonDiscord.onClick.AddListener(AbrirDiscord);
        var jugar = ConstructorUI.Boton(ventana, "Jugar", new Vector2(155f, -250f), new Vector2(380f, 68f), ConstructorUI.Verde,
                                        ConstructorUI.VerdeTexto, null, Textos.De("novedades_jugar"), 34f, fuente, pildora, click);
        jugar.onClick.AddListener(Cerrar);
        jugar.GetComponent<BotonJugoso>().respirar = true;
        panel.SetActive(false);
    }

    private void ArmarFila(int i, float y, TMP_FontAsset fuente)
    {
        var fila = ConstructorUI.Rect(ventana, "Fila" + (i + 1), new Vector2(0f, y), TamFila);
        filas[i] = fila;
        grupos[i] = fila.gameObject.AddComponent<CanvasGroup>();
        var pildora = bestiario != null ? bestiario.pildora : null;
        // El borde fino: la fila sobre un rectangulo 2 u mas grande del color del borde.
        var borde = fila.gameObject.AddComponent<Image>();
        ConstructorUI.Redondear(borde, pildora, 8f);   // puntas de 16 u (127 / 8)
        borde.color = BordeFila;
        borde.raycastTarget = false;
        var adentro = ConstructorUI.Rect(fila, "Fondo", Vector2.zero, TamFila - new Vector2(4f, 4f));
        var imgAdentro = adentro.gameObject.AddComponent<Image>();
        ConstructorUI.Redondear(imgAdentro, pildora, 9f);
        imgAdentro.color = FondoFila;
        imgAdentro.raycastTarget = false;

        // El disco con el anillo del acento y el dibujo adentro.
        var lugar = new Vector2(-TamFila.x * 0.5f + 16f + LadoDelDisco * 0.5f, 0f);
        ConstructorUI.Imagen(fila, "Disco", lugar, new Vector2(LadoDelDisco, LadoDelDisco), circulo, VentanaBestiario.FondoDelRetrato);
        Sprite dibujo = Retratos[i] != null ? Resources.Load<Sprite>(VentanaBestiario.RutaDelRetrato(Retratos[i]))
                      : (misiones != null ? misiones.iconoCofre : null);
        float lado = Retratos[i] != null ? LadoDelDisco - 6f : LadoDelDisco * 0.56f;
        var imgDibujo = ConstructorUI.Imagen(fila, "Dibujo", lugar, new Vector2(lado, lado), dibujo,
                                             Retratos[i] != null ? Color.white : Acentos[i]);
        imgDibujo.enabled = dibujo != null;
        ConstructorUI.Imagen(fila, "Anillo", lugar, new Vector2(LadoDelDisco, LadoDelDisco), anillo, Acentos[i]);

        // Las dos lineas, a la izquierda: el titulo en amarillo y la descripcion en blanco.
        float x0 = lugar.x + LadoDelDisco * 0.5f + 18f;
        float ancho = TamFila.x * 0.5f - x0 - 16f;
        var titulo = ConstructorUI.Texto(fila, "Titulo", Textos.De(Titulos[i]), 28f, AmarilloTitulo,
                                         new Vector2(x0 + ancho * 0.5f, 17f), new Vector2(ancho, 36f), fuente);
        titulo.alignment = TextAlignmentOptions.MidlineLeft;
        var texto = ConstructorUI.Texto(fila, "Texto", Textos.De(Descripciones[i]), 22f, BlancoHielo,
                                        new Vector2(x0 + ancho * 0.5f, -18f), new Vector2(ancho, 32f), fuente);
        texto.alignment = TextAlignmentOptions.MidlineLeft;
        texto.enableAutoSizing = true;
        texto.fontSizeMin = 16f;
        texto.fontSizeMax = 22f;
    }

    // El halo de neon de los titulos (Bangers SDF - Neon), de algun titulo del menu que ya lo tenga (el del idioma).
    private static Material MaterialDelTitulo()
    {
        foreach (var t in FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var material = t.fontSharedMaterial;
            if (material != null && material.name.StartsWith("Bangers SDF - Neon") && !material.name.Contains("HUD")) return material;
        }
        return null;
    }
}
