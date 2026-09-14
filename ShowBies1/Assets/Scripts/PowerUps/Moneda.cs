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
    public float altura = 0.5f;                                  // altura del centro de la moneda en el piso

    [Header("En el piso")]
    public float velocidadDeGiro = 360f;                         // grados por segundo; volando gira el triple
    public float flotacion = 0.1f;                               // cuanto sube y baja quieta en el piso
    public float vida = 20f;                                     // segundos antes de desaparecer
    public float parpadeoFinal = 3f;
    public float frecuenciaParpadeo = 8f;

    [Header("Iman")]
    public float radioIman = 4f;
    public float esperaAntesDelIman = 0.5f;                      // para que se vea la fuente antes de que vuelen
    public float aceleracionIman = 60f;
    public float velocidadMaximaIman = 40f;
    public float distanciaDeCobro = 0.8f;

    [Header("Cobro")]
    public AudioClip sonido;
    [Range(0f, 1f)] public float volumen = 0.6f;
    public float ventanaCombo = 0.6f;                            // agarrada antes de esto, suena un tono mas arriba
    public ParticleSystem brilloPrefab;
    public int particulasPorCobro = 8;

    [Header("Techo")]
    public int maxMonedasEnEscena = 150;
    public int maxMonedasEnEscenaMovil = 80;

    // Tonos de la escala que sube al juntar monedas seguidas, en semitonos
    // (pentatonica mayor): juntar una fuente entera suena como una escala.
    private static readonly int[] escala = { 0, 2, 4, 7, 9, 12 };
    private const int CantidadDeFuentes = 8;
    private const float SeparacionEntreSonidos = 0.05f;

    private static readonly Stack<Moneda> pool = new Stack<Moneda>();
    private static int enEscena;
    private static int rondaDeAtraccion;
    private static PlayerController jugador;
    private static int frameDeBusqueda = -1;
    private static Transform camara;
    private static ParticleSystem brillo;
    private static AudioSource[] fuentes;
    private static int proximaFuente;
    private static float ultimoSonido = float.NegativeInfinity;
    private static int combo;

    // Los static sobreviven al cambio de escena y al "enter play mode" sin domain
    // reload. Lo que es de la escena (jugador, camara, brillo, fuentes y las
    // monedas del pool) se destruye con ella: se vuelve a buscar al notarlo null.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetearEstadoCompartido()
    {
        pool.Clear();
        enEscena = 0;
        rondaDeAtraccion = 0;
        jugador = null;
        frameDeBusqueda = -1;
        camara = null;
        brillo = null;
        fuentes = null;
        proximaFuente = 0;
        ultimoSonido = float.NegativeInfinity;
        combo = 0;
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
    private int rondaAlSalir;
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

    // Las monedas que ya estan en la escena vuelan al jugador, esten donde esten.
    // Las que salgan despues no.
    public static void AtraerTodas()
    {
        rondaDeAtraccion++;
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
        rondaAlSalir = rondaDeAtraccion;
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
        Ubicar(origen, 0f);
        Mostrar(true);
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
            if (rondaAlSalir < rondaDeAtraccion || distancia.sqrMagnitude <= radioIman * radioIman)
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

    // Una fuente de audio por cobro sonaria una encima de otra con monedas que
    // llegan en el mismo frame: se deja pasar un sonido cada 50 ms, y cada uno que
    // sigue al anterior dentro de la ventana sube un tono en la escala.
    private void Sonar()
    {
        if (sonido == null) return;

        float ahora = Time.time;
        if (ahora - ultimoSonido < SeparacionEntreSonidos) return;

        combo = ahora - ultimoSonido <= ventanaCombo ? combo + 1 : 0;
        ultimoSonido = ahora;

        AudioSource fuente = Fuente();
        fuente.pitch = Mathf.Pow(2f, escala[Mathf.Min(combo, escala.Length - 1)] / 12f);
        fuente.PlayOneShot(sonido, volumen);
    }

    // PlayOneShot usa el pitch de la fuente, asi que cada tono necesita su propia
    // fuente mientras suena: se rotan varias.
    private static AudioSource Fuente()
    {
        if (fuentes == null || fuentes[0] == null)
        {
            var go = new GameObject("SonidoMonedas");
            fuentes = new AudioSource[CantidadDeFuentes];
            for (int i = 0; i < fuentes.Length; i++)
            {
                fuentes[i] = go.AddComponent<AudioSource>();
                fuentes[i].playOnAwake = false;
                fuentes[i].spatialBlend = 0f;
            }
        }

        proximaFuente = (proximaFuente + 1) % fuentes.Length;
        return fuentes[proximaFuente];
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
