using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Hace que un boton se sienta al tocarlo: al apoyar el dedo se aprieta y suena en
// el mismo frame, al soltar rebota, y si 'respirar' esta prendido late despacio
// para decir "esto se puede tocar". Ademas expone Golpe y Sacudir para que otros
// (la tienda, las tarjetas) lo festejen o lo rechacen.
//
// Anima SIEMPRE a 'visual', un hijo, nunca a la raiz: la raiz es la que recibe el
// toque y la que acomodan los layouts. Si se achicara la raiz, el area tocable se
// achicaria con ella (un toque en el borde entraria y saldria del boton) y un
// LayoutGroup le pisaria la posicion de la sacudida.
//
// Con tiempo sin escalar: la tienda y la derrota no tienen timeScale propio, pero
// la pausa de impacto de Efectos no tiene que congelar un boton.
//
// El clic sale por Sonidos.TocarUI, que la pausa no calla: con AudioListener.pause el
// de CONTINUAR sonaba recien al reanudar, y el de MENU y REINICIAR nunca.
public class BotonJugoso : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public RectTransform visual;
    public float escalaApretado = 0.9f;
    public bool respirar;
    public float amplitudRespiracion = 0.04f;
    public float frecuenciaRespiracion = 1.4f;
    public AudioClip sonidoClick;           // vacio: el clic de siempre (ClicPorDefecto)
    public float volumenClick = 0.4f;
    public float semitonosClick = 7f;

    private const float VelocidadApretado = 18f;

    // El clic de los que no traen uno. Casi ningun boton de las escenas lo tenia puesto:
    // en el menu solo sonaba MEJORAS, y PLAY, los modos, SALIR, la pausa y todo lo que
    // arman en codigo las ventanas del menu (que pasan su sonidoClick, tambien vacio)
    // eran mudos. Todos los que lo tienen usan golpe.wav, asi que el primero que aparece
    // con uno se lo presta a los demas: el menu es la primera escena y MEJORAS esta
    // prendido desde que carga. En una partida abierta directo desde el editor, el golpe
    // de Efectos, que es el mismo sonido.
    private static AudioClip clicPrestado;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetearEstadoCompartido()
    {
        clicPrestado = null;
    }

    public static AudioClip ClicPorDefecto
    {
        get { return clicPrestado != null ? clicPrestado : Efectos.ClipGolpe; }
    }

    private Selectable selectable;
    private Vector3 escalaBase = Vector3.one;
    private Vector2 posicionBase;
    private float fase;
    private bool apretado;
    private float escalaActual = 1f;

    private bool golpeando;
    private float golpePico;
    private float golpeDuracion;
    private float golpeTranscurrido;

    private bool sacudiendo;
    private float sacudidaAmplitud;
    private float sacudidaDuracion;
    private float sacudidaTranscurrido;

    private void Awake()
    {
        if (visual == null)
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                var hijo = transform.GetChild(i) as RectTransform;
                if (hijo != null)
                {
                    visual = hijo;
                    break;
                }
            }
        }

        if (visual != null)
        {
            escalaBase = visual.localScale;
            posicionBase = visual.anchoredPosition;
        }

        // Fase al azar: varios botones respirando juntos se ven como uno solo.
        fase = Random.value;
        selectable = GetComponent<Selectable>();

        if (sonidoClick != null && clicPrestado == null) clicPrestado = sonidoClick;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        // Un boton apagado no responde: que no parezca que se puede tocar.
        if (selectable != null && !selectable.IsInteractable()) return;

        apretado = true;
        AudioClip clic = sonidoClick != null ? sonidoClick : ClicPorDefecto;
        Sonidos.TocarUI(clic, volumenClick, Sonidos.PitchDe(semitonosClick), 0.05f, 0.03f);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Soltar();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Soltar();
    }

    private void Soltar()
    {
        if (!apretado) return;
        apretado = false;
        Golpe(1.1f, 0.2f);
    }

    // Crece hasta 'escalaPico' y vuelve. Un golpe nuevo reinicia el anterior, asi
    // tocar rapido se siente como una seguidilla y no se pierde ninguno.
    public void Golpe(float escalaPico = 1.2f, float duracion = 0.3f)
    {
        golpeando = true;
        golpePico = escalaPico;
        golpeDuracion = Mathf.Max(0.01f, duracion);
        golpeTranscurrido = 0f;
    }

    // Tiembla de costado, amortiguado: el "no" de un boton.
    public void Sacudir(float amplitud = 16f, float duracion = 0.3f)
    {
        sacudiendo = true;
        sacudidaAmplitud = amplitud;
        sacudidaDuracion = Mathf.Max(0.01f, duracion);
        sacudidaTranscurrido = 0f;
    }

    private void Update()
    {
        if (visual == null) return;

        // Delta con tope: el primer frame de una escena dura mucho y se comeria la
        // animacion entera.
        float dt = Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);

        float objetivo = apretado ? escalaApretado : 1f;
        escalaActual = Mathf.Lerp(escalaActual, objetivo, 1f - Mathf.Exp(-VelocidadApretado * dt));

        float golpe = 1f;
        if (golpeando)
        {
            golpeTranscurrido += dt;
            float t = golpeTranscurrido / golpeDuracion;
            if (t >= 1f) golpeando = false;
            else golpe = 1f + (golpePico - 1f) * CurvasUI.Campana(t);
        }

        float respiracion = 1f;
        if (respirar)
        {
            respiracion = 1f + amplitudRespiracion * Mathf.Sin(2f * Mathf.PI * frecuenciaRespiracion * (Time.unscaledTime + fase));
        }

        visual.localScale = escalaBase * (escalaActual * golpe * respiracion);

        if (sacudiendo)
        {
            sacudidaTranscurrido += dt;
            float t = sacudidaTranscurrido / sacudidaDuracion;
            if (t >= 1f)
            {
                sacudiendo = false;
                visual.anchoredPosition = posicionBase;
            }
            else
            {
                visual.anchoredPosition = posicionBase + new Vector2(sacudidaAmplitud * CurvasUI.Oscilacion(t, 3f), 0f);
            }
        }
    }

    // Si se apaga en medio de una animacion (se cierra el panel), que al volver no
    // quede apretado, grande ni corrido.
    private void OnDisable()
    {
        apretado = false;
        escalaActual = 1f;
        golpeando = false;
        sacudiendo = false;

        if (visual == null) return;
        visual.localScale = escalaBase;
        visual.anchoredPosition = posicionBase;
    }
}
