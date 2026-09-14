using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Efectos de pantalla para la UI: monedas que vuelan de un lugar a otro, estallidos
// de chispas y textos que saltan ("-95", "¡x3!", "¡MÁXIMO!"). Es a la tienda lo que
// Efectos es a la partida.
//
// Va en un GameObject con Canvas anidado propio: todo lo que se mueve aca cambia
// cada frame, y en un Canvas aparte eso rearma sólo su malla y no la del panel con
// las tarjetas.
//
// Todo sale de pools que se crean una vez, sin corrutinas ni alocaciones por efecto:
// con compras rapidas se disparan varios por segundo. Si un pool se llena, las
// monedas y chispas de mas no salen (nadie las extraña en el medio de un estallido)
// y los textos reciclan el mas viejo (el ultimo texto si importa).
//
// Con tiempo sin escalar y delta topeado en 1/30, como el resto del jugo de UI.
public class EfectosUI : MonoBehaviour
{
    public Sprite spriteParticula;
    public Sprite spriteMoneda;
    public Color colorMoneda = new Color(1f, 0.8f, 0.2f);
    public TMP_FontAsset fuente;
    public int particulasEnPool = 64;
    public int monedasEnPool = 16;
    public int textosEnPool = 6;

    private const float TamanoParticula = 22f;
    private const float TamanoMoneda = 44f;
    private static readonly Vector2 TamanoTexto = new Vector2(600f, 140f);
    private static readonly Color Dorado = new Color(1f, 0.85f, 0.3f);

    private class Particula
    {
        public RectTransform rect;
        public Image imagen;
        public bool activa;
        public Vector2 posicion;
        public Vector2 velocidad;
        public float transcurrido;
        public float duracion;
    }

    private class MonedaVolando
    {
        public RectTransform rect;
        public Image imagen;
        public bool activa;
        public bool visible;
        public Vector2 desde;
        public Vector2 control;
        public Vector2 hasta;
        public RectTransform destino;
        public float demora;
        public float transcurrido;
        public float duracion;
        public int indice;
        public System.Action<int> alLlegar;
    }

    private class TextoVolando
    {
        public RectTransform rect;
        public TextMeshProUGUI texto;
        public bool activo;
        public Vector2 origen;
        public Color color;
        public bool cae;
        public float transcurrido;
        public int orden;
    }

    private Particula[] particulas;
    private MonedaVolando[] monedas;
    private TextoVolando[] textos;
    private int ordenTextos;

    private void Awake()
    {
        CrearPoolsSiFaltan();
    }

    // Las monedas salen escalonadas desde 'desde' y llegan a 'hasta' en arco. Por
    // cada una que llega se llama a alLlegar con su indice (0, 1, 2...), que es lo que
    // usa la tienda para hacer sonar una nota distinta por moneda. Si el pool no
    // alcanza salen menos, y alLlegar se llama sólo por las que salieron.
    public void MonedasVolando(RectTransform desde, RectTransform hasta, int cantidad, System.Action<int> alLlegar)
    {
        if (desde == null || hasta == null || cantidad <= 0) return;
        CrearPoolsSiFaltan();

        Vector2 inicio = PosicionLocal(desde);
        Vector2 fin = PosicionLocal(hasta);
        int lanzadas = 0;

        for (int i = 0; i < monedas.Length && lanzadas < cantidad; i++)
        {
            MonedaVolando m = monedas[i];
            if (m.activa) continue;

            // El control arriba del punto medio hace el arco; el corrimiento al azar
            // hace que no vuelen todas por el mismo carril.
            m.activa = true;
            m.visible = false;
            m.desde = inicio;
            m.hasta = fin;
            m.destino = hasta;
            m.control = (inicio + fin) * 0.5f + new Vector2(Random.Range(-120f, 120f), 250f);
            m.demora = lanzadas * 0.025f;
            m.transcurrido = 0f;
            m.duracion = 0.45f + Random.Range(-0.08f, 0.08f);
            m.indice = lanzadas;
            m.alLlegar = alLlegar;
            lanzadas++;
        }
    }

    // Chispas que salen en circulo desde el centro de 'en', frenan, caen y se
    // achican. El color se sortea entre el pedido, blanco y dorado.
    public void Estallido(RectTransform en, Color color, int cantidad)
    {
        if (en == null || cantidad <= 0) return;
        CrearPoolsSiFaltan();

        Vector2 centro = PosicionLocal(en);
        int lanzadas = 0;

        for (int i = 0; i < particulas.Length && lanzadas < cantidad; i++)
        {
            Particula p = particulas[i];
            if (p.activa) continue;

            float angulo = (lanzadas + Random.Range(-0.4f, 0.4f)) / cantidad * 2f * Mathf.PI;
            float rapidez = Random.Range(600f, 1100f);

            p.activa = true;
            p.posicion = centro;
            p.velocidad = new Vector2(Mathf.Cos(angulo), Mathf.Sin(angulo)) * rapidez;
            p.transcurrido = 0f;
            p.duracion = Random.Range(0.5f, 0.7f);

            int sorteo = Random.Range(0, 3);
            p.imagen.color = sorteo == 0 ? color : (sorteo == 1 ? Color.white : Dorado);
            p.rect.localPosition = centro;
            p.rect.localScale = Vector3.one;
            p.rect.gameObject.SetActive(true);
            lanzadas++;
        }
    }

    // Un texto que salta sobre 'en': aparece con un rebote de escala, sube (o baja
    // si 'cae') y se desvanece al final.
    public void TextoFlotante(RectTransform en, string texto, Color color, float tamano = 72f, bool cae = false)
    {
        if (en == null) return;
        CrearPoolsSiFaltan();
        if (textos.Length == 0) return;

        // Sin libres se recicla el mas viejo: el texto nuevo es el que importa.
        TextoVolando elegido = null;
        TextoVolando masViejo = textos[0];
        for (int i = 0; i < textos.Length; i++)
        {
            if (!textos[i].activo)
            {
                elegido = textos[i];
                break;
            }
            if (textos[i].orden < masViejo.orden) masViejo = textos[i];
        }
        if (elegido == null) elegido = masViejo;

        elegido.activo = true;
        elegido.origen = PosicionLocal(en);
        elegido.color = color;
        elegido.cae = cae;
        elegido.transcurrido = 0f;
        elegido.orden = ++ordenTextos;

        elegido.texto.text = texto;
        elegido.texto.fontSize = tamano;
        elegido.texto.color = color;
        elegido.rect.localPosition = elegido.origen;
        elegido.rect.localScale = Vector3.zero;
        // Al frente de los otros textos, por si se pisan.
        elegido.rect.SetAsLastSibling();
        elegido.rect.gameObject.SetActive(true);
    }

    private void Update()
    {
        if (particulas == null) return;

        float dt = Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);
        ActualizarParticulas(dt);
        ActualizarMonedas(dt);
        ActualizarTextos(dt);
    }

    private void ActualizarParticulas(float dt)
    {
        float arrastre = Mathf.Exp(-3f * dt);
        for (int i = 0; i < particulas.Length; i++)
        {
            Particula p = particulas[i];
            if (!p.activa) continue;

            p.transcurrido += dt;
            float t = p.transcurrido / p.duracion;
            if (t >= 1f)
            {
                p.activa = false;
                p.rect.gameObject.SetActive(false);
                continue;
            }

            p.velocidad *= arrastre;
            p.velocidad.y -= 1500f * dt;
            p.posicion += p.velocidad * dt;
            p.rect.localPosition = p.posicion;
            p.rect.localScale = Vector3.one * (1f - t);
        }
    }

    private void ActualizarMonedas(float dt)
    {
        for (int i = 0; i < monedas.Length; i++)
        {
            MonedaVolando m = monedas[i];
            if (!m.activa) continue;

            if (m.demora > 0f)
            {
                m.demora -= dt;
                if (m.demora > 0f) continue;
            }

            if (!m.visible)
            {
                m.visible = true;
                m.rect.localPosition = m.desde;
                m.rect.localScale = Vector3.one;
                m.rect.gameObject.SetActive(true);
            }

            // El destino se relee por si se movio (el scroll de las tarjetas): la
            // moneda tiene que caer sobre el boton, no donde estaba al salir.
            if (m.destino != null) m.hasta = PosicionLocal(m.destino);

            m.transcurrido += dt;
            float t = m.transcurrido / m.duracion;
            if (t >= 1f)
            {
                m.activa = false;
                m.rect.gameObject.SetActive(false);
                System.Action<int> alLlegar = m.alLlegar;
                m.alLlegar = null;
                m.destino = null;
                if (alLlegar != null) alLlegar(m.indice);
                continue;
            }

            // Arranca a velocidad media y acelera: llega con envion, que es lo que
            // le da sentido al golpe del boton al recibirla.
            float u = 0.5f * t + 0.5f * t * t;
            float v = 1f - u;
            Vector2 punto = v * v * m.desde + 2f * v * u * m.control + u * u * m.hasta;

            m.rect.localPosition = punto;
            m.rect.localScale = Vector3.one * Mathf.Lerp(1f, 0.6f, t);
            m.rect.localEulerAngles = new Vector3(0f, 0f, 540f * m.transcurrido);
        }
    }

    private void ActualizarTextos(float dt)
    {
        const float duracion = 0.7f;
        for (int i = 0; i < textos.Length; i++)
        {
            TextoVolando tx = textos[i];
            if (!tx.activo) continue;

            tx.transcurrido += dt;
            float s = tx.transcurrido;
            if (s >= duracion)
            {
                tx.activo = false;
                tx.rect.gameObject.SetActive(false);
                continue;
            }

            // Rebote de entrada en los primeros 0,25 s: de 0 a 1,3 y de vuelta a 1.
            float escala = s < 0.15f
                ? Mathf.Lerp(0f, 1.3f, CurvasUI.SalidaCubica(s / 0.15f))
                : Mathf.Lerp(1.3f, 1f, Mathf.Clamp01((s - 0.15f) / 0.1f));

            float t = s / duracion;
            float desplazamiento = (tx.cae ? -80f : 120f) * CurvasUI.SalidaCubica(t);
            float alfa = t < 0.6f ? 1f : 1f - (t - 0.6f) / 0.4f;

            tx.rect.localPosition = tx.origen + new Vector2(0f, desplazamiento);
            tx.rect.localScale = Vector3.one * escala;
            Color c = tx.color;
            c.a *= alfa;
            tx.texto.color = c;
        }
    }

    // Si el panel se cierra con efectos en el aire, se cortan: al volver a abrir no
    // tienen que aparecer monedas de una compra vieja.
    private void OnDisable()
    {
        if (particulas == null) return;

        for (int i = 0; i < particulas.Length; i++)
        {
            particulas[i].activa = false;
            particulas[i].rect.gameObject.SetActive(false);
        }
        for (int i = 0; i < monedas.Length; i++)
        {
            monedas[i].activa = false;
            monedas[i].alLlegar = null;
            monedas[i].destino = null;
            monedas[i].rect.gameObject.SetActive(false);
        }
        for (int i = 0; i < textos.Length; i++)
        {
            textos[i].activo = false;
            textos[i].rect.gameObject.SetActive(false);
        }
    }

    // Posicion del centro de 'r' en el espacio local de este objeto. Los elementos se
    // ubican con localPosition, que se mide desde el pivot de este objeto igual que
    // InverseTransformPoint, asi no importa cual sea ese pivot.
    private Vector2 PosicionLocal(RectTransform r)
    {
        return (Vector2)transform.InverseTransformPoint(r.TransformPoint(r.rect.center));
    }

    // Se llama desde Awake y desde cada efecto: el objeto vive dentro del panel de la
    // tienda, que arranca apagado, y un efecto pedido antes de su primer Awake no
    // tiene que perderse por no tener pool.
    private void CrearPoolsSiFaltan()
    {
        if (particulas != null) return;

        particulas = new Particula[Mathf.Max(0, particulasEnPool)];
        for (int i = 0; i < particulas.Length; i++)
        {
            var p = new Particula();
            p.imagen = CrearImagen("Particula", spriteParticula, Color.white, TamanoParticula);
            p.rect = p.imagen.rectTransform;
            particulas[i] = p;
        }

        monedas = new MonedaVolando[Mathf.Max(0, monedasEnPool)];
        for (int i = 0; i < monedas.Length; i++)
        {
            var m = new MonedaVolando();
            m.imagen = CrearImagen("Moneda", spriteMoneda, colorMoneda, TamanoMoneda);
            m.rect = m.imagen.rectTransform;
            monedas[i] = m;
        }

        textos = new TextoVolando[Mathf.Max(0, textosEnPool)];
        for (int i = 0; i < textos.Length; i++)
        {
            var tx = new TextoVolando();
            var go = new GameObject("Texto", typeof(RectTransform));
            tx.rect = (RectTransform)go.transform;
            PrepararRect(tx.rect, TamanoTexto);
            tx.texto = go.AddComponent<TextMeshProUGUI>();
            if (fuente != null) tx.texto.font = fuente;
            tx.texto.alignment = TextAlignmentOptions.Center;
            tx.texto.textWrappingMode = TextWrappingModes.NoWrap;
            tx.texto.raycastTarget = false;
            go.SetActive(false);
            textos[i] = tx;
        }
    }

    private Image CrearImagen(string nombre, Sprite sprite, Color color, float lado)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        PrepararRect((RectTransform)go.transform, new Vector2(lado, lado));
        var imagen = go.AddComponent<Image>();
        imagen.sprite = sprite;
        imagen.color = color;
        imagen.raycastTarget = false;
        go.SetActive(false);
        return imagen;
    }

    private void PrepararRect(RectTransform rect, Vector2 tamano)
    {
        rect.SetParent(transform, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = tamano;
    }
}
