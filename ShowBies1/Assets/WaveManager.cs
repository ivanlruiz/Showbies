using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    // Configuraci�n de las oleadas
    public Wave[] waves;
    public float timeBetweenWaves = 5f;

    // Variables internas
    private int currentWaveIndex;
    private float timeUntilNextWave;
    private bool spawningWave;

    // Actualizaci�n del sistema de oleadas
    void Update()
    {
        if (spawningWave)
        {
            // Si estamos en plena oleada, no hacemos nada
            return;
        }

        // Si ya pas� el tiempo suficiente para la siguiente oleada, la comenzamos
        if (Time.time >= timeUntilNextWave)
        {
            StartCoroutine(SpawnWave());
        }
    }

    // Funci�n para comenzar una nueva oleada
    IEnumerator SpawnWave()
    {
        spawningWave = true;

        // Obtenemos la oleada actual
        Wave currentWave = waves[currentWaveIndex];

        // Para cada enemigo en la oleada
        for (int i = 0; i < currentWave.enemyCount; i++)
        {
            // Instanciamos el enemigo en la posici�n correspondiente
            Instantiate(currentWave.enemyPrefab, currentWave.spawnPoints[i].position, Quaternion.identity);

            // Esperamos un tiempo antes de instanciar el siguiente enemigo
            yield return new WaitForSeconds(currentWave.timeBetweenSpawns);
        }

        // Incrementamos el �ndice de oleada actual
        currentWaveIndex++;

        // Si ya no hay m�s oleadas, salimos del bucle
        if (currentWaveIndex >= waves.Length)
        {
            yield break;
        }

        // De lo contrario, establecemos el tiempo para la siguiente oleada
        timeUntilNextWave = Time.time + timeBetweenWaves;
        spawningWave = false;
    }
}

[System.Serializable]
public class Wave
{
    public GameObject enemyPrefab;
    public Transform[] spawnPoints;
    public int enemyCount;
    public float timeBetweenSpawns;
}
