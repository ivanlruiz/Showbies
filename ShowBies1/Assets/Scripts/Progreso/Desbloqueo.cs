// Lo que un jugador nuevo no ve de entrada (10/10, de la revision de Gemini: para un juego chico
// hay demasiados sistemas de golpe, y el que recien entiende los joysticks se abruma). Aparece en
// dos tandas, siempre en este orden:
//
//  - Las misiones del dia, al llegar a la oleada 5 (Misiones). Hasta entonces el menu no muestra
//    su boton, la derrota no las propone de proximo objetivo y no se arman: no avanzan, no se
//    cumplen a escondidas ni se cobran solas a medianoche. Se arman al destaparse, en la misma
//    partida, y empiezan a contar desde ahi.
//  - El resto, en el nivel 5 del jugador (Todo): el trofeo del bestiario, la medalla de los
//    logros (la ventana del nivel sigue, sin la lista), la fila del desafio semanal y el boton
//    de monedas por video, y en la partida los avisos de estrellas, logros y desafio. Esos
//    contadores siguen sumando por detras: al aparecer no se perdio nada, y lo ganado espera
//    para cobrarse. Pide tambien las misiones, asi la fila de botones de arriba a la derecha
//    se llena de izquierda a derecha (el de video cuelga del de las misiones) y Discord nunca
//    deja un hueco.
//
// AvisoDeMisiones anuncia cada tanda con su festejo. El nivel sale de NivelJugador y la oleada
// de Progreso.MejorOleada: quien ya paso los dos no ve ninguna diferencia.
public static class Desbloqueo
{
    public const int NivelDeTodo = 5;
    // Llegar a la 5 es haber completado la 4, que es lo que guarda MejorOleada (como el modo libre).
    public const int OleadaDeLasMisiones = 5;

    public static bool Misiones
    {
        get { return Progreso.MejorOleada >= OleadaDeLasMisiones - 1; }
    }

    public static bool Todo
    {
        get { return Misiones && NivelJugador.Nivel >= NivelDeTodo; }
    }
}
