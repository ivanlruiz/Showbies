using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunController : MonoBehaviour
{
    

    public bool isFiring;
    public PlayerController player;
    public BulletController bala;
    public int velocidadBala;

    public float tiempoDisparo;
    private float contadorDisp;

    public AudioSource AudioSource;
    

    public Transform firePoint;
    // Start is called before the first frame update
    void Start()
    {
        
        AudioSource = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        if(isFiring && player.cantBalas>0) 
        {
            
            contadorDisp -= Time.deltaTime;
            if(contadorDisp <= 0)
            {
                player.cantBalas--;
                AudioSource.Play();
                contadorDisp = tiempoDisparo;
                BulletController newBullet = Instantiate(bala, firePoint.position, firePoint.rotation) as BulletController;
                newBullet.velocidad = velocidadBala;
            }
        } 
        else
        {
            contadorDisp = 0;
        }

        
    }

    


}