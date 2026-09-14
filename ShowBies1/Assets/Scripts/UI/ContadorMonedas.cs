using TMPro;
using UnityEngine;

// Monedas acumuladas: en el HUD de las escenas de juego, en el menu y en la
// tienda. Reescribe el texto solo cuando cambia la parte entera: armarlo por
// frame aloca por frame. Cuando sube, el texto da un salto y se prende, para que
// cada moneda agarrada se note. Cuando baja (una compra en la tienda) se pone
// rojo y se achica un instante, y con duracionRodado > 0 rueda hasta el valor
// nuevo en vez de saltar, para que se vea cuanto se gasto.
//
// Todo con tiempo sin escalar: si se pausa justo al agarrar una, que no quede
// grande, y la tienda anda con cualquier timeScale.
public class ContadorMonedas : MonoBehaviour
{
    public TMP_Text texto;
    public string prefijo = "Monedas: ";
    public float escalaDelSalto = 1.4f;
    public float duracionDelSalto = 0.2f;
    public Color colorDelSalto = Color.white;

    [Tooltip("Segundos que tarda en rodar hacia abajo al gastar. Con 0 escribe el valor nuevo de una.")]
    public float duracionRodado = 0f;
    public Color colorAlBajar = new Color(1f, 0.45f, 0.4f);

    private const float EscalaAlBajar = 0.9f;
    private const float DuracionBajada = 0.3f;

    // Delta con tope: el primer frame de una escena dura mucho y se comeria la animacion.
    private const float DeltaMaximo = 1f / 30f;

    // Para anclar efectos al contador (el "-95" que sube en la tienda).
    public RectTransform Rect { get { return texto.rectTransform; } }

    private bool iniciado;
    private Color colorBase;
    private Vector3 escalaBase;
    private Vector2 posicionBase;

    private long leidas = -1;       // lo ultimo que se leyo de Progreso
    private long mostradas = -1;    // lo que dice el texto

    private float salto;            // 1 al subir, baja a 0
    private float bajada;           // 1 al bajar, baja a 0

    private bool rodando;
    private double rodadoDesde;
    private long rodadoHasta;
    private double valorRodado;
    private float progresoRodado;

    private bool sacudiendo;
    private float amplitudSacudida;
    private float duracionSacudida;
    private float progresoSacudida;

    private bool fueraDeLaBase;     // escala, color o posicion quedaron animados

    private void Awake()
    {
        Iniciar();
    }

    // Tambien lo llama Sacudir: la tienda puede pedirlo antes de que el contador,
    // que arranca apagado dentro de su panel, haya pasado por Awake.
    private void Iniciar()
    {
        if (iniciado) return;
        iniciado = true;
        colorBase = texto.color;
        escalaBase = texto.rectTransform.localScale;
        posicionBase = texto.rectTransform.anchoredPosition;
    }

    private void Update()
    {
        float dt = Mathf.Min(Time.unscaledDeltaTime, DeltaMaximo);
        long actuales = Progreso.MonedasEnteras;

        if (leidas < 0)
        {
            // El primer texto de la escena no se anima: no se agarro ni se gasto nada.
            leidas = actuales;
            Escribir(actuales);
        }
        else if (actuales > leidas)
        {
            leidas = actuales;
            rodando = false;
            salto = 1f;
            Escribir(actuales);
        }
        else if (actuales < leidas)
        {
            leidas = actuales;
            bajada = 1f;
            if (duracionRodado <= 0f)
            {
                rodando = false;
                Escribir(actuales);
            }
            else
            {
                // Si ya rodaba (dos compras seguidas), sigue desde donde iba y no
                // desde el valor anterior: el numero nunca vuelve a subir.
                rodadoDesde = rodando ? valorRodado : mostradas;
                rodadoHasta = actuales;
                valorRodado = rodadoDesde;
                progresoRodado = 0f;
                rodando = true;
            }
        }

        if (rodando) Rodar(dt);
        Animar(dt);
    }

    private void Rodar(float dt)
    {
        progresoRodado += dt / Mathf.Max(0.01f, duracionRodado);
        if (progresoRodado >= 1f)
        {
            rodando = false;
            Escribir(rodadoHasta);
            return;
        }

        valorRodado = rodadoDesde + (rodadoHasta - rodadoDesde) * CurvasUI.SalidaCubica(progresoRodado);
        Escribir((long)System.Math.Round(valorRodado));
    }

    private void Escribir(long valor)
    {
        if (valor == mostradas) return;
        mostradas = valor;
        texto.text = prefijo + FormatoNumeros.Compacto(valor);
    }

    public void Sacudir(float amplitud = 10f, float duracion = 0.3f)
    {
        Iniciar();
        amplitudSacudida = amplitud;
        duracionSacudida = Mathf.Max(0.01f, duracion);
        progresoSacudida = 0f;
        sacudiendo = true;
    }

    private void Animar(float dt)
    {
        if (salto <= 0f && bajada <= 0f && !sacudiendo)
        {
            if (fueraDeLaBase) VolverALaBase();
            return;
        }
        fueraDeLaBase = true;

        if (salto > 0f) salto = Mathf.Max(0f, salto - dt / Mathf.Max(0.01f, duracionDelSalto));
        if (bajada > 0f) bajada = Mathf.Max(0f, bajada - dt / DuracionBajada);

        float curvaSalto = salto * salto;
        float curvaBajada = bajada * bajada;
        RectTransform rect = texto.rectTransform;
        rect.localScale = escalaBase * Mathf.Lerp(1f, escalaDelSalto, curvaSalto) * Mathf.Lerp(1f, EscalaAlBajar, curvaBajada);
        texto.color = Color.Lerp(Color.Lerp(colorBase, colorDelSalto, curvaSalto), colorAlBajar, curvaBajada);

        float desplazamiento = 0f;
        if (sacudiendo)
        {
            progresoSacudida += dt / duracionSacudida;
            if (progresoSacudida >= 1f) sacudiendo = false;
            else desplazamiento = amplitudSacudida * CurvasUI.Oscilacion(progresoSacudida, 3f);
        }
        rect.anchoredPosition = posicionBase + new Vector2(desplazamiento, 0f);
    }

    private void VolverALaBase()
    {
        fueraDeLaBase = false;
        RectTransform rect = texto.rectTransform;
        rect.localScale = escalaBase;
        rect.anchoredPosition = posicionBase;
        texto.color = colorBase;
    }

    // La tienda apaga su panel con el contador adentro: si quedo a mitad de una
    // animacion, al volver a prenderse tiene que estar entero y con el valor final.
    private void OnDisable()
    {
        if (!iniciado) return;

        salto = 0f;
        bajada = 0f;
        sacudiendo = false;
        if (rodando)
        {
            rodando = false;
            Escribir(rodadoHasta);
        }
        VolverALaBase();
    }
}
