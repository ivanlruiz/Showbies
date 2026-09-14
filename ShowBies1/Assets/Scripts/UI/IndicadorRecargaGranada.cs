using UnityEngine;
using UnityEngine.UI;

// Anillo de recarga sobre el boton de granada: una sombra radial que se vacia
// mientras la granada no esta disponible. Solo escribe fillAmount cuando cambia,
// porque tocarlo reconstruye el canvas.
public class IndicadorRecargaGranada : MonoBehaviour
{
    public PlayerController jugador;
    public Image relleno;   // Image Filled Radial360, encima del boton y debajo de la "G"

    private void Update()
    {
        if (jugador == null || relleno == null) return;

        float valor = jugador.granadaCooldown > 0f ? jugador.GranadaRestante / jugador.granadaCooldown : 0f;
        if (!Mathf.Approximately(relleno.fillAmount, valor)) relleno.fillAmount = valor;
    }
}
