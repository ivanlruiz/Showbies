using UnityEngine;

// Ajustes de rendimiento que se aplican al arrancar el juego, antes de la
// primera escena. Salio de la primera prueba en un telefono: bajos FPS.
public static class ConfiguracionRendimiento
{
    // Fraccion de la resolucion nativa a la que se renderiza en movil. Las
    // pantallas de telefono son 1080p o mas, y este juego no necesita esa
    // nitidez: renderizar a 3/4 baja ~45% el trabajo de la GPU. La UI no se
    // entera porque los canvas escalan con la pantalla.
    public const float EscalaResolucionMovil = 0.75f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Aplicar()
    {
        // Unity en Android limita a 30 FPS por defecto salvo que se le pida otra
        // cosa. Sin esta linea el juego se siente lento aunque sobre GPU.
        Application.targetFrameRate = 60;

        if (Plataforma.EsMovil && !Application.isEditor)
        {
            int w = Mathf.RoundToInt(Screen.width * EscalaResolucionMovil);
            int h = Mathf.RoundToInt(Screen.height * EscalaResolucionMovil);
            Screen.SetResolution(w, h, FullScreenMode.FullScreenWindow);
        }
    }
}
