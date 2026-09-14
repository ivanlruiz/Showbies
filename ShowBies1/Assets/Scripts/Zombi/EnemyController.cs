using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    private int vidaActual;
    private int vidaMaxima;
    private BarraDeVida barraDeVida;

    // Los pone quien hace aparecer al zombi: WaveManager (con el multiplicador de
    // la oleada) y GeneradorZombis (con la mitad). Un zombi sin moneda, como los
    // del tutorial, no suelta nada.
    [System.NonSerialized] public float multiplicadorMonedas = 1f;
    [System.NonSerialized] public Moneda monedaPrefab;
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

    [Header("Golpe visual")]
    [SerializeField] private Vector3 aplastadoAlGolpear = new Vector3(1.15f, 0.85f, 1.15f);
    [SerializeField] private float recuperacionDelAplastado = 0.35f;   // fraccion que recupera por paso de fisica

    // Destello: los renderers visibles pasan un instante al material blanco de
    // Efectos. Los arrays se arman una vez por zombi, no por golpe.
    private Renderer[] renderersVisibles;
    private Material[][] materialesOriginales;
    private Material[][] materialesDestello;
    private float destelloHasta;
    private bool destellando;
    private Vector3 escalaBase;
    private bool aplastado;

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
        if (barraDeVida != null) Destroy(barraDeVida.gameObject);
    }

    void Start()
    {
        vidaActual = enemyType.hp;
        vidaMaxima = vidaActual;
        rb = GetComponent<Rigidbody>();
        thePlayer = ObtenerJugador();
        escalaBase = transform.localScale;
        PrepararDestello();
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

       ActualizarGolpeVisual();

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
        Efectos.Golpe(transform.position + Vector3.up * (1f + escalaBase.y), daño);

        // La barra aparece recien con el primer golpe que no mata.
        if (vidaActual > 0)
        {
            MostrarBarraDeVida();
            GolpeVisual();
        }

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

            SoltarMonedas();
            Efectos.Muerte(transform.position, enemyType.hp);
        }
    }

    // Salen en el mismo bloque que los puntos, y por lo mismo: es el unico lugar
    // donde el zombi muere de verdad una sola vez. Se cobran recien cuando el
    // jugador las agarra (ver Moneda).
    private void SoltarMonedas()
    {
        if (monedaPrefab == null) return;

        int cantidad = Random.Range(enemyType.monedasMin, enemyType.monedasMax + 1);
        double valor = multiplicadorMonedas;

        // Con un multiplicador menor a 1 (el modo libre) cada moneda sale con esa
        // probabilidad y vale 1, en vez de salir todas valiendo 0,5: una moneda
        // que no mueve el contador al agarrarla no se siente como una moneda.
        if (multiplicadorMonedas < 1f)
        {
            int salen = 0;
            for (int i = 0; i < cantidad; i++)
            {
                if (Random.value < multiplicadorMonedas) salen++;
            }
            cantidad = salen;
            valor = 1;
        }

        Moneda.Soltar(monedaPrefab, transform.position, cantidad, valor);
    }

    private void PrepararDestello()
    {
        var todos = GetComponentsInChildren<Renderer>();
        var visibles = new List<Renderer>();
        foreach (var r in todos)
        {
            // Los normales y rapidos tienen renderers apagados (capsula y cubos
            // que solo son hitbox): esos no se tocan.
            if (r.enabled) visibles.Add(r);
        }

        renderersVisibles = visibles.ToArray();
        materialesOriginales = new Material[renderersVisibles.Length][];
        for (int i = 0; i < renderersVisibles.Length; i++)
        {
            materialesOriginales[i] = renderersVisibles[i].sharedMaterials;
        }
    }

    // Blanco un instante y aplastado: se nota cada bala que entra.
    private void GolpeVisual()
    {
        if (!aplastado && escalaBase != Vector3.zero)
        {
            transform.localScale = Vector3.Scale(escalaBase, aplastadoAlGolpear);
            aplastado = true;
        }

        Material blanco = Efectos.MaterialDestello;
        if (blanco == null || renderersVisibles == null) return;

        if (materialesDestello == null)
        {
            materialesDestello = new Material[renderersVisibles.Length][];
            for (int i = 0; i < renderersVisibles.Length; i++)
            {
                var blancos = new Material[materialesOriginales[i].Length];
                for (int j = 0; j < blancos.Length; j++) blancos[j] = blanco;
                materialesDestello[i] = blancos;
            }
        }

        for (int i = 0; i < renderersVisibles.Length; i++) renderersVisibles[i].sharedMaterials = materialesDestello[i];
        destellando = true;
        destelloHasta = Time.time + Efectos.DuracionDestello;
    }

    private void ActualizarGolpeVisual()
    {
        if (destellando && Time.time >= destelloHasta)
        {
            destellando = false;
            for (int i = 0; i < renderersVisibles.Length; i++) renderersVisibles[i].sharedMaterials = materialesOriginales[i];
        }

        if (aplastado)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, escalaBase, recuperacionDelAplastado);
            if ((transform.localScale - escalaBase).sqrMagnitude < 0.0001f)
            {
                transform.localScale = escalaBase;
                aplastado = false;
            }
        }
    }

    private void MostrarBarraDeVida()
    {
        if (barraDeVida == null) barraDeVida = BarraDeVida.Crear(this);
        barraDeVida.Mostrar((float)vidaActual / Mathf.Max(1, vidaMaxima));
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
