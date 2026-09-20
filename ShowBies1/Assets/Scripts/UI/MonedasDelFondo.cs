using UnityEngine;
using UnityEngine.UI;

// Monedas doradas que caen despacio en el fondo del menu, girando y brillando, por
// delante de los zombis de FondoMenu y detras de los botones. Se arman en codigo con
// TexturasUI; este componente es duenio de la textura y la destruye.
//
// Va en la raiz del canvas "Main Menu": las monedas se crean como primeros hijos
// despues del fondo, asi quedan debajo de todo lo demas.
public class MonedasDelFondo : MonoBehaviour
{
    public RectTransform fondo;             // BG: las monedas van justo encima
    public int cantidad = 11;
    public float tamanioMinimo = 30f;
    public float tamanioMaximo = 58f;
    public float velocidadMinima = 40f;     // unidades del canvas por segundo
    public float velocidadMaxima = 110f;
    public Color colorMoneda = new Color(1f, 0.76f, 0.12f, 1f);
    public Color colorBorde = new Color(0.72f, 0.42f, 0.02f, 1f);

    private class Moneda
    {
        public RectTransform rt;
        public Image imagen;
        public Image borde;
        public float velocidad;
        public float fase;
        public float giro;
        public float tamanio;
    }

    private Moneda[] monedas;
    private Texture2D textura;
    private Texture2D texturaBorde;
    private Sprite sprite;
    private Sprite spriteBorde;
    private RectTransform canvas;

    private void Start()
    {
        canvas = (RectTransform)transform;
        // Los sprites, como las texturas, son de esta clase: se destruyen los cuatro.
        textura = TexturasUI.Circulo(96);
        sprite = Sprite.Create(textura, new Rect(0, 0, 96, 96), new Vector2(0.5f, 0.5f));
        texturaBorde = TexturasUI.Anillo(96, 0.2f);
        spriteBorde = Sprite.Create(texturaBorde, new Rect(0, 0, 96, 96), new Vector2(0.5f, 0.5f));

        var capa = new GameObject("MonedasDelFondo", typeof(RectTransform));
        var rtCapa = (RectTransform)capa.transform;
        rtCapa.SetParent(canvas, false);
        rtCapa.anchorMin = Vector2.zero;
        rtCapa.anchorMax = Vector2.one;
        rtCapa.offsetMin = Vector2.zero;
        rtCapa.offsetMax = Vector2.zero;
        int indice = fondo != null ? fondo.GetSiblingIndex() + 1 : 0;
        rtCapa.SetSiblingIndex(indice);

        monedas = new Moneda[cantidad];
        for (int i = 0; i < cantidad; i++)
        {
            var go = new GameObject("Moneda", typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(rtCapa, false);
            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;
            var go2 = new GameObject("Borde", typeof(RectTransform));
            var rtBorde = (RectTransform)go2.transform;
            rtBorde.SetParent(rt, false);
            rtBorde.anchorMin = Vector2.zero; rtBorde.anchorMax = Vector2.one;
            rtBorde.offsetMin = Vector2.zero; rtBorde.offsetMax = Vector2.zero;
            var borde = go2.AddComponent<Image>();
            borde.sprite = spriteBorde;
            borde.raycastTarget = false;
            var m = new Moneda { rt = rt, imagen = img, borde = borde };
            Reiniciar(m, Random.value);
            monedas[i] = m;
        }
    }

    private void OnDestroy()
    {
        if (sprite != null) Destroy(sprite);
        if (spriteBorde != null) Destroy(spriteBorde);
        if (textura != null) Destroy(textura);
        if (texturaBorde != null) Destroy(texturaBorde);
    }

    // alturaInicial: 0 = arriba de todo, 1 = abajo; al volver a salir, arriba.
    private void Reiniciar(Moneda m, float alturaInicial)
    {
        Vector2 tam = canvas.rect.size;
        m.tamanio = Random.Range(tamanioMinimo, tamanioMaximo);
        m.velocidad = Random.Range(velocidadMinima, velocidadMaxima) * (m.tamanio / tamanioMaximo);
        m.fase = Random.Range(0f, Mathf.PI * 2f);
        m.giro = Random.Range(1.5f, 3.5f);
        m.rt.sizeDelta = new Vector2(m.tamanio, m.tamanio);
        m.rt.anchorMin = m.rt.anchorMax = new Vector2(0.5f, 0.5f);
        m.rt.anchoredPosition = new Vector2(Random.Range(-tam.x * 0.5f, tam.x * 0.5f),
                                            tam.y * 0.5f + m.tamanio - alturaInicial * (tam.y + m.tamanio * 2f));
    }

    private void Update()
    {
        if (monedas == null) return;
        float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
        float t = Time.unscaledTime;
        float piso = -canvas.rect.height * 0.5f;

        foreach (var m in monedas)
        {
            var p = m.rt.anchoredPosition;
            p.y -= m.velocidad * dt;
            p.x += Mathf.Sin(t * 0.7f + m.fase) * 12f * dt;
            m.rt.anchoredPosition = p;
            if (p.y < piso - m.tamanio) Reiniciar(m, 0f);

            // Gira de canto (el ancho va y viene) y brilla cuando da de frente.
            float frente = Mathf.Cos(t * m.giro + m.fase);
            m.rt.localScale = new Vector3(Mathf.Max(0.12f, Mathf.Abs(frente)), 1f, 1f);
            float brillo = Mathf.Pow(Mathf.Abs(frente), 6f);
            var c = Color.Lerp(colorMoneda, new Color(1f, 0.97f, 0.7f), brillo * 0.45f);
            c.a = 0.95f;
            m.imagen.color = c;
            var b = colorBorde; b.a = 0.95f;
            m.borde.color = b;
        }
    }
}
