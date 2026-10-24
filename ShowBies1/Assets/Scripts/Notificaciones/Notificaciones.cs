using System;
using System.Collections.Generic;
using UnityEngine;

// El pegamento de las notificaciones locales: al irse la app arma el estado con el progreso, le pide el plan a
// PlanDeNotificaciones y lo programa; al volver cancela todo (VigiaAplicacion, en cualquier escena). Tambien lleva la
// preferencia de OPCIONES y cuantas veces se mostro la ventana propia que pide el permiso (PedidoDeNotificaciones).
public static class Notificaciones
{
    public const string ClavePreferencia = "Notificaciones";              // 1 o 0; sin nada, prendidas
    public const string ClaveVecesPedidas = "NotificacionesPedidas";      // las veces que se mostro la ventana propia
    public const int VecesQueSePide = 2;

    // Lo ultimo que se programo, para la caja de diagnostico de la APK de prueba.
    public static List<Aviso> UltimoPlan { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetearEstadoCompartido()
    {
        UltimoPlan = null;
        PruebaPendiente = false;
    }

    // Los textos, con sus variantes: se elige una por el dia en que sale, para que no repita siempre la misma.
    // Los ids van enteros entre comillas: la prueba de idiomas los busca asi.
    private static readonly string[] TitulosPartida = { "notif_partida_titulo_1", "notif_partida_titulo_2" };
    private static readonly string[] TextosPartida = { "notif_partida_texto_1", "notif_partida_texto_2" };
    private static readonly string[] TitulosDiaria = { "notif_diaria_titulo_1", "notif_diaria_titulo_2", "notif_diaria_titulo_3" };
    private static readonly string[] TextosDiaria = { "notif_diaria_texto_1", "notif_diaria_texto_2", "notif_diaria_texto_3" };
    private static readonly string[] TitulosVuelta = { "notif_vuelta_titulo_1", "notif_vuelta_titulo_2" };
    private static readonly string[] TextosVuelta = { "notif_vuelta_texto_1", "notif_vuelta_texto_2" };
    private static readonly string[] TitulosRacha = { "notif_racha_titulo_1", "notif_racha_titulo_2" };
    private static readonly string[] TextosRacha = { "notif_racha_texto_1", "notif_racha_texto_2" };
    private static readonly string[] TitulosSemanal = { "notif_semanal_titulo_1", "notif_semanal_titulo_2" };
    private static readonly string[] TextosSemanal = { "notif_semanal_texto_1", "notif_semanal_texto_2" };
    private static readonly string[] TitulosEvento = { "notif_halloween_titulo_1" };
    private static readonly string[] TextosEvento = { "notif_halloween_texto_1" };

    public static bool Preferidas
    {
        get { return PlayerPrefs.GetInt(ClavePreferencia, 1) == 1; }
    }

    public static bool Permitidas
    {
        get { return NotificacionesAndroid.Permiso == EstadoDelPermiso.Concedido; }
    }

    // Lo que muestra el interruptor de OPCIONES: prendido si el jugador las quiere y Android las deja salir.
    public static bool Activas
    {
        get { return Preferidas && Permitidas; }
    }

    // El interruptor de OPCIONES. Prenderlo sin permiso lo pide (o, si Android ya no deja pedirlo, abre los ajustes
    // del juego); apagarlo cancela todo.
    public static void Fijar(bool activas)
    {
        PlayerPrefs.SetInt(ClavePreferencia, activas ? 1 : 0);
        PlayerPrefs.Save();
        if (!activas)
        {
            NotificacionesAndroid.CancelarTodo();
            return;
        }
        switch (NotificacionesAndroid.Permiso)
        {
            case EstadoDelPermiso.SinPedir:
            case EstadoDelPermiso.Negado:
                NotificacionesAndroid.PedirPermiso();
                break;
            case EstadoDelPermiso.Bloqueado:
                NotificacionesAndroid.AbrirAjustes();
                break;
        }
    }

    // Al irse la app (VigiaAplicacion): lo viejo se cancela y se programa el plan de esta ausencia.
    public static void AlIrse()
    {
        if (!NotificacionesAndroid.Disponible) return;
        NotificacionesAndroid.CancelarTodo();
        if (!Activas) { UltimoPlan = null; return; }
        var plan = PlanDeNotificaciones.Planear(EstadoActual(DateTime.Now));
        foreach (var aviso in plan)
            NotificacionesAndroid.Programar(Titulo(aviso), Texto(aviso), aviso.cuando);
        UltimoPlan = plan;
        ProgramarLaPrueba();
    }

    // Al volver: el jugador ya esta, no hace falta avisarle nada.
    public static void AlVolver()
    {
        if (!NotificacionesAndroid.Disponible) return;
        NotificacionesAndroid.CancelarTodo();
    }

    public static EstadoParaAvisar EstadoActual(DateTime ahora)
    {
        var estado = new EstadoParaAvisar
        {
            ahora = ahora,
            hayPartida = Progreso.HayOleadaParaRetomar,
            oleadaEnCurso = Progreso.OleadaEnCurso,
            diariaHabilitada = !PrimeraVez.NoTerminoPartidas,
            todo = Desbloqueo.Todo,
            diaUltimaDiaria = Progreso.DiaUltimaRecompensa,
            racha = Progreso.RachaRecompensa,
            semanalPendiente = Desbloqueo.Todo && !DesafioSemanal.Cumplido,
        };
        // El evento cuenta en sus fechas de verdad (forzado, en la APK de prueba, no tiene final) y si falta el sombrero.
        int hoy = EventoHalloween.Hoy();
        if (EventoHalloween.Activo && EventoHalloween.EnFechas(hoy) && !EventoHalloween.TieneSombrero)
            estado.finDelEvento = new DateTime(hoy / 10000, EventoHalloween.Fin / 100, EventoHalloween.Fin % 100);
        return estado;
    }

    public static string Titulo(Aviso aviso)
    {
        return Textos.Formato(Elegir(TitulosDe(aviso.tipo), aviso), aviso.numero);
    }

    public static string Texto(Aviso aviso)
    {
        return Textos.Formato(Elegir(TextosDe(aviso.tipo), aviso), aviso.numero);
    }

    private static string Elegir(string[] variantes, Aviso aviso)
    {
        return variantes[(aviso.cuando.DayOfYear + (int)aviso.tipo) % variantes.Length];
    }

    private static string[] TitulosDe(TipoDeAviso tipo)
    {
        switch (tipo)
        {
            case TipoDeAviso.Partida: return TitulosPartida;
            case TipoDeAviso.Diaria: return TitulosDiaria;
            case TipoDeAviso.Racha: return TitulosRacha;
            case TipoDeAviso.Semanal: return TitulosSemanal;
            case TipoDeAviso.Evento: return TitulosEvento;
            default: return TitulosVuelta;
        }
    }

    private static string[] TextosDe(TipoDeAviso tipo)
    {
        switch (tipo)
        {
            case TipoDeAviso.Partida: return TextosPartida;
            case TipoDeAviso.Diaria: return TextosDiaria;
            case TipoDeAviso.Racha: return TextosRacha;
            case TipoDeAviso.Semanal: return TextosSemanal;
            case TipoDeAviso.Evento: return TextosEvento;
            default: return TextosVuelta;
        }
    }

    // --- La ventana propia que pide el permiso (PedidoDeNotificaciones) ---------------------------------------------

    public static int VecesPedidas
    {
        get { return PlayerPrefs.GetInt(ClaveVecesPedidas, 0); }
    }

    // La primera vez, al llegar a la oleada 5 (las misiones) o al cobrar la primera diaria; la segunda (si dijo AHORA
    // NO), en el nivel 5 o con una racha de 2 dias. Nunca si Android ya lo concedio o no deja pedirlo, ni si el jugador
    // las apago en OPCIONES.
    public static bool CorrespondePedir(int vecesPedidas, EstadoDelPermiso permiso, bool preferidas, bool misiones,
                                        bool cobroUnaDiaria, bool todo, int racha)
    {
        if (!preferidas || vecesPedidas >= VecesQueSePide) return false;
        if (permiso != EstadoDelPermiso.SinPedir && permiso != EstadoDelPermiso.Negado) return false;
        if (vecesPedidas == 0) return misiones || cobroUnaDiaria;
        return todo || racha >= PlanDeNotificaciones.RachaMinima;
    }

    public static bool CorrespondePedirAhora
    {
        get
        {
            if (!NotificacionesAndroid.Disponible) return false;
            return CorrespondePedir(VecesPedidas, NotificacionesAndroid.Permiso, Preferidas, Desbloqueo.Misiones,
                                    Progreso.DiaUltimaRecompensa > 0, Desbloqueo.Todo, Progreso.RachaRecompensa);
        }
    }

    public static void AnotarPedido()
    {
        PlayerPrefs.SetInt(ClaveVecesPedidas, VecesPedidas + 1);
        PlayerPrefs.Save();
    }

    // Solo la APK de prueba (DiagnosticoAnuncios): la proxima vez que la app se vaya, una de la partida a los 10 s, para
    // ver que salen sin esperar un dia. Al irse, no antes: irse cancela todo lo programado.
    public static bool PruebaPendiente { get; private set; }

    public static void ProbarAlIrse()
    {
        PruebaPendiente = true;
    }

    private static void ProgramarLaPrueba()
    {
        if (!PruebaPendiente) return;
        PruebaPendiente = false;
        var aviso = new Aviso { tipo = TipoDeAviso.Partida, cuando = DateTime.Now.AddSeconds(10), numero = Math.Max(2, Progreso.OleadaEnCurso) };
        NotificacionesAndroid.Programar(Titulo(aviso), Texto(aviso), aviso.cuando);
    }
}
