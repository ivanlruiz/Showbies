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
    // Los zombis que se matan en una partida que llega a la oleada m: la oleada n saca
    // 10 + 4n, y esto es la suma hasta m.
    public static double ZombisPorPartida(int mejorOleada)
    {
        int m = Math.Max(3, mejorOleada);
        return 10.0 * m + 2.0 * m * m;
    }

    // Lo que deja de monedas esa partida: cada zombi suelta unas 2 y el multiplicador de
    // la oleada (1,08 por oleada, ver WaveManager) se toma a mitad de camino.
    public static double MonedasPorPartida(int mejorOleada)
    {
        return ZombisPorPartida(mejorOleada) * 2.0 * Math.Pow(1.08, Math.Max(3, mejorOleada) * 0.5);
    }

    // Numeros redondos para leer de un vistazo: de a 5, de a 10 o de a 50.
    public static double Redondo(double x)
    {
        double paso = x < 100 ? 5 : x < 1000 ? 10 : 50;
        return Math.Max(paso, Math.Round(x / paso) * paso);
    }
}
