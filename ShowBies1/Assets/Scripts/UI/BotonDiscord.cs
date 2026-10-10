using UnityEngine;
using UnityEngine.UI;

// El boton de Discord del menu (pedido de Ivan para la 1.4.0): una copia redonda del globo
// del idioma con el logo de Discord, arriba a la derecha, despues de las misiones, el
// bestiario y la medalla. Abre la invitacion del servidor de ShowBies: en el telefono, la
// app de Discord si esta instalada, y si no el navegador. Es un enlace y nada mas: el juego
// no le manda nada a Discord.
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
    private RectTransform rtBoton;
    private float delGlobo, anchoDelGlobo;
    private int lugarPuesto = -1;

    // Start y no Awake: SelectorIdioma pone los dibujos del globo en su Awake, y la copia
    // tiene que salir con ellos.
    private void Start()
    {
        if (selectorIdioma == null || selectorIdioma.botonGlobo == null) return;
        var globo = (RectTransform)selectorIdioma.botonGlobo.transform;
        var boton = ConstructorUI.BotonDeEsquina(selectorIdioma, "BotonDiscord",
                                                 DesdeLaDerecha(globo.anchoredPosition.x, globo.rect.width, separacion), icono, Abrir);
        if (boton != null)
        {
            rtBoton = (RectTransform)boton.transform;
            delGlobo = globo.anchoredPosition.x;
            anchoDelGlobo = globo.rect.width;
            Acomodar();
        }

        // Blanco puro: los iconos de las esquinas salen color crema, el del icono del globo, y
        // el logo de Discord no se tine.
        if (boton == null || selectorIdioma.iconoGlobo == null) return;
        var dibujo = boton.transform.Find(ConstructorUI.Ruta(selectorIdioma.iconoGlobo.transform, globo));
        var imagen = dibujo != null ? dibujo.GetComponent<Image>() : null;
        if (imagen != null) imagen.color = Color.white;
    }

    // A la derecha ya estan las misiones, el bestiario y la medalla: este es el cuarto.
    public static float DesdeLaDerecha(float delGlobo, float ancho, float separacion)
    {
        return delGlobo + 3f * (ancho + separacion);
    }

    // El lugar en la fila de arriba a la derecha, desde el borde: 0 las misiones, 1 el
    // bestiario, 2 la medalla y 3 Discord, con todo destapado.
    public static float EnElLugar(int lugar, float delGlobo, float ancho, float separacion)
    {
        return delGlobo + lugar * (ancho + separacion);
    }

    // Donde va segun lo que haya destapado (Desbloqueo), para no dejar huecos: hasta el nivel 5
    // no estan el bestiario ni la medalla, y va al lado de las misiones; antes de la oleada 5
    // tampoco estan las misiones, y va primero.
    public static int Lugar()
    {
        return Desbloqueo.Todo ? 3 : Desbloqueo.Misiones ? 1 : 0;
    }

    private void Update()
    {
        Acomodar();
    }

    private void Acomodar()
    {
        if (rtBoton == null) return;
        int lugar = Lugar();
        if (lugar == lugarPuesto) return;
        lugarPuesto = lugar;
        float x = EnElLugar(lugar, delGlobo, anchoDelGlobo, separacion);
        rtBoton.anchoredPosition = new Vector2(-x, rtBoton.anchoredPosition.y);
    }

    private void Abrir()
    {
        if (Time.unscaledTime < proximoToque) return;
        proximoToque = Time.unscaledTime + EsperaEntreToques;
        Application.OpenURL(Invitacion);
    }
}
