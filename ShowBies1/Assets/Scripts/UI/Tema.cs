using UnityEngine;
using UnityEngine.UI;

// El papel que cumple un color de la interfaz. Lo que se pinta con un rol cambia con
// el tema; lo que tiene un color propio (el verde de jugar, el dorado de la tienda,
// el rojo de GAME OVER) no se toca, porque ese color es lo que significa.
public enum RolDeTema
{
    Panel,       // el fondo de una ventana o de un panel
    Tarjeta,     // una tarjeta o un recuadro sobre el panel
    Texto,       // el texto principal sobre el panel o la tarjeta
    TextoSuave,  // lo secundario: la unidad, "NIVEL 3", los avisos
    Hueco,       // el relieve suave que separa una fila del panel
    Surco,       // el fondo de una barra de avance
    Vidrio,      // los botones secundarios translucidos
    Apagado,     // un boton que ahora no se puede usar
    Fondo,       // el fondo de una pantalla entera (la derrota)
    Acento       // un verde que sobre el fondo claro es oscuro y sobre el oscuro no se leeria
}

// El tema de la interfaz. Desde el 24/9 hay uno solo, **carbón neón** (pedido de Ivan:
// eligió esa paleta para la tienda y "la idea es que todo el juego tenga esa temática"):
// paneles casi negros, textos blancos y grises azulados, y el neón en los bordes y los
// botones. Antes había un claro ("pasto de día", paneles crema) y un oscuro que se
// prendía en Opciones; el claro se fue y el oscuro pasó a ser el neón, que queda siempre.
//
// El mecanismo de papeles sigue igual: los colores de las escenas y los prefabs son los
// del claro de antes (PintarConTema los guarda y Elegir los recibe), y con el tema fijo
// en oscuro cada papel toma el suyo de acá. Así no hubo que tocar cada escena. El claro
// solo lo usan las pruebas, para ver que el mecanismo sigue devolviendo lo de la escena.
//
// Nadie se suscribe a nada: quien pinta mira Revision en su Update, igual que con
// Progreso.Revision y con Idioma.Revision.
public static class Tema
{
    // La paleta de carbón neón, la de la tienda (ConstructorTienda). Los grises tiran a
    // azul: al lado del neón, un gris neutro se ve sucio.
    public static readonly Color PanelOscuro = new Color(0.043f, 0.051f, 0.075f, 0.98f);    // #0B0D13
    public static readonly Color TarjetaOscura = new Color(0.071f, 0.086f, 0.133f, 1f);     // #121622
    public static readonly Color TextoClaro = new Color(0.949f, 0.957f, 0.973f, 1f);        // #F2F4F8
    public static readonly Color TextoSuaveClaro = new Color(0.541f, 0.58f, 0.678f, 1f);    // #8A94AD
    public static readonly Color HuecoClaro = new Color(1f, 1f, 1f, 0.06f);
    public static readonly Color SurcoClaro = new Color(1f, 1f, 1f, 0.14f);
    public static readonly Color VidrioClaro = new Color(0.067f, 0.078f, 0.114f, 0.92f);   // #11141D
    public static readonly Color ApagadoOscuro = new Color(0.086f, 0.102f, 0.141f, 1f);    // #161A24
    public static readonly Color FondoOscuro = new Color(0.024f, 0.027f, 0.043f, 1f);      // #06070B
    public static readonly Color AcentoClaro = new Color(0.224f, 1f, 0.533f, 1f);          // #39FF88, el verde neón

    private static bool oscuroDePrueba;
    private static bool fijadoParaPruebas;

    public static int Revision { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetearEstadoCompartido()
    {
        oscuroDePrueba = false;
        fijadoParaPruebas = false;
        Revision = 0;
    }

    // Siempre: el neón es el único tema. La preferencia vieja ("TemaOscuro" en
    // PlayerPrefs) ya no se lee.
    public static bool Oscuro
    {
        get { return fijadoParaPruebas ? oscuroDePrueba : true; }
    }

    // El color que le toca a un rol en el tema oscuro.
    public static Color ColorOscuro(RolDeTema rol)
    {
        switch (rol)
        {
            case RolDeTema.Panel: return PanelOscuro;
            case RolDeTema.Tarjeta: return TarjetaOscura;
            case RolDeTema.Texto: return TextoClaro;
            case RolDeTema.TextoSuave: return TextoSuaveClaro;
            case RolDeTema.Hueco: return HuecoClaro;
            case RolDeTema.Surco: return SurcoClaro;
            case RolDeTema.Vidrio: return VidrioClaro;
            case RolDeTema.Apagado: return ApagadoOscuro;
            case RolDeTema.Fondo: return FondoOscuro;
            case RolDeTema.Acento: return AcentoClaro;
            default: return TextoClaro;
        }
    }

    // Lo que usa quien arma una ventana en codigo: su color de siempre si el tema es
    // claro, el del rol si es oscuro.
    public static Color Elegir(Color claro, RolDeTema rol)
    {
        return Oscuro ? ColorOscuro(rol) : claro;
    }

    // Le pone su papel a algo que se armo en codigo y lo pinta. Lo usan las ventanas
    // que se arman una sola vez (la diaria, el aviso de salir): con el componente puesto
    // se repintan solas cuando el jugador cambia el tema, sin volver a armarse.
    public static void Pintar(Graphic grafico, RolDeTema rol, Color claro)
    {
        if (grafico == null) return;
        // Reusa el que haya: las ventanas se copian unas de otras y podria venir puesto.
        var pintor = grafico.GetComponent<PintarConTema>();
        if (pintor == null) pintor = grafico.gameObject.AddComponent<PintarConTema>();
        pintor.rol = rol;
        pintor.colorClaro = claro;
        // En el acto: sobre un objeto prendido, AddComponent ya lo pinto en su OnEnable con
        // el blanco de fabrica, y sin esto quedaba blanco hasta el proximo cambio de tema
        // (le paso al boton del nivel, VentanaLogros).
        pintor.Repintar();
    }

    // Solo para las pruebas del editor: con false se ve el claro de antes (lo que traen
    // las escenas), para probar que el mecanismo lo devuelve tal cual. Con null, el de
    // siempre.
    public static void UsarParaPruebas(bool? tema)
    {
        fijadoParaPruebas = tema.HasValue;
        oscuroDePrueba = tema ?? true;
        Revision++;
    }
}
