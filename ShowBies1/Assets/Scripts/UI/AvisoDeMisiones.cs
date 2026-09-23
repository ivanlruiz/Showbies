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
// dorado, con el tipo y el escalon). Si llegan dos a la vez, salen una despues de otra.
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
    }

    private readonly HashSet<int> yaCumplidas = new HashSet<int>();
    private readonly Queue<Aviso> pendientes = new Queue<Aviso>();
    private readonly int[] estrellasVistas = new int[Bestiario.Tipos.Length];
    private TMP_Text cartel;
    private float proximaRevision;
    private float mostrandoDesde = -1f;
    private int diaVisto;

    private void Start()
    {
        MisionesDiarias.Asegurar();
        Anotar(true);
        for (int i = 0; i < estrellasVistas.Length; i++) estrellasVistas[i] = Bestiario.Alcanzadas(Bestiario.Tipos[i]);
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
            Anotar(false);
            AnotarEstrellas();
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
        CamaraJugador.Temblar(0.15f);
    }
}
