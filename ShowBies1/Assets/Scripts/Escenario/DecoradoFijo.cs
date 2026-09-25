using UnityEngine;

// Un decorado puesto en la escena, que no sale del piso ni cambia: el de la pradera en el
// modo libre y en el tutorial (en las oleadas lo maneja CapitulosDeEscenario). Al empezar lo
// junta en pocos draw calls, como hace CapitulosDeEscenario cuando termina de salir.
//
// Va en la instancia de la escena y no en el prefab: en las oleadas el mismo prefab sale del
// piso pieza por pieza, y juntado de entrada las piezas ya no se moverian.
public class DecoradoFijo : MonoBehaviour
{
    private void Start()
    {
        CapitulosDeEscenario.Juntar(gameObject);
    }
}
