using UnityEngine;

// Un objeto que sobrevive a los cambios de escena y hace dos cosas chicas que
// nadie mas puede hacer:
//
// 1. Vacia en el hilo principal los avisos de los videos. El SDK avisa cuando
//    quiere y desde donde quiere, y tocar Unity fuera del hilo principal explota.
// 2. Guarda el progreso cuando la app pierde el foco en CUALQUIER escena. Hasta
//    ahora eso lo hacia MenuPausa, que no esta ni en el menu ni en la derrota: si
//    Android mataba la app ahi, se perdia lo ultimo.
//
// Se instala solo al cargar la primera escena, sin ponerlo en ninguna: lo mismo que
// hace MedidorBalance.
public class VigiaAplicacion : MonoBehaviour
{
    private static VigiaAplicacion instancia;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetearEstadoCompartido()
    {
        instancia = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void Asegurar()
    {
        // En el editor fuera de play (las pruebas) no hay a quien vigilar, y crear
        // el objeto ensuciaria la escena abierta.
        if (instancia != null || !Application.isPlaying) return;

        var objeto = new GameObject("VigiaAplicacion");
        DontDestroyOnLoad(objeto);
        instancia = objeto.AddComponent<VigiaAplicacion>();
    }

    private void Awake()
    {
        if (instancia != null && instancia != this)
        {
            Destroy(gameObject);
            return;
        }
        instancia = this;
    }

    private void OnDestroy()
    {
        if (instancia == this) instancia = null;
    }

    private void Update()
    {
        ServicioAnuncios.AtenderAvisos();
    }

    private void OnApplicationPause(bool pausada)
    {
        if (pausada) Progreso.Guardar();
    }

    private void OnApplicationQuit()
    {
        Progreso.Guardar();
    }
}
