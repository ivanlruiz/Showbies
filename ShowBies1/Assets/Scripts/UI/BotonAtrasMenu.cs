using UnityEngine;

// El boton atras de Android en el menu principal, que Unity entrega como
// Escape. Con el selector de idioma abierto lo cierra; con la tienda de mejoras
// abierta la cierra, como su boton "VOLVER";
// con el panel de modos abierto vuelve al principal, como su boton "Back"; en
// el principal cierra el juego, que es lo que se espera en Android. En PC,
// Escape en el principal no hace nada: para salir esta el boton Quit.
//
// Es el unico lector de Escape del menu. Va en el canvas y no en MainMenu porque
// MainMenu esta en los dos paneles, y el panel que se activa en este frame puede
// leer el mismo Escape; por lo mismo la tienda no lee Escape por su cuenta: si
// lo hicieran los dos, un solo toque cerraria la tienda y saldria del juego.
public class BotonAtrasMenu : MonoBehaviour
{
    public GameObject menuPrincipal;
    public GameObject menuModos;
    public TiendaMejoras tienda;
    public SelectorIdioma selectorIdioma;
    public OpcionesSonido opcionesSonido;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) Atras();
    }

    public void Atras()
    {
        if (opcionesSonido != null && opcionesSonido.Abierto)
        {
            opcionesSonido.Cerrar();
            return;
        }

        if (selectorIdioma != null && selectorIdioma.Abierto)
        {
            selectorIdioma.Cerrar();
            return;
        }

        if (tienda != null && tienda.Abierta)
        {
            tienda.Cerrar();
            return;
        }

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
