using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// Los caramelos de Halloween en el HUD (EventoHalloween), debajo de la oleada o del nivel:
// una copia del texto de las monedas, en naranja, que salta al agarrar uno. Va donde estaba
// el contador de FPS, que baja un renglon (si esta prendido en OPCIONES). Lo arma
// InstaladorHalloween; las escenas no lo traen.
public class ContadorCaramelos : MonoBehaviour
{
    public static readonly Color Naranja = new Color(1f, 0.6f, 0.15f, 1f);
    public const float Renglon = 60f;

    private TMP_Text texto;
    private long mostrados = -1;
    private float salto;
    private Vector3 escalaBase;

    // Copia el texto de las monedas ("Monedas", con su ContadorMonedas) en el lugar del de
    // los FPS y baja ese un renglon. Sin alguno de los dos, no pone nada.
    public static ContadorCaramelos Instalar(Scene escena)
    {
        ContadorMonedas monedas = null;
        ContadorFps fps = null;
        foreach (var raiz in escena.GetRootGameObjects())
        {
            if (monedas == null) monedas = raiz.GetComponentInChildren<ContadorMonedas>(true);
            if (fps == null) fps = raiz.GetComponentInChildren<ContadorFps>(true);
        }
        if (monedas == null || monedas.texto == null || fps == null) return null;

        var original = monedas.texto.rectTransform;
        var rtFps = (RectTransform)fps.transform;
        var copia = Instantiate(original.gameObject, original.parent);
        copia.name = "Caramelos";
        // Lo que escribia el texto de las monedas se va: lo escribe este. En el acto: con
        // Destroy, el de las monedas escribiria una vez mas en este cuadro.
        foreach (var c in copia.GetComponents<ContadorMonedas>()) DestroyImmediate(c);
        foreach (var c in copia.GetComponents<TextoTraducido>()) DestroyImmediate(c);
        var rt = (RectTransform)copia.transform;
        rt.anchoredPosition = new Vector2(original.anchoredPosition.x, rtFps.anchoredPosition.y);
        rtFps.anchoredPosition += Vector2.down * Renglon;

        var contador = copia.AddComponent<ContadorCaramelos>();
        contador.texto = copia.GetComponent<TMP_Text>();
        contador.texto.color = Naranja;
        return contador;
    }

    private void Awake()
    {
        if (texto == null) texto = GetComponent<TMP_Text>();
        escalaBase = transform.localScale;
    }

    private void Update()
    {
        if (texto == null) return;
        long ahora = (long)System.Math.Floor(EventoHalloween.Caramelos + 1e-6);
        if (ahora != mostrados)
        {
            if (mostrados >= 0 && ahora > mostrados) salto = 1f;
            mostrados = ahora;
            texto.text = Textos.Formato("hud_caramelos", FormatoNumeros.Compacto(ahora));
        }
        if (salto > 0f)
        {
            salto = Mathf.Max(0f, salto - Mathf.Min(Time.unscaledDeltaTime, 1f / 30f) / 0.2f);
            transform.localScale = escalaBase * (1f + 0.35f * salto);
        }
    }
}
