using System.Collections.Generic;
using UnityEngine;

// Le pone fecha de vencimiento a un pickup. Antes un power-up que nadie
// agarraba quedaba en el piso para siempre, y con spawns cada 8-20 segundos
// la escena los acumulaba sin limite. Parpadea los ultimos segundos para
// avisar que se va.
public class PickupCaducidad : MonoBehaviour
{
    public float vida = 30f;           // segundos en el piso antes de desaparecer
    public float parpadeoFinal = 3f;   // cuantos segundos antes empieza a parpadear
    public float frecuenciaParpadeo = 8f;

    private float venceEn = -1f;
    private Renderer[] renderers;

    // Las que estan en el piso, para las flechas del borde (FlechasDelBorde). El tutorial
    // apaga el componente, y esas no cuentan: ahi las cajas salen al lado del jugador.
    private static readonly List<PickupCaducidad> puestas = new List<PickupCaducidad>();
    public static IReadOnlyList<PickupCaducidad> Puestas { get { return puestas; } }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetearEstadoCompartido()
    {
        puestas.Clear();
    }

    private void OnEnable()
    {
        if (!puestas.Contains(this)) puestas.Add(this);
    }

    private void OnDisable()
    {
        puestas.Remove(this);
    }

    // Lo que le queda, de 1 (recien puesta) a 0 (se va). Sin vencer (el tutorial apaga el
    // componente para que las cajas esperen al jugador) o antes de empezar, 1. Lo dibuja el
    // anillo de AspectoDeCaja.
    public float Restante01
    {
        get
        {
            if (!enabled || vida <= 0f || venceEn < 0f) return 1f;
            return Mathf.Clamp01((venceEn - Time.time) / vida);
        }
    }

    private void Start()
    {
        venceEn = Time.time + vida;
        renderers = GetComponentsInChildren<Renderer>(true);
    }

    private void Update()
    {
        float restante = venceEn - Time.time;

        if (restante <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        if (restante <= parpadeoFinal)
        {
            bool visible = Mathf.FloorToInt(restante * frecuenciaParpadeo) % 2 == 0;
            foreach (var r in renderers) r.enabled = visible;
        }
    }
}
