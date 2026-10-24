using System;
using System.Collections.Generic;
using System.Globalization;

// Las notificaciones locales (1.7.1; las diseño Argon el 24/10, pensando en el jugador de Norteamerica, que "tiene
// tolerancia cero al spam"). Esto es solo la cuenta: con el estado del juego y la hora del telefono decide que avisos
// salen y cuando, sin tocar Android, asi la prueba de logica la recorre con un reloj inyectado. Las programa
// Notificaciones al irse la app y se cancelan todas al volver.
//
// Los avisos (ninguno pide mirar un anuncio: "destruye la percepcion de calidad"):
// - Partida: la oleada a medias se puede retomar. 4 h despues de irse.
// - Diaria: la recompensa del dia (desde la primera partida terminada). 24 h despues, a la hora en que jugo.
// - Vuelta: para el que todavia no la tiene, o el que no paso del nivel 5, sin partida a medias: a las 24 h y a las 72 h.
// - Racha: la racha de 2 dias o mas se corta a la medianoche del dia siguiente al ultimo cobro; avisa ese dia a las 20.
// - Semanal: el desafio de la semana (desde el nivel 5) sin cumplir, el sabado a las 19, con 18 h sin abrir el juego.
// - Evento: dos dias antes del final de un evento (Halloween) a las 18:30, si todavia no tiene el premio grande.
//
// Las reglas: nada entre las 22 y las 10 (pasa a las 12:30), uno por dia como mucho (con el de la partida aparte, que es
// a las 4 h), y no mas de tres por ausencia: el que ignora tres no recibe mas hasta que vuelva a abrir el juego.
public enum TipoDeAviso { Partida, Diaria, Vuelta, Racha, Semanal, Evento }

public class Aviso
{
    public TipoDeAviso tipo;
    public DateTime cuando;
    // La oleada de la partida o el dia de la racha que se cobraria; 0 si el texto no lleva numero.
    public int numero;

    public override string ToString()
    {
        return tipo + " " + cuando.ToString("ddd dd/MM HH:mm", CultureInfo.InvariantCulture) + (numero > 0 ? " (" + numero + ")" : "");
    }
}

// Lo que mira el plan. Lo arma Notificaciones.EstadoActual con el progreso; las pruebas, a mano.
public struct EstadoParaAvisar
{
    public DateTime ahora;          // la hora del telefono al irse la app (las alarmas de Android van con ese reloj)
    public bool hayPartida;         // Progreso.HayOleadaParaRetomar
    public int oleadaEnCurso;
    public bool diariaHabilitada;   // la recompensa diaria sale desde la primera partida terminada (PrimeraVez)
    public bool todo;               // Desbloqueo.Todo: el nivel 5, que destapa el desafio semanal
    public int diaUltimaDiaria;     // aaaammdd del ultimo cobro; 0 si nunca
    public int racha;               // la racha de ese cobro
    public bool semanalPendiente;   // el desafio armado y sin cumplir
    public DateTime finDelEvento;   // el ultimo dia de un evento andando sin el premio grande; default si no hay
}

public static class PlanDeNotificaciones
{
    public const int SilencioDesde = 22;
    public const int SilencioHasta = 10;
    public static readonly TimeSpan HoraDeRescate = new TimeSpan(12, 30, 0);

    public const double HorasPartida = 4;
    public const double HorasVuelta = 24;
    public const double HorasSegundaVuelta = 72;
    // La racha se corta a medianoche: 4 h antes.
    public static readonly TimeSpan HoraRacha = new TimeSpan(20, 0, 0);
    public const int RachaMinima = 2;
    public static readonly TimeSpan HoraSemanal = new TimeSpan(19, 0, 0);
    public const double HorasSinAbrirParaElSemanal = 18;
    public static readonly TimeSpan HoraEvento = new TimeSpan(18, 30, 0);
    public const int DiasAntesDelFin = 2;

    public const double HorasEntreAvisos = 24;
    public const int MaximoPorAusencia = 3;
    // Lo que sale antes de media hora no se programa: el que se va y vuelve enseguida no tiene por que enterarse.
    public const double MinutosMinimos = 30;
    public const int DiasDeHorizonte = 8;

    // Si cae entre las 22 y las 10, pasa a las 12:30: de noche, al otro dia; de madrugada, ese mismo.
    public static DateTime FueraDelSilencio(DateTime cuando)
    {
        if (cuando.Hour >= SilencioDesde) return cuando.Date.AddDays(1) + HoraDeRescate;
        if (cuando.Hour < SilencioHasta) return cuando.Date + HoraDeRescate;
        return cuando;
    }

    // Mas alto, gana cuando dos caen el mismo dia. La partida no compite: tiene su lugar aparte.
    public static int Prioridad(TipoDeAviso tipo)
    {
        switch (tipo)
        {
            case TipoDeAviso.Evento: return 5;
            case TipoDeAviso.Racha: return 4;
            case TipoDeAviso.Semanal: return 3;
            case TipoDeAviso.Diaria: return 2;
            default: return 1;
        }
    }

    public static List<Aviso> Planear(EstadoParaAvisar e)
    {
        var candidatos = new List<Aviso>();

        if (e.hayPartida && e.oleadaEnCurso > 1)
            Agregar(candidatos, TipoDeAviso.Partida, FueraDelSilencio(e.ahora.AddHours(HorasPartida)), e.oleadaEnCurso);

        // La vuelta del dia siguiente, a la hora en que jugo: la diaria si ya la tiene, si no un "volve".
        DateTime manana = FueraDelSilencio(e.ahora.AddHours(HorasVuelta));
        if (e.diariaHabilitada)
            Agregar(candidatos, TipoDeAviso.Diaria, manana,
                    RecompensaDiaria.RachaParaHoy(Dia(manana), e.diaUltimaDiaria, e.racha));
        else
            Agregar(candidatos, TipoDeAviso.Vuelta, manana, 0);
        if (!e.todo && !e.hayPartida)
            Agregar(candidatos, TipoDeAviso.Vuelta, FueraDelSilencio(e.ahora.AddHours(HorasSegundaVuelta)), 0);

        // La racha: el dia siguiente al ultimo cobro es el ultimo para seguirla. Si ya paso, no hay nada que salvar
        // (lo descarta el filtro de la hora).
        if (e.diariaHabilitada && e.racha >= RachaMinima)
        {
            DateTime ultimo;
            if (Fecha(e.diaUltimaDiaria, out ultimo))
                Agregar(candidatos, TipoDeAviso.Racha, ultimo.AddDays(1) + HoraRacha, e.racha + 1);
        }

        if (e.semanalPendiente)
            Agregar(candidatos, TipoDeAviso.Semanal, ProximoSabado(e.ahora.AddHours(HorasSinAbrirParaElSemanal)), 0);

        if (e.finDelEvento != default(DateTime))
            Agregar(candidatos, TipoDeAviso.Evento, e.finDelEvento.Date.AddDays(-DiasAntesDelFin) + HoraEvento, 0);

        DateTime desde = e.ahora.AddMinutes(MinutosMinimos);
        DateTime hasta = e.ahora.AddDays(DiasDeHorizonte);
        candidatos.RemoveAll(a => a.cuando < desde || a.cuando > hasta);

        // Uno por dia: por prioridad, y el que cae a menos de 24 h de uno ya elegido se cae.
        var elegidos = new List<Aviso>();
        var otros = candidatos.FindAll(a => a.tipo != TipoDeAviso.Partida);
        otros.Sort((a, b) =>
        {
            int p = Prioridad(b.tipo).CompareTo(Prioridad(a.tipo));
            return p != 0 ? p : a.cuando.CompareTo(b.cuando);
        });
        foreach (var aviso in otros)
        {
            bool cerca = elegidos.Exists(x => Math.Abs((x.cuando - aviso.cuando).TotalHours) < HorasEntreAvisos);
            if (!cerca) elegidos.Add(aviso);
        }
        elegidos.AddRange(candidatos.FindAll(a => a.tipo == TipoDeAviso.Partida));
        elegidos.Sort((a, b) => a.cuando.CompareTo(b.cuando));
        if (elegidos.Count > MaximoPorAusencia) elegidos.RemoveRange(MaximoPorAusencia, elegidos.Count - MaximoPorAusencia);
        return elegidos;
    }

    // El primer sabado a las 19 desde un momento (ese mismo dia si todavia no son las 19).
    public static DateTime ProximoSabado(DateTime desde)
    {
        int dias = ((int)DayOfWeek.Saturday - (int)desde.DayOfWeek + 7) % 7;
        DateTime sabado = desde.Date.AddDays(dias) + HoraSemanal;
        return sabado < desde ? sabado.AddDays(7) : sabado;
    }

    public static int Dia(DateTime fecha)
    {
        return fecha.Year * 10000 + fecha.Month * 100 + fecha.Day;
    }

    public static bool Fecha(int aaaammdd, out DateTime fecha)
    {
        return DateTime.TryParseExact(aaaammdd.ToString(CultureInfo.InvariantCulture), "yyyyMMdd",
                                      CultureInfo.InvariantCulture, DateTimeStyles.None, out fecha);
    }

    private static void Agregar(List<Aviso> lista, TipoDeAviso tipo, DateTime cuando, int numero)
    {
        lista.Add(new Aviso { tipo = tipo, cuando = cuando, numero = numero });
    }
}
