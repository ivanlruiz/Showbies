using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Granade : MonoBehaviour
{
    public float radioExplosion = 5f;
    public int damage = 10;
    public ParticleSystem explosion;

    [SerializeField] private float tiempoDeMecha = 3f;

    [Header("Lanzamiento")]
    [SerializeField] private float tiempoDeVuelo = 0.8f;
    [SerializeField] private float alturaDelArco = 2.5f;
    [SerializeField] private float demoraAlCaer = 0.3f;
    [SerializeField] private float alturaAlCaer = 0.1f;      // la mitad del alto de la granada: apoyada, no enterrada
    [SerializeField] private float radioDeContacto = 0.6f;   // que tan cerca de un zombi tiene que pasar para explotar en el aire
    [SerializeField] private LineRenderer indicador;         // anillo en el piso con el radio de la explosion
    [SerializeField] private int segmentosIndicador = 48;

    private bool yaExploto;
    private bool lanzada;
    private Vector3 origen;
    private Vector3 destino;
    private float lanzadaEn;
    private readonly Collider[] contactos = new Collider[16];

    // El jugador copia este anillo para marcar donde va a caer mientras apunta.
    public LineRenderer Indicador { get { return indicador; } }

    // La lanza hacia un punto del piso. Vuela por una trayectoria calculada y no
    // por fisica: asi cae justo sobre el anillo, y no choca con el jugador del que
    // sale ni con las balas que van en la misma direccion.
    public void Lanzar(Vector3 puntoEnElPiso)
    {
        lanzada = true;
        origen = transform.position;
        destino = new Vector3(puntoEnElPiso.x, alturaAlCaer, puntoEnElPiso.z);
        lanzadaEn = Time.time;

        var cuerpo = GetComponent<Rigidbody>();
        if (cuerpo != null) cuerpo.isKinematic = true;
        var colision = GetComponent<Collider>();
        if (colision != null) colision.enabled = false;

        MostrarIndicador(puntoEnElPiso);
    }

    private void Start()
    {
        // Sin Lanzar (una granada instanciada por otro camino) queda como mina:
        // cae donde nace y explota con la mecha.
        if (!lanzada) StartCoroutine(Mecha());
    }

    private IEnumerator Mecha()
    {
        yield return new WaitForSeconds(tiempoDeMecha);
        Explode();
    }

    private void Update()
    {
        if (!lanzada || yaExploto) return;

        float t = (Time.time - lanzadaEn) / tiempoDeVuelo;
        if (t < 1f)
        {
            Vector3 posicion = Vector3.Lerp(origen, destino, t);
            posicion.y += 4f * alturaDelArco * t * (1f - t);
            transform.position = posicion;

            if (TocaUnZombi()) Explode();
            return;
        }

        transform.position = destino;
        if (Time.time - lanzadaEn >= tiempoDeVuelo + demoraAlCaer) Explode();
    }

    private bool TocaUnZombi()
    {
        int n = Physics.OverlapSphereNonAlloc(transform.position, radioDeContacto, contactos);
        for (int i = 0; i < n; i++)
        {
            if (contactos[i].GetComponentInParent<EnemyController>() != null) return true;
        }
        return false;
    }

    private void MostrarIndicador(Vector3 centro)
    {
        if (indicador == null) return;

        // Suelto de la granada para que no vuele con ella, y con los puntos en
        // espacio de mundo para que la escala aplastada del prefab no lo deforme.
        indicador.transform.SetParent(null, false);
        DibujarAnillo(indicador, centro, radioExplosion, segmentosIndicador);
        indicador.gameObject.SetActive(true);
    }

    // Un circulo apenas por encima del piso, con los puntos en espacio de mundo.
    public static void DibujarAnillo(LineRenderer linea, Vector3 centro, float radio, int segmentos = 48)
    {
        linea.useWorldSpace = true;
        linea.loop = true;
        linea.positionCount = segmentos;
        for (int i = 0; i < segmentos; i++)
        {
            float angulo = i * Mathf.PI * 2f / segmentos;
            linea.SetPosition(i, new Vector3(centro.x + Mathf.Cos(angulo) * radio, 0.05f, centro.z + Mathf.Sin(angulo) * radio));
        }
    }

    private void Explode()
    {
        if (yaExploto) return;
        yaExploto = true;

        Collider[] colliders = Physics.OverlapSphere(transform.position, radioExplosion);

        // GetComponentInParent porque los zombis tienen hitboxes hijas, y el
        // HashSet porque entonces los tres colliders del mismo zombi resuelven
        // al mismo componente: sin dedup la explosion lo dañaria tres veces.
        var yaDanados = new HashSet<EnemyController>();
        foreach (Collider nearbyObject in colliders)
        {
            EnemyController enemyController = nearbyObject.GetComponentInParent<EnemyController>();

            if (enemyController != null && yaDanados.Add(enemyController))
            {
                // Aplicar daño al zombi. Escala con la vida del zombi y no con la
                // mejora de daño de bala: la granada es un recurso con cooldown que
                // tiene que seguir matando lo mismo en la oleada 20 que en la 1, y
                // la mejora de bala ya se paga con monedas por su lado.
                enemyController.DanoZombi(damage * enemyController.multiplicadorVida);
            }
        }

        if (explosion != null)
        {
            // La particula es hija de la granada y tiene stopAction = Destroy, asi
            // que hay que despegarla antes de destruir la granada: si no, se corta
            // apenas empieza. Despegada se limpia sola cuando termina.
            explosion.transform.SetParent(null, true);
            explosion.Play();
        }

        Efectos.Explosion(transform.position);

        if (indicador != null) Destroy(indicador.gameObject);
        Destroy(gameObject);
    }
}
