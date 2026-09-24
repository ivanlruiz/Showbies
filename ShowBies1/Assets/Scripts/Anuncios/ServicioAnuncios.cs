using System;
using System.Collections.Generic;
using UnityEngine;

// La puerta de entrada a los anuncios. El juego nunca habla con la red: pregunta
// si se puede ofrecer (PuedeOfrecer) y, si el jugador acepta, pide el video
// (Mostrar). Todo lo demas (topes del dia, separacion entre videos, el proveedor,
// el hilo del aviso) vive aca.
//
// Reglas que no se negocian, porque son la diferencia entre un premio y una
// trampa: el video es siempre opt-in, se anuncia el premio exacto antes, se
// entrega una sola vez y cerrarlo antes no castiga.
public static class ServicioAnuncios
{
    private static IProveedorAnuncios proveedor;
    private static bool inicializado;
    private static ConfigAnuncios configDePruebas;

    // Cual solicitud esta esperando respuesta. El aviso de una anterior (o repetido)
    // se ignora: los SDK avisan de mas y desde cualquier hilo.
    private static int solicitud;
    private static int solicitudResuelta;

    private static Action premioPendiente;
    private static Action sinPremioPendiente;
    private static string lugarPendiente;
    private static float ultimoAnuncioEn = float.NegativeInfinity;   // en tiempo real
    private static bool audioPausadoAntes;

    private static readonly object candado = new object();
    private static readonly Queue<KeyValuePair<int, ResultadoAnuncio>> avisos = new Queue<KeyValuePair<int, ResultadoAnuncio>>();

    // Mientras hay un video en pantalla: el juego esta detras y la app puede perder
    // el foco sin que eso signifique que el jugador se fue.
    public static bool MostrandoAnuncio { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetearEstadoCompartido()
    {
        proveedor = null;
        inicializado = false;
        configDePruebas = null;
        solicitud = 0;
        solicitudResuelta = 0;
        premioPendiente = null;
        sinPremioPendiente = null;
        lugarPendiente = null;
        ultimoAnuncioEn = float.NegativeInfinity;
        audioPausadoAntes = false;
        MostrandoAnuncio = false;
        lock (candado) avisos.Clear();
    }

    public static string NombreDelProveedor
    {
        get { return Proveedor != null ? Proveedor.Nombre : "ninguno"; }
    }

    // Si hay alguien que pueda llegar a mostrar un video. Lo mira el interruptor del
    // menu, que no tiene sentido en una build sin anuncios.
    public static bool HayProveedor
    {
        get { return !(Proveedor is ProveedorNulo); }
    }

    private static IProveedorAnuncios Proveedor
    {
        get
        {
            if (inicializado) return proveedor;
            inicializado = true;

            ConfigAnuncios config = ConfigAnuncios.Instancia;
            var cual = config != null ? config.proveedor : ConfigAnuncios.Proveedor.Nulo;
            // Fuera del editor, en PC no hay anuncios (Nulo es el de Windows): asi el cartel
            // de prueba no llega a una build de Windows si el asset quedo en Falso.
            if (!Application.isEditor && !Plataforma.EsMovil) cual = ConfigAnuncios.Proveedor.Nulo;
            if (cual == ConfigAnuncios.Proveedor.Real)
                Debug.LogError("ServicioAnuncios: el proveedor Real todavia no existe; no se ofrecen videos.");
            switch (cual)
            {
                case ConfigAnuncios.Proveedor.Falso:
                    proveedor = new ProveedorFalso();
                    break;
                // Real todavia no existe: hasta que se integre la red, no hay anuncios.
                default:
                    proveedor = new ProveedorNulo();
                    break;
            }
            proveedor.Inicializar();
            return proveedor;
        }
    }

    // Crea e inicializa el proveedor al abrir la app: lo llama VigiaAplicacion.Asegurar. Si
    // no, se inicializaria la primera vez que alguien pregunta si hay video, que es justo al
    // ofrecerlo, y con una red de verdad arrancar y cargar un video tarda segundos: la
    // primera oferta de cada sesion (casi siempre el x2 de la diaria, apenas se abre el
    // menu) no encontraria nunca video. Con el nulo y el falso no cambia nada que se vea:
    // su Inicializar esta vacio y el cartel falso se arma recien al mostrarse.
    public static void Arrancar()
    {
        _ = Proveedor;
    }

    // Solo para las pruebas del editor, como Progreso.UsarCarpetaDePruebas: cambia
    // el proveedor y la config por los de la prueba y deja el servicio como recien
    // arrancado. Con null en los dos vuelve a lo de siempre. Esto existe porque lo
    // que hay del otro lado son monedas: el circuito entero (premio una sola vez,
    // topes, cerrar sin castigo) se prueba sin entrar en play.
    public static void UsarParaPruebas(IProveedorAnuncios proveedorDePrueba, ConfigAnuncios config)
    {
        proveedor = proveedorDePrueba;
        inicializado = proveedorDePrueba != null;
        configDePruebas = config;
        solicitud = 0;
        solicitudResuelta = 0;
        premioPendiente = null;
        sinPremioPendiente = null;
        lugarPendiente = null;
        ultimoAnuncioEn = float.NegativeInfinity;
        MostrandoAnuncio = false;
        lock (candado) avisos.Clear();
    }

    // La config que rige ahora: la del asset, o la de la prueba si hay una.
    public static ConfigAnuncios ConfigEnUso
    {
        get { return configDePruebas != null ? configDePruebas : ConfigAnuncios.Instancia; }
    }

    // Los segundos reales desde el ultimo video. No sobrevive a cerrar el juego, y
    // esta bien: el tope que importa entre sesiones es el del dia.
    public static float SegundosDesdeElUltimo
    {
        get { return Time.realtimeSinceStartup - ultimoAnuncioEn; }
    }

    // El nucleo de "se puede ofrecer", sin escena ni proveedor: asi se prueba entero.
    public static bool PuedeOfrecerConDatos(ConfigAnuncios config, bool ofrecerVideos, bool mostrandoAnuncio,
                                            int partidasTerminadas, double segundosJugados, int usosDeHoy,
                                            float segundosDesdeElUltimo, bool proveedorListo,
                                            int videosDeLaPartida)
    {
        if (config == null || !ofrecerVideos || mostrandoAnuncio || !proveedorListo) return false;
        if (partidasTerminadas < config.partidasTerminadasMinimas) return false;
        if (segundosJugados < config.segundosJugadosMinimos) return false;
        if (usosDeHoy >= config.vecesPorDia) return false;
        if (videosDeLaPartida >= config.vecesPorPartida) return false;
        return segundosDesdeElUltimo >= config.segundosEntreAnuncios;
    }

    public static bool PuedeOfrecer(string lugar)
    {
        if (string.IsNullOrEmpty(lugar)) return false;

        ConfigAnuncios config = ConfigEnUso;
        return PuedeOfrecerConDatos(config, Progreso.OfrecerVideos, MostrandoAnuncio,
                                    Progreso.PartidasTerminadas, Progreso.SegundosJugados,
                                    // En TOTAL, no por lugar: el tope de "3 videos por dia"
                                    // es global. Contandolo por lugar eran 3 de revivir mas
                                    // 3 del x2 de la derrota mas 3 del x2 de la diaria: nueve.
                                    Progreso.UsosDeHoyEnTotal(), SegundosDesdeElUltimo,
                                    Proveedor.Listo(lugar),
                                    // El tope por partida es para la derrota y el revivir; la diaria
                                    // se cobra en el menu, entre partidas.
                                    lugar == LugarAnuncio.DuplicarRegalo ? 0 : Progreso.VideosDeLaPartida);
    }

    // Pide el video. Devuelve si se lanzo; el premio llega despues, en el hilo
    // principal. "alNoRecompensar" es para volver a la pantalla como estaba, no para
    // castigar: cerrar el video no cuesta nada.
    public static bool Mostrar(string lugar, Action alRecompensar, Action alNoRecompensar)
    {
        if (!PuedeOfrecer(lugar)) return false;

        solicitud++;
        MostrandoAnuncio = true;
        lugarPendiente = lugar;
        premioPendiente = alRecompensar;
        sinPremioPendiente = alNoRecompensar;

        // Antes de irse a pantalla completa: si Android mata la app mientras se ve el
        // video, lo jugado hasta acá tiene que estar en disco.
        Progreso.Guardar();
        PlayerPrefs.Save();

        audioPausadoAntes = AudioListener.pause;
        AudioListener.pause = true;

        int token = solicitud;
        VigiaAplicacion.Asegurar();
        // Si el proveedor tira una excepcion al pedir el video, nadie va a avisar como
        // termino y lo de arriba no se deshace nunca: el juego mudo, MostrandoAnuncio
        // prendido el resto de la sesion (no se ofrece nada mas) y la pantalla que lo pidio
        // esperando para siempre (el revivir, congelado y sin botones; la diaria, sin poder
        // cerrarse). Se resuelve como si no hubiera habido video: sin premio y sin castigo,
        // y la pantalla vuelve a como estaba. No como FallaAlMostrar, que se premia: este ni
        // llego a empezar.
        try
        {
            Proveedor.Mostrar(lugar, resultado => Encolar(token, resultado));
        }
        catch (Exception e)
        {
            Debug.LogError("ServicioAnuncios: el proveedor fallo al pedir el video de " + lugar + "; se sigue sin video. " + e);
            Encolar(token, ResultadoAnuncio.NoDisponible);
        }
        return true;
    }

    // La llama el proveedor, quiza desde otro hilo: solo encola.
    private static void Encolar(int token, ResultadoAnuncio resultado)
    {
        lock (candado)
        {
            avisos.Enqueue(new KeyValuePair<int, ResultadoAnuncio>(token, resultado));
        }
    }

    // La vacia VigiaAplicacion en su Update, en el hilo principal.
    public static void AtenderAvisos()
    {
        while (true)
        {
            KeyValuePair<int, ResultadoAnuncio> aviso;
            lock (candado)
            {
                if (avisos.Count == 0) return;
                aviso = avisos.Dequeue();
            }
            Resolver(aviso.Key, aviso.Value);
        }
    }

    private static void Resolver(int token, ResultadoAnuncio resultado)
    {
        // De una solicitud vieja, o la segunda vez que avisan de la misma.
        if (token != solicitud || token == solicitudResuelta) return;
        solicitudResuelta = token;

        string lugar = lugarPendiente;
        Action premio = premioPendiente;
        Action sinPremio = sinPremioPendiente;
        premioPendiente = null;
        sinPremioPendiente = null;
        lugarPendiente = null;

        AudioListener.pause = audioPausadoAntes;
        MostrandoAnuncio = false;

        bool premiar = resultado == ResultadoAnuncio.Recompensado;

        // Un video que se rompio al mostrarse no es culpa del jugador: se premia
        // igual, pero pocas veces por dia, porque cortar la red seria la forma facil
        // de cobrar sin mirar nada.
        if (resultado == ResultadoAnuncio.FallaAlMostrar)
        {
            ConfigAnuncios config = ConfigEnUso;
            int tope = config != null ? config.fallasPremiadasPorDia : 0;
            if (Progreso.FallasPremiadasHoy < tope)
            {
                Progreso.RegistrarFallaPremiada();
                premiar = true;
            }
        }

        // Solo gasta el tope y la separacion entre videos lo que se premio: cerrar el video
        // deja la oferta en pie. Antes la separacion contaba cualquier resultado y la oferta
        // desaparecia 60 s despues de cerrarlo.
        if (premiar)
        {
            Progreso.RegistrarUsoDeAnuncio(lugar);
            ultimoAnuncioEn = Time.realtimeSinceStartup;
        }

        if (premiar)
        {
            if (premio != null) premio();
        }
        else if (sinPremio != null)
        {
            sinPremio();
        }
    }
}
