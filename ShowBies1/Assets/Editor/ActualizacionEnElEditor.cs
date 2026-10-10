using UnityEditor;
using UnityEngine;

// ShowBies > Actualización > Simular en el editor: en play, el menu cree que hay una version
// nueva en Play (ActualizacionDePlay) y saca el aviso, con una descarga de mentira de unos
// segundos y un REINICIAR que solo lo anota en la consola. Es de esta maquina (EditorPrefs),
// no del proyecto, como Forzar Halloween. Se lee al entrar en play.
public static class ActualizacionEnElEditor
{
    const string Entrada = "ShowBies/Actualización/Simular en el editor";

    [MenuItem(Entrada)]
    static void Simular()
    {
        bool prendido = !EditorPrefs.GetBool(ActualizacionDePlay.ClaveSimularEnElEditor, false);
        EditorPrefs.SetBool(ActualizacionDePlay.ClaveSimularEnElEditor, prendido);
        Debug.Log("Actualización en el editor: " + (prendido ? "simulada (hay una versión nueva)" : "apagada"));
    }

    [MenuItem(Entrada, true)]
    static bool SimularValidar()
    {
        Menu.SetChecked(Entrada, EditorPrefs.GetBool(ActualizacionDePlay.ClaveSimularEnElEditor, false));
        return true;
    }
}
