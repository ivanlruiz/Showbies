using UnityEngine;
using UnityEngine.EventSystems;

// El boton de granada como joystick. Arrastrar desde donde se apoyo el dedo apunta
// (direccion y distancia) y el jugador marca en el piso donde va a caer; soltar la
// tira. Un toque sin arrastrar la tira rapido, y volver al centro antes de soltar
// cancela el tiro.
//
// No es un Joystick del Joystick Pack porque ese mide desde el centro del boton:
// con un boton chico, un toque cerca del borde ya contaria como apuntar.
public class JoystickGranada : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public PlayerController jugador;
    public RectTransform mango;            // lo que sigue al dedo (la "G")
    public float radioArrastre = 110f;     // unidades del canvas que hay que arrastrar para llegar a la distancia maxima
    [Range(0f, 1f)] public float zonaMuerta = 0.2f;

    private Canvas canvas;
    private Vector2 inicio;
    private Vector2 palanca;               // direccion por cuanto se arrastro, de 0 a 1, ya sin la zona muerta
    private bool apretado;
    private bool apunto;

    private void Awake()
    {
        canvas = GetComponentInParent<Canvas>().rootCanvas;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        apretado = true;
        apunto = false;
        inicio = eventData.position;
        palanca = Vector2.zero;
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 arrastre = (eventData.position - inicio) / (radioArrastre * canvas.scaleFactor);
        if (arrastre.magnitude > 1f) arrastre.Normalize();
        if (mango != null) mango.anchoredPosition = arrastre * radioArrastre;

        float fuera = arrastre.magnitude > zonaMuerta ? (arrastre.magnitude - zonaMuerta) / (1f - zonaMuerta) : 0f;
        palanca = fuera > 0f ? arrastre.normalized * fuera : Vector2.zero;
        if (fuera > 0f) apunto = true;
    }

    private void Update()
    {
        if (!apretado || jugador == null) return;

        if (palanca != Vector2.zero) jugador.ApuntarGranada(palanca);
        else jugador.OcultarPunteroGranada();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        apretado = false;
        if (mango != null) mango.anchoredPosition = Vector2.zero;
        if (jugador == null) return;

        if (!apunto) jugador.ThrowGranade();
        else if (palanca != Vector2.zero) jugador.TirarGranadaApuntada(palanca);
        else jugador.OcultarPunteroGranada();
    }
}
