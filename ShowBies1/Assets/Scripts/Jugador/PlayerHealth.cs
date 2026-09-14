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
    public int maxHealth = 80;
    public int curaPorPickup = 40;
    public TMP_Text healthTMP;

    private bool estaMuerto;
    private int ultimaVidaMostrada = int.MinValue;

    // La cura de las cajas escala con la mejora de vida, así una caja sigue
    // valiendo la misma fracción de la barra. Lo fija AplicarMejoras.
    private float multiplicadorCura = 1f;

    // El daño de los zombis es float (escala por oleada) y la vida es int: lo que
    // no llega a un punto entero queda acá y se suma al golpe siguiente.
    private float danoPendiente;

    public float MultiplicadorCura { get { return multiplicadorCura; } }
    public int CuraPorCaja { get { return Mathf.RoundToInt(curaPorPickup * multiplicadorCura); } }

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

    // Lo llama AplicarMejoras al empezar la partida: la vida arranca llena con el
    // máximo mejorado.
    public void FijarVidaMaxima(int vidaMaxima, float multiplicadorCura)
    {
        maxHealth = Mathf.Max(1, vidaMaxima);
        health = maxHealth;
        this.multiplicadorCura = Mathf.Max(0f, multiplicadorCura);
        danoPendiente = 0f;
    }

    // Suma "cantidad" a lo pendiente y devuelve la parte entera, que es lo que se
    // descuenta de la vida; el resto queda para el golpe siguiente. Redondear
    // cada golpe por separado haría que un daño de 1,3 fuera siempre 1 y el
    // escalado del daño por oleada no se notara hasta el 1,5. Estático para
    // probarlo sin escena.
    public static int AcumularDano(ref float pendiente, float cantidad)
    {
        if (!(cantidad > 0f)) return 0;

        pendiente += cantidad;
        // El 0,0001 absorbe el error de float: que 0,3 + 0,7 dé 0,9999 y no 1.
        int entero = Mathf.FloorToInt(pendiente + 0.0001f);
        if (entero <= 0) return 0;

        pendiente = Mathf.Max(0f, pendiente - entero);
        return entero;
    }

    public void TakeDamage(float amount)
    {
        // Varios zombis pegando en el mismo paso de fisica llamaban a esto varias
        // veces con la vida ya en cero, y el bloque de muerte corria de nuevo.
        if (estaMuerto) return;

        int dano = AcumularDano(ref danoPendiente, amount);
        if (dano <= 0) return;

        health -= dano;
        if (health > 0) Efectos.DanioJugador();
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

            // El tope es maxHealth, que fija AplicarMejoras con la mejora de vida.
            // La cura escala con la misma mejora y cura siempre la mitad: 40 sin
            // mejora, 80 con 160 de vida.
            health = Mathf.Min(health + CuraPorCaja, maxHealth);
            Efectos.Caja(other.transform.position);
        }
    }
}
