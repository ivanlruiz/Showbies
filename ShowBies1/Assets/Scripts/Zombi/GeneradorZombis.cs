using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// El modo libre: una corrutina por cada uno de los cinco zombis de siempre, con su intervalo, y una por cada zombi con
// habilidad, que sale desde su nivel (ver especiales, abajo). Todos respetan el techo de poblacion.
public class GeneradorZombis : MonoBehaviour
{
    [SerializeField]
    private GameObject Zombi;
    [SerializeField]
    private GameObject ZombiRapido;
    [SerializeField]
    private GameObject ZombiTanque;
    [SerializeField]
    private GameObject ZombiBOSS;
    [SerializeField]
    private GameObject ZombiFASTER;

    [SerializeField]
    public float intervaloZombi; //0.25
    [SerializeField]
    public float intervaloZombiRapido; //1
    [SerializeField]
    public float intervaloZombiTanque; //3
    [SerializeField]
    public float intervaloZombiBOSS; //30
    [SerializeField]
    public float intervaloZombiFASTER; //2

    // Los cinco con habilidad (ZOMBIS.md, de Argon, 19/10): uno nuevo por nivel desde el 2 (el escupidor en el 2, el
    // saltador en el 3, el chaman en el 4, el blindado en el 5 y el hinchado en el 6: muchas partidas casuales duran 4-6
    // minutos), con su tope de vivos y como mucho WaveManager.MaxEspecialesVivos entre todos. Aca desdeOleada es el
    // nivel. Prueban cada intervaloEspeciales, y cada uno arranca desfaseEspeciales despues del anterior, para que no
    // pidan lugar en el mismo cuadro. El que puede salir y encuentra lleno el techo de todos se queda con el proximo
    // lugar que se libere, antes que los comunes (especialesEsperando): los comunes prueban cada 0,25 s y se llevaban
    // casi todos, y con el techo lleno el 82 % del tiempo el chaman y el hinchado salian un minuto tarde, o nunca. Y el
    // ultimo lugar de los especiales es del recien desbloqueado (PuedeSalirEspecial).
    // Los pone ConstructorZombis (ShowBies > Zombis > Poner los especiales en el modo libre).
    [Header("Los zombis con habilidad")]
    public TipoEnOleada[] especiales;
    public float intervaloEspeciales = 5.5f;
    public float desfaseEspeciales = 0.8f;

    // Si hay un modo libre andando: el escupidor se acerca solo despues de escupir (no hay final de oleada). El tutorial
    // tiene el componente con su objeto apagado, asi que ahi es falso.
    public static GeneradorZombis Actual { get; private set; }
    public static bool EnElLibre { get { return Actual != null; } }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ReiniciarEstatico()
    {
        Actual = null;
    }

    private void OnEnable()
    {
        Actual = this;
        especialesEsperando = 0;
    }

    private void OnDisable()
    {
        if (Actual == this) Actual = null;
        // Las corrutinas se cortan al apagarse, y la que estaba esperando lugar no llega a descontarse.
        especialesEsperando = 0;
    }

    // Los especiales que esperan el proximo lugar libre: mientras haya alguno, los comunes no lo toman.
    private int especialesEsperando;

    // Techo de poblacion. Los cinco generadores corren en paralelo y para siempre,
    // asi que sin esto son ~350 zombis en el primer minuto y sigue creciendo.
    [SerializeField]
    public int maxZombisVivos = 60;

    // En movil cada zombi cuesta mas (animador, fisica y sombra en una CPU y GPU
    // chicas), asi que el techo es mas bajo. Salio de la primera prueba en un
    // telefono, con bajos FPS.
    [SerializeField]
    public int maxZombisVivosMovil = 35;

    // El modo libre da la mitad de monedas y no tiene bono de oleada: sin
    // oleadas que se pongan dificiles, juntar monedas ahi es mas facil.
    [SerializeField]
    public float multiplicadorMonedas = 0.5f;
    [SerializeField]
    public Moneda monedaPrefab;

    // Sin esto el modo libre era una granja de monedas sin dificultad: ~445
    // monedas por minuto fijas, mas que las oleadas hasta la 20, con los zombis
    // cada vez mas faciles a medida que el jugador compra mejoras. Ahora cada
    // segundosPorNivel sube un nivel y los zombis nuevos salen con la vida, el
    // daño y las monedas de esa "oleada", con los mismos crecimientos que el
    // modo de oleadas. Los que ya estan en la escena no cambian.
    [Header("Dificultad con el tiempo")]
    public float segundosPorNivel = 45f;     // cada cuánto sube un nivel (tiempo escalado: la pausa lo congela)
    public float crecimientoVida = 1.15f;    // mismos crecimientos que las oleadas
    public float crecimientoDano = 1.07f;
    public float crecimientoMonedas = 1.05f;
    public TMPro.TMP_Text textoNivel;        // "Nivel N" en el HUD; opcional
    public float distanciaMinimaAlJugador = 8f; // ningun zombi nace mas cerca que esto

    // La mejora de botin se lee una vez al empezar: no se puede comprar en medio
    // de la partida.
    private float botin = 1f;
    private float inicio;
    private int nivelMostrado;

    // Con Time.time y no con un contador propio: la pausa (timeScale 0) y la
    // pausa de impacto lo frenan solas.
    public int NivelActual => 1 + Mathf.FloorToInt((Time.time - inicio) / Mathf.Max(1f, segundosPorNivel));

    public float MultiplicadorVidaActual => Escalado.PorOleada(crecimientoVida, NivelActual);
    public float MultiplicadorDanoActual => Escalado.PorOleada(crecimientoDano, NivelActual);

    // Incluye el botin: es lo que vale cada moneda de un zombi que salga ahora.
    public float MultiplicadorMonedasActual => multiplicadorMonedas * Escalado.PorOleada(crecimientoMonedas, NivelActual) * botin;

    // En Awake y no en Start: MedidorBalance se crea al cargar la escena, despues
    // de los Awake y antes de los Start, y lee NivelActual y los multiplicadores
    // enseguida. Con inicio en 0 daba el nivel del tiempo de sesion (3 minutos
    // jugados eran "nivel 5") y el botin en 1.
    private void Awake()
    {
        botin = CatalogoMejoras.MultiplicadorBotin;
        inicio = Time.time;
    }

    // Start is called before the first frame update
    void Start()
    {
        if (Plataforma.EsMovil) maxZombisVivos = maxZombisVivosMovil;
        EnemyController.FijarTecho(maxZombisVivos);
        Analitica.PartidaEmpezada("libre", 1, 0, false);

        StartCoroutine(spawnEnemy(intervaloZombi, Zombi));
        StartCoroutine(spawnEnemy(intervaloZombiRapido, ZombiRapido));
        StartCoroutine(spawnEnemy(intervaloZombiTanque, ZombiTanque));
        StartCoroutine(spawnEnemy(intervaloZombiBOSS, ZombiBOSS));
        StartCoroutine(spawnEnemy(intervaloZombiFASTER, ZombiFASTER));
        if (especiales != null)
            for (int i = 0; i < especiales.Length; i++)
                if (especiales[i] != null && especiales[i].prefab != null) StartCoroutine(SacarEspecial(especiales[i], i * desfaseEspeciales));
    }

    private void Update()
    {
        // Muerto, el nivel ya no cuenta: la partida sigue andando detras de la derrota, y
        // sin esto cada 45 s sonaba el jingle del cartel y se guardaba el progreso.
        if (PlayerHealth.instance != null && PlayerHealth.instance.EstaMuerto) return;

        int nivel = NivelActual;
        if (nivel == nivelMostrado) return;

        // Solo al cambiar: armar el texto por frame aloca por frame.
        nivelMostrado = nivel;
        if (textoNivel != null) textoNivel.SetText(Textos.De("hud_nivel"), nivel);

        // El nivel 1 es el de arranque y no se festeja. Los siguientes apagan y
        // prenden el texto para que su AparecerConRebote, si lo tiene, rebote, y
        // suenan como el cartel de oleada: el jugador tiene que notar que subio.
        // El jingle suena aunque la escena no tenga el texto, que es opcional: sin
        // el, el modo libre se pondria mas dificil sin ningun aviso.
        if (nivel > 1)
        {
            Progreso.RegistrarNivelLibre(nivel);   // el logro SIN FIN

            // Un punto seguro para guardar, como el fin de oleada en WaveMode: antes el
            // modo libre solo guardaba al pausar y al morir, y si Android mataba la app
            // se perdian las monedas de toda la partida. En otro hilo, como el de cada oleada.
            Progreso.GuardarEnSegundoPlano();
            if (textoNivel != null)
            {
                textoNivel.gameObject.SetActive(false);
                textoNivel.gameObject.SetActive(true);
            }
            Efectos.CartelOleada();
        }
    }

    // Un punto al azar del mapa, lejos del jugador y fuera de la vista: hasta unos intentos,
    // y si no sale ninguno, el ultimo (en un mapa de casi 100 m no deberia pasar). A los 8 m
    // todavia se esta en pantalla (ver EnemyController.SeVeriaAlAparecer), y hasta el 25/9
    // uno de cada 30 a 45 zombis se materializaba a la vista, a veces un FASTER que pegaba
    // antes de que se lo viera venir.
    private Vector3 PosicionLejosDelJugador()
    {
        Transform jugador = PlayerHealth.instance != null ? PlayerHealth.instance.transform : null;
        Camera camara = Camera.main;
        float minimo = distanciaMinimaAlJugador * distanciaMinimaAlJugador;
        Vector3 posicion = Vector3.zero;
        for (int intento = 0; intento < 10; intento++)
        {
            posicion = new Vector3(Random.Range(-48f, 48f), 0.5f, Random.Range(-45f, 45f));
            if (jugador == null) break;
            Vector3 d = posicion - jugador.position;
            d.y = 0f;
            if (d.sqrMagnitude >= minimo && !EnemyController.SeVeriaAlAparecer(camara, posicion)) break;
        }
        return posicion;
    }

    // El jefe que anda dando vueltas, para no sacar otro hasta que muera.
    private EnemyController jefeVivo;
    private int numeroDelJefe;

    private IEnumerator spawnEnemy(float interval, GameObject enemy)
    {
        // Antes esto se rellamaba a si mismo con un StartCoroutine al final, lo que
        // creaba una corrutina nueva por spawn. Un while hace lo mismo sin alocar.
        while (true)
        {
            yield return new WaitForSeconds(interval);                   //X                         //Y                     //Z

            // Los lugares que reservo un jefe que esta por invocar cuentan como ocupados, y el que se libera es de un
            // especial si alguno lo esta esperando.
            if (TechoLleno || especialesEsperando > 0) continue;

            // Un jefe por vez: con su corrutina cada 30 s se juntaban varios, y cada uno
            // invoca. El que hay se sigue con su numero de aparicion, que no se reusa.
            if (enemy == ZombiBOSS && EnemyController.SigueVivo(jefeVivo, numeroDelJefe)) continue;

            var enemigo = Preparar(EnemyController.Aparecer(enemy, PosicionLejosDelJugador()), enemy == ZombiBOSS);
            if (enemigo != null && enemigo.EsJefe)
            {
                jefeVivo = enemigo;
                numeroDelJefe = enemigo.NumeroDeAparicion;
            }
        }
    }

    // Un zombi con habilidad: desde su nivel, con su tope y el de todos los especiales (las mismas reglas que en las
    // oleadas, WaveManager.Elegible, con el nivel en lugar de la oleada).
    private IEnumerator SacarEspecial(TipoEnOleada tipo, float desfase)
    {
        if (desfase > 0f) yield return new WaitForSeconds(desfase);
        var espera = new WaitForSeconds(Mathf.Max(0.1f, intervaloEspeciales));
        while (true)
        {
            yield return espera;
            if (!PuedeSalir(tipo)) continue;
            if (TechoLleno)
            {
                // Hasta que muera alguno, mirando en cada cuadro si todavia le toca: el lugar de los especiales lo puede
                // llenar otro mientras tanto.
                especialesEsperando++;
                while (TechoLleno && PuedeSalir(tipo)) yield return null;
                especialesEsperando--;
                if (TechoLleno || !PuedeSalir(tipo)) continue;
            }
            Preparar(EnemyController.Aparecer(tipo.prefab, PosicionLejosDelJugador()), false);
        }
    }

    private bool TechoLleno
    {
        get { return EnemyController.ZombisVivos + EnemyController.Reservados >= maxZombisVivos; }
    }

    private bool PuedeSalir(TipoEnOleada tipo)
    {
        return PuedeSalirEspecial(especiales, tipo, NivelActual, EnemyController.VivosDe);
    }

    // Si un especial puede salir: las reglas de las oleadas (WaveManager.Elegible) y, ademas, el que ya tiene alguno
    // vivo no se lleva el ultimo lugar de los especiales mientras haya un desbloqueado sin ninguno. Con los ocho lugares
    // llenos (los topes suman 15), el que se liberaba lo tomaba el primero que probara, y el blindado salia 19 s despues
    // de desbloquearse: uno nuevo por nivel (Argon) es que se lo vea en su nivel. Estatico para probarlo sin escena.
    public static bool PuedeSalirEspecial(IReadOnlyList<TipoEnOleada> lista, TipoEnOleada tipo, int nivel, System.Func<GameObject, int> vivosDe)
    {
        if (tipo == null || tipo.prefab == null) return false;
        int vivosDelTipo = vivosDe(tipo.prefab);
        int especiales = WaveManager.EspecialesVivos(lista, vivosDe);
        if (!WaveManager.Elegible(tipo, nivel, vivosDelTipo, especiales)) return false;
        if (vivosDelTipo == 0 || lista == null) return true;
        int faltan = 0;
        for (int i = 0; i < lista.Count; i++)
        {
            var otro = lista[i];
            if (otro == null || otro.prefab == null || !otro.especial || otro.prefab == tipo.prefab) continue;
            if (!WaveManager.Elegible(otro, nivel, 0, 0) || vivosDe(otro.prefab) > 0) continue;
            // El que va dos veces en la lista cuenta una.
            bool contado = false;
            for (int j = 0; j < i && !contado; j++)
                contado = lista[j] != null && lista[j].prefab == otro.prefab && lista[j].especial && WaveManager.Elegible(lista[j], nivel, 0, 0);
            if (!contado) faltan++;
        }
        return especiales + faltan < WaveManager.MaxEspecialesVivos;
    }

    // Antes de su primer golpe: la vida se calcula con el multiplicador que tenga en ese momento.
    private EnemyController Preparar(EnemyController enemigo, bool jefe)
    {
        if (enemigo == null) return null;
        enemigo.multiplicadorVida = MultiplicadorVidaActual;
        enemigo.multiplicadorDano = MultiplicadorDanoActual;
        enemigo.multiplicadorMonedas = MultiplicadorMonedasActual;
        enemigo.monedaPrefab = monedaPrefab;
        enemigo.EsJefe = jefe;
        return enemigo;
    }
}
