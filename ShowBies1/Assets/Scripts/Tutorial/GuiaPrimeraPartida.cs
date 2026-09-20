using TMPro;
using UnityEngine;
using UnityEngine.UI;

// La primera partida de alguien que recien instala. En el telefono, un pulgar fantasma
// sobre cada joystick (el de mover da vueltas, el de disparar empuja hacia afuera) con
// su cartel, hasta que se usa cada uno; en PC, un cartel con WASD y el clic. Cuando cae
// el primer zombi, "COGE LAS MONEDAS": las monedas son el corazon del juego y nadie lo
// explica en otro lado. No frena nada ni pide tocar "siguiente".
//
// Va en WaveMode (objeto GuiaPrimeraPartida), que es adonde PLAY manda la primera vez.
// Solo aparece si el jugador no termino ninguna partida (PrimeraVez).
public class GuiaPrimeraPartida : MonoBehaviour
{
    public PlayerJS joysticks;
    [Tooltip("El canvas del HUD, para el cartel de PC y el de las monedas.")]
    public Canvas canvas;
    public TMP_FontAsset fuente;
    public Material materialContorno;
    public float segundosCartelMonedas = 5f;

    private Texture2D texturaPulgar;
    private Sprite spritePulgar;
    private Guia mover, disparar;
    private TMP_Text cartelPC, cartelMonedas;
    private bool movio, disparo;
    private long matadosAlEmpezar;
    private float monedasDesde = -1f;
    private bool monedasMostradas;

    // Un joystick con su pulgar fantasma y su cartel.
    private class Guia
    {
        public RectTransform pulgar;
        public TMP_Text cartel;
        public CanvasGroup grupo;
        public float radio;
    }

    private void Start()
    {
        if (!PrimeraVez.NoTerminoPartidas)
        {
            enabled = false;
            return;
        }

        matadosAlEmpezar = Progreso.MatadosEnTotal;
        texturaPulgar = TexturasUI.Circulo(128);
        if (canvas == null && joysticks != null && joysticks.moveJoystick != null)
            canvas = joysticks.moveJoystick.GetComponentInParent<Canvas>(true).rootCanvas;

        if (Plataforma.EsMovil && joysticks != null)
        {
            mover = CrearGuia(joysticks.moveJoystick, Textos.De("guia_mover"), false);
            disparar = CrearGuia(joysticks.lookJoystick, Textos.De("guia_disparar"), true);
        }
        else if (canvas != null)
        {
            cartelPC = CrearTexto((RectTransform)canvas.transform, Textos.De("guia_pc"), 46f,
                                  new Vector2(0.5f, 0f), new Vector2(0f, 230f), new Vector2(1100f, 140f));
        }
    }

    private void OnDestroy()
    {
        // El sprite tambien es un objeto de Unity: con destruir la textura no alcanza.
        if (spritePulgar != null) Destroy(spritePulgar);
        if (texturaPulgar != null) Destroy(texturaPulgar);
    }

    private Guia CrearGuia(FixedJoystick joystick, string texto, bool empuja)
    {
        if (joystick == null) return null;
        var raiz = (RectTransform)joystick.transform;

        var go = new GameObject("GuiaPrimeraPartida", typeof(RectTransform), typeof(CanvasGroup));
        var rt = (RectTransform)go.transform;
        rt.SetParent(raiz, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = raiz.rect.size;
        var grupo = go.GetComponent<CanvasGroup>();
        grupo.blocksRaycasts = false;
        grupo.interactable = false;

        float lado = Mathf.Min(raiz.rect.width, raiz.rect.height);
        var pulgarGo = new GameObject("Pulgar", typeof(RectTransform), typeof(Image));
        var pulgar = (RectTransform)pulgarGo.transform;
        pulgar.SetParent(rt, false);
        pulgar.sizeDelta = Vector2.one * lado * 0.42f;
        var imagen = pulgarGo.GetComponent<Image>();
        // Uno solo para los dos pulgares, y se destruye con la guia.
        if (spritePulgar == null) spritePulgar = Sprite.Create(texturaPulgar, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f));
        imagen.sprite = spritePulgar;
        imagen.color = new Color(1f, 1f, 1f, 0.6f);
        imagen.raycastTarget = false;

        var cartel = CrearTexto(rt, texto, 44f, new Vector2(0.5f, 0.5f), new Vector2(0f, lado * 0.5f + 55f), new Vector2(560f, 110f));

        return new Guia { pulgar = pulgar, cartel = cartel, grupo = grupo, radio = lado * (empuja ? 0.32f : 0.22f) };
    }

    private TMP_Text CrearTexto(RectTransform padre, string texto, float tamanio, Vector2 ancla, Vector2 posicion, Vector2 caja)
    {
        var go = new GameObject("Cartel", typeof(RectTransform), typeof(TextMeshProUGUI));
        var rt = (RectTransform)go.transform;
        rt.SetParent(padre, false);
        rt.anchorMin = rt.anchorMax = ancla;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = posicion;
        rt.sizeDelta = caja;
        var tmp = go.GetComponent<TextMeshProUGUI>();
        if (fuente != null) tmp.font = fuente;
        if (materialContorno != null) tmp.fontSharedMaterial = materialContorno;
        tmp.text = texto;
        tmp.fontSize = tamanio;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.raycastTarget = false;
        return tmp;
    }

    private void Update()
    {
        if (MenuPausa.JuegoCongelado) return;
        float t = Time.unscaledTime;

        // Lo que hizo el jugador.
        if (Plataforma.EsMovil && joysticks != null)
        {
            if (joysticks.moveJoystick != null && joysticks.moveJoystick.Direction.sqrMagnitude > 0.04f) movio = true;
            if (joysticks.lookJoystick != null && joysticks.lookJoystick.Direction.sqrMagnitude > 0.04f) disparo = true;
        }
        else
        {
            if (Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.1f || Mathf.Abs(Input.GetAxisRaw("Vertical")) > 0.1f) movio = true;
            if (Input.GetMouseButton(0)) disparo = true;
        }

        // Los pulgares: el de mover da vueltas, el de disparar sale hacia arriba a la
        // derecha y vuelve. Cada guia se va cuando se usa su joystick.
        if (mover != null)
        {
            float a = t * 2.4f;
            Animar(mover, movio, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * mover.radio, t);
        }
        if (disparar != null)
        {
            float ida = Mathf.PingPong(t * 1.3f, 1f);
            Animar(disparar, disparo, new Vector2(0.7f, 0.7f) * disparar.radio * CurvasUI.SalidaAtras(ida), t);
        }
        if (cartelPC != null)
        {
            cartelPC.rectTransform.localScale = Vector3.one * (1f + 0.04f * Mathf.Sin(t * 5f));
            if (movio && disparo) Apagar(cartelPC.gameObject, ref cartelPC);
        }

        // Las monedas: con el primer zombi muerto, un cartel que rebota, hasta que se
        // agarra la primera o pasan unos segundos.
        if (!monedasMostradas && Progreso.MatadosEnTotal > matadosAlEmpezar && canvas != null)
        {
            monedasMostradas = true;
            monedasDesde = t;
            cartelMonedas = CrearTexto((RectTransform)canvas.transform, Textos.De("guia_monedas"), 64f,
                                       new Vector2(0.5f, 0.5f), new Vector2(0f, 190f), new Vector2(1000f, 120f));
            cartelMonedas.color = new Color(1f, 0.85f, 0.3f);
        }
        if (cartelMonedas != null)
        {
            float edad = t - monedasDesde;
            float entrada = Mathf.Clamp01(edad / 0.35f);
            cartelMonedas.rectTransform.localScale = Vector3.one * CurvasUI.SalidaAtras(entrada) * (1f + 0.05f * Mathf.Sin(t * 6f));
            if (edad > segundosCartelMonedas || Progreso.MonedasDeLaPartida > 0) Apagar(cartelMonedas.gameObject, ref cartelMonedas);
        }

        if (mover == null && disparar == null && cartelPC == null && monedasMostradas && cartelMonedas == null) enabled = false;
    }

    private void Animar(Guia guia, bool usado, Vector2 posicion, float t)
    {
        guia.pulgar.anchoredPosition = posicion;
        guia.cartel.rectTransform.localScale = Vector3.one * (1f + 0.05f * Mathf.Sin(t * 5f));
        if (!usado) return;

        guia.grupo.alpha -= Time.unscaledDeltaTime * 3f;
        if (guia.grupo.alpha > 0f) return;
        Destroy(guia.grupo.gameObject);
        if (guia == mover) mover = null;
        else disparar = null;
    }

    private static void Apagar(GameObject go, ref TMP_Text referencia)
    {
        Destroy(go);
        referencia = null;
    }
}
