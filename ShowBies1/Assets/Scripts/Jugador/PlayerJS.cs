using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// OJO: nada de este archivo va adentro de un #if UNITY_ANDROID.
//
// Antes la clase entera estaba envuelta en uno, asi que en un editor con target
// Windows compilaba VACIA. Unity guarda las escenas segun los campos que el
// script tiene en ese momento, y al no tener ninguno los borraba: cada vez que
// se guardaba una escena desde Windows se perdia el cableado de los joysticks.
// Ya paso, por eso hubo que volver a asignar las referencias a mano.
//
// Ahora el codigo compila igual en las dos plataformas y quien decide es
// Application.isMobilePlatform en runtime. Ademas de arreglar el guardado, eso
// hace que compilar en Windows valide tambien la build de Android.
//
// Corre antes que el arma (GunController), asi el tiro de este cuadro sale con la mira de este
// cuadro (16/10: con el orden librado al azar, el arma a veces disparaba antes; ver
// GunController.FijarRumbo), y despues de los joysticks dinamicos, que le pasan los toques.
[DefaultExecutionOrder(-10)]
public class PlayerJS : MonoBehaviour
{
    public PlayerController player;
    public GunController thegun;

    public FixedJoystick moveJoystick;
    public FixedJoystick lookJoystick;

    // AUTO-APUNTAR (10/10, de la revision de Gemini: el twin-stick con las dos manos aleja al
    // publico casual). En el telefono, sin el pulgar en el joystick de disparo, el jugador apunta
    // solo al zombi vivo mas cercano que se ve en pantalla y dispara. El joystick sigue mandando
    // cuando se lo toca: es para elegir a quien (el jefe, el tesoro). Es una preferencia del
    // telefono (PlayerPrefs, como MOSTRAR FPS) y viene prendido. En el tutorial no anda: ahi se
    // ensenia el joystick.
    public const string ClaveAutoApuntar = "AutoApuntar";
    // Mas lejos que esto no se elige aunque se vea. Era 16 (la camara ve unos 12 m a los
    // costados) hasta el 14/10: Ivan lo probo en el telefono y "mata todo solo", y Argon (Gemini)
    // lo paso de francotirador a defensa personal, hasta media pantalla. Lo que esta mas lejos se
    // le tira con el joystick.
    public const float AlcanceAutoApuntar = 7.5f;
    // Y despues de cada muerte tarda esto en enganchar al siguiente (14/10, de Argon), sin tirar.
    public const float PausaAlCambiarDeBlanco = 0.25f;
    // El blanco de ahora se cambia por otro solo si el otro esta bastante mas cerca: con dos a la
    // misma distancia, el jugador giraba de uno al otro en cada cuadro.
    public const float VentajaParaCambiar = 0.75f;
    // Que se vea: con el centro a esto de los bordes de la pantalla.
    public const float MargenEnPantalla = 0.03f;

    private static int autoApuntar = -1;   // -1 sin leer todavia

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetearEstadoCompartido()
    {
        autoApuntar = -1;
    }

    public static bool AutoApuntar
    {
        get
        {
            if (autoApuntar < 0) autoApuntar = PlayerPrefs.GetInt(ClaveAutoApuntar, 1) != 0 ? 1 : 0;
            return autoApuntar == 1;
        }
    }

    // Lo escribe a disco quien lo cambia al terminar (la ventana de opciones, al cerrarse).
    public static void FijarAutoApuntar(bool valor)
    {
        autoApuntar = valor ? 1 : 0;
        PlayerPrefs.SetInt(ClaveAutoApuntar, autoApuntar);
    }

    private EnemyController blanco;
    // La aparicion del blanco (EnemyController.SigueVivo: un zombi del pool vuelve como otro) y
    // hasta cuando no tira despues de perderlo.
    private int aparicionDelBlanco;
    private float sinTirarHasta = float.NegativeInfinity;
    private bool enTutorial;
    private Camera camara;

    // Los joysticks dinamicos (JoystickDinamico, 12/10): null con la opcion apagada o en PC.
    private JoystickDinamico dinamicoDeMover;
    private JoystickDinamico dinamicoDeDisparo;

    public JoystickDinamico DinamicoDeMover
    {
        get { return dinamicoDeMover; }
    }

    public JoystickDinamico DinamicoDeDisparo
    {
        get { return dinamicoDeDisparo; }
    }

    // A quien esta apuntando solo, o null (lo miran las pruebas).
    public EnemyController Blanco
    {
        get { return blanco; }
    }

    // Para la granada, que en movil sale hacia donde se venia apuntando.
    public Vector3 UltimaDireccionApuntada { get; private set; }
    private float ultimoApuntadoEn = float.NegativeInfinity;

    public bool ApuntoHaceMenosDe(float segundos)
    {
        return Time.time - ultimoApuntadoEn <= segundos;
    }

    // En Awake y no en Start: la guia de la primera partida mira en su Start si son dinamicos.
    void Awake()
    {
        if (!Plataforma.EsMovil || !JoystickDinamico.Activos) return;
        dinamicoDeMover = JoystickDinamico.Instalar(moveJoystick, JoystickDinamico.ZonaIzquierda);
        dinamicoDeDisparo = JoystickDinamico.Instalar(lookJoystick, JoystickDinamico.ZonaDerecha);
    }

    void Start()
    {
        enTutorial = FindAnyObjectByType<TutorialManager>() != null;
        camara = Camera.main;
    }

    void Update()
    {
        if (!Plataforma.EsMovil) return;
        // Antes de mirar la pausa: un joystick soltado en plena eleccion de cartas vuelve a su
        // rincon, y ahi se tiene que ver como en reposo.
        MostrarJoysticks();
        if (MenuPausa.JuegoCongelado) return;
        // Volando por el golpe del jefe no hay control (PlayerController.Lanzar): sin esto, con el
        // pulgar en el joystick de disparo, seguia girando y disparando en el aire.
        if (player != null && player.EnElAire)
        {
            player.FijarDisparo(false);
            return;
        }

        UpdateMoveJoystick();
        UpdateShootJoystick();
    }

    void UpdateMoveJoystick()
    {
        if (player == null || moveJoystick == null) return;

        player.Move(new Vector2(moveJoystick.Horizontal, moveJoystick.Vertical));
    }

    void UpdateShootJoystick()
    {
        if (player == null || thegun == null || lookJoystick == null) return;

        float hoz = lookJoystick.Horizontal;
        float ver = lookJoystick.Vertical;

        bool aMano = hoz != 0 || ver != 0;
        if (!aMano)
        {
            // Murio (o se fue) el que tenia: un rato sin tirar antes de buscar al siguiente.
            if (blanco != null && !EnemyController.SigueVivo(blanco, aparicionDelBlanco))
            {
                blanco = null;
                sinTirarHasta = Time.time + PausaAlCambiarDeBlanco;
            }
            Vector3 haciaElBlanco;
            if (AutoApuntar && !enTutorial && Time.time >= sinTirarHasta && BuscarBlanco(out haciaElBlanco))
            {
                thegun.FijarAutomatico(true);
                Apuntar(haciaElBlanco);
                return;
            }
            blanco = null;
            thegun.FijarAutomatico(false);
            player.FijarDisparo(false);
            return;
        }

        blanco = null;
        thegun.FijarAutomatico(false);
        Apuntar(new Vector3(hoz, 0f, ver));
    }

    // Con el auto-apuntado el joystick de disparo se queda (es el "boton de daño maximo": a mano
    // la cadencia es entera), pero mientras tira el automatico se ve apagado, y al tocarlo se
    // prende y salta un poco (12/10, de Gemini). Sin auto-apuntado, o en el tutorial, siempre entero.
    public const float AlfaDelJoystickEnAutomatico = 0.4f;
    private const float EscalaDelJoystickTocado = 1.08f;
    private CanvasGroup grupoDelJoystick;
    private CanvasGroup grupoDeMover;
    private Vector3 escalaDelJoystick = Vector3.one;
    private float tocado;   // 0 suelto, 1 con el pulgar: se acerca de a poco

    public float AlfaDelJoystickDeDisparo { get { return grupoDelJoystick != null ? grupoDelJoystick.alpha : 1f; } }
    public float AlfaDelJoystickDeMover { get { return grupoDeMover != null ? grupoDeMover.alpha : 1f; } }

    // Cuanto se ve cada joystick sin el pulgar encima. Los dinamicos no se ven hasta que se los
    // toca (de Gemini), salvo el de disparo con el auto-apuntado, que espera al 40 % en su rincon,
    // y los dos en el tutorial: ahi los textos dicen "el joystick de la izquierda", y tienen que
    // estar a la vista. Se pueden tomar igual desde cualquier lugar de su zona.
    public float ReposoDelDeMover
    {
        get { return dinamicoDeMover != null && !enTutorial ? 0f : 1f; }
    }

    public float ReposoDelDeDisparo
    {
        get
        {
            if (AutoApuntar && !enTutorial) return AlfaDelJoystickEnAutomatico;
            return dinamicoDeDisparo != null && !enTutorial ? 0f : 1f;
        }
    }

    // Con el pulgar encima: el del dinamico, aunque la palanca este al medio, o la palanca movida
    // (los fijos, y los bancos que le hablan al joystick directo).
    static bool Tocado(Joystick joystick, JoystickDinamico dinamico)
    {
        return (dinamico != null && dinamico.Tocado) || joystick.Direction != Vector2.zero;
    }

    static CanvasGroup Grupo(Joystick joystick)
    {
        var grupo = joystick.GetComponent<CanvasGroup>();
        if (grupo == null) grupo = joystick.gameObject.AddComponent<CanvasGroup>();
        return grupo;
    }

    void MostrarJoysticks()
    {
        if (moveJoystick != null)
        {
            if (grupoDeMover == null) grupoDeMover = Grupo(moveJoystick);
            float reposo = ReposoDelDeMover;
            if (dinamicoDeMover != null) dinamicoDeMover.alfaEnReposo = reposo;
            grupoDeMover.alpha = Tocado(moveJoystick, dinamicoDeMover) ? 1f : reposo;
        }
        if (lookJoystick != null) MostrarJoystickDeDisparo(Tocado(lookJoystick, dinamicoDeDisparo));
    }

    void MostrarJoystickDeDisparo(bool aMano)
    {
        if (grupoDelJoystick == null)
        {
            grupoDelJoystick = Grupo(lookJoystick);
            escalaDelJoystick = lookJoystick.transform.localScale;
        }
        float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
        tocado = Mathf.MoveTowards(tocado, aMano ? 1f : 0f, dt / 0.12f);
        // El dinamico, al soltarlo, salta a su rincon: ahi aparece ya en reposo, sin apagarse de a
        // poco (se lo veria prenderse un cuadro en el rincon).
        if (dinamicoDeDisparo != null && !aMano) tocado = 0f;
        float reposo = ReposoDelDeDisparo;
        if (dinamicoDeDisparo != null) dinamicoDeDisparo.alfaEnReposo = reposo;
        // Si en reposo no se ve, aparece y se va de golpe: al soltarlo vuelve a su rincon, y
        // apagandose de a poco se lo veria saltar hasta ahi.
        grupoDelJoystick.alpha = reposo <= 0f ? (aMano ? 1f : 0f) : Mathf.Lerp(reposo, 1f, tocado);
        lookJoystick.transform.localScale = escalaDelJoystick * Mathf.Lerp(1f, EscalaDelJoystickTocado, CurvasUI.Campana(tocado) * 0.5f + tocado * 0.5f);
    }

    void Apuntar(Vector3 hacia)
    {
        UltimaDireccionApuntada = hacia.normalized;
        ultimoApuntadoEn = Time.time;

        // El cuerpo mira hacia donde se apunta, como en PC con el mouse: la pistola esta en
        // la mano y el brazo apunta al frente (ArmaEnLaMano). Hasta el 23/9 el cuerpo miraba
        // hacia donde caminaba y lo que giraba era el arma sola, un cubito en el pecho.
        // Move ya puso el rumbo de la caminata en este cuadro; esto lo pisa.
        player.transform.rotation = Quaternion.LookRotation(UltimaDireccionApuntada);

        // La direccion pura, y no un LookAt a un punto a un metro del centro: con el arma a
        // otra altura que el jugador, el LookAt la inclinaba y las balas pasaban por arriba.
        thegun.transform.rotation = Quaternion.LookRotation(UltimaDireccionApuntada);
        // Y el tiro sale con esta, no con la del transform, que la interpolacion del cuerpo pisa.
        thegun.FijarRumbo(UltimaDireccionApuntada);

        // Por el mismo camino que el clic de PC, que ademas prende la animacion de
        // disparo: aca solo se prendia el arma y el muñeco no disparaba nunca.
        player.FijarDisparo(true);
    }

    // Hasta donde apunta solo: AlcanceAutoApuntar, o lo que llega el arma de la mano si es menos
    // (la escopeta, 7 m: tirarle a lo que no alcanza era gastar balas en el aire).
    public static float AlcanceDelAutoApuntado(GunController arma)
    {
        if (arma == null) return AlcanceAutoApuntar;
        return Mathf.Min(AlcanceAutoApuntar, Proyectiles.Alcance(arma.TramoDelArma));
    }

    // El zombi vivo mas cercano que se ve, a menos de AlcanceAutoApuntar, y la direccion hacia
    // el en el piso. Se queda con el de antes mientras el otro no este bastante mas cerca.
    bool BuscarBlanco(out Vector3 hacia)
    {
        hacia = Vector3.zero;
        if (camara == null) camara = Camera.main;
        Vector3 yo = player.transform.position;
        float alcance = AlcanceDelAutoApuntado(player != null ? player.theGun : null);
        float alcance2 = alcance * alcance;

        EnemyController mejor = null;
        float mejorDistancia2 = float.MaxValue;
        var vivos = EnemyController.Vivos;
        for (int i = 0; i < vivos.Count; i++)
        {
            var zombi = vivos[i];
            if (zombi == null || !zombi.Vivo) continue;
            float d2 = DistanciaEnElPiso2(zombi.transform.position, yo);
            if (d2 > alcance2 || d2 >= mejorDistancia2 || !SeVe(zombi.transform.position)) continue;
            mejorDistancia2 = d2;
            mejor = zombi;
        }

        if (blanco != null && blanco != mejor && blanco.Vivo && SeVe(blanco.transform.position))
        {
            float actual2 = DistanciaEnElPiso2(blanco.transform.position, yo);
            if (actual2 <= alcance2 && mejorDistancia2 >= actual2 * VentajaParaCambiar * VentajaParaCambiar) mejor = blanco;
        }

        if (mejor != null && mejor != blanco) aparicionDelBlanco = mejor.NumeroDeAparicion;
        blanco = mejor;
        if (mejor == null) return false;
        hacia = mejor.transform.position - yo;
        hacia.y = 0f;
        return hacia.sqrMagnitude > 1e-4f;
    }

    static float DistanciaEnElPiso2(Vector3 a, Vector3 b)
    {
        float x = a.x - b.x;
        float z = a.z - b.z;
        return x * x + z * z;
    }

    bool SeVe(Vector3 punto)
    {
        if (camara == null) return true;
        Vector3 v = camara.WorldToViewportPoint(punto);
        return v.z > 0f && v.x > MargenEnPantalla && v.x < 1f - MargenEnPantalla
               && v.y > MargenEnPantalla && v.y < 1f - MargenEnPantalla;
    }
}
