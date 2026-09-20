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

// El tema de la interfaz: **claro** (el de siempre, "pasto de dia") u **oscuro**
// (pedido de Ivan). Cambia los paneles, las tarjetas y los textos de las pantallas:
// el menu, la tienda, la derrota y las ventanas. El pasto, el cielo y la partida no
// se tocan, porque el tema es de la interfaz y no del mundo.
//
// El tema claro es lo que ya esta guardado en cada escena y prefab: PintarConTema se
// acuerda del color que traia el objeto y lo devuelve tal cual, y las ventanas que se
// arman en codigo pasan su color de siempre por Elegir. La paleta de aca es solo la
// del oscuro, asi que sumar el modo oscuro no puede cambiar como se ve hoy el juego.
//
// Va en PlayerPrefs y no en el progreso porque es una preferencia del dispositivo,
// como el idioma y los volumenes. Nadie se suscribe a nada: quien pinta mira Revision
// en su Update, igual que con Progreso.Revision y con Idioma.Revision.
public static class Tema
{
    public const string ClavePreferencia = "TemaOscuro";

    // La paleta del tema oscuro. Los grises tiran a azul: un gris neutro al lado del
    // dorado de la tienda y del verde de los botones se ve sucio.
    public static readonly Color PanelOscuro = new Color(0.094f, 0.106f, 0.149f, 0.98f);
    public static readonly Color TarjetaOscura = new Color(0.141f, 0.161f, 0.22f, 1f);
    public static readonly Color TextoClaro = new Color(0.937f, 0.945f, 0.969f, 1f);
    public static readonly Color TextoSuaveClaro = new Color(0.64f, 0.67f, 0.75f, 0.95f);
    public static readonly Color HuecoClaro = new Color(1f, 1f, 1f, 0.07f);
    public static readonly Color SurcoClaro = new Color(1f, 1f, 1f, 0.16f);
    public static readonly Color VidrioClaro = new Color(1f, 1f, 1f, 0.14f);
    public static readonly Color ApagadoOscuro = new Color(0.231f, 0.255f, 0.322f, 1f);
    public static readonly Color FondoOscuro = new Color(0.067f, 0.09f, 0.149f, 1f);
    public static readonly Color AcentoClaro = new Color(0.435f, 0.878f, 0.31f, 1f);

    private static bool cargado;
    private static bool oscuro;
    private static bool fijadoParaPruebas;

    public static int Revision { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetearEstadoCompartido()
    {
        cargado = false;
        oscuro = false;
        fijadoParaPruebas = false;
        Revision = 0;
    }

    // Arranca en claro: es como se ve el juego desde siempre y como salen las capturas
    // de la ficha. Quien quiera el oscuro lo prende una vez y queda.
    public static bool Oscuro
    {
        get
        {
            if (!cargado)
            {
                cargado = true;
                oscuro = PlayerPrefs.GetInt(ClavePreferencia, 0) != 0;
            }
            return oscuro;
        }
    }

    public static void Fijar(bool nuevo)
    {
        if (Oscuro == nuevo) return;
        oscuro = nuevo;
        Revision++;

        if (fijadoParaPruebas) return;
        PlayerPrefs.SetInt(ClavePreferencia, nuevo ? 1 : 0);
        PlayerPrefs.Save();
    }

    public static void Alternar()
    {
        Fijar(!Oscuro);
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
    }

    // Solo para las pruebas del editor: fija el tema sin escribir PlayerPrefs.
    // Con null vuelve al guardado.
    public static void UsarParaPruebas(bool? tema)
    {
        if (tema.HasValue)
        {
            fijadoParaPruebas = true;
            cargado = true;
            oscuro = tema.Value;
        }
        else
        {
            fijadoParaPruebas = false;
            cargado = false;
        }
        Revision++;
    }
}
