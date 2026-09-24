using TMPro;
using UnityEngine;
using UnityEngine.UI;

// La primera compra: si el jugador nunca compro nada y le alcanza para la mejora
// recomendada (el daño, que es la que mas se nota en la partida siguiente), una
// flecha dorada que rebota le senala desde abajo el boton de comprar de esa tarjeta, con
// un cartel, y la lista se desplaza hasta ella si no se ve. Apenas compra, la flecha pasa a
// ¡A JUGAR! (desde su izquierda, con el cartel del otro lado, en el pie: encima no hay lugar
// sin tapar las tarjetas), para cerrar el circuito jugar, comprar, notarlo. La tienda muestra
// ocho tarjetas, varias verdes, y no decia cual conviene.
//
// Va en la raiz del prefab Tienda, junto a TiendaMejoras. Todo lo decide el progreso
// (PrimeraVez.NuncaCompro): no hay marca que guardar.
public class GuiaPrimeraCompra : MonoBehaviour
{
    // La flecha: su lado, lo que la separa del objetivo (mas el rebote) y lo que cae su
    // sombra. Publicas para la prueba de que no pisa las tarjetas.
    public const float LadoFlecha = 90f;
    public const float Separacion = 60f;
    public const float Rebote = 22f;
    public const float CaidaSombra = 6f;
    private const float DuracionFundido = 0.15f;

    public TiendaMejoras tienda;
    public string idRecomendada = "dano_bala";
    public Color colorFlecha = new Color(1f, 0.78f, 0.2f, 1f);
    [Tooltip("Lo que espera despues de abrir la tienda: las tarjetas entran de a una.")]
    public float demora = 0.9f;

    private enum Paso { Nada, Comprar, Jugar }

    private Texture2D texturaFlecha;
    private Sprite spriteFlecha;
    private RectTransform raiz;
    private RectTransform flecha, sombra;
    private float desplazarDesde = -1f;
    private float desplazarDe;
    private TMP_Text cartel;
    private Paso paso;
    private float abiertaDesde = -1f;
    private bool comproEnEstaVisita;
    private bool guiaEnCamino;              // en el cuadro anterior senalaba el daño o lo iba a senalar
    private CanvasGroup grupo;
    private readonly Vector3[] esquinas = new Vector3[4];

    private void Start()
    {
        if (tienda == null || tienda.panel == null) return;

        texturaFlecha = TexturasUI.Play(128);
        var go = new GameObject("GuiaPrimeraCompra", typeof(RectTransform), typeof(CanvasGroup));
        raiz = (RectTransform)go.transform;
        raiz.SetParent(tienda.panel.transform, false);
        raiz.anchorMin = raiz.anchorMax = new Vector2(0.5f, 0.5f);
        raiz.sizeDelta = new Vector2(420f, 220f);
        grupo = go.GetComponent<CanvasGroup>();
        grupo.blocksRaycasts = false;
        grupo.interactable = false;

        // La flecha es el triangulo de PLAY, girado segun a que apunte (ver Mostrar), con su sombra.
        spriteFlecha = Sprite.Create(texturaFlecha, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f));
        var sprite = spriteFlecha;
        sombra = Imagen("Sombra", sprite, new Color(0f, 0f, 0f, 0.35f), new Vector2(0f, -CaidaSombra));
        flecha = Imagen("Flecha", sprite, colorFlecha, Vector2.zero);

        var textoGo = new GameObject("Cartel", typeof(RectTransform), typeof(TextMeshProUGUI));
        var rtTexto = (RectTransform)textoGo.transform;
        rtTexto.SetParent(raiz, false);
        rtTexto.sizeDelta = new Vector2(560f, 90f);
        cartel = textoGo.GetComponent<TextMeshProUGUI>();
        if (tienda.pista != null)
        {
            cartel.font = tienda.pista.font;
            cartel.fontSharedMaterial = tienda.pista.fontSharedMaterial;
        }
        cartel.fontSize = 46f;
        cartel.color = colorFlecha;
        cartel.alignment = TextAlignmentOptions.Center;
        cartel.textWrappingMode = TextWrappingModes.NoWrap;
        cartel.raycastTarget = false;

        raiz.gameObject.SetActive(false);
    }

    private RectTransform Imagen(string nombre, Sprite sprite, Color color, Vector2 corrimiento)
    {
        var go = new GameObject(nombre, typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(raiz, false);
        rt.sizeDelta = new Vector2(LadoFlecha, LadoFlecha);
        rt.anchoredPosition = corrimiento;
        rt.localRotation = Quaternion.Euler(0f, 0f, -90f);
        var imagen = go.GetComponent<Image>();
        imagen.sprite = sprite;
        imagen.color = color;
        imagen.raycastTarget = false;
        return rt;
    }

    private void OnDestroy()
    {
        // El sprite tambien es un objeto de Unity: con destruir la textura no alcanza.
        if (spriteFlecha != null) Destroy(spriteFlecha);
        if (texturaFlecha != null) Destroy(texturaFlecha);
    }

    private void LateUpdate()
    {
        if (raiz == null) return;

        if (!tienda.Abierta)
        {
            abiertaDesde = -1f;
            desplazarDesde = -1f;
            comproEnEstaVisita = false;
            guiaEnCamino = false;
            Mostrar(Paso.Nada, null);
            return;
        }
        if (abiertaDesde < 0f) abiertaDesde = Time.unscaledTime;

        TarjetaMejora tarjeta = BuscarTarjeta();
        bool nuncaCompro = PrimeraVez.NuncaCompro;
        bool senalaElDano = nuncaCompro && tarjeta != null && tarjeta.Estado == EstadoMejora.Comprable;
        // La primera compra pasa la flecha a ¡A JUGAR! si en el cuadro anterior la guia senalaba
        // el daño o lo iba a senalar al terminar la demora. Mirando solo el paso Comprar, una
        // compra en los 0,9 s de la demora (las tarjetas ya entraron a los 0,72) dejaba a la
        // guia sin la flecha a ¡A JUGAR!.
        if (!nuncaCompro && guiaEnCamino) comproEnEstaVisita = true;
        guiaEnCamino = senalaElDano;

        Paso siguiente = Paso.Nada;
        RectTransform objetivo = null;
        if (Time.unscaledTime - abiertaDesde >= demora)
        {
            if (senalaElDano)
            {
                siguiente = Paso.Comprar;
                objetivo = tarjeta.raizBoton;
            }
            else if (comproEnEstaVisita && tienda.botonJugar != null)
            {
                siguiente = Paso.Jugar;
                objetivo = (RectTransform)tienda.botonJugar.transform;
            }
        }

        bool recienMostrada = Mostrar(siguiente, tarjeta);
        if (objetivo == null) return;
        Desplazar();

        // Pegada al objetivo, rebotando. La posicion se toma en cada frame: las tarjetas entran
        // animadas y la lista se mueve.
        bool comprar = paso == Paso.Comprar;
        float distancia = Separacion + Mathf.Abs(Mathf.Sin(Time.unscaledTime * 5f)) * Rebote;
        raiz.anchoredPosition = CentroDeLaFlecha(EnElPanel(objetivo), comprar, distancia);

        // La lista recorta las tarjetas pero no a la guia, que cuelga del panel: con el boton
        // del daño afuera de la lista, la flecha y el cartel quedaban flotando al costado. Se
        // va y vuelve con un fundido corto; al cambiar de paso aparece de una, como siempre.
        float alfa = !comprar || SeVeEnLaLista(objetivo) ? 1f : 0f;
        float dt = Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);
        grupo.alpha = recienMostrada ? alfa : Mathf.MoveTowards(grupo.alpha, alfa, dt / DuracionFundido);
    }

    // El centro de la flecha, en el espacio del rectangulo que se le pasa: debajo del boton de
    // comprar (encima estan los numeros de la tarjeta) o a la izquierda de ¡A JUGAR!, que esta
    // abajo de todo, a 'distancia' de su borde. Encima de ¡A JUGAR! no va: en 20:9 y 21:9 el pie
    // queda a unos 80 de las tarjetas y la flecha, de 90, pisaba los botones de comprar.
    // Estatica para la prueba.
    public static Vector2 CentroDeLaFlecha(Rect objetivo, bool comprar, float distancia)
    {
        return comprar
            ? new Vector2(objetivo.center.x, objetivo.yMin - distancia)
            : new Vector2(objetivo.xMin - distancia, objetivo.center.y);
    }

    // El rectangulo del objetivo en el espacio del panel, del que cuelga la guia (con anclas
    // y pivote al medio, asi que su anchoredPosition se mide en ese espacio).
    private Rect EnElPanel(RectTransform objetivo)
    {
        objetivo.GetWorldCorners(esquinas);
        Transform panel = raiz.parent;
        Vector3 min = panel.InverseTransformPoint(esquinas[0]);
        Vector3 max = panel.InverseTransformPoint(esquinas[2]);
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    // Si el centro del boton cae dentro de la lista (el viewport, con su RectMask2D, es el que
    // recorta las tarjetas), con lugar para la flecha a los costados.
    private bool SeVeEnLaLista(RectTransform objetivo)
    {
        RectTransform vista = tienda.viewport;
        if (vista == null) return true;
        float x = vista.InverseTransformPoint(objetivo.TransformPoint(objetivo.rect.center)).x;
        Rect r = vista.rect;
        return x >= r.xMin + LadoFlecha * 0.5f && x <= r.xMax - LadoFlecha * 0.5f;
    }

    // Si la tarjeta entra entera en la lista.
    private bool EnteraEnLaLista(RectTransform tarjeta)
    {
        RectTransform vista = tienda.viewport;
        if (vista == null) return false;
        tarjeta.GetWorldCorners(esquinas);
        Rect r = vista.rect;
        return vista.InverseTransformPoint(esquinas[0]).x >= r.xMin - 1f
            && vista.InverseTransformPoint(esquinas[2]).x <= r.xMax + 1f;
    }

    // La lista de tarjetas se desplaza sola hasta la recomendada si no se ve entera, una vez
    // por visita: despues el jugador la mueve como quiere.
    private void Desplazar()
    {
        if (desplazarDesde < 0f || tienda.scroll == null) return;
        float t = Mathf.Clamp01((Time.unscaledTime - desplazarDesde) / 0.5f);
        tienda.scroll.horizontalNormalizedPosition = Mathf.Lerp(desplazarDe, destinoScroll, CurvasUI.SalidaAtras(t));
        if (t >= 1f) desplazarDesde = -1f;
    }

    private float destinoScroll;

    // Devuelve si cambio de paso.
    private bool Mostrar(Paso nuevo, TarjetaMejora tarjeta)
    {
        if (nuevo == paso) return false;
        paso = nuevo;
        raiz.gameObject.SetActive(nuevo != Paso.Nada);
        if (nuevo == Paso.Nada) return true;

        // Comprar: la flecha apunta hacia arriba y el cartel va a su derecha (abajo a la
        // izquierda esta el pie de la tienda). Jugar: la flecha apunta a la derecha, a
        // ¡A JUGAR!, y el cartel va a su izquierda, en el lugar libre del pie (es mas bajo
        // que la flecha: si ella no pisa las tarjetas, el tampoco).
        bool comprar = nuevo == Paso.Comprar;
        var giro = Quaternion.Euler(0f, 0f, comprar ? 90f : 0f);
        flecha.localRotation = giro;
        sombra.localRotation = giro;
        var rtTexto = cartel.rectTransform;
        rtTexto.anchoredPosition = new Vector2(comprar ? 340f : -340f, 0f);
        cartel.alignment = comprar ? TextAlignmentOptions.Left : TextAlignmentOptions.Right;
        cartel.text = comprar ? Textos.De("guia_compra") : Textos.De("guia_a_jugar");

        // Si ya se ve entera no se desplaza: la tienda abre con la fila al principio, y
        // desplazarla igual era pelearle medio segundo el dedo al que ya la estaba moviendo.
        if (comprar && tarjeta != null && tienda.scroll != null && !EnteraEnLaLista((RectTransform)tarjeta.transform))
        {
            int indice = IndiceDe(tarjeta);
            int total = tienda.Tarjetas.Count;
            destinoScroll = total > 1 ? Mathf.Clamp01(indice / (float)(total - 1)) : 0f;
            desplazarDe = tienda.scroll.horizontalNormalizedPosition;
            desplazarDesde = Time.unscaledTime;
        }
        return true;
    }

    private int IndiceDe(TarjetaMejora tarjeta)
    {
        var tarjetas = tienda.Tarjetas;
        for (int i = 0; i < tarjetas.Count; i++)
        {
            if (tarjetas[i] == tarjeta) return i;
        }
        return 0;
    }

    private TarjetaMejora BuscarTarjeta()
    {
        var tarjetas = tienda.Tarjetas;
        for (int i = 0; i < tarjetas.Count; i++)
        {
            if (tarjetas[i] != null && tarjetas[i].Mejora != null && tarjetas[i].Mejora.id == idRecomendada) return tarjetas[i];
        }
        return null;
    }
}
