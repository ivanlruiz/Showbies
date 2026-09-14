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
        // Kill-Z. Si los zombis empujan al jugador fuera del mapa, cae al vacio
        // para siempre sin morir: softlock. Cuenta como muerte normal.
        if (!estaMuerto && transform.position.y < -20f)
        {
            TakeDamage(Mathf.Max(health, 1));
        }

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
        Progreso.EmpezarPartida();
    }

    // La clave del record de un modo, por el buildIndex de su escena. La pantalla
    // de derrota la arma con "UltimoModo". La clave vieja "HighScore", sin modo,
    // ya no la lee nadie.
    public static string ClaveRecord(int modo)
    {
        return "HighScore_" + modo;
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

            // Un record por modo: los puntos del modo libre y los de las oleadas
            // no se comparan, y antes compartian una sola clave.
            int modo = SceneManager.GetActiveScene().buildIndex;
            string claveRecord = ClaveRecord(modo);
            int highScore = PlayerPrefs.GetInt(claveRecord);

            PlayerPrefs.SetInt("Score", Puntaje.instance.contadorKill);

            if (Puntaje.instance.contadorKill > highScore)
            {

                PlayerPrefs.SetInt(claveRecord, Puntaje.instance.contadorKill);
            }

            // Para que "Retry" vuelva al modo que se estaba jugando y no siempre
            // al primero. Sin esto, morir en WaveMode te reiniciaba en ShowBies1.
            PlayerPrefs.SetInt("UltimoModo", modo);

            // Sin Save() esto queda sólo en memoria hasta que el juego cierre bien.
            PlayerPrefs.Save();
            Progreso.Guardar();

            SceneManager.LoadScene(2);
            Destroy(gameObject);
            
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        // El jugador tiene dos colliders, asi que el mismo pickup dispara este
        // evento dos veces en el mismo paso de fisica, y Destroy es diferido: la
        // cura se aplicaba doble (50+100+100 clampeado a 200 en vez de 150). El
        // SetActive(false) inmediato marca el pickup como ya consumido.
        if (!other.gameObject.activeSelf) return;

        if (other.gameObject.CompareTag("PUVida"))
        {
            other.gameObject.SetActive(false);
            Destroy(other.gameObject);

            // El tope estaba hardcodeado en 200 y maxHealth no lo leia nadie: el
            // campo decia 10 en el codigo y 200 en las escenas. Ahora el que manda
            // es maxHealth, que en las dos escenas ya vale 200 (mismo resultado).
            health = Mathf.Min(health + curaPorPickup, maxHealth);
        }
    }
}
