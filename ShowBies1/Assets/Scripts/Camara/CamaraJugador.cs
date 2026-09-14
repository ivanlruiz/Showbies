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

    private Vector3 posicionRelativa;
    private Quaternion rotacionBase;
    private float trauma;

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
