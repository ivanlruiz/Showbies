using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletController : MonoBehaviour
{
    public int velocidad;
    public float lifeTime;
    public int dañoDar;   // respaldo del prefab: el daño de verdad lo pone el arma en cada tiro

    // El daño de esta bala, puesto en cada tiro por GunController desde la mejora
    // de daño. No se serializa: si se guardara en el prefab, el primer tiro de la
    // partida siguiente arrancaría con el daño de la anterior.
    [System.NonSerialized] public float danoAplicado;

    // Pool de balas.
    //
    // Son 20 tiros por segundo de base, hasta 108 con la cadencia al tope y la
    // caja de arma, con un techo de 120. Cada una era un Instantiate y un
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
        // El del prefab por si quien la pide no pone otro; GunController lo pisa.
        bala.danoAplicado = prefab.dañoDar;
        bala.enUso = true;
        return bala;
    }

    // Mueve la bala lo que habría recorrido si hubiera salido "segundos" antes.
    // Con varios tiros en el mismo frame, cada uno sale adelantado según su atraso
    // y el chorro queda parejo en vez de amontonado en la boca del arma.
    public void Adelantar(float segundos)
    {
        if (!(segundos > 0f)) return;

        transform.Translate(Vector3.forward * velocidad * segundos);
        lifeTime -= segundos;
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
        // Los eventos de un mismo paso de fisica se despachan todos aunque el
        // primero apague la bala: sin esta guarda, una bala que toca a dos
        // zombis superpuestos daña a los dos.
        if (!enUso) return;

        // GetComponentInParent y no GetComponent: los zombis tienen dos hitboxes
        // hijas ("Cube") ademas del capsule de la raiz, y el componente vive en
        // la raiz. Con GetComponent, una bala que pegaba en la hitbox hija
        // rebotaba sin hacer daño.
        //
        // Antes ademas habia cinco if identicos por tag que sumaban puntos ACA,
        // o sea por impacto y no por muerte. Los puntos los da EnemyController.
        EnemyController zombi = other.gameObject.GetComponentInParent<EnemyController>();
        if (zombi == null) return;

        zombi.DanoZombi(danoAplicado);
        Devolver();
    }
}
