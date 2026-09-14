using TMPro;
using UnityEngine;

// Monedas acumuladas, en el HUD de las escenas de juego. Reescribe el texto solo
// cuando cambia la parte entera: armarlo por frame aloca por frame. Cuando sube,
// el texto da un salto y se prende, para que cada moneda agarrada se note.
public class ContadorMonedas : MonoBehaviour
{
    public TMP_Text texto;
    public float escalaDelSalto = 1.4f;
    public float duracionDelSalto = 0.2f;
    public Color colorDelSalto = Color.white;

    private long mostradas = -1;
    private float salto;          // 1 al subir, baja a 0
    private Color colorBase;
    private Vector3 escalaBase;

    private void Awake()
    {
        colorBase = texto.color;
        escalaBase = texto.rectTransform.localScale;
    }

    private void Update()
    {
        long actuales = (long)System.Math.Floor(Progreso.Monedas);
        if (actuales != mostradas)
        {
            // El primer texto de la escena no salta: no se agarro nada.
            if (mostradas >= 0 && actuales > mostradas) salto = 1f;

            mostradas = actuales;
            texto.text = "Monedas: " + FormatoNumeros.Compacto(actuales);
        }

        if (salto <= 0f) return;

        // Tiempo sin escalar: si se pausa justo al agarrar una, que no quede grande.
        salto = Mathf.Max(0f, salto - Time.unscaledDeltaTime / duracionDelSalto);
        float curva = salto * salto;
        texto.rectTransform.localScale = escalaBase * Mathf.Lerp(1f, escalaDelSalto, curva);
        texto.color = Color.Lerp(colorBase, colorDelSalto, curva);
    }
}
