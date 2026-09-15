using UnityEngine;

// La furia: un pico de poder que se compra una sola vez en la tienda y se activa en
// la partida con su boton del HUD (o con la F en PC). Mientras dura, el arma
// dispara y pega el doble y el jugador corre mas; despues hay que esperar el
// enfriamiento, que cuenta desde que se activa.
//
// Va en Jugador.prefab, como AplicarMejoras, y se entera de la compra en su Awake:
// comprarla no cambia la partida en curso. Usa tiempo escalado, asi la pausa
// congela la furia y el enfriamiento. La cadencia y el daño van por un
// multiplicador aparte del arma (GunController.FijarFuria), asi una caja que llega
// durante la furia no la pisa.
[DisallowMultipleComponent]
public class Furia : MonoBehaviour
{
    public PlayerController jugador;

    [Header("Balance")]
    public float enfriamiento = 120f;          // segundos desde que se activa hasta que se puede otra vez
    public float multiplicadorCadencia = 2f;
    public float multiplicadorDano = 2f;
    public float multiplicadorVelocidad = 1.3f;

    [Header("PC")]
    public KeyCode tecla = KeyCode.F;

    // La del jugador de la escena, para el boton del HUD.
    public static Furia Instancia { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetearEstadoCompartido()
    {
        Instancia = null;
    }

    public bool Desbloqueada { get; private set; }
    public float Duracion { get; private set; }

    // Cuantas veces se activo en la partida. El boton la mira para prender el
    // cartel tambien cuando se activa con la tecla.
    public int Activaciones { get; private set; }

    private float activadaEn = float.NegativeInfinity;
    private bool aplicada;

    public float RestanteFuria
    {
        get { return Desbloqueada ? Restante(Time.time, activadaEn, Duracion) : 0f; }
    }

    public float RestanteEnfriamiento
    {
        get { return Desbloqueada ? Restante(Time.time, activadaEn, enfriamiento) : 0f; }
    }

    public bool Activa
    {
        get { return RestanteFuria > 0f; }
    }

    public bool Lista
    {
        get { return Desbloqueada && !MenuPausa.Pausado && RestanteEnfriamiento <= 0f; }
    }

    // Lo que falta de un reloj que arranco en "desde" y dura "duracion". Sin
    // arrancar (desde = -infinito) da 0. Estatico para probarlo sin escena.
    public static float Restante(float ahora, float desde, float duracion)
    {
        return Mathf.Max(0f, desde + Mathf.Max(0f, duracion) - ahora);
    }

    private void Awake()
    {
        Instancia = this;
        if (jugador == null) jugador = GetComponent<PlayerController>();
        Desbloqueada = CatalogoMejoras.FuriaDesbloqueada;
        Duracion = CatalogoMejoras.DuracionFuria;
    }

    private void OnDestroy()
    {
        if (Instancia == this) Instancia = null;
    }

    private void Update()
    {
        if (!Desbloqueada) return;

        // En movil la activa el boton del HUD. Como todo lo que lee input, se corta
        // en la pausa.
        if (!Plataforma.EsMovil && !MenuPausa.Pausado && Input.GetKeyDown(tecla)) Activar();

        bool activa = Activa;
        if (activa != aplicada) Aplicar(activa);
    }

    // Devuelve si se activo: no pasa nada sin comprarla, en pausa o enfriando.
    public bool Activar()
    {
        if (!Lista || jugador == null) return false;

        activadaEn = Time.time;
        Activaciones++;
        Aplicar(true);
        Efectos.EmpezarFuria(transform.position, Duracion);
        return true;
    }

    private void Aplicar(bool activa)
    {
        aplicada = activa;
        if (jugador == null) return;

        if (jugador.theGun != null)
            jugador.theGun.FijarFuria(activa ? multiplicadorCadencia : 1f, activa ? multiplicadorDano : 1f);
        jugador.multiplicadorVelocidad = activa ? Mathf.Max(0f, multiplicadorVelocidad) : 1f;
        if (!activa) Efectos.TerminarFuria();
    }
}
