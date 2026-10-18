using System.Collections.Generic;
using UnityEngine;

// El chaman (ZOMBIS.md, de Argon; estrena en el cementerio, oleadas 18 a 20, y vuelve desde la 31): el apoyo de la
// horda, desde atras. Camina hasta quedar a 7 m del jugador y se planta; si hay un pesado cerca (el tanque), se pone
// detras de el. Cada 4 s se frena, se arquea hacia atras 0,5 s con el cristal del totem prendido y el aro de los pies
// creciendo hasta 4,5 m, y potencia a los 4 aliados mas cercanos que quedaron adentro durante 3 s: corren un 25 % mas
// y les entra un 25 % menos de daño, con un cubito rubi girando sobre la cabeza mientras les dura. Obliga a elegir
// blanco: el auto-apuntado le tira al de adelante, potenciado, y al chaman hay que apuntarle a mano. Si muere, el bono
// se apaga en todos en el acto (EnemyController.Potenciado mira si el que se lo dio sigue vivo).
//
// No potencia a los que se mueven por su cuenta (el jefe, el tesoro y los otros zombis con habilidad, chamanes
// incluidos): su movimiento no pasa por la persecucion de siempre, y el bono les quedaria a medias.
//
// Solo pulsa si se lo ve en la pantalla (como escupe el escupidor y ataca el jefe) y con al menos un aliado al
// alcance. El bate lo empuja (es liviano) y le corta el pulso; congelado no pulsa.
//
// Se lee por la piel hueso palido, alto y flaco, el totem en T sobre la cabeza con un cubo rubi que gira, y el aro rubi
// de los pies. El totem lo pone ConstructorZombis; el aro y las marcas de los potenciados son objetos aparte que lo
// siguen (como la barra de vida): hijos del zombi, el destello los pintaria de blanco y cada bala los aplastaria.
//
// Sin RequireComponent: FondoMenu le borra los scripts a los zombis (ver la trampa en CLAUDE.md).
public class ZombiChaman : MonoBehaviour, IMovimientoPropio
{
    [Header("Donde se planta (de Argon)")]
    public float distanciaParaPlantarse = 7f;
    public float borde = 45f;                    // del centro: las paredes invisibles estan en 49

    [Header("Detras de un pesado")]
    public float pesadoHasta = 5f;               // cuan cerca del chaman tiene que estar el pesado
    public float detrasDelPesado = 1.6f;
    public float refugioDesde = 5.5f;            // del jugador: mas cerca, el pesado lo esta llevando a sus pies
    public float refugioHasta = 9f;

    [Header("El pulso")]
    public float cadaCuanto = 4f;                // de pulso a pulso, con el aviso adentro
    public float primerPulso = 1.5f;
    public float aviso = 0.5f;
    public float radioDelPulso = 4.5f;
    public int aliadosPorPulso = 4;
    public float duracionDelBono = 3f;
    public float velocidadConElBono = 1.25f;
    public float velocidadMaximaConElBono = 11f;   // m/s: el jugador corre a 11,5 y siempre se tiene que poder escapar
    public float danoRecibidoConElBono = 0.75f;

    [Header("Lo que se ve")]
    public float arqueo = 25f;
    public float radioDelAro = 0.55f;
    public Material materialAviso;               // el de la linea del jefe (Sprites/Default)
    public Material materialRubi;                // Unlit: el cristal y las marcas
    public Mesh mallaDeLaMarca;                  // el cubo
    public Color colorRubi = new Color(1f, 0.09f, 0.27f);   // #FF1744, de Argon
    public Transform cristal;                    // el cubo del totem: lo pone ConstructorZombis
    public float ladoDeLaMarca = 0.22f;

    public enum Estado { Acercandose, Rodeando, Refugiandose, Plantado, Avisando }

    // Lo cerca del lugar detras del pesado que tiene que llegar para quedarse quieto.
    public const float LlegoAlRefugio = 0.35f;
    // Cuanto arriba de la cabeza va la marca de un potenciado: por encima de su barra de vida.
    public const float AlturaDeLaMarca = 0.6f;

    public Estado EstadoActual { get; private set; }
    public int Pulsos { get; private set; }
    public int Cortados { get; private set; }
    public int UltimosPotenciados { get; private set; }
    public Vector3 Refugio { get; private set; }
    public bool AroPrendido { get { return aro != null && aro.enabled; } }
    public float RadioDelAroAhora { get; private set; }

    public int MarcasALaVista
    {
        get
        {
            int n = 0;
            if (marcas != null)
                foreach (var m in marcas) if (m != null && m.gameObject.activeSelf) n++;
            return n;
        }
    }

    private EnemyController zombi;
    private Transform modelo;
    private Quaternion giroBaseDelModelo;
    private Vector3 escalaDelCristal;
    private LineRenderer aro;
    private EnemyController[] marcados;
    private Transform[] marcas;
    private float avisoDesde;
    private float proximoPulso;
    private bool primerPulsoPuesto;
    private float rodeandoDesde = -1f;
    private float arqueado;   // de 0 a 1: se suaviza

    private struct Candidato
    {
        public EnemyController zombi;
        public float distancia;
    }

    private static readonly List<Candidato> candidatos = new List<Candidato>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetearEstadoCompartido()
    {
        candidatos.Clear();
    }

    // Si 'otro' es un aliado que el pulso puede potenciar: vivo, no el mismo chaman y que camine con la persecucion de
    // siempre (ni el jefe, ni el tesoro, ni los otros con habilidad).
    public static bool Potenciable(EnemyController otro, EnemyController chaman)
    {
        return otro != null && otro != chaman && otro.Vivo && !otro.EsJefe && !otro.SeMuevePorSuCuenta;
    }

    // El lugar detras de un pesado, visto desde el jugador.
    public static Vector3 DetrasDe(Vector3 pesado, Vector3 jugador, float cuanto)
    {
        Vector3 desdeElJugador = pesado - jugador;
        desdeElJugador.y = 0f;
        if (desdeElJugador.sqrMagnitude < 1e-6f) return pesado;
        return pesado + desdeElJugador.normalized * cuanto;
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
        if (cristal != null) escalaDelCristal = cristal.localScale;
        marcados = new EnemyController[Mathf.Max(1, aliadosPorPulso)];
        ArmarElAro();
        ArmarLasMarcas();
    }

    private void OnEnable()
    {
        EstadoActual = Estado.Acercandose;
        primerPulsoPuesto = false;
        rodeandoDesde = -1f;
        arqueado = 0f;
        UltimosPotenciados = 0;
        if (marcados != null) System.Array.Clear(marcados, 0, marcados.Length);
        Esconder();
    }

    private void OnDisable()
    {
        Esconder();
    }

    private void OnDestroy()
    {
        if (aro != null) Destroy(aro.gameObject);
        if (marcas != null)
            foreach (var m in marcas) if (m != null) Destroy(m.gameObject);
    }

    public bool Mover(Rigidbody rb, Transform jugador)
    {
        if (zombi == null || !zombi.Vivo || jugador == null) return false;
        Vector3 hacia = jugador.position - transform.position;
        hacia.y = 0f;
        float distancia = hacia.magnitude;
        float ahora = Time.time;
        if (!primerPulsoPuesto)
        {
            primerPulsoPuesto = true;
            proximoPulso = ahora + primerPulso;
        }

        if (EstadoActual == Estado.Avisando)
        {
            Quieto(rb, hacia);
            if (ahora - avisoDesde >= aviso) Pulsar(ahora);
            return true;
        }

        // El pulso, cada 4 s, se este acercando o plantado: se frena donde este.
        if (ahora >= proximoPulso && PuedePulsar())
        {
            EstadoActual = Estado.Avisando;
            avisoDesde = ahora;
            Quieto(rb, hacia);
            return true;
        }

        // Detras de un pesado, si hay uno cerca y ese lugar queda a buena distancia del jugador.
        Vector3 refugio;
        if (BuscarRefugio(jugador.position, out refugio))
        {
            rodeandoDesde = -1f;
            Refugio = refugio;
            Vector3 alRefugio = refugio - transform.position;
            alRefugio.y = 0f;
            if (alRefugio.magnitude > LlegoAlRefugio)
            {
                EstadoActual = Estado.Refugiandose;
                Caminar(rb, refugio);
            }
            else
            {
                EstadoActual = Estado.Plantado;
                Quieto(rb, hacia);
            }
            return true;
        }

        bool plantarse = ZombiEscupidor.SePlanta(distancia, EstadoActual == Estado.Plantado, distanciaParaPlantarse);
        // Desde afuera de la pantalla no se planta: rodea hacia los costados, como el escupidor, y si no, se acerca.
        if (plantarse && !SeVe())
        {
            if (rodeandoDesde < 0f) rodeandoDesde = ahora;
            if (ahora - rodeandoDesde < ZombiEscupidor.TopeDelRodeo)
            {
                EstadoActual = Estado.Rodeando;
                Vector3 rumbo = ZombiEscupidor.RumboDelRodeo(transform.position, hacia, distancia, distanciaParaPlantarse, borde);
                if (hacia.sqrMagnitude > 1e-4f) transform.rotation = Quaternion.LookRotation(hacia);
                Vector3 v = rumbo * (zombi.enemyType.velocidad * zombi.MultiplicadorDeVelocidad);
                v.y = rb.linearVelocity.y;
                rb.linearVelocity = v;
                return true;
            }
            plantarse = false;
        }
        if (!plantarse)
        {
            EstadoActual = Estado.Acercandose;
            return false;
        }
        rodeandoDesde = -1f;
        EstadoActual = Estado.Plantado;
        Quieto(rb, hacia);
        return true;
    }

    private void Quieto(Rigidbody rb, Vector3 hacia)
    {
        if (hacia.sqrMagnitude > 1e-4f) transform.rotation = Quaternion.LookRotation(hacia);
        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
    }

    // Hasta un lugar, con el rodeo de los edificios y el esquive de lo redondo, como la persecucion.
    private void Caminar(Rigidbody rb, Vector3 destino)
    {
        Vector3 objetivo = EnemyController.Rodeo(transform.position, destino, CapitulosDeEscenario.Huellas, zombi.RadioDeSuCuerpo);
        objetivo = EnemyController.Esquive(transform.position, objetivo, CapitulosDeEscenario.Redondos, zombi.RadioDeSuCuerpo, false);
        Vector3 rumbo = objetivo - transform.position;
        rumbo.y = 0f;
        if (rumbo.sqrMagnitude < 1e-4f) return;
        rumbo.Normalize();
        transform.rotation = Quaternion.LookRotation(rumbo);
        Vector3 v = rumbo * (zombi.enemyType.velocidad * zombi.MultiplicadorDeVelocidad);
        v.y = rb.linearVelocity.y;
        rb.linearVelocity = v;
    }

    // El lugar detras del pesado vivo mas cercano al chaman (el tanque; el jefe no), si queda entre 5,5 y 9 m del
    // jugador: mas cerca, el pesado lo estaria llevando a sus pies.
    private bool BuscarRefugio(Vector3 jugador, out Vector3 refugio)
    {
        refugio = Vector3.zero;
        float mejor = float.MaxValue;
        var vivos = EnemyController.Vivos;
        for (int i = 0; i < vivos.Count; i++)
        {
            var otro = vivos[i];
            if (otro == null || otro == zombi || !otro.Vivo || otro.EsJefe || !otro.EsPesado) continue;
            Vector3 alPesado = otro.transform.position - transform.position;
            alPesado.y = 0f;
            float distancia = alPesado.magnitude;
            if (distancia > pesadoHasta || distancia >= mejor) continue;
            Vector3 lugar = DetrasDe(otro.transform.position, jugador, detrasDelPesado);
            Vector3 alJugador = lugar - jugador;
            alJugador.y = 0f;
            float lejos = alJugador.magnitude;
            if (lejos < refugioDesde || lejos > refugioHasta) continue;
            lugar.y = transform.position.y;
            refugio = lugar;
            mejor = distancia;
        }
        return mejor < float.MaxValue;
    }

    private bool PuedePulsar()
    {
        if (zombi.Congelado || zombi.Empujado || zombi.Frenado || DerrotaEnLaPartida.Activa) return false;
        if (!SeVe()) return false;
        return JuntarAliados() > 0;
    }

    // Los aliados potenciables adentro del radio del pulso, del mas cercano al mas lejano.
    private int JuntarAliados()
    {
        candidatos.Clear();
        var vivos = EnemyController.Vivos;
        for (int i = 0; i < vivos.Count; i++)
        {
            var otro = vivos[i];
            if (!Potenciable(otro, zombi)) continue;
            Vector3 d = otro.transform.position - transform.position;
            d.y = 0f;
            float distancia = d.magnitude;
            if (distancia > radioDelPulso) continue;
            candidatos.Add(new Candidato { zombi = otro, distancia = distancia });
        }
        candidatos.Sort((a, b) => a.distancia.CompareTo(b.distancia));
        return candidatos.Count;
    }

    private void Pulsar(float ahora)
    {
        EstadoActual = Estado.Acercandose;
        // "Cada 4 s" es de pulso a pulso, con el aviso adentro.
        proximoPulso = ahora + Mathf.Max(0.1f, cadaCuanto - aviso);
        Pulsos++;
        int n = Mathf.Min(JuntarAliados(), marcados.Length);
        System.Array.Clear(marcados, 0, marcados.Length);
        for (int i = 0; i < n; i++)
        {
            var aliado = candidatos[i].zombi;
            aliado.Potenciar(zombi, duracionDelBono, velocidadConElBono, velocidadMaximaConElBono, danoRecibidoConElBono);
            marcados[i] = aliado;
        }
        candidatos.Clear();
        UltimosPotenciados = n;
        Efectos.PulsoDelChaman(transform.position + Vector3.up * (zombi.AlturaDeLaCabeza * 0.9f), colorRubi, n);
    }

    // El bate le corta el pulso (lo empuja EnemyController antes de que corra Mover), y el hielo, morirse y la derrota
    // tambien.
    private void Update()
    {
        if (EstadoActual != Estado.Avisando) return;
        if (zombi == null || !zombi.Vivo || zombi.Empujado || zombi.Frenado || zombi.Congelado || DerrotaEnLaPartida.Activa)
        {
            EstadoActual = Estado.Acercandose;
            proximoPulso = Time.time + cadaCuanto * 0.5f;
            Cortados++;
        }
    }

    private bool SeVe()
    {
        var camara = Camera.main;
        if (camara == null) return true;
        return ZombiEscupidor.SeVeEn(camara.WorldToViewportPoint(transform.position + Vector3.up * (zombi.AlturaDeLaCabeza * 0.5f)));
    }

    // Despues del Animator: el arqueo del aviso (y nada en el festejo, que posa el modelo por su cuenta), el cristal que
    // gira y se prende, el aro de los pies que crece con el aviso, y las marcas sobre los potenciados.
    private void LateUpdate()
    {
        bool vivo = zombi != null && zombi.Vivo && !DerrotaEnLaPartida.Activa;
        float dt = Mathf.Min(Time.deltaTime, 0.1f);
        float t = vivo && EstadoActual == Estado.Avisando ? Mathf.Clamp01((Time.time - avisoDesde) / Mathf.Max(0.01f, aviso)) : 0f;
        arqueado = Mathf.MoveTowards(arqueado, t > 0f ? 1f : 0f, dt * 6f);
        if (modelo != null && (vivo || arqueado > 0f))
            modelo.localRotation = giroBaseDelModelo * Quaternion.Euler(-arqueo * arqueado, 0f, 0f);

        if (cristal != null)
        {
            float giro = Time.time * (90f + 540f * t);
            cristal.rotation = Quaternion.Euler(0f, giro, 0f) * Quaternion.Euler(45f, 0f, 35f);
            cristal.localScale = escalaDelCristal * ((1f + 0.08f * Mathf.Sin(Time.time * 5f)) * (1f + 0.7f * t));
        }

        if (aro != null)
        {
            if (!vivo)
            {
                aro.enabled = false;
            }
            else
            {
                // Sale rapido y frena: el aro llega al radio del pulso cuando potencia.
                float sale = 1f - (1f - t) * (1f - t);
                RadioDelAroAhora = Mathf.Lerp(radioDelAro, radioDelPulso, sale);
                Granade.DibujarAnillo(aro, transform.position, RadioDelAroAhora, 40);
                aro.widthMultiplier = Mathf.Lerp(0.08f, 0.16f, t);
                Color c = colorRubi;
                c.a = t > 0f ? 0.5f + 0.5f * Mathf.Abs(Mathf.Sin(Time.time * 16f)) : 0.85f;
                aro.startColor = aro.endColor = c;
                aro.enabled = true;
            }
        }

        if (marcas != null)
        {
            for (int i = 0; i < marcas.Length; i++)
            {
                if (marcas[i] == null) continue;
                var marcado = marcados[i];
                bool ver = vivo && marcado != null && marcado.PotenciadoPor == zombi;
                if (marcas[i].gameObject.activeSelf != ver) marcas[i].gameObject.SetActive(ver);
                if (!ver) continue;
                marcas[i].position = marcado.transform.position + Vector3.up * (marcado.AlturaDeLaCabeza + AlturaDeLaMarca);
                marcas[i].rotation = Quaternion.Euler(0f, Time.time * 240f + i * 45f, 0f) * Quaternion.Euler(45f, 0f, 35f);
            }
        }
    }

    private void Esconder()
    {
        if (aro != null) aro.enabled = false;
        if (marcas != null)
            foreach (var m in marcas) if (m != null && m.gameObject.activeSelf) m.gameObject.SetActive(false);
    }

    private void ArmarElAro()
    {
        if (materialAviso == null) return;
        var go = new GameObject("AroChaman");
        aro = go.AddComponent<LineRenderer>();
        aro.material = materialAviso;
        aro.widthMultiplier = 0.08f;
        aro.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        aro.receiveShadows = false;
        // Plano sobre el piso, como la linea del jefe.
        aro.alignment = LineAlignment.TransformZ;
        go.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
        aro.enabled = false;
    }

    private void ArmarLasMarcas()
    {
        if (materialRubi == null || mallaDeLaMarca == null) return;
        marcas = new Transform[marcados.Length];
        for (int i = 0; i < marcas.Length; i++)
        {
            var go = new GameObject("MarcaChaman");
            go.AddComponent<MeshFilter>().sharedMesh = mallaDeLaMarca;
            var render = go.AddComponent<MeshRenderer>();
            render.sharedMaterial = materialRubi;
            render.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            render.receiveShadows = false;
            go.transform.localScale = Vector3.one * ladoDeLaMarca;
            go.SetActive(false);
            marcas[i] = go.transform;
        }
    }
}
