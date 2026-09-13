using UnityEngine;

// El boton atras de Android en el menu principal, que Unity entrega como
// Escape. Con el panel de modos abierto vuelve al principal, como su boton
// "Back"; en el principal cierra el juego, que es lo que se espera en Android.
// En PC, Escape en el principal no hace nada: para salir esta el boton Quit.
//
// Va en el canvas y no en MainMenu porque MainMenu esta en los dos paneles, y
// el panel que se activa en este frame puede leer el mismo Escape.
public class BotonAtrasMenu : MonoBehaviour
{
    public GameObject menuPrincipal;
    public GameObject menuModos;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) Atras();
    }

    public void Atras()
    {
        if (menuModos.activeSelf)
        {
            menuModos.SetActive(false);
            menuPrincipal.SetActive(true);
        }
        else if (Plataforma.EsMovil)
        {
            Application.Quit();
        }
    }
}
