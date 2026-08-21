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

    private float venceEn;
    private Renderer[] renderers;

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
