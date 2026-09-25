using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class PlayerHealth : MonoBehaviour
{
    
    public static PlayerHealth instance;

    

    public int health;
    public int maxHealth = 80;
    public int curaPorPickup = 40;
    public TMP_Text healthTMP;

    private bool estaMuerto;

    // Hasta cuándo no recibe daño. Al revivir con un video vuelve en el mismo
    // lugar donde lo mataron: sin unos segundos de gracia, el zombi que estaba
    // pegado lo mata de nuevo en el acto.
    private float invulnerableHasta;

    // Un solo revivir por partida: si no, con un video cada vez la partida no
    // termina nunca.
    private bool yaRevivio;
    private int ultimaVidaMostrada = int.MinValue;

    // La cura de las cajas escala con la mejora de vida, así una caja sigue
    // valiendo la misma fracción de la barra. Lo fija AplicarMejoras.
    private float multiplicadorCura = 1f;

    // El daño de los zombis es float (escala por oleada) y la vida es int: lo que
    // no llega a un punto entero queda acá y se suma al golpe siguiente.
    private float danoPendiente;

    // Cuándo empezó la partida, en tiempo escalado: así la pausa no cuenta como
    // jugado. Lo mira la oferta de duplicar, que no premia una partida de dos
    // segundos.
    private float empezoEn;

    public float MultiplicadorCura { get { return multiplicadorCura; } }
    public int CuraPorCaja { get { return Mathf.RoundToInt(curaPorPickup * multiplicadorCura); } }

    // Update is called once per frame
    void Update()
    {
        // Kill-Z. Si los zombis empujan al jugador fuera del mapa, cae al vacio
        // para siempre sin morir: softlock. Cuenta como muerte normal.
        if (!estaMuerto && transform.position.y < -20f)
        {
            // Directo, sin TakeDamage: caer al vacio no se revive (el revivir deja al jugador
            // en el mismo lugar, 20 m bajo el piso, y volveria a morir con el video ya
            // gastado), y durante la gracia TakeDamage no hace nada y el jugador caeria para
            // siempre.
            health = 0;
            estaMuerto = true;
            Terminar();
            return;
        }

        // Solo al cambiar: el ToString por frame es una alocacion por frame.
        if (health != ultimaVidaMostrada)
        {
            ultimaVidaMostrada = health;
            healthTMP.text = health.ToString();
            // El color dice cómo estás sin tener que leer el número: en medio de una
            // horda no hay tiempo de comparar 34 contra 80.
            healthTMP.color = ColorDeVida(maxHealth > 0 ? (float)health / maxHealth : 0f);
        }
    }

    private void Awake()
    {
        instance = this;
        empezoEn = Time.time;
        RecordNuevo = false;
        Progreso.EmpezarPartida();
    }

    // La partida supero el record de su modo (estrictamente: empatarlo no cuenta). Lo
    // lee la derrota (Score.HuboRecordNuevo), que antes comparaba el puntaje con el
    // record ya guardado y un empate salia como NUEVO RECORD.
    public static bool RecordNuevo { get; private set; }

    public bool EstaMuerto
    {
        get { return estaMuerto; }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetearRecordNuevo()
    {
        RecordNuevo = false;
    }

    // Verde de 60 para arriba, amarillo hasta 30 y rojo abajo de eso: los del neón
    // (ConstructorUI). Estatico para probarlo sin escena.
    public static Color ColorDeVida(float fraccion)
    {
        if (fraccion > 0.6f) return ConstructorUI.Verde;
        if (fraccion > 0.3f) return ConstructorUI.Amarillo;
        return ConstructorUI.Rojo;
    }

    // La clave del record de un modo, por el buildIndex de su escena. La pantalla
    // de derrota la arma con "UltimoModo". La clave vieja "HighScore", sin modo,
    // ya no la lee nadie.
    public static string ClaveRecord(int modo)
    {
        return "HighScore_" + modo;
    }

    // Lo llama AplicarMejoras al empezar la partida: la vida arranca llena con el
    // máximo mejorado.
    public void FijarVidaMaxima(int vidaMaxima, float multiplicadorCura)
    {
        maxHealth = Mathf.Max(1, vidaMaxima);
        health = maxHealth;
        this.multiplicadorCura = Mathf.Max(0f, multiplicadorCura);
        danoPendiente = 0f;
    }

    // Suma "cantidad" a lo pendiente y devuelve la parte entera, que es lo que se
    // descuenta de la vida; el resto queda para el golpe siguiente. Redondear
    // cada golpe por separado haría que un daño de 1,3 fuera siempre 1 y el
    // escalado del daño por oleada no se notara hasta el 1,5. Estático para
    // probarlo sin escena.
    public static int AcumularDano(ref float pendiente, float cantidad)
    {
        if (!(cantidad > 0f)) return 0;

        pendiente += cantidad;
        // El 0,0001 absorbe el error de float: que 0,3 + 0,7 dé 0,9999 y no 1.
        int entero = Mathf.FloorToInt(pendiente + 0.0001f);
        if (entero <= 0) return 0;

        pendiente = Mathf.Max(0f, pendiente - entero);
        return entero;
    }

    public bool YaRevivio { get { return yaRevivio; } }
    public float SegundosDePartida { get { return Time.time - empezoEn; } }

    // Los golpes que le sacaron vida en la partida. WaveManager lo mira al empezar y al
    // terminar cada oleada: si no cambio, la oleada salio sin un rasguño (logro INTOCABLE).
    public int GolpesRecibidos { get; private set; }

    public void TakeDamage(float amount)
    {
        // Varios zombis pegando en el mismo paso de fisica llamaban a esto varias
        // veces con la vida ya en cero, y el bloque de muerte corria de nuevo.
        if (estaMuerto || Time.time < invulnerableHasta) return;

        int dano = AcumularDano(ref danoPendiente, amount);
        if (dano <= 0) return;

        // Sin bajar de cero: el golpe que mata suele sacar mas de lo que quedaba (la
        // carga del jefe pega x2,5), y con la oferta de revivir la partida no termina:
        // el HUD mostraba "-17" en rojo durante los diez segundos del ¡HAS MUERTO!.
        health = Mathf.Max(0, health - dano);
        GolpesRecibidos++;
        if (health > 0) Efectos.DanioJugador();
        if (health > 0) return;

        estaMuerto = true;

        // Se desploma ya, tambien si despues sale la oferta de revivir (que congela el
        // juego: la caida va en tiempo sin escalar). Revivir lo levanta.
        var control = GetComponent<PlayerController>();
        if (control != null) control.Caer();

        // Antes de dar la partida por terminada: si hay un video para revivir, el
        // juego queda congelado con la oferta en pantalla y la derrota espera. Es
        // la oferta la que después llama a Revivir o a Terminar.
        // El record primero: si Android mata la app durante el video de revivir, Terminar
        // no llega a correr y la partida record se perderia.
        GuardarRecord();
        if (!yaRevivio && OfertaDeRevivir.Ofrecer(this)) return;

        Terminar();
    }

    // La muerte de verdad: guarda el récord, cierra la partida y muestra la derrota.
    public void Terminar()
    {
        // Una sola vez: el jugador ya no se destruye (ver abajo) y una segunda llamada
        // contaria la partida dos veces.
        if (terminada) return;
        terminada = true;

        // Un record por modo: los puntos del modo libre y los de las oleadas
        // no se comparan, y antes compartian una sola clave.
        int modo = SceneManager.GetActiveScene().buildIndex;
        PlayerPrefs.SetInt("Score", Puntaje.instance.contadorKill);
        GuardarRecord();

        // Para que "Retry" vuelva al modo que se estaba jugando y no siempre
        // al primero. Sin esto, morir en WaveMode te reiniciaba en ShowBies1.
        PlayerPrefs.SetInt("UltimoModo", modo);

        // Sin Save() esto queda sólo en memoria hasta que el juego cierre bien.
        PlayerPrefs.Save();
        Progreso.TerminarPartida(SegundosDePartida);
        if (modo == TiendaMejoras.EscenaOleadas) Progreso.OlvidarOleadaEnCurso();
        Progreso.Guardar();

        // La derrota va encima de la partida, que sigue andando y se pone gris, sin
        // cambiar de escena. El jugador NO se destruye, aunque antes si: el WaveManager
        // espera mientras el jugador este muerto (por si revive), y destruido
        // PlayerHealth.instance pasa a null, el bucle sale y la oleada se da por
        // completada DESPUES de morir, con su bono y guardada. Queda ahi, muerto y
        // quieto, hasta que se sale de la derrota.
        QuedarseQuieto();
        DerrotaEnLaPartida.Mostrar();
    }

    // Muerto en un mundo que sigue andando: sin control, sin arma y sin que la horda lo
    // arrastre. Con el tiempo congelado no hacia falta; corriendo, seguia caminando con
    // la ultima direccion y, si murio disparando, el arma seguia tirando sola y matando
    // zombis: puntos y monedas despues de morir.
    private void QuedarseQuieto()
    {
        var control = GetComponent<PlayerController>();
        if (control != null)
        {
            control.Soltar();
            if (control.theGun != null) control.theGun.enabled = false;
            control.enabled = false;
        }
        var joysticks = GetComponent<PlayerJS>();
        if (joysticks != null) joysticks.enabled = false;

        var cuerpo = GetComponent<Rigidbody>();
        if (cuerpo != null)
        {
            cuerpo.linearVelocity = Vector3.zero;
            cuerpo.angularVelocity = Vector3.zero;
            cuerpo.isKinematic = true;
        }
    }

    private bool terminada;

    // El record del modo, que solo puede subir. Lo llaman la muerte (antes de ofrecer
    // revivir) y Terminar.
    private void GuardarRecord()
    {
        if (Puntaje.instance == null) return;
        string claveRecord = ClaveRecord(SceneManager.GetActiveScene().buildIndex);
        if (Puntaje.instance.contadorKill > PlayerPrefs.GetInt(claveRecord))
        {
            PlayerPrefs.SetInt(claveRecord, Puntaje.instance.contadorKill);
            PlayerPrefs.Save();
            RecordNuevo = true;
        }
    }

    // Volver a jugar después de un video: vida llena, unos segundos sin recibir
    // daño y la zona despejada. Los zombis de alrededor se van SIN dar puntos ni
    // monedas: si los diera, revivir sería la forma barata de cobrar una pantalla
    // llena de zombis.
    public void Revivir(float radioDespeje, float segundosDeGracia)
    {
        estaMuerto = false;
        yaRevivio = true;
        health = maxHealth;
        danoPendiente = 0f;
        invulnerableHasta = Time.time + Mathf.Max(0f, segundosDeGracia);

        // Se habia desplomado en el golpe que lo mato.
        var control = GetComponent<PlayerController>();
        if (control != null) control.Levantarse();

        // El jefe no se despeja (la oleada lo contaria como muerto), asi que se le corta
        // el ataque: volver con la carga a medio avisar es morir de nuevo sin jugar. La
        // lista de jefes ya la lleva EnemyController, y no incluye a los del pool.
        var jefes = EnemyController.Jefes;
        for (int i = 0; i < jefes.Count; i++)
        {
            var patrones = jefes[i] != null ? jefes[i].GetComponent<JefePatrones>() : null;
            if (patrones != null) patrones.Postergar(Mathf.Max(segundosDeGracia, 2.5f));
        }

        int despejados = EnemyController.DespejarAlrededor(transform.position, radioDespeje);
        Efectos.Explosion(transform.position);
        if (despejados > 0) Efectos.CartelOleada();
    }
    private void OnTriggerEnter(Collider other)
    {
        // Muerto no agarra nada: la partida sigue andando detras de la derrota y una caja
        // que cae encima del cuerpo lo curaria (los eventos de trigger llegan igual a los
        // componentes apagados).
        if (estaMuerto) return;

        // El jugador tiene dos colliders, asi que el mismo pickup dispara este
        // evento dos veces en el mismo paso de fisica, y Destroy es diferido: la
        // cura se aplicaba doble (50+100+100 clampeado a 200 en vez de 150). El
        // SetActive(false) inmediato marca el pickup como ya consumido.
        if (!other.gameObject.activeSelf) return;

        if (other.gameObject.CompareTag("PUVida"))
        {
            other.gameObject.SetActive(false);
            Destroy(other.gameObject);

            // El tope es maxHealth, que fija AplicarMejoras con la mejora de vida.
            // La cura escala con la misma mejora y cura siempre la mitad: 40 sin
            // mejora, 80 con 160 de vida.
            health = Mathf.Min(health + CuraPorCaja, maxHealth);
            Efectos.Caja(other.transform.position);
        }
    }
}
