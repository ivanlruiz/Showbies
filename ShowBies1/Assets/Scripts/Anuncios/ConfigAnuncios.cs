using UnityEngine;

// Todos los numeros de los anuncios en un asset, como el catalogo de mejoras: se
// ajustan sin recompilar y sin tocar escenas. Vive en
// Assets/Anuncios/Resources/ConfigAnuncios.asset para que lo encuentre cualquiera.
//
// Si falta, ServicioAnuncios no ofrece nada (con un LogError): mejor sin anuncios
// que con topes inventados.
[CreateAssetMenu(fileName = "ConfigAnuncios", menuName = "ShowBies/Config de anuncios")]
public class ConfigAnuncios : ScriptableObject
{
    public const string RutaEnResources = "ConfigAnuncios";

    public enum Proveedor
    {
        Nulo,     // no hay anuncios (Windows, o mientras no haya red de anuncios)
        Falso,    // el de las pruebas: un cartel a pantalla completa con botones
        Real      // la red de verdad, cuando se integre
    }

    [Tooltip("Cual se usa. Se elige acá y no con un #if: así el proveedor falso también "
        + "llega al teléfono en la APK de prueba. ConstructorAndroid lo fuerza a Falso al buildear la APK.")]
    public Proveedor proveedor = Proveedor.Falso;

    [Header("Cuándo se puede ofrecer")]
    [Tooltip("Partidas terminadas que hacen falta: 2 = nunca en la primera partida.")]
    public int partidasTerminadasMinimas = 2;
    [Tooltip("Segundos de la partida que acaba de terminar, para que morir a propósito no regale videos.")]
    public float segundosDeLaPartidaMinimos = 90f;
    [Tooltip("Segundos jugados en total desde que se instaló.")]
    public float segundosJugadosMinimos = 180f;
    [Tooltip("Monedas mínimas de la partida para ofrecer el x2.")]
    public int monedasMinimasParaDuplicar = 20;

    [Header("Topes")]
    public int vecesPorDia = 3;
    [Tooltip("Segundos reales entre dos videos.")]
    public float segundosEntreAnuncios = 60f;
    [Tooltip("Cuántas veces por día se premia igual un video que falló al mostrarse.")]
    public int fallasPremiadasPorDia = 1;

    [Header("Descanso")]
    [Tooltip("Minutos de sesión a partir de los cuales la derrota sugiere descansar. 0 lo apaga.")]
    public float minutosParaAvisoDeDescanso = 60f;

    private static ConfigAnuncios instancia;
    private static bool avisoDeFaltante;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetearEstadoCompartido()
    {
        instancia = null;
        avisoDeFaltante = false;
    }

    public static ConfigAnuncios Instancia
    {
        get
        {
            if (instancia == null)
            {
                instancia = Resources.Load<ConfigAnuncios>(RutaEnResources);
                if (instancia == null && !avisoDeFaltante)
                {
                    avisoDeFaltante = true;
                    Debug.LogError("ConfigAnuncios: no se encontro Resources/" + RutaEnResources + ". No se ofrecen videos.");
                }
            }
            return instancia;
        }
    }

    private void OnValidate()
    {
        partidasTerminadasMinimas = Mathf.Max(0, partidasTerminadasMinimas);
        segundosDeLaPartidaMinimos = Mathf.Max(0f, segundosDeLaPartidaMinimos);
        segundosJugadosMinimos = Mathf.Max(0f, segundosJugadosMinimos);
        monedasMinimasParaDuplicar = Mathf.Max(0, monedasMinimasParaDuplicar);
        vecesPorDia = Mathf.Max(0, vecesPorDia);
        segundosEntreAnuncios = Mathf.Max(0f, segundosEntreAnuncios);
        fallasPremiadasPorDia = Mathf.Max(0, fallasPremiadasPorDia);
        minutosParaAvisoDeDescanso = Mathf.Max(0f, minutosParaAvisoDeDescanso);
    }
}
