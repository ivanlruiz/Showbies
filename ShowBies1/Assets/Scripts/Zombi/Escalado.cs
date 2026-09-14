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
}
