using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// Medidor de balance para probar las mejoras jugando: muestra lo que se aplico
// (dano por bala, cadencia, vida, botin), la cadencia medida de verdad mientras
// se dispara, el escalado de los zombis de la oleada o del nivel del modo libre
// y las monedas por zombi reales contra las esperadas. Se prende y se apaga con
// F1 o con un toque de tres dedos.
//
// Se instala solo en las escenas con arma, sin tocar escenas ni prefabs, y solo en
// el editor y en builds de desarrollo: la APK de ConstructorAndroid no es de
// desarrollo, asi que un jugador nunca lo ve. Solo lee: no toca el estado del juego.
public class MedidorBalance : MonoBehaviour
{
    private const float IntervaloMuestras = 0.25f;
    private const float VentanaMuestras = 2f;
    private const float TramoMinimoMedido = 0.5f;   // con menos, un frame de mas o de menos pesa demasiado
    private const float IntervaloTexto = 0.5f;

    // Sobrevive al cambio de escena a proposito: prendido en una partida, la
    // siguiente arranca con el medidor a la vista.
    private static bool visible;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetearEstadoCompartido()
    {
        SceneManager.sceneLoaded -= AlCargar;
        visible = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Instalar()
    {
        if (!Application.isEditor && !Debug.isDebugBuild) return;

        SceneManager.sceneLoaded -= AlCargar;
        SceneManager.sceneLoaded += AlCargar;

        // La escena con la que arranca el juego puede haber cargado antes de
        // engancharse; si no, AlCargar no crea un segundo medidor.
        AlCargar(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private static void AlCargar(Scene escena, LoadSceneMode modo)
    {
        if (FindFirstObjectByType<GunController>() == null) return;
        if (FindFirstObjectByType<MedidorBalance>() != null) return;
        new GameObject("MedidorBalance").AddComponent<MedidorBalance>();
    }

    private struct Muestra
    {
        public float tiempo;
        public int tiros;
    }

    private GameObject panel;
    private TextMeshProUGUI texto;

    private GunController arma;
    private PlayerHealth vida;
    private WaveManager oleadas;
    private GeneradorZombis libre;

    // Los contadores de EnemyController son de toda la sesion: se mide desde que
    // aparecio el medidor en esta escena.
    private int muertesAlEmpezar;
    private double esperadasAlEmpezar;
    private double soltadasAlEmpezar;

    private readonly Queue<Muestra> muestras = new Queue<Muestra>(16);
    private Muestra ultimaMuestra;
    private float proximaMuestra;

    private int frames;
    private float acumuladoFps;
    private int fps;

    private readonly StringBuilder armado = new StringBuilder(512);

    private void Awake()
    {
        ArmarUI();

        arma = FindFirstObjectByType<GunController>();
        vida = PlayerHealth.instance;
        oleadas = FindFirstObjectByType<WaveManager>();
        libre = FindFirstObjectByType<GeneradorZombis>();

        muertesAlEmpezar = EnemyController.MuertesConBotin;
        esperadasAlEmpezar = EnemyController.MonedasEsperadas;
        soltadasAlEmpezar = EnemyController.MonedasSoltadas;

        panel.SetActive(visible);
        if (visible) Reescribir();
    }

    // Todo por codigo para no depender de prefabs ni escenas: un canvas propio por
    // encima de todo (la pausa esta en 10) y sin GraphicRaycaster, que no tape toques.
    private void ArmarUI()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        var escalador = gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
        escalador.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escalador.referenceResolution = new Vector2(1920f, 1080f);
        escalador.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        escalador.matchWidthOrHeight = 0.5f;

        panel = new GameObject("Panel", typeof(RectTransform));
        var rectPanel = (RectTransform)panel.transform;
        rectPanel.SetParent(transform, false);
        rectPanel.anchorMin = new Vector2(0f, 0.5f);
        rectPanel.anchorMax = new Vector2(0f, 0.5f);
        rectPanel.pivot = new Vector2(0f, 0.5f);
        rectPanel.anchoredPosition = new Vector2(20f, 0f);
        rectPanel.sizeDelta = new Vector2(780f, 440f);

        var fondo = panel.AddComponent<UnityEngine.UI.Image>();
        fondo.color = new Color(0f, 0f, 0f, 0.65f);
        fondo.raycastTarget = false;

        var objetoTexto = new GameObject("Texto", typeof(RectTransform));
        var rectTexto = (RectTransform)objetoTexto.transform;
        rectTexto.SetParent(rectPanel, false);
        rectTexto.anchorMin = Vector2.zero;
        rectTexto.anchorMax = Vector2.one;
        rectTexto.offsetMin = new Vector2(16f, 16f);
        rectTexto.offsetMax = new Vector2(-16f, -16f);

        texto = objetoTexto.AddComponent<TextMeshProUGUI>();
        texto.fontSize = 26f;
        texto.color = Color.white;
        texto.alignment = TextAlignmentOptions.TopLeft;
        texto.textWrappingMode = TextWrappingModes.NoWrap;
        texto.richText = false;
        texto.raycastTarget = false;
    }

    private void Update()
    {
        if (!MenuPausa.Pausado && PidioAlternar())
        {
            visible = !visible;
            panel.SetActive(visible);
            if (visible) Reescribir();
        }

        Muestrear();

        frames++;
        acumuladoFps += Time.unscaledDeltaTime;
        if (acumuladoFps < IntervaloTexto) return;

        fps = Mathf.RoundToInt(frames / acumuladoFps);
        frames = 0;
        acumuladoFps = 0f;
        if (visible) Reescribir();
    }

    private static bool PidioAlternar()
    {
        if (Input.GetKeyDown(KeyCode.F1)) return true;
        if (Input.touchCount != 3) return false;

        for (int i = 0; i < 3; i++)
        {
            if (Input.GetTouch(i).phase == TouchPhase.Began) return true;
        }
        return false;
    }

    // Tiros disparados contra Time.time, en una ventana corta: mide la cadencia que
    // sale de verdad (con el acumulador, los topes y los FPS) y no la calculada.
    // Con tiempo escalado, para que la pausa no cuente como tiempo sin disparar.
    private void Muestrear()
    {
        if (arma == null || !arma.isFiring)
        {
            muestras.Clear();
            return;
        }

        if (muestras.Count > 0 && Time.time < proximaMuestra) return;
        proximaMuestra = Time.time + IntervaloMuestras;

        ultimaMuestra = new Muestra { tiempo = Time.time, tiros = arma.TirosDisparados };
        muestras.Enqueue(ultimaMuestra);
        while (muestras.Count > 1 && ultimaMuestra.tiempo - muestras.Peek().tiempo > VentanaMuestras)
        {
            muestras.Dequeue();
        }
    }

    private string TirosMedidos()
    {
        if (muestras.Count < 2) return "—";

        Muestra primera = muestras.Peek();
        float tramo = ultimaMuestra.tiempo - primera.tiempo;
        if (tramo < TramoMinimoMedido) return "—";
        return Numero((ultimaMuestra.tiros - primera.tiros) / tramo, "0.0");
    }

    private void Reescribir()
    {
        if (vida == null) vida = PlayerHealth.instance;

        CatalogoMejoras catalogo = CatalogoMejoras.Instancia;
        Mejora mejoraDano = catalogo != null ? catalogo.danoBala : null;
        Mejora mejoraBotin = catalogo != null ? catalogo.botin : null;
        Mejora mejoraIman = catalogo != null ? catalogo.iman : null;

        armado.Length = 0;
        armado.Append("MEDIDOR (F1)\n");

        if (arma != null)
        {
            armado.Append("Daño/bala ").Append(Numero(arma.DanoPorBala, "0.0"))
                .Append(" (nivel ").Append(NivelDe(mejoraDano)).Append(")\n");
            armado.Append("Tiros/s ").Append(TirosMedidos()).Append(" medido | ")
                .Append(Numero(arma.TirosPorSegundo, "0.0")).Append(" esperado (×")
                .Append(Numero(arma.MultiplicadorCadencia, "0.0")).Append(" caja)\n");
        }
        else
        {
            armado.Append("Sin arma\n");
        }

        if (vida != null)
        {
            armado.Append("Vida ").Append(vida.health).Append('/').Append(vida.maxHealth)
                .Append(" · cura ").Append(vida.CuraPorCaja).Append('\n');
        }
        else
        {
            armado.Append("Sin jugador\n");
        }

        armado.Append("Botín ×").Append(Numero(CatalogoMejoras.MultiplicadorBotin, "0.0"))
            .Append(" (nivel ").Append(NivelDe(mejoraBotin)).Append(")\n");
        armado.Append("Imán ").Append(Numero(Moneda.RadioImanDeLaPartida >= 0f ? Moneda.RadioImanDeLaPartida : CatalogoMejoras.RadioIman, "0.0"))
            .Append(" m (nivel ").Append(NivelDe(mejoraIman)).Append(")\n");

        if (oleadas != null)
        {
            armado.Append("Oleada ").Append(oleadas.OleadaActual)
                .Append(" · vida ×").Append(Numero(oleadas.MultiplicadorVidaActual, "0.00"))
                .Append(" · daño ×").Append(Numero(oleadas.MultiplicadorDanoActual, "0.00"))
                .Append(" · moneda ×").Append(Numero(oleadas.MultiplicadorMonedasActual, "0.000")).Append('\n');
        }
        else if (libre != null)
        {
            armado.Append("Modo libre · nivel ").Append(libre.NivelActual)
                .Append(" · vida ×").Append(Numero(libre.MultiplicadorVidaActual, "0.00"))
                .Append(" · daño ×").Append(Numero(libre.MultiplicadorDanoActual, "0.00"))
                .Append(" · moneda ×").Append(Numero(libre.MultiplicadorMonedasActual, "0.000")).Append('\n');
        }
        else
        {
            armado.Append("Sin generador de zombis\n");
        }

        int muertes = EnemyController.MuertesConBotin - muertesAlEmpezar;
        if (muertes > 0)
        {
            armado.Append("Monedas/zombi ")
                .Append(Numero((EnemyController.MonedasSoltadas - soltadasAlEmpezar) / muertes, "0.00")).Append(" real | ")
                .Append(Numero((EnemyController.MonedasEsperadas - esperadasAlEmpezar) / muertes, "0.00")).Append(" esperado (");
        }
        else
        {
            armado.Append("Monedas/zombi — real | — esperado (");
        }
        armado.Append(muertes).Append(" muertes)\n");

        armado.Append("FPS ").Append(fps);

        texto.SetText(armado);
    }

    private static int NivelDe(Mejora mejora)
    {
        return mejora != null ? Progreso.Nivel(mejora.id) : 0;
    }

    // Con coma decimal, como el resto de los numeros del juego.
    private static string Numero(double valor, string formato)
    {
        return valor.ToString(formato, CultureInfo.InvariantCulture).Replace('.', ',');
    }
}
