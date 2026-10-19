using UnityEngine;

// El escupidor (1.7.5, ZOMBIS.md; lo diseño Argon y a Ivan le gusto de la ronda de ideas del 18/10): artilleria
// que niega zonas. Camina hasta quedar a 9,5 m del jugador y se planta, afuera de los 7,5 m del auto-apuntado: hay
// que apuntarle a mano o ir a buscarlo. Cada 3,5 s se frena, infla los sacos de acido de los hombros 0,65 s mientras
// marca en el piso un anillo donde esta el jugador, y le tira un globo en arco que deja un charco de acido 4 s
// (AcidoDelEscupidor). Si el jugador se le acerca a menos de 4,5 m, retrocede despacio. Castiga quedarse quieto.
//
// Se mueve por su cuenta (IMovimientoPropio) solo cuando ya llego: mientras se acerca, devuelve falso y lo lleva la
// persecucion de siempre, con el rodeo de los edificios. Solo escupe si se lo ve en la pantalla, como el jefe ataca:
// desde afuera de cuadro seria un charco que cae de la nada.
//
// El bate lo empuja (es liviano) y, si le pega mientras infla los sacos, le corta el escupitajo. Congelado, tarda 5 s
// entre escupitajo y escupitajo en vez de 3,5.
//
// En el modo libre no hay final de oleada que lo acerque: escupe tres veces y despues se planta a 6,5 m, adentro del
// auto-apuntado (de Argon, 19/10). Si no, a los dos minutos habia tres clavados en los bordes que el automatico no mata
// nunca, ocupando tres de los ocho lugares de los especiales.
//
// Se lee por los dos sacos amarillos de los hombros, que laten y se inflan antes de tirar, por la piel amarillo acido y
// por ir inclinado hacia adelante. Los sacos los pone ConstructorZombis en el prefab; la inclinacion va aca, en el
// LateUpdate, sobre el hijo del modelo, como las poses del jefe.
//
// Sin RequireComponent: FondoMenu le borra los scripts a los zombis (ver la trampa en CLAUDE.md).
public class ZombiEscupidor : MonoBehaviour, IMovimientoPropio
{
    [Header("Distancias (de Argon)")]
    public float distanciaParaPlantarse = 9.5f;
    public float distanciaAlFinal = 6.5f;        // al final de la oleada: adentro de los 7,5 m del auto-apuntado
    public int pocosAlFinal = 3;                 // vivos de la oleada (ver WaveManager.FinalDeLaOleada)
    public int escupitajosEnElLibre = 3;         // en el modo libre, despues de estos se acerca a distanciaAlFinal
    public float distanciaParaRetroceder = 4.5f;
    public float velocidadAlRetroceder = 0.6f;   // de la suya
    public float borde = 45f;                    // del centro: las paredes invisibles estan en 49

    [Header("El escupitajo")]
    public float cadaCuanto = 3.5f;
    public float cadaCuantoCongelado = 5f;
    public float primeroAlPlantarse = 1f;        // el primero, un poco despues de llegar
    public float aviso = 0.65f;
    public float velocidadDelEscupitajo = 11f;
    public float alturaDelArco = 1.6f;
    public float radioDelCharco = 1.1f;          // 2,2 m de diametro
    public float duracionDelCharco = 4f;
    public float graciaDelCharco = 0.25f;
    public float cadaCuantoPegaElCharco = 0.5f;
    public float parteDelGolpe = 0.5f;           // del golpe de un normal

    [Header("Lo que se ve")]
    public float inclinacion = 20f;
    public float infladoAlAvisar = 0.4f;
    public Material materialAviso;               // el de la linea del jefe (Sprites/Default)
    public Material materialCharco;              // ShowBies/CharcoDeLuz, acido
    public Material materialAcido;               // Unlit, amarillo: el globo
    public Color colorAcido = new Color(0.83f, 1f, 0f);
    public Transform[] sacos;                    // los pone ConstructorZombis

    public enum Estado { Acercandose, Rodeando, Plantado, Avisando }

    // Si a la distancia de plantarse no se lo ve (al norte o al sur del jugador, que en la pantalla apaisada es lo mas
    // corto: al norte se plantaba a 9 m debajo del boton de pausa, y con el margen, a 5,9 m, adentro del auto-apuntado),
    // rodea al jugador hacia los costados, donde la pantalla es mas ancha, hasta que se lo vea. Si en este rato no lo
    // logra (un borde del mapa, un edificio), se acerca.
    public const float TopeDelRodeo = 3f;

    public Estado EstadoActual { get; private set; }
    public Vector3 Objetivo { get; private set; }
    public int Escupitajos { get; private set; }
    public int Cortados { get; private set; }
    public bool AnilloPrendido { get { return anillo != null && anillo.enabled; } }

    private EnemyController zombi;
    private Transform modelo;
    private Quaternion giroBaseDelModelo;
    private Vector3[] escalaDeLosSacos;
    private LineRenderer anillo;
    private float avisoDesde;
    private float proximoEscupitajo = -1f;
    private float inclinado;   // de 0 a 1: se suaviza
    private float rodeandoDesde = -1f;
    private int escupitajosDeEstaAparicion;   // los que tiro desde que aparecio: Escupitajos no vuelve a cero

    // Lo que hace a cada distancia, sin contar el escupitajo. Estatico para probarlo sin escena: lejos se acerca (lo
    // lleva la persecucion), cerca retrocede, y en el medio se queda. El medio metro de mas es para que no tiemble entre
    // acercarse y plantarse justo en el borde.
    public static Estado QueHacer(float distancia, Estado ahora, float plantarse)
    {
        return SePlanta(distancia, ahora == Estado.Plantado, plantarse) ? Estado.Plantado : Estado.Acercandose;
    }

    // A que distancia se planta: lejos (9,5 m), salvo al final de la oleada o, en el modo libre, cuando ya escupio sus tres
    // veces, que se acerca (6,5 m). Estatico para probarlo sin escena.
    public static float DistanciaParaPlantarse(bool finalDeLaOleada, bool enElLibre, int escupitajos, int antesDeAcercarse,
                                               float lejos, float cerca)
    {
        return finalDeLaOleada || (enElLibre && escupitajos >= antesDeAcercarse) ? cerca : lejos;
    }

    // Lo mismo para cualquiera que se planta a una distancia (tambien el chaman).
    public static bool SePlanta(float distancia, bool plantado, float plantarse)
    {
        return plantado ? distancia <= plantarse + 0.5f : distancia <= plantarse;
    }

    private void Awake()
    {
        zombi = GetComponent<EnemyController>();
        foreach (var animador in GetComponentsInChildren<Animator>(true))
        {
            if (animador.transform == transform || animador.runtimeAnimatorController == null) continue;
            modelo = animador.transform;
            break;
        }
        if (modelo != null) giroBaseDelModelo = modelo.localRotation;
        if (sacos != null)
        {
            escalaDeLosSacos = new Vector3[sacos.Length];
            for (int i = 0; i < sacos.Length; i++) if (sacos[i] != null) escalaDeLosSacos[i] = sacos[i].localScale;
        }
        ArmarElAnillo();
    }

    private void OnEnable()
    {
        EstadoActual = Estado.Acercandose;
        rodeandoDesde = -1f;
        proximoEscupitajo = -1f;
        escupitajosDeEstaAparicion = 0;
        inclinado = 0f;
        if (anillo != null) anillo.enabled = false;
    }

    private void OnDisable()
    {
        if (anillo != null) anillo.enabled = false;
    }

    private void OnDestroy()
    {
        if (anillo != null) Destroy(anillo.gameObject);
    }

    public bool Mover(Rigidbody rb, Transform jugador)
    {
        if (zombi == null || !zombi.Vivo || jugador == null) return false;
        Vector3 hacia = jugador.position - transform.position;
        hacia.y = 0f;
        float distancia = hacia.magnitude;
        float ahora = Time.time;

        if (EstadoActual == Estado.Avisando)
        {
            Quieto(rb, hacia);
            if (ahora - avisoDesde >= aviso) Escupir(ahora);
            return true;
        }

        // Al final de la oleada (ya salieron todos y quedan pocos, o solo escupidores) se acerca al alcance del
        // auto-apuntado, de Argon (18/10): con el automatico solo, la oleada no terminaba hasta salir a cazarlo a mano. En
        // el modo libre, despues de sus tres escupitajos (19/10).
        float plantarse = DistanciaParaPlantarse(WaveManager.FinalDeLaOleada(pocosAlFinal), GeneradorZombis.EnElLibre,
                                                 escupitajosDeEstaAparicion, escupitajosEnElLibre, distanciaParaPlantarse, distanciaAlFinal);
        var siguiente = QueHacer(distancia, EstadoActual, plantarse);
        // Desde afuera de la pantalla no se planta: rodea hacia los costados (TopeDelRodeo), y si no, se acerca.
        if (siguiente == Estado.Plantado && !SeVe())
        {
            if (rodeandoDesde < 0f) rodeandoDesde = ahora;
            if (ahora - rodeandoDesde < TopeDelRodeo)
            {
                EstadoActual = Estado.Rodeando;
                Rodear(rb, hacia, distancia, plantarse);
                return true;
            }
            siguiente = Estado.Acercandose;
        }
        if (siguiente == Estado.Acercandose)
        {
            EstadoActual = Estado.Acercandose;
            return false;
        }
        rodeandoDesde = -1f;
        if (EstadoActual != Estado.Plantado)
        {
            EstadoActual = Estado.Plantado;
            if (proximoEscupitajo < ahora) proximoEscupitajo = ahora + primeroAlPlantarse;
        }

        if (distancia < distanciaParaRetroceder) Retroceder(rb, hacia);
        else Quieto(rb, hacia);

        if (ahora >= proximoEscupitajo) EmpezarAviso(jugador.position, ahora);
        return true;
    }

    private void Quieto(Rigidbody rb, Vector3 hacia)
    {
        if (hacia.sqrMagnitude > 1e-4f) transform.rotation = Quaternion.LookRotation(hacia);
        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
    }

    // De costado alrededor del jugador, hacia su misma linea este-oeste, sin dejar la distancia de plantarse, y mirandolo.
    private void Rodear(Rigidbody rb, Vector3 hacia, float distancia, float plantarse)
    {
        if (hacia.sqrMagnitude < 1e-4f) return;
        Vector3 rumbo = RumboDelRodeo(transform.position, hacia, distancia, plantarse, borde);
        transform.rotation = Quaternion.LookRotation(hacia);
        Vector3 v = rumbo * (zombi.enemyType.velocidad * (1f - zombi.LentitudActual));
        v.y = rb.linearVelocity.y;
        rb.linearVelocity = v;
    }

    // Hacia donde rodea: de costado, hacia la linea este-oeste del jugador (donde la pantalla apaisada es mas ancha), sin
    // salirse del mapa y corrigiendo de a poco la distancia. Estatico: lo usa tambien el chaman.
    public static Vector3 RumboDelRodeo(Vector3 posicion, Vector3 hacia, float distancia, float plantarse, float borde)
    {
        Vector3 alJugador = hacia.normalized;
        Vector3 costado = Vector3.Cross(Vector3.up, alJugador);
        // Desde el jugador, el zombi esta en -hacia: hacia los costados es achicar esa diferencia norte-sur.
        if (costado.z * -hacia.z > 0f) costado = -costado;
        Vector3 adelante = posicion + costado * 2f;
        if (Mathf.Abs(adelante.x) > borde || Mathf.Abs(adelante.z) > borde) costado = -costado;
        float corregir = Mathf.Clamp((distancia - plantarse) * 0.5f, -0.5f, 0.5f);
        return (costado + alJugador * corregir).normalized;
    }

    // Para atras, mirando al jugador. Contra el borde del mapa, de costado hacia adentro.
    private void Retroceder(Rigidbody rb, Vector3 hacia)
    {
        if (hacia.sqrMagnitude > 1e-4f) transform.rotation = Quaternion.LookRotation(hacia);
        Vector3 atras = hacia.sqrMagnitude > 1e-4f ? -hacia.normalized : -transform.forward;
        Vector3 adelante = transform.position + atras * 2f;
        if (Mathf.Abs(adelante.x) > borde || Mathf.Abs(adelante.z) > borde)
        {
            Vector3 costado = Vector3.Cross(Vector3.up, atras);
            if (Vector3.Dot(costado, -transform.position) < 0f) costado = -costado;
            atras = costado;
        }
        Vector3 v = atras * (zombi.enemyType.velocidad * velocidadAlRetroceder * (1f - zombi.LentitudActual));
        v.y = rb.linearVelocity.y;
        rb.linearVelocity = v;
    }

    private void EmpezarAviso(Vector3 donde, float ahora)
    {
        EstadoActual = Estado.Avisando;
        avisoDesde = ahora;
        donde.y = 0f;
        Objetivo = donde;
        if (anillo != null)
        {
            Granade.DibujarAnillo(anillo, donde, radioDelCharco);
            anillo.enabled = true;
        }
    }

    private void Escupir(float ahora)
    {
        EstadoActual = Estado.Plantado;
        // "Cada 3,5 s" es de escupitajo a escupitajo, con el aviso adentro.
        proximoEscupitajo = ahora + Mathf.Max(0.1f, (zombi.Congelado ? cadaCuantoCongelado : cadaCuanto) - aviso);
        if (anillo != null) anillo.enabled = false;
        Escupitajos++;
        escupitajosDeEstaAparicion++;
        Vector3 boca = transform.position + Vector3.up * (zombi.AlturaDeLaCabeza * 0.8f) + transform.forward * 0.3f;
        var charco = new AcidoDelEscupidor.Charco
        {
            radio = radioDelCharco,
            duracion = duracionDelCharco,
            gracia = graciaDelCharco,
            cadaCuanto = cadaCuantoPegaElCharco,
            dano = zombi.DanoPorGolpe * parteDelGolpe,
            color = colorAcido,
        };
        AcidoDelEscupidor.Lanzar(boca, Objetivo, velocidadDelEscupitajo, alturaDelArco, charco, materialAcido, materialCharco, materialAviso);
        Efectos.Escupitajo(boca, colorAcido);
    }

    // El bate le corta el escupitajo (lo empuja EnemyController antes de que corra Mover), y morirse tambien.
    private void Update()
    {
        if (EstadoActual != Estado.Avisando) return;
        // Tambien si el jugador murio: la horda festeja (Mover ya no corre) y el anillo quedaba parpadeando sobre el
        // cuerpo toda la derrota (auditoria del 18/10).
        if (zombi == null || !zombi.Vivo || zombi.Empujado || zombi.Frenado || DerrotaEnLaPartida.Activa)
        {
            EstadoActual = zombi != null && zombi.Vivo ? Estado.Plantado : Estado.Acercandose;
            proximoEscupitajo = Time.time + cadaCuanto * 0.5f;
            if (anillo != null) anillo.enabled = false;
            Cortados++;
        }
    }

    // Bien adentro de la pantalla, y lejos del borde de arriba, donde estan la pausa, el cartel de la oleada y la barra
    // del jefe: al norte se plantaba a 9 m, justo debajo del boton de pausa, y no se lo veia.
    public const float MargenAlCostado = 0.06f, MargenArriba = 0.16f, MargenAbajo = 0.08f;

    public static bool SeVeEn(Vector3 viewport)
    {
        return viewport.z > 0f && viewport.x > MargenAlCostado && viewport.x < 1f - MargenAlCostado
               && viewport.y > MargenAbajo && viewport.y < 1f - MargenArriba;
    }

    private bool SeVe()
    {
        var camara = Camera.main;
        if (camara == null) return true;
        return SeVeEn(camara.WorldToViewportPoint(transform.position + Vector3.up * (zombi.AlturaDeLaCabeza * 0.5f)));
    }

    // Despues del Animator: el torso inclinado (suave, y no en el festejo ni muerto) y los sacos, que laten y se inflan
    // en el aviso. El anillo parpadea.
    private void LateUpdate()
    {
        bool vivo = zombi != null && zombi.Vivo && !DerrotaEnLaPartida.Activa;
        float dt = Mathf.Min(Time.deltaTime, 0.1f);
        inclinado = Mathf.MoveTowards(inclinado, vivo ? 1f : 0f, dt * 4f);
        if (modelo != null && (vivo || inclinado > 0f))
            modelo.localRotation = giroBaseDelModelo * Quaternion.Euler(inclinacion * inclinado, 0f, 0f);

        float t = EstadoActual == Estado.Avisando ? Mathf.Clamp01((Time.time - avisoDesde) / Mathf.Max(0.01f, aviso)) : 0f;
        float latido = 1f + 0.08f * Mathf.Sin(Time.time * 6f);
        float escala = latido * (1f + infladoAlAvisar * t);
        if (sacos != null && escalaDeLosSacos != null)
            for (int i = 0; i < sacos.Length; i++)
                if (sacos[i] != null) sacos[i].localScale = escalaDeLosSacos[i] * escala;

        if (anillo != null && anillo.enabled && !vivo) anillo.enabled = false;
        if (anillo != null && anillo.enabled)
        {
            Color c = colorAcido;
            c.a = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(Time.time * 14f));
            anillo.startColor = anillo.endColor = c;
        }
    }

    private void ArmarElAnillo()
    {
        if (materialAviso == null) return;
        var go = new GameObject("AvisoEscupidor");
        anillo = go.AddComponent<LineRenderer>();
        anillo.material = materialAviso;
        anillo.widthMultiplier = 0.18f;
        anillo.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        anillo.receiveShadows = false;
        // Plano sobre el piso, como la linea del jefe.
        anillo.alignment = LineAlignment.TransformZ;
        go.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
        anillo.enabled = false;
    }
}
