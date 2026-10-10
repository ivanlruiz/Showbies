using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// El aviso del menu de que hay una version nueva (ActualizacionDePlay, la actualizacion
// flexible de Play). Pedido de Ivan el 9/10: hasta ahi el jugador se enteraba solo si Play
// actualizaba solo o si entraba a la ficha.
//
// Es un cartel arriba al centro, en la franja de CartelDeCobros (debajo de la fila de botones
// redondos de las esquinas y encima de MEJORAS en 16:9, 20:9 y 21:9), y no una ventana: no tapa
// nada ni hay que cerrarlo para jugar. Tiene tres caras:
// - "¡NUEVA VERSION DISPONIBLE!" con ACTUALIZAR (verde, que abre la hoja de Play) y AHORA NO,
//   que no la vuelve a ofrecer por unos dias (ActualizacionDePlay.DiasDePostergacion).
// - Mientras baja, la barra con el porcentaje. Se puede seguir jugando: Play la baja igual, y el
//   aviso de que esta lista sale en la proxima visita al menu, nunca en la partida.
// - "¡ACTUALIZACION LISTA!" con REINICIAR (Play la instala y vuelve a abrir el juego) y MAS
//   TARDE, que la esconde hasta la proxima visita al menu.
//
// Sale con el menu tranquilo un rato (Espera): despues de la recompensa diaria, del cartel de
// los cobros y del de consentimiento, y con la tienda y las ventanas cerradas; si se abre algo,
// se va y vuelve al cerrarlo. La reseña (PedidoDeResena) no se pide mientras hay una para
// ofrecer, y el consentimiento sale antes que esto (se pide a los 1,5 s, y esto espera 2,5).
//
// Nada de esto esta en la escena: se instala solo al cargar el menu, como CartelDeCobros, y
// toma la fuente, la pildora, los colores y los sonidos de VentanaMisiones.
public class AvisoDeActualizacion : MonoBehaviour
{
    public const float Espera = 2.5f;       // con el menu tranquilo
    public const int OrdenDelCanvas = 20;   // la tienda va en 5 y el cartel de los cobros en 30
    public const float DesdeArriba = 180f;  // el de CartelDeCobros
    public const float Ancho = 1000f;
    // Con el borde de neon, que sobresale 40, termina a 470 del borde de arriba: MEJORAS empieza a
    // 540 - 50k (k, el ancho relativo a 16:9), 474 en 21:9. La prueba de logica lo mide.
    public const float Alto = 250f;
    public const float BrilloDelBorde = 40f;

    // Los botones: el de la accion a la derecha, cerca del pulgar, como PLAY.
    public static readonly Vector2 TamanioAccion = new Vector2(400f, 84f);
    public static readonly Vector2 TamanioSecundario = new Vector2(320f, 84f);
    public const float EntreBotones = 40f;

    private static readonly int[] SemitonosFestejo = { -12, -8, -5, 0, 4, 7, 12 };

    private enum Vista { Ninguna, Disponible, Descargando, Lista }

    // Hay un cartel en pantalla.
    public static bool Abierto { get; private set; }
    // Hay algo para avisar en esta visita al menu, aunque todavia no haya salido.
    public static bool Pendiente { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetearEstadoCompartido()
    {
        Abierto = false;
        Pendiente = false;
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
        var go = new GameObject("AvisoDeActualizacion");
        SceneManager.MoveGameObjectToScene(go, escena);
        go.AddComponent<AvisoDeActualizacion>();
    }

    private VentanaMisiones misiones;
    private BotonAtrasMenu atras;
    private Sprite iconoActualizar;
    private Sprite iconoReiniciar;
    private GameObject lienzo;
    private RectTransform cartel;
    private RectTransform relleno;
    private TMP_Text porcentaje;
    private Vista vista;
    private float tranquiloDesde = -1f;
    private float desde;
    // AHORA NO y MAS TARDE valen para esta visita (AHORA NO, ademas, por unos dias).
    private bool disponibleDescartada;
    private bool listaDescartada;
    private Vista yaSono = Vista.Ninguna;
    private int versionMirada = -1;
    private bool ofrecerMirada;

    private void Start()
    {
        misiones = FindAnyObjectByType<VentanaMisiones>(FindObjectsInactive.Include);
        atras = FindAnyObjectByType<BotonAtrasMenu>(FindObjectsInactive.Include);
        if (misiones == null || atras == null)
        {
            enabled = false;
            return;
        }
        iconoActualizar = Resources.Load<Sprite>("IconoActualizar");
        iconoReiniciar = Resources.Load<Sprite>("IconoReiniciar");
        // Cada visita al menu le vuelve a preguntar a Play: una bajada que termino durante la
        // partida se ve aca.
        ActualizacionDePlay.Revisar();
    }

    private void OnDestroy()
    {
        if (cartel != null) Abierto = false;
        Pendiente = false;
    }

    private void Update()
    {
        ActualizacionDePlay.Atender();
        Vista quiere = QueMostrar();
        Pendiente = quiere != Vista.Ninguna;
        // Cambio lo que hay (empezo a bajar, termino, fallo): el cartel se rearma.
        if (cartel != null && quiere != vista) Quitar();
        if (quiere == Vista.Ninguna) return;

        if (!MenuTranquilo())
        {
            tranquiloDesde = -1f;
            if (cartel != null) Quitar();
            return;
        }
        float t = Time.unscaledTime;
        if (cartel == null)
        {
            if (tranquiloDesde < 0f) tranquiloDesde = t;
            if (t - tranquiloDesde < Espera) return;
            Armar(quiere);
        }

        cartel.localScale = Vector3.one * CurvasUI.SalidaAtras(Mathf.Clamp01((t - desde) / 0.4f));
        if (vista == Vista.Descargando) MostrarAvance();
    }

    private Vista QueMostrar()
    {
        if (ActualizacionDePlay.EsperandoRespuesta) return Vista.Ninguna;
        switch (ActualizacionDePlay.Estado)
        {
            case EstadoActualizacion.Disponible:
                if (disponibleDescartada) return Vista.Ninguna;
                // PlayerPrefs se lee una vez por version, no en cada cuadro.
                if (versionMirada != ActualizacionDePlay.VersionDisponible)
                {
                    versionMirada = ActualizacionDePlay.VersionDisponible;
                    ofrecerMirada = ActualizacionDePlay.OfrecerLaDisponible();
                }
                return ofrecerMirada ? Vista.Disponible : Vista.Ninguna;
            case EstadoActualizacion.Descargando:
                return Vista.Descargando;
            case EstadoActualizacion.Lista:
                return listaDescartada ? Vista.Ninguna : Vista.Lista;
            default:
                return Vista.Ninguna;
        }
    }

    // Con todo cerrado de verdad: lo mismo que mira la reseña, mas la recompensa diaria
    // mientras se va y los otros carteles que se instalan solos.
    private bool MenuTranquilo()
    {
        return !VentanaRecompensaDiaria.Abierta && !VentanaRecompensaDiaria.Ocupada && !VentanaMisiones.Abierta
               && !VentanaBestiario.Abierta && !VentanaLogros.Abierta
               && (atras.tienda == null || !atras.tienda.Abierta)
               && (atras.confirmarSalir == null || !atras.confirmarSalir.Abierta)
               && (atras.selectorIdioma == null || !atras.selectorIdioma.Abierto)
               && (atras.opcionesSonido == null || !atras.opcionesSonido.Abierto)
               && (atras.menuModos == null || !atras.menuModos.activeSelf)
               && !VentanaHalloween.Abierta && !CartelDeCobros.Abierto
               && !ServicioAnuncios.ConsentimientoEnPantalla;
    }

    private void Armar(Vista cual)
    {
        lienzo = new GameObject("Lienzo", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        lienzo.transform.SetParent(transform, false);
        var canvas = lienzo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = OrdenDelCanvas;
        var escala = lienzo.GetComponent<CanvasScaler>();
        escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escala.referenceResolution = new Vector2(1920f, 1080f);
        // Por el alto, como CartelDeCobros: asi entra entre las esquinas y MEJORAS en 16:9, 20:9 y 21:9.
        escala.matchWidthOrHeight = 1f;

        cartel = ConstructorUI.Rect((RectTransform)lienzo.transform, "Cartel", Vector2.zero, new Vector2(Ancho, Alto));
        cartel.anchorMin = cartel.anchorMax = new Vector2(0.5f, 1f);
        cartel.pivot = new Vector2(0.5f, 1f);
        cartel.anchoredPosition = new Vector2(0f, -DesdeArriba);
        cartel.localScale = Vector3.zero;
        var fondo = cartel.gameObject.AddComponent<Image>();
        ConstructorUI.VentanaNeon(cartel, fondo, misiones.pildora);
        fondo.color = Tema.Elegir(misiones.colorVentana, RolDeTema.Panel);

        string titulo, detalle;
        switch (cual)
        {
            case Vista.Disponible:
                titulo = Textos.De("actualizacion_titulo");
                detalle = Textos.De("actualizacion_detalle");
                break;
            case Vista.Descargando:
                titulo = Textos.De("actualizacion_descargando");
                detalle = Textos.De("actualizacion_descargando_detalle");
                break;
            default:
                titulo = Textos.De("actualizacion_lista");
                detalle = Textos.De("actualizacion_lista_detalle");
                break;
        }

        // Lo de adentro cuelga del centro del cartel (el ancla de ConstructorUI.Rect).
        float y = Alto / 2f - 48f;
        var textoTitulo = ConstructorUI.Texto(cartel, "Titulo", titulo, 54f, misiones.colorTitulo,
                                              new Vector2(0f, y), new Vector2(Ancho - 60f, 64f), misiones.fuente);
        if (misiones.materialContorno != null) textoTitulo.fontSharedMaterial = misiones.materialContorno;
        textoTitulo.enableAutoSizing = true;
        textoTitulo.fontSizeMax = 54f;
        textoTitulo.fontSizeMin = 36f;
        y -= 48f;
        var textoDetalle = ConstructorUI.Texto(cartel, "Detalle", detalle, 30f,
                                               Tema.Elegir(misiones.colorTextoOscuro, RolDeTema.TextoSuave),
                                               new Vector2(0f, y), new Vector2(Ancho - 60f, 40f), misiones.fuente);
        textoDetalle.enableAutoSizing = true;
        textoDetalle.fontSizeMax = 30f;
        textoDetalle.fontSizeMin = 22f;

        float yAbajo = -Alto / 2f + 62f;
        relleno = null;
        porcentaje = null;
        if (cual == Vista.Descargando)
        {
            relleno = ConstructorUI.Barra(cartel, "Barra", new Vector2(-60f, yAbajo), new Vector2(640f, 30f), misiones.pildora,
                                          Tema.Elegir(new Color(0f, 0f, 0f, 0.12f), RolDeTema.Surco), ConstructorUI.Verde);
            porcentaje = ConstructorUI.Texto(cartel, "Porcentaje", "", 46f, ConstructorUI.Verde,
                                             new Vector2(340f, yAbajo), new Vector2(160f, 60f), misiones.fuente);
            MostrarAvance();
        }
        else
        {
            bool lista = cual == Vista.Lista;
            float anchoFila = TamanioSecundario.x + EntreBotones + TamanioAccion.x;
            float xSecundario = -anchoFila / 2f + TamanioSecundario.x / 2f;
            float xAccion = anchoFila / 2f - TamanioAccion.x / 2f;

            var secundario = ConstructorUI.Boton(cartel, "Despues", new Vector2(xSecundario, yAbajo), TamanioSecundario,
                                                 Tema.Elegir(misiones.colorVidrio, RolDeTema.Vidrio), Color.white, misiones.iconoAtras,
                                                 lista ? Textos.De("actualizacion_mas_tarde") : Textos.De("actualizacion_ahora_no"), 40f,
                                                 misiones.fuente, misiones.pildora, misiones.sonidoClick);
            secundario.onClick.AddListener(lista ? (UnityEngine.Events.UnityAction)MasTarde : AhoraNo);

            var accion = ConstructorUI.Boton(cartel, "Accion", new Vector2(xAccion, yAbajo), TamanioAccion,
                                             ConstructorUI.Verde, ConstructorUI.VerdeTexto, lista ? iconoReiniciar : iconoActualizar,
                                             lista ? Textos.De("actualizacion_reiniciar") : Textos.De("actualizacion_actualizar"), 46f,
                                             misiones.fuente, misiones.pildora, misiones.sonidoClick);
            accion.onClick.AddListener(lista ? (UnityEngine.Events.UnityAction)Reiniciar : Actualizar);
            accion.GetComponent<BotonJugoso>().respirar = true;
        }

        vista = cual;
        desde = Time.unscaledTime;
        Abierto = true;
        Sonar(cual);
    }

    // La primera vez que sale cada cara en la visita: el jingle del cartel al ofrecerla y el
    // arpegio de cobrar una mision cuando esta lista. Al volver despues de cerrar la tienda, no.
    private void Sonar(Vista cual)
    {
        if (cual == Vista.Descargando || yaSono == cual) return;
        yaSono = cual;
        if (cual == Vista.Lista)
        {
            for (int k = 0; k < SemitonosFestejo.Length; k++)
                Sonidos.Programar(misiones.nota, 0.05 * k, 0.8f, Sonidos.PitchDe(SemitonosFestejo[k]));
        }
        if (misiones.sonidoFestejo != null) Sonidos.Tocar(misiones.sonidoFestejo, 0.6f);
    }

    private void MostrarAvance()
    {
        float avance = ActualizacionDePlay.Avance;
        if (relleno != null) relleno.anchorMax = new Vector2(Mathf.Clamp01(avance), 1f);
        if (porcentaje != null)
        {
            string texto = Mathf.FloorToInt(avance * 100f) + "%";
            if (porcentaje.text != texto) porcentaje.text = texto;
        }
    }

    private void Actualizar()
    {
        ActualizacionDePlay.Empezar();
        Quitar();
    }

    private void AhoraNo()
    {
        ActualizacionDePlay.Postergar();
        disponibleDescartada = true;
        Quitar();
    }

    private void Reiniciar()
    {
        ActualizacionDePlay.Completar();
        Quitar();
    }

    private void MasTarde()
    {
        listaDescartada = true;
        Quitar();
    }

    private void Quitar()
    {
        if (lienzo != null) Destroy(lienzo);
        lienzo = null;
        cartel = null;
        relleno = null;
        porcentaje = null;
        vista = Vista.Ninguna;
        Abierto = false;
    }
}
