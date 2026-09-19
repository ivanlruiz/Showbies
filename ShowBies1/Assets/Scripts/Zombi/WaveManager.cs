using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Un tipo de zombi dentro de las oleadas: desde que oleada entra en la mezcla y
// cuanto pesa en ella. Los pesos son relativos entre los tipos ya activos.
[System.Serializable]
public class TipoEnOleada
{
    public GameObject prefab;
    public int desdeOleada = 1;
    public float peso = 1f;
}

// Oleadas de verdad: cada una termina cuando mueren todos sus zombis, y antes de
// cada una hay un descanso con el cartel "Oleada N". La cantidad crece con la
// oleada, los tipos se suman a la mezcla a medida que se avanza y cada tantas
// oleadas sale un jefe ademas de los demas.
//
// Antes las oleadas eran por tiempo (la siguiente salia aunque quedaran zombis
// vivos) y cada 5 oleadas el tipo de zombi se reemplazaba en vez de sumarse:
// desde la oleada 20 solo salian jefes.
public class WaveManager : MonoBehaviour
{
    // La partida de oleadas a medias se olvida al morir y al reiniciar: las dos
    // cosas empiezan una partida nueva. Salir al menu o cerrar la app no.
    public static void OlvidarPartidaSiEsOleadas()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex != TiendaMejoras.EscenaOleadas) return;
        Progreso.OlvidarOleadaEnCurso();
        Progreso.Guardar();
    }

    [Header("Zombis")]
    public TipoEnOleada[] tipos;
    public GameObject jefe;
    public int jefeCadaOleadas = 10;
    public Transform[] spawnPoints;          // se elige uno al azar por zombi
    public float distanciaMinimaAlJugador = 8f; // ninguno nace mas cerca que esto, si se puede

    [Header("Ritmo")]
    public int zombisBase = 6;               // zombis por oleada = zombisBase + zombisPorOleada * oleada
    public int zombisPorOleada = 2;
    public float intervaloEntreApariciones = 0.8f;
    public float descansoEntreOleadas = 3f;
    public int maxZombisVivos = 60;          // techo de poblacion: si esta lleno, la oleada espera
    public int maxZombisVivosMovil = 35;     // en movil cada zombi cuesta mas; ver GeneradorZombis

    // Con las mejoras compradas el jugador pega mas y aguanta mas: sin esto, a las
    // pocas compras las oleadas dejaban de ser un desafio. La vida crece 15 % por
    // oleada, compuesto, y el daño de bala suma 1 por nivel: las primeras compras le
    // ganan al escalado, y mas adelante hace falta sumarles la cadencia.
    [Header("Dificultad")]
    public float crecimientoVida = 1.15f;    // la vida de cada zombi es hp * crecimientoVida^(oleada - 1)
    public float crecimientoDano = 1.07f;    // su golpe es daño * crecimientoDano^(oleada - 1)

    [Header("Monedas")]
    public Moneda monedaPrefab;              // la que sueltan los zombis al morir
    public float crecimientoMonedas = 1.05f; // cada moneda vale crecimientoMonedas^(oleada - 1)
    public int bonoPorOleada = 2;            // al completar la oleada N se cobran bonoPorOleada * N

    [Header("HUD")]
    public TMP_Text textoOleada;             // "Oleada N" y "Zombis muertos/total" en el HUD
    public TMP_Text cartelOleada;            // cartel grande que se prende durante el descanso

    public int OleadaActual { get; private set; }

    public float MultiplicadorVidaActual => Escalado.PorOleada(crecimientoVida, OleadaActual);
    public float MultiplicadorDanoActual => Escalado.PorOleada(crecimientoDano, OleadaActual);

    // Incluye el botin: es lo que vale cada moneda de un zombi de la oleada actual.
    public float MultiplicadorMonedasActual => Escalado.PorOleada(crecimientoMonedas, OleadaActual) * botin;

    // La mejora de botin se lee una vez al empezar: no se puede comprar en medio
    // de la partida. El bono de la oleada no la usa.
    private float botin = 1f;

    // Un zombi de la oleada con el numero de aparicion con que salio: los zombis se
    // reusan, y uno que murio puede volver a salir como otro de la misma oleada.
    private struct ZombiAnotado
    {
        public EnemyController zombi;
        public int aparicion;
    }

    private readonly List<ZombiAnotado> zombisDeLaOleada = new List<ZombiAnotado>();
    private int zombisEnLaOleada;
    private int muertosMostrados = -1;
    private int oleadaMostrada = -1;
    private int bonoDeLaOleadaAnterior;

    private void Start()
    {
        if (Plataforma.EsMovil) maxZombisVivos = maxZombisVivosMovil;
        if (cartelOleada != null) cartelOleada.gameObject.SetActive(false);
        botin = CatalogoMejoras.MultiplicadorBotin;

        // Una partida que quedo a medias sigue en la oleada en que se dejo, desde el
        // principio de esa oleada y con los puntos que se tenian al empezarla.
        int guardada = Progreso.OleadaEnCurso;
        if (guardada > 1)
        {
            OleadaActual = guardada - 1;
            if (Puntaje.instance != null)
            {
                Puntaje.instance.contadorKill = Progreso.PuntosEnCurso;
                Puntaje.instance.UpdateKillCounterUI();
            }
        }
        StartCoroutine(Jugar());
    }

    // Una sola corrutina para toda la partida. Relanzar una por oleada desde
    // Update ya salio mal: se arrancaba una nueva en cada frame de la espera.
    private IEnumerator Jugar()
    {
        while (true)
        {
            OleadaActual++;
            Progreso.GuardarOleadaEnCurso(OleadaActual, Puntaje.instance != null ? Puntaje.instance.contadorKill : 0);
            Progreso.Guardar();
            int cantidad = zombisBase + zombisPorOleada * OleadaActual;
            bool conJefe = jefe != null && jefeCadaOleadas > 0 && OleadaActual % jefeCadaOleadas == 0;
            zombisEnLaOleada = cantidad + (conJefe ? 1 : 0);
            zombisDeLaOleada.Clear();
            ActualizarHud();
            yield return Descanso();

            if (conJefe)
            {
                Aparecer(jefe);
            }

            for (int i = 0; i < cantidad; i++)
            {
                while (EnemyController.ZombisVivos >= maxZombisVivos)
                {
                    yield return null;
                }

                Aparecer(ElegirTipo());
                yield return new WaitForSeconds(intervaloEntreApariciones);
            }

            while (QuedanZombisDeLaOleada())
            {
                yield return null;
            }

            // Si el jugador murio en el mismo paso en que cayo el ultimo zombi, la oleada
            // no cuenta mientras la oferta de revivir espera: si revive sigue, y si no,
            // la partida termina en esta oleada. Antes se completaba y se guardaba la
            // siguiente como oleada en curso.
            while (PlayerHealth.instance != null && PlayerHealth.instance.EstaMuerto)
            {
                yield return null;
            }

            bonoDeLaOleadaAnterior = bonoPorOleada * OleadaActual;
            Progreso.Sumar(bonoDeLaOleadaAnterior);
            Progreso.RegistrarOleadaCompletada(OleadaActual);
            MisionesDiarias.RegistrarOleada(OleadaActual);
            Progreso.Guardar();
        }
    }

    private void Update()
    {
        ActualizarHud();
    }

    // Los zombis de la oleada que ya no estan cuentan como muertos, tambien los
    // caidos por el kill-Z: la oleada termina cuando no queda ninguno, y el
    // contador tiene que llegar al total justo en ese momento.
    private void ActualizarHud()
    {
        if (textoOleada == null) return;

        int muertos = 0;
        for (int i = 0; i < zombisDeLaOleada.Count; i++)
        {
            if (!EnemyController.SigueVivo(zombisDeLaOleada[i].zombi, zombisDeLaOleada[i].aparicion)) muertos++;
        }

        // Solo al cambiar: armar el texto por frame aloca por frame.
        if (muertos == muertosMostrados && OleadaActual == oleadaMostrada) return;
        muertosMostrados = muertos;
        oleadaMostrada = OleadaActual;
        textoOleada.text = Textos.Formato("hud_oleada", OleadaActual, muertos, zombisEnLaOleada);
    }

    private IEnumerator Descanso()
    {
        if (cartelOleada != null)
        {
            cartelOleada.text = Textos.Formato("cartel_oleada", OleadaActual);
            if (bonoDeLaOleadaAnterior > 0)
            {
                cartelOleada.text += Textos.Formato("cartel_bono", bonoDeLaOleadaAnterior, OleadaActual - 1);
            }
            cartelOleada.gameObject.SetActive(true);
            Efectos.CartelOleada();
        }

        yield return new WaitForSeconds(descansoEntreOleadas);

        if (cartelOleada != null) cartelOleada.gameObject.SetActive(false);
    }

    private GameObject ElegirTipo()
    {
        float total = 0f;
        foreach (var tipo in tipos)
        {
            if (tipo.prefab != null && OleadaActual >= tipo.desdeOleada) total += tipo.peso;
        }
        if (total <= 0f) return null;

        float sorteo = Random.value * total;
        foreach (var tipo in tipos)
        {
            if (tipo.prefab == null || OleadaActual < tipo.desdeOleada) continue;
            sorteo -= tipo.peso;
            if (sorteo <= 0f) return tipo.prefab;
        }
        return null;
    }

    // Un punto de aparicion al azar lejos del jugador: si el jugador esta parado al
    // lado de uno, el zombi nacia encima y pegaba en el acto. Si todos estan cerca,
    // el mas lejano.
    private Transform ElegirPunto()
    {
        Transform jugador = PlayerHealth.instance != null ? PlayerHealth.instance.transform : null;
        if (jugador == null) return spawnPoints[Random.Range(0, spawnPoints.Length)];

        float minimo = distanciaMinimaAlJugador * distanciaMinimaAlJugador;
        int inicio = Random.Range(0, spawnPoints.Length);
        Transform masLejano = null;
        float mayor = -1f;
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            Transform punto = spawnPoints[(inicio + i) % spawnPoints.Length];
            if (punto == null) continue;
            Vector3 d = punto.position - jugador.position;
            d.y = 0f;
            float distancia = d.sqrMagnitude;
            if (distancia >= minimo) return punto;
            if (distancia > mayor) { mayor = distancia; masLejano = punto; }
        }
        return masLejano;
    }

    private void Aparecer(GameObject prefab)
    {
        if (prefab == null || spawnPoints.Length == 0) return;

        Transform punto = ElegirPunto();
        if (punto == null) return;

        var enemigo = EnemyController.Aparecer(prefab, punto.position);
        if (enemigo == null) return;

        // Antes de su primer golpe, que es cuando calcula la vida. Los .asset no se
        // tocan: son los stats de la oleada 1.
        enemigo.multiplicadorVida = Escalado.PorOleada(crecimientoVida, OleadaActual);
        enemigo.multiplicadorDano = Escalado.PorOleada(crecimientoDano, OleadaActual);
        enemigo.multiplicadorMonedas = Escalado.PorOleada(crecimientoMonedas, OleadaActual) * botin;
        enemigo.monedaPrefab = monedaPrefab;
        enemigo.EsJefe = prefab == jefe;
        zombisDeLaOleada.Add(new ZombiAnotado { zombi = enemigo, aparicion = enemigo.NumeroDeAparicion });
    }

    // Los zombis muertos, o caidos por el kill-Z, ya no siguen vivos en la aparicion
    // anotada, aunque su objeto haya vuelto a salir del pool.
    private bool QuedanZombisDeLaOleada()
    {
        for (int i = 0; i < zombisDeLaOleada.Count; i++)
        {
            if (EnemyController.SigueVivo(zombisDeLaOleada[i].zombi, zombisDeLaOleada[i].aparicion)) return true;
        }
        return false;
    }
}
