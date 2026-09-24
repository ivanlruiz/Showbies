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

    // Lo que suelta cada zombi, en promedio. De menos a proposito, como todo aca: desde que
    // salen los tanques y los veloces la mezcla de WaveMode suelta mas (unas 2,4 desde la
    // oleada 9), y eso sin contar al jefe.
    private const double MonedasPorZombi = 2.0;

    // Los zombis que se matan en una partida que llega a la oleada m: la oleada n saca
    // 10 + 4n, y esto es la suma hasta m, 12m + 2m². Hasta el 23/9 devolvia 10m + 2m²: le
    // faltaban 2m (6 de 54 en la oleada 3). La prueba la compara con la suma hecha con los
    // numeros del WaveManager de WaveMode, asi no se desincroniza si se tocan.
    public static double ZombisPorPartida(int mejorOleada)
    {
        int m = Math.Max(3, mejorOleada);
        return 12.0 * m + 2.0 * m * m;
    }

    // Lo que deja de monedas esa partida: cada zombi suelta unas 2 y el multiplicador de
    // la oleada (CrecimientoMonedasOleadas por oleada) se toma a mitad de camino.
    public static double MonedasPorPartida(int mejorOleada)
    {
        return ZombisPorPartida(mejorOleada) * MonedasPorZombi
             * Math.Pow(CrecimientoMonedasOleadas, Math.Max(3, mejorOleada) * 0.5);
    }

    // Cuanto dura esa partida, en segundos. Hace falta para los objetivos que se cumplen
    // con el reloj y no matando: la furia sale cada 120 s y la granada cada 5, asi que
    // cuantas entran depende del tiempo y no del numero de oleada.
    //
    // La cuenta: cada zombi se tarda un tercio de segundo largo en promedio (aparecer,
    // acercarse y morir), mas el descanso de 3 s entre oleadas. Da unos 1.400 s para una
    // partida que llega a la 40, que es lo que dura de verdad.
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
