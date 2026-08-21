using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Icono de cooldown en el HUD para la cadencia mejorada: un circulo que se
// vacia radialmente con el tiempo restante y los segundos en el centro.
// Aparece al agarrar un pickup de cadencia y desaparece cuando vence.
public class IndicadorMejoraCadencia : MonoBehaviour
{
    public GunController gun;
    public GameObject contenido;   // el hijo que se prende y apaga
    public Image relleno;          // Image en modo Filled Radial360
    public TMP_Text segundos;

    private int ultimoSegundoMostrado = int.MinValue;

    private void Update()
    {
        // El script vive en el padre y apaga solo al hijo: si apagara su propio
        // GameObject, este Update dejaria de correr y no podria volver a prenderse.
        if (gun == null || !gun.MejoraActiva)
        {
            if (contenido.activeSelf)
            {
                contenido.SetActive(false);
                ultimoSegundoMostrado = int.MinValue;
            }
            return;
        }

        if (!contenido.activeSelf) contenido.SetActive(true);

        float restante = gun.MejoraRestante;
        relleno.fillAmount = gun.duracionMejora > 0f ? restante / gun.duracionMejora : 0f;

        // Solo al cambiar de segundo: escribir el texto por frame aloca por frame.
        int segundoActual = Mathf.CeilToInt(restante);
        if (segundoActual != ultimoSegundoMostrado)
        {
            ultimoSegundoMostrado = segundoActual;
            segundos.text = segundoActual.ToString();
        }
    }
}
