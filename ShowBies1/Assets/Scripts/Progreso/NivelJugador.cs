using System;
using System.Collections.Generic;

// El nivel del jugador, pedido de Ivan: la experiencia sale de jugar (los puntos de cada
// zombi que se mata, los mismos del puntaje) y de los logros (Logros), y cada nivel deja
// un premio que se cobra en el menu (VentanaLogros). De aca van a colgar despues las
// armas y las gemas; por ahora el premio son monedas, y cada cinco niveles, el triple.
//
// La curva: pasar del nivel L al siguiente cuesta 100L - 75 de experiencia (25 el
// primero, 125 el segundo, 925 el decimo). Una partida da mas experiencia cuanto mas
// lejos se llega (mas zombis por oleada, y mas duros), asi que con esta curva se sube
// mas o menos un nivel por partida en todo el juego: con el modelo de Economia, el 10
// cae cerca de la partida 10 y el 50 cerca de la 55. La prueba lo mide.
//
// El premio de cada nivel se congela con la mejor oleada del momento en que se sube, no
// con la de cuando se cobra: si no, la jugada optima seria no cobrarlo nunca, como ya
// paso con las misiones, la diaria y el semanal. Entra por CobrarPremio: no cuenta como
// monedas ganadas jugando.
public static class NivelJugador
{
    // Que parte de lo que deja una partida (Economia) paga cada nivel, y cuanto mas los
    // grandes. Con un nivel por partida, es un 15 % mas de monedas, y algo mas con los
    // grandes: un premio de subir, no una segunda forma de jugar.
    public const double FraccionPorNivel = 0.15;
    public const int CadaCuantosGrande = 5;
    public const double MultiplicadorGrande = 3.0;

    // Los puntos de cada tipo de zombi, los de su asset Enemy (Assets/Zombies). Hacen falta
    // sin escena para la experiencia de un progreso viejo (Progreso.MigrarAlNivel); la
    // prueba los compara con los assets, asi que si se toca el balance de un zombi y no
    // esto, falla.
    public static int PuntosPorTipo(string tipo)
    {
        switch (tipo)
        {
            case Bestiario.Normal: return 1;
            case Bestiario.Rapido: return 2;
            case Bestiario.Faster: return 5;
            case Bestiario.Tanque: return 20;
            case Bestiario.Jefe: return 100;
            default: return 1;
        }
    }

    // Lo que cuesta pasar de ese nivel al siguiente.
    public static int CostoDelNivel(int nivel)
    {
        return 100 * Math.Max(1, nivel) - 75;
    }

    // La experiencia total con que se llega a ese nivel: la suma de los costos de antes,
    // (L - 1)(50L - 75). El 1 es 0 y el 2 es 25.
    public static double ExperienciaDelNivel(int nivel)
    {
        int l = Math.Max(1, nivel);
        return (l - 1) * (50.0 * l - 75.0);
    }

    // El nivel con esa experiencia: la raiz de la cuenta de arriba, corregida mirando la
    // tabla por si el double queda un pelo corto o largo justo en el borde.
    public static int NivelCon(double experiencia)
    {
        if (!(experiencia > 0)) return 1;
        if (double.IsInfinity(experiencia)) return int.MaxValue / 2;
        double raiz = (125.0 + Math.Sqrt(625.0 + 200.0 * experiencia)) / 100.0;
        int nivel = (int)Math.Max(1.0, Math.Min(int.MaxValue / 2, Math.Floor(raiz)));
        while (nivel > 1 && ExperienciaDelNivel(nivel) > experiencia) nivel--;
        while (ExperienciaDelNivel(nivel + 1) <= experiencia) nivel++;
        return nivel;
    }

    public static double Experiencia
    {
        get { return Progreso.Jugador.experiencia; }
    }

    public static int Nivel
    {
        get { return NivelCon(Experiencia); }
    }

    // Cuanto se lleva del nivel actual y cuanto cuesta: la barra.
    public static double EnElNivel
    {
        get { double xp = Experiencia; return xp - ExperienciaDelNivel(NivelCon(xp)); }
    }

    public static int CostoActual
    {
        get { return CostoDelNivel(Nivel); }
    }

    public static bool EsGrande(int nivel)
    {
        return nivel > 0 && nivel % CadaCuantosGrande == 0;
    }

    // Lo que paga ese nivel con esa mejor oleada, en numeros redondos.
    public static double Premio(int nivel, int mejorOleada)
    {
        double monto = FraccionPorNivel * Economia.MonedasPorPartida(mejorOleada);
        if (EsGrande(nivel)) monto *= MultiplicadorGrande;
        return Economia.Redondo(monto);
    }

    // Lo que pagaria el nivel siguiente si se subiera ahora: lo que muestra la ventana
    // antes de llegar. El de verdad se congela al subir.
    public static double PremioDelSiguiente
    {
        get { return Premio(Nivel + 1, Progreso.MejorOleada); }
    }

    // Suma experiencia. Cada nivel que se cruza deja su premio pendiente, con la mejor
    // oleada de ahora. Devuelve cuantos niveles se subieron. No guarda: lo de jugar se
    // guarda en los puntos seguros de siempre (como las monedas) y lo de un logro, al
    // cobrarlo (Logros.Cobrar).
    public static int Sumar(double experiencia)
    {
        if (!(experiencia > 0) || double.IsInfinity(experiencia)) return 0;
        var estado = Progreso.Jugador;
        int antes = NivelCon(estado.experiencia);
        estado.experiencia += experiencia;
        int despues = NivelCon(estado.experiencia);
        if (despues == antes) return 0;

        int oleada = Progreso.MejorOleada;
        while (estado.nivelPremiado < despues)
        {
            estado.nivelPremiado++;
            estado.premios.Add(new PremioDeNivel { nivel = estado.nivelPremiado, oleada = oleada });
        }
        Progreso.AvisarCambio();
        return despues - antes;
    }

    public static int PorCobrar
    {
        get { return Progreso.Jugador.premios.Count; }
    }

    // El premio pendiente de nivel mas bajo, o null si no hay: es el que se cobra primero.
    public static PremioDeNivel Pendiente
    {
        get
        {
            PremioDeNivel menor = null;
            foreach (var premio in Progreso.Jugador.premios)
                if (menor == null || premio.nivel < menor.nivel) menor = premio;
            return menor;
        }
    }

    // El premio pendiente de ese nivel, o null.
    public static PremioDeNivel PendienteDe(int nivel)
    {
        foreach (var premio in Progreso.Jugador.premios)
            if (premio.nivel == nivel) return premio;
        return null;
    }

    public static double Monto(PremioDeNivel premio)
    {
        return premio == null ? 0 : Premio(premio.nivel, premio.oleada);
    }

    // Cobra el premio pendiente de nivel mas bajo; devuelve cuanto dio (0 si no habia).
    // Guarda en el acto, por CobrarPremio.
    public static double Cobrar()
    {
        var premio = Pendiente;
        if (premio == null) return 0;
        Progreso.Jugador.premios.Remove(premio);
        double monto = Monto(premio);
        Progreso.CobrarPremio("nivel_" + premio.nivel, monto, false);
        return monto;
    }
}

[Serializable]
public class EstadoJugador
{
    public double experiencia;
    public int nivelPremiado = 1;     // el ultimo nivel que ya dejo su premio (pendiente o cobrado)
    public List<PremioDeNivel> premios = new List<PremioDeNivel>();   // los que faltan cobrar
    public List<LogroGanado> logros = new List<LogroGanado>();        // los ganados, cobrados o no
}

[Serializable]
public class PremioDeNivel
{
    public int nivel;
    public int oleada;                // la mejor oleada al subir: es la que paga
}

[Serializable]
public class LogroGanado
{
    public string familia;            // el id de la familia (Logros): se guarda, no se renombra
    public int escalon;               // 0 bronce, 1 plata, 2 oro
    public int nivelAlGanar;          // el nivel del jugador cuando se gano: es el que paga
    public bool cobrado;
}
