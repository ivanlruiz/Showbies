using System.Collections;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    public GameObject[] enemyPrefabs; // Lista de enemigos prefabricados
    public Transform[] spawnPoints; // Puntos de aparición: se elige uno al azar por enemigo
    public float timeBetweenWaves = 10f; // Tiempo entre oleadas
    public int enemiesPerWave = 10; // Número de enemigos por oleada
    public int wavesBeforeNewEnemy = 5; // Cada cuántas oleadas aparece un nuevo tipo de enemigo
    public int maxZombisVivos = 60; // Techo de población: si está lleno, la oleada espera
    public int maxZombisVivosMovil = 35; // En móvil cada zombi cuesta más; ver GeneradorZombis

    private int currentWave = 0;
    private int currentEnemyIndex = 0;

    private void Start()
    {
        if (Plataforma.EsMovil) maxZombisVivos = maxZombisVivosMovil;
        StartCoroutine(SpawnWaves());
    }

    // Una sola corrutina para toda la partida.
    //
    // Antes era una corrutina por oleada, relanzada desde Update mientras
    // enemiesSpawned >= enemiesPerWave. Como enemiesSpawned recién se reseteaba
    // después del WaitForSeconds inicial, Update arrancaba una corrutina nueva
    // en CADA frame de esa espera: con timeBetweenWaves = 2 y 60fps son ~120
    // oleadas simultáneas, o sea 1200 enemigos.
    private IEnumerator SpawnWaves()
    {
        while (true)
        {
            yield return new WaitForSeconds(timeBetweenWaves);
            currentWave++;

            // Determina si es hora de introducir un nuevo tipo de enemigo
            if (currentWave % wavesBeforeNewEnemy == 0)
            {
                currentEnemyIndex++; // Incrementa el índice del enemigo actual
                currentEnemyIndex = Mathf.Clamp(currentEnemyIndex, 0, enemyPrefabs.Length - 1); // Asegura que no se exceda el número de tipos de enemigos
            }

            // Genera los enemigos de la oleada actual
            for (int i = 0; i < enemiesPerWave; i++)
            {
                while (EnemyController.ZombisVivos >= maxZombisVivos)
                {
                    yield return null;
                }

                SpawnEnemy();
                yield return new WaitForSeconds(1f); // Intervalo entre apariciones de enemigos
            }
        }
    }

    void SpawnEnemy()
    {
        if (enemyPrefabs.Length == 0 || spawnPoints.Length == 0) return;

        // Selecciona un enemigo del array de acuerdo al índice actual
        GameObject enemyPrefab = enemyPrefabs[currentEnemyIndex];

        // Antes había un solo punto y toda la oleada salía del mismo lugar
        Transform punto = spawnPoints[Random.Range(0, spawnPoints.Length)];
        if (punto == null) return;

        Instantiate(enemyPrefab, punto.position, Quaternion.identity);
    }
}
