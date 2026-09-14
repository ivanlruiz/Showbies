using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public Transitions trans;

    [Header("Movement Settings")]
    public float moveSpeed = 8f;
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

    [Header("Granade Settings")]
    public float granadaCooldown = 5f;
    public float distanciaMinimaGranada = 3f;    // para que no caiga a los pies del jugador
    public float distanciaMaximaGranada = 12f;
    public float distanciaGranadaMovil = 8f;     // en movil no hay un punto al que apuntar: sale a distancia fija

    private float granadaDisponibleEn;

    // Segundos que faltan para poder tirar otra granada. Lo muestra el boton de granada.
    public float GranadaRestante { get { return Mathf.Max(0f, granadaDisponibleEn - Time.time); } }

    private void Start()
    {
        myRigidbody = GetComponent<Rigidbody>();

        // Camera.main y no FindObjectOfType: FindObjectOfType esta deprecado en
        // Unity 6 y Camera.main esta cacheado por el engine desde 2020.2.
        mainCamera = Camera.main;
    }

    private void Update()
    {
        if (MenuPausa.Pausado)
        {
            // Lo que se suelte durante la pausa no llega como GetMouseButtonUp:
            // sin esto el arma quedaria disparando sola al reanudar.
            theGun.isFiring = false;
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
        moveVelocity = moveInput * moveSpeed;

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
            theGun.MejorarCadencia(0.03f);
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
            theGun.MejorarCadencia(0.01f);
        }
    }

    private void HandleMovement()
    {
        moveInput = new Vector3(Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical"));
        moveVelocity = moveInput * moveSpeed;

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
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ThrowGranade();
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (cantBalas > 0)
            {
                trans.anim.SetBool("shoot", true);
                theGun.isFiring = true;
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            trans.anim.SetBool("shoot", false);
            theGun.isFiring = false;
        }
    }

    // Publico porque en movil lo llama el boton de granada del Canvas (Espacio no
    // existe ahi). El cooldown vive aca adentro, asi que el boton no puede spamear.
    public void ThrowGranade()
    {
        // El cooldown de 5 segundos vivía en Granade, sobre la instancia recién
        // creada, así que no limitaba nada: se podían tirar granadas por frame.
        // Va acá, que es donde está el input.
        if (granadaPrefab == null || MenuPausa.Pausado || Time.time < granadaDisponibleEn) return;

        granadaDisponibleEn = Time.time + granadaCooldown;

        // La granada se encarga sola del vuelo, de explotar y de destruirse.
        Granade granada = Instantiate(granadaPrefab, transform.position, transform.rotation);
        granada.Lanzar(DestinoGranada());
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
