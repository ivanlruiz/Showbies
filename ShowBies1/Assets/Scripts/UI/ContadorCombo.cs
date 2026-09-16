using TMPro;
using UnityEngine;

// "COMBO x12" en el HUD: cuenta los zombis que mueren seguidos, sin que pasen mas
// de ventana segundos entre uno y otro. Aparece desde minimoParaMostrar, salta
// con cada muerte, cambia de color cada muertesPorColor y se desvanece cuando se
// corta la racha.
//
// Las muertes llegan por RegistrarMuerte (lo llama Efectos.Muerte) a un contador
// static y se procesan en Update: asi no importa cuantas mueran en un frame.
public class ContadorCombo : MonoBehaviour
{
    public TMP_Text texto;
    public float ventana = 1.5f;
    public int minimoParaMostrar = 3;
    public int muertesPorColor = 10;
    public Color[] colores =
    {
        Color.white,
        new Color(1f, 0.9f, 0.2f),
        new Color(1f, 0.55f, 0.1f),
        new Color(1f, 0.25f, 0.6f),
        new Color(0.6f, 0.4f, 1f),
    };
    public float escalaDelSalto = 1.5f;
    public float duracionDelSalto = 0.15f;
    public float desvanecido = 0.4f;          // segundos del final de la ventana en que se va apagando

    private static int muertesSinProcesar;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetearEstadoCompartido()
    {
        muertesSinProcesar = 0;
    }

    public static void RegistrarMuerte()
    {
        muertesSinProcesar++;
    }

    private int combo;
    private float ultimaMuerte = float.NegativeInfinity;
    private float salto;
    private Vector3 escalaBase;
    private Color colorActual;

    private void Awake()
    {
        escalaBase = texto.rectTransform.localScale;
        texto.enabled = false;
        // Las que se registraron en otra escena, o sin HUD, no cuentan aca.
        muertesSinProcesar = 0;
    }

    private void Update()
    {
        if (MenuPausa.Pausado) return;

        if (muertesSinProcesar > 0)
        {
            if (Time.time - ultimaMuerte > ventana) combo = 0;
            combo += muertesSinProcesar;
            muertesSinProcesar = 0;
            ultimaMuerte = Time.time;

            if (combo >= minimoParaMostrar)
            {
                colorActual = colores[Mathf.Min(combo / Mathf.Max(1, muertesPorColor), colores.Length - 1)];
                texto.SetText(Textos.De("hud_combo"), combo);
                texto.enabled = true;
                salto = 1f;
            }
        }

        if (!texto.enabled) return;

        float resto = ventana - (Time.time - ultimaMuerte);
        if (resto <= 0f)
        {
            combo = 0;
            texto.enabled = false;
            return;
        }

        Color c = colorActual;
        c.a = Mathf.Clamp01(resto / desvanecido);
        texto.color = c;

        if (salto > 0f) salto = Mathf.Max(0f, salto - Time.unscaledDeltaTime / duracionDelSalto);
        texto.rectTransform.localScale = escalaBase * Mathf.Lerp(1f, escalaDelSalto, salto * salto);
    }
}
