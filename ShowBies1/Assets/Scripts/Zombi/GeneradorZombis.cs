using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

        StartCoroutine(spawnEnemy(intervaloZombi, Zombi));
        StartCoroutine(spawnEnemy(intervaloZombiRapido, ZombiRapido));
        StartCoroutine(spawnEnemy(intervaloZombiTanque, ZombiTanque));
        StartCoroutine(spawnEnemy(intervaloZombiBOSS, ZombiBOSS));
        StartCoroutine(spawnEnemy(intervaloZombiFASTER, ZombiFASTER));
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
            // Un punto seguro para guardar, como el fin de oleada en WaveMode: antes el
            // modo libre solo guardaba al pausar y al morir, y si Android mataba la app
            // se perdian las monedas de toda la partida.
            Progreso.Guardar();
            if (textoNivel != null)
            {
                textoNivel.gameObject.SetActive(false);
                textoNivel.gameObject.SetActive(true);
            }
            Efectos.CartelOleada();
        }
    }

    // Un punto al azar del mapa, lejos del jugador: hasta unos intentos, y si no sale
    // ninguno, el ultimo (en un mapa de casi 100 m no deberia pasar).
    private Vector3 PosicionLejosDelJugador()
    {
        Transform jugador = PlayerHealth.instance != null ? PlayerHealth.instance.transform : null;
        float minimo = distanciaMinimaAlJugador * distanciaMinimaAlJugador;
        Vector3 posicion = Vector3.zero;
        for (int intento = 0; intento < 6; intento++)
        {
            posicion = new Vector3(Random.Range(-48f, 48f), 0.5f, Random.Range(-45f, 45f));
            if (jugador == null) break;
            Vector3 d = posicion - jugador.position;
            d.y = 0f;
            if (d.sqrMagnitude >= minimo) break;
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

            if (EnemyController.ZombisVivos >= maxZombisVivos) continue;

            // Un jefe por vez: con su corrutina cada 30 s se juntaban varios, y cada uno
            // invoca. El que hay se sigue con su numero de aparicion, que no se reusa.
            if (enemy == ZombiBOSS && EnemyController.SigueVivo(jefeVivo, numeroDelJefe)) continue;

            var enemigo = EnemyController.Aparecer(enemy, PosicionLejosDelJugador());
            if (enemigo != null)
            {
                // Antes de su primer golpe: la vida se calcula con el multiplicador
                // que tenga en ese momento.
                enemigo.multiplicadorVida = MultiplicadorVidaActual;
                enemigo.multiplicadorDano = MultiplicadorDanoActual;
                enemigo.multiplicadorMonedas = MultiplicadorMonedasActual;
                enemigo.monedaPrefab = monedaPrefab;
                enemigo.EsJefe = enemy == ZombiBOSS;
                if (enemigo.EsJefe)
                {
                    jefeVivo = enemigo;
                    numeroDelJefe = enemigo.NumeroDeAparicion;
                }
            }
        }
    }
}
