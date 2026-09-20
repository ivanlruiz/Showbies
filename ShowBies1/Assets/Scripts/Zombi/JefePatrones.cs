using System.Collections.Generic;
using UnityEngine;

// El jefe con patrones propios, pedido de Ivan: antes era un zombi grande y lento, y la
// oleada 10 no se sentia como un evento. Alterna dos ataques, los dos con aviso:
//
// - CARGA: se frena, marca en el piso una linea roja hacia el jugador, ruge y embiste en
//   linea recta sin corregir. Se esquiva moviendose de costado durante el aviso. Embistiendo
//   pega mucho mas fuerte, y al terminar -haya chocado o no- **queda aturdido un rato**,
//   tambaleandose y sin atacar: esa es la ventana para castigarlo. Asi la carga es una
//   apuesta del jefe y no solo un golpe gratis.
// - INVOCACION: se frena, marca un anillo rojo alrededor y hace aparecer zombis normales
//   con sus mismos multiplicadores. En WaveMode cuentan en la oleada (SumarALaOleada), asi
//   no termina con ellos vivos.
//
// A la mitad de su vida entra en furia: ataca mas seguido e invoca mas. Los relojes van en
// tiempo escalado, asi la pausa los congela. Va en el prefab ZombiBOSS; el estado arranca
// de cero en cada aparicion, por el pool.
//
// **Sin RequireComponent(EnemyController)**, aunque lo necesite: FondoMenu le borra los
// scripts a los zombis que cruzan por detras del menu, y un RequireComponent no deja
// borrar el componente del que se depende. El EnemyController quedaba vivo sin Rigidbody
// y el jefe del fondo tiraba una excepcion por aparicion. Sin el atributo, el que falta
// se chequea en Update como cualquier otra referencia.
public class JefePatrones : MonoBehaviour, IMovimientoPropio
{
    [Header("Ritmo")]
    public float esperaInicial = 3f;
    public float cadaCuanto = 5f;
    [Tooltip("Ataca solo con el jugador a esta distancia: de mas lejos el aviso no se ve.")]
    public float distanciaParaAtacar = 15f;

    [Header("Carga")]
    public float avisoCarga = 0.9f;
    public float velocidadCarga = 16f;
    public float duracionCarga = 1f;
    public float anchoLinea = 1.4f;
    [Tooltip("Lo que multiplica su golpe mientras embiste.")]
    public float golpeDeLaCarga = 2.5f;
    [Tooltip("Lo que queda quieto y tambaleandose despues de embestir.")]
    public float duracionAturdido = 1.3f;

    [Header("Invocacion")]
    public GameObject invocado;
    public int cantidadInvocados = 4;
    [Tooltip("Cuantos invocados suyos pueden estar vivos a la vez.")]
    public int maxInvocadosVivos = 8;
    public float radioInvocacion = 3.5f;
    public float avisoInvocar = 0.8f;

    [Header("Furia a mitad de vida")]
    [Range(0f, 1f)] public float fraccionFuria = 0.5f;
    public float ritmoEnFuria = 0.65f;       // multiplica cadaCuanto
    public int invocadosExtraEnFuria = 2;

    [Header("Aviso")]
    public Material materialAviso;
    public Color colorAviso = new Color(1f, 0.2f, 0.15f, 0.85f);
    public AudioClip rugido;

    private enum Estado { Persiguiendo, AvisandoCarga, Cargando, AvisandoInvocar, Aturdido }

    private EnemyController zombi;
    private LineRenderer linea;
    private Estado estado;
    private float desde;
    private float proximoAtaque;
    private float aparecio;
    private bool tocaCarga;
    private bool enFuria;
    private int golpesAlCargar;
    private Quaternion rotacionAlAturdirse;
    private Vector3 direccion;
    private WaveManager oleadas;
    // Los que invoco, para no pasarse: un zombi muerto vuelve al pool y se prende de
    // nuevo, asi que se guarda con su numero de aparicion (ver EnemyController.SigueVivo).
    private readonly List<EnemyController> invocados = new List<EnemyController>();
    private readonly List<int> numerosInvocados = new List<int>();

    private void Awake()
    {
        zombi = GetComponent<EnemyController>();
        var go = new GameObject("AvisoJefe");
        linea = go.AddComponent<LineRenderer>();
        linea.useWorldSpace = true;
        linea.material = materialAviso;
        linea.startColor = linea.endColor = colorAviso;
        linea.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        linea.receiveShadows = false;
        // Plana sobre el piso y no mirando a la camara: con Alignment.View la cinta queda
        // parada y, con la camara desde arriba, medio enterrada en el piso.
        linea.alignment = LineAlignment.TransformZ;
        go.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
        linea.enabled = false;
    }

    private void OnEnable()
    {
        estado = Estado.Persiguiendo;
        invocados.Clear();
        numerosInvocados.Clear();
        aparecio = Time.time;
        proximoAtaque = Time.time + esperaInicial;
        tocaCarga = true;
        enFuria = false;
        if (linea != null) linea.enabled = false;
        oleadas = FindAnyObjectByType<WaveManager>();
    }

    private void OnDisable()
    {
        if (linea != null) linea.enabled = false;
        if (zombi != null) zombi.multiplicadorGolpe = 1f;
    }

    private void OnDestroy()
    {
        if (linea != null) Destroy(linea.gameObject);
    }

    public bool Mover(Rigidbody rb, Transform jugador)
    {
        switch (estado)
        {
            case Estado.Cargando:
            {
                Vector3 v = direccion * velocidadCarga;
                v.y = rb.linearVelocity.y;
                rb.linearVelocity = v;
                return true;
            }
            case Estado.Aturdido:
            {
                // Quieto y tambaleandose: se ve que esta expuesto.
                float balanceo = Mathf.Sin((Time.time - desde) * 22f) * 9f;
                transform.rotation = rotacionAlAturdirse * Quaternion.Euler(0f, 0f, balanceo);
                rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
                return true;
            }
            case Estado.AvisandoCarga:
            case Estado.AvisandoInvocar:
            {
                // Quieto, mirando hacia donde va a cargar (o al jugador, si invoca).
                Vector3 mirar = estado == Estado.AvisandoCarga ? transform.position + direccion : jugador.position;
                mirar.y = transform.position.y;
                if ((mirar - transform.position).sqrMagnitude > 0.0001f) transform.LookAt(mirar);
                rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
                return true;
            }
            default:
                return false;
        }
    }

    private void Update()
    {
        if (zombi == null || zombi.thePlayer == null) return;
        float ahora = Time.time;
        RevisarFuria(ahora);

        switch (estado)
        {
            case Estado.Persiguiendo:
                if (ahora >= proximoAtaque && CercaDelJugador()) Empezar(ahora);
                break;

            case Estado.AvisandoCarga:
                DibujarLinea(ahora);
                if (ahora - desde >= avisoCarga)
                {
                    estado = Estado.Cargando;
                    desde = ahora;
                    linea.enabled = false;
                    // Embistiendo pega mucho mas fuerte: el golpe lo sigue dando
                    // EnemyController, con su intervalo, asi no hay dos daños.
                    golpesAlCargar = zombi.GolpesDados;
                    zombi.multiplicadorGolpe = golpeDeLaCarga;
                    CamaraJugador.Temblar(0.3f);
                }
                break;

            case Estado.Cargando:
                // Choco: frena en seco, con mas temblor.
                if (zombi.GolpesDados > golpesAlCargar)
                {
                    CamaraJugador.Temblar(0.55f);
                    Aturdir(ahora);
                }
                else if (ahora - desde >= duracionCarga)
                {
                    Aturdir(ahora);
                }
                break;

            case Estado.Aturdido:
                if (ahora - desde >= duracionAturdido) Terminar(ahora);
                break;

            case Estado.AvisandoInvocar:
                DibujarAnillo(ahora);
                if (ahora - desde >= avisoInvocar)
                {
                    linea.enabled = false;
                    Invocar();
                    Terminar(ahora);
                }
                break;
        }
    }

    private void Empezar(float ahora)
    {
        desde = ahora;
        Vector3 hacia = zombi.thePlayer.transform.position - transform.position;
        hacia.y = 0f;
        direccion = hacia.sqrMagnitude > 0.01f ? hacia.normalized : transform.forward;
        estado = tocaCarga ? Estado.AvisandoCarga : Estado.AvisandoInvocar;
        linea.enabled = true;
        if (rugido != null) Sonidos.Tocar(rugido, 0.9f, tocaCarga ? 0.75f : 0.55f);
        CamaraJugador.Temblar(0.15f);
    }

    // Le corta el ataque que estaba por hacer y lo deja quieto un rato. Lo llama el
    // revivir: volver justo cuando el jefe termina de avisar la carga es morir de nuevo
    // sin poder hacer nada.
    public void Postergar(float segundos)
    {
        if (estado != Estado.Persiguiendo)
        {
            estado = Estado.Persiguiendo;
            if (linea != null) linea.enabled = false;
            if (zombi != null) zombi.multiplicadorGolpe = 1f;
        }
        proximoAtaque = Mathf.Max(proximoAtaque, Time.time + Mathf.Max(0f, segundos));
    }

    private bool CercaDelJugador()
    {
        Vector3 d = zombi.thePlayer.transform.position - transform.position;
        d.y = 0f;
        return d.sqrMagnitude <= distanciaParaAtacar * distanciaParaAtacar;
    }

    // Despues de embestir queda expuesto un rato, haya chocado o no: es la ventana para
    // pegarle, y lo que hace que esquivar valga la pena.
    private void Aturdir(float ahora)
    {
        estado = Estado.Aturdido;
        desde = ahora;
        zombi.multiplicadorGolpe = 1f;
        rotacionAlAturdirse = transform.rotation;
    }

    private void Terminar(float ahora)
    {
        estado = Estado.Persiguiendo;
        transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        zombi.multiplicadorGolpe = 1f;
        tocaCarga = !tocaCarga;
        proximoAtaque = ahora + cadaCuanto * (enFuria ? ritmoEnFuria : 1f);
    }

    private void RevisarFuria(float ahora)
    {
        // Un segundo despues de aparecer: quien lo hace aparecer le pone los
        // multiplicadores en el mismo frame, y preguntar la vida antes la fijaria sin ellos.
        if (enFuria || ahora - aparecio < 1f || zombi.VidaMaxima <= 0f) return;
        if (zombi.VidaActual / zombi.VidaMaxima > fraccionFuria) return;
        enFuria = true;
        CamaraJugador.Temblar(0.45f);
        if (rugido != null) Sonidos.Tocar(rugido, 1f, 0.45f);
    }

    // Cuantos de los suyos siguen vivos, limpiando la lista de paso.
    private int InvocadosVivos()
    {
        int vivos = 0;
        for (int i = invocados.Count - 1; i >= 0; i--)
        {
            if (EnemyController.SigueVivo(invocados[i], numerosInvocados[i])) { vivos++; continue; }
            invocados.RemoveAt(i);
            numerosInvocados.RemoveAt(i);
        }
        return vivos;
    }

    private void Invocar()
    {
        if (invocado == null) return;
        int n = cantidadInvocados + (enFuria ? invocadosExtraEnFuria : 0);
        // Ni mas de los suyos de los que se banca, ni mas de los que entran en la escena:
        // el techo lo fija el generador (60, o 35 en movil) y sin mirarlo el modo libre
        // junta una pantalla de zombis y el telefono se traba.
        n = Mathf.Min(n, Mathf.Max(0, maxInvocadosVivos - InvocadosVivos()));
        n = Mathf.Min(n, EnemyController.LugarParaZombis);
        if (n <= 0) return;

        for (int i = 0; i < n; i++)
        {
            float angulo = i * Mathf.PI * 2f / n;
            Vector3 punto = transform.position + new Vector3(Mathf.Cos(angulo), 0f, Mathf.Sin(angulo)) * radioInvocacion;
            var nuevo = EnemyController.Aparecer(invocado, punto);
            if (nuevo == null) continue;
            invocados.Add(nuevo);
            numerosInvocados.Add(nuevo.NumeroDeAparicion);
            // Los mismos multiplicadores que el jefe: son zombis de esta oleada.
            nuevo.multiplicadorVida = zombi.multiplicadorVida;
            nuevo.multiplicadorDano = zombi.multiplicadorDano;
            nuevo.multiplicadorMonedas = zombi.multiplicadorMonedas;
            nuevo.monedaPrefab = zombi.monedaPrefab;
            if (oleadas != null) oleadas.SumarALaOleada(nuevo);
            Efectos.Caja(punto);
        }
        CamaraJugador.Temblar(0.35f);
    }

    // La linea de la carga en el piso, del jefe hasta donde llega, titilando.
    private void DibujarLinea(float ahora)
    {
        float titileo = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin((ahora - desde) * 14f));
        Color c = colorAviso;
        c.a *= titileo;
        linea.startColor = linea.endColor = c;
        linea.widthMultiplier = anchoLinea;
        linea.loop = false;
        linea.positionCount = 2;
        Vector3 desdeAca = transform.position;
        desdeAca.y = 0.06f;
        linea.SetPosition(0, desdeAca);
        linea.SetPosition(1, desdeAca + direccion * velocidadCarga * duracionCarga);
    }

    // El anillo de la invocacion, que se achica hasta el radio donde van a salir.
    private void DibujarAnillo(float ahora)
    {
        const int Puntos = 40;
        float t = Mathf.Clamp01((ahora - desde) / Mathf.Max(0.01f, avisoInvocar));
        float radio = Mathf.Lerp(radioInvocacion * 2f, radioInvocacion, t);
        linea.startColor = linea.endColor = colorAviso;
        linea.widthMultiplier = 0.35f;
        linea.loop = true;
        linea.positionCount = Puntos;
        Vector3 centro = transform.position;
        centro.y = 0.06f;
        for (int i = 0; i < Puntos; i++)
        {
            float a = i * Mathf.PI * 2f / Puntos;
            linea.SetPosition(i, centro + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radio);
        }
    }
}
