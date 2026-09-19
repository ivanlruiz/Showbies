using System;
using UnityEngine;

// La hora con que se decide "que dia es hoy" para la recompensa diaria y los topes de
// los anuncios, a prueba de adelantar el reloj del telefono. Atrasarlo ya no servia
// (Progreso.EsDiaNuevo solo acepta un dia mayor), pero adelantarlo un dia por vez cobraba
// la diaria en cadena.
//
// El juego no usa internet, asi que no hay una hora que no se pueda tocar. Lo que si hay
// en Android son dos contadores que el reloj no mueve: el tiempo desde que se prendio el
// telefono, contando el tiempo dormido (SystemClock.elapsedRealtime), y cuantas veces se
// prendio (Settings.Global.BOOT_COUNT). Al cobrar se guarda una marca con la hora y esos
// dos numeros. Despues, si el telefono no se reinicio y el reloj dice que paso bastante
// mas tiempo que el que de verdad paso, se usa la hora de la marca mas el tiempo real.
// Reiniciando el telefono se puede igual: es friccion, no un candado.
//
// Fuera de Android (el editor, Windows) no hay marca y vale el reloj.
public static class RelojConfiable
{
    // Lo que el reloj puede adelantarse sin que se note: cambios de hora de verano y
    // ajustes chicos. Mas que esto, en el mismo arranque, es haberlo movido a mano.
    public static readonly TimeSpan Tolerancia = TimeSpan.FromHours(2);

    private static int frameLeido = -1;
    private static bool leido;
    private static long msLeidos;
    private static int arranquesLeidos;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetearEstadoCompartido()
    {
        frameLeido = -1;
        leido = false;
    }

    // La hora que vale, en UTC. Estatica y sin nada del telefono para probarla: la marca
    // es la del ultimo cobro (hora en ticks UTC, milisegundos desde el arranque, numero de
    // arranque); sin marca, o en otro arranque, vale el reloj.
    public static DateTime Confiable(DateTime ahoraUtc, long ahoraMs, int ahoraArranques,
                                     long marcaUtcTicks, long marcaMs, int marcaArranques)
    {
        bool mismoArranque = marcaUtcTicks > 0 && marcaArranques > 0 && ahoraArranques == marcaArranques && ahoraMs >= marcaMs;
        if (!mismoArranque) return ahoraUtc;

        DateTime real = new DateTime(marcaUtcTicks, DateTimeKind.Utc).AddMilliseconds(ahoraMs - marcaMs);
        return ahoraUtc > real + Tolerancia ? real : ahoraUtc;
    }

    // Los dos contadores del telefono. Falso fuera de Android o si no se pudieron leer.
    // Se leen una vez por frame como mucho: son dos llamadas por JNI.
    public static bool Leer(out long msDesdeArranque, out int arranques)
    {
        if (Time.frameCount != frameLeido)
        {
            frameLeido = Time.frameCount;
            leido = LeerDelTelefono(out msLeidos, out arranquesLeidos);
        }
        msDesdeArranque = msLeidos;
        arranques = arranquesLeidos;
        return leido;
    }

    private static bool LeerDelTelefono(out long ms, out int arranques)
    {
        ms = 0;
        arranques = 0;
        if (Application.platform != RuntimePlatform.Android) return false;

        try
        {
            using (var reloj = new AndroidJavaClass("android.os.SystemClock"))
                ms = reloj.CallStatic<long>("elapsedRealtime");

            using (var jugador = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var actividad = jugador.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var resolvedor = actividad.Call<AndroidJavaObject>("getContentResolver"))
            using (var global = new AndroidJavaClass("android.provider.Settings$Global"))
                arranques = global.CallStatic<int>("getInt", resolvedor, "boot_count", 0);

            return ms > 0 && arranques > 0;
        }
        catch (Exception e)
        {
            Debug.LogWarning("RelojConfiable: no se pudo leer el reloj del telefono: " + e.Message);
            return false;
        }
    }
}
