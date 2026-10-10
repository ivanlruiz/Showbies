using System;
using System.Globalization;
using UnityEngine;

public enum EstadoActualizacion
{
    Nada,           // no hay version nueva, o no se puede saber (fuera de Play, sin Play)
    Disponible,     // hay una nueva y se puede bajar con la actualizacion flexible
    Descargando,    // el jugador dijo que si y Play la esta bajando
    Lista           // bajada: falta reiniciar para instalarla
}

// Las actualizaciones dentro del juego (In-App Updates de Play), con la actualizacion
// flexible: Play baja la version nueva mientras se sigue jugando y despues se reinicia con un
// toque. Lo que se ve es AvisoDeActualizacion, en el menu; esto es solo hablar con Play.
//
// Llama a la libreria oficial (com.google.android.play:app-update, sumada en
// Plugins/Android/mainTemplate.gradle) por JNI, sin el plugin de Unity, como la reseña
// (PedidoDeResena). Solo en Android: se decide en runtime, sin #if (ver la trampa en CLAUDE.md).
// Fuera de una instalacion desde Play (una APK a mano) Play contesta con un error y no pasa nada.
//
// Lo que llega de Play llega en el hilo de Android: ahi solo se escriben campos sueltos, y lo
// que toca el juego (PlayerPrefs) se hace en Atender, en el hilo de Unity.
//
// Para probarlo sin publicar una version:
// - En la APK de prueba (el paquete .prueba) se usa el FakeAppUpdateManager de la misma
//   libreria, con las mismas llamadas: hay una version nueva siempre, la "descarga" avanza sola
//   mientras se mira el menu y REINICIAR no reinicia (avisa con un Toast). Asi se recorre en el
//   telefono el camino de JNI de verdad, que es lo que mas facil se rompe.
// - En el editor, con ShowBies > Actualizacion > Simular en el editor, lo mismo hecho en C#.
public static class ActualizacionDePlay
{
    // Las constantes de la libreria (leidas del jar de app-update 2.1.0 con javap).
    public const int HayActualizacion = 2;          // UpdateAvailability.UPDATE_AVAILABLE
    public const int InstalacionPendiente = 1;      // InstallStatus.PENDING
    public const int InstalacionBajando = 2;        // InstallStatus.DOWNLOADING
    public const int InstalacionBajada = 11;        // InstallStatus.DOWNLOADED
    public const int InstalacionFallo = 5;          // InstallStatus.FAILED
    public const int InstalacionCancelada = 6;      // InstallStatus.CANCELED
    public const int TipoFlexible = 0;              // AppUpdateType.FLEXIBLE
    public const int RespuestaSi = -1;              // Activity.RESULT_OK
    public const int RespuestaNo = 0;               // Activity.RESULT_CANCELED

    // "AHORA NO" (o decirle que no a la hoja de Play) guarda la version y el dia: esa version no
    // se vuelve a ofrecer hasta dentro de DiasDePostergacion. Una mas nueva, si.
    public const string ClavePostergada = "ActualizacionPostergada";
    public const int DiasDePostergacion = 3;

    public const string ClaveSimularEnElEditor = "ShowBies.SimularActualizacion";

    // Mientras baja, se le vuelve a preguntar a Play cada tanto (solo con el menu delante).
    public const float SegundosEntreRevisiones = 1.5f;
    // La descarga de mentira (editor y APK de prueba).
    public const float SegundosDeLaPrueba = 5f;
    private const long BytesDeLaPrueba = 30L * 1024L * 1024L;

    private enum Modo { SinDecidir, Ninguno, Play, Prueba, Editor }

    private static Modo modo;
    // El estado va en un int volatil y no en el enum: con un enum volatile, il2cpp.exe se cayo
    // dos veces seguidas al armar la APK (9/10; la memoria de la maquina estaba al limite, asi
    // que puede no haber sido eso), y con el int paso. El avance es un float comun: se escribe
    // entero, y si el menu lee el de un momento antes no pasa nada.
    private static volatile int estadoNumero;
    private static float avance;
    private static EstadoActualizacion estado
    {
        get { return (EstadoActualizacion)estadoNumero; }
        set { estadoNumero = (int)value; }
    }
    private static volatile int versionDisponible;
    private static volatile bool revisando;
    private static volatile bool esperandoRespuesta;
    private static volatile bool postergarPendiente;
    // El jugador ya dijo que si en esta sesion: lo que conteste Play mientras arranca la descarga
    // todavia puede decir "disponible", y no se le vuelve a ofrecer.
    private static volatile bool aceptada;
    private static float proximaRevision;
    private static float avanceDeLaPrueba;

    // Se guardan para que el recolector no suelte los objetos de Java mientras Play los usa.
    private static AndroidJavaObject administrador;
    private static AndroidJavaObject actividad;
    private static AndroidJavaObject informacion;

    public static EstadoActualizacion Estado { get { return estado; } }
    // De 0 a 1, mientras baja.
    public static float Avance { get { return avance; } }
    public static int VersionDisponible { get { return versionDisponible; } }
    // Se abrio la hoja de Play y todavia no contesto: el aviso no se muestra mientras.
    public static bool EsperandoRespuesta { get { return esperandoRespuesta; } }
    // Lo ultimo que fallo, para el diagnostico de la APK de prueba.
    public static string UltimoError { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetearEstadoCompartido()
    {
        modo = Modo.SinDecidir;
        estado = EstadoActualizacion.Nada;
        avance = 0f;
        versionDisponible = 0;
        revisando = false;
        esperandoRespuesta = false;
        postergarPendiente = false;
        aceptada = false;
        proximaRevision = 0f;
        avanceDeLaPrueba = 0f;
        administrador = null;
        actividad = null;
        informacion = null;
        UltimoError = null;
    }

    // Separada para las pruebas: lo que hay segun lo que contesta Play.
    public static EstadoActualizacion Decidir(int disponibilidad, int instalacion, bool flexiblePermitida)
    {
        if (instalacion == InstalacionBajada) return EstadoActualizacion.Lista;
        if (instalacion == InstalacionPendiente || instalacion == InstalacionBajando) return EstadoActualizacion.Descargando;
        if (disponibilidad == HayActualizacion && flexiblePermitida) return EstadoActualizacion.Disponible;
        return EstadoActualizacion.Nada;
    }

    // Separada para las pruebas. Recien aceptada, Play puede tardar un momento en empezar a bajarla
    // y seguir diciendo que esta disponible: se la da por bajando, salvo que la descarga haya
    // fallado o se haya cancelado (desde la notificacion de Play), y ahi no se ofrece mas en esta
    // sesion.
    public static EstadoActualizacion DespuesDeAceptar(EstadoActualizacion decidido, int instalacion, bool aceptada)
    {
        if (!aceptada || decidido != EstadoActualizacion.Disponible) return decidido;
        if (instalacion == InstalacionFallo || instalacion == InstalacionCancelada) return EstadoActualizacion.Nada;
        return EstadoActualizacion.Descargando;
    }

    // Separada para las pruebas: si se ofrece esta version, con lo que se postergo antes
    // ("version|aaaa-mm-dd"). Un reloj atrasado no la vuelve a ofrecer antes: solo cuenta el
    // tiempo hacia adelante.
    public static bool Ofrecer(int version, string postergada, DateTime ahora, int dias)
    {
        if (string.IsNullOrEmpty(postergada)) return true;
        string[] partes = postergada.Split('|');
        int versionPostergada;
        DateTime dia;
        if (partes.Length != 2 || !int.TryParse(partes[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out versionPostergada)
            || !DateTime.TryParseExact(partes[1], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out dia))
            return true;
        if (version != versionPostergada) return true;
        return (ahora.Date - dia.Date).TotalDays >= dias;
    }

    public static string Postergacion(int version, DateTime ahora)
    {
        return version.ToString(CultureInfo.InvariantCulture) + "|" + ahora.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    // Si toca ofrecer la que hay, con lo postergado de este telefono.
    public static bool OfrecerLaDisponible()
    {
        return Ofrecer(versionDisponible, PlayerPrefs.GetString(ClavePostergada, ""), DateTime.Now, DiasDePostergacion);
    }

    // "AHORA NO": esta version no se ofrece por unos dias.
    public static void Postergar()
    {
        PlayerPrefs.SetString(ClavePostergada, Postergacion(versionDisponible, DateTime.Now));
        PlayerPrefs.Save();
    }

    // Le pregunta a Play que hay. Lo llama el aviso al cargar el menu, y Atender mientras baja.
    public static void Revisar()
    {
        if (modo == Modo.SinDecidir) modo = ElegirModo();
        if (modo == Modo.Editor)
        {
            if (estado == EstadoActualizacion.Nada && !esperandoRespuesta) { estado = EstadoActualizacion.Disponible; versionDisponible = 99; }
            return;
        }
        if (modo != Modo.Play && modo != Modo.Prueba) return;
        if (revisando) return;
        try
        {
            if (!Asegurar()) return;
            revisando = true;
            var tarea = administrador.Call<AndroidJavaObject>("getAppUpdateInfo");
            tarea.Call<AndroidJavaObject>("addOnCompleteListener", new AlCompletar(AlLlegarLaInformacion));
        }
        catch (Exception e)
        {
            revisando = false;
            Fallo("revisar", e);
        }
    }

    // ACTUALIZAR: abre la hoja de Play que pregunta si bajarla.
    public static void Empezar()
    {
        if (estado != EstadoActualizacion.Disponible || esperandoRespuesta) return;
        if (modo == Modo.Editor)
        {
            estado = EstadoActualizacion.Descargando;
            avance = 0f;
            return;
        }
        if (administrador == null || informacion == null || actividad == null) return;
        esperandoRespuesta = true;
        EnElHiloDeAndroid(() =>
        {
            try
            {
                AndroidJavaObject opciones;
                using (var clase = new AndroidJavaClass("com.google.android.play.core.appupdate.AppUpdateOptions"))
                    opciones = clase.CallStatic<AndroidJavaObject>("defaultOptions", TipoFlexible);
                var tarea = administrador.Call<AndroidJavaObject>("startUpdateFlow", informacion, actividad, opciones);
                tarea.Call<AndroidJavaObject>("addOnCompleteListener", new AlCompletar(AlContestar));
            }
            catch (Exception e)
            {
                esperandoRespuesta = false;
                Fallo("empezar", e);
            }
        });
    }

    // REINICIAR: Play instala la version bajada y vuelve a abrir el juego. Antes se guarda todo,
    // como antes de un video: el proceso se termina.
    public static void Completar()
    {
        if (estado != EstadoActualizacion.Lista) return;
        Progreso.Guardar();
        PlayerPrefs.Save();
        if (modo == Modo.Editor)
        {
            Debug.Log("ActualizacionDePlay: en el telefono, Play reiniciaria el juego con la version nueva.");
            estado = EstadoActualizacion.Nada;
            return;
        }
        if (administrador == null || actividad == null) return;
        bool deprueba = modo == Modo.Prueba;
        EnElHiloDeAndroid(() =>
        {
            try
            {
                administrador.Call<AndroidJavaObject>("completeUpdate");
                if (deprueba)
                {
                    // El de mentira queda "instalando": se termina a mano, y no reinicia nada.
                    administrador.Call("installCompletes");
                    estado = EstadoActualizacion.Nada;
                    Avisar("(prueba) Aca Play reiniciaria ShowBies con la version nueva.");
                }
            }
            catch (Exception e)
            {
                Fallo("completar", e);
            }
        });
    }

    // En el hilo de Unity, cada cuadro del menu (AvisoDeActualizacion): lo que pide PlayerPrefs y
    // las revisiones mientras baja.
    public static void Atender()
    {
        if (postergarPendiente)
        {
            postergarPendiente = false;
            Postergar();
        }
        if (estado != EstadoActualizacion.Descargando) return;

        if (modo == Modo.Editor)
        {
            avance = Mathf.Clamp01(avance + Mathf.Min(Time.unscaledDeltaTime, 0.1f) / SegundosDeLaPrueba);
            if (avance >= 1f) estado = EstadoActualizacion.Lista;
            return;
        }
        if (Time.unscaledTime < proximaRevision) return;
        proximaRevision = Time.unscaledTime + SegundosEntreRevisiones;
        if (modo == Modo.Prueba) AvanzarLaPrueba();
        Revisar();
    }

    private static Modo ElegirModo()
    {
        if (Application.isEditor)
        {
#if UNITY_EDITOR
            if (UnityEditor.EditorPrefs.GetBool(ClaveSimularEnElEditor, false)) return Modo.Editor;
#endif
            return Modo.Ninguno;
        }
        if (Application.platform != RuntimePlatform.Android) return Modo.Ninguno;
        return ConfigAnuncios.EsPaqueteDePrueba(Application.identifier) ? Modo.Prueba : Modo.Play;
    }

    private static bool Asegurar()
    {
        if (administrador != null) return true;
        using (var jugador = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            actividad = jugador.GetStatic<AndroidJavaObject>("currentActivity");
        if (modo == Modo.Prueba)
        {
            administrador = new AndroidJavaObject("com.google.android.play.core.appupdate.testing.FakeAppUpdateManager", actividad);
            administrador.Call("setUpdateAvailable", 9999);
        }
        else
        {
            using (var fabrica = new AndroidJavaClass("com.google.android.play.core.appupdate.AppUpdateManagerFactory"))
                administrador = fabrica.CallStatic<AndroidJavaObject>("create", actividad);
        }
        return administrador != null;
    }

    // La descarga de mentira de la APK de prueba: el FakeAppUpdateManager no baja nada, hay que
    // decirle que paso. Lo que lee el juego sigue saliendo de getAppUpdateInfo.
    private static void AvanzarLaPrueba()
    {
        if (administrador == null) return;
        try
        {
            avanceDeLaPrueba = Mathf.Clamp01(avanceDeLaPrueba + SegundosEntreRevisiones / SegundosDeLaPrueba);
            if (avanceDeLaPrueba >= 1f) administrador.Call("downloadCompletes");
            else administrador.Call("setBytesDownloaded", (long)(BytesDeLaPrueba * avanceDeLaPrueba));
        }
        catch (Exception e)
        {
            Fallo("prueba", e);
        }
    }

    // Llega desde el hilo de Android.
    private static void AlLlegarLaInformacion(AndroidJavaObject tarea)
    {
        try
        {
            if (tarea == null || !tarea.Call<bool>("isSuccessful"))
            {
                // Sin Play o instalada a mano: no hay nada que ofrecer.
                UltimoError = "info: " + Excepcion(tarea);
                estado = EstadoActualizacion.Nada;
                return;
            }
            var info = tarea.Call<AndroidJavaObject>("getResult");
            int disponibilidad = info.Call<int>("updateAvailability");
            int instalacion = info.Call<int>("installStatus");
            bool flexible = info.Call<bool>("isUpdateTypeAllowed", TipoFlexible);
            long bajados = info.Call<long>("bytesDownloaded");
            long total = info.Call<long>("totalBytesToDownload");
            informacion = info;
            versionDisponible = info.Call<int>("availableVersionCode");
            avance = total > 0 ? Mathf.Clamp01((float)((double)bajados / total)) : 0f;
            estado = DespuesDeAceptar(Decidir(disponibilidad, instalacion, flexible), instalacion, aceptada);
        }
        catch (Exception e)
        {
            Fallo("info", e);
        }
        finally
        {
            revisando = false;
        }
    }

    // Lo que contesto el jugador en la hoja de Play. Llega desde el hilo de Android.
    private static void AlContestar(AndroidJavaObject tarea)
    {
        try
        {
            if (tarea == null || !tarea.Call<bool>("isSuccessful"))
            {
                UltimoError = "hoja: " + Excepcion(tarea);
                return;
            }
            int respuesta;
            using (var resultado = tarea.Call<AndroidJavaObject>("getResult"))
                respuesta = resultado.Call<int>("intValue");
            if (respuesta == RespuestaSi)
            {
                aceptada = true;
                estado = EstadoActualizacion.Descargando;
                avance = 0f;
                proximaRevision = 0f;
                if (modo == Modo.Prueba)
                {
                    // El de mentira "abrio la hoja": se le dice que el jugador acepto y que bajo.
                    avanceDeLaPrueba = 0f;
                    administrador.Call("userAcceptsUpdate");
                    administrador.Call("setTotalBytesToDownload", BytesDeLaPrueba);
                    administrador.Call("downloadStarts");
                }
            }
            else if (respuesta == RespuestaNo)
            {
                // Dijo que no en la hoja de Play: como AHORA NO.
                estado = EstadoActualizacion.Nada;
                postergarPendiente = true;
            }
            else
            {
                UltimoError = "hoja: respuesta " + respuesta;
                estado = EstadoActualizacion.Nada;
            }
        }
        catch (Exception e)
        {
            Fallo("hoja", e);
        }
        finally
        {
            esperandoRespuesta = false;
        }
    }

    private static string Excepcion(AndroidJavaObject tarea)
    {
        try
        {
            if (tarea == null) return "sin tarea";
            using (var e = tarea.Call<AndroidJavaObject>("getException"))
                return e != null ? e.Call<string>("toString") : "sin excepcion";
        }
        catch (Exception e)
        {
            return e.Message;
        }
    }

    private static void Fallo(string donde, Exception e)
    {
        UltimoError = donde + ": " + e.Message;
        Debug.LogWarning("ActualizacionDePlay: fallo " + donde + ": " + e.Message);
    }

    // Lo que abre una pantalla de Play (la hoja, la instalacion) va en el hilo de la interfaz de
    // Android, no en el de Unity.
    private static void EnElHiloDeAndroid(Action accion)
    {
        try
        {
            actividad.Call("runOnUiThread", new AndroidJavaRunnable(() => accion()));
        }
        catch (Exception e)
        {
            esperandoRespuesta = false;
            Fallo("hilo", e);
        }
    }

    // Solo en la APK de prueba: un Toast de Android, que se ve aunque no haya texto en la tabla.
    private static void Avisar(string texto)
    {
        try
        {
            using (var toast = new AndroidJavaClass("android.widget.Toast"))
            using (var mensaje = toast.CallStatic<AndroidJavaObject>("makeText", actividad, texto, 1))
                mensaje.Call("show");
        }
        catch (Exception e)
        {
            Debug.LogWarning("ActualizacionDePlay: " + texto + " (" + e.Message + ")");
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

        // El nombre y la firma son los de la interfaz de Java: Unity lo busca por nombre (ver
        // PedidoDeResena). Preserve evita que el linker lo saque.
        [UnityEngine.Scripting.Preserve]
        public void onComplete(AndroidJavaObject tarea)
        {
            alCompletar(tarea);
        }
    }
}
