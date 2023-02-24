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

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(spawnEnemy(intervaloZombi, Zombi));
        StartCoroutine(spawnEnemy(intervaloZombiRapido, ZombiRapido));
        StartCoroutine(spawnEnemy(intervaloZombiTanque, ZombiTanque));
        StartCoroutine(spawnEnemy(intervaloZombiBOSS, ZombiBOSS));
        StartCoroutine(spawnEnemy(intervaloZombiFASTER, ZombiFASTER));
    }

    private IEnumerator spawnEnemy(float interval, GameObject enemy)
    {
        yield return new WaitForSeconds(interval);                      //X                         //Y                     //Z
        GameObject newEnemy = Instantiate(enemy, new Vector3(Random.Range(-48f, 48), Random.Range(0.5f, 0.5f), Random.Range(-45, 45)), Quaternion.identity);
        StartCoroutine(spawnEnemy(interval, enemy));
    }
}