using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class PlayerController : MonoBehaviour
{

    //public float moveSpeed;
    private Rigidbody myRigidbody;

    public float moveSpeed = 8f;
    

    private Vector3 moveInput;
    private Vector3 moveVelocity;

    private Camera mainCamera;

    public GunController theGun;

    public TMPro.TextMeshProUGUI textoContBalas;

    public int cantBalas = 0;
    public int maxBalas = 500;
    public void Move(Vector2 direction)
    {
        transform.Translate(new Vector3(direction.x, 0, direction.y) * Time.deltaTime * moveSpeed);
    }
    // Start is called before the first frame update
    void Start()
    {            
        myRigidbody = GetComponent<Rigidbody>();
        mainCamera = FindObjectOfType<Camera>();
    }

    // Update is called once per frame
    void Update()
    {



        // MOVIMIENTO Y CAMARA
#if UNITY_STANDALONE
        moveInput = new Vector3(Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical"));
        moveVelocity = moveInput * moveSpeed;

        Ray cameraRay = mainCamera.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        float rayLength;

        if (groundPlane.Raycast(cameraRay, out rayLength))
        {
            Vector3 pointToLook = cameraRay.GetPoint(rayLength);
            Debug.DrawLine(cameraRay.origin, pointToLook, Color.blue);

            transform.LookAt(new Vector3(pointToLook.x, transform.position.y, pointToLook.z));
        }



        if (Input.GetMouseButtonDown(0))
        {

            if (cantBalas > 0)
            {

                theGun.isFiring = true;

            }


        }

        if (Input.GetMouseButtonUp(0))
        {
            theGun.isFiring = false;
        }



#endif
    }

   
    void FixedUpdate()
    {
        if (cantBalas <= 0)
        {
            theGun.isFiring = false;
            
        }
        
        myRigidbody.velocity = moveVelocity;
    }

   

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Balas"))
        {
            Destroy(other.gameObject);
            cantBalas = maxBalas;
        }
    }

}
