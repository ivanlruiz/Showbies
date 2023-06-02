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

    private bool isThrowingGranade = false;

    private void Start()
    {
        myRigidbody = GetComponent<Rigidbody>();
        mainCamera = FindObjectOfType<Camera>();
    }

    private void Update()
    {
        HandleMovement();
        HandleCamera();
        HandleShooting();
    }

    private void FixedUpdate()
    {
        myRigidbody.velocity = moveVelocity;
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
        if (!isThrowingGranade && granadaPrefab != null)
        {
            isThrowingGranade = true;
            StartCoroutine(ThrowGranadeWithDelay());
        }
    }

    private IEnumerator ThrowGranadeWithDelay()
    {
        // Deja pasar 3 segundos
        yield return new WaitForSeconds(0f);

        // Suelta la granada en el suelo
        Granade granadaInstance = Instantiate(granadaPrefab, transform.position, transform.rotation);

        // Llama al método Explode() después de 3 segundos
        granadaInstance.Invoke("Explode", 3f);
        isThrowingGranade = false;
    }
}
