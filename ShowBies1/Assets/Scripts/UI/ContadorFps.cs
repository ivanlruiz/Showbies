using UnityEngine;
using TMPro;

// FPS en una esquina del HUD, promediado cada medio segundo. Esta para poder
// medir en el telefono sin el Profiler: "anda lento" no se puede optimizar,
// "32 FPS con 40 zombis" si.
//
// Se ve solo si el jugador lo prende con MOSTRAR FPS en la ventana de opciones del
// menu (pedido de Ivan, 6/10): de fabrica esta apagado. Es una preferencia del
// dispositivo, como los volumenes, asi que va en PlayerPrefs y no en el progreso.
//
// Desde el 10/10 (los tirones del Redmi Note 8) dice ademas el peor cuadro de los ultimos
// segundos y cuantos tirones hubo desde que empezo la partida: "47 FPS · PEOR 120 MS · 3
// TIRONES". Un promedio bajo y parejo es temblor; saltos sueltos son otra cosa (un tipo de zombi
// que sale por primera vez, la primera explosion), y se ven en el numero de tirones.
public class ContadorFps : MonoBehaviour
{
    public const string ClaveMostrar = "MostrarFps";

    // Un cuadro de mas de esto es un tiron (a 30 FPS un cuadro dura 33 ms).
    public const float CuadroDeTiron = 0.05f;
    // El peor cuadro se sostiene en pantalla este rato, para que se alcance a leer.
    public const float SegundosDelPeor = 5f;
    // Los primeros cuadros de una escena son la carga: no cuentan.
    public const float SegundosSinContar = 2f;

    public TMP_Text texto;
    public float intervalo = 0.5f;

    private int frames;
    private float acumulado;
    private float peor;
    private float peorDesde;
    private int tirones;
    private float desde;

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
        if (texto != null)
        {
            texto.enabled = Mostrar;
            // Con el peor cuadro el renglon es mas largo: que no se parta en dos.
            texto.textWrappingMode = TextWrappingModes.NoWrap;
        }
        desde = Time.realtimeSinceStartup;
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

        float cuadro = Time.unscaledDeltaTime;
        float ahora = Time.realtimeSinceStartup;
        if (ahora - desde >= SegundosSinContar)
        {
            if (cuadro > CuadroDeTiron) tirones++;
            if (cuadro >= peor || ahora - peorDesde > SegundosDelPeor)
            {
                peor = cuadro;
                peorDesde = ahora;
            }
        }

        frames++;
        acumulado += cuadro;
        if (acumulado < intervalo) return;

        int fps = Mathf.RoundToInt(frames / acumulado);
        texto.text = Textos.Formato("hud_fps", fps, Mathf.RoundToInt(peor * 1000f), tirones);
        frames = 0;
        acumulado = 0f;
    }
}
