using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

// EL JOYSTICK DINAMICO (12/10; lo pidieron en Discord, en #ideas, y lo diseño Gemini): el joystick
// no espera al pulgar en su rincon, sino que aparece donde se apoya el pulgar. Cada uno tiene su
// zona, la mitad de su lado de la pantalla desde abajo hasta el 70 % del alto (arriba esta el HUD),
// y un toque que empieza en la zona pone ahi el centro del joystick, con la palanca al medio. Si el
// pulgar se va mas alla del borde, el joystick lo sigue. Al soltar vuelve a su rincon.
//
// Va en la raiz de cada FixedJoystick (lo pone PlayerJS, solo en el telefono y con la opcion
// prendida) y le pasa los toques a mano: OnPointerDown, OnDrag y OnPointerUp del joystick del pack.
// No es una imagen invisible que recibe los toques: cualquier texto del HUD con raycastTarget (la
// vida, los avisos, la barra del PODER) se los comeria. Lee los toques (Input.touches, y el raton en
// el editor) y para cada uno nuevo mira lo mismo que el EventSystem: si lo primero que toca es algo
// que atiende toques (la granada, la furia, la pausa), es de eso; si no, y cae en la zona, es suyo.
// Asi la granada y la furia quedan afuera de la zona sin medirlas. El joystick no recibe toques
// propios mientras tanto (blocksRaycasts en falso).
//
// Que se vea o no lo decide PlayerJS: invisible hasta que se lo toca, salvo el de disparo con el
// auto-apuntado, que espera al 40 % en su rincon, y los dos en el tutorial, que los nombra.
//
// Corre antes que PlayerJS (que va antes que el arma): el toque de este cuadro llega a la mira de
// este cuadro.
[DefaultExecutionOrder(-20)]
public class JoystickDinamico : MonoBehaviour
{
    public const string Clave = "JoysticksDinamicos";

    // Las zonas, en fracciones del area segura (el CanvasHelper donde cuelgan los joysticks).
    public static readonly Rect ZonaIzquierda = new Rect(0f, 0f, 0.5f, 0.7f);
    public static readonly Rect ZonaDerecha = new Rect(0.5f, 0f, 0.5f, 0.7f);

    public const int Ninguno = int.MinValue;
    public const int Raton = -1;
    // El de los bancos en play, que no tienen dedos: no se suelta solo por no estar en Input.touches.
    public const int DedoDePrueba = -100;

    private static int activos = -1;   // -1 sin leer todavia
    private static readonly List<RaycastResult> resultados = new List<RaycastResult>();
    private static PointerEventData eventoDelRaycast;
    private static EventSystem sistemaDelEvento;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetearEstadoCompartido()
    {
        activos = -1;
        resultados.Clear();
        eventoDelRaycast = null;
        sistemaDelEvento = null;
    }

    // Es una preferencia del telefono (PlayerPrefs, como AUTO-APUNTADO) y viene prendida. Se cambia
    // en OPCIONES, en el menu: las partidas la leen al empezar.
    public static bool Activos
    {
        get
        {
            if (activos < 0) activos = PlayerPrefs.GetInt(Clave, 1) != 0 ? 1 : 0;
            return activos == 1;
        }
    }

    // Lo escribe a disco quien lo cambia al terminar (la ventana de opciones, al cerrarse).
    public static void Fijar(bool valor)
    {
        activos = valor ? 1 : 0;
        PlayerPrefs.SetInt(Clave, activos);
    }

    public static JoystickDinamico Instalar(Joystick joystick, Rect zona)
    {
        if (joystick == null) return null;
        var dinamico = joystick.GetComponent<JoystickDinamico>();
        if (dinamico == null) dinamico = joystick.gameObject.AddComponent<JoystickDinamico>();
        dinamico.joystick = joystick;
        dinamico.zona = zona;
        return dinamico;
    }

    // La zona en la pantalla, de un area (en pixeles) y la zona en fracciones de ella.
    public static Rect ZonaEnPantalla(Rect area, Rect zona)
    {
        return new Rect(area.x + zona.x * area.width, area.y + zona.y * area.height,
                        zona.width * area.width, zona.height * area.height);
    }

    // Donde queda el centro del joystick con el pulgar en "dedo": donde estaba, o, si el pulgar se
    // fue mas alla del radio, arrastrado hasta que el pulgar quede justo en el borde.
    public static Vector2 CentroQueSigue(Vector2 centro, Vector2 dedo, float radio)
    {
        Vector2 hacia = dedo - centro;
        float distancia = hacia.magnitude;
        if (radio <= 0f || distancia <= radio) return centro;
        return dedo - hacia / distancia * radio;
    }

    // Lo que se ve sin el pulgar encima: lo pone PlayerJS, y es lo que queda al soltar aunque
    // PlayerJS ya no corra (se apaga al morir, y la oferta de revivir deja el HUD a la vista).
    [System.NonSerialized] public float alfaEnReposo;

    private Joystick joystick;
    private Rect zona;
    private RectTransform raiz;
    private RectTransform padre;
    private Canvas canvas;
    private CanvasGroup grupo;
    private Vector2 casa;
    private int dedo = Ninguno;
    private PointerEventData evento;

    public bool Tocado
    {
        get { return dedo != Ninguno; }
    }

    public Rect Zona
    {
        get { return zona; }
    }

    // Donde espera el joystick, en su padre.
    public Vector2 Casa
    {
        get { return casa; }
    }

    private void Start()
    {
        if (joystick == null) joystick = GetComponent<Joystick>();
        raiz = (RectTransform)transform;
        padre = raiz.parent as RectTransform;
        canvas = GetComponentInParent<Canvas>();
        casa = raiz.anchoredPosition;
        grupo = GetComponent<CanvasGroup>();
        if (grupo == null) grupo = gameObject.AddComponent<CanvasGroup>();
        // Los toques los reparte este componente: el joystick, en su rincon, no tiene que tomar
        // los que caen encima de el.
        grupo.blocksRaycasts = false;
    }

    private void OnDisable()
    {
        Soltar();
    }

    private void OnApplicationPause(bool pausada)
    {
        if (pausada) Soltar();
    }

    private void OnApplicationFocus(bool foco)
    {
        // Con la app sin foco (una llamada, la cortina) el toque puede terminar sin que llegue.
        if (!foco) Soltar();
    }

    private void Update()
    {
        if (joystick == null || raiz == null) return;
        bool puede = SePuedeJugar();
        if (dedo != Ninguno)
        {
            if (dedo == DedoDePrueba)
            {
                if (!puede) Soltar();
                return;
            }
            Vector2 donde;
            // La pausa, una eleccion de cartas o la muerte lo sueltan en el acto: el toque puede
            // terminar mientras tanto sin que este componente lo vea.
            if (!puede || !Seguir(dedo, out donde))
            {
                Soltar();
                return;
            }
            Arrastrar(donde);
            return;
        }

        if (!puede) return;
        Vector2 nuevo;
        int id;
        if (ToqueNuevo(out nuevo, out id)) Apoyar(nuevo, id);
    }

    private static bool SePuedeJugar()
    {
        if (MenuPausa.JuegoCongelado) return false;
        var salud = PlayerHealth.instance;
        return salud == null || !salud.EstaMuerto;
    }

    // Un toque que empieza en este cuadro, en la zona y sin nada que lo atienda encima.
    private bool ToqueNuevo(out Vector2 donde, out int id)
    {
        if (Input.touchCount > 0 || Input.touchSupported)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch toque = Input.GetTouch(i);
                if (toque.phase != TouchPhase.Began || !EsMio(toque.position)) continue;
                donde = toque.position;
                id = toque.fingerId;
                return true;
            }
        }
        else if (Input.GetMouseButtonDown(0) && EsMio(Input.mousePosition))
        {
            // El editor con el target en Android: el raton hace de dedo, como con los joysticks fijos.
            donde = Input.mousePosition;
            id = Raton;
            return true;
        }
        donde = Vector2.zero;
        id = Ninguno;
        return false;
    }

    // Donde esta el dedo que se sigue, o falso si ya se levanto.
    private static bool Seguir(int id, out Vector2 donde)
    {
        if (id == Raton)
        {
            donde = Input.mousePosition;
            return Input.GetMouseButton(0);
        }
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch toque = Input.GetTouch(i);
            if (toque.fingerId != id) continue;
            donde = toque.position;
            return toque.phase != TouchPhase.Ended && toque.phase != TouchPhase.Canceled;
        }
        donde = Vector2.zero;
        return false;
    }

    // Si un toque que empieza en este punto de la pantalla le toca a este joystick.
    public bool EsMio(Vector2 pantalla)
    {
        return ZonaEnPantalla().Contains(pantalla) && !AtiendeAlgo(pantalla);
    }

    public Rect ZonaEnPantalla()
    {
        if (padre == null) return new Rect(0f, 0f, 0f, 0f);
        var esquinas = new Vector3[4];
        padre.GetWorldCorners(esquinas);
        Camera camara = Camara();
        Vector2 abajo = RectTransformUtility.WorldToScreenPoint(camara, esquinas[0]);
        Vector2 arriba = RectTransformUtility.WorldToScreenPoint(camara, esquinas[2]);
        return ZonaEnPantalla(new Rect(abajo, arriba - abajo), zona);
    }

    // Lo mismo que hace el EventSystem con un toque: lo primero que toca se lo lleva, y si ahi (o en
    // sus padres) hay algo que atiende toques, el toque es de eso. Un texto o una imagen sueltos no
    // atienden nada: el toque sigue siendo libre.
    public static bool AtiendeAlgo(Vector2 pantalla)
    {
        var sistema = EventSystem.current;
        if (sistema == null) return false;
        if (eventoDelRaycast == null || sistemaDelEvento != sistema)
        {
            eventoDelRaycast = new PointerEventData(sistema);
            sistemaDelEvento = sistema;
        }
        eventoDelRaycast.Reset();
        eventoDelRaycast.position = pantalla;
        resultados.Clear();
        sistema.RaycastAll(eventoDelRaycast, resultados);
        GameObject primero = null;
        for (int i = 0; i < resultados.Count; i++)
        {
            if (resultados[i].gameObject == null) continue;
            primero = resultados[i].gameObject;
            break;
        }
        resultados.Clear();
        if (primero == null) return false;
        return ExecuteEvents.GetEventHandler<IPointerDownHandler>(primero) != null
               || ExecuteEvents.GetEventHandler<IPointerClickHandler>(primero) != null
               || ExecuteEvents.GetEventHandler<IDragHandler>(primero) != null;
    }

    // Apoyar, arrastrar y soltar: el camino de Update, y el de los bancos con DedoDePrueba.
    public void Apoyar(Vector2 pantalla, int id)
    {
        if (joystick == null || raiz == null) return;
        if (dedo != Ninguno) Soltar();
        dedo = id;
        // Primero el centro bajo el pulgar y despues el toque: la palanca arranca al medio.
        Colocar(pantalla);
        joystick.OnPointerDown(Evento(pantalla));
    }

    public void Arrastrar(Vector2 pantalla)
    {
        if (dedo == Ninguno || joystick == null) return;
        Vector2 centro = RectTransformUtility.WorldToScreenPoint(Camara(), raiz.position);
        Vector2 nuevo = CentroQueSigue(centro, pantalla, Radio());
        if (nuevo != centro) Colocar(nuevo);
        joystick.OnDrag(Evento(pantalla));
    }

    public void Soltar()
    {
        if (dedo == Ninguno) return;
        dedo = Ninguno;
        if (joystick != null) joystick.OnPointerUp(Evento(Vector2.zero));
        if (raiz != null) raiz.anchoredPosition = casa;
        if (grupo != null) grupo.alpha = alfaEnReposo;
    }

    // El radio del joystick en pixeles de la pantalla, con la misma cuenta que el pack.
    private float Radio()
    {
        return raiz.sizeDelta.x * 0.5f * (canvas != null ? canvas.scaleFactor : 1f);
    }

    private void Colocar(Vector2 pantalla)
    {
        if (padre == null) return;
        Vector3 mundo;
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(padre, pantalla, Camara(), out mundo))
            raiz.position = new Vector3(mundo.x, mundo.y, raiz.position.z);
    }

    private Camera Camara()
    {
        if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay) return null;
        return canvas.worldCamera;
    }

    private PointerEventData Evento(Vector2 pantalla)
    {
        if (evento == null) evento = new PointerEventData(EventSystem.current);
        evento.position = pantalla;
        return evento;
    }
}
