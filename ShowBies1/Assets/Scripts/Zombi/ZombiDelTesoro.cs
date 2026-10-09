using System.Collections.Generic;
using UnityEngine;

// El zombi del tesoro (revision del 9/10, mejora 8): dorado, raro, no ataca y huye. Si se lo
// alcanza revienta en una lluvia de monedas (las suelta EnemyController al morir: suelta mas
// que Moneda.lluviaDesde, asi salen todas aunque el piso este lleno) con "¡TESORO!", y si no,
// a los 'duracion' segundos se escapa sin dejar nada ("¡SE ESCAPO!"). Lo saca WaveManager con
// un 25 % por oleada desde la 3, cerca del jugador y a la vista, y la oleada no lo espera: no
// cuenta en ella. Es el duende del tesoro de Diablo, y rompe la rutina de quedarse quieto
// disparando.
//
// Se mueve por su cuenta (IMovimientoPropio): en cada paso de fisica elige, entre dieciseis
// rumbos, el que mas lo aleja del jugador sin meterse en las paredes del borde ni en un
// obstaculo (ElegirRumbo), con un vaiven de costado para que no sea un blanco quieto, y dobla
// de a poco hacia ese. Al salir se queda 'quietoAlSalir' mirando al jugador: corriendo desde el
// primer cuadro, a los 0,4 s ya estaba fuera de la pantalla y nadie lo veia. No tira zarpazos
// (puedeZarpar) y su golpe es 0.
//
// El brillo (un halo de frente a la camara y un charco dorado, como las cajas) es un objeto
// aparte que lo sigue, como la barra de vida: hijo del zombi, el destello del golpe lo pintaria
// de blanco, giraria con el y se aplastaria con cada bala. Late mas rapido cuando esta por irse.
//
// Sin RequireComponent: FondoMenu le borra los scripts a los zombis (ver la trampa en CLAUDE.md).
public class ZombiDelTesoro : MonoBehaviour, IMovimientoPropio
{
    public float duracion = 12f;
    public float quietoAlSalir = 0.7f;      // segundos mirando al jugador antes de salir corriendo
    public float borde = 45f;               // del centro del mapa: las paredes invisibles estan en 49
    public float vaiven = 35f;              // grados de un lado al otro
    public float periodoDelVaiven = 1.6f;   // segundos
    public float giro = 7f;                 // que tan rapido dobla hacia el rumbo elegido
    public Material materialHalo;
    public Material materialCharco;
    public Color color = new Color(1f, 0.82f, 0.25f);

    public const int Rumbos = 16;
    public const float MirarAdelante = 3f;   // metros: donde se fija si hay pared
    public const float PorIrse = 0.25f;      // del tiempo: desde ahi el brillo late rapido

    private static readonly List<ZombiDelTesoro> vivos = new List<ZombiDelTesoro>();
    public static IReadOnlyList<ZombiDelTesoro> Vivos { get { return vivos; } }

    private EnemyController zombi;
    private Transform brillo, halo, charco;
    private float aparecioEn;
    private Vector3 rumbo;
    private float radio;

    public EnemyController Zombi { get { return zombi; } }
    public bool SeEscapo { get; private set; }
    public bool Atrapado { get; private set; }

    // Lo que le queda antes de irse, de 1 a 0.
    public float Restante01
    {
        get { return duracion > 0f ? Mathf.Clamp01(1f - (Time.time - aparecioEn) / duracion) : 0f; }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetearEstadoCompartido()
    {
        vivos.Clear();
    }

    // El rumbo de huida desde 'desde', con el jugador en 'jugador', a los 't' segundos de salir:
    // entre 'rumbos' direcciones, la que mas se parece a alejarse (meciendose 'vaiven' grados
    // con 'periodo'), descontando lo que se mete mas alla de 'borde' a MirarAdelante metros y
    // lo que tiene un obstaculo justo adelante. Contra una pared corre a lo largo; arrinconado,
    // sale por un costado (y puede pasar cerca del jugador: es la chance de agarrarlo).
    public static Vector3 ElegirRumbo(Vector3 desde, Vector3 jugador, float t, float vaiven, float periodo, float borde, float radio, int rumbos = Rumbos)
    {
        Vector3 lejos = desde - jugador;
        lejos.y = 0f;
        if (lejos.sqrMagnitude < 1e-4f) lejos = Vector3.forward;
        lejos.Normalize();
        float meneo = periodo > 0f ? vaiven * Mathf.Sin(t * 2f * Mathf.PI / periodo) : 0f;
        Vector3 ideal = Quaternion.Euler(0f, meneo, 0f) * lejos;

        Vector3 mejor = ideal;
        float mejorPuntaje = float.NegativeInfinity;
        for (int i = 0; i < rumbos; i++)
        {
            Vector3 d = Quaternion.Euler(0f, i * 360f / rumbos, 0f) * Vector3.forward;
            float puntaje = Vector3.Dot(d, ideal);
            Vector3 adelante = desde + d * MirarAdelante;
            float afuera = Mathf.Max(0f, Mathf.Abs(adelante.x) - borde) + Mathf.Max(0f, Mathf.Abs(adelante.z) - borde);
            puntaje -= afuera * 1.5f;
            if (CapitulosDeEscenario.Ocupado(desde + d * 1.5f, radio)) puntaje -= 3f;
            if (puntaje > mejorPuntaje)
            {
                mejorPuntaje = puntaje;
                mejor = d;
            }
        }
        return mejor;
    }

    private void Awake()
    {
        zombi = GetComponent<EnemyController>();
        radio = EnemyController.RadioDelCuerpo(gameObject);
        ArmarBrillo();
    }

    private void OnEnable()
    {
        aparecioEn = Time.time;
        SeEscapo = Atrapado = false;
        rumbo = Vector3.zero;
        if (!vivos.Contains(this)) vivos.Add(this);
        if (brillo != null) brillo.gameObject.SetActive(true);
    }

    private void OnDisable()
    {
        vivos.Remove(this);
        if (brillo != null) brillo.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (brillo != null) Destroy(brillo.gameObject);
    }

    public bool Mover(Rigidbody rb, Transform jugador)
    {
        if (zombi == null || !zombi.Vivo || jugador == null) return false;
        // EnemyController lo vuelve a prender al aparecer: aca se apaga en cada paso.
        zombi.puedeZarpar = false;
        if (Time.time - aparecioEn < quietoAlSalir)
        {
            Vector3 mirar = jugador.position - transform.position;
            mirar.y = 0f;
            if (mirar.sqrMagnitude > 1e-4f) transform.rotation = Quaternion.LookRotation(mirar);
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            return true;
        }
        Vector3 deseado = ElegirRumbo(transform.position, jugador.position, Time.time - aparecioEn, vaiven, periodoDelVaiven, borde, radio);
        rumbo = rumbo == Vector3.zero ? deseado : Vector3.Slerp(rumbo, deseado, 1f - Mathf.Exp(-giro * Time.fixedDeltaTime));
        rumbo.y = 0f;
        if (rumbo.sqrMagnitude < 1e-6f) rumbo = deseado;
        rumbo.Normalize();
        transform.rotation = Quaternion.LookRotation(rumbo);
        Vector3 v = rumbo * zombi.enemyType.velocidad;
        v.y = rb.linearVelocity.y;
        rb.linearVelocity = v;
        return true;
    }

    // Lo llama EnemyController en el bloque de la muerte, despues de soltar las monedas.
    public void AlMorir()
    {
        if (Atrapado || SeEscapo) return;
        Atrapado = true;
        Efectos.TesoroAtrapado(transform.position);
        if (brillo != null) brillo.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (zombi == null || !zombi.Vivo || SeEscapo || Atrapado) return;
        // Con el jugador muerto no: festeja con los demas, y "¡SE ESCAPO!" saldria encima de la
        // derrota (lo que cambia la partida despues de morir mira esto, ver CLAUDE.md).
        if (PlayerHealth.instance != null && PlayerHealth.instance.EstaMuerto) return;
        // Con tiempo escalado: la pausa lo congela.
        if (Time.time - aparecioEn < duracion) return;
        SeEscapo = true;
        Efectos.TesoroSeEscapo(transform.position);
        zombi.Retirar();
    }

    private void LateUpdate()
    {
        if (brillo == null || !brillo.gameObject.activeSelf) return;
        var camara = Camera.main;
        Vector3 pos = transform.position;
        charco.position = new Vector3(pos.x, 0.2f, pos.z);
        if (camara != null)
        {
            // Detras del zombi, visto desde la camara, para no pasarle por delante, y medio
            // metro arriba: la camara mira para abajo, y corrido hacia atras sin subirlo se
            // hundia en el piso, que lo cortaba con una raya.
            halo.position = pos + Vector3.up * 0.5f + camara.transform.forward * 0.25f;
            halo.rotation = camara.transform.rotation;
        }
        float pulso = Restante01 < PorIrse ? 10f : 3f;
        float latido = 1f + 0.12f * Mathf.Sin(Time.time * pulso);
        halo.localScale = new Vector3(2.4f, 2.4f, 1f) * latido;
    }

    private void ArmarBrillo()
    {
        if (materialHalo == null || materialCharco == null) return;
        brillo = new GameObject("BrilloDelTesoro").transform;
        halo = Pieza(brillo, "Halo", materialHalo);
        charco = Pieza(brillo, "Charco", materialCharco);
        charco.rotation = Quaternion.Euler(90f, 0f, 0f);
        charco.localScale = new Vector3(3f, 3f, 1f);
        brillo.gameObject.SetActive(isActiveAndEnabled);
    }

    private static Transform Pieza(Transform padre, string nombre, Material material)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = nombre;
        // En el acto: nace en el origen, y un collider fijo ahi, aunque fuera un cuadro, podia
        // empujar al jugador.
        DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(padre, false);
        go.layer = Personajes.Capa;
        var render = go.GetComponent<MeshRenderer>();
        render.sharedMaterial = material;
        render.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        render.receiveShadows = false;
        return go.transform;
    }
}
