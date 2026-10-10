using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// Entrar en play sin traer Unity al frente. Pedido de Ivan (10/10): cuando un banco o una
// prueba manejada desde afuera le daba Play mientras el usaba la PC, la ventana de Unity se le
// ponia delante. Lo hace la vista Game, que por defecto esta en "Play Focused": al entrar en
// play toma el foco, y con el, la ventana. En "Play Unfocused" no.
//
// Solo para lo que entra en play solo (los bancos, por RespaldoDelBanco, o Entrar desde
// afuera): al volver a modo edicion la vista queda como estaba (la de Ivan, en "Play
// Maximized"), asi su Play sigue como siempre y el teclado le llega al juego sin hacer clic.
//
// La opcion es interna de Unity (PlayModeView.enterPlayModeBehavior): se toca por reflexion,
// y si en otra version de Unity no esta, no se hace nada y se avisa en la consola.
[InitializeOnLoad]
public static class PlaySinFoco
{
    const string ClaveAnterior = "ShowBies.PlaySinFoco.Anterior";

    static PlaySinFoco()
    {
        EditorApplication.playModeStateChanged -= AlCambiar;
        EditorApplication.playModeStateChanged += AlCambiar;
    }

    // Lo que hay que llamar desde afuera en vez de EditorApplication.EnterPlaymode.
    public static void Entrar()
    {
        Prender();
        EditorApplication.EnterPlaymode();
    }

    // Pone cada vista Game en "Play Unfocused" y anota como estaba, para devolverlo al salir.
    public static void Prender()
    {
        Type vista;
        PropertyInfo comportamiento;
        object sinFoco;
        if (!Buscar(out vista, out comportamiento, out sinFoco)) return;
        var vistas = Resources.FindObjectsOfTypeAll(vista);
        // Se anota la primera vez nomas: dos bancos seguidos no tienen que anotar el "sin foco".
        // Hay una sola vista Game; si hubiera mas, vuelven todas a lo que tenia la primera.
        if (vistas.Length > 0 && string.IsNullOrEmpty(SessionState.GetString(ClaveAnterior, "")))
            SessionState.SetString(ClaveAnterior, comportamiento.GetValue(vistas[0]).ToString());
        foreach (var v in vistas) comportamiento.SetValue(v, sinFoco);
    }

    static void AlCambiar(PlayModeStateChange cambio)
    {
        if (cambio == PlayModeStateChange.EnteredEditMode) Devolver();
    }

    static void Devolver()
    {
        string anterior = SessionState.GetString(ClaveAnterior, "");
        if (string.IsNullOrEmpty(anterior)) return;
        SessionState.EraseString(ClaveAnterior);
        Type vista;
        PropertyInfo comportamiento;
        object sinFoco;
        if (!Buscar(out vista, out comportamiento, out sinFoco)) return;
        object valor;
        try { valor = Enum.Parse(comportamiento.PropertyType, anterior); }
        catch (ArgumentException) { return; }
        foreach (var v in Resources.FindObjectsOfTypeAll(vista)) comportamiento.SetValue(v, valor);
    }

    static bool Buscar(out Type vista, out PropertyInfo comportamiento, out object sinFoco)
    {
        vista = typeof(EditorWindow).Assembly.GetType("UnityEditor.PlayModeView");
        comportamiento = vista != null
            ? vista.GetProperty("enterPlayModeBehavior", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            : null;
        sinFoco = null;
        if (comportamiento != null && comportamiento.PropertyType.IsEnum)
        {
            try { sinFoco = Enum.Parse(comportamiento.PropertyType, "PlayUnfocused"); }
            catch (ArgumentException) { }
        }
        if (sinFoco != null && comportamiento.CanWrite) return true;
        Debug.LogWarning("PlaySinFoco: esta version de Unity no tiene PlayModeView.enterPlayModeBehavior con PlayUnfocused; " +
                         "Play va a traer Unity al frente.");
        return false;
    }

    // Para ver que lo encuentra: lo que dice cada vista Game.
    [MenuItem("ShowBies/Controles/Ver el foco de la vista Game")]
    public static void Mirar()
    {
        Type vista;
        PropertyInfo comportamiento;
        object sinFoco;
        if (!Buscar(out vista, out comportamiento, out sinFoco)) return;
        foreach (var v in Resources.FindObjectsOfTypeAll(vista))
            Debug.Log("PlaySinFoco: " + v.GetType().Name + " '" + ((EditorWindow)v).titleContent.text + "': " + comportamiento.GetValue(v)
                      + " (opciones: " + string.Join(", ", Enum.GetNames(comportamiento.PropertyType)) + ")");
    }
}
