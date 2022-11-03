using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunController : MonoBehaviour
{
    public bool isFiring;

    public BulletController bala;
    public int velocidadBala;

    public float tiempoDisparo;
    private float contadorDisp;

    

    public Transform firePoint;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(isFiring)
        {
            contadorDisp -= Time.deltaTime;
            if(contadorDisp <= 0)
            {
                contadorDisp = tiempoDisparo;
                BulletController newBullet = Instantiate(bala, firePoint.position, firePoint.rotation) as BulletController;
                newBullet.velocidad = velocidadBala;
            }
        } else
        {
            contadorDisp = 0;
        }

    }

    
}
