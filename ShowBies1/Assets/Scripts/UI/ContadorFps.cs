using UnityEngine;
using TMPro;

// FPS en una esquina del HUD, promediado cada medio segundo. Esta para poder
// medir en el telefono sin el Profiler: "anda lento" no se puede optimizar,
// "32 FPS con 40 zombis" si.
//
// Se ve solo si el jugador lo prende con MOSTRAR FPS en la ventana de opciones del
// menu (pedido de Ivan, 6/10): de fabrica esta apagado. Es una preferencia del
// dispositivo, como los volumenes, asi que va en PlayerPrefs y no en el progreso.
public class ContadorFps : MonoBehaviour
{
    public const string ClaveMostrar = "MostrarFps";

    public TMP_Text texto;
    public float intervalo = 0.5f;

    private int frames;
    private float acumulado;

    // -1 sin leer todavia.
    private static int mostrar = -1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetearEstadoCompartido()
    {
        mostrar = -1;
    }

    public static bool Mostrar
    {
        get
        {
            if (mostrar < 0) mostrar = PlayerPrefs.GetInt(ClaveMostrar, 0) != 0 ? 1 : 0;
            return mostrar == 1;
        }
    }

    // Lo escribe a disco quien lo cambia al terminar (la ventana de opciones al cerrarse).
    public static void FijarMostrar(bool valor)
    {
        mostrar = valor ? 1 : 0;
        PlayerPrefs.SetInt(ClaveMostrar, mostrar);
    }

    // En Awake y no recien en el primer Update: el texto de la escena dice algo y se
    // veria un cuadro.
    private void Awake()
    {
        if (texto != null) texto.enabled = Mostrar;
    }

    private void Update()
    {
        if (texto == null) return;
        bool ver = Mostrar;
        if (texto.enabled != ver)
        {
            texto.enabled = ver;
            frames = 0;
            acumulado = 0f;
        }
        if (!ver) return;

        frames++;
        acumulado += Time.unscaledDeltaTime;
        if (acumulado < intervalo) return;

        int fps = Mathf.RoundToInt(frames / acumulado);
        texto.text = fps + " FPS";
        frames = 0;
        acumulado = 0f;
    }
}
