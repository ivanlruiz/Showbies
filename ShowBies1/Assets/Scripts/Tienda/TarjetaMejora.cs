using UnityEngine;
using UnityEngine.EventSystems;

// Una tarjeta de la tienda: nombre, nivel, "valor → siguiente" y el botón con el
// precio. La genera TiendaMejoras desde el catálogo; no cobra nada, sólo le avisa
// a la tienda del toque y anima lo que la tienda le pide.
//
// Tres estados que tienen que leerse de lejos: Comprable (verde, respira, el
// borde late y un brillo barre el botón), SinMonedas (gris y quieto, precio
// apagado y "faltan N") y EnTope (amarillo con "MÁX"; la de carbón neón ya no
// lleva el sello de "¡MÁXIMO!", pero si el prefab tiene estampa, la anima). Lo que se
// puede comprar se mueve; lo que no, queda quieto. El botón queda siempre
// tocable: un toque sin monedas también responde, con un rechazo. El que no compra
// es el toque que frena la fila mientras se desliza (ver ToqueQueFrena).
//
// Se viste de carbón neón (pedido de Ivan, 24/9; lo arma ConstructorTienda): la
// tarjeta oscura con un borde celeste que brilla (haloTarjeta) y late cuando se
// puede comprar, y el botón con su halo (haloBoton). La paleta es propia y no
// cambia con el tema claro u oscuro: todo sale de los colores de acá.
//
// Anima siempre `contenido` y sus hijos, nunca la raíz: la raíz la ubica el
// HorizontalLayoutGroup de la fila y se pelearían. Tiempo sin escalar con el
// delta topeado en 1/30, sin corrutinas.
public class TarjetaMejora : MonoBehaviour
{
    public RectTransform contenido;
    public CanvasGroup grupo;
    public UnityEngine.UI.Image borde, franja, icono, imagenIcono, fondoBoton, destello, brillo;
    public RectTransform barraNivel, rellenoNivel, raizBoton;
    public TMPro.TMP_Text nombre, nivel, valorActual, valorSiguiente, descripcion, faltan, precio, textoTope, estampa;
    public GameObject flecha, grupoPrecio;
    public UnityEngine.UI.Button boton;
    public BotonJugoso jugoBoton;

    public Color colorComprable = new Color(0.2f, 0.78f, 0.3f);
    public Color colorSinMonedas = new Color(0.27f, 0.29f, 0.34f);
    public Color colorTope = new Color(1f, 0.84f, 0.25f);
    public Color colorPrecioFalta = new Color(1f, 0.5f, 0.45f);
    public Color colorValorSiguiente = new Color(0.5f, 1f, 0.45f);
    [Tooltip("El precio sobre el boton de comprar.")]
    public Color colorPrecioComprable = Color.white;

    [Header("Carbon neon")]
    [Tooltip("El borde que brilla alrededor de la tarjeta (Sprites/UI/NeonBorde).")]
    public UnityEngine.UI.Image haloTarjeta;
    [Tooltip("El halo detras del boton (Sprites/UI/NeonPildora), del color del boton.")]
    public UnityEngine.UI.Image haloBoton;
    public Color colorNeon = new Color(0f, 0.9f, 1f);

    private const float DuracionEntrada = 0.3f;
    private const float CaidaEntrada = 60f;
    private const float EscalaEntrada = 0.85f;
    private const float DuracionFestejo = 0.25f;
    private const float DuracionNivel = 0.3f;
    private const float DuracionRechazo = 0.3f;
    private const float DuracionEstampa = 0.35f;
    private const float DuracionBarrido = 0.45f;
    private const float DuracionRelleno = 0.25f;
    private static readonly Color RojoRechazo = new Color(1f, 0.2f, 0.2f);

    public Mejora Mejora { get; private set; }
    public EstadoMejora Estado { get; private set; }

    // El color al que vuelve el texto del nivel despues de festejar. Lo decide su
    // pintor, que es el que sabe si el tema es claro u oscuro; sin pintor, el del prefab.
    private Color ColorNivel
    {
        get { return pintorNivel != null ? pintorNivel.Actual() : colorBaseNivel; }
    }

    // Adonde vuelan las monedas de la compra y donde salen las chispas.
    public RectTransform DestinoMonedas
    {
        get { return raizBoton; }
    }

    private TiendaMejoras tienda;
    private int indice;
    private bool inicializada;

    // Lo que trae el prefab, para volver después de cada animación.
    private Vector2 posicionBaseContenido;
    private Vector3 escalaBaseContenido = Vector3.one;
    private Color colorBaseNivel = Color.white;
    private PintarConTema pintorNivel;      // el del texto del nivel, que sabe del tema
    private Vector3 escalaBaseNivel = Vector3.one;
    private Vector3 escalaBaseValor = Vector3.one;
    private Vector3 escalaBaseFaltan = Vector3.one;
    private Vector3 escalaBaseEstampa = Vector3.one;
    private Vector2 posicionBaseBrillo;
    private Color colorPrecioActual = Color.white;

    private float relleno;
    private float objetivoRelleno;
    private float fase;                              // desfase del latido y del brillo

    // Relojes de cada animación; negativo es "no está corriendo". La entrada
    // arranca negativa con su demora y corre desde 0.
    private bool entrando;
    private float tiempoEntrada;
    private float tiempoFestejo = -1f;
    private float tiempoNivel = -1f;
    private float tiempoRechazo = -1f;
    private float tiempoEstampa = -1f;
    private bool poseAnimada;

    private void Awake()
    {
        Inicializar();

        // Lo que hace que el toque que frena la fila no compre. En Awake, que corre solo en
        // play: fuera de play nadie toca la tarjeta.
        if (boton != null && boton.GetComponent<ToqueQueFrena>() == null)
            boton.gameObject.AddComponent<ToqueQueFrena>().tarjeta = this;
    }

    // La tienda crea las tarjetas dentro del panel apagado y las configura antes
    // de que corra su Awake: las bases se toman en el primero de los dos.
    private void Inicializar()
    {
        if (inicializada) return;
        inicializada = true;

        if (contenido != null)
        {
            posicionBaseContenido = contenido.anchoredPosition;
            escalaBaseContenido = contenido.localScale;
        }
        if (nivel != null)
        {
            pintorNivel = nivel.GetComponent<PintarConTema>();
            colorBaseNivel = nivel.color;
            escalaBaseNivel = nivel.rectTransform.localScale;
        }
        if (valorActual != null) escalaBaseValor = valorActual.rectTransform.localScale;
        if (faltan != null) escalaBaseFaltan = faltan.rectTransform.localScale;
        if (estampa != null) escalaBaseEstampa = estampa.rectTransform.localScale;
        if (brillo != null) posicionBaseBrillo = brillo.rectTransform.anchoredPosition;
        if (precio != null) colorPrecioActual = precio.color;
    }

    public void Configurar(Mejora mejora, TiendaMejoras tienda, int indice)
    {
        Inicializar();

        Mejora = mejora;
        this.tienda = tienda;
        this.indice = indice;
        fase = indice * 0.37f;

        if (mejora == null) return;

        if (icono != null) icono.color = mejora.color;
        if (franja != null) franja.color = mejora.color;
        PonerColorRelleno(mejora.color);
        if (borde != null)
        {
            Color colorBorde = mejora.color;
            colorBorde.a = 0f;
            borde.color = colorBorde;
        }

        if (imagenIcono != null)
        {
            imagenIcono.sprite = mejora.icono;
            imagenIcono.gameObject.SetActive(mejora.icono != null);
        }

        if (boton != null)
        {
            boton.onClick.RemoveAllListeners();
            boton.onClick.AddListener(() =>
            {
                if (this.tienda != null) this.tienda.IntentarComprar(this);
            });
        }

        Refrescar();

        // Al configurar la barra ya muestra el nivel que hay: sólo se llena
        // animada con una compra.
        relleno = objetivoRelleno;
        AplicarRelleno();
    }

    public void Refrescar()
    {
        Inicializar();
        if (Mejora == null) return;

        int n = Progreso.Nivel(Mejora.id);
        Estado = Progreso.Estado(Mejora);

        // El nombre y la unidad se escriben en cada refresco y no al configurar: la
        // tienda refresca cuando cambia el idioma. Salen de la tabla por el id de la
        // mejora, que ya es fijo para siempre.
        if (nombre != null) nombre.text = Textos.De("mejora_" + Mejora.id + "_nombre");
        if (descripcion != null) descripcion.text = Textos.De("mejora_" + Mejora.id + "_unidad");

        if (nivel != null)
            nivel.text = Mejora.TieneTope
                ? Textos.Formato("tarjeta_nivel_tope", Mathf.Min(n, Mejora.nivelMaximo), Mejora.nivelMaximo)
                : Textos.Formato("tarjeta_nivel", n);

        if (barraNivel != null) barraNivel.gameObject.SetActive(Mejora.TieneTope);
        objetivoRelleno = Mejora.TieneTope ? Mathf.Clamp01((float)n / Mejora.nivelMaximo) : 0f;

        if (valorActual != null) valorActual.text = Mejora.TextoValor(n);

        if (Estado == EstadoMejora.EnTope)
        {
            ActivarSiHay(flecha, false);
            if (valorSiguiente != null) valorSiguiente.gameObject.SetActive(false);
            ActivarSiHay(grupoPrecio, false);
            if (faltan != null) faltan.gameObject.SetActive(false);

            if (textoTope != null)
            {
                textoTope.gameObject.SetActive(true);
                textoTope.text = Textos.De("tarjeta_max");
            }
            if (estampa != null) estampa.gameObject.SetActive(true);

            if (fondoBoton != null) fondoBoton.color = colorTope;
            if (franja != null) franja.color = colorTope;
            PonerColorRelleno(colorTope);
            PintarHalos(colorTope, colorTope);
            if (jugoBoton != null) jugoBoton.respirar = false;
            return;
        }

        ActivarSiHay(flecha, true);
        if (valorSiguiente != null)
        {
            valorSiguiente.gameObject.SetActive(true);
            valorSiguiente.text = Mejora.TextoValor(n + 1);
            // Siempre el de la tarjeta: la tienda es de carbon neon con los dos temas.
            valorSiguiente.color = colorValorSiguiente;
        }
        ActivarSiHay(grupoPrecio, true);
        double costo = Mejora.Precio(n);
        if (precio != null) precio.text = FormatoNumeros.Compacto(costo);

        if (textoTope != null) textoTope.gameObject.SetActive(false);
        if (estampa != null) estampa.gameObject.SetActive(false);
        tiempoEstampa = -1f;

        if (franja != null) franja.color = Mejora.color;
        PonerColorRelleno(Mejora.color);

        bool comprable = Estado == EstadoMejora.Comprable;
        if (fondoBoton != null) fondoBoton.color = comprable ? colorComprable : colorSinMonedas;
        PintarHalos(colorNeon, colorComprable);
        colorPrecioActual = comprable ? colorPrecioComprable : colorPrecioFalta;
        // Durante el destello del rechazo el color lo lleva Update.
        if (precio != null && tiempoRechazo < 0f) precio.color = colorPrecioActual;

        if (faltan != null)
        {
            faltan.gameObject.SetActive(!comprable);
            if (!comprable)
                faltan.text = Textos.Formato("tarjeta_faltan", FormatoNumeros.Compacto(System.Math.Max(1.0, costo - Progreso.MonedasEnteras)));
        }

        if (jugoBoton != null) jugoBoton.respirar = comprable;
    }

    // Entrada escalonada al abrir la tienda. Deja la tarjeta en su pose inicial
    // en el acto, así no se ve entera un frame antes de entrar.
    public void Entrar(float demora)
    {
        Inicializar();
        entrando = true;
        tiempoEntrada = -Mathf.Max(0f, demora);
        AplicarPoseContenido();
    }

    public void Festejar(bool llegoAlTope)
    {
        Inicializar();
        tiempoFestejo = 0f;
        tiempoNivel = 0f;
        PonerAlfaDestello(0.6f);

        if (jugoBoton != null) jugoBoton.Golpe(1.25f);
        if (tienda != null && tienda.efectos != null && Mejora != null)
            tienda.efectos.Estallido(raizBoton, Mejora.color, 18);

        if (llegoAlTope && estampa != null)
        {
            tiempoEstampa = 0f;
            estampa.rectTransform.localScale = escalaBaseEstampa * 2.5f;
        }
    }

    public void Rechazar()
    {
        Inicializar();
        if (jugoBoton != null) jugoBoton.Sacudir(14f, 0.3f);
        tiempoRechazo = 0f;
        if (precio != null) precio.color = RojoRechazo;
    }

    public void RechazarTope()
    {
        if (jugoBoton != null) jugoBoton.Golpe(1.06f, 0.2f);
        if (tienda != null && tienda.efectos != null)
            tienda.efectos.Estallido(raizBoton, colorTope, 8);
    }

    // Cada moneda que llega al botón le da un golpecito.
    public void GolpeChico()
    {
        if (jugoBoton != null) jugoBoton.Golpe(1.08f, 0.12f);
    }

    private void OnDisable()
    {
        if (!inicializada) return;

        // Si la tienda se cierra en medio de una animación, la tarjeta vuelve
        // entera: la próxima vez la entrada arranca de su pose inicial.
        entrando = false;
        tiempoFestejo = -1f;
        tiempoNivel = -1f;
        tiempoRechazo = -1f;
        if (tiempoEstampa >= 0f) tiempoEstampa = DuracionEstampa;

        AplicarPoseContenido();
        PonerAlfaDestello(0f);
        if (nivel != null)
        {
            nivel.color = ColorNivel;
            nivel.rectTransform.localScale = escalaBaseNivel;
        }
        if (valorActual != null) valorActual.rectTransform.localScale = escalaBaseValor;
        if (faltan != null) faltan.rectTransform.localScale = escalaBaseFaltan;
        if (precio != null) precio.color = colorPrecioActual;
        relleno = objetivoRelleno;
        AplicarRelleno();
    }

    private void Update()
    {
        if (Mejora == null) return;

        float dt = Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);

        if (entrando)
        {
            tiempoEntrada += dt;
            if (tiempoEntrada >= DuracionEntrada) entrando = false;
        }
        if (tiempoFestejo >= 0f)
        {
            tiempoFestejo += dt;
            if (tiempoFestejo >= DuracionFestejo) tiempoFestejo = -1f;
            PonerAlfaDestello(tiempoFestejo < 0f ? 0f : 0.6f * (1f - tiempoFestejo / DuracionFestejo));
        }

        // Tocar escala, posición o alfa quieta ensucia el canvas por frame: la pose
        // se escribe mientras anima y una vez más al terminar, para dejarla en su base.
        bool animandoPose = entrando || tiempoFestejo >= 0f;
        if (animandoPose || poseAnimada) AplicarPoseContenido();
        poseAnimada = animandoPose;

        ActualizarNivel(dt);
        ActualizarRechazo(dt);
        ActualizarEstampa(dt);
        ActualizarBorde();

        if (!Mathf.Approximately(relleno, objetivoRelleno))
        {
            relleno = Mathf.MoveTowards(relleno, objetivoRelleno, dt / DuracionRelleno);
            AplicarRelleno();
        }

        ActualizarBrillo();
    }

    // La entrada y el golpe del festejo se multiplican: una compra justo al abrir
    // no corta la entrada.
    private void AplicarPoseContenido()
    {
        if (contenido == null) return;

        float escala = 1f;
        float caida = 0f;
        float alfa = 1f;
        if (entrando)
        {
            if (tiempoEntrada <= 0f)
            {
                escala = EscalaEntrada;
                caida = CaidaEntrada;
                alfa = 0f;
            }
            else
            {
                float k = CurvasUI.SalidaAtras(tiempoEntrada / DuracionEntrada);
                escala = Mathf.LerpUnclamped(EscalaEntrada, 1f, k);
                caida = CaidaEntrada * (1f - k);
                alfa = Mathf.Clamp01(k);
            }
        }
        if (tiempoFestejo >= 0f)
            escala *= 1f + 0.1f * CurvasUI.Campana(tiempoFestejo / DuracionFestejo);

        contenido.anchoredPosition = posicionBaseContenido - new Vector2(0f, caida);
        contenido.localScale = escalaBaseContenido * escala;
        if (grupo != null) grupo.alpha = alfa;
    }

    // "NIVEL n" crece y se pone dorado, y el valor actual da un golpe.
    private void ActualizarNivel(float dt)
    {
        if (tiempoNivel < 0f) return;

        tiempoNivel += dt;
        float t = tiempoNivel / DuracionNivel;
        float golpe = t >= 1f ? 0f : CurvasUI.Campana(t);
        if (t >= 1f) tiempoNivel = -1f;

        if (nivel != null)
        {
            nivel.rectTransform.localScale = escalaBaseNivel * (1f + 0.5f * golpe);
            nivel.color = Color.Lerp(ColorNivel, colorTope, golpe);
        }
        if (valorActual != null)
        {
            // El golpe del valor dura lo del festejo, más corto que el del nivel.
            float tv = tiempoNivel < 0f ? 1f : tiempoNivel / DuracionFestejo;
            valorActual.rectTransform.localScale = escalaBaseValor * (tv >= 1f ? 1f : 1f + 0.35f * CurvasUI.Campana(tv));
        }
    }

    // El precio destella rojo intenso y vuelve a su color; "faltan N" da un golpe.
    private void ActualizarRechazo(float dt)
    {
        if (tiempoRechazo < 0f) return;

        tiempoRechazo += dt;
        float t = tiempoRechazo / DuracionRechazo;
        if (t >= 1f)
        {
            tiempoRechazo = -1f;
            if (precio != null) precio.color = colorPrecioActual;
            if (faltan != null) faltan.rectTransform.localScale = escalaBaseFaltan;
            return;
        }

        if (precio != null) precio.color = Color.Lerp(RojoRechazo, colorPrecioActual, CurvasUI.SalidaCubica(t));
        if (faltan != null) faltan.rectTransform.localScale = escalaBaseFaltan * (1f + 0.3f * CurvasUI.Campana(t));
    }

    // La estampa entra de golpe desde grande y después late, mientras dure el tope.
    private void ActualizarEstampa(float dt)
    {
        if (estampa == null || !estampa.gameObject.activeSelf) return;

        float escala;
        if (tiempoEstampa >= 0f && tiempoEstampa < DuracionEstampa)
        {
            tiempoEstampa += dt;
            escala = Mathf.LerpUnclamped(2.5f, 1f, CurvasUI.SalidaAtras(tiempoEstampa / DuracionEstampa));
        }
        else
        {
            escala = 1f + 0.03f * Mathf.Sin(2f * Mathf.PI * 1.2f * (Time.unscaledTime + fase));
        }
        estampa.rectTransform.localScale = escalaBaseEstampa * escala;
    }

    // El neon: el borde de la tarjeta late si se puede comprar, queda entero en el tope y
    // tenue si no alcanza; el halo del boton brilla solo si hay algo que tocar.
    private void ActualizarHalos()
    {
        float latido = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 1.4f * (Time.unscaledTime + fase));
        if (haloTarjeta != null)
        {
            float alfa = Estado == EstadoMejora.Comprable ? 0.7f + 0.3f * latido
                : Estado == EstadoMejora.EnTope ? 1f
                : 0.4f;
            PonerAlfa(haloTarjeta, alfa);
        }
        if (haloBoton != null)
        {
            float alfa = Estado == EstadoMejora.Comprable ? 0.4f + 0.3f * latido
                : Estado == EstadoMejora.EnTope ? 0.45f
                : 0f;
            PonerAlfa(haloBoton, alfa);
        }
    }

    // El tono de los halos; el alfa lo lleva ActualizarHalos.
    private void PintarHalos(Color tarjeta, Color boton)
    {
        if (haloTarjeta != null) haloTarjeta.color = new Color(tarjeta.r, tarjeta.g, tarjeta.b, haloTarjeta.color.a);
        if (haloBoton != null) haloBoton.color = new Color(boton.r, boton.g, boton.b, haloBoton.color.a);
    }

    // Solo si cambia: tocar el color de un grafico quieto ensucia el canvas en cada cuadro.
    private static void PonerAlfa(UnityEngine.UI.Graphic grafico, float alfa)
    {
        Color color = grafico.color;
        if (Mathf.Abs(color.a - alfa) < 0.004f) return;
        color.a = alfa;
        grafico.color = color;
    }

    private void ActualizarBorde()
    {
        ActualizarHalos();
        if (borde == null) return;

        float alfa = 0f;
        if (Estado == EstadoMejora.Comprable)
            alfa = 0.15f + 0.35f * (0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 1.4f * (Time.unscaledTime + fase)));

        Color color = borde.color;
        if (Mathf.Approximately(color.a, alfa)) return;
        color.a = alfa;
        borde.color = color;
    }

    // Un brillo cruza el botón de tanto en tanto: cada 2,5 s si se puede comprar y
    // cada 3 s en el tope. Desfasado por tarjeta para que no crucen todas juntas.
    private void ActualizarBrillo()
    {
        if (brillo == null) return;

        float periodo = Estado == EstadoMejora.Comprable ? 2.5f
            : Estado == EstadoMejora.EnTope ? 3f
            : 0f;
        if (periodo <= 0f)
        {
            if (brillo.enabled) brillo.enabled = false;
            return;
        }

        float t = Mathf.Repeat(Time.unscaledTime + indice * 0.4f, periodo);
        bool cruzando = t < DuracionBarrido;
        if (brillo.enabled != cruzando) brillo.enabled = cruzando;
        if (!cruzando) return;

        float ancho = raizBoton != null ? raizBoton.rect.width : 320f;
        float extremo = ancho * 0.5f + brillo.rectTransform.rect.width + 20f;
        float x = Mathf.Lerp(-extremo, extremo, CurvasUI.SalidaCubica(t / DuracionBarrido));
        brillo.rectTransform.anchoredPosition = new Vector2(x, posicionBaseBrillo.y);
    }

    private void AplicarRelleno()
    {
        if (rellenoNivel == null) return;

        Vector2 anclaMax = rellenoNivel.anchorMax;
        anclaMax.x = relleno;
        rellenoNivel.anchorMax = anclaMax;
    }

    private void PonerColorRelleno(Color color)
    {
        if (rellenoNivel == null) return;

        var imagen = rellenoNivel.GetComponent<UnityEngine.UI.Image>();
        if (imagen != null) imagen.color = color;
    }

    private void PonerAlfaDestello(float alfa)
    {
        if (destello == null) return;

        Color color = destello.color;
        color.a = alfa;
        destello.color = color;
    }

    private static void ActivarSiHay(GameObject objeto, bool activo)
    {
        if (objeto != null && objeto.activeSelf != activo) objeto.SetActive(activo);
    }

    // Un toque que frena la fila mientras se desliza no compra. En uGUI ese toque hace las
    // dos cosas: el ScrollRect se frena (initializePotentialDrag le pone la velocidad en cero)
    // y, si el dedo no se arrastra, el mismo toque llega como click al botón que quedó
    // debajo, y la compra es en el acto y sin deshacer. Va en el objeto del botón, junto al
    // Button: el pointerDown se reparte entre los componentes del primero de la jerarquía que
    // lo atiende, y llega antes que initializePotentialDrag, cuando todavía se sabe si la fila
    // se movía. Le saca el click a ese toque y nada más, sin marcas que queden colgadas: el
    // siguiente compra.
    private class ToqueQueFrena : MonoBehaviour, IPointerDownHandler
    {
        public TarjetaMejora tarjeta;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (tarjeta != null && tarjeta.tienda != null && tarjeta.tienda.FilaEnMovimiento)
                eventData.eligibleForClick = false;
        }
    }
}
