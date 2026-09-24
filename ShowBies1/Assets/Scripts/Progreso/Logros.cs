using System;
using System.Collections.Generic;

// Los logros, pedido de Ivan: doce familias con tres monedas cada una (bronce, plata y
// oro) que se ganan llegando a una marca, como completar la oleada 10, 25 y 50 o hacer un
// combo x25, x50 y x100. Se ganan jugando (el critico, comprando) y se cobran en el menu
// (VentanaLogros), y lo que dan es experiencia para el nivel del jugador (NivelJugador).
// La de bronce vale lo que cuesta un nivel; la de plata, dos, y la de oro, cuatro. Del
// nivel en que se gano y no del de cuando se cobra: guardarla para cobrarla mas arriba
// no rinde, que es la regla de todos los premios del juego.
//
// Una moneda ganada queda anotada para siempre en el progreso, aunque despues la marca
// baje: el renacer va a devolver las mejoras a cero, y el critico al 100 % con ellas. Se
// anota al verla (Revisar), que en la partida pasa cada tres decimas (AvisoDeMisiones) y
// en el menu al abrir la ventana.
//
// Los ids de las familias se guardan en el JSON: no se renombran.
public static class Logros
{
    public const string Superviviente = "superviviente";
    public const string Exterminador = "exterminador";
    public const string Imparable = "imparable";
    public const string Critico = "critico";
    public const string Millonario = "millonario";
    public const string Granadero = "granadero";
    public const string Furioso = "furioso";
    public const string Intocable = "intocable";
    public const string Constante = "constante";
    public const string Misionero = "misionero";
    public const string Coleccionista = "coleccionista";
    public const string SinFin = "sin_fin";

    public const int Escalones = 3;

    // Cuantos niveles de experiencia vale cada escalon, del nivel en que se gano.
    public static readonly int[] NivelesPorEscalon = { 1, 2, 4 };

    public class Familia
    {
        public readonly string id;
        public readonly double[] metas;
        private readonly Func<double> valor;

        public Familia(string id, Func<double> valor, params double[] metas)
        {
            this.id = id;
            this.valor = valor;
            this.metas = metas;
        }

        // Cuanto se lleva hacia las metas: la oleada, los zombis, el combo...
        public double Valor
        {
            get { return valor(); }
        }
    }

    // En el orden de la ventana. Las metas van de a tres y crecen.
    public static readonly Familia[] Familias =
    {
        new Familia(Superviviente, () => Progreso.MejorOleada, 10, 25, 50),        // la 10 es el primer jefe
        new Familia(Exterminador, () => Progreso.MatadosEnTotal, 1000, 10000, 100000),
        new Familia(Imparable, () => Progreso.MejorCombo, 25, 50, 100),
        new Familia(Critico, () => PorcentajeDeCritico, 30, 50, 100),
        new Familia(Millonario, () => Progreso.MonedasGanadasJugando, 10000, 100000, 1000000),
        new Familia(Granadero, () => Progreso.MejorGranada, 5, 10, 15),
        new Familia(Furioso, () => Progreso.FuriasActivadas, 1, 25, 100),
        new Familia(Intocable, () => Progreso.MejorOleadaIntacta, 5, 10, 20),
        new Familia(Constante, () => Progreso.MejorRacha, 3, 7, 30),
        new Familia(Misionero, () => Progreso.MisionesCobradas, 10, 50, 200),
        new Familia(Coleccionista, () => EstrellasDelBestiario, 5, 10, 15),
        new Familia(SinFin, () => Progreso.MejorNivelLibre, 5, 10, 20),
    };

    // La probabilidad de critico comprada, en porcentaje entero: 30 y no 29,999.
    private static double PorcentajeDeCritico
    {
        get { return Math.Round(CatalogoMejoras.ProbabilidadCritico * 100.0); }
    }

    // Las ganadas, no las cobradas: se ganan matando, igual que las monedas de aca.
    private static double EstrellasDelBestiario
    {
        get
        {
            int n = 0;
            foreach (string tipo in Bestiario.Tipos) n += Bestiario.Alcanzadas(tipo);
            return n;
        }
    }

    public static Familia Buscar(string id)
    {
        foreach (var familia in Familias) if (familia.id == id) return familia;
        return null;
    }

    // Cuantas de esa familia estan ganadas (cobradas o no). Se ganan en orden.
    public static int Ganadas(string familia)
    {
        int n = 0;
        foreach (var logro in Progreso.Jugador.logros)
            if (logro.familia == familia && logro.escalon + 1 > n) n = logro.escalon + 1;
        return n;
    }

    public static int Cobradas(string familia)
    {
        int n = 0;
        foreach (var logro in Progreso.Jugador.logros)
            if (logro.familia == familia && logro.cobrado) n++;
        return n;
    }

    public static LogroGanado Ganado(string familia, int escalon)
    {
        foreach (var logro in Progreso.Jugador.logros)
            if (logro.familia == familia && logro.escalon == escalon) return logro;
        return null;
    }

    // La de escalon mas bajo ganada y sin cobrar de esa familia, o null.
    public static LogroGanado ParaCobrar(string familia)
    {
        LogroGanado menor = null;
        foreach (var logro in Progreso.Jugador.logros)
            if (logro.familia == familia && !logro.cobrado && (menor == null || logro.escalon < menor.escalon)) menor = logro;
        return menor;
    }

    public static int PorCobrar
    {
        get
        {
            int n = 0;
            foreach (var logro in Progreso.Jugador.logros)
                if (!logro.cobrado && Buscar(logro.familia) != null) n++;
            return n;
        }
    }

    // Todas las que hay y las ganadas: el "12 / 36" de la ventana.
    public static int Total
    {
        get { return Familias.Length * Escalones; }
    }

    public static int GanadasEnTotal
    {
        get
        {
            int n = 0;
            foreach (var familia in Familias) n += Ganadas(familia.id);
            return n;
        }
    }

    // La meta siguiente de esa familia, o 0 si ya estan las tres.
    public static double Siguiente(Familia familia)
    {
        int ganadas = Ganadas(familia.id);
        return ganadas < Escalones ? familia.metas[ganadas] : 0;
    }

    // La experiencia que da esa moneda: los niveles de su escalon, a lo que costaba el
    // nivel en que se gano.
    public static double Experiencia(LogroGanado logro)
    {
        if (logro == null) return 0;
        int e = Math.Max(0, Math.Min(logro.escalon, NivelesPorEscalon.Length - 1));
        return NivelesPorEscalon[e] * (double)NivelJugador.CostoDelNivel(logro.nivelAlGanar);
    }

    private static readonly List<LogroGanado> nuevas = new List<LogroGanado>();

    // Anota las monedas que se ganaron desde la ultima vez, con el nivel de ahora, y las
    // devuelve (para el aviso de la partida). La lista es la misma en cada llamada: vale
    // hasta la siguiente. Se llama seguido, asi que no aloca si no hay nada nuevo.
    public static List<LogroGanado> Revisar()
    {
        nuevas.Clear();
        var logros = Progreso.Jugador.logros;
        int nivel = -1;
        foreach (var familia in Familias)
        {
            int ganadas = Ganadas(familia.id);
            if (ganadas >= Escalones) continue;
            double valor = familia.Valor;
            while (ganadas < Escalones && valor >= familia.metas[ganadas])
            {
                if (nivel < 0) nivel = NivelJugador.Nivel;
                var logro = new LogroGanado { familia = familia.id, escalon = ganadas, nivelAlGanar = nivel };
                logros.Add(logro);
                nuevas.Add(logro);
                ganadas++;
            }
        }
        if (nuevas.Count > 0) Progreso.AvisarCambio();
        return nuevas;
    }

    // Cobra la moneda ganada de escalon mas bajo de esa familia: su experiencia va al
    // nivel. Devuelve cuanta dio (0 si no habia). Guarda en el acto, como todo cobro.
    public static double Cobrar(string familia)
    {
        var logro = ParaCobrar(familia);
        if (logro == null) return 0;
        logro.cobrado = true;
        double experiencia = Experiencia(logro);
        NivelJugador.Sumar(experiencia);
        Progreso.AvisarCambio();
        Progreso.Guardar();
        return experiencia;
    }

    public static string Nombre(string familia)
    {
        return Textos.De("logro_" + familia + "_nombre");
    }

    // Lo que pide esa meta: "Llega a un combo x50". Una meta de 1 tiene su texto propio
    // ("Usa la furia por primera vez"): con la plantilla salia "1 veces".
    public static string Descripcion(string familia, double meta)
    {
        if (meta == 1) return Textos.De("logro_" + familia + "_uno");
        return Textos.Formato("logro_" + familia, FormatoNumeros.Compacto(meta));
    }
}
