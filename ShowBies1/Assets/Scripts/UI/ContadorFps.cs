using UnityEngine;
using TMPro;

// FPS en una esquina del HUD, promediado cada medio segundo. Esta para poder
// medir en el telefono sin el Profiler: "anda lento" no se puede optimizar,
// "32 FPS con 40 zombis" si.
public class ContadorFps : MonoBehaviour
{
    public TMP_Text texto;
    public float intervalo = 0.5f;

    private int frames;
    private float acumulado;

    private void Update()
    {
        frames++;
        acumulado += Time.unscaledDeltaTime;
        if (acumulado < intervalo) return;

        int fps = Mathf.RoundToInt(frames / acumulado);
        texto.text = fps + " FPS";
        frames = 0;
        acumulado = 0f;
    }
}
