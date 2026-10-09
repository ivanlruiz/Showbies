using System.Collections.Generic;
using TMPro;
using UnityEngine;

// "¡MISION CUMPLIDA!" en plena partida, con lo que pedia la mision debajo, un rebote y el
// jingle del cartel: es lo que hace que cada partida tenga un objetivo. Se cobra despues
// en el menu (VentanaMisiones).
//
// Mira las misiones del dia cada tanto (no hace falta en cada frame) y avisa las que se
// cumplen durante esta partida; las que ya estaban cumplidas al empezar no se repiten.
// Tambien avisa las estrellas del bestiario que se ganan jugando ("¡ESTRELLA!", en
// dorado, con el tipo y el escalon), los logros ("¡LOGRO DE PLATA!", del color de su
// moneda), cada nivel del jugador que se sube ("¡NIVEL 13!", con el premio que espera
// en el menu), el desbloqueo del modo libre y, en Halloween, cada hito de caramelos que se
// alcanza (EventoHalloween). Si llegan dos a la vez, salen una despues de otra.
//
// Y lo que antes pasaba en silencio (revision del 9/10, mejora 5): "¡DESAFIO SEMANAL
// CUMPLIDO!", el premio mas grande del juego, al cumplirse en la partida, y lo que se cobro
// solo al empezarla (el cierre del dia o de la semana: CobrosSolos), si el menu no llego a
// mostrarlo.
//
// Y el progreso de la partida (revision del 9/10): en las oleadas, "¡TE LLEGA PARA UNA
// MEJORA!" cuando las monedas juntadas alcanzan para comprar algo, solo en el descanso entre
// oleadas (ir a la tienda desde la pausa ahi no hace perder nada; en plena horda distrae), y
// "¡NUEVO RECORD!" en el momento de pasar la mejor marca, con el festejo del cofre. Sin esto,
// el MEJORAS de la pausa casi no se usaba, y el record se enteraba recien en la derrota.
// Va en ShowBies1 y WaveMode (objeto AvisoDeMisiones), con el texto armado en codigo
// sobre el canvas del HUD.
public class AvisoDeMisiones : MonoBehaviour
{
    public Canvas canvas;
    public TMP_FontAsset fuente;
    public Material materialContorno;
    public AudioClip sonido;                 // cartel.wav
    public float cadaCuanto = 0.3f;
    public float duracion = 2.6f;

    public Color colorMision = new Color(0.72f, 0.56f, 1f);
    public Color colorEstrella = new Color(1f, 0.8f, 0.2f);
    public Color colorNivel = new Color(0.4f, 0.85f, 1f);
    public Color colorHalloween = new Color(1f, 0.55f, 0.1f);
    public Color colorMejora = new Color(1f, 0.882f, 0.302f);   // el amarillo de la tienda
    public Color colorRecord = new Color(1f, 0.43f, 0.85f);

    // Cada cuantas oleadas, como mucho, vuelve a avisar que alcanza para mas mejoras.
    public const int OleadasEntreAvisosDeMejora = 3;

    // Donde sale, desde el centro de la pantalla, y su letra. Debajo del jugador: arriba
    // estan el cartel de la oleada (170), la barra del jefe (arriba de 320) y el cartel del
    // capitulo (385), y los tres pueden salir a la vez que este. Hasta el 23/9 iba en 300 y
    // se pisaba con los dos ultimos; "completa N oleadas" se cumple justo al terminar la
    // oleada, que es cuando sale el cartel de la siguiente, y en la 10, 20 y 30 tambien el
    // del capitulo. Mas abajo esta la vida. La prueba de logica mide que no se pisen.
    public const float Altura = -225f;
    public const float Letra = 72f;
    public const float LetraDelDetalle = 0.55f;   // del titulo

    private struct Aviso
    {
        public string titulo;
        public string detalle;
        public Color color;
        public bool festejo;
    }

    private readonly HashSet<int> yaCumplidas = new HashSet<int>();
    private readonly Queue<Aviso> pendientes = new Queue<Aviso>();
    private readonly int[] estrellasVistas = new int[Bestiario.Tipos.Length];
    private TMP_Text cartel;
    private float proximaRevision;
    private float mostrandoDesde = -1f;
    private int diaVisto;

    private int nivelVisto;
    private bool libreVisto;
    private bool semanalVisto;
    private int hitosVistos;

    private WaveManager oleadas;
    private int revisionDeLasCompras = -1;
    private int comprasAvisadas;
    private bool mejoraPorAvisar;
    private int oleadaDelAvisoDeMejora = -1000;   // ninguno todavia
    private int recordAlEmpezar;
    private bool recordAvisado;

    private void Start()
    {
        MisionesDiarias.Asegurar();
        DesafioSemanal.Asegurar();
        Anotar(true);
        semanalVisto = DesafioSemanal.Cumplido;
        for (int i = 0; i < estrellasVistas.Length; i++) estrellasVistas[i] = Bestiario.Alcanzadas(Bestiario.Tipos[i]);
        // Los logros que se ganaron fuera de una partida (en el menu, comprando el
        // critico) se anotan sin aviso: no se ganaron aca.
        Logros.Revisar();
        nivelVisto = NivelJugador.Nivel;
        libreVisto = ModoLibre.Desbloqueado;
        hitosVistos = EventoHalloween.Activo ? EventoHalloween.Alcanzados : 0;

        // Lo que ya alcanzaba al empezar no se avisa: venia de la tienda y no compro.
        oleadas = FindAnyObjectByType<WaveManager>();
        revisionDeLasCompras = Progreso.Revision;
        comprasAvisadas = CatalogoMejoras.ComprasPosibles();
        // La marca a pasar: la mejor oleada en las oleadas, los puntos en el libre.
        recordAlEmpezar = oleadas != null ? Progreso.MejorOleada
                                          : PlayerPrefs.GetInt(PlayerHealth.ClaveRecord(gameObject.scene.buildIndex), 0);
    }

    // Cuando lo juntado alcanza para mas compras que lo ya avisado. Se anota ahora y sale en
    // el descanso de la oleada, como mucho cada OleadasEntreAvisosDeMejora.
    private void AnotarMejoras()
    {
        if (oleadas == null) return;
        if (Progreso.Revision != revisionDeLasCompras)
        {
            revisionDeLasCompras = Progreso.Revision;
            int compras = CatalogoMejoras.ComprasPosibles();
            if (compras > comprasAvisadas) mejoraPorAvisar = true;
            comprasAvisadas = Mathf.Max(comprasAvisadas, compras);
        }
        if (!mejoraPorAvisar || !oleadas.EnDescanso) return;
        if (oleadas.OleadaActual - oleadaDelAvisoDeMejora < OleadasEntreAvisosDeMejora) return;
        mejoraPorAvisar = false;
        oleadaDelAvisoDeMejora = oleadas.OleadaActual;
        int ahora = CatalogoMejoras.ComprasPosibles();
        pendientes.Enqueue(new Aviso
        {
            titulo = ahora == 1 ? Textos.De("aviso_compras_una") : Textos.Formato("aviso_compras_varias", ahora),
            detalle = Textos.De(Plataforma.EsMovil ? "aviso_compras_pausa" : "aviso_compras_pausa_pc"),
            color = colorMejora,
        });
    }

    // Una sola vez por partida, al pasar la mejor marca (la de antes de empezar: sin marca,
    // la primera partida, no hay nada que festejar).
    private void AnotarRecord()
    {
        if (recordAvisado || recordAlEmpezar <= 0) return;
        bool paso = oleadas != null ? Progreso.MejorOleada > recordAlEmpezar
                                    : Puntaje.instance != null && Puntaje.instance.contadorKill > recordAlEmpezar;
        if (!paso) return;
        recordAvisado = true;
        pendientes.Enqueue(new Aviso
        {
            titulo = Textos.De("aviso_record"),
            detalle = oleadas != null ? Textos.Formato("aviso_record_oleada", recordAlEmpezar)
                                      : Textos.Formato("aviso_record_puntos", FormatoNumeros.Compacto(recordAlEmpezar)),
            color = colorRecord,
            festejo = true,
        });
    }

    // El desafio de la semana, una vez, al cumplirse en esta partida.
    private void AnotarSemanal()
    {
        if (semanalVisto || !DesafioSemanal.Cumplido) return;
        semanalVisto = true;
        pendientes.Enqueue(new Aviso
        {
            titulo = Textos.De("aviso_semanal"),
            detalle = Textos.Formato("aviso_nivel_premio", FormatoNumeros.Compacto(DesafioSemanal.MontoDeEstaSemana)),
            color = colorEstrella,
            festejo = true,
        });
    }

    // Lo que se cobro solo (CobrosSolos) y el menu no llego a mostrar: un aviso por cierre.
    private void AnotarCobrosSolos()
    {
        if (!CobrosSolos.Hay) return;
        foreach (var cobro in CobrosSolos.Tomar())
        {
            pendientes.Enqueue(new Aviso
            {
                titulo = Textos.De("cobro_solo_titulo"),
                detalle = CobrosSolos.Renglon(cobro),
                color = colorEstrella,
            });
        }
    }

    // Los hitos de Halloween que se alcanzan en esta partida, con lo que espera en el menu.
    private void AnotarHalloween()
    {
        if (!EventoHalloween.Activo) return;
        int alcanzados = EventoHalloween.Alcanzados;
        // Si la edicion empezo en plena partida (la medianoche del 23/10), arranca de cero.
        if (hitosVistos > alcanzados) hitosVistos = alcanzados;
        while (hitosVistos < alcanzados)
        {
            int hito = hitosVistos++;
            pendientes.Enqueue(new Aviso
            {
                titulo = Textos.De("halloween_aviso"),
                detalle = EventoHalloween.EsElSombrero(hito)
                    ? Textos.De("halloween_aviso_sombrero")
                    : Textos.Formato("halloween_aviso_monedas", FormatoNumeros.Compacto(EventoHalloween.Premio(hito))),
                color = colorHalloween,
            });
        }
    }

    // El modo libre se gana llegando a la oleada 12 (ModoLibre), y hasta el 25/9 llegaba en
    // silencio: el unico cambio era el color de su boton en el panel de modos, que OTRA VEZ
    // y ¡A JUGAR! no muestran. Sale una sola vez, en la partida en que se gana: en la
    // siguiente ya arranca desbloqueado.
    private void AnotarModoLibre()
    {
        if (libreVisto || !ModoLibre.Desbloqueado) return;
        libreVisto = true;
        pendientes.Enqueue(new Aviso
        {
            titulo = Textos.De("aviso_libre"),
            detalle = Textos.De("aviso_libre_detalle"),
            color = colorEstrella,
        });
    }

    // Los logros nuevos, uno por moneda, del color de su escalon.
    private void AnotarLogros()
    {
        foreach (var logro in Logros.Revisar())
        {
            var familia = Logros.Buscar(logro.familia);
            if (familia == null) continue;
            pendientes.Enqueue(new Aviso
            {
                titulo = Textos.De(TitulosDeLogro[Mathf.Clamp(logro.escalon, 0, TitulosDeLogro.Length - 1)]),
                detalle = Logros.Nombre(logro.familia) + ": " + Logros.Descripcion(logro.familia, familia.metas[logro.escalon]),
                color = ColoresDeLogro.De(logro.escalon),
            });
        }
    }

    private static readonly string[] TitulosDeLogro = { "aviso_logro_bronce", "aviso_logro_plata", "aviso_logro_oro" };

    // Cada nivel que se subio, con el premio que quedo esperando en el menu.
    private void AnotarNivel()
    {
        int nivel = NivelJugador.Nivel;
        while (nivelVisto < nivel)
        {
            nivelVisto++;
            var premio = NivelJugador.PendienteDe(nivelVisto);
            pendientes.Enqueue(new Aviso
            {
                titulo = Textos.Formato("aviso_nivel", nivelVisto),
                detalle = premio != null ? Textos.Formato("aviso_nivel_premio", FormatoNumeros.Compacto(NivelJugador.Monto(premio))) : "",
                color = colorNivel,
            });
        }
    }

    // Las estrellas nuevas desde la ultima mirada, una por escalon cruzado.
    private void AnotarEstrellas()
    {
        for (int i = 0; i < estrellasVistas.Length; i++)
        {
            string tipo = Bestiario.Tipos[i];
            int alcanzadas = Bestiario.Alcanzadas(tipo);
            var escalones = Bestiario.Escalones(tipo);
            while (estrellasVistas[i] < alcanzadas)
            {
                int escalon = escalones[estrellasVistas[i]];
                estrellasVistas[i]++;
                pendientes.Enqueue(new Aviso
                {
                    titulo = Textos.De("aviso_estrella"),
                    detalle = Bestiario.Nombre(tipo) + " x" + FormatoNumeros.Compacto(escalon),
                    color = colorEstrella,
                });
            }
        }
    }

    // Las cumplidas pasan a yaCumplidas; con avisar, las nuevas van a la fila de avisos.
    private void Anotar(bool alEmpezar)
    {
        var estado = Progreso.Misiones;
        if (estado.dia != diaVisto)
        {
            // Cambio el dia en plena partida: las nuevas empiezan de cero.
            diaVisto = estado.dia;
            yaCumplidas.Clear();
            alEmpezar = true;
        }

        var misiones = MisionesDiarias.DeHoy;
        for (int i = 0; i < misiones.Count; i++)
        {
            if (yaCumplidas.Contains(i) || !MisionesDiarias.Cumplida(misiones[i])) continue;
            yaCumplidas.Add(i);
            if (!alEmpezar) pendientes.Enqueue(new Aviso
            {
                titulo = Textos.De("mision_cumplida"),
                detalle = MisionesDiarias.Descripcion(misiones[i]),
                color = colorMision,
            });
        }
    }

    private void Update()
    {
        float t = Time.unscaledTime;
        if (t >= proximaRevision && !MenuPausa.JuegoCongelado)
        {
            proximaRevision = t + cadaCuanto;
            AnotarCobrosSolos();
            AnotarModoLibre();
            Anotar(false);
            AnotarSemanal();
            AnotarEstrellas();
            AnotarLogros();
            AnotarNivel();
            AnotarHalloween();
            AnotarRecord();
            AnotarMejoras();
        }

        if (cartel == null && pendientes.Count > 0 && canvas != null) Mostrar(pendientes.Dequeue(), t);
        if (cartel == null) return;

        float edad = t - mostrandoDesde;
        float entrada = Mathf.Clamp01(edad / 0.35f);
        float salida = Mathf.Clamp01((edad - (duracion - 0.3f)) / 0.3f);
        cartel.rectTransform.localScale = Vector3.one * CurvasUI.SalidaAtras(entrada) * (1f - salida) * (1f + 0.04f * Mathf.Sin(t * 6f));
        if (edad >= duracion)
        {
            Destroy(cartel.gameObject);
            cartel = null;
        }
    }

    private void Mostrar(Aviso aviso, float t)
    {
        var go = new GameObject("MisionCumplida", typeof(RectTransform), typeof(TextMeshProUGUI));
        var rt = (RectTransform)go.transform;
        rt.SetParent(canvas.transform, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, Altura);
        rt.sizeDelta = new Vector2(1200f, 180f);
        cartel = go.GetComponent<TextMeshProUGUI>();
        if (fuente != null) cartel.font = fuente;
        if (materialContorno != null) cartel.fontSharedMaterial = materialContorno;
        cartel.text = aviso.titulo + "\n<size=" + Mathf.RoundToInt(LetraDelDetalle * 100f) + "%>" + aviso.detalle + "</size>";
        cartel.fontSize = Letra;
        cartel.color = aviso.color;
        cartel.alignment = TextAlignmentOptions.Center;
        cartel.textWrappingMode = TextWrappingModes.NoWrap;
        cartel.raycastTarget = false;
        mostrandoDesde = t;
        if (sonido != null) Sonidos.Tocar(sonido, 0.7f);
        if (aviso.festejo) Efectos.Festejo();
        CamaraJugador.Temblar(aviso.festejo ? 0.3f : 0.15f);
    }
}
