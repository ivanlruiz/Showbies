using TMPro;
using UnityEngine;

// El nombre del juego como parte del fondo del menu: al fondo del piso, detras de los
// zombis de FondoMenu, escondiendose a medias en la niebla y volviendo a salir. Pedido
// de Ivan.
//
// Hoy es el LOGO (un quad con Materiales/LogoMenu.mat, ver Shaders/LogoEnLaNiebla), y
// antes era el nombre escrito con un TextMeshPro 3D. Los dos caminos siguen: si `logo`
// esta puesto manda ese, y si no cae al texto.
//
// La niebla es a mano y no la de Unity: ni el shader de TextMeshPro la tiene ni serviria,
// porque tiene que ir al color del cielo de FondoMenu y no al de la escena. Nunca llega
// a taparlo entero: el nombre tiene que leerse, tambien en las capturas de la ficha.
//
// El cielo sale de FondoMenu.CieloActual y no de un color propio: con el modo oscuro el
// fondo se hace de noche, y un logo que se esconde hacia el celeste quedaria como un
// halo claro sobre el cielo oscuro.
public class TituloEnLaNiebla : MonoBehaviour
{
    [Tooltip("El quad del logo. Si esta puesto, se usa este y no el texto.")]
    public Renderer logo;
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

    private static readonly int IdNiebla = Shader.PropertyToID("_Niebla");
    private static readonly int IdColorNiebla = Shader.PropertyToID("_ColorNiebla");

    private void Start()
    {
        posicionBase = transform.localPosition;
        if (logo != null)
        {
            // Copia propia del material: la niebla es de este objeto y el .mat es
            // compartido.
            material = logo.material;
        }
        else
        {
            if (texto == null) texto = GetComponent<TextMeshPro>();
            // Copia propia del material (fontMaterial): el contorno se desvanece con la
            // niebla y no tiene que tocar a los demas textos con Bangers.
            if (texto != null)
            {
                material = texto.fontMaterial;
                material.SetFloat(ShaderUtilities.ID_OutlineWidth, grosorContorno);
            }
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
        Color cielo = FondoMenu.CieloActual;
        if (logo != null)
        {
            // El shader mezcla la imagen con el color del cielo; el alfa no se toca, asi
            // que lo que se esconde es el color y no la silueta.
            if (material == null) return;
            material.SetColor(IdColorNiebla, cielo);
            material.SetFloat(IdNiebla, niebla);
            return;
        }

        if (texto == null) return;
        texto.color = Color.Lerp(colorLetras, cielo, niebla);
        if (material != null) material.SetColor(ShaderUtilities.ID_OutlineColor, Color.Lerp(colorContorno, cielo, niebla));
    }
}
