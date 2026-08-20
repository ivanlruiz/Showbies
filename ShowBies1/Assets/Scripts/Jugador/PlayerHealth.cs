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
    public int maxHealth = 10;
    public TMP_Text healthTMP;
    
    BulletController bulletController;
    void Start()
    {
        
        
    }

    // Update is called once per frame
    void Update()
    {
        healthTMP.text = health.ToString();
    }

    private void Awake()
    {
        instance = this;
    }

    public void TakeDamage(int amount)
    {
        health -= amount;
        if(health <= 0)
        {
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
            health = health+100;    
            if (health > 200)
            {
                health = 200;
            }
        }
    }
}
