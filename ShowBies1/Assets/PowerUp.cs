using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PowerUp : MonoBehaviour
{
    [SerializeField]
    private GameObject PUBalas;

    [SerializeField]
    private float IntervaloPUBalas = 3.5f;
    void Start()
    {
        StartCoroutine(spawnPU(IntervaloPUBalas, PUBalas));
    }

    
        private IEnumerator spawnPU(float interval, GameObject powerup)
        {
            yield return new WaitForSeconds(interval);                      //distancia              //altura    //no sé
            GameObject newEnemy = Instantiate(powerup, new Vector3(Random.Range(-48f, 48), Random.Range(0.5f, 0.5f), -45), Quaternion.identity);
            StartCoroutine(spawnPU(interval, powerup));
        }
    
}
