using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Un escenario de los capitulos: como se ve el mundo y que decorado tiene. El primero de
// la lista es "lo que trae la escena" (la pradera de dia): sus colores y su piso se leen
// al empezar y no se cargan a mano.
[System.Serializable]
public class EscenarioDeCapitulo
{
    [Tooltip("El id del nombre en la tabla: escenario_pradera, escenario_cementerio, escenario_ciudad.")]
    public string idTexto = "escenario_pradera";

    [Tooltip("El prefab que sale del piso al entrar. Vacio: el capitulo no tiene decorado.")]
    public GameObject decorado;

    [Tooltip("El piso. Vacio: el que trae la escena.")]
    public Material piso;

    public Color cielo = new Color(0.66f, 0.86f, 0.96f, 1f);
    public Color luz = Color.white;
    public float intensidadLuz = 1.25f;
    public Vector3 rotacionLuz = new Vector3(50f, -30f, 0f);
    public Color ambiente = new Color(0.55f, 0.55f, 0.55f, 1f);

    [Tooltip("De dia la niebla queda lejisimos; de noche se acerca y tapa el borde del mapa.")]
    public bool conNiebla;
    public float nieblaInicio = 16f;
    public float nieblaFin = 52f;
}

// Los capitulos de las oleadas, pedido de Ivan: cada 10 oleadas cambia el escenario. Van
// en el orden de `escenarios` y despues vuelven a empezar: 1-10 la pradera de dia, 11-20
// el cementerio de noche, 21-30 la ciudad de noche y 31-40 otra vez la pradera.
//
// Al pasar de capitulo, en el descanso de la oleada: el cartel "CAPITULO 3 / LA CIUDAD",
// la luz, el cielo, la luz ambiente y la niebla se funden de un escenario al otro, el
// piso cambia a mitad del fundido y el decorado nuevo sale del suelo mientras el viejo se
// va en lo oscuro. Una partida retomada en la 25 arranca directamente en la ciudad.
//
// Los decorados son prefabs hechos con formas simples (ConstructorEscenarios, en el
// editor), sin colliders: los zombis van derecho al jugador y se trabarian. Cada uno se
// arma una sola vez en la partida y despues se prende y se apaga, que son cientos de
// objetos. Cuando termina de salir se junta en pocos draw calls (StaticBatchingUtility) y
// desde ahi las piezas ya no se mueven por separado, asi que **la primera vez sale cada
// pieza sola del piso y las siguientes sale el decorado entero**. Va en WaveMode.
public class CapitulosDeEscenario : MonoBehaviour
{
    public WaveManager oleadas;
    public int oleadasPorCapitulo = 10;

    [Tooltip("En orden. El primero es lo que trae la escena: sus colores y su piso se leen al empezar.")]
    public EscenarioDeCapitulo[] escenarios;

    public float duracionFundido = 2.5f;

    [Header("Lo que se pinta")]
    public Renderer piso;
    public Light sol;
    public Camera camara;

    [Header("El cartel")]
    public Canvas canvas;
    public TMP_FontAsset fuente;
    public Material materialContorno;
    public Color colorCartel = new Color(1f, 0.85f, 0.3f);
    public float duracionCartel = 3.2f;

    private const float Hundido = 2.5f;      // cuanto se hunde el decorado para salir del piso

    // Lo puesto de cada escenario: se arma la primera vez que toca y se reusa.
    private class Puesta
    {
        public GameObject objeto;
        public bool combinado;
        public readonly List<Transform> piezas = new List<Transform>();
        public readonly List<float> alturas = new List<float>();
        public readonly List<float> demoras = new List<float>();
        public float saliendoDesde = -1f;
    }

    private Puesta[] puestas;
    private int capitulo = -1;
    private int indiceDesde, indiceHacia;
    private float mezcla = 1f;               // 0 = el de indiceDesde, 1 = el de indiceHacia
    private TMP_Text cartel;
    private float cartelDesde;
    private Color ambienteDeLaEscena = Color.gray;

    private void Start()
    {
        if (camara == null) camara = Camera.main;
        ambienteDeLaEscena = RenderSettings.ambientLight;
        if (escenarios == null || escenarios.Length == 0) return;

        // El primero es lo que ya hay en la escena: asi el capitulo 1 se ve igual que
        // siempre sin cargar los mismos colores dos veces.
        var dia = escenarios[0];
        if (camara != null) dia.cielo = camara.backgroundColor;
        if (sol != null)
        {
            dia.luz = sol.color;
            dia.intensidadLuz = sol.intensity;
            dia.rotacionLuz = sol.transform.rotation.eulerAngles;
        }
        dia.ambiente = ambienteDeLaEscena;
        if (piso != null) dia.piso = piso.sharedMaterial;
        dia.conNiebla = false;

        puestas = new Puesta[escenarios.Length];
        for (int i = 0; i < puestas.Length; i++) puestas[i] = new Puesta();
    }

    private void OnDestroy()
    {
        // La niebla y la luz ambiente son de la aplicacion: el menu no hereda la noche.
        RenderSettings.fog = false;
        RenderSettings.ambientLight = ambienteDeLaEscena;
        if (puestas == null) return;
        foreach (var puesta in puestas) if (puesta.objeto != null) Destroy(puesta.objeto);
    }

    public static int CapituloDe(int oleada, int oleadasPorCapitulo)
    {
        return oleada <= 0 ? 0 : (oleada - 1) / Mathf.Max(1, oleadasPorCapitulo);
    }

    // Que escenario le toca a ese capitulo: van en orden y vuelven a empezar.
    public int EscenarioDe(int capituloDeLaOleada)
    {
        if (escenarios == null || escenarios.Length == 0) return 0;
        int largo = escenarios.Length;
        return ((capituloDeLaOleada % largo) + largo) % largo;
    }

    private void Update()
    {
        if (oleadas == null || oleadas.OleadaActual <= 0 || puestas == null) return;

        int nuevo = CapituloDe(oleadas.OleadaActual, oleadasPorCapitulo);
        if (nuevo != capitulo)
        {
            bool primero = capitulo < 0;
            capitulo = nuevo;
            int destino = EscenarioDe(capitulo);

            if (primero)
            {
                // Retomada o recien empezada: sin fundido ni piezas saliendo.
                indiceDesde = indiceHacia = destino;
                mezcla = 1f;
                if (escenarios[destino].decorado != null) Poner(destino, false);
                Aplicar();
            }
            else if (destino != indiceHacia)
            {
                indiceDesde = indiceHacia;
                indiceHacia = destino;
                mezcla = 0f;
            }
            if (capitulo > 0) MostrarCartel(destino);
        }

        if (mezcla < 1f)
        {
            bool antes = mezcla >= 0.5f;
            mezcla = Mathf.MoveTowards(mezcla, 1f, Time.deltaTime / Mathf.Max(0.01f, duracionFundido));
            // El cambio de piso y de decorado pasa en lo oscuro, a mitad del fundido.
            if (!antes && mezcla >= 0.5f)
            {
                Sacar(indiceDesde);
                Poner(indiceHacia, true);
            }
            Aplicar();
        }

        AnimarDecorado();
        AnimarCartel();
    }

    private void Aplicar()
    {
        var a = escenarios[indiceDesde];
        var b = escenarios[indiceHacia];
        float m = mezcla;

        Color cielo = Color.Lerp(a.cielo, b.cielo, m);
        if (camara != null) camara.backgroundColor = cielo;
        if (sol != null)
        {
            sol.color = Color.Lerp(a.luz, b.luz, m);
            sol.intensity = Mathf.Lerp(a.intensidadLuz, b.intensidadLuz, m);
            sol.transform.rotation = Quaternion.Slerp(Quaternion.Euler(a.rotacionLuz), Quaternion.Euler(b.rotacionLuz), m);
        }
        RenderSettings.ambientLight = Color.Lerp(a.ambiente, b.ambiente, m);

        // El que no tiene niebla la manda lejisimos, asi pasar de uno con niebla a uno sin
        // ella se ve como que se abre, y no como un corte.
        RenderSettings.fog = a.conNiebla || b.conNiebla;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = cielo;
        RenderSettings.fogStartDistance = Mathf.Lerp(a.conNiebla ? a.nieblaInicio : 300f, b.conNiebla ? b.nieblaInicio : 300f, m);
        RenderSettings.fogEndDistance = Mathf.Lerp(a.conNiebla ? a.nieblaFin : 600f, b.conNiebla ? b.nieblaFin : 600f, m);

        if (piso != null)
        {
            var material = (m >= 0.5f ? b : a).piso;
            if (material != null) piso.sharedMaterial = material;
        }
    }

    // --- Los decorados ---------------------------------------------------------------

    private void Poner(int indice, bool saliendo)
    {
        var escenario = escenarios[indice];
        var puesta = puestas[indice];
        if (escenario.decorado == null) return;
        if (puesta.objeto == null) Armar(escenario, puesta);
        puesta.objeto.SetActive(true);

        if (!saliendo)
        {
            Aterrizar(puesta);
            return;
        }

        if (puesta.combinado)
        {
            // Ya esta pegado en una sola malla y las piezas no se mueven por separado:
            // sale el decorado entero de una.
            var p = puesta.objeto.transform.position;
            p.y = -Hundido;
            puesta.objeto.transform.position = p;
        }
        else
        {
            for (int i = 0; i < puesta.piezas.Count; i++)
            {
                var p = puesta.piezas[i].localPosition;
                p.y = puesta.alturas[i] - Hundido;
                puesta.piezas[i].localPosition = p;
            }
        }
        puesta.saliendoDesde = Time.time;
        CamaraJugador.Temblar(0.35f);
    }

    private static void Armar(EscenarioDeCapitulo escenario, Puesta puesta)
    {
        puesta.objeto = Instantiate(escenario.decorado);
        puesta.objeto.SetActive(false);
        foreach (Transform grupo in puesta.objeto.transform)
        {
            foreach (Transform pieza in grupo)
            {
                puesta.piezas.Add(pieza);
                puesta.alturas.Add(pieza.localPosition.y);
                puesta.demoras.Add(Random.Range(0f, 1.4f));
            }
        }
    }

    // Todo en su lugar y, la primera vez, pegado en una sola malla.
    private static void Aterrizar(Puesta puesta)
    {
        puesta.saliendoDesde = -1f;
        var raiz = puesta.objeto.transform.position;
        raiz.y = 0f;
        puesta.objeto.transform.position = raiz;
        if (puesta.combinado) return;

        for (int i = 0; i < puesta.piezas.Count; i++)
        {
            var p = puesta.piezas[i].localPosition;
            p.y = puesta.alturas[i];
            puesta.piezas[i].localPosition = p;
        }
        StaticBatchingUtility.Combine(puesta.objeto);
        puesta.combinado = true;
    }

    private void Sacar(int indice)
    {
        var puesta = puestas[indice];
        if (puesta.objeto != null) puesta.objeto.SetActive(false);
        puesta.saliendoDesde = -1f;
    }

    private void AnimarDecorado()
    {
        foreach (var puesta in puestas)
        {
            if (puesta.saliendoDesde < 0f || puesta.objeto == null) continue;
            float t = Time.time - puesta.saliendoDesde;

            if (puesta.combinado)
            {
                float f = Mathf.Clamp01(t / 1.2f);
                var p = puesta.objeto.transform.position;
                p.y = -Hundido * (1f - CurvasUI.SalidaAtras(f));
                puesta.objeto.transform.position = p;
                if (f >= 1f) Aterrizar(puesta);
                continue;
            }

            bool termino = true;
            for (int i = 0; i < puesta.piezas.Count; i++)
            {
                float f = Mathf.Clamp01((t - puesta.demoras[i]) / 0.5f);
                if (f < 1f) termino = false;
                var p = puesta.piezas[i].localPosition;
                p.y = puesta.alturas[i] - Hundido * (1f - CurvasUI.SalidaAtras(f));
                puesta.piezas[i].localPosition = p;
            }
            if (termino) Aterrizar(puesta);
        }
    }

    // --- El cartel -------------------------------------------------------------------

    private void MostrarCartel(int indiceEscenario)
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
        string nombre = Textos.De(escenarios[indiceEscenario].idTexto);
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
