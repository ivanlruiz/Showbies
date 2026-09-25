using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

// La tienda de mejoras del menú: un panel con una tarjeta por mejora de
// CatalogoMejoras.enTienda, generadas al cargar la escena y no puestas a mano,
// así una mejora nueva aparece sola al sumarla al catálogo.
//
// Cobra con Progreso.Comprar, que guarda en el acto. Las mejoras no se aplican
// acá: las aplica AplicarMejoras al empezar cada partida.
//
// Todo lo que se mueve usa tiempo sin escalar con el delta topeado en 1/30: el
// menú puede cargarse con timeScale todavía en cero, y el primer frame de la
// escena dura mucho y se comería las animaciones de entrada.
//
// No lee input: Escape y el botón atrás de Android son de BotonAtrasMenu, que
// cierra primero la tienda. Así hay un solo lector de Escape en el menú.
public class TiendaMejoras : MonoBehaviour
{
    // Índices de Build Settings. Están hardcodeados como en el resto del juego:
    // reordenar las escenas rompe la navegación en silencio.
    public const int EscenaMenu = 0;
    public const int EscenaModoLibre = 1;
    public const int EscenaOleadas = 3;

    // Lo prende el botón MEJORAS de la derrota antes de cargar el menú, para que
    // la tienda se abra sola al llegar.
    public static bool AbrirAlCargarMenu;

    public GameObject panel;
    public CanvasGroup grupoPanel;

    [Header("Menú (en la escena)")]
    public GameObject menuPrincipal;
    public GameObject menuModos;
    public GameObject extrasMenu;
    public UnityEngine.UI.Button botonAbrir;

    [Header("Panel")]
    public UnityEngine.UI.Button botonVolver;
    public UnityEngine.UI.Button botonJugar;
    public TarjetaMejora tarjetaPrefab;
    public RectTransform contenedor;
    public RectTransform viewport;
    public UnityEngine.UI.ScrollRect scroll;
    public ContadorMonedas contador;
    public RectTransform iconoMonedas;
    public TMPro.TMP_Text pista;
    public RectTransform sacudible;
    public UnityEngine.UI.RawImage rayos;
    public UnityEngine.UI.RawImage resplandor;
    public EfectosUI efectos;
    public UnityEngine.UI.Image flash;

    [Header("Audio")]
    public AudioSource musica;
    public float volumenMusicaAbierta = 0.4f;
    public AudioClip nota;          // moneda.wav
    public AudioClip sonidoTope;    // cartel.wav
    public AudioClip sonidoError;   // danio.wav
    public AudioClip sonidoClick;   // golpe.wav
    public AudioClip sonidoPop;     // pop.mp3

    public Color colorTope = new Color(1f, 0.84f, 0.25f);
    [Tooltip("Segundos entre dos compras para que la segunda cuente como combo")]
    public float ventanaCombo = 1.5f;

    private const float DuracionFade = 0.15f;
    private const float VelocidadRayos = 6f;         // grados por segundo
    private const float LatidoResplandor = 0.03f;
    private const float DuracionFlash = 0.25f;
    private const float AlfaFlash = 0.35f;

    // Un toque que llega con la fila deslizándose a más de esto (unidades del canvas por
    // segundo) es para frenarla y no compra (ver TarjetaMejora). Por debajo la fila ya se ve
    // quieta: con la desaceleración del ScrollRect le quedan menos de 30 de recorrido.
    public const float VelocidadParaFrenar = 60f;
    // Lo que queda entre la tarjeta que se muestra al abrir y el borde de la lista: el mismo
    // relleno que tiene la fila a los costados.
    private const float MargenDeLaFila = 10f;

    // Todo en la bemol mayor, en semitonos desde la bemol: las tarjetas entran
    // subiendo, las monedas que vuelan bajan por la escala y el arpegio de cada
    // compra es el acorde, transpuesto por I-IV-V-I' con la racha.
    private static readonly int[] SemitonosPop = { 0, 2, 4, 5 };
    private static readonly int[] SemitonosMonedas = { 12, 11, 9, 7, 5, 4, 2, 0 };
    private static readonly int[] SemitonosArpegio = { -12, -8, -5, 0 };
    private static readonly int[] TransporteCombo = { 0, 5, 7, 12 };
    private static readonly int[] SemitonosTope = { 12, 16, 19 };

    private readonly List<TarjetaMejora> tarjetas = new List<TarjetaMejora>();

    // Las lee GuiaPrimeraCompra para senalar la tarjeta recomendada.
    public IReadOnlyList<TarjetaMejora> Tarjetas
    {
        get { return tarjetas; }
    }
    // Segundos que faltan para el golpe de bienvenida de cada tarjeta comprable;
    // negativo si no hay ninguno pendiente.
    private readonly List<float> golpesPendientes = new List<float>();

    private Texture2D texturaRayos;
    private Texture2D texturaResplandor;
    private bool sinCatalogo;

    private bool abierta;
    // Lo que pidió AbrirAlCargarMenu: se abre en el primer Update en que la recompensa
    // diaria no está ocupada.
    private bool abrirAlLlegar;
    private bool filaPorAcomodar;
    private readonly Vector3[] esquinas = new Vector3[4];
    private float tiempoAbierta;
    private float fade;
    private int revisionVista;
    private int idiomaVisto = -1;
    private int temaVisto = -1;

    private bool musicaBajada;
    private float volumenMusicaOriginal;

    // La cámara del menú mientras la tienda la tapa entera, y lo que dibujaba antes (ver
    // DejarDeDibujarLaEscena). Null con la escena a la vista.
    private Camera camaraTapada;
    private int mascaraDeLaCamara;

    private int racha;
    private float ultimaCompra = float.NegativeInfinity;

    private Vector2 posicionBaseSacudible;
    private float sacudidaAmplitud;
    private float sacudidaDuracion;
    private float sacudidaTiempo = -1f;              // negativo: quieta

    private float flashTiempo = -1f;                 // negativo: apagado
    private Vector3 escalaBaseResplandor = Vector3.one;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetearEstadoCompartido()
    {
        AbrirAlCargarMenu = false;
    }

    public bool Abierta
    {
        get { return abierta; }
    }

    // Si la fila se está deslizando sola. Lo pregunta cada tarjeta al apoyar el dedo (ver
    // TarjetaMejora): en uGUI el toque que la frena también llega como click al botón de abajo.
    public bool FilaEnMovimiento
    {
        get
        {
            if (!abierta || scroll == null || !scroll.enabled) return false;
            return SeEstaDeslizando(scroll.velocity.x, scroll.horizontalNormalizedPosition);
        }
    }

    // Con la velocidad del contenido (positiva lo corre a la derecha, hacia el principio) y la
    // posición normalizada. Contra el borde al que va no cuenta: en Clamped el ScrollRect
    // corrige la posición y no la velocidad, que sigue bajando sola un par de segundos con la
    // fila ya quieta, y un toque ahí sería una compra que no se hace. Estática para las pruebas.
    public static bool SeEstaDeslizando(float velocidad, float posicion)
    {
        if (Mathf.Abs(velocidad) <= VelocidadParaFrenar) return false;
        return velocidad > 0f ? posicion > 0.001f : posicion < 0.999f;
    }

    private void Awake()
    {
        // Las texturas se hacen en código para no sumar assets; son de esta
        // tienda y se destruyen con ella.
        // Los rayos los apagó el carbón neón (ConstructorTienda): con el objeto apagado no se
        // dibuja su textura (512 x 512 con Atan2, Cos y Pow por píxel, y 1 MB en la GPU), que
        // se hacía en cada carga del menú, al abrir el juego y al volver de cada partida, para
        // algo que no se ve; y tampoco se lo gira en cada cuadro. activeSelf y no
        // activeInHierarchy: Panel arranca apagado, y con la jerarquía daría apagado siempre.
        if (rayos != null && !rayos.gameObject.activeSelf) rayos = null;
        if (rayos != null)
        {
            texturaRayos = TexturasUI.Rayos(512, 16);
            rayos.texture = texturaRayos;
        }
        texturaResplandor = TexturasUI.Resplandor(128);
        if (resplandor != null)
        {
            resplandor.texture = texturaResplandor;
            escalaBaseResplandor = resplandor.rectTransform.localScale;
        }
        if (sacudible != null) posicionBaseSacudible = sacudible.anchoredPosition;

        // El botón MEJORAS del menú se engancha acá y no con un listener
        // persistente en la escena: con los dos, cada toque abriría dos veces.
        if (botonAbrir != null) botonAbrir.onClick.AddListener(Abrir);
        if (botonVolver != null) botonVolver.onClick.AddListener(Cerrar);
        if (botonJugar != null) botonJugar.onClick.AddListener(Jugar);

        CrearTarjetas();
        if (panel != null) panel.SetActive(false);
    }

    private void Start()
    {
        if (!AbrirAlCargarMenu) return;

        AbrirAlCargarMenu = false;
        // No en el acto: si hoy hay recompensa diaria, primero ella (VentanaRecompensaDiaria.
        // Ocupada). Con la tienda encima la diaria esperaba a que se cerrara, y el circuito de
        // la derrota que enseña la primera vez (MEJORAS, comprar, ¡A JUGAR!) carga la partida
        // sin cerrarla: el día 1 no salía nunca. Todos los Start corren antes del primer
        // Update, así que sin diaria se abre antes del primer cuadro, como antes.
        abrirAlLlegar = true;
    }

    // También al destruirse (OnDisable corre antes que OnDestroy) o apagarse abierta: con ella
    // se va su panel, y la escena no puede quedar sin dibujarse.
    private void OnDisable()
    {
        VolverADibujarLaEscena();
    }

    private void OnDestroy()
    {
        if (texturaRayos != null) Destroy(texturaRayos);
        if (texturaResplandor != null) Destroy(texturaResplandor);
        if (abierta) RestaurarMusica();
    }

    private void CrearTarjetas()
    {
        CatalogoMejoras catalogo = CatalogoMejoras.Instancia;
        if (catalogo == null || catalogo.enTienda == null)
        {
            sinCatalogo = true;
            if (pista != null) pista.text = Textos.De("tienda_sin_catalogo");
            return;
        }

        if (tarjetaPrefab == null || contenedor == null)
        {
            Debug.LogWarning("TiendaMejoras: falta el prefab de tarjeta o el contenedor.", this);
            return;
        }

        // Las tarjetas nacen dentro del panel apagado: su Awake todavía no corrió
        // cuando se configuran, y por eso TarjetaMejora se inicializa sola.
        foreach (Mejora mejora in catalogo.enTienda)
        {
            if (mejora == null) continue;

            TarjetaMejora tarjeta = Instantiate(tarjetaPrefab, contenedor);
            tarjeta.Configurar(mejora, this, tarjetas.Count);
            tarjetas.Add(tarjeta);
            golpesPendientes.Add(-1f);
        }
    }

    // Idempotente: si ya está abierta sólo refresca, así un segundo camino que la
    // abra no reinicia la entrada de las tarjetas.
    public void Abrir()
    {
        if (abierta)
        {
            RefrescarTodas();
            return;
        }

        abierta = true;
        tiempoAbierta = 0f;
        fade = 0f;

        if (menuPrincipal != null) menuPrincipal.SetActive(false);
        if (menuModos != null) menuModos.SetActive(false);
        if (extrasMenu != null) extrasMenu.SetActive(false);

        if (panel != null) panel.SetActive(true);
        if (grupoPanel != null) grupoPanel.alpha = 0f;

        // Las tarjetas están dentro de un ScrollRect: con el umbral de arrastre por
        // defecto (10 px) un dedo que se mueve apenas al tocar en un teléfono de
        // muchos dpi se toma como arrastre y cancela la compra.
        if (EventSystem.current != null)
        {
            float dpi = Screen.dpi > 0 ? Screen.dpi : 160f;
            EventSystem.current.pixelDragThreshold = Mathf.Max(EventSystem.current.pixelDragThreshold, Mathf.RoundToInt(dpi * 0.06f));
        }

        // Los sonidos de la tienda están en la bemol y la música del menú puede no
        // estarlo: baja mientras la tienda está abierta.
        // Con FuenteConVolumen se baja por su Atenuacion: tocar volume directo lo pisaria
        // el volumen del jugador en el cuadro siguiente.
        if (musica != null && !musicaBajada)
        {
            var conVolumen = musica.GetComponent<FuenteConVolumen>();
            if (conVolumen != null) conVolumen.Atenuacion = volumenMusicaAbierta;
            else
            {
                volumenMusicaOriginal = musica.volume;
                musica.volume = volumenMusicaOriginal * volumenMusicaAbierta;
            }
            musicaBajada = true;
        }

        RefrescarTodas();
        // Después de refrescar: dónde arranca la fila depende de qué se puede comprar.
        filaPorAcomodar = !AcomodarFila(true);

        for (int i = 0; i < tarjetas.Count; i++)
        {
            tarjetas[i].Entrar(0.06f * i);
            Sonidos.Programar(sonidoPop, 0.06 * i,0.6f, Sonidos.PitchDe(SemitonosPop[i % SemitonosPop.Length]));
            golpesPendientes[i] = tarjetas[i].Estado == EstadoMejora.Comprable ? 0.35f + 0.06f * i : -1f;
        }
    }

    public void Cerrar()
    {
        // Lo primero, antes de que vuelva a verse algo de la escena (si la salida tuviera un
        // fundido, esto iría al empezarlo, no al terminarlo).
        VolverADibujarLaEscena();
        abierta = false;
        filaPorAcomodar = false;
        if (panel != null) panel.SetActive(false);
        if (menuPrincipal != null) menuPrincipal.SetActive(true);
        if (extrasMenu != null) extrasMenu.SetActive(true);
        if (menuModos != null) menuModos.SetActive(false);

        RestaurarMusica();
        TerminarSacudidaYFlash();
    }

    // Vuelve al último modo jugado; sin uno válido, a las oleadas, que son las
    // que dan el bono de monedas.
    public void Jugar()
    {
        int modo = PlayerPrefs.GetInt("UltimoModo", EscenaOleadas);
        if (modo != EscenaModoLibre && modo != EscenaOleadas) modo = EscenaOleadas;
        modo = ModoLibre.EscenaPara(modo);

        Time.timeScale = 1f;
        SceneManager.LoadScene(modo);
    }

    // Desde otra escena (la derrota): carga el menú con la tienda abierta.
    // timeScale y AudioListener.pause son globales y cruzan escenas.
    public static void AbrirEnMenu()
    {
        AbrirAlCargarMenu = true;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene(EscenaMenu);
    }

    // Con la tienda entera a la vista (el fundido de entrada terminado: su fondo es opaco y
    // ocupa toda la pantalla) la cámara del menú no dibuja nada: seguía pintando el piso con
    // niebla y los zombis del fondo, con su sombra, detrás de un panel que no deja ver nada,
    // en la pantalla donde más rato se pasa entre partidas. Igual limpia con su color, y los
    // canvas del menú y de la tienda son overlay, que no pasan por ella. No en Abrir: el panel
    // entra con un fundido, y se vería el color liso de golpe. Que el fondo tape todo lo mira
    // la prueba de lógica.
    private void DejarDeDibujarLaEscena()
    {
        if (camaraTapada != null || panel == null) return;
        Camera camara = Camera.main;
        if (camara == null) return;
        camaraTapada = camara;
        mascaraDeLaCamara = camara.cullingMask;
        camara.cullingMask = 0;
    }

    // Se puede llamar de más. Si la cámara ya no está (se está descargando el menú), no hay
    // nada que devolver.
    private void VolverADibujarLaEscena()
    {
        if (camaraTapada != null) camaraTapada.cullingMask = mascaraDeLaCamara;
        camaraTapada = null;
    }

    private void RestaurarMusica()
    {
        if (!musicaBajada) return;

        musicaBajada = false;
        if (musica == null) return;
        var conVolumen = musica.GetComponent<FuenteConVolumen>();
        if (conVolumen != null) conVolumen.Atenuacion = 1f;
        else musica.volume = volumenMusicaOriginal;
    }

    private void RefrescarTodas()
    {
        for (int i = 0; i < tarjetas.Count; i++) tarjetas[i].Refrescar();
        revisionVista = Progreso.Revision;
        idiomaVisto = Idioma.Revision;
        temaVisto = Tema.Revision;
        EscribirPista();
    }

    private void EscribirPista()
    {
        if (pista == null) return;
        if (sinCatalogo)
        {
            pista.text = Textos.De("tienda_sin_catalogo");
            return;
        }

        int mejorOleada = Progreso.MejorOleada;
        pista.text = mejorOleada > 0
            ? Textos.Formato("tienda_pie_mejor_oleada", mejorOleada)
            : Textos.De("tienda_pie_sin_oleadas");
    }

    private void Update()
    {
        if (abrirAlLlegar && !VentanaRecompensaDiaria.Ocupada)
        {
            abrirAlLlegar = false;
            Abrir();
        }
        if (!abierta) return;

        float dt = Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);
        tiempoAbierta += dt;

        if (fade < 1f)
        {
            fade = Mathf.Min(1f, fade + dt / DuracionFade);
            if (grupoPanel != null) grupoPanel.alpha = fade;
            // En el mismo cuadro en que el panel queda opaco: la escena ya no se ve.
            if (fade >= 1f) DejarDeDibujarLaEscena();
        }

        if (rayos != null)
            rayos.rectTransform.localEulerAngles = new Vector3(0f, 0f, -VelocidadRayos * tiempoAbierta);
        if (resplandor != null)
            resplandor.rectTransform.localScale = escalaBaseResplandor * (1f + LatidoResplandor * Mathf.Sin(Mathf.PI * tiempoAbierta));

        // Sin eventos: la tienda mira el contador de cambios de Progreso. Así se
        // entera también de las monedas y niveles que cambian las herramientas
        // del editor con la tienda abierta.
        // Y del tema: lo que pinta cada tarjeta en su refresco (el valor siguiente) cambia
        // con el.
        if (Progreso.Revision != revisionVista || Idioma.Revision != idiomaVisto || Tema.Revision != temaVisto) RefrescarTodas();

        ActualizarScroll();
        if (filaPorAcomodar && AcomodarFila(false)) filaPorAcomodar = false;
        ActualizarGolpesPendientes(dt);
        ActualizarSacudida(dt);
        ActualizarFlash(dt);
    }

    // Con las tarjetas que entran en pantalla el scroll queda apagado: si no, un
    // toque con arrastre chico movería la fila en vez de comprar. Se prende solo
    // cuando haya más tarjetas que ancho.
    private void ActualizarScroll()
    {
        if (scroll == null || contenedor == null || viewport == null) return;

        bool hayQueDesplazar = contenedor.rect.width > viewport.rect.width + 1f;
        if (scroll.enabled != hayQueDesplazar) scroll.enabled = hayQueDesplazar;
        if (!hayQueDesplazar && contenedor.anchoredPosition != Vector2.zero)
            contenedor.anchoredPosition = Vector2.zero;
    }

    // La fila arranca al principio, con el daño (la primera tarjeta y la que más se compra) a
    // la vista. Contenido está centrado en la lista y el ScrollRect, en Clamped, no corrige una
    // fila que se pasa por los dos lados: abría centrada, con el daño afuera y la cadencia
    // cortada, en cada visita (el menú se vuelve a cargar al volver de jugar). Devuelve falso si
    // la fila todavía no tenía su ancho, para volver a probar en el cuadro siguiente.
    private bool AcomodarFila(bool forzarLayout)
    {
        if (scroll == null || contenedor == null || viewport == null) return true;

        // El panel recién se prendió: sin esto la fila todavía no tiene el ancho del layout.
        if (forzarLayout) Canvas.ForceUpdateCanvases();
        float anchoFila = contenedor.rect.width;
        float anchoVista = viewport.rect.width;
        if (anchoFila <= anchoVista + 1f) return false;

        // El borde derecho de la primera tarjeta que se puede comprar, medido desde el comienzo
        // de la fila. La raíz de la tarjeta la ubica el layout y no se anima: lo que entra
        // animado es su contenido.
        float derecha = float.NaN;
        foreach (TarjetaMejora tarjeta in tarjetas)
        {
            if (tarjeta == null || tarjeta.Estado != EstadoMejora.Comprable) continue;
            ((RectTransform)tarjeta.transform).GetWorldCorners(esquinas);
            derecha = contenedor.InverseTransformPoint(esquinas[2]).x - contenedor.rect.xMin;
            break;
        }

        scroll.StopMovement();
        scroll.horizontalNormalizedPosition = PosicionAlAbrir(anchoFila, anchoVista, derecha);
        return true;
    }

    // Dónde arranca la fila, como posición normalizada del ScrollRect (0 es el principio): al
    // principio, salvo que la primera tarjeta que se puede comprar no entre entera desde ahí;
    // entonces corre lo justo para mostrarla, con el margen de la fila. 'derecha' es el borde
    // derecho de esa tarjeta medido desde el comienzo de la fila, y NaN si no hay ninguna: sin
    // nada para comprar queda el principio, con el daño y lo que le falta. Estática para las
    // pruebas.
    public static float PosicionAlAbrir(float anchoFila, float anchoVista, float derecha)
    {
        float oculto = anchoFila - anchoVista;
        if (oculto <= 0f || float.IsNaN(derecha)) return 0f;
        return Mathf.Clamp01((derecha + MargenDeLaFila - anchoVista) / oculto);
    }

    private void ActualizarGolpesPendientes(float dt)
    {
        for (int i = 0; i < golpesPendientes.Count; i++)
        {
            if (golpesPendientes[i] < 0f) continue;

            golpesPendientes[i] -= dt;
            if (golpesPendientes[i] >= 0f) continue;

            // Si mientras entraba dejó de ser comprable, que no invite a tocarla.
            TarjetaMejora tarjeta = tarjetas[i];
            if (tarjeta.Estado == EstadoMejora.Comprable && tarjeta.jugoBoton != null)
                tarjeta.jugoBoton.Golpe(1.15f);
        }
    }

    private void Sacudir(float amplitud, float duracion)
    {
        sacudidaAmplitud = amplitud;
        sacudidaDuracion = Mathf.Max(0.01f, duracion);
        sacudidaTiempo = 0f;
    }

    private void ActualizarSacudida(float dt)
    {
        if (sacudidaTiempo < 0f || sacudible == null) return;

        sacudidaTiempo += dt;
        if (sacudidaTiempo >= sacudidaDuracion)
        {
            sacudidaTiempo = -1f;
            sacudible.anchoredPosition = posicionBaseSacudible;
            return;
        }

        float x = sacudidaAmplitud * CurvasUI.Oscilacion(sacudidaTiempo / sacudidaDuracion, 3f);
        sacudible.anchoredPosition = posicionBaseSacudible + new Vector2(x, 0f);
    }

    private void ActualizarFlash(float dt)
    {
        if (flashTiempo < 0f || flash == null) return;

        flashTiempo += dt;
        float alfa = flashTiempo >= DuracionFlash ? 0f : AlfaFlash * (1f - flashTiempo / DuracionFlash);
        if (flashTiempo >= DuracionFlash) flashTiempo = -1f;
        PonerAlfaFlash(alfa);
    }

    private void PonerAlfaFlash(float alfa)
    {
        Color color = colorTope;
        color.a = alfa;
        flash.color = color;
    }

    private void TerminarSacudidaYFlash()
    {
        sacudidaTiempo = -1f;
        if (sacudible != null) sacudible.anchoredPosition = posicionBaseSacudible;

        flashTiempo = -1f;
        if (flash != null) PonerAlfaFlash(0f);
    }

    public void IntentarComprar(TarjetaMejora tarjeta)
    {
        if (tarjeta == null) return;

        Mejora mejora = tarjeta.Mejora;
        int nivel = mejora != null ? Progreso.Nivel(mejora.id) : 0;
        double precio = mejora != null ? mejora.Precio(nivel) : 0;

        ResultadoCompra resultado = Progreso.Comprar(mejora);
        switch (resultado)
        {
            case ResultadoCompra.Comprada:
                FestejarCompra(tarjeta, mejora, nivel, precio);
                break;

            case ResultadoCompra.SinMonedas:
                // Sin carteles ni vibración: tiembla lo que falta y suena seco.
                tarjeta.Rechazar();
                if (contador != null) contador.Sacudir(8f, 0.3f);
                Sonidos.Tocar(sonidoError, 0.45f, 1.5f, 0.05f, 0.12f);
                break;

            case ResultadoCompra.EnTope:
                tarjeta.RechazarTope();
                Sonidos.Tocar(sonidoClick, 0.3f, 2f, 0f, 0.1f);
                break;

            default:
                Debug.LogWarning("TiendaMejoras: se intentó comprar una mejora inválida.", tarjeta);
                break;
        }
    }

    private void FestejarCompra(TarjetaMejora tarjeta, Mejora mejora, int nivel, double precio)
    {
        // Comprar seguido se siente como un combo: cada compra dentro de la
        // ventana sube el acorde del arpegio.
        float ahora = Time.unscaledTime;
        racha = ahora - ultimaCompra <= ventanaCombo ? racha + 1 : 0;
        ultimaCompra = ahora;
        int transporte = TransporteCombo[Mathf.Min(racha, TransporteCombo.Length - 1)];

        RectTransform destino = tarjeta.DestinoMonedas;

        if (efectos != null)
        {
            if (contador != null)
                // Baja en vez de subir: el contador esta pegado al borde de arriba y el texto
                // se iba fuera de la pantalla.
                efectos.TextoFlotante(contador.Rect, "-" + FormatoNumeros.Compacto(precio), new Color(1f, 0.45f, 0.4f), 56f, true);

            // Más monedas cuanto más caro, pero con techo: con 12 ya se lee como
            // chorro y cada llegada es una nota.
            int monedas = Mathf.Clamp(Mathf.RoundToInt(3f + (float)System.Math.Log(System.Math.Max(1.0, precio), 2.0)), 4, 12);
            efectos.MonedasVolando(iconoMonedas, destino, monedas, i =>
            {
                Sonidos.Tocar(nota, 0.3f, Sonidos.PitchDe(SemitonosMonedas[i % SemitonosMonedas.Length]), 0f, 0f);
                tarjeta.GolpeChico();
            });
        }

        bool llegoAlTope = mejora.EnTope(nivel + 1);
        tarjeta.Festejar(llegoAlTope);

        // Programado con PlayScheduled: con Tocar por frame las notas saldrían
        // con el ritmo de los frames y no cada 45 ms.
        for (int k = 0; k < SemitonosArpegio.Length; k++)
            Sonidos.Programar(nota, 0.045 * k, 0.8f, Sonidos.PitchDe(SemitonosArpegio[k] + transporte));

        Sacudir(8f, 0.15f);

        if (racha >= 1 && efectos != null)
            efectos.TextoFlotante(destino, Textos.Formato("tienda_racha", racha + 1), Color.white, 60f);

        if (llegoAlTope)
        {
            Sonidos.Tocar(sonidoTope, 1f);
            for (int k = 0; k < SemitonosTope.Length; k++)
                Sonidos.Programar(nota, 0.30 + 0.06 * k, 0.5f, Sonidos.PitchDe(SemitonosTope[k]));

            if (flash != null)
            {
                flashTiempo = 0f;
                PonerAlfaFlash(AlfaFlash);
            }
            Sacudir(14f, 0.25f);

            if (efectos != null)
            {
                efectos.Estallido(destino, colorTope, 40);
                efectos.TextoFlotante(destino, Textos.De("tienda_maximo"), colorTope, 90f);
            }
        }

        RefrescarTodas();
    }
}
