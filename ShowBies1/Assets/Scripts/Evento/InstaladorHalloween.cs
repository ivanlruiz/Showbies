using System;
using UnityEngine;
using UnityEngine.SceneManagement;

// Pone Halloween en cada escena que carga (EventoHalloween), sin que las escenas lo traigan:
// asi el evento no toca Menu, ShowBies1, WaveMode ni Tutorial, y cuando termina no queda nada.
//
// - El menu: las calabazas del fondo y el boton HALLOWEEN con su ventana. Al entrar cierra la
//   edicion que termino (cobra lo que quedo sin cobrar).
// - Las partidas: las calabazas por el mapa y los caramelos en el HUD (en el tutorial no, que
//   no suelta nada). Los zombis se disfrazan solos (EnemyController.Awake) y los del menu,
//   en FondoMenu.
// - El sombrero de calabaza del jugador, en las tres escenas de juego, con evento o sin el.
//
// La derrota se carga encima de la partida y no lleva nada. Nada de esto puede romper una
// escena: si algo falla, se anota y la escena sigue sin Halloween.
public static class InstaladorHalloween
{
    // Los indices de Build Settings (ver la tabla de CLAUDE.md).
    public const int EscenaMenu = 0;
    public const int EscenaLibre = 1;
    public const int EscenaOleadas = 3;
    public const int EscenaTutorial = 4;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Suscribir()
    {
        // Sin recargar el dominio al entrar en play, el static sigue suscripto: sin duplicar.
        SceneManager.sceneLoaded -= AlCargar;
        SceneManager.sceneLoaded += AlCargar;
    }

    private static void AlCargar(Scene escena, LoadSceneMode modo)
    {
        try
        {
            Instalar(escena);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    public static void Instalar(Scene escena)
    {
        int indice = escena.buildIndex;
        if (indice == EscenaMenu)
        {
            EventoHalloween.CerrarSiTermino();
            if (!EventoHalloween.Activo) return;
            EventoHalloween.Asegurar();
            DecoradoHalloween.EnElMenu(escena, GiroDeLaCamaraDelMenu(escena));
            VentanaHalloween.Instalar(escena);
            return;
        }

        if (indice != EscenaLibre && indice != EscenaOleadas && indice != EscenaTutorial) return;
        if (EventoHalloween.LlevaSombrero) SombreroDelJugador.Instalar(escena);
        if (!EventoHalloween.Activo) return;
        EventoHalloween.Asegurar();
        DecoradoHalloween.EnLaPartida(escena);
        if (indice != EscenaTutorial) ContadorCaramelos.Instalar(escena);
    }

    // La camara del menu la acomoda FondoMenu en su Start, que todavia no corrio.
    private static Quaternion GiroDeLaCamaraDelMenu(Scene escena)
    {
        foreach (var raiz in escena.GetRootGameObjects())
        {
            var fondo = raiz.GetComponentInChildren<FondoMenu>(true);
            if (fondo != null) return Quaternion.Euler(fondo.rotacionCamara);
        }
        return Camera.main != null ? Camera.main.transform.rotation : Quaternion.identity;
    }
}
