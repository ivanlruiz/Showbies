using UnityEngine;
using UnityEngine.SceneManagement;

// El sombrero de calabaza, el premio final de Halloween (EventoHalloween), en la cabeza del
// jugador en todas las partidas, con evento o sin el. Se apaga en OPCIONES. Lo pone
// InstaladorHalloween en la raiz del jugador y se cuelga en Start, como la pistola
// (ArmaEnLaMano): el modelo no esta en el prefab sino en cada escena, y el Animator tiene
// que haber arrancado para darnos el hueso de la cabeza.
public class SombreroDelJugador : MonoBehaviour
{
    public static SombreroDelJugador Instalar(Scene escena)
    {
        foreach (var raiz in escena.GetRootGameObjects())
        {
            var jugador = raiz.GetComponentInChildren<PlayerController>(true);
            if (jugador == null) continue;
            var sombrero = jugador.GetComponent<SombreroDelJugador>();
            return sombrero != null ? sombrero : jugador.gameObject.AddComponent<SombreroDelJugador>();
        }
        return null;
    }

    private void Start()
    {
        var animador = GetComponentInChildren<Animator>();
        Transform cabeza = animador != null && animador.isHuman ? animador.GetBoneTransform(HumanBodyBones.Head) : null;
        if (cabeza == null) cabeza = Disfraces.Cabeza(transform);
        var sombrero = Disfraces.PonerSombrero(cabeza);
        // De noche, con la luz de relleno de los personajes, como la pistola.
        if (sombrero != null) Personajes.PonerEnLaCapa(sombrero);
    }
}
