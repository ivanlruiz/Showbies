// Lo que un jugador nuevo no ve hasta el nivel 5 del jugador (10/10, de la revision de Gemini:
// para un juego chico hay demasiados sistemas de golpe, y el que recien entiende los joysticks se
// abruma). Hasta entonces el menu no muestra el trofeo del bestiario, la medalla de los logros
// (la ventana del nivel sigue, sin la lista), la fila del desafio semanal ni el boton de monedas
// por video, y en la partida no salen los avisos de estrellas, logros y desafio. Los contadores
// siguen sumando por detras: al aparecer no se perdio nada, y lo ganado espera para cobrarse.
// Al llegar al nivel, AvisoDeMisiones lo anuncia ("¡NUEVO EN EL MENU!").
//
// El nivel sale de NivelJugador, que esta en el progreso: un jugador que ya paso el 5 no ve
// ninguna diferencia.
public static class Desbloqueo
{
    public const int NivelDeTodo = 5;

    public static bool Todo
    {
        get { return NivelJugador.Nivel >= NivelDeTodo; }
    }
}
