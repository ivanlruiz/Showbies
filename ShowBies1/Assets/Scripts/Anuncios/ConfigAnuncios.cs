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
        Real      // AdMob (ProveedorAdMob), solo en Android
    }

    [Tooltip("Cual se usa. Se elige acá y no con un #if. ConstructorAndroid lo fuerza a Real al buildear "
        + "la APK, que con el paquete .prueba usa los bloques de prueba de Google.")]
    public Proveedor proveedor = Proveedor.Nulo;

    [Header("Revivir")]
    [Tooltip("Segundos que da la ventanita para decidir.")]
    public float segundosParaDecidirRevivir = 10f;
    [Tooltip("Segundos que tarda la pantalla en agrisarse del todo.")]
    public float segundosDeAgrisado = 5f;
    [Tooltip("Metros de zombis que se despejan al volver.")]
    public float radioDeDespeje = 7f;
    [Tooltip("Segundos sin recibir daño después de revivir.")]
    public float segundosDeGracia = 2.5f;
    [Tooltip("Lo mínimo que tiene que haber durado la partida para ofrecer revivir.")]
    public float segundosDeLaPartidaParaRevivir = 30f;

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
    [Tooltip("Videos premiados por día, sumando los lugares. 0 = sin tope (lo que tiene el asset desde el 6/10, "
        + "pedido de Ivan).")]
    public int vecesPorDia = 3;
    [Tooltip("Videos premiados en una misma partida. 1 = o revivís o duplicás, no las dos. 0 = sin tope (el asset, "
        + "desde el 6/10: se puede revivir y duplicar en la misma partida).")]
    public int vecesPorPartida = 1;
    [Tooltip("Segundos reales entre dos videos. 0 = sin espera (el asset, desde el 6/10).")]
    public float segundosEntreAnuncios = 60f;
    [Tooltip("Cuántas veces por día se premia igual un video que falló al mostrarse.")]
    public int fallasPremiadasPorDia = 1;

    [Header("Descanso")]
    [Tooltip("Minutos de sesión a partir de los cuales la derrota sugiere descansar. 0 lo apaga.")]
    public float minutosParaAvisoDeDescanso = 60f;

    [Header("AdMob")]
    [Tooltip("El bloque bonificado de AdMob de cada lugar (ver publicacion/pasos.md). La APK de prueba usa "
        + "siempre el de prueba de Google: mirar anuncios reales propios es tráfico no válido.")]
    public string bloqueRevivir = "ca-app-pub-5295383586829735/1512416812";
    public string bloqueDuplicarDerrota = "ca-app-pub-5295383586829735/8640931940";
    public string bloqueRegaloX2 = "ca-app-pub-5295383586829735/6299120897";

    [Tooltip("La clasificación máxima de los anuncios: G, PG, T o MA. Ivan eligió T el 6/10/2026 (PG es para "
        + "todo público; T suma imágenes de miedo, deportes de lucha, redes sociales y salud). Apuestas, "
        + "citas, alcohol y lo de adultos se bloquean aparte, en AdMob.")]
    public string clasificacionMaxima = "T";

    [Tooltip("Solo en la APK de prueba: UMP hace como si el teléfono estuviera en Europa, para ver el cartel "
        + "de consentimiento y el botón PRIVACIDAD desde acá. Prenderlo recién con el mensaje europeo creado en "
        + "AdMob: sin él el cartel no puede salir, y los anuncios de la prueba se quedan esperando un "
        + "consentimiento que no llega.")]
    public bool simularEuropaEnLaPrueba = false;

    // El id de la app en AdMob. Va en el manifiesto de Plugins/Android/ShowBiesAnuncios.androidlib;
    // aca esta para que la prueba de logica compare los dos.
    public const string IdAppAdMob = "ca-app-pub-5295383586829735~6982656335";
    // El bloque bonificado de prueba de Google para Android, y la cuenta de prueba de Google.
    public const string BloqueDePruebaDeGoogle = "ca-app-pub-3940256099942544/5224354917";
    private const string CuentaDePruebaDeGoogle = "ca-app-pub-3940256099942544";
    // El sufijo del paquete de la APK de prueba (ver ConstructorAndroid).
    public const string SufijoPaqueteDePrueba = ".prueba";

    // Los lugares que tienen video hoy, en el orden en que se le pasan al puente de AdMob.
    public static readonly string[] LugaresConVideo =
    {
        LugarAnuncio.Revivir, LugarAnuncio.DuplicarDerrota, LugarAnuncio.DuplicarRegalo
    };

    public static readonly string[] ClasificacionesValidas = { "G", "PG", "T", "MA" };

    public static bool EsPaqueteDePrueba(string paquete)
    {
        return !string.IsNullOrEmpty(paquete) && paquete.EndsWith(SufijoPaqueteDePrueba);
    }

    public static bool EsBloqueDePrueba(string bloque)
    {
        return !string.IsNullOrEmpty(bloque) && bloque.StartsWith(CuentaDePruebaDeGoogle);
    }

    // El bloque de verdad de un lugar, o null si el lugar no tiene video.
    public string BloqueReal(string lugar)
    {
        switch (lugar)
        {
            case LugarAnuncio.Revivir: return bloqueRevivir;
            case LugarAnuncio.DuplicarDerrota: return bloqueDuplicarDerrota;
            case LugarAnuncio.DuplicarRegalo: return bloqueRegaloX2;
            default: return null;
        }
    }

    // El que se le pide a AdMob: el de prueba de Google en la APK de prueba, el de verdad si no.
    public string BloquePara(string lugar, bool deprueba)
    {
        return deprueba ? BloqueDePruebaDeGoogle : BloqueReal(lugar);
    }

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
        segundosParaDecidirRevivir = Mathf.Max(1f, segundosParaDecidirRevivir);
        segundosDeAgrisado = Mathf.Max(0f, segundosDeAgrisado);
        radioDeDespeje = Mathf.Max(0f, radioDeDespeje);
        segundosDeGracia = Mathf.Max(0f, segundosDeGracia);
        segundosDeLaPartidaParaRevivir = Mathf.Max(0f, segundosDeLaPartidaParaRevivir);
        segundosJugadosMinimos = Mathf.Max(0f, segundosJugadosMinimos);
        monedasMinimasParaDuplicar = Mathf.Max(0, monedasMinimasParaDuplicar);
        vecesPorDia = Mathf.Max(0, vecesPorDia);
        vecesPorPartida = Mathf.Max(0, vecesPorPartida);
        segundosEntreAnuncios = Mathf.Max(0f, segundosEntreAnuncios);
        fallasPremiadasPorDia = Mathf.Max(0, fallasPremiadasPorDia);
        minutosParaAvisoDeDescanso = Mathf.Max(0f, minutosParaAvisoDeDescanso);
    }
}
