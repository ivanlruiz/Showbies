using System;
using UnityEngine;
#if UNITY_ANDROID
using Unity.Notifications.Android;
#endif

public enum EstadoDelPermiso { SinPedir, Pidiendo, Concedido, Negado, Bloqueado }

// Lo unico que habla con el paquete Mobile Notifications de Unity (com.unity.mobile.notifications, sin servidor y sin
// el EDM4U). Su ensamblado existe solo en Android y en el editor, asi que lo de adentro va con #if UNITY_ANDROID: es una
// clase estatica sin campos serializados, y la trampa del #if (que borra el cableado de las escenas) no aplica. Fuera
// del telefono no hace nada (en el editor el paquete intentaria hablar con Java).
public static class NotificacionesAndroid
{
    public const string Canal = "showbies_rewards";
    // Los dibujos los registra ConstructorNotificaciones en la configuracion del paquete (Project Settings).
    public const string IconoChico = "icono_chico";
    public const string IconoGrande = "icono_grande";

    private static bool iniciado;
#if UNITY_ANDROID
    private static PermissionRequest pedido;
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetearEstadoCompartido()
    {
        iniciado = false;
#if UNITY_ANDROID
        pedido = null;
#endif
    }

    public static bool Disponible
    {
        get { return Application.platform == RuntimePlatform.Android; }
    }

    private static bool Iniciar()
    {
        if (!Disponible) return false;
        if (iniciado) return true;
#if UNITY_ANDROID
        try
        {
            AndroidNotificationCenter.Initialize();
            AndroidNotificationCenter.RegisterNotificationChannel(new AndroidNotificationChannel
            {
                Id = Canal,
                Name = Textos.De("notif_canal"),
                Description = Textos.De("notif_canal_descripcion"),
                Importance = Importance.Default,
            });
            iniciado = true;
        }
        catch (Exception e)
        {
            Debug.LogWarning("NotificacionesAndroid: no se pudo iniciar: " + e.Message);
        }
#endif
        return iniciado;
    }

    public static EstadoDelPermiso Permiso
    {
        get
        {
            if (!Iniciar()) return EstadoDelPermiso.Bloqueado;
#if UNITY_ANDROID
            if (pedido != null && pedido.Status == PermissionStatus.RequestPending) return EstadoDelPermiso.Pidiendo;
            switch (AndroidNotificationCenter.UserPermissionToPost)
            {
                case PermissionStatus.Allowed: return EstadoDelPermiso.Concedido;
                case PermissionStatus.NotRequested: return EstadoDelPermiso.SinPedir;
                case PermissionStatus.RequestPending: return EstadoDelPermiso.Pidiendo;
                case PermissionStatus.NotificationsBlockedForApp: return EstadoDelPermiso.Bloqueado;
                default:
                    // Negado: Android deja volver a pedirlo despues de un rechazo, no de dos (ahi ya no muestra el
                    // dialogo y solo queda abrir los ajustes).
                    return AndroidNotificationCenter.ShouldShowPermissionToPostRationale ? EstadoDelPermiso.Negado : EstadoDelPermiso.Bloqueado;
            }
#else
            return EstadoDelPermiso.Bloqueado;
#endif
        }
    }

    // El dialogo del sistema (Android 13+; antes, queda concedido en el acto). Lo que contesta se lee con Permiso.
    public static void PedirPermiso()
    {
        if (!Iniciar()) return;
#if UNITY_ANDROID
        try
        {
            pedido = new PermissionRequest();
        }
        catch (Exception e)
        {
            Debug.LogWarning("NotificacionesAndroid: no se pudo pedir el permiso: " + e.Message);
        }
#endif
    }

    // Los ajustes de notificaciones del juego, para el que lo nego para siempre y lo quiere prender desde OPCIONES.
    public static void AbrirAjustes()
    {
        if (!Iniciar()) return;
#if UNITY_ANDROID
        try { AndroidNotificationCenter.OpenNotificationSettings(); }
        catch (Exception e) { Debug.LogWarning("NotificacionesAndroid: no se pudieron abrir los ajustes: " + e.Message); }
#endif
    }

    public static void Programar(string titulo, string texto, DateTime cuando)
    {
        if (!Iniciar()) return;
#if UNITY_ANDROID
        try
        {
            var aviso = new AndroidNotification(titulo, texto, cuando)
            {
                SmallIcon = IconoChico,
                LargeIcon = IconoGrande,
                ShouldAutoCancel = true,
            };
            AndroidNotificationCenter.SendNotification(aviso, Canal);
        }
        catch (Exception e)
        {
            Debug.LogWarning("NotificacionesAndroid: no se pudo programar: " + e.Message);
        }
#endif
    }

    // Las programadas y las que estan en la barra: al volver al juego no queda nada viejo.
    public static void CancelarTodo()
    {
        if (!Iniciar()) return;
#if UNITY_ANDROID
        try { AndroidNotificationCenter.CancelAllNotifications(); }
        catch (Exception e) { Debug.LogWarning("NotificacionesAndroid: no se pudo cancelar: " + e.Message); }
#endif
    }
}
