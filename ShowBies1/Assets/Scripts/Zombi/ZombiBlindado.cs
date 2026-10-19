using UnityEngine;

// El blindado (ZOMBIS.md, de Argon; estrena en la ciudad, oleada 21): un antidisturbios con un escudo delante del pecho.
// Se pone adelante de la fila y absorbe las balas comunes: premia el bate, la perforacion y los explosivos.
//
// - El escudo: toda bala comun que le entra de frente (un arco de 90 grados adelante) pega un 80 % menos, suena a metal,
//   saca chispas blancas y no lo aplasta (EnemyController.DanoZombi pregunta Para). Bala comun es la pistola, el
//   revolver, los perdigones de la escopeta, el rifle, las de mas del disparo doble, la de hacia atras y los drones; de
//   donde viene se mide con el rumbo de la bala. Lo atraviesan la de energia, la granada, el bate, la quemadura, el
//   area de la explosiva y TODA bala si el jugador tiene una carta de perforante (de Argon, 18/10).
// - Gira despacio: 120 grados por segundo, 50 congelado (EnemyController.Perseguir). Rodearlo deja la espalda.
// - La guardia: a 1,5 m del jugador abre el escudo hacia su izquierda y deja el pecho descubierto (el escudo no para
//   nada mientras esta abierta), y recien 0,5 s despues puede tirar el zarpazo, que pega 1,4 veces. Con la guardia
//   cerrada no ataca.
// - Es pesado, como el tanque (la masa del prefab): no se aparta del jugador, el chaman se refugia detras de el, y el
//   bate lo frena 1 s y le arranca el escudo para siempre (EnemyController.RecibirBatazo): sale volando, y queda como
//   uno comun (sin escudo, sin guardia y girando derecho).
//
// No se mueve por su cuenta (no es IMovimientoPropio): lo lleva la persecucion de siempre, con el giro limitado, y por
// eso el chaman lo potencia. Las piezas (el casco, la visera y el escudo, colgado de un pivote hijo del torso) las pone
// ConstructorZombis; aca solo se mueven. Lo que se arranca no se suelta del hueso (cambiar de padre mientras el zombi se
// prende o se apaga en el pool tira un error): se mueve en el mundo desde el LateUpdate y despues se esconden sus
// renderers.
//
// Sin RequireComponent: FondoMenu le borra los scripts a los zombis (ver la trampa en CLAUDE.md).
public class ZombiBlindado : MonoBehaviour
{
    [Header("El escudo (de Argon)")]
    public float danoQuePasa = 0.2f;               // de frente le entra un 80 % menos
    public float arcoDelEscudo = 90f;
    public float giroPorSegundo = 120f;
    public float giroCongelado = 50f;
    public float golpe = 1.4f;                     // multiplica el zarpazo (EnemyController.DanoPorGolpe)

    [Header("La guardia")]
    public float distanciaDeLaGuardia = 1.5f;      // del jugador, centro a centro
    public float distanciaParaCerrar = 2.3f;
    public float aviso = 0.5f;                     // de abrir la guardia al primer zarpazo
    public float giroDeLaGuardia = -65f;           // hacia su izquierda
    public float tiempoDeAbrir = 0.12f;

    [Header("Lo que se ve")]
    public Transform pivote;                       // el escudo entero, hijo del torso
    public Transform placa;
    public Color colorAmbar = new Color(1f, 0.7f, 0f);

    public bool Roto { get; private set; }
    public bool GuardiaAbierta { get { return abiertaDesde >= 0f; } }
    public int Bloqueados { get; private set; }
    public bool EscudoALaVista { get { return renderers != null && renderers.Length > 0 && renderers[0] != null && renderers[0].enabled; } }

    // Lo que gira por segundo (EnemyController.Perseguir): sin escudo, derecho como uno comun.
    public float GiroPorSegundo
    {
        get
        {
            if (Roto || zombi == null) return float.PositiveInfinity;
            return zombi.Congelado ? giroCongelado : giroPorSegundo;
        }
    }

    // Donde salen las chispas de una bala que pega en el escudo.
    public Vector3 PuntoDelEscudo
    {
        get { return placa != null && !Roto ? placa.position : transform.position + Vector3.up; }
    }

    private const float VuelaDurante = 0.45f;
    private const float SeAchicaEn = 0.25f;

    private EnemyController zombi;
    private Renderer[] renderers;
    private Vector3 posicionBase;
    private Quaternion giroBase;
    private Vector3 escalaBase;
    private float abiertaDesde = -1f;
    private float apertura;
    private bool volando;
    private float voloDesde;
    private Vector3 posicionAlVolar;
    private Vector3 velocidadAlVolar;
    private Quaternion giroAlVolar;
    private Vector3 ejeAlVolar;

    // Si una bala que viene con el rumbo "direccionDelTiro" la para el escudo.
    public bool Para(OrigenDelDano origen, Vector3 direccionDelTiro)
    {
        if (origen != OrigenDelDano.Bala || Roto || GuardiaAbierta) return false;
        if (PoderDeLaPartida.Perforaciones > 0) return false;
        return DeFrente(transform.forward, direccionDelTiro, arcoDelEscudo);
    }

    // Si un tiro con el rumbo "direccionDelTiro" le entra a "frente" dentro del arco (en grados, entero: 90 son 45 para
    // cada lado). De donde viene es -direccionDelTiro, en el piso. Estatico para probarlo.
    public static bool DeFrente(Vector3 frente, Vector3 direccionDelTiro, float arco)
    {
        frente.y = 0f;
        Vector3 viene = -direccionDelTiro;
        viene.y = 0f;
        if (frente.sqrMagnitude < 1e-6f || viene.sqrMagnitude < 1e-6f) return false;
        return Vector3.Angle(frente, viene) <= arco * 0.5f + 1e-3f;
    }

    public void ContarBloqueado()
    {
        Bloqueados++;
    }

    private void Awake()
    {
        zombi = GetComponent<EnemyController>();
        if (pivote != null)
        {
            posicionBase = pivote.localPosition;
            giroBase = pivote.localRotation;
            escalaBase = pivote.localScale;
            renderers = pivote.GetComponentsInChildren<Renderer>(true);
        }
    }

    private void OnEnable()
    {
        Roto = false;
        abiertaDesde = -1f;
        apertura = 0f;
        volando = false;
        Bloqueados = 0;
        if (pivote != null)
        {
            pivote.localPosition = posicionBase;
            pivote.localRotation = giroBase;
            pivote.localScale = escalaBase;
        }
        MostrarElEscudo(true);
    }

    // El bate (EnemyController.RecibirBatazo): el escudo sale volando para donde iba el golpe, girando, y se achica.
    public void RomperElEscudo(Vector3 direccion)
    {
        if (Roto) return;
        Efectos.EscudoRoto(PuntoDelEscudo, colorAmbar);
        Soltar(direccion, 4f, 4.5f);
    }

    private void Soltar(Vector3 direccion, float rapidez, float subida)
    {
        Roto = true;
        abiertaDesde = -1f;
        if (zombi != null) zombi.puedeZarpar = true;
        if (pivote == null) return;
        direccion.y = 0f;
        if (direccion.sqrMagnitude < 1e-4f) direccion = transform.forward;
        volando = true;
        voloDesde = Time.time;
        posicionAlVolar = pivote.position;
        giroAlVolar = pivote.rotation;
        velocidadAlVolar = direccion.normalized * rapidez + Vector3.up * subida;
        ejeAlVolar = Vector3.Cross(Vector3.up, direccion.normalized);
        if (ejeAlVolar.sqrMagnitude < 1e-4f) ejeAlVolar = Vector3.right;
    }

    // La guardia, al ritmo de la fisica: antes de los choques del paso, que son los que arrancan el zarpazo.
    private void FixedUpdate()
    {
        if (zombi == null || !zombi.Vivo) return;
        if (Roto)
        {
            zombi.puedeZarpar = true;
            return;
        }
        var jugador = zombi.thePlayer;
        if (jugador == null || DerrotaEnLaPartida.Activa)
        {
            abiertaDesde = -1f;
            return;
        }
        Vector3 hacia = jugador.transform.position - transform.position;
        hacia.y = 0f;
        float distancia = hacia.magnitude;
        if (!GuardiaAbierta && distancia <= distanciaDeLaGuardia) abiertaDesde = Time.time;
        else if (GuardiaAbierta && distancia > distanciaParaCerrar) abiertaDesde = -1f;
        zombi.puedeZarpar = GuardiaAbierta && Time.time - abiertaDesde >= aviso;
    }

    // Despues del Animator: el escudo se abre hacia la izquierda, girando alrededor del pecho, y el arrancado vuela.
    private void LateUpdate()
    {
        if (pivote == null) return;
        // Al morir lo suelta: colgado de la raiz, quedaria flotando mientras el cuerpo se desploma.
        if (!Roto && zombi != null && !zombi.Vivo) Soltar(transform.forward, 1.2f, 1.5f);
        if (volando)
        {
            Volar();
            return;
        }
        if (Roto) return;
        float dt = Mathf.Min(Time.deltaTime, 0.1f);
        bool abierta = GuardiaAbierta && zombi != null && zombi.Vivo;
        apertura = Mathf.MoveTowards(apertura, abierta ? 1f : 0f, dt / Mathf.Max(0.01f, tiempoDeAbrir));
        pivote.localRotation = giroBase;
        if (apertura > 0f)
        {
            float suave = apertura * apertura * (3f - 2f * apertura);
            pivote.rotation = Quaternion.AngleAxis(giroDeLaGuardia * suave, transform.up) * pivote.rotation;
        }
    }

    private void Volar()
    {
        float t = Time.time - voloDesde;
        float dt = Mathf.Min(Time.deltaTime, 0.1f);
        velocidadAlVolar += Vector3.down * 20f * dt;
        posicionAlVolar += velocidadAlVolar * dt;
        if (posicionAlVolar.y < 0.15f)
        {
            posicionAlVolar.y = 0.15f;
            velocidadAlVolar = Vector3.zero;
        }
        giroAlVolar = Quaternion.AngleAxis(720f * dt, ejeAlVolar) * giroAlVolar;
        pivote.position = posicionAlVolar;
        pivote.rotation = giroAlVolar;
        float achica = 1f - Mathf.Clamp01((t - VuelaDurante) / SeAchicaEn);
        pivote.localScale = escalaBase * achica;
        if (achica <= 0f)
        {
            volando = false;
            MostrarElEscudo(false);
        }
    }

    private void MostrarElEscudo(bool mostrar)
    {
        if (renderers == null) return;
        foreach (var r in renderers)
            if (r != null) r.enabled = mostrar;
    }
}
