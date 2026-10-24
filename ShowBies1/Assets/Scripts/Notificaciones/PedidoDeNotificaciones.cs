using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// La ventana propia que pide el permiso de las notificaciones antes del dialogo de Android 13+ (de Argon, 24/10: pedido en
// frio, en EE. UU. "el 75 % toca Don't allow por reflejo", y Android no deja volver a mostrar su cartel despues de dos
// rechazos). "¡NO PIERDAS TU BOTIN!", que se avisa y "cero spam", y dos botones: ¡SI, AVISAME! (verde, late) recien ahi
// llama al dialogo del sistema; AHORA NO cierra sin gastarlo. Sale en el menu tranquilo, en la fila de PedidoDeResena
// (despues del consentimiento de los anuncios y antes de la reseña), cuando Notificaciones.CorrespondePedirAhora: la
// primera vez al llegar a la oleada 5 o al cobrar la primera diaria, y una segunda si dijo que no.
//
// No esta en la escena: se instala al cargar el menu sobre ConfirmarSalir y copia su ventana ya armada (el mismo esquema:
// titulo, aviso, un boton verde y uno de vidrio). El atras de Android es AHORA NO (BotonAtrasMenu).
public class PedidoDeNotificaciones : MonoBehaviour
{
    private const float DuracionRebote = 0.3f;

    private static PedidoDeNotificaciones instancia;

    private ConfirmarSalir confirmarSalir;
    private GameObject panel;
    private RectTransform ventana;
    private float abiertaDesde = -1f;

    public static bool Abierto
    {
        get { return instancia != null && instancia.panel != null && instancia.panel.activeSelf; }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetearEstadoCompartido()
    {
        instancia = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Instalar()
    {
        SceneManager.sceneLoaded -= AlCargar;
        SceneManager.sceneLoaded += AlCargar;
    }

    private static void AlCargar(Scene escena, LoadSceneMode modo)
    {
        if (escena.buildIndex != 0) return;
        var salir = FindAnyObjectByType<ConfirmarSalir>(FindObjectsInactive.Include);
        if (salir == null) return;
        instancia = salir.gameObject.AddComponent<PedidoDeNotificaciones>();
        instancia.confirmarSalir = salir;
    }

    private void OnDestroy()
    {
        if (instancia == this) instancia = null;
    }

    // La llama PedidoDeResena con el menu tranquilo. Devuelve si la abrio (y entonces la reseña espera otra visita).
    public static bool PedirSiCorresponde()
    {
        if (instancia == null || !Notificaciones.CorrespondePedirAhora) return false;
        if (!instancia.Abrir()) return false;
        Notificaciones.AnotarPedido();
        return true;
    }

    // Para mirarla en el editor, donde nunca corresponde (no hay Android).
    public static bool AbrirParaPruebas()
    {
        return instancia != null && instancia.Abrir();
    }

    public static void CerrarElAbierto()
    {
        if (instancia != null) instancia.Cerrar();
    }

    private bool Abrir()
    {
        if (panel == null) Armar();
        if (panel == null) return false;
        panel.SetActive(true);
        abiertaDesde = Time.unscaledTime;
        if (ventana != null) ventana.localScale = Vector3.zero;
        return true;
    }

    private void Cerrar()
    {
        if (panel != null) panel.SetActive(false);
        abiertaDesde = -1f;
    }

    private void Si()
    {
        Cerrar();
        // Prende la preferencia y, como no hay permiso, pide el del sistema.
        Notificaciones.Fijar(true);
    }

    private void Armar()
    {
        if (confirmarSalir == null) return;
        panel = confirmarSalir.CopiarVentana("PanelNotificaciones", out ventana);
        if (panel == null) return;

        // Los textos de la copia pasan a los de este pedido. Los ids van enteros: la prueba de idiomas los busca asi.
        foreach (var traducido in panel.GetComponentsInChildren<TextoTraducido>(true))
        {
            switch (traducido.id)
            {
                case "salir_titulo": traducido.id = "notif_pedido_titulo"; break;
                case "salir_seguir": traducido.id = "notif_pedido_si"; break;
                case "menu_salir": traducido.id = "notif_pedido_no"; break;
                case "salir_aviso":
                    traducido.id = "notif_pedido_texto";
                    // Es mas largo que el de salir: tres renglones mas chicos.
                    var texto = traducido.GetComponent<TMP_Text>();
                    texto.fontSize = 30f;
                    texto.rectTransform.sizeDelta = new Vector2(620f, 120f);
                    texto.rectTransform.anchoredPosition = new Vector2(0f, 90f);
                    break;
            }
        }

        Conectar("BotonSeguir", Si);
        Conectar("BotonSalir", Cerrar);
    }

    private void Conectar(string nombre, UnityEngine.Events.UnityAction alTocar)
    {
        foreach (var boton in panel.GetComponentsInChildren<Button>(true))
        {
            if (boton.name != nombre) continue;
            boton.onClick = new Button.ButtonClickedEvent();
            boton.onClick.AddListener(alTocar);
        }
    }

    private void Update()
    {
        if (abiertaDesde < 0f || ventana == null) return;
        float t = Mathf.Clamp01((Time.unscaledTime - abiertaDesde) / DuracionRebote);
        ventana.localScale = Vector3.one * CurvasUI.SalidaAtras(t);
        if (t >= 1f) abiertaDesde = -1f;
    }
}
