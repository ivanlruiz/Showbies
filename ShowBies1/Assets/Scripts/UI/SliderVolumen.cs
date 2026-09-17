using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Un control de volumen armado por codigo: el nombre arriba a la izquierda, el
// porcentaje arriba a la derecha y la barra con su perilla abajo. Lo usan la
// ventanita de sonido del menu y el menu de pausa, asi los dos se ven igual.
//
// Se arma en codigo, como las texturas de TexturasUI, para no sumar prefabs: el
// que lo crea es duenio de la textura de la perilla y la destruye.
public class SliderVolumen : MonoBehaviour
{
    public static readonly Color ColorBarra = new Color(0f, 0f, 0f, 0.45f);
    public static readonly Color ColorRelleno = new Color(1f, 0.78f, 0.22f, 1f);

    private Slider slider;
    private TMP_Text porcentaje;
    private Action<float> alCambiar;
    private float cambioSinGuardar = -1f;

    public static SliderVolumen Crear(RectTransform padre, string idTexto, Vector2 posicion, float ancho,
                                      TMP_FontAsset fuente, float valor, Action<float> alCambiar, Sprite perilla)
    {
        var raiz = new GameObject("Volumen_" + idTexto, typeof(RectTransform));
        var rt = (RectTransform)raiz.transform;
        rt.SetParent(padre, false);
        rt.sizeDelta = new Vector2(ancho, 110f);
        rt.anchoredPosition = posicion;

        var nombre = Texto(rt, "Nombre", fuente, 46f, TextAlignmentOptions.Left);
        // Apagado mientras se le pone el id: TextoTraducido escribe en OnEnable, y sin id
        // mostraria "[]" y avisaria un texto que falta.
        nombre.gameObject.SetActive(false);
        nombre.gameObject.AddComponent<TextoTraducido>().id = idTexto;
        nombre.gameObject.SetActive(true);
        var pct = Texto(rt, "Porcentaje", fuente, 46f, TextAlignmentOptions.Right);

        // La barra: fondo, relleno y perilla, como un Slider de Unity de los de siempre.
        var barra = Rect(rt, "Barra", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 22f), new Vector2(0f, 36f));
        var fondo = barra.gameObject.AddComponent<Image>();
        fondo.color = ColorBarra;

        var areaRelleno = Rect(barra, "AreaRelleno", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var relleno = Rect(areaRelleno, "Relleno", Vector2.zero, new Vector2(0f, 1f), Vector2.zero, Vector2.zero);
        relleno.gameObject.AddComponent<Image>().color = ColorRelleno;

        var areaPerilla = Rect(barra, "AreaPerilla", Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-40f, 0f));
        var perillaRt = Rect(areaPerilla, "Perilla", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(64f, 64f));
        var imagenPerilla = perillaRt.gameObject.AddComponent<Image>();
        imagenPerilla.sprite = perilla;
        imagenPerilla.color = Color.white;
        // El Slider estira la perilla al alto de la barra (sus anclas van de 0 a 1 en y):
        // sin esto el circulo sale ovalado.
        imagenPerilla.preserveAspect = true;

        var s = barra.gameObject.AddComponent<Slider>();
        s.fillRect = relleno;
        s.handleRect = perillaRt;
        s.targetGraphic = imagenPerilla;
        s.direction = Slider.Direction.LeftToRight;
        s.minValue = 0f;
        s.maxValue = 1f;
        s.SetValueWithoutNotify(Mathf.Clamp01(valor));

        var control = raiz.AddComponent<SliderVolumen>();
        control.slider = s;
        control.porcentaje = pct;
        control.alCambiar = alCambiar;
        control.EscribirPorcentaje(s.value);
        s.onValueChanged.AddListener(control.Cambio);
        return control;
    }

    private void Cambio(float valor)
    {
        EscribirPorcentaje(valor);
        if (alCambiar != null) alCambiar(valor);
        cambioSinGuardar = Time.unscaledTime;
    }

    // Mientras se arrastra solo cambia en memoria; se escribe a disco medio segundo
    // despues del ultimo movimiento, o al cerrarse la ventana. El Slider se queda con
    // el evento de soltar, por eso no se usa OnPointerUp.
    private void Update()
    {
        if (cambioSinGuardar >= 0f && Time.unscaledTime - cambioSinGuardar > 0.5f) GuardarSiHaceFalta();
    }

    private void OnDisable()
    {
        GuardarSiHaceFalta();
    }

    private void GuardarSiHaceFalta()
    {
        if (cambioSinGuardar < 0f) return;
        cambioSinGuardar = -1f;
        Volumen.Guardar();
    }

    private void EscribirPorcentaje(float valor)
    {
        if (porcentaje != null) porcentaje.SetText("{0}%", Mathf.RoundToInt(valor * 100f));
    }

    private static TMP_Text Texto(RectTransform padre, string nombre, TMP_FontAsset fuente, float tamanio, TextAlignmentOptions alineacion)
    {
        var rt = Rect(padre, nombre, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -28f), new Vector2(0f, 56f));
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (fuente != null) t.font = fuente;
        t.fontSize = tamanio;
        t.alignment = alineacion;
        t.color = Color.white;
        t.raycastTarget = false;
        return t;
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
