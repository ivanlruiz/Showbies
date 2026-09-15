using System.Collections.Generic;
using UnityEngine;

// Las monedas que sueltan los zombis al morir. Salen volando para los costados,
// caen despacio, dan un rebote, quedan girando en el piso y se cobran recien
// cuando el jugador las agarra: al acercarse, vuelan solas hacia el. Si nadie
// las agarra, parpadean y desaparecen.
//
// Se reusan desde un pool, como las balas: un jefe suelta decenas de una vez.
// No tienen Rigidbody ni collider: el vuelo es una parabola hecha a mano y el
// cobro es una distancia al jugador, mas barato que la fisica con cien monedas.
//
// Gira la raiz, siempre de frente a la camara, y el hijo "Modelo" es solo lo que
// se ve: para cambiar el modelo provisorio por otro asset se reemplaza ese hijo,
// con la cara de la moneda mirando hacia +Z.
public class Moneda : MonoBehaviour
{
    [Header("Vuelo")]
    public float velocidadVertical = 8f;                         // hacia arriba al salir, con un 25% de azar
    public Vector2 velocidadHorizontal = new Vector2(2.5f, 5f);  // minima y maxima, hacia un costado al azar
    public float frenadoHorizontal = 1.5f;                       // llega a velocidad / frenado metros
    public float gravedad = 14f;
    public float velocidadMaximaDeCaida = 3f;                    // lo que la hace caer lento
    public float rebote = 3.5f;                                  // velocidad hacia arriba del unico rebote
    public float altura = 0.3f;                                  // altura del centro de la moneda en el piso
    public float margenContraParedes = 0.3f;                     // como minimo, cae a esto de una pared

    [Header("En el piso")]
    public float velocidadDeGiro = 360f;                         // grados por segundo; volando gira el triple
    public float flotacion = 0.06f;                              // cuanto sube y baja quieta en el piso
    public float vida = 20f;                                     // segundos antes de desaparecer
    public float parpadeoFinal = 3f;
    public float frecuenciaParpadeo = 8f;

    [Header("Iman")]
    public float radioIman = 0f;                                 // sin AplicarMejoras; en partida manda la mejora de iman (0 sin comprarla)
    public float esperaAntesDelIman = 0.5f;                      // para que se vea la fuente antes de que vuelen
    public float aceleracionIman = 60f;
    public float velocidadMaximaIman = 40f;
    public float distanciaDeCobro = 0.8f;                        // sin iman, tambien la distancia a la que hay que pasar para agarrarla

    [Header("Cobro")]
    public AudioClip sonido;
    [Range(0f, 1f)] public float volumen = 0.6f;
    public float afinacion = 0f;                                 // semitonos que llevan la nota del sonido a la bemol

    // Cada moneda agarrada toca una nota de la escala de la bemol mayor, sorteada
    // con estos pesos (la probabilidad de cada una es su peso sobre la suma). Las
    // del acorde salen mas seguido, asi una lluvia de monedas suena consonante.
    public Nota[] notas =
    {
        new Nota("La bemol", 0, 3f),
        new Nota("Si bemol", 2, 1f),
        new Nota("Do", 4, 2f),
        new Nota("Re bemol", 5, 1f),
        new Nota("Mi bemol", 7, 2f),
        new Nota("Fa", 9, 1f),
        new Nota("Sol", 11, 0.5f),
        new Nota("La bemol agudo", 12, 1.5f),
    };

    public ParticleSystem brilloPrefab;
    public int particulasPorCobro = 8;

    [Header("Techo")]
    public int maxMonedasEnEscena = 150;
    public int maxMonedasEnEscenaMovil = 80;

    private const float SeparacionEntreSonidos = 0.05f;

    private static readonly Stack<Moneda> pool = new Stack<Moneda>();
    private static int enEscena;
    private static float radioImanDeLaPartida = -1f;

    // Para el raycast de salida. No guarda nada entre una moneda y otra, asi que no
    // va en el reset.
    private static readonly RaycastHit[] golpesDeSalida = new RaycastHit[8];

    // Cuantas monedas se cobraron desde que arranco el juego. AnilloIman lo mira
    // para latir con cada una, sin eventos, como la tienda con Progreso.Revision.
    public static int Cobros { get; private set; }

    // Las que estan en la escena ahora, volando o en el piso.
    public static int MonedasEnEscena
    {
        get { return enEscena; }
    }
    private static PlayerController jugador;
    private static int frameDeBusqueda = -1;
    private static Transform camara;
    private static ParticleSystem brillo;

    // Los static sobreviven al cambio de escena y al "enter play mode" sin domain
    // reload. Lo que es de la escena (jugador, camara, brillo y las
    // monedas del pool) se destruye con ella: se vuelve a buscar al notarlo null.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetearEstadoCompartido()
    {
        pool.Clear();
        enEscena = 0;
        radioImanDeLaPartida = -1f;
        Cobros = 0;
        jugador = null;
        frameDeBusqueda = -1;
        camara = null;
        brillo = null;
    }

    private double valor;
    private bool enUso;
    private Vector3 velocidad;
    private float salioEn;
    private float venceEn;
    private float aterrizoEn;
    private float angulo;
    private float rapidezIman;
    private bool reboto;
    private bool enElPiso;
    private bool atraida;
    private Renderer[] renderers;

    // Suelta 'cantidad' monedas de 'valor' cada una. Si el techo no deja soltarlas
    // todas, las que entran se reparten el valor de las que no: no se pierde nada.
    public static void Soltar(Moneda prefab, Vector3 origen, int cantidad, double valor)
    {
        if (prefab == null || cantidad <= 0 || valor <= 0) return;

        int techo = Plataforma.EsMovil ? prefab.maxMonedasEnEscenaMovil : prefab.maxMonedasEnEscena;
        int entran = Mathf.Clamp(techo - enEscena, 1, cantidad);
        double valorPorMoneda = valor * cantidad / entran;

        for (int i = 0; i < entran; i++)
        {
            Obtener(prefab).Salir(origen, valorPorMoneda);
        }
    }

    // El alcance del iman en esta partida: lo fija AplicarMejoras al empezar, con la
    // mejora de iman. Mientras nadie lo fije vale el radioIman del prefab. No hay
    // otro iman: al terminar la oleada las monedas se quedan donde cayeron, y
    // juntarlas es parte del juego.
    public static void FijarRadioIman(float radio)
    {
        radioImanDeLaPartida = Mathf.Max(0f, radio);
    }

    public static float RadioImanDeLaPartida
    {
        get { return radioImanDeLaPartida; }
    }

    private static Moneda Obtener(Moneda prefab)
    {
        // Al cambiar de escena las monedas guardadas se destruyen y en la pila
        // quedan referencias muertas. Se descartan antes de usarlas.
        Moneda moneda = null;
        while (moneda == null && pool.Count > 0) moneda = pool.Pop();

        if (moneda == null) return Instantiate(prefab);

        moneda.gameObject.SetActive(true);
        return moneda;
    }

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>(true);
    }

    private void Salir(Vector3 origen, double valorMoneda)
    {
        valor = valorMoneda;
        enUso = true;
        enEscena++;
        salioEn = Time.time;
        venceEn = salioEn + vida;
        reboto = false;
        enElPiso = false;
        atraida = false;
        rapidezIman = 0f;
        angulo = Random.value * 360f;

        float direccion = Random.value * Mathf.PI * 2f;
        float rapidez = Random.Range(velocidadHorizontal.x, velocidadHorizontal.y);
        velocidad = new Vector3(
            Mathf.Cos(direccion) * rapidez,
            velocidadVertical * Random.Range(0.75f, 1.25f),
            Mathf.Sin(direccion) * rapidez);

        origen.y = Mathf.Max(origen.y, altura);
        velocidad = FrenarAntesDeLasParedes(origen, velocidad);
        Ubicar(origen, 0f);
        Mostrar(true);
    }

    // Las monedas no tienen collider y su vuelo no ve las paredes: una que salia hacia
    // el borde del mapa caia detras de la pared invisible, donde el jugador no llega,
    // y sin iman quedaba perdida. Antes de salir se mira hasta donde podria llegar (lo
    // maximo que recorre con el frenado: velocidad / frenado) y, si hay algo fijo en
    // el camino, se la frena para que caiga antes. Solo cuentan los colliders fijos:
    // ni un zombi ni una bala en vuelo la frenan.
    private Vector3 FrenarAntesDeLasParedes(Vector3 origen, Vector3 v)
    {
        Vector3 horizontal = new Vector3(v.x, 0f, v.z);
        float rapidez = horizontal.magnitude;
        if (rapidez <= 0.001f || frenadoHorizontal <= 0f) return v;

        Vector3 direccion = horizontal / rapidez;
        float alcance = rapidez / frenadoHorizontal + margenContraParedes;
        int cantidad = Physics.RaycastNonAlloc(origen, direccion, golpesDeSalida, alcance, ~0, QueryTriggerInteraction.Ignore);

        float libre = alcance;
        for (int i = 0; i < cantidad; i++)
        {
            Collider golpeado = golpesDeSalida[i].collider;
            if (golpeado.attachedRigidbody != null || golpeado.GetComponentInParent<BulletController>() != null) continue;
            libre = Mathf.Min(libre, golpesDeSalida[i].distance);
        }
        if (libre >= alcance) return v;

        float nuevaRapidez = Mathf.Max(0f, libre - margenContraParedes) * frenadoHorizontal;
        v.x = direccion.x * nuevaRapidez;
        v.z = direccion.z * nuevaRapidez;
        return v;
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        Vector3 posicion = transform.position;
        PlayerController objetivo = Jugador();

        if (objetivo == null)
        {
            // Sin jugador (murio) no hay a quien volar: la que venia volando cae.
            if (atraida) { atraida = false; enElPiso = false; velocidad = Vector3.zero; }
        }
        else if (!atraida && Time.time - salioEn >= esperaAntesDelIman)
        {
            Vector3 distancia = objetivo.transform.position - posicion;
            distancia.y = 0f;
            // Sin la mejora de iman el radio es 0 y se agarran igual, pasandoles por
            // encima: nunca menos que la distancia de cobro.
            float radio = Mathf.Max(radioImanDeLaPartida >= 0f ? radioImanDeLaPartida : radioIman, distanciaDeCobro);
            if (distancia.sqrMagnitude <= radio * radio)
            {
                atraida = true;
                Mostrar(true);
            }
        }

        if (atraida)
        {
            Vector3 hacia = objetivo.transform.position + Vector3.up * 0.5f - posicion;
            float distancia = hacia.magnitude;
            rapidezIman = Mathf.Min(rapidezIman + aceleracionIman * dt, velocidadMaximaIman);
            float paso = Mathf.Min(rapidezIman * dt, distancia);

            if (distancia - paso <= distanciaDeCobro)
            {
                Cobrar(posicion);
                return;
            }
            posicion += hacia / distancia * paso;
        }
        else if (!enElPiso)
        {
            Volar(ref posicion, dt);
        }
        else
        {
            posicion.y = altura + Mathf.Sin((Time.time - aterrizoEn) * 5f) * flotacion;

            float restante = venceEn - Time.time;
            if (restante <= 0f)
            {
                Devolver();
                return;
            }
            if (restante <= parpadeoFinal)
            {
                Mostrar(Mathf.FloorToInt(restante * frecuenciaParpadeo) % 2 == 0);
            }
        }

        Ubicar(posicion, dt);
    }

    private void Volar(ref Vector3 posicion, float dt)
    {
        velocidad.y = Mathf.Max(velocidad.y - gravedad * dt, -velocidadMaximaDeCaida);
        float frenado = Mathf.Exp(-frenadoHorizontal * dt);
        velocidad.x *= frenado;
        velocidad.z *= frenado;
        posicion += velocidad * dt;

        if (posicion.y > altura || velocidad.y > 0f) return;

        posicion.y = altura;
        if (!reboto)
        {
            reboto = true;
            velocidad.y = rebote;
            return;
        }

        enElPiso = true;
        aterrizoEn = Time.time;
    }

    // De frente a la camara y girando alrededor de su vertical: se ve como una
    // moneda clasica que se afina y se ensancha. Volando gira mas rapido.
    private void Ubicar(Vector3 posicion, float dt)
    {
        angulo += velocidadDeGiro * (enElPiso ? 1f : 3f) * dt;

        Transform cam = Camara();
        Quaternion frente = cam != null ? Quaternion.LookRotation(cam.forward, cam.up) : Quaternion.identity;
        transform.SetPositionAndRotation(posicion, frente * Quaternion.Euler(0f, angulo, 0f));
    }

    private void Cobrar(Vector3 posicion)
    {
        Progreso.Sumar(valor);
        Cobros++;
        Brillar(posicion);
        Sonar();
        Devolver();
    }

    private void Brillar(Vector3 posicion)
    {
        if (brilloPrefab == null) return;
        if (brillo == null) brillo = Instantiate(brilloPrefab);

        var parametros = new ParticleSystem.EmitParams { position = posicion, applyShapeToPosition = true };
        brillo.Emit(parametros, particulasPorCobro);
    }

    // Monedas que llegan en el mismo frame sonarian una encima de otra: se deja
    // pasar un sonido cada 50 ms.
    private void Sonar()
    {
        Sonidos.Tocar(sonido, volumen, Mathf.Pow(2f, (SortearSemitonos(notas) + afinacion) / 12f), 0f, SeparacionEntreSonidos);
    }

    [System.Serializable]
    public class Nota
    {
        public string nombre;
        public int semitonos;       // sobre la bemol del sonido
        public float peso = 1f;     // relativo a los de las otras notas; 0 no sale nunca

        public Nota() { }

        public Nota(string nombre, int semitonos, float peso)
        {
            this.nombre = nombre;
            this.semitonos = semitonos;
            this.peso = peso;
        }
    }

    // Elige una nota con probabilidad proporcional a su peso. Sin ninguna con peso,
    // suena la nota del sonido tal cual.
    public static int SortearSemitonos(Nota[] notas)
    {
        float total = 0f;
        foreach (var nota in notas) total += Mathf.Max(0f, nota.peso);
        if (total <= 0f) return 0;

        float sorteo = Random.value * total;
        int ultimaConPeso = 0;
        foreach (var nota in notas)
        {
            if (nota.peso <= 0f) continue;
            ultimaConPeso = nota.semitonos;
            sorteo -= nota.peso;
            if (sorteo <= 0f) return nota.semitonos;
        }

        // Random.value puede dar 1 justo, y con el redondeo el sorteo queda apenas
        // por encima de cero despues de la ultima.
        return ultimaConPeso;
    }

    // Con el jugador muerto no hay nada que encontrar: se busca una sola vez por
    // frame y no una vez por moneda.
    private static PlayerController Jugador()
    {
        if (jugador == null && frameDeBusqueda != Time.frameCount)
        {
            frameDeBusqueda = Time.frameCount;
            jugador = FindFirstObjectByType<PlayerController>();
        }
        return jugador;
    }

    private static Transform Camara()
    {
        if (camara == null && Camera.main != null) camara = Camera.main.transform;
        return camara;
    }

    private void Devolver()
    {
        if (!enUso) return;

        enUso = false;
        enEscena--;
        gameObject.SetActive(false);
        pool.Push(this);
    }

    // Las que estan en uso cuando se descarga la escena no pasan por Devolver:
    // sin esto el techo quedaria contando monedas que ya no existen.
    private void OnDestroy()
    {
        if (enUso) enEscena--;
    }

    private void Mostrar(bool visible)
    {
        foreach (var r in renderers) r.enabled = visible;
    }
}
