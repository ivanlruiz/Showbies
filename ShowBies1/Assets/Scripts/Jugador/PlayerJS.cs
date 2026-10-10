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
    // Mas lejos que esto no se elige aunque se vea (la camara ve unos 12 m a los costados).
    public const float AlcanceAutoApuntar = 16f;
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
    private bool enTutorial;
    private Camera camara;

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

    void Start()
    {
        enTutorial = FindAnyObjectByType<TutorialManager>() != null;
        camara = Camera.main;
    }

    void Update()
    {
        if (!Plataforma.EsMovil || MenuPausa.JuegoCongelado) return;
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

        if (hoz == 0 && ver == 0)
        {
            Vector3 haciaElBlanco;
            if (AutoApuntar && !enTutorial && BuscarBlanco(out haciaElBlanco))
            {
                Apuntar(haciaElBlanco);
                return;
            }
            blanco = null;
            player.FijarDisparo(false);
            return;
        }

        blanco = null;
        Apuntar(new Vector3(hoz, 0f, ver));
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

        // Por el mismo camino que el clic de PC, que ademas prende la animacion de
        // disparo: aca solo se prendia el arma y el muñeco no disparaba nunca.
        player.FijarDisparo(true);
    }

    // El zombi vivo mas cercano que se ve, a menos de AlcanceAutoApuntar, y la direccion hacia
    // el en el piso. Se queda con el de antes mientras el otro no este bastante mas cerca.
    bool BuscarBlanco(out Vector3 hacia)
    {
        hacia = Vector3.zero;
        if (camara == null) camara = Camera.main;
        Vector3 yo = player.transform.position;
        float alcance2 = AlcanceAutoApuntar * AlcanceAutoApuntar;

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
