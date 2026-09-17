using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// El fondo vivo del menu: el piso de la partida y zombis del juego cruzando la
// pantalla de un lado al otro, detras de los botones. Pedido de Ivan, para que el
// menu no sea un verde liso y se vea de entrada de que va el juego.
//
// Los zombis son los prefabs de verdad sin nada de su logica: se instancian dentro
// de un objeto apagado y se les borran scripts, fisica y colliders antes de
// prenderlos, asi no cuentan en ZombisVivos ni buscan al jugador. Caminan con su
// Animator al mismo ritmo relativo que en la partida (EnemyController.velocidadDeAnimacion).
//
// La camara del menu se acomoda aca (mirando un poco desde arriba, fondo del color
// del piso): la imagen BG del canvas quedo transparente para que esto se vea.
public class FondoMenu : MonoBehaviour
{
    [System.Serializable]
    public class ZombiDelFondo
    {
        public GameObject prefab;
        public float peso = 1f;
    }

    public ZombiDelFondo[] zombis;
    public Material materialPiso;
    public Color colorCielo = new Color(0.07f, 0.2f, 0.06f, 1f);

    [Header("Camara")]
    public Vector3 posicionCamara = new Vector3(0f, 3.4f, -6.5f);
    public Vector3 rotacionCamara = new Vector3(17f, 0f, 0f);
    public float intensidadLuz = 1.25f;

    [Header("Ritmo")]
    public float intervaloMinimo = 1.4f;
    public float intervaloMaximo = 2.8f;
    public int maximoALaVez = 8;
    public float factorVelocidad = 0.45f;     // mas lento que en la partida: es un fondo
    public float profundidadMinima = 0.5f;    // distancia en Z del recorrido
    public float profundidadMaxima = 7f;
    public float anchoDelRecorrido = 11f;     // desde x = -ancho hasta x = +ancho

    private class Caminante
    {
        public Transform transform;
        public Vector3 direccion;
        public float velocidad;
        public float limite;
    }

    private readonly List<Caminante> caminantes = new List<Caminante>();
    private float proximo;
    private static readonly FieldInfo campoAnimacion =
        typeof(EnemyController).GetField("velocidadDeAnimacion", BindingFlags.NonPublic | BindingFlags.Instance);

    private void Start()
    {
        var camara = Camera.main;
        if (camara != null)
        {
            camara.transform.position = posicionCamara;
            camara.transform.rotation = Quaternion.Euler(rotacionCamara);
            camara.clearFlags = CameraClearFlags.SolidColor;
            camara.backgroundColor = colorCielo;
        }

        // Niebla del color del cielo: el piso se funde con el fondo y no se ve donde termina.
        // Las RenderSettings son de la escena, asi que no pasan a la partida.
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = colorCielo;
        RenderSettings.fogStartDistance = 14f;
        RenderSettings.fogEndDistance = 45f;

        // La luz direccional de la escena se reacomoda para los zombis; si no hay, una propia.
        Light luz = null;
        foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (l.type == LightType.Directional) { luz = l; break; }
        if (luz == null)
        {
            luz = new GameObject("LuzDelFondo").AddComponent<Light>();
            luz.transform.SetParent(transform, false);
            luz.type = LightType.Directional;
        }
        luz.intensity = intensidadLuz;
        luz.shadows = LightShadows.Soft;
        luz.transform.rotation = Quaternion.Euler(50f, -25f, 0f);

        if (materialPiso != null)
        {
            var piso = GameObject.CreatePrimitive(PrimitiveType.Plane);
            piso.name = "PisoDelFondo";
            Destroy(piso.GetComponent<Collider>());
            piso.transform.SetParent(transform, false);
            piso.transform.localScale = new Vector3(12f, 1f, 12f);
            piso.transform.position = new Vector3(0f, 0f, 45f);
            piso.GetComponent<Renderer>().sharedMaterial = materialPiso;
        }

        // Unos cuantos ya en camino, para que el menu no arranque vacio.
        for (int i = 0; i < 4; i++) Soltar(Random.Range(-0.7f, 0.7f));
        proximo = Time.unscaledTime + Random.Range(intervaloMinimo, intervaloMaximo);
    }

    private void Update()
    {
        // Tiempo sin escalar: el menu no se pausa, pero timeScale puede venir tocado.
        float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
        for (int i = caminantes.Count - 1; i >= 0; i--)
        {
            var c = caminantes[i];
            if (c.transform == null) { caminantes.RemoveAt(i); continue; }
            c.transform.position += c.direccion * (c.velocidad * dt);
            if (Mathf.Abs(c.transform.position.x) > c.limite)
            {
                Destroy(c.transform.gameObject);
                caminantes.RemoveAt(i);
            }
        }

        if (Time.unscaledTime >= proximo)
        {
            if (caminantes.Count < maximoALaVez) Soltar(-1f);
            proximo = Time.unscaledTime + Random.Range(intervaloMinimo, intervaloMaximo);
        }
    }

    // arrancaEn: -1 = desde el borde; entre -1 y 1 = ya en esa fraccion del recorrido.
    private void Soltar(float arrancaEn)
    {
        var prefab = Elegir();
        if (prefab == null) return;

        var enemigo = prefab.GetComponent<EnemyController>();
        float velocidadJuego = enemigo != null && enemigo.enemyType != null ? enemigo.enemyType.velocidad : 5f;
        float animacion = enemigo != null && campoAnimacion != null ? (float)campoAnimacion.GetValue(enemigo) : 1f;

        // Dentro de un padre apagado: nada de Awake ni OnEnable hasta borrarle la logica.
        var caja = new GameObject("ZombiDelFondo");
        caja.SetActive(false);
        caja.transform.SetParent(transform, false);
        var zombi = Instantiate(prefab, caja.transform);

        float z = Random.Range(profundidadMinima, profundidadMaxima);
        bool haciaLaDerecha = Random.value < 0.5f;
        float x = arrancaEn < -0.99f
            ? (haciaLaDerecha ? -anchoDelRecorrido : anchoDelRecorrido)
            : arrancaEn * anchoDelRecorrido;
        zombi.transform.position = new Vector3(x, 0f, z);
        var direccion = haciaLaDerecha ? Vector3.right : Vector3.left;
        zombi.transform.rotation = Quaternion.LookRotation(direccion);

        // Los pies en el piso: el fondo de la capsula, como SubirSobreElPiso.
        var capsula = zombi.GetComponent<CapsuleCollider>();
        if (capsula != null)
        {
            float alto = capsula.direction == 1 ? Mathf.Max(capsula.height * 0.5f, capsula.radius) : capsula.radius;
            float fondo = zombi.transform.TransformPoint(capsula.center).y - alto * Mathf.Abs(zombi.transform.lossyScale.y);
            zombi.transform.position += Vector3.up * -fondo;
        }

        foreach (var s in zombi.GetComponentsInChildren<MonoBehaviour>(true)) DestroyImmediate(s);
        foreach (var co in zombi.GetComponentsInChildren<Collider>(true)) DestroyImmediate(co);
        foreach (var rb in zombi.GetComponentsInChildren<Rigidbody>(true)) DestroyImmediate(rb);

        zombi.transform.SetParent(transform, true);
        Destroy(caja);
        foreach (var a in zombi.GetComponentsInChildren<Animator>(true)) a.speed = animacion * factorVelocidad;
        zombi.SetActive(true);

        caminantes.Add(new Caminante
        {
            transform = zombi.transform,
            direccion = direccion,
            velocidad = velocidadJuego * factorVelocidad,
            limite = anchoDelRecorrido + 1f
        });
    }

    private GameObject Elegir()
    {
        if (zombis == null || zombis.Length == 0) return null;
        float total = 0f;
        foreach (var z in zombis) if (z != null && z.prefab != null) total += Mathf.Max(0f, z.peso);
        float r = Random.value * total;
        foreach (var z in zombis)
        {
            if (z == null || z.prefab == null) continue;
            r -= Mathf.Max(0f, z.peso);
            if (r <= 0f) return z.prefab;
        }
        return zombis[0].prefab;
    }
}
