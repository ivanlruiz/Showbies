using TMPro;
using UnityEngine;

// El nombre del juego como parte del fondo del menu: letras 3D al fondo del piso,
// detras de los zombis de FondoMenu, que se esconden a medias en la niebla y vuelven
// a salir. Pedido de Ivan.
//
// La niebla de Unity no toca a TextMeshPro (su shader no la tiene), asi que la niebla
// del titulo es a mano: el color de las letras va hacia el del cielo. Nunca llega a
// taparlo entero: el nombre tiene que leerse, tambien en las capturas de la ficha.
//
// El cielo sale de FondoMenu.CieloActual y no de un color propio: con el modo oscuro el
// fondo se hace de noche, y unas letras que se esconden hacia el celeste quedarian como
// un halo claro sobre el cielo oscuro.
public class TituloEnLaNiebla : MonoBehaviour
{
    public TextMeshPro texto;
    public Color colorLetras = new Color(1f, 0.83f, 0.2f, 1f);
    public Color colorContorno = new Color(0.17f, 0.29f, 0.1f, 1f);
    [Range(0f, 0.5f)] public float grosorContorno = 0.2f;

    [Range(0f, 1f)] public float nieblaMinima = 0.05f;
    [Range(0f, 1f)] public float nieblaMaxima = 0.55f;
    public float periodo = 7f;              // segundos de una ida y vuelta de la niebla
    public float duracionEntrada = 2.2f;    // al abrir el menu sale de la niebla entera
    public float amplitudFlotado = 0.12f;   // sube y baja apenas, en metros

    private Vector3 posicionBase;
    private Material material;
    private float inicio;

    private void Start()
    {
        if (texto == null) texto = GetComponent<TextMeshPro>();
        posicionBase = transform.localPosition;
        // Copia propia del material (fontMaterial): el contorno se desvanece con la niebla
        // y no tiene que tocar a los demas textos con Bangers.
        if (texto != null)
        {
            material = texto.fontMaterial;
            material.SetFloat(ShaderUtilities.ID_OutlineWidth, grosorContorno);
        }
        inicio = Time.unscaledTime;
        Pintar(1f);
    }

    private void OnDestroy()
    {
        if (material != null) Destroy(material);
    }

    private void Update()
    {
        float t = Time.unscaledTime - inicio;
        float onda = 0.5f - 0.5f * Mathf.Cos(t * Mathf.PI * 2f / Mathf.Max(0.1f, periodo));
        float niebla = Mathf.Lerp(nieblaMinima, nieblaMaxima, onda);

        // La entrada: de niebla entera a la de la onda, con salida suave.
        float entrada = Mathf.Clamp01(t / Mathf.Max(0.01f, duracionEntrada));
        entrada = 1f - (1f - entrada) * (1f - entrada);
        Pintar(Mathf.Lerp(1f, niebla, entrada));

        transform.localPosition = posicionBase + Vector3.up * (Mathf.Sin(t * 0.9f) * amplitudFlotado);
    }

    private void Pintar(float niebla)
    {
        if (texto == null) return;
        Color cielo = FondoMenu.CieloActual;
        texto.color = Color.Lerp(colorLetras, cielo, niebla);
        if (material != null) material.SetColor(ShaderUtilities.ID_OutlineColor, Color.Lerp(colorContorno, cielo, niebla));
    }
}
