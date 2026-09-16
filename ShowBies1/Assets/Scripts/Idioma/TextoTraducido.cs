using TMPro;
using UnityEngine;

// Va al lado de cada texto fijo de escenas y prefabs (los titulos, las etiquetas de
// los botones) y le escribe el texto del idioma actual: al prenderse y cada vez que
// cambia el idioma, asi el selector cambia todo lo que esta en pantalla sin
// recargar la escena.
//
// Los textos que arma el codigo (puntajes, contadores, las tarjetas de la tienda)
// no lo llevan: los escribe su script con Textos.De o Textos.Formato.
[RequireComponent(typeof(TMP_Text))]
public class TextoTraducido : MonoBehaviour
{
    [Tooltip("La fila de Assets/Idioma/Resources/Textos.txt.")]
    public string id;

    private TMP_Text texto;
    private int revisionVista = -1;

    private void Awake()
    {
        texto = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        Aplicar();
    }

    private void Update()
    {
        if (Idioma.Revision != revisionVista) Aplicar();
    }

    public void Aplicar()
    {
        revisionVista = Idioma.Revision;
        if (texto == null || string.IsNullOrEmpty(id)) return;
        texto.text = Textos.De(id);
    }
}
