using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{

    //vida
    
    private int vidaActual;
    
    private Rigidbody rb;
   

    public Enemy enemyType;

    public PlayerController thePlayer;
    // Start is called before the first frame update
    void Start()
    {
        vidaActual = enemyType.hp;


        rb = GetComponent<Rigidbody>();
        thePlayer = FindObjectOfType<PlayerController>();
    }

    private void FixedUpdate()
    {
        if (thePlayer == null) return;


        transform.LookAt(thePlayer.transform.position);
        rb.velocity = (transform.forward * enemyType.velocidad);


        
    }

    void Update()
    {
       

    }

    public void DanoZombi(int daño)
    {

        vidaActual -= daño;

        if (vidaActual <= 0)
       
        
        {
            Destroy(gameObject);

            Puntaje.instance.contadorKill++;
            Puntaje.instance.UpdateKillCounterUI();




        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            PlayerHealth.instance.TakeDamage(enemyType.daño);
            
        }
    }


}
