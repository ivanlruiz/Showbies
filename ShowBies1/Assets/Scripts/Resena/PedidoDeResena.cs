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

    // Lo demas que se puede abrir encima del menu y no tiene estatica propia: el idioma,
    // las opciones y el panel de modos. No estan cableados aca: los toma de BotonAtrasMenu,
    // que esta en el mismo canvas y es el que sabe que se puede cerrar con el atras.
    private SelectorIdioma selectorIdioma;
    private OpcionesSonido opcionesSonido;
    private GameObject menuModos;

    private float tranquiloDesde = -1f;
    private bool pedida;

    // Se guardan para que el recolector no suelte los objetos de Java mientras el
    // flujo sigue abierto del lado de Android.
    private static AndroidJavaObject administrador;
    private static AndroidJavaObject actividad;

    // Lo prende el aviso de Android cuando Play dio la ventana y se lanzo el flujo. La fecha
    // se anota en el Update siguiente, en el hilo de Unity (PlayerPrefs no se toca desde el
    // de Android), y si el pedido falla no se anota nada: se vuelve a intentar en la proxima
    // visita al menu. Hasta el 24/9 la fecha se guardaba antes de pedir, y una falla (sin
    // conexion, una Play vieja) gastaba los 60 dias sin haber pedido nada. Es estatico para
    // que llegue aunque el menu se haya descargado: se anota al volver.
    private static volatile bool pedidoConfirmado;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetearEstadoCompartido()
    {
        administrador = null;
        actividad = null;
        pedidoConfirmado = false;
    }

    private void Awake()
    {
        var atras = GetComponent<BotonAtrasMenu>();
        if (atras == null) atras = FindAnyObjectByType<BotonAtrasMenu>();
        if (atras == null) return;
        selectorIdioma = atras.selectorIdioma;
        opcionesSonido = atras.opcionesSonido;
        menuModos = atras.menuModos;
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
        if (pedidoConfirmado)
        {
            pedidoConfirmado = false;
            AnotarPedido();
        }
        if (pedida) return;

        // Con todo cerrado de verdad: hasta el 24/9 no miraba el idioma, las opciones ni el
        // panel de modos, y la hoja de Play podia salir mientras se arrastraba el volumen.
        bool tranquilo = !VentanaRecompensaDiaria.Abierta && !VentanaMisiones.Abierta && !VentanaBestiario.Abierta
                         && !VentanaLogros.Abierta && (tienda == null || !tienda.Abierta)
                         && (confirmarSalir == null || !confirmarSalir.Abierta)
                         && (selectorIdioma == null || !selectorIdioma.Abierto)
                         && (opcionesSonido == null || !opcionesSonido.Abierto)
                         && (menuModos == null || !menuModos.activeSelf);
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

        if (Application.platform != RuntimePlatform.Android)
        {
            // Fuera de Android no hay nada que pueda fallar: se anota en el acto, asi el
            // aviso sale una vez cada diasEntrePedidos, como en el telefono.
            Debug.Log("PedidoDeResena: en Android se pediria la reseña ahora.");
            AnotarPedido();
            return;
        }
        Pedir();
    }

    private static void AnotarPedido()
    {
        PlayerPrefs.SetString(ClaveUltimoPedido, DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        PlayerPrefs.Save();
    }

    // Solo en Android. La fecha la anota el Update cuando Play confirma (pedidoConfirmado).
    private static void Pedir()
    {
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

    // Llega desde el hilo de Android, no desde el de Unity: no se toca nada del juego, solo
    // se prende la marca para que el Update anote la fecha.
    private static void LanzarFlujo(AndroidJavaObject tarea)
    {
        try
        {
            if (tarea == null || !tarea.Call<bool>("isSuccessful") || administrador == null) return;
            var informacion = tarea.Call<AndroidJavaObject>("getResult");
            administrador.Call<AndroidJavaObject>("launchReviewFlow", actividad, informacion);
            pedidoConfirmado = true;
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

        // El nombre y la firma son los de la interfaz de Java. Nadie lo llama desde C#: Unity
        // lo busca por nombre cuando Java llama al proxy, y con la limpieza de codigo mas
        // alta (hoy esta en Low) el linker lo sacaria sin ningun error. Preserve lo impide.
        [UnityEngine.Scripting.Preserve]
        public void onComplete(AndroidJavaObject tarea)
        {
            alCompletar(tarea);
        }
    }
}
