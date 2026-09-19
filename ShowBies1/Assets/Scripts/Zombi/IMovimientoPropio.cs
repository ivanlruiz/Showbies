using UnityEngine;

// Un componente del zombi que a veces decide el movimiento en vez de EnemyController (el
// jefe cuando avisa o carga, JefePatrones). EnemyController lo busca en su Awake y en
// cada paso de fisica le pregunta primero: si devuelve verdadero, ya movio al zombi y
// la persecucion de siempre no corre ese paso.
public interface IMovimientoPropio
{
    bool Mover(Rigidbody rb, Transform jugador);
}
