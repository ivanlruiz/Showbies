using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Icono de cooldown en el HUD para la cadencia mejorada: un circulo que se
// vacia radialmente con el tiempo restante y los segundos en el centro.
// Aparece al agarrar un pickup de cadencia y desaparece cuando vence.
//
// Desde el 9/10 (pedido de Ivan: que se sienta agarrar una caja) es del color de la caja que lo
// prendio, dorado la de balas y celeste la del rayo, dice el multiplicador abajo ("x3", armado en
// codigo con una copia de los segundos) y salta cada vez que se agarra una, tambien la segunda,
// que pisa a la primera.
public class IndicadorMejoraCadencia : MonoBehaviour
{
    public GunController gun;
    public GameObject contenido;   // el hijo que se prende y apaga
    public Image relleno;          // Image en modo Filled Radial360
    public TMP_Text segundos;

    public Color colorBalas = new Color(1f, 0.78f, 0.22f);
    public Color colorRayo = new Color(0.1f, 0.9f, 1f);
    public float multiplicadorDelRayo = 2f;   // desde este multiplicador, es la caja del rayo
    public float duracionSalto = 0.35f;
    public float escalaDelSalto = 1.45f;

    private int ultimoSegundoMostrado = int.MinValue;
    private TMP_Text multiplicador;
    private float multiplicadorMostrado = -1f;
    private float ultimoRestante = -1f;
    private float saltoDesde = -10f;
    private Vector3 escalaBase = Vector3.one;

    private void Start()
    {
        if (contenido != null) escalaBase = contenido.transform.localScale;
        ArmarMultiplicador();
    }

    // Una copia de los segundos, mas chica, debajo del circulo.
    private void ArmarMultiplicador()
    {
        if (segundos == null) return;
        var copia = Instantiate(segundos.gameObject, segundos.transform.parent);
        copia.name = "Multiplicador";
        multiplicador = copia.GetComponent<TMP_Text>();
        var rt = multiplicador.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -2f);
        rt.sizeDelta = new Vector2(140f, 44f);
        multiplicador.enableAutoSizing = false;
        multiplicador.fontSize = segundos.fontSize * 0.6f;
        multiplicador.textWrappingMode = TextWrappingModes.NoWrap;
        multiplicador.alignment = TextAlignmentOptions.Top;
        multiplicador.text = "";
    }

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
                multiplicadorMostrado = -1f;
                ultimoRestante = -1f;
            }
            return;
        }

        if (!contenido.activeSelf) contenido.SetActive(true);

        float restante = gun.MejoraRestante;
        relleno.fillAmount = gun.duracionMejora > 0f ? restante / gun.duracionMejora : 0f;

        // Una caja nueva: el tiempo vuelve a llenarse.
        if (restante > ultimoRestante + 0.5f) saltoDesde = Time.unscaledTime;
        ultimoRestante = restante;
        float t = (Time.unscaledTime - saltoDesde) / Mathf.Max(0.01f, duracionSalto);
        float escala = t < 1f ? Mathf.Lerp(escalaDelSalto, 1f, 1f - (1f - t) * (1f - t)) : 1f;
        contenido.transform.localScale = escalaBase * escala;

        float veces = gun.MultiplicadorCadencia;
        if (!Mathf.Approximately(veces, multiplicadorMostrado))
        {
            multiplicadorMostrado = veces;
            Color color = veces >= multiplicadorDelRayo ? colorRayo : colorBalas;
            color.a = relleno.color.a;
            relleno.color = color;
            if (multiplicador != null)
            {
                multiplicador.color = new Color(color.r, color.g, color.b, 1f);
                multiplicador.text = Textos.Formato("hud_multiplicador", FormatoNumeros.ConDecimales(veces, 1));
            }
        }

        // Solo al cambiar de segundo: escribir el texto por frame aloca por frame.
        int segundoActual = Mathf.CeilToInt(restante);
        if (segundoActual != ultimoSegundoMostrado)
        {
            ultimoSegundoMostrado = segundoActual;
            segundos.text = segundoActual.ToString();
        }
    }
}
