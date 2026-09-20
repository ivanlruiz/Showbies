using TMPro;
using UnityEngine;
using UnityEngine.UI;

// La barra de vida del jefe, grande y arriba de todo (pedido de Ivan, como la de los
// jefes de Minecraft): mientras hay un jefe vivo se ve su nombre, cuanta vida le queda y
// la muesca de la mitad, que es donde entra en furia. Pegarle 500 veces a un zombi grande
// sin saber cuanto falta no se siente un evento.
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
    public float desdeArriba = 100f;

    [Header("Colores")]
    public Color colorFondo = new Color(0f, 0f, 0f, 0.55f);
    public Color colorVida = new Color(0.87f, 0.18f, 0.2f, 1f);
    public Color colorFuria = new Color(1f, 0.45f, 0.1f, 1f);
    public Color colorGolpe = new Color(1f, 1f, 1f, 0.75f);
    public Color colorMuesca = new Color(1f, 1f, 1f, 0.5f);

    [Tooltip("Lo que tarda el jefe en tener su vida definitiva: quien lo saca le pone los multiplicadores en el mismo frame.")]
    public float esperaAlAparecer = 1f;
    public float velocidadDelGolpe = 0.4f;    // fracciones de barra por segundo

    private RectTransform raiz;
    private RectTransform relleno;
    private RectTransform golpe;
    private RectTransform muesca;
    private TMP_Text nombre;

    private EnemyController jefe;
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
        var padre = (RectTransform)transform;
        raiz = ConstructorUI.Rect(padre, "BarraDelJefe", Vector2.zero, new Vector2(ancho + 40f, alto + 66f));
        raiz.anchorMin = raiz.anchorMax = new Vector2(0.5f, 1f);
        raiz.pivot = new Vector2(0.5f, 1f);
        raiz.anchoredPosition = new Vector2(0f, -desdeArriba);

        nombre = ConstructorUI.Texto(raiz, "Nombre", "", 44f, Color.white, new Vector2(0f, -22f), new Vector2(ancho, 52f), fuente);
        if (materialContorno != null) nombre.fontSharedMaterial = materialContorno;

        var marco = ConstructorUI.Rect(raiz, "Marco", new Vector2(0f, -62f), new Vector2(ancho, alto));
        var imgMarco = marco.gameObject.AddComponent<Image>();
        ConstructorUI.Redondear(imgMarco, pildora, 8f);
        imgMarco.color = colorFondo;
        imgMarco.raycastTarget = false;

        golpe = Relleno(marco, "Golpe", colorGolpe);
        relleno = Relleno(marco, "Relleno", colorVida);

        // La mitad, que es donde entra en furia.
        muesca = ConstructorUI.Rect(marco, "Muesca", Vector2.zero, new Vector2(4f, alto));
        var imgMuesca = muesca.gameObject.AddComponent<Image>();
        imgMuesca.color = colorMuesca;
        imgMuesca.raycastTarget = false;
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
            if (Time.time - desdeQueAparecio < esperaAlAparecer) return;

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
        muesca.anchoredPosition = Vector2.zero;   // la mitad del marco

        // Entra deslizando desde arriba y se va igual.
        float suave = CurvasUI.SalidaAtras(visible);
        raiz.anchoredPosition = new Vector2(0f, -desdeArriba + (1f - suave) * 90f);
        var grupo = raiz.GetComponent<CanvasGroup>();
        if (grupo == null) grupo = raiz.gameObject.AddComponent<CanvasGroup>();
        grupo.alpha = visible;

        // Con la mitad de la vida el jefe entra en furia: la barra lo dice latiendo.
        var img = relleno.GetComponent<Image>();
        if (fraccion <= 0.5f)
        {
            float latido = 0.5f + 0.5f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 5f));
            img.color = Color.Lerp(colorVida, colorFuria, latido);
        }
        else
        {
            img.color = colorVida;
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
            fraccionGolpe = fraccion = 1f;
        }
        return jefe;
    }

    private static string Nombre(EnemyController zombi)
    {
        return zombi.enemyType != null ? Bestiario.Nombre(zombi.enemyType.name) : "";
    }
}
