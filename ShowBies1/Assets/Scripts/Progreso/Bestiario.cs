using System;

// El bestiario: una tarjeta por tipo de zombi con tres estrellas, que se ganan matando
// 100, 1.000 y 10.000 de ese tipo (el jefe: 1, 10 y 50). Cada estrella se cobra una vez
// en el menu (VentanaBestiario) y paga 200, 1.000 y 5.000 monedas, que crecen con la
// mejor oleada como todo lo demas. Llenar colecciones funciona sin fecha: explica para
// que sirve seguir jugando cuando ya no hay misiones.
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
    public static readonly double[] Premios = { 200, 1000, 5000 };
    public const double CrecimientoPorOleada = 0.1;

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

    public static double Premio(int estrella, int mejorOleada)
    {
        double base_ = Premios[Math.Max(0, Math.Min(estrella, Premios.Length - 1))];
        return Math.Floor(base_ * (1.0 + CrecimientoPorOleada * Math.Max(0, mejorOleada)) + 0.5 + 1e-9);
    }

    // Cobra la siguiente estrella ganada de ese tipo; devuelve cuanto dio (0 si no hay).
    public static double Cobrar(string tipo)
    {
        int cobradas = Cobradas(tipo);
        if (cobradas >= Alcanzadas(tipo)) return 0;
        double monto = Premio(cobradas, Progreso.MejorOleada);
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
