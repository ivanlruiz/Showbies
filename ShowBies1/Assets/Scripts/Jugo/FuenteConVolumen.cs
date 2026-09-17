using UnityEngine;

// Aplica el volumen del jugador a un AudioSource de escena o de prefab: su volumen
// queda en el del inspector por el de efectos o el de musica. Lo agrega el editor a
// todas las fuentes de las escenas (las que hacen loop son musica, el resto efectos).
//
// Quien quiera bajar una fuente un rato (la tienda baja la musica del menu) usa
// Atenuacion en vez de tocar volume, que este componente pisa.
[RequireComponent(typeof(AudioSource))]
public class FuenteConVolumen : MonoBehaviour
{
    public enum Tipo { Efecto, Musica }

    public Tipo tipo = Tipo.Efecto;

    private AudioSource fuente;
    private float volumenBase;
    private float atenuacion = 1f;
    private int revisionVista = -1;

    public float Atenuacion
    {
        get { return atenuacion; }
        set { atenuacion = Mathf.Clamp01(value); Aplicar(); }
    }

    // Estatico para probarlo sin escena.
    public static float Calcular(float volumenBase, float volumenJugador, float atenuacion)
    {
        return Mathf.Clamp01(volumenBase) * Mathf.Clamp01(volumenJugador) * Mathf.Clamp01(atenuacion);
    }

    private void Awake()
    {
        fuente = GetComponent<AudioSource>();
        volumenBase = fuente.volume;
        Aplicar();
    }

    private void Update()
    {
        if (revisionVista != Volumen.Revision) Aplicar();
    }

    private void Aplicar()
    {
        if (fuente == null) return;
        revisionVista = Volumen.Revision;
        float jugador = tipo == Tipo.Musica ? Volumen.Musica : Volumen.Efectos;
        fuente.volume = Calcular(volumenBase, jugador, atenuacion);
    }
}
