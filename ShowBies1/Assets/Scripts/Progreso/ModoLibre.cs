// El modo libre se gana: hasta llegar a la oleada OleadaParaDesbloquear en el modo
// oleadas, el menu lo muestra bloqueado y nada lleva a esa escena. Llegar a una
// oleada es haber completado la anterior, y eso es lo que guarda el progreso
// (MejorOleada cuenta las completadas).
//
// Pedido de Ivan: el libre es el premio de las oleadas, y un jugador nuevo que
// entra directo al libre no ve el bono por oleada ni el jefe.
public static class ModoLibre
{
    public const int OleadaParaDesbloquear = 12;

    public static bool Desbloqueado
    {
        get { return DesbloqueadoCon(Progreso.MejorOleada); }
    }

    // Estatico y sin progreso para probarlo.
    public static bool DesbloqueadoCon(int mejorOleadaCompletada)
    {
        return mejorOleadaCompletada >= OleadaParaDesbloquear - 1;
    }

    // Adonde mandar a alguien que pidio el libre: si todavia no lo tiene, a las oleadas.
    public static int EscenaPara(int modoPedido)
    {
        if (modoPedido == TiendaMejoras.EscenaModoLibre && !Desbloqueado) return TiendaMejoras.EscenaOleadas;
        return modoPedido;
    }
}
