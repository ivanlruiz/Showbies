using UnityEngine;

// Ajustes de rendimiento que se aplican al arrancar el juego, antes de la
// primera escena. Salio de la primera prueba en un telefono: bajos FPS.
public static class ConfiguracionRendimiento
{
    // Fraccion de la resolucion nativa a la que se renderiza en movil. Este juego
    // no necesita la nitidez de una pantalla de 1080p o mas: renderizar a 3/4 baja
    // ~45% el trabajo de la GPU. La UI no cambia de lugar porque los canvas escalan
    // con la pantalla, pero se dibuja en la misma resolucion: tambien pierde nitidez.
    public const float EscalaResolucionMovil = 0.75f;

    // Pero sin bajar de este alto (el lado corto: el juego va en horizontal). Con 0,75
    // fijo, un telefono de 720p, lo mas comun en la gama baja, dibujaba todo (el HUD y
    // los textos incluidos) en 540 y lo estiraba, y esos son los que tienen menos
    // pixeles y menos ganan con bajar. Asi un 720p va nativo, un 1080p queda como
    // antes (en 810) y un 1440p, en 1080.
    public const int AltoMinimoMovil = 720;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Aplicar()
    {
        // Unity en Android limita a 30 FPS por defecto salvo que se le pida otra
        // cosa. Sin esta linea el juego se siente lento aunque sobre GPU.
        Application.targetFrameRate = 60;

        if (Plataforma.EsMovil && !Application.isEditor)
        {
            int alto = Mathf.Min(Screen.width, Screen.height);
            float escala = Mathf.Clamp((float)AltoMinimoMovil / alto, EscalaResolucionMovil, 1f);
            if (escala < 1f)
            {
                int w = Mathf.RoundToInt(Screen.width * escala);
                int h = Mathf.RoundToInt(Screen.height * escala);
                Screen.SetResolution(w, h, FullScreenMode.FullScreenWindow);
            }
        }
    }
}
