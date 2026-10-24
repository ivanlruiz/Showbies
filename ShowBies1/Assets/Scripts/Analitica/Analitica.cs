using System;
using System.Globalization;
using System.Text;
using UnityEngine;

// La analitica (1.7.1): Firebase Analytics por JNI (la libreria esta en Plugins/Android/ShowBiesFirebase.androidlib), para
// saber en que oleada y contra que zombi abandona la gente, que es lo que Play Console no ve. Los eventos son pocos y
// fijos, con nombres en español, y estan todos aca: ninguno por cuadro ni por bala (lo que mas se manda son las oleadas
// completadas y las muertes). Los pidio Argon (24/10) y salen del informe de ese dia.
//
// Arranca apagada (el manifiesto la apaga) y se prende cuando el consentimiento de los anuncios esta resuelto: fuera de
// Europa en el acto, en Europa despues del cartel (Revisar, desde VigiaAplicacion). Lo que pasa antes no se manda. Con ella
// se prende el token de las notificaciones push (Firebase Cloud Messaging, para avisar las versiones), que es el mismo
// tipo de dato. En el editor, y fuera de Android, no manda nada: con MostrarEnElEditor escribe cada evento en la consola.
public static class Analitica
{
    public static bool MostrarEnElEditor = true;
    private const float SegundosEntreRevisiones = 2f;

    private static AndroidJavaObject firebase;
    private static bool encendida;
    private static bool fallo;
    private static float revisadaEn = -100f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetearEstadoCompartido()
    {
        firebase = null;
        encendida = false;
        fallo = false;
        revisadaEn = -100f;
    }

    public static bool Encendida
    {
        get { return encendida; }
    }

    // VigiaAplicacion, en cada cuadro: mira cada 2 s si ya se puede prender.
    public static void Revisar()
    {
        if (encendida || fallo || Application.platform != RuntimePlatform.Android) return;
        if (Time.unscaledTime - revisadaEn < SegundosEntreRevisiones) return;
        revisadaEn = Time.unscaledTime;
        if (ServicioAnuncios.ConsentimientoResuelto) Encender();
    }

    private static void Encender()
    {
        try
        {
            using (var jugador = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var actividad = jugador.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var clase = new AndroidJavaClass("com.google.firebase.analytics.FirebaseAnalytics"))
            {
                firebase = clase.CallStatic<AndroidJavaObject>("getInstance", actividad);
            }
            firebase.Call("setAnalyticsCollectionEnabled", true);
            using (var clase = new AndroidJavaClass("com.google.firebase.messaging.FirebaseMessaging"))
            using (var mensajeria = clase.CallStatic<AndroidJavaObject>("getInstance"))
            {
                mensajeria.Call("setAutoInitEnabled", true);
            }
            encendida = true;
        }
        catch (Exception e)
        {
            fallo = true;
            Debug.LogWarning("Analitica: no se pudo prender Firebase: " + e.Message);
        }
    }

    // Un evento con sus parametros en pares (nombre, valor): los enteros van como long, los decimales como double y el
    // resto como texto, que es lo unico que acepta Analytics.
    public static void Evento(string nombre, params object[] pares)
    {
#if UNITY_EDITOR
        if (MostrarEnElEditor) Debug.Log("Analitica: " + nombre + " " + Describir(pares));
#endif
        if (!encendida || firebase == null) return;
        try
        {
            using (var datos = new AndroidJavaObject("android.os.Bundle"))
            {
                for (int i = 0; i + 1 < pares.Length; i += 2)
                {
                    string clave = pares[i] as string;
                    object valor = pares[i + 1];
                    if (clave == null || valor == null) continue;
                    if (valor is int || valor is long) datos.Call("putLong", clave, Convert.ToInt64(valor));
                    else if (valor is float || valor is double) datos.Call("putDouble", clave, Convert.ToDouble(valor));
                    else if (valor is bool) datos.Call("putLong", clave, (bool)valor ? 1L : 0L);
                    else datos.Call("putString", clave, Convert.ToString(valor, CultureInfo.InvariantCulture));
                }
                firebase.Call("logEvent", nombre, datos);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("Analitica: no se pudo mandar " + nombre + ": " + e.Message);
        }
    }

    private static string Describir(object[] pares)
    {
        var sb = new StringBuilder();
        for (int i = 0; i + 1 < pares.Length; i += 2)
            sb.Append(pares[i]).Append('=').Append(Convert.ToString(pares[i + 1], CultureInfo.InvariantCulture)).Append(' ');
        return sb.ToString();
    }

    // --- Los eventos ------------------------------------------------------------------------------------------------

    // modo: "oleadas" o "libre". capitulo: el de las oleadas (1 a 3), 0 en el libre.
    public static void PartidaEmpezada(string modo, int oleadaInicial, int capitulo, bool retomada)
    {
        Evento("partida_empezada", "modo", modo, "oleada_inicial", oleadaInicial, "capitulo", capitulo, "retomada", retomada);
    }

    public static void OleadaCompletada(int oleada, bool perfecta)
    {
        Evento("oleada_completada", "oleada", oleada, "perfecta", perfecta);
    }

    // nivel: la oleada en las oleadas, el nivel en el libre. causa: el zombi que dio el ultimo golpe.
    public static void Muerte(string modo, int nivel, int segundos, long puntos, string causa)
    {
        Evento("muerte", "modo", modo, "nivel", nivel, "segundos", segundos, "puntos", puntos, "causa", string.IsNullOrEmpty(causa) ? "desconocida" : causa);
    }

    public static void CompraMejora(string mejora, int nivel)
    {
        Evento("compra_mejora", "mejora", mejora, "nivel", nivel);
    }

    public static void CartaElegida(string carta, int poder, bool dorada)
    {
        Evento("carta_elegida", "carta", carta, "poder", poder, "dorada", dorada);
    }

    public static void CapituloElegido(int capitulo)
    {
        Evento("capitulo_elegido", "capitulo", capitulo);
    }

    public static void TutorialCompletado()
    {
        Evento("tutorial_completado");
    }

    // resultado: "premiado", "cerrado", "no_disponible" o "falla".
    public static void Video(string lugar, string resultado)
    {
        Evento("video", "lugar", lugar, "resultado", resultado);
    }

    public static void Intersticial(string resultado)
    {
        Evento("intersticial", "resultado", resultado);
    }

    // resultado: "si" (aceptó la ventana propia), "ahora_no", "concedido" o "negado" (el dialogo de Android).
    public static void PermisoDeNotificaciones(string resultado)
    {
        Evento("notif_permiso", "resultado", resultado);
    }

    public static void NovedadesVistas(string version)
    {
        Evento("novedades_vistas", "version", version);
    }
}
