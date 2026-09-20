using System;
using System.Collections.Generic;

// Tres misiones por dia (facil, media y dificil), pedido de Ivan: le ponen un objetivo a
// cada partida y dan monedas. Cambian a medianoche con el dia confiable
// (Progreso.DiaDeHoy, que no se adelanta moviendo el reloj), y el mismo dia salen
// siempre las mismas: se sortean con el dia de semilla.
//
// El avance sale de los contadores de por vida del progreso: al armar las misiones se
// anota cuanto marcaba cada uno, y el avance es la diferencia. La de oleadas cuenta
// **cuantas oleadas se completaron hoy** (WaveManager avisa con RegistrarOleada): antes
// miraba el numero de la oleada mas alta del dia, y retomar una partida guardada en la 25
// cumplia de una las de "llega a la 15" y "llega a la 24".
//
// Lo cumplido que no se cobro **no se pierde a medianoche**: al cambiar el dia se cobra
// solo (CerrarElDia) antes de armar las nuevas, cofre incluido.
// Los objetivos se ajustan a la mejor oleada del jugador, para que no sean ni regalados
// ni imposibles, y las misiones de la furia, la granada y los criticos solo salen si
// estan comprados.
//
// Cobradas las tres, se abre el cofre del dia (CobrarCofre), uno por dia: empuja a jugar
// la segunda y la tercera partida.
//
// El premio **sale de lo que cuesta el objetivo**, no de un monto fijo: cada dificultad
// vale unas partidas (PartidasPorDificultad) y el premio es una fraccion de lo que dan
// esas partidas. Con montos fijos (150/300/600 y 800 el cofre, mas un 10 % por oleada) el
// primer dia regalaban ~1.850 monedas contra ~300 de jugar, y el jugador nuevo se saltaba
// la parte de arrancar flojo, que es el juego; mas adelante, al reves, no se notaban. Asi
// el premio siempre es un extra de la mitad de lo que ya ganaste cumpliendolo. Se paga
// con la **mejor oleada del dia en que se armaron** (mejorOleadaAlArmar) y no con la de
// ahora: el objetivo tambien quedo dimensionado con esa, y si no, guardar las misiones
// sin cobrar hasta mejorar la marca era la jugada optima -y a medianoche se cobraban
// solas al precio mas alto del dia-.
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

    // Que parte de lo que dan esas partidas se paga de premio: la facil paga menos
    // porque tambien cuesta menos. Mas de 1 seria cobrar dos veces lo mismo.
    public static readonly double[] FraccionPorDificultad = { 0.4, 0.5, 0.6 };
    public const double FraccionCofre = 0.5;    // de las tres juntas: es la segunda vuelta

    // Lo que avanza cada partida de la mejor oleada: los zombis que se matan llegando
    // a la oleada m (10 + 4n por oleada), por dificultad.
    private static readonly double[] PartidasPorDificultad = { 0.4, 1.0, 2.5 };

    private static List<MisionDelDia> Lista
    {
        get { Asegurar(); return Progreso.Misiones.lista; }
    }

    // La mejor oleada con que se armaron las de hoy: es la que paga, para que el premio
    // no dependa de cuando se cobra.
    public static int OleadaDeHoy
    {
        get { Asegurar(); return Math.Max(0, Progreso.Misiones.mejorOleadaAlArmar); }
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
        if (estado.dia != 0 && !Progreso.EsDiaNuevo(estado.dia, hoy) && estado.lista.Count == Cantidad)
        {
            // Un progreso guardado antes de que el premio se congelara no trae la marca: se
            // completa una sola vez. El centinela es -1 y no 0, que 0 es una marca valida
            // (nadie completo una oleada todavia) y volver a asignarla cada vez haria que
            // el premio siguiera a la mejor oleada, que es justo lo que se quiso sacar.
            if (estado.mejorOleadaAlArmar < 0) estado.mejorOleadaAlArmar = Progreso.MejorOleada;
            return;
        }

        if (estado.dia != 0) CerrarElDia(estado);

        estado.dia = hoy;
        estado.oleadasDelDia = 0;
        estado.cofreCobrado = false;
        estado.mejorOleadaAlArmar = Progreso.MejorOleada;
        estado.lista = Armar(hoy, Progreso.MejorOleada, Progreso.Nivel("furia") > 0,
                             Progreso.Nivel("granada") > 0, Progreso.Nivel("criticos") > 0);
        foreach (var mision in estado.lista) mision.inicio = Contador(mision.tipo);
        Progreso.AvisarCambio();
    }

    // Cobra lo que quedo cumplido y sin cobrar del dia que termina, cofre incluido: el
    // jugador ya hizo el trabajo, y perderlo por dormirse es de las cosas que hacen
    // desinstalar. No avisa en pantalla: las monedas aparecen en el contador.
    private static void CerrarElDia(EstadoMisiones estado)
    {
        int marca = Math.Max(0, estado.mejorOleadaAlArmar);
        int cobradas = 0;
        foreach (var mision in estado.lista)
        {
            if (mision.cobrada) { cobradas++; continue; }
            if (!Cumplida(mision)) continue;
            mision.cobrada = true;
            cobradas++;
            Progreso.CobrarPremio("mision_" + mision.tipo, Monto(mision.dificultad, marca), false);
        }
        if (cobradas >= Cantidad && !estado.cofreCobrado && estado.lista.Count == Cantidad)
        {
            estado.cofreCobrado = true;
            Progreso.CobrarPremio("mision_cofre", MontoCofre(marca), false);
        }
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
        double partidas = Partidas(dificultad);
        double zombisPorPartida = ZombisPorPartida(m);
        switch (tipo)
        {
            case Matar: return Redondo(partidas * zombisPorPartida);
            case Monedas: return Redondo(partidas * MonedasPorPartida(mejorOleada));
            // Cuantas oleadas completar hoy: las que da la partida que cuesta esa
            // dificultad. Sin Redondo, que son numeros chicos y redondear de a 5 se pasa.
            case Oleada: return Math.Max(2, Math.Round(partidas * m));
            // La furia sale cada 120 s y la granada cada 5: cuantas entran depende de lo
            // que dure la partida, que crece con la oleada. Con numeros fijos, el premio
            // -que si escala- se cobraba tirando 25 granadas parado en un rincon.
            case Furia: return Math.Max(2, Math.Round(partidas * Math.Max(2.0, m / 3.5)));
            case Granadas: return Math.Max(5, Redondo(partidas * m * 1.2));
            case Criticos: return Redondo(partidas * 60.0 * (1.0 + m / 10.0));
            // Un jefe cada 10 oleadas: los que entran en las partidas que cuesta.
            case Jefe: return Math.Max(1, Math.Round(partidas * m / 10.0));
            default: return 1;
        }
    }

    // Numeros redondos para leer de un vistazo: de a 5, de a 10 o de a 50.
    public static double Redondo(double x)
    {
        double paso = x < 100 ? 5 : x < 1000 ? 10 : 50;
        return Math.Max(paso, Math.Round(x / paso) * paso);
    }

    // Los zombis que se matan en una partida que llega a la oleada m: 10 + 4n por oleada.
    private static double ZombisPorPartida(int m)
    {
        return 10.0 * m + 2.0 * m * m;
    }

    // Lo que deja de monedas esa partida: cada zombi suelta unas 2 y el multiplicador de
    // la oleada (1,08 por oleada) se toma a mitad de camino. No cuenta el bono de cada
    // oleada ni el botin: es de menos a proposito, que el premio no se pase.
    public static double MonedasPorPartida(int mejorOleada)
    {
        int m = Math.Max(3, mejorOleada);
        return ZombisPorPartida(m) * 2.0 * Math.Pow(1.08, m * 0.5);
    }

    public static double Partidas(int dificultad)
    {
        return PartidasPorDificultad[Math.Max(0, Math.Min(dificultad, Cantidad - 1))];
    }

    // Una fraccion de lo que dan las partidas que cuesta cumplirla, en numeros redondos.
    public static double Monto(int dificultad, int mejorOleada)
    {
        int d = Math.Max(0, Math.Min(dificultad, Cantidad - 1));
        return Redondo(FraccionPorDificultad[d] * Partidas(d) * MonedasPorPartida(mejorOleada));
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
        if (mision.tipo == Oleada) return Progreso.Misiones.oleadasDelDia;
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
        double monto = Monto(mision.dificultad, OleadaDeHoy);
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

    // La mitad de las tres juntas: es lo que se lleva quien vuelve a jugar hasta cerrar
    // el dia, no un premio aparte.
    public static double MontoCofre(int mejorOleada)
    {
        double total = 0;
        for (int d = 0; d < Cantidad; d++) total += Monto(d, mejorOleada);
        return Redondo(FraccionCofre * total);
    }

    // Lo abre si estan las tres cobradas y no se abrio hoy; devuelve cuanto dio.
    public static double CobrarCofre()
    {
        if (!CofreDisponible) return 0;
        Progreso.Misiones.cofreCobrado = true;
        double monto = MontoCofre(OleadaDeHoy);
        Progreso.CobrarPremio("mision_cofre", monto, false);
        return monto;
    }

    // Desde WaveManager, al completar una oleada: cuenta una, sea la que sea. El numero
    // no importa a proposito (ver la cabecera): asi retomar una partida avanzada no cumple
    // de un saque las misiones de las oleadas de abajo.
    public static void RegistrarOleada(int oleada)
    {
        Asegurar();
        Progreso.Misiones.oleadasDelDia++;
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
    public int oleadasDelDia;       // cuantas oleadas se completaron hoy
    public int mejorOleadaAlArmar = -1;   // la mejor oleada del dia en que se armaron: es la que paga (-1: sin marca)
    public bool cofreCobrado;       // el cofre de las tres, uno por dia
    public List<MisionDelDia> lista = new List<MisionDelDia>();
}
