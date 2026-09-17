using System;
using System.Globalization;

// Monedas por entrar al juego una vez por dia, que crecen si se entra varios dias
// seguidos. Pedido de Ivan para que los jugadores vuelvan cada dia: es de lo que mas
// sube la retencion en los incrementales.
//
// - La racha sube si el ultimo cobro fue AYER; si se salto un dia, vuelve a 1.
// - Del dia 7 en adelante se cobra lo del dia 7 mientras no se corte la racha.
// - El monto crece con la mejor oleada, para que no quede chico cuando las mejoras
//   ya cuestan miles.
// - Atrasar el reloj del telefono no da otra recompensa: solo cuenta un dia mayor al
//   guardado (Progreso.EsDiaNuevo), igual que los topes de los anuncios.
// - Entra por Progreso.CobrarPremio: no son monedas ganadas jugando.
//
// Lo que se puede probar sin escena es estatico y recibe el dia (RachaParaHoy, Monto).
public static class RecompensaDiaria
{
    public static readonly int[] MonedasPorDia = { 50, 75, 100, 150, 200, 300, 500 };
    public const double CrecimientoPorOleada = 0.1;   // +10 % por cada oleada de la mejor marca

    public static int DiasDelCiclo => MonedasPorDia.Length;

    // Que dia de racha se cobraria hoy (1 en adelante), o 0 si hoy ya no hay.
    public static int RachaParaHoy(int hoy, int ultimoDia, int racha)
    {
        if (ultimoDia <= 0) return 1;
        if (!Progreso.EsDiaNuevo(ultimoDia, hoy)) return 0;
        return DiaSiguiente(ultimoDia) == hoy ? Math.Max(0, racha) + 1 : 1;
    }

    // aaaammdd del dia siguiente; 0 si la fecha guardada no es valida.
    public static int DiaSiguiente(int aaaammdd)
    {
        DateTime fecha;
        if (!DateTime.TryParseExact(aaaammdd.ToString(CultureInfo.InvariantCulture), "yyyyMMdd",
                                    CultureInfo.InvariantCulture, DateTimeStyles.None, out fecha)) return 0;
        fecha = fecha.AddDays(1);
        return fecha.Year * 10000 + fecha.Month * 100 + fecha.Day;
    }

    // En que casillero del ciclo cae una racha (1 a 7): del 7 en adelante se queda en 7.
    public static int Casillero(int racha)
    {
        return Math.Max(1, Math.Min(racha, DiasDelCiclo));
    }

    public static double Monto(int racha, int mejorOleada)
    {
        double base_ = MonedasPorDia[Casillero(racha) - 1];
        return Math.Floor(base_ * (1.0 + CrecimientoPorOleada * Math.Max(0, mejorOleada)) + 0.5 + 1e-9);
    }

    public static int RachaDeHoy
    {
        get { return RachaParaHoy(Progreso.DiaDeHoy(), Progreso.DiaUltimaRecompensa, Progreso.RachaRecompensa); }
    }

    public static bool Disponible
    {
        get { return RachaDeHoy > 0; }
    }

    // Cobra la de hoy y devuelve cuanto dio (0 si ya se habia cobrado).
    public static double Cobrar()
    {
        return CobrarEl(Progreso.DiaDeHoy());
    }

    public static double CobrarEl(int hoy)
    {
        int racha = RachaParaHoy(hoy, Progreso.DiaUltimaRecompensa, Progreso.RachaRecompensa);
        if (racha <= 0) return 0;
        double monto = Monto(racha, Progreso.MejorOleada);
        Progreso.RegistrarRecompensaDiaria(hoy, racha);
        Progreso.CobrarPremio("recompensa_diaria", monto, false);
        return monto;
    }
}
