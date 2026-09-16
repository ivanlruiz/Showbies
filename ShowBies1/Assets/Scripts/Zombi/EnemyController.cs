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
    // pasarse del techo de población: sin esto spawnean para siempre. Cuenta los
    // zombis prendidos (OnEnable/OnDisable), salgan o no del pool.
    public static int ZombisVivos { get; private set; }

    // Los zombis se reusan, como las balas y las monedas: con decenas muriendo por
    // minuto, crear y destruir cada uno era basura para el recolector y trabajo
    // para la fisica en cada aparicion, y en los telefonos flojos se notaba en
    // tirones. El pool es por prefab: un tanque no vuelve como zombi normal.
    private static readonly Dictionary<GameObject, Stack<EnemyController>> pool = new Dictionary<GameObject, Stack<EnemyController>>();
    private static int ultimaAparicion;

    private GameObject prefabDeOrigen;   // null si no salio del pool (el tutorial): al morir se destruye
    private bool enUso;
    private Animator[] animadores;

    // Distinto en cada aparicion aunque el objeto sea el mismo: lo anotan los que
    // guardan zombis para preguntar despues si murieron (ver SigueVivo).
    public int NumeroDeAparicion { get; private set; }

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
        pool.Clear();
        ultimaAparicion = 0;
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

    // Hace aparecer un zombi de ese prefab: uno apagado del pool si hay, o uno
    // nuevo. Quien lo llama le pone despues los multiplicadores, como antes con
    // Instantiate: la vida se calcula perezosa, con los que tenga en el primer golpe.
    public static EnemyController Aparecer(GameObject prefab, Vector3 posicion)
    {
        if (prefab == null) return null;

        // Al cambiar de escena los zombis guardados se destruyen y en la pila quedan
        // referencias muertas: se descartan antes de usarlas.
        EnemyController zombi = null;
        Stack<EnemyController> pila;
        if (pool.TryGetValue(prefab, out pila))
        {
            while (zombi == null && pila.Count > 0) zombi = pila.Pop();
        }

        if (zombi != null)
        {
            // Se mueve apagado y despues se prende: la fisica lo toma ya en su lugar.
            zombi.transform.SetPositionAndRotation(posicion, Quaternion.identity);
            zombi.gameObject.SetActive(true);
            return zombi;
        }

        var nuevo = Instantiate(prefab, posicion, Quaternion.identity);
        zombi = nuevo.GetComponent<EnemyController>();
        if (zombi == null)
        {
            Debug.LogWarning("EnemyController.Aparecer: " + prefab.name + " no tiene EnemyController.", prefab);
            Destroy(nuevo);
            return null;
        }
        zombi.prefabDeOrigen = prefab;
        return zombi;
    }

    // Vivo y en la misma aparicion que se anoto: un zombi que murio y volvio a
    // salir del pool es otro zombi, aunque sea el mismo objeto.
    public static bool SigueVivo(EnemyController zombi, int numeroDeAparicion)
    {
        return zombi != null && zombi.enUso && zombi.NumeroDeAparicion == numeroDeAparicion;
    }

    // Lo que no cambia de una aparicion a otra se arma una sola vez.
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        // La rotacion la pone FixedUpdate, mirando al jugador: que los choques no
        // lo inclinen entre un paso y otro.
        rb.freezeRotation = true;
        escalaBase = transform.localScale;
        animadores = GetComponentsInChildren<Animator>(true);
        PrepararDestello();
    }

    // Cada aparicion, nueva o salida del pool, arranca de cero. Corre antes de que
    // quien lo hizo aparecer le ponga los multiplicadores.
    private void OnEnable()
    {
        enUso = true;
        ZombisVivos++;
        NumeroDeAparicion = ++ultimaAparicion;

        vidaIniciada = false;
        estaMuerto = false;
        proximoGolpe = 0f;
        multiplicadorMonedas = 1f;
        monedaPrefab = null;
        multiplicadorVida = 1f;
        multiplicadorDano = 1f;

        transform.localScale = escalaBase;
        aplastado = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        thePlayer = ObtenerJugador();
        foreach (var animador in animadores) animador.speed = velocidadDeAnimacion;
        SubirSobreElPiso();
    }

    // Al morir, al caerse o al descargarse la escena. Lo que quedo a mitad de un
    // golpe (el destello, la barra) no puede pasar a la aparicion siguiente.
    private void OnDisable()
    {
        if (!enUso) return;
        enUso = false;
        ZombisVivos--;

        // La barra se apaga en el acto, no en su LateUpdate: el zombi puede volver a
        // salir del pool en este mismo frame. Queda guardada para la aparicion
        // siguiente, que la prende con su primer golpe que no mata; crearla de nuevo
        // eran tres GameObjects por cada zombi golpeado.
        if (barraDeVida != null) barraDeVida.Ocultar();
        if (destellando)
        {
            destellando = false;
            for (int i = 0; i < renderersVisibles.Length; i++) renderersVisibles[i].sharedMaterials = materialesOriginales[i];
        }
    }

    // La barra es un objeto aparte y, apagada, no se entera de que el zombi ya no
    // existe: se va con el (al descargar la escena, o un zombi del tutorial).
    private void OnDestroy()
    {
        if (barraDeVida != null) Destroy(barraDeVida.gameObject);
    }

    // Saca del mapa a los zombis que esten a 'radio' del punto, SIN puntos ni
    // monedas ni mancha, como el kill-Z. Lo usa el revivir con un video: el jugador
    // vuelve en el mismo lugar donde lo mataron y necesita aire, pero cobrar por esos
    // zombis convertiria el video en una forma barata de limpiar la pantalla.
    // Devuelve cuantos se fueron.
    public static int DespejarAlrededor(Vector3 punto, float radio)
    {
        if (!(radio > 0f)) return 0;

        int despejados = 0;
        float radioAlCuadrado = radio * radio;
        var zombis = FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
        foreach (var zombi in zombis)
        {
            if (zombi == null || !zombi.enUso) continue;
            if ((zombi.transform.position - punto).sqrMagnitude > radioAlCuadrado) continue;

            Efectos.ParticulasDeMuerte(zombi.deathParticles, zombi.transform.position);
            zombi.estaMuerto = true;
            zombi.Devolver();
            despejados++;
        }
        return despejados;
    }

    // Vuelve apagado al pool. Los que no salieron del pool se destruyen como antes.
    private void Devolver()
    {
        if (prefabDeOrigen == null)
        {
            Destroy(gameObject);
            return;
        }
        if (!enUso) return;

        gameObject.SetActive(false);
        Stack<EnemyController> pila;
        if (!pool.TryGetValue(prefabDeOrigen, out pila))
        {
            pila = new Stack<EnemyController>();
            pool[prefabDeOrigen] = pila;
        }
        pila.Push(this);
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

    // Perezosa y una sola vez por aparicion: quien hace aparecer al zombi le pone
    // los multiplicadores despues de Aparecer, y un zombi puede recibir daño (una
    // granada que explota justo donde aparece) antes del primer FixedUpdate. La vida
    // sale del multiplicador vigente en ese momento, y la barra usa esta vidaMaxima,
    // asi que el escalado no la rompe mientras se aplique antes del primer golpe.
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
           Devolver();   // sin puntos ni mancha: nadie lo mato
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
        // Dos golpes en el mismo paso de fisica llaman a esto dos veces con la vida
        // ya en cero (los eventos de colision del paso se despachan aunque el zombi
        // ya se haya apagado), y sin la guarda el bloque de muerte corria de nuevo
        // entero: puntos dobles, dos manchas, dos explosiones.
        // Un daño de cero, negativo o NaN no hace nada: no hay numero que mostrar.
        // Uno apagado en el pool tampoco: el que cayo por el kill-Z no llego a morir.
        if (estaMuerto || !enUso || !(daño > 0f)) return;

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

            // Sin Instantiate por muerte: la rafaga sale de una copia del prefab en Efectos.
            Efectos.ParticulasDeMuerte(deathParticles, transform.position);

            // Unico lugar donde se suman puntos. Antes tambien sumaba
            // BulletController por cada impacto, asi que matar con granada valia
            // 1 punto y matar a tiros valia cien veces mas.
            Puntaje.instance.contadorKill += enemyType.puntos;
            Puntaje.instance.UpdateKillCounterUI();

            SoltarMonedas();
            Efectos.Muerte(transform.position, enemyType.hp);

            // Al final: todo lo de arriba usa su posicion.
            Devolver();
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
        if (manchaDeSangrePrefab == null || sangreSprites == null || sangreSprites.Length == 0) return;

        // Sale de un pool y se levanta sola a los duracionMancha segundos. No la
        // puede limpiar el zombi: en este mismo frame vuelve apagado a su pool, o se
        // destruye si es del tutorial (antes una corrutina sobre el zombi dejaba las
        // manchas en la escena para siempre).
        ManchaDeSangre.Poner(manchaDeSangrePrefab, transform.position, SpriteDeSangreAlAzar(), duracionMancha);
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
        // Un muerto no pega: los eventos del paso en que murio llegan igual.
        if (estaMuerto || !enUso) return;
        if (Time.time < proximoGolpe || !collision.gameObject.CompareTag("Player")) return;
        if (PlayerHealth.instance == null) return;

        proximoGolpe = Time.time + intervaloDeGolpe;
        PlayerHealth.instance.TakeDamage(DanoPorGolpe);
    }
}
