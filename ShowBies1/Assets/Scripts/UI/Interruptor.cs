using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Un interruptor de si o no armado en codigo, con el mismo molde que SliderVolumen:
// el nombre a la izquierda y, a la derecha, una pildora con una perilla que se corre
// de un lado al otro. Lo usa el modo oscuro en la ventana de opciones del menu.
//
// Se arma en codigo, como los volumenes, para no sumar prefabs: quien lo crea le pasa
// los dos dibujos (la pildora y el circulo de la perilla) y es duenio de ellos.
//
// La pildora y la perilla cuelgan de un hijo "Visual", como todos los botones del
// juego: BotonJugoso anima ese hijo y este componente mueve la perilla adentro, asi
// no se pelean por la misma posicion.
public class Interruptor : MonoBehaviour
{
    public const float Ancho = 132f;
    public const float Alto = 64f;

    private static readonly Color ColorEncendido = new Color(0.49f, 0.878f, 0.29f, 1f);
    private static readonly Color ColorApagadoClaro = new Color(0f, 0f, 0f, 0.35f);

    // Apagado es un hueco en la ventana, asi que sigue al tema: negro sobre la crema y
    // claro sobre la oscura.
    private static Color ColorApagado
    {
        get { return Tema.Elegir(ColorApagadoClaro, RolDeTema.Surco); }
    }

    private Image fondo;
    private RectTransform perilla;
    private Func<bool> leer;
    private Action<bool> alCambiar;
    private bool encendido;
    private float desde = -1f;      // cuando se toco, para el deslizado
    private float origen;
    private int revisionVista = -1;

    // `colorTexto` lo pone quien lo crea: depende de sobre que fondo esta, como en
    // SliderVolumen.
    //
    // `leer` es de donde sale lo que muestra, y no una foto del valor al crearlo: si
    // alguien mas lo cambia, la perilla se corre igual. Hoy el unico que cambia el tema
    // es este mismo interruptor, pero una foto se queda vieja en silencio.
    public static Interruptor Crear(RectTransform padre, string idTexto, Vector2 posicion, float ancho,
                                    TMP_FontAsset fuente, Func<bool> leer, Action<bool> alCambiar,
                                    Sprite pildora, Sprite circulo, AudioClip sonidoClick, Color colorTexto)
    {
        var raiz = new GameObject("Interruptor_" + idTexto, typeof(RectTransform));
        var rt = (RectTransform)raiz.transform;
        rt.SetParent(padre, false);
        rt.sizeDelta = new Vector2(ancho, 110f);
        rt.anchoredPosition = posicion;

        // Apagado mientras se le pone el id: TextoTraducido escribe en OnEnable, y sin
        // id mostraria "[]" y avisaria un texto que falta.
        var nombreRt = Rect(rt, "Nombre", new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(0f, 64f));
        var nombre = nombreRt.gameObject.AddComponent<TextMeshProUGUI>();
        if (fuente != null) nombre.font = fuente;
        nombre.fontSize = 46f;
        nombre.alignment = TextAlignmentOptions.Left;
        nombre.color = colorTexto;
        nombre.raycastTarget = false;
        nombre.gameObject.SetActive(false);
        nombre.gameObject.AddComponent<TextoTraducido>().id = idTexto;
        nombre.gameObject.SetActive(true);

        var llave = Rect(rt, "Llave", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-Ancho * 0.5f, 0f), new Vector2(Ancho, Alto));
        var toque = llave.gameObject.AddComponent<Image>();
        toque.color = new Color(1f, 1f, 1f, 0f);

        var visual = Rect(llave, "Visual", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(Ancho, Alto));
        var imgFondo = visual.gameObject.AddComponent<Image>();
        imgFondo.sprite = pildora;
        imgFondo.type = Image.Type.Sliced;
        imgFondo.pixelsPerUnitMultiplier = 4f;
        imgFondo.raycastTarget = false;

        var perillaRt = Rect(visual, "Perilla", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(Alto - 12f, Alto - 12f));
        var imgPerilla = perillaRt.gameObject.AddComponent<Image>();
        imgPerilla.sprite = circulo;
        imgPerilla.color = Color.white;
        imgPerilla.preserveAspect = true;
        imgPerilla.raycastTarget = false;

        var control = raiz.AddComponent<Interruptor>();
        control.fondo = imgFondo;
        control.perilla = perillaRt;
        control.leer = leer;
        control.alCambiar = alCambiar;
        control.encendido = leer != null && leer();
        control.Acomodar(1f);

        var boton = llave.gameObject.AddComponent<Button>();
        boton.targetGraphic = toque;
        boton.onClick.AddListener(control.Tocar);
        var jugoso = llave.gameObject.AddComponent<BotonJugoso>();
        jugoso.visual = visual;
        jugoso.sonidoClick = sonidoClick;
        return control;
    }

    private void Tocar()
    {
        encendido = !encendido;
        origen = perilla != null ? perilla.anchoredPosition.x : 0f;
        desde = Time.unscaledTime;
        if (alCambiar != null) alCambiar(encendido);
    }

    private void Update()
    {
        // Este mismo interruptor es el que cambia el tema: mientras se desliza ya se pinta
        // con el nuevo, y quieto se repinta cuando cambia.
        bool cambioElTema = revisionVista != Tema.Revision;
        revisionVista = Tema.Revision;

        // Si lo que muestra cambio desde afuera, la perilla se corre igual que si lo
        // hubieran tocado, pero sin volver a avisar.
        if (desde < 0f && leer != null && leer() != encendido)
        {
            encendido = !encendido;
            origen = perilla != null ? perilla.anchoredPosition.x : 0f;
            desde = Time.unscaledTime;
        }

        if (desde < 0f)
        {
            if (cambioElTema) Acomodar(1f);
            return;
        }
        float t = Mathf.Clamp01((Time.unscaledTime - desde) / 0.22f);
        Acomodar(t);
        if (t >= 1f) desde = -1f;
    }

    // La perilla va de un extremo al otro con el mismo rebote que los botones, y la
    // pildora se tiñe al mismo tiempo.
    private void Acomodar(float t)
    {
        if (perilla == null || fondo == null) return;
        float destino = (encendido ? 1f : -1f) * (Ancho - Alto) * 0.5f;
        float x = t >= 1f ? destino : Mathf.LerpUnclamped(origen, destino, CurvasUI.SalidaAtras(t));
        perilla.anchoredPosition = new Vector2(x, 0f);
        float mezcla = CurvasUI.SalidaCubica(t);
        fondo.color = Color.Lerp(ColorApagado, ColorEncendido, encendido ? mezcla : 1f - mezcla);
    }

    private static RectTransform Rect(RectTransform padre, string nombre, Vector2 anclaMin, Vector2 anclaMax, Vector2 posicion, Vector2 tamanio)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(padre, false);
        rt.anchorMin = anclaMin;
        rt.anchorMax = anclaMax;
        rt.anchoredPosition = posicion;
        rt.sizeDelta = tamanio;
        return rt;
    }
}
