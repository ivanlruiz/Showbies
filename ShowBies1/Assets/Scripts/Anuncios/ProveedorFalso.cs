using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// El proveedor de prueba: en vez de un video, un cartel a pantalla completa con
// una barra que se llena y dos botones, "Saltear" y (cuando termina) "Listo".
// Sirve para probar TODO el circuito (topes, premio, cerrar antes, perder el foco)
// sin cuenta de anuncios ni internet, y anda igual en el telefono.
//
// La UI se arma por codigo a proposito: no es parte del juego y no tiene que
// ensuciar ninguna escena ni prefab.
public class ProveedorFalso : IProveedorAnuncios
{
    public const float SegundosDeVideo = 5f;

    private CarteloFalso cartel;

    public string Nombre { get { return "falso"; } }

    public void Inicializar()
    {
    }

    // Siempre hay "video": es justo lo que se quiere probar.
    public bool Listo(string lugar)
    {
        return true;
    }

    public void Mostrar(string lugar, Action<ResultadoAnuncio> alTerminar)
    {
        if (cartel == null) cartel = CarteloFalso.Crear();
        cartel.Mostrar(lugar, SegundosDeVideo, alTerminar);
    }

    // El cartel en si. Vive en un objeto con DontDestroyOnLoad porque el x2 de la
    // derrota puede terminar cambiando de escena.
    private class CarteloFalso : MonoBehaviour
    {
        private Canvas canvas;
        private Image relleno;
        private TextMeshProUGUI texto;
        private Button botonSaltear;
        private TextMeshProUGUI textoSaltear;

        private Action<ResultadoAnuncio> alTerminar;
        private float duracion;
        private float desde;
        private bool corriendo;
        private string lugar;

        public static CarteloFalso Crear()
        {
            var objeto = new GameObject("AnuncioFalso");
            DontDestroyOnLoad(objeto);
            var cartel = objeto.AddComponent<CarteloFalso>();
            cartel.Construir();
            return cartel;
        }

        private void Construir()
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // Por encima de todo, incluida la pausa (10).
            canvas.sortingOrder = 100;

            var escalador = gameObject.AddComponent<CanvasScaler>();
            escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            escalador.referenceResolution = new Vector2(1920f, 1080f);
            escalador.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();

            Image fondo = NuevaImagen("Fondo", transform, new Color(0.04f, 0.04f, 0.06f, 1f));
            Estirar(fondo.rectTransform);

            texto = NuevoTexto("Titulo", fondo.transform, 72f);
            texto.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            texto.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            texto.rectTransform.sizeDelta = new Vector2(1600f, 300f);
            texto.rectTransform.anchoredPosition = new Vector2(0f, 60f);

            Image barra = NuevaImagen("Barra", fondo.transform, new Color(1f, 1f, 1f, 0.15f));
            barra.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            barra.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            barra.rectTransform.sizeDelta = new Vector2(900f, 24f);
            barra.rectTransform.anchoredPosition = new Vector2(0f, -110f);

            // El ancho se mueve a mano y no con Image.Type.Filled: un Image sin sprite
            // ignora el fillAmount y la barra se ve llena desde el primer frame.
            relleno = NuevaImagen("Relleno", barra.transform, new Color(1f, 0.75f, 0.2f, 1f));
            var rectRelleno = relleno.rectTransform;
            rectRelleno.anchorMin = new Vector2(0f, 0f);
            rectRelleno.anchorMax = new Vector2(0f, 1f);
            rectRelleno.pivot = new Vector2(0f, 0.5f);
            rectRelleno.offsetMin = Vector2.zero;
            rectRelleno.offsetMax = Vector2.zero;
            rectRelleno.sizeDelta = new Vector2(0f, 0f);

            botonSaltear = NuevoBoton("Saltear", fondo.transform, out textoSaltear);
            botonSaltear.onClick.AddListener(Apretar);
        }

        private void Update()
        {
            if (!corriendo) return;

            // Tiempo sin escalar: la derrota puede estar en pausa de impacto.
            float pasado = Time.unscaledTime - desde;
            float fraccion = duracion > 0f ? Mathf.Clamp01(pasado / duracion) : 1f;
            Llenar(fraccion);

            if (fraccion >= 1f)
            {
                textoSaltear.text = "LISTO";
                texto.text = "ANUNCIO DE PRUEBA\n<size=44>Terminó: tocá LISTO y cobrás</size>";
            }
            else
            {
                int faltan = Mathf.CeilToInt(duracion - pasado);
                textoSaltear.text = "SALTEAR (" + faltan + ")";
            }
        }

        public void Mostrar(string lugar, float segundos, Action<ResultadoAnuncio> alTerminar)
        {
            this.lugar = lugar;
            this.alTerminar = alTerminar;
            duracion = Mathf.Max(0.1f, segundos);
            desde = Time.unscaledTime;
            corriendo = true;

            Llenar(0f);
            texto.text = "ANUNCIO DE PRUEBA\n<size=44>lugar: " + lugar + "</size>";
            textoSaltear.text = "SALTEAR";
            gameObject.SetActive(true);
            canvas.enabled = true;
        }

        private void Apretar()
        {
            if (!corriendo) return;
            corriendo = false;
            canvas.enabled = false;

            bool completo = Time.unscaledTime - desde >= duracion;
            Action<ResultadoAnuncio> aviso = alTerminar;
            alTerminar = null;
            Debug.Log("AnuncioFalso: " + lugar + (completo ? " visto entero" : " salteado"));
            if (aviso != null) aviso(completo ? ResultadoAnuncio.Recompensado : ResultadoAnuncio.Cerrado);
        }

        private void Llenar(float fraccion)
        {
            var rect = relleno.rectTransform;
            float ancho = ((RectTransform)rect.parent).rect.width;
            rect.sizeDelta = new Vector2(ancho * Mathf.Clamp01(fraccion), 0f);
        }

        private static void Estirar(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static Image NuevaImagen(string nombre, Transform padre, Color color)
        {
            var objeto = new GameObject(nombre, typeof(RectTransform));
            objeto.transform.SetParent(padre, false);
            var imagen = objeto.AddComponent<Image>();
            imagen.color = color;
            return imagen;
        }

        private static TextMeshProUGUI NuevoTexto(string nombre, Transform padre, float tamano)
        {
            var objeto = new GameObject(nombre, typeof(RectTransform));
            objeto.transform.SetParent(padre, false);
            var texto = objeto.AddComponent<TextMeshProUGUI>();
            texto.fontSize = tamano;
            texto.alignment = TextAlignmentOptions.Center;
            texto.color = Color.white;
            texto.raycastTarget = false;
            return texto;
        }

        private static Button NuevoBoton(string nombre, Transform padre, out TextMeshProUGUI etiqueta)
        {
            Image fondo = NuevaImagen(nombre, padre, new Color(1f, 1f, 1f, 0.9f));
            fondo.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            fondo.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            fondo.rectTransform.sizeDelta = new Vector2(560f, 130f);
            fondo.rectTransform.anchoredPosition = new Vector2(0f, -280f);

            etiqueta = NuevoTexto("Etiqueta", fondo.transform, 52f);
            Estirar(etiqueta.rectTransform);
            etiqueta.color = new Color(0.08f, 0.08f, 0.1f, 1f);

            var boton = fondo.gameObject.AddComponent<Button>();
            boton.targetGraphic = fondo;
            return boton;
        }
    }
}
