using UnityEngine;

// Hace entrar un elemento de la UI con un rebote de escala cada vez que se
// activa: crece de chico, se pasa y vuelve a su tamaño. Lo usa el cartel de la
// oleada. Con tiempo sin escalar, para que la pausa de impacto no lo congele.
public class AparecerConRebote : MonoBehaviour
{
    public float duracion = 0.35f;
    public float escalaInicial = 0.2f;
    public float escalaDelRebote = 1.25f;

    private Vector3 escalaBase;
    private float transcurrido;
    private bool animando;

    private void Awake()
    {
        escalaBase = transform.localScale;
    }

    private void OnEnable()
    {
        transcurrido = 0f;
        animando = true;
        Aplicar(0f);
    }

    private void OnDisable()
    {
        transform.localScale = escalaBase;
    }

    private void Update()
    {
        if (!animando) return;

        // Delta con tope: el frame en que carga la escena dura mucho y, sin tope, el
        // cartel de la primera oleada se salteaba la animacion entera.
        transcurrido += Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);
        float t = transcurrido / duracion;
        if (t >= 1f)
        {
            animando = false;
            transform.localScale = escalaBase;
            return;
        }
        Aplicar(t);
    }

    private void Aplicar(float t)
    {
        // Sube rapido hasta el rebote en el primer 60 % y vuelve en el resto.
        float escala = t < 0.6f
            ? Mathf.Lerp(escalaInicial, escalaDelRebote, 1f - (1f - t / 0.6f) * (1f - t / 0.6f))
            : Mathf.Lerp(escalaDelRebote, 1f, (t - 0.6f) / 0.4f);
        transform.localScale = escalaBase * escala;
    }
}
