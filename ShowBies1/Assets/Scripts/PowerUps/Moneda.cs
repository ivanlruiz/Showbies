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

    // La escalera: las monedas agarradas seguidas (sin que pasen mas de ventanaEscalera
    // segundos entre una y otra) suben la escala de la bemol mayor grado por grado, y al
    // llegar arriba siguen dando vueltas por la octava de arriba. Pasar por encima de un
    // monton de monedas es una melodia que pide mas. Si se corta, vuelve a la bemol.
    // Antes cada moneda sorteaba una nota: sonaba lindo pero no llevaba a ningun lado.
    public float ventanaEscalera = 0.45f;
    [Tooltip("Veces que brillan de mas al completar cada octava.")]
    public int brilloDeOctava = 3;

    public ParticleSystem brilloPrefab;
    public int particulasPorCobro = 8;

    [Header("Techo")]
    public int maxMonedasEnEscena = 150;
    public int maxMonedasEnEscenaMovil = 80;
    [Tooltip("Desde cuantas monedas una muerte es una lluvia (hoy solo la del jefe, 30 a 40; el tanque suelta 8 como mucho): sale entera aunque el techo este lleno.")]
    public int lluviaDesde = 10;

    private const float SeparacionEntreSonidos = 0.05f;

    // La bemol mayor en semitonos, dos octavas.
    private static readonly int[] Escala = { 0, 2, 4, 5, 7, 9, 11, 12, 14, 16, 17, 19, 21, 23, 24 };
    private const int PrimerGradoDeArriba = 7;
    private static int escalon = -1;
    private static float ultimaNotaEn = float.NegativeInfinity;

    private static readonly Stack<Moneda> pool = new Stack<Moneda>();
    // Las que estan en uso, sin orden: el techo las cuenta y, lleno, busca aca la mas
    // vieja (ver Soltar). Cada una sabe su lugar, asi sacarla no recorre la lista.
    private static readonly List<Moneda> enEscena = new List<Moneda>();
    private static float radioImanDeLaPartida = -1f;

    // Para el raycast de salida. No guarda nada entre una moneda y otra, asi que no
    // va en el reset.
    private static readonly RaycastHit[] golpesDeSalida = new RaycastHit[8];

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
        enEscena.Clear();
        radioImanDeLaPartida = -1f;
        jugador = null;
        frameDeBusqueda = -1;
        camara = null;
        brillo = null;
        escalon = -1;
        ultimaNotaEn = float.NegativeInfinity;
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
    private int lugarEnEscena = -1;
    private Renderer[] renderers;

    // Suelta 'cantidad' monedas de 'valor' cada una sin pasarse del techo de monedas en
    // escena, que esta por rendimiento. Si el techo no deja soltarlas todas, las que salen
    // se reparten el valor de las que no: no se pierde nada. Igual sale al menos una, para
    // que se vea que el zombi solto algo, y una lluvia (la del jefe) sale entera: el lugar
    // lo hacen las mas viejas del piso, que se van como si vencieran (ver LaMasVieja).
    // Antes, con el techo lleno, cada muerte sumaba una de mas, asi que en las oleadas mas
    // pesadas el techo del telefono (80) no atajaba nada, y la lluvia del jefe salia como
    // una o dos monedas iguales a las demas.
    public static void Soltar(Moneda prefab, Vector3 origen, int cantidad, double valor)
    {
        if (prefab == null || cantidad <= 0 || valor <= 0) return;

        int techo = Plataforma.EsMovil ? prefab.maxMonedasEnEscenaMovil : prefab.maxMonedasEnEscena;
        int hacenLugar;
        int salen = CuantasSalen(cantidad, enEscena.Count, techo, prefab.lluviaDesde, out hacenLugar);
        for (int i = 0; i < hacenLugar; i++)
        {
            Moneda vieja = LaMasVieja();
            if (vieja == null) break;   // todas vuelan al jugador: se pasa del techo un instante
            vieja.Devolver();
        }

        double valorPorMoneda = valor * cantidad / salen;
        origen = SacarDeLoTapado(origen, prefab.margenContraParedes);
        for (int i = 0; i < salen; i++)
        {
            Obtener(prefab).Salir(origen, valorPorMoneda);
        }
    }

    // Cuantas salen de una muerte que suelta 'cantidad' con 'enElPiso' monedas en escena, y
    // cuantas de las del piso se van para hacerles lugar. Estatico para probarlo sin escena.
    public static int CuantasSalen(int cantidad, int enElPiso, int techo, int lluviaDesde, out int hacenLugar)
    {
        int minimo = cantidad >= lluviaDesde ? cantidad : 1;
        int salen = Mathf.Clamp(techo - enElPiso, minimo, cantidad);
        // Pasado del techo (si alguna vez no hubo a quien sacar) no se sacan de mas: baja solo.
        hacenLugar = Mathf.Max(0, Mathf.Min(salen, enElPiso + salen - techo));
        return salen;
    }

    // La que se va para hacer lugar: la mas vieja, que es la que iba a vencer primero, y
    // nunca una que ya vuela hacia el jugador. Se va con lo que valia, como si venciera
    // antes. Pasarle su valor a la nueva rescataria lo que hoy vence sin que nadie lo junte:
    // en una simulacion del techo del telefono lleno, con la mezcla de WaveMode a su ritmo
    // mas alto, lo cobrado subia un 10-30 %, y yendose con su valor queda como estaba.
    // Juntarlas es parte del juego: por eso no hay iman global.
    private static Moneda LaMasVieja()
    {
        Moneda vieja = null;
        for (int i = 0; i < enEscena.Count; i++)
        {
            Moneda moneda = enEscena[i];
            if (moneda == null || moneda.atraida) continue;
            if (vieja == null || moneda.salioEn < vieja.salioEn) vieja = moneda;
        }
        return vieja;
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
        // De noche, con la luz de relleno de los personajes: sin ella el dorado se apagaba.
        Personajes.PonerEnLaCapa(gameObject);
    }

    private void Salir(Vector3 origen, double valorMoneda)
    {
        valor = valorMoneda;
        enUso = true;
        lugarEnEscena = enEscena.Count;
        enEscena.Add(this);
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
        // Los edificios de la ciudad no tienen collider, pero la taparian igual: se frena
        // antes, como contra una pared.
        libre = LibreHastaLoTapado(origen, direccion, libre);
        if (libre >= alcance) return v;

        float nuevaRapidez = Mathf.Max(0f, libre - margenContraParedes) * frenadoHorizontal;
        v.x = direccion.x * nuevaRapidez;
        v.z = direccion.z * nuevaRapidez;
        return v;
    }

    // Un zombi que muere cruzando un edificio de la ciudad (no tienen collider: los zombis
    // se trabarian) soltaba las monedas adentro, donde el techo las tapa y sin iman habia
    // que entrar a buscarlas a ciegas. Salen por el borde mas cercano de lo que tapa el
    // edificio (ver CapitulosDeEscenario.Tapado), a 'margen' de el; afuera, nada cambia. Las
    // zonas no se tocan (entre dos edificios hay una calle): con salir de una alcanza.
    // Publico para la prueba de logica.
    public static Vector3 SacarDeLoTapado(Vector3 punto, float margen)
    {
        var tapado = CapitulosDeEscenario.LoTapado;
        for (int i = 0; i < tapado.Count; i++)
        {
            Rect zona = tapado[i];
            float oeste = zona.xMin - margen, este = zona.xMax + margen;
            float sur = zona.yMin - margen, norte = zona.yMax + margen;
            if (punto.x <= oeste || punto.x >= este || punto.z <= sur || punto.z >= norte) continue;

            float hastaOeste = punto.x - oeste, hastaEste = este - punto.x;
            float hastaSur = punto.z - sur, hastaNorte = norte - punto.z;
            float menor = Mathf.Min(Mathf.Min(hastaOeste, hastaEste), Mathf.Min(hastaSur, hastaNorte));
            if (menor == hastaOeste) punto.x = oeste;
            else if (menor == hastaEste) punto.x = este;
            else if (menor == hastaSur) punto.z = sur;
            else punto.z = norte;
            return punto;
        }
        return punto;
    }

    // Cuanto puede avanzar desde 'origen' hacia 'direccion' (horizontal y de largo 1) sin
    // entrar en lo que tapa un edificio, hasta 'libre'. El rayo contra cada rectangulo, eje
    // por eje: se entra donde empiezan a coincidir el tramo entre los dos bordes de x y el
    // tramo entre los dos de z. Publico para la prueba de logica.
    public static float LibreHastaLoTapado(Vector3 origen, Vector3 direccion, float libre)
    {
        var tapado = CapitulosDeEscenario.LoTapado;
        for (int i = 0; i < tapado.Count; i++)
        {
            Rect zona = tapado[i];
            float entra = 0f, sale = libre;
            if (Tramo(origen.x, direccion.x, zona.xMin, zona.xMax, ref entra, ref sale) &&
                Tramo(origen.z, direccion.z, zona.yMin, zona.yMax, ref entra, ref sale))
            {
                libre = entra;
            }
        }
        return libre;
    }

    // Recorta [entra, sale] a lo que el rayo pasa entre 'min' y 'max' en un eje. Falso si
    // no queda nada.
    private static bool Tramo(float desde, float paso, float min, float max, ref float entra, ref float sale)
    {
        if (Mathf.Abs(paso) < 1e-6f) return desde >= min && desde <= max;
        float a = (min - desde) / paso, b = (max - desde) / paso;
        if (a > b) { float c = a; a = b; b = c; }
        entra = Mathf.Max(entra, a);
        sale = Mathf.Min(sale, b);
        return entra <= sale;
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
        bool octava = Sonar();
        Brillar(posicion, octava ? particulasPorCobro * Mathf.Max(1, brilloDeOctava) : particulasPorCobro);
        Devolver();
    }

    private void Brillar(Vector3 posicion, int particulas)
    {
        if (brilloPrefab == null) return;
        if (brillo == null) brillo = Instantiate(brilloPrefab);

        var parametros = new ParticleSystem.EmitParams { position = posicion, applyShapeToPosition = true };
        brillo.Emit(parametros, particulas);
    }

    // El grado que sigue en la escalera. Estatico para probarlo sin escena.
    public static int GradoSiguiente(int escalonActual, float segundosDesdeLaAnterior, float ventana)
    {
        if (escalonActual < 0 || segundosDesdeLaAnterior > ventana) return 0;
        int siguiente = escalonActual + 1;
        return siguiente < Escala.Length ? siguiente : PrimerGradoDeArriba;
    }

    public static int SemitonosDelGrado(int grado)
    {
        return Escala[Mathf.Clamp(grado, 0, Escala.Length - 1)];
    }

    // Monedas que llegan en el mismo frame sonarian una encima de otra: se deja pasar
    // un sonido cada 50 ms, y la que no suena no sube la escalera. Devuelve si esta
    // nota completo una octava (para brillar de mas).
    private bool Sonar()
    {
        int grado = GradoSiguiente(escalon, Time.unscaledTime - ultimaNotaEn, ventanaEscalera);
        float pitch = Mathf.Pow(2f, (SemitonosDelGrado(grado) + afinacion) / 12f);
        if (!Sonidos.Tocar(sonido, volumen, pitch, 0f, SeparacionEntreSonidos)) return false;

        escalon = grado;
        ultimaNotaEn = Time.unscaledTime;
        return grado > 0 && SemitonosDelGrado(grado) % 12 == 0;
    }


    // Con el jugador muerto no hay nada que encontrar: se busca una sola vez por
    // frame y no una vez por moneda.
    private static PlayerController Jugador()
    {
        // Muerto no junta nada. Antes eso lo resolvia que el jugador se destruia al morir;
        // ahora se queda en el mundo, que sigue andando detras de la derrota, y las
        // monedas del piso seguian volando hacia el cuerpo y se cobraban despues de
        // morir, con la derrota ya mostrando el total.
        if (PlayerHealth.instance != null && PlayerHealth.instance.EstaMuerto) return null;

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
        DejarDeContar();
        gameObject.SetActive(false);
        pool.Push(this);
    }

    // Sale de la cuenta del techo: la ultima de la lista pasa a su lugar.
    private void DejarDeContar()
    {
        int ultima = enEscena.Count - 1;
        if (lugarEnEscena >= 0 && lugarEnEscena <= ultima && ReferenceEquals(enEscena[lugarEnEscena], this))
        {
            Moneda otra = enEscena[ultima];
            enEscena[lugarEnEscena] = otra;
            otra.lugarEnEscena = lugarEnEscena;
            enEscena.RemoveAt(ultima);
        }
        lugarEnEscena = -1;
    }

    // Las que estan en uso cuando se descarga la escena no pasan por Devolver:
    // sin esto el techo quedaria contando monedas que ya no existen.
    private void OnDestroy()
    {
        if (enUso) DejarDeContar();
    }

    private void Mostrar(bool visible)
    {
        foreach (var r in renderers) r.enabled = visible;
    }
}
