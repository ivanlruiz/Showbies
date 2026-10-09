using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// El cartel del menu con lo que se cobro solo (CobrosSolos, mejora 5 de la revision del
// 9/10): "¡PREMIOS COBRADOS!", "los habias dejado sin cobrar" y un renglon por cierre (las
// misiones del dia, el desafio de la semana, Halloween), con el arpegio de cobrar una
// mision. Entra con un rebote arriba al centro, debajo de la fila de botones redondos de las
// esquinas (el globo, el engranaje y el nivel a la izquierda; las misiones, el bestiario, la
// medalla y Discord a la derecha: llegan a 132 del borde, y el cartel, a esa altura, los
// pisaba) y encima de MEJORAS, se queda unos segundos y se va solo, o al tocarlo: no es una
// ventana mas para cerrar. Su canvas escala por el alto y el del menu por el ancho: en una
// pantalla mas ancha que 16:9 los botones del menu crecen y el cartel no, y asi entra entre
// los dos en 16:9, 20:9 y 21:9 (con k el ancho relativo a 16:9, las esquinas llegan a 132k
// del borde y MEJORAS empieza a 540 - 50k; el cartel va de 180 a 320 + 48 por cobro).
// Espera a que
// se vaya la recompensa diaria y el cartel de consentimiento, y mientras esta, la reseña no
// se pide (PedidoDeResena).
//
// Nada de esto esta en la escena: se instala solo al cargar el menu y toma la fuente, la
// pildora, los colores y los sonidos de VentanaMisiones. Va en un canvas propio por encima
// de la tienda: si la tienda se abrio sola (MEJORAS de la derrota), es ahi donde se ven las
// monedas que subieron.
public class CartelDeCobros : MonoBehaviour
{
    public const float Espera = 0.8f;       // desde que se cargo el menu
    public const float Duracion = 5f;
    public const int OrdenDelCanvas = 30;   // la tienda va en 5
    public const float DesdeArriba = 180f;

    // El arpegio de cobrar una mision (VentanaMisiones).
    private static readonly int[] SemitonosFestejo = { -12, -8, -5, 0, 4, 7, 12 };

    public static bool Abierto { get; private set; }

    private VentanaMisiones misiones;
    private GameObject lienzo;
    private RectTransform cartel;
    private float desde;
    private float cerrandoDesde = -1f;
    private float cargadoEn;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetearEstadoCompartido()
    {
        Abierto = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Engancharse()
    {
        // Sin recargar el dominio al entrar en play, el static sigue suscripto: sin duplicar.
        SceneManager.sceneLoaded -= AlCargar;
        SceneManager.sceneLoaded += AlCargar;
    }

    private static void AlCargar(Scene escena, LoadSceneMode modo)
    {
        if (escena.buildIndex != TiendaMejoras.EscenaMenu) return;
        var go = new GameObject("CartelDeCobros");
        SceneManager.MoveGameObjectToScene(go, escena);
        go.AddComponent<CartelDeCobros>();
    }

    private void Start()
    {
        cargadoEn = Time.unscaledTime;
        misiones = FindAnyObjectByType<VentanaMisiones>(FindObjectsInactive.Include);
        if (misiones == null) enabled = false;
    }

    private void OnDestroy()
    {
        if (cartel != null) Abierto = false;
    }

    private void Update()
    {
        float t = Time.unscaledTime;
        if (cartel == null)
        {
            if (!CobrosSolos.Hay || t - cargadoEn < Espera) return;
            if (VentanaRecompensaDiaria.Abierta || VentanaRecompensaDiaria.Ocupada || ServicioAnuncios.ConsentimientoEnPantalla) return;
            Armar(CobrosSolos.Tomar());
            return;
        }

        float edad = t - desde;
        if (cerrandoDesde < 0f && edad >= Duracion) cerrandoDesde = t;
        float entrada = CurvasUI.SalidaAtras(Mathf.Clamp01(edad / 0.4f));
        float salida = cerrandoDesde < 0f ? 0f : Mathf.Clamp01((t - cerrandoDesde) / 0.3f);
        cartel.localScale = Vector3.one * entrada * (1f - salida);
        if (salida >= 1f) Quitar();
    }

    private void Armar(List<CobrosSolos.Cobro> cobros)
    {
        if (cobros.Count == 0) return;

        lienzo = new GameObject("Lienzo", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        lienzo.transform.SetParent(transform, false);
        var canvas = lienzo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = OrdenDelCanvas;
        var escala = lienzo.GetComponent<CanvasScaler>();
        escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escala.referenceResolution = new Vector2(1920f, 1080f);
        escala.matchWidthOrHeight = 1f;

        float alto = AltoDelCartel(cobros.Count);
        cartel = ConstructorUI.Rect((RectTransform)lienzo.transform, "Cartel", Vector2.zero, new Vector2(1000f, alto));
        cartel.anchorMin = cartel.anchorMax = new Vector2(0.5f, 1f);
        cartel.pivot = new Vector2(0.5f, 1f);
        cartel.anchoredPosition = new Vector2(0f, -DesdeArriba);
        cartel.localScale = Vector3.zero;
        var fondo = cartel.gameObject.AddComponent<Image>();
        ConstructorUI.VentanaNeon(cartel, fondo, misiones.pildora);
        fondo.color = Tema.Elegir(misiones.colorVentana, RolDeTema.Panel);
        var boton = cartel.gameObject.AddComponent<Button>();
        boton.transition = Selectable.Transition.None;
        boton.targetGraphic = fondo;
        boton.onClick.AddListener(Cerrar);

        float y = alto / 2f - 46f;
        var titulo = ConstructorUI.Texto(cartel, "Titulo", Textos.De("cobro_solo_titulo"), 54f, misiones.colorTitulo,
                                         new Vector2(0f, y), new Vector2(940f, 64f), misiones.fuente);
        if (misiones.materialContorno != null) titulo.fontSharedMaterial = misiones.materialContorno;
        y -= 44f;
        ConstructorUI.Texto(cartel, "Detalle", Textos.De("cobro_solo_detalle"), 28f,
                            Tema.Elegir(misiones.colorTextoOscuro, RolDeTema.TextoSuave),
                            new Vector2(0f, y), new Vector2(940f, 36f), misiones.fuente);
        y -= RenglonDelCobro;
        for (int i = 0; i < cobros.Count; i++)
        {
            ConstructorUI.Texto(cartel, "Cobro" + i, CobrosSolos.Renglon(cobros[i]), 38f, ConstructorUI.Amarillo,
                                new Vector2(0f, y - RenglonDelCobro * i), new Vector2(940f, 46f), misiones.fuente);
        }

        desde = Time.unscaledTime;
        cerrandoDesde = -1f;
        Abierto = true;
        Festejar();
    }

    // El titulo, el detalle y un renglon por cobro, con el margen del borde.
    public const float RenglonDelCobro = 48f;

    public static float AltoDelCartel(int cobros)
    {
        return 140f + RenglonDelCobro * cobros;
    }

    private void Festejar()
    {
        for (int k = 0; k < SemitonosFestejo.Length; k++)
            Sonidos.Programar(misiones.nota, 0.05 * k, 0.8f, Sonidos.PitchDe(SemitonosFestejo[k]));
        if (misiones.sonidoFestejo != null) Sonidos.Tocar(misiones.sonidoFestejo, 0.7f);
    }

    private void Cerrar()
    {
        if (cerrandoDesde < 0f) cerrandoDesde = Time.unscaledTime;
    }

    private void Quitar()
    {
        if (lienzo != null) Destroy(lienzo);
        lienzo = null;
        cartel = null;
        Abierto = false;
    }
}
