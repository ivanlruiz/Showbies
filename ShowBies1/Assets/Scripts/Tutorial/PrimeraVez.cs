// Lo que cambia para alguien que recien instala el juego: PLAY lo manda derecho a la
// oleada 1 en vez de abrirle el panel de modos, la primera partida trae la guia de los
// controles (GuiaPrimeraPartida), la tienda le senala la primera compra
// (GuiaPrimeraCompra) y la recompensa diaria espera a que termine una partida.
//
// Todo sale del progreso y no de una marca aparte: el que ya jugo lo tiene anotado.
public static class PrimeraVez
{
    // Nunca termino una partida, no completo ninguna oleada y no dejo una a medias.
    public static bool NuncaJugo
    {
        get { return NoTerminoPartidas && Progreso.OleadaEnCurso == 0; }
    }

    // Lo mismo sin mirar la oleada a medias: la guia de la primera partida sigue si se
    // sale y se retoma, y WaveManager guarda la oleada en curso apenas empieza.
    public static bool NoTerminoPartidas
    {
        get { return Progreso.PartidasTerminadas == 0 && Progreso.MejorOleada == 0; }
    }

    // Ninguna mejora de la tienda comprada. Sin catalogo no se sabe: se dice que no,
    // asi la guia no aparece.
    public static bool NuncaCompro
    {
        get
        {
            var catalogo = CatalogoMejoras.Instancia;
            if (catalogo == null || catalogo.enTienda == null) return false;
            foreach (var mejora in catalogo.enTienda)
            {
                if (mejora != null && Progreso.Nivel(mejora.id) > 0) return false;
            }
            return true;
        }
    }
}
