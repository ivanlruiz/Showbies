using System;
using System.Globalization;
using UnityEngine;

// El pedido de reseña de Google Play (In-App Review): la ventanita nativa de Play con
// las estrellas, que se cierra con un toque. Las valoraciones son lo que mas mueve la
// ficha, y la ventana nativa es la unica forma de pedirlas sin sacar al jugador del juego.
//
// Cuando: en el menu, con todo cerrado, despues de haber llegado a la oleada 10 (el
// primer jefe) y con unas cuantas partidas encima: es un momento tranquilo y el jugador
// ya vio lo mejor del juego. Nunca en la partida ni en la derrota. Como mucho una vez
// cada diasEntrePedidos, y ademas Google decide en silencio si la muestra (tiene su
// propio tope): pedirla no garantiza que aparezca, y no hay forma de saber si aparecio.
//
// Las reglas de Play: no se pregunta antes "¿te gusta?" para mandar solo a los contentos,
// y no se da nada a cambio de reseñar.
//
// Llama a la libreria oficial (com.google.android.play:review, sumada en
// Plugins/Android/mainTemplate.gradle) por JNI, sin el plugin de Unity. Solo en Android:
// se decide en runtime, sin #if (ver la trampa en CLAUDE.md). Fuera de una build de Play
// (una APK instalada a mano) la llamada sale bien pero no muestra nada.
//
// Vive en la raiz del canvas "Main Menu".
public class PedidoDeResena : MonoBehaviour
{
    public const string ClaveUltimoPedido = "ResenaPedidaEn";

    public int oleadaMinima = 10;
    public int partidasMinimas = 3;
    public int diasEntrePedidos = 60;
    [Tooltip("Lo que espera con el menu tranquilo antes de pedirla.")]
    public float demora = 1.5f;

    public TiendaMejoras tienda;
    public ConfirmarSalir confirmarSalir;

    private float tranquiloDesde = -1f;
    private bool pedida;

    // Se guardan para que el recolector no suelte los objetos de Java mientras el
    // flujo sigue abierto del lado de Android.
    private static AndroidJavaObject administrador;
    private static AndroidJavaObject actividad;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetearEstadoCompartido()
    {
        administrador = null;
        actividad = null;
    }

    // Separada para las pruebas: si tocaria pedirla con este progreso y esta fecha.
    public static bool Corresponde(int mejorOleada, int partidas, string ultimoPedido, DateTime ahora,
                                   int oleadaMinima, int partidasMinimas, int diasEntrePedidos)
    {
        if (mejorOleada < oleadaMinima || partidas < partidasMinimas) return false;
        if (string.IsNullOrEmpty(ultimoPedido)) return true;
        DateTime anterior;
        if (!DateTime.TryParseExact(ultimoPedido, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out anterior))
            return true;
        // Un reloj atrasado no la vuelve a pedir: solo cuenta el tiempo hacia adelante.
        return (ahora.Date - anterior.Date).TotalDays >= diasEntrePedidos;
    }

    private void Update()
    {
        if (pedida) return;

        bool tranquilo = !VentanaRecompensaDiaria.Abierta && !VentanaMisiones.Abierta
                         && (tienda == null || !tienda.Abierta)
                         && (confirmarSalir == null || !confirmarSalir.Abierta);
        if (!tranquilo)
        {
            tranquiloDesde = -1f;
            return;
        }
        if (tranquiloDesde < 0f) tranquiloDesde = Time.unscaledTime;
        if (Time.unscaledTime - tranquiloDesde < demora) return;

        // Una sola evaluacion por visita al menu.
        pedida = true;
        if (!Corresponde(Progreso.MejorOleada, Progreso.PartidasTerminadas, PlayerPrefs.GetString(ClaveUltimoPedido, ""),
                         DateTime.Now, oleadaMinima, partidasMinimas, diasEntrePedidos)) return;

        PlayerPrefs.SetString(ClaveUltimoPedido, DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        PlayerPrefs.Save();
        Pedir();
    }

    private static void Pedir()
    {
        if (Application.platform != RuntimePlatform.Android)
        {
            Debug.Log("PedidoDeResena: en Android se pediria la reseña ahora.");
            return;
        }

        try
        {
            using (var jugador = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var fabrica = new AndroidJavaClass("com.google.android.play.core.review.ReviewManagerFactory"))
            {
                actividad = jugador.GetStatic<AndroidJavaObject>("currentActivity");
                administrador = fabrica.CallStatic<AndroidJavaObject>("create", actividad);
                var tarea = administrador.Call<AndroidJavaObject>("requestReviewFlow");
                tarea.Call<AndroidJavaObject>("addOnCompleteListener", new AlCompletar(LanzarFlujo));
            }
        }
        catch (Exception e)
        {
            // Sin la libreria o sin Play en el telefono: no pasa nada, no hay reseña.
            Debug.LogWarning("PedidoDeResena: no se pudo pedir la reseña: " + e.Message);
        }
    }

    // Llega desde el hilo de Android, no desde el de Unity: no se toca nada del juego.
    private static void LanzarFlujo(AndroidJavaObject tarea)
    {
        try
        {
            if (tarea == null || !tarea.Call<bool>("isSuccessful") || administrador == null) return;
            var informacion = tarea.Call<AndroidJavaObject>("getResult");
            administrador.Call<AndroidJavaObject>("launchReviewFlow", actividad, informacion);
        }
        catch (Exception e)
        {
            Debug.LogWarning("PedidoDeResena: no se pudo mostrar la reseña: " + e.Message);
        }
    }

    private class AlCompletar : AndroidJavaProxy
    {
        private readonly Action<AndroidJavaObject> alCompletar;

        public AlCompletar(Action<AndroidJavaObject> alCompletar)
            : base("com.google.android.gms.tasks.OnCompleteListener")
        {
            this.alCompletar = alCompletar;
        }

        // El nombre y la firma son los de la interfaz de Java.
        public void onComplete(AndroidJavaObject tarea)
        {
            alCompletar(tarea);
        }
    }
}
