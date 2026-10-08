using TMPro;
using UnityEngine;
using UnityEngine.UI;

// El engranaje del menu y la ventana de opciones que abre: los volumenes de efectos y
// de musica y MOSTRAR FPS (el contador del HUD, ContadorFps; desde el 6/10). Hasta el
// 24/9 tenia tambien el modo oscuro, que se fue cuando el neon paso a ser el unico tema
// (ver Tema), y el interruptor de los FPS ocupa su lugar. Con anuncios de AdMob suma
// PRIVACIDAD, para cambiar el consentimiento de Europa, solo cuando UMP lo pide, y el
// interruptor del SOMBRERO DE CALABAZA, solo si ya se gano (EventoHalloween). No tiene
// objetos propios en la escena: al arrancar copia el globo y la ventana del idioma
// (SelectorIdioma) y los adapta, asi se ven igual sin mantener dos copias a mano. El
// engranaje queda a la derecha del globo.
//
// Se llamaba "sonido" cuando solo tenia los dos volumenes; el nombre de la clase quedo
// porque es lo que esta cableado en la escena.
//
// Vive en la raiz del canvas "Main Menu", junto a SelectorIdioma. El atras de
// Android la cierra desde BotonAtrasMenu.
public class OpcionesSonido : MonoBehaviour
{
    public SelectorIdioma selectorIdioma;
    public float separacionDelGlobo = 24f;
    public float duracionRebote = 0.3f;

    [Tooltip("El texto de los controles sobre la ventana crema; con el tema oscuro lo cambia Tema.")]
    public Color colorTexto = new Color(0.16f, 0.14f, 0.2f, 1f);

    // La del idioma mide 620 x 480 y tiene dos botones; esta tiene tres controles, y dos
    // que pueden estar o no: el SOMBRERO, si se gano, y PRIVACIDAD, si UMP lo pide. Con
    // cada uno la ventana crece y todo sube (Acomodar); si no entra, se achica (EscalaQueEntra).
    private const float AltoVentana = 640f;
    private const float AnchoControl = 500f;
    private const float YTitulo = 255f;
    private const float YEfectos = 140f;
    private const float YMusica = 25f;
    private const float YFps = -95f;
    private const float YVolver = -255f;
    private const float DePrivacidadAlFps = 135f;
    private const float CrecidaConPrivacidad = 130f;
    private const float DelSombreroAlFps = 120f;

    private GameObject panel;
    private RectTransform ventana;
    private RectTransform titulo;
    private RectTransform controlEfectos;
    private RectTransform controlMusica;
    private RectTransform controlFps;
    private RectTransform botonVolver;
    private RectTransform botonPrivacidad;
    private RectTransform controlSombrero;
    private int acomodada = -1;   // -1 sin acomodar; si no, 1 con PRIVACIDAD + 2 con el SOMBRERO
    private Texture2D texturaEngranaje;
    private Texture2D texturaPerilla;
    private Sprite spriteEngranaje;
    private Sprite spritePerilla;
    private float abiertaDesde = -1f;

    public bool Abierto
    {
        get { return panel != null && panel.activeSelf; }
    }

    // Start y no Awake: SelectorIdioma pone los dibujos del globo en su Awake, y la
    // copia los tiene que traer puestos.
    private void Start()
    {
        if (selectorIdioma == null || selectorIdioma.botonGlobo == null || selectorIdioma.panel == null) return;

        texturaEngranaje = TexturasUI.Engranaje(128);
        texturaPerilla = TexturasUI.Circulo(64);
        // Esta clase es duenia de las texturas y de sus sprites, y destruye las dos cosas.
        spriteEngranaje = Sprite.Create(texturaEngranaje, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f));
        spritePerilla = Sprite.Create(texturaPerilla, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));

        CrearEngranaje();
        CrearVentana();
    }

    private void OnDestroy()
    {
        if (spriteEngranaje != null) Destroy(spriteEngranaje);
        if (spritePerilla != null) Destroy(spritePerilla);
        if (texturaEngranaje != null) Destroy(texturaEngranaje);
        if (texturaPerilla != null) Destroy(texturaPerilla);
    }

    private void CrearEngranaje()
    {
        var globo = (RectTransform)selectorIdioma.botonGlobo.transform;
        var copia = Instantiate(globo.gameObject, globo.parent);
        copia.name = "BotonSonido";
        var rt = (RectTransform)copia.transform;
        rt.anchoredPosition = globo.anchoredPosition + new Vector2(globo.rect.width + separacionDelGlobo, 0f);

        if (selectorIdioma.iconoGlobo != null)
        {
            var icono = copia.transform.Find(Ruta(selectorIdioma.iconoGlobo.transform, globo));
            var imagen = icono != null ? icono.GetComponent<Image>() : null;
            if (imagen != null) imagen.sprite = spriteEngranaje;
        }

        var boton = copia.GetComponent<Button>();
        boton.onClick.RemoveAllListeners();
        boton.onClick.AddListener(Abrir);
    }

    private void CrearVentana()
    {
        var original = selectorIdioma.panel;
        panel = Instantiate(original, original.transform.parent);
        panel.name = "PanelSonido";
        panel.SetActive(false);

        var ventanaOriginal = selectorIdioma.ventana;
        ventana = (RectTransform)panel.transform.Find(Ruta(ventanaOriginal, (RectTransform)original.transform));
        // Mas alta que la del idioma: entran tres controles en vez de dos botones.
        ventana.sizeDelta = new Vector2(ventana.sizeDelta.x, AltoVentana);

        // El titulo pasa a OPCIONES y sube, que la ventana creció (Acomodar).
        foreach (var t in panel.GetComponentsInChildren<TextoTraducido>(true))
        {
            if (t.id != "idioma_titulo") continue;
            t.id = "opciones_titulo";
            titulo = (RectTransform)t.transform;
        }

        // Los botones de idioma se van; en su lugar, los volumenes y MOSTRAR FPS. De paso,
        // de uno salen la pildora y la fuente con que se arman los controles.
        TMP_FontAsset fuente = null;
        Sprite pildora = null;
        var botones = selectorIdioma.botonesIdioma;
        for (int i = 0; i < botones.Length; i++)
        {
            if (botones[i] == null) continue;
            var copiaBoton = panel.transform.Find(Ruta(botones[i].transform, (RectTransform)original.transform));
            if (copiaBoton == null) continue;
            var texto = copiaBoton.GetComponentInChildren<TMP_Text>(true);
            if (fuente == null && texto != null) fuente = texto.font;
            var fondo = copiaBoton.Find("Visual/Fondo");
            var imagen = fondo != null ? fondo.GetComponent<Image>() : null;
            if (pildora == null && imagen != null) pildora = imagen.sprite;
            Destroy(copiaBoton.gameObject);
        }

        var volver = selectorIdioma.botonVolver != null
            ? panel.transform.Find(Ruta(selectorIdioma.botonVolver.transform, (RectTransform)original.transform))
            : null;
        AudioClip sonidoClick = null;
        if (volver != null)
        {
            var jugoso = volver.GetComponent<BotonJugoso>();
            if (jugoso != null) sonidoClick = jugoso.sonidoClick;
            var boton = volver.GetComponent<Button>();
            if (boton != null)
            {
                boton.onClick.RemoveAllListeners();
                boton.onClick.AddListener(Cerrar);
            }
            botonVolver = (RectTransform)volver;
        }

        // El texto y el surco van con el tema, con el pintor puesto: como el resto de la
        // ventana, que es copia de la del idioma.
        Color colorDelTexto = Tema.Elegir(colorTexto, RolDeTema.Texto);
        Color colorDelSurco = Tema.Elegir(SliderVolumen.ColorBarra, RolDeTema.Surco);
        // El de efectos suena al moverlo (FijarEfectosConMuestra).
        controlEfectos = RectDe(SeguirElTema(SliderVolumen.Crear(ventana, "sonido_efectos", Vector2.zero, AnchoControl, fuente,
                                           Volumen.Efectos, SliderVolumen.FijarEfectosConMuestra, spritePerilla, colorDelTexto, colorDelSurco)));
        controlMusica = RectDe(SeguirElTema(SliderVolumen.Crear(ventana, "sonido_musica", Vector2.zero, AnchoControl, fuente,
                                          Volumen.Musica, Volumen.FijarMusica, spritePerilla, colorDelTexto, colorDelSurco)));
        // Se escribe a disco al cerrar la ventana, con los volumenes (Volumen.Guardar).
        controlFps = RectDe(SeguirElTema(Interruptor.Crear(ventana, "opciones_fps", Vector2.zero, AnchoControl, fuente,
                                       () => ContadorFps.Mostrar, ContadorFps.FijarMostrar, pildora, spritePerilla, sonidoClick, colorDelTexto)));

        // El sombrero de calabaza, el premio de Halloween: se arma siempre y se muestra si se
        // gano. Se guarda con los volumenes, como MOSTRAR FPS (es un PlayerPrefs).
        controlSombrero = RectDe(SeguirElTema(Interruptor.Crear(ventana, "opciones_sombrero", Vector2.zero, AnchoControl, fuente,
                                            () => EventoHalloween.SombreroPuesto, EventoHalloween.PonerSombrero, pildora, spritePerilla, sonidoClick, colorDelTexto)));

        CrearPrivacidad(fuente, pildora, sonidoClick);
        Acomodar(false, false);
    }

    // PRIVACIDAD: el cartel de consentimiento de Europa para cambiar lo que se eligio (lo
    // pide la politica de Google). Solo si UMP lo pide (ServicioAnuncios.PrivacidadRequerida),
    // y eso se sabe recien cuando contesta, despues de armada la ventana: se acomoda al
    // abrirla. Es un boton de vidrio como VOLVER, y toma de el el color, el tamanio y la letra.
    private void CrearPrivacidad(TMP_FontAsset fuente, Sprite pildora, AudioClip sonidoClick)
    {
        if (botonVolver == null) return;
        var fondoVolver = botonVolver.Find("Visual/Fondo");
        var imgVolver = fondoVolver != null ? fondoVolver.GetComponent<Image>() : null;
        var textoVolver = botonVolver.GetComponentInChildren<TMP_Text>(true);
        Color color = imgVolver != null ? imgVolver.color : Tema.Elegir(Color.gray, RolDeTema.Vidrio);
        Color colorDelTexto = textoVolver != null ? textoVolver.color : Color.white;
        float tamanioTexto = textoVolver != null ? textoVolver.fontSize : 50f;
        if (pildora == null && imgVolver != null) pildora = imgVolver.sprite;

        var boton = ConstructorUI.Boton(ventana, "BotonPrivacidad", Vector2.zero, botonVolver.sizeDelta, color, colorDelTexto,
                                        null, "", tamanioTexto, fuente, pildora, sonidoClick);
        boton.onClick.AddListener(ServicioAnuncios.MostrarPrivacidad);
        // Apagado mientras se le pone el id: TextoTraducido escribe en OnEnable, y sin id
        // mostraria "[]". Asi tambien cambia con el idioma, como los otros controles.
        var texto = boton.GetComponentInChildren<TMP_Text>(true);
        if (texto != null)
        {
            texto.gameObject.SetActive(false);
            var traducido = texto.gameObject.AddComponent<TextoTraducido>();
            traducido.id = "opciones_privacidad";
            texto.gameObject.SetActive(true);
        }
        botonPrivacidad = (RectTransform)boton.transform;
    }

    // Sin los dos que pueden faltar, los tres controles de siempre; con cada uno, la ventana
    // crece y todo sube para hacerle lugar entre MOSTRAR FPS y VOLVER: primero el SOMBRERO y
    // abajo PRIVACIDAD.
    private void Acomodar(bool conPrivacidad, bool conSombrero)
    {
        int cual = (conPrivacidad ? 1 : 0) + (conSombrero ? 2 : 0);
        if (cual == acomodada || ventana == null) return;
        acomodada = cual;

        float crecida = (conPrivacidad ? CrecidaConPrivacidad : 0f) + (conSombrero ? DelSombreroAlFps : 0f);
        float subir = crecida * 0.5f;
        ventana.sizeDelta = new Vector2(ventana.sizeDelta.x, AltoVentana + crecida);
        Poner(titulo, YTitulo + subir);
        Poner(controlEfectos, YEfectos + subir);
        Poner(controlMusica, YMusica + subir);
        Poner(controlFps, YFps + subir);
        Poner(botonVolver, YVolver - subir);
        float y = YFps + subir;
        if (controlSombrero != null)
        {
            if (conSombrero) y -= DelSombreroAlFps;
            Poner(controlSombrero, y);
            controlSombrero.gameObject.SetActive(conSombrero);
        }
        if (botonPrivacidad != null)
        {
            Poner(botonPrivacidad, y - DePrivacidadAlFps);
            botonPrivacidad.gameObject.SetActive(conPrivacidad);
        }
    }

    // Con los dos controles de mas mide 890 de alto, mas el halo: en 20:9 el canvas del menu
    // mide 864 y en 21:9, 823. Como las ventanas de las misiones, se achica lo justo.
    private float EscalaQueEntra()
    {
        float alto = ((RectTransform)panel.transform).rect.height;
        if (alto <= 0f || ventana == null) return 1f;
        float lugar = alto * 0.5f - Mathf.Abs(ventana.anchoredPosition.y) - 10f;
        float mitad = ventana.rect.height * 0.5f + 40f;
        return Mathf.Clamp(lugar / mitad, 0.5f, 1f);
    }

    private static void Poner(RectTransform rt, float y)
    {
        if (rt != null) rt.anchoredPosition = new Vector2(0f, y);
    }

    private static RectTransform RectDe(Component control)
    {
        return control != null ? (RectTransform)control.transform : null;
    }

    // Le pone su papel a los textos y al surco de un control recien armado, para que
    // cambien en el acto si cambia el tema. Devuelve el mismo control.
    private Component SeguirElTema(Component control)
    {
        if (control == null) return null;
        foreach (var t in control.GetComponentsInChildren<TMP_Text>(true))
            Tema.Pintar(t, RolDeTema.Texto, colorTexto);
        var barra = control.transform.Find("Barra");
        if (barra != null) Tema.Pintar(barra.GetComponent<Image>(), RolDeTema.Surco, SliderVolumen.ColorBarra);
        return control;
    }

    public void Abrir()
    {
        if (panel == null) return;
        if (selectorIdioma != null) selectorIdioma.Cerrar();
        Acomodar(ServicioAnuncios.PrivacidadRequerida, EventoHalloween.TieneSombrero);
        panel.SetActive(true);
        abiertaDesde = Time.unscaledTime;
        if (ventana != null) ventana.localScale = Vector3.zero;
    }

    public void Cerrar()
    {
        if (panel != null) panel.SetActive(false);
        abiertaDesde = -1f;
        Volumen.Guardar();
    }

    private void Update()
    {
        if (abiertaDesde < 0f || ventana == null) return;
        float t = duracionRebote > 0f ? Mathf.Clamp01((Time.unscaledTime - abiertaDesde) / duracionRebote) : 1f;
        ventana.localScale = Vector3.one * (EscalaQueEntra() * CurvasUI.SalidaAtras(t));
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
