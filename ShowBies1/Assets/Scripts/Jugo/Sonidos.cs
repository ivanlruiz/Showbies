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
// pegando 20 veces por segundo, sin techo se apilarian decenas del mismo sonido. Una
// variante (Aparte) lleva su propia cuenta con el mismo clip: la muerte del tanque y
// la de un zombi chico son el mismo muerte.wav, y compartiendo la cuenta la del
// tanque se perdia cada vez que un chico habia muerto en los 40 ms de antes.
//
// Los clics de la interfaz (TocarUI) salen por fuentes que no se pausan: con la
// pausa puesta, AudioListener.pause calla todas las demas, y el clic de CONTINUAR
// sonaba recien al reanudar (el de MENU y REINICIAR, nunca).
//
// El limitador. No hay mezclador: lo que suena a la vez se suma, y lo que pasa de la
// escala completa lo recorta el telefono, con un crujido. Una granada que mata toca
// en el mismo cuadro el golpe, la muerte y la explosion, que ya viene casi a 0 dBFS,
// y la suma pasaba de 1 durante 10 a 40 ms. Por eso se lleva la carga de lo que
// arranco hace poco (sus volumenes, cada uno cayendo segun lo que dura su clip) y, si
// lo nuevo la pasa del techo, suena mas bajo, en proporcion. Lo que se toca en el
// mismo cuadro sale junto, en el mismo buffer de audio, asi que baja parejo: la
// explosion no queda sola pagando por el golpe y la muerte que se tocaron antes que
// ella (salvo lo que ya salio por la fuente neutra, que es de todos y no se toca).
// El techo es de volumenes y no de muestras, y alto a proposito: los picos de dos
// sonidos casi nunca caen juntos, y lo de siempre (un tiro que mata, la muerte del
// jefe, la escalera de monedas, los ticks de la derrota) no llega y suena igual.
public static class Sonidos
{
    private const int CantidadDeFuentes = 12;
    private const int CantidadProgramadas = 16;
    private const int CantidadDeInterfaz = 3;

    // La variante con su propia separacion (ver arriba).
    public const int Aparte = 1;

    private static AudioSource neutra;
    private static AudioSource[] conTono;
    private static double[] conTonoLibreEn;
    private static int proxima;
    private static AudioSource[] programadas;
    private static double[] programadasLibreEn;
    private static int proximaProgramada;
    private static AudioSource[] interfaz;
    private static double[] interfazLibreEn;
    private static int proximaInterfaz;
    private static readonly Dictionary<(AudioClip, int), float> ultimaVez = new Dictionary<(AudioClip, int), float>();

    // AudioSettings.dspTime avanza de a un buffer entero (10 a 40 ms en Android) y
    // marca el que ya se esta procesando: agendar en dspTime + 0 cae en el pasado y
    // la nota sale en el buffer siguiente, y dos llamadas del mismo frame pueden
    // leer buffers distintos. Por eso todo lo de un frame cuenta desde una misma
    // base, la primera lectura del frame, y lo agendado le suma un margen que lo
    // deja siempre en el futuro.
    private const double MargenProgramado = 0.05;
    private static int frameBase = -1;
    private static double dspBase;

    // El limitador: lo que arranco hace poco, en una ronda que pisa lo mas viejo.
    private const float TechoDeCarga = 1.4f;
    private const int Recientes = 32;
    private static readonly double[] recienteInicio = new double[Recientes];
    private static readonly float[] recienteVolumen = new float[Recientes];
    private static readonly float[] recienteAtaque = new float[Recientes];
    private static int proximoReciente;

    // Lo tocado en el frame actual, para bajarlo parejo si algo del mismo frame lo pide.
    private const int MaximoPorFrame = 8;
    private static int frameDeCarga = -1;
    private static float cargaDeAntes;          // lo de frames anteriores, al empezar este
    private static float pedidoDelFrame;        // la suma de lo pedido en este frame
    private static int enElFrame;
    private static readonly AudioSource[] fuenteDelFrame = new AudioSource[MaximoPorFrame];   // null: la neutra
    private static readonly int[] recienteDelFrame = new int[MaximoPorFrame];
    private static readonly float[] pedidoDeCadaUno = new float[MaximoPorFrame];

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
        interfaz = null;
        interfazLibreEn = null;
        proximaInterfaz = 0;
        ultimaVez.Clear();
        frameBase = -1;
        dspBase = 0;
        System.Array.Clear(recienteInicio, 0, Recientes);
        System.Array.Clear(recienteVolumen, 0, Recientes);
        System.Array.Clear(recienteAtaque, 0, Recientes);
        proximoReciente = 0;
        frameDeCarga = -1;
        cargaDeAntes = 0f;
        pedidoDelFrame = 0f;
        enElFrame = 0;
        System.Array.Clear(fuenteDelFrame, 0, MaximoPorFrame);
    }

    // Devuelve si sono, o si la separacion minima lo dejo afuera.
    public static bool Tocar(AudioClip clip, float volumen = 1f, float pitch = 1f, float variacionPitch = 0f,
                             float separacionMinima = 0.03f, int variante = 0)
    {
        return Sonar(clip, volumen, pitch, variacionPitch, separacionMinima, variante, false);
    }

    // Lo mismo, por una fuente que la pausa no calla: los clics de los botones y la
    // muestra del volumen de efectos.
    public static bool TocarUI(AudioClip clip, float volumen = 1f, float pitch = 1f, float variacionPitch = 0f,
                               float separacionMinima = 0.03f)
    {
        return Sonar(clip, volumen, pitch, variacionPitch, separacionMinima, 0, true);
    }

    private static bool Sonar(AudioClip clip, float volumen, float pitch, float variacionPitch, float separacionMinima,
                              int variante, bool deInterfaz)
    {
        if (clip == null) return false;

        // Tiempo sin escalar: la pausa de impacto baja timeScale y el techo de
        // sonidos tiene que medir igual.
        float ahora = Time.unscaledTime;
        var clave = (clip, variante);
        float anterior;
        if (ultimaVez.TryGetValue(clave, out anterior) && ahora - anterior < separacionMinima) return false;
        ultimaVez[clave] = ahora;

        CrearFuentesSiFaltan();

        // El volumen de efectos que eligio el jugador.
        volumen *= Volumen.Efectos;
        if (volumen <= 0f) return true;

        float tono = pitch * (1f + Random.Range(-variacionPitch, variacionPitch));
        AudioSource fuente = null;   // null: la neutra
        if (deInterfaz)
        {
            int i = ElegirFuente(interfazLibreEn, ref proximaInterfaz);
            fuente = interfaz[i];
            interfazLibreEn[i] = AudioSettings.dspTime + clip.length / Mathf.Max(0.01f, tono);
        }
        else if (!Mathf.Approximately(tono, 1f))
        {
            int i = ElegirFuente(conTonoLibreEn, ref proxima);
            fuente = conTono[i];
            conTonoLibreEn[i] = AudioSettings.dspTime + clip.length / Mathf.Max(0.01f, tono);
        }

        float ganancia = LimitarEnElFrame(volumen, clip, tono, fuente);
        if (fuente == null)
        {
            neutra.PlayOneShot(clip, volumen * ganancia);
            return true;
        }

        // Una fuente propia lleva la ganancia en su volumen, asi se puede bajar despues
        // si algo del mismo frame lo pide (todavia no empezo a sonar).
        fuente.pitch = tono;
        fuente.volume = ganancia;
        fuente.PlayOneShot(clip, volumen);
        return true;
    }

    // Agenda un sonido para dentro de 'demora' segundos, exacto al sample. No pasa
    // por la separacion minima: quien programa un arpegio quiere todas sus notas.
    // Si pasa por el limitador, nota por nota, contra lo que este sonando cuando
    // arranca cada una (las anteriores del mismo arpegio incluidas).
    public static void Programar(AudioClip clip, double demora, float volumen = 1f, float pitch = 1f)
    {
        if (clip == null) return;

        CrearFuentesSiFaltan();

        double inicio = DspDelFrame() + MargenProgramado + System.Math.Max(0.0, demora);
        int i = ElegirFuente(programadasLibreEn, ref proximaProgramada);
        AudioSource fuente = programadas[i];
        fuente.clip = clip;
        float volumenFinal = Mathf.Clamp01(volumen * Volumen.Efectos);
        if (volumenFinal > 0f)
        {
            volumenFinal *= GananciaDelLimitador(volumenFinal, CargaEn(inicio));
            Anotar(inicio, volumenFinal, clip, pitch);
        }
        fuente.volume = volumenFinal;
        fuente.pitch = pitch;
        fuente.PlayScheduled(inicio);
        programadasLibreEn[i] = inicio + clip.length / Mathf.Max(0.01f, pitch);
    }

    // Semitonos a pitch: +12 es el doble (una octava arriba), -12 la mitad.
    public static float PitchDe(float semitonos)
    {
        return Mathf.Pow(2f, semitonos / 12f);
    }

    // --- El limitador ------------------------------------------------------------

    // Cuanto baja lo que pide 'pedido' con 'carga' sonando: 1 mientras no pasen del
    // techo, y en proporcion si pasan. Nunca llega a callar. Estatica y sin estado,
    // para probarla sin escena.
    public static float GananciaDelLimitador(float pedido, float carga)
    {
        float total = carga + pedido;
        return total > TechoDeCarga ? TechoDeCarga / total : 1f;
    }

    // Lo que queda de un sonido de 'volumen' que arranco hace 'segundos'.
    public static float CargaQueQueda(float volumen, float segundos, float ataque)
    {
        return volumen * Mathf.Exp(-Mathf.Max(0f, segundos) / Mathf.Max(0.001f, ataque));
    }

    // Cuanto tarda en caer lo fuerte de un sonido que dura 'duracion': un cuarto, entre
    // 20 y 300 ms. El golpe se apaga enseguida y la explosion sigue fuerte un buen rato.
    public static float AtaqueDe(float duracion)
    {
        return Mathf.Clamp(0.25f * duracion, 0.02f, 0.3f);
    }

    // La hora de DSP con que cuenta todo lo de un frame: la primera lectura del frame.
    private static double DspDelFrame()
    {
        if (Time.frameCount != frameBase)
        {
            frameBase = Time.frameCount;
            dspBase = AudioSettings.dspTime;
        }
        return dspBase;
    }

    // Lo que suena en 'momento' (hora de DSP). Lo que arranca despues (una nota agendada
    // mas adelante) no cuenta: todavia no suena.
    private static float CargaEn(double momento)
    {
        float carga = 0f;
        for (int i = 0; i < Recientes; i++)
        {
            if (recienteVolumen[i] <= 0f) continue;
            double hace = momento - recienteInicio[i];
            if (hace < 0.0) continue;
            carga += CargaQueQueda(recienteVolumen[i], (float)hace, recienteAtaque[i]);
        }
        return carga;
    }

    private static int Anotar(double inicio, float volumen, AudioClip clip, float tono)
    {
        int i = proximoReciente;
        recienteInicio[i] = inicio;
        recienteVolumen[i] = volumen;
        recienteAtaque[i] = AtaqueDe(clip.length / Mathf.Max(0.01f, tono));
        proximoReciente = (proximoReciente + 1) % Recientes;
        return i;
    }

    // La ganancia de algo de 'volumen' que arranca en este frame. Lo que ya se toco en el
    // mismo frame baja a la misma: su fuente es suya (salvo la neutra, que sigue como
    // salio) y todavia no empezo a sonar.
    private static float LimitarEnElFrame(float volumen, AudioClip clip, float tono, AudioSource fuente)
    {
        double ahora = DspDelFrame();
        if (Time.frameCount != frameDeCarga)
        {
            frameDeCarga = Time.frameCount;
            cargaDeAntes = CargaEn(ahora);
            pedidoDelFrame = 0f;
            enElFrame = 0;
        }
        pedidoDelFrame += volumen;
        float ganancia = GananciaDelLimitador(pedidoDelFrame, cargaDeAntes);

        for (int k = 0; k < enElFrame; k++)
        {
            if (fuenteDelFrame[k] == null) continue;
            fuenteDelFrame[k].volume = ganancia;
            // Si la ronda ya piso ese lugar (con algo de otro frame o una nota agendada),
            // se deja.
            int r = recienteDelFrame[k];
            if (recienteInicio[r] == ahora) recienteVolumen[r] = pedidoDeCadaUno[k] * ganancia;
        }

        int reciente = Anotar(ahora, volumen * ganancia, clip, tono);
        if (enElFrame < MaximoPorFrame)
        {
            fuenteDelFrame[enElFrame] = fuente;
            recienteDelFrame[enElFrame] = reciente;
            pedidoDeCadaUno[enElFrame] = volumen;
            enElFrame++;
        }
        return ganancia;
    }

    // --- Las fuentes -------------------------------------------------------------

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

        interfaz = new AudioSource[CantidadDeInterfaz];
        interfazLibreEn = new double[CantidadDeInterfaz];
        for (int i = 0; i < interfaz.Length; i++)
        {
            interfaz[i] = CrearFuente(go);
            interfaz[i].ignoreListenerPause = true;
        }
    }

    private static AudioSource CrearFuente(GameObject go)
    {
        var fuente = go.AddComponent<AudioSource>();
        fuente.playOnAwake = false;
        fuente.spatialBlend = 0f;
        return fuente;
    }
}
