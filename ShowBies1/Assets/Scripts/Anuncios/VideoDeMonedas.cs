using System;

// El video del boton de monedas del menu (BotonVideoDeMonedas, pedido de Ivan el 10/10): un
// video con premio que se elige mirar, como los otros, y paga monedas. Lo eligio Ivan:
//
// - Paga un cuarto de partida en la mejor oleada (con la vara de Economia, como la diaria y las
//   misiones), con piso de PremioMinimo: al principio 50 monedas y en la oleada 30 unas 3.500.
// - Uno cada SegundosEntreVideos (10 minutos): da una razon para pasar por el menu entre
//   partidas sin volverlo una fabrica de monedas. La espera se cuenta con el reloj del telefono:
//   adelantarlo da otro video antes, que es mirar mas anuncios, y atrasarlo no deja esperando.
//
// Lo demas es lo de todos los videos (ServicioAnuncios): los 3 minutos jugados, que haya video
// cargado y que el jugador no los haya apagado. El premio entra por CobrarPremio: no son monedas
// ganadas jugando.
public static class VideoDeMonedas
{
    public const double FraccionDeLaPartida = 0.25;
    public const double PremioMinimo = 50;
    public const long SegundosEntreVideos = 600;

    // Separada para las pruebas.
    public static double Premio(int mejorOleada)
    {
        return Math.Max(PremioMinimo, Economia.Redondo(FraccionDeLaPartida * Economia.MonedasPorPartida(mejorOleada)));
    }

    // Separada para las pruebas: cuanto falta para el proximo, en segundos. Sin uno anterior, o
    // con el reloj atrasado por debajo del ultimo, nada.
    public static long SegundosQueFaltan(long ultimo, long ahora)
    {
        if (ultimo <= 0 || ahora < ultimo) return 0;
        return Math.Max(0, SegundosEntreVideos - (ahora - ultimo));
    }

    public static long Ahora
    {
        get { return DateTimeOffset.UtcNow.ToUnixTimeSeconds(); }
    }

    public static long Faltan
    {
        get { return SegundosQueFaltan(Progreso.UltimoVideoDeMonedas, Ahora); }
    }

    public static double PremioDeHoy
    {
        get { return Premio(Progreso.MejorOleada); }
    }

    // Lo que hay que mostrar: el boton existe si se puede mirar ya o si se esta esperando el
    // proximo (con la cuenta atras). Sin video cargado, o con los videos apagados, no existe.
    public static bool HayVideo
    {
        get { return ServicioAnuncios.PuedeOfrecer(LugarAnuncio.MonedasMenu); }
    }

    // Lo llama el boton cuando el video termino con premio. Devuelve lo que pago.
    public static double Cobrar()
    {
        double premio = PremioDeHoy;
        Progreso.RegistrarVideoDeMonedas(Ahora);
        Progreso.CobrarPremio(LugarAnuncio.MonedasMenu, premio, false);
        return premio;
    }
}
