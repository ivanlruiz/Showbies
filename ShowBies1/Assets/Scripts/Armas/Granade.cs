using System.Collections;
using UnityEngine;

public class Granade : MonoBehaviour
{
    public float radioExplosion = 5f;
    public int damage = 10;
    public ParticleSystem explosion;

    [SerializeField] private float tiempoDeMecha = 3f;

    private bool yaExploto;

    private void Start()
    {
        Physics.IgnoreLayerCollision(6, 7);

        // La granada enciende su propia mecha al nacer. Antes el jugador la
        // instanciaba y le hacia Invoke("Explode", 3f) mientras este script
        // ademas escuchaba Espacio en Update, asi que la misma granada explotaba
        // dos veces: una por el Invoke y otra en el frame en que se creo.
        StartCoroutine(Mecha());
    }

    private IEnumerator Mecha()
    {
        yield return new WaitForSeconds(tiempoDeMecha);
        Explode();
    }

    private void Explode()
    {
        if (yaExploto) return;
        yaExploto = true;

        Collider[] colliders = Physics.OverlapSphere(transform.position, radioExplosion);
        foreach (Collider nearbyObject in colliders)
        {
            // Verificar si el objeto colisionado es un zombi
            if (nearbyObject.CompareTag("ZombiNormal") ||
                nearbyObject.CompareTag("ZombiBoss") ||
                nearbyObject.CompareTag("ZombiFaster") ||
                nearbyObject.CompareTag("ZombiRapido") ||
                nearbyObject.CompareTag("ZombiTanque"))
            {
                // Obtener el componente EnemyController del zombi
                EnemyController enemyController = nearbyObject.GetComponent<EnemyController>();

                if (enemyController != null)
                {
                    // Aplicar daño al zombi
                    enemyController.DanoZombi(damage);
                }
            }
        }

        if (explosion != null)
        {
            // La particula es hija de la granada y tiene stopAction = Destroy, asi
            // que hay que despegarla antes de destruir la granada: si no, se corta
            // apenas empieza. Despegada se limpia sola cuando termina.
            explosion.transform.SetParent(null, true);
            explosion.Play();
        }

        Destroy(gameObject);
    }
}
