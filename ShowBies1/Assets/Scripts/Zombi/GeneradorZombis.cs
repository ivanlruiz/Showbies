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
    private float intervaloZombi = 3.5f;
    [SerializeField]
    private float intervaloZombiRapido = 10f;
    [SerializeField]
    private float intervaloZombiTanque = 10f;

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(spawnEnemy(intervaloZombi, Zombi));
        StartCoroutine(spawnEnemy(intervaloZombiRapido, ZombiRapido));
        StartCoroutine(spawnEnemy(intervaloZombiTanque, ZombiTanque));
    }

    private IEnumerator spawnEnemy(float interval, GameObject enemy)
    {
        yield return new WaitForSeconds(interval);
        GameObject newEnemy = Instantiate(enemy, new Vector3(Random.Range(-10f, 10), Random.Range(-2f, 2f), 0), Quaternion.identity);
        StartCoroutine(spawnEnemy(interval, enemy));
    }
}