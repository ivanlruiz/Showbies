using TMPro;
using UnityEngine;
using UnityEngine.UI;

// La ventana de la recompensa diaria (RecompensaDiaria): aparece sola al abrir el menu
// si hoy hay algo para cobrar. Siete casilleros con los dias de la racha (los cobrados
// en verde con su tilde, el de hoy dorado y latiendo), y un boton COBRAR +N. Al cobrar
// suena el arpegio de la tienda, el casillero de hoy se pone verde y la ventana se va.
//
// Despues de cobrar, si se puede ofrecer un video (LugarAnuncio.DuplicarRegalo, con las
// reglas de siempre: opt-in, el premio exacto escrito en el boton, cerrarlo antes no
// castiga), la ventana no se va: ofrece VIDEO: +N MAS al lado de VOLVER (pedido de Ivan:
// un boton de atras, no un NO, GRACIAS). Si no hay video, se va sola como siempre.
//
// Se arma entera en codigo, como OpcionesSonido: en la escena solo esta este componente
// (en la raiz del canvas "Main Menu") con los sprites, la fuente y los sonidos. El
// atras de Android la cierra sin cobrar (BotonAtrasMenu): vuelve a salir la proxima vez
// que se abra el menu en el dia.
public class VentanaRecompensaDiaria : MonoBehaviour
{
    public TMP_FontAsset fuente;
    public Material materialContorno;       // titulo sobre el crema
    public Sprite pildora;
    public Sprite tilde;
    public Sprite iconoAtras;
    public AudioClip nota;                  // moneda.wav
    public AudioClip sonidoFestejo;         // cartel.wav
    public AudioClip sonidoClick;           // el de los demas botones

    public Color colorVentana = new Color(1f, 0.96f, 0.86f, 1f);
    public Color colorTextoOscuro = new Color(0.16f, 0.14f, 0.2f, 1f);
    public Color colorCobrado = new Color(0.49f, 0.88f, 0.29f, 1f);
    public Color colorHoy = new Color(0.97f, 0.79f, 0.28f, 1f);
    public Color colorFuturo = new Color(0f, 0f, 0f, 0.08f);
    public Color colorMoneda = new Color(1f, 0.76f, 0.12f, 1f);
    public Color colorVideo = new Color(1f, 0.52f, 0.12f, 1f);
    public Color colorBordeMoneda = new Color(0.72f, 0.42f, 0.02f, 1f);
    public Color colorTilde = new Color(0.1f, 0.3f, 0.05f, 1f);

    // Los colores de arriba son los del tema claro, que es como se ve el juego desde
    // siempre; con el oscuro cada uno pasa a su papel (ver Tema).
    private Color ColorDeVentana { get { return Tema.Elegir(colorVentana, RolDeTema.Panel); } }
    private Color ColorDeTexto { get { return Tema.Elegir(colorTextoOscuro, RolDeTema.Texto); } }
    private Color ColorDeFuturo { get { return Tema.Elegir(colorFuturo, RolDeTema.Hueco); } }

    private static readonly int[] SemitonosFestejo = { -12, -8, -5, 0, 4, 7, 12 };
    private const float DuracionEntrada = 0.45f;
    private const float DuracionSalida = 0.25f;
    private const float EsperaTrasCobrar = 1.3f;

    public static bool Abierta { get; private set; }

    private GameObject panel;
    private RectTransform ventana;
    private RectTransform casilleroHoy;
    private Image fondoHoy;
    private Image tildeHoy;
    private TMP_Text titulo;
    private GameObject boton;
    private GameObject botonVideo;
    private GameObject botonAtras;
    private double montoHoy;
    private bool esperandoVideo;
    private bool cobrado;
    private float relojGolpe = -1f;       // tiempo desde el ultimo festejo, para el salto del casillero
    private Texture2D texturaMoneda;
    private Texture2D texturaClaqueta;
    private Texture2D texturaBorde;
    private Sprite spriteMoneda;
    private Sprite spriteClaqueta;
    private Sprite bordeMoneda;
    private int idiomaArmado = -1;
    private int temaArmado = -1;

    private int racha;
    private int diaDeLaVentana;       // el dia con el que se armo: se cobra ese, aunque pase la medianoche
    private bool pendiente;                 // armada, esperando a que se cierre la tienda
    private TiendaMejoras tienda;
    private float reloj = -1f;              // tiempo desde que se abrio
    private float relojSalida = -1f;        // tiempo desde que empezo a irse
    private float esperaParaIrse = -1f;     // tras cobrar, cuanto falta para empezar a irse

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reiniciar()
    {
        Abierta = false;
    }

    private void Start()
    {
        racha = RecompensaDiaria.RachaDeHoy;
        diaDeLaVentana = Progreso.DiaDeHoy();
        if (racha <= 0) return;
        // Espera a la primera partida terminada: si no, alguien que recien instala
        // cobra 150 monedas y compra antes de haber jugado, y la guia de la primera
        // compra llega tarde.
        if (PrimeraVez.NoTerminoPartidas) return;
        // Los dibujos se hacen una sola vez y no en Armar, que puede correr de nuevo si
        // cambia el idioma o el tema. Esta clase es duenia de las texturas y de sus
        // sprites, y destruye las dos cosas.
        texturaMoneda = TexturasUI.Circulo(64);
        spriteMoneda = Sprite.Create(texturaMoneda, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));
        texturaBorde = TexturasUI.Anillo(64, 0.2f);
        bordeMoneda = Sprite.Create(texturaBorde, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));
        texturaClaqueta = TexturasUI.Claqueta(128);
        spriteClaqueta = Sprite.Create(texturaClaqueta, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f));
        Armar();
        panel.SetActive(false);
        // No se abre en el acto: si el menu cargo con la tienda abierta (MEJORAS de la
        // derrota), la tienda tiene su propio canvas por encima y la diaria quedaria
        // escondida debajo. Se abre en el primer Update con la tienda cerrada.
        tienda = FindAnyObjectByType<TiendaMejoras>(FindObjectsInactive.Include);
        pendiente = true;
    }

    private void OnDestroy()
    {
        if (panel != null) Abierta = false;
        if (spriteMoneda != null) Destroy(spriteMoneda);
        if (spriteClaqueta != null) Destroy(spriteClaqueta);
        if (bordeMoneda != null) Destroy(bordeMoneda);
        if (texturaMoneda != null) Destroy(texturaMoneda);
        if (texturaClaqueta != null) Destroy(texturaClaqueta);
        if (texturaBorde != null) Destroy(texturaBorde);
    }

    private void Abrir()
    {
        // Los textos y los colores se escriben al armarla, como en misiones y bestiario:
        // si cambio el idioma o el tema, se vuelve a armar. Hoy no llega a pasar (se abre
        // sola al cargar el menu, antes de que se pueda tocar opciones), pero si algun dia
        // se puede volver a abrir, no tiene que salir con el tema viejo. Aca todavia no se
        // cobro nada: Abrir corre una sola vez, con `pendiente`.
        if (Idioma.Revision != idiomaArmado || Tema.Revision != temaArmado)
        {
            Destroy(panel);
            Armar();
        }
        panel.SetActive(true);
        panel.transform.SetAsLastSibling();
        Abierta = true;
        reloj = 0f;
        ventana.localScale = Vector3.zero;
    }

    public void Cerrar()
    {
        // Con un video en pantalla no se cierra: el premio tiene que encontrar la ventana.
        if (!Abierta || relojSalida >= 0f || esperandoVideo) return;
        relojSalida = 0f;
    }

    private void Cobrar()
    {
        if (esperaParaIrse >= 0f || relojSalida >= 0f || cobrado) return;
        // El dia de la ventana y no el de ahora: abierta a las 23:59 y cobrada a las 00:00,
        // cobraba con el dia nuevo, la racha volvia a 1 y el boton del video seguia
        // prometiendo el monto de la racha vieja. Lo del dia nuevo se cobra igual manana.
        double monto = RecompensaDiaria.CobrarEl(diaDeLaVentana);
        if (monto <= 0) { Cerrar(); return; }
        cobrado = true;
        montoHoy = monto;

        Festejar();
        if (fondoHoy != null) fondoHoy.color = colorCobrado;
        if (tildeHoy != null) tildeHoy.enabled = true;
        titulo.text = Textos.Formato("diaria_cobrado", FormatoNumeros.Compacto(monto));
        boton.SetActive(false);

        // Recien ahora la oferta del video; sin video, se va sola.
        if (ServicioAnuncios.PuedeOfrecer(LugarAnuncio.DuplicarRegalo)) MostrarOferta(true);
        else esperaParaIrse = EsperaTrasCobrar;
    }

    private void MostrarOferta(bool mostrar)
    {
        if (botonVideo != null) botonVideo.SetActive(mostrar);
        if (botonAtras != null) botonAtras.SetActive(mostrar);
    }

    private void PedirVideo()
    {
        if (relojSalida >= 0f || esperandoVideo) return;
        esperandoVideo = true;
        MostrarOferta(false);
        bool lanzado = ServicioAnuncios.Mostrar(LugarAnuncio.DuplicarRegalo, () =>
        {
            esperandoVideo = false;
            double extra = RecompensaDiaria.CobrarDuplicado();
            if (extra > 0)
            {
                Festejar();
                titulo.text = Textos.Formato("diaria_cobrado", FormatoNumeros.Compacto(montoHoy + extra));
            }
            esperaParaIrse = EsperaTrasCobrar;
        }, () =>
        {
            // Cerrarlo antes no castiga: vuelve la oferta si sigue en pie.
            esperandoVideo = false;
            SeguirTrasElVideo();
        });
        if (!lanzado)
        {
            esperandoVideo = false;
            SeguirTrasElVideo();
        }
    }

    private void SeguirTrasElVideo()
    {
        if (ServicioAnuncios.PuedeOfrecer(LugarAnuncio.DuplicarRegalo)) MostrarOferta(true);
        else esperaParaIrse = EsperaTrasCobrar;
    }

    private void Atras()
    {
        if (esperandoVideo) return;
        MostrarOferta(false);
        Cerrar();
    }

    private void Festejar()
    {
        for (int k = 0; k < SemitonosFestejo.Length; k++)
            Sonidos.Programar(nota, 0.05 * k, 0.8f, Sonidos.PitchDe(SemitonosFestejo[k]));
        Sonidos.Tocar(sonidoFestejo, 0.7f);
        relojGolpe = 0f;
    }

    private void Update()
    {
        if (pendiente && (tienda == null || !tienda.Abierta))
        {
            pendiente = false;
            Abrir();
        }
        if (!Abierta) return;
        float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);

        if (relojSalida >= 0f)
        {
            relojSalida += dt;
            float s = Mathf.Clamp01(relojSalida / DuracionSalida);
            ventana.localScale = Vector3.one * (1f - CurvasUI.SalidaCubica(s));
            if (s >= 1f)
            {
                panel.SetActive(false);
                Abierta = false;
                relojSalida = -1f;
            }
            return;
        }

        reloj += dt;
        float e = Mathf.Clamp01(reloj / DuracionEntrada);
        ventana.localScale = Vector3.one * CurvasUI.SalidaAtras(e);

        if (casilleroHoy != null)
        {
            // Late mientras espera el cobro; al cobrar da un salto y se queda quieto.
            if (relojGolpe >= 0f) relojGolpe += dt;
            float latido = cobrado
                ? (relojGolpe >= 0f ? 0.25f * CurvasUI.Campana(Mathf.Clamp01(relojGolpe / 0.4f)) : 0f)
                : 0.06f * Mathf.Sin(reloj * 6f);
            casilleroHoy.localScale = Vector3.one * (1.12f + latido);
        }

        if (esperaParaIrse >= 0f)
        {
            esperaParaIrse -= dt;
            if (esperaParaIrse <= 0f) { esperaParaIrse = -1f; Cerrar(); }
        }
    }

    private void Armar()
    {
        var moneda = spriteMoneda;
        var borde = bordeMoneda;
        idiomaArmado = Idioma.Revision;
        temaArmado = Tema.Revision;

        panel = new GameObject("RecompensaDiaria", typeof(RectTransform));
        var rtPanel = Estirar((RectTransform)panel.transform, (RectTransform)transform);
        var velo = panel.AddComponent<Image>();
        velo.color = new Color(0f, 0f, 0f, 0.45f);          // tapa los toques del menu

        ventana = Rect(rtPanel, "Ventana", new Vector2(0f, 10f), new Vector2(1180f, 640f));
        var fondo = ventana.gameObject.AddComponent<Image>();
        ConstructorUI.VentanaNeon(ventana, fondo, pildora);
        fondo.color = ColorDeVentana;

        titulo = Texto(ventana, "Titulo", Textos.De("diaria_titulo"), 84f, colorHoy, new Vector2(0f, 235f), new Vector2(1100f, 110f));
        if (materialContorno != null) titulo.fontSharedMaterial = materialContorno;

        string textoBajada = racha > 1 ? Textos.Formato("diaria_racha", racha) : Textos.De("diaria_volve");
        Texto(ventana, "Bajada", textoBajada, 40f, ColorDeTexto, new Vector2(0f, 158f), new Vector2(1100f, 60f));

        int hoy = RecompensaDiaria.Casillero(racha);
        // La misma marca congelada con la que se va a pagar, no la de ahora: si no, la
        // ventana prometeria un numero y cobraria otro.
        int mejor = RecompensaDiaria.OleadaDeHoy;
        const float Ancho = 138f, Paso = 154f;
        for (int dia = 1; dia <= RecompensaDiaria.DiasDelCiclo; dia++)
        {
            float x = (dia - (RecompensaDiaria.DiasDelCiclo + 1) * 0.5f) * Paso;
            var casillero = Rect(ventana, "Dia" + dia, new Vector2(x, 10f), new Vector2(Ancho, 190f));
            var img = casillero.gameObject.AddComponent<Image>();
            Redondear(img, 3f);
            img.color = dia < hoy ? colorCobrado : dia == hoy ? colorHoy : ColorDeFuturo;
            // Los casilleros de color (el de hoy, dorado, y los cobrados, verdes) no
            // cambian con el tema: su texto va oscuro siempre. El de los dias que faltan
            // es un hueco sobre el panel, asi que ese si sigue al tema.
            bool sobreColor = dia <= hoy;
            Color textoDelCasillero = sobreColor ? colorTextoOscuro : ColorDeTexto;

            // Cada casillero es un dia de racha: el de hoy es la racha actual (del 7 en adelante
            // se queda en el ultimo casillero) y los otros, los dias de al lado. La etiqueta y el
            // monto salen del mismo numero, asi en el dia 8 no dice DIA 7 con el monto del 8.
            int rachaDelCasillero = racha - hoy + dia;
            Texto(casillero, "Dia", Textos.Formato("diaria_dia", rachaDelCasillero), 30f, textoDelCasillero, new Vector2(0f, 62f), new Vector2(Ancho, 40f));
            var circulo = Rect(casillero, "Moneda", new Vector2(0f, 8f), new Vector2(54f, 54f));
            var imgMoneda = circulo.gameObject.AddComponent<Image>();
            imgMoneda.sprite = moneda;
            imgMoneda.color = colorMoneda;
            ConBorde(circulo, borde);
            // A partir del dia de hoy se muestra lo que se cobraria con la racha intacta.
            string monto = FormatoNumeros.Compacto(RecompensaDiaria.Monto(rachaDelCasillero, mejor));
            Texto(casillero, "Monto", monto, 36f, textoDelCasillero, new Vector2(0f, -52f), new Vector2(Ancho, 46f));

            var rtTilde = Rect(casillero, "Tilde", new Vector2(0f, 8f), new Vector2(44f, 44f));
            var imgTilde = rtTilde.gameObject.AddComponent<Image>();
            imgTilde.sprite = tilde;
            imgTilde.color = colorTilde;
            imgTilde.raycastTarget = false;
            imgTilde.enabled = dia < hoy && tilde != null;

            if (dia == hoy)
            {
                casilleroHoy = casillero;
                fondoHoy = img;
                tildeHoy = imgTilde;
                casillero.SetAsLastSibling();
            }
        }

        montoHoy = RecompensaDiaria.Monto(racha, mejor);
        boton = ArmarBoton(ventana, "BotonCobrar", 560f, ConstructorUI.Verde, ConstructorUI.VerdeTexto,
                           moneda, colorMoneda, true, Textos.Formato("diaria_cobrar", FormatoNumeros.Compacto(montoHoy)), Cobrar);

        var claqueta = spriteClaqueta;
        botonVideo = ArmarBoton(ventana, "BotonVideo", 620f, colorVideo, new Color32(0x3a, 0x1a, 0x00, 255),
                                claqueta, new Color32(0x3a, 0x1a, 0x00, 255), false,
                                Textos.Formato("diaria_video", FormatoNumeros.Compacto(montoHoy)), PedirVideo);
        ((RectTransform)botonVideo.transform).anchoredPosition = new Vector2(-175f, -218f);
        // VOLVER se lee igual de bien que el video: mismo tamanio de letra, vidrio oscuro como los demas.
        botonAtras = ArmarBoton(ventana, "BotonAtras", 340f, new Color(0.06f, 0.12f, 0.05f, 0.45f), Color.white,
                                iconoAtras, Color.white, false, Textos.De("comun_volver"), Atras);
        ((RectTransform)botonAtras.transform).anchoredPosition = new Vector2(330f, -218f);
        MostrarOferta(false);
    }

    private GameObject ArmarBoton(RectTransform padre, string nombre, float ancho, Color color, Color colorTexto,
                                  Sprite dibujo, Color colorIcono, bool iconoConBorde, string texto,
                                  UnityEngine.Events.UnityAction alTocar)
    {
        var tamanio = new Vector2(ancho, 130f);
        var raiz = Rect(padre, nombre, new Vector2(0f, -218f), tamanio);
        var toque = raiz.gameObject.AddComponent<Image>();
        toque.color = new Color(1f, 1f, 1f, 0f);
        var button = raiz.gameObject.AddComponent<Button>();
        button.targetGraphic = toque;
        button.onClick.AddListener(alTocar);

        // Visual antes que la sombra: BotonJugoso toma en su Awake el primer hijo como visual.
        var visual = Rect(raiz, "Visual", Vector2.zero, tamanio);
        var fondo = Rect(visual, "Fondo", Vector2.zero, tamanio);
        var imgFondo = fondo.gameObject.AddComponent<Image>();
        imgFondo.sprite = pildora; imgFondo.type = Image.Type.Sliced;
        imgFondo.color = color;
        imgFondo.raycastTarget = false;

        var tmp = Texto(visual, "Texto", texto, 54f, colorTexto, Vector2.zero, tamanio);
        if (dibujo != null)
        {
            var icono = Rect(visual, "Icono", Vector2.zero, new Vector2(58f, 58f));
            icono.anchorMin = icono.anchorMax = new Vector2(0f, 0.5f);
            icono.SetSiblingIndex(tmp.transform.GetSiblingIndex());
            var imgIcono = icono.gameObject.AddComponent<Image>();
            imgIcono.sprite = dibujo;
            imgIcono.color = colorIcono;
            imgIcono.preserveAspect = true;
            imgIcono.raycastTarget = false;
            if (iconoConBorde) ConBorde(icono, bordeMoneda);
            var junto = icono.gameObject.AddComponent<IconoDeBoton>();
            junto.texto = tmp;
            junto.separacion = 12f;
        }

        var jugoso = raiz.gameObject.AddComponent<BotonJugoso>();
        jugoso.respirar = true;
        jugoso.sonidoClick = sonidoClick;

        var sombra = Rect(raiz, "Sombra", new Vector2(0f, -8f), tamanio);
        sombra.SetAsFirstSibling();
        var imgSombra = sombra.gameObject.AddComponent<Image>();
        imgSombra.sprite = pildora; imgSombra.type = Image.Type.Sliced;
        imgSombra.color = new Color(0f, 0f, 0f, 0.3f);
        imgSombra.raycastTarget = false;
        ConstructorUI.HaloDeBoton(imgSombra, color);
        return raiz.gameObject;
    }

    private void ConBorde(RectTransform moneda, Sprite borde)
    {
        var rt = Rect(moneda, "Borde", Vector2.zero, Vector2.zero);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = borde;
        img.color = colorBordeMoneda;
        img.raycastTarget = false;
    }

    private void Redondear(Image img, float multiplicador)
    {
        img.sprite = pildora;
        img.type = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = multiplicador;   // bordes mas chicos que una pildora
    }

    private TMP_Text Texto(RectTransform padre, string nombre, string texto, float tamanio, Color color, Vector2 posicion, Vector2 caja)
    {
        var rt = Rect(padre, nombre, posicion, caja);
        var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (fuente != null) tmp.font = fuente;
        tmp.text = texto;
        tmp.fontSize = tamanio;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static RectTransform Rect(RectTransform padre, string nombre, Vector2 posicion, Vector2 tamanio)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(padre, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = tamanio;
        rt.anchoredPosition = posicion;
        return rt;
    }

    private static RectTransform Estirar(RectTransform rt, RectTransform padre)
    {
        rt.SetParent(padre, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return rt;
    }
}
