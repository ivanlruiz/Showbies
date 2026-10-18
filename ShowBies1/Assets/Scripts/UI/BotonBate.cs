using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// El boton del bate en el HUD (1.7.0, ver Bate): un poco mas chico que el de la granada y a su
// izquierda, en el lado del pulgar que dispara. Pega al apoyar el dedo y no al soltarlo, como un
// boton de panico tiene que hacer. La sombra radial muestra el enfriamiento, con los segundos que
// faltan; listo, respira, y al volver a estarlo salta. En PC tambien esta, para ver la recarga,
// con lo que hay que apretar (clic derecho o Shift).
//
// No esta en las escenas: lo arma Bate al empezar, como copia del boton de la furia (el anillo, la
// sombra, los textos y el lugar en el area segura), en el color del bate. Donde no hay boton de la
// furia (el tutorial) no hay boton del bate.
public class BotonBate : MonoBehaviour, IPointerDownHandler
{
    // A la izquierda del de la granada, a su altura, con su brillo sin tocarla, y por encima del
    // numero de la vida (la prueba de logica lo mide; los cuatro cuelgan del mismo punto, el 90 % del
    // ancho y el 10 % del alto del area segura, asi que vale en cualquier proporcion). En PC no hay
    // boton de granada: va en su lugar, debajo del de la furia.
    public static readonly Vector2 LugarEnElTelefono = new Vector2(-575f, 100f);
    public static readonly Vector2 LugarEnPC = new Vector2(-400f, 110f);
    public const float Escala = 0.8f;   // el de la furia mide 150: este, 120 (el de la granada, 140)

    public static readonly Color ColorListo = Bate.ColorDelBate;
    public static readonly Color ColorEnfriando = new Color(0.36f, 0.14f, 0.3f);

    public RectTransform visual;
    public Image fondo;
    public Image sombra;
    public TMPro.TMP_Text etiqueta;
    public TMPro.TMP_Text segundos;

    private Bate bate;
    private Vector3 escalaBaseVisual = Vector3.one;
    private int segundosMostrados = -1;
    private bool estabaListo = true;
    private float saltoDesde = -1f;      // en tiempo sin escalar; negativo sin salto
    private float golpeDesde = -1f;
    private int batazosVistos;
    private const float DuracionSalto = 0.35f;

    // El que hay en la escena, para las pruebas.
    public static BotonBate Puesto { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetearEstadoCompartido()
    {
        Puesto = null;
    }

    public static BotonBate Instalar(Bate bate)
    {
        if (bate == null || !bate.Desbloqueado) return null;
        var furia = FindAnyObjectByType<BotonFuria>(FindObjectsInactive.Include);
        if (furia == null || furia.boton == null) return null;

        var copia = Instantiate(furia.boton.gameObject, furia.boton.transform.parent);
        copia.name = "BotonBate";
        // Lo del boton de la furia que no es del bate: lo que escribe un texto por su cuenta.
        foreach (var traducido in copia.GetComponentsInChildren<TextoTraducido>(true)) Destroy(traducido);
        var button = copia.GetComponent<Button>();
        if (button != null) button.onClick.RemoveAllListeners();

        var rect = (RectTransform)copia.transform;
        rect.anchoredPosition = Plataforma.EsMovil ? LugarEnElTelefono : LugarEnPC;
        rect.localScale = Vector3.one * Escala;
        copia.SetActive(true);

        var boton = copia.AddComponent<BotonBate>();
        boton.bate = bate;
        boton.visual = copia.transform.Find("Visual") as RectTransform;
        boton.fondo = Buscar<Image>(copia.transform, "Visual/Fondo");
        boton.sombra = Buscar<Image>(copia.transform, "Visual/Sombra");
        boton.etiqueta = Buscar<TMPro.TMP_Text>(copia.transform, "Visual/Etiqueta");
        boton.segundos = Buscar<TMPro.TMP_Text>(copia.transform, "Visual/Segundos");
        var neon = Buscar<Image>(copia.transform, "Neon");
        if (neon != null) neon.color = new Color(ColorListo.r, ColorListo.g, ColorListo.b, neon.color.a);
        Puesto = boton;
        return boton;
    }

    private static T Buscar<T>(Transform raiz, string camino) where T : Component
    {
        var hijo = raiz.Find(camino);
        return hijo != null ? hijo.GetComponent<T>() : null;
    }

    private void Start()
    {
        if (visual != null) escalaBaseVisual = visual.localScale;
        if (etiqueta != null)
        {
            etiqueta.gameObject.SetActive(true);
            etiqueta.text = Plataforma.EsMovil ? Textos.De("bate_boton") : Textos.De("bate_boton_pc");
        }
        if (segundos != null) segundos.gameObject.SetActive(false);
        if (sombra != null) sombra.fillAmount = 0f;
        if (bate != null) batazosVistos = bate.Batazos;
    }

    private void OnDestroy()
    {
        if (Puesto == this) Puesto = null;
    }

    public void OnPointerDown(PointerEventData evento)
    {
        if (bate != null) bate.Golpear();
    }

    private void Update()
    {
        if (bate == null) return;

        if (bate.Batazos != batazosVistos)
        {
            batazosVistos = bate.Batazos;
            golpeDesde = Time.unscaledTime;
        }

        float enfriando = bate.RestanteEnfriamiento;
        bool listo = enfriando <= 0f;
        // Volvio a estar listo: salta (sin sonido: cada cuatro segundos seria ruido).
        if (listo && !estabaListo && !MenuPausa.JuegoCongelado) saltoDesde = Time.unscaledTime;
        estabaListo = listo;

        float cubierto = listo ? 0f : enfriando / Mathf.Max(0.01f, bate.enfriamiento);
        Color color = listo ? ColorListo : ColorEnfriando;
        int cuenta = listo ? 0 : Mathf.CeilToInt(enfriando);

        // Tocar la UI la reconstruye: solo se escribe lo que cambio.
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

        if (visual == null) return;
        float escala = 1f;
        if (!MenuPausa.JuegoCongelado)
        {
            if (listo) escala = 1f + 0.05f * (0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 1.2f * Time.unscaledTime));
            // Al pegar se hunde y rebota: que el dedo sienta que salio.
            if (golpeDesde >= 0f)
            {
                float t = (Time.unscaledTime - golpeDesde) / 0.25f;
                if (t >= 1f) golpeDesde = -1f;
                else escala *= 1f - 0.18f * CurvasUI.Campana(t);
            }
            if (saltoDesde >= 0f)
            {
                float t = (Time.unscaledTime - saltoDesde) / DuracionSalto;
                if (t >= 1f) saltoDesde = -1f;
                else escala *= 1f + 0.25f * CurvasUI.Campana(t);
            }
        }
        visual.localScale = escalaBaseVisual * escala;
    }
}
