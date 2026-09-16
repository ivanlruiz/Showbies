using UnityEngine;
using TMPro;

// El boton OLEADAS del panel de modos. Si hay una partida de oleadas a medias
// (Progreso.OleadaEnCurso), avisa abajo en que oleada se sigue: tocar OLEADAS
// retoma esa, y el jugador tiene que saberlo antes de tocar.
//
// Como BotonModoLibre, escribe el texto el mismo y apaga el TextoTraducido del
// hijo mientras muestra el aviso, para que no se pisen al cambiar de idioma.
public class BotonOleadas : MonoBehaviour
{
    public TMP_Text texto;

    private TextoTraducido traducido;
    private int revisionProgreso = -1;
    private int revisionIdioma = -1;

    private void Awake()
    {
        if (texto != null) traducido = texto.GetComponent<TextoTraducido>();
    }

    private void OnEnable()
    {
        Pintar();
    }

    private void Update()
    {
        if (revisionProgreso != Progreso.Revision || revisionIdioma != Idioma.Revision) Pintar();
    }

    private void Pintar()
    {
        revisionProgreso = Progreso.Revision;
        revisionIdioma = Idioma.Revision;

        int oleada = Progreso.OleadaEnCurso;
        bool sigue = oleada > 1;
        if (traducido != null) traducido.enabled = !sigue;
        if (texto == null) return;

        texto.text = sigue
            ? Textos.De("modo_oleadas") + "\n<size=62%>" + Textos.Formato("modo_oleadas_continuar", oleada) + "</size>"
            : Textos.De("modo_oleadas");
    }
}
