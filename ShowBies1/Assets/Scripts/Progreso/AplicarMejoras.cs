using UnityEngine;

// Pasa las mejoras compradas al jugador de la partida: dano y cadencia al arma,
// vida maxima y cura a la vida, el alcance del iman a las monedas y si hay granada. Va en
// Jugador.prefab, asi lo tienen las tres escenas de juego sin cablear nada por
// escena.
//
// Se aplica una vez, al empezar la partida (Awake, antes de que nadie dispare o
// reciba dano), y no al comprar: las compras son en el menu, y una mejora que
// cambiara a mitad de partida mezclaria la vida y la cadencia de dos niveles.
//
// Nunca escribe sobre gun.bala: es el prefab de la bala, un asset, y cambiarle
// el dañoDar lo cambiaria en disco en el editor y para todas las partidas. El
// dano va por tiro con FijarDanoPorBala.
[DisallowMultipleComponent]
public class AplicarMejoras : MonoBehaviour
{
    public PlayerController jugador;
    public PlayerHealth vida;

    // Lo que se aplico, para el medidor y las pruebas.
    public float DanoPorBala { get; private set; }
    public float TirosPorSegundo { get; private set; }
    public int VidaMaxima { get; private set; }
    public float MultiplicadorCura { get; private set; }
    public float RadioIman { get; private set; }
    public float ProbabilidadCritico { get; private set; }

    private void Awake()
    {
        Aplicar();
    }

    public void Aplicar()
    {
        if (jugador == null) jugador = GetComponent<PlayerController>();
        if (vida == null) vida = GetComponent<PlayerHealth>();
        if (jugador == null || jugador.theGun == null || vida == null)
        {
            Debug.LogWarning("AplicarMejoras: falta el PlayerController, su arma o el PlayerHealth; "
                + "la partida sigue sin mejoras.", this);
            return;
        }

        DanoPorBala = CatalogoMejoras.DanoPorBala;
        TirosPorSegundo = CatalogoMejoras.TirosPorSegundo;
        VidaMaxima = CatalogoMejoras.VidaMaxima;
        MultiplicadorCura = CatalogoMejoras.MultiplicadorVida;
        RadioIman = CatalogoMejoras.RadioIman;

        GunController arma = jugador.theGun;
        arma.FijarDanoPorBala(DanoPorBala);
        arma.FijarTirosPorSegundo(TirosPorSegundo);
        vida.FijarVidaMaxima(VidaMaxima, MultiplicadorCura);
        Moneda.FijarRadioIman(RadioIman);
        jugador.GranadaDesbloqueada = CatalogoMejoras.GranadaDesbloqueada;
        ProbabilidadCritico = CatalogoMejoras.ProbabilidadCritico;
        arma.FijarProbabilidadCritico(ProbabilidadCritico);
    }
}
