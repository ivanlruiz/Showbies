using System.Collections.Generic;
using UnityEngine;

// Sonidos cortos de efectos: golpes, muertes, explosiones, monedas. Comparten un
// objeto de la escena con fuentes de audio 2D.
//
// PlayOneShot usa el pitch de la fuente en la que suena, asi que un sonido con el
// tono cambiado necesita su propia fuente mientras suena: esos rotan entre varias.
// Los que suenan con su tono (pitch 1) van todos a una fuente que nunca cambia de
// pitch, para que un sonido largo como la explosion no quede desafinado cuando
// un golpe reusa su fuente.
//
// Cada clip tiene una separacion minima entre dos veces: con el arma pegando 20
// veces por segundo, sin techo se apilarian decenas del mismo sonido.
public static class Sonidos
{
    private const int CantidadDeFuentes = 12;

    private static AudioSource neutra;
    private static AudioSource[] conTono;
    private static int proxima;
    private static readonly Dictionary<AudioClip, float> ultimaVez = new Dictionary<AudioClip, float>();

    // Las fuentes son de la escena y se destruyen con ella: se vuelven a crear al
    // notarlas null. El diccionario es de clips, que son assets y no se destruyen.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetearEstadoCompartido()
    {
        neutra = null;
        conTono = null;
        proxima = 0;
        ultimaVez.Clear();
    }

    // Devuelve si sono, o si la separacion minima lo dejo afuera.
    public static bool Tocar(AudioClip clip, float volumen = 1f, float pitch = 1f, float variacionPitch = 0f, float separacionMinima = 0.03f)
    {
        if (clip == null) return false;

        // Tiempo sin escalar: la pausa de impacto baja timeScale y el techo de
        // sonidos tiene que medir igual.
        float ahora = Time.unscaledTime;
        float anterior;
        if (ultimaVez.TryGetValue(clip, out anterior) && ahora - anterior < separacionMinima) return false;
        ultimaVez[clip] = ahora;

        CrearFuentesSiFaltan();

        float tono = pitch * (1f + Random.Range(-variacionPitch, variacionPitch));
        if (Mathf.Approximately(tono, 1f))
        {
            neutra.PlayOneShot(clip, volumen);
            return true;
        }

        proxima = (proxima + 1) % conTono.Length;
        AudioSource fuente = conTono[proxima];
        fuente.pitch = tono;
        fuente.PlayOneShot(clip, volumen);
        return true;
    }

    private static void CrearFuentesSiFaltan()
    {
        if (neutra != null) return;

        var go = new GameObject("Sonidos");
        neutra = CrearFuente(go);
        conTono = new AudioSource[CantidadDeFuentes];
        for (int i = 0; i < conTono.Length; i++) conTono[i] = CrearFuente(go);
    }

    private static AudioSource CrearFuente(GameObject go)
    {
        var fuente = go.AddComponent<AudioSource>();
        fuente.playOnAwake = false;
        fuente.spatialBlend = 0f;
        return fuente;
    }
}
