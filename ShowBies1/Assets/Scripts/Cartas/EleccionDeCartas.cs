using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Las cartas en la partida (1.6.0, ver CARTAS.md): la barra del PODER encima de la vida, la fila
// de las cartas que se tienen, la chispa que vuela de cada zombi muerto al jugador, y la eleccion
// de una entre tres al subir de PODER.
//
// No esta en las escenas: se instala sola al cargar el libre o las oleadas (sceneLoaded, como las
// flechas del borde), y al instalarse empieza el PODER de la partida (PoderDeLaPartida.Empezar),
// de cero o como estaba guardado al empezar la oleada que se retoma.
//
// La eleccion congela el juego (Time.timeScale en 0, y MenuPausa.JuegoCongelado la cuenta) y no
// se abre con otra cosa congelando: el jugador muerto, la oferta de revivir, la derrota o la
// pausa. Queda pendiente y se abre en cuanto se puede. Los bancos en play no la abren
// (PoderDeLaPartida.SinCartasAutomaticas).
public class EleccionDeCartas : MonoBehaviour
{
    public static bool Activa { get; private set; }
    public static EleccionDeCartas Instancia { get; private set; }

    // Los toques en la primera fraccion de segundo no eligen: el pulgar que estaba en el joystick
    // de disparo cae justo donde aparecen las cartas.
    public const float SinElegirAlAbrir = 0.6f;
    private const float EntradaPorCarta = 0.12f;
    private const float DuracionEntrada = 0.35f;
    private const float DuracionSalida = 0.4f;
    // Entre una eleccion y la siguiente, si se subio mas de un PODER de golpe.
    private const float EntreElecciones = 0.25f;

    private const int OrdenBarra = 9;      // por debajo de la pausa (10), por encima de las flechas (8)
    private const int OrdenEleccion = 12;  // por encima de la pausa: tapa su boton

    private static readonly Vector2 TamCarta = new Vector2(380f, 520f);
    private const float SeparacionCartas = 430f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetearEstadoCompartido()
    {
        Activa = false;
        Instancia = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Instalar()
    {
        SceneManager.sceneLoaded -= AlCargar;
        SceneManager.sceneLoaded += AlCargar;
    }

    private static void AlCargar(Scene escena, LoadSceneMode modo)
    {
        bool deJuego = escena.buildIndex == TiendaMejoras.EscenaModoLibre || escena.buildIndex == TiendaMejoras.EscenaOleadas;
        if (!deJuego)
        {
            // La derrota se carga encima de la partida: esa no termina nada.
            if (modo == LoadSceneMode.Single) PoderDeLaPartida.Terminar();
            return;
        }
        var go = new GameObject("EleccionDeCartas");
        SceneManager.MoveGameObjectToScene(go, escena);
        go.AddComponent<EleccionDeCartas>();
    }

    // Una chispa de experiencia, del zombi muerto al jugador (desde DanoZombi). Sin la eleccion en
    // la escena (el tutorial), nada.
    public static void Chispa(Vector3 desde, int experiencia)
    {
        if (Instancia == null || experiencia <= 0 || !PoderDeLaPartida.Activo) return;
        Instancia.SoltarChispa(desde);
    }

    private class Tarjeta
    {
        public string id;
        public bool dorada;
        public RectTransform raiz;
        public Button boton;
        public float aparece;
    }

    private TMP_FontAsset fuente;
    private Material materialContorno;
    private Sprite pildora;
    private Sprite circulo;
    private Texture2D texturaCirculo;

    private RectTransform lienzoBarra;
    private RectTransform bloqueBarra;
    private RectTransform rellenoBarra;
    private TMP_Text textoPoder;
    private RectTransform filaIconos;
    private int revisionVista = -1;
    private int poderMostrado = -1;
    private int vecesMostradas = -1;
    private float golpeBarra = -1f;
    private float proximoAcomodo;

    private GameObject lienzoEleccion;
    private RectTransform panel;
    private TMP_Text titulo;
    private readonly List<Tarjeta> tarjetas = new List<Tarjeta>();
    private float abiertaEn;
    private float cerrandoDesde = -1f;
    private Tarjeta elegida;
    private float proximaEleccion;
    private bool armada;

    // Las chispas: un pool chico, que con una granada que mata a diez no hacen falta mas.
    private class ChispaEnVuelo
    {
        public SpriteRenderer sprite;
        public Vector3 desde;
        public float salio;
        public bool enVuelo;
    }
    private const int MaxChispas = 24;
    private const float VueloChispa = 0.35f;
    private readonly List<ChispaEnVuelo> chispas = new List<ChispaEnVuelo>();

    private void Awake()
    {
        Instancia = this;
        bool oleadas = gameObject.scene.buildIndex == TiendaMejoras.EscenaOleadas;
        bool retomar = oleadas && Progreso.OleadaEnCurso > 1;
        PoderDeLaPartida.Empezar(retomar ? Progreso.PoderEnCurso : null);
    }

    // Start y no Awake: las piezas salen de la barra del jefe y del aviso de las misiones, que
    // ya estan armados, y la vida ya la fijo AplicarMejoras.
    private void Start()
    {
        var jefe = FindAnyObjectByType<BarraDelJefe>(FindObjectsInactive.Include);
        if (jefe != null)
        {
            fuente = jefe.fuente;
            materialContorno = jefe.materialContorno;
            pildora = jefe.pildora;
        }
        texturaCirculo = TexturasUI.Circulo(128);
        circulo = Sprite.Create(texturaCirculo, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f));

        // Una partida retomada con cartas de vida arranca con el maximo de las cartas.
        if (PoderDeLaPartida.MultiplicadorVida > 1f && PlayerHealth.instance != null)
            PlayerHealth.instance.AplicarCartasDeVida();

        ArmarBarra();
        armada = true;
    }

    private void OnDestroy()
    {
        if (Activa)
        {
            Activa = false;
            Time.timeScale = 1f;
        }
        if (Instancia == this) Instancia = null;
        if (lienzoBarra != null) Destroy(lienzoBarra.gameObject);
        if (lienzoEleccion != null) Destroy(lienzoEleccion);
        foreach (var c in chispas) if (c.sprite != null) Destroy(c.sprite.gameObject);
        if (circulo != null) Destroy(circulo);
        if (texturaCirculo != null) Destroy(texturaCirculo);
    }

    // --- La barra ---------------------------------------------------------------------------

    private static RectTransform NuevoLienzo(Scene escena, string nombre, int orden, bool conToques)
    {
        var go = new GameObject(nombre, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        SceneManager.MoveGameObjectToScene(go, escena);
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = orden;
        var escalador = go.GetComponent<CanvasScaler>();
        escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escalador.referenceResolution = new Vector2(1920f, 1080f);
        escalador.matchWidthOrHeight = 0.5f;
        if (conToques) go.AddComponent<GraphicRaycaster>();
        return (RectTransform)go.transform;
    }

    private void ArmarBarra()
    {
        lienzoBarra = NuevoLienzo(gameObject.scene, "BarraDePoder", OrdenBarra, false);
        bloqueBarra = ConstructorUI.Rect(lienzoBarra, "Bloque", Vector2.zero, new Vector2(620f, 90f));
        bloqueBarra.anchorMin = bloqueBarra.anchorMax = new Vector2(0.5f, 0f);
        bloqueBarra.pivot = new Vector2(0.5f, 0f);
        bloqueBarra.anchoredPosition = new Vector2(0f, 150f);

        textoPoder = ConstructorUI.Texto(bloqueBarra, "Poder", "", 34f, ConstructorUI.Celeste,
                                         new Vector2(-230f, 12f), new Vector2(160f, 44f), fuente);
        if (materialContorno != null) textoPoder.fontSharedMaterial = materialContorno;
        textoPoder.alignment = TextAlignmentOptions.Right;
        rellenoBarra = ConstructorUI.Barra(bloqueBarra, "Barra", new Vector2(80f, 12f), new Vector2(420f, 18f), pildora,
                                           new Color(0f, 0f, 0f, 0.55f), ConstructorUI.Celeste);
        filaIconos = ConstructorUI.Rect(bloqueBarra, "Cartas", new Vector2(80f, 54f), new Vector2(420f, 40f));
        Refrescar();
    }

    private void Refrescar()
    {
        if (rellenoBarra == null) return;
        revisionVista = PoderDeLaPartida.Revision;
        int costo = Mathf.Max(1, PoderDeLaPartida.CostoActual);
        rellenoBarra.anchorMax = new Vector2(Mathf.Clamp01((float)PoderDeLaPartida.Experiencia / costo), 1f);
        if (PoderDeLaPartida.Poder != poderMostrado)
        {
            if (poderMostrado >= 0 && PoderDeLaPartida.Poder > poderMostrado) golpeBarra = 0f;
            poderMostrado = PoderDeLaPartida.Poder;
            textoPoder.text = Textos.Formato("poder_barra", poderMostrado);
        }
        // Las cartas cambian al elegir, no al subir de PODER.
        int veces = 0;
        foreach (var par in PoderDeLaPartida.Elegidas) veces += par.Value;
        if (veces != vecesMostradas)
        {
            vecesMostradas = veces;
            ArmarIconos();
        }
    }

    // Una fila de circulitos con las cartas que se tienen, del color de cada una, con las veces.
    private void ArmarIconos()
    {
        if (filaIconos == null) return;
        for (int i = filaIconos.childCount - 1; i >= 0; i--) Destroy(filaIconos.GetChild(i).gameObject);
        var catalogo = CatalogoCartas.Instancia;
        if (catalogo == null) return;
        int n = 0;
        foreach (var par in PoderDeLaPartida.Elegidas)
        {
            var carta = catalogo.Buscar(par.Key);
            if (carta == null) continue;
            var icono = ConstructorUI.Imagen(filaIconos, carta.id, new Vector2(-190f + 44f * n, 0f), new Vector2(38f, 38f), circulo, carta.color);
            PonerDibujo((RectTransform)icono.transform, carta, 38f);
            if (par.Value > 1)
                ConstructorUI.Texto((RectTransform)icono.transform, "Veces", par.Value.ToString(), 18f, Color.white,
                                    new Vector2(16f, -14f), new Vector2(24f, 22f), fuente);
            n++;
            if (n >= 10) break;
        }
    }

    // El dibujo de la carta dentro de su circulo de color, oscuro: el icono (de Gemini, ver
    // ConstructorCartas) o, si no tiene, su simbolo escrito.
    private static readonly Color ColorDelDibujo = new Color(0.04f, 0.04f, 0.07f);

    private void PonerDibujo(RectTransform circulo, Carta carta, float lado)
    {
        if (carta != null && carta.icono != null)
        {
            float dibujo = lado * 0.64f;
            ConstructorUI.Imagen(circulo, "Dibujo", Vector2.zero, new Vector2(dibujo, dibujo), carta.icono, ColorDelDibujo);
            return;
        }
        ConstructorUI.Texto(circulo, "Simbolo", carta != null ? carta.simbolo : "+", lado * 0.5f, ColorDelDibujo,
                            new Vector2(0f, -lado * 0.025f), new Vector2(lado, lado), fuente);
    }

    // La barra va justo encima del numero de la vida, donde esta abajo al centro en las dos
    // plataformas; se mide cada medio segundo porque el HUD escala con la pantalla.
    private void Acomodar()
    {
        if (bloqueBarra == null || PlayerHealth.instance == null || PlayerHealth.instance.healthTMP == null) return;
        var vida = PlayerHealth.instance.healthTMP.rectTransform;
        var esquinas = new Vector3[4];
        vida.GetWorldCorners(esquinas);
        // Lo que ocupan las letras, no la caja: la caja del numero es mucho mas alta.
        float arriba = esquinas[1].y;
        var bounds = PlayerHealth.instance.healthTMP.textBounds;
        if (bounds.size.y > 0f) arriba = vida.TransformPoint(new Vector3(0f, bounds.max.y, 0f)).y;
        Vector2 local;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(lienzoBarra, new Vector2(Screen.width * 0.5f, arriba), null, out local))
        {
            float desdeAbajo = local.y + lienzoBarra.rect.height * 0.5f;
            bloqueBarra.anchoredPosition = new Vector2(0f, desdeAbajo + 8f);
        }
    }

    // --- Las chispas ------------------------------------------------------------------------

    private void SoltarChispa(Vector3 desde)
    {
        ChispaEnVuelo libre = null;
        foreach (var c in chispas) if (!c.enVuelo) { libre = c; break; }
        if (libre == null)
        {
            if (chispas.Count >= MaxChispas || circulo == null) return;
            var go = new GameObject("ChispaDeExperiencia");
            SceneManager.MoveGameObjectToScene(go, gameObject.scene);
            libre = new ChispaEnVuelo { sprite = go.AddComponent<SpriteRenderer>() };
            libre.sprite.sprite = circulo;
            libre.sprite.color = ConstructorUI.Celeste;
            libre.sprite.sortingOrder = 50;
            go.transform.localScale = Vector3.one * 0.32f;
            chispas.Add(libre);
        }
        libre.desde = desde + Vector3.up;
        libre.salio = Time.time;
        libre.enVuelo = true;
        libre.sprite.transform.position = libre.desde;
        libre.sprite.gameObject.SetActive(true);
    }

    private void MoverChispas()
    {
        var jugador = PlayerHealth.instance;
        var camara = Camera.main;
        foreach (var c in chispas)
        {
            if (!c.enVuelo) continue;
            float t = (Time.time - c.salio) / VueloChispa;
            if (jugador == null || t >= 1f)
            {
                c.enVuelo = false;
                c.sprite.gameObject.SetActive(false);
                if (jugador != null) golpeBarra = Mathf.Max(golpeBarra, 0f);
                continue;
            }
            Vector3 hacia = jugador.transform.position + Vector3.up;
            float e = t * t;   // arranca lento y llega rapido: se lee como que la absorbe
            Vector3 p = Vector3.Lerp(c.desde, hacia, e) + Vector3.up * Mathf.Sin(t * Mathf.PI) * 1.2f;
            c.sprite.transform.position = p;
            if (camara != null) c.sprite.transform.rotation = camara.transform.rotation;
        }
    }

    // --- La eleccion ------------------------------------------------------------------------

    // Para los bancos en play, que no las abren solas (SinCartasAutomaticas): abre la eleccion
    // pendiente si se puede, con las mismas reglas que la automatica. Devuelve si se abrio.
    public bool AbrirAMano()
    {
        if (!SePuedeAbrir(true)) return false;
        Abrir();
        return Activa;
    }

    private bool SePuedeAbrir(bool aMano = false)
    {
        if (!armada || Activa || PoderDeLaPartida.Pendientes <= 0) return false;
        if (PoderDeLaPartida.SinCartasAutomaticas && !aMano) return false;
        if (CatalogoCartas.Instancia == null) return false;
        var jugador = PlayerHealth.instance;
        if (jugador == null || jugador.EstaMuerto) return false;
        if (MenuPausa.Pausado || OfertaDeRevivir.Activa || DerrotaEnLaPartida.Activa) return false;
        return Time.unscaledTime >= proximaEleccion;
    }

    private void Update()
    {
        if (!armada) return;
        if (PoderDeLaPartida.Revision != revisionVista) Refrescar();
        if (Time.unscaledTime >= proximoAcomodo)
        {
            proximoAcomodo = Time.unscaledTime + 0.5f;
            Acomodar();
        }
        MoverChispas();

        if (golpeBarra >= 0f && bloqueBarra != null)
        {
            golpeBarra += Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            float salto = 0.12f * CurvasUI.Campana(Mathf.Clamp01(golpeBarra / 0.3f));
            bloqueBarra.localScale = Vector3.one * (1f + salto);
            if (golpeBarra >= 0.3f) { golpeBarra = -1f; bloqueBarra.localScale = Vector3.one; }
        }

        if (!Activa)
        {
            if (SePuedeAbrir()) Abrir();
            return;
        }
        AnimarEleccion();
    }

    private void Abrir()
    {
        int dorada;
        var ids = PoderDeLaPartida.Ofrecer(out dorada);
        if (ids.Count == 0) return;
        if (lienzoEleccion == null) ArmarEleccion();

        foreach (var t in tarjetas) if (t.raiz != null) Destroy(t.raiz.gameObject);
        tarjetas.Clear();
        float ancho = SeparacionCartas * (ids.Count - 1);
        for (int i = 0; i < ids.Count; i++)
        {
            var t = ArmarTarjeta(ids[i], i == dorada, new Vector2(-ancho * 0.5f + SeparacionCartas * i, -40f));
            t.aparece = EntradaPorCarta * i;
            tarjetas.Add(t);
        }
        int eligiendo = PoderDeLaPartida.Poder - PoderDeLaPartida.Pendientes + 1;
        titulo.text = Textos.Formato("poder_titulo", eligiendo);

        lienzoEleccion.SetActive(true);
        Activa = true;
        Time.timeScale = 0f;
        abiertaEn = Time.unscaledTime;
        cerrandoDesde = -1f;
        elegida = null;
        Efectos.CartelOleada();
        AnimarEleccion();
    }

    private void ArmarEleccion()
    {
        var lienzo = NuevoLienzo(gameObject.scene, "EleccionDeCartas", OrdenEleccion, true);
        lienzoEleccion = lienzo.gameObject;
        var fondo = ConstructorUI.Estirar(lienzo, "Fondo");
        var imgFondo = fondo.gameObject.AddComponent<Image>();
        imgFondo.color = new Color(0.02f, 0.02f, 0.05f, 0.78f);   // tapa la partida y frena los toques
        panel = ConstructorUI.Rect(lienzo, "Panel", Vector2.zero, new Vector2(1400f, 820f));
        titulo = ConstructorUI.Texto(panel, "Titulo", "", 92f, ConstructorUI.TituloNeon, new Vector2(0f, 340f), new Vector2(1000f, 120f), fuente);
        var neon = MaterialDelTituloDeLaPausa();
        if (neon != null) titulo.fontSharedMaterial = neon;
        else if (materialContorno != null) titulo.fontSharedMaterial = materialContorno;
        var sub = ConstructorUI.Texto(panel, "Elige", Textos.De("poder_elige"), 40f, new Color(0.75f, 0.82f, 0.95f),
                                      new Vector2(0f, 262f), new Vector2(1000f, 60f), fuente);
        if (materialContorno != null) sub.fontSharedMaterial = materialContorno;
        lienzoEleccion.SetActive(false);
    }

    // El halo de neon de los titulos (Bangers SDF - Neon) no esta en Resources: se toma del titulo de
    // la pausa, que esta en las dos escenas de juego.
    private static Material MaterialDelTituloDeLaPausa()
    {
        var pausa = FindAnyObjectByType<MenuPausa>(FindObjectsInactive.Include);
        var t = pausa != null && pausa.panel != null ? pausa.panel.transform.Find("Titulo") : null;
        var texto = t != null ? t.GetComponent<TMP_Text>() : null;
        return texto != null ? texto.fontSharedMaterial : null;
    }

    private Tarjeta ArmarTarjeta(string id, bool dorada, Vector2 posicion)
    {
        var catalogo = CatalogoCartas.Instancia;
        var carta = catalogo != null ? catalogo.Buscar(id) : null;
        Color color = carta != null ? carta.color : ConstructorUI.Verde;
        Color borde = dorada ? ConstructorUI.Amarillo : color;

        var t = new Tarjeta { id = id, dorada = dorada };
        t.raiz = ConstructorUI.Rect(panel, "Carta_" + id, posicion, TamCarta);
        var fondo = t.raiz.gameObject.AddComponent<Image>();
        fondo.color = dorada ? new Color(0.16f, 0.13f, 0.03f, 0.97f) : new Color(0.06f, 0.07f, 0.1f, 0.97f);
        if (pildora != null) ConstructorUI.VentanaNeon(t.raiz, fondo, pildora);
        var halo = t.raiz.Find("Neon");
        if (halo != null) halo.GetComponent<Image>().color = new Color(borde.r, borde.g, borde.b, dorada ? 1f : 0.85f);
        t.boton = t.raiz.gameObject.AddComponent<Button>();
        t.boton.targetGraphic = fondo;
        var colores = t.boton.colors;
        colores.normalColor = Color.white;
        t.boton.colors = colores;
        var tarjeta = t;
        t.boton.onClick.AddListener(() => Elegir(tarjeta));

        var circ = ConstructorUI.Imagen(t.raiz, "Icono", new Vector2(0f, 130f), new Vector2(170f, 170f), circulo, color);
        PonerDibujo((RectTransform)circ.transform, carta, 170f);

        var nombre = ConstructorUI.Texto(t.raiz, "Nombre", Textos.De("carta_" + id + "_nombre"), 48f, Color.white,
                                         new Vector2(0f, -8f), new Vector2(TamCarta.x - 40f, 60f), fuente);
        if (materialContorno != null) nombre.fontSharedMaterial = materialContorno;
        var desc = ConstructorUI.Texto(t.raiz, "Que", Descripcion(id), 31f, new Color(0.78f, 0.84f, 0.95f),
                                       new Vector2(0f, -100f), new Vector2(TamCarta.x - 50f, 120f), fuente);
        desc.textWrappingMode = TextWrappingModes.Normal;

        if (carta != null && carta.tope > 0)
        {
            int ya = PoderDeLaPartida.Veces(id);
            int despues = Mathf.Min(carta.tope, ya + (dorada ? 2 : 1));
            ConstructorUI.Texto(t.raiz, "Veces", despues + "/" + carta.tope, 34f, color,
                                new Vector2(0f, -200f), new Vector2(200f, 44f), fuente);
        }
        if (dorada)
        {
            var sello = ConstructorUI.Texto(t.raiz, "Dorada", Textos.De("carta_dorada"), 56f, ConstructorUI.Amarillo,
                                            new Vector2(TamCarta.x * 0.5f - 46f, TamCarta.y * 0.5f - 44f), new Vector2(110f, 70f), fuente);
            if (materialContorno != null) sello.fontSharedMaterial = materialContorno;
        }
        t.raiz.localScale = Vector3.zero;
        return t;
    }

    // Lo que hace la carta, con su numero del asset, en el idioma del jugador.
    public static string Descripcion(string id)
    {
        if (id == PoderDeLaPartida.Curacion) return Textos.De("carta_curacion");
        var catalogo = CatalogoCartas.Instancia;
        var carta = catalogo != null ? catalogo.Buscar(id) : null;
        if (carta == null) return "";
        return Textos.Formato("carta_" + id, FormatoNumeros.ConDecimales(NumeroDeLaCarta(carta), 0));
    }

    // El numero que dice la tarjeta: los multiplicadores como porcentaje ("+25 %"), la recarga de
    // la granada como lo que baja, y las sumas tal cual (los criticos y la velocidad en %).
    public static double NumeroDeLaCarta(Carta carta)
    {
        switch (carta.id)
        {
            case PoderDeLaPartida.Dano:
            case PoderDeLaPartida.Cadencia:
            case PoderDeLaPartida.Vida:
                return System.Math.Round((carta.valor - 1f) * 100.0);
            case PoderDeLaPartida.GranadaRapida:
                return System.Math.Round((1f - carta.valor) * 100.0);
            case PoderDeLaPartida.Critico:
            case PoderDeLaPartida.Velocidad:
                return System.Math.Round(carta.valor * 100.0);
            default:
                return System.Math.Round(carta.valor, 1);
        }
    }

    private void AnimarEleccion()
    {
        float desde = Time.unscaledTime - abiertaEn;
        foreach (var t in tarjetas)
        {
            if (t.raiz == null) continue;
            float k = Mathf.Clamp01((desde - t.aparece) / DuracionEntrada);
            float escala = CurvasUI.SalidaAtras(k);
            float giro = (1f - k) * 90f;
            if (cerrandoDesde >= 0f)
            {
                float s = Mathf.Clamp01((Time.unscaledTime - cerrandoDesde) / DuracionSalida);
                if (t == elegida) escala = 1f + 0.12f * CurvasUI.Campana(s);
                else escala = 1f - s;
            }
            else if (t.dorada && k >= 1f)
            {
                escala *= 1f + 0.035f * Mathf.Sin(Time.unscaledTime * 6f);
            }
            t.raiz.localScale = Vector3.one * Mathf.Max(0f, escala);
            t.raiz.localRotation = Quaternion.Euler(0f, giro, 0f);
        }
        if (cerrandoDesde >= 0f && Time.unscaledTime - cerrandoDesde >= DuracionSalida) Cerrar();
    }

    private void Elegir(Tarjeta t)
    {
        if (!Activa || cerrandoDesde >= 0f || t == null) return;
        if (Time.unscaledTime - abiertaEn < SinElegirAlAbrir) return;
        int sumadas = PoderDeLaPartida.Elegir(t.id, t.dorada);
        var jugador = PlayerHealth.instance;
        if (jugador != null)
        {
            if (t.id == PoderDeLaPartida.Vida && sumadas > 0) jugador.AplicarCartasDeVida();
            else if (t.id == PoderDeLaPartida.Curacion) jugador.CurarEntero();
        }
        elegida = t;
        cerrandoDesde = Time.unscaledTime;
        Efectos.CartaElegida(t.dorada);
    }

    private void Cerrar()
    {
        if (lienzoEleccion != null) lienzoEleccion.SetActive(false);
        foreach (var t in tarjetas) if (t.raiz != null) Destroy(t.raiz.gameObject);
        tarjetas.Clear();
        Activa = false;
        Time.timeScale = 1f;
        proximaEleccion = Time.unscaledTime + EntreElecciones;
        Refrescar();
    }
}
