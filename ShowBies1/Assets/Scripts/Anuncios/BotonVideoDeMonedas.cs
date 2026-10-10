using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// El boton del menu para mirar un video y cobrar monedas (VideoDeMonedas). Pedido de Ivan el
// 10/10: redondo, arriba a la derecha, debajo del de las misiones (el portapapeles), copiado del
// globo como los otros de las esquinas, con la claqueta de la oferta de revivir en el naranja
// del video. Colgando de abajo lleva una etiqueta con lo que paga ("+240", dorada, y el boton
// respira) o, mientras espera el proximo, la cuenta atras ("9:41", en vidrio, y tocarlo lo hace
// temblar). Sin video para ofrecer (no cargo, o los videos estan apagados) el boton no existe,
// como todas las ofertas. Al cobrar, el arpegio de cobrar una mision y "¡+240 MONEDAS!" que sube
// desde el boton: el menu no muestra monedas, y asi se ve lo que entro.
//
// Nada de esto esta en la escena: se instala solo al cargar el menu, como CartelDeCobros, y
// toma la fuente, la pildora y los sonidos de VentanaMisiones y el molde del globo de
// SelectorIdioma.
public class BotonVideoDeMonedas : MonoBehaviour
{
    // Debajo de la fila de botones redondos de arriba a la derecha.
    public const float Separacion = 34f;
    public static readonly Vector2 TamanioEtiqueta = new Vector2(150f, 46f);
    public const float DuracionDelCobro = 1.4f;

    private static readonly int[] SemitonosFestejo = { -12, -8, -5, 0, 4, 7, 12 };
    private static readonly Color Vidrio = new Color(0.06f, 0.08f, 0.12f, 0.85f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Engancharse()
    {
        // Sin recargar el dominio al entrar en play, el static sigue suscripto: sin duplicar.
        SceneManager.sceneLoaded -= AlCargar;
        SceneManager.sceneLoaded += AlCargar;
    }

    private static void AlCargar(Scene escena, LoadSceneMode modo)
    {
        if (escena.buildIndex != TiendaMejoras.EscenaMenu) return;
        var go = new GameObject("BotonVideoDeMonedas");
        SceneManager.MoveGameObjectToScene(go, escena);
        go.AddComponent<BotonVideoDeMonedas>();
    }

    private VentanaMisiones misiones;
    private Button boton;
    private BotonJugoso jugoso;
    private Image icono;
    private Image fondoEtiqueta;
    private TMP_Text textoEtiqueta;
    private Texture2D texturaClaqueta;
    private Sprite spriteClaqueta;
    private bool esperandoVideo;
    private float proximaMirada;
    private TMP_Text cobro;
    private float cobroDesde = -1f;

    private void Start()
    {
        misiones = FindAnyObjectByType<VentanaMisiones>(FindObjectsInactive.Include);
        var selector = FindAnyObjectByType<SelectorIdioma>(FindObjectsInactive.Include);
        if (misiones == null || selector == null || selector.botonGlobo == null)
        {
            enabled = false;
            return;
        }
        Armar(selector);
        Refrescar();
    }

    private void OnDestroy()
    {
        if (spriteClaqueta != null) Destroy(spriteClaqueta);
        if (texturaClaqueta != null) Destroy(texturaClaqueta);
    }

    private void Armar(SelectorIdioma selector)
    {
        texturaClaqueta = TexturasUI.Claqueta(160);
        spriteClaqueta = Sprite.Create(texturaClaqueta, new Rect(0f, 0f, texturaClaqueta.width, texturaClaqueta.height),
                                       new Vector2(0.5f, 0.5f), 100f, 0u, SpriteMeshType.FullRect);

        var globo = (RectTransform)selector.botonGlobo.transform;
        // A la misma distancia del borde que el de las misiones, una fila mas abajo.
        boton = ConstructorUI.BotonDeEsquina(selector, "BotonVideoMonedas", globo.anchoredPosition.x, spriteClaqueta, Tocar);
        if (boton == null)
        {
            enabled = false;
            return;
        }
        var rt = (RectTransform)boton.transform;
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, globo.anchoredPosition.y - globo.rect.height - Separacion);
        jugoso = boton.GetComponent<BotonJugoso>();
        if (selector.iconoGlobo != null)
        {
            var copia = boton.transform.Find(ConstructorUI.Ruta(selector.iconoGlobo.transform, globo));
            if (copia != null) icono = copia.GetComponent<Image>();
        }

        // La etiqueta cuelga del borde de abajo, montada a la mitad.
        var etiqueta = ConstructorUI.Rect(rt, "Etiqueta", Vector2.zero, TamanioEtiqueta);
        etiqueta.anchorMin = etiqueta.anchorMax = new Vector2(0.5f, 0f);
        fondoEtiqueta = etiqueta.gameObject.AddComponent<Image>();
        fondoEtiqueta.sprite = misiones.pildora;
        ConstructorUI.RedondearPildora(fondoEtiqueta);
        fondoEtiqueta.raycastTarget = false;
        textoEtiqueta = ConstructorUI.Texto(etiqueta, "Texto", "", 34f, ConstructorUI.AmarilloTexto, Vector2.zero,
                                            TamanioEtiqueta, misiones.fuente);
    }

    private void Update()
    {
        if (boton == null) return;
        if (Time.unscaledTime >= proximaMirada) Refrescar();
        AnimarCobro();
    }

    // Cuatro veces por segundo alcanza para la cuenta atras, y preguntar si hay video no cruza a
    // Java (ProveedorAdMob.Listo sale de lo que ya aviso el puente).
    private void Refrescar()
    {
        proximaMirada = Time.unscaledTime + 0.25f;
        long faltan = VideoDeMonedas.Faltan;
        bool esperando = faltan > 0;
        // Hasta el nivel 5 no esta (Desbloqueo), salvo que ya se este mirando uno.
        bool existe = esperandoVideo || Desbloqueo.Todo && (esperando || VideoDeMonedas.HayVideo);
        if (boton.gameObject.activeSelf != existe) boton.gameObject.SetActive(existe);
        if (!existe) return;

        string texto;
        if (esperando)
        {
            texto = (faltan / 60) + ":" + (faltan % 60).ToString("00");
            fondoEtiqueta.color = Vidrio;
            textoEtiqueta.color = Color.white;
            if (icono != null) icono.color = new Color(1f, 1f, 1f, 0.45f);
        }
        else
        {
            texto = Textos.Formato("video_monedas_premio", FormatoNumeros.Compacto(VideoDeMonedas.PremioDeHoy));
            fondoEtiqueta.color = ConstructorUI.Amarillo;
            textoEtiqueta.color = ConstructorUI.AmarilloTexto;
            if (icono != null) icono.color = ConstructorUI.Naranja;
        }
        if (textoEtiqueta.text != texto) textoEtiqueta.text = texto;
        if (jugoso != null) jugoso.respirar = !esperando;
    }

    private void Tocar()
    {
        if (esperandoVideo) return;
        if (VideoDeMonedas.Faltan > 0)
        {
            if (jugoso != null) jugoso.Sacudir();
            return;
        }
        esperandoVideo = true;
        bool lanzado = ServicioAnuncios.Mostrar(LugarAnuncio.MonedasMenu, () =>
        {
            esperandoVideo = false;
            Festejar(VideoDeMonedas.Cobrar());
            Refrescar();
        }, () =>
        {
            // Cerrarlo antes no castiga: el boton sigue ahi, sin espera.
            esperandoVideo = false;
            Refrescar();
        });
        if (!lanzado) esperandoVideo = false;
        Refrescar();
    }

    private void Festejar(double premio)
    {
        for (int k = 0; k < SemitonosFestejo.Length; k++)
            Sonidos.Programar(misiones.nota, 0.05 * k, 0.8f, Sonidos.PitchDe(SemitonosFestejo[k]));
        if (misiones.sonidoFestejo != null) Sonidos.Tocar(misiones.sonidoFestejo, 0.7f);
        if (jugoso != null) jugoso.Golpe(1.35f, 0.45f);

        // "¡+240 MONEDAS!" sube desde el boton y se apaga.
        if (cobro == null)
        {
            var padre = (RectTransform)boton.transform.parent;
            cobro = ConstructorUI.Texto(padre, "CobroDelVideo", "", 50f, ConstructorUI.Amarillo, Vector2.zero,
                                        new Vector2(520f, 70f), misiones.fuente);
            if (misiones.materialContorno != null) cobro.fontSharedMaterial = misiones.materialContorno;
        }
        cobro.text = Textos.Formato("video_monedas_cobrado", FormatoNumeros.Compacto(premio));
        cobro.gameObject.SetActive(true);
        cobroDesde = Time.unscaledTime;
        AnimarCobro();
    }

    private void AnimarCobro()
    {
        if (cobro == null || cobroDesde < 0f) return;
        float t = Mathf.Clamp01((Time.unscaledTime - cobroDesde) / DuracionDelCobro);
        var rtBoton = (RectTransform)boton.transform;
        var rt = cobro.rectTransform;
        rt.anchorMin = rt.anchorMax = rtBoton.anchorMin;
        rt.pivot = new Vector2(1f, 0.5f);
        // Sale a la izquierda del boton, a su altura, y sube.
        rt.anchoredPosition = rtBoton.anchoredPosition + new Vector2(-rtBoton.rect.width - 10f, -rtBoton.rect.height * 0.5f + 60f * t);
        rt.localScale = Vector3.one * CurvasUI.SalidaAtras(Mathf.Clamp01(t / 0.25f));
        var color = cobro.color;
        color.a = t < 0.7f ? 1f : 1f - (t - 0.7f) / 0.3f;
        cobro.color = color;
        if (t >= 1f)
        {
            cobro.gameObject.SetActive(false);
            cobroDesde = -1f;
        }
    }
}
