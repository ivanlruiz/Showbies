using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuPerdiste : MonoBehaviour
{
    // La derrota va encima de la partida congelada y no sola: la carga en modo aditivo
    // DerrotaEnLaPartida, que prende esto antes de dejarla arrancar. Sin esto la escena
    // es la de siempre, con su camara y su fondo (asi se ve si se abre sola).
    public static bool SobreLaPartida;

    // Cuanto tapa el fondo de la derrota cuando va encima de la partida: nada. Lo que
    // tiene que verse detras es el mundo poniendose gris ("la pantalla y el gris", pidio
    // Ivan); con el celeste encima, aunque fuera translucido, el gris no se notaba. Los
    // textos llevan contorno y se leen sobre el mundo, como el HUD.
    [Range(0f, 1f)] public float opacidadDelFondo = 0f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetearEstadoCompartido()
    {
        SobreLaPartida = false;
    }

    // Encima de la partida, la camara, el oido y el EventSystem son los del juego: los
    // de esta escena sobran (dos EventSystem o dos AudioListener se pelean y avisan en
    // cada cuadro), y la camara taparia el mundo con el cielo. La luz tambien: esta
    // escena trae la Directional Light de siempre, y cargada encima es un segundo sol
    // sobre el mundo de la partida, que quedaba sobreexpuesto, casi blanco. En Awake,
    // antes del primer cuadro.
    private void Awake()
    {
        if (!SobreLaPartida) return;

        foreach (GameObject raiz in gameObject.scene.GetRootGameObjects())
        {
            foreach (var camara in raiz.GetComponentsInChildren<Camera>(true)) camara.enabled = false;
            foreach (var oido in raiz.GetComponentsInChildren<AudioListener>(true)) oido.enabled = false;
            foreach (var luz in raiz.GetComponentsInChildren<Light>(true)) luz.enabled = false;
            foreach (var sistema in raiz.GetComponentsInChildren<UnityEngine.EventSystems.EventSystem>(true))
                sistema.gameObject.SetActive(false);
            foreach (var pintar in raiz.GetComponentsInChildren<PintarConTema>(true))
                if (pintar.rol == RolDeTema.Fondo) pintar.FijarOpacidad(opacidadDelFondo);
        }
    }

    private void OnDestroy()
    {
        SobreLaPartida = false;
    }

    // La partida de abajo quedo en timeScale 0, y el timeScale es global: vuelve a 1
    // antes de cargar lo que sigue, o la escena nueva arranca congelada.
    private static void Salir()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SobreLaPartida = false;
    }

    public void Retry ()
    {
        Salir();
        // Vuelve al modo que se estaba jugando, no siempre al primero.
        // PlayerHealth lo guarda al morir; si no hay nada, a las oleadas. El libre
        // pasa por ModoLibre por si todavia no esta desbloqueado.
        int modo = PlayerPrefs.GetInt("UltimoModo", TiendaMejoras.EscenaOleadas);
        SceneManager.LoadScene(ModoLibre.EscenaPara(modo));
    }

    public void Menu()
    {
        Salir();
        SceneManager.LoadScene(0);
    }

    // El boton MEJORAS de la derrota: la tienda vive en el menu, asi que carga
    // el menu con la tienda ya abierta. Es el momento en que el jugador acaba de
    // cobrar y tiene mas ganas de gastar.
    public void AbrirMejoras()
    {
        Salir();
        TiendaMejoras.AbrirEnMenu();
    }

    // El boton atras de Android llega como Escape.
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) Menu();
    }
}
