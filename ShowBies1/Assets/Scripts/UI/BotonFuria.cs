using UnityEngine;
using UnityEngine.UI;

// El boton de la furia en el HUD: aparece solo si se compro y se toca para
// activarla. La sombra radial muestra lo que falta: mientras dura la furia crece
// hasta cubrirlo, y en el enfriamiento se va retirando mientras el numero cuenta
// los segundos. Listo, late. Cada vez que la furia arranca (con el boton o con la
// tecla) prende el cartel "¡FURIA!" del centro.
//
// Es el prefab Prefabs/UI/BotonFuria, estirado sobre el area segura del HUD: el
// boton en la esquina, arriba del de granada, y el cartel en el centro. Anima el
// hijo "visual" y nunca la raiz del boton, asi el area que se toca no cambia.
public class BotonFuria : MonoBehaviour
{
    public Button boton;
    public RectTransform visual;
    public Image fondo;
    public Image sombra;                  // Image Filled Radial360 encima del fondo
    public TMPro.TMP_Text etiqueta;
    public TMPro.TMP_Text segundos;
    public GameObject cartel;             // "¡FURIA!", con AparecerConRebote
    public float duracionCartel = 1.2f;

    public Color colorListo = new Color(1f, 0.28f, 0.12f);
    public Color colorActivo = new Color(1f, 0.82f, 0.2f);
    public Color colorEnfriando = new Color(0.45f, 0.22f, 0.18f);

    private Furia furia;
    private int activacionesVistas;
    private float cartelHasta = -1f;     // en tiempo sin escalar; negativo sin cartel
    private int segundosMostrados = -1;
    private Vector3 escalaBaseVisual = Vector3.one;

    // Start y no Awake: Furia se registra en el Awake del jugador.
    private void Start()
    {
        furia = Furia.Instancia;
        bool hay = furia != null && furia.Desbloqueada;

        if (boton != null)
        {
            boton.gameObject.SetActive(hay);
            if (hay) boton.onClick.AddListener(Tocar);
        }
        if (cartel != null) cartel.SetActive(false);
        if (visual != null) escalaBaseVisual = visual.localScale;
        if (etiqueta != null) etiqueta.text = Plataforma.EsMovil ? "FURIA" : "FURIA\n<size=55%>(F)</size>";
        if (segundos != null) segundos.gameObject.SetActive(false);
        if (furia != null) activacionesVistas = furia.Activaciones;
    }

    private void Tocar()
    {
        if (furia != null) furia.Activar();
    }

    private void Update()
    {
        if (furia == null || !furia.Desbloqueada) return;

        if (furia.Activaciones != activacionesVistas)
        {
            activacionesVistas = furia.Activaciones;
            if (cartel != null)
            {
                // Apagar y prender reinicia el rebote aunque siga prendido.
                cartel.SetActive(false);
                cartel.SetActive(true);
                cartelHasta = Time.unscaledTime + duracionCartel;
            }
        }
        if (cartelHasta >= 0f && Time.unscaledTime >= cartelHasta)
        {
            cartelHasta = -1f;
            if (cartel != null) cartel.SetActive(false);
        }

        bool activa = furia.Activa;
        float enfriando = furia.RestanteEnfriamiento;
        bool lista = !activa && enfriando <= 0f;

        float cubierto;
        Color color;
        int cuenta = 0;
        if (activa)
        {
            cubierto = 1f - furia.RestanteFuria / Mathf.Max(0.01f, furia.Duracion);
            color = colorActivo;
        }
        else if (!lista)
        {
            cubierto = enfriando / Mathf.Max(0.01f, furia.enfriamiento);
            color = colorEnfriando;
            cuenta = Mathf.CeilToInt(enfriando);
        }
        else
        {
            cubierto = 0f;
            color = colorListo;
        }

        // Tocar la UI la reconstruye: sólo se escribe lo que cambió.
        if (sombra != null && !Mathf.Approximately(sombra.fillAmount, cubierto)) sombra.fillAmount = cubierto;
        if (fondo != null && fondo.color != color) fondo.color = color;
        if (cuenta != segundosMostrados)
        {
            segundosMostrados = cuenta;
            if (segundos != null)
            {
                segundos.gameObject.SetActive(cuenta > 0);
                if (cuenta > 0) segundos.SetText("{0}", cuenta);
            }
            if (etiqueta != null) etiqueta.gameObject.SetActive(cuenta <= 0);
        }

        if (visual != null)
        {
            // Lista, respira; activa, vibra grande. Sin escalar: la pausa de impacto
            // del arranque no la congela.
            float escala = 1f;
            if (lista) escala = 1f + 0.07f * (0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 1.5f * Time.unscaledTime));
            else if (activa) escala = 1.12f + 0.04f * Mathf.Sin(2f * Mathf.PI * 7f * Time.unscaledTime);
            visual.localScale = escalaBaseVisual * escala;
        }
    }
}
