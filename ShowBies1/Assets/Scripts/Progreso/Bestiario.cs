using System;

// El bestiario: una tarjeta por tipo de zombi con tres estrellas, que se ganan matando
// 100, 1.000 y 10.000 de ese tipo (el jefe: 1, 10 y 50). Cada estrella se cobra una vez
// en el menu (VentanaBestiario). Llenar colecciones funciona sin fecha: explica para
// que sirve seguir jugando cuando ya no hay misiones.
//
// **El premio sale de lo que dejan esas muertes**, como el de las misiones: la mitad de
// lo que sueltan los zombis del escalon, con el multiplicador de la oleada. Eran 200,
// 1.000 y 5.000 fijos mas un 10 % por oleada, y pagaban lo mismo por 100 caminantes que
// por 100 tanques, que cuestan tres veces mas; de paso, las primeras estrellas eran un
// regalo temprano y las ultimas, calderilla.
//
// Las muertes salen de los contadores de por vida (Progreso.Matados, por el nombre del
// asset Enemy, que por eso no se renombra) y lo cobrado se guarda en el progreso. El
// premio entra por CobrarPremio: no cuenta como monedas ganadas jugando.
public static class Bestiario
{
    public const string Normal = "ZombiNormal";
    public const string Rapido = "ZombiRapido";
    public const string Faster = "ZombiFASTER";
    public const string Tanque = "ZombiTanque";
    public const string Jefe = "ZombiBOSS";

    public static readonly string[] Tipos = { Normal, Rapido, Faster, Tanque, Jefe };
    public const int Estrellas = 3;

    private static readonly int[] EscalonesComunes = { 100, 1000, 10000 };
    private static readonly int[] EscalonesJefe = { 1, 10, 50 };

    // Lo que suelta cada tipo al morir, el promedio de monedasMin y monedasMax de su
    // asset Enemy. No se lee del asset para poder probar esto sin escena: si cambia el
    // balance de un zombi, se cambia aca. En el orden de Tipos.
    private static readonly double[] MonedasPorTipo = { 2, 2, 3, 6.5, 35 };

    // El jefe suelta 35 monedas pero cuesta 500 balas: sin esto su primera estrella
    // pagaria menos que la de un caminante.
    private const double EsfuerzoDelJefe = 6.0;

    // Que parte de lo que dejan esas muertes se paga de premio.
    public const double FraccionDelEscalon = 0.5;

    public static int[] Escalones(string tipo)
    {
        return tipo == Jefe ? EscalonesJefe : EscalonesComunes;
    }

    // Cuantas estrellas gano ese tipo con estas muertes. Estatico para probarlo.
    public static int AlcanzadasCon(string tipo, int muertes)
    {
        int n = 0;
        foreach (int escalon in Escalones(tipo)) if (muertes >= escalon) n++;
        return n;
    }

    public static int Alcanzadas(string tipo)
    {
        return AlcanzadasCon(tipo, Progreso.Matados(tipo));
    }

    public static int Cobradas(string tipo)
    {
        return Math.Min(Progreso.EstrellasCobradas(tipo), Estrellas);
    }

    public static int PorCobrar
    {
        get
        {
            int n = 0;
            foreach (string tipo in Tipos) n += Math.Max(0, Alcanzadas(tipo) - Cobradas(tipo));
            return n;
        }
    }

    // El siguiente escalon sin alcanzar, o 0 si ya estan las tres.
    public static int Siguiente(string tipo)
    {
        int muertes = Progreso.Matados(tipo);
        foreach (int escalon in Escalones(tipo)) if (muertes < escalon) return escalon;
        return 0;
    }

    // Lo que deja un zombi de ese tipo a esa altura del juego: lo que suelta por el
    // multiplicador de monedas de la oleada, tomado a mitad de camino, igual que en las
    // misiones.
    public static double MonedasQueDeja(string tipo, int mejorOleada)
    {
        int i = Array.IndexOf(Tipos, tipo);
        double monedas = i >= 0 ? MonedasPorTipo[i] : 2.0;
        if (tipo == Jefe) monedas *= EsfuerzoDelJefe;
        return monedas * Math.Pow(1.08, Math.Max(3, mejorOleada) * 0.5);
    }

    // La mitad de lo que dejan las muertes del escalon, en numeros redondos.
    public static double Premio(string tipo, int estrella, int mejorOleada)
    {
        var escalones = Escalones(tipo);
        int e = Math.Max(0, Math.Min(estrella, escalones.Length - 1));
        return Economia.Redondo(FraccionDelEscalon * escalones[e] * MonedasQueDeja(tipo, mejorOleada));
    }

    // Cobra la siguiente estrella ganada de ese tipo; devuelve cuanto dio (0 si no hay).
    public static double Cobrar(string tipo)
    {
        int cobradas = Cobradas(tipo);
        if (cobradas >= Alcanzadas(tipo)) return 0;
        double monto = Premio(tipo, cobradas, Progreso.MejorOleada);
        Progreso.SumarEstrellaCobrada(tipo);
        Progreso.CobrarPremio("bestiario_" + tipo, monto, false);
        return monto;
    }

    public static string Nombre(string tipo)
    {
        switch (tipo)
        {
            case Normal: return Textos.De("zombi_normal");
            case Rapido: return Textos.De("zombi_rapido");
            case Faster: return Textos.De("zombi_faster");
            case Tanque: return Textos.De("zombi_tanque");
            case Jefe: return Textos.De("zombi_jefe");
            default: return tipo;
        }
    }
}
