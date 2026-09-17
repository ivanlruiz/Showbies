using TMPro;
using UnityEngine;

// Pone el icono de un boton pildora pegado a la izquierda de su texto, con los dos
// centrados juntos en el boton. El ancho del texto cambia con el idioma (PLAY /
// JUGAR) y con lo que escriben BotonOleadas o BotonModoLibre, asi que se mide cada
// vez que cambia el texto o el tamanio, no una sola vez al armar la escena.
//
// Va en el hijo "Icono" (dentro de Visual, con el ancla a la izquierda y al medio).
public class IconoDeBoton : MonoBehaviour
{
    public TMP_Text texto;
    public float separacion = 18f;

    private string ultimoTexto;
    private float ultimoAncho = -1f;

    private void LateUpdate()
    {
        if (texto == null) return;
        var rtTexto = texto.rectTransform;
        float ancho = rtTexto.rect.width;
        if (texto.text == ultimoTexto && Mathf.Approximately(ancho, ultimoAncho)) return;
        ultimoTexto = texto.text;
        ultimoAncho = ancho;
        Acomodar();
    }

    private void Acomodar()
    {
        var rt = (RectTransform)transform;
        var rtTexto = texto.rectTransform;
        float icono = rt.rect.width;
        float lugar = icono + separacion;

        // El texto se corre a la derecha la mitad de lo que ocupa el icono.
        texto.margin = new Vector4(lugar, texto.margin.y, 0f, texto.margin.w);
        float anchoTexto = Mathf.Min(texto.GetPreferredValues(texto.text, Mathf.Infinity, Mathf.Infinity).x,
                                     rtTexto.rect.width - lugar);

        // Centro del texto en el eje del padre (Visual), con el ancla del icono a la izquierda.
        var padre = (RectTransform)rt.parent;
        float centroTexto = rtTexto.rect.center.x + rtTexto.localPosition.x + lugar * 0.5f;
        float bordeIzquierdo = padre.rect.xMin;
        float x = centroTexto - anchoTexto * 0.5f - separacion - icono * 0.5f;
        rt.anchoredPosition = new Vector2(x - bordeIzquierdo, rt.anchoredPosition.y);
    }
}
