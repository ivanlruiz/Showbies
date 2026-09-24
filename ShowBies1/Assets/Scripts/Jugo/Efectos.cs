using System.Collections.Generic;
using UnityEngine;

// El "jugo" de las escenas de juego: lo que hace que cada golpe, muerte y
// explosion se sienta. Vive en el prefab Assets/Prefabs/Jugo/Efectos.prefab,
// puesto en ShowBies1, WaveMode y Tutorial, con los sonidos, las chispas, las
// particulas de muerte, el material del destello, los numeros de daño y la
// musica de la partida.
//
// Los que producen los eventos (EnemyController, Granade, PlayerHealth,
// PlayerController, GunController, WaveManager) llaman a los metodos static,
// que no hacen nada si la escena no tiene Efectos: el juego anda igual, plano.
// La excepcion es ParticulasDeMuerte, que sin Efectos instancia las del zombi
// como antes.
public class Efectos : MonoBehaviour
{
    public static Efectos instance;

    [Header("Sonidos")]
    public AudioClip golpe;
    public AudioClip muerte;
    public AudioClip explosion;
    public AudioClip danioJugador;
    public AudioClip caja;
    public AudioClip cartelOleada;

    [Header("Chispas (un solo sistema por escena, con Emit)")]
    public ParticleSystem chispasPrefab;
    public int chispasPorGolpe = 3;
    public int chispasPorMuerte = 12;
    public int chispasPorMuerteGrande = 40;
    public int chispasPorExplosion = 60;
    public int chispasPorDisparo = 2;
    public int chispasPorCaja = 25;

    [Header("Furia del jugador")]
    public float temblorFuria = 0.6f;
    public float pausaFuria = 0.06f;
    public float tonoMusicaFuria = 1.12f;       // la musica va mas aguda mientras dura
    public float pulsoVinetaFuria = 0.35f;
    public float intervaloPulsoFuria = 0.45f;  // segundos entre latidos del borde rojo

    [Header("Destello de golpe")]
    public Material materialDestello;
    public float duracionDestello = 0.07f;

    [Header("Numeros de daño")]
    public NumeroFlotante numeroPrefab;
    public int maxNumeros = 40;

    [Header("Temblor de camara (trauma de 0 a 1 que suma cada evento)")]
    public float temblorMuerte = 0.06f;
    public float temblorMuerteGrande = 0.45f;
    public float temblorJefe = 1f;
    public float temblorExplosion = 0.7f;
    public float temblorDanio = 0.2f;
    public float separacionDanioVisual = 0.4f;   // segundos minimos entre dos temblores/destellos rojos por daño

    [Header("Pausa de impacto (camara lenta de un instante)")]
    public float escalaDeTiempoEnPausa = 0.05f;
    public float pausaMuerteGrande = 0.05f;
    public float pausaJefe = 0.18f;
    public float pausaExplosion = 0.04f;
    public int vidaParaMuerteGrande = 20;    // vida base desde la que una muerte es grande: el tanque
    public int vidaParaJefe = 100;

    private readonly Stack<NumeroFlotante> numerosLibres = new Stack<NumeroFlotante>();
    private int numerosCreados;
    private ParticleSystem chispas;
    private float pausaHasta;               // en tiempo sin escalar
    private bool enPausaDeImpacto;
    private float proximoDanioVisual;
    private AudioSource musica;
    private float furiaHasta = -1f;         // en Time.time; negativo sin furia
    private float proximoPulsoFuria;        // en tiempo sin escalar

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetearEstadoCompartido()
    {
        instance = null;
    }

    public static Material MaterialDestello { get { return instance != null ? instance.materialDestello : null; } }
    public static float DuracionDestello { get { return instance != null ? instance.duracionDestello : 0f; } }

    private void Awake()
    {
        instance = this;
        if (chispasPrefab != null) chispas = Instantiate(chispasPrefab, transform);
        musica = GetComponent<AudioSource>();
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;

        // timeScale es global: si la escena se descarga en plena pausa de impacto,
        // la siguiente arrancaria en camara lenta.
        if (enPausaDeImpacto && !MenuPausa.JuegoCongelado) Time.timeScale = 1f;
    }

    private void Update()
    {
        ActualizarFuria();

        if (!enPausaDeImpacto || Time.unscaledTime < pausaHasta) return;

        enPausaDeImpacto = false;
        // Si en el medio se abrio el menu de pausa, el tiempo lo maneja el menu.
        if (!MenuPausa.JuegoCongelado) Time.timeScale = 1f;
    }

    // Cualquier daño a un zombi, mate o no: el numero, unas chispas y un tic.
    // Un critico se tiene que notar: numero rojo, mas grande y con "!", el doble de
    // chispas y el golpe mas agudo.
    public static void Golpe(Vector3 punto, int daño, bool critico = false)
    {
        var e = instance;
        if (e == null) return;

        e.Emitir(punto, critico ? e.chispasPorGolpe * 2 : e.chispasPorGolpe);
        e.MostrarNumero(punto, daño, critico);
        if (critico) Sonidos.Tocar(e.golpe, 0.5f, 1.35f, 0.1f, 0.04f);
        else Sonidos.Tocar(e.golpe, 0.35f, 1f, 0.15f, 0.05f);
    }

    // Un zombi que muere. Las muertes grandes (tanque y jefe) sacuden la camara y
    // frenan el tiempo un instante; las de los chicos apenas suman temblor, porque
    // mueren de a muchos por segundo.
    public static void Muerte(Vector3 punto, int vidaBase)
    {
        ContadorCombo.RegistrarMuerte();

        var e = instance;
        if (e == null) return;

        bool jefe = vidaBase >= e.vidaParaJefe;
        bool grande = vidaBase >= e.vidaParaMuerteGrande;

        e.Emitir(punto, grande ? e.chispasPorMuerteGrande : e.chispasPorMuerte);
        Sonidos.Tocar(e.muerte, grande ? 1f : 0.55f, grande ? 0.7f : 1f, 0.2f, 0.04f);
        CamaraJugador.Temblar(jefe ? e.temblorJefe : grande ? e.temblorMuerteGrande : e.temblorMuerte);
        if (jefe) e.PausaDeImpacto(e.pausaJefe);
        else if (grande) e.PausaDeImpacto(e.pausaMuerteGrande);
    }

    public static void Explosion(Vector3 punto)
    {
        var e = instance;
        if (e == null) return;

        e.Emitir(punto, e.chispasPorExplosion);
        Sonidos.Tocar(e.explosion, 1f);
        CamaraJugador.Temblar(e.temblorExplosion);
        e.PausaDeImpacto(e.pausaExplosion);
    }

    public static void DanioJugador()
    {
        var e = instance;
        if (e == null) return;

        // Rodeado, lo golpean varios zombis por segundo: sin separacion, el
        // temblor y el borde rojo quedaban prendidos todo el tiempo.
        if (Time.unscaledTime < e.proximoDanioVisual) return;
        e.proximoDanioVisual = Time.unscaledTime + e.separacionDanioVisual;

        CamaraJugador.Temblar(e.temblorDanio);
        VinetaDanio.Pulso();
        Sonidos.Tocar(e.danioJugador, 0.8f, 1f, 0.1f, 0.12f);
    }

    public static void Caja(Vector3 punto)
    {
        var e = instance;
        if (e == null) return;

        e.Emitir(punto, e.chispasPorCaja);
        Sonidos.Tocar(e.caja, 0.9f, 1f, 0.1f, 0.05f);
    }

    public static void Disparo(Vector3 punto)
    {
        var e = instance;
        if (e == null) return;

        e.Emitir(punto, e.chispasPorDisparo);
    }

    // La horda festejando que el jugador murio (ver el festejo en EnemyController): el
    // estruendo bien grave, como el rugido del jefe, a dos voces para que suene a muchos.
    public static void FestejoZombis()
    {
        var e = instance;
        if (e == null || e.explosion == null) return;

        Sonidos.Tocar(e.explosion, 0.5f, 0.5f, 0.05f, 0f);
        Sonidos.Tocar(e.explosion, 0.35f, 0.64f, 0.05f, 0f);
    }

    public static void CartelOleada()
    {
        var e = instance;
        if (e == null) return;

        Sonidos.Tocar(e.cartelOleada, 0.8f);
    }

    // Arranca la furia del jugador: estruendo agudo, chispas, temblor y un instante
    // de camara lenta. Mientras dura, la musica va mas aguda y el borde rojo late.
    public static void EmpezarFuria(Vector3 punto, float duracion)
    {
        var e = instance;
        if (e == null) return;

        e.Emitir(punto, e.chispasPorExplosion);
        Sonidos.Tocar(e.cartelOleada, 1f, 1.5f);
        Sonidos.Tocar(e.explosion, 0.7f, 1.4f);
        CamaraJugador.Temblar(e.temblorFuria);
        e.PausaDeImpacto(e.pausaFuria);
        e.furiaHasta = Time.time + Mathf.Max(0f, duracion);
        e.proximoPulsoFuria = 0f;
        if (e.musica != null) e.musica.pitch = e.tonoMusicaFuria;
    }

    public static void TerminarFuria()
    {
        if (instance != null) instance.ApagarFuria();
    }

    // Con tiempo escalado, para terminar junto con la furia del jugador; los
    // latidos del borde, sin escalar, como la vineta.
    private void ActualizarFuria()
    {
        if (furiaHasta < 0f) return;
        if (Time.time >= furiaHasta)
        {
            ApagarFuria();
            return;
        }
        // JuegoCongelado y no Pausado: detras del ¡HAS MUERTO! el borde seguia latiendo.
        if (!MenuPausa.JuegoCongelado && Time.unscaledTime >= proximoPulsoFuria)
        {
            proximoPulsoFuria = Time.unscaledTime + intervaloPulsoFuria;
            VinetaDanio.Pulso(pulsoVinetaFuria);
        }
    }

    private void ApagarFuria()
    {
        furiaHasta = -1f;
        if (musica != null) musica.pitch = 1f;
    }

    private void Emitir(Vector3 punto, int cantidad)
    {
        if (chispas == null || cantidad <= 0) return;

        var parametros = new ParticleSystem.EmitParams { position = punto, applyShapeToPosition = true };
        chispas.Emit(parametros, cantidad);
    }

    // Las particulas que suelta cada tipo de zombi al morir (Particulas/Explosion*).
    // Antes eran un Instantiate por muerte que se borraba solo al terminar; ahora
    // cada prefab tiene una copia por escena, pasada a espacio mundo, y cada muerte
    // emite ahi la rafaga del prefab, como las chispas.
    private struct CopiaDeMuerte
    {
        public ParticleSystem sistema;   // null: el prefab no se puede emitir asi y se instancia como antes
        public int rafaga;
    }

    private readonly Dictionary<ParticleSystem, CopiaDeMuerte> copiasDeMuerte = new Dictionary<ParticleSystem, CopiaDeMuerte>();

    public static void ParticulasDeMuerte(ParticleSystem prefab, Vector3 punto)
    {
        if (prefab == null) return;

        // Sin Efectos en la escena, o con un prefab que no es una sola rafaga, como antes.
        var e = instance;
        CopiaDeMuerte copia;
        if (e == null || !e.CopiaParaMuerte(prefab, out copia))
        {
            Instantiate(prefab, punto, Quaternion.identity);
            return;
        }

        // En el editor, un Emit sobre un sistema detenido no dejaba particulas. La
        // copia no emite sola (su emision esta apagada), asi que Play no tira nada.
        if (!copia.sistema.isPlaying) copia.sistema.Play();
        var parametros = new ParticleSystem.EmitParams { position = punto, applyShapeToPosition = true };
        copia.sistema.Emit(parametros, copia.rafaga);
    }

    private bool CopiaParaMuerte(ParticleSystem prefab, out CopiaDeMuerte copia)
    {
        if (!copiasDeMuerte.TryGetValue(prefab, out copia))
        {
            copia = CrearCopiaDeMuerte(prefab);
            copiasDeMuerte[prefab] = copia;   // tambien si no sirve: no se vuelve a revisar en cada muerte
        }
        return copia.sistema != null;
    }

    private CopiaDeMuerte CrearCopiaDeMuerte(ParticleSystem prefab)
    {
        var copia = new CopiaDeMuerte();
        var main = prefab.main;
        var emision = prefab.emission;
        Vector3 escala = prefab.transform.localScale;

        // Lo que se comprobo que se ve igual emitido a mano en espacio mundo: un solo
        // sistema en espacio local, con escala local y pareja, sin loop, que tira todo
        // en rafagas fijas al arrancar. Los modulos que mueven particulas en espacio
        // local (salvo el limite de velocidad, que se corrige abajo) no se probaron.
        bool sirve = !main.loop && main.simulationSpace == ParticleSystemSimulationSpace.Local &&
                     main.scalingMode == ParticleSystemScalingMode.Local &&
                     Mathf.Approximately(escala.x, escala.y) && Mathf.Approximately(escala.x, escala.z) &&
                     prefab.GetComponentsInChildren<ParticleSystem>(true).Length == 1 &&
                     emision.rateOverTimeMultiplier <= 0f && emision.rateOverDistanceMultiplier <= 0f && emision.burstCount > 0 &&
                     main.gravityModifierMultiplier == 0f && !prefab.velocityOverLifetime.enabled &&
                     !prefab.forceOverLifetime.enabled && !prefab.noise.enabled && !prefab.collision.enabled &&
                     !prefab.subEmitters.enabled && !prefab.trails.enabled && !prefab.inheritVelocity.enabled &&
                     !(prefab.limitVelocityOverLifetime.enabled && (prefab.limitVelocityOverLifetime.separateAxes || prefab.limitVelocityOverLifetime.dragMultiplier != 0f));
        for (int i = 0; sirve && i < emision.burstCount; i++)
        {
            var burst = emision.GetBurst(i);
            sirve = burst.time <= 0f && burst.cycleCount == 1 && burst.probability >= 1f && burst.count.mode == ParticleSystemCurveMode.Constant;
            copia.rafaga += Mathf.RoundToInt(burst.count.constant);
        }
        if (!sirve || copia.rafaga <= 0)
        {
            copia.rafaga = 0;
            return copia;
        }

        var sistema = Instantiate(prefab, transform);
        sistema.name = prefab.name + " (compartida)";

        var mainCopia = sistema.main;
        mainCopia.playOnAwake = false;
        mainCopia.stopAction = ParticleSystemStopAction.None;   // el prefab se destruye al terminar
        mainCopia.simulationSpace = ParticleSystemSimulationSpace.World;   // cada rafaga queda donde murio su zombi
        mainCopia.maxParticles = Mathf.Max(main.maxParticles, copia.rafaga * 40);   // ahora las rafagas se suman

        var emisionCopia = sistema.emission;
        emisionCopia.enabled = false;   // la rafaga la pone Emit; Play no tira la del prefab

        // En espacio local el limite de velocidad se compara en unidades del prefab, que
        // esta escalado (de 0,1 a 1); en espacio mundo, sin escalarlo, las particulas de
        // los prefabs chicos no frenaban y llegaban un 4 % mas lejos.
        var limite = sistema.limitVelocityOverLifetime;
        if (limite.enabled) limite.limitMultiplier = prefab.limitVelocityOverLifetime.limitMultiplier * escala.x;

        sistema.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        copia.sistema = sistema;
        return copia;
    }

    private void PausaDeImpacto(float segundos)
    {
        if (MenuPausa.JuegoCongelado || segundos <= 0f) return;

        pausaHasta = Mathf.Max(pausaHasta, Time.unscaledTime + segundos);
        enPausaDeImpacto = true;
        Time.timeScale = escalaDeTiempoEnPausa;
    }

    // Los numeros salen de un pool con techo: si ya hay maxNumeros en pantalla,
    // el golpe nuevo no muestra numero en vez de crear otro.
    private void MostrarNumero(Vector3 punto, int valor, bool critico)
    {
        if (numeroPrefab == null) return;

        NumeroFlotante numero = numerosLibres.Count > 0 ? numerosLibres.Pop() : null;
        if (numero == null)
        {
            if (numerosCreados >= maxNumeros) return;
            numerosCreados++;
            numero = Instantiate(numeroPrefab, transform);
            numero.alTerminar = numerosLibres.Push;
        }
        numero.Mostrar(punto, valor, critico);
    }
}
