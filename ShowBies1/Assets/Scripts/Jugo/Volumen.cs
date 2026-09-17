using UnityEngine;

// Los dos volumenes que elige el jugador: efectos y musica, de 0 a 1. Son una
// preferencia del dispositivo, como el idioma, asi que van en PlayerPrefs y no en
// el progreso.
//
// Nadie se suscribe a nada: quien suena mira Revision (FuenteConVolumen) o lee el
// valor en el momento de tocar (Sonidos), igual que con Idioma y Progreso.
public static class Volumen
{
    public const string ClaveEfectos = "VolumenEfectos";
    public const string ClaveMusica = "VolumenMusica";

    private static float efectos = -1f;
    private static float musica = -1f;

    public static int Revision { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetearEstadoCompartido()
    {
        efectos = -1f;
        musica = -1f;
        Revision = 0;
    }

    public static float Efectos
    {
        get
        {
            if (efectos < 0f) efectos = Mathf.Clamp01(PlayerPrefs.GetFloat(ClaveEfectos, 1f));
            return efectos;
        }
    }

    public static float Musica
    {
        get
        {
            if (musica < 0f) musica = Mathf.Clamp01(PlayerPrefs.GetFloat(ClaveMusica, 1f));
            return musica;
        }
    }

    public static void FijarEfectos(float valor)
    {
        efectos = Mathf.Clamp01(valor);
        PlayerPrefs.SetFloat(ClaveEfectos, efectos);
        Revision++;
    }

    public static void FijarMusica(float valor)
    {
        musica = Mathf.Clamp01(valor);
        PlayerPrefs.SetFloat(ClaveMusica, musica);
        Revision++;
    }

    // Se llama al soltar el control, no en cada movimiento: Save escribe a disco.
    public static void Guardar()
    {
        PlayerPrefs.Save();
    }
}
