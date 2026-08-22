using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GeneradorZombis : MonoBehaviour
{
    [SerializeField]
    private GameObject Zombi;
    [SerializeField]
    private GameObject ZombiRapido;
    [SerializeField]
    private GameObject ZombiTanque;
    [SerializeField]
    private GameObject ZombiBOSS;
    [SerializeField]
    private GameObject ZombiFASTER;

    [SerializeField]
    public float intervaloZombi; //0.25
    [SerializeField]
    public float intervaloZombiRapido; //1
    [SerializeField]
    public float intervaloZombiTanque; //3
    [SerializeField]
    public float intervaloZombiBOSS; //30
    [SerializeField]
    public float intervaloZombiFASTER; //2

    // Techo de poblacion. Los cinco generadores corren en paralelo y para siempre,
    // asi que sin esto son ~350 zombis en el primer minuto y sigue creciendo.
    [SerializeField]
    public int maxZombisVivos = 60;

    // En movil cada zombi cuesta mas (animador, fisica y sombra en una CPU y GPU
    // chicas), asi que el techo es mas bajo. Salio de la primera prueba en un
    // telefono, con bajos FPS.
    [SerializeField]
    public int maxZombisVivosMovil = 35;

    // Start is called before the first frame update
    void Start()
    {
        if (Plataforma.EsMovil) maxZombisVivos = maxZombisVivosMovil;

        StartCoroutine(spawnEnemy(intervaloZombi, Zombi));
        StartCoroutine(spawnEnemy(intervaloZombiRapido, ZombiRapido));
        StartCoroutine(spawnEnemy(intervaloZombiTanque, ZombiTanque));
        StartCoroutine(spawnEnemy(intervaloZombiBOSS, ZombiBOSS));
        StartCoroutine(spawnEnemy(intervaloZombiFASTER, ZombiFASTER));
    }

    private IEnumerator spawnEnemy(float interval, GameObject enemy)
    {
        // Antes esto se rellamaba a si mismo con un StartCoroutine al final, lo que
        // creaba una corrutina nueva por spawn. Un while hace lo mismo sin alocar.
        while (true)
        {
            yield return new WaitForSeconds(interval);                   //X                         //Y                     //Z

            if (EnemyController.ZombisVivos >= maxZombisVivos) continue;

            Instantiate(enemy, new Vector3(Random.Range(-48f, 48), Random.Range(0.5f, 0.5f), Random.Range(-45, 45)), Quaternion.identity);
        }
    }
}
