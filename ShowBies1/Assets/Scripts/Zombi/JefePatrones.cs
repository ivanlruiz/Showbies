using System.Collections.Generic;
using UnityEngine;

// El jefe con patrones propios, pedido de Ivan: antes era un zombi grande y lento, y la
// oleada 10 no se sentia como un evento. Alterna dos ataques, los dos con aviso:
//
// - CARGA: se frena, marca en el piso una linea roja hacia el jugador, del ancho de su
//   cuerpo, ruge y embiste en linea recta sin corregir. Se esquiva moviendose de costado
//   durante el aviso. Embistiendo pega mucho mas fuerte, y al terminar -haya chocado o no-
//   **queda aturdido un rato**, tambaleandose y sin atacar: esa es la ventana para
//   castigarlo. Asi la carga es una apuesta del jefe y no solo un golpe gratis.
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
    [Tooltip("Y solo con su centro en pantalla, a este margen de los costados (x) y de arriba y abajo (y), en fracciones de la pantalla: asi se ven el jefe y su pose, y no solo la linea entrando por el borde.")]
    public Vector2 margenEnPantalla = new Vector2(0.08f, 0.1f);

    [Header("Carga")]
    public float avisoCarga = 0.9f;
    public float velocidadCarga = 16f;
    public float duracionCarga = 1f;
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

    [Header("Pose")]
    [Tooltip("Cuanto se agacha y se echa atras mientras avisa la carga, en grados.")]
    public float gradosAlAgazaparse = 20f;
    [Tooltip("Cuanto se inclina hacia adelante mientras embiste, en grados.")]
    public float gradosAlEmbestir = 24f;
    [Tooltip("Cuanto se tambalea de lado a lado aturdido, en grados.")]
    public float gradosAlTambalearse = 16f;
    public float vaivenDelTambaleo = 3.2f;     // veces por segundo
    [Tooltip("Cuanto se arquea hacia atras al invocar, en grados.")]
    public float gradosAlInvocar = 26f;
    [Tooltip("Cuanto sube o baja el cuerpo, como fraccion de su alto.")]
    public float fraccionQueSeAgacha = 0.14f;
    [Tooltip("Lo que tarda en llegar a la pose. Bajo es seco; alto, blando.")]
    public float suavidadDeLaPose = 12f;

    [Header("Paso")]
    [Tooltip("Lo que avanza el ciclo de correr (Z_run_rm) con el modelo a escala 1, en m/s. Medido del FBX: la raiz avanza 2 m en los 0,67 s del ciclo, con los pies apoyados. El paso de la embestida sale de aca, de velocidadCarga y de la escala del modelo, asi los pies no patinan; si se cambia el clip, hay que volver a medirlo. Mas alto da pasos mas lentos.")]
    public float velocidadDelClipDeCorrer = 3f;

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
    private Vector3 direccion;
    private WaveManager oleadas;
    private Camera camara;

    // Lo que barre la embestida, medido de la capsula en Awake (ver MedirElCuerpo): el
    // radio de su cuerpo y lo que asoma su frente por delante del centro. Sin capsula
    // quedan la linea de 1,4 m de antes y el centro.
    private float radioDelCuerpo = 0.7f;
    private float frenteDelCuerpo;

    // Los invocados no salen a menos de esto de una pared (ver PuntoDelAnillo): el radio
    // de un zombi normal (0,36 m) y aire.
    private const float MargenContraLasParedes = 1f;
    private static readonly RaycastHit[] golpesContraLasParedes = new RaycastHit[8];

    // El paso de las piernas en cada patron, con los parametros Paso y Ritmo del estado
    // Andar (ver ConstructorAnimaciones). Hasta el 24/9 no se tocaban: al "frenarse" para
    // avisar, invocar o aturdido caminaba en el lugar, y embestia a 16 m/s -ocho veces lo
    // que camina- con el paso lento de caminar, patinando. Los valores propios del zombi
    // (velocidadDeAnimacion y ritmoDeAndar de su EnemyController) se leen del Animator al
    // pisarlos y se le devuelven al volver a perseguir.
    private static readonly int IdPaso = Animator.StringToHash("Paso");
    private static readonly int IdRitmo = Animator.StringToHash("Ritmo");
    private Animator animador;
    private float escalaDelModelo = 1f;
    private bool pasoPisado;
    private float pasoPropio, ritmoPropio;

    // La pose va sobre el modelo (el hijo con el Animator) y no sobre la raiz: la
    // raiz la maneja EnemyController -mira al jugador en cada paso de fisica y le
    // aplasta la escala al recibir un tiro- y escribirle encima se pelearia con eso.
    // Se aplica en LateUpdate, despues de que el Animator escribio los huesos, que
    // es como se hace cualquier pose por codigo encima de un clip.
    //
    // No hay clips de agazaparse, embestir ni tambalearse: el pack trae correr,
    // pegar, morir, quieto y caminar. Esto es lo que se puede hacer sin modelar, y
    // alcanza porque lo que hay que leer de un vistazo es la silueta.
    private Transform modelo;
    private Vector3 posicionBaseDelModelo;
    private Quaternion rotacionBaseDelModelo;
    private float altoDelModelo = 1f;
    private float inclinacion, balanceo, altura;
    // Los que invoco, para no pasarse: un zombi muerto vuelve al pool y se prende de
    // nuevo, asi que se guarda con su numero de aparicion (ver EnemyController.SigueVivo).
    private readonly List<EnemyController> invocados = new List<EnemyController>();
    private readonly List<int> numerosInvocados = new List<int>();

    private void Awake()
    {
        zombi = GetComponent<EnemyController>();

        // El hijo que se ve: el del Animator con controller. Su transform local esta
        // libre porque los prefabs tienen Apply Root Motion apagado.
        foreach (var candidato in GetComponentsInChildren<Animator>(true))
        {
            if (candidato.runtimeAnimatorController == null || candidato.transform == transform) continue;
            animador = candidato;
            modelo = candidato.transform;
            break;
        }
        if (modelo != null)
        {
            posicionBaseDelModelo = modelo.localPosition;
            rotacionBaseDelModelo = modelo.localRotation;
            // El alto en unidades locales del modelo, para que agacharse se vea igual
            // con cualquier escala. Sale de los renderers y no de un numero a mano.
            var renderers = modelo.GetComponentsInChildren<Renderer>(true);
            float alto = 0f;
            foreach (var r in renderers) alto = Mathf.Max(alto, r.bounds.size.y);
            float escala = Mathf.Abs(modelo.lossyScale.y);
            altoDelModelo = escala > 0.0001f ? Mathf.Max(0.1f, alto / escala) : 1f;
            // Lo que mide el modelo en el mundo (el jefe: 2 x 1,2), para el paso al embestir.
            escalaDelModelo = Mathf.Max(0.01f, Mathf.Abs(modelo.lossyScale.z));
        }

        var capsula = GetComponent<CapsuleCollider>();
        if (capsula != null) MedirElCuerpo(capsula, transform.lossyScale, out radioDelCuerpo, out frenteDelCuerpo);

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
        // El Animator ya tiene el paso propio: EnemyController se lo pone en cada aparicion.
        pasoPisado = false;
        if (linea != null) linea.enabled = false;
        inclinacion = balanceo = altura = 0f;
        if (modelo != null) modelo.SetLocalPositionAndRotation(posicionBaseDelModelo, rotacionBaseDelModelo);
        oleadas = FindAnyObjectByType<WaveManager>();
        camara = Camera.main;
    }

    private void OnDisable()
    {
        if (linea != null) linea.enabled = false;
        // Que no se lo lleve al pool torcido: la aparicion siguiente sale de aca.
        inclinacion = balanceo = altura = 0f;
        if (modelo != null) modelo.SetLocalPositionAndRotation(posicionBaseDelModelo, rotacionBaseDelModelo);
        // El paso no se devuelve aca: el Animator se esta apagando, y la aparicion
        // siguiente arranca con el de EnemyController.
        pasoPisado = false;
        if (zombi != null)
        {
            zombi.multiplicadorGolpe = 1f;
            zombi.golpeaAlChocar = false;
            zombi.puedeZarpar = true;
        }
    }

    private void OnDestroy()
    {
        if (linea != null) Destroy(linea.gameObject);
    }

    // La pose de cada patron, encima del ciclo de correr. Va en LateUpdate porque el
    // Animator escribe los huesos en el paso de animacion y lo que se ponga antes se
    // pierde. Sin tiempo escalado no seria: la pausa tiene que congelarla como a todo
    // lo demas.
    private void LateUpdate()
    {
        // Festejando, la pose la pone EnemyController.
        if (modelo == null || DerrotaEnLaPartida.Activa) return;

        float inclinacionQueVa = 0f, balanceoQueVa = 0f, alturaQueVa = 0f;
        if (zombi != null && zombi.Vivo)
        {
            switch (estado)
            {
                // Se agazapa: baja el cuerpo y se echa atras, como el que toma carrera.
                case Estado.AvisandoCarga:
                {
                    float cuanto = avisoCarga > 0f ? Mathf.Clamp01((Time.time - desde) / avisoCarga) : 1f;
                    inclinacionQueVa = -gradosAlAgazaparse * cuanto;
                    alturaQueVa = -fraccionQueSeAgacha * altoDelModelo * cuanto;
                    break;
                }

                // Embiste echado hacia adelante, que es lo que dice "no me pares".
                case Estado.Cargando:
                    inclinacionQueVa = gradosAlEmbestir;
                    alturaQueVa = -fraccionQueSeAgacha * altoDelModelo * 0.4f;
                    break;

                // Aturdido se tambalea de lado a lado, cada vez menos: es la ventana
                // para castigarlo y hasta ahora no se leia en ninguna parte.
                case Estado.Aturdido:
                {
                    float queda = duracionAturdido > 0f
                        ? Mathf.Clamp01(1f - (Time.time - desde) / duracionAturdido) : 1f;
                    balanceoQueVa = gradosAlTambalearse * queda
                                    * Mathf.Sin((Time.time - desde) * vaivenDelTambaleo * Mathf.PI * 2f);
                    inclinacionQueVa = gradosAlEmbestir * 0.35f * queda;
                    alturaQueVa = -fraccionQueSeAgacha * altoDelModelo * 0.3f * queda;
                    break;
                }

                // Invocando se arquea hacia atras y se estira hacia arriba.
                case Estado.AvisandoInvocar:
                {
                    float cuanto = avisoInvocar > 0f ? Mathf.Clamp01((Time.time - desde) / avisoInvocar) : 1f;
                    inclinacionQueVa = -gradosAlInvocar * cuanto;
                    alturaQueVa = fraccionQueSeAgacha * altoDelModelo * 0.5f * cuanto;
                    break;
                }
            }
        }

        // Suavizado independiente de los FPS: en un telefono a 30 y en el editor a 200
        // tarda lo mismo en llegar a la pose.
        float paso = 1f - Mathf.Exp(-suavidadDeLaPose * Time.deltaTime);
        inclinacion = Mathf.Lerp(inclinacion, inclinacionQueVa, paso);
        altura = Mathf.Lerp(altura, alturaQueVa, paso);
        // El tambaleo no se suaviza: es un vaiven y suavizarlo lo aplanaria.
        balanceo = balanceoQueVa;

        modelo.SetLocalPositionAndRotation(
            posicionBaseDelModelo + Vector3.up * altura,
            rotacionBaseDelModelo * Quaternion.Euler(inclinacion, 0f, balanceo));
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
                // Quieto y con el rumbo con el que termino la carga: devolviendo verdadero,
                // EnemyController no lo gira. El tambaleo es solo del modelo (LateUpdate):
                // hasta el 24/9 la raiz tambien se balanceaba aca, a paso de fisica y sin
                // decaer, y los dos vaivenes sumados daban un temblor irregular que ademas
                // ladeaba la capsula y las hitboxes.
                rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
                return true;
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

        // Con el jugador muerto festeja con la horda (EnemyController), y lo que quedaba
        // a medias (el aviso de la carga, la linea roja) se corta. Tambien el paso: con
        // las piernas quietas de un aviso iba patinando hasta su lugar del festejo.
        if (DerrotaEnLaPartida.Activa)
        {
            if (estado != Estado.Persiguiendo) VolverAPerseguir();
            return;
        }

        // Un jefe muerto no invoca ni embiste. Su objeto se queda prendido mientras
        // se desploma (ver EnemyController.Morir), y sin esto el cadaver seguia
        // dibujando la linea de la carga y sacando invocados un segundo y medio
        // despues de que la barra de arriba ya habia llegado a cero.
        if (!zombi.Vivo)
        {
            if (estado != Estado.Persiguiendo) VolverAPerseguir();
            return;
        }
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
                    // Sin un zarpazo a medias ni el intervalo de uno de antes: el unico
                    // golpe que cuenta embistiendo es el del cuerpo (ver EmpezarEmbestida).
                    zombi.EmpezarEmbestida();
                    // Embistiendo pega mucho mas fuerte: el golpe lo sigue dando
                    // EnemyController, con su intervalo, asi no hay dos daños.
                    golpesAlCargar = zombi.GolpesDados;
                    zombi.multiplicadorGolpe = golpeDeLaCarga;
                    // Embistiendo pega con el cuerpo y en el acto, no con un zarpazo:
                    // a 16 m/s, esperar a que baje el brazo lo dejaria pasar de largo.
                    zombi.golpeaAlChocar = true;
                    // Y corre al ritmo de lo que avanza.
                    PisarElPaso(PasoParaCorrer(velocidadCarga, velocidadDelClipDeCorrer, escalaDelModelo), 1f);
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
        // Desde el aviso hasta volver a perseguir no tira zarpazos (ver
        // EnemyController.puedeZarpar), y se frena tambien de piernas.
        zombi.puedeZarpar = false;
        Frenar();
        if (rugido != null) Sonidos.Tocar(rugido, 0.9f, tocaCarga ? 0.75f : 0.55f);
        CamaraJugador.Temblar(0.15f);
    }

    // Le corta el ataque que estaba por hacer y lo deja quieto un rato. Lo llama el
    // revivir: volver justo cuando el jefe termina de avisar la carga es morir de nuevo
    // sin poder hacer nada.
    public void Postergar(float segundos)
    {
        if (estado != Estado.Persiguiendo) VolverAPerseguir();
        proximoAtaque = Mathf.Max(proximoAtaque, Time.time + Mathf.Max(0f, segundos));
    }

    // Todas las salidas a perseguir pasan por aca -terminar un patron, el revivir, la
    // derrota y la muerte del jefe-, asi no queda nada de lo que cambia cada patron: la
    // linea, el golpe de la embestida, los zarpazos cortados y el paso.
    private void VolverAPerseguir()
    {
        estado = Estado.Persiguiendo;
        if (linea != null) linea.enabled = false;
        if (zombi != null)
        {
            zombi.multiplicadorGolpe = 1f;
            zombi.golpeaAlChocar = false;
            zombi.puedeZarpar = true;
        }
        DevolverElPaso();
    }

    private bool CercaDelJugador()
    {
        // A un muerto no lo ataca: la partida sigue andando detras de la derrota, y sin
        // esto el jefe seguia embistiendo el cuerpo y rugiendo cada pocos segundos
        // encima de la pantalla. Camina, nada mas.
        if (PlayerHealth.instance != null && PlayerHealth.instance.EstaMuerto) return false;

        Vector3 d = zombi.thePlayer.transform.position - transform.position;
        d.y = 0f;
        if (d.sqrMagnitude > distanciaParaAtacar * distanciaParaAtacar) return false;
        return EnPantalla();
    }

    // Si se lo ve. La distancia sola no alcanzaba: la camara del juego ve unos 6 m por
    // detras del jugador y 10 por delante, y con el jefe a 7-15 m por debajo -que es lo
    // normal al escaparle- sonaba el rugido y la linea roja entraba por el borde sin que
    // se viera quien embestia. Fuera de cuadro sigue persiguiendo con el ataque vencido,
    // asi que ataca apenas entra: el ritmo no cambia. Sin camara vale la distancia sola.
    private bool EnPantalla()
    {
        if (camara == null) camara = Camera.main;
        if (camara == null) return true;
        return DentroDelCuadro(camara.WorldToViewportPoint(transform.position), margenEnPantalla);
    }

    // Un punto en coordenadas de la pantalla (0 a 1, y z la distancia) que esta delante de
    // la camara y a margen de los bordes. Estatica para probarla con la camara del juego.
    public static bool DentroDelCuadro(Vector3 enPantalla, Vector2 margen)
    {
        return enPantalla.z > 0f
            && enPantalla.x >= margen.x && enPantalla.x <= 1f - margen.x
            && enPantalla.y >= margen.y && enPantalla.y <= 1f - margen.y;
    }

    // Despues de embestir queda expuesto un rato, haya chocado o no: es la ventana para
    // pegarle, y lo que hace que esquivar valga la pena.
    private void Aturdir(float ahora)
    {
        estado = Estado.Aturdido;
        desde = ahora;
        zombi.multiplicadorGolpe = 1f;
        zombi.golpeaAlChocar = false;
        // Sin atacar: ni el cuerpo ni un zarpazo. Hasta el 24/9 el que quedaba pegado al
        // jefe se comia zarpazos en plena ventana para castigarlo, con el brazo pegando
        // encima del tambaleo. Y con las piernas quietas.
        zombi.puedeZarpar = false;
        Frenar();
    }

    // No lo gira: la raiz la gira EnemyController, mirando al jugador en el paso de fisica
    // siguiente. Hasta el 23/9 le aplicaba un rumbo guardado, y saliendo de invocar (que
    // era el de la carga anterior) el jefe pegaba un salto de giro.
    private void Terminar(float ahora)
    {
        VolverAPerseguir();
        tocaCarga = !tocaCarga;
        proximoAtaque = ahora + cadaCuanto * (enFuria ? ritmoEnFuria : 1f);
    }

    // Si ya entro en furia. La barra de arriba (BarraDelJefe) late con esto y pone su
    // muesca en fraccionFuria: asi dice la verdad aunque se balancee la furia en el prefab.
    public bool EnFuria => enFuria;

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

        // En el piso, donde se dibujo el anillo, y no a la altura del centro del jefe (2 m):
        // nacian con los pies a metro y medio y caian, lejos del anillo. SubirSobreElPiso
        // los para encima, como a los de los puntos de aparicion.
        Vector3 centro = transform.position;
        centro.y = 0f;
        for (int i = 0; i < n; i++)
        {
            float angulo = i * Mathf.PI * 2f / n;
            Vector3 punto = PuntoDelAnillo(centro, new Vector3(Mathf.Cos(angulo), 0f, Mathf.Sin(angulo)));
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
            // Del cuerpo del que sale, ya parado en el piso: en el punto del anillo (y = 0)
            // la mitad de las chispas quedaria bajo el piso. Las chispas de una caja, sin
            // su pop: el sonido de agarrar algo bueno anunciaba enemigos.
            Efectos.Invocado(nuevo.transform.position);
        }
        CamaraJugador.Temblar(0.35f);
    }

    // Donde sale un invocado: en el anillo y sin quedar dentro o detras de una pared. Con
    // el jefe contra el borde del mapa (la carga recorre 16 m, y seguido termina ahi) parte
    // del anillo caia fuera, y esos se iban por el kill-Z, contados muertos y sin monedas.
    // El que no entra de su lado sale del de enfrente, que da al mapa: acortarlo contra la
    // pared lo dejaba metido en el cuerpo del jefe, y la fisica lo empujaba contra la pared.
    private Vector3 PuntoDelAnillo(Vector3 centro, Vector3 hacia)
    {
        float radio = RadioLibre(hacia);
        if (radio < radioDelCuerpo + MargenContraLasParedes)
        {
            float delOtroLado = RadioLibre(-hacia);
            if (delOtroLado > radio)
            {
                hacia = -hacia;
                radio = delOtroLado;
            }
        }
        return centro + hacia * radio;
    }

    // Hasta donde entra un invocado en esa direccion: el anillo, o MargenContraLasParedes
    // antes de lo primero fijo que haya (las paredes invisibles del borde). Solo cuentan
    // los colliders fijos, como en Moneda: ni un zombi ni el jugador achican el anillo, y
    // las cajas son triggers.
    private float RadioLibre(Vector3 hacia)
    {
        float alcance = radioInvocacion + MargenContraLasParedes;
        int cantidad = Physics.RaycastNonAlloc(transform.position, hacia, golpesContraLasParedes, alcance, ~0, QueryTriggerInteraction.Ignore);
        float libre = alcance;
        for (int i = 0; i < cantidad; i++)
        {
            Collider golpeado = golpesContraLasParedes[i].collider;
            if (golpeado.attachedRigidbody != null || golpeado.GetComponentInParent<BulletController>() != null) continue;
            libre = Mathf.Min(libre, golpesContraLasParedes[i].distance);
        }
        return Mathf.Clamp(libre - MargenContraLasParedes, 0f, radioInvocacion);
    }

    // La linea de la carga en el piso, del jefe hasta donde llega, titilando. Es la franja
    // que barre su cuerpo: tan ancha como el y hasta donde llega su frente al final de la
    // carga, asi "no pisar lo rojo" es exactamente no comerse el golpe. Hasta el 24/9 media
    // 1,4 m fijos y el cuerpo barre casi 3 (la capsula tiene 1,44 m de radio): el que se
    // corria medio metro afuera de lo rojo se comia igual el golpe mas fuerte del juego.
    private void DibujarLinea(float ahora)
    {
        float titileo = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin((ahora - desde) * 14f));
        Color c = colorAviso;
        c.a *= titileo;
        linea.startColor = linea.endColor = c;
        linea.widthMultiplier = 2f * radioDelCuerpo;
        linea.loop = false;
        linea.positionCount = 2;
        Vector3 desdeAca = transform.position;
        desdeAca.y = 0.06f;
        linea.SetPosition(0, desdeAca);
        linea.SetPosition(1, desdeAca + direccion * (velocidadCarga * duracionCarga + frenteDelCuerpo));
    }

    // El cuerpo del jefe en el piso, de su capsula (vertical): el radio con la escala y lo
    // que asoma su frente por delante del centro (la capsula esta corrida hacia adelante).
    // Las hitboxes quedan adentro. Estatica para probarla con el prefab.
    public static void MedirElCuerpo(CapsuleCollider capsula, Vector3 escala, out float radio, out float frente)
    {
        radio = capsula.radius * Mathf.Max(Mathf.Abs(escala.x), Mathf.Abs(escala.z));
        frente = capsula.center.z * Mathf.Abs(escala.z) + radio;
    }

    // El Paso con el que el ciclo de correr avanza a esa velocidad, sin que los pies
    // patinen: el clip avanza velocidadDelClip por unidad de escala del modelo. El jefe
    // (2 x 1,2 = 2,4) a 16 m/s: 2,2.
    public static float PasoParaCorrer(float velocidad, float velocidadDelClip, float escalaDelModelo)
    {
        return velocidad / Mathf.Max(0.01f, velocidadDelClip * escalaDelModelo);
    }

    // Quieto: un cuadro del ciclo de caminar (Ritmo 0: caminando siempre hay un pie en el
    // piso, y corriendo hay cuadros con los dos en el aire). La pose de cada patron va
    // encima, en LateUpdate.
    private void Frenar()
    {
        PisarElPaso(0f, 0f);
    }

    private void PisarElPaso(float paso, float ritmo)
    {
        if (animador == null || !animador.isActiveAndEnabled) return;
        if (!pasoPisado)
        {
            pasoPropio = animador.GetFloat(IdPaso);
            ritmoPropio = animador.GetFloat(IdRitmo);
            pasoPisado = true;
        }
        animador.SetFloat(IdPaso, paso);
        animador.SetFloat(IdRitmo, ritmo);
    }

    private void DevolverElPaso()
    {
        if (!pasoPisado) return;
        pasoPisado = false;
        if (animador == null || !animador.isActiveAndEnabled) return;
        animador.SetFloat(IdPaso, pasoPropio);
        animador.SetFloat(IdRitmo, ritmoPropio);
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
