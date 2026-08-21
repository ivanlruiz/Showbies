using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class PlayerHealth : MonoBehaviour
{
    
    public static PlayerHealth instance;

    

    public int health;
    public int maxHealth = 200;
    public int curaPorPickup = 100;
    public TMP_Text healthTMP;

    private bool estaMuerto;
    private int ultimaVidaMostrada = int.MinValue;

    // Update is called once per frame
    void Update()
    {
        // Solo al cambiar: el ToString por frame es una alocacion por frame.
        if (health != ultimaVidaMostrada)
        {
            ultimaVidaMostrada = health;
            healthTMP.text = health.ToString();
        }
    }

    private void Awake()
    {
        instance = this;
    }

    public void TakeDamage(int amount)
    {
        // Varios zombis pegando en el mismo paso de fisica llamaban a esto varias
        // veces con la vida ya en cero, y el bloque de muerte corria de nuevo.
        if (estaMuerto) return;

        health -= amount;
        if(health <= 0)
        {
            estaMuerto = true;

            int highScore = PlayerPrefs.GetInt("HighScore");

            PlayerPrefs.SetInt("Score", Puntaje.instance.contadorKill);

            if (Puntaje.instance.contadorKill > highScore)
            {

                PlayerPrefs.SetInt("HighScore", Puntaje.instance.contadorKill);
            }

            // Para que "Retry" vuelva al modo que se estaba jugando y no siempre
            // al primero. Sin esto, morir en WaveMode te reiniciaba en ShowBies1.
            PlayerPrefs.SetInt("UltimoModo", SceneManager.GetActiveScene().buildIndex);

            // Sin Save() esto queda sólo en memoria hasta que el juego cierre bien.
            PlayerPrefs.Save();

            SceneManager.LoadScene(2);
            Destroy(gameObject);
            
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Vida"))
        {
            Destroy(other.gameObject);

            // El tope estaba hardcodeado en 200 y maxHealth no lo leia nadie: el
            // campo decia 10 en el codigo y 200 en las escenas. Ahora el que manda
            // es maxHealth, que en las dos escenas ya vale 200 (mismo resultado).
            health = Mathf.Min(health + curaPorPickup, maxHealth);
        }
    }
}
