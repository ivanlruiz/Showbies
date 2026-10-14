using System;

// La vara con que se miden los premios del juego: cuanto deja jugar. La usan las misiones
// del dia, el bestiario y la recompensa diaria, que pagan una fraccion de eso en vez de
// montos fijos; con montos fijos, un premio que servia en la oleada 5 era un regalo en la
// 1 y calderilla en la 40, y hubo que arreglarlo tres veces por separado.
//
// Es de menos a proposito: cuenta los zombis por lo que sueltan y no suma el bono de cada
// oleada ni el botin, asi un premio calculado con esto nunca se pasa de lo que da jugarlo.
public static class Economia
{
    // Cuanto crece por oleada lo que vale cada moneda: el crecimientoMonedas del WaveManager
    // de WaveMode, copiado aca porque los premios se calculan en el menu, sin escena. Lo usan
    // MonedasPorPartida y el Bestiario, y la prueba de logica lo compara con la escena: si se
    // cambia uno y no el otro, todos los premios pasan a medirse con otra vara sin que nada
    // lo avise.
    public const double CrecimientoMonedasOleadas = 1.08;

    // Y lo que vale de base cada moneda en las oleadas: el valorDeLaMoneda del WaveManager de
    // WaveMode (x1,4 desde la 1.6.0, con la mitad de zombis por oleada), copiado por lo mismo.
    // Lo usan MonedasPorPartida y el Bestiario, y la prueba lo compara con la escena.
    public const double MultiplicadorMonedasOleadas = 1.4;

    // Lo que suelta cada zombi, en promedio. De menos a proposito, como todo aca: desde que
    // salen los tanques y los veloces la mezcla de WaveMode suelta mas (unas 2,4 desde la
    // oleada 9), y eso sin contar al jefe.
    private const double MonedasPorZombi = 2.0;

    // Los zombis de cada oleada: ZombisBase + ZombisPorOleada x n, los del WaveManager de
    // WaveMode (10 + 2n desde la 1.6.0; eran 10 + 4n). La prueba los compara con la escena.
    public const int ZombisBase = 10;
    public const int ZombisPorOleada = 2;

    // Los zombis que se matan en una partida que llega a la oleada m: la suma hasta m, que con
    // 10 + 2n es 11m + m². Hasta el 23/9 le faltaban 2m (con 10 + 4n devolvia 10m + 2m²). La
    // prueba la compara con la suma hecha con los numeros del WaveManager de WaveMode.
    public static double ZombisPorPartida(int mejorOleada)
    {
        int m = Math.Max(3, mejorOleada);
        return ZombisBase * (double)m + ZombisPorOleada * (double)m * (m + 1) * 0.5;
    }

    // Lo que deja de monedas esa partida: cada zombi suelta unas 2, que valen
    // MultiplicadorMonedasOleadas, y el multiplicador de la oleada (CrecimientoMonedasOleadas
    // por oleada) se toma a mitad de camino.
    public static double MonedasPorPartida(int mejorOleada)
    {
        return ZombisPorPartida(mejorOleada) * MonedasPorZombi * MultiplicadorMonedasOleadas
             * Math.Pow(CrecimientoMonedasOleadas, Math.Max(3, mejorOleada) * 0.5);
    }

    // Cuanto dura esa partida, en segundos. Hace falta para los objetivos que se cumplen
    // con el reloj y no matando: la furia sale cada 120 s y la granada cada 5, asi que
    // cuantas entran depende del tiempo y no del numero de oleada.
    //
    // La cuenta: cada zombi se tarda un tercio de segundo largo en promedio (aparecer,
    // acercarse y morir), mas el descanso de 3 s entre oleadas. Es lo que se tarda de verdad,
    // no el intervalo entre apariciones: con 10 + 2n cada 0,20 s (1.6.0) da unos 840 s para una
    // partida que llega a la 40 y 1.220 para la 50, algo de mas contra la simulacion (17-19
    // minutos para terminar la 50, simulaciones/cartas_1.6.0): de mas es pedir menos furias y
    // granadas de las que entran.
    public static double SegundosPorPartida(int mejorOleada)
    {
        int m = Math.Max(3, mejorOleada);
        return ZombisPorPartida(m) * 0.35 + m * 3.0;
    }

    // Las balas que se disparan en esa partida. Los zombis por lo que cuesta matarlos, que
    // NO es constante: el danio comprable crece con el logaritmo de las monedas y la vida
    // de los zombis, exponencial, asi que cada vez hacen falta mas balas por zombi.
    public static double BalasPorPartida(int mejorOleada)
    {
        int m = Math.Max(3, mejorOleada);
        double balasPorZombi = 5.0 * Math.Pow(1.03, m);
        return ZombisPorPartida(m) * balasPorZombi;
    }

    // Numeros redondos para leer de un vistazo: de a 5, de a 10 o de a 50.
    public static double Redondo(double x)
    {
        double paso = x < 100 ? 5 : x < 1000 ? 10 : 50;
        return Math.Max(paso, Math.Round(x / paso) * paso);
    }
}
