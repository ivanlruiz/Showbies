using System;
using System.Globalization;

// El desafio de la semana, pedido de Ivan: un objetivo grande, uno solo, que dura de
// lunes a domingo y paga bastante mas que una mision del dia. Las diarias dan una razon
// para entrar hoy; esta da una para volver toda la semana, que es otra escala de tiempo.
//
// Cuesta unas quince partidas (PartidasQueCuesta) y paga la mitad de lo que dan esas
// quince, con la vara de Economia, como todo lo demas. El premio y el objetivo se fijan
// con la mejor oleada del lunes en que se armo: si no, guardarlo sin cobrar hasta mejorar
// la marca seria la jugada optima, igual que pasaba con las misiones.
//
// El avance sale de los contadores de por vida del progreso, como las misiones: al armarlo
// se anota cuanto marcaba el contador y el avance es la diferencia. Las oleadas las cuenta
// aparte (oleadasDeLaSemana), porque no hay un contador de por vida de oleadas.
//
// Al cambiar de semana, lo cumplido sin cobrar se cobra solo antes de armar el nuevo,
// como el cierre del dia de las misiones.
public static class DesafioSemanal
{
    // Los tipos se guardan en el JSON: no se renombran.
    public const string Matar = "matar";
    public const string Oleadas = "oleadas";
    public const string Monedas = "monedas";
    public const string Jefes = "jefes";

    private static readonly string[] Tipos = { Matar, Oleadas, Monedas, Jefes };

    // Lo que cuesta cumplirlo, medido en partidas de la mejor oleada: una semana de jugar
    // un rato por dia.
    public const double PartidasQueCuesta = 15.0;
    public const double FraccionDelPremio = 0.5;

    public static EstadoSemanal Estado
    {
        get { Asegurar(); return Progreso.Semanal; }
    }

    // El lunes de la semana de ese dia, en aaaammdd. 0 si la fecha no es valida.
    public static int LunesDe(int aaaammdd)
    {
        DateTime fecha;
        if (!DateTime.TryParseExact(aaaammdd.ToString(CultureInfo.InvariantCulture), "yyyyMMdd",
                                    CultureInfo.InvariantCulture, DateTimeStyles.None, out fecha)) return 0;
        int desdeElLunes = ((int)fecha.DayOfWeek + 6) % 7;   // domingo es 0 en DayOfWeek
        fecha = fecha.AddDays(-desdeElLunes);
        return fecha.Year * 10000 + fecha.Month * 100 + fecha.Day;
    }

    // Arma el de la semana si cambio la semana. Un reloj atrasado no lo cambia: solo
    // cuenta un lunes mayor al guardado, como Progreso.EsDiaNuevo con los dias.
    public static void Asegurar()
    {
        var estado = Progreso.Semanal;
        int lunes = LunesDe(Progreso.DiaDeHoy());
        if (lunes <= 0) return;
        if (estado.lunes != 0 && lunes <= estado.lunes)
        {
            if (estado.mejorOleadaAlArmar < 0) estado.mejorOleadaAlArmar = Progreso.MejorOleada;
            return;
        }

        if (estado.lunes != 0) CerrarLaSemana(estado);

        int mejorOleada = Progreso.MejorOleada;
        estado.lunes = lunes;
        estado.tipo = Elegir(lunes, mejorOleada);
        estado.objetivo = Objetivo(estado.tipo, mejorOleada);
        estado.inicio = Contador(estado.tipo);
        estado.oleadasDeLaSemana = 0;
        estado.cobrado = false;
        estado.mejorOleadaAlArmar = mejorOleada;
        Progreso.AvisarCambio();
    }

    // Cobra lo que quedo cumplido y sin cobrar de la semana que termina: el jugador ya
    // hizo el trabajo.
    private static void CerrarLaSemana(EstadoSemanal estado)
    {
        if (estado.cobrado || !CumplidoCon(estado)) return;
        estado.cobrado = true;
        Progreso.CobrarPremio("semanal_" + estado.tipo, Monto(Math.Max(0, estado.mejorOleadaAlArmar)), false);
    }

    // El tipo de la semana, sorteado con el lunes de semilla: el mismo lunes siempre sale
    // el mismo. Los jefes solo con la oleada 9, que antes no sale ninguno.
    public static string Elegir(int lunes, int mejorOleada)
    {
        var azar = new Random(lunes);
        int cuantos = mejorOleada >= 9 ? Tipos.Length : Tipos.Length - 1;
        return Tipos[azar.Next(cuantos)];
    }

    public static double Objetivo(string tipo, int mejorOleada)
    {
        int m = Math.Max(3, mejorOleada);
        switch (tipo)
        {
            case Matar: return Economia.Redondo(PartidasQueCuesta * Economia.ZombisPorPartida(m));
            case Monedas: return Economia.Redondo(PartidasQueCuesta * Economia.MonedasPorPartida(m));
            case Oleadas: return Math.Max(10, Math.Round(PartidasQueCuesta * m));
            case Jefes: return Math.Max(2, Math.Round(PartidasQueCuesta * m / 10.0));
            default: return 1;
        }
    }

    // La mitad de lo que dan las quince partidas que cuesta.
    public static double Monto(int mejorOleada)
    {
        return Economia.Redondo(FraccionDelPremio * PartidasQueCuesta * Economia.MonedasPorPartida(mejorOleada));
    }

    public static double MontoDeEstaSemana
    {
        get { return Monto(Math.Max(0, Estado.mejorOleadaAlArmar)); }
    }

    private static double Contador(string tipo)
    {
        switch (tipo)
        {
            case Matar: return Progreso.MatadosEnTotal;
            case Monedas: return Progreso.MonedasGanadasJugando;
            case Jefes: return Progreso.JefesMatados;
            default: return 0;       // las oleadas se cuentan aparte
        }
    }

    public static double AvanceDe(EstadoSemanal estado)
    {
        if (estado == null || estado.lunes == 0) return 0;
        if (estado.tipo == Oleadas) return estado.oleadasDeLaSemana;
        return Math.Max(0, Contador(estado.tipo) - estado.inicio);
    }

    public static double Avance
    {
        get { return AvanceDe(Estado); }
    }

    public static bool CumplidoCon(EstadoSemanal estado)
    {
        return estado != null && estado.objetivo > 0 && AvanceDe(estado) >= estado.objetivo;
    }

    public static bool Cumplido
    {
        get { return CumplidoCon(Estado); }
    }

    public static bool PorCobrar
    {
        get { return Cumplido && !Estado.cobrado; }
    }

    // Lo cobra si esta cumplido y no se cobro; devuelve cuanto dio.
    public static double Cobrar()
    {
        var estado = Estado;
        if (estado.cobrado || !CumplidoCon(estado)) return 0;
        estado.cobrado = true;
        double monto = MontoDeEstaSemana;
        Progreso.CobrarPremio("semanal_" + estado.tipo, monto, false);
        return monto;
    }

    // Desde WaveManager, al completar una oleada, como con las misiones.
    public static void RegistrarOleada()
    {
        Asegurar();
        Progreso.Semanal.oleadasDeLaSemana++;
    }

    public static string Descripcion(EstadoSemanal estado)
    {
        if (estado == null) return "";
        string n = FormatoNumeros.Compacto(estado.objetivo);
        switch (estado.tipo)
        {
            case Matar: return Textos.Formato("semanal_matar", n);
            case Oleadas: return Textos.Formato("semanal_oleadas", n);
            case Monedas: return Textos.Formato("semanal_monedas", n);
            case Jefes: return Textos.Formato("semanal_jefes", n);
            default: return estado.tipo;
        }
    }

    // Cuantos dias faltan para el lunes que viene, para el "TERMINA EN N DIAS".
    public static int DiasQueFaltan()
    {
        DateTime ahora = Progreso.AhoraConfiable();
        int desdeElLunes = ((int)ahora.DayOfWeek + 6) % 7;
        return 7 - desdeElLunes;
    }
}

// Lo que se guarda del desafio en el JSON del progreso.
[Serializable]
public class EstadoSemanal
{
    public int lunes;                    // aaaammdd del lunes de la semana; 0 = ninguno
    public string tipo = "";
    public double objetivo;
    public double inicio;                // lo que marcaba su contador al armarlo
    public int oleadasDeLaSemana;        // para el de oleadas, que no tiene contador propio
    public bool cobrado;
    public int mejorOleadaAlArmar = -1;  // la marca con que se fijaron objetivo y premio
}
