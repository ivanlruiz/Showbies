// Los lugares donde el juego puede ofrecer un video con recompensa. Son strings y
// no un enum porque se guardan en progreso.json (los usos del dia): **un lugar no
// se renombra nunca**, igual que el id de una mejora.
//
// En uso: revivir, el x2 de las monedas en la derrota, el x2 de la recompensa diaria y
// el boton de monedas del menu, que son videos con premio, y el automatico de la
// derrota. El otro queda reservado para cuando entre, con el nombre ya elegido.
public static class LugarAnuncio
{
    public const string DuplicarDerrota = "duplicar_derrota";

    public const string Revivir = "revivir";
    public const string DuplicarRegalo = "regalo_x2";     // la recompensa diaria
    // El boton redondo del menu, debajo de las misiones: monedas por mirar un video
    // (VideoDeMonedas). Era el reservado "monedas_tienda", que nunca se uso: Ivan lo quiso
    // en el menu.
    public const string MonedasMenu = "monedas_menu";

    // El anuncio automatico (intersticial) al salir de la derrota: no premia ni es opt-in.
    // El puente de Android lo reconoce por este nombre (PuenteAnuncios.AUTOMATICO).
    public const string Automatico = "automatico";

    // Reservado, todavia sin usar: duplicar el bono de la oleada.
    public const string DuplicarBono = "duplicar_bono";
}
