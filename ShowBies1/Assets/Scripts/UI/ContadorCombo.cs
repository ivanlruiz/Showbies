using TMPro;
using UnityEngine;

// "COMBO x12" en el HUD: cuenta los zombis que mueren seguidos, sin que pasen mas
// de ventana segundos entre uno y otro. Aparece desde minimoParaMostrar, salta
// con cada muerte, cambia de color cada muertesPorColor y se desvanece cuando se
// corta la racha.
//
// Las muertes llegan por RegistrarMuerte (lo llama Efectos.Muerte) a un contador
// static y se procesan en Update: asi no importa cuantas mueran en un frame.
//
// Suena: cada salto toca una nota que sube la escala de la bemol mayor (una por frame
// como mucho, asi veinte muertes por segundo no son ruido), y al cruzar un hito (x10,
// x25, x50, x100) un arpegio, un temblor y el cartel con su palabra ("¡MASACRE!") un
// rato. Solo efecto: los hitos no dan monedas.
public class ContadorCombo : MonoBehaviour
{
    public TMP_Text texto;
    public float ventana = 1.5f;
    public int minimoParaMostrar = 3;
    public int muertesPorColor = 10;
    public Color[] colores =
    {
        Color.white,
        new Color(1f, 0.9f, 0.2f),
        new Color(1f, 0.55f, 0.1f),
        new Color(1f, 0.25f, 0.6f),
        new Color(0.6f, 0.4f, 1f),
    };
    public float escalaDelSalto = 1.5f;
    public float duracionDelSalto = 0.15f;
    public float desvanecido = 0.4f;          // segundos del final de la ventana en que se va apagando

    [Header("Sonido y hitos")]
    public AudioClip nota;                    // combo.wav, en la bemol
    [Range(0f, 1f)] public float volumenNota = 0.35f;
    public int[] hitos = { 10, 25, 50, 100 };
    public float duracionHito = 1.3f;
    public float escalaDelHito = 2.2f;
    public float temblorDelHito = 0.3f;

    // La bemol mayor en semitonos, dos octavas: cada salto del combo sube un grado.
    private static readonly int[] Escala = { 0, 2, 4, 5, 7, 9, 11, 12, 14, 16, 17, 19, 21, 23, 24 };
    private float hitoHasta = float.NegativeInfinity;
    private string palabraDelHito;
    private bool saltoDeHito;

    private static int muertesSinProcesar;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetearEstadoCompartido()
    {
        muertesSinProcesar = 0;
    }

    public static void RegistrarMuerte()
    {
        muertesSinProcesar++;
    }

    private int combo;
    private float ultimaMuerte = float.NegativeInfinity;
    private float salto;
    private Vector3 escalaBase;
    private Color colorActual;

    private void Awake()
    {
        escalaBase = texto.rectTransform.localScale;
        texto.enabled = false;
        // Las que se registraron en otra escena, o sin HUD, no cuentan aca.
        muertesSinProcesar = 0;
    }

    private void Update()
    {
        if (MenuPausa.Pausado) return;

        if (muertesSinProcesar > 0)
        {
            if (Time.time - ultimaMuerte > ventana) combo = 0;
            int anterior = combo;
            combo += muertesSinProcesar;
            muertesSinProcesar = 0;
            ultimaMuerte = Time.time;

            if (combo >= minimoParaMostrar)
            {
                colorActual = colores[Mathf.Min(combo / Mathf.Max(1, muertesPorColor), colores.Length - 1)];
                salto = 1f;
                Sonar(combo - minimoParaMostrar);

                int hito = HitoCruzado(hitos, anterior, combo);
                if (hito > 0) Festejar(hito);

                if (Time.unscaledTime < hitoHasta) texto.SetText(palabraDelHito + "  x{0}", combo);
                else texto.SetText(Textos.De("hud_combo"), combo);
                texto.enabled = true;
            }
        }

        if (!texto.enabled) return;

        float resto = ventana - (Time.time - ultimaMuerte);
        if (resto <= 0f)
        {
            combo = 0;
            texto.enabled = false;
            return;
        }

        Color c = colorActual;
        c.a = Mathf.Clamp01(resto / desvanecido);
        texto.color = c;

        if (salto > 0f) salto = Mathf.Max(0f, salto - Time.unscaledDeltaTime / duracionDelSalto);
        else saltoDeHito = false;
        float pico = saltoDeHito ? escalaDelHito : escalaDelSalto;
        texto.rectTransform.localScale = escalaBase * Mathf.Lerp(1f, pico, salto * salto);
    }

    // El hito mas alto que se cruzo al pasar de 'anterior' a 'actual', o 0. Estatico
    // para probarlo sin escena.
    public static int HitoCruzado(int[] hitos, int anterior, int actual)
    {
        int cruzado = 0;
        if (hitos == null) return 0;
        foreach (int hito in hitos)
        {
            if (anterior < hito && actual >= hito && hito > cruzado) cruzado = hito;
        }
        return cruzado;
    }

    public static int SemitonosDelSalto(int salto)
    {
        return Escala[Mathf.Clamp(salto, 0, Escala.Length - 1)];
    }

    private void Sonar(int salto)
    {
        if (nota == null) return;
        Sonidos.Tocar(nota, volumenNota, Sonidos.PitchDe(SemitonosDelSalto(salto)), 0f, 0.05f);
    }

    // El hito: la palabra un rato, un arpegio de la bemol hacia arriba, un temblor y un
    // salto mas grande.
    private void Festejar(int hito)
    {
        palabraDelHito = PalabraDelHito(hito);
        hitoHasta = Time.unscaledTime + duracionHito;
        saltoDeHito = true;
        CamaraJugador.Temblar(temblorDelHito);
        if (nota == null) return;
        int[] arpegio = { 12, 16, 19, 24 };
        for (int i = 0; i < arpegio.Length; i++)
            Sonidos.Programar(nota, 0.07 * i, volumenNota * 1.3f, Sonidos.PitchDe(arpegio[i]));
    }

    private static string PalabraDelHito(int hito)
    {
        if (hito >= 100) return Textos.De("combo_100");
        if (hito >= 50) return Textos.De("combo_50");
        if (hito >= 25) return Textos.De("combo_25");
        return Textos.De("combo_10");
    }
}
