using UnityEngine;

// Sigue al jugador con el desplazamiento con el que arranca la escena y tiembla
// cuando pasa algo fuerte (explosiones, muertes grandes, daño al jugador).
//
// El temblor es "trauma": cada evento suma entre 0 y 1, se descarga solo con el
// tiempo, y la sacudida crece con el cuadrado, asi los golpes chicos casi no se
// notan y los grandes si. Usa tiempo sin escalar para seguir durante la pausa de
// impacto, y se detiene en la pausa del menu.
public class CamaraJugador : MonoBehaviour
{
    public GameObject personaje;

    [Header("Temblor")]
    public float desplazamientoMaximo = 0.7f;   // metros, con trauma 1
    public float giroMaximo = 2.5f;             // grados de giro sobre el eje de la camara, con trauma 1
    public float frecuencia = 22f;
    public float descargaPorSegundo = 1.8f;

    // Al morir la camara se corre para que el cuerpo quede a un costado de la pantalla
    // (pedido de Ivan): la derrota y la ventanita de revivir van al centro, y el cuerpo
    // caia justo debajo de "+N MONEDAS". A la izquierda no hay nada en ninguna de las dos:
    // los textos de la derrota empiezan en el 31 % del ancho y la ventanita en el 33 %. La
    // horda festeja alrededor del cuerpo, en esa franja (EnemyController).
    [Header("Al morir")]
    [Tooltip("Donde queda el cuerpo en la pantalla, de 0 (el borde izquierdo) a 1 (el derecho).")]
    public float cuerpoEnLaPantalla = 0.22f;
    [Tooltip("Lo que tarda en correrse, en segundos sin escalar: la oferta de revivir congela el juego.")]
    public float duracionDelCorrimiento = 0.8f;

    private static CamaraJugador activa;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetearEstadoCompartido()
    {
        activa = null;
    }

    public static void Temblar(float trauma)
    {
        if (activa != null) activa.trauma = Mathf.Min(1f, activa.trauma + trauma);
    }

    // Lo llama el jugador al caer (true) y al levantarse (false).
    public static void MostrarElCuerpo(bool mostrar)
    {
        if (activa == null) return;
        if (mostrar && !activa.mostrandoElCuerpo) activa.desvio = activa.DesvioParaElCuerpo();
        activa.mostrandoElCuerpo = mostrar;
    }

    // Cuanto va corrida, de 0 a 1. Para las pruebas.
    public static float Corrimiento { get { return activa != null ? activa.corrimiento : 0f; } }

    private Vector3 posicionRelativa;
    private Quaternion rotacionBase;
    private float trauma;
    private bool mostrandoElCuerpo;
    private float corrimiento;
    private Vector3 desvio;

    // Lo que hay que mover la camara para que lo que hoy esta en el centro de la pantalla
    // (el jugador) quede en cuerpoEnLaPantalla: la distancia, sobre el piso, entre el
    // centro y ese punto. Sale de la camara y no de un numero fijo, asi vale en 16:9 y en
    // 20:9.
    private Vector3 DesvioParaElCuerpo()
    {
        var camara = GetComponentInChildren<Camera>();
        if (camara == null) camara = Camera.main;
        if (camara == null || personaje == null) return Vector3.zero;
        var piso = new Plane(Vector3.up, new Vector3(0f, personaje.transform.position.y, 0f));
        Vector3 centro, costado;
        if (!PuntoEnElPiso(camara, piso, new Vector3(0.5f, 0.5f, 0f), out centro) ||
            !PuntoEnElPiso(camara, piso, new Vector3(cuerpoEnLaPantalla, 0.5f, 0f), out costado))
            return Vector3.zero;
        Vector3 desvio = centro - costado;
        desvio.y = 0f;
        return desvio;
    }

    private static bool PuntoEnElPiso(Camera camara, Plane piso, Vector3 enLaPantalla, out Vector3 punto)
    {
        Ray rayo = camara.ViewportPointToRay(enLaPantalla);
        float distancia;
        punto = Vector3.zero;
        if (!piso.Raycast(rayo, out distancia)) return false;
        punto = rayo.GetPoint(distancia);
        return true;
    }

    private void Awake()
    {
        activa = this;
    }

    private void OnDestroy()
    {
        if (activa == this) activa = null;
    }

    private void Start()
    {
        posicionRelativa = transform.position - personaje.transform.position;
        rotacionBase = transform.rotation;
    }

    private void Update()
    {
        // Al morir, PlayerHealth destruye al jugador y recien despues carga la
        // escena de Perdiste. En ese hueco esto seguia leyendo un objeto muerto y
        // tiraba MissingReferenceException en cada frame.
        if (personaje == null) return;

        Vector3 posicion = personaje.transform.position + posicionRelativa;
        Quaternion rotacion = rotacionBase;

        // El corrimiento al morir, suave de punta a punta, en tiempo sin escalar.
        float haciaDonde = mostrandoElCuerpo ? 1f : 0f;
        if (corrimiento != haciaDonde)
            corrimiento = Mathf.MoveTowards(corrimiento, haciaDonde, Time.unscaledDeltaTime / Mathf.Max(0.01f, duracionDelCorrimiento));
        if (corrimiento > 0f) posicion += desvio * Mathf.SmoothStep(0f, 1f, corrimiento);

        if (trauma > 0f && !MenuPausa.Pausado)
        {
            trauma = Mathf.Max(0f, trauma - descargaPorSegundo * Time.unscaledDeltaTime);
            float fuerza = trauma * trauma;
            float t = Time.unscaledTime * frecuencia;
            float x = Mathf.PerlinNoise(t, 0.3f) * 2f - 1f;
            float y = Mathf.PerlinNoise(0.7f, t) * 2f - 1f;
            float giro = Mathf.PerlinNoise(t, t + 5f) * 2f - 1f;

            posicion += (rotacionBase * new Vector3(x, y, 0f)) * (desplazamientoMaximo * fuerza);
            rotacion = rotacionBase * Quaternion.Euler(0f, 0f, giro * giroMaximo * fuerza);
        }

        transform.SetPositionAndRotation(posicion, rotacion);
    }
}
