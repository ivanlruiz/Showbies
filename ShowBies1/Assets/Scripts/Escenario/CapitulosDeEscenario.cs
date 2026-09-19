using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Los capitulos de las oleadas, pedido de Ivan: cada 10 oleadas cambia el escenario. Las
// oleadas 1-10 son la pradera de dia, 11-20 el cementerio de noche, y despues se alternan.
// Al pasar de capitulo, en el descanso de la oleada: el cartel "CAPITULO 2: EL CEMENTERIO",
// la luz, el cielo y la niebla se funden a la noche, el piso pasa a tierra y las lapidas
// salen del suelo de a una. Al volver al dia el decorado se va en lo oscuro del fundido.
// Una partida retomada en la oleada 15 arranca directamente de noche.
//
// El decorado es un prefab hecho con formas simples (ConstructorEscenarios, en el editor),
// sin colliders. Cuando termina de salir se junta en pocos draw calls
// (StaticBatchingUtility), asi en el telefono cuesta casi nada. Lo del dia (la luz, el
// color del cielo, el piso) se lee de la escena al empezar. Va en WaveMode.
public class CapitulosDeEscenario : MonoBehaviour
{
    public WaveManager oleadas;
    public int oleadasPorCapitulo = 10;

    [Header("La noche")]
    public GameObject decoradoNoche;
    public Renderer piso;
    public Material pisoNoche;
    public Light sol;
    public Camera camara;
    public Color cieloNoche = new Color(0.07f, 0.09f, 0.17f);
    public Color luzNoche = new Color(0.55f, 0.66f, 1f);
    public float intensidadNoche = 0.45f;
    public Vector3 rotacionNoche = new Vector3(55f, 200f, 0f);
    public Color ambienteNoche = new Color(0.2f, 0.23f, 0.34f);
    public float nieblaInicio = 16f;
    public float nieblaFin = 52f;
    public float duracionFundido = 2.5f;

    [Header("El cartel")]
    public Canvas canvas;
    public TMP_FontAsset fuente;
    public Material materialContorno;
    public Color colorCartel = new Color(1f, 0.85f, 0.3f);
    public float duracionCartel = 3.2f;

    // Lo del dia, leido de la escena.
    private Color cieloDia, luzDia, ambienteDia;
    private float intensidadDia;
    private Quaternion rotacionDia;
    private Material pisoDia;

    private int capitulo = -1;
    private float mezcla;              // 0 dia, 1 noche
    private float objetivo;
    private GameObject decorado;
    private readonly List<Transform> piezas = new List<Transform>();
    private readonly List<float> alturas = new List<float>();
    private readonly List<float> demoras = new List<float>();
    private float saliendoDesde = -1f;
    private TMP_Text cartel;
    private float cartelDesde;

    private void Start()
    {
        if (camara == null) camara = Camera.main;
        if (camara != null) cieloDia = camara.backgroundColor;
        if (sol != null)
        {
            luzDia = sol.color;
            intensidadDia = sol.intensity;
            rotacionDia = sol.transform.rotation;
        }
        ambienteDia = RenderSettings.ambientLight;
        if (piso != null) pisoDia = piso.sharedMaterial;
    }

    private void OnDestroy()
    {
        // La niebla y la luz ambiente son de la aplicacion: el menu no hereda la noche.
        RenderSettings.fog = false;
        RenderSettings.ambientLight = ambienteDia;
    }

    public static int CapituloDe(int oleada, int oleadasPorCapitulo)
    {
        return oleada <= 0 ? 0 : (oleada - 1) / Mathf.Max(1, oleadasPorCapitulo);
    }

    public static bool EsNoche(int capitulo)
    {
        return capitulo % 2 == 1;
    }

    private void Update()
    {
        if (oleadas == null || oleadas.OleadaActual <= 0) return;

        int nuevo = CapituloDe(oleadas.OleadaActual, oleadasPorCapitulo);
        if (nuevo != capitulo)
        {
            bool primero = capitulo < 0;
            capitulo = nuevo;
            objetivo = EsNoche(capitulo) ? 1f : 0f;
            if (primero)
            {
                // Retomada o recien empezada: sin fundido ni lapidas saliendo.
                mezcla = objetivo;
                if (objetivo > 0f) PonerDecorado(false);
                Aplicar();
            }
            if (capitulo > 0) MostrarCartel();
        }

        if (!Mathf.Approximately(mezcla, objetivo))
        {
            bool antes = mezcla >= 0.5f;
            mezcla = Mathf.MoveTowards(mezcla, objetivo, Time.deltaTime / Mathf.Max(0.01f, duracionFundido));
            bool despues = mezcla >= 0.5f;
            // El cambio de piso y de decorado pasa en lo oscuro, a mitad del fundido.
            if (!antes && despues) PonerDecorado(true);
            if (antes && !despues) SacarDecorado();
            Aplicar();
        }

        AnimarDecorado();
        AnimarCartel();
    }

    private void Aplicar()
    {
        float m = mezcla;
        if (camara != null) camara.backgroundColor = Color.Lerp(cieloDia, cieloNoche, m);
        if (sol != null)
        {
            sol.color = Color.Lerp(luzDia, luzNoche, m);
            sol.intensity = Mathf.Lerp(intensidadDia, intensidadNoche, m);
            sol.transform.rotation = Quaternion.Slerp(rotacionDia, Quaternion.Euler(rotacionNoche), m);
        }
        RenderSettings.ambientLight = Color.Lerp(ambienteDia, ambienteNoche, m);
        RenderSettings.fog = m > 0.001f;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = cieloNoche;
        // De dia la niebla queda lejisimos; se acerca con la noche.
        RenderSettings.fogStartDistance = Mathf.Lerp(300f, nieblaInicio, m);
        RenderSettings.fogEndDistance = Mathf.Lerp(600f, nieblaFin, m);
        if (piso != null) piso.sharedMaterial = m >= 0.5f && pisoNoche != null ? pisoNoche : pisoDia;
    }

    private void PonerDecorado(bool saliendo)
    {
        SacarDecorado();
        if (decoradoNoche == null) return;
        decorado = Instantiate(decoradoNoche);
        piezas.Clear();
        alturas.Clear();
        demoras.Clear();
        if (!saliendo)
        {
            StaticBatchingUtility.Combine(decorado);
            return;
        }

        // Cada pieza (una lapida, un arbol, un poste) sale del piso con su demora.
        foreach (Transform grupo in decorado.transform)
        {
            foreach (Transform pieza in grupo)
            {
                piezas.Add(pieza);
                alturas.Add(pieza.localPosition.y);
                demoras.Add(Random.Range(0f, 1.4f));
                var p = pieza.localPosition;
                p.y -= 2.5f;
                pieza.localPosition = p;
            }
        }
        saliendoDesde = Time.time;
        CamaraJugador.Temblar(0.35f);
    }

    private void SacarDecorado()
    {
        if (decorado != null) Destroy(decorado);
        decorado = null;
        piezas.Clear();
        saliendoDesde = -1f;
    }

    private void AnimarDecorado()
    {
        if (saliendoDesde < 0f || decorado == null) return;
        float t = Time.time - saliendoDesde;
        bool termino = true;
        for (int i = 0; i < piezas.Count; i++)
        {
            float f = Mathf.Clamp01((t - demoras[i]) / 0.5f);
            if (f < 1f) termino = false;
            var p = piezas[i].localPosition;
            p.y = alturas[i] - 2.5f * (1f - CurvasUI.SalidaAtras(f));
            piezas[i].localPosition = p;
        }
        if (!termino) return;
        saliendoDesde = -1f;
        StaticBatchingUtility.Combine(decorado);
    }

    private void MostrarCartel()
    {
        if (canvas == null) return;
        if (cartel != null) Destroy(cartel.gameObject);
        var go = new GameObject("CartelCapitulo", typeof(RectTransform), typeof(TextMeshProUGUI));
        var rt = (RectTransform)go.transform;
        rt.SetParent(canvas.transform, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        // Arriba de todo: al medio sale el cartel de la oleada, al mismo tiempo.
        rt.anchoredPosition = new Vector2(0f, 385f);
        rt.sizeDelta = new Vector2(1400f, 200f);
        cartel = go.GetComponent<TextMeshProUGUI>();
        if (fuente != null) cartel.font = fuente;
        if (materialContorno != null) cartel.fontSharedMaterial = materialContorno;
        string nombre = EsNoche(capitulo) ? Textos.De("escenario_cementerio") : Textos.De("escenario_pradera");
        cartel.text = Textos.Formato("capitulo_titulo", capitulo + 1) + "\n<size=60%>" + nombre + "</size>";
        cartel.fontSize = 84f;
        cartel.color = colorCartel;
        cartel.alignment = TextAlignmentOptions.Center;
        cartel.textWrappingMode = TextWrappingModes.NoWrap;
        cartel.raycastTarget = false;
        cartelDesde = Time.unscaledTime;
        Efectos.CartelOleada();
    }

    private void AnimarCartel()
    {
        if (cartel == null) return;
        float edad = Time.unscaledTime - cartelDesde;
        float entrada = Mathf.Clamp01(edad / 0.4f);
        float salida = Mathf.Clamp01((edad - (duracionCartel - 0.35f)) / 0.35f);
        cartel.rectTransform.localScale = Vector3.one * CurvasUI.SalidaAtras(entrada) * (1f - salida);
        if (edad >= duracionCartel)
        {
            Destroy(cartel.gameObject);
            cartel = null;
        }
    }
}
