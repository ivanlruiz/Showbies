using UnityEngine;
using UnityEngine.UI;
using TMPro;

// El botoncito "VIDEOS: SÍ / NO" del menú. Apagarlo es apagar de verdad: el juego
// deja de ofrecer videos en la derrota y en donde entren después, sin pedir nada
// a cambio y sin volver a preguntar.
//
// Existe porque el premio tiene que ser una elección, no una insistencia: quien
// no quiere ver videos no tiene que esquivar el botón partida por partida. Si no
// hay proveedor de anuncios (Windows, o mientras no haya red), el interruptor no
// aparece: no hay nada que prender.
public class InterruptorVideos : MonoBehaviour
{
    public GameObject raiz;
    public Button boton;
    public TMP_Text etiqueta;

    private void Start()
    {
        if (!ServicioAnuncios.HayProveedor)
        {
            if (raiz != null) raiz.SetActive(false);
            return;
        }

        if (raiz != null) raiz.SetActive(true);
        if (boton != null)
        {
            boton.onClick.RemoveListener(Apretar);
            boton.onClick.AddListener(Apretar);
        }
        Escribir();
    }

    private void Apretar()
    {
        Progreso.OfrecerVideos = !Progreso.OfrecerVideos;
        Escribir();
    }

    private void Escribir()
    {
        if (etiqueta == null) return;
        etiqueta.text = Progreso.OfrecerVideos ? "VIDEOS: SÍ" : "VIDEOS: NO";
    }
}
