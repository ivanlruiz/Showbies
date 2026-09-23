using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public Transitions trans;

    [Header("Movement Settings")]
    public float moveSpeed = 8f;

    // Lo sube la furia mientras dura. No se serializa: es estado de la partida.
    [System.NonSerialized] public float multiplicadorVelocidad = 1f;
    private Rigidbody myRigidbody;

    [Header("Input Settings")]
    private Vector3 moveInput;
    private Vector3 moveVelocity;

    [Header("Camera Settings")]
    private Camera mainCamera;

    [Header("Gun Settings")]
    public GunController theGun;
    public Granade granadaPrefab;

    [Header("Ammo Settings")]
    public int cantBalas = 0;
    public int maxBalas = 500;
    public int cargadorMejorado = 1000;   // a lo que sube maxBalas al agarrar un PUArma

    [Header("Cajas de cadencia")]
    public float multiplicadorCadenciaPUBalas = 1.5f;   // 20 tiros/s de base pasan a 30
    public float multiplicadorCadenciaPUArma = 3f;      // 20 pasan a 60; con la cadencia al tope, 108

    [Header("Granade Settings")]
    public float granadaCooldown = 5f;
    public float distanciaMinimaGranada = 3f;    // para que no caiga a los pies del jugador
    public float distanciaMaximaGranada = 12f;
    public float distanciaGranadaMovil = 8f;     // el toque rapido del boton G: sin arrastrar no hay distancia elegida
    public Color colorPunteroGranada = new Color(1f, 1f, 1f, 0.6f);   // el anillo mientras se apunta; en vuelo usa el del prefab

    private float granadaDisponibleEn;
    private LineRenderer punteroGranada;

    // Si se puede tirar granadas: la fija AplicarMejoras con la compra de la tienda
    // (y el tutorial la prende). Arranca en verdadero para que una escena sin
    // AplicarMejoras siga teniendo granada.
    public bool GranadaDesbloqueada { get; set; } = true;

    // Segundos que faltan para poder tirar otra granada. Lo muestra el boton de granada.
    public float GranadaRestante { get { return Mathf.Max(0f, granadaDisponibleEn - Time.time); } }

    private void Start()
    {
        myRigidbody = GetComponent<Rigidbody>();

        // Camera.main y no FindObjectOfType: FindObjectOfType esta deprecado en
        // Unity 6 y Camera.main esta cacheado por el engine desde 2020.2.
        mainCamera = Camera.main;

        // El anillo que marca donde va a caer mientras se apunta: una copia del de
        // la granada, en otro color.
        if (granadaPrefab != null && granadaPrefab.Indicador != null)
        {
            punteroGranada = Instantiate(granadaPrefab.Indicador);
            punteroGranada.startColor = colorPunteroGranada;
            punteroGranada.endColor = colorPunteroGranada;
            punteroGranada.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (MenuPausa.JuegoCongelado)
        {
            // Con el juego congelado no se dispara, y la animacion tampoco queda
            // disparando.
            FijarDisparo(false);
            OcultarPunteroGranada();
            return;
        }

        // En móvil el input lo maneja PlayerJS con los joysticks. Si además
        // corriera esto, los dos se pelearían por moveVelocity y por isFiring.
        if (Plataforma.EsMovil) return;

        HandleMovement();
        HandleCamera();
        HandleShooting();
    }

    // La entrada del joystick de movimiento. La llama PlayerJS.
    public void Move(Vector2 input)
    {
        moveInput = new Vector3(input.x, 0f, input.y);
        moveVelocity = moveInput * moveSpeed * multiplicadorVelocidad;

        if (trans != null && trans.anim != null)
        {
            trans.anim.SetBool("run", moveInput.magnitude > 0.1f);
        }

        if (moveInput.magnitude > 0.1f)
        {
            transform.rotation = Quaternion.LookRotation(moveInput);
        }
    }

    private void FixedUpdate()
    {
        myRigidbody.linearVelocity = moveVelocity;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Muerto no agarra cajas: la partida sigue andando detras de la derrota, y este
        // componente queda apagado pero los triggers le llegan igual.
        if (PlayerHealth.instance != null && PlayerHealth.instance.EstaMuerto) return;

        // El jugador tiene dos colliders: el mismo pickup dispara este evento dos
        // veces en el mismo paso de fisica. Aca los efectos son absolutos y no se
        // notaba, pero es el mismo agujero que la cura doble de PlayerHealth.
        if (!other.gameObject.activeSelf) return;

        // Las tags coinciden con los nombres de los prefabs. Antes el pickup de
        // municion llevaba la tag "Balas" (que sonaba a las balas del arma) y el
        // de arma llevaba "pwBalas": estaban cruzadas con lo que hacian.
        if (other.gameObject.CompareTag("PUBalas"))
        {
            other.gameObject.SetActive(false);
            Destroy(other.gameObject);
            cantBalas = maxBalas;
            // Multiplica la cadencia de la mejora en vez de fijar un tiempo entre
            // tiros: con la cadencia comprada, una caja nunca te deja más lento.
            theGun.PotenciarCadencia(multiplicadorCadenciaPUBalas);
            Efectos.Caja(other.transform.position);
        }
        else if (other.gameObject.CompareTag("PUArma"))
        {
            other.gameObject.SetActive(false);
            Destroy(other.gameObject);

            // El pickup de arma mejora el arma de verdad: agranda el cargador en
            // forma PERMANENTE y lo llena. Antes cargaba 1000 sin tocar maxBalas
            // y el contador quedaba mostrando "1000/500". La cadencia si expira.
            maxBalas = Mathf.Max(maxBalas, cargadorMejorado);
            cantBalas = maxBalas;
            theGun.PotenciarCadencia(multiplicadorCadenciaPUArma);
            Efectos.Caja(other.transform.position);
        }
    }

    private void HandleMovement()
    {
        moveInput = new Vector3(Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical"));
        moveVelocity = moveInput * moveSpeed * multiplicadorVelocidad;

        if (moveInput.magnitude > 0.1f)
        {
            trans.anim.SetBool("run", true);
        }
        else if (moveInput.magnitude < 0.1f)
        {
            trans.anim.SetBool("run", false);
        }
    }

    private void HandleCamera()
    {
        Ray cameraRay = mainCamera.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        float rayLength;

        if (groundPlane.Raycast(cameraRay, out rayLength))
        {
            Vector3 pointToLook = cameraRay.GetPoint(rayLength);
            Debug.DrawLine(cameraRay.origin, pointToLook, Color.blue);

            transform.LookAt(new Vector3(pointToLook.x, transform.position.y, pointToLook.z));
        }
    }

    private void HandleShooting()
    {
        // Espacio: mantenerlo marca en el piso donde va a caer (bajo el mouse) y
        // soltarlo la tira.
        if (Input.GetKey(KeyCode.Space))
        {
            if (GranadaLista) MostrarPunteroGranada(DestinoGranada());
            else OcultarPunteroGranada();
        }
        if (Input.GetKeyUp(KeyCode.Space))
        {
            ThrowGranade();
        }

        // Mientras se mantenga apretado, en cada cuadro: con GetMouseButtonDown y la
        // condicion de tener balas, apretar sin balas y agarrar una caja sin soltar no
        // disparaba hasta volver a apretar.
        FijarDisparo(Input.GetMouseButton(0));
    }

    // Querer disparar, desde las dos plataformas: el clic en PC y el joystick de disparo
    // en movil (PlayerJS). Prende el arma y la animacion juntas. Hasta el 23/9 la
    // animacion solo la tocaba el camino de PC, y en el telefono el muñeco no disparaba
    // nunca aunque salieran balas. Las balas las decide el arma (GunController), que no
    // tira sin municion; la animacion, lo mismo.
    public void FijarDisparo(bool quiere)
    {
        if (theGun != null) theGun.isFiring = quiere;
        if (trans != null && trans.anim != null) trans.anim.SetBool("shoot", quiere && cantBalas > 0);
    }

    // Al morir: quieto y sin disparar, tambien en la animacion. La partida sigue andando
    // detras de la derrota y el cuerpo se ve: el que moria caminando seguia corriendo en
    // el lugar, porque nadie volvia a apagar "run".
    public void Soltar()
    {
        moveInput = Vector3.zero;
        moveVelocity = Vector3.zero;
        FijarDisparo(false);
        if (trans != null && trans.anim != null) trans.anim.SetBool("run", false);
    }

    // Se desploma con la animacion de morir del pack (pedido de Ivan: quedaba parado detras
    // de la derrota mientras la horda festejaba). Lo llama PlayerHealth en el golpe que
    // mata, antes de la oferta de revivir: el Animator pasa a tiempo sin escalar para que
    // la caida se vea aunque la oferta congele el juego. Levantarse lo deshace.
    private AnimatorUpdateMode modoAntesDeCaer;
    private bool caido;

    public void Caer()
    {
        Soltar();
        if (caido || trans == null || trans.anim == null) return;
        caido = true;
        modoAntesDeCaer = trans.anim.updateMode;
        trans.anim.updateMode = AnimatorUpdateMode.UnscaledTime;
        trans.anim.SetBool("muerto", true);
        // Que se vea: la derrota y la ventanita de revivir van al centro de la pantalla,
        // justo donde cae. La camara lo corre a un costado.
        CamaraJugador.MostrarElCuerpo(true);
    }

    // Al revivir: de pie otra vez, y el Animator vuelve al tiempo del juego.
    public void Levantarse()
    {
        if (!caido || trans == null || trans.anim == null) return;
        caido = false;
        trans.anim.SetBool("muerto", false);
        trans.anim.updateMode = modoAntesDeCaer;
        CamaraJugador.MostrarElCuerpo(false);
    }

    // Publico porque en movil lo llama el boton de granada del Canvas (Espacio no
    // existe ahi). El cooldown vive aca adentro, asi que el boton no puede spamear.
    public void ThrowGranade()
    {
        // El cooldown de 5 segundos vivía en Granade, sobre la instancia recién
        // creada, así que no limitaba nada: se podían tirar granadas por frame.
        // Va acá, que es donde está el input.
        LanzarGranadaA(DestinoGranada());
    }

    // Lo llama el joystick de granada al soltar despues de apuntar. "palanca" es la
    // direccion en pantalla por cuanto se arrastro, de 0 a 1.
    public void TirarGranadaApuntada(Vector2 palanca)
    {
        LanzarGranadaA(DestinoApuntado(palanca));
    }

    // Mientras se arrastra el joystick de granada: marca en el piso donde va a caer.
    public void ApuntarGranada(Vector2 palanca)
    {
        if (GranadaLista) MostrarPunteroGranada(DestinoApuntado(palanca));
        else OcultarPunteroGranada();
    }

    public void OcultarPunteroGranada()
    {
        if (punteroGranada != null && punteroGranada.gameObject.activeSelf) punteroGranada.gameObject.SetActive(false);
    }

    public bool GranadaLista
    {
        get { return GranadaDesbloqueada && granadaPrefab != null && !MenuPausa.JuegoCongelado && Time.time >= granadaDisponibleEn; }
    }

    private void LanzarGranadaA(Vector3 destino)
    {
        OcultarPunteroGranada();
        if (!GranadaLista) return;

        granadaDisponibleEn = Time.time + granadaCooldown;

        // La granada se encarga sola del vuelo, de explotar y de destruirse.
        Granade granada = Instantiate(granadaPrefab, transform.position, transform.rotation);
        granada.Lanzar(destino);
        Progreso.ContarGranada();
    }

    // Igual que el joystick de disparo: arriba en la pantalla es +Z en el mundo.
    private Vector3 DestinoApuntado(Vector2 palanca)
    {
        float distancia = Mathf.Lerp(distanciaMinimaGranada, distanciaMaximaGranada, Mathf.Clamp01(palanca.magnitude));
        Vector3 apuntado = transform.position + new Vector3(palanca.x, 0f, palanca.y);
        return PuntoEnElPiso(transform.position, apuntado, transform.forward, distancia, distancia);
    }

    private void MostrarPunteroGranada(Vector3 centro)
    {
        if (punteroGranada == null) return;
        Granade.DibujarAnillo(punteroGranada, centro, granadaPrefab.radioExplosion);
        if (!punteroGranada.gameObject.activeSelf) punteroGranada.gameObject.SetActive(true);
    }

    // En PC, donde esta el mouse. En movil no hay puntero: hacia donde se venia
    // apuntando con el joystick de disparo (apretar G obliga a soltarlo), y si no
    // se apunto hace poco, hacia donde mira el jugador.
    private Vector3 DestinoGranada()
    {
        if (Plataforma.EsMovil)
        {
            Vector3 direccion = transform.forward;
            PlayerJS joysticks = GetComponent<PlayerJS>();
            if (joysticks != null && joysticks.ApuntoHaceMenosDe(1f)) direccion = joysticks.UltimaDireccionApuntada;
            return PuntoEnElPiso(transform.position, transform.position + direccion, transform.forward,
                distanciaGranadaMovil, distanciaGranadaMovil);
        }

        Ray rayo = mainCamera.ScreenPointToRay(Input.mousePosition);
        Plane piso = new Plane(Vector3.up, Vector3.zero);
        float largo;
        Vector3 apuntado = piso.Raycast(rayo, out largo) ? rayo.GetPoint(largo) : transform.position + transform.forward;
        return PuntoEnElPiso(transform.position, apuntado, transform.forward, distanciaMinimaGranada, distanciaMaximaGranada);
    }

    // El punto del piso en la direccion de "apuntado", a una distancia del origen
    // entre minima y maxima. Si "apuntado" cae sobre el origen, usa "adelante".
    public static Vector3 PuntoEnElPiso(Vector3 origen, Vector3 apuntado, Vector3 adelante, float minima, float maxima)
    {
        Vector3 plano = apuntado - origen;
        plano.y = 0f;
        float distancia = plano.magnitude;

        Vector3 direccion;
        if (distancia > 0.001f)
        {
            direccion = plano / distancia;
        }
        else
        {
            direccion = new Vector3(adelante.x, 0f, adelante.z).normalized;
        }

        distancia = Mathf.Clamp(distancia, minima, maxima);
        return new Vector3(origen.x, 0f, origen.z) + direccion * distancia;
    }
}
