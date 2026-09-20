using UnityEngine;
using UnityEngine.UI;

// Le pone un papel (RolDeTema) al color de un Image o de un texto de una escena o un
// prefab, para que cambie con el tema. Va en los fondos, los textos y los surcos que
// hoy son claros; los botones de color (el verde de jugar, el dorado de la tienda) no
// lo llevan, porque su color es lo que significan.
//
// El color del tema claro es el que ya trae el objeto: se guarda en `colorClaro`, que
// se completa solo al agregar el componente y se mantiene al día en el editor. Se
// serializa y no se lee en Awake a proposito: OpcionesSonido y ConfirmarSalir copian
// la ventana del idioma cuando ya puede estar pintada de oscuro, y la copia leeria el
// color oscuro como si fuera el de siempre.
//
// No usa RequireComponent: si el objeto no tiene Graphic, el componente no hace nada.
public class PintarConTema : MonoBehaviour
{
    public RolDeTema rol = RolDeTema.Texto;

    [Tooltip("El color del tema claro: el que trae la escena. Se completa solo.")]
    public Color colorClaro = Color.white;

    private Graphic grafico;
    private int revisionVista = -1;

    // El color que le toca hoy. Lo consulta quien anima ese mismo grafico y necesita
    // saber a que color volver (TarjetaMejora con el texto del nivel).
    public Color Actual()
    {
        return Tema.Elegir(colorClaro, rol);
    }

    private void Reset()
    {
        TomarColor();
    }

    private void OnValidate()
    {
        // En el editor el juego no esta pintado, asi que lo que tiene el objeto es
        // siempre el color claro. En play no, que ahi puede estar el oscuro.
        if (!Application.isPlaying) TomarColor();
    }

    private void OnEnable()
    {
        revisionVista = -1;
        Aplicar();
    }

    private void Update()
    {
        if (revisionVista != Tema.Revision) Aplicar();
    }

    private void Aplicar()
    {
        revisionVista = Tema.Revision;
        if (grafico == null) grafico = GetComponent<Graphic>();
        if (grafico == null) return;
        grafico.color = Tema.Elegir(colorClaro, rol);
    }

    private void TomarColor()
    {
        var g = GetComponent<Graphic>();
        if (g != null) colorClaro = g.color;
    }
}
