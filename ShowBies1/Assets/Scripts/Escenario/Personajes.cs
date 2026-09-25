using UnityEngine;

// La capa de lo que tiene que leerse de noche: los zombis, el jugador con su pistola y las
// monedas. En las escenas de juego hay una luz de relleno que solo alumbra esta capa (la arma
// ConstructorEscenarios.PonerLaNoche): el piso queda oscuro, con los charcos de neon, y ellos
// se ven igual. Sin ella, con la luna sola, eran siluetas negras.
//
// Se pasan a la capa, al arrancar, solo los objetos que se ven y no tienen collider (las
// mallas de los modelos): la fisica de los colliders sigue en su capa de siempre.
public static class Personajes
{
    // "Personajes" en el TagManager. La prueba de logica mira que el nombre coincida.
    public const int Capa = 8;

    public static void PonerEnLaCapa(GameObject raiz)
    {
        if (raiz == null) return;
        foreach (var render in raiz.GetComponentsInChildren<Renderer>(true))
        {
            if (render.GetComponent<Collider>() != null) continue;
            render.gameObject.layer = Capa;
        }
    }
}
