using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    private int vidaActual;
    private Rigidbody rb;
    [Header("Unity Setup")]
    public ParticleSystem deathParticles;
    public Enemy enemyType;
    public PlayerController thePlayer;
    public GameObject manchaDeSangrePrefab;
    public Texture2D[] sangreSprites;

    [Header("Manchas de sangre")]
    [SerializeField] private float duracionMancha = 2f;

    [Header("Golpe")]
    [SerializeField] private float intervaloDeGolpe = 0.8f;   // segundos entre golpes mientras toca al jugador

    private float proximoGolpe;

    // Cuántos zombis hay vivos ahora mismo. Los generadores lo miran para no
    // pasarse del techo de población: sin esto spawnean para siempre.
    public static int ZombisVivos { get; private set; }

    // Sprite.Create aloca un Sprite nuevo cada vez, y antes se llamaba una vez por
    // muerte. Son 8 texturas fijas: se crean una sola vez y se reusan.
    private static readonly Dictionary<Texture2D, Sprite> spritesDeSangre = new Dictionary<Texture2D, Sprite>();

    // Los static sobreviven al cambio de escena (y al "enter play mode" sin domain
    // reload), asi que hay que limpiarlos a mano al arrancar.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetearEstadoCompartido()
    {
        ZombisVivos = 0;
        jugadorCache = null;
        spritesDeSangre.Clear();
    }

    private void Awake()
    {
        ZombisVivos++;
    }

    private void OnDestroy()
    {
        ZombisVivos--;
    }

    void Start()
    {
        vidaActual = enemyType.hp;
        rb = GetComponent<Rigidbody>();
        thePlayer = ObtenerJugador();
    }

    // Esto era un FindObjectOfType por zombi spawneado, o sea un barrido completo
    // de la escena cada vez. Y como los zombis son objetos de la escena, cuantos
    // mas habia mas caro salia buscar: costo cuadratico. El jugador es uno solo y
    // no cambia, asi que se busca una vez.
    private static PlayerController jugadorCache;

    private static PlayerController ObtenerJugador()
    {
        if (jugadorCache == null)
        {
            jugadorCache = FindFirstObjectByType<PlayerController>();
        }
        return jugadorCache;
    }

    private void FixedUpdate()
    {
       // Kill-Z. Un zombi que se cae del mapa (empujado por otros en el borde, o
       // spawneado mal) cae para siempre y SIGUE contando en ZombisVivos: con el
       // techo de poblacion, cada caido es un lugar menos que no se recupera
       // nunca, hasta que el generador queda tapado y la partida se vacia.
       if (transform.position.y < -20f)
       {
           Destroy(gameObject);   // sin puntos ni mancha: nadie lo mato
           return;
       }

       if (thePlayer == null) return;
       transform.LookAt(thePlayer.transform.position);
       rb.linearVelocity = (transform.forward * enemyType.velocidad);
    }

    private bool estaMuerto;

    public void DanoZombi(int daño)
    {
        // Destroy es diferido: dos golpes en el mismo paso de fisica llamaban a
        // esto dos veces con la vida ya en cero, y el bloque de muerte corria de
        // nuevo entero: puntos dobles, dos manchas, dos explosiones.
        if (estaMuerto) return;

        vidaActual -= daño;

        if (vidaActual <= 0)
        {
            estaMuerto = true;

            DejarManchaDeSangre();

            Instantiate(deathParticles, transform.position, Quaternion.identity);
            Destroy(gameObject);

            // Unico lugar donde se suman puntos. Antes tambien sumaba
            // BulletController por cada impacto, asi que matar con granada valia
            // 1 punto y matar a tiros valia cien veces mas.
            Puntaje.instance.contadorKill += enemyType.puntos;
            Puntaje.instance.UpdateKillCounterUI();
        }
    }

    private void DejarManchaDeSangre()
    {
        if (manchaDeSangrePrefab == null || sangreSprites.Length == 0) return;

        GameObject manchaDeSangre = Instantiate(manchaDeSangrePrefab, transform.position, Quaternion.identity);
        manchaDeSangre.SetActive(true);
        manchaDeSangre.GetComponent<SpriteRenderer>().sprite = SpriteDeSangreAlAzar();
        manchaDeSangre.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        manchaDeSangre.transform.position = new Vector3(transform.position.x, .1f, transform.position.z);

        // Con Destroy diferido y no con una corrutina: la corrutina corria sobre el
        // zombi, que se destruye en este mismo frame, asi que nunca llegaba a
        // ejecutarse y las manchas quedaban en la escena para siempre.
        Destroy(manchaDeSangre, duracionMancha);
    }

    private Sprite SpriteDeSangreAlAzar()
    {
        Texture2D textura = sangreSprites[Random.Range(0, sangreSprites.Length)];

        Sprite sprite;
        if (!spritesDeSangre.TryGetValue(textura, out sprite) || sprite == null)
        {
            sprite = Sprite.Create(textura, new Rect(0f, 0f, textura.width, textura.height), new Vector2(0.5f, 0.5f), 100f);
            spritesDeSangre[textura] = sprite;
        }
        return sprite;
    }

    // Pega al tocar al jugador y despues cada intervaloDeGolpe mientras lo siga
    // tocando. Antes pegaba solo en OnCollisionEnter: un zombi pegado al jugador
    // no volvia a dañar hasta separarse, y el daño dependia de cuanto temblara la
    // fisica. El intervalo ademas evita que los varios colliders del zombi y del
    // jugador cuenten el mismo toque mas de una vez.
    private void OnCollisionEnter(Collision collision)
    {
        Golpear(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        Golpear(collision);
    }

    private void Golpear(Collision collision)
    {
        if (Time.time < proximoGolpe || !collision.gameObject.CompareTag("Player")) return;
        if (PlayerHealth.instance == null) return;

        proximoGolpe = Time.time + intervaloDeGolpe;
        PlayerHealth.instance.TakeDamage(enemyType.daño);
    }
}
