using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletController : MonoBehaviour
{
    public int velocidad;
    public float lifeTime;
    public int dañoDar;

    public static Puntaje instance;
    public int Score;

    
    void Start()
    {
        Physics.IgnoreLayerCollision(6, 7);
            
    }

    // Update is called once per frame
    void Update()
    {
        transform.Translate(Vector3.forward * velocidad * Time.deltaTime);

        lifeTime -= Time.deltaTime;
        if (lifeTime < 0)
        {
            Destroy(gameObject);
        }
    }

    void OnCollisionEnter(Collision other)
    {
        if(other.gameObject.tag == "ZombiNormal")
        {
            other.gameObject.GetComponent<EnemyController>().DanoZombi(dañoDar);
            Destroy(gameObject);
            
        }

        if(other.gameObject.tag == "ZombiBoss")
        {
            other.gameObject.GetComponent<EnemyController>().DanoZombi(dañoDar);
            Destroy(gameObject);
            Puntaje.instance.contadorKill += 100;
        }

        if(other.gameObject.tag == "ZombiFaster")
        {
            other.gameObject.GetComponent<EnemyController>().DanoZombi(dañoDar);
            Destroy(gameObject);
            Puntaje.instance.contadorKill += 5;
        }

        if(other.gameObject.tag == "ZombiRapido")
        {
            other.gameObject.GetComponent<EnemyController>().DanoZombi(dañoDar);
            Destroy(gameObject);
            Puntaje.instance.contadorKill += 2;
        }

        if(other.gameObject.tag == "ZombiTanque")
        {
            other.gameObject.GetComponent<EnemyController>().DanoZombi(dañoDar);
            Destroy(gameObject);
            Puntaje.instance.contadorKill += 20;
        }

        
    }
}
