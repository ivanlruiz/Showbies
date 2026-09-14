using System.Collections.Generic;
using UnityEngine;

// El "jugo" de las escenas de juego: lo que hace que cada golpe, muerte y
// explosion se sienta. Vive en el prefab Assets/Prefabs/Jugo/Efectos.prefab,
// puesto en ShowBies1, WaveMode y Tutorial, con los sonidos, las chispas, el
// material del destello, los numeros de daño y la musica de la partida.
//
// Los que producen los eventos (EnemyController, Granade, PlayerHealth,
// PlayerController, GunController, WaveManager) llaman a los metodos static,
// que no hacen nada si la escena no tiene Efectos: el juego anda igual, plano.
public class Efectos : MonoBehaviour
{
    public static Efectos instance;

    [Header("Sonidos")]
    public AudioClip golpe;
    public AudioClip muerte;
    public AudioClip explosion;
    public AudioClip danioJugador;
    public AudioClip caja;
    public AudioClip cartelOleada;

    [Header("Chispas (un solo sistema por escena, con Emit)")]
    public ParticleSystem chispasPrefab;
    public int chispasPorGolpe = 3;
    public int chispasPorMuerte = 12;
    public int chispasPorMuerteGrande = 40;
    public int chispasPorExplosion = 60;
    public int chispasPorDisparo = 2;
    public int chispasPorCaja = 25;

    [Header("Destello de golpe")]
    public Material materialDestello;
    public float duracionDestello = 0.07f;

    [Header("Numeros de daño")]
    public NumeroFlotante numeroPrefab;
    public int maxNumeros = 40;

    [Header("Temblor de camara (trauma de 0 a 1 que suma cada evento)")]
    public float temblorMuerte = 0.06f;
    public float temblorMuerteGrande = 0.45f;
    public float temblorJefe = 1f;
    public float temblorExplosion = 0.7f;
    public float temblorDanio = 0.2f;
    public float separacionDanioVisual = 0.4f;   // segundos minimos entre dos temblores/destellos rojos por daño

    [Header("Pausa de impacto (camara lenta de un instante)")]
    public float escalaDeTiempoEnPausa = 0.05f;
    public float pausaMuerteGrande = 0.05f;
    public float pausaJefe = 0.18f;
    public float pausaExplosion = 0.04f;
    public int vidaParaMuerteGrande = 20;    // vida base desde la que una muerte es grande: el tanque
    public int vidaParaJefe = 100;

    private readonly Stack<NumeroFlotante> numerosLibres = new Stack<NumeroFlotante>();
    private int numerosCreados;
    private ParticleSystem chispas;
    private float pausaHasta;               // en tiempo sin escalar
    private bool enPausaDeImpacto;
    private float proximoDanioVisual;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetearEstadoCompartido()
    {
        instance = null;
    }

    public static Material MaterialDestello { get { return instance != null ? instance.materialDestello : null; } }
    public static float DuracionDestello { get { return instance != null ? instance.duracionDestello : 0f; } }

    private void Awake()
    {
        instance = this;
        if (chispasPrefab != null) chispas = Instantiate(chispasPrefab, transform);
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;

        // timeScale es global: si la escena se descarga en plena pausa de impacto,
        // la siguiente arrancaria en camara lenta.
        if (enPausaDeImpacto && !MenuPausa.Pausado) Time.timeScale = 1f;
    }

    private void Update()
    {
        if (!enPausaDeImpacto || Time.unscaledTime < pausaHasta) return;

        enPausaDeImpacto = false;
        // Si en el medio se abrio el menu de pausa, el tiempo lo maneja el menu.
        if (!MenuPausa.Pausado) Time.timeScale = 1f;
    }

    // Cualquier daño a un zombi, mate o no: el numero, unas chispas y un tic.
    public static void Golpe(Vector3 punto, int daño)
    {
        var e = instance;
        if (e == null) return;

        e.Emitir(punto, e.chispasPorGolpe);
        e.MostrarNumero(punto, daño);
        Sonidos.Tocar(e.golpe, 0.35f, 1f, 0.15f, 0.05f);
    }

    // Un zombi que muere. Las muertes grandes (tanque y jefe) sacuden la camara y
    // frenan el tiempo un instante; las de los chicos apenas suman temblor, porque
    // mueren de a muchos por segundo.
    public static void Muerte(Vector3 punto, int vidaBase)
    {
        ContadorCombo.RegistrarMuerte();

        var e = instance;
        if (e == null) return;

        bool jefe = vidaBase >= e.vidaParaJefe;
        bool grande = vidaBase >= e.vidaParaMuerteGrande;

        e.Emitir(punto, grande ? e.chispasPorMuerteGrande : e.chispasPorMuerte);
        Sonidos.Tocar(e.muerte, grande ? 1f : 0.55f, grande ? 0.7f : 1f, 0.2f, 0.04f);
        CamaraJugador.Temblar(jefe ? e.temblorJefe : grande ? e.temblorMuerteGrande : e.temblorMuerte);
        if (jefe) e.PausaDeImpacto(e.pausaJefe);
        else if (grande) e.PausaDeImpacto(e.pausaMuerteGrande);
    }

    public static void Explosion(Vector3 punto)
    {
        var e = instance;
        if (e == null) return;

        e.Emitir(punto, e.chispasPorExplosion);
        Sonidos.Tocar(e.explosion, 1f);
        CamaraJugador.Temblar(e.temblorExplosion);
        e.PausaDeImpacto(e.pausaExplosion);
    }

    public static void DanioJugador()
    {
        var e = instance;
        if (e == null) return;

        // Rodeado, lo golpean varios zombis por segundo: sin separacion, el
        // temblor y el borde rojo quedaban prendidos todo el tiempo.
        if (Time.unscaledTime < e.proximoDanioVisual) return;
        e.proximoDanioVisual = Time.unscaledTime + e.separacionDanioVisual;

        CamaraJugador.Temblar(e.temblorDanio);
        VinetaDanio.Pulso();
        Sonidos.Tocar(e.danioJugador, 0.8f, 1f, 0.1f, 0.12f);
    }

    public static void Caja(Vector3 punto)
    {
        var e = instance;
        if (e == null) return;

        e.Emitir(punto, e.chispasPorCaja);
        Sonidos.Tocar(e.caja, 0.9f, 1f, 0.1f, 0.05f);
    }

    public static void Disparo(Vector3 punto)
    {
        var e = instance;
        if (e == null) return;

        e.Emitir(punto, e.chispasPorDisparo);
    }

    public static void CartelOleada()
    {
        var e = instance;
        if (e == null) return;

        Sonidos.Tocar(e.cartelOleada, 0.8f);
    }

    private void Emitir(Vector3 punto, int cantidad)
    {
        if (chispas == null || cantidad <= 0) return;

        var parametros = new ParticleSystem.EmitParams { position = punto, applyShapeToPosition = true };
        chispas.Emit(parametros, cantidad);
    }

    private void PausaDeImpacto(float segundos)
    {
        if (MenuPausa.Pausado || segundos <= 0f) return;

        pausaHasta = Mathf.Max(pausaHasta, Time.unscaledTime + segundos);
        enPausaDeImpacto = true;
        Time.timeScale = escalaDeTiempoEnPausa;
    }

    // Los numeros salen de un pool con techo: si ya hay maxNumeros en pantalla,
    // el golpe nuevo no muestra numero en vez de crear otro.
    private void MostrarNumero(Vector3 punto, int valor)
    {
        if (numeroPrefab == null) return;

        NumeroFlotante numero = numerosLibres.Count > 0 ? numerosLibres.Pop() : null;
        if (numero == null)
        {
            if (numerosCreados >= maxNumeros) return;
            numerosCreados++;
            numero = Instantiate(numeroPrefab, transform);
            numero.alTerminar = numerosLibres.Push;
        }
        numero.Mostrar(punto, valor);
    }
}
