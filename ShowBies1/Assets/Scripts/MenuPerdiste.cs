using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuPerdiste : MonoBehaviour
{
    public void Retry ()
    {
        // Vuelve al modo que se estaba jugando, no siempre al primero.
        // PlayerHealth lo guarda al morir; si no hay nada, cae en ShowBies1.
        SceneManager.LoadScene(PlayerPrefs.GetInt("UltimoModo", 1));
    }

    public void Menu()
    {
        SceneManager.LoadScene(0);
    }
}
