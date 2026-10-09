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

    // La oleada que se pierde si se reinicia ahora, o 0 si no se pierde nada que valga
    // preguntar: fuera de las oleadas, o en la 1. La mira la pausa antes de REINICIAR.
    public static int OleadaQueSePierdeAlReiniciar
    {
        get
        {
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex != TiendaMejoras.EscenaOleadas) return 0;
            int oleada = Progreso.OleadaEnCurso;
            return oleada > 1 ? oleada : 0;
        }
    }

    [Header("Zombis")]
    public TipoEnOleada[] tipos;
    public GameObject jefe;
    public int jefeCadaOleadas = 10;
    public Transform[] spawnPoints;          // se elige uno al azar por zombi
    public float distanciaMinimaAlJugador = 8f; // ninguno nace mas cerca que esto, si se puede

    [Header("Ritmo")]
    public int zombisBase = 10;              // zombis por oleada = zombisBase + zombisPorOleada * oleada
    public int zombisPorOleada = 4;
    public float intervaloEntreApariciones = 0.35f;
    public float esperaTrasElJefe = 1.5f;    // entre el jefe y el primer zombi de su oleada
    public float descansoEntreOleadas = 3f;
    public int maxZombisVivos = 60;          // techo de poblacion: si esta lleno, la oleada espera
    public int maxZombisVivosMovil = 35;     // en movil cada zombi cuesta mas; ver GeneradorZombis

    // Con las mejoras compradas el jugador pega mas y aguanta mas: sin esto, a las
    // pocas compras las oleadas dejaban de ser un desafio. La vida crece 11 % por
    // oleada, compuesto, y el daño de bala suma 1 por nivel: las primeras compras le
    // ganan al escalado, y mas adelante hace falta sumarles la cadencia.
    [Header("Dificultad")]
    public float crecimientoVida = 1.11f;    // la vida de cada zombi es hp * crecimientoVida^(oleada - 1)
    public float crecimientoDano = 1.07f;    // su golpe es daño * crecimientoDano^(oleada - 1)

    [Header("Monedas")]
    public Moneda monedaPrefab;              // la que sueltan los zombis al morir
    public float crecimientoMonedas = 1.08f; // cada moneda vale crecimientoMonedas^(oleada - 1)
    public int bonoPorOleada = 4;            // al completar la oleada N se cobran bonoPorOleada * N

    [Header("HUD")]
    public TMP_Text textoOleada;             // "Oleada N" y "Zombis muertos/total" en el HUD
    public TMP_Text cartelOleada;            // cartel grande que se prende durante el descanso

    public int OleadaActual { get; private set; }

    // Si esta en el descanso entre dos oleadas, con el cartel: el momento en que ir a la
    // tienda desde la pausa no hace perder nada (lo mira AvisoDeMisiones).
    public bool EnDescanso { get; private set; }

    // Terminar una oleada es un momento (revision del 9/10: pasaba en silencio y el bono se
    // cobraba sin que nadie lo viera): una pausa de impacto con el arpegio
    // (Efectos.OleadaSuperada), "¡OLEADA N SUPERADA!" con su bono durante la primera parte
    // del descanso y las monedas del bono volando al contador. Despues, el cartel de la
    // oleada que viene. El descanso dura lo mismo que antes: lo que se ve cambia, no el ritmo.
    public const float DuracionSuperada = 1.4f;
    private Vector3 dondeQuedabaElUltimo;
    private EfectosUI monedasDelBono;

    // Una oleada sin un solo golpe paga el bono doble (revision del 9/10, mejora 4), con
    // "¡PERFECTA! +N MONEDAS" en dorado: esquivar el zarpazo, la carga y el salto del jefe
    // tiene que pagar dentro de la partida, no solo en el logro INTOCABLE, que mira lo mismo.
    public const int MultiplicadorPerfecta = 2;
    public bool UltimaFuePerfecta { get; private set; }

    public static int Bono(int bonoPorOleada, int oleada, bool perfecta)
    {
        return bonoPorOleada * oleada * (perfecta ? MultiplicadorPerfecta : 1);
    }

    // La segunda linea del cartel de la oleada superada.
    public static string TextoDelBono(int bono, bool perfecta)
    {
        return Textos.Formato(perfecta ? "cartel_bono_perfecta" : "cartel_bono", bono);
    }

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
    private int totalMostrado = -1;
    private int bonoDeLaOleadaAnterior;

    private void Start()
    {
        if (Plataforma.EsMovil) maxZombisVivos = maxZombisVivosMovil;
        EnemyController.FijarTecho(maxZombisVivos);
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
            int golpesAlEmpezar = PlayerHealth.instance != null ? PlayerHealth.instance.GolpesRecibidos : -1;
            int cantidad = zombisBase + zombisPorOleada * OleadaActual;
            bool conJefe = jefe != null && jefeCadaOleadas > 0 && OleadaActual % jefeCadaOleadas == 0;
            zombisEnLaOleada = cantidad + (conJefe ? 1 : 0);
            zombisDeLaOleada.Clear();
            ActualizarHud();
            yield return Descanso();

            // El jefe sale solo y el primero de los demas, un rato despues y en otro punto:
            // salian en el mismo cuadro, y una de cada tres o cuatro veces en el mismo punto,
            // con el chico entero adentro de la capsula del jefe (1,44 m de radio y masa 1e9),
            // de donde la fisica lo sacaba empujandolo sobre todo hacia abajo, contra el piso.
            // En esperaTrasElJefe el jefe camina 3 m y deja el punto libre.
            Transform puntoDelJefe = null;
            if (conJefe)
            {
                puntoDelJefe = Aparecer(jefe);
                if (puntoDelJefe != null) yield return new WaitForSeconds(esperaTrasElJefe);
            }

            for (int i = 0; i < cantidad; i++)
            {
                // Los lugares que reservo un jefe que esta por invocar cuentan como ocupados.
                while (EnemyController.ZombisVivos + EnemyController.Reservados >= maxZombisVivos)
                {
                    yield return null;
                }

                Aparecer(ElegirTipo(), i == 0 ? puntoDelJefe : null);
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

            // Sin un solo golpe en toda la oleada: el bono doble y el logro INTOCABLE.
            bool perfecta = golpesAlEmpezar >= 0 && PlayerHealth.instance != null && PlayerHealth.instance.GolpesRecibidos == golpesAlEmpezar;
            UltimaFuePerfecta = perfecta;
            bonoDeLaOleadaAnterior = Bono(bonoPorOleada, OleadaActual, perfecta);
            Efectos.OleadaSuperada(dondeQuedabaElUltimo);
            Progreso.Sumar(bonoDeLaOleadaAnterior);
            Progreso.RegistrarOleadaCompletada(OleadaActual);
            if (perfecta) Progreso.RegistrarOleadaIntacta(OleadaActual);
            MisionesDiarias.RegistrarOleada(OleadaActual);
            DesafioSemanal.RegistrarOleada();
            // Sin Guardar aca: lo de esta oleada (el bono, la marca, las misiones) lo
            // escribe el Guardar del principio de la vuelta siguiente, que corre en este
            // mismo cuadro porque no hay ningun yield en el medio. Guardar aca tambien
            // escribia el archivo entero dos veces seguidas, justo con el cartel.
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
        // El total tambien cambia: el jefe suma los que invoca.
        if (muertos == muertosMostrados && OleadaActual == oleadaMostrada && zombisEnLaOleada == totalMostrado) return;
        muertosMostrados = muertos;
        oleadaMostrada = OleadaActual;
        totalMostrado = zombisEnLaOleada;
        textoOleada.text = Textos.Formato("hud_oleada", OleadaActual, muertos, zombisEnLaOleada);
    }

    private IEnumerator Descanso()
    {
        EnDescanso = true;
        float resto = descansoEntreOleadas;

        // La que se acaba de terminar, con su bono (en la primera de la partida, o en una
        // retomada, no hay).
        if (cartelOleada != null && bonoDeLaOleadaAnterior > 0)
        {
            cartelOleada.text = Textos.Formato("cartel_oleada_superada", OleadaActual - 1) + TextoDelBono(bonoDeLaOleadaAnterior, UltimaFuePerfecta);
            PrenderElCartel();
            VolarMonedasDelBono(OleadaActual - 1, UltimaFuePerfecta);
            if (UltimaFuePerfecta) Efectos.OleadaPerfecta();
            float primero = Mathf.Min(DuracionSuperada, resto * 0.6f);
            yield return new WaitForSeconds(primero);
            resto -= primero;
        }

        if (cartelOleada != null)
        {
            cartelOleada.text = Textos.Formato("cartel_oleada", OleadaActual);
            PrenderElCartel();
            Efectos.CartelOleada();
        }

        yield return new WaitForSeconds(resto);

        if (cartelOleada != null) cartelOleada.gameObject.SetActive(false);
        EnDescanso = false;
    }

    // Apagar y prender: el rebote (AparecerConRebote) arranca al prenderse.
    private void PrenderElCartel()
    {
        cartelOleada.gameObject.SetActive(false);
        cartelOleada.gameObject.SetActive(true);
    }

    // Las monedas del bono vuelan del cartel al contador del HUD, con una nota cada una. Los
    // efectos de UI son los de la tienda (EfectosUI), en un canvas propio arriba del HUD, que
    // se arma la primera vez. El bono ya se sumo: esto es lo que se ve.
    private void VolarMonedasDelBono(int oleada, bool perfecta)
    {
        var contador = FindAnyObjectByType<ContadorMonedas>();
        if (contador == null || contador.texto == null || cartelOleada.canvas == null) return;
        if (monedasDelBono == null)
        {
            var go = new GameObject("MonedasDelBono", typeof(RectTransform));
            go.SetActive(false);   // EfectosUI arma sus pools en Awake: primero los sprites
            var rt = (RectTransform)go.transform;
            rt.SetParent(cartelOleada.canvas.rootCanvas.transform, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var canvas = go.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = cartelOleada.canvas.rootCanvas.sortingOrder + 1;
            monedasDelBono = go.AddComponent<EfectosUI>();
            var circulo = TexturasUI.Circulo(64);
            monedasDelBono.spriteMoneda = Sprite.Create(circulo, new Rect(0f, 0f, circulo.width, circulo.height), new Vector2(0.5f, 0.5f));
            monedasDelBono.spriteParticula = monedasDelBono.spriteMoneda;
            monedasDelBono.fuente = cartelOleada.font;
            go.SetActive(true);
        }
        int cantidad = Mathf.Clamp(3 + oleada / 2, 4, 14);
        if (perfecta) cantidad = Mathf.Min(cantidad * MultiplicadorPerfecta, 20);
        monedasDelBono.MonedasVolando(cartelOleada.rectTransform, contador.Rect, cantidad, Efectos.MonedaDelBono);
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

    // Un punto de aparicion al azar lejos del jugador y fuera de la vista: si el jugador
    // esta parado al lado de uno, el zombi nacia encima y pegaba en el acto, y a los 8 m
    // todavia se esta en pantalla (ver EnemyController.SeVeriaAlAparecer), donde se
    // materializaba de la nada: kiteando cerca del punto este u oeste, uno de cada cuatro.
    // Si todos los lejanos se ven, el primero de esos; si todos estan cerca, el mas lejano.
    // "evitar" es donde acaba de salir el jefe: solo si no queda otro.
    private Transform ElegirPunto(Transform evitar)
    {
        Transform jugador = PlayerHealth.instance != null ? PlayerHealth.instance.transform : null;
        Camera camara = Camera.main;
        float minimo = distanciaMinimaAlJugador * distanciaMinimaAlJugador;
        int inicio = Random.Range(0, spawnPoints.Length);
        Transform lejosPeroALaVista = null, masLejano = null;
        float mayor = -1f;
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            Transform punto = spawnPoints[(inicio + i) % spawnPoints.Length];
            if (punto == null || punto == evitar) continue;
            if (jugador == null) return punto;
            Vector3 d = punto.position - jugador.position;
            d.y = 0f;
            float distancia = d.sqrMagnitude;
            if (distancia >= minimo)
            {
                if (!EnemyController.SeVeriaAlAparecer(camara, punto.position)) return punto;
                if (lejosPeroALaVista == null) lejosPeroALaVista = punto;
            }
            if (distancia > mayor) { mayor = distancia; masLejano = punto; }
        }
        if (lejosPeroALaVista != null) return lejosPeroALaVista;
        if (masLejano != null) return masLejano;
        return evitar;
    }

    // Devuelve el punto donde salio, o null si no salio.
    private Transform Aparecer(GameObject prefab, Transform evitar = null)
    {
        if (prefab == null || spawnPoints.Length == 0) return null;

        Transform punto = ElegirPunto(evitar);
        if (punto == null) return null;

        var enemigo = EnemyController.Aparecer(prefab, punto.position);
        if (enemigo == null) return null;

        // Antes de su primer golpe, que es cuando calcula la vida. Los .asset no se
        // tocan: son los stats de la oleada 1.
        enemigo.multiplicadorVida = Escalado.PorOleada(crecimientoVida, OleadaActual);
        enemigo.multiplicadorDano = Escalado.PorOleada(crecimientoDano, OleadaActual);
        enemigo.multiplicadorMonedas = Escalado.PorOleada(crecimientoMonedas, OleadaActual) * botin;
        enemigo.monedaPrefab = monedaPrefab;
        enemigo.EsJefe = prefab == jefe;
        zombisDeLaOleada.Add(new ZombiAnotado { zombi = enemigo, aparicion = enemigo.NumeroDeAparicion });
        return punto;
    }

    // Los zombis muertos, o caidos por el kill-Z, ya no siguen vivos en la aparicion
    // anotada, aunque su objeto haya vuelto a salir del pool.
    // Los que invoca el jefe (JefePatrones) cuentan en la oleada y en el total del HUD: sin
    // esto la oleada terminaba con ellos vivos.
    public void SumarALaOleada(EnemyController zombi)
    {
        if (zombi == null) return;
        zombisDeLaOleada.Add(new ZombiAnotado { zombi = zombi, aparicion = zombi.NumeroDeAparicion });
        zombisEnLaOleada++;
    }

    // De paso anota donde esta el que queda: al terminar la oleada, las chispas salen ahi.
    private bool QuedanZombisDeLaOleada()
    {
        for (int i = 0; i < zombisDeLaOleada.Count; i++)
        {
            if (!EnemyController.SigueVivo(zombisDeLaOleada[i].zombi, zombisDeLaOleada[i].aparicion)) continue;
            dondeQuedabaElUltimo = zombisDeLaOleada[i].zombi.transform.position;
            return true;
        }
        return false;
    }
}
