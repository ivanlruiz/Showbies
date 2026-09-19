using TMPro;
using UnityEngine;
using UnityEngine.UI;

// La primera compra: si el jugador nunca compro nada y le alcanza para la mejora
// recomendada (el daño, que es la que mas se nota en la partida siguiente), una
// flecha dorada que rebota le senala desde abajo el boton de comprar de esa tarjeta, con
// un cartel, y la lista se desplaza hasta ella. Apenas compra, la flecha pasa a ¡A JUGAR!
// (desde arriba, con el cartel al costado para no tapar las tarjetas), para cerrar el
// circuito jugar, comprar, notarlo. La tienda muestra ocho tarjetas, varias verdes, y no decia cual conviene.
//
// Va en la raiz del prefab Tienda, junto a TiendaMejoras. Todo lo decide el progreso
// (PrimeraVez.NuncaCompro): no hay marca que guardar.
public class GuiaPrimeraCompra : MonoBehaviour
{
    public TiendaMejoras tienda;
    public string idRecomendada = "dano_bala";
    public Color colorFlecha = new Color(1f, 0.78f, 0.2f, 1f);
    [Tooltip("Lo que espera despues de abrir la tienda: las tarjetas entran de a una.")]
    public float demora = 0.9f;

    private enum Paso { Nada, Comprar, Jugar }

    private Texture2D texturaFlecha;
    private RectTransform raiz;
    private RectTransform flecha, sombra;
    private float desplazarDesde = -1f;
    private float desplazarDe;
    private TMP_Text cartel;
    private Paso paso;
    private float abiertaDesde = -1f;
    private bool comproEnEstaVisita;

    private void Start()
    {
        if (tienda == null || tienda.panel == null) return;

        texturaFlecha = TexturasUI.Play(128);
        var go = new GameObject("GuiaPrimeraCompra", typeof(RectTransform), typeof(CanvasGroup));
        raiz = (RectTransform)go.transform;
        raiz.SetParent(tienda.panel.transform, false);
        raiz.anchorMin = raiz.anchorMax = new Vector2(0.5f, 0.5f);
        raiz.sizeDelta = new Vector2(420f, 220f);
        var grupo = go.GetComponent<CanvasGroup>();
        grupo.blocksRaycasts = false;
        grupo.interactable = false;

        // La flecha es el triangulo de PLAY girado para apuntar hacia abajo, con su sombra.
        var sprite = Sprite.Create(texturaFlecha, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f));
        sombra = Imagen("Sombra", sprite, new Color(0f, 0f, 0f, 0.35f), new Vector2(0f, -6f));
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
        rt.sizeDelta = new Vector2(90f, 90f);
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
            Mostrar(Paso.Nada, null);
            return;
        }
        if (abiertaDesde < 0f) abiertaDesde = Time.unscaledTime;

        TarjetaMejora tarjeta = BuscarTarjeta();
        bool nuncaCompro = PrimeraVez.NuncaCompro;
        if (!nuncaCompro && paso == Paso.Comprar) comproEnEstaVisita = true;

        Paso siguiente = Paso.Nada;
        RectTransform objetivo = null;
        if (Time.unscaledTime - abiertaDesde >= demora)
        {
            if (nuncaCompro && tarjeta != null && tarjeta.Estado == EstadoMejora.Comprable)
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

        Mostrar(siguiente, tarjeta);
        if (objetivo == null) return;
        Desplazar();

        // Pegada al objetivo, rebotando: debajo del boton de comprar (encima estan los
        // numeros de la tarjeta) y encima de ¡A JUGAR!, que esta abajo de todo. La
        // posicion se toma en cada frame: las tarjetas entran animadas y la lista se mueve.
        bool desdeAbajo = paso == Paso.Comprar;
        float y = desdeAbajo ? objetivo.rect.yMin : objetivo.rect.yMax;
        raiz.position = objetivo.TransformPoint(new Vector3(objetivo.rect.center.x, y, 0f));
        float distancia = 60f + Mathf.Abs(Mathf.Sin(Time.unscaledTime * 5f)) * 22f;
        raiz.anchoredPosition += new Vector2(0f, desdeAbajo ? -distancia : distancia);
    }

    // La lista de tarjetas se desplaza sola hasta la recomendada, una vez por visita:
    // despues el jugador la mueve como quiere.
    private void Desplazar()
    {
        if (desplazarDesde < 0f || tienda.scroll == null) return;
        float t = Mathf.Clamp01((Time.unscaledTime - desplazarDesde) / 0.5f);
        tienda.scroll.horizontalNormalizedPosition = Mathf.Lerp(desplazarDe, destinoScroll, CurvasUI.SalidaAtras(t));
        if (t >= 1f) desplazarDesde = -1f;
    }

    private float destinoScroll;

    private void Mostrar(Paso nuevo, TarjetaMejora tarjeta)
    {
        if (nuevo == paso) return;
        paso = nuevo;
        raiz.gameObject.SetActive(nuevo != Paso.Nada);
        if (nuevo == Paso.Nada) return;

        // Comprar: la flecha apunta hacia arriba y el cartel va a su derecha (abajo a la
        // izquierda esta el pie de la tienda). Jugar: la flecha apunta hacia abajo y el
        // cartel va a su izquierda (a la derecha se termina la pantalla).
        bool comprar = nuevo == Paso.Comprar;
        var giro = Quaternion.Euler(0f, 0f, comprar ? 90f : -90f);
        flecha.localRotation = giro;
        sombra.localRotation = giro;
        var rtTexto = cartel.rectTransform;
        rtTexto.anchoredPosition = new Vector2(comprar ? 340f : -340f, 0f);
        cartel.alignment = comprar ? TextAlignmentOptions.Left : TextAlignmentOptions.Right;
        cartel.text = comprar ? Textos.De("guia_compra") : Textos.De("guia_a_jugar");

        if (comprar && tarjeta != null && tienda.scroll != null)
        {
            int indice = IndiceDe(tarjeta);
            int total = tienda.Tarjetas.Count;
            destinoScroll = total > 1 ? Mathf.Clamp01(indice / (float)(total - 1)) : 0f;
            desplazarDe = tienda.scroll.horizontalNormalizedPosition;
            desplazarDesde = Time.unscaledTime;
        }
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
