using UnityEngine;

public enum Lengua
{
    // El orden es el de las columnas de la tabla de textos: no cambiarlo.
    Ingles = 0,
    Espanol = 1
}

// El idioma en el que se muestra el juego. Arranca en **ingles**: la primera vez
// que se abre, y en cada instalacion nueva, sin mirar el idioma del telefono. El
// jugador lo cambia desde el globo del menu, y la eleccion queda guardada.
//
// Va en PlayerPrefs y no en el progreso porque es una preferencia del dispositivo,
// como el record: no tiene nada que ver con las monedas ni tiene que migrarse.
//
// Nadie se suscribe a nada: quien muestra texto mira Revision, que sube cada vez
// que cambia el idioma, igual que Progreso.Revision.
public static class Idioma
{
    public const string ClavePreferencia = "Idioma";
    public const Lengua PorDefecto = Lengua.Ingles;

    // En el orden en que aparecen en el selector.
    public static readonly Lengua[] Todas = { Lengua.Ingles, Lengua.Espanol };

    private static bool cargado;
    private static Lengua actual;
    private static bool fijadoParaPruebas;

    public static int Revision { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetearEstadoCompartido()
    {
        cargado = false;
        fijadoParaPruebas = false;
        Revision = 0;
    }

    public static Lengua Actual
    {
        get
        {
            Cargar();
            return actual;
        }
    }

    public static void Cambiar(Lengua nueva)
    {
        Cargar();
        if (nueva == actual) return;

        actual = nueva;
        Revision++;

        if (fijadoParaPruebas) return;
        PlayerPrefs.SetString(ClavePreferencia, Codigo(nueva));
        PlayerPrefs.Save();
    }

    public static string Codigo(Lengua lengua)
    {
        return lengua == Lengua.Espanol ? "es" : "en";
    }

    // Lo que no se reconoce (nada guardado, un codigo de una version futura) es el
    // idioma por defecto. Estatico para probarlo sin tocar PlayerPrefs.
    public static Lengua DesdeCodigo(string codigo)
    {
        return codigo == "es" ? Lengua.Espanol : PorDefecto;
    }

    // Cada idioma escrito en si mismo: "Español" lo reconoce quien no lee ingles.
    public static string NombrePropio(Lengua lengua)
    {
        return lengua == Lengua.Espanol ? "Español" : "English";
    }

    private static void Cargar()
    {
        if (cargado) return;
        cargado = true;
        actual = DesdeCodigo(PlayerPrefs.GetString(ClavePreferencia, ""));
    }

    // Solo para las pruebas del editor: fija el idioma sin escribir PlayerPrefs.
    // Con null vuelve al guardado.
    public static void UsarParaPruebas(Lengua? lengua)
    {
        if (lengua.HasValue)
        {
            fijadoParaPruebas = true;
            cargado = true;
            actual = lengua.Value;
        }
        else
        {
            fijadoParaPruebas = false;
            cargado = false;
        }
        Revision++;
    }
}
