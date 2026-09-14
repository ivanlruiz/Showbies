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
    public float escalaBase = 1f;
    public float dañoParaEscalaMaxima = 50f;  // desde este daño sale al doble de tamaño y con colorFuerte
    public Color colorDebil = Color.white;
    public Color colorFuerte = new Color(1f, 0.8f, 0.1f);

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

    public void Mostrar(Vector3 punto, int valor)
    {
        float fuerza = Mathf.Clamp01(valor / dañoParaEscalaMaxima);
        color = Color.Lerp(colorDebil, colorFuerte, fuerza);
        escala = escalaBase * (1f + fuerza);

        transform.position = punto + new Vector3(Random.Range(-0.3f, 0.3f), 0f, Random.Range(-0.3f, 0.3f));
        velocidad = new Vector3(Random.Range(-1.2f, 1.2f), velocidadInicial, 0f);
        nacio = Time.time;

        texto.SetText("{0}", valor);
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
        float golpe = t < 0.1f ? Mathf.Lerp(0.3f, 1.5f, t / 0.1f) : Mathf.Lerp(1.5f, 1f, Mathf.Clamp01((t - 0.1f) / 0.15f));
        float final = t > 0.65f ? 1f - (t - 0.65f) / 0.35f : 1f;
        transform.localScale = Vector3.one * (escala * golpe * Mathf.Lerp(0.5f, 1f, final));

        Color c = color;
        c.a = final;
        texto.color = c;

        Camera camara = Camera.main;
        if (camara != null) transform.rotation = camara.transform.rotation;
    }
}
