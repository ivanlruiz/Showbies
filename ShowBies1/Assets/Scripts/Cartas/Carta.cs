using UnityEngine;

// Una carta de la partida (1.6.0, ver CARTAS.md): lo que se elige de a una entre tres al subir
// de PODER, y dura hasta que la partida termina. Es un asset y no codigo, como las mejoras, para
// balancear sin recompilar; lo que hace cada una lo lee quien la usa (el arma, el jugador, la
// furia) por su id, con PoderDeLaPartida.
//
// El id se guarda en progreso.json con la partida en curso: no se renombra.
[CreateAssetMenu(fileName = "Carta", menuName = "ShowBies/Carta")]
public class Carta : ScriptableObject
{
    // Lo que tiene que estar comprado para que la carta pueda salir: sin granada, una carta
    // que la recarga mas rapido no sirve.
    public enum Requisito { Ninguno, Granada, Furia }

    [Tooltip("Con este id se guarda en la partida en curso. No cambiarlo despues de publicar.")]
    public string id;
    [Tooltip("Cuantas veces se puede elegir en una partida.")]
    public int tope = 1;
    [Tooltip("Lo que vale cada vez: un multiplicador (1,25 es +25 %) o una suma, segun la carta. Ver PoderDeLaPartida.")]
    public float valor = 1f;
    public Requisito requiere = Requisito.Ninguno;
    [Tooltip("El color de la tarjeta y de su icono en el HUD.")]
    public Color color = Color.white;
    [Tooltip("Lo que dice el circulo de la tarjeta mientras no tenga dibujo: uno o dos signos.")]
    public string simbolo = "?";
}
