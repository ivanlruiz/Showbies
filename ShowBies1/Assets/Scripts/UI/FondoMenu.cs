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
// Animator como en la partida: el paso (EnemyController.velocidadDeAnimacion) y si
// caminan o corren (ritmoDeAndar), por los parametros Paso y Ritmo del controller.
//
// La camara del menu se acomoda aca (mirando un poco desde arriba, fondo del color
// del piso): la imagen BG del canvas quedo transparente para que esto se vea.
//
// **Con el modo oscuro el fondo pasa a la noche** (pedido de Ivan: si no, el menu
// quedaba igual y el interruptor parecia no hacer nada): el cielo, la luz, la luz
// ambiente y la niebla se funden en 0,7 s y el pasto pasa a la tierra del cementerio,
// con la misma paleta que el capitulo 2 de las oleadas (CapitulosDeEscenario). Las
// partidas siguen de dia. El color del cielo de ahora queda en CieloActual, que es lo
// que usa el titulo para esconderse en la niebla.
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
    public Color colorCielo = new Color(0.66f, 0.86f, 0.96f, 1f);

    [Header("La noche, con el modo oscuro")]
    public Material materialPisoNoche;
    public Color colorCieloNoche = new Color(0.07f, 0.09f, 0.17f, 1f);
    public Color colorLuzNoche = new Color(0.55f, 0.66f, 1f, 1f);
    public float intensidadLuzNoche = 0.5f;
    public Vector3 rotacionLuzNoche = new Vector3(55f, 200f, 0f);
    public Color ambienteNoche = new Color(0.2f, 0.23f, 0.34f, 1f);
    public float duracionFundido = 0.7f;

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

    // El cielo de ahora, para el titulo: la niebla de TextMeshPro es a mano y tiene que
    // ir al mismo color (ver TituloEnLaNiebla).
    public static Color CieloActual { get; private set; }

    private Camera camara;
    private Light luz;
    private Renderer pisoRenderer;
    private Color luzDia = Color.white;
    private float intensidadDia = 1.25f;
    private Quaternion rotacionDia = Quaternion.identity;
    private Color ambienteDia = Color.gray;
    private float mezcla;          // 0 de dia, 1 de noche
    private float objetivo;
    private int revisionTema = -1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetearEstadoCompartido()
    {
        CieloActual = new Color(0.66f, 0.86f, 0.96f, 1f);
    }
    private static readonly FieldInfo campoAnimacion =
        typeof(EnemyController).GetField("velocidadDeAnimacion", BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly FieldInfo campoRitmo =
        typeof(EnemyController).GetField("ritmoDeAndar", BindingFlags.NonPublic | BindingFlags.Instance);

    // Los del controller de los zombis (ConstructorAnimaciones), como hash, igual que en
    // EnemyController.
    private static readonly int idPaso = Animator.StringToHash("Paso");
    private static readonly int idRitmo = Animator.StringToHash("Ritmo");

    private void Start()
    {
        camara = Camera.main;
        if (camara != null)
        {
            camara.transform.position = posicionCamara;
            camara.transform.rotation = Quaternion.Euler(rotacionCamara);
            camara.clearFlags = CameraClearFlags.SolidColor;
            camara.backgroundColor = colorCielo;
            // Como las camaras de las escenas de juego: sin post-proceso, HDR y MSAA no aportan
            // nada, y el HDR (donde el tier del telefono lo deja) es dibujar en un buffer de 16
            // bits por canal y copiarlo a la pantalla en cada cuadro, en la pantalla donde mas
            // rato se pasa entre partidas. En la escena siguen prendidos: se apagan aca.
            camara.allowHDR = false;
            camara.allowMSAA = false;
        }

        // Niebla del color del cielo: el piso se funde con el fondo y no se ve donde termina.
        // Las RenderSettings son de la escena, asi que no pasan a la partida.
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = colorCielo;
        RenderSettings.fogStartDistance = 14f;
        RenderSettings.fogEndDistance = 45f;

        // La luz direccional de la escena se reacomoda para los zombis; si no hay, una propia.
        luz = null;
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
            pisoRenderer = piso.GetComponent<Renderer>();
            pisoRenderer.sharedMaterial = materialPiso;
        }

        // Lo del dia, para volver: la luz ya quedo acomodada arriba.
        if (luz != null)
        {
            luzDia = luz.color;
            intensidadDia = luz.intensity;
            rotacionDia = luz.transform.rotation;
        }
        ambienteDia = RenderSettings.ambientLight;

        // Si ya estaba en modo oscuro, el menu abre de noche, sin fundido.
        revisionTema = Tema.Revision;
        mezcla = objetivo = Tema.Oscuro ? 1f : 0f;
        Aplicar();

        // Unos cuantos ya en camino, para que el menu no arranque vacio.
        for (int i = 0; i < 4; i++) Soltar(Random.Range(-0.7f, 0.7f));
        proximo = Time.unscaledTime + Random.Range(intervaloMinimo, intervaloMaximo);
    }

    private void Update()
    {
        // Tiempo sin escalar: el menu no se pausa, pero timeScale puede venir tocado.
        float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);

        // El modo oscuro se toca en la ventana de opciones, con el menu detras: el
        // fondo se hace de noche mientras se mira.
        if (revisionTema != Tema.Revision)
        {
            revisionTema = Tema.Revision;
            objetivo = Tema.Oscuro ? 1f : 0f;
        }
        if (!Mathf.Approximately(mezcla, objetivo))
        {
            mezcla = Mathf.MoveTowards(mezcla, objetivo, dt / Mathf.Max(0.01f, duracionFundido));
            Aplicar();
        }
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
    // El cielo, la luz, la luz ambiente, la niebla y el piso, mezclados entre el dia y
    // la noche. El piso cambia a la mitad, que es cuando esta mas oscuro.
    private void Aplicar()
    {
        Color cielo = Color.Lerp(colorCielo, colorCieloNoche, mezcla);
        CieloActual = cielo;
        if (camara != null) camara.backgroundColor = cielo;
        RenderSettings.fogColor = cielo;
        if (luz != null)
        {
            luz.color = Color.Lerp(luzDia, colorLuzNoche, mezcla);
            luz.intensity = Mathf.Lerp(intensidadDia, intensidadLuzNoche, mezcla);
            luz.transform.rotation = Quaternion.Slerp(rotacionDia, Quaternion.Euler(rotacionLuzNoche), mezcla);
        }
        RenderSettings.ambientLight = Color.Lerp(ambienteDia, ambienteNoche, mezcla);
        if (pisoRenderer != null)
            pisoRenderer.sharedMaterial = mezcla >= 0.5f && materialPisoNoche != null ? materialPisoNoche : materialPiso;
    }

    private void OnDestroy()
    {
        // La niebla y la luz ambiente son de la aplicacion: la partida no hereda la noche
        // del menu (lo mismo hace CapitulosDeEscenario).
        RenderSettings.fog = false;
        RenderSettings.ambientLight = ambienteDia;
    }

    private void Soltar(float arrancaEn)
    {
        var prefab = Elegir();
        if (prefab == null) return;

        var enemigo = prefab.GetComponent<EnemyController>();
        float velocidadJuego = enemigo != null && enemigo.enemyType != null ? enemigo.enemyType.velocidad : 5f;
        float animacion = enemigo != null && campoAnimacion != null ? (float)campoAnimacion.GetValue(enemigo) : 1f;
        float ritmo = enemigo != null && campoRitmo != null ? (float)campoRitmo.GetValue(enemigo) : 1f;

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

        // Ningun script de un zombi puede llevar RequireComponent de otro script suyo: eso
        // impide borrarlo aca y el zombi del fondo queda con logica a medias (ver JefePatrones).
        foreach (var s in zombi.GetComponentsInChildren<MonoBehaviour>(true)) DestroyImmediate(s);
        foreach (var co in zombi.GetComponentsInChildren<Collider>(true)) DestroyImmediate(co);
        foreach (var rb in zombi.GetComponentsInChildren<Rigidbody>(true)) DestroyImmediate(rb);

        zombi.transform.SetParent(transform, true);
        Destroy(caja);
        zombi.SetActive(true);

        // El paso va por los parametros del controller, como en la partida
        // (EnemyController.OnEnable), con el Animator entero a 1: Paso es el ritmo del
        // estado de andar y Ritmo elige entre caminar y correr. Con animator.speed y sin
        // Ritmo (que en el controller vale 1) todos corrian, y el tanque y el jefe, que
        // caminan, corrian en camara lenta. Despues de prenderlo (a un Animator apagado no
        // se le fijan parametros), y solo los que tienen controller: el rapido trae un
        // segundo Animator vacio, y pedirle un parametro avisa en la consola.
        foreach (var a in zombi.GetComponentsInChildren<Animator>(true))
        {
            if (a.runtimeAnimatorController == null) continue;
            a.speed = 1f;
            a.SetFloat(idPaso, animacion * factorVelocidad);
            a.SetFloat(idRitmo, ritmo);
        }

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
