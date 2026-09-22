using TMPro;
using UnityEngine;

// Un numero de daño que salta desde el zombi: aparece con un golpe de escala,
// sube frenando, y se achica y desvanece al final. Mira siempre a la camara.
// Los reusa Efectos desde un pool: con el arma a full salen varios por segundo.
[RequireComponent(typeof(TextMeshPro))]
public class NumeroFlotante : MonoBehaviour
{
    public float duracion = 0.7f;
    public float velocidadInicial = 5f;       // hacia arriba, en m/s
    public float gravedad = 10f;
    public float escalaBase = 0.55f;          // achicados a pedido de Ivan: tapaban la partida
    public float dañoParaEscalaMaxima = 50f;  // desde este daño sale al doble de tamaño y con colorFuerte
    public Color colorDebil = Color.white;
    public Color colorFuerte = new Color(1f, 0.8f, 0.1f);
    public Color colorCritico = new Color(1f, 0.22f, 0.12f);
    [Tooltip("Cuanto crece el numero entre el danio mas chico y el mas grande. Era 1 (el doble).")]
    public float crecimientoPorDanio = 0.6f;
    public float escalaCritico = 1.1f;              // sobre la escala que le toca por el daño

    [System.NonSerialized] public System.Action<NumeroFlotante> alTerminar;

    private TextMeshPro texto;
    private float nacio;
    private Vector3 velocidad;
    private float escala;
    private Color color;

    private void Awake()
    {
        texto = GetComponent<TextMeshPro>();
    }

    public void Mostrar(Vector3 punto, int valor, bool critico = false)
    {
        float fuerza = Mathf.Clamp01(valor / dañoParaEscalaMaxima);
        color = critico ? colorCritico : Color.Lerp(colorDebil, colorFuerte, fuerza);
        // El crecimiento por danio ya no llega al doble: con las mejoras, el danio pasa
        // enseguida de danioParaEscalaMaxima y TODOS los numeros salian del tamanio maximo,
        // asi que lo unico que hacia era tapar la partida.
        escala = escalaBase * (1f + crecimientoPorDanio * fuerza) * (critico ? escalaCritico : 1f);

        transform.position = punto + new Vector3(Random.Range(-0.3f, 0.3f), 0f, Random.Range(-0.3f, 0.3f));
        velocidad = new Vector3(Random.Range(-1.2f, 1.2f), velocidadInicial, 0f);
        nacio = Time.time;

        if (critico) texto.SetText("{0}!", valor);
        else texto.SetText("{0}", valor);
        gameObject.SetActive(true);
        Animar(0f);
    }

    private void Update()
    {
        float t = (Time.time - nacio) / duracion;
        if (t >= 1f)
        {
            gameObject.SetActive(false);
            if (alTerminar != null) alTerminar(this);
            return;
        }

        velocidad.y -= gravedad * Time.deltaTime;
        transform.position += velocidad * Time.deltaTime;
        Animar(t);
    }

    private void Animar(float t)
    {
        // Golpe de escala al nacer y achique en el ultimo tercio.
        float golpe = t < 0.1f ? Mathf.Lerp(0.3f, 1.35f, t / 0.1f) : Mathf.Lerp(1.35f, 1f, Mathf.Clamp01((t - 0.1f) / 0.15f));
        float final = t > 0.65f ? 1f - (t - 0.65f) / 0.35f : 1f;
        transform.localScale = Vector3.one * (escala * golpe * Mathf.Lerp(0.5f, 1f, final));

        Color c = color;
        c.a = final;
        texto.color = c;

        Camera camara = Camera.main;
        if (camara != null) transform.rotation = camara.transform.rotation;
    }
}
