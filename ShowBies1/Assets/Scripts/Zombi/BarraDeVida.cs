using UnityEngine;

// Barra de vida sobre un zombi. La crea EnemyController con el primer golpe que
// no lo mata, asi que los zombis que mueren de un tiro nunca la muestran.
//
// Es un objeto aparte que sigue al zombi, no un hijo: los zombis rotan hacia el
// jugador y tienen escalas distintas (el jefe x2, el FASTER x0.3), y una barra
// hija heredaria las dos cosas. Son dos SpriteRenderer y no un Canvas: con
// decenas de zombis, un canvas por barra cuesta mucho mas en el telefono.
public class BarraDeVida : MonoBehaviour
{
    public float ancho = 1.2f;
    public float alto = 0.14f;
    public float borde = 0.025f;
    public float margenSobreLaCabeza = 0.35f;

    // Un sprite blanco compartido por todas las barras, hecho a partir de la
    // textura blanca del engine: no hace falta ningun asset.
    private static Sprite blanco;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetearEstadoCompartido()
    {
        blanco = null;
    }

    private EnemyController zombi;
    private SpriteRenderer relleno;
    private float altura;

    public static BarraDeVida Crear(EnemyController zombi)
    {
        if (blanco == null)
        {
            var textura = Texture2D.whiteTexture;
            blanco = Sprite.Create(textura, new Rect(0f, 0f, textura.width, textura.height), new Vector2(0f, 0.5f), textura.width);
        }

        var barra = new GameObject("BarraDeVida").AddComponent<BarraDeVida>();
        barra.zombi = zombi;
        barra.altura = AlturaDeLaCabeza(zombi) + barra.margenSobreLaCabeza;

        var fondo = barra.Capa("Fondo", new Color(0f, 0f, 0f, 0.6f), 0);
        fondo.transform.localPosition = new Vector3(-barra.ancho / 2f, 0f, 0f);
        fondo.transform.localScale = new Vector3(barra.ancho, barra.alto, 1f);

        // Un poco hacia la camara ademas del sortingOrder, para que el relleno
        // quede delante del fondo aunque el orden de transparentes lo ignore.
        barra.relleno = barra.Capa("Relleno", Color.green, 1);
        barra.relleno.transform.localPosition = new Vector3(-barra.ancho / 2f + barra.borde, 0f, -0.01f);

        barra.Seguir();
        return barra;
    }

    public void Mostrar(float fraccion)
    {
        fraccion = Mathf.Clamp01(fraccion);
        relleno.transform.localScale = new Vector3((ancho - 2f * borde) * fraccion, alto - 2f * borde, 1f);
        relleno.color = Color.HSVToRGB(fraccion / 3f, 0.85f, 0.95f);   // verde con la vida llena, rojo casi muerto
    }

    private void LateUpdate()
    {
        if (zombi == null)
        {
            Destroy(gameObject);
            return;
        }
        Seguir();
    }

    private void Seguir()
    {
        transform.position = zombi.transform.position + Vector3.up * altura;
        var camara = Camera.main;
        if (camara != null) transform.rotation = camara.transform.rotation;
    }

    private SpriteRenderer Capa(string nombre, Color color, int orden)
    {
        var capa = new GameObject(nombre);
        capa.transform.SetParent(transform, false);
        var sprite = capa.AddComponent<SpriteRenderer>();
        sprite.sprite = blanco;
        sprite.color = color;
        sprite.sortingOrder = orden;
        return sprite;
    }

    // Cuanto sobresale el zombi por encima de su pivote, mirando sus renderers
    // prendidos: los modelos de ToonyTiny y los zombis de primitivas miden
    // distinto, y el jefe esta escalado.
    private static float AlturaDeLaCabeza(EnemyController zombi)
    {
        bool hay = false;
        var limites = new Bounds();
        foreach (var r in zombi.GetComponentsInChildren<Renderer>())
        {
            if (!r.enabled) continue;
            if (!hay)
            {
                limites = r.bounds;
                hay = true;
            }
            else
            {
                limites.Encapsulate(r.bounds);
            }
        }
        return hay ? limites.max.y - zombi.transform.position.y : 2f;
    }
}
