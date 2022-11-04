using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GeneradorZombis1 : MonoBehaviour
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
        GameObject newEnemy = Instantiate(enemy, new Vector3(Random.Range(-45f, 45), Random.Range(0.5f, 0.5f), 45), Quaternion.identity);
        StartCoroutine(spawnEnemy(interval, enemy));
    }
}