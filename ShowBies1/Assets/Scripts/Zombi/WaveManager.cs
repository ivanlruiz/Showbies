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
    [Tooltip("La ultima oleada en que sale; 0 sin fin. Para que un tipo descanse un capitulo, va dos veces (ZOMBIS.md).")]
    public int hastaOleada;
    public float peso = 1f;
    [Tooltip("Cuantos vivos a la vez como mucho; 0 sin tope.")]
    public int maxVivos;
    [Tooltip("Los de habilidad (ZOMBIS.md): entre todos, como mucho WaveManager.MaxEspecialesVivos a la vez.")]
    public bool especial;
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
    // 10 + 2n, uno cada 0,20 s, desde la 1.6.0 (13/10, de Gemini: eran 10 + 4n cada 0,35 s, y
    // llegar a la 50 llevaba 35 minutos; ahora unos 20). Economia los lee de aca (ZombisBase y
    // ZombisPorOleadaDeWaveMode), y la prueba de logica compara con WaveMode.unity.
    public int zombisBase = 10;              // zombis por oleada = zombisBase + zombisPorOleada * oleada
    public int zombisPorOleada = 2;
    public float intervaloEntreApariciones = 0.2f;
    public float esperaTrasElJefe = 1.5f;    // entre el jefe y el primer zombi de su oleada
    public float descansoEntreOleadas = 3f;
    public int maxZombisVivos = 60;          // techo de poblacion: si esta lleno, la oleada espera
    public int maxZombisVivosMovil = 35;     // en movil cada zombi cuesta mas; ver GeneradorZombis

    // Con las mejoras compradas el jugador pega mas y aguanta mas: sin esto, a las
    // pocas compras las oleadas dejaban de ser un desafio. La vida y el golpe crecen
    // compuestos por oleada, en tres tramos (Escalado.PorOleada).
    //
    // La curva de la 1.6.0 la cerro Argon (Gemini) el 14/10 con la simulacion
    // (simulaciones/cartas_1.6.0, resultados_arranque_v3.md), despues de que Ivan la probo en el
    // telefono: con las cartas el jugador medio llegaba a la 49 en su tercera partida sin haber
    // perdido nunca. Lo duro paso a las oleadas 11-30, donde se arma el mazo de cartas, y el
    // tercer tramo es mas suave: el muro queda en la 48 / 59 / 69 (bajo, medio, alto), y el medio
    // muere en las oleadas 3, 6, 8, 14 y 23 de sus primeras cinco partidas. Antes: x1,11/x1,07,
    // desde la 11 x1,08/x1,04 (la 1.5.2) y desde la 26 x1,20/x1,10 (13/10).
    [Header("Dificultad")]
    public float crecimientoVida = 1.13f;    // la vida de cada zombi es hp * crecimientoVida^(oleada - 1)
    public float crecimientoDano = 1.09f;    // su golpe es daño * crecimientoDano^(oleada - 1)
    // Desde esta oleada, el segundo tramo, hasta el tercer jefe. 0 = sin segundo tramo.
    public int oleadaDelSegundoTramo = 11;
    public float crecimientoVidaTardio = 1.145f;
    public float crecimientoDanoTardio = 1.085f;
    // Y desde esta, el tercero, que pone el muro para el renacer de la 1.8.0. 0 = sin tercer tramo.
    public int oleadaDelTercerTramo = 31;
    public float crecimientoVidaTercerTramo = 1.12f;
    public float crecimientoDanoTercerTramo = 1.065f;

    [Header("Monedas")]
    public Moneda monedaPrefab;              // la que sueltan los zombis al morir
    // Cada moneda vale crecimientoMonedas^(oleada - 1) por valorDeLaMoneda. Desde el 14/10 (de
    // Argon) crece x1,03 y vale x1 de base, y el bono es de 2 por oleada: con x1,08, x1,4 y 4 por
    // oleada, el que pasaba la 10 juntaba media tienda de un tiron y la partida siguiente llegaba
    // quince oleadas mas lejos. Es el valor, no la cantidad: el telefono tiene un techo de 80
    // monedas. Economia copia el crecimiento y el valor (CrecimientoMonedasOleadas y
    // MultiplicadorMonedasOleadas) y la prueba los compara.
    public float crecimientoMonedas = 1.03f;
    public float valorDeLaMoneda = 1f;
    public int bonoPorOleada = 2;            // al completar la oleada N se cobran bonoPorOleada * N

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

    // El zombi del tesoro (ZombiDelTesoro, mejora 8 de la revision del 9/10): con esta
    // probabilidad por oleada desde DesdeOleadaTesoro, a mitad de la oleada, cerca del jugador
    // y de costado (a la vista: es para perseguirlo). No cuenta en la oleada, que no lo espera.
    // El prefab esta en Resources (Prefabs/Personajes/Resources), asi la escena no lo cablea.
    public const float ProbabilidadTesoro = 0.25f;
    public const int DesdeOleadaTesoro = 3;
    public const float DistanciaDelTesoro = 6f;   // a 11 nacia en el borde de la pantalla y se iba en el acto
    public const string RutaTesoro = "ZombiTesoro";
    private GameObject tesoroPrefab;

    // Lo prenden los bancos en play (RespaldoDelBanco): sin tesoro al azar. SacarTesoro sigue andando.
    public static bool SinTesoroAutomatico { get; set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetearEstadoCompartido()
    {
        SinTesoroAutomatico = false;
        actual = null;
    }

    // El de la escena (uno solo, el de WaveMode), para lo que pregunta por la oleada sin cablearlo: el escupidor.
    private static WaveManager actual;

    private void OnEnable() { actual = this; }
    private void OnDisable() { if (actual == this) actual = null; }

    // El final de la oleada (de Argon, 18/10): ya salieron todos sus zombis y quedan 'pocos' o menos vivos, o los que
    // quedan son todos escupidores. Ahi el escupidor deja de plantarse lejos y se acerca al alcance del auto-apuntado:
    // el jugador casual no tiene que salir a cazar a mano al ultimo para cerrar la oleada. Se calcula una vez por paso
    // de fisica (lo pregunta cada escupidor en cada uno). Sin WaveManager (el modo libre), nunca.
    public static bool FinalDeLaOleada(int pocos)
    {
        var w = actual;
        if (w == null || !w.isActiveAndEnabled) return false;
        if (w.finalCalculadoEn != Time.fixedTime || w.finalCalculadoCon != pocos)
        {
            w.finalCalculadoEn = Time.fixedTime;
            w.finalCalculadoCon = pocos;
            w.esFinal = EsFinal(w.zombisEnLaOleada, w.zombisDeLaOleada, pocos);
        }
        return w.esFinal;
    }

    private float finalCalculadoEn = -1f;
    private int finalCalculadoCon = -1;
    private bool esFinal;

    private static bool EsFinal(int enLaOleada, List<ZombiAnotado> salieron, int pocos)
    {
        if (enLaOleada <= 0 || salieron.Count < enLaOleada) return false;
        int vivos = 0;
        bool soloEscupidores = true;
        for (int i = 0; i < salieron.Count; i++)
        {
            if (!EnemyController.SigueVivo(salieron[i].zombi, salieron[i].aparicion)) continue;
            vivos++;
            if (!(salieron[i].zombi.MovimientoPropio is ZombiEscupidor)) soloEscupidores = false;
        }
        return EsFinalCon(enLaOleada, salieron.Count, vivos, soloEscupidores, pocos);
    }

    // La cuenta, sin escena, para la prueba: salieron todos, y quedan pocos o solo escupidores.
    public static bool EsFinalCon(int enLaOleada, int salieron, int vivos, bool soloEscupidores, int pocos)
    {
        if (enLaOleada <= 0 || salieron < enLaOleada) return false;
        return vivos <= pocos || soloEscupidores;
    }
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

    public float MultiplicadorVidaActual => MultiplicadorVidaEn(OleadaActual);
    public float MultiplicadorDanoActual => MultiplicadorDanoEn(OleadaActual);

    // La vida y el golpe de los zombis de una oleada, en sus dos tramos. Publicos para las
    // pruebas, que comparan contra esto lo que de verdad le pusieron a cada zombi.
    public float MultiplicadorVidaEn(int oleada)
    {
        return Escalado.PorOleada(crecimientoVida, oleada, oleadaDelSegundoTramo, crecimientoVidaTardio,
                                  oleadaDelTercerTramo, crecimientoVidaTercerTramo);
    }

    public float MultiplicadorDanoEn(int oleada)
    {
        return Escalado.PorOleada(crecimientoDano, oleada, oleadaDelSegundoTramo, crecimientoDanoTardio,
                                  oleadaDelTercerTramo, crecimientoDanoTercerTramo);
    }

    // Lo que vale cada moneda de un zombi de una oleada, sin el botin (las pruebas).
    public float MultiplicadorMonedasEn(int oleada)
    {
        return Escalado.PorOleada(crecimientoMonedas, oleada) * valorDeLaMoneda;
    }

    // Incluye el botin: es lo que vale cada moneda de un zombi de la oleada actual (y del tesoro).
    public float MultiplicadorMonedasActual => MultiplicadorMonedasEn(OleadaActual) * botin;

    // La mejora de botin se lee una vez al empezar la escena: lo que se compra con MEJORAS de la
    // pausa entra al volver, que carga la escena de nuevo. El bono de la oleada no la usa.
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

    // La partida retomada (Progreso.HayOleadaParaRetomar): su primera oleada no es nueva, y si ya habia recibido un
    // golpe antes de salir no puede salir perfecta.
    private bool retomada;
    private bool golpeHeredado;
    // Los golpes del jugador al terminar el descanso de la oleada en juego, para anotar el primero (-1 fuera de una).
    private int golpesAlEmpezarLaOleada = -1;
    private bool golpeAnotado;

    // La vida y las balas con que se retoma la oleada: las que habia al empezarla la primera vez (salir al menu no cura
    // ni recarga, superauditoria del 29/9), sin pasar del maximo de ahora. La vida maxima que se compro en el medio
    // (la pausa tiene MEJORAS) se suma, como la carta de vida, que cura solo lo que suma. Sin anotar, las de arranque:
    // la vida llena y las balas de siempre (-1).
    public static int VidaAlRetomar(int guardada, int maximaGuardada, int maxima)
    {
        if (guardada <= 0) return maxima;
        int comprada = maximaGuardada > 0 ? Mathf.Max(0, maxima - maximaGuardada) : 0;
        return Mathf.Min(guardada + comprada, maxima);
    }

    public static int BalasAlRetomar(int guardadas, int maximoGuardado, int maximo)
    {
        return guardadas >= 0 && maximoGuardado > 0 ? Mathf.Min(guardadas, maximo) : -1;
    }

    private void Start()
    {
        if (Plataforma.EsMovil) maxZombisVivos = maxZombisVivosMovil;
        EnemyController.FijarTecho(maxZombisVivos);
        if (cartelOleada != null) cartelOleada.gameObject.SetActive(false);
        botin = CatalogoMejoras.MultiplicadorBotin;
        tesoroPrefab = Resources.Load<GameObject>(RutaTesoro);

        // Una partida que quedo a medias sigue en la oleada en que se dejo, desde el
        // principio de esa oleada y con los puntos que se tenian al empezarla. Una sola vez por
        // oleada: con la retoma gastada, la partida se olvida y esta arranca de cero.
        int guardada = Progreso.OleadaEnCurso;
        retomada = Progreso.HayOleadaParaRetomar;
        if (retomada)
        {
            OleadaActual = guardada - 1;
            golpeHeredado = Progreso.GolpeEnOleadaEnCurso;
            Progreso.ContarRetoma();
            if (Puntaje.instance != null)
            {
                Puntaje.instance.contadorKill = Progreso.PuntosEnCurso;
                Puntaje.instance.UpdateKillCounterUI();
            }
        }
        else if (guardada > 1)
        {
            Progreso.OlvidarOleadaEnCurso();
        }
        StartCoroutine(Jugar());
    }

    // Una sola corrutina para toda la partida. Relanzar una por oleada desde
    // Update ya salio mal: se arrancaba una nueva en cada frame de la espera.
    private IEnumerator Jugar()
    {
        // Retomada: un cuadro, para que AplicarMejoras y las cartas de vida terminen de llenar la vida, y despues la vida
        // y las balas con que se empezo la oleada.
        if (retomada)
        {
            yield return null;
            DevolverVidaYBalas();
        }
        bool primera = true;
        while (true)
        {
            OleadaActual++;
            bool nueva = !(primera && retomada);
            bool yaGolpeada = !nueva && golpeHeredado;
            primera = false;
            var jugadorAlEmpezar = PlayerHealth.instance;
            var controlAlEmpezar = jugadorAlEmpezar != null ? jugadorAlEmpezar.GetComponent<PlayerController>() : null;
            Progreso.GuardarOleadaEnCurso(OleadaActual, Puntaje.instance != null ? Puntaje.instance.contadorKill : 0, nueva,
                                          jugadorAlEmpezar != null ? jugadorAlEmpezar.health : -1,
                                          jugadorAlEmpezar != null ? jugadorAlEmpezar.maxHealth : -1,
                                          controlAlEmpezar != null ? controlAlEmpezar.cantBalas : -1,
                                          controlAlEmpezar != null ? controlAlEmpezar.maxBalas : -1);
            // Las cartas, como estan al empezar la oleada: retomarla las devuelve asi.
            Progreso.GuardarPoderEnCurso(PoderDeLaPartida.Instantanea());
            // En otro hilo: el fsync en el del juego era un tiron justo con el cartel de la oleada.
            Progreso.GuardarEnSegundoPlano();
            int cantidad = zombisBase + zombisPorOleada * OleadaActual;
            bool conTesoro = OleadaActual >= DesdeOleadaTesoro && Random.value < ProbabilidadTesoro;
            bool conJefe = jefe != null && jefeCadaOleadas > 0 && OleadaActual % jefeCadaOleadas == 0;
            zombisEnLaOleada = cantidad + (conJefe ? 1 : 0);
            zombisDeLaOleada.Clear();
            ActualizarHud();
            yield return Descanso();
            // Los golpes, recien despues del descanso: un charco de acido de la oleada anterior que pega mientras se lee
            // el cartel no le tiene que quitar la perfecta a esta (auditoria del 18/10).
            int golpesAlEmpezar = PlayerHealth.instance != null ? PlayerHealth.instance.GolpesRecibidos : -1;
            golpesAlEmpezarLaOleada = golpesAlEmpezar;
            golpeAnotado = yaGolpeada;

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
                if (conTesoro && i == cantidad / 2 && !SinTesoroAutomatico) SacarTesoro();
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

            // Una parte del cargador, y lleno despues de un jefe (14/10, de Argon: lleno en cada
            // oleada, como desde el 13/10, nadie se quedaba nunca sin balas; antes, el que juntaba
            // pocas cajas pasaba un cuarto del tiempo sin balas).
            RecargarAlTerminar(conJefe);

            // Sin un solo golpe en toda la oleada: el bono doble y el logro INTOCABLE.
            golpesAlEmpezarLaOleada = -1;
            bool perfecta = !yaGolpeada && golpesAlEmpezar >= 0 && PlayerHealth.instance != null
                            && PlayerHealth.instance.GolpesRecibidos == golpesAlEmpezar;
            UltimaFuePerfecta = perfecta;
            bonoDeLaOleadaAnterior = Bono(bonoPorOleada, OleadaActual, perfecta);
            Efectos.OleadaSuperada(dondeQuedabaElUltimo);
            Progreso.Sumar(bonoDeLaOleadaAnterior);
            Progreso.RegistrarOleadaCompletada(OleadaActual);
            if (perfecta) Progreso.RegistrarOleadaIntacta(OleadaActual);
            // Hasta la oleada 5 no hay misiones, ni desafio hasta el nivel 5 (Desbloqueo). Completar
            // la 4 destapa las misiones, y como la marca ya se anoto, esta oleada ya cuenta.
            if (Desbloqueo.Misiones) MisionesDiarias.RegistrarOleada(OleadaActual);
            if (Desbloqueo.Todo) DesafioSemanal.RegistrarOleada();
            // Sin Guardar aca: lo de esta oleada (el bono, la marca, las misiones) lo
            // escribe el Guardar del principio de la vuelta siguiente, que corre en este
            // mismo cuadro porque no hay ningun yield en el medio. Guardar aca tambien
            // escribia el archivo entero dos veces seguidas, justo con el cartel.
        }
    }

    private void Update()
    {
        ActualizarHud();
        AnotarElPrimerGolpe();
    }

    // El primer golpe de la oleada queda en el progreso: si se sale y se retoma, ya no sale perfecta.
    private void AnotarElPrimerGolpe()
    {
        if (golpeAnotado || golpesAlEmpezarLaOleada < 0 || PlayerHealth.instance == null) return;
        if (PlayerHealth.instance.GolpesRecibidos <= golpesAlEmpezarLaOleada) return;
        golpeAnotado = true;
        Progreso.MarcarGolpeEnOleadaEnCurso();
    }

    private void DevolverVidaYBalas()
    {
        var jugador = PlayerHealth.instance;
        if (jugador == null || jugador.EstaMuerto) return;
        jugador.health = VidaAlRetomar(Progreso.VidaEnCurso, Progreso.VidaMaximaEnCurso, jugador.maxHealth);
        var control = jugador.GetComponent<PlayerController>();
        if (control == null) return;
        int balas = BalasAlRetomar(Progreso.BalasEnCurso, Progreso.MaxBalasEnCurso, Mathf.Max(control.maxBalas, Progreso.MaxBalasEnCurso));
        if (balas < 0) return;
        // El cargador de 1.000 de la caja de arma es para siempre: si se tenia, vuelve.
        control.maxBalas = Mathf.Max(control.maxBalas, Progreso.MaxBalasEnCurso);
        control.cantBalas = balas;
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

    // Los zombis con habilidad (ZOMBIS.md, de Argon): cada tipo tiene su tope de vivos, y entre todos no pasan de 8 a
    // la vez, asi de los 35 del telefono siempre quedan al menos 27 comunes haciendo bulto.
    public const int MaxEspecialesVivos = 8;

    // Si ese tipo puede salir ahora. Estatico para probarlo sin escena.
    public static bool Elegible(TipoEnOleada tipo, int oleada, int vivosDelTipo, int especialesVivos)
    {
        if (tipo == null || tipo.prefab == null || oleada < tipo.desdeOleada || !(tipo.peso > 0f)) return false;
        if (tipo.hastaOleada > 0 && oleada > tipo.hastaOleada) return false;
        if (tipo.maxVivos > 0 && vivosDelTipo >= tipo.maxVivos) return false;
        if (tipo.especial && especialesVivos >= MaxEspecialesVivos) return false;
        return true;
    }

    // Los especiales vivos, contando cada prefab una vez: el que descansa un capitulo va dos veces en la mezcla, y
    // sumandolo por entrada el tope real quedaba en 5 o 6 en vez de 8 (auditoria del 18/10). 'vivosDe' cuenta los de un
    // prefab (EnemyController.VivosDe, o lo que diga la prueba).
    public static int EspecialesVivos(IReadOnlyList<TipoEnOleada> lista, System.Func<GameObject, int> vivosDe)
    {
        int especiales = 0;
        if (lista == null) return 0;
        for (int i = 0; i < lista.Count; i++)
        {
            var tipo = lista[i];
            if (tipo == null || !tipo.especial || tipo.prefab == null) continue;
            bool repetido = false;
            for (int j = 0; j < i && !repetido; j++)
                repetido = lista[j] != null && lista[j].especial && lista[j].prefab == tipo.prefab;
            if (!repetido) especiales += vivosDe(tipo.prefab);
        }
        return especiales;
    }

    private static readonly System.Func<GameObject, int> vivosDelPrefab = EnemyController.VivosDe;

    private GameObject ElegirTipo()
    {
        int especiales = EspecialesVivos(tipos, vivosDelPrefab);

        float total = 0f;
        foreach (var tipo in tipos)
        {
            if (Elegible(tipo, OleadaActual, EnemyController.VivosDe(tipo != null ? tipo.prefab : null), especiales)) total += tipo.peso;
        }
        if (total <= 0f) return null;

        float sorteo = Random.value * total;
        foreach (var tipo in tipos)
        {
            if (!Elegible(tipo, OleadaActual, EnemyController.VivosDe(tipo != null ? tipo.prefab : null), especiales)) continue;
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
        enemigo.multiplicadorVida = MultiplicadorVidaActual;
        enemigo.multiplicadorDano = MultiplicadorDanoActual;
        enemigo.multiplicadorMonedas = MultiplicadorMonedasActual;
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

    // Lo que se recarga al terminar cada oleada: esta parte del cargador (de 500, o 1.000 con la
    // caja del arma), o el cargador entero si la oleada tenia jefe. Solo en las oleadas: el modo
    // libre y el tutorial no tienen WaveManager.
    public const float RecargaPorOleada = 0.25f;

    // Las balas despues de recargar: puro, para la prueba de logica.
    public static int BalasAlRecargar(int balas, int maximo, bool conJefe)
    {
        if (conJefe) return Mathf.Max(balas, maximo);
        return Mathf.Max(balas, Mathf.Min(maximo, balas + Mathf.RoundToInt(maximo * RecargaPorOleada)));
    }

    // Publico para el banco.
    public static void RecargarAlTerminar(bool conJefe)
    {
        var jugador = PlayerHealth.instance;
        if (jugador == null || jugador.EstaMuerto) return;
        var control = jugador.GetComponent<PlayerController>();
        if (control != null) control.cantBalas = BalasAlRecargar(control.cantBalas, control.maxBalas, conJefe);
    }

    // Saca el zombi del tesoro a DistanciaDelTesoro del jugador, a un costado (en la pantalla
    // apaisada, al este o al oeste se ve; al norte o al sur, a esa distancia, no), con los
    // multiplicadores de la oleada. Publico para el banco (PruebaTesoro).
    public EnemyController SacarTesoro()
    {
        var jugador = PlayerHealth.instance;
        if (tesoroPrefab == null || jugador == null || jugador.EstaMuerto) return null;
        float angulo = Random.Range(-25f, 25f) + (Random.value < 0.5f ? 0f : 180f);
        Vector3 donde = LugarDelTesoro(jugador.transform.position, angulo);
        // Contra una pared, del otro lado: si no, acomodado adentro del mapa nacia encima del jugador.
        if (Vector3.Distance(Plano(donde), Plano(jugador.transform.position)) < DistanciaDelTesoro - 1f)
            donde = LugarDelTesoro(jugador.transform.position, angulo + 180f);
        var tesoro = EnemyController.Aparecer(tesoroPrefab, donde);
        if (tesoro == null) return null;
        tesoro.multiplicadorVida = MultiplicadorVidaActual;
        tesoro.multiplicadorDano = MultiplicadorDanoActual;
        tesoro.multiplicadorMonedas = MultiplicadorMonedasActual;
        tesoro.monedaPrefab = monedaPrefab;
        Efectos.TesoroAparece(tesoro.transform.position);
        return tesoro;
    }

    public static Vector3 LugarDelTesoro(Vector3 jugador, float angulo)
    {
        Vector3 donde = jugador + Quaternion.Euler(0f, angulo, 0f) * Vector3.right * DistanciaDelTesoro;
        donde.x = Mathf.Clamp(donde.x, -42f, 42f);
        donde.z = Mathf.Clamp(donde.z, -42f, 42f);
        donde.y = 0f;
        return donde;
    }

    private static Vector3 Plano(Vector3 v)
    {
        v.y = 0f;
        return v;
    }

    // Cuantos zombis le faltan matar a la oleada (los que no salieron todavia tambien), y en
    // 'vivos' los que estan en el mapa. Para las flechas del borde (FlechasDelBorde), que
    // señalan a los ultimos.
    public int FaltanDeLaOleada(List<EnemyController> vivos)
    {
        vivos.Clear();
        for (int i = 0; i < zombisDeLaOleada.Count; i++)
            if (EnemyController.SigueVivo(zombisDeLaOleada[i].zombi, zombisDeLaOleada[i].aparicion))
                vivos.Add(zombisDeLaOleada[i].zombi);
        return Mathf.Max(0, zombisEnLaOleada - zombisDeLaOleada.Count) + vivos.Count;
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
