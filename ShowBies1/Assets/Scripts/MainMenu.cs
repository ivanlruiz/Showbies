using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class MainMenu : MonoBehaviour
{
   // El modo libre; si todavia no esta desbloqueado, las oleadas.
   public void PlayGame ()
    {
        SceneManager.LoadScene(ModoLibre.EscenaPara(TiendaMejoras.EscenaModoLibre));
    }

    public void QuitGame ()
    {
        Application.Quit();
       
    }

    public void GameModes()
    {
        SceneManager.LoadScene(3);
    }

    public void Tutorial()
    {
        SceneManager.LoadScene(4);
    }
}
