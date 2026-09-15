using UnityEditor;
using UnityEngine;

// Elegir con que se juega en el editor sin cambiar el build target: con el target
// en Android el editor se comporta como un telefono (joysticks), y cambiar el
// target reimporta todo. Es una preferencia de esta maquina (EditorPrefs), no del
// proyecto. Se aplica al entrar en play: los joysticks y los textos que muestra
// ConditionalShow se deciden al cargar la escena.
public static class ControlesEnElEditor
{
    const string Ruta = "ShowBies/Controles/Teclado y mouse en el editor";

    [MenuItem(Ruta)]
    static void Alternar()
    {
        bool teclado = !EditorPrefs.GetBool(Plataforma.ClaveTecladoEnElEditor, false);
        EditorPrefs.SetBool(Plataforma.ClaveTecladoEnElEditor, teclado);
        Plataforma.OlvidarPreferenciaDelEditor();
        Debug.Log("Controles en el editor: " + (teclado
            ? "teclado y mouse (WASD, mouse, Espacio para la granada)"
            : "los del build target (" + EditorUserBuildSettings.activeBuildTarget + ")") +
            (EditorApplication.isPlaying ? ". Se aplica al volver a entrar en play." : "."));
    }

    [MenuItem(Ruta, true)]
    static bool Validar()
    {
        Menu.SetChecked(Ruta, EditorPrefs.GetBool(Plataforma.ClaveTecladoEnElEditor, false));
        return true;
    }
}
