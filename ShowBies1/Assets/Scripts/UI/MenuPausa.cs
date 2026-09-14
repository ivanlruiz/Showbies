using UnityEngine;
using UnityEngine.SceneManagement;

// Pausa de las escenas de juego. Se abre con el boton de pausa del HUD (solo en
// movil), con Escape en PC y con el boton atras de Android, que Unity entrega
// como Escape. Tambien se abre sola cuando la app pierde el foco (una llamada,
// la cortina de notificaciones, alt-tab), para que la partida no siga corriendo
// sin nadie jugando.
//
// Pausar es Time.timeScale = 0, que congela la fisica, las corrutinas con
// WaitForSeconds y todo lo que cuenta con Time.time o Time.deltaTime. El input
// no se congela: PlayerController y PlayerJS miran Pausado antes de leerlo.
public class MenuPausa : MonoBehaviour
{
    public GameObject panel;

    public static bool Pausado { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetearEstadoCompartido()
    {
        Pausado = false;
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape)) return;

        if (Pausado) Reanudar();
        else Pausar();
    }

    // En el editor no: pierde el foco con cada click en otra ventana.
    private void OnApplicationFocus(bool tieneFoco)
    {
        if (!tieneFoco && !Application.isEditor) Pausar();
    }

    private void OnApplicationPause(bool enPausa)
    {
        if (enPausa && !Application.isEditor) Pausar();
    }

    public void Pausar()
    {
        if (Pausado) return;

        Pausado = true;
        Time.timeScale = 0f;
        AudioListener.pause = true;
        panel.SetActive(true);

        // Pausar tambien pasa cuando la app pierde el foco, y despues Android la
        // puede matar sin avisar: es el ultimo momento seguro para guardar.
        Progreso.Guardar();
    }

    public void Reanudar()
    {
        if (!Pausado) return;

        Restaurar();
        panel.SetActive(false);
    }

    public void Reiniciar()
    {
        Restaurar();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void IrAlMenu()
    {
        Restaurar();
        SceneManager.LoadScene(0);
    }

    // timeScale y AudioListener.pause son globales: si la escena se descarga en
    // pausa por otro camino (la R de RestartScene), la siguiente arrancaria
    // congelada y muda.
    private void OnDestroy()
    {
        if (Pausado) Restaurar();
    }

    private static void Restaurar()
    {
        Pausado = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }
}
