using UnityEngine;

// Con poca vida el mundo pierde color, pedido de Ivan: desde un cuarto de la vida, cada vez
// mas, hasta medio gris al borde de morir. Avisa sin tapar nada, y engancha solo con la
// muerte: la oferta de revivir y la derrota toman el mismo filtro (FiltroBlancoYNegro) donde
// este y lo llevan hasta el gris total. Si se cura, vuelve el color.
//
// No llega al gris total mientras se juega porque los zombis se distinguen por el color
// (lima el rapido, rojo el tanque, celeste el veloz, violeta el jefe). Y el filtro solo se
// prende con poca vida: es una pasada de pantalla completa, y en el telefono cuesta.
//
// Va en la raiz de Jugador.prefab.
public class GrisDePocaVida : MonoBehaviour
{
    [Tooltip("Desde que parte de la vida empieza a perder color.")]
    public float desde = 0.25f;
    [Tooltip("Cuanto gris con la vida en cero (1 es el gris total).")]
    public float maximo = 0.5f;
    [Tooltip("Que tan rapido sigue a la vida: mas alto, mas brusco.")]
    public float seguimiento = 5f;

    private PlayerHealth vida;
    private FiltroBlancoYNegro filtro;
    private float actual;

    // El gris que toca con esa parte de la vida (de 0 a 1): nada hasta "desde" y de ahi,
    // en linea recta, hasta "maximo" con la vida en cero. Estatico para probarlo sin escena.
    public static float CantidadDeGris(float parteDeLaVida, float desde, float maximo)
    {
        if (desde <= 0f || parteDeLaVida >= desde) return 0f;
        return maximo * Mathf.Clamp01(1f - parteDeLaVida / desde);
    }

    // Cuanto gris hay ahora. Para las pruebas.
    public float Actual { get { return actual; } }

    private void Awake()
    {
        vida = GetComponent<PlayerHealth>();
    }

    private void Update()
    {
        if (vida == null) return;

        // Muerto, el filtro es de la oferta de revivir o de la derrota, que lo toman donde
        // quedo. Si revive, vuelve con la vida llena y desde cero.
        if (vida.EstaMuerto)
        {
            filtro = null;
            actual = 0f;
            return;
        }

        float parte = vida.maxHealth > 0 ? (float)vida.health / vida.maxHealth : 1f;
        float objetivo = CantidadDeGris(parte, desde, maximo);
        // Se acerca de a poco: un golpe no lo hace saltar. Con tiempo escalado, asi la pausa
        // lo deja quieto.
        actual = Mathf.Lerp(actual, objetivo, 1f - Mathf.Exp(-seguimiento * Time.deltaTime));
        if (objetivo <= 0f && actual < 0.005f) actual = 0f;

        if (actual > 0f)
        {
            if (filtro == null) filtro = FiltroBlancoYNegro.Tomar(Camera.main);
            if (filtro != null) filtro.cantidad = actual;
        }
        else if (filtro != null)
        {
            filtro.Soltar();
            filtro = null;
        }
    }

    private void OnDisable()
    {
        if (filtro != null && vida != null && !vida.EstaMuerto) filtro.Soltar();
        filtro = null;
        actual = 0f;
    }
}
