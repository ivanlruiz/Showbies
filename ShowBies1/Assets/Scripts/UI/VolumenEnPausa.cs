using TMPro;
using UnityEngine;

// Los volumenes de efectos y de musica dentro del menu de pausa, debajo de los
// botones: se arman al arrancar, con la fuente del titulo de la pausa. Va en la
// raiz del prefab MenuPausa.
public class VolumenEnPausa : MonoBehaviour
{
    public MenuPausa menuPausa;
    public TMP_Text titulo;                 // de donde sale la fuente
    public float alturaFila = -390f;        // bajo el ultimo boton del panel
    public float ancho = 440f;
    public float separacion = 160f;

    private Texture2D texturaPerilla;

    private void Start()
    {
        if (menuPausa == null || menuPausa.panel == null) return;

        texturaPerilla = TexturasUI.Circulo(64);
        var perilla = Sprite.Create(texturaPerilla, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));
        var padre = (RectTransform)menuPausa.panel.transform;
        var fuente = titulo != null ? titulo.font : null;
        float x = (ancho + separacion) * 0.5f;

        SliderVolumen.Crear(padre, "sonido_efectos", new Vector2(-x, alturaFila), ancho, fuente, Volumen.Efectos, Volumen.FijarEfectos, perilla);
        SliderVolumen.Crear(padre, "sonido_musica", new Vector2(x, alturaFila), ancho, fuente, Volumen.Musica, Volumen.FijarMusica, perilla);
    }

    private void OnDestroy()
    {
        if (texturaPerilla != null) Destroy(texturaPerilla);
    }
}
