using TMPro;
using UnityEngine;
using UnityEngine.UI;

// La barra de vida del jefe, grande y arriba de todo (pedido de Ivan, como la de los
// jefes de Minecraft): mientras hay un jefe vivo se ve su nombre, cuanta vida le queda y
// la muesca donde entra en furia (la mitad). Pegarle 500 veces a un zombi grande sin
// saber cuanto falta no se siente un evento.
//
// Detras del relleno va una barra blanca que baja despacio: es el golpe que acaba de
// entrar, y hace que cada bala se vea.
//
// Se arma en codigo y vive en el canvas del prefab MenuPausa, que esta en las tres
// escenas de juego (como CursorMira): asi esta en todas sin tocar cada escena. Cuelga
// del area segura, debajo del boton de pausa, y el panel de la pausa se dibuja despues,
// asi que la tapa mientras el juego esta pausado.
public class BarraDelJefe : MonoBehaviour
{
    public TMP_FontAsset fuente;
    public Material materialContorno;
    public Sprite pildora;

    [Header("Medidas")]
    public float ancho = 820f;
    public float alto = 26f;
    [Tooltip("Debajo del boton de pausa, que en movil va arriba al centro.")]
    public float desdeArriba = 170f;

    [Header("Colores")]
    public Color colorFondo = new Color(0f, 0f, 0f, 0.55f);
    public Color colorVida = new Color(0.87f, 0.18f, 0.2f, 1f);
    public Color colorFuria = new Color(1f, 0.45f, 0.1f, 1f);
    public Color colorGolpe = new Color(1f, 1f, 1f, 0.75f);
    public Color colorMuesca = new Color(1f, 1f, 1f, 0.5f);

    [Tooltip("Lo que tarda el jefe en tener su vida definitiva: quien lo saca le pone los multiplicadores en el mismo frame.")]
    public float esperaAlAparecer = 1f;
    public float velocidadDelGolpe = 0.4f;    // fracciones de barra por segundo

    // El bloque es el nombre y, debajo, la barra. `desdeArriba` apunta al techo del
    // bloque y el nombre empieza un poco mas abajo, que es el aire contra el boton de
    // pausa.
    private const float AltoNombre = 52f;
    private const float MargenSuperior = 42f;
    private const float SeparacionNombreMarco = 1f;   // el texto ya trae su propio aire

    // Donde queda la raiz cuando termino de entrar. El techo del nombre, no el del rect.
    private float YEnReposo
    {
        get { return -(desdeArriba + MargenSuperior); }
    }

    // La franja que ocupa, medida desde el borde de arriba: del techo del nombre al piso de
    // la barra. La prueba de logica la usa para que el aviso de mision no se le pise.
    public float TechoDesdeArriba { get { return desdeArriba + MargenSuperior; } }
    public float PisoDesdeArriba { get { return TechoDesdeArriba + AltoNombre + SeparacionNombreMarco + alto; } }

    private RectTransform raiz;
    private RectTransform relleno;
    private RectTransform golpe;
    private RectTransform muesca;
    private TMP_Text nombre;
    private Image imagenRelleno;
    private CanvasGroup grupo;

    private EnemyController jefe;
    // Sus patrones: donde entra en furia y si ya entro. Null en un jefe sin patrones.
    private JefePatrones patrones;
    private int numeroDelJefe;
    private float desdeQueAparecio;
    private float visible;              // 0 escondida, 1 puesta
    private float fraccion = 1f;
    private float fraccionGolpe = 1f;

    private void Start()
    {
        Armar();
        raiz.gameObject.SetActive(false);
    }

    private void Armar()
    {
        // Los hijos cuelgan del techo de la raiz y la raiz mide lo que ocupan los dos:
        // antes el marco caia entero por debajo del rect de su propia raiz. No se veia
        // porque nada recorta, pero cualquier mascara o layout que se sume lo cortaria.
        var padre = (RectTransform)transform;
        raiz = ConstructorUI.Rect(padre, "BarraDelJefe", Vector2.zero,
                                  new Vector2(ancho, AltoNombre + SeparacionNombreMarco + alto));
        raiz.anchorMin = raiz.anchorMax = new Vector2(0.5f, 1f);
        raiz.pivot = new Vector2(0.5f, 1f);
        raiz.anchoredPosition = new Vector2(0f, YEnReposo);

        nombre = ConstructorUI.Texto(raiz, "Nombre", "", 44f, Color.white, Vector2.zero, new Vector2(ancho, AltoNombre), fuente);
        DesdeElTecho((RectTransform)nombre.transform, 0f);
        if (materialContorno != null) nombre.fontSharedMaterial = materialContorno;

        var marco = ConstructorUI.Rect(raiz, "Marco", Vector2.zero, new Vector2(ancho, alto));
        DesdeElTecho(marco, AltoNombre + SeparacionNombreMarco);
        var imgMarco = marco.gameObject.AddComponent<Image>();
        ConstructorUI.Redondear(imgMarco, pildora, 8f);
        imgMarco.color = colorFondo;
        imgMarco.raycastTarget = false;

        golpe = Relleno(marco, "Golpe", colorGolpe);
        relleno = Relleno(marco, "Relleno", colorVida);
        imagenRelleno = relleno.GetComponent<Image>();
        grupo = raiz.gameObject.AddComponent<CanvasGroup>();

        // Donde entra en furia: la ubica UbicarMuesca con cada jefe nuevo.
        muesca = ConstructorUI.Rect(marco, "Muesca", Vector2.zero, new Vector2(4f, alto));
        var imgMuesca = muesca.gameObject.AddComponent<Image>();
        imgMuesca.color = colorMuesca;
        imgMuesca.raycastTarget = false;
    }

    // Cuelga un hijo del techo de su padre, a `y` de distancia: asi donde queda no
    // depende de cuanto mida el padre.
    private static void DesdeElTecho(RectTransform rt, float y)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -y);
    }

    private RectTransform Relleno(RectTransform marco, string nombreHijo, Color color)
    {
        var rt = ConstructorUI.Estirar(marco, nombreHijo);
        rt.anchorMax = new Vector2(0f, 1f);
        var img = rt.gameObject.AddComponent<Image>();
        ConstructorUI.Redondear(img, pildora, 8f);
        img.color = color;
        img.raycastTarget = false;
        return rt;
    }

    private void Update()
    {
        float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
        var actual = Elegir();

        if (actual != null)
        {
            // La vida definitiva la pone quien lo saca, en el frame siguiente al que
            // aparece: preguntarla antes la fijaria sin los multiplicadores de la oleada.
            // Solo se saltea la lectura, no el resto: cortar el Update entero dejaba la
            // barra congelada un segundo cada vez que cambiaba el jefe que se muestra.
            if (!actual.VidaEmpezada && Time.time - desdeQueAparecio < esperaAlAparecer) return;

            float maxima = actual.VidaMaxima;
            fraccion = maxima > 0f ? Mathf.Clamp01(actual.VidaActual / maxima) : 0f;
            nombre.text = Nombre(actual);
            visible = Mathf.MoveTowards(visible, 1f, dt / 0.3f);
        }
        else
        {
            visible = Mathf.MoveTowards(visible, 0f, dt / 0.25f);
        }

        if (visible <= 0f)
        {
            if (raiz.gameObject.activeSelf) raiz.gameObject.SetActive(false);
            fraccionGolpe = fraccion = 1f;
            return;
        }
        if (!raiz.gameObject.activeSelf) raiz.gameObject.SetActive(true);

        // La barra blanca de atras alcanza a la roja, nunca al reves.
        fraccionGolpe = fraccionGolpe > fraccion
            ? Mathf.Max(fraccion, fraccionGolpe - velocidadDelGolpe * dt)
            : fraccion;

        relleno.anchorMax = new Vector2(fraccion, 1f);
        golpe.anchorMax = new Vector2(fraccionGolpe, 1f);

        // Entra deslizando desde arriba y se va igual.
        float suave = CurvasUI.SalidaAtras(visible);
        raiz.anchoredPosition = new Vector2(0f, YEnReposo + (1f - suave) * 90f);
        grupo.alpha = visible;

        // Con la furia del jefe la barra late. Con la suya (JefePatrones.EnFuria) y no con
        // la mitad fija: si se balanceaba fraccionFuria en el prefab, la barra latia donde
        // el jefe todavia no habia cambiado de patron.
        if (patrones != null && patrones.EnFuria)
        {
            float latido = 0.5f + 0.5f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 5f));
            imagenRelleno.color = Color.Lerp(colorVida, colorFuria, latido);
        }
        else
        {
            imagenRelleno.color = colorVida;
        }
    }

    // El jefe que se muestra: el de menos vida, que es al que le estas pegando. En las
    // oleadas hay uno solo; en el modo libre puede haber otro dando vueltas.
    private EnemyController Elegir()
    {
        var jefes = EnemyController.Jefes;
        EnemyController mejor = null;
        float menos = float.MaxValue;
        for (int i = 0; i < jefes.Count; i++)
        {
            var candidato = jefes[i];
            if (candidato == null) continue;
            float vida = candidato.VidaEmpezada ? candidato.VidaActual : float.MaxValue - 1f;
            if (vida >= menos) continue;
            menos = vida;
            mejor = candidato;
        }

        if (mejor != jefe || (mejor != null && mejor.NumeroDeAparicion != numeroDelJefe))
        {
            jefe = mejor;
            numeroDelJefe = mejor != null ? mejor.NumeroDeAparicion : 0;
            desdeQueAparecio = Time.time;
            if (mejor != null)
            {
                fraccionGolpe = fraccion = 1f;
                patrones = mejor.GetComponent<JefePatrones>();
                UbicarMuesca();
            }
            else
            {
                // Se fue el que se mostraba: murio, o cayo por el kill-Z. La roja se vacia y
                // la blanca baja sola mientras la barra se va. Hasta el 24/9 las dos volvian
                // a 1 aca, y en el tiro que lo mataba la barra se rellenaba entera al irse:
                // parecia que el jefe se curaba. A 1 vuelven cuando termina de irse (Update)
                // o con el jefe siguiente.
                fraccion = 0f;
            }
        }
        return jefe;
    }

    // La muesca va donde entra en furia (JefePatrones.fraccionFuria), no en la mitad fija:
    // si se balancea la furia en el prefab, la barra sigue diciendo la verdad. Un jefe sin
    // patrones no tiene furia, y no lleva muesca.
    private void UbicarMuesca()
    {
        if (muesca == null) return;
        muesca.gameObject.SetActive(patrones != null);
        if (patrones == null) return;
        muesca.anchorMin = muesca.anchorMax = new Vector2(Mathf.Clamp01(patrones.fraccionFuria), 0.5f);
        muesca.anchoredPosition = Vector2.zero;
    }

    private static string Nombre(EnemyController zombi)
    {
        return zombi.enemyType != null ? Bestiario.Nombre(zombi.enemyType.name) : "";
    }
}
