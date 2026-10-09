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
    [Tooltip("Segundos dentro de Z_attack_A en que la mano llega adelante del todo: el cuadro en que el zarpazo conecta. Se midio muestreando el clip (la mano derecha va hacia atras hasta 0,27 s, arriba en 0,30 y adelante del todo en 0,37); si se cambia el clip, hay que volver a medirlo.")]
    [SerializeField] private float momentoDelImpacto = 0.37f;
    [Tooltip("Cuanto se ve venir el golpe: lo que pasa entre que el zombi toca al jugador y el brazo baja. El clip arranca adelantado para que el impacto caiga justo ahi.")]
    [SerializeField] private float anticipacionDelGolpe = 0.2f;
    [Tooltip("Cuanto mas lejos que al empezar el zarpazo puede estar el jugador y que igual le llegue, en metros a escala 1: en el impacto el brazo se estira casi un metro.")]
    [SerializeField] private float alcanceDelBrazo = 0.5f;

    private float proximoGolpe;

    // El zarpazo en curso. Arranca al tocar al jugador y pega cuando el brazo baja, no
    // al tocarlo: hasta el 23/9 el daño entraba en el acto y recien despues arrancaba
    // la animacion, asi que el jugador veia el borde rojo y la vida bajar, y el brazo
    // conectaba 0,37 s mas tarde, contra el aire.
    private bool golpeEnCurso;
    private float golpeImpactaEn;
    private float danoDelGolpe;
    private float distanciaAlEmpezar;

    [Header("Animacion")]
    [Tooltip("Ritmo del paso. Todos usan el modelo del zombi normal a distinta escala: el paso tiene que ir con lo que camina cada uno (el tanque pesado, el FASTER frenetico). Solo afecta a andar; atacar y morir van siempre a 1.")]
    [SerializeField] private float velocidadDeAnimacion = 1f;

    [Tooltip("0 camina y 1 corre, y en el medio se mezclan. Los lentos (el tanque, el jefe) caminan: con el ciclo de correr a media velocidad parecen alguien corriendo en camara lenta, porque tiene los dos pies en el aire.")]
    [Range(0f, 1f)]
    [SerializeField] private float ritmoDeAndar = 1f;

    // Los del controller que arma ConstructorAnimaciones. Como hash, que es lo que
    // usa el Animator por dentro: si van por string, los convierte en cada llamada.
    private static readonly int idPaso = Animator.StringToHash("Paso");
    private static readonly int idRitmo = Animator.StringToHash("Ritmo");
    private static readonly int idAtacar = Animator.StringToHash("Atacar");
    private static readonly int idMorir = Animator.StringToHash("Morir");
    private static readonly int idAndar = Animator.StringToHash("Andar");
    private static readonly int idFestejar = Animator.StringToHash("Festejar");
    private static readonly int idFestejo = Animator.StringToHash("Festejo");

    [Tooltip("Lo que se queda el cadaver en escena mientras se desploma, antes de volver al pool. El clip de morir dura 1,36 s con la velocidad del estado.")]
    [SerializeField] private float duracionDeLaMuerte = 1.4f;

    [Header("Festejo (cuando el jugador muere)")]
    [Tooltip("Donde festejan a los costados del cuerpo, en metros sobre el piso a lo ancho de la pantalla (negativo es a la izquierda). Al morir la camara corre el cuerpo al costado izquierdo (CamaraJugador), fuera de la derrota: la pantalla termina a unos 5,8 m a su izquierda y los textos empiezan a unos 2,4 m a su derecha, en 16:9.")]
    [SerializeField] private Vector2 costadoDelFestejo = new Vector2(-5f, 1.8f);
    [Tooltip("Y a lo alto de la pantalla, en metros sobre el piso desde el cuerpo.")]
    [SerializeField] private Vector2 alturaDelFestejo = new Vector2(-4.5f, 4.5f);
    [Tooltip("Lo mas cerca del cuerpo que se paran a festejar.")]
    [SerializeField] private float cercaDelCuerpo = 1.6f;
    [Tooltip("Cuanto se arquea hacia atras en cada festejo, en grados.")]
    [SerializeField] private float gradosDelFestejo = 22f;
    [Tooltip("Cuanto salta en cada puño en alto, como fraccion de su alto. Es lo unico que se lee desde la camara del juego: a esa distancia un zombi mide unos 40 px, el puño levantado apunta a la camara y el arco casi no cambia la silueta; lo que se ve es el zombi separandose de su sombra. Con 0,12 no se notaba.")]
    [SerializeField] private float saltoDelFestejo = 0.5f;

    [Header("Empujon al morir")]
    [Tooltip("A que velocidad sale despedido el cadaver del zombi normal en la direccion del tiro, en m/s. Los mas grandes, dividido por cuantas veces mas grandes que el normal son (ver VelocidadDelEmpuje): el tanque sale a la mitad y el jefe a un cuarto. Los mas chicos, como el normal.")]
    [SerializeField] private float empujeAlMorir = 5f;
    [Tooltip("Cuanto frena ese empujon, en m/s2. Con 5 y 14 el cadaver recorre unos 90 cm en un tercio de segundo.")]
    [SerializeField] private float frenadoDelEmpuje = 14f;
    [Tooltip("Cuanto se va de espaldas el cadaver al salir despedido, en grados. Se endereza con el empujon.")]
    [SerializeField] private float inclinacionDelEmpuje = 12f;

    // La escala de la raiz del zombi normal (Zombi.prefab), que es la vara del empujon: los
    // demas salen despedidos segun cuanto mas grandes que el son. Si cambia la del prefab,
    // la prueba de logica lo avisa.
    public const float EscalaDelNormal = 0.5f;

    // Un cadaver es una malla con huesos animandose: cuesta lo mismo que un zombi
    // vivo. Sin techo, una granada que mata a diez deja diez animandose encima de
    // los que siguen saliendo, justo en el momento de mas carga. Pasado el techo
    // se muere como antes, de golpe, que es lo que se veia hasta ahora.
    private const int MaxCadaveres = 12;
    private const int MaxCadaveresMovil = 5;
    private static int cadaveres;

    // El cadaver ya no cuenta como vivo ni se le puede pegar, pero el GameObject
    // sigue prendido hasta que termina de caerse.
    private bool desplomandose;
    private float sacarloEn;
    private Collider[] colliders;
    private CapsuleCollider capsula;

    // Lo que mide de radio el cuerpo en el piso (la capsula de la raiz, con su escala): lo que
    // deja de margen al rodear un edificio de la ciudad (ver Rodeo).
    private float radioDelCuerpo = 0.5f;

    // Hasta cuando (en tiempo de fisica) cuenta que esta tocando al jugador: lo anota cada
    // OnCollisionStay, y lo mira Apartarse en el paso siguiente.
    private float tocaAlJugadorHasta = -1f;

    // Los colliders de los zombis, sin friccion (ver Apartarse). Uno para todos, armado la primera
    // vez que hace falta.
    private static PhysicsMaterial sinFriccion;

    // Lo que le falta bajar al cadaver hasta el piso, y a que velocidad baja. El cadaver
    // queda kinematic donde muere, y uno que muere en el aire (subido encima de la horda,
    // despedido por la embestida del jefe, recien aparecido) se desplomaba flotando hasta
    // irse, a la altura en que lo mataron.
    private float caidaDelCadaver;
    private float velocidadDeCaida;

    // El festejo, pedido de Ivan: con el jugador muerto la partida sigue andando detras
    // de la derrota (DerrotaEnLaPartida), y la horda festeja. Cada zombi va a un lugar
    // alrededor del cuerpo, en la franja de la pantalla que la derrota deja libre (la
    // camara corre el cuerpo a un costado, CamaraJugador.MostrarElCuerpo), se da vuelta
    // a mirarlo y, en oleadas cada PeriodoDelFestejo, salta tres veces
    // con el puño en alto y el cuerpo arqueado hacia atras. Festejan callados (pedido de
    // Ivan): hasta el 27/9 rugian con el estruendo del jefe, y sonaba a que estaba el jefe.
    const float PeriodoDelFestejo = 2.4f;
    const float DuracionDelFestejo = 1.3f;
    const int PuñosPorFestejo = 3;
    const float DesfaseMaximo = 0.45f;       // para que no sea un baile sincronizado
    // Del muestreo de Z_attack_A (ver momentoDelImpacto): la mano derecha abajo al
    // principio, a la altura del hombro a los 0,15 s y por encima de la cabeza a los
    // 0,29 s, en un clip de 1,33 s. Normalizado, que es lo que pide el Motion Time.
    const float LargoDelZarpazo = 1.3333f;
    const float ManoAbajo = 0.02f / LargoDelZarpazo;
    const float ManoAlHombro = 0.15f / LargoDelZarpazo;
    const float ManoArriba = 0.29f / LargoDelZarpazo;

    private static float festejoDesde;

    private bool festejando;
    private bool tieneLugarDelFestejo;
    private Vector3 lugarDelFestejo;
    private float desfaseDelFestejo;

    // El lugar del festejo se sortea sin mirar si se puede pisar: cae detras de una pared
    // invisible del borde si el jugador murio cerca de ella, o adentro de un tanque o del
    // jefe, que un zombi chico no corre. Hasta el 25/9 esos quedaban corriendo contra eso
    // mientras durara la derrota, de espaldas al cuerpo y en la franja de la pantalla que la
    // camara deja para el festejo. Ahora, si en EsperaSinAcercarse no se acerca
    // AcercamientoMinimo a su lugar, prueba otro, y si tampoco llega, festeja donde esta.
    public const float EsperaSinAcercarse = 0.5f;
    public const float AcercamientoMinimo = 0.1f;
    private float masCercaDelLugar;       // lo mas cerca que estuvo del lugar al que va
    private float seAcercoEn;             // cuando se acerco por ultima vez
    private bool cambioDeLugar;           // si ya probo otro lugar

    private Transform modelo;
    private Vector3 posicionBaseDelModelo;
    private Quaternion rotacionBaseDelModelo;
    private Vector3 escalaBaseDelModelo;
    private float altoDelModelo = 1f;

    // Lo llama la derrota cuando aparece: desde ahi se cuentan las oleadas.
    public static void EmpezarFestejo()
    {
        festejoDesde = Time.time;
    }

    public bool Festejando => festejando;

    // De donde vino el golpe que lo mato, para que caiga hacia alla y no siempre
    // igual. Lo pasan la bala (su direccion) y la granada (del centro hacia afuera);
    // el kill-Z y el despeje no pasan nada y el cadaver cae donde esta.
    private Vector3 direccionDelEmpuje;
    private float velocidadDelEmpuje;
    private Quaternion rotacionAlMorir;

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

    // El techo de poblacion de la escena, que fija el generador en su Start (60, o 35 en
    // movil). Quien haga aparecer zombis fuera del generador -el jefe cuando invoca- lo
    // tiene que mirar con LugarParaZombis: sin eso, en el modo libre se juntan jefes
    // invocando y el telefono se traba. 0 es sin techo.
    public static int TechoDeZombis { get; private set; }

    public static void FijarTecho(int techo)
    {
        TechoDeZombis = Mathf.Max(0, techo);
    }

    // Cuantos mas entran sin pasarse del techo, sin contar los lugares reservados.
    public static int LugarParaZombis
    {
        get { return TechoDeZombis <= 0 ? int.MaxValue : Mathf.Max(0, TechoDeZombis - ZombisVivos - Reservados); }
    }

    // Los lugares que tiene apartados un jefe mientras avisa su invocacion. Mirar el lugar al
    // empezar el aviso no alcanzaba: en los 0,8 s del anillo el generador lo ocupaba, y con el
    // techo lleno el jefe hacia el aviso entero, rugido incluido, y no salia nadie
    // (superauditoria del 29/9). Los generadores los cuentan como ocupados.
    public static int Reservados { get; private set; }

    // Aparta hasta 'cuantos' lugares y devuelve cuantos pudo.
    public static int Reservar(int cuantos)
    {
        int dados = Mathf.Clamp(cuantos, 0, LugarParaZombis);
        Reservados += dados;
        return dados;
    }

    public static void Soltar(int cuantos)
    {
        Reservados = Mathf.Max(0, Reservados - Mathf.Max(0, cuantos));
    }

    // Los zombis se reusan, como las balas y las monedas: con decenas muriendo por
    // minuto, crear y destruir cada uno era basura para el recolector y trabajo
    // para la fisica en cada aparicion, y en los telefonos flojos se notaba en
    // tirones. El pool es por prefab: un tanque no vuelve como zombi normal.
    private static readonly Dictionary<GameObject, Stack<EnemyController>> pool = new Dictionary<GameObject, Stack<EnemyController>>();
    private static readonly List<EnemyController> jefes = new List<EnemyController>();
    private static int ultimaAparicion;

    private GameObject prefabDeOrigen;   // null si no salio del pool (el tutorial): al morir se destruye
    private bool enUso;
    // Los que tienen controller. El zombi rapido tiene DOS Animator (ver la trampa
    // del zombi invisible) y uno esta sin controller: mandarle un trigger a ese
    // loguea un warning por golpe.
    private Animator[] animadores;

    // Si sigue en juego. Un cadaver desplomandose dice que no: el objeto esta
    // prendido y visible, pero para el resto del juego el zombi ya murio.
    public bool Vivo { get { return enUso; } }

    // Distinto en cada aparicion aunque el objeto sea el mismo: lo anotan los que
    // guardan zombis para preguntar despues si murieron (ver SigueVivo).
    public int NumeroDeAparicion { get; private set; }

    // Contadores del botin, para el medidor de balance y las pruebas: cuantas
    // muertes soltaron monedas, cuanto valor se esperaba en promedio y cuanto
    // salio de verdad. Con muchas muertes los dos ultimos tienen que parecerse; si
    // no, el sorteo de monedas o la regla del multiplicador menor a 1 estan mal.
    public static int MuertesConBotin { get; private set; }

    // Cuantos zarpazos arrancaron y cuantos llegaron a pegar, para las pruebas. Con el
    // jugador quieto tienen que ser casi los mismos: si no, el alcance del brazo quedo
    // corto y los zombis erran sin que nadie esquive, que los haria mas debiles.
    public static int ZarpazosEmpezados { get; private set; }
    public static int ZarpazosQuePegaron { get; private set; }
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
        cadaveres = 0;
        TechoDeZombis = 0;
        Reservados = 0;
        jefes.Clear();
        pool.Clear();
        ultimaAparicion = 0;
        jugadorCache = null;
        sinFriccion = null;
        PasosApartado = 0;
        spritesDeSangre.Clear();
        MuertesConBotin = 0;
        ZarpazosEmpezados = 0;
        ZarpazosQuePegaron = 0;
        festejoDesde = 0f;
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

    public float DanoPorGolpe => enemyType.daño * multiplicadorDano * multiplicadorGolpe;

    // Lo sube quien quiera un golpe mas fuerte por un rato: el jefe mientras carga
    // (JefePatrones). Vuelve a 1 en cada aparicion.
    [System.NonSerialized] public float multiplicadorGolpe = 1f;

    // Pega en el acto al chocar, sin zarpazo: la embestida del jefe, donde el golpe es
    // el cuerpo. A 16 m/s, esperar a que baje un brazo lo dejaria pasar de largo sin
    // pegar, y la carga detecta que choco mirando GolpesDados. Lo prende y lo apaga
    // JefePatrones junto con multiplicadorGolpe; vuelve a falso en cada aparicion.
    [System.NonSerialized] public bool golpeaAlChocar;

    // Si puede arrancar zarpazos. El jefe los corta desde que avisa un ataque hasta que
    // vuelve a perseguir (JefePatrones): aturdido tiene que quedar "sin atacar", que es la
    // ventana para castigarlo, y hasta el 24/9 el que quedaba pegado a el se comia
    // zarpazos igual. No corta la embestida (golpeaAlChocar). Vuelve a verdadero en cada
    // aparicion.
    [System.NonSerialized] public bool puedeZarpar = true;

    // Para las pruebas: donde cae el impacto dentro del clip de atacar.
    public float MomentoDelImpacto => momentoDelImpacto;

    // Cuantas veces le pego al jugador en esta aparicion. La carga del jefe la mira para
    // saber si llego a chocarlo, sin meterse en como pega un zombi.
    public int GolpesDados { get; private set; }

    // Hace aparecer un zombi de ese prefab: uno apagado del pool si hay, o uno
    // nuevo. Quien lo llama le pone despues los multiplicadores, como antes con
    // Instantiate: la vida se calcula perezosa, con los que tenga en el primer golpe.
    // Lo marca quien lo hace aparecer (el jefe de la oleada o el BOSS del modo libre).
    // Vuelve a falso en cada aparicion.
    //
    // La lista de los que hay se lleva desde aca y no desde OnEnable porque el zombi se
    // marca despues de aparecer: la mira BarraDelJefe para saber a quien mostrar.
    public bool EsJefe
    {
        get { return esJefe; }
        set
        {
            if (esJefe == value) return;
            esJefe = value;
            if (value)
            {
                if (!jefes.Contains(this)) jefes.Add(this);
                PisarLoChico(CapitulosDeEscenario.Chicos);
            }
            else
            {
                jefes.Remove(this);
            }
        }
    }

    public static IReadOnlyList<EnemyController> Jefes
    {
        get { return jefes; }
    }

    // El jefe pisa lo chico (pedido de Ivan, 9/10: "es un gigante"): atraviesa las lapidas y
    // las cruces del cementerio, que entre dos no le dejan lugar, y lo frenan los edificios, los
    // autos y los arboles. Unity olvida un IgnoreCollision cuando se apaga cualquiera de los dos
    // colliders (el jefe vuelve al pool al morir y el decorado se apaga al cambiar de
    // capitulo), asi que se vuelve a pedir al marcar al jefe (EsJefe) y al poner un decorado
    // (CapitulosDeEscenario.Poner).
    public void PisarLoChico(IReadOnlyList<Collider> chicos)
    {
        if (chicos == null || chicos.Count == 0 || colliders == null || !gameObject.activeInHierarchy) return;
        foreach (var propio in colliders)
        {
            if (propio == null || !propio.gameObject.activeInHierarchy) continue;
            for (int i = 0; i < chicos.Count; i++)
            {
                var chico = chicos[i];
                if (chico != null && chico.gameObject.activeInHierarchy) Physics.IgnoreCollision(propio, chico, true);
            }
        }
    }

    // Si ya se le calculo la vida con sus multiplicadores (ver IniciarVida): preguntar
    // la vida antes de tiempo la fijaria sin ellos.
    public bool VidaEmpezada
    {
        get { return vidaIniciada; }
    }

    private bool esJefe;

    public static EnemyController Aparecer(GameObject prefab, Vector3 posicion)
    {
        if (prefab == null) return null;
        // Nunca adentro de un obstaculo (un invocado del jefe al lado de un auto): saldria
        // disparado. Los puntos de aparicion de WaveMode ya estan libres.
        posicion = CapitulosDeEscenario.FueraDeLosObstaculos(posicion, RadioDelCuerpo(prefab));

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

    // Si un zombi que aparece en ese punto del piso se veria en pantalla. Los generadores
    // no los sacan ahi: a los 8 m de distancia minima al jugador todavia se esta en cuadro
    // (la camara ve unos 12 m a los costados en 16:9 y 15 en 20:9), y el zombi se
    // materializaba de la nada, a veces un FASTER que pegaba antes de que se lo viera venir.
    // Se mira el punto a AlturaAlAparecer con MargenAlAparecer por fuera del cuadro, que con
    // la camara del juego alcanza para la cabeza del jefe (4 m) en el borde de abajo y para
    // los pies en el de arriba. Sin camara no se descarta nada. Estatica para probarla con
    // la camara de WaveMode.
    public const float AlturaAlAparecer = 1f;
    public const float MargenAlAparecer = 0.1f;   // en fracciones de la pantalla

    public static bool SeVeriaAlAparecer(Camera camara, Vector3 punto)
    {
        if (camara == null) return false;
        Vector3 enPantalla = camara.WorldToViewportPoint(new Vector3(punto.x, AlturaAlAparecer, punto.z));
        return JefePatrones.DentroDelCuadro(enPantalla, new Vector2(-MargenAlAparecer, -MargenAlAparecer));
    }

    // --- Los edificios y los obstaculos de caja --------------------------------------
    //
    // Hacia donde caminar para ir de 'desde' a 'hasta' sin atravesar las huellas de los
    // edificios y de los autos, los canteros y los contenedores (CapitulosDeEscenario.Huellas:
    // x es x, y es z). Si la recta cruza uno, va a la esquina por la que es mas corto rodearlo,
    // agrandado en el radio del cuerpo mas HolguraDelRodeo; al llegar a la esquina sigue a la
    // proxima, o derecho si ya ve el destino. Los edificios estan sueltos, con calles de 13 m
    // entre uno y otro, y las cajas tambien, con lo que mide el jefe mas la holgura entre dos
    // (ConstructorEscenarios.Separacion): asi alcanza con rodear la primera que se cruza (y, si
    // para llegar a su esquina se cruza otra, esa antes), y la esquina de una nunca cae en la
    // de al lado. Con dos pegadas el zombi se quedaba dudando entre rodear una u otra. Lo
    // redondo (las tumbas, los troncos) no se rodea: se esquiva resbalando.
    // El zombi o el jugador pegados a una pared estan mas cerca de lo que el radio deja:
    // se cuentan desde el borde de afuera, o la recta no cruzaria nada y el zombi quedaria
    // empujando la pared. Si alguno esta adentro del edificio de verdad (no deberia: tienen
    // paredes, ver CapitulosDeEscenario.PonerParedes), ese no cuenta: derecho. Sin edificios
    // (todo lo que no es la ciudad) devuelve 'hasta'. Estatica para la prueba.
    public const float HolguraDelRodeo = 0.6f;
    public const float EsquinaAlcanzada = 0.4f;   // mas que lo que avanza el FASTER en un paso

    public static Vector3 Rodeo(Vector3 desde, Vector3 hasta, IReadOnlyList<Rect> huellas, float radio)
    {
        return Rodeo(desde, hasta, huellas, radio, 2);
    }

    // El radio de la capsula de la raiz con su escala (la de un prefab, para la prueba).
    public static float RadioDelCuerpo(GameObject zombi)
    {
        var capsula = zombi != null ? zombi.GetComponent<CapsuleCollider>() : null;
        if (capsula == null) return 0.5f;
        Vector3 escala = zombi.transform.lossyScale;
        return capsula.radius * Mathf.Max(Mathf.Abs(escala.x), Mathf.Abs(escala.z));
    }

    private static Vector3 Rodeo(Vector3 desde, Vector3 hasta, IReadOnlyList<Rect> huellas, float radio, int vueltas)
    {
        if (huellas == null || huellas.Count == 0) return hasta;
        var a = new Vector2(desde.x, desde.z);
        var b = new Vector2(hasta.x, hasta.z);
        int primero = -1;
        float entrada = float.MaxValue;
        // Lo que puede tocar el tramo: el resto ni lo cruza ni tiene adentro a ninguna punta.
        // Con los obstaculos son unas cuarenta huellas por zombi y por paso de fisica.
        float xMin = Mathf.Min(a.x, b.x) - radio, xMax = Mathf.Max(a.x, b.x) + radio;
        float zMin = Mathf.Min(a.y, b.y) - radio, zMax = Mathf.Max(a.y, b.y) + radio;
        for (int i = 0; i < huellas.Count; i++)
        {
            Rect h = huellas[i];
            if (h.xMin > xMax || h.xMax < xMin || h.yMin > zMax || h.yMax < zMin) continue;
            if (h.Contains(a) || h.Contains(b)) continue;
            Rect cuerpo = Agrandar(huellas[i], radio);
            float t;
            if (Cruza(Afuera(a, cuerpo), Afuera(b, cuerpo), cuerpo, out t) && t < entrada)
            {
                entrada = t;
                primero = i;
            }
        }
        if (primero < 0)
        {
            // El jugador pegado a una pared: al borde de afuera de la franja frente a el (si no,
            // el ultimo tramo raspa la pared en diagonal), y ya ahi, derecho a el.
            for (int i = 0; i < huellas.Count; i++)
            {
                if (huellas[i].Contains(b)) continue;
                Rect cuerpo = Agrandar(huellas[i], radio);
                if (!cuerpo.Contains(b)) continue;
                Vector2 frente = Afuera(b, cuerpo);
                if ((frente - a).sqrMagnitude > 1f) return new Vector3(frente.x, hasta.y, frente.y);
                break;
            }
            return hasta;
        }

        Rect elCuerpo = Agrandar(huellas[primero], radio);
        Vector2 llegada = Afuera(b, elCuerpo);
        Vector2 esquina = Esquina(Afuera(a, elCuerpo), llegada, elCuerpo, Agrandar(huellas[primero], radio + HolguraDelRodeo));
        if (esquina == llegada) return hasta;
        var punto = new Vector3(esquina.x, hasta.y, esquina.y);
        return vueltas > 1 ? Rodeo(desde, punto, huellas, radio, vueltas - 1) : punto;
    }

    private static Rect Agrandar(Rect r, float margen)
    {
        return Rect.MinMaxRect(r.xMin - margen, r.yMin - margen, r.xMax + margen, r.yMax + margen);
    }

    // El punto, o si cae dentro del rectangulo, el borde mas cercano, un pelo afuera.
    private static Vector2 Afuera(Vector2 p, Rect r)
    {
        if (!r.Contains(p)) return p;
        const float Pelo = 0.01f;
        float oeste = p.x - r.xMin, este = r.xMax - p.x, sur = p.y - r.yMin, norte = r.yMax - p.y;
        float menor = Mathf.Min(Mathf.Min(oeste, este), Mathf.Min(sur, norte));
        if (menor == oeste) p.x = r.xMin - Pelo;
        else if (menor == este) p.x = r.xMax + Pelo;
        else if (menor == sur) p.y = r.yMin - Pelo;
        else p.y = r.yMax + Pelo;
        return p;
    }

    // Si el tramo a-b entra al rectangulo y en que fraccion del tramo (Liang-Barsky). Rozar
    // un borde o una esquina no es entrar.
    private static bool Cruza(Vector2 a, Vector2 b, Rect r, out float entrada)
    {
        float t0 = 0f, t1 = 1f;
        Vector2 d = b - a;
        entrada = 0f;
        if (!Recortar(-d.x, a.x - r.xMin, ref t0, ref t1)) return false;
        if (!Recortar(d.x, r.xMax - a.x, ref t0, ref t1)) return false;
        if (!Recortar(-d.y, a.y - r.yMin, ref t0, ref t1)) return false;
        if (!Recortar(d.y, r.yMax - a.y, ref t0, ref t1)) return false;
        entrada = t0;
        return (t1 - t0) * d.magnitude > 1e-3f;
    }

    private static bool Recortar(float p, float q, ref float t0, ref float t1)
    {
        if (Mathf.Abs(p) < 1e-7f) return q >= 0f;
        float t = q / p;
        if (p < 0f)
        {
            if (t > t1) return false;
            if (t > t0) t0 = t;
        }
        else
        {
            if (t < t0) return false;
            if (t < t1) t1 = t;
        }
        return true;
    }

    // --- Lo redondo -------------------------------------------------------------------
    //
    // Las tumbas, las cruces y los troncos (CapitulosDeEscenario.Redondos) no se rodean con
    // el Rodeo: son mas de cien y estan sueltos, y contra algo redondo el zombi que empuja de
    // costado resbala solo. Pero de frente no: la fisica lo frena justo en el medio y ahi se
    // queda mientras el jugador no se mueva (lo encontro la prueba de logica, con el jefe
    // contra un arbol). Asi que, si lo redondo que se cruza primero en el camino esta a menos
    // de AlcanceDelEsquive, va por la tangente del lado que menos lo desvia; ya pegado, de
    // costado. El destino al lado de una tumba (el jugador escondido detras) no se esquiva: se
    // va derecho y la fisica lo frena contra ella. El jefe no esquiva lo que pisa. Estatica
    // para la prueba.
    public const float AlcanceDelEsquive = 2.5f;
    public const float HolguraDelEsquive = 0.1f;

    public static Vector3 Esquive(Vector3 desde, Vector3 hasta, IReadOnlyList<CapitulosDeEscenario.Redondo> redondos, float radio, bool pisaLoChico)
    {
        if (redondos == null || redondos.Count == 0) return hasta;
        var a = new Vector2(desde.x, desde.z);
        var b = new Vector2(hasta.x, hasta.z);
        Vector2 camino = b - a;
        float largo = camino.magnitude;
        if (largo < 1e-3f) return hasta;
        Vector2 u = camino / largo;
        float mira = Mathf.Min(largo, AlcanceDelEsquive);

        int primero = -1;
        float masCerca = float.MaxValue;
        for (int i = 0; i < redondos.Count; i++)
        {
            var r = redondos[i];
            if (pisaLoChico && r.chico) continue;
            float alcance = r.radio + radio + HolguraDelEsquive;
            Vector2 hacia = r.centro - a;
            // Solo lo que tiene adelante: del que queda al costado o atras el camino se aleja (si
            // no, pegado a una tumba que ya paso, iba y venia por el borde).
            float adelante = Vector2.Dot(hacia, u);
            if (adelante <= 0f || adelante > mira + alcance) continue;
            float costado = hacia.sqrMagnitude - adelante * adelante;
            if (costado >= alcance * alcance) continue;
            if ((b - r.centro).sqrMagnitude < alcance * alcance) continue;
            if (adelante < masCerca)
            {
                masCerca = adelante;
                primero = i;
            }
        }
        if (primero < 0) return hasta;

        var elRedondo = redondos[primero];
        float elAlcance = elRedondo.radio + radio + HolguraDelEsquive;
        Vector2 alCentro = elRedondo.centro - a;
        float distancia = alCentro.magnitude;
        // Del lado en que el camino pasa mas lejos del centro (si va justo al medio, a la derecha).
        float lado = u.x * alCentro.y - u.y * alCentro.x >= 0f ? -1f : 1f;
        Vector2 rumbo;
        if (distancia <= elAlcance + 0.01f)
        {
            // Ya pegado: de costado, a lo largo del borde.
            Vector2 normal = distancia > 1e-4f ? -alCentro / distancia : -u;
            rumbo = new Vector2(-normal.y, normal.x) * -lado;
            if (Vector2.Dot(rumbo, u) < 0f) rumbo = -rumbo;
        }
        else
        {
            float angulo = Mathf.Asin(Mathf.Clamp01(elAlcance / distancia)) * lado;
            Vector2 alCentroUnitario = alCentro / distancia;
            float c = Mathf.Cos(angulo), s = Mathf.Sin(angulo);
            rumbo = new Vector2(alCentroUnitario.x * c - alCentroUnitario.y * s, alCentroUnitario.x * s + alCentroUnitario.y * c);
        }
        Vector2 punto = a + rumbo * Mathf.Max(1f, Mathf.Sqrt(Mathf.Max(0f, distancia * distancia - elAlcance * elAlcance)));
        return new Vector3(punto.x, hasta.y, punto.y);
    }

    // Las cuatro esquinas, en orden alrededor del edificio: compartidas para no alocar en
    // cada paso de fisica de cada zombi (todo esto corre en el hilo principal).
    private static readonly Vector2[] esquinas = new Vector2[4];
    private static readonly bool[] seVenDesdeA = new bool[4];
    private static readonly bool[] seVenDesdeB = new bool[4];

    // La primera esquina del camino mas corto de a a b alrededor del cuerpo, que es el
    // edificio agrandado en el radio: se prueban las dos vueltas (horaria y antihoraria) desde
    // cada esquina que se ve desde a hasta la primera que ve b. Las esquinas son las del
    // edificio agrandado un poco mas, asi que ir de una a otra no roza el cuerpo. Devuelve b si
    // no hay camino (no deberia pasar: desde afuera de un rectangulo siempre se ven dos).
    private static Vector2 Esquina(Vector2 a, Vector2 b, Rect cuerpo, Rect afuera)
    {
        esquinas[0] = new Vector2(afuera.xMin, afuera.yMin);
        esquinas[1] = new Vector2(afuera.xMax, afuera.yMin);
        esquinas[2] = new Vector2(afuera.xMax, afuera.yMax);
        esquinas[3] = new Vector2(afuera.xMin, afuera.yMax);
        float t;
        for (int i = 0; i < 4; i++)
        {
            seVenDesdeA[i] = !Cruza(a, esquinas[i], cuerpo, out t);
            seVenDesdeB[i] = !Cruza(esquinas[i], b, cuerpo, out t);
        }

        float mejor = float.MaxValue;
        Vector2 elegida = b;
        for (int i = 0; i < 4; i++)
        {
            if (!seVenDesdeA[i]) continue;
            float hastaLaPrimera = Vector2.Distance(a, esquinas[i]);
            bool yaLlego = hastaLaPrimera < EsquinaAlcanzada;
            for (int sentido = 1; sentido <= 3; sentido += 2)
            {
                float largo = hastaLaPrimera;
                int j = i;
                for (int pasos = 0; pasos < 4; pasos++)
                {
                    if (seVenDesdeB[j])
                    {
                        float total = largo + Vector2.Distance(esquinas[j], b);
                        if (total < mejor)
                        {
                            mejor = total;
                            // En la esquina ya: a la que sigue, o derecho si desde ahi se ve b.
                            elegida = !yaLlego ? esquinas[i] : j == i ? b : esquinas[(i + sentido) % 4];
                        }
                        break;
                    }
                    int k = (j + sentido) % 4;
                    largo += Vector2.Distance(esquinas[j], esquinas[k]);
                    j = k;
                }
            }
        }
        return elegida;
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
        nombreTipo = enemyType != null ? enemyType.name : "";
        rb = GetComponent<Rigidbody>();
        // La rotacion la pone FixedUpdate, mirando al jugador: que los choques no
        // lo inclinen entre un paso y otro.
        rb.freezeRotation = true;
        escalaBase = transform.localScale;
        animadores = ConControlador(GetComponentsInChildren<Animator>(true));
        colliders = GetComponentsInChildren<Collider>(true);
        capsula = GetComponent<CapsuleCollider>();
        radioDelCuerpo = RadioDelCuerpo(gameObject);
        // Sin friccion: con la de fabrica, el roce con la caja del jugador que se les venia encima
        // no los dejaba correrse de costado (ver Apartarse).
        if (sinFriccion == null)
        {
            sinFriccion = new PhysicsMaterial("SinFriccion")
            {
                dynamicFriction = 0f,
                staticFriction = 0f,
                bounciness = 0f,
                frictionCombine = PhysicsMaterialCombine.Minimum,
                bounceCombine = PhysicsMaterialCombine.Minimum,
            };
        }
        foreach (var c in colliders)
            if (c.sharedMaterial == null) c.sharedMaterial = sinFriccion;
        BuscarElModelo();
        // En Halloween salen disfrazados. Despues de medir el modelo (un sombrero de bruja
        // agrandaria altoDelModelo, y con el las poses del jefe y la barra) y antes de la
        // capa y del destello, que tambien lo pintan. Del pool vuelve con el mismo disfraz.
        if (EventoHalloween.Activo) Disfraces.Vestir(gameObject, enemyType != null && enemyType.puntos >= Disfraces.PuntosDelJefe);
        // De noche, la luz de relleno de la escena solo alumbra la capa de los personajes.
        Personajes.PonerEnLaCapa(gameObject);
        movimientoPropio = GetComponent<IMovimientoPropio>();
        tesoro = GetComponent<ZombiDelTesoro>();
        // La altura de la barra de vida, una vez y en la pose de reposo: medida en el golpe,
        // si el primero que no mataba le entraba al jefe agazapado para el salto o la carga,
        // la barra quedaba hundida en su cabeza, y del pool la heredaban los jefes siguientes
        // (revision del 9/10). Con el disfraz puesto, que lo agranda.
        AlturaDeLaCabeza = BarraDeVida.AlturaDeLaCabeza(this);
        PrepararDestello();
    }

    public float AlturaDeLaCabeza { get; private set; }

    // El zombi del tesoro, si lo es: festeja cuando lo matan (ZombiDelTesoro.AlMorir).
    private ZombiDelTesoro tesoro;

    // Cada aparicion, nueva o salida del pool, arranca de cero. Corre antes de que
    // quien lo hizo aparecer le ponga los multiplicadores.
    private void OnEnable()
    {
        enUso = true;
        EsJefe = false;
        ZombisVivos++;
        NumeroDeAparicion = ++ultimaAparicion;

        vidaIniciada = false;
        estaMuerto = false;
        proximoGolpe = 0f;
        multiplicadorGolpe = 1f;
        golpeaAlChocar = false;
        puedeZarpar = true;
        golpeEnCurso = false;
        GolpesDados = 0;
        festejando = false;
        tieneLugarDelFestejo = false;
        cambioDeLugar = false;
        desfaseDelFestejo = Random.Range(0f, DesfaseMaximo);
        if (modelo != null)
        {
            modelo.localPosition = posicionBaseDelModelo;
            modelo.localRotation = rotacionBaseDelModelo;
            modelo.localScale = escalaBaseDelModelo;
        }
        multiplicadorMonedas = 1f;
        monedaPrefab = null;
        multiplicadorVida = 1f;
        multiplicadorDano = 1f;

        // Uno que sale del pool puede venir de desplomarse: colliders de vuelta y
        // fisica de vuelta.
        if (desplomandose)
        {
            desplomandose = false;
            cadaveres--;
        }
        foreach (var c in colliders) c.enabled = true;
        rb.isKinematic = false;
        velocidadDelEmpuje = 0f;
        direccionDelEmpuje = Vector3.zero;
        caidaDelCadaver = 0f;
        velocidadDeCaida = 0f;
        // Uno que se desplomo inclinado vuelve derecho: la rotacion la pisa
        // FixedUpdate con el LookAt, pero recien en el primer paso de fisica, y
        // hasta ahi se veria torcido.
        transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);

        transform.localScale = escalaBase;
        aplastado = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        thePlayer = ObtenerJugador();
        // El ritmo va como multiplicador del estado de correr y no del Animator
        // entero: con animador.speed, el jefe (0,3) tardaba cuatro segundos y medio
        // en morirse y el tanque (0,45) pegaba en camara lenta.
        // Y cada aparicion arranca corriendo: uno que salio del pool puede venir de
        // morirse, o con un golpe a medias y su gatillo sin consumir.
        foreach (var animador in animadores)
        {
            animador.speed = 1f;
            animador.SetFloat(idPaso, velocidadDeAnimacion);
            animador.SetFloat(idRitmo, ritmoDeAndar);
            animador.ResetTrigger(idMorir);
            animador.Play(idAndar, 0, 0f);
        }
        SubirSobreElPiso();
    }

    // Al morir, al caerse o al descargarse la escena. Lo que quedo a mitad de un
    // golpe (el destello, la barra) no puede pasar a la aparicion siguiente.
    private void OnDisable()
    {
        if (desplomandose)
        {
            desplomandose = false;
            cadaveres--;
        }
        DejarDeContar();
    }

    // Todo lo que deja de valer apenas muere, y que hasta el 22/9 estaba dentro de
    // OnDisable porque morir y apagarse eran lo mismo. Ahora el cadaver se queda
    // prendido mientras se desploma, pero para el resto del juego ya no existe: no
    // cuenta en ZombisVivos (o el generador quedaria tapado), la oleada lo cuenta
    // muerto y deja de ser jefe (o la barra de arriba seguiria ahi).
    private void DejarDeContar()
    {
        // Matarlo a mitad del zarpazo es llegar a tiempo: el golpe que venia no entra.
        golpeEnCurso = false;
        if (!enUso) return;
        enUso = false;
        EsJefe = false;
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

    // El cadaver lleva su propio reloj, como ManchaDeSangre: una corrutina no
    // sobrevive a que el objeto se apague por otro lado (el final de la escena).
    // Time.time es tiempo escalado, asi que la pausa lo congela.
    private void Update()
    {
        if (!desplomandose) return;
        float dt = Time.deltaTime;

        // Si murio en el aire, baja hasta el piso con la gravedad de la fisica, que es como
        // venia cayendo: kinematic, nadie mas lo baja.
        if (caidaDelCadaver > 0f)
        {
            velocidadDeCaida += Mathf.Abs(Physics.gravity.y) * dt;
            float baja = Mathf.Min(caidaDelCadaver, velocidadDeCaida * dt);
            transform.position += Vector3.down * baja;
            caidaDelCadaver -= baja;
        }

        // El cadaver sale despedido hacia donde apuntaba el tiro y frena solo. Sin
        // esto todos caian igual, en la direccion que tiene el clip, y un tiro por
        // la espalda se veia como uno de frente.
        if (velocidadDelEmpuje > 0f)
        {
            transform.position += direccionDelEmpuje * (velocidadDelEmpuje * dt);
            velocidadDelEmpuje = Mathf.Max(0f, velocidadDelEmpuje - frenadoDelEmpuje * dt);

            // Y se va de espaldas, enderezandose a medida que frena. El eje es el
            // perpendicular al empujon, asi se inclina hacia donde lo empujaron sin
            // importar hacia donde este mirando.
            float cuanto = empujeAlMorir > 0f ? velocidadDelEmpuje / empujeAlMorir : 0f;
            Vector3 eje = Vector3.Cross(Vector3.up, direccionDelEmpuje);
            transform.rotation = eje.sqrMagnitude > 0.0001f
                ? Quaternion.AngleAxis(inclinacionDelEmpuje * cuanto, eje) * rotacionAlMorir
                : rotacionAlMorir;
        }

        if (Time.time >= sacarloEn) Devolver();
    }

    // Morir con la animacion de morir. El zombi sale de la cuenta en el acto y el
    // objeto se queda prendido lo que dura el desplome. Si no hay lugar para otro
    // cadaver -o no hay Animator, como los zombis del fondo del menu- se va de
    // golpe, que es como se veia hasta ahora.
    private void Morir(Vector3 empuje)
    {
        int techo = Plataforma.EsMovil ? MaxCadaveresMovil : MaxCadaveres;
        if (animadores.Length == 0 || cadaveres >= techo || duracionDeLaMuerte <= 0f)
        {
            Devolver();
            return;
        }

        DejarDeContar();
        desplomandose = true;
        cadaveres++;
        sacarloEn = Time.time + duracionDeLaMuerte;

        // Sin colliders no frena balas ni empuja al jugador, y kinematic para que no
        // resbale por el empujon del ultimo tiro mientras se cae. Si estaba en el aire,
        // lo baja Update, con la velocidad a la que ya venia cayendo.
        float fondo;
        caidaDelCadaver = FondoDeLaCapsula(out fondo) ? Mathf.Max(0f, fondo - AlturaDelPiso) : 0f;
        velocidadDeCaida = Mathf.Max(0f, -rb.linearVelocity.y);
        foreach (var c in colliders) c.enabled = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;

        // Los grandes casi no se mueven: el empujon del normal, dividido por cuanto mas
        // grandes son.
        rotacionAlMorir = transform.rotation;
        empuje.y = 0f;
        if (empuje.sqrMagnitude > 0.0001f)
        {
            direccionDelEmpuje = empuje.normalized;
            velocidadDelEmpuje = VelocidadDelEmpuje(empujeAlMorir, escalaBase.y);
        }
        else
        {
            direccionDelEmpuje = Vector3.zero;
            velocidadDelEmpuje = 0f;
        }

        foreach (var animador in animadores) animador.SetTrigger(idMorir);
    }

    // A que velocidad sale despedido el cadaver de un zombi de esa escala: el empujon del
    // normal, dividido por cuantas veces mas grande que el normal es. El tanque (1) sale a
    // la mitad y recorre unos 22 cm, el jefe (2) a un cuarto y unos 6 cm; el rapido y el
    // FASTER, mas chicos, como el normal (90 cm). Hasta el 25/9 se dividia por la escala a
    // secas, con el normal ya en 0,5: el tanque salia despedido igual que un normal y solo
    // frenaba el jefe. Estatica para probarla con los prefabs.
    public static float VelocidadDelEmpuje(float empuje, float escala)
    {
        return empuje / Mathf.Max(1f, Mathf.Abs(escala) / EscalaDelNormal);
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
    // El jefe no se despeja: sale uno por oleada y la oleada lo contaria como muerto, asi que
    // revivir al lado del jefe lo borraria sin pelear. Es lento: con la gracia hay tiempo
    // de alejarse.
    public static int DespejarAlrededor(Vector3 punto, float radio)
    {
        if (!(radio > 0f)) return 0;

        int despejados = 0;
        float radioAlCuadrado = radio * radio;
        var zombis = FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
        foreach (var zombi in zombis)
        {
            if (zombi == null || !zombi.enUso || zombi.EsJefe) continue;
            if ((zombi.transform.position - punto).sqrMagnitude > radioAlCuadrado) continue;

            Efectos.ParticulasDeMuerte(zombi.deathParticles, zombi.transform.position);
            zombi.estaMuerto = true;
            zombi.Devolver();
            despejados++;
        }
        return despejados;
    }

    // Lo saca del mapa sin puntos, monedas ni mancha, como el despeje del revivir pero de a uno:
    // el zombi del tesoro que se escapa (ZombiDelTesoro).
    public void Retirar()
    {
        if (!enUso) return;
        estaMuerto = true;
        Devolver();
    }

    // Vuelve apagado al pool. Los que no salieron del pool se destruyen como antes.
    private void Devolver()
    {
        if (prefabDeOrigen == null)
        {
            Destroy(gameObject);
            return;
        }
        // El cadaver ya solto enUso al morir (para que la oleada lo cuente y deje
        // de contar en ZombisVivos) y tiene que poder volver al pool igual.
        if (!enUso && !desplomandose) return;

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
        float fondo;
        if (FondoDeLaCapsula(out fondo) && fondo < AlturaDelPiso)
        {
            transform.position += Vector3.up * (AlturaDelPiso - fondo + MargenSobreElPiso);
        }
    }

    // A que altura queda el fondo de la capsula, con la escala de siempre: la del
    // aplastado de un golpe es de un momento, y con ella el cadaver bajaria de mas. Los
    // zombis no tienen padre, asi que la escala local es la del mundo. Sin capsula, falso.
    private bool FondoDeLaCapsula(out float fondo)
    {
        fondo = 0f;
        if (capsula == null) return false;
        float alto = capsula.direction == 1 ? Mathf.Max(capsula.height * 0.5f, capsula.radius) : capsula.radius;
        fondo = transform.TransformPoint(capsula.center).y - alto * Mathf.Abs(escalaBase.y);
        return true;
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

       // Un cadaver no persigue a nadie ni se aplasta: solo se esta cayendo.
       if (desplomandose) return;

       ResolverGolpe();

       if (thePlayer == null) return;

       // Con el jugador muerto no hay a quien perseguir: festejan (tambien el jefe, antes
       // que sus patrones).
       if (DerrotaEnLaPartida.Activa)
       {
           MoverseAlFestejo();
           return;
       }

       if (movimientoPropio != null && movimientoPropio.Mover(rb, thePlayer.transform)) return;

       // Si el jugador se le viene encima corriendo, el liviano se corre de costado, mirandolo.
       Vector3 apartado;
       if (Apartarse(out apartado))
       {
           Vector3 alJugador = thePlayer.transform.position;
           alJugador.y = transform.position.y;
           if ((alJugador - transform.position).sqrMagnitude > 0.0001f) transform.LookAt(alJugador);
           apartado.y = rb.linearVelocity.y;
           rb.linearVelocity = apartado;
           return;
       }

       // Mira y camina en horizontal, y la velocidad vertical queda en manos de la
       // fisica. Antes miraba al centro del jugador y pisaba la velocidad entera en
       // cada paso: la gravedad nunca actuaba, y un zombi que terminaba debajo del
       // piso se quedaba ahi, invisible, persiguiendo al jugador y pegandole desde
       // abajo. Ahora cae y lo saca el kill-Z.
       // En la ciudad, rodeando los edificios: los atravesaban (lo dijeron en Discord, 8/10).
       // Y esquivando lo redondo del cementerio.
       Vector3 objetivo = Rodeo(transform.position, thePlayer.transform.position, CapitulosDeEscenario.Huellas, radioDelCuerpo);
       objetivo = Esquive(transform.position, objetivo, CapitulosDeEscenario.Redondos, radioDelCuerpo, EsJefe);
       objetivo.y = transform.position.y;
       if ((objetivo - transform.position).sqrMagnitude > 0.0001f) transform.LookAt(objetivo);
       Vector3 velocidad = transform.forward * enemyType.velocidad;
       velocidad.y = rb.linearVelocity.y;
       rb.linearVelocity = velocidad;
    }

    private bool estaMuerto;

    // Un componente que a veces mueve al zombi por su cuenta (el jefe, JefePatrones).
    private IMovimientoPropio movimientoPropio;

    // El nombre del asset Enemy, para los contadores de por vida. Leido una vez en
    // Awake: .name arma un string nuevo en cada llamada.
    private string nombreTipo;

    // "empuje" es hacia donde iba el golpe, para que el cadaver caiga hacia alla.
    // Es opcional: quien no la sabe (el medidor, las pruebas) lo deja caer donde esta.
    public void DanoZombi(float daño, bool critico = false, Vector3 empuje = default(Vector3))
    {
        // Dos golpes en el mismo paso de fisica llaman a esto dos veces con la vida
        // ya en cero (los eventos de colision del paso se despachan aunque el zombi
        // ya se haya apagado), y sin la guarda el bloque de muerte corria de nuevo
        // entero: puntos dobles, dos manchas, dos explosiones.
        // Un daño de cero, negativo o NaN no hace nada: no hay numero que mostrar.
        // Uno apagado en el pool tampoco: el que cayo por el kill-Z no llego a morir.
        if (estaMuerto || !enUso || !(daño > 0f)) return;

        // Con el jugador muerto nada le pega a un zombi: la partida sigue andando detras
        // de la derrota, y las balas y la granada que quedaron en el aire seguian
        // matando, con puntos, monedas y estadisticas despues de morir.
        if (PlayerHealth.instance != null && PlayerHealth.instance.EstaMuerto) return;

        IniciarVida();
        vidaActual -= daño;
        if (critico) Progreso.ContarCritico();

        // El numero flotante va redondeado y nunca en 0: un 5,75 se lee como 6, y
        // una bala que pega tiene que mostrar algo.
        Efectos.Golpe(transform.position + Vector3.up * (1f + escalaBase.y), Mathf.Max(1, Mathf.RoundToInt(daño)), critico);

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
            // La experiencia del nivel del jugador son los mismos puntos.
            NivelJugador.Sumar(enemyType.puntos);

            SoltarMonedas();
            SoltarCaramelos();
            Progreso.ContarMuerte(nombreTipo, EsJefe);
            Efectos.Muerte(transform.position, enemyType.hp);
            if (tesoro != null) tesoro.AlMorir();

            // Al final: todo lo de arriba usa su posicion.
            Morir(empuje);
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

    // Los caramelos del evento de Halloween, aparte de las monedas: no entran en las
    // cuentas del botin (MonedasEsperadas y MonedasSoltadas), que compara MedirPartida.
    // Solo los zombis que sueltan monedas: los del tutorial no.
    private void SoltarCaramelos()
    {
        if (monedaPrefab == null || !EventoHalloween.Activo) return;
        int cantidad = EventoHalloween.CaramelosAlMorir(EsJefe, Random.value);
        if (cantidad > 0) Moneda.Soltar(EventoHalloween.CarameloPrefab, transform.position, cantidad, 1);
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

    // Arranca un zarpazo al tocar al jugador y despues cada intervaloDeGolpe mientras
    // lo siga tocando; el daño entra cuando el brazo baja (ver ResolverGolpe). Antes
    // pegaba solo en OnCollisionEnter: un zombi pegado al jugador no volvia a dañar
    // hasta separarse, y el daño dependia de cuanto temblara la fisica. El intervalo
    // ademas evita que los varios colliders del zombi y del jugador cuenten el mismo
    // toque mas de una vez.
    private void OnCollisionEnter(Collision collision)
    {
        AnotarAlJugador(collision);
        Golpear(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        AnotarAlJugador(collision);
        Golpear(collision);
    }

    private void AnotarAlJugador(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player")) tocaAlJugadorHasta = Time.fixedTime + 0.05f;
    }

    // --- El empujon del jugador ------------------------------------------------------
    //
    // Corriendo contra un zombi, el jugador lo atropellaba y se lo llevaba por delante, pegandole
    // (Discord, 9/10: "te llevas un zombi para la direccion que estas yendo"): en las escenas el
    // jugador pesa 1e8, para que la horda no lo empuje, y el zombi, que volvia a caminar hacia el
    // en cada paso de fisica, quedaba en su camino. El banco del arrastre lo medio: de frente, el
    // jugador se llevaba casi 13 m en un segundo a cualquiera, tanque incluido. Ivan eligio que se
    // aparten de costado, cada uno con su peso: si lo toca y se le viene encima, el que cede (el
    // normal, el rapido y el veloz) se corre de costado a casi la velocidad del jugador; el tanque
    // y el jefe no se corren, y es el jugador el que se frena contra ellos
    // (PlayerController.FrenarContraLosPesados).
    public const float MasaDelJugador = 10f;    // la del prefab: en las escenas pesa mas, a proposito

    // Los pasos de fisica en que algun zombi se aparto: para el banco del arrastre.
    public static int PasosApartado { get; private set; }

    // Cuanto le cede al jugador un cuerpo de esa masa: 1 lo que no pesa nada, 0 lo inamovible. El
    // normal (1) cede 0,91, el rapido y el veloz (0,5) 0,95, el tanque (100) 0,09 y el jefe nada.
    public static float Ceder(float masa)
    {
        return MasaDelJugador / (MasaDelJugador + Mathf.Max(0f, masa));
    }

    private bool Apartarse(out Vector3 velocidad)
    {
        velocidad = Vector3.zero;
        if (Time.fixedTime > tocaAlJugadorHasta || thePlayer == null) return false;
        float ceder = Ceder(rb.mass);
        if (ceder < 0.5f) return false;
        Vector3 avance = thePlayer.VelocidadDeseada;
        avance.y = 0f;
        float rapidez = avance.magnitude;
        if (rapidez < 1f) return false;
        Vector3 hacia = avance / rapidez;
        Vector3 lado = transform.position - thePlayer.transform.position;
        lado.y = 0f;
        // Solo el que le queda adelante: al que viene de atras o de costado no lo pisa.
        if (lado.sqrMagnitude < 1e-4f || Vector3.Dot(hacia, lado.normalized) < 0.3f) return false;
        lado -= Vector3.Dot(lado, hacia) * hacia;
        // De frente justo, a un costado fijo por zombi (no a uno distinto en cada paso).
        if (lado.sqrMagnitude < 0.0025f) lado = Vector3.Cross(Vector3.up, hacia) * ((NumeroDeAparicion & 1) == 0 ? 1f : -1f);
        velocidad = lado.normalized * (rapidez * ceder);
        PasosApartado++;
        return true;
    }

    private void Golpear(Collision collision)
    {
        // Un muerto no pega: los eventos del paso en que murio llegan igual.
        if (estaMuerto || !enUso) return;
        // Y a un jugador muerto no se le pega: festejan (ver MoverseAlFestejo).
        if (PlayerHealth.instance != null && PlayerHealth.instance.EstaMuerto) return;
        if (Time.time < proximoGolpe || !collision.gameObject.CompareTag("Player")) return;
        if (PlayerHealth.instance == null) return;
        // El jefe avisando, invocando o aturdido no tira zarpazos. Sin tocar el intervalo:
        // al volver a perseguir, si sigue pegado, pega en el acto.
        if (!golpeaAlChocar && !puedeZarpar) return;

        proximoGolpe = Time.time + intervaloDeGolpe;

        // La embestida del jefe pega con el cuerpo, en el acto.
        if (golpeaAlChocar)
        {
            GolpesDados++;
            PlayerHealth.instance.TakeDamage(DanoPorGolpe);
            return;
        }

        // El zarpazo: el clip arranca adelantado para que la mano llegue adelante justo
        // cuando entra el daño, anticipacionDelGolpe despues. El daño se anota ahora,
        // con los multiplicadores que tiene al empezar.
        float anticipacion = Mathf.Clamp(anticipacionDelGolpe, 0f, momentoDelImpacto);
        danoDelGolpe = DanoPorGolpe;
        distanciaAlEmpezar = DistanciaAlJugador();
        golpeImpactaEn = Time.time + anticipacion;
        golpeEnCurso = true;
        ZarpazosEmpezados++;
        foreach (var animador in animadores)
            animador.CrossFadeInFixedTime(idAtacar, 0.05f, 0, momentoDelImpacto - anticipacion);
    }

    // Lo llama el jefe al empezar a embestir (JefePatrones): el unico golpe que cuenta
    // embistiendo es el del cuerpo. Un zarpazo de justo antes dejaba el intervalo
    // corriendo, y el primer choque no contaba: el jefe arrastraba al jugador a 16 m/s
    // hasta que vencia. O el zarpazo pegaba en plena carga y JefePatrones lo tomaba por
    // el choque, y la carga se cortaba a los 0,2 s.
    public void EmpezarEmbestida()
    {
        proximoGolpe = 0f;
        golpeEnCurso = false;
    }

    // Lo llama el jefe al empezar un patron (JefePatrones.Empezar): el zarpazo que venia
    // tirando no sigue. puedeZarpar solo frena los nuevos, y hasta el 27/9 el de antes
    // pegaba durante el aviso y el clip de atacar tapaba la pose de agazaparse o de
    // invocar (lo vio la revision de la grabacion del jefe).
    public void CortarZarpazo()
    {
        golpeEnCurso = false;
        foreach (var animador in animadores)
        {
            if (animador == null || !animador.isActiveAndEnabled) continue;
            if (animador.GetCurrentAnimatorStateInfo(0).shortNameHash == idAtacar ||
                animador.GetNextAnimatorStateInfo(0).shortNameHash == idAtacar)
                animador.CrossFadeInFixedTime(idAndar, 0.15f, 0);
        }
    }

    // El momento del impacto: pega si el jugador sigue al alcance del brazo. Si se fue
    // mientras el zombi levantaba el brazo, lo esquivo. Es alcance y no contacto porque
    // en el impacto la mano se estira casi un metro adelante: exigiendo que siga
    // tocandolo habria zarpazos que se ven conectar y no hacen daño, que es el mismo
    // problema que habia al reves.
    private void ResolverGolpe()
    {
        if (!golpeEnCurso || Time.time < golpeImpactaEn) return;
        golpeEnCurso = false;
        if (estaMuerto || !enUso || PlayerHealth.instance == null) return;

        float alcance = alcanceDelBrazo * Mathf.Max(0.1f, escalaBase.y);
        if (DistanciaAlJugador() > distanciaAlEmpezar + alcance) return;

        GolpesDados++;
        ZarpazosQuePegaron++;
        PlayerHealth.instance.TakeDamage(danoDelGolpe);
    }

    // En el piso: la altura no cuenta, que el jugador y el zombi tienen el pivote en
    // lugares distintos.
    private float DistanciaAlJugador()
    {
        if (thePlayer == null) return float.MaxValue;
        Vector3 d = thePlayer.transform.position - transform.position;
        d.y = 0f;
        return d.magnitude;
    }

    // Camina hasta su lugar al costado y ahi se da vuelta a mirar el cuerpo y festeja. Si
    // no llega (ver EsperaSinAcercarse), prueba otro lugar y despues festeja donde este.
    private void MoverseAlFestejo()
    {
        Vector3 cuerpo = thePlayer.transform.position;
        if (!tieneLugarDelFestejo) ElegirLugarDelFestejo(cuerpo);

        Vector3 falta = lugarDelFestejo - transform.position;
        falta.y = 0f;
        bool yendo = !festejando && falta.sqrMagnitude > 0.6f * 0.6f;
        // Lo que falta es por el camino que hace el Rodeo, no en linea recta: rodeando un
        // edificio camina de costado y la recta no baja, y a los 0,5 s cambiaba de lugar y al
        // rato festejaba detras del edificio, fuera de cuadro (revision del 9/10).
        Vector3 hacia = yendo ? Rodeo(transform.position, lugarDelFestejo, CapitulosDeEscenario.Huellas, radioDelCuerpo) : lugarDelFestejo;
        float porElCamino = EnElPiso(hacia - transform.position).magnitude + EnElPiso(lugarDelFestejo - hacia).magnitude;
        if (yendo && SinAcercarse(porElCamino, Time.time, ref masCercaDelLugar, ref seAcercoEn))
        {
            if (cambioDeLugar)
            {
                yendo = false;
            }
            else
            {
                // El nuevo cuenta desde el paso siguiente: en este sigue hacia el de antes.
                cambioDeLugar = true;
                ElegirLugarDelFestejo(cuerpo);
            }
        }

        Vector3 velocidad = Vector3.zero;
        if (yendo)
        {
            Vector3 rumbo = Esquive(transform.position, hacia, CapitulosDeEscenario.Redondos, radioDelCuerpo, EsJefe) - transform.position;
            rumbo.y = 0f;
            transform.rotation = Quaternion.LookRotation(rumbo.sqrMagnitude > 0.0001f ? rumbo : falta);
            velocidad = transform.forward * enemyType.velocidad;
        }
        else
        {
            Vector3 alCuerpo = cuerpo - transform.position;
            alCuerpo.y = 0f;
            if (alCuerpo.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(alCuerpo);
            if (!festejando) EmpezarAFestejar();
        }

        velocidad.y = rb.linearVelocity.y;
        rb.linearVelocity = velocidad;
    }

    // Alrededor del cuerpo, en la franja de la pantalla que la derrota deja libre: la
    // camara corrio el cuerpo al costado izquierdo, asi que es mas lugar a su izquierda que a
    // su derecha, donde empiezan los textos. Hasta el 23/9 el cuerpo quedaba en el centro,
    // tapado, y la horda festejaba a los costados de la pantalla, lejos de el. Los ejes salen
    // de la camara y no del mundo, por si algun dia gira.
    private void ElegirLugarDelFestejo(Vector3 cuerpo)
    {
        tieneLugarDelFestejo = true;
        masCercaDelLugar = float.PositiveInfinity;
        seAcercoEn = Time.time;
        Transform camara = Camera.main != null ? Camera.main.transform : null;
        Vector3 derecha = camara != null ? camara.right : Vector3.right;
        derecha.y = 0f;
        derecha = derecha.sqrMagnitude > 0.0001f ? derecha.normalized : Vector3.right;
        Vector3 adelante = Vector3.Cross(derecha, Vector3.up);

        Vector2 lugar = Vector2.zero;
        for (int intento = 0; intento < 8; intento++)
        {
            lugar = new Vector2(Random.Range(costadoDelFestejo.x, costadoDelFestejo.y),
                                Random.Range(alturaDelFestejo.x, alturaDelFestejo.y));
            if (lugar.sqrMagnitude >= cercaDelCuerpo * cercaDelCuerpo) break;
        }
        // Si ninguno quedo lejos del cuerpo, el ultimo se corre hasta la distancia minima.
        if (lugar.sqrMagnitude < cercaDelCuerpo * cercaDelCuerpo)
            lugar = (lugar.sqrMagnitude > 0.0001f ? lugar.normalized : Vector2.left) * cercaDelCuerpo;
        lugarDelFestejo = DentroDeLaPantalla(cuerpo + derecha * lugar.x + adelante * lugar.y, derecha, adelante);
    }

    // Lo que falta de la pantalla para que el zombi entero se vea, de cada lado: con el
    // cuerpo en el 22 % del ancho, los 5 m de la franja a su izquierda pasaban del borde y
    // dos o tres festejaban cortados, medio afuera del cuadro.
    const float MargenDelFestejoEnLaPantalla = 0.05f;

    // Corre el lugar del festejo hasta que quede dentro de la pantalla tal como va a
    // quedar con la camara corrida (CamaraJugador.EnLaPantallaAlMostrarElCuerpo). La
    // pantalla sobre el piso no es un rectangulo (la camara mira inclinada), asi que se
    // corrige con lo que se mueve el punto en la pantalla por cada metro, y dos veces.
    private static Vector3 DentroDeLaPantalla(Vector3 lugar, Vector3 derecha, Vector3 adelante)
    {
        const float m = MargenDelFestejoEnLaPantalla;
        for (int vuelta = 0; vuelta < 2; vuelta++)
        {
            Vector3 aca, aLaDerecha, adelanteEnPantalla;
            if (!CamaraJugador.EnLaPantallaAlMostrarElCuerpo(lugar, out aca) ||
                !CamaraJugador.EnLaPantallaAlMostrarElCuerpo(lugar + derecha, out aLaDerecha) ||
                !CamaraJugador.EnLaPantallaAlMostrarElCuerpo(lugar + adelante, out adelanteEnPantalla))
                return lugar;
            float porMetroX = aLaDerecha.x - aca.x;
            float porMetroY = adelanteEnPantalla.y - aca.y;
            if (porMetroX > 0.0001f)
            {
                if (aca.x < m) lugar += derecha * ((m - aca.x) / porMetroX);
                else if (aca.x > 1f - m) lugar -= derecha * ((aca.x - (1f - m)) / porMetroX);
            }
            if (porMetroY > 0.0001f)
            {
                if (aca.y < m) lugar += adelante * ((m - aca.y) / porMetroY);
                else if (aca.y > 1f - m) lugar -= adelante * ((aca.y - (1f - m)) / porMetroY);
            }
        }
        return lugar;
    }

    private static Vector3 EnElPiso(Vector3 v)
    {
        v.y = 0f;
        return v;
    }

    // Si hace EsperaSinAcercarse que no se acerca AcercamientoMinimo al lugar al que va, a
    // "distancia" de el: trabado contra una pared, un tanque o el jefe. Caminando, el mas
    // lento (el jefe) se acerca un metro en ese rato, y abrirse paso entre la horda lo
    // frena pero no tanto. Estatica para probarla sin escena.
    public static bool SinAcercarse(float distancia, float ahora, ref float masCerca, ref float seAcerco)
    {
        if (distancia <= masCerca - AcercamientoMinimo)
        {
            masCerca = distancia;
            seAcerco = ahora;
            return false;
        }
        return ahora - seAcerco >= EsperaSinAcercarse;
    }

    private void EmpezarAFestejar()
    {
        festejando = true;
        golpeEnCurso = false;
        foreach (var animador in animadores)
        {
            animador.SetFloat(idFestejo, ManoAbajo);
            animador.CrossFadeInFixedTime(idFestejar, 0.25f, 0, 0f);
        }
    }

    // La pose del festejo, sobre el modelo y en LateUpdate como la del jefe: despues de
    // que el Animator escribio los huesos. El brazo va por el Motion Time del estado
    // Festejar y el cuerpo (el arco y el salto) por codigo.
    private void LateUpdate()
    {
        if (!festejando || desplomandose || modelo == null) return;

        float t = Time.time - festejoDesde - desfaseDelFestejo;
        float envolvente = 0f, puño = 0f;
        if (t >= 0f)
        {
            int oleada = Mathf.FloorToInt(t / PeriodoDelFestejo);
            float enLaOleada = t - oleada * PeriodoDelFestejo;
            if (enLaOleada < DuracionDelFestejo)
            {
                float u = enLaOleada / DuracionDelFestejo;
                envolvente = Mathf.Sin(u * Mathf.PI);
                puño = Mathf.Abs(Mathf.Sin(u * PuñosPorFestejo * Mathf.PI));
            }
        }

        float mano = envolvente > 0f ? Mathf.Lerp(ManoAlHombro, ManoArriba, puño) : ManoAbajo;
        foreach (var animador in animadores) animador.SetFloat(idFestejo, mano);

        // El salto con un estiron, que desde arriba se ve mas que el brazo: el brazo
        // levantado apunta a la camara y casi no cambia la silueta.
        float salto = puño * envolvente;
        modelo.localPosition = posicionBaseDelModelo + Vector3.up * (saltoDelFestejo * altoDelModelo * salto);
        modelo.localRotation = rotacionBaseDelModelo * Quaternion.Euler(-gradosDelFestejo * envolvente, 0f, 0f);
        float estiron = 1f + 0.2f * salto;
        modelo.localScale = new Vector3(escalaBaseDelModelo.x / Mathf.Sqrt(estiron),
                                        escalaBaseDelModelo.y * estiron,
                                        escalaBaseDelModelo.z / Mathf.Sqrt(estiron));
    }

    // El hijo que se ve, como en JefePatrones: el del Animator con controller. Su
    // transform local esta libre porque los prefabs no usan root motion.
    private void BuscarElModelo()
    {
        foreach (var animador in animadores)
        {
            if (animador.transform == transform) continue;
            modelo = animador.transform;
            break;
        }
        if (modelo == null) return;
        posicionBaseDelModelo = modelo.localPosition;
        rotacionBaseDelModelo = modelo.localRotation;
        escalaBaseDelModelo = modelo.localScale;
        float alto = 0f;
        foreach (var r in modelo.GetComponentsInChildren<Renderer>(true)) alto = Mathf.Max(alto, r.bounds.size.y);
        float escala = Mathf.Abs(modelo.lossyScale.y);
        altoDelModelo = escala > 0.0001f ? Mathf.Max(0.1f, alto / escala) : 1f;
    }

    // Los que pueden animar de verdad. Se filtra una vez, en el Awake, y no en cada
    // golpe.
    private static Animator[] ConControlador(Animator[] todos)
    {
        int cuantos = 0;
        for (int i = 0; i < todos.Length; i++)
            if (todos[i].runtimeAnimatorController != null) cuantos++;
        if (cuantos == todos.Length) return todos;

        var buenos = new Animator[cuantos];
        int n = 0;
        for (int i = 0; i < todos.Length; i++)
            if (todos[i].runtimeAnimatorController != null) buenos[n++] = todos[i];
        return buenos;
    }
}
