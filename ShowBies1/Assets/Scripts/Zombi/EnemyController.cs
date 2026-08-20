using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public TransitionsZM transZM;

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
        thePlayer = FindObjectOfType<PlayerController>();
    }

    private void FixedUpdate()
    {
       if (thePlayer == null) return;
       transform.LookAt(thePlayer.transform.position);
       rb.linearVelocity = (transform.forward * enemyType.velocidad);
    }

    public void DanoZombi(int daño)
    {
        vidaActual -= daño;

        if (vidaActual <= 0)
        {
            DejarManchaDeSangre();

            Instantiate(deathParticles, transform.position, Quaternion.identity);
            Destroy(gameObject);
            Puntaje.instance.contadorKill++;
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

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            PlayerHealth.instance.TakeDamage(enemyType.daño);
        }
    }
}
