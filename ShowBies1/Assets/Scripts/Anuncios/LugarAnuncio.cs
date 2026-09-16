// Los lugares donde el juego puede ofrecer un video con recompensa. Son strings y
// no un enum porque se guardan en progreso.json (los usos del dia): **un lugar no
// se renombra nunca**, igual que el id de una mejora.
//
// En esta version hay uno solo: el x2 de las monedas en la derrota. Los otros
// quedan reservados para cuando entren, con el nombre ya elegido.
public static class LugarAnuncio
{
    public const string DuplicarDerrota = "duplicar_derrota";

    // Reservados, todavia sin usar: revivir, monedas gratis en la tienda, duplicar
    // el bono de la oleada, duplicar el regalo diario.
    public const string Revivir = "revivir";
    public const string MonedasTienda = "monedas_tienda";
    public const string DuplicarBono = "duplicar_bono";
    public const string DuplicarRegalo = "regalo_x2";
}
