using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// El selector de capitulos (1.7.0, de Argon, 21/10): OLEADAS, sin una partida para retomar y con la 10 completada, abre
// esta ventana en lugar de entrar a la 1. La ventana de carbon neon del bestiario (1180 x 660) con tres tarjetas de
// 320 x 420: LA PRADERA (verde, desde la 1, sin ventajas), EL CEMENTERIO (violeta, desde la 11, +6 cartas) y LA CIUDAD
// (rosa, desde la 21, +12 cartas), cada una con el retrato de un zombi de su capitulo (el caminante, el escupidor y el
// blindado, de los retratos del bestiario, a color aunque no se lo haya matado) y ¡JUGAR!. La bloqueada sale con la
// silueta en gris, un candado y la oleada que hay que completar, sin boton. La mas alta destrabada respira: es la
// preseleccionada. Elegir fija Capitulos.ParaEmpezar y carga las oleadas.
//
// No esta en la escena: se instala al cargar el menu, en el canvas "Main Menu" (donde vive el bestiario, de donde toma
// la fuente, la pildora y los sonidos), y se arma la primera vez que se abre. El atras de Android la cierra
// (BotonAtrasMenu).
public class SelectorDeCapitulos : MonoBehaviour
{
    // Los colores de cada capitulo (de Argon: verde, violeta y rosa, 22/10).
    public static readonly Color[] Colores =
    {
        new Color(0.463f, 1f, 0.012f),    // #76FF03, la pradera
        new Color(0.835f, 0f, 0.976f),    // #D500F9, el cementerio
        new Color(1f, 0.169f, 0.839f),    // #FF2BD6, la ciudad
    };
    // El zombi de cada uno, con su retrato del bestiario.
    public static readonly string[] Zombis = { Bestiario.Normal, Bestiario.Escupidor, Bestiario.Blindado };
    public static readonly string[] Escenarios = { "escenario_pradera", "escenario_cementerio", "escenario_ciudad" };
    public static readonly Color ColorBloqueado = new Color(0.27f, 0.302f, 0.369f);   // #454D5E
    public static readonly Vector2 TamVentana = new Vector2(1180f, 660f);
    public static readonly Vector2 TamTarjeta = new Vector2(320f, 420f);
    public const float Separacion = 36f;
    public const float LadoDelRetrato = 120f;

    public static bool Abierto { get; private set; }
    private static SelectorDeCapitulos instancia;

    private VentanaBestiario bestiario;
    private GameObject panel;
    private RectTransform ventana;
    private float reloj = -1f;
    private int idiomaArmado = -1;
    private int mejorArmada = -1;
    private Texture2D texturaCirculo, texturaAnillo;
    private Sprite circulo, anillo;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetearEstadoCompartido()
    {
        Abierto = false;
        instancia = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Engancharse()
    {
        SceneManager.sceneLoaded -= AlCargar;
        SceneManager.sceneLoaded += AlCargar;
    }

    private static void AlCargar(Scene escena, LoadSceneMode modo)
    {
        if (escena.buildIndex != TiendaMejoras.EscenaMenu) return;
        var bestiario = FindAnyObjectByType<VentanaBestiario>(FindObjectsInactive.Include);
        if (bestiario == null) return;
        var go = new GameObject("SelectorDeCapitulos", typeof(RectTransform));
        go.transform.SetParent(bestiario.transform, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var selector = go.AddComponent<SelectorDeCapitulos>();
        selector.bestiario = bestiario;
    }

    // Si OLEADAS tiene que abrirlo: sin partida para retomar y con mas de un capitulo destrabado.
    public static bool HayQueElegir
    {
        get { return !Progreso.HayOleadaParaRetomar && Capitulos.Desbloqueados(Progreso.MejorOleada) > 1; }
    }

    // OLEADAS (MainMenu.GameModes): devuelve si lo abrio; si no, quien lo pidio carga las oleadas como siempre.
    public static bool AbrirSiHaceFalta()
    {
        if (!HayQueElegir || instancia == null) return false;
        instancia.Abrir();
        return Abierto;
    }

    public static void CerrarElAbierto()
    {
        if (instancia != null) instancia.Cerrar();
    }

    private void Awake()
    {
        instancia = this;
    }

    private void OnDestroy()
    {
        if (instancia == this) instancia = null;
        if (panel != null) Abierto = false;
        foreach (var s in new[] { circulo, anillo }) if (s != null) Destroy(s);
        foreach (var t in new[] { texturaCirculo, texturaAnillo }) if (t != null) Destroy(t);
    }

    public void Abrir()
    {
        // Los textos se escriben al armarla, y lo destrabado tambien: si cambio algo, se vuelve a armar.
        if (panel != null && (Idioma.Revision != idiomaArmado || Progreso.MejorOleada != mejorArmada))
        {
            Destroy(panel);
            panel = null;
        }
        if (panel == null) Armar();
        panel.SetActive(true);
        // Encima de las otras ventanas del canvas, que se arman despues que esto.
        transform.SetAsLastSibling();
        Abierto = true;
        reloj = 0f;
        ventana.localScale = Vector3.zero;
    }

    public void Cerrar()
    {
        if (panel != null) panel.SetActive(false);
        Abierto = false;
        reloj = -1f;
    }

    private void Update()
    {
        if (!Abierto || ventana == null) return;
        reloj += Mathf.Min(Time.unscaledDeltaTime, 0.1f);
        ventana.localScale = Vector3.one * (EscalaQueEntra() * CurvasUI.SalidaAtras(Mathf.Clamp01(reloj / 0.45f)));
    }

    // Si no entra en la pantalla (en 21:9 el menu mide 823 de alto), se achica, como las otras ventanas del menu.
    public float EscalaQueEntra()
    {
        float alto = ((RectTransform)panel.transform).rect.height;
        if (alto <= 0f) return 1f;
        float lugar = alto * 0.5f - Mathf.Abs(ventana.anchoredPosition.y) - 10f;
        float mitad = ventana.rect.height * 0.5f + 20f;
        return Mathf.Clamp(lugar / mitad, 0.5f, 1f);
    }

    // Elegir: el capitulo para la partida nueva, y a las oleadas.
    public void Jugar(int capitulo)
    {
        Capitulos.ParaEmpezar = capitulo;
        Analitica.CapituloElegido(capitulo);
        Cerrar();
        SceneManager.LoadScene(TiendaMejoras.EscenaOleadas);
    }

    private void Armar()
    {
        idiomaArmado = Idioma.Revision;
        mejorArmada = Progreso.MejorOleada;
        if (texturaCirculo == null)
        {
            texturaCirculo = TexturasUI.Circulo(128);
            circulo = Sprite.Create(texturaCirculo, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f));
            texturaAnillo = TexturasUI.Anillo(128, 5f / (LadoDelRetrato * 0.5f));
            anillo = Sprite.Create(texturaAnillo, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f));
        }
        var fuente = bestiario != null ? bestiario.fuente : null;
        var contorno = bestiario != null ? bestiario.materialContorno : null;
        var pildora = bestiario != null ? bestiario.pildora : null;
        var click = bestiario != null ? bestiario.sonidoClick : null;

        var rtPanel = ConstructorUI.Estirar((RectTransform)transform, "Selector");
        panel = rtPanel.gameObject;
        panel.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.45f);   // tapa los toques del menu

        ventana = ConstructorUI.Rect(rtPanel, "Ventana", new Vector2(0f, 10f), TamVentana);
        var fondo = ventana.gameObject.AddComponent<Image>();
        ConstructorUI.VentanaNeon(ventana, fondo, pildora);
        fondo.color = Tema.Elegir(Color.white, RolDeTema.Panel);

        var titulo = ConstructorUI.Texto(ventana, "Titulo", Textos.De("capitulos_titulo"), 72f, ConstructorUI.TituloNeon,
                                         new Vector2(0f, 268f), new Vector2(1100f, 90f), fuente);
        var neon = MaterialDelTitulo();
        if (neon != null) titulo.fontSharedMaterial = neon;
        else if (contorno != null) titulo.fontSharedMaterial = contorno;

        int destrabados = Capitulos.Desbloqueados(Progreso.MejorOleada);
        for (int i = 0; i < Capitulos.Elegibles; i++)
        {
            float x = (i - 1) * (TamTarjeta.x + Separacion);
            ArmarTarjeta(i + 1, new Vector2(x, 2f), i + 1 <= destrabados, i + 1 == destrabados, fuente, contorno, pildora, click);
        }

        var volver = ConstructorUI.Boton(ventana, "Volver", new Vector2(0f, -278f), new Vector2(300f, 80f),
                                         Tema.Elegir(new Color(0.06f, 0.12f, 0.05f, 0.45f), RolDeTema.Vidrio), Color.white,
                                         bestiario != null ? bestiario.iconoAtras : null, Textos.De("comun_volver"), 40f, fuente, pildora, click);
        volver.onClick.AddListener(Cerrar);
        panel.SetActive(false);
    }

    private void ArmarTarjeta(int capitulo, Vector2 posicion, bool destrabado, bool elegido, TMP_FontAsset fuente,
                              Material contorno, Sprite pildora, AudioClip click)
    {
        Color color = Colores[capitulo - 1];
        Color acento = destrabado ? color : ColorBloqueado;
        var raiz = ConstructorUI.Rect(ventana, "Capitulo" + capitulo, posicion, TamTarjeta);
        var fondo = raiz.gameObject.AddComponent<Image>();
        fondo.color = new Color(0.06f, 0.07f, 0.1f, 0.97f);
        ConstructorUI.VentanaNeon(raiz, fondo, pildora);
        var halo = raiz.Find("Neon");
        if (halo != null) halo.GetComponent<Image>().color = new Color(acento.r, acento.g, acento.b, destrabado ? (elegido ? 1f : 0.8f) : 0.5f);

        // El halo del material de contorno es magenta: en la bloqueada teñia de rosa el gris.
        var numero = ConstructorUI.Texto(raiz, "Numero", Textos.Formato("capitulos_capitulo", capitulo), 32f, acento,
                                         new Vector2(0f, 172f), new Vector2(TamTarjeta.x - 30f, 40f), fuente);
        if (contorno != null && destrabado) numero.fontSharedMaterial = contorno;
        var nombre = ConstructorUI.Texto(raiz, "Nombre", Textos.De(Escenarios[capitulo - 1]), 42f,
                                         destrabado ? Color.white : ColorBloqueado, new Vector2(0f, 132f), new Vector2(TamTarjeta.x - 30f, 50f), fuente);
        if (contorno != null && destrabado) nombre.fontSharedMaterial = contorno;

        // El retrato en su disco, con el anillo del color del capitulo; bloqueado, la silueta en gris y un candado.
        var lugar = new Vector2(0f, 44f);
        ConstructorUI.Imagen(raiz, "Disco", lugar, new Vector2(LadoDelRetrato, LadoDelRetrato), circulo, VentanaBestiario.FondoDelRetrato);
        string zombi = Zombis[capitulo - 1];
        var foto = Resources.Load<Sprite>(destrabado ? VentanaBestiario.RutaDelRetrato(zombi) : VentanaBestiario.RutaDeLaSilueta(zombi));
        float adentro = LadoDelRetrato - 10f;
        var retrato = ConstructorUI.Imagen(raiz, "Retrato", lugar, new Vector2(adentro, adentro), foto, destrabado ? Color.white : ColorBloqueado);
        retrato.enabled = foto != null;
        ConstructorUI.Imagen(raiz, "Anillo", lugar, new Vector2(LadoDelRetrato, LadoDelRetrato), anillo,
                             destrabado ? acento : new Color(acento.r, acento.g, acento.b, VentanaBestiario.AlfaDelAnilloTapado));
        if (!destrabado) ArmarCandado(raiz, lugar + new Vector2(0f, -6f), pildora);

        if (destrabado)
        {
            ConstructorUI.Texto(raiz, "Oleada", Textos.Formato("cartel_oleada", Capitulos.OleadaInicial(capitulo)), 40f, Color.white,
                                new Vector2(0f, -40f), new Vector2(TamTarjeta.x - 30f, 48f), fuente);
            // Lo que trae: nada en la 1, y las cartas de la preparacion en las otras.
            int elecciones = PoderDeLaPartida.EleccionesDePreparacion(capitulo);
            int cartas = elecciones * PoderDeLaPartida.CopiasEnLaPreparacion;
            int doradas = elecciones / PoderDeLaPartida.EleccionesPorCapitulo;
            string ventaja = elecciones <= 0 ? Textos.De("capitulos_sin_ventajas")
                           : doradas == 1 ? Textos.Formato("capitulos_cartas_una_dorada", cartas)
                           : Textos.Formato("capitulos_cartas_doradas", cartas, doradas);
            var pastilla = ConstructorUI.Rect(raiz, "Ventaja", new Vector2(0f, -92f), new Vector2(TamTarjeta.x - 34f, 46f));
            var imgPastilla = pastilla.gameObject.AddComponent<Image>();
            imgPastilla.sprite = pildora;
            ConstructorUI.RedondearPildora(imgPastilla);
            imgPastilla.color = elecciones <= 0 ? new Color(1f, 1f, 1f, 0.1f) : ConstructorUI.Celeste;
            imgPastilla.raycastTarget = false;
            var textoVentaja = ConstructorUI.Texto(pastilla, "Texto", ventaja, 24f, elecciones <= 0 ? Color.white : CelesteTexto,
                                                   Vector2.zero, new Vector2(TamTarjeta.x - 50f, 46f), fuente);
            textoVentaja.enableAutoSizing = true;
            textoVentaja.fontSizeMin = 16f;
            textoVentaja.fontSizeMax = 24f;

            var jugar = ConstructorUI.Boton(raiz, "Jugar", new Vector2(0f, -160f), new Vector2(250f, 78f), ConstructorUI.Verde,
                                            ConstructorUI.VerdeTexto, IconoJugar(), Textos.De("capitulos_jugar"), 40f, fuente, pildora, click);
            int elegidoCapitulo = capitulo;
            jugar.onClick.AddListener(() => Jugar(elegidoCapitulo));
            if (elegido) jugar.GetComponent<BotonJugoso>().respirar = true;
        }
        else
        {
            var bloqueado = ConstructorUI.Texto(raiz, "Bloqueado",
                                                Textos.Formato("capitulos_bloqueado", Capitulos.OleadaInicial(capitulo) - 1), 30f,
                                                new Color(0.75f, 0.78f, 0.85f), new Vector2(0f, -92f), new Vector2(TamTarjeta.x - 30f, 90f), fuente);
            bloqueado.textWrappingMode = TextWrappingModes.Normal;
        }
    }

    // Un candado de dos piezas: el arco (el anillo, con la mitad de abajo tapada por el cuerpo) y el cuerpo redondeado.
    private void ArmarCandado(RectTransform padre, Vector2 lugar, Sprite pildora)
    {
        var color = new Color(0.62f, 0.66f, 0.75f);
        ConstructorUI.Imagen(padre, "CandadoArco", lugar + new Vector2(0f, 16f), new Vector2(40f, 40f), anillo, color);
        var cuerpo = ConstructorUI.Rect(padre, "CandadoCuerpo", lugar + new Vector2(0f, -8f), new Vector2(54f, 40f));
        var img = cuerpo.gameObject.AddComponent<Image>();
        ConstructorUI.Redondear(img, pildora, 8f);
        img.color = color;
        img.raycastTarget = false;
    }

    static readonly Color CelesteTexto = new Color(0f, 0.165f, 0.188f, 1f);

    // El triangulo de PLAY, de su boton en el menu.
    private static Sprite IconoJugar()
    {
        foreach (var img in FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (img.sprite != null && img.sprite.name == "IconoJugar") return img.sprite;
        return null;
    }

    // El halo de neon de los titulos (Bangers SDF - Neon), de algun titulo del menu que ya lo tenga (el del idioma).
    private static Material MaterialDelTitulo()
    {
        foreach (var texto in FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var material = texto.fontSharedMaterial;
            if (material != null && material.name.StartsWith("Bangers SDF - Neon") && !material.name.Contains("HUD")) return material;
        }
        return null;
    }
}
