using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    // En float: con la vida escalada por oleada y el daño por bala mejorado, los
    // dos dejan de ser enteros. Con int, 1,15^n redondeado cambiaba cuantas balas
    // hacian falta de una oleada a la otra a los saltos.
    private float vidaActual;
    private float vidaMaxima;
    private bool vidaIniciada;
    private BarraDeVida barraDeVida;

    // Por debajo de esto el zombi esta muerto. Sin margen, cinco balas de 5 * 1,15
    // contra un zombi de 25 * 1,15 dejaban un resto de coma flotante (1e-6) y
    // hacia falta una sexta bala para una vida que no se ve.
    private const float VidaResidual = 0.01f;

    private const float AlturaDelPiso = 0f;
    private const float MargenSobreElPiso = 0.02f;

    // Por debajo de esto el zombi se cayo del mapa o quedo bajo el piso. No es el
    // -20 del jugador: bajo el piso ya no se ve ni se le puede disparar, y no hay
    // por que esperar a que caiga 20 m.
    private const float AlturaKillZ = -2f;

    // Los multiplicadores los pone quien hace aparecer al zombi, despues del
    // Instantiate y antes de su Start: WaveManager (con los de la oleada y el
    // botin) y GeneradorZombis (con los del nivel del modo libre). Un zombi sin
    // moneda, como los del tutorial, no suelta nada, y uno que nadie toca vale
    // lo de su .asset.
    [System.NonSerialized] public float multiplicadorMonedas = 1f;
    [System.NonSerialized] public Moneda monedaPrefab;
    [System.NonSerialized] public float multiplicadorVida = 1f;
    [System.NonSerialized] public float multiplicadorDano = 1f;
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

    [Header("Animacion")]
    [Tooltip("Velocidad del Animator del modelo. Todos usan el modelo del zombi normal a distinta escala: el paso tiene que ir con lo que camina cada uno (el tanque pesado, el FASTER frenetico).")]
    [SerializeField] private float velocidadDeAnimacion = 1f;

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

    // Contadores del botin, para el medidor de balance y las pruebas: cuantas
    // muertes soltaron monedas, cuanto valor se esperaba en promedio y cuanto
    // salio de verdad. Con muchas muertes los dos ultimos tienen que parecerse; si
    // no, el sorteo de monedas o la regla del multiplicador menor a 1 estan mal.
    public static int MuertesConBotin { get; private set; }
    public static double MonedasEsperadas { get; private set; }
    public static double MonedasSoltadas { get; private set; }

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
        MuertesConBotin = 0;
        MonedasEsperadas = 0;
        MonedasSoltadas = 0;
    }

    public float VidaMaxima
    {
        get { IniciarVida(); return vidaMaxima; }
    }

    public float VidaActual
    {
        get { IniciarVida(); return vidaActual; }
    }

    public float DanoPorGolpe => enemyType.daño * multiplicadorDano;

    private void Awake()
    {
        ZombisVivos++;
        SubirSobreElPiso();
    }

    // El piso de las escenas de juego esta en Y = 0 y el pivote del zombi es el
    // centro de su capsula. Los puntos de aparicion de WaveMode estan en el piso,
    // asi que cada zombi nacia con media capsula enterrada en un piso sin espesor
    // (un plano), y dependia de la fisica que lo sacara para arriba y no para
    // abajo. Se lo sube antes del primer paso de fisica, lo haga aparecer quien lo
    // haga aparecer.
    private void SubirSobreElPiso()
    {
        var capsula = GetComponent<CapsuleCollider>();
        if (capsula == null) return;

        float alto = capsula.direction == 1 ? Mathf.Max(capsula.height * 0.5f, capsula.radius) : capsula.radius;
        float fondo = transform.TransformPoint(capsula.center).y - alto * Mathf.Abs(transform.lossyScale.y);
        if (fondo < AlturaDelPiso)
        {
            transform.position += Vector3.up * (AlturaDelPiso - fondo + MargenSobreElPiso);
        }
    }

    private void OnDestroy()
    {
        ZombisVivos--;
        if (barraDeVida != null) Destroy(barraDeVida.gameObject);
    }

    void Start()
    {
        IniciarVida();
        rb = GetComponent<Rigidbody>();
        // La rotacion la pone FixedUpdate, mirando al jugador: que los choques no
        // lo inclinen entre un paso y otro.
        rb.freezeRotation = true;
        thePlayer = ObtenerJugador();
        escalaBase = transform.localScale;
        PrepararDestello();
        foreach (var animador in GetComponentsInChildren<Animator>()) animador.speed = velocidadDeAnimacion;
    }

    // Perezosa y una sola vez: un zombi puede recibir daño (una granada que
    // explota justo donde aparece) o ser consultado antes de su Start. La vida sale
    // del multiplicador vigente en ese momento, y la barra usa esta vidaMaxima, asi
    // que el escalado no la rompe mientras se aplique antes del primer golpe.
    private void IniciarVida()
    {
        if (vidaIniciada) return;
        vidaIniciada = true;

        vidaMaxima = enemyType.hp * Mathf.Max(0.01f, multiplicadorVida);
        vidaActual = vidaMaxima;
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
       if (transform.position.y < AlturaKillZ)
       {
           Destroy(gameObject);   // sin puntos ni mancha: nadie lo mato
           return;
       }

       ActualizarGolpeVisual();

       if (thePlayer == null) return;

       // Mira y camina en horizontal, y la velocidad vertical queda en manos de la
       // fisica. Antes miraba al centro del jugador y pisaba la velocidad entera en
       // cada paso: la gravedad nunca actuaba, y un zombi que terminaba debajo del
       // piso se quedaba ahi, invisible, persiguiendo al jugador y pegandole desde
       // abajo. Ahora cae y lo saca el kill-Z.
       Vector3 objetivo = thePlayer.transform.position;
       objetivo.y = transform.position.y;
       if ((objetivo - transform.position).sqrMagnitude > 0.0001f) transform.LookAt(objetivo);
       Vector3 velocidad = transform.forward * enemyType.velocidad;
       velocidad.y = rb.linearVelocity.y;
       rb.linearVelocity = velocidad;
    }

    private bool estaMuerto;

    public void DanoZombi(float daño)
    {
        // Destroy es diferido: dos golpes en el mismo paso de fisica llamaban a
        // esto dos veces con la vida ya en cero, y el bloque de muerte corria de
        // nuevo entero: puntos dobles, dos manchas, dos explosiones.
        // Un daño de cero, negativo o NaN no hace nada: no hay numero que mostrar.
        if (estaMuerto || !(daño > 0f)) return;

        IniciarVida();
        vidaActual -= daño;

        // El numero flotante va redondeado y nunca en 0: un 5,75 se lee como 6, y
        // una bala que pega tiene que mostrar algo.
        Efectos.Golpe(transform.position + Vector3.up * (1f + escalaBase.y), Mathf.Max(1, Mathf.RoundToInt(daño)));

        // La barra aparece recien con el primer golpe que no mata.
        if (vidaActual > VidaResidual)
        {
            MostrarBarraDeVida();
            GolpeVisual();
        }
        else
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

        // Lo esperado es el promedio del sorteo por el multiplicador, sin la regla
        // de arriba; lo soltado, lo que salio de verdad. Lo que el techo de monedas
        // en escena reparta despues ya no cambia el total.
        MuertesConBotin++;
        MonedasEsperadas += (enemyType.monedasMin + enemyType.monedasMax) * 0.5 * multiplicadorMonedas;
        MonedasSoltadas += cantidad * valor;

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
        barraDeVida.Mostrar(vidaActual / Mathf.Max(0.01f, vidaMaxima));
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
        PlayerHealth.instance.TakeDamage(DanoPorGolpe);
    }
}
