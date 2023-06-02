using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveController : MonoBehaviour
{
    public Puntaje score;
    public GameObject[][] enemyPrefabs;
    public int numberOfWaves; // Número total de oleadas
    public int enemiesPerWave; // Número de enemigos por oleada

    
    private int currentWave; // Oleada actual
    private int enemiesRemaining; // Enemigos restantes en la oleada actual

    void Start()
    {
        enemyPrefabs = new GameObject[numberOfWaves][];

        for (int i = 0; i < numberOfWaves; i++)
        {
            enemyPrefabs[i] = new GameObject[5];
        }

        /*enemyPrefabs[0][0] = zombiePrefab;
        enemyPrefabs[0][1] = fastZombiePrefab;
        enemyPrefabs[0][2] = fatZombiePrefab;
        enemyPrefabs[0][3] = rangedZombiePrefab;
        enemyPrefabs[0][4] = bossZombiePrefab;*/

        currentWave = 1;
        StartWave();
    }

    void StartWave()
    {
        enemiesRemaining = enemiesPerWave;
        for (int i = 0; i < enemiesPerWave; i++)
        {
            int randomEnemyIndex = Random.Range(0, enemyPrefabs.Length);
            Instantiate(enemyPrefabs[currentWave - 1][randomEnemyIndex], GetRandomSpawnPosition(), Quaternion.identity);
        }
    }

    void Update()
    {

        // Comprobar si todos los enemigos de la oleada actual han sido derrotados
        if (Puntaje.instance.contadorKill >= 100)
        {
            Invoke("StartNextWave", 5f);
            Puntaje.instance.contadorKill = 0;
            currentWave++;



        }
    }



    Vector3 GetRandomSpawnPosition()
    {
        // Devuelve una posición aleatoria en la escena para instanciar el enemigo
        return new Vector3(Random.Range(-48, 48), 0, Random.Range(-48, 48));
    }

    void StartNextWave()
    {
        StartWave();

    }
}

