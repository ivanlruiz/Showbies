using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{

    public float moveSpeed;
    private Rigidbody myRigidbody;



    private Vector3 moveInput;
    private Vector3 moveVelocity;

    private Camera mainCamera;

    public GunController theGun;

    public TMPro.TextMeshProUGUI textoContBalas;

    int cantBalas = 0;

    // Start is called before the first frame update
    void Start()
    {
        cantBalas = 500;
        myRigidbody = GetComponent<Rigidbody>();
        mainCamera = FindObjectOfType<Camera>();
    }

    // Update is called once per frame
    void Update()
    {
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



        //cantidad de balas
        textoContBalas.text = cantBalas.ToString();



    }


    void FixedUpdate()
    {
        if (cantBalas > 0)
        {
            if (theGun.isFiring == true)
            {

                cantBalas--;
                
            }
        }
        else { theGun.isFiring = false; }
        myRigidbody.velocity = moveVelocity;
    }

   

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Balas"))
        {
            Destroy(other.gameObject);
            cantBalas += 500;
        }
    }
}
