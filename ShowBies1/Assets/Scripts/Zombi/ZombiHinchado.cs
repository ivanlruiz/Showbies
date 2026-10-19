using System.Collections.Generic;
using UnityEngine;

// El hinchado (ZOMBIS.md, de Argon; estrena en la ciudad, oleada 24): una bomba que camina, y que sirve contra la horda.
// No tira zarpazos. Cuando llega a 1,6 m del jugador, o cuando su vida llega a cero, se clava en el piso y se infla
// 0,75 s (1,1 congelado) marcando un circulo esmeralda de 2,6 m con un disco que se llena, y revienta: al jugador que
// sigue adentro le pega 2 golpes, y a los zombis de adentro les saca el 35 % de su vida maxima (al jefe, el 4 %) y los
// empuja 2,5 m (a los pesados, la mitad y los frena medio segundo; al jefe, nada). Lo que mata paga entero, como
// cualquier muerte, y su propia muerte tambien, aunque se haya volado solo (de Argon).
//
// - Con la vida en cero no muere: EnemyController.DanoZombi le pregunta EmpezarAInflarse antes del bloque de la muerte,
//   y al reventar muere de verdad (MorirReventado), sin cadaver. Mientras se infla no le entra nada: ni otra bala ni otra
//   explosion, que es el anti-bucle de la cadena (un hinchado que la explosion de otro deja en cero se infla y revienta
//   tambien).
// - El bate, inflado, lo patea 4 m (EnemyController.RecibirBatazo) sin cortarle la cuenta, y el circulo va con el:
//   la jugada estrella, mandarlo al medio de la horda. Caminando es liviano, y el bate lo empuja 3 m como al normal.
// - Respeta el bono del chaman (el 35 % de un potenciado es el 26,25 %) y atraviesa el escudo del blindado: el origen es
//   OrigenDelDano.Hinchado.
// - Si el jugador muere mientras se infla, se desinfla en 0,2 s y festeja con los demas.
//
// Las piezas (el tacho esmeralda de la espalda con sus tapas, la panza y el charco del piso) las pone ConstructorZombis;
// aca se mueven: el bamboleo al caminar, el inflado y el parpadeo del tubo. El aviso del piso es un objeto aparte que lo
// sigue, como el anillo del escupidor.
//
// Sin RequireComponent: FondoMenu le borra los scripts a los zombis (ver la trampa en CLAUDE.md).
public class ZombiHinchado : MonoBehaviour, IMovimientoPropio
{
    [Header("La explosion (de Argon)")]
    public float distanciaParaClavarse = 1.6f;
    public float aviso = 0.75f;
    public float avisoCongelado = 1.1f;
    public float radio = 2.6f;
    public float golpeAlJugador = 2f;              // veces su golpe
    public float parteDeLaVida = 0.35f;            // de la vida maxima de los zombis alcanzados
    public float parteDelJefe = 0.04f;
    public float empujon = 2.5f;
    public float duracionDelEmpujon = 0.3f;
    public float frenadoDelPesado = 0.5f;
    public float pateada = 4f;                     // el bate, inflado (EnemyController.RecibirBatazo)

    [Header("Lo que se ve")]
    public float bamboleo = 9f;                    // grados de lado a lado al caminar
    public float ritmoDelBamboleo = 17.5f;         // 2,8 Hz
    public float infladoDelCuerpo = 1.6f / 1.35f;  // el modelo, en X y Z
    public float infladoDeLasPiezas = 1.45f;       // la panza y el tacho
    public float desinflarEn = 0.2f;
    public Transform piezas;                       // el pivote de las piezas, en la raiz: sigue al torso
    public Transform torso;
    public Renderer tubo;
    public Transform[] piezasQueSeInflan;
    public Renderer charco;
    public Material materialAviso;                 // el de la linea del jefe (Sprites/Default)
    public Color verde = new Color(0f, 1f, 0.4f);   // #00FF66
    public Color lima = new Color(0.8f, 1f, 0f);    // #CCFF00

    public bool Inflando { get; private set; }
    public int Reventones { get; private set; }
    public int UltimosAlcanzados { get; private set; }
    public int UltimosMuertos { get; private set; }
    public bool UltimoLePegoAlJugador { get; private set; }
    public float CuentaDeEsteAviso { get; private set; }
    public bool AvisoPrendido { get { return borde != null && borde.enabled; } }
    public float RadioDelDiscoAhora { get { return disco != null && disco.gameObject.activeSelf ? disco.localScale.x : 0f; } }

    public static readonly Color ColorDelHinchado = new Color(0f, 1f, 0.4f);

    private EnemyController zombi;
    private Rigidbody cuerpo;
    private Transform modelo;
    private Quaternion giroBaseDelModelo;
    private Vector3 escalaBaseDelModelo;
    private Vector3[] escalasBaseDeLasPiezas;
    private Vector3 piezasDesdeElTorso;
    private float desde;
    private float inflado;
    private float inclinado;
    private LineRenderer borde;
    private Transform disco;
    private MaterialPropertyBlock propiedades;
    private static Mesh mallaDelDisco;
    private static readonly int idColor = Shader.PropertyToID("_Color");
    private static readonly Collider[] enLaExplosion = new Collider[96];
    private readonly HashSet<EnemyController> alcanzados = new HashSet<EnemyController>();

    // Lo que le saca la explosion a un zombi: su parte de la vida maxima. Estatico para probarlo.
    public static float DanoDeLaExplosion(float vidaMaxima, bool esJefe, float parte, float parteDelJefe)
    {
        return vidaMaxima * (esJefe ? parteDelJefe : parte);
    }

    private void Awake()
    {
        zombi = GetComponent<EnemyController>();
        cuerpo = GetComponent<Rigidbody>();
        foreach (var animador in GetComponentsInChildren<Animator>(true))
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
        if (piezas != null && torso != null) piezasDesdeElTorso = transform.InverseTransformVector(piezas.position - torso.position);
        if (piezasQueSeInflan != null)
        {
            escalasBaseDeLasPiezas = new Vector3[piezasQueSeInflan.Length];
            for (int i = 0; i < piezasQueSeInflan.Length; i++)
                if (piezasQueSeInflan[i] != null) escalasBaseDeLasPiezas[i] = piezasQueSeInflan[i].localScale;
        }
        ArmarElAviso();
    }

    // Cada aparicion arranca de cero, tambien lo que cuenta (el pool reusa el mismo componente).
    private void OnEnable()
    {
        Inflando = false;
        Reventones = 0;
        UltimosAlcanzados = 0;
        UltimosMuertos = 0;
        UltimoLePegoAlJugador = false;
        inflado = 0f;
        inclinado = 0f;
        PrenderElAviso(false);
        if (charco != null) charco.enabled = true;
        AplicarInflado(0f, 0f);
    }

    private void OnDisable()
    {
        Inflando = false;
        PrenderElAviso(false);
    }

    private void OnDestroy()
    {
        if (borde != null) Destroy(borde.gameObject);
        if (disco != null) Destroy(disco.gameObject);
    }

    // Lo pregunta EnemyController.DanoZombi con la vida en cero: si se hace cargo (empieza a inflarse, o ya se inflaba),
    // el zombi no muere todavia.
    public bool EmpezarAInflarse()
    {
        if (Inflando) return true;
        if (zombi == null || !zombi.Vivo) return false;
        Inflar();
        return true;
    }

    public bool Mover(Rigidbody rb, Transform jugador)
    {
        if (zombi == null) return false;
        // No tira zarpazos nunca: su golpe es la explosion (EnemyController lo prende en cada aparicion).
        zombi.puedeZarpar = false;
        if (!zombi.Vivo || jugador == null) return false;
        if (Inflando)
        {
            // Clavado en el piso. El bate lo mueve antes (EnemyController.MoverPorElBatazo) y el circulo va con el.
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            return true;
        }
        Vector3 hacia = jugador.position - transform.position;
        hacia.y = 0f;
        if (hacia.magnitude > distanciaParaClavarse) return false;
        Inflar();
        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        return true;
    }

    private void Inflar()
    {
        Inflando = true;
        desde = Time.time;
        CuentaDeEsteAviso = zombi != null && zombi.Congelado ? avisoCongelado : aviso;
        if (zombi != null) zombi.CortarZarpazo();
        PrenderElAviso(true);
        Efectos.HinchadoSeInfla(transform.position + Vector3.up, verde);
    }

    // La cuenta, con tiempo escalado: la pausa la congela. Si el jugador murio, espera (la oferta de revivir congela el
    // juego, y si revive sigue); con la derrota, se desinfla y festeja con los demas.
    private void Update()
    {
        if (!Inflando) return;
        if (zombi == null || !zombi.Vivo)
        {
            Inflando = false;
            PrenderElAviso(false);
            return;
        }
        if (DerrotaEnLaPartida.Activa)
        {
            Inflando = false;
            PrenderElAviso(false);
            return;
        }
        if (PlayerHealth.instance != null && PlayerHealth.instance.EstaMuerto)
        {
            desde += Time.deltaTime;
            return;
        }
        // Congelado en el medio de la cuenta, tambien se estira.
        if (zombi.Congelado && CuentaDeEsteAviso < avisoCongelado) CuentaDeEsteAviso = avisoCongelado;
        if (Time.time - desde >= CuentaDeEsteAviso) Reventar();
    }

    private void Reventar()
    {
        Inflando = false;
        PrenderElAviso(false);
        if (charco != null) charco.enabled = false;
        Vector3 centro = transform.position;

        bool alJugador = false;
        var vida = PlayerHealth.instance;
        if (vida != null && !vida.EstaMuerto)
        {
            Vector3 d = vida.transform.position - centro;
            d.y = 0f;
            if (d.magnitude <= radio)
            {
                vida.TakeDamage(zombi.enemyType.daño * zombi.multiplicadorDano * golpeAlJugador);
                alJugador = true;
            }
        }

        int muertos = 0;
        alcanzados.Clear();
        alcanzados.Add(zombi);
        int n = Physics.OverlapSphereNonAlloc(centro, radio, enLaExplosion);
        for (int i = 0; i < n; i++)
        {
            var otro = enLaExplosion[i].GetComponentInParent<EnemyController>();
            if (otro == null || !otro.Vivo || !alcanzados.Add(otro)) continue;
            Vector3 afuera = otro.transform.position - centro;
            afuera.y = 0f;
            if (afuera.magnitude > radio + 0.5f) continue;
            Vector3 rumbo = afuera.sqrMagnitude > 1e-4f ? afuera.normalized : transform.forward;
            otro.DanoZombi(DanoDeLaExplosion(otro.VidaMaxima, otro.EsJefe, parteDeLaVida, parteDelJefe), false, rumbo,
                           ColorDelHinchado, OrigenDelDano.Hinchado);
            if (!otro.Vivo) muertos++;
            else otro.Despedir(rumbo, empujon, duracionDelEmpujon, frenadoDelPesado);
        }
        Reventones++;
        UltimosAlcanzados = alcanzados.Count - 1;
        UltimosMuertos = muertos;
        UltimoLePegoAlJugador = alJugador;
        Efectos.ExplosionDelHinchado(centro + Vector3.up, verde, lima, muertos, alJugador);
        zombi.MorirReventado();
    }

    // Despues del Animator: el bamboleo al caminar, el inflado (el cuerpo y las piezas), el parpadeo del tubo y el aviso,
    // que sigue al zombi (el bate lo patea).
    private void LateUpdate()
    {
        float dt = Mathf.Min(Time.deltaTime, 0.1f);
        bool vivo = zombi != null && zombi.Vivo && !DerrotaEnLaPartida.Activa;
        if (Inflando) inflado = Mathf.Clamp01((Time.time - desde) / Mathf.Max(0.01f, CuentaDeEsteAviso));
        else inflado = Mathf.MoveTowards(inflado, 0f, dt / Mathf.Max(0.01f, desinflarEn));

        float anda = 0f;
        if (vivo && !Inflando && cuerpo != null)
        {
            Vector3 v = cuerpo.linearVelocity;
            v.y = 0f;
            anda = v.magnitude > 0.3f ? 1f : 0f;
        }
        inclinado = Mathf.MoveTowards(inclinado, anda, dt * 5f);
        float giro = Mathf.Sin(Time.time * ritmoDelBamboleo) * bamboleo * inclinado;
        AplicarInflado(inflado, giro);

        // El tubo parpadea entre el esmeralda y el lima: suave caminando (2 Hz) y furioso en el aviso (10 Hz).
        if (tubo != null && tubo.enabled)
        {
            float t = Inflando
                ? (Mathf.Sin(Time.time * Mathf.PI * 2f * 10f) > 0f ? 1f : 0f)
                : 0.5f + 0.5f * Mathf.Sin(Time.time * Mathf.PI * 2f * 2f);
            if (propiedades == null) propiedades = new MaterialPropertyBlock();
            propiedades.SetColor(idColor, Color.Lerp(verde, lima, t));
            tubo.SetPropertyBlock(propiedades);
            if (charco != null && charco.enabled) charco.SetPropertyBlock(propiedades);
            if (!Inflando)
            {
                float grueso = 1f + 0.05f * t;
                Vector3 e = tubo.transform.localScale;
                tubo.transform.localScale = new Vector3(e.x * grueso, e.y, e.z * grueso);
            }
        }

        if (Inflando && borde != null)
        {
            Vector3 aca = transform.position;
            Granade.DibujarAnillo(borde, aca, radio);
            Color c = verde;
            c.a = 0.75f;
            borde.startColor = borde.endColor = c;
            disco.position = new Vector3(aca.x, Granade.AlturaDelAnillo - 0.01f, aca.z);
            float r = Mathf.Max(0.001f, radio * inflado);
            disco.localScale = new Vector3(r, 1f, r);
        }
    }

    // El modelo de 1,35 a 1,60 de ancho, y la panza y el tacho a x1,45, con el bamboleo en Z.
    private void AplicarInflado(float t, float giro)
    {
        if (modelo != null)
        {
            float ancho = Mathf.Lerp(1f, infladoDelCuerpo, t);
            modelo.localScale = new Vector3(escalaBaseDelModelo.x * ancho, escalaBaseDelModelo.y, escalaBaseDelModelo.z * ancho);
            modelo.localRotation = giroBaseDelModelo * Quaternion.Euler(0f, 0f, giro);
        }
        // Las piezas siguen al torso (el paso y el bamboleo) con el giro del bamboleo, sin los giros del hueso.
        if (piezas != null && torso != null)
        {
            piezas.rotation = transform.rotation * Quaternion.Euler(0f, 0f, giro);
            piezas.position = torso.position + transform.TransformVector(piezasDesdeElTorso);
        }
        if (piezasQueSeInflan != null && escalasBaseDeLasPiezas != null)
        {
            float crece = Mathf.Lerp(1f, infladoDeLasPiezas, t);
            for (int i = 0; i < piezasQueSeInflan.Length; i++)
                if (piezasQueSeInflan[i] != null) piezasQueSeInflan[i].localScale = escalasBaseDeLasPiezas[i] * crece;
        }
    }

    private void PrenderElAviso(bool prender)
    {
        if (borde != null) borde.enabled = prender;
        if (disco != null) disco.gameObject.SetActive(prender);
        if (prender && disco != null) disco.localScale = new Vector3(0.001f, 1f, 0.001f);
    }

    // El borde fijo de 2,6 m (al 75 %) y el disco que se llena (al 22 %), sueltos y no hijos: el destello del golpe los
    // pintaria de blanco, y girarian y se inflarian con el.
    private void ArmarElAviso()
    {
        if (materialAviso == null) return;
        var go = new GameObject("AvisoDelHinchado");
        borde = go.AddComponent<LineRenderer>();
        borde.material = materialAviso;
        borde.widthMultiplier = 0.14f;
        borde.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        borde.receiveShadows = false;
        borde.alignment = LineAlignment.TransformZ;
        go.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
        borde.enabled = false;

        var d = new GameObject("DiscoDelHinchado");
        d.AddComponent<MeshFilter>().sharedMesh = MallaDelDisco(new Color(verde.r, verde.g, verde.b, 0.22f));
        var render = d.AddComponent<MeshRenderer>();
        render.sharedMaterial = materialAviso;
        render.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        render.receiveShadows = false;
        disco = d.transform;
        d.SetActive(false);
    }

    // Un disco de radio 1, acostado, con el color en los vertices (Sprites/Default lo usa). Uno para todos.
    private static Mesh MallaDelDisco(Color color)
    {
        if (mallaDelDisco != null) return mallaDelDisco;
        const int lados = 40;
        var vertices = new Vector3[lados + 1];
        var colores = new Color[lados + 1];
        var triangulos = new int[lados * 3];
        vertices[0] = Vector3.zero;
        colores[0] = color;
        for (int i = 0; i < lados; i++)
        {
            float a = i * Mathf.PI * 2f / lados;
            vertices[i + 1] = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
            colores[i + 1] = color;
            triangulos[i * 3] = 0;
            triangulos[i * 3 + 1] = (i + 1) % lados + 1;
            triangulos[i * 3 + 2] = i + 1;
        }
        mallaDelDisco = new Mesh { name = "DiscoDelHinchado", vertices = vertices, colors = colores, triangles = triangulos };
        mallaDelDisco.RecalculateBounds();
        return mallaDelDisco;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetearEstadoCompartido()
    {
        mallaDelDisco = null;
    }
}
