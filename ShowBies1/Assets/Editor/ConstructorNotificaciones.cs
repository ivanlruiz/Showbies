using Unity.Notifications;
using UnityEditor;
using UnityEngine;

// Deja configurado el paquete de notificaciones locales (com.unity.mobile.notifications) para ShowBies; queda en
// ProjectSettings/NotificationsSettings.asset, que se versiona. Se vuelve a correr si cambian los dibujos.
// - Sin alarmas exactas: la politica de Play prohibe declarar sus permisos si el juego no las necesita, y un aviso de
//   recompensa puede llegar unos minutos tarde.
// - Que sobrevivan a un reinicio del telefono (agrega RECEIVE_BOOT_COMPLETED, que no pide declaracion).
// - Los dibujos: el chico (la barra de estado) tiene que ser blanco sobre transparente o Android muestra un cuadrado; es
//   el cofre de las misiones. El grande es el icono del juego. Los dos legibles (isReadable), que el paquete los copia.
public static class ConstructorNotificaciones
{
    public const string IconoChico = "Assets/Notificaciones/IconoNotificacionChico.png";
    public const string IconoGrande = "Assets/Notificaciones/IconoNotificacionGrande.png";

    [MenuItem("ShowBies/Notificaciones/Configurar el paquete")]
    public static void Configurar()
    {
        Legible(IconoChico, true);
        Legible(IconoGrande, false);

        NotificationSettings.AndroidSettings.ExactSchedulingOption = 0;
        NotificationSettings.AndroidSettings.RescheduleOnDeviceRestart = true;

        NotificationSettings.AndroidSettings.RemoveDrawableResource(NotificacionesAndroid.IconoChico);
        NotificationSettings.AndroidSettings.RemoveDrawableResource(NotificacionesAndroid.IconoGrande);
        NotificationSettings.AndroidSettings.AddDrawableResource(NotificacionesAndroid.IconoChico,
            AssetDatabase.LoadAssetAtPath<Texture2D>(IconoChico), NotificationIconType.Small);
        NotificationSettings.AndroidSettings.AddDrawableResource(NotificacionesAndroid.IconoGrande,
            AssetDatabase.LoadAssetAtPath<Texture2D>(IconoGrande), NotificationIconType.Large);
        Debug.Log("ConstructorNotificaciones: paquete configurado (sin alarmas exactas, se reprograman al reiniciar, dos iconos).");
    }

    private static void Legible(string ruta, bool conAlfa)
    {
        var importador = AssetImporter.GetAtPath(ruta) as TextureImporter;
        if (importador == null) { Debug.LogError("ConstructorNotificaciones: no esta " + ruta); return; }
        importador.textureType = TextureImporterType.Default;
        importador.isReadable = true;
        importador.mipmapEnabled = false;
        importador.npotScale = TextureImporterNPOTScale.None;
        importador.alphaIsTransparency = conAlfa;
        importador.textureCompression = TextureImporterCompression.Uncompressed;
        importador.SaveAndReimport();
    }
}
