using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletController : MonoBehaviour
{
    public int velocidad;
    public float lifeTime;
    public int dañoDar;

    // Pool de balas.
    //
    // Con tiempoDisparo = 0.04 son 25 balas por segundo, y el power-up de balas
    // lo baja a 0.01, o sea 100 por segundo. Cada una era un Instantiate y un
    // Destroy: de lejos la mayor fuente de basura del juego.
    private static readonly Stack<BulletController> pool = new Stack<BulletController>();

    private float lifeTimeInicial;
    private bool enUso;

    // El static sobrevive al cambio de escena, pero las balas guardadas no.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetearPool()
    {
        pool.Clear();
    }

    public static BulletController Obtener(BulletController prefab, Vector3 posicion, Quaternion rotacion)
    {
        BulletController bala = null;

        // Al cambiar de escena las balas dormidas se destruyen y en la pila
        // quedan referencias muertas. Descartarlas antes de usarlas.
        while (bala == null && pool.Count > 0) bala = pool.Pop();

        if (bala == null)
        {
            bala = Instantiate(prefab, posicion, rotacion);
            bala.lifeTimeInicial = prefab.lifeTime;
        }
        else
        {
            bala.transform.SetPositionAndRotation(posicion, rotacion);
            bala.gameObject.SetActive(true);
        }

        bala.lifeTime = bala.lifeTimeInicial;
        bala.enUso = true;
        return bala;
    }

    private void Devolver()
    {
        // Sin esta guarda, dos colisiones en el mismo paso de fisica meterian
        // la misma bala dos veces en la pila y se entregaria a dos disparos.
        if (!enUso) return;

        enUso = false;
        gameObject.SetActive(false);
        pool.Push(this);
    }

    // Update is called once per frame
    void Update()
    {
        transform.Translate(Vector3.forward * velocidad * Time.deltaTime);

        lifeTime -= Time.deltaTime;
        if (lifeTime < 0)
        {
            Devolver();
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
        Devolver();
    }
}
