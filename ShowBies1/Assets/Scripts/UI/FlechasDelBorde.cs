using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Flechas en el borde de la pantalla hacia lo que importa y no se ve (revision del 9/10,
// mejora 7), un solo sistema para tres cosas:
// - las cajas, de su color y con un punto detras, latiendo mas rapido cuando estan por vencer:
//   hasta aca casi todas nacian y vencian sin que nadie las viera;
// - los ultimos zombis de la oleada, cuando faltan UltimosZombis o menos: buscar al ultimo
//   corredor en la ciudad era tiempo muerto;
// - el jefe, mas grande y violeta como su piel, en los dos modos;
// - el zombi del tesoro (ZombiDelTesoro), dorada, latiendo mas rapido cuando esta por irse.
// La flecha va en el borde del area segura, sobre la recta del centro de la pantalla a lo que
// señala, y apunta hacia eso (EnElBorde). Lo que se ve no lleva flecha. Si cae encima del HUD
// (los textos de arriba a la izquierda, la vida, los joysticks y los botones del telefono, la
// barra del jefe) se corre por el borde hasta salir y vuelve a apuntar desde ahi (FueraDelHud):
// en la primera foto tapaban MONEDAS y la vida. Lo que ocupa el HUD se mide cada medio segundo
// de los graficos que se ven cerca de los bordes (de un texto, lo que ocupan sus letras, que
// la caja es mucho mas ancha), asi vale con el HUD de la PC y el del telefono, en cualquier
// proporcion. Cada flecha esquiva tambien las que ya se pusieron: dos que se corrian al mismo
// lugar quedaban una encima de la otra.
//
// No esta en las escenas: se instala sola en las partidas (el libre y las oleadas, no el
// tutorial, que pone las cajas al lado del jugador) con un canvas propio por debajo de la
// pausa, raiz de la escena: la derrota lo apaga con el resto del HUD. Con el juego congelado
// (la pausa, el revivir) o el jugador muerto no hay flechas.
public class FlechasDelBorde : MonoBehaviour
{
    public const int OrdenDelCanvas = 8;          // la pausa va en 10
    public const float Margen = 64f;              // del borde del area segura, en unidades de un canvas de 1080 de alto
    public const float Lado = 54f;
    public const float Largo = 1.25f;              // del lado, hacia donde apunta
    public const float Ancho = 0.75f;
    public const float TamanioJefe = 1.6f;
    public const float TamanioTesoro = 1.3f;
    public const int UltimosZombis = 3;
    public const float PorVencer = 0.3f;          // de la vida de la caja: desde ahi late rapido
    public const float DentroDeLaPantalla = 24f;  // pixeles: algo pegado al borde no se ve, y lleva flecha

    public static readonly Color ColorZombi = new Color(0.72f, 1f, 0.48f);   // el verde de la piel de los zombis
    public static readonly Color ColorJefe = new Color(0.66f, 0.36f, 1f);    // violeta, como su piel

    public enum Que { Caja, Zombi, Jefe, Tesoro }

    // Lo que se dibujo este cuadro, para el banco (PruebaFlechas).
    public struct Vista
    {
        public Que que;
        public Object objetivo;
        public Vector2 punto;      // pixeles de pantalla
        public float angulo;       // grados, 0 a la derecha
        public float tamanio;
        public Color color;
    }

    private static readonly List<Vista> vistas = new List<Vista>();
    public static IReadOnlyList<Vista> Vistas { get { return vistas; } }

    private struct Flecha
    {
        public RectTransform raiz;
        public Image punta;
        public Image punto;
    }

    private readonly List<Flecha> flechas = new List<Flecha>();
    private readonly List<Rect> hud = new List<Rect>();
    private readonly List<Rect> ocupado = new List<Rect>();
    private float hudMedidoEn = -10f;
    private const float CadaCuantoElHud = 0.5f;
    private readonly List<EnemyController> vivos = new List<EnemyController>();
    private Canvas canvas;
    private Sprite triangulo, circulo;
    private WaveManager oleadas;
    private int usadas;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetearEstadoCompartido()
    {
        vistas.Clear();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Engancharse()
    {
        // Sin recargar el dominio al entrar en play, el static sigue suscripto: sin duplicar.
        SceneManager.sceneLoaded -= AlCargar;
        SceneManager.sceneLoaded += AlCargar;
    }

    private static void AlCargar(Scene escena, LoadSceneMode modo)
    {
        // Sin mirar el modo: la escena abierta en el editor al darle play no llega como Single,
        // y la derrota, que si se carga aditiva encima de la partida, es otra escena.
        if (escena.buildIndex != TiendaMejoras.EscenaModoLibre && escena.buildIndex != TiendaMejoras.EscenaOleadas) return;
        var go = new GameObject("FlechasDelBorde", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        SceneManager.MoveGameObjectToScene(go, escena);
        go.AddComponent<FlechasDelBorde>();
    }

    // Donde va la flecha de algo que esta en 'objetivo' (pixeles de pantalla): en el borde de
    // 'area' achicada en 'margen', sobre la recta que va del centro del area al objetivo, y el
    // angulo hacia el (grados, 0 a la derecha, 90 arriba). Falso si el objetivo esta adentro
    // de 'visible': se ve y no lleva flecha.
    public static bool EnElBorde(Vector2 objetivo, Rect visible, Rect area, float margen, out Vector2 punto, out float angulo)
    {
        punto = objetivo;
        angulo = 0f;
        if (visible.Contains(objetivo)) return false;
        Vector2 centro = area.center;
        Vector2 d = objetivo - centro;
        if (d.sqrMagnitude < 1e-6f) return false;
        float medioAncho = Mathf.Max(1f, area.width * 0.5f - margen);
        float medioAlto = Mathf.Max(1f, area.height * 0.5f - margen);
        float porAncho = Mathf.Abs(d.x) > 1e-6f ? medioAncho / Mathf.Abs(d.x) : float.MaxValue;
        float porAlto = Mathf.Abs(d.y) > 1e-6f ? medioAlto / Mathf.Abs(d.y) : float.MaxValue;
        punto = centro + d * Mathf.Min(porAncho, porAlto);
        angulo = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
        return true;
    }

    // Corre 'punto' (en el borde de 'area' achicada en 'margen') por ese borde hasta que no
    // caiga encima de ningun rectangulo de 'hud', agrandados en 'radio' (media flecha y aire).
    public static Vector2 FueraDelHud(Vector2 punto, Rect area, float margen, List<Rect> hud, float radio)
    {
        var borde = Rect.MinMaxRect(area.xMin + margen, area.yMin + margen, area.xMax - margen, area.yMax - margen);
        bool acostado = Mathf.Abs(punto.y - borde.yMax) < 1f || Mathf.Abs(punto.y - borde.yMin) < 1f;
        for (int vuelta = 0; vuelta < 4; vuelta++)
        {
            bool corrio = false;
            for (int i = 0; i < hud.Count; i++)
            {
                var r = Rect.MinMaxRect(hud[i].xMin - radio, hud[i].yMin - radio, hud[i].xMax + radio, hud[i].yMax + radio);
                if (!r.Contains(punto)) continue;
                if (acostado)
                {
                    float x = punto.x - r.xMin < r.xMax - punto.x ? r.xMin : r.xMax;
                    if (x < borde.xMin) x = r.xMax;
                    if (x > borde.xMax) x = r.xMin;
                    punto.x = Mathf.Clamp(x, borde.xMin, borde.xMax);
                }
                else
                {
                    float y = punto.y - r.yMin < r.yMax - punto.y ? r.yMin : r.yMax;
                    if (y < borde.yMin) y = r.yMax;
                    if (y > borde.yMax) y = r.yMin;
                    punto.y = Mathf.Clamp(y, borde.yMin, borde.yMax);
                }
                corrio = true;
            }
            if (!corrio) break;
        }
        return punto;
    }

    // Lo que ocupa el HUD cerca de los bordes, en pixeles: los graficos que se ven de los
    // canvas overlay que no son este, ni enormes (un fondo) ni lejos de los bordes.
    private void MedirElHud()
    {
        hud.Clear();
        float w = Screen.width, h = Screen.height, cerca = 0.3f * Mathf.Min(w, h);
        var esquinas = new Vector3[4];
        foreach (var g in FindObjectsByType<Graphic>(FindObjectsSortMode.None))
        {
            if (!g.isActiveAndEnabled || g.canvas == null) continue;
            var raiz = g.canvas.rootCanvas;
            if (raiz == canvas || raiz.renderMode != RenderMode.ScreenSpaceOverlay) continue;
            if (g.color.a < 0.05f || g.canvasRenderer.GetInheritedAlpha() < 0.05f) continue;
            Rect r;
            var texto = g as TMP_Text;
            if (texto != null)
            {
                // Lo que ocupan las letras, no la caja del texto.
                if (string.IsNullOrEmpty(texto.text)) continue;
                var b = texto.textBounds;
                Vector3 a = texto.rectTransform.TransformPoint(b.min), z = texto.rectTransform.TransformPoint(b.max);
                r = Rect.MinMaxRect(Mathf.Min(a.x, z.x), Mathf.Min(a.y, z.y), Mathf.Max(a.x, z.x), Mathf.Max(a.y, z.y));
            }
            else
            {
                g.rectTransform.GetWorldCorners(esquinas);
                r = Rect.MinMaxRect(esquinas[0].x, esquinas[0].y, esquinas[2].x, esquinas[2].y);
            }
            if (r.width < 4f || r.height < 4f || r.width > 0.45f * w || r.height > 0.45f * h) continue;
            if (r.xMin > cerca && r.yMin > cerca && r.xMax < w - cerca && r.yMax < h - cerca) continue;
            hud.Add(r);
        }
    }

    private void Start()
    {
        canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = OrdenDelCanvas;
        var escala = GetComponent<CanvasScaler>();
        escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escala.referenceResolution = new Vector2(1920f, 1080f);
        escala.matchWidthOrHeight = 1f;
        var t = TexturasUI.Play(64);
        triangulo = Sprite.Create(t, new Rect(0f, 0f, t.width, t.height), new Vector2(0.5f, 0.5f));
        var c = TexturasUI.Circulo(64);
        circulo = Sprite.Create(c, new Rect(0f, 0f, c.width, c.height), new Vector2(0.5f, 0.5f));
        oleadas = FindAnyObjectByType<WaveManager>();
    }

    private void OnDestroy()
    {
        vistas.Clear();
        if (triangulo != null) { Destroy(triangulo.texture); Destroy(triangulo); }
        if (circulo != null) { Destroy(circulo.texture); Destroy(circulo); }
    }

    // Despues de que la camara siguio al jugador (CamaraJugador va en Update).
    private void LateUpdate()
    {
        usadas = 0;
        vistas.Clear();
        var camara = Camera.main;
        var jugador = PlayerHealth.instance;
        bool apagado = camara == null || canvas == null || MenuPausa.JuegoCongelado || jugador == null || jugador.EstaMuerto;
        if (!apagado)
        {
            float t = Time.unscaledTime;
            if (t - hudMedidoEn > CadaCuantoElHud)
            {
                hudMedidoEn = t;
                MedirElHud();
            }
            ocupado.Clear();
            ocupado.AddRange(hud);
            var cajas = PickupCaducidad.Puestas;
            for (int i = 0; i < cajas.Count; i++)
            {
                var caja = cajas[i];
                if (caja == null) continue;
                float pulso = caja.Restante01 < PorVencer ? 9f : 3f;
                Poner(camara, Que.Caja, caja, caja.transform.position + Vector3.up * 0.5f,
                      AspectoDeCaja.ColorDe(caja, Color.white), 1f, pulso, t);
            }

            if (oleadas != null && !oleadas.EnDescanso)
            {
                int faltan = oleadas.FaltanDeLaOleada(vivos);
                if (faltan > 0 && faltan <= UltimosZombis)
                {
                    for (int i = 0; i < vivos.Count; i++)
                        if (vivos[i] != null && !vivos[i].EsJefe && vivos[i].GetComponent<ZombiDelTesoro>() == null)
                            Poner(camara, Que.Zombi, vivos[i], vivos[i].transform.position + Vector3.up, ColorZombi, 1f, 2.5f, t);
                }
            }

            var tesoros = ZombiDelTesoro.Vivos;
            for (int i = 0; i < tesoros.Count; i++)
            {
                var tesoro = tesoros[i];
                if (tesoro == null || tesoro.Zombi == null || !tesoro.Zombi.Vivo) continue;
                Poner(camara, Que.Tesoro, tesoro.Zombi, tesoro.transform.position + Vector3.up * 0.8f, Efectos.ColorTesoro,
                      TamanioTesoro, tesoro.Restante01 < ZombiDelTesoro.PorIrse ? 9f : 4f, t);
            }

            var jefes = EnemyController.Jefes;
            for (int i = 0; i < jefes.Count; i++)
                if (jefes[i] != null && jefes[i].Vivo)
                    Poner(camara, Que.Jefe, jefes[i], jefes[i].transform.position + Vector3.up * 1.5f, ColorJefe, TamanioJefe, 2f, t);
        }
        for (int i = usadas; i < flechas.Count; i++)
            if (flechas[i].raiz.gameObject.activeSelf) flechas[i].raiz.gameObject.SetActive(false);
    }

    private void Poner(Camera camara, Que que, Object objetivo, Vector3 mundo, Color color, float tamanio, float pulso, float t)
    {
        Vector3 p = camara.WorldToScreenPoint(mundo);
        // Detras de la camara la proyeccion sale espejada (con la camara de arriba no pasa).
        if (p.z < 0f) p = new Vector3(Screen.width - p.x, Screen.height - p.y, -p.z);
        var visible = new Rect(DentroDeLaPantalla, DentroDeLaPantalla,
                               Screen.width - 2f * DentroDeLaPantalla, Screen.height - 2f * DentroDeLaPantalla);
        Vector2 punto;
        float angulo;
        Vector2 objetivoEnPantalla = new Vector2(p.x, p.y);
        float margen = Margen * canvas.scaleFactor;
        if (!EnElBorde(objetivoEnPantalla, visible, Screen.safeArea, margen, out punto, out angulo)) return;
        float radio = 0.5f * Lado * Largo * tamanio * canvas.scaleFactor + 6f;
        Vector2 corrido = FueraDelHud(punto, Screen.safeArea, margen, ocupado, radio);
        if ((corrido - punto).sqrMagnitude > 0.25f)
        {
            punto = corrido;
            Vector2 hacia = objetivoEnPantalla - punto;
            angulo = Mathf.Atan2(hacia.y, hacia.x) * Mathf.Rad2Deg;
        }

        var f = Tomar();
        f.raiz.anchoredPosition = punto / canvas.scaleFactor;
        f.raiz.localEulerAngles = new Vector3(0f, 0f, angulo);
        // Alargada: el triangulo de "play" es casi equilatero, y girado (a 151 grados, por
        // ejemplo) le queda un lado acostado arriba y se lee como que apunta para abajo.
        f.raiz.sizeDelta = new Vector2(Lado * Largo, Lado * Ancho) * tamanio;
        f.raiz.localScale = Vector3.one * (1f + 0.14f * Mathf.Sin(t * pulso * 2f * Mathf.PI));
        f.punta.color = color;
        bool conPunto = que == Que.Caja;
        if (f.punto.gameObject.activeSelf != conPunto) f.punto.gameObject.SetActive(conPunto);
        if (conPunto) f.punto.color = new Color(color.r, color.g, color.b, 0.55f);
        vistas.Add(new Vista { que = que, objetivo = objetivo, punto = punto, angulo = angulo, tamanio = tamanio, color = color });
        // La que sigue no se pone encima de esta.
        float media = radio - 6f;
        ocupado.Add(Rect.MinMaxRect(punto.x - media, punto.y - media, punto.x + media, punto.y + media));
    }

    private Flecha Tomar()
    {
        if (usadas < flechas.Count)
        {
            var usada = flechas[usadas++];
            if (!usada.raiz.gameObject.activeSelf) usada.raiz.gameObject.SetActive(true);
            return usada;
        }

        var go = new GameObject("Flecha", typeof(RectTransform));
        var raiz = (RectTransform)go.transform;
        raiz.SetParent(transform, false);
        raiz.anchorMin = raiz.anchorMax = Vector2.zero;
        raiz.pivot = new Vector2(0.5f, 0.5f);
        // El punto detras de la punta (las cajas): del color de la caja, un poco transparente.
        var punto = ConstructorUI.Imagen(raiz, "Punto", Vector2.zero, Vector2.one, circulo, Color.white);
        punto.rectTransform.anchorMin = new Vector2(-0.45f, 0.2f);
        punto.rectTransform.anchorMax = new Vector2(0.15f, 0.8f);
        punto.rectTransform.offsetMin = punto.rectTransform.offsetMax = Vector2.zero;
        var punta = ConstructorUI.Imagen(raiz, "Punta", Vector2.zero, Vector2.one, triangulo, Color.white);
        punta.preserveAspect = false;
        punta.rectTransform.anchorMin = Vector2.zero;
        punta.rectTransform.anchorMax = Vector2.one;
        punta.rectTransform.offsetMin = punta.rectTransform.offsetMax = Vector2.zero;
        var flecha = new Flecha { raiz = raiz, punta = punta, punto = punto };
        flechas.Add(flecha);
        usadas++;
        return flecha;
    }
}
