using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletController : MonoBehaviour
{
    public int velocidad;
    public float lifeTime;
    public int dañoDar;

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
        // Antes habia cinco if identicos, uno por tag de zombi, y cada uno sumaba
        // puntos ACA: o sea por bala que pegaba, no por zombi muerto. El BOSS
        // tiene 500 de vida y la bala hace 5, asi que daba 100 impactos x 100
        // puntos = 10000. Ahora los puntos los da EnemyController al morir.
        EnemyController zombi = other.gameObject.GetComponent<EnemyController>();
        if (zombi == null) return;

        zombi.DanoZombi(dañoDar);
        Destroy(gameObject);
    }
}
