using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine;

// Las herramientas que abren escenas en modo Single (los bancos en play, Poner la noche, los
// constructores de neon) cierran lo que este abierto sin preguntar: un cambio sin guardar se
// pierde, y un banco que se corre para verificar un cambio prueba la version de disco y dice
// TODO OK con el cambio perdido (superauditoria del 29/9). Antes de abrir nada, miran aca.
// Sin ventanas: se niega con un error que dice que escenas y como salir.
public static class EscenasSinGuardar
{
    // Si hay alguna escena cargada con cambios sin guardar: lo avisa con un error, en nombre
    // de 'quien', y devuelve verdadero para que no siga.
    public static bool Hay(string quien)
    {
        var sucias = new List<string>();
        for (int i = 0; i < EditorSceneManager.sceneCount; i++)
        {
            var escena = EditorSceneManager.GetSceneAt(i);
            if (escena.isLoaded && escena.isDirty) sucias.Add(string.IsNullOrEmpty(escena.path) ? "(una escena sin guardar nunca)" : escena.path);
        }
        if (sucias.Count == 0) return false;
        Debug.LogError(quien + ": no abre escenas con cambios sin guardar en " + string.Join(", ", sucias)
                       + ". Guardalas (File > Save, o EditorSceneManager.SaveOpenScenes()) o volve a abrirlas sin guardar, y volve a correrlo.");
        return true;
    }

    // Lo que estaba abierto, para dejarlo igual al terminar (lo que recorre varias escenas).
    public static SceneSetup[] Recordar()
    {
        return EditorSceneManager.GetSceneManagerSetup();
    }

    // Vuelve a lo que estaba abierto. Una escena que nunca se guardo no se puede volver a
    // abrir: ahi queda lo ultimo que se abrio.
    public static void Volver(SceneSetup[] abiertas)
    {
        if (abiertas == null || abiertas.Length == 0) return;
        foreach (var s in abiertas)
            if (string.IsNullOrEmpty(s.path)) return;
        EditorSceneManager.RestoreSceneManagerSetup(abiertas);
    }
}
