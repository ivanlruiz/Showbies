using System;
using System.IO;
using UnityEngine;

// Lo que el jugador conserva entre partidas: las monedas y la mejor oleada
// completada. Vive en un JSON en persistentDataPath y no en PlayerPrefs porque
// es estado estructurado que va a crecer (los niveles de las mejoras).
//
// Las monedas se suman en memoria en el momento y se guardan en disco en puntos
// seguros: al completar una oleada, al pausar (que tambien pasa cuando la app
// pierde el foco, antes de que Android pueda matarla), al morir y al cerrar.
public static class Progreso
{
    [Serializable]
    private class Datos
    {
        public int version = 1;
        public double monedas;
        public int mejorOleada;
    }

    private const string NombreArchivo = "progreso.json";

    private static Datos datos;

    // Lo que se gano en la partida en curso, o en la ultima si ya termino.
    public static double MonedasDeLaPartida { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetearEstadoCompartido()
    {
        Application.quitting -= Guardar;
        datos = null;
        MonedasDeLaPartida = 0;
    }

    public static double Monedas
    {
        get { Cargar(); return datos.monedas; }
    }

    public static int MejorOleada
    {
        get { Cargar(); return datos.mejorOleada; }
    }

    public static void EmpezarPartida()
    {
        Cargar();
        MonedasDeLaPartida = 0;
    }

    public static void Sumar(double cantidad)
    {
        if (cantidad <= 0) return;
        Cargar();
        datos.monedas += cantidad;
        MonedasDeLaPartida += cantidad;
    }

    public static void RegistrarOleadaCompletada(int oleada)
    {
        Cargar();
        if (oleada > datos.mejorOleada) datos.mejorOleada = oleada;
    }

    // Primero un .tmp y despues la copia: si la app muere a mitad de escritura,
    // queda al menos una de las dos versiones entera.
    public static void Guardar()
    {
        if (datos == null) return;

        string ruta = Ruta();
        string temporal = ruta + ".tmp";
        try
        {
            File.WriteAllText(temporal, JsonUtility.ToJson(datos, true));
            File.Copy(temporal, ruta, true);
            File.Delete(temporal);
        }
        catch (Exception e)
        {
            Debug.LogWarning("Progreso: no se pudo guardar en " + ruta + ": " + e.Message);
        }
    }

    private static void Cargar()
    {
        if (datos != null) return;

        datos = Leer(Ruta()) ?? Leer(Ruta() + ".tmp") ?? new Datos();
        Application.quitting -= Guardar;
        Application.quitting += Guardar;
    }

    private static Datos Leer(string ruta)
    {
        try
        {
            return File.Exists(ruta) ? JsonUtility.FromJson<Datos>(File.ReadAllText(ruta)) : null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static string Ruta()
    {
        return Path.Combine(Application.persistentDataPath, NombreArchivo);
    }
}
