// Cuanto crece algo con la oleada (o con el nivel del modo libre): crecimiento
// elevado a la cantidad de oleadas pasadas. La oleada 1 vale siempre 1, asi que
// los stats de los .asset son los de la primera oleada sin mejoras.
//
// Es una funcion pura y aparte para que la usen igual WaveManager, GeneradorZombis
// y las pruebas, sin escena: si cada uno hiciera su propio Pow, un off-by-one en
// uno solo haria que las pruebas comparen contra otra curva.
public static class Escalado
{
    public static float PorOleada(float crecimiento, int oleada)
    {
        // Un crecimiento sin cargar (0 en un inspector viejo) no escala, en vez de
        // dejar a los zombis con vida cero.
        return crecimiento <= 0f ? 1f : (float)System.Math.Pow(crecimiento, System.Math.Max(0, oleada - 1));
    }

    // En dos tramos: crece 'crecimiento' hasta la oleada anterior a 'desde', y 'tardio' de
    // ahi en adelante: la oleada 'desde' ya vale la anterior por 'tardio'. Con 'desde' en 0
    // o un 'tardio' sin cargar es la de un tramo. Lo usan las oleadas desde el 10/10 (ver
    // WaveManager.oleadaDelSegundoTramo); el modo libre sigue con la de un tramo.
    public static float PorOleada(float crecimiento, int oleada, int desde, float tardio)
    {
        if (desde <= 1 || tardio <= 0f || oleada < desde) return PorOleada(crecimiento, oleada);
        double primero = crecimiento <= 0f ? 1.0 : System.Math.Pow(crecimiento, desde - 2);
        return (float)(primero * System.Math.Pow(tardio, oleada - desde + 1));
    }
}
