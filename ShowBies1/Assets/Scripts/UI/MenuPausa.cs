using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Pausa de las escenas de juego. Se abre con el boton de pausa del HUD (solo en
// movil), con Escape en PC y con el boton atras de Android, que Unity entrega
// como Escape. Tambien se abre sola cuando la app pierde el foco (una llamada,
// la cortina de notificaciones, alt-tab), para que la partida no siga corriendo
// sin nadie jugando.
//
// Pausar es Time.timeScale = 0, que congela la fisica, las corrutinas con
// WaitForSeconds y todo lo que cuenta con Time.time o Time.deltaTime. El input
// no se congela: PlayerController y PlayerJS miran Pausado antes de leerlo.
public class MenuPausa : MonoBehaviour
{
    public GameObject panel;

    // Donde van, en la confirmacion de REINICIAR, el aviso y los dos botones (el titulo queda
    // donde esta el de la pausa). REINICIAR va lejos de SEGUIR JUGANDO.
    public float alturaAviso = 125f;
    public float alturaSeguir = -10f;
    public float alturaReiniciar = -230f;

    // La confirmacion de REINICIAR: una copia del panel con el titulo, CONTINUAR (que pasa a
    // SEGUIR JUGANDO) y REINICIAR. Se arma la primera vez que hace falta.
    private GameObject panelReiniciar;
    private TMP_Text avisoReiniciar;
    private int oleadaQueSePierde;

    // El boton de pausa del telefono va arriba al centro, encima de la primera linea del cartel
    // del capitulo ("CAPITULO 2" quedaba tapada en todas las proporciones: superauditoria del
    // 29/9). Mientras el cartel esta en pantalla el boton se desvanece, y se puede tocar igual.
    public float alfaConElCartel = 0.25f;
    private CanvasGroup grupoDelBoton;
    private CapitulosDeEscenario capitulos;

    public static bool Pausado { get; private set; }

    public bool ConfirmandoReiniciar
    {
        get { return panelReiniciar != null && panelReiniciar.activeSelf; }
    }

    // Nadie esta jugando: el menu de pausa, la oferta de revivir abierta (que deja el
    // timeScale en 0 con el jugador muerto) o la derrota encima de la partida (que la
    // deja andar: el nombre es de cuando todo esto congelaba). Lo miran los que leen
    // input y la pausa de impacto de Efectos, que si no devolvia el timeScale a 1
    // detras del HAS MUERTO.
    public static bool JuegoCongelado
    {
        get { return Pausado || OtroLoCongela; }
    }

    // Pausar encima de la oferta de revivir o de la derrota solo puede romper el
    // timeScale: al reanudar volveria a 1 con el jugador muerto.
    private static bool OtroLoCongela
    {
        get { return OfertaDeRevivir.Activa || DerrotaEnLaPartida.Activa; }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetearEstadoCompartido()
    {
        Pausado = false;
    }

    private void Start()
    {
        var boton = transform.Find("AreaSegura/BotonPausa");
        if (boton != null)
        {
            grupoDelBoton = boton.GetComponent<CanvasGroup>();
            if (grupoDelBoton == null) grupoDelBoton = boton.gameObject.AddComponent<CanvasGroup>();
        }
        // Solo las oleadas tienen capitulos.
        capitulos = FindAnyObjectByType<CapitulosDeEscenario>();
    }

    private void Update()
    {
        if (grupoDelBoton != null)
        {
            float alfa = capitulos != null && capitulos.CartelEnPantalla ? alfaConElCartel : 1f;
            grupoDelBoton.alpha = Mathf.MoveTowards(grupoDelBoton.alpha, alfa, Mathf.Min(Time.unscaledDeltaTime, 0.1f) * 4f);
        }

        // Con la oferta de revivir o la derrota en pantalla el juego ya esta congelado y
        // el jugador, muerto. Escape en la derrota es de MenuPerdiste (vuelve al menu).
        if (OtroLoCongela) return;
        if (!Input.GetKeyDown(KeyCode.Escape)) return;

        // Con la confirmacion abierta, el atras vuelve a la pausa, sin reanudar ni reiniciar.
        if (ConfirmandoReiniciar) CerrarConfirmacion();
        else if (Pausado) Reanudar();
        else Pausar();
    }

    // En el editor no: pierde el foco con cada click en otra ventana.
    private void OnApplicationFocus(bool tieneFoco)
    {
        if (!tieneFoco && !Application.isEditor) Pausar();
    }

    private void OnApplicationPause(bool enPausa)
    {
        if (enPausa && !Application.isEditor) Pausar();
    }

    public void Pausar()
    {
        // Tambien cuando se pierde el foco: con la derrota encima, pausar dejaria el
        // audio en pausa y un panel invisible (su canvas esta apagado).
        if (OtroLoCongela) return;
        if (Pausado) return;

        Pausado = true;
        Time.timeScale = 0f;
        AudioListener.pause = true;
        panel.SetActive(true);

        // Pausar tambien pasa cuando la app pierde el foco, y despues Android la
        // puede matar sin avisar: es el ultimo momento seguro para guardar.
        Progreso.Guardar();
    }

    public void Reanudar()
    {
        if (!Pausado) return;

        Restaurar();
        panel.SetActive(false);
        if (panelReiniciar != null) panelReiniciar.SetActive(false);
    }

    // El boton REINICIAR de la pausa. En las oleadas, pasada la 1, pregunta antes: reiniciar
    // borra la partida guardada (WaveManager.OlvidarPartidaSiEsOleadas), y rehacerla desde la 1
    // son 13 minutos hasta la 30 y 22 hasta la 40. Y un toque sin querer era facil: el halo de
    // REINICIAR se llevaba el borde de abajo de CONTINUAR (superauditoria del 29/9).
    public void Reiniciar()
    {
        int oleada = WaveManager.OleadaQueSePierdeAlReiniciar;
        if (oleada > 0 && AbrirConfirmacion(oleada)) return;
        ReiniciarYa();
    }

    // La R de RestartScene: lo mismo, con la pausa abierta para preguntar.
    public void PedirReiniciar()
    {
        if (ConfirmandoReiniciar) return;
        int oleada = WaveManager.OleadaQueSePierdeAlReiniciar;
        if (oleada > 0)
        {
            Pausar();
            if (Pausado && AbrirConfirmacion(oleada)) return;
        }
        ReiniciarYa();
    }

    public void ReiniciarYa()
    {
        Restaurar();
        WaveManager.OlvidarPartidaSiEsOleadas();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // Devuelve si la abrio: si no se pudo armar, quien la pide reinicia como antes.
    private bool AbrirConfirmacion(int oleada)
    {
        if (panelReiniciar == null) ArmarConfirmacion();
        if (panelReiniciar == null) return false;
        oleadaQueSePierde = oleada;
        panel.SetActive(false);
        panelReiniciar.SetActive(true);
        // Despues de prenderla: el aviso es una copia del titulo, y su texto no lo escribe
        // nadie mas.
        if (avisoReiniciar != null) avisoReiniciar.text = Textos.Formato("reiniciar_aviso", oleadaQueSePierde);
        return true;
    }

    public void CerrarConfirmacion()
    {
        if (panelReiniciar != null) panelReiniciar.SetActive(false);
        if (Pausado) panel.SetActive(true);
    }

    private void SeguirJugando()
    {
        if (panelReiniciar != null) panelReiniciar.SetActive(false);
        Reanudar();
    }

    private void ArmarConfirmacion()
    {
        if (panel == null) return;
        var titulo = panel.transform.Find("Titulo");
        var continuar = panel.transform.Find("BotonContinuar");
        var reiniciar = panel.transform.Find("BotonReiniciar");
        if (titulo == null || continuar == null || reiniciar == null) return;

        panelReiniciar = Instantiate(panel, panel.transform.parent);
        panelReiniciar.name = "PanelReiniciar";
        panelReiniciar.SetActive(false);
        // Justo encima del panel de la pausa: debajo de lo que vaya despues en el canvas.
        panelReiniciar.transform.SetSiblingIndex(panel.transform.GetSiblingIndex() + 1);
        var copiaTitulo = panelReiniciar.transform.Find("Titulo");
        var seguir = (RectTransform)panelReiniciar.transform.Find("BotonContinuar");
        var reiniciarSi = (RectTransform)panelReiniciar.transform.Find("BotonReiniciar");
        // Lo demas del panel (MENU PRINCIPAL, los volumenes) no va.
        foreach (Transform hijo in panelReiniciar.transform)
            if (hijo != copiaTitulo && hijo != seguir && hijo != reiniciarSi) Destroy(hijo.gameObject);

        // Los ids se escriben enteros (x.id = "...";) para que la prueba de idiomas los encuentre.
        var traducidoTitulo = copiaTitulo.GetComponent<TextoTraducido>();
        if (traducidoTitulo != null) traducidoTitulo.id = "reiniciar_titulo";
        // En un renglon: el del titulo de la pausa mide 900 y "¿EMPEZAR DE CERO?" se partia en
        // dos, con el segundo encima del aviso. Si no entra, se achica.
        var textoTitulo = copiaTitulo.GetComponent<TMP_Text>();
        if (textoTitulo != null)
        {
            textoTitulo.textWrappingMode = TextWrappingModes.NoWrap;
            textoTitulo.fontSizeMax = textoTitulo.fontSize;
            textoTitulo.fontSizeMin = textoTitulo.fontSize * 0.5f;
            textoTitulo.enableAutoSizing = true;
            textoTitulo.rectTransform.sizeDelta = new Vector2(1500f, textoTitulo.rectTransform.sizeDelta.y);
        }

        // El aviso: una copia del titulo, mas chica, blanca y con el material del texto de los
        // botones (el del titulo lleva el halo de neon).
        var aviso = Instantiate(copiaTitulo.gameObject, panelReiniciar.transform);
        aviso.name = "Aviso";
        var traducidoAviso = aviso.GetComponent<TextoTraducido>();
        if (traducidoAviso != null)
        {
            traducidoAviso.enabled = false;
            Destroy(traducidoAviso);
        }
        avisoReiniciar = aviso.GetComponent<TMP_Text>();
        var textoBoton = continuar.GetComponentInChildren<TMP_Text>(true);
        if (avisoReiniciar != null)
        {
            if (textoBoton != null) avisoReiniciar.fontSharedMaterial = textoBoton.fontSharedMaterial;
            avisoReiniciar.color = Color.white;
            avisoReiniciar.enableVertexGradient = false;
            avisoReiniciar.enableAutoSizing = false;
            avisoReiniciar.fontSize = 46f;
            avisoReiniciar.textWrappingMode = TextWrappingModes.Normal;
            avisoReiniciar.rectTransform.sizeDelta = new Vector2(900f, 80f);
            avisoReiniciar.rectTransform.anchoredPosition = new Vector2(0f, alturaAviso);
        }

        // SEGUIR JUGANDO es CONTINUAR, verde y latiendo: es lo que se esperaba tocar. REINICIAR
        // queda como en la pausa, pero lejos.
        seguir.name = "BotonSeguir";
        seguir.anchoredPosition = new Vector2(seguir.anchoredPosition.x, alturaSeguir);
        var traducidoSeguir = seguir.GetComponentInChildren<TextoTraducido>(true);
        if (traducidoSeguir != null) traducidoSeguir.id = "salir_seguir";
        Accion(seguir, SeguirJugando);
        var jugoso = seguir.GetComponent<BotonJugoso>();
        if (jugoso != null) jugoso.respirar = true;

        reiniciarSi.name = "BotonReiniciarSi";
        reiniciarSi.anchoredPosition = new Vector2(reiniciarSi.anchoredPosition.x, alturaReiniciar);
        Accion(reiniciarSi, ReiniciarYa);
    }

    // Un evento nuevo y no RemoveAllListeners: ese no saca los que vienen puestos desde el
    // inspector (los de la pausa, que la copia trae).
    private static void Accion(RectTransform boton, UnityEngine.Events.UnityAction alTocar)
    {
        var button = boton.GetComponent<Button>();
        if (button == null) return;
        button.onClick = new Button.ButtonClickedEvent();
        button.onClick.AddListener(alTocar);
    }

    public void IrAlMenu()
    {
        Restaurar();
        SceneManager.LoadScene(0);
    }

    // timeScale y AudioListener.pause son globales: si la escena se descarga en
    // pausa por otro camino (la R de RestartScene), la siguiente arrancaria
    // congelada y muda.
    private void OnDestroy()
    {
        if (Pausado) Restaurar();
    }

    private static void Restaurar()
    {
        Pausado = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }
}
