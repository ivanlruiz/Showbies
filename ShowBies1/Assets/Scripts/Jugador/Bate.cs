using System.Collections.Generic;
using UnityEngine;

// El bate (1.7.0; lo pregunto Ivan el 12/10, con Nex Machina de referencia, y lo diseño Gemini, ver
// PLAN.md 4d): un golpe cuerpo a cuerpo para sacarse la horda de encima cuando acorrala, no una
// fuente de daño. Se compra una sola vez en la tienda, como la granada y la furia, y se usa con su
// boton del HUD (BotonBate) o, en PC, con el clic derecho o Shift. Nada de golpe automatico de
// cerca: le sacaria al jugador la decision de cuando usar su salvavidas.
//
// El golpe pega en un cono de 120° y 2,5 m delante del muñeco, con el daño de la bala por 3 (crece
// con la mejora de daño y las cartas, sin la furia), y despues cada zombi reacciona segun su peso
// (EnemyController.RecibirBatazo): el normal, el rapido y el veloz salen despedidos 3 m, el tanque
// se queda quieto 1 s y al jefe no le hace nada. Despues hay que esperar 4 s.
//
// El muñeco saca el bate que traia el modelo (ArmaEnLaMano lo esconde al empezar) y tira el hachazo
// del pack (m_melee_combat_attack_A) de la cintura para arriba, en la capa "Bate" del Animator: las
// piernas siguen corriendo. Mientras dura no dispara (la pistola esta escondida). El daño entra
// cuando el bate llega adelante, no al apretar, como el zarpazo de los zombis.
//
// Va en Jugador.prefab, como la furia, y se entera de la compra en su Awake. Usa tiempo escalado:
// la pausa lo congela. Corre despues de PlayerJS y PlayerController, que giran al muñeco en su
// Update: el frente del golpe es hacia donde mira en ese cuadro.
[DisallowMultipleComponent]
[DefaultExecutionOrder(20)]
public class Bate : MonoBehaviour
{
    public PlayerController jugador;

    [Header("Balance (de Gemini)")]
    public float multiplicadorDano = 3f;
    public float alcance = 2.5f;
    public float angulo = 120f;
    public float empuje = 3f;
    public float duracionDelEmpuje = 0.3f;
    public float frenadoDelPesado = 1f;
    public float enfriamiento = 4f;

    // El que esta pegado al jugador entra aunque este a un costado: el que te pisa los pies no se
    // esquiva con el angulo.
    public const float RadioQueEntraSiempre = 0.8f;

    // La animacion, medida del clip (m_melee_combat_attack_A, 1 s): sube el bate hasta los 0,27 s
    // y la punta llega adelante y abajo a los 0,33 s. Arranca pasado el reposo del principio y va a
    // x1,25, asi el golpe conecta 0,19 s despues de apretar: un veloz a dos metros llega a pegar
    // antes de que el bate baje si va mas lento. Vuelve a la pistola pasado el golpe (del 0,55 en
    // adelante el clip es la guardia de pelea, que no es la de la pistola). Los usa tambien
    // ConstructorAnimaciones, que arma la capa.
    public const string CapaDelBate = "Bate";
    public const string GatilloDelBate = "batazo";
    public const float InicioDelClip = 0.1f;
    public const float ImpactoEnElClip = 0.333f;
    public const float SalidaDelClip = 0.55f;
    public const float VelocidadDelClip = 1.25f;
    public const float VueltaALaPistola = 0.15f;
    public static float DemoraDelImpacto { get { return (ImpactoEnElClip - InicioDelClip) / VelocidadDelClip; } }
    // Lo que dura con el bate en la mano y sin disparar: hasta que empieza a volver a la pistola.
    public static float DuracionDelGolpe { get { return (SalidaDelClip - InicioDelClip) / VelocidadDelClip; } }

    public static readonly Color ColorDelBate = new Color(1f, 0.32f, 0.78f);

    // El del jugador de la escena, para el boton del HUD.
    public static Bate Instancia { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetearEstadoCompartido()
    {
        Instancia = null;
    }

    public bool Desbloqueado { get; private set; }

    // Para el boton y las pruebas.
    public int Batazos { get; private set; }
    public int Impactos { get; private set; }
    public int UltimosTocados { get; private set; }
    public int UltimosEmpujados { get; private set; }
    public int UltimosFrenados { get; private set; }
    public Vector3 UltimoFrente { get; private set; }

    private float golpeadoEn = float.NegativeInfinity;
    private bool impactoPendiente;
    private bool enLaMano;
    private Animator animador;
    private int capa = -1;
    private int idGatillo;
    private ArmaEnLaMano armaEnLaMano;
    private readonly List<EnemyController> tocados = new List<EnemyController>();

    // El arco en el piso: la zona que barrio el golpe, un instante. La punta del bate llega a 0,75 m
    // y el golpe a 2,5: sin esto no se lee hasta donde pego.
    private LineRenderer arco;
    private float arcoDesde = -1f;
    private const float DuracionDelArco = 0.22f;
    private const int PuntosDelArco = 24;

    public float RestanteEnfriamiento
    {
        get { return Desbloqueado ? Furia.Restante(Time.time, golpeadoEn, enfriamiento) : 0f; }
    }

    // Con el bate en la mano: no se dispara.
    public bool EnCurso
    {
        get { return Desbloqueado && Time.time < golpeadoEn + DuracionDelGolpe; }
    }

    public bool Lista
    {
        get
        {
            if (!Desbloqueado || MenuPausa.JuegoCongelado || RestanteEnfriamiento > 0f) return false;
            if (PlayerHealth.instance != null && PlayerHealth.instance.EstaMuerto) return false;
            return jugador == null || !jugador.EnElAire;
        }
    }

    private void Awake()
    {
        Instancia = this;
        if (jugador == null) jugador = GetComponent<PlayerController>();
        Desbloqueado = CatalogoMejoras.BateDesbloqueado;
        idGatillo = Animator.StringToHash(GatilloDelBate);
    }

    private void Start()
    {
        if (!Desbloqueado) return;
        armaEnLaMano = GetComponent<ArmaEnLaMano>();
        if (jugador != null && jugador.trans != null) animador = jugador.trans.anim;
        if (animador == null) animador = GetComponentInChildren<Animator>();
        if (animador != null) capa = animador.GetLayerIndex(CapaDelBate);
        if (capa < 0) Debug.LogWarning("Bate: el controller del jugador no tiene la capa \"" + CapaDelBate
            + "\" (ShowBies > Animaciones > Armar el controller del jugador). Pega igual, sin el golpe.", this);
        ArmarElArco();
        BotonBate.Instalar(this);
    }

    private void OnDestroy()
    {
        if (Instancia == this) Instancia = null;
    }

    private void Update()
    {
        if (!Desbloqueado) return;

        // Muerto a mitad del golpe: el desplome del cuerpo va en la capa de abajo, y la del bate
        // seguiria tirando el hachazo encima.
        bool muerto = PlayerHealth.instance != null && PlayerHealth.instance.EstaMuerto;
        if (muerto && (impactoPendiente || enLaMano)) Cortar();

        // En el telefono lo activa el boton del HUD. Como todo lo que lee input, se corta en la pausa.
        if (!Plataforma.EsMovil && !MenuPausa.JuegoCongelado && QuierePegarEnPC()) Golpear();

        if (impactoPendiente && Time.time >= golpeadoEn + DemoraDelImpacto)
        {
            impactoPendiente = false;
            Impactar();
        }
        if (enLaMano && !EnCurso) Guardar();
        ActualizarElArco();
    }

    private static bool QuierePegarEnPC()
    {
        return Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift);
    }

    // Devuelve si arranco: no pasa nada sin comprarlo, en pausa, muerto, volando o enfriando.
    public bool Golpear()
    {
        if (!Lista) return false;

        golpeadoEn = Time.time;
        impactoPendiente = true;
        Batazos++;
        enLaMano = true;
        if (armaEnLaMano != null) armaEnLaMano.MostrarBate(true);
        if (jugador != null) jugador.FijarDisparo(false);
        if (animador != null && capa >= 0) animador.SetTrigger(idGatillo);
        Efectos.SilbidoDelBate();
        return true;
    }

    private void Impactar()
    {
        Impactos++;
        Vector3 centro = transform.position;
        Vector3 frente = transform.forward;
        frente.y = 0f;
        if (frente.sqrMagnitude < 1e-6f) frente = Vector3.forward;
        frente.Normalize();
        UltimoFrente = frente;

        float dano = multiplicadorDano * (jugador != null && jugador.theGun != null ? jugador.theGun.DanoSinFuria : 1f);

        // Primero se juntan y despues se les pega: el que muere sale de EnemyController.Vivos.
        tocados.Clear();
        var vivos = EnemyController.Vivos;
        for (int i = 0; i < vivos.Count; i++)
        {
            var zombi = vivos[i];
            if (zombi != null && zombi.Vivo && EnElCono(centro, frente, zombi.transform.position, alcance + zombi.RadioDeSuCuerpo, angulo))
                tocados.Add(zombi);
        }

        int empujados = 0, frenados = 0;
        for (int i = 0; i < tocados.Count; i++)
        {
            var zombi = tocados[i];
            Vector3 hacia = zombi.transform.position - centro;
            hacia.y = 0f;
            hacia = hacia.sqrMagnitude > 1e-4f ? hacia.normalized : frente;
            // El cadaver del que muere sale para alla (DanoZombi); al que queda, lo empuja su peso.
            zombi.DanoZombi(dano, false, hacia);
            if (!zombi.Vivo) continue;
            var reaccion = zombi.RecibirBatazo(hacia, empuje, duracionDelEmpuje, frenadoDelPesado);
            if (reaccion == ReaccionAlBatazo.Empujado) empujados++;
            else if (reaccion == ReaccionAlBatazo.Frenado) frenados++;
        }
        UltimosTocados = tocados.Count;
        UltimosEmpujados = empujados;
        UltimosFrenados = frenados;

        MostrarElArco(centro, frente);
        Efectos.GolpeDelBate(centro + frente * (alcance * 0.5f), tocados.Count);
        tocados.Clear();
    }

    // Si un punto cae en el cono de 'angulo' grados y 'alcance' metros que sale de 'centro' hacia
    // 'frente', medido en el piso. Lo pegado al centro entra siempre. Estatica para probarla sin
    // escena.
    public static bool EnElCono(Vector3 centro, Vector3 frente, Vector3 punto, float alcance, float angulo)
    {
        Vector3 hacia = punto - centro;
        hacia.y = 0f;
        float distancia = hacia.magnitude;
        if (distancia > alcance) return false;
        if (distancia <= RadioQueEntraSiempre) return true;
        frente.y = 0f;
        if (frente.sqrMagnitude < 1e-6f) return false;
        return Vector3.Angle(frente, hacia) <= angulo * 0.5f;
    }

    // Termina el golpe: vuelve la pistola.
    private void Guardar()
    {
        enLaMano = false;
        if (armaEnLaMano != null) armaEnLaMano.MostrarBate(false);
    }

    // Al morir a mitad del golpe: sin impacto y de vuelta a la pistola. El desplome ya va en tiempo
    // sin escalar (PlayerController.Caer).
    private void Cortar()
    {
        impactoPendiente = false;
        golpeadoEn = Mathf.Min(golpeadoEn, Time.time - DuracionDelGolpe);
        Guardar();
        if (animador != null && capa >= 0)
        {
            animador.ResetTrigger(idGatillo);
            animador.CrossFadeInFixedTime("Nada", 0.1f, capa);
        }
    }

    private void ArmarElArco()
    {
        if (jugador == null || jugador.granadaPrefab == null || jugador.granadaPrefab.Indicador == null) return;
        // El anillo de la granada (el material de sprites, acostado en el piso), con otra forma.
        arco = Instantiate(jugador.granadaPrefab.Indicador);
        arco.name = "ArcoDelBate";
        arco.useWorldSpace = true;
        arco.loop = false;
        arco.positionCount = PuntosDelArco;
        // Media luna: fina en las puntas y gruesa en el medio.
        arco.widthCurve = new AnimationCurve(new Keyframe(0f, 0.05f), new Keyframe(0.5f, 1f), new Keyframe(1f, 0.05f));
        arco.widthMultiplier = 0.55f;
        arco.gameObject.SetActive(false);
    }

    private void MostrarElArco(Vector3 centro, Vector3 frente)
    {
        if (arco == null) return;
        float radio = alcance * 0.8f;
        float giro = Mathf.Atan2(frente.x, frente.z);
        float mitad = angulo * 0.5f * Mathf.Deg2Rad;
        for (int i = 0; i < PuntosDelArco; i++)
        {
            float a = giro + Mathf.Lerp(-mitad, mitad, i / (float)(PuntosDelArco - 1));
            arco.SetPosition(i, new Vector3(centro.x + Mathf.Sin(a) * radio, Granade.AlturaDelAnillo, centro.z + Mathf.Cos(a) * radio));
        }
        arcoDesde = Time.time;
        PintarElArco(1f);
        arco.gameObject.SetActive(true);
    }

    private void ActualizarElArco()
    {
        if (arco == null || arcoDesde < 0f) return;
        float t = (Time.time - arcoDesde) / DuracionDelArco;
        if (t >= 1f)
        {
            arcoDesde = -1f;
            arco.gameObject.SetActive(false);
            return;
        }
        PintarElArco(1f - t);
        // Se abre un poco mientras se apaga.
        arco.widthMultiplier = 0.55f * (1f + 0.4f * t);
    }

    private void PintarElArco(float alfa)
    {
        Color color = Color.Lerp(Color.white, ColorDelBate, 0.5f);
        color.a = 0.85f * alfa;
        arco.startColor = color;
        arco.endColor = color;
    }

    // Las pruebas: el arco esta prendido.
    public bool ArcoPrendido { get { return arco != null && arco.gameObject.activeSelf; } }
}
