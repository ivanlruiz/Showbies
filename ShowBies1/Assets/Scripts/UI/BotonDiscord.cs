using UnityEngine;
using UnityEngine.UI;

// El boton de Discord del menu (pedido de Ivan para la 1.4.0): una copia redonda del globo
// del idioma con el logo de Discord, arriba a la derecha, despues de las misiones, el
// bestiario y la medalla. Abre la invitacion del servidor de ShowBies: en el telefono, la
// app de Discord si esta instalada, y si no el navegador. Es un enlace y nada mas: el juego
// no le manda nada a Discord.
//
// Aparece en el nivel 5, con el bestiario y la medalla (Desbloqueo, 10/10, de la revision
// de Gemini elegido por Ivan): nadie se une a la comunidad de un juego que instalo hace dos
// minutos, y para el que recien empieza es un boton mas.
//
// El logo es el oficial (el "Clyde" de discord-mark-white.svg, en Sprites/UI/IconoDiscord),
// en blanco: las reglas de la marca dejan usarlo para llevar a un servidor, sin cambiarle la
// forma ni el color.
//
// Nada de esto esta en la escena: se arma en codigo con ConstructorUI, como la medalla.
// Vive en la raiz del canvas "Main Menu".
public class BotonDiscord : MonoBehaviour
{
    // La invitacion permanente (ver publicacion/pasos.md): no vence, no tiene limite de
    // usos y entra a #welcome.
    public const string Invitacion = "https://discord.gg/XsKgU7BBUd";
    // Dos toques seguidos abririan Discord dos veces.
    private const float EsperaEntreToques = 1f;

    public SelectorIdioma selectorIdioma;   // el globo, que se copia para el boton
    public Sprite icono;                    // IconoDiscord
    public float separacion = 24f;

    private float proximoToque;
    private GameObject boton;

    // Start y no Awake: SelectorIdioma pone los dibujos del globo en su Awake, y la copia
    // tiene que salir con ellos.
    private void Start()
    {
        if (selectorIdioma == null || selectorIdioma.botonGlobo == null) return;
        var globo = (RectTransform)selectorIdioma.botonGlobo.transform;
        var copia = ConstructorUI.BotonDeEsquina(selectorIdioma, "BotonDiscord",
                                                 DesdeLaDerecha(globo.anchoredPosition.x, globo.rect.width, separacion), icono, Abrir);
        if (copia == null) return;
        boton = copia.gameObject;
        Mostrar();

        // Blanco puro: los iconos de las esquinas salen color crema, el del icono del globo, y
        // el logo de Discord no se tine.
        if (selectorIdioma.iconoGlobo == null) return;
        var dibujo = copia.transform.Find(ConstructorUI.Ruta(selectorIdioma.iconoGlobo.transform, globo));
        var imagen = dibujo != null ? dibujo.GetComponent<Image>() : null;
        if (imagen != null) imagen.color = Color.white;
    }

    // A la derecha ya estan las misiones, el bestiario y la medalla: este es el cuarto.
    public static float DesdeLaDerecha(float delGlobo, float ancho, float separacion)
    {
        return delGlobo + 3f * (ancho + separacion);
    }

    private void Update()
    {
        Mostrar();
    }

    private void Mostrar()
    {
        bool hay = Desbloqueo.Todo;
        if (boton != null && boton.activeSelf != hay) boton.SetActive(hay);
    }

    private void Abrir()
    {
        if (Time.unscaledTime < proximoToque) return;
        proximoToque = Time.unscaledTime + EsperaEntreToques;
        Application.OpenURL(Invitacion);
    }
}
