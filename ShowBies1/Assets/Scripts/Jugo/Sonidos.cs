using System.Collections.Generic;
using UnityEngine;

// Sonidos cortos de efectos: golpes, muertes, explosiones, monedas, la tienda.
// Comparten un objeto de la escena con fuentes de audio 2D.
//
// PlayOneShot usa el pitch de la fuente en la que suena, asi que un sonido con el
// tono cambiado necesita su propia fuente mientras suena: si otro sonido reusa esa
// fuente y le cambia el pitch, el que estaba sonando se desafina en el acto. Los
// que suenan con su tono (pitch 1) van todos a una fuente que nunca cambia de
// pitch, para que un sonido largo como la explosion no quede desafinado.
//
// Las fuentes con tono se eligen libres: cada una guarda en tiempo de DSP cuando
// termina lo ultimo que se le mando, y se toma la primera que ya termino. No se usa
// isPlaying porque con PlayOneShot no es confiable (mira el clip de la fuente, no
// los one-shots). Si estan todas ocupadas se usa la que termina antes, que corta la
// cola de esa nota en vez de desafinar una que recien empieza.
//
// Programar usa PlayScheduled, que reemplaza el clip de la fuente: por eso tiene su
// propio grupo, y una fuente con algo agendado no se reusa hasta que termina. Es lo
// que hace sonar a tiempo los arpegios, que con Update quedarian atados a los FPS.
//
// Cada clip tiene una separacion minima entre dos veces en Tocar: con el arma
// pegando 20 veces por segundo, sin techo se apilarian decenas del mismo sonido.
public static class Sonidos
{
    private const int CantidadDeFuentes = 12;
    private const int CantidadProgramadas = 16;

    private static AudioSource neutra;
    private static AudioSource[] conTono;
    private static double[] conTonoLibreEn;
    private static int proxima;
    private static AudioSource[] programadas;
    private static double[] programadasLibreEn;
    private static int proximaProgramada;
    private static readonly Dictionary<AudioClip, float> ultimaVez = new Dictionary<AudioClip, float>();

    // AudioSettings.dspTime avanza de a un buffer entero (10 a 40 ms en Android) y
    // marca el que ya se esta procesando: agendar en dspTime + 0 cae en el pasado y
    // la nota sale en el buffer siguiente, y dos llamadas del mismo frame pueden
    // leer buffers distintos. Por eso todo lo agendado en un frame cuenta desde una
    // misma base, con un margen que la deja siempre en el futuro.
    private const double MargenProgramado = 0.05;
    private static int frameBase = -1;
    private static double dspBase;

    // Las fuentes son de la escena y se destruyen con ella: se vuelven a crear al
    // notarlas null. El diccionario es de clips, que son assets y no se destruyen.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetearEstadoCompartido()
    {
        neutra = null;
        conTono = null;
        conTonoLibreEn = null;
        proxima = 0;
        programadas = null;
        programadasLibreEn = null;
        proximaProgramada = 0;
        ultimaVez.Clear();
        frameBase = -1;
        dspBase = 0;
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

        // El volumen de efectos que eligio el jugador.
        volumen *= Volumen.Efectos;
        if (volumen <= 0f) return true;

        float tono = pitch * (1f + Random.Range(-variacionPitch, variacionPitch));
        if (Mathf.Approximately(tono, 1f))
        {
            neutra.PlayOneShot(clip, volumen);
            return true;
        }

        int i = ElegirFuente(conTonoLibreEn, ref proxima);
        AudioSource fuente = conTono[i];
        fuente.pitch = tono;
        fuente.PlayOneShot(clip, volumen);
        conTonoLibreEn[i] = AudioSettings.dspTime + clip.length / Mathf.Max(0.01f, tono);
        return true;
    }

    // Agenda un sonido para dentro de 'demora' segundos, exacto al sample. No pasa
    // por la separacion minima: quien programa un arpegio quiere todas sus notas.
    public static void Programar(AudioClip clip, double demora, float volumen = 1f, float pitch = 1f)
    {
        if (clip == null) return;

        CrearFuentesSiFaltan();

        if (Time.frameCount != frameBase)
        {
            frameBase = Time.frameCount;
            dspBase = AudioSettings.dspTime + MargenProgramado;
        }
        double inicio = dspBase + System.Math.Max(0.0, demora);
        int i = ElegirFuente(programadasLibreEn, ref proximaProgramada);
        AudioSource fuente = programadas[i];
        fuente.clip = clip;
        fuente.volume = Mathf.Clamp01(volumen * Volumen.Efectos);
        fuente.pitch = pitch;
        fuente.PlayScheduled(inicio);
        programadasLibreEn[i] = inicio + clip.length / Mathf.Max(0.01f, pitch);
    }

    // Semitonos a pitch: +12 es el doble (una octava arriba), -12 la mitad.
    public static float PitchDe(float semitonos)
    {
        return Mathf.Pow(2f, semitonos / 12f);
    }

    // Recorre en ronda desde la siguiente a la ultima usada, asi las fuentes libres
    // se van turnando, y toma la primera que ya termino. Si no hay ninguna, la que
    // termina antes.
    private static int ElegirFuente(double[] libreEn, ref int proximaFuente)
    {
        double ahora = AudioSettings.dspTime;
        int cantidad = libreEn.Length;
        int elegida = -1;
        int menor = 0;
        for (int k = 1; k <= cantidad; k++)
        {
            int i = (proximaFuente + k) % cantidad;
            if (libreEn[i] <= ahora)
            {
                elegida = i;
                break;
            }
            if (libreEn[i] < libreEn[menor]) menor = i;
        }
        if (elegida < 0) elegida = menor;

        proximaFuente = elegida;
        return elegida;
    }

    private static void CrearFuentesSiFaltan()
    {
        if (neutra != null) return;

        var go = new GameObject("Sonidos");
        neutra = CrearFuente(go);

        conTono = new AudioSource[CantidadDeFuentes];
        conTonoLibreEn = new double[CantidadDeFuentes];
        for (int i = 0; i < conTono.Length; i++) conTono[i] = CrearFuente(go);

        programadas = new AudioSource[CantidadProgramadas];
        programadasLibreEn = new double[CantidadProgramadas];
        for (int i = 0; i < programadas.Length; i++) programadas[i] = CrearFuente(go);
    }

    private static AudioSource CrearFuente(GameObject go)
    {
        var fuente = go.AddComponent<AudioSource>();
        fuente.playOnAwake = false;
        fuente.spatialBlend = 0f;
        return fuente;
    }
}
