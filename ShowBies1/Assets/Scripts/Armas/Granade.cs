using System.Collections;
using UnityEngine;

public class Granade : MonoBehaviour
{
    public float radioExplosion = 5f;
    public int damage = 10;
    public ParticleSystem explosion;

    private bool canExplode = true;

    private void Start()
    {
        Physics.IgnoreLayerCollision(6, 7);
    }

    private void Update()
    {
        // Verificar si se puede lanzar la granada y si se presionó el botón de lanzar
        if (canExplode && Input.GetKeyDown(KeyCode.Space))
        {
            // Lanzar la granada
            Explode();

            // Aplicar el cooldown
            StartCoroutine(Cooldown(5f));
        }
    }

    private IEnumeratorsd   Cooldown(float cooldownTime)
    {
        // Desactivar la capacidad de lanzar granadas durante el cooldown
        canExplode = false;

        // Esperar el tiempo del cooldown
        yield return new WaitForSeconds(cooldownTime);

        // Activar la capacidad de lanzar granadas después del cooldown
        canExplode = true;
    }

    private void Explode()
    {
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
        explosion.Play();
        // No destruir la granada aquí, para que pueda continuar su vida útil y permitir que la corrutina de cooldown termine
    }
}
