using UnityEngine;

public class Granade : MonoBehaviour
{
    public float radioExplosion = 5f;
    public int damage = 10;
    

    private void Start()
    {
        Physics.IgnoreLayerCollision(6, 7);
        // Iniciar una cuenta atr�s para la explosi�n
    }

    public void Explode()
    {
        Invoke("Explode", 3f);
        // Obtener todos los colliders en el radio de explosi�n
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
                    // Aplicar da�o al zombi
                    enemyController.DanoZombi(damage);
                }
            }
        }

        // Destruir la granada despu�s de la explosi�n
        Destroy(gameObject);
    }
}
