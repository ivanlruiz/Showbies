using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    // Configuración de las oleadas
    public Wave[] waves;
    public float timeBetweenWaves = 5f;

    // Variables internas
    private int currentWaveIndex;
    private float timeUntilNextWave;
    private bool spawningWave;

    // Actualización del sistema de oleadas
    void Update()
    {
        if (spawningWave)
        {
            // Si estamos en plena oleada, no hacemos nada
            return;
        }

        // Si ya pasó el tiempo suficiente para la siguiente oleada, la comenzamos
        if (Time.time >= timeUntilNextWave)
        {
            StartCoroutine(SpawnWave());
        }
    }

    // Función para comenzar una nueva oleada
    IEnumerator SpawnWave()
    {
        spawningWave = true;

        // Obtenemos la oleada actual
        Wave currentWave = waves[currentWaveIndex];

        // Para cada enemigo en la oleada
        for (int i = 0; i < currentWave.enemyCount; i++)
        {
            // Instanciamos el enemigo en la posición correspondiente
            Instantiate(currentWave.enemyPrefab, currentWave.spawnPoints[i].position, Quaternion.identity);

            // Esperamos un tiempo antes de instanciar el siguiente enemigo
            yield return new WaitForSeconds(currentWave.timeBetweenSpawns);
        }

        // Incrementamos el índice de oleada actual
        currentWaveIndex++;

        // Si ya no hay más oleadas, salimos del bucle
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
