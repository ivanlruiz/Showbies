using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class Enemy : ScriptableObject
{
    public int hp;
    public int daño;
    // En m/s. Con decimales desde el 9/10 (era entero): el veloz va a 10,5, un poco mas lento que
    // el jugador (11,5), que si no no tiene como escaparle.
    public float velocidad;

    // Cuánto suma matarlo. Antes esto estaba repartido en cinco if de
    // BulletController, que sumaba por bala en vez de por muerte.
    public int puntos = 1;

    // Cuantas monedas suelta al morir: al azar entre min y max, los dos incluidos.
    // Cada una vale el multiplicador que le puso quien lo hizo aparecer.
    public int monedasMin = 1;
    public int monedasMax = 3;
}
