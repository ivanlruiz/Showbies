using TMPro;
using UnityEngine;
using UnityEngine.UI;

// La ventanita de "¿SALIR DEL JUEGO?" del menu. El boton SALIR cerraba el juego en el
// acto, y esta en el medio del menu, justo debajo de MEJORAS: un toque de mas y afuera.
// Ahora pregunta, con SEGUIR JUGANDO grande, verde y latiendo (lo que se espera que se
// toque) y SALIR chico y gris. La abren el boton SALIR (MainMenu.QuitGame) y el atras de
// Android en el menu principal (BotonAtrasMenu, que tambien la cierra).
//
// No tiene objetos propios en la escena: al arrancar copia la ventana del idioma
// (SelectorIdioma), como OpcionesSonido, y cambia los botones de idioma por el aviso y los
// dos suyos, hechos con el boton VOLVER de la copia. Vive en la raiz del canvas "Main Menu".
public class ConfirmarSalir : MonoBehaviour
{
    public SelectorIdioma selectorIdioma;
    public Sprite iconoSeguir;
    public Sprite iconoSalir;

    [Tooltip("Los de PLAY: el verde es lo que te devuelve al juego.")]
    public Color colorSeguir = new Color(0.49f, 0.878f, 0.29f, 1f);
    public Color colorTextoSeguir = new Color(0.063f, 0.141f, 0.055f, 1f);
    public Color colorAviso = new Color(0.14f, 0.1f, 0.05f, 1f);
    public float duracionRebote = 0.3f;

    private GameObject panel;
    private RectTransform ventana;
    private float abiertaDesde = -1f;

    public bool Abierta
    {
        get { return panel != null && panel.activeSelf; }
    }

    // Start y no Awake, como OpcionesSonido: la ventana del idioma termina de armarse en el
    // Awake de SelectorIdioma.
    private void Start()
    {
        if (selectorIdioma == null || selectorIdioma.panel == null || selectorIdioma.ventana == null
            || selectorIdioma.botonVolver == null) return;
        Armar();
    }

    private void Armar()
    {
        var original = (RectTransform)selectorIdioma.panel.transform;
        panel = Instantiate(selectorIdioma.panel, original.parent);
        panel.name = "PanelSalir";
        panel.SetActive(false);
        ventana = (RectTransform)panel.transform.Find(Ruta(selectorIdioma.ventana, original));
        // Mas ancha que la del idioma: los botones del menu miden 700 y asomaban a los costados.
        ventana.sizeDelta = new Vector2(760f, ventana.sizeDelta.y);

        TMP_Text titulo = null;
        foreach (var t in panel.GetComponentsInChildren<TextoTraducido>(true))
        {
            if (t.id != "idioma_titulo") continue;
            t.id = "salir_titulo";
            titulo = t.GetComponent<TMP_Text>();
        }

        foreach (var boton in selectorIdioma.botonesIdioma)
        {
            if (boton == null) continue;
            var copia = panel.transform.Find(Ruta(boton.transform, original));
            if (copia != null) Destroy(copia.gameObject);
        }

        // SALIR es el VOLVER de la copia con otro icono y otro texto; SEGUIR JUGANDO, una
        // copia suya mas grande y verde, arriba.
        var salir = (RectTransform)panel.transform.Find(Ruta(selectorIdioma.botonVolver.transform, original));
        var seguir = (RectTransform)Instantiate(salir.gameObject, salir.parent).transform;
        salir.name = "BotonSalir";
        seguir.name = "BotonSeguir";

        // Los ids se escriben enteros (x.id = "...";) para que la prueba de idiomas los encuentre.
        var traducidoSalir = Vestir(salir, iconoSalir, Salir);
        if (traducidoSalir != null) traducidoSalir.id = "menu_salir";
        salir.anchoredPosition = new Vector2(0f, -158f);

        var traducidoSeguir = Vestir(seguir, iconoSeguir, Cerrar);
        TMP_Text textoSeguir = null;
        if (traducidoSeguir != null)
        {
            traducidoSeguir.id = "salir_seguir";
            textoSeguir = traducidoSeguir.GetComponent<TMP_Text>();
            textoSeguir.color = colorTextoSeguir;
            textoSeguir.fontSize = 50f;
        }
        seguir.sizeDelta = new Vector2(440f, 100f);
        seguir.anchoredPosition = new Vector2(0f, -18f);
        var fondo = seguir.Find("Visual/Fondo");
        if (fondo != null)
        {
            // Viene del boton VOLVER, que es de vidrio y sigue al tema; este es verde
            // siempre, asi que se le saca el pintor.
            var pintor = fondo.GetComponent<PintarConTema>();
            if (pintor != null) Destroy(pintor);
            fondo.GetComponent<Image>().color = colorSeguir;
        }
        var icono = seguir.GetComponentInChildren<IconoDeBoton>(true);
        if (icono != null)
        {
            // El icono mide 0,42 del alto de la pildora, como en todos los botones.
            ((RectTransform)icono.transform).sizeDelta = new Vector2(42f, 42f);
            icono.GetComponent<Image>().color = colorTextoSeguir;
        }
        var jugoso = seguir.GetComponent<BotonJugoso>();
        if (jugoso != null) jugoso.respirar = true;

        // El aviso va entre el titulo y los botones, oscuro y sin contorno sobre la crema,
        // con el material del texto de los botones.
        if (titulo != null)
        {
            var aviso = Instantiate(titulo.gameObject, titulo.transform.parent);
            aviso.name = "Aviso";
            aviso.GetComponent<TextoTraducido>().id = "salir_aviso";
            var texto = aviso.GetComponent<TMP_Text>();
            if (textoSeguir != null) texto.fontSharedMaterial = textoSeguir.fontSharedMaterial;
            texto.color = Tema.Elegir(colorAviso, RolDeTema.Texto);
            // La ventana se arma una sola vez y se abre cuando sea: el componente la
            // repinta sola si el jugador cambio el tema mientras tanto.
            Tema.Pintar(texto, RolDeTema.Texto, colorAviso);
            texto.enableVertexGradient = false;
            texto.enableAutoSizing = false;
            texto.fontSize = 36f;
            texto.textWrappingMode = TextWrappingModes.Normal;
            texto.rectTransform.sizeDelta = new Vector2(560f, 80f);
            texto.rectTransform.anchoredPosition = new Vector2(0f, 100f);
        }
    }

    // Le cambia al boton el icono y lo que hace, y devuelve su texto para cambiarle el id.
    // Un evento nuevo y no RemoveAllListeners: ese no saca los que vienen puestos desde el
    // inspector.
    private static TextoTraducido Vestir(RectTransform boton, Sprite icono, UnityEngine.Events.UnityAction alTocar)
    {
        var button = boton.GetComponent<Button>();
        button.onClick = new Button.ButtonClickedEvent();
        button.onClick.AddListener(alTocar);

        var iconoDeBoton = boton.GetComponentInChildren<IconoDeBoton>(true);
        if (iconoDeBoton != null && icono != null) iconoDeBoton.GetComponent<Image>().sprite = icono;

        return boton.GetComponentInChildren<TextoTraducido>(true);
    }

    // Devuelve si la abrio: si no se pudo armar, quien la pide cierra el juego como antes.
    public bool Abrir()
    {
        if (panel == null) return false;
        panel.SetActive(true);
        abiertaDesde = Time.unscaledTime;
        if (ventana != null) ventana.localScale = Vector3.zero;
        return true;
    }

    public void Cerrar()
    {
        if (panel != null) panel.SetActive(false);
        abiertaDesde = -1f;
    }

    // En el editor no hace nada, como antes el boton SALIR.
    private void Salir()
    {
        Application.Quit();
    }

    private void Update()
    {
        if (abiertaDesde < 0f || ventana == null) return;
        float t = duracionRebote > 0f ? Mathf.Clamp01((Time.unscaledTime - abiertaDesde) / duracionRebote) : 1f;
        ventana.localScale = Vector3.one * CurvasUI.SalidaAtras(t);
        if (t >= 1f) abiertaDesde = -1f;
    }

    // La ruta de un hijo desde un ancestro, para encontrar lo mismo en la copia.
    private static string Ruta(Transform hijo, Transform ancestro)
    {
        string ruta = hijo.name;
        var t = hijo.parent;
        while (t != null && t != ancestro)
        {
            ruta = t.name + "/" + ruta;
            t = t.parent;
        }
        return ruta;
    }
}
