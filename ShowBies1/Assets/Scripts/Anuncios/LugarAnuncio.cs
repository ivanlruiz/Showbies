// Los lugares donde el juego puede ofrecer un video con recompensa. Son strings y
// no un enum porque se guardan en progreso.json (los usos del dia): **un lugar no
// se renombra nunca**, igual que el id de una mejora.
//
// En uso: revivir, el x2 de las monedas en la derrota y el x2 de la recompensa
// diaria, que son videos con premio, y el automatico de la derrota. Los otros quedan
// reservados para cuando entren, con el nombre ya elegido.
public static class LugarAnuncio
{
    public const string DuplicarDerrota = "duplicar_derrota";

    public const string Revivir = "revivir";
    public const string DuplicarRegalo = "regalo_x2";     // la recompensa diaria

    // El anuncio automatico (intersticial) al salir de la derrota: no premia ni es opt-in.
    // El puente de Android lo reconoce por este nombre (PuenteAnuncios.AUTOMATICO).
    public const string Automatico = "automatico";

    // Reservados, todavia sin usar: monedas gratis en la tienda y duplicar el bono de la oleada.
    public const string MonedasTienda = "monedas_tienda";
    public const string DuplicarBono = "duplicar_bono";
}
