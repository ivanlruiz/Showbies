using UnityEngine;

// Un objeto que sobrevive a los cambios de escena y hace dos cosas chicas que
// nadie mas puede hacer:
//
// 1. Vacia en el hilo principal los avisos de los videos. El SDK avisa cuando
//    quiere y desde donde quiere, y tocar Unity fuera del hilo principal explota.
//    Tambien le avisa al proveedor cuando la app recupera el foco (el vigia de AdMob).
// 2. Guarda el progreso cuando la app pierde el foco en CUALQUIER escena. Hasta
//    ahora eso lo hacia MenuPausa, que no esta ni en el menu ni en la derrota: si
//    Android mataba la app ahi, se perdia lo ultimo.
// 3. Programa las notificaciones locales al irse la app y las cancela al volver
//    (Notificaciones), por la misma razon: tiene que pasar en cualquier escena.
//
// Se instala solo al cargar la primera escena, sin ponerlo en ninguna: lo mismo que
// hace MedidorBalance.
public class VigiaAplicacion : MonoBehaviour
{
    private static VigiaAplicacion instancia;

    // La sesion de juego, para el aviso de descanso de la derrota: solo cuenta con la app
    // delante, y vuelve a cero si estuvo en segundo plano un rato largo. Antes se usaba
    // Time.realtimeSinceStartup, que en Android sigue corriendo con la app en recientes:
    // diez minutos de juego repartidos en una tarde daban "llevas mas de una hora".
    public static float SegundosDeSesion { get; private set; }
    // Cuantas veces volvio a cero la sesion desde que se abrio la app: el reloj de los automaticos
    // (ServicioAnuncios) sabe asi que lo que anoto es de una sesion vieja.
    public static int Sesion { get; private set; }
    private const float AusenciaQueCortaLaSesion = 20f * 60f;
    private static float fueraDesde = -1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetearEstadoCompartido()
    {
        instancia = null;
        SegundosDeSesion = 0f;
        Sesion = 0;
        fueraDesde = -1f;
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
        // Solo la APK de prueba: el estado de los anuncios en el menu.
        if (ConfigAnuncios.EsPaqueteDePrueba(Application.identifier)) objeto.AddComponent<DiagnosticoAnuncios>();

        // Esto corre al abrir la app, que es cuando tiene que arrancar el proveedor de
        // anuncios (ver ServicioAnuncios.Arrancar). Despues de crear el vigia: si una red
        // de verdad fallara al arrancar, guardar el progreso tiene que seguir andando.
        ServicioAnuncios.Arrancar();
        // El jugador abrio el juego: lo que quedo programado de la ausencia ya no hace falta.
        Notificaciones.AlVolver();
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
        // Con tope: el primer cuadro despues de volver no se come la ausencia.
        SegundosDeSesion += Mathf.Min(Time.unscaledDeltaTime, 0.1f);
    }

    private void OnApplicationPause(bool pausada)
    {
        if (pausada) Progreso.Guardar();
        Ausencia(pausada);
        // Las notificaciones locales: se programan al irse y se cancelan al volver (ver Notificaciones).
        if (pausada) Notificaciones.AlIrse();
        else Notificaciones.AlVolver();
    }

    private void OnApplicationFocus(bool conFoco)
    {
        Ausencia(!conFoco);
        // El video de AdMob saca el foco a la app: al volver, si el SDK no avisa como
        // termino, el proveedor lo resuelve igual (ver ProveedorAdMob).
        ServicioAnuncios.CambioElFoco(conFoco);
    }

    private static void Ausencia(bool empieza)
    {
        if (empieza)
        {
            if (fueraDesde < 0f) fueraDesde = Time.realtimeSinceStartup;
            return;
        }
        if (fueraDesde >= 0f && Time.realtimeSinceStartup - fueraDesde >= AusenciaQueCortaLaSesion)
        {
            SegundosDeSesion = 0f;
            Sesion++;
        }
        fueraDesde = -1f;
    }

    private void OnApplicationQuit()
    {
        Progreso.Guardar();
        Notificaciones.AlIrse();
    }
}
