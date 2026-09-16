using TMPro;
using UnityEngine;

// En la pantalla de derrota: cuantas monedas dejo la partida y cuantas hay en
// total. El "+N" cuenta desde 0 con ticks que suben por la escala de la bemol
// mayor y termina con un golpe y un destello: la derrota tambien es el momento de
// cobrar, y un numero que aparece quieto no se siente como una ganancia.
public class TextoMonedasPartida : MonoBehaviour
{
    public TMP_Text texto;
    public float demoraConteo = 0.3f;
    public float duracionConteo = 1.2f;
    public AudioClip tick;
    public float volumenTick = 0.45f;
    public float escalaFinal = 1.3f;

    private const float DuracionGolpe = 0.3f;

    // Delta con tope: la escena recien cargada tiene un primer frame largo que se
    // comeria la demora y parte del conteo.
    private const float DeltaMaximo = 1f / 30f;

    // Grados de la escala mayor en semitonos, dos octavas: el tick i suena en el
    // grado i. Con moneda.wav afinado en la bemol, la subida queda en la tonalidad
    // del juego. Tambien es el techo de ticks: mas seguidos se vuelven un zumbido.
    private static readonly float[] SemitonosTick = { 0f, 2f, 4f, 5f, 7f, 9f, 11f, 12f, 14f, 16f, 17f, 19f, 21f, 23f };

    private double desde;
    private double objetivo;
    private double total;
    private long mostradas = -1;

    private bool contando;
    private float tiempo;
    private int cantidadTicks;
    private int ticksTocados;

    private float progresoGolpe = -1f;  // -1 = sin golpe
    private Color colorBase;
    private Vector3 escalaBase;

    private void Start()
    {
        objetivo = Progreso.MonedasDeLaPartida;
        total = Progreso.Monedas;
        colorBase = texto.color;
        escalaBase = texto.rectTransform.localScale;

        // Sin monedas no hay nada que contar: el texto final, quieto.
        if (objetivo < 1)
        {
            Escribir((long)System.Math.Floor(System.Math.Max(0, objetivo)));
            return;
        }

        Arrancar(0, objetivo);
    }

    // Lo llama OfertaDeDuplicar cuando el video ya se vio y las monedas ya
    // entraron: el numero sube desde donde estaba hasta el nuevo, con los mismos
    // ticks. Contar de nuevo desde cero se leeria como si el premio fuera todo lo
    // que hay.
    public void Duplicar()
    {
        total = Progreso.Monedas;
        double nuevo = Progreso.MonedasDeLaPartida;
        if (nuevo <= objetivo)
        {
            Escribir((long)System.Math.Floor(System.Math.Max(0, nuevo)));
            return;
        }

        progresoGolpe = -1f;
        texto.rectTransform.localScale = escalaBase;
        texto.color = colorBase;
        Arrancar(objetivo, nuevo);
    }

    private void Arrancar(double desdeValor, double hasta)
    {
        desde = desdeValor;
        objetivo = hasta;
        tiempo = 0f;
        ticksTocados = 0;
        cantidadTicks = (int)System.Math.Min(SemitonosTick.Length, System.Math.Floor(objetivo - desde));
        contando = true;
        Escribir((long)System.Math.Floor(System.Math.Max(0, desde)));
    }

    private void Update()
    {
        if (!contando && progresoGolpe < 0f) return;

        float dt = Mathf.Min(Time.unscaledDeltaTime, DeltaMaximo);
        if (contando) Contar(dt);
        else Golpear(dt);
    }

    private void Contar(float dt)
    {
        tiempo += dt;
        if (tiempo < demoraConteo) return;

        float t = (tiempo - demoraConteo) / Mathf.Max(0.01f, duracionConteo);
        float k = CurvasUI.SalidaCubica(t);
        Escribir((long)System.Math.Floor(desde + (objetivo - desde) * k));

        // Los ticks se reparten por el recorrido del numero y no por el tiempo: con
        // la curva, al principio suenan seguidos y al final se espacian.
        while (cantidadTicks > 0 && ticksTocados < cantidadTicks && k >= (ticksTocados + 1f) / cantidadTicks)
        {
            Sonidos.Tocar(tick, volumenTick, Sonidos.PitchDe(SemitonosTick[ticksTocados]), 0f, 0f);
            ticksTocados++;
        }

        if (t < 1f) return;

        contando = false;
        Escribir((long)System.Math.Floor(objetivo));
        progresoGolpe = 0f;
    }

    private void Golpear(float dt)
    {
        RectTransform rect = texto.rectTransform;
        progresoGolpe += dt / DuracionGolpe;
        if (progresoGolpe >= 1f)
        {
            progresoGolpe = -1f;
            rect.localScale = escalaBase;
            texto.color = colorBase;
            return;
        }

        float campana = CurvasUI.Campana(progresoGolpe);
        rect.localScale = escalaBase * Mathf.Lerp(1f, escalaFinal, campana);
        texto.color = Color.Lerp(colorBase, Color.white, campana);
    }

    // Solo al cambiar el entero: armar el texto por frame aloca por frame.
    private void Escribir(long valor)
    {
        if (valor == mostradas) return;
        mostradas = valor;
        texto.text = "+" + FormatoNumeros.Compacto(valor) + " monedas  (total " + FormatoNumeros.Compacto(total) + ")";
    }
}
