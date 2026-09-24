using UnityEngine;
using UnityEngine.SceneManagement;

// La derrota sin cambiar de escena, pedido de Ivan: al morir, la pantalla de siempre
// aparece enseguida encima de la partida, y detras de ella el mundo se va poniendo en
// blanco y negro en unos segundos **sin detenerse**: los zombis siguen caminando y
// dando zarpazos sobre el cuerpo. Hasta el 23/9 PlayerHealth.Terminar cargaba la
// escena Perdiste de golpe y la partida desaparecia en el mismo cuadro en que moria
// el jugador.
//
// Asi quedo despues de probarlo con Ivan. La primera version dejaba cinco segundos de
// gris sin pantalla y recien ahi la mostraba, con el fondo celeste encima que tapaba
// el gris ("la pantalla y el gris", a la vez); y congelaba el juego con timeScale en 0
// ("que no se freeze el juego, que siga todo").
//
// Que la partida siga andando obliga a que nada la cambie despues de morir, y eso lo
// cuidan otros: PlayerHealth deja al jugador quieto y sin arma, EnemyController no
// recibe daño (las balas y la granada que quedaron en el aire), las monedas no vuelan
// a un muerto, las cajas no se agarran, el jefe no ataca a un muerto y el modo libre
// no sigue subiendo de nivel. Todos miran PlayerHealth.EstaMuerto.
//
// La pantalla es la misma escena Perdiste, cargada en modo aditivo y sin tocarla:
// MenuPerdiste se entera de que va encima de la partida (SobreLaPartida) y apaga su
// camara, su EventSystem y su AudioListener, y deja el fondo transparente. Asi la
// escena sigue sirviendo sola, que es lo que se carga si esto no puede correr.
public class DerrotaEnLaPartida : MonoBehaviour
{
    public const int EscenaDerrota = 2;

    // Lo que tarda el mundo en quedar gris del todo. Lo que pidio Ivan: "ponele, no
    // se, 5 seg".
    public const float SegundosDeTransicion = 5f;

    // Mientras dura, nadie esta jugando aunque el tiempo corra: lo mira
    // MenuPausa.JuegoCongelado, y con eso se corta el input, la furia, la pausa y la
    // pausa de impacto de Efectos.
    public static bool Activa { get; private set; }

    // Cuanto va del gris, de 0 a 1. Para las pruebas.
    public static float Avance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetearEstadoCompartido()
    {
        Activa = false;
        Avance = 0f;
    }

    private FiltroBlancoYNegro filtro;
    private float desde;
    private float grisInicial;
    private float duracion;

    // Lo llama PlayerHealth.Terminar, despues de guardar todo.
    public static void Mostrar()
    {
        if (Activa) return;

        Camera camara = Camera.main;
        if (camara == null)
        {
            CargarComoAntes();
            return;
        }
        new GameObject("DerrotaEnLaPartida").AddComponent<DerrotaEnLaPartida>().Empezar(camara);
    }

    // Sin camara no hay mundo para dejar gris: la derrota de antes.
    private static void CargarComoAntes()
    {
        MenuPerdiste.SobreLaPartida = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(EscenaDerrota);
    }

    private void Empezar(Camera camara)
    {
        Activa = true;
        // La partida sigue andando. En 1 explicito: si venia de la oferta de revivir,
        // que si congela, o de una pausa de impacto, que la deja en camara lenta y ya no
        // la devuelve con la derrota activa.
        Time.timeScale = 1f;

        // Toma el filtro como este: si venia de la oferta de revivir ya esta gris (o a
        // mitad de camino) y sigue desde ahi, sin volver al color.
        filtro = FiltroBlancoYNegro.Tomar(camara);
        grisInicial = filtro != null ? filtro.cantidad : 1f;
        duracion = SegundosDeTransicion * (1f - grisInicial);
        desde = Time.unscaledTime;
        Avance = grisInicial;

        // La horda festeja desde ahora (EnemyController, el festejo): de aca se cuentan
        // sus oleadas.
        EnemyController.EmpezarFestejo();

        // La pantalla sale enseguida: se carga ya, y MenuPerdiste lee esto en su Awake.
        MenuPerdiste.SobreLaPartida = true;
        Scene juego = SceneManager.GetActiveScene();
        AsyncOperation carga = SceneManager.LoadSceneAsync(EscenaDerrota, LoadSceneMode.Additive);
        if (carga == null)
        {
            CargarComoAntes();
            return;
        }
        // El HUD se apaga cuando la derrota ya esta, no al morir: la escena aditiva tarda al
        // menos un cuadro en aparecer (mas en un telefono cargado), y en ese hueco no habia
        // ni HUD ni derrota, solo el mundo.
        carga.completed += _ => EsconderElHud(juego);
    }

    // El gris sigue detras de la pantalla. Tiempo sin escalar, que es el de la UI.
    private void Update()
    {
        if (Avance >= 1f) return;

        float pasado = Time.unscaledTime - desde;
        float t = duracion > 0f ? Mathf.Clamp01(pasado / duracion) : 1f;
        Avance = Mathf.Lerp(grisInicial, 1f, t);
        if (filtro != null) filtro.cantidad = Avance;
    }

    // "Se pone todo en blanco y negro", y la UI en overlay no pasa por la camara: el
    // HUD, los joysticks y la barra del jefe se quedarian a color encima del mundo
    // gris. Se apagan los canvas raiz de la escena del juego; los hijos se van con
    // ellos. No se vuelven a prender: de la derrota se sale siempre cambiando de escena.
    private static void EsconderElHud(Scene juego)
    {
        // Por si se salio de la partida antes de que la derrota terminara de cargar (la R la
        // recarga): esa escena ya no esta, y la nueva tiene su HUD.
        if (!juego.IsValid() || !juego.isLoaded) return;
        foreach (GameObject raiz in juego.GetRootGameObjects())
            foreach (Canvas canvas in raiz.GetComponentsInChildren<Canvas>(true))
                if (canvas.isRootCanvas) canvas.enabled = false;
    }

    // Se destruye con la escena del juego, al salir de la derrota.
    private void OnDestroy()
    {
        Activa = false;
        Avance = 0f;
    }
}
