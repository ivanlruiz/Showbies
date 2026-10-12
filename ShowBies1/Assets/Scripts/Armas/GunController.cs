using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunController : MonoBehaviour
{


    public bool isFiring;
    public PlayerController player;
    public BulletController bala;
    public int velocidadBala;

    // La cadencia de verdad sale de la mejora de cadencia, en tiros por segundo, y
    // la fija AplicarMejoras al empezar la partida. Este campo queda como respaldo.
    [Tooltip("Respaldo para escenas sin AplicarMejoras: segundos entre tiros. Con AplicarMejoras manda la mejora de cadencia.")]
    public float tiempoDisparo;
    private float contadorDisp;

    [Header("Cadencia")]
    public int maxTirosPorFrame = 8;        // a 30 FPS y 108 tiros/s salen 3 o 4 por frame; el tope sólo corta un tirón largo
    public float maxTirosPorSegundo = 120f; // techo con la cadencia al tope y una caja encima

    [Header("Mejora de cadencia")]
    public float duracionMejora = 10f;   // segundos que dura la cadencia de un pickup

    private float tirosPorSegundoBase = -1f;   // -1: nadie la fijó, se usa tiempoDisparo
    private float multiplicadorPickup = 1f;
    private float multiplicadorCadenciaFuria = 1f;
    private float multiplicadorDanoFuria = 1f;
    private float multiplicadorAutomatico = 1f;
    private float danoPorBala = -1f;           // -1: nadie lo fijó, se usa el dañoDar del prefab

    // El auto-apuntado del teléfono tira a la mitad de la cadencia (12/10, de Gemini: tiraba
    // entero y mataba todo solo); apuntar con el joystick de disparo da la cadencia entera.
    public const float CadenciaDelAutomatico = 0.5f;

    public float MultiplicadorAutomatico { get { return multiplicadorAutomatico; } }

    public void FijarAutomatico(bool automatico)
    {
        multiplicadorAutomatico = automatico ? CadenciaDelAutomatico : 1f;
    }
    private float mejoraVenceEn;
    private bool mejoraActiva;

    public AudioSource AudioSource;

    [Header("Sonido")]
    public float intervaloMinimoSonido = 0.04f;   // techo de sonidos de disparo por segundo

    // El disparo suena mas grave cuanto mas pega: el tono de cada tramo de la bala
    // (BulletController.Tramo), del 1 de la base a 0,8 en el violeta.
    public static readonly float[] TonoPorTramo = { 1f, 0.95f, 0.9f, 0.85f, 0.8f };
    private float proximoSonido;

    // Cada arma de la mano suena distinto (1.6.0, etapa 3 de las cartas; ArmaEnLaMano): la pistola
    // con el clip del AudioSource (shot.mp3) y las otras con el suyo, sintetizado por
    // sonidos/disparos.py, de Resources/Disparos. Su tono es el de su arma, y baja con lo que la
    // bala sube por encima de ella (la furia). Las de cola larga dejan mas aire entre sonido y
    // sonido, que a 20 tiros por segundo se apilaban en un rugido.
    public static readonly string[] SonidosDeLasArmas = { null, "Disparos/Revolver", "Disparos/Escopeta", "Disparos/Rifle", "Disparos/Energia" };
    public static readonly float[] IntervaloDeSonidoPorArma = { 0.04f, 0.06f, 0.09f, 0.04f, 0.06f };
    private AudioClip[] sonidosDeLasArmas;

    public AudioClip SonidoDelArma(int tramoDelArma)
    {
        int i = Mathf.Clamp(tramoDelArma, 0, SonidosDeLasArmas.Length - 1);
        if (sonidosDeLasArmas == null)
        {
            sonidosDeLasArmas = new AudioClip[SonidosDeLasArmas.Length];
            for (int k = 1; k < SonidosDeLasArmas.Length; k++)
                sonidosDeLasArmas[k] = Resources.Load<AudioClip>(SonidosDeLasArmas[k]);
        }
        var clip = sonidosDeLasArmas[i];
        if (clip == null && AudioSource != null) clip = AudioSource.clip;
        return clip;
    }


    public Transform firePoint;

    // La boca de la pistola que el muñeco tiene en la mano (ArmaEnLaMano): si esta, las
    // balas y el fogonazo salen de ahi. La direccion sigue siendo la de firePoint, que es
    // hacia donde se apunta; la mano se mueve con la animacion.
    [System.NonSerialized] public Transform boca;

    // De donde salio la ultima bala. Para las pruebas.
    public Vector3 UltimaSalida { get; private set; }

    // Cuántas balas salieron en toda la partida. Lo usan el medidor y las pruebas
    // para comparar la cadencia medida con la esperada.
    public int TirosDisparados { get; private set; }

    public float TirosPorSegundoBase
    {
        get { return tirosPorSegundoBase > 0f ? tirosPorSegundoBase : 1f / Mathf.Max(0.001f, tiempoDisparo); }
    }

    // 1 sin caja.
    public float MultiplicadorCadencia { get { return multiplicadorPickup; } }

    // 1 y 1 sin furia.
    public float MultiplicadorCadenciaFuria { get { return multiplicadorCadenciaFuria; } }
    public float MultiplicadorDanoFuria { get { return multiplicadorDanoFuria; } }
    // La furia prendida (la luz del arma de la mano parpadea en rojo).
    public bool EnFuria { get { return multiplicadorDanoFuria > 1f || multiplicadorCadenciaFuria > 1f; } }

    // La cadencia vigente: la base por la caja, por la furia y por las cartas de la partida
    // (PoderDeLaPartida), con el techo, y la mitad con el auto-apuntado.
    public float TirosPorSegundo
    {
        get { return Mathf.Min(TirosPorSegundoBase * multiplicadorPickup * multiplicadorCadenciaFuria * PoderDeLaPartida.MultiplicadorCadencia, Mathf.Max(1f, maxTirosPorSegundo)) * multiplicadorAutomatico; }
    }

    // El daño de la mejora, sin la furia: el que muestran el medidor y las pruebas.
    public float DanoPorBala
    {
        get { return danoPorBala >= 0f ? danoPorBala : (bala != null ? bala.dañoDar : 0); }
    }

    // Golpes criticos: cada bala sortea al salir y, si toca, pega multiplicadorCritico
    // veces. La probabilidad la fija la mejora (0 sin comprarla).
    public float multiplicadorCritico = 2f;
    private float probabilidadCritico;

    public float ProbabilidadCritico { get { return probabilidadCritico; } }

    // La de la mejora mas la de las cartas de la partida: la que sortea cada bala.
    public float ProbabilidadCriticoEnLaPartida { get { return Mathf.Clamp01(probabilidadCritico + PoderDeLaPartida.CriticoExtra); } }

    public void FijarProbabilidadCritico(float probabilidad)
    {
        probabilidadCritico = Mathf.Clamp01(probabilidad);
    }

    // Estatico para probarlo sin azar. Random.value puede dar 1 exacto, asi que el
    // 100 % se trata aparte: con la mejora al tope todas son criticas.
    public static bool EsCritico(float probabilidad, float sorteo)
    {
        return probabilidad >= 1f || sorteo < probabilidad;
    }

    // Lo que lleva cada bala que sale ahora, con la furia y las cartas de la partida.
    public float DanoPorTiro
    {
        get { return DanoPorBala * multiplicadorDanoFuria * PoderDeLaPartida.MultiplicadorDano; }
    }

    // El arma de la mano cambia con el daño (1.6.0, etapa 3 de las cartas): el de la mejora por
    // las cartas de daño, sin la furia, que es un pico de 6 s (las balas si suben un tramo con
    // ella). Es el poder que el jugador tiene de verdad, y no cambia de arma en cada furia.
    public float DanoSinFuria { get { return DanoPorBala * PoderDeLaPartida.MultiplicadorDano; } }
    public int TramoDelArma { get { return BulletController.Tramo(DanoSinFuria); } }

    // Start is called before the first frame update
    void Start()
    {

        AudioSource = GetComponent<AudioSource>();
    }

    public void FijarTirosPorSegundo(float tirosPorSegundo)
    {
        tirosPorSegundoBase = Mathf.Max(0.1f, tirosPorSegundo);
    }

    // El daño va en cada bala al dispararla y no en el prefab: "bala" es el asset,
    // y escribirle encima cambiaría el prefab (en el editor, para siempre).
    public void FijarDanoPorBala(float dano)
    {
        danoPorBala = Mathf.Max(0f, dano);
    }

    // Las cajas multiplican la cadencia de la mejora en vez de pisarla: antes
    // bajaban tiempoDisparo a un valor fijo, y con la cadencia mejorada una caja
    // podía dejarte más lento que sin ella. No acumula: la última caja pisa a la
    // anterior y reinicia el reloj.
    public void PotenciarCadencia(float multiplicador)
    {
        multiplicadorPickup = Mathf.Max(1f, multiplicador);
        mejoraActiva = true;
        mejoraVenceEn = Time.time + duracionMejora;
    }

    // La furia multiplica encima de la mejora y de la caja, así una caja que llega
    // durante la furia no la pisa. Con 1 y 1 se apaga. La prende y la apaga Furia,
    // que lleva el reloj.
    public void FijarFuria(float cadencia, float dano)
    {
        multiplicadorCadenciaFuria = Mathf.Max(1f, cadencia);
        multiplicadorDanoFuria = Mathf.Max(1f, dano);
    }

    // Para el indicador del HUD.
    public bool MejoraActiva { get { return mejoraActiva; } }
    public float MejoraRestante { get { return mejoraActiva ? Mathf.Max(0f, mejoraVenceEn - Time.time) : 0f; } }

    // Cuántos tiros tocan en este frame. "contador" es el tiempo que falta para el
    // próximo tiro: puede quedar negativo si el frame fue más largo que el
    // intervalo, y esa deuda se paga con varios tiros en el mismo frame. Antes
    // salía como mucho una bala por frame: 0,04 s daba 20 tiros/s a 60 FPS y 15 a
    // 30 FPS, y las cajas no pasaban de 60 por segundo. "atrasoPrimero" es cuánto
    // tarde sale el primero, para adelantar cada bala lo que le corresponde.
    // Estático y sin estado para probarlo sin escena.
    public static int TirosDelFrame(ref float contador, float deltaTime, float intervalo, int maximo, out float atrasoPrimero)
    {
        intervalo = Mathf.Max(1e-4f, intervalo);
        maximo = Mathf.Max(1, maximo);

        atrasoPrimero = contador < 0f ? -contador : 0f;

        int n = 0;
        while (contador <= 0f && n < maximo)
        {
            n++;
            contador += intervalo;
        }

        // Si el tope cortó, la deuda que queda se perdona: si no, después de un
        // tirón el arma seguiría escupiendo ráfagas durante varios frames. Las
        // balas que salen son las últimas de la deuda, no las más viejas: el
        // atraso se descuenta de los tiros perdonados (enteros, así se conserva la
        // fase). Sin esto, tras un tirón de 0,33 s la ráfaga aparecía varios
        // metros adelante y salteaba a los zombis pegados al jugador.
        if (contador < 0f)
        {
            float perdonados = Mathf.Floor(-contador / intervalo) + 1f;
            atrasoPrimero = Mathf.Max(0f, atrasoPrimero - perdonados * intervalo);
            contador = 0f;
        }

        contador -= deltaTime;
        return n;
    }

    // Lo que queda del intervalo entre tiros en un frame sin disparar: baja hasta 0 y
    // ahi se queda. Asi, despues de una pausa mas larga que el intervalo el primer
    // tiro sale en el acto, pero soltar no recarga el arma. Estatico para probarlo.
    public static float EnfriarSinDisparar(float contador, float deltaTime)
    {
        return Mathf.Max(0f, contador - Mathf.Max(0f, deltaTime));
    }

    // Update is called once per frame
    void Update()
    {
        if (mejoraActiva && Time.time >= mejoraVenceEn)
        {
            mejoraActiva = false;
            multiplicadorPickup = 1f;
        }

        if(isFiring && player.cantBalas>0)
        {
            float intervalo = 1f / TirosPorSegundo;
            float atraso;
            int tiros = Mathf.Min(TirosDelFrame(ref contadorDisp, Time.deltaTime, intervalo, maxTirosPorFrame, out atraso), player.cantBalas);

            for (int i = 0; i < tiros; i++)
            {
                Disparar(Mathf.Max(0f, atraso - i * intervalo));
            }
        }
        else
        {
            // Sin disparar, lo que faltaba para el proximo tiro sigue corriendo hasta
            // 0. Antes volvia a 0 de golpe al soltar, y soltar y volver a tocar el
            // disparo mas rapido que la cadencia tiraba mas balas por segundo que la
            // mejora: con 4 tiros/s, moviendo el joystick a golpecitos salian muchas mas.
            contadorDisp = EnfriarSinDisparar(contadorDisp, Time.deltaTime);
        }


    }

    // Un tiro. "atraso" es cuánto tarde sale respecto de cuándo le tocaba: la bala
    // se adelanta eso en su recorrido, así las balas del mismo frame salen
    // escalonadas en un chorro y no apiladas en la boca del arma.
    private void Disparar(float atraso)
    {
        player.cantBalas--;
        TirosDisparados++;
        Vector3 salida = boca != null ? boca.position : firePoint.position;
        UltimaSalida = salida;

        // PlayOneShot y no Play: Play reinicia el mismo sonido, y a esta
        // cadencia lo cortaba en cada tiro antes de que llegara a oirse. El
        // techo evita apilar decenas de sonidos con la cadencia mejorada, y
        // también varios tiros del mismo frame.
        float probabilidad = ProbabilidadCriticoEnLaPartida;
        bool critico = probabilidad > 0f && EsCritico(probabilidad, Random.value);
        int tramo = BulletController.Tramo(DanoPorTiro);
        int tramoDelArma = Mathf.Min(TramoDelArma, tramo);
        var sonido = AudioSource != null ? SonidoDelArma(tramoDelArma) : null;
        if (Time.time >= proximoSonido && sonido != null)
        {
            AudioSource.pitch = TonoPorTramo[tramo] / TonoPorTramo[tramoDelArma];
            AudioSource.PlayOneShot(sonido);
            proximoSonido = Time.time + intervaloMinimoSonido * IntervaloDeSonidoPorArma[tramoDelArma] / IntervaloDeSonidoPorArma[0];
            Efectos.Disparo(salida, critico ? BulletController.ColorCritico : BulletController.ColoresPorTramo[tramo]);
        }

        // Antes era un Instantiate por disparo. Ahora las balas se reusan.
        //
        // Las cartas de mas balas (1.6.0, etapa 2): el abanico del disparo doble, de a 8° entre
        // bala y bala y centrado en el tiro (con una de mas, las dos van a ±4°), y la de atras.
        // Salen del mismo tiro: no gastan otra bala del cargador (si no, la carta seria un
        // castigo) ni cuentan en TirosDisparados, que es la cadencia que miden el medidor y las
        // pruebas.
        int extra = PoderDeLaPartida.BalasExtraAdelante;
        float abanico = PoderDeLaPartida.AbanicoDelDisparoDoble;
        Quaternion primera = Quaternion.AngleAxis(-extra * 0.5f * abanico, Vector3.up) * firePoint.rotation;
        for (int i = 0; i <= extra; i++)
            SacarBala(salida, Quaternion.AngleAxis(i * abanico, Vector3.up) * primera, atraso, critico);
        for (int i = 0; i < PoderDeLaPartida.BalasAtras; i++)
            SacarBala(salida, Quaternion.AngleAxis(180f, Vector3.up) * firePoint.rotation, atraso, critico);
    }

    // La ultima bala que salio (las pruebas y el abanico).
    public BulletController UltimaBala { get; private set; }

    // Una bala con lo de este tiro: el daño (y el critico), el tramo, y lo que le ponen las cartas
    // de la partida. Por aca salen todas: las del tiro, las de mas de las cartas y las del dron.
    public BulletController SacarBala(Vector3 salida, Quaternion rumbo, float atraso, bool critico)
    {
        float dano = DanoPorTiro;
        BulletController nueva = BulletController.Obtener(bala, salida, rumbo);
        nueva.velocidad = velocidadBala;
        nueva.critico = critico;
        nueva.danoAplicado = critico ? dano * multiplicadorCritico : dano;
        nueva.perforacionesRestantes = PoderDeLaPartida.Perforaciones;
        nueva.fraccionDeExplosion = PoderDeLaPartida.FraccionDeLaExplosion;
        nueva.radioDeExplosion = PoderDeLaPartida.RadioDeLaExplosion;
        nueva.lentitudDelHielo = PoderDeLaPartida.LentitudDelHielo;
        // La quemadura se sortea bala por bala: con una carta, una de cada cuatro.
        float quema = PoderDeLaPartida.ProbabilidadDeQuemadura;
        nueva.quemaduraPorSegundo = quema > 0f && Random.value < quema ? nueva.danoAplicado * PoderDeLaPartida.QuemaduraPorSegundo : 0f;
        nueva.Vestir(dano, critico);
        nueva.Adelantar(atraso);
        UltimaBala = nueva;
        return nueva;
    }

}
