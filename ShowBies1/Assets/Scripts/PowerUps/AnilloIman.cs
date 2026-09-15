using UnityEngine;

// El alcance del iman dibujado en el piso, alrededor del jugador: un anillo del
// color de las monedas que aparece cuando hay monedas sueltas en la escena y late
// cada vez que se agarra una. Hace visible la mejora de iman: comprarla agranda el
// anillo, y el jugador ve hasta donde tiene que acercarse para juntarlas. Sin la
// mejora comprada no hay iman y no se dibuja.
//
// Va en la raiz de Jugador.prefab. Crea su LineRenderer al empezar, en un hijo con
// los puntos en espacio de mundo, asi no le afectan la rotacion ni la escala del
// jugador. El alcance lo lee de Moneda, que lo fija AplicarMejoras.
[DisallowMultipleComponent]
public class AnilloIman : MonoBehaviour
{
    [Tooltip("Sprites-Default, como el indicador de la granada: respeta el alfa del color.")]
    public Material material;
    public Color color = new Color(1f, 0.8f, 0.15f, 1f);
    public float ancho = 0.12f;                                   // como el indicador de la granada: mas fino no se lee en el telefono
    public int segmentos = 64;
    public float altura = 0.06f;                                  // apenas sobre el piso y las calles

    [Header("Visibilidad")]
    [Range(0f, 1f)] public float alfaConMonedas = 0.5f;           // con monedas sueltas en la escena
    [Range(0f, 1f)] public float alfaSinMonedas = 0f;
    public float segundosDeFundido = 0.35f;                       // de invisible a alfaConMonedas

    [Header("Latido")]
    public float respiracion = 0.025f;                            // cuanto sube y baja el radio, en fraccion
    public float velocidadRespiracion = 3f;
    public float crecimientoAlCobrar = 0.12f;                     // fraccion que crece el radio con cada moneda
    public float anchoAlCobrar = 1.8f;                            // factor del trazo con cada moneda
    [Range(0f, 1f)] public float brilloAlCobrar = 0.4f;           // alfa que suma con cada moneda
    public float duracionLatido = 0.3f;

    private LineRenderer linea;
    private Vector3[] puntos;
    private Vector2[] circulo;                                    // coseno y seno de cada punto, calculados una vez
    private float alfa;
    private float latido;                                         // 1 al agarrar una moneda, baja hasta 0
    private float tiempo;
    private int cobrosVistos;

    // El que fijo AplicarMejoras al empezar la partida; sin partida, el del catalogo.
    public static float RadioActual
    {
        get { return Moneda.RadioImanDeLaPartida >= 0f ? Moneda.RadioImanDeLaPartida : CatalogoMejoras.RadioIman; }
    }

    // Lo que el anillo dibuja ahora, para inspeccionarlo desde el editor.
    public LineRenderer Linea { get { return linea; } }
    public float RadioDibujado { get; private set; }

    private void Awake()
    {
        if (material == null)
        {
            Debug.LogWarning("AnilloIman: falta el material; el anillo no se dibuja.", this);
            enabled = false;
            return;
        }

        segmentos = Mathf.Max(8, segmentos);
        puntos = new Vector3[segmentos];
        circulo = new Vector2[segmentos];
        for (int i = 0; i < segmentos; i++)
        {
            float angulo = i * Mathf.PI * 2f / segmentos;
            circulo[i] = new Vector2(Mathf.Cos(angulo), Mathf.Sin(angulo));
        }

        var hijo = new GameObject("AnilloIman");
        hijo.transform.SetParent(transform, false);
        linea = hijo.AddComponent<LineRenderer>();
        linea.sharedMaterial = material;
        linea.useWorldSpace = true;
        linea.loop = true;
        linea.alignment = LineAlignment.View;
        linea.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        linea.receiveShadows = false;
        // Debajo de las manchas de sangre, las chispas y las barras de vida. Todos
        // usan materiales transparentes que no escriben profundidad, asi que la
        // altura no ordena nada: sin esto, una mancha entre el jugador y la camara
        // tapaba el anillo y una del otro lado quedaba debajo.
        linea.sortingOrder = -1;
        linea.positionCount = segmentos;
        linea.enabled = false;

        // Las monedas que se cobraron antes de esta partida no laten.
        cobrosVistos = Moneda.Cobros;
    }

    private void LateUpdate()
    {
        // Tiempo sin escalar y topeado: la pausa de impacto baja timeScale y el
        // anillo tiene que seguir latiendo; en la pausa del menu se congela.
        float dt = MenuPausa.Pausado ? 0f : Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);
        tiempo += dt;

        // Sin la mejora de iman no hay alcance que mostrar: las monedas se agarran
        // pasandoles por encima. Los cobros de mientras no quedan pendientes de latir.
        if (RadioActual <= 0f)
        {
            cobrosVistos = Moneda.Cobros;
            alfa = 0f;
            latido = 0f;
            if (linea.enabled) linea.enabled = false;
            return;
        }

        if (Moneda.Cobros != cobrosVistos)
        {
            cobrosVistos = Moneda.Cobros;
            latido = 1f;
        }
        latido = Mathf.Max(0f, latido - dt / Mathf.Max(0.01f, duracionLatido));

        float objetivo = Moneda.MonedasEnEscena > 0 ? alfaConMonedas : alfaSinMonedas;
        alfa = Mathf.MoveTowards(alfa, objetivo, dt * alfaConMonedas / Mathf.Max(0.01f, segundosDeFundido));

        // Sube de golpe al cobrar y vuelve suave.
        float golpe = latido * latido;
        float brillo = Mathf.Clamp01(alfa + brilloAlCobrar * golpe);
        if (brillo <= 0.001f)
        {
            if (linea.enabled) linea.enabled = false;
            return;
        }
        if (!linea.enabled) linea.enabled = true;

        RadioDibujado = RadioActual * (1f + respiracion * Mathf.Sin(tiempo * velocidadRespiracion) + crecimientoAlCobrar * golpe);

        Color c = color;
        c.a = brillo;
        linea.startColor = c;
        linea.endColor = c;
        linea.widthMultiplier = ancho * (1f + (anchoAlCobrar - 1f) * golpe);

        Vector3 centro = transform.position;
        for (int i = 0; i < puntos.Length; i++)
        {
            puntos[i] = new Vector3(centro.x + circulo[i].x * RadioDibujado, altura, centro.z + circulo[i].y * RadioDibujado);
        }
        linea.SetPositions(puntos);
    }
}
