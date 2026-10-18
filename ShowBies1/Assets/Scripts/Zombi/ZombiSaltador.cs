using UnityEngine;

// El saltador (ZOMBIS.md, de Argon; estrena en el cementerio, oleadas 15 a 20, y vuelve desde la 31): una emboscada en
// linea recta. Camina un poco mas rapido que el normal hasta quedar a 6 m del jugador, se frena en seco, se agacha 0,6 s
// mientras marca en el piso una franja de 5,5 m apuntando al jugador, y sale disparado por esa linea a 14 m/s durante
// 0,4 s, con el brazo del zarpazo estirado: si lo toca en el camino, pega 1,2 veces el golpe. Al caer se queda 0,8 s
// quieto, recuperandose: es la ventana para pegarle. Recarga: 5 s, contados desde que salta. Castiga caminar derecho sin
// mirar los avisos del piso, como la embestida del jefe pero corta.
//
// Mientras se acerca lo lleva la persecucion de siempre (Mover devuelve falso). El golpe del salto es el del cuerpo, como
// la embestida del jefe: EnemyController.golpeaAlChocar con su multiplicador, y una sola vez por salto (el intervalo de
// golpe es mas largo que el salto).
//
// El bate, mientras se agacha o en el aire, le corta el salto y lo empuja 3 m. El hielo le acorta el salto a 3,2 m y le
// duplica el rato que queda quieto al caer.
//
// Se lee por la silueta (flaco y largo, inclinado hacia adelante, con dos crestas hacia atras en la cabeza), los ojos
// grandes que brillan solos y la piel azul noche; las piezas las pone ConstructorZombis. La inclinacion y el agacharse
// van aca, en el LateUpdate, sobre el hijo del modelo.
//
// Sin RequireComponent: FondoMenu le borra los scripts a los zombis (ver la trampa en CLAUDE.md).
public class ZombiSaltador : MonoBehaviour, IMovimientoPropio
{
    [Header("El salto (de Argon)")]
    public float distanciaParaSaltar = 6f;
    public float aviso = 0.6f;
    public float velocidadDelSalto = 14f;
    public float duracionDelSalto = 0.4f;          // 5,6 m
    public float largoDelSaltoCongelado = 3.2f;
    public float recuperacion = 0.8f;
    public float recuperacionCongelado = 1.6f;
    public float recarga = 5f;                     // desde que salta
    public float golpeDelSalto = 1.2f;
    public float largoDeLaFranja = 5.5f;
    public float anchoDeLaFranja = 0.6f;

    [Header("Lo que se ve")]
    public float inclinacion = 30f;
    public float agachado = 0.6f;                  // la escala en Y del modelo mientras avisa
    public Material materialAviso;                 // el de la linea del jefe (Sprites/Default)
    public Color colorAviso = new Color(0.98f, 0.93f, 0.78f);   // crema

    public enum Estado { Acercandose, Agachado, Saltando, Recuperando }

    public Estado EstadoActual { get; private set; }
    public Vector3 Rumbo { get; private set; }
    public int Saltos { get; private set; }
    public int Cortados { get; private set; }
    public float UltimoLargo { get; private set; }
    public bool FranjaPrendida { get { return franja != null && franja.enabled; } }

    private static readonly int IdAtacar = Animator.StringToHash("Atacar");
    // El momento del zarpazo con el brazo estirado (EnemyController.momentoDelImpacto): ahi se congela el clip en el aire.
    private const float BrazoEstirado = 0.37f;

    private EnemyController zombi;
    private Transform modelo;
    private Quaternion giroBaseDelModelo;
    private Vector3 escalaBaseDelModelo;
    private Animator[] animadores;
    private LineRenderer franja;
    private float desde;
    private float proximoSalto = -1f;
    private float duracionDeEsteSalto;
    private Vector3 salioDe;
    private float inclinado;

    // Lo que dura el salto: 0,4 s, o lo que tarda en hacer 3,2 m congelado. Estatico para probarlo.
    public static float DuracionDelSalto(bool congelado, float duracion, float largoCongelado, float velocidad)
    {
        return congelado ? Mathf.Min(duracion, largoCongelado / Mathf.Max(0.1f, velocidad)) : duracion;
    }

    private void Awake()
    {
        zombi = GetComponent<EnemyController>();
        animadores = GetComponentsInChildren<Animator>(true);
        foreach (var animador in animadores)
        {
            if (animador.transform == transform || animador.runtimeAnimatorController == null) continue;
            modelo = animador.transform;
            break;
        }
        if (modelo != null)
        {
            giroBaseDelModelo = modelo.localRotation;
            escalaBaseDelModelo = modelo.localScale;
        }
        ArmarLaFranja();
    }

    private void OnEnable()
    {
        EstadoActual = Estado.Acercandose;
        proximoSalto = -1f;
        inclinado = 0f;
        if (franja != null) franja.enabled = false;
    }

    private void OnDisable()
    {
        Terminar();
    }

    private void OnDestroy()
    {
        if (franja != null) Destroy(franja.gameObject);
    }

    public bool Mover(Rigidbody rb, Transform jugador)
    {
        if (zombi == null || !zombi.Vivo || jugador == null) return false;
        float ahora = Time.time;
        Vector3 hacia = jugador.position - transform.position;
        hacia.y = 0f;

        switch (EstadoActual)
        {
            case Estado.Agachado:
                Quieto(rb);
                if (ahora - desde >= aviso) Saltar(ahora);
                return true;

            case Estado.Saltando:
            {
                Vector3 v = Rumbo * velocidadDelSalto;
                v.y = rb.linearVelocity.y;
                rb.linearVelocity = v;
                if (ahora - desde >= duracionDeEsteSalto) Caer(ahora);
                return true;
            }

            case Estado.Recuperando:
                Quieto(rb);
                if (ahora - desde >= (zombi.Congelado ? recuperacionCongelado : recuperacion))
                {
                    EstadoActual = Estado.Acercandose;
                    zombi.puedeZarpar = true;
                }
                return true;
        }

        // Acercandose: lo lleva la persecucion, hasta que este cerca y pueda saltar.
        if (hacia.magnitude > distanciaParaSaltar || ahora < proximoSalto) return false;
        Agacharse(hacia, ahora);
        Quieto(rb);
        return true;
    }

    private void Quieto(Rigidbody rb)
    {
        if (Rumbo.sqrMagnitude > 1e-4f) transform.rotation = Quaternion.LookRotation(Rumbo);
        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
    }

    private void Agacharse(Vector3 hacia, float ahora)
    {
        EstadoActual = Estado.Agachado;
        desde = ahora;
        Rumbo = hacia.sqrMagnitude > 1e-4f ? hacia.normalized : transform.forward;
        zombi.puedeZarpar = false;
        zombi.CortarZarpazo();
        if (franja != null)
        {
            Vector3 a = transform.position;
            a.y = JefePatrones.AlturaDelAviso;
            franja.SetPosition(0, a);
            franja.SetPosition(1, a + Rumbo * largoDeLaFranja);
            franja.enabled = true;
        }
    }

    private void Saltar(float ahora)
    {
        EstadoActual = Estado.Saltando;
        desde = ahora;
        Saltos++;
        proximoSalto = ahora + recarga;
        salioDe = transform.position;
        duracionDeEsteSalto = DuracionDelSalto(zombi.Congelado, duracionDelSalto, largoDelSaltoCongelado, velocidadDelSalto);
        if (franja != null) franja.enabled = false;
        // El golpe del salto es el del cuerpo, como la embestida del jefe: limpio, una vez, por 1,2.
        zombi.EmpezarEmbestida();
        zombi.golpeaAlChocar = true;
        zombi.multiplicadorGolpe = golpeDelSalto;
        // El brazo estirado, congelado en el aire.
        foreach (var animador in animadores)
        {
            if (animador == null || animador.runtimeAnimatorController == null || !animador.isActiveAndEnabled) continue;
            animador.Play(IdAtacar, 0, BrazoEstirado / 1.33f);
            animador.speed = 0f;
        }
        Efectos.SaltoDelSaltador(transform.position, colorAviso);
    }

    private void Caer(float ahora)
    {
        Vector3 d = transform.position - salioDe;
        d.y = 0f;
        UltimoLargo = d.magnitude;
        Terminar();
        // Quieto y sin zarpazos mientras se recupera: es la ventana para pegarle.
        zombi.puedeZarpar = false;
        EstadoActual = Estado.Recuperando;
        desde = ahora;
    }

    // Deja al zombi como uno comun: sin el golpe del cuerpo, con los zarpazos y el Animator andando.
    private void Terminar()
    {
        if (franja != null) franja.enabled = false;
        if (zombi != null)
        {
            if (EstadoActual == Estado.Saltando || EstadoActual == Estado.Agachado)
            {
                zombi.golpeaAlChocar = false;
                zombi.multiplicadorGolpe = 1f;
            }
            zombi.puedeZarpar = true;
        }
        if (animadores != null)
            foreach (var animador in animadores)
                if (animador != null) animador.speed = 1f;
    }

    // El bate (EnemyController lo empuja antes de que corra Mover) le corta el salto, mientras se agacha o en el aire.
    private void Update()
    {
        if (EstadoActual != Estado.Agachado && EstadoActual != Estado.Saltando) return;
        // Tambien si el jugador murio: la horda festeja, y el saltador quedaria congelado en el aire.
        if (zombi == null || !zombi.Vivo || zombi.Empujado || zombi.Frenado || DerrotaEnLaPartida.Activa)
        {
            Terminar();
            EstadoActual = Estado.Acercandose;
            proximoSalto = Time.time + recarga * 0.5f;
            Cortados++;
        }
    }

    // Despues del Animator: inclinado hacia adelante (suave, y no en el festejo ni muerto), y agachado mientras avisa. La
    // franja parpadea.
    private void LateUpdate()
    {
        bool vivo = zombi != null && zombi.Vivo && !DerrotaEnLaPartida.Activa;
        float dt = Mathf.Min(Time.deltaTime, 0.1f);
        inclinado = Mathf.MoveTowards(inclinado, vivo ? 1f : 0f, dt * 4f);
        if (modelo != null && (vivo || inclinado > 0f))
        {
            modelo.localRotation = giroBaseDelModelo * Quaternion.Euler(inclinacion * inclinado, 0f, 0f);
            float y = EstadoActual == Estado.Agachado && vivo
                ? Mathf.Lerp(1f, agachado, Mathf.Clamp01((Time.time - desde) / 0.12f))
                : 1f;
            modelo.localScale = new Vector3(escalaBaseDelModelo.x, escalaBaseDelModelo.y * y, escalaBaseDelModelo.z);
        }
        if (franja != null && franja.enabled)
        {
            Color c = colorAviso;
            c.a = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(Time.time * 14f));
            franja.startColor = franja.endColor = c;
        }
    }

    private void ArmarLaFranja()
    {
        if (materialAviso == null) return;
        var go = new GameObject("AvisoSaltador");
        franja = go.AddComponent<LineRenderer>();
        franja.useWorldSpace = true;
        franja.positionCount = 2;
        franja.material = materialAviso;
        franja.widthMultiplier = anchoDeLaFranja;
        franja.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        franja.receiveShadows = false;
        // Plana sobre el piso, como la linea de la carga del jefe.
        franja.alignment = LineAlignment.TransformZ;
        go.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
        franja.enabled = false;
    }
}
