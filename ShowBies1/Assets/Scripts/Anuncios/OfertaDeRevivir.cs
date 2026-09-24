using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Lo que pasa cuando el jugador muere y hay un video para ofrecerle: el juego se
// congela donde estaba, la pantalla se va agrisando y aparece una ventanita con
// "¡HAS MUERTO!" y un botón de video con un reloj que se cierra. Si toca el video
// y lo mira entero, vuelve a jugar en el mismo lugar; si dice que no o se le acaba
// el tiempo, recién ahí se carga la pantalla de derrota.
//
// Es el momento más caro del juego (perdiste todo lo de la partida) y por eso es
// el que más se presta a apretar al jugador. Las reglas:
//
// - **La cuenta atrás no es una trampa**: el botón NO, GRACIAS está desde el
//   primer segundo y es igual de grande de leer. Al vencerse el reloj no pasa nada
//   malo: es exactamente lo mismo que decir que no.
// - **Una sola vez por partida** (`PlayerHealth.yaRevivio`). Un revivir por video
//   sin límite es una partida que no termina nunca y una tienda que no hace falta.
// - **Revivir no da nada más que seguir jugando**: los zombis despejados no dan
//   puntos ni monedas.
// - **Si no hay video, esto no existe**: se muere como siempre, sin ventanita, sin
//   "mirá un video" en gris y sin enterarse de que los anuncios existen.
//
// Vive en el prefab Prefabs/UI/OfertaRevivir, puesto en ShowBies1 y WaveMode.
public class OfertaDeRevivir : MonoBehaviour
{
    [Tooltip("Todo lo que se ve: arranca apagado.")]
    public GameObject panel;
    [Tooltip("El velo que agrisa la pantalla.")]
    public Image velo;
    [Tooltip("La ventanita, para el rebote de entrada.")]
    public RectTransform ventana;
    public Button botonVideo;
    public Button botonNo;
    [Tooltip("El anillo del reloj: Image Filled Radial360.")]
    public Image anillo;
    [Tooltip("Los segundos que faltan, adentro del botón.")]
    public TMP_Text segundos;
    [Tooltip("El círculo de fondo del botón de video.")]
    public Image circulo;
    [Tooltip("La claqueta.")]
    public Image icono;

    [Header("Números")]
    [Tooltip("Cuánto oscurece el velo. El grueso del efecto lo pone el blanco y negro: "
        + "con el velo muy oscuro no se ve que la pantalla perdió el color.")]
    public float alfaDelVelo = 0.45f;
    public float duracionRebote = 0.35f;

    [Tooltip("Segundos finales en los que el reloj late y se pone rojo.")]
    public float segundosDeApuro = 3f;

    [Header("Sonido")]
    [Tooltip("Suena al aparecer la ventanita. Grave, que es una mala noticia.")]
    public AudioClip sonido;
    public float volumenSonido = 0.7f;
    public float semitonosSonido = -5f;

    private static OfertaDeRevivir instancia;

    // Mientras esta la ventanita el juego esta congelado con timeScale 0 y el
    // jugador, muerto. Lo mira MenuPausa para no pausar encima: reanudar desde el
    // menu de pausa devolveria el timeScale a 1 y el juego seguiria con el jugador
    // muerto en pantalla.
    public static bool Activa { get; private set; }

    private PlayerHealth jugador;
    private bool corriendo;

    // Se toco el video y se esta esperando el resultado. El reloj se congela: si
    // siguiera corriendo, un video de mas de lo que queda de cuenta mandaria a la
    // derrota a alguien que lo miro entero, y encima le gastaria el video.
    private bool esperandoVideo;
    private float desde;
    private float aceptadoEn = -1f;   // cuando toco el video, para devolverle su tiempo
    private float ignorarAtrasHasta;  // el atras que cerro el video no rechaza
    private float duracion;
    private float agrisado;
    private float radioDespeje;
    private float gracia;
    private int ultimoSegundoEscrito = -1;

    // La pantalla se queda en blanco y negro mientras dura la oferta. Es un filtro
    // en la camara, asi que la ventanita (UI en overlay) sigue a color.
    private FiltroBlancoYNegro filtro;
    private float grisInicial;

    // Para devolver el anillo como estaba: lo late el apuro y lo tiñe de rojo.
    private Color colorDelAnillo;
    private bool guardoElColor;

    // Los sprites se dibujan en código (TexturasUI) para no sumar imágenes al
    // proyecto: quien los pide es dueño de las texturas y las destruye.
    private Texture2D texturaCirculo;
    private Texture2D texturaAnillo;
    private Texture2D texturaIcono;
    private Sprite spriteCirculo;
    private Sprite spriteAnillo;
    private Sprite spriteIcono;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetearEstadoCompartido()
    {
        instancia = null;
        Activa = false;
    }

    // La llama PlayerHealth al morir. Devuelve si la oferta se hizo cargo: si da
    // falso, el que muere sigue con la muerte de siempre.
    public static bool Ofrecer(PlayerHealth quienMurio)
    {
        if (instancia == null || quienMurio == null) return false;
        return instancia.Mostrar(quienMurio);
    }

    private void Awake()
    {
        instancia = this;
        if (panel != null) panel.SetActive(false);

        texturaCirculo = TexturasUI.Circulo(128);
        texturaAnillo = TexturasUI.Anillo(256, 0.18f);
        texturaIcono = TexturasUI.Claqueta(160);

        spriteCirculo = SpriteDe(texturaCirculo);
        spriteAnillo = SpriteDe(texturaAnillo);
        spriteIcono = SpriteDe(texturaIcono);
        Vestir(circulo, spriteCirculo);
        Vestir(anillo, spriteAnillo);
        Vestir(icono, spriteIcono);

        if (botonVideo != null) botonVideo.onClick.AddListener(Aceptar);
        if (botonNo != null) botonNo.onClick.AddListener(Rechazar);
    }

    private void OnDestroy()
    {
        // Si la escena se descarga con la oferta abierta, el timeScale es global y
        // se llevaria el cero a la escena siguiente.
        if (corriendo)
        {
            corriendo = false;
            Activa = false;
            if (filtro != null) filtro.Soltar();
            if (!MenuPausa.Pausado) Time.timeScale = 1f;
        }
        if (instancia == this) instancia = null;
        if (spriteCirculo != null) Destroy(spriteCirculo);
        if (spriteAnillo != null) Destroy(spriteAnillo);
        if (spriteIcono != null) Destroy(spriteIcono);
        if (texturaCirculo != null) Destroy(texturaCirculo);
        if (texturaAnillo != null) Destroy(texturaAnillo);
        if (texturaIcono != null) Destroy(texturaIcono);
    }

    // El sprite tambien hay que destruirlo, no solo la textura.
    private static Sprite SpriteDe(Texture2D textura)
    {
        if (textura == null) return null;
        return Sprite.Create(textura, new Rect(0f, 0f, textura.width, textura.height),
                             new Vector2(0.5f, 0.5f), 100f, 0u, SpriteMeshType.FullRect);
    }

    private static void Vestir(Image imagen, Sprite sprite)
    {
        if (imagen == null || sprite == null) return;
        imagen.sprite = sprite;
    }

    private bool Mostrar(PlayerHealth quienMurio)
    {
        ConfigAnuncios config = ServicioAnuncios.ConfigEnUso;
        if (config == null) return false;
        if (quienMurio.SegundosDePartida < config.segundosDeLaPartidaParaRevivir) return false;
        if (!ServicioAnuncios.PuedeOfrecer(LugarAnuncio.Revivir)) return false;

        jugador = quienMurio;
        duracion = config.segundosParaDecidirRevivir;
        agrisado = config.segundosDeAgrisado;
        radioDespeje = config.radioDeDespeje;
        gracia = config.segundosDeGracia;

        desde = Time.unscaledTime;
        corriendo = true;
        esperandoVideo = false;
        ultimoSegundoEscrito = -1;

        if (velo != null) velo.color = ColorDelVelo(0f);
        // Como este: con poca vida el mundo ya venia perdiendo color (GrisDePocaVida), y
        // volver al color un cuadro para agrisar de nuevo se ve como un parpadeo.
        filtro = FiltroBlancoYNegro.Tomar(Camera.main);
        grisInicial = filtro != null ? filtro.cantidad : 0f;
        if (anillo != null) anillo.fillAmount = 1f;
        if (ventana != null) ventana.localScale = Vector3.zero;
        if (botonVideo != null) botonVideo.interactable = true;
        if (botonNo != null) botonNo.interactable = true;
        if (panel != null) panel.SetActive(true);

        if (anillo != null)
        {
            if (!guardoElColor) { colorDelAnillo = anillo.color; guardoElColor = true; }
            anillo.color = colorDelAnillo;
            anillo.rectTransform.localScale = Vector3.one;
        }

        // Tiempo sin escalar y sin separacion minima: es el unico sonido que suena
        // en este momento, y el juego ya esta congelado.
        Sonidos.Tocar(sonido, volumenSonido, Sonidos.PitchDe(semitonosSonido), 0f, 0f);

        // El juego queda donde estaba: los zombis congelados encima y la partida
        // esperando. Todo lo de esta ventana usa tiempo sin escalar.
        Activa = true;
        Time.timeScale = 0f;
        return true;
    }

    private void Update()
    {
        if (!corriendo || esperandoVideo) return;

        // El atras de Android llega como Escape: aca es NO, GRACIAS, que no castiga. Era la
        // unica pantalla donde el atras no hacia nada (MenuPausa no la pausa) y se leia como
        // que el juego se habia colgado. Un rato despues de volver de un video no cuenta: el
        // atras que cerro el anuncio podria llegar tambien a la actividad de Unity.
        if (Input.GetKeyDown(KeyCode.Escape) && Time.unscaledTime >= ignorarAtrasHasta)
        {
            Rechazar();
            return;
        }

        float pasado = Time.unscaledTime - desde;

        float grisado = agrisado > 0f ? Mathf.Clamp01(pasado / agrisado) : 1f;
        if (velo != null) velo.color = ColorDelVelo(grisado);
        if (filtro != null) filtro.cantidad = Mathf.Lerp(grisInicial, 1f, grisado);

        if (ventana != null)
        {
            float t = duracionRebote > 0f ? Mathf.Clamp01(pasado / duracionRebote) : 1f;
            ventana.localScale = Vector3.one * CurvasUI.SalidaAtras(t);
        }

        float restante = Mathf.Max(0f, duracion - pasado);
        if (anillo != null) anillo.fillAmount = duracion > 0f ? restante / duracion : 0f;

        // Los ultimos segundos el reloj late y se pone rojo: no es para apurar la
        // decision (que se venza es lo mismo que decir que no), es para que nadie se
        // quede mirando sin enterarse de que se le acaba.
        if (anillo != null && guardoElColor)
        {
            bool apura = restante <= segundosDeApuro && restante > 0f;
            if (apura)
            {
                float latido = 1f + 0.08f * Mathf.Abs(Mathf.Sin(pasado * 6f));
                anillo.rectTransform.localScale = Vector3.one * latido;
                anillo.color = Color.Lerp(colorDelAnillo, new Color(1f, 0.3f, 0.25f, 1f),
                                          Mathf.PingPong(pasado * 3f, 1f));
            }
            else
            {
                anillo.rectTransform.localScale = Vector3.one;
                anillo.color = colorDelAnillo;
            }
        }

        int entero = Mathf.CeilToInt(restante);
        if (segundos != null && entero != ultimoSegundoEscrito)
        {
            ultimoSegundoEscrito = entero;
            segundos.text = entero.ToString();
        }

        // Que se venza es lo mismo que decir que no: no hay castigo por dudar.
        if (restante <= 0f) Rechazar();
    }

    private Color ColorDelVelo(float t)
    {
        Color color = velo.color;
        color.a = Mathf.Lerp(0f, alfaDelVelo, t);
        return color;
    }

    private void Aceptar()
    {
        if (!corriendo || esperandoVideo) return;

        esperandoVideo = true;
        if (botonVideo != null) botonVideo.interactable = false;
        if (botonNo != null) botonNo.interactable = false;

        aceptadoEn = Time.unscaledTime;

        if (!ServicioAnuncios.Mostrar(LugarAnuncio.Revivir, Volver, SinPremio))
        {
            // Dejó de haber video entre que murió y que tocó: no se le cobra la duda, pero
            // tampoco tiene sentido volver a ofrecérselo.
            SinPremio();
            if (botonVideo != null) botonVideo.interactable = false;
        }
    }

    // El video se cerró antes, no había, o falló sin premio. **No termina la partida**: el
    // jugador vuelve a la ventanita con los segundos que le quedaban y decide él.
    //
    // Antes esto era `Rechazar`, o sea que cerrar el video —o que la red fallara al
    // mostrarlo— mandaba derecho a la pantalla de derrota. Va contra la regla de que cerrar
    // el video antes no castiga, y es el peor momento para romperle la partida a alguien:
    // justo cuando se le pidió que mirara un anuncio.
    private void SinPremio()
    {
        if (!corriendo) return;
        esperandoVideo = false;

        // Mientras se miraba el video el reloj estaba congelado (Update corta con
        // `esperandoVideo`) pero `Time.unscaledTime` no: sin correr el origen, `pasado`
        // pega un salto del largo del video, la cuenta atras se vence en el acto y termina
        // la partida igual, por otro camino.
        if (aceptadoEn >= 0f) desde += Time.unscaledTime - aceptadoEn;
        aceptadoEn = -1f;
        ignorarAtrasHasta = Time.unscaledTime + 0.4f;

        if (botonVideo != null) botonVideo.interactable = true;
        if (botonNo != null) botonNo.interactable = true;
    }

    private void Volver()
    {
        if (!corriendo) return;
        corriendo = false;
        esperandoVideo = false;
        aceptadoEn = -1f;

        Esconder();
        Time.timeScale = 1f;
        if (jugador != null) jugador.Revivir(radioDespeje, gracia);
        jugador = null;
    }

    private void Rechazar()
    {
        if (!corriendo) return;
        corriendo = false;
        esperandoVideo = false;
        aceptadoEn = -1f;

        // El gris queda como esta: la derrota va encima de la partida
        // (DerrotaEnLaPartida), la descongela y sigue el gris desde donde quedo. Si se
        // soltara el filtro, el mundo volveria al color un cuadro antes de la derrota.
        Esconder(false);

        PlayerHealth quienMurio = jugador;
        jugador = null;
        if (quienMurio != null) quienMurio.Terminar();
        else Time.timeScale = 1f;
    }

    private void Esconder(bool soltarElGris = true)
    {
        Activa = false;
        if (panel != null) panel.SetActive(false);
        if (filtro != null && soltarElGris) filtro.Soltar();
        filtro = null;
    }
}
