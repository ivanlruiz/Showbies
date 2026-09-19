using System;
using System.Collections.Generic;

// Tres misiones por dia (facil, media y dificil), pedido de Ivan: le ponen un objetivo a
// cada partida y dan monedas. Cambian a medianoche con el dia confiable
// (Progreso.DiaDeHoy, que no se adelanta moviendo el reloj), y el mismo dia salen
// siempre las mismas: se sortean con el dia de semilla.
//
// El avance sale de los contadores de por vida del progreso: al armar las misiones se
// anota cuanto marcaba cada uno, y el avance es la diferencia. "Completa la oleada N"
// mira la mejor oleada completada en el dia (WaveManager avisa con RegistrarOleada).
// Los objetivos se ajustan a la mejor oleada del jugador, para que no sean ni regalados
// ni imposibles, y las misiones de la furia, la granada y los criticos solo salen si
// estan comprados.
//
// Cobradas las tres, se abre el cofre del dia (CobrarCofre), uno por dia: empuja a jugar
// la segunda y la tercera partida.
//
// El premio entra por Progreso.CobrarPremio: no cuenta como monedas ganadas jugando.
// Lo que se puede probar sin escena es estatico y recibe lo que necesita (Armar, Monto).
public static class MisionesDiarias
{
    // Los tipos se guardan con estos nombres en el JSON: no se renombran.
    public const string Matar = "matar";
    public const string Oleada = "oleada";
    public const string Monedas = "monedas";
    public const string Furia = "furia";
    public const string Granadas = "granadas";
    public const string Criticos = "criticos";
    public const string Jefe = "jefe";

    public const int Cantidad = 3;
    public static readonly double[] PremioBase = { 150, 300, 600 };   // facil, media, dificil
    public const double CrecimientoPorOleada = 0.1;                   // como la diaria
    public const double PremioCofre = 800;

    // Lo que avanza cada partida de la mejor oleada: los zombis que se matan llegando
    // a la oleada m (10 + 4n por oleada), por dificultad.
    private static readonly double[] PartidasPorDificultad = { 0.4, 1.0, 2.5 };

    private static List<MisionDelDia> Lista
    {
        get { Asegurar(); return Progreso.Misiones.lista; }
    }

    public static IReadOnlyList<MisionDelDia> DeHoy
    {
        get { return Lista; }
    }

    // Arma las del dia si cambio el dia. Un reloj atrasado no las cambia (EsDiaNuevo).
    public static void Asegurar()
    {
        var estado = Progreso.Misiones;
        int hoy = Progreso.DiaDeHoy();
        if (estado.dia != 0 && !Progreso.EsDiaNuevo(estado.dia, hoy) && estado.lista.Count == Cantidad) return;

        estado.dia = hoy;
        estado.mejorOleadaDelDia = 0;
        estado.cofreCobrado = false;
        estado.lista = Armar(hoy, Progreso.MejorOleada, Progreso.Nivel("furia") > 0,
                             Progreso.Nivel("granada") > 0, Progreso.Nivel("criticos") > 0);
        foreach (var mision in estado.lista) mision.inicio = Contador(mision.tipo);
        Progreso.AvisarCambio();
    }

    // Las tres del dia, sin contadores: un tipo distinto por dificultad, sorteados con
    // el dia de semilla.
    public static List<MisionDelDia> Armar(int dia, int mejorOleada, bool conFuria, bool conGranada, bool conCriticos)
    {
        var azar = new Random(dia);
        var usados = new List<string>();
        var lista = new List<MisionDelDia>(Cantidad);
        for (int dificultad = 0; dificultad < Cantidad; dificultad++)
        {
            var posibles = new List<string> { Matar, Oleada, Monedas };
            if (conFuria) posibles.Add(Furia);
            if (conGranada) posibles.Add(Granadas);
            if (conCriticos) posibles.Add(Criticos);
            if (dificultad == 2 && mejorOleada >= 9) posibles.Add(Jefe);
            posibles.RemoveAll(usados.Contains);

            string tipo = posibles[azar.Next(posibles.Count)];
            usados.Add(tipo);
            lista.Add(new MisionDelDia { tipo = tipo, dificultad = dificultad, objetivo = Objetivo(tipo, dificultad, mejorOleada) });
        }
        return lista;
    }

    public static double Objetivo(string tipo, int dificultad, int mejorOleada)
    {
        int m = Math.Max(3, mejorOleada);
        double partidas = PartidasPorDificultad[Math.Max(0, Math.Min(dificultad, 2))];
        double zombisPorPartida = 10.0 * m + 2.0 * m * m;
        switch (tipo)
        {
            case Matar: return Redondo(partidas * zombisPorPartida);
            case Monedas: return Redondo(partidas * zombisPorPartida * 2.0 * Math.Pow(1.08, m * 0.5));
            case Oleada: return Math.Max(2, Math.Round(m * (dificultad == 0 ? 0.5 : dificultad == 1 ? 0.8 : 1.0)));
            case Furia: return dificultad == 0 ? 2 : dificultad == 1 ? 3 : 5;
            case Granadas: return dificultad == 0 ? 5 : dificultad == 1 ? 12 : 25;
            case Criticos: return Redondo(partidas * 60.0 * (1.0 + m / 10.0));
            case Jefe: return 1;
            default: return 1;
        }
    }

    // Numeros redondos para leer de un vistazo: de a 5, de a 10 o de a 50.
    public static double Redondo(double x)
    {
        double paso = x < 100 ? 5 : x < 1000 ? 10 : 50;
        return Math.Max(paso, Math.Round(x / paso) * paso);
    }

    public static double Monto(int dificultad, int mejorOleada)
    {
        double base_ = PremioBase[Math.Max(0, Math.Min(dificultad, PremioBase.Length - 1))];
        return Math.Floor(base_ * (1.0 + CrecimientoPorOleada * Math.Max(0, mejorOleada)) + 0.5 + 1e-9);
    }

    // Cuanto marca hoy el contador de un tipo (Oleada no usa contador).
    private static double Contador(string tipo)
    {
        switch (tipo)
        {
            case Matar: return Progreso.MatadosEnTotal;
            case Monedas: return Progreso.MonedasGanadasJugando;
            case Furia: return Progreso.FuriasActivadas;
            case Granadas: return Progreso.GranadasTiradas;
            case Criticos: return Progreso.Criticos;
            case Jefe: return Progreso.JefesMatados;
            default: return 0;
        }
    }

    public static double Avance(MisionDelDia mision)
    {
        if (mision == null) return 0;
        if (mision.tipo == Oleada) return Progreso.Misiones.mejorOleadaDelDia;
        return Math.Max(0, Contador(mision.tipo) - mision.inicio);
    }

    public static bool Cumplida(MisionDelDia mision)
    {
        return mision != null && Avance(mision) >= mision.objetivo;
    }

    public static int PorCobrar
    {
        get
        {
            int n = 0;
            foreach (var mision in Lista) if (Cumplida(mision) && !mision.cobrada) n++;
            if (CofreDisponible) n++;
            return n;
        }
    }

    // La cobra si esta cumplida y no se cobro; devuelve cuanto dio. Guarda en el acto.
    public static double Cobrar(int indice)
    {
        var lista = Lista;
        if (indice < 0 || indice >= lista.Count) return 0;
        var mision = lista[indice];
        if (mision.cobrada || !Cumplida(mision)) return 0;

        mision.cobrada = true;
        double monto = Monto(mision.dificultad, Progreso.MejorOleada);
        Progreso.CobrarPremio("mision_" + mision.tipo, monto, false);
        return monto;
    }

    // Cuantas de las del dia estan cobradas: el cofre se abre con las tres.
    public static int Cobradas
    {
        get
        {
            int n = 0;
            foreach (var mision in Lista) if (mision.cobrada) n++;
            return n;
        }
    }

    public static bool CofreDisponible
    {
        get { return Lista.Count == Cantidad && Cobradas >= Cantidad && !Progreso.Misiones.cofreCobrado; }
    }

    public static bool CofreCobrado
    {
        get { Asegurar(); return Progreso.Misiones.cofreCobrado; }
    }

    public static double MontoCofre(int mejorOleada)
    {
        return Math.Floor(PremioCofre * (1.0 + CrecimientoPorOleada * Math.Max(0, mejorOleada)) + 0.5 + 1e-9);
    }

    // Lo abre si estan las tres cobradas y no se abrio hoy; devuelve cuanto dio.
    public static double CobrarCofre()
    {
        if (!CofreDisponible) return 0;
        Progreso.Misiones.cofreCobrado = true;
        double monto = MontoCofre(Progreso.MejorOleada);
        Progreso.CobrarPremio("mision_cofre", monto, false);
        return monto;
    }

    // Desde WaveManager, al completar una oleada.
    public static void RegistrarOleada(int oleada)
    {
        Asegurar();
        var estado = Progreso.Misiones;
        if (oleada > estado.mejorOleadaDelDia) estado.mejorOleadaDelDia = oleada;
    }

    public static string Descripcion(MisionDelDia mision)
    {
        string n = FormatoNumeros.Compacto(mision.objetivo);
        switch (mision.tipo)
        {
            case Matar: return Textos.Formato("mision_matar", n);
            case Oleada: return Textos.Formato("mision_oleada", n);
            case Monedas: return Textos.Formato("mision_monedas", n);
            case Furia: return Textos.Formato("mision_furia", n);
            case Granadas: return Textos.Formato("mision_granadas", n);
            case Criticos: return Textos.Formato("mision_criticos", n);
            case Jefe: return Textos.De("mision_jefe");
            default: return mision.tipo;
        }
    }
}

// Lo que se guarda de cada mision en el JSON del progreso.
[Serializable]
public class MisionDelDia
{
    public string tipo;
    public int dificultad;       // 0 facil, 1 media, 2 dificil
    public double objetivo;
    public double inicio;        // lo que marcaba su contador al armarla
    public bool cobrada;
}

[Serializable]
public class EstadoMisiones
{
    public int dia;                 // aaaammdd de las misiones guardadas; 0 = ninguna
    public int mejorOleadaDelDia;   // para "completa la oleada N"
    public bool cofreCobrado;       // el cofre de las tres, uno por dia
    public List<MisionDelDia> lista = new List<MisionDelDia>();
}
