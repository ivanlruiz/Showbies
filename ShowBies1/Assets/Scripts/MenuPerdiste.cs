using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuPerdiste : MonoBehaviour
{
    public void Retry ()
    {
        // Vuelve al modo que se estaba jugando, no siempre al primero.
        // PlayerHealth lo guarda al morir; si no hay nada, a las oleadas. El libre
        // pasa por ModoLibre por si todavia no esta desbloqueado.
        int modo = PlayerPrefs.GetInt("UltimoModo", TiendaMejoras.EscenaOleadas);
        SceneManager.LoadScene(ModoLibre.EscenaPara(modo));
    }

    public void Menu()
    {
        SceneManager.LoadScene(0);
    }

    // El boton MEJORAS de la derrota: la tienda vive en el menu, asi que carga
    // el menu con la tienda ya abierta. Es el momento en que el jugador acaba de
    // cobrar y tiene mas ganas de gastar.
    public void AbrirMejoras()
    {
        TiendaMejoras.AbrirEnMenu();
    }

    // El boton atras de Android llega como Escape.
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) Menu();
    }
}
