using UnityEditor;
using UnityEngine;

// Cambiar el idioma desde el editor, sin tener que entrar al menu y tocar el globo.
// Escribe la misma preferencia que el selector del juego (PlayerPrefs["Idioma"]),
// asi que tambien sirve para ver una escena en el otro idioma: en play el cambio se
// ve en el acto, y fuera de play se aplica al entrar.
//
// "Olvidar" borra la preferencia: es lo que ve alguien que abre el juego por
// primera vez, que tiene que ser en ingles.
public static class IdiomaEnElEditor
{
    const string RutaIngles = "ShowBies/Idioma/English";
    const string RutaEspanol = "ShowBies/Idioma/Español";
    const string RutaOlvidar = "ShowBies/Idioma/Olvidar (como la primera vez)";

    [MenuItem(RutaIngles, false, 0)]
    static void Ingles() { Poner(Lengua.Ingles); }

    [MenuItem(RutaEspanol, false, 1)]
    static void Espanol() { Poner(Lengua.Espanol); }

    [MenuItem(RutaOlvidar, false, 20)]
    static void Olvidar()
    {
        PlayerPrefs.DeleteKey(Idioma.ClavePreferencia);
        PlayerPrefs.Save();
        Idioma.UsarParaPruebas(null);
        Debug.Log("Idioma: sin preferencia guardada, arranca en " + Idioma.NombrePropio(Idioma.Actual) + ".");
    }

    [MenuItem(RutaIngles, true)]
    static bool ValidarIngles() { return Marcar(); }

    [MenuItem(RutaEspanol, true)]
    static bool ValidarEspanol() { return Marcar(); }

    static bool Marcar()
    {
        Menu.SetChecked(RutaIngles, Idioma.Actual == Lengua.Ingles);
        Menu.SetChecked(RutaEspanol, Idioma.Actual == Lengua.Espanol);
        return true;
    }

    static void Poner(Lengua lengua)
    {
        Idioma.Cambiar(lengua);
        // Cambiar no escribe si el idioma ya era ese: se escribe igual, para que quede
        // guardado aunque viniera del valor por defecto.
        PlayerPrefs.SetString(Idioma.ClavePreferencia, Idioma.Codigo(lengua));
        PlayerPrefs.Save();
        Textos.Recargar();
        Debug.Log("Idioma: " + Idioma.NombrePropio(lengua) + ".");
    }
}
