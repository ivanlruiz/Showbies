using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class Enemy : ScriptableObject
{
    public int hp;
    public int daño;
    public int velocidad;

    // Cuánto suma matarlo. Antes esto estaba repartido en cinco if de
    // BulletController, que sumaba por bala en vez de por muerte.
    public int puntos = 1;
}
