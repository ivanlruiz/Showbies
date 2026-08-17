using System.Collections;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    public GameObject[] enemyPrefabs; // Lista de enemigos prefabricados
    public Transform spawnPoint; // Punto de aparición de enemigos
    public float timeBetweenWaves = 10f; // Tiempo entre oleadas
    public int enemiesPerWave = 10; // Número de enemigos por oleada
    public int wavesBeforeNewEnemy = 5; // Cada cuántas oleadas aparece un nuevo tipo de enemigo

    private int currentWave = 0;
    private int enemiesSpawned = 0;
    private int currentEnemyIndex = 0;

    private void Start()
    {
        StartCoroutine(SpawnWave());
    }

    private void Update()
    {
        // Comprueba si se han derrotado a todos los enemigos de la oleada actual
        if (enemiesSpawned >= enemiesPerWave)
        {
            StartCoroutine(SpawnWave());
        }
    }

    IEnumerator SpawnWave()
    {
        yield return new WaitForSeconds(timeBetweenWaves);
        currentWave++;
        enemiesSpawned = 0;

        // Determina si es hora de introducir un nuevo tipo de enemigo
        if (currentWave % wavesBeforeNewEnemy == 0)
        {
            currentEnemyIndex++; // Incrementa el índice del enemigo actual
            currentEnemyIndex = Mathf.Clamp(currentEnemyIndex, 0, enemyPrefabs.Length - 1); // Asegura que no se exceda el número de tipos de enemigos

            // Opcional: aquí puedes agregar código para mostrar un mensaje o efecto de transición al introducir un nuevo tipo de enemigo

            // Ejemplo: Debug.Log("¡Nuevos enemigos aparecerán en esta oleada!");
        }

        // Genera los enemigos de la oleada actual
        for (int i = 0; i < enemiesPerWave; i++)
        {
            SpawnEnemy();
            yield return new WaitForSeconds(1f); // Intervalo entre apariciones de enemigos
        }
    }

    void SpawnEnemy()
    {
        // Selecciona un enemigo del array de acuerdo al índice actual
        GameObject enemyPrefab = enemyPrefabs[currentEnemyIndex];

        // Instancia el enemigo en el punto de aparición
        Instantiate(enemyPrefab, spawnPoint.position, Quaternion.identity);

        enemiesSpawned++;
    }
}
