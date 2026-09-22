using System;
using System.Globalization;

// Monedas por entrar al juego una vez por dia, que crecen si se entra varios dias
// seguidos. Pedido de Ivan para que los jugadores vuelvan cada dia: es de lo que mas
// sube la retencion en los incrementales.
//
// - La racha sube si el ultimo cobro fue AYER; si se salto un dia, vuelve a 1.
// - Del dia 7 en adelante se cobra lo del dia 7 mientras no se corte la racha.
// - El monto crece con la mejor oleada: paga las partidas que dice PartidasPorDia, con
//   la vara de Economia, igual que las misiones y el bestiario. Antes era un monto fijo
//   con un +10 % por oleada, y en las oleadas altas quedaba en calderilla al lado de lo
//   que da jugar, justo cuando mas hace falta la razon para volver. Los montos de
//   MonedasPorDia quedaron como **piso**: en el arranque, donde 150 monedas son la
//   primera mejora, la diaria paga lo mismo de siempre.
// - Atrasar el reloj del telefono no da otra recompensa: solo cuenta un dia mayor al
//   guardado (Progreso.EsDiaNuevo), igual que los topes de los anuncios. Adelantarlo
//   tampoco, mientras no se reinicie el telefono (RelojConfiable).
// - Entra por Progreso.CobrarPremio: no son monedas ganadas jugando.
// - Despues de cobrar se puede mirar un video (LugarAnuncio.DuplicarRegalo) para cobrar
//   lo mismo otra vez (CobrarDuplicado), una sola vez por cobro.
//
// Lo que se puede probar sin escena es estatico y recibe el dia (RachaParaHoy, Monto).
public static class RecompensaDiaria
{
    // El piso de cada dia de racha, que es lo que se paga en el arranque.
    public static readonly int[] MonedasPorDia = { 150, 250, 400, 600, 900, 1300, 2000 };   // Ivan: "que sean mas monedas"

    // Y lo que paga cada dia medido en partidas, que es lo que manda en cuanto el jugador
    // avanza: del cuarto de partida del primer dia a las dos del septimo.
    public static readonly double[] PartidasPorDia = { 0.25, 0.35, 0.5, 0.7, 1.0, 1.4, 2.0 };

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
        int casillero = Casillero(racha);
        double piso = MonedasPorDia[casillero - 1];
        double porLoQueDaJugar = PartidasPorDia[casillero - 1] * Economia.MonedasPorPartida(mejorOleada);
        return Economia.Redondo(Math.Max(piso, porLoQueDaJugar));
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
    // Lo ultimo que se cobro y todavia se puede duplicar con un video. En memoria: si se
    // cierra el juego antes de mirar el video, la oferta se pierde, y esta bien.
    private static double paraDuplicar;

    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reiniciar()
    {
        paraDuplicar = 0;
    }

    public static double ParaDuplicar
    {
        get { return paraDuplicar; }
    }

    public static double Cobrar()
    {
        return CobrarEl(Progreso.DiaDeHoy());
    }

    // La mejor oleada con la que se paga hoy: congelada la primera vez que la recompensa
    // queda disponible, no la de ahora. Si no, la jugada optima era cerrar la ventana sin
    // cobrar (el atras de Android lo hace), ir a mejorar la marca y volver al menu: de la
    // oleada 20 a la 25 el premio pasaba de 8.650 a 15.700. Es la misma regla que ya tienen
    // las misiones y el desafio semanal.
    public static int OleadaDeHoy
    {
        get { return Progreso.OleadaDeLaRecompensa(Progreso.DiaDeHoy()); }
    }

    public static double CobrarEl(int hoy)
    {
        int racha = RachaParaHoy(hoy, Progreso.DiaUltimaRecompensa, Progreso.RachaRecompensa);
        if (racha <= 0) return 0;
        double monto = Monto(racha, Progreso.OleadaDeLaRecompensa(hoy));
        Progreso.RegistrarRecompensaDiaria(hoy, racha);
        Progreso.CobrarPremio("recompensa_diaria", monto, false);
        paraDuplicar = monto;
        return monto;
    }

    // El premio del video: lo mismo que se acaba de cobrar, una sola vez. Devuelve cuanto dio.
    public static double CobrarDuplicado()
    {
        double monto = paraDuplicar;
        if (!(monto > 0)) return 0;
        paraDuplicar = 0;
        Progreso.CobrarPremio("recompensa_diaria_video", monto, false);
        return monto;
    }
}
