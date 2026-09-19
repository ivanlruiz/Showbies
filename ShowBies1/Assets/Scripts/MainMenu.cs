using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class MainMenu : MonoBehaviour
{
    // La ventana de "¿SALIR DEL JUEGO?", en la raiz del canvas. La usa el panel
    // principal, que es el del boton SALIR; en el de modos queda vacia.
    public ConfirmarSalir confirmarSalir;

    // El panel de modos, que abre PLAY. Solo en el panel principal.
    public GameObject menuModos;

    // PLAY: la primera vez va derecho a la oleada 1 (PrimeraVez), sin el panel de modos:
    // cuatro opciones, el libre bloqueado y un tutorial opcional son demasiado para
    // alguien que acaba de instalar. Despues abre el panel de modos, como siempre.
    public void TocarJugar()
    {
        if (PrimeraVez.NuncaJugo)
        {
            SceneManager.LoadScene(TiendaMejoras.EscenaOleadas);
            return;
        }
        if (menuModos != null) menuModos.SetActive(true);
        gameObject.SetActive(false);
    }

   // El modo libre; si todavia no esta desbloqueado, las oleadas.
   public void PlayGame ()
    {
        SceneManager.LoadScene(ModoLibre.EscenaPara(TiendaMejoras.EscenaModoLibre));
    }

    // Pregunta antes de cerrar: el boton esta en el medio del menu y un toque sin querer
    // cerraba el juego. Si la ventana no esta, cierra como antes.
    public void QuitGame ()
    {
        if (confirmarSalir != null && confirmarSalir.Abrir()) return;
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
