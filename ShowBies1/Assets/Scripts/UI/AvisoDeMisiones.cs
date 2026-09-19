using System.Collections.Generic;
using TMPro;
using UnityEngine;

// "¡MISION CUMPLIDA!" en plena partida, con lo que pedia la mision debajo, un rebote y el
// jingle del cartel: es lo que hace que cada partida tenga un objetivo. Se cobra despues
// en el menu (VentanaMisiones).
//
// Mira las misiones del dia cada tanto (no hace falta en cada frame) y avisa las que se
// cumplen durante esta partida; las que ya estaban cumplidas al empezar no se repiten.
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

    private readonly HashSet<int> yaCumplidas = new HashSet<int>();
    private readonly Queue<string> pendientes = new Queue<string>();
    private TMP_Text cartel;
    private float proximaRevision;
    private float mostrandoDesde = -1f;
    private int diaVisto;

    private void Start()
    {
        MisionesDiarias.Asegurar();
        Anotar(true);
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
            if (!alEmpezar) pendientes.Enqueue(MisionesDiarias.Descripcion(misiones[i]));
        }
    }

    private void Update()
    {
        float t = Time.unscaledTime;
        if (t >= proximaRevision && !MenuPausa.JuegoCongelado)
        {
            proximaRevision = t + cadaCuanto;
            Anotar(false);
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

    private void Mostrar(string descripcion, float t)
    {
        var go = new GameObject("MisionCumplida", typeof(RectTransform), typeof(TextMeshProUGUI));
        var rt = (RectTransform)go.transform;
        rt.SetParent(canvas.transform, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, 300f);
        rt.sizeDelta = new Vector2(1200f, 180f);
        cartel = go.GetComponent<TextMeshProUGUI>();
        if (fuente != null) cartel.font = fuente;
        if (materialContorno != null) cartel.fontSharedMaterial = materialContorno;
        cartel.text = Textos.De("mision_cumplida") + "\n<size=55%>" + descripcion + "</size>";
        cartel.fontSize = 72f;
        cartel.color = new Color(0.72f, 0.56f, 1f);
        cartel.alignment = TextAlignmentOptions.Center;
        cartel.textWrappingMode = TextWrappingModes.NoWrap;
        cartel.raycastTarget = false;
        mostrandoDesde = t;
        if (sonido != null) Sonidos.Tocar(sonido, 0.7f);
        CamaraJugador.Temblar(0.15f);
    }
}
