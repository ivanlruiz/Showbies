using TMPro;
using UnityEngine;
using UnityEngine.UI;

// En la derrota, entre la oferta de video y los botones: lo que esta mas cerca de
// completarse, con una barra que se llena al entrar. Una mision del dia a medias
// ("PROXIMO: Mata 300 zombis 120/300") o la mejora mas barata que todavia no alcanza
// ("TE FALTAN 40 PARA CADENCIA NIVEL 9"), la que tenga mas avance. La derrota es cuando
// se cierra la app, y un objetivo casi lleno es el mejor "una mas". Las mejoras que ya
// alcanzan no van aca: las dice el aviso de compras.
//
// Se arma en codigo sobre el fondo de Perdiste, con el estilo del aviso de compras.
public class ProximoObjetivo : MonoBehaviour
{
    public RectTransform padre;              // el fondo de la pantalla
    public TMP_Text estilo;                  // el aviso de compras: la fuente, el material y el color
    public Vector2 posicion = new Vector2(0f, -155f);
    public float anchoBarra = 380f;
    public float demora = 0.9f;
    public float duracionLlenado = 0.8f;
    public Color colorBarra = new Color(0.3f, 0.75f, 0.2f, 1f);
    public Color colorFondoBarra = new Color(0f, 0f, 0f, 0.2f);

    private CanvasGroup grupo;
    private RectTransform relleno;
    private float fraccion;
    private float reloj;

    private void Start()
    {
        string texto;
        if (padre == null || !Elegir(out texto, out fraccion))
        {
            enabled = false;
            return;
        }
        Armar(texto);
    }

    // El objetivo con mas avance entre las misiones sin cumplir y las mejoras que no
    // alcanzan. Falso si no hay ninguno.
    public static bool Elegir(out string texto, out float fraccion)
    {
        texto = null;
        fraccion = -1f;

        foreach (var mision in MisionesDiarias.DeHoy)
        {
            if (mision.cobrada || MisionesDiarias.Cumplida(mision) || !(mision.objetivo > 0)) continue;
            double avance = MisionesDiarias.Avance(mision);
            float f = (float)(avance / mision.objetivo);
            if (f <= fraccion) continue;
            fraccion = f;
            texto = Textos.Formato("objetivo_mision", MisionesDiarias.Descripcion(mision),
                                   FormatoNumeros.Compacto(avance), FormatoNumeros.Compacto(mision.objetivo));
        }

        var catalogo = CatalogoMejoras.Instancia;
        long monedas = Progreso.MonedasEnteras;
        if (catalogo != null && catalogo.enTienda != null)
        {
            foreach (var mejora in catalogo.enTienda)
            {
                if (mejora == null) continue;
                int nivel = Progreso.Nivel(mejora.id);
                if (mejora.EnTope(nivel)) continue;
                double precio = mejora.Precio(nivel);
                if (!(precio > 0) || monedas >= precio) continue;
                float f = (float)(monedas / precio);
                if (f <= fraccion) continue;
                fraccion = f;
                texto = Textos.Formato("objetivo_mejora", FormatoNumeros.Compacto(precio - monedas),
                                       Textos.De("mejora_" + mejora.id + "_nombre"), nivel + 1);
            }
        }
        return texto != null;
    }

    private void Armar(string texto)
    {
        var raiz = new GameObject("ProximoObjetivo", typeof(RectTransform), typeof(CanvasGroup));
        var rt = (RectTransform)raiz.transform;
        rt.SetParent(padre, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = posicion;
        rt.sizeDelta = new Vector2(1400f, 70f);
        grupo = raiz.GetComponent<CanvasGroup>();
        grupo.alpha = 0f;
        grupo.blocksRaycasts = false;

        var textoGo = new GameObject("Texto", typeof(RectTransform), typeof(TextMeshProUGUI));
        var rtTexto = (RectTransform)textoGo.transform;
        rtTexto.SetParent(rt, false);
        rtTexto.anchoredPosition = Vector2.zero;
        rtTexto.sizeDelta = new Vector2(1400f, 40f);
        var tmp = textoGo.GetComponent<TextMeshProUGUI>();
        if (estilo != null)
        {
            tmp.font = estilo.font;
            tmp.fontSharedMaterial = estilo.fontSharedMaterial;
            tmp.color = estilo.color;
        }
        tmp.text = texto;
        tmp.fontSize = 32f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.raycastTarget = false;

        // La barra, sin sprite: el relleno crece con el ancla derecha (un Image Filled sin
        // sprite no llena, ver las trampas de CLAUDE.md).
        var barra = Imagen(rt, "Barra", colorFondoBarra);
        barra.anchoredPosition = new Vector2(0f, -30f);
        barra.sizeDelta = new Vector2(anchoBarra, 10f);
        relleno = Imagen(barra, "Relleno", colorBarra);
        relleno.anchorMin = Vector2.zero;
        relleno.anchorMax = new Vector2(0f, 1f);
        relleno.offsetMin = relleno.offsetMax = Vector2.zero;
    }

    private static RectTransform Imagen(RectTransform padre, string nombre, Color color)
    {
        var go = new GameObject(nombre, typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(padre, false);
        var img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return rt;
    }

    private void Update()
    {
        if (grupo == null) return;
        // Delta con tope: el primer frame de la escena es largo.
        reloj += Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);
        float t = reloj - demora;
        if (t < 0f) return;
        grupo.alpha = Mathf.Clamp01(t / 0.3f);
        float llenado = Mathf.Clamp01(t / duracionLlenado);
        relleno.anchorMax = new Vector2(fraccion * CurvasUI.SalidaCubica(llenado), 1f);
        if (llenado >= 1f && grupo.alpha >= 1f) enabled = false;
    }
}
