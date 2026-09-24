using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RestartScene : MonoBehaviour
{
    // Sin #if: asi el codigo compila igual en Windows y en Android y una
    // compilacion aca valida las dos. En movil no hay teclado, con lo cual
    // GetKey no dispara nunca y no hace falta apagar nada.

    // Update is called once per frame
    void Update()
    {
        // Reinicia el modo que se esta jugando. Antes cargaba la escena 1 fija,
        // asi que apretar R en WaveMode te sacaba al otro modo.
        // GetKeyDown y no GetKey: con GetKey, mantener la R apretada recargaba
        // la escena en loop, una vez por frame hasta soltarla.
        // Ni con un video en pantalla ni con el ¡HAS MUERTO! abierto: recargar ahi perdia
        // el premio del video, o terminaba la partida sin pasar por PlayerHealth.Terminar
        // (sin contarla como terminada ni olvidar la oleada en curso).
        if (ServicioAnuncios.MostrandoAnuncio || OfertaDeRevivir.Activa) return;
        if (Input.GetKeyDown(KeyCode.R))
        {
            WaveManager.OlvidarPartidaSiEsOleadas();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
