using System.Collections.Generic;
using UnityEngine;

// Las cartas que pueden salir, en Resources para que la partida las encuentre sin cablear nada
// (como el catalogo de las mejoras). Lo arma ShowBies > Cartas > Armar las cartas
// (ConstructorCartas). Si falta, no hay cartas: la experiencia se suma igual y la eleccion no
// se abre, con un LogError.
[CreateAssetMenu(fileName = "CatalogoCartas", menuName = "ShowBies/Catalogo de cartas")]
public class CatalogoCartas : ScriptableObject
{
    public const string Ruta = "CatalogoCartas";

    public List<Carta> cartas = new List<Carta>();

    private static CatalogoCartas instancia;
    private static bool buscado;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetearEstadoCompartido()
    {
        instancia = null;
        buscado = false;
    }

    public static CatalogoCartas Instancia
    {
        get
        {
            if (!buscado)
            {
                buscado = true;
                instancia = Resources.Load<CatalogoCartas>(Ruta);
                if (instancia == null) Debug.LogError("CatalogoCartas: no esta en Resources; la partida sigue sin cartas.");
            }
            return instancia;
        }
    }

    public Carta Buscar(string id)
    {
        foreach (var c in cartas) if (c != null && c.id == id) return c;
        return null;
    }
}
