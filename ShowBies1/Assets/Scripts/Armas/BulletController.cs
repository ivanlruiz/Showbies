using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletController : MonoBehaviour
{
    public int velocidad;
    public float lifeTime;
    public int da�oDar;

    

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
        if(other.gameObject.tag == "Zombi")
        {
            other.gameObject.GetComponent<EnemyController>().DanoZombi(da�oDar);
            Destroy(gameObject);
        }

        
    }
}
