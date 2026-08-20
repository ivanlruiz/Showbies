using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using TMPro;

public class PlayerController : MonoBehaviour
{
    public Transitions trans;
    public GeneradorZombis GeneradorZombis;

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

    [Header("UI Settings")]
    public TextMeshProUGUI textoContBalas;

    [Header("Ammo Settings")]
    public int cantBalas = 0;
    public int maxBalas = 500;

    [Header("Granade Settings")]
    public float granadaCooldown = 5f;

    private float granadaDisponibleEn;

    private void Start()
    {
        myRigidbody = GetComponent<Rigidbody>();
        mainCamera = FindObjectOfType<Camera>();
    }

    private void Update()
    {
        // En móvil el input lo maneja PlayerJS con los joysticks. Si además
        // corriera esto, los dos se pelearían por moveVelocity y por isFiring.
        if (Application.isMobilePlatform) return;

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
        if (other.gameObject.CompareTag("Balas"))
        {
            Destroy(other.gameObject);
            cantBalas = maxBalas;
            theGun.tiempoDisparo = 0.03f;
        }
        else if (other.gameObject.CompareTag("pwBalas"))
        {
            Destroy(other.gameObject);
            cantBalas = 1000;
            theGun.tiempoDisparo = 0.01f;
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

    private void ThrowGranade()
    {
        // El cooldown de 5 segundos vivía en Granade, sobre la instancia recién
        // creada, así que no limitaba nada: se podían tirar granadas por frame.
        // Va acá, que es donde está el input.
        if (granadaPrefab == null || Time.time < granadaDisponibleEn) return;

        granadaDisponibleEn = Time.time + granadaCooldown;

        // La granada se encarga sola de su mecha y de destruirse al explotar.
        Instantiate(granadaPrefab, transform.position, transform.rotation);
    }
}
