using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Unity.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;

// Graba clips verticales de la partida para TikTok, Shorts y Reels (15/10, pedido de Ivan: que lo
// juegue gente de afuera; que grabar lo decidio Argon, ver publicacion/clips.md). No usa
// ScreenCapture, que necesita la vista Game y se traba con Unity atras: renderiza la camara del
// juego a una textura de 1080 x 1920 en cada cuadro, con el tiempo fijo a 30 cuadros por segundo
// (Time.captureFramerate), asi corre igual con Unity en segundo plano o en -batchmode. La interfaz
// en overlay no sale (y esta armada para horizontal): el texto de cada clip se pone despues, al
// armar el video (Marketing/clips). Los numeros de dano si, que son del mundo. El sonido se graba
// aparte con AudioRenderer, al mismo ritmo que los cuadros.
//
// Cada toma es una partida retomada en una oleada, con las mejoras y las cartas que pide, y un
// guion: por donde camina el heroe, a quien apunta, cuando prende la furia y cuando muere. Antes
// de grabar corre una previa con el heroe inmortal, para que la horda llegue. Los clips 2 y 3
// (17/10) tienen ademas un objetivo, el jefe o el tesoro (Guion): lo sacan o lo ponen, leen el
// estado de JefePatrones para esquivar y llevan su vida a mano (por reflexion: los campos son
// privados del juego, y si alguno cambia de nombre la toma no arranca).
//
// Con el editor abierto y al frente (ShowBies > Clips; con Unity atras el play se traba en el
// primer cuadro), o desde la linea de comandos con el editor cerrado, sin -quit, que cerraria Unity
// antes de entrar en play: al terminar sale solo.
//   Unity.exe -batchmode -projectPath ShowBies1 -executeMethod GrabarClips.Clip1Debil -logFile ...
// La primera version (15/10, en -batchmode) salio "re bugeada" (Ivan): las balas se iban para otro
// lado por el rumbo viejo del cuerpo del heroe, que era un bug del juego (ver GunController.FijarRumbo),
// el blanco a mano podia estar fuera del cuadro o encima del heroe, y el zoom de 1,8 agrandaba de mas
// los numeros, las barras y la onda de energia.
//
// OJO: entrar y salir de play vuelve a serializar ProjectSettings (ver la trampa en CLAUDE.md).
[InitializeOnLoad]
public static class GrabarClips
{
    const string Clave = "ShowBies.GrabarClips";
    const string Banco = "GrabarClips";
    const string Escena = "Assets/Escenas/WaveMode.unity";
    const string Carpeta = "../Builds/clips";
    const int Ancho = 1080, Alto = 1920, Fps = 30;
    // Un banco colgado no puede dejar un Unity vivo en segundo plano.
    const double TopeReal = 900.0;

    [System.Serializable]
    class Toma
    {
        public string nombre;
        public int oleada = 1;
        public bool alTope;                // las mejoras al tope; si no, todas en cero
        public string[] cartas = new string[0];   // "id:veces"
        public float previa = 1.5f;        // segundos de juego antes de grabar, con el heroe inmortal
        public int cuadros = 30;
        public float zoom = 1f;            // cuanto se acerca la camara
        public bool halloween;
        public Vector3 inicio;             // donde lo pone al empezar la previa
        public bool ponerEnInicio;
        public string recorrido = "circulo";   // circulo, quieto, norte
        public float paso = 1f;            // cuanto empuja el joystick de mover (0 a 1)
        public bool apuntarAMano;          // con el joystick de disparo, al mas cercano: cadencia entera y derecho
        public bool quietoEnLaPrevia;      // en la previa ni camina ni dispara: la horda lo rodea
        public float dispararAntes;        // pero empieza a tirar estos segundos antes de grabar
        public float furiaEn = -1f;        // segundos de la toma
        public float morirEn = -1f;        // segundos de la toma; hasta ahi la vida baja sola
        // La vida maxima durante la toma: con la de verdad (80), en la oleada 25 dos zarpazos en el
        // mismo cuadro lo mataban antes de tiempo. Con mas, la vida que baja sola manda.
        public int vidaMaxima = -1;

        // Lo de los clips 2 a 4 (16/10):
        public string[] mejoras = new string[0];   // "id:nivel", encima del tope o del cero
        public bool sombrero;                      // el sombrero de calabaza puesto
        public string objetivo = "";               // "jefe" o "tesoro": a quien persigue, esquiva y le tira
        public bool despejar;                      // para la oleada y saca a los demas (el jefe queda), antes de grabar
        public float tesoroEn = -1f;               // segundos de la toma: saca el zombi del tesoro al norte
        public float granadaEn = -1f;              // tira una granada hacia el objetivo
        public bool furiaAlAturdir;                // el jefe: la furia (y la ventana para matarlo) cuando queda aturdido tras una carga
        public float matarDesde = -1f;             // la vida del objetivo va de llena a cero entre matarDesde
        public float matarEn = -1f;                //   y matarDesde + matarEn; antes queda llena
        public float seguirTrasMatar = -1f;        // segundos que sigue grabando despues de matarlo
    }

    static readonly string[] Mejoras = { "dano_bala", "cadencia", "criticos", "vida_maxima", "granada", "iman", "botin", "furia" };
    static readonly int[] AlTope = { 16, 16, 8, 20, 1, 13, 15, 1 };

    static Toma toma;
    static RenderTexture textura;
    static Texture2D lectura;
    static int cuadro;
    static bool empezo, furiaPrendida, murio;

    // El objetivo de la toma (el jefe o el tesoro) y lo que le pasa.
    static EnemyController objetivo;
    static int numeroDelObjetivo;
    static bool despejado, tesoroSacado, granadaTirada, vioCarga, objetivoMuerto;
    static float ventanaDesde = -1f, muerteDelObjetivo = -1f;

    const BindingFlags Privado = BindingFlags.NonPublic | BindingFlags.Instance;
    static readonly FieldInfo campoVida = typeof(EnemyController).GetField("vidaActual", Privado);
    static readonly FieldInfo campoEstado = typeof(JefePatrones).GetField("estado", Privado);
    static readonly FieldInfo campoDesde = typeof(JefePatrones).GetField("desde", Privado);
    static readonly FieldInfo campoDestino = typeof(JefePatrones).GetField("destinoDelSalto", Privado);
    static readonly FieldInfo campoDireccion = typeof(JefePatrones).GetField("direccion", Privado);
    static readonly FieldInfo campoProximoAtaque = typeof(JefePatrones).GetField("proximoAtaque", Privado);
    static readonly FieldInfo campoTesoro = typeof(WaveManager).GetField("tesoroPrefab", Privado);
    const string PrefabJefe = "Assets/Prefabs/Personajes/ZombiBOSS.prefab";
    static float baseFov = -1f;
    static List<float> sonido;
    static bool grabandoSonido;

    static GrabarClips()
    {
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += AlCambiarDeModo;
    }

    // La prueba: 30 cuadros en la oleada 1, para ver que en -batchmode entra en play y renderiza.
    public static void Probar()
    {
        Arrancar(new Toma { nombre = "prueba" });
    }

    // Clip 1 de Argon, "nivel 1 contra nivel maximo": la toma del debil, que muere acorralado.
    // El zoom, cerca del del juego: con 1,8 (la primera version, 15/10) los numeros de dano, las
    // barras, las monedas y la onda de energia salian gigantes y tapaban al heroe.
    [MenuItem("ShowBies/Clips/Clip 1: el debil")]
    public static void Clip1Debil()
    {
        Arrancar(new Toma
        {
            nombre = "clip1_debil", oleada = 25, previa = 9f, cuadros = 150, zoom = 1.15f, vidaMaxima = 400,
            recorrido = "quieto", morirEn = 3.5f, ponerEnInicio = true, inicio = new Vector3(0f, 0f, -6f)
        });
    }

    // Y la del fuerte: el arma de energia, la furia, las cartas vistosas. En la previa deja que la
    // horda lo rodee en el cruce y despues gira despacio barriendola. Subiendo por la calle (la
    // primera version) se trababa contra un auto y casi no habia zombis en cuadro.
    [MenuItem("ShowBies/Clips/Clip 1: el fuerte")]
    public static void Clip1Fuerte()
    {
        Arrancar(new Toma
        {
            nombre = "clip1_fuerte", oleada = 25, alTope = true, previa = 9f, cuadros = 330, zoom = 1.15f,
            cartas = new[] { "disparo_doble:2", "perforante:2", "explosiva:2", "quemadura:2", "hielo:2", "dron:2", "iman:2" },
            recorrido = "circulo", paso = 0.3f, apuntarAMano = true, furiaEn = 0.1f, quietoEnLaPrevia = true,
            ponerEnInicio = true, inicio = new Vector3(0f, 0f, -6f)
        });
    }

    // La toma del fuerte, mas corta y en otra carpeta, para medir a donde apunta (AnotarPunteria).
    [MenuItem("ShowBies/Clips/Probar la punteria")]
    public static void ProbarPunteria()
    {
        Arrancar(new Toma
        {
            nombre = "prueba_punteria", oleada = 25, alTope = true, previa = 9f, cuadros = 150, zoom = 1.15f,
            cartas = new[] { "disparo_doble:2", "perforante:2", "explosiva:2", "quemadura:2", "hielo:2", "dron:2", "iman:2" },
            recorrido = "circulo", paso = 0.3f, apuntarAMano = true, furiaEn = 0.1f, quietoEnLaPrevia = true,
            ponerEnInicio = true, inicio = new Vector3(0f, 0f, -6f)
        });
    }

    // Clip 2 de Argon, "esperá el error del jefe y contraatacá en furia": la oleada 30 sin la horda, el
    // jefe a 9 m. Salta y carga (ataca mas seguido que en el juego), el heroe esquiva sin tirar y,
    // cuando el jefe queda aturdido despues de una carga, prende la furia y le tira con el rifle, la
    // critica al 100 %, el disparo doble y la explosiva. La vida del jefe baja en 5 s (llevada a mano:
    // con su vida de verdad de la oleada 30 no se muere en un clip), revienta y el iman junta las monedas.
    [MenuItem("ShowBies/Clips/Clip 2: el jefe")]
    public static void Clip2Jefe()
    {
        Arrancar(new Toma
        {
            nombre = "clip2_jefe", oleada = 30, alTope = true, mejoras = new[] { "dano_bala:10" },
            cartas = new[] { "disparo_doble:1", "explosiva:2", "iman:2" },
            previa = 5f, cuadros = 690, zoom = 0.8f, vidaMaxima = 3000,
            ponerEnInicio = true, inicio = new Vector3(0f, 0f, 6f),
            objetivo = "jefe", recorrido = "objetivo", paso = 1f, despejar = true,
            furiaAlAturdir = true, matarEn = 5f, seguirTrasMatar = 3.5f
        });
    }

    // Clip 3, "la codicia mata": la oleada 24 en la ciudad, el zombi del tesoro sale al norte y el
    // heroe lo persigue entre la horda con el rifle, la perforante y el hielo; una granada, la furia,
    // y lo revienta antes de que se escape. Su vida baja de a poco (llevada a mano, como la del jefe).
    // Por la calle del medio, de sur a norte, y en 7 s: mas largo, el tesoro (a 7 m/s) llegaba a los
    // edificios de las afueras y el heroe se trababa contra uno. Con el zoom de cerca: es chico.
    [MenuItem("ShowBies/Clips/Clip 3: el tesoro")]
    public static void Clip3Tesoro()
    {
        Arrancar(new Toma
        {
            nombre = "clip3_tesoro", oleada = 24, alTope = true, mejoras = new[] { "dano_bala:10", "criticos:3" },
            cartas = new[] { "perforante:1", "hielo:2", "iman:2" },
            previa = 10f, cuadros = 450, zoom = 1.35f,
            ponerEnInicio = true, inicio = new Vector3(0f, 0f, -22f),
            objetivo = "tesoro", recorrido = "objetivo", paso = 0.85f, tesoroEn = 0.2f, quietoEnLaPrevia = true,
            apuntarAMano = true, granadaEn = 3f, furiaEn = 4f, matarDesde = 0.8f, matarEn = 6.2f, seguirTrasMatar = 4f
        });
    }

    // Clip 4, "Halloween: la escalada de armas": cuatro cortes con el sombrero de calabaza, sin
    // criticos (la bala critica es blanca y taparia el color de cada arma). La horda lo rodea en la
    // previa y en la toma la barre. El ultimo es mas largo: sus 3 s finales son el fondo del cierre.
    // La previa es larga: en la oleada 9 los zombis nacen en los bordes y con 7 s no llegaban. Y
    // empieza a tirar un poco antes de grabar: con la horda encima, al heroe no se lo veia.
    [MenuItem("ShowBies/Clips/Clip 4: revolver")]
    public static void Clip4Revolver()
    {
        Arrancar(Clip4("clip4_revolver", 9, 2, 90, true, new[] { "hielo:1" }, -1f));
    }

    [MenuItem("ShowBies/Clips/Clip 4: escopeta")]
    public static void Clip4Escopeta()
    {
        // En el cementerio, donde arranca la escena: en (0, 0, -6) puede haber una tumba.
        Arrancar(Clip4("clip4_escopeta", 16, 5, 90, false, new[] { "disparo_doble:1", "escudo:1" }, -1f));
    }

    [MenuItem("ShowBies/Clips/Clip 4: rifle")]
    public static void Clip4Rifle()
    {
        Arrancar(Clip4("clip4_rifle", 24, 10, 90, true, new[] { "hacia_atras:1", "perforante:1" }, -1f));
    }

    [MenuItem("ShowBies/Clips/Clip 4: energia")]
    public static void Clip4Energia()
    {
        Arrancar(Clip4("clip4_energia", 28, 16, 180, true, new[] { "dron:2", "explosiva:2" }, 0.1f));
    }

    static Toma Clip4(string nombre, int oleada, int dano, int cuadros, bool enInicio, string[] cartas, float furiaEn)
    {
        return new Toma
        {
            nombre = nombre, oleada = oleada, alTope = true, mejoras = new[] { "dano_bala:" + dano, "criticos:0" },
            cartas = cartas, previa = 14f, cuadros = cuadros, zoom = 1.4f, halloween = true, sombrero = true,
            ponerEnInicio = enInicio, inicio = new Vector3(0f, 0f, -6f),
            recorrido = "circulo", paso = 0.25f, apuntarAMano = true, quietoEnLaPrevia = true, dispararAntes = 4f, furiaEn = furiaEn
        };
    }

    static void Arrancar(Toma nueva)
    {
        if (campoVida == null || campoEstado == null || campoDesde == null || campoDestino == null
            || campoDireccion == null || campoProximoAtaque == null || campoTesoro == null)
        {
            Debug.LogError("GrabarClips: cambio el nombre de un campo privado que lee el guion");
            Salir(1);
            return;
        }
        if (!RespaldoDelBanco.PuedeArrancar(Banco)) { Salir(1); return; }
        RespaldoDelBanco.Guardar(Banco);
        PruebaDisparo.ModoTelefono(Clave);
        PlayerSettings.runInBackground = true;
        SessionState.SetBool(Clave + ".forzarHalloween", EditorPrefs.GetBool(EventoHalloween.ClaveForzarEnElEditor, false));
        SessionState.SetBool(Clave + ".apagarHalloween", EditorPrefs.GetBool(EventoHalloween.ClaveApagarEnElEditor, false));
        EditorPrefs.SetBool(EventoHalloween.ClaveForzarEnElEditor, nueva.halloween);
        EditorPrefs.SetBool(EventoHalloween.ClaveApagarEnElEditor, !nueva.halloween);

        for (int i = 0; i < Mejoras.Length; i++) Progreso.DepurarFijarNivel(Mejoras[i], nueva.alTope ? AlTope[i] : 0);
        foreach (var m in nueva.mejoras)
        {
            var partes = m.Split(':');
            Progreso.DepurarFijarNivel(partes[0], int.Parse(partes[1]));
        }
        if (nueva.sombrero && Progreso.Halloween != null)
        {
            Progreso.Halloween.sombrero = true;
            EventoHalloween.PonerSombrero(true);
        }
        var poder = new EstadoPoder { semilla = 7, poder = nueva.cartas.Length * 2 };
        foreach (var c in nueva.cartas)
        {
            var partes = c.Split(':');
            poder.cartas.Add(new CartaElegida { id = partes[0], veces = partes.Length > 1 ? int.Parse(partes[1]) : 1 });
        }
        Progreso.GuardarOleadaEnCurso(nueva.oleada, 0);
        Progreso.GuardarPoderEnCurso(poder);
        Progreso.Guardar();

        string carpeta = Path.GetFullPath(Path.Combine(Carpeta, nueva.nombre));
        if (Directory.Exists(carpeta)) Directory.Delete(carpeta, true);
        Directory.CreateDirectory(carpeta);

        Debug.Log("GrabarClips: " + nueva.nombre + ", oleada " + nueva.oleada + ", " + nueva.cuadros + " cuadros; target "
                  + EditorUserBuildSettings.activeBuildTarget + ", batch " + Application.isBatchMode);
        EditorSceneManager.OpenScene(Escena);
        SessionState.SetString(Clave + ".toma", JsonUtility.ToJson(nueva));
        SessionState.SetFloat(Clave + ".inicio", (float)EditorApplication.timeSinceStartup);
        SessionState.SetBool(Clave + ".salir", Application.isBatchMode);
        SessionState.SetBool(Clave, true);
        empezo = false;
        EditorApplication.EnterPlaymode();
    }

    static void Tick()
    {
        if (!SessionState.GetBool(Clave, false)) return;
        if (EditorApplication.timeSinceStartup - SessionState.GetFloat(Clave + ".inicio", 0f) > TopeReal)
        {
            Terminar("se paso del tope de " + TopeReal + " s reales");
            return;
        }
        if (!EditorApplication.isPlaying) return;
        if (!RespaldoDelBanco.SigueArmado(Banco, Clave)) return;

        try
        {
            Avanzar();
        }
        catch (System.Exception e)
        {
            Terminar("excepcion: " + e);
        }
    }

    static void Avanzar()
    {
        if (!empezo)
        {
            empezo = true;
            toma = JsonUtility.FromJson<Toma>(SessionState.GetString(Clave + ".toma", "{}"));
            cuadro = 0;
            furiaPrendida = murio = false;
            blancoAMano = null;
            objetivo = null;
            numeroDelObjetivo = 0;
            despejado = tesoroSacado = granadaTirada = vioCarga = objetivoMuerto = jefePuesto = false;
            ventanaDesde = muerteDelObjetivo = -1f;
            pasoDelGuion = Vector2.zero;
            marcaDelTiempo = destrabarHasta = -1f;
            subidaMaxima = 0f;
            cuadroDeLaMedida = 0;
            baseFov = -1f;
            grabandoSonido = false;
            sonido = new List<float>();
            Time.captureFramerate = Fps;
            Debug.Log("GrabarClips: en play; EsMovil " + Plataforma.EsMovil + ", Halloween " + EventoHalloween.Activo);
        }

        var vida = PlayerHealth.instance;
        if (vida == null || EventSystem.current == null) return;
        var joysticks = vida.GetComponent<PlayerJS>();
        var cuerpo = vida.GetComponent<Rigidbody>();
        if (joysticks == null || cuerpo == null) return;

        float t = Time.timeSinceLevelLoad - toma.previa;   // segundos de la toma (negativo en la previa)
        bool quieto = toma.quietoEnLaPrevia && t < -toma.dispararAntes;
        if (toma.objetivo != "") Guion(vida, joysticks, t);
        PlayerJS.FijarAutoApuntar(!quieto && !SinDisparar);

        // Al principio de la previa, al lugar de inicio.
        if (toma.ponerEnInicio && Time.timeSinceLevelLoad < 0.5f)
        {
            var donde = new Vector3(toma.inicio.x, cuerpo.position.y, toma.inicio.z);
            cuerpo.position = donde;
            cuerpo.linearVelocity = Vector3.zero;
            vida.transform.position = donde;
        }

        // La vida: inmortal en la previa; en la toma, si tiene que morir, baja hasta un poco antes
        // (con el gris de poca vida) y en el momento justo, el golpe final.
        if (toma.vidaMaxima > 0 && vida.maxHealth != toma.vidaMaxima) vida.maxHealth = toma.vidaMaxima;
        // La derrota corre la camara para dejar el cuerpo a un costado de la pantalla horizontal; en
        // vertical lo sacaba de cuadro.
        if (vida.EstaMuerto) CamaraJugador.MostrarElCuerpo(false);
        if (!vida.EstaMuerto)
        {
            if (t < 0f || toma.morirEn < 0f) vida.health = vida.maxHealth;
            else if (t < toma.morirEn)
            {
                float hasta = Mathf.Clamp01(t / Mathf.Max(0.1f, toma.morirEn - 0.2f));
                vida.health = Mathf.Max(1, Mathf.RoundToInt(Mathf.Lerp(vida.maxHealth, vida.maxHealth * 0.15f, hasta)));
            }
            else if (!murio)
            {
                murio = true;
                vida.TakeDamage(vida.maxHealth * 100f);
            }
        }

        if (!vida.EstaMuerto && quieto)
        {
            if (joysticks.moveJoystick != null) PruebaDisparo.Soltar(joysticks.moveJoystick);
            if (joysticks.lookJoystick != null) PruebaDisparo.Soltar(joysticks.lookJoystick);
        }
        else if (!vida.EstaMuerto)
        {
            Mover(joysticks, t);
            Apuntar(joysticks, vida.transform.position);
            if (toma.furiaEn >= 0f && t >= toma.furiaEn && !furiaPrendida)
            {
                furiaPrendida = true;
                var furia = vida.GetComponent<Furia>();
                Debug.Log("GrabarClips: furia " + (furia != null && furia.Activar()));
            }
        }

        var camara = Camera.main;
        if (camara == null) { Terminar("no hay camara principal"); return; }
        if (baseFov < 0f) baseFov = camara.fieldOfView;
        camara.fieldOfView = 2f * Mathf.Atan(Mathf.Tan(baseFov * 0.5f * Mathf.Deg2Rad) / Mathf.Max(0.1f, toma.zoom)) * Mathf.Rad2Deg;
        // En vertical tambien para el juego, no solo para la foto: lo que decide si algo se ve
        // (donde nacen los zombis, si el jefe esta en pantalla para atacar, a quien apunta el
        // automatico) mira la camara principal, que con la vista Game es horizontal. Con eso, los
        // zombis nacian "fuera de la vista" adentro del cuadro vertical.
        camara.aspect = Ancho / (float)Alto;

        MedirSubidos(t);
        if (t < 0f) return;

        if (!grabandoSonido)
        {
            grabandoSonido = true;
            AudioRenderer.Start();
        }
        GrabarSonido();
        Grabar(camara, Path.GetFullPath(Path.Combine(Carpeta, toma.nombre)));
        if (toma.apuntarAMano && cuadro % 30 == 0) AnotarPunteria(joysticks, camara, vida.transform.position);
        if (cuadro % 30 == 0)
            Debug.Log("GrabarClips: cuadro " + cuadro + ", t " + t.ToString("0.00") + ", vivos " + EnemyController.ZombisVivos
                      + ", dispara " + (joysticks.thegun != null && joysticks.thegun.isFiring)
                      + ", vida " + vida.health + ", jugador en " + vida.transform.position + ", fov " + camara.fieldOfView.ToString("0.0")
                      + (ObjetivoVivo ? ", objetivo a " + Mathf.Sqrt(EnElPiso2(objetivo.transform.position - vida.transform.position)).ToString("0.0")
                         + " m, vida " + objetivo.VidaActual.ToString("0") + ", en cuadro " + (Vector2)camara.WorldToViewportPoint(objetivo.transform.position) : ""));
        cuadro++;
        if (cuadro >= toma.cuadros) Terminar(null);
        else if (muerteDelObjetivo >= 0f && toma.seguirTrasMatar >= 0f && t >= muerteDelObjetivo + toma.seguirTrasMatar) Terminar(null);
    }

    // Los zombis que se suben encima de otros o del heroe (lo reporto Ivan el 17/10: quieto, con
    // muchos encima, se le suben y salen volando): el pie de la capsula sobre el piso.
    static float subidaMaxima;
    static int cuadroDeLaMedida;

    static void MedirSubidos(float t)
    {
        int subidos = 0;
        float maxima = 0f;
        foreach (var z in EnemyController.Vivos)
        {
            if (z == null || !z.Vivo) continue;
            var capsula = z.GetComponent<CapsuleCollider>();
            if (capsula == null) continue;
            float pie = capsula.bounds.min.y;
            maxima = Mathf.Max(maxima, pie);
            if (pie > 0.3f) subidos++;
        }
        subidaMaxima = Mathf.Max(subidaMaxima, maxima);
        if (cuadroDeLaMedida++ % 30 == 0)
            Debug.Log("GrabarClips subidos: t " + t.ToString("0.0") + ", vivos " + EnemyController.ZombisVivos + ", subidos " + subidos
                      + ", pie mas alto " + maxima.ToString("0.00") + ", el mas alto de la toma " + subidaMaxima.ToString("0.00"));
    }

    // El guion del jefe y del tesoro: lo que hacen ellos (sacarlos, ponerlos, su vida) y lo que
    // el heroe tiene que hacer con eso (pasoDelGuion, que lee Mover).
    static bool jefePuesto;
    static Vector2 pasoDelGuion;

    static bool ObjetivoVivo
    {
        get { return objetivo != null && EnemyController.SigueVivo(objetivo, numeroDelObjetivo); }
    }

    // Con el jefe, el heroe no tira hasta que lo aturde la carga: "espera el error".
    static bool SinDisparar
    {
        get { return toma != null && toma.objetivo == "jefe" && (ventanaDesde < 0f || !ObjetivoVivo); }
    }

    static void Guion(PlayerHealth vida, PlayerJS joysticks, float t)
    {
        var olas = Object.FindAnyObjectByType<WaveManager>();
        Vector3 yo = vida.transform.position;
        pasoDelGuion = Vector2.zero;

        if (toma.objetivo == "jefe")
        {
            if (objetivo == null)
                foreach (var j in EnemyController.Jefes)
                    if (j != null && j.Vivo) { objetivo = j; numeroDelObjetivo = j.NumeroDeAparicion; break; }
            if (objetivo == null) return;
            var patrones = objetivo.GetComponent<JefePatrones>();
            // La oleada se para y se van los demas: el clip es el duelo.
            if (toma.despejar && !despejado && olas != null)
            {
                despejado = true;
                olas.StopAllCoroutines();
                Debug.Log("GrabarClips: oleada parada en t " + t.ToString("0.00"));
            }
            if (toma.despejar && t < 0f) EnemyController.DespejarAlrededor(yo, 500f);
            if (patrones == null) return;
            patrones.cadaCuanto = 0.4f;
            // La carga mas corta: de 16 m, desde 6 m terminaba fuera del cuadro vertical, abajo. Asi
            // frena al lado del heroe.
            patrones.duracionCarga = 0.45f;
            if (t < 0f || ventanaDesde >= 0f) campoProximoAtaque.SetValue(patrones, Time.time + 100f);
            if (t < 0f) return;
            if (!jefePuesto)
            {
                // A 6,5 m al norte, que en el cuadro vertical se ve entero (a 8 se cortaba arriba y
                // no atacaba hasta acercarse: JefePatrones.EnPantalla), y a atacar enseguida.
                jefePuesto = true;
                Vector3 donde = yo + Vector3.forward * 6.5f;
                donde.y = objetivo.transform.position.y;
                var rb = objetivo.GetComponent<Rigidbody>();
                if (rb != null) { rb.position = donde; rb.linearVelocity = Vector3.zero; }
                objetivo.transform.position = donde;
                campoProximoAtaque.SetValue(patrones, Time.time + 0.3f);
                Debug.Log("GrabarClips: jefe puesto en " + donde + ", vida " + objetivo.VidaMaxima.ToString("0"));
            }
            if (ObjetivoVivo) GuionDelJefe(patrones, vida, t);
        }
        else if (toma.objetivo == "tesoro")
        {
            if (!tesoroSacado && toma.tesoroEn >= 0f && t >= toma.tesoroEn && olas != null)
            {
                tesoroSacado = true;
                SacarElTesoro(olas, yo);
            }
            if (ObjetivoVivo)
            {
                // Lo persigue desde 3 m al sur: en el cuadro vertical queda arriba del heroe, debajo
                // del texto del clip. Persiguiendolo derecho se iba en diagonal y quedaba en la
                // esquina de arriba, cortado.
                Vector3 hacia = objetivo.transform.position - Vector3.forward * 3f - yo;
                hacia.y = 0f;
                float d = hacia.magnitude;
                if (d > 0.3f) pasoDelGuion = new Vector2(hacia.x, hacia.z) / d * toma.paso * Mathf.Clamp01(d / 1.5f);
            }
            if (toma.granadaEn >= 0f && !granadaTirada && t >= toma.granadaEn && t < toma.granadaEn + 2f)
                TirarGranada(vida, yo);
        }

        // La vida del objetivo, llevada a mano: llena hasta la ventana, y de ahi a cero, cada vez mas
        // rapido. Con la de verdad, el jefe de la oleada 30 aguanta mas que el clip, y al tesoro el
        // rifle lo mata en un segundo. El ultimo tramo lo hacen las balas; si no llegan, el remate.
        if (ObjetivoVivo)
        {
            float desde = toma.objetivo == "jefe" ? ventanaDesde : toma.matarDesde;
            float ahora = toma.objetivo == "jefe" ? Time.time : t;
            float maxima = objetivo.VidaMaxima;
            if (desde < 0f || ahora < desde) campoVida.SetValue(objetivo, maxima);
            else
            {
                float p = Mathf.Clamp01((ahora - desde) / Mathf.Max(0.1f, toma.matarEn));
                if (p >= 1f)
                {
                    Vector3 empuje = objetivo.transform.position - yo;
                    empuje.y = 0f;
                    // Con lo que le queda: el numero de dano es el que se ve.
                    objetivo.DanoZombi(objetivo.VidaActual + 1f, true, empuje.normalized);
                    Debug.Log("GrabarClips: remate del objetivo en t " + t.ToString("0.00"));
                }
                else campoVida.SetValue(objetivo, Mathf.Max(0.02f * maxima, maxima * (1f - p * p)));
            }
        }
        if (objetivo != null && !objetivoMuerto && !ObjetivoVivo)
        {
            objetivoMuerto = true;
            muerteDelObjetivo = t;
            dondeMurio = objetivo.transform.position;
            Debug.Log("GrabarClips: el objetivo murio (o se fue) en t " + t.ToString("0.00") + ", en " + dondeMurio);
        }
        // Despues, a juntar las monedas: camina hasta donde murio.
        if (objetivoMuerto)
        {
            Vector3 hacia = dondeMurio - yo;
            hacia.y = 0f;
            if (hacia.magnitude > 1f) pasoDelGuion = new Vector2(hacia.x, hacia.z).normalized * 0.45f;
        }
    }

    static Vector3 dondeMurio;

    // Esquiva el salto saliendo del anillo hacia atras y la carga de costado, a ultimo momento;
    // cuando la carga lo deja aturdido, la furia y la ventana para matarlo. En la ventana le tira
    // a una distancia que se vea entero: si quedo lejos (la carga sigue de largo), se acerca.
    static void GuionDelJefe(JefePatrones patrones, PlayerHealth vida, float t)
    {
        string estado = campoEstado.GetValue(patrones).ToString();
        float enEstado = Time.time - (float)campoDesde.GetValue(patrones);
        Vector3 yo = vida.transform.position;
        Vector3 jefe = objetivo.transform.position;
        Vector3 desdeElJefe = yo - jefe;
        desdeElJefe.y = 0f;

        if (estado == "Cargando") vioCarga = true;
        if (estado == "Aturdido" && vioCarga && ventanaDesde < 0f)
        {
            ventanaDesde = Time.time;
            // Aturdido mientras le llueven las balas: el castigo al error se lee entero.
            patrones.duracionAturdido = toma.matarEn + 1f;
            var furia = vida.GetComponent<Furia>();
            furiaPrendida = true;
            Debug.Log("GrabarClips: aturdido tras la carga en t " + t.ToString("0.00") + ", furia " + (furia != null && furia.Activar()));
        }

        if (estado == "AvisandoSalto" || estado == "Saltando")
        {
            Vector3 centro = (Vector3)campoDestino.GetValue(patrones);
            Vector3 afuera = yo - centro;
            afuera.y = 0f;
            if (afuera.sqrMagnitude < 0.04f) afuera = desdeElJefe;
            if ((enEstado >= 0.55f || estado == "Saltando") && afuera.magnitude < patrones.radioGolpe + 1f)
                pasoDelGuion = new Vector2(afuera.x, afuera.z).normalized;
        }
        else if (estado == "AvisandoCarga" || estado == "Cargando")
        {
            Vector3 linea = (Vector3)campoDireccion.GetValue(patrones);
            linea.y = 0f;
            linea.Normalize();
            Vector3 costado = new Vector3(linea.z, 0f, -linea.x);
            float lado = Vector3.Dot(desdeElJefe, costado);
            if ((enEstado >= 0.6f || estado == "Cargando") && Mathf.Abs(lado) < 2f)
            {
                // Hacia el lado en que ya esta; si esta justo en la linea, hacia el centro del mapa.
                float signo = Mathf.Abs(lado) > 0.2f ? Mathf.Sign(lado) : (Vector3.Dot(-yo, costado) >= 0f ? 1f : -1f);
                Vector3 v = costado * signo;
                pasoDelGuion = new Vector2(v.x, v.z);
            }
        }
        else if (ventanaDesde >= 0f)
        {
            // 3,5 m al sur del jefe: en el cuadro vertical, al costado queda afuera (la primera toma
            // le tiraba a un jefe fuera de cuadro), y al sur del heroe se ve menos piso que al norte
            // (la camara mira inclinada): ahi lo tapaban los numeros y las explosiones. Mas lejos,
            // los numeros se iban por arriba y el jefe caia bajo el texto del clip.
            Vector3 lugar = jefe - Vector3.forward * 3.5f;
            Vector3 hacia = lugar - yo;
            hacia.y = 0f;
            float d = hacia.magnitude;
            if (d > 0.4f) pasoDelGuion = new Vector2(hacia.x, hacia.z) / d * Mathf.Clamp(d * 0.3f, 0.2f, 0.6f);
        }
    }

    // El tesoro, al norte del heroe (SacarTesoro lo saca de costado: en el cuadro vertical, al
    // costado queda fuera), con lo de la oleada.
    static void SacarElTesoro(WaveManager olas, Vector3 yo)
    {
        var prefab = campoTesoro.GetValue(olas) as GameObject;
        if (prefab == null) { Debug.LogError("GrabarClips: sin el prefab del tesoro"); return; }
        Vector3 donde = yo + Vector3.forward * 6f;
        donde.x = Mathf.Clamp(donde.x, -42f, 42f);
        donde.z = Mathf.Clamp(donde.z, -42f, 42f);
        donde.y = 0f;
        var tesoro = EnemyController.Aparecer(prefab, donde);
        if (tesoro == null) { Debug.LogError("GrabarClips: el tesoro no salio"); return; }
        tesoro.multiplicadorVida = olas.MultiplicadorVidaActual;
        tesoro.multiplicadorDano = olas.MultiplicadorDanoActual;
        tesoro.multiplicadorMonedas = olas.MultiplicadorMonedasActual;
        tesoro.monedaPrefab = olas.monedaPrefab;
        Efectos.TesoroAparece(tesoro.transform.position);
        objetivo = tesoro;
        numeroDelObjetivo = tesoro.NumeroDeAparicion;
        Debug.Log("GrabarClips: tesoro en " + donde + ", vida " + tesoro.VidaMaxima.ToString("0"));
    }

    // La granada, al grupo mas grande que se vea entre 4 y 11 m y lejos del tesoro: abre el hueco
    // por donde pasa. Al tesoro no: le sacaria dos tercios de la vida de un golpe, y la vida va a mano.
    static void TirarGranada(PlayerHealth vida, Vector3 yo)
    {
        var control = vida.GetComponent<PlayerController>();
        if (control == null || !control.GranadaLista) return;
        var camara = Camera.main;
        const float Radio = 5f;
        EnemyController mejor = null;
        int mejorCuenta = 0;
        foreach (var z in EnemyController.Vivos)
        {
            if (z == null || !z.Vivo || z == objetivo) continue;
            float d = Mathf.Sqrt(EnElPiso2(z.transform.position - yo));
            if (d < 4f || d > 11f) continue;
            if (camara != null)
            {
                Vector3 v = camara.WorldToViewportPoint(z.transform.position);
                if (v.z <= 0f || v.x < 0.08f || v.x > 0.92f || v.y < 0.08f || v.y > 0.92f) continue;
            }
            if (ObjetivoVivo && Mathf.Sqrt(EnElPiso2(z.transform.position - objetivo.transform.position)) < Radio + 1.5f) continue;
            int cuenta = 0;
            foreach (var otro in EnemyController.Vivos)
                if (otro != null && otro.Vivo && otro != objetivo && EnElPiso2(otro.transform.position - z.transform.position) < 9f) cuenta++;
            if (cuenta > mejorCuenta) { mejorCuenta = cuenta; mejor = z; }
        }
        // Un grupo de verdad, si en el primer segundo y medio aparece; despues, lo que haya.
        if (mejor == null || (mejorCuenta < 3 && Time.timeSinceLevelLoad - toma.previa < toma.granadaEn + 1.5f)) return;
        Vector3 hacia = mejor.transform.position - yo;
        hacia.y = 0f;
        float lejos = Mathf.Clamp01((hacia.magnitude - control.distanciaMinimaGranada)
                                    / Mathf.Max(0.1f, control.distanciaMaximaGranada - control.distanciaMinimaGranada));
        control.TirarGranadaApuntada(new Vector2(hacia.x, hacia.z).normalized * lejos);
        granadaTirada = true;
        Debug.Log("GrabarClips: granada a " + mejorCuenta + " zombis, a " + hacia.magnitude.ToString("0.0") + " m");
    }

    // A quien le tira: el jefe en la ventana (y a nadie antes), el tesoro si se ve, y si no, el mas
    // cercano que se vea.
    static void Apuntar(PlayerJS joysticks, Vector3 yo)
    {
        if (joysticks.lookJoystick == null) return;
        if (toma.objetivo == "jefe")
        {
            if (!SinDisparar) ApuntarA(joysticks, yo, objetivo.transform.position);
            else PruebaDisparo.Soltar(joysticks.lookJoystick);
            return;
        }
        if (toma.objetivo == "tesoro" && ObjetivoVivo && SirveDeBlanco(objetivo, Camera.main, yo, Alcance(joysticks)))
        {
            ApuntarA(joysticks, yo, objetivo.transform.position);
            return;
        }
        if (toma.apuntarAMano) ApuntarAlMasCercano(joysticks, yo);
    }

    static void ApuntarA(PlayerJS joysticks, Vector3 yo, Vector3 blanco)
    {
        Vector3 hacia = blanco - yo;
        if (hacia.x * hacia.x + hacia.z * hacia.z < 0.01f) return;
        pedidoAMano = new Vector2(hacia.x, hacia.z).normalized;
        PruebaDisparo.Apretar(joysticks.lookJoystick, pedidoAMano);
    }

    // Hasta donde llega el arma que tiene en la mano: con la escopeta, 7 m.
    static float Alcance(PlayerJS joysticks)
    {
        if (joysticks.thegun == null) return AlcanceAMano;
        return Mathf.Min(AlcanceAMano, Proyectiles.Alcance(joysticks.thegun.TramoDelArma));
    }

    static void Mover(PlayerJS joysticks, float t)
    {
        if (joysticks.moveJoystick == null) return;
        Vector2 direccion;
        switch (toma.recorrido)
        {
            case "quieto": PruebaDisparo.Soltar(joysticks.moveJoystick); return;
            case "norte": direccion = t < 0f ? Vector2.zero : Vector2.up; break;
            case "objetivo":
                // Ya con cuanto empuja: lo decide el guion (Guion).
                if (t < 0f || pasoDelGuion == Vector2.zero)
                {
                    PruebaDisparo.Soltar(joysticks.moveJoystick);
                    marcaDelTiempo = -1f;
                    return;
                }
                Empujar(joysticks.moveJoystick, Destrabar(joysticks.transform.position, pasoDelGuion));
                return;
            default:
                float angulo = Time.time * Mathf.PI * 2f / 10f;
                direccion = new Vector2(Mathf.Cos(angulo), Mathf.Sin(angulo));
                break;
        }
        if (direccion == Vector2.zero) { PruebaDisparo.Soltar(joysticks.moveJoystick); return; }
        Empujar(joysticks.moveJoystick, direccion * toma.paso);
    }

    // Si empujando no avanza (un auto, un tanque, que no ceden), sale de costado un rato, una vez
    // para cada lado. En la primera toma del tesoro quedaba empujando un edificio hasta el final.
    static Vector3 marcaDelLugar;
    static float marcaDelTiempo = -1f, destrabarHasta = -1f, ladoDelDestrabe = 1f;

    static Vector2 Destrabar(Vector3 yo, Vector2 paso)
    {
        if (marcaDelTiempo < 0f || Time.time - marcaDelTiempo > 0.5f)
        {
            float esperado = paso.magnitude * 11.5f * (Time.time - marcaDelTiempo);
            if (marcaDelTiempo >= 0f && Time.time >= destrabarHasta && EnElPiso2(yo - marcaDelLugar) < 0.09f * esperado * esperado)
            {
                destrabarHasta = Time.time + 0.6f;
                ladoDelDestrabe = -ladoDelDestrabe;
                Debug.Log("GrabarClips: trabado en " + yo + ", sale de costado");
            }
            marcaDelLugar = yo;
            marcaDelTiempo = Time.time;
        }
        if (Time.time < destrabarHasta)
        {
            Vector2 costado = new Vector2(paso.y, -paso.x) * ladoDelDestrabe;
            return (paso * 0.3f + costado).normalized * paso.magnitude;
        }
        return paso;
    }

    // Como PruebaDisparo.Apretar, pero con cuanto se empuja: el dedo cae a esa fraccion del radio.
    static void Empujar(Joystick joystick, Vector2 cuanto)
    {
        var canvas = joystick.GetComponentInParent<Canvas>();
        Camera camara = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        var fondo = joystick.transform as RectTransform;
        Vector2 centro = RectTransformUtility.WorldToScreenPoint(camara, joystick.transform.position);
        float radio = (fondo != null ? fondo.sizeDelta.x * 0.5f : 100f) * (canvas != null ? canvas.scaleFactor : 1f);
        var dedo = new PointerEventData(EventSystem.current) { position = centro + cuanto * radio };
        joystick.OnPointerDown(dedo);
    }

    // El blanco a mano: el mas cercano que se ve en el cuadro vertical y no esta encima del heroe, y
    // se queda con el que tiene mientras otro no este bastante mas cerca (como el automatico,
    // PlayerJS.VentajaParaCambiar). Con el mas cercano a secas (15/10) el blanco solia estar fuera
    // del cuadro, que en vertical mide pocos metros de ancho, o pegado al heroe, donde la direccion
    // no esta definida: la mira saltaba de un lado al otro y el chorro se iba al vacio.
    const float DistanciaMinimaAlBlanco = 1.2f;
    const float AlcanceAMano = 22f;
    const float VentajaParaCambiar = 0.7f;

    static void ApuntarAlMasCercano(PlayerJS joysticks, Vector3 desde)
    {
        if (joysticks.lookJoystick == null) return;
        var camara = Camera.main;
        float alcance = Alcance(joysticks);
        EnemyController mejor = null;
        float mejorDistancia = alcance * alcance;
        foreach (var z in EnemyController.Vivos)
        {
            if (!SirveDeBlanco(z, camara, desde, alcance)) continue;
            float d = EnElPiso2(z.transform.position - desde);
            if (d < mejorDistancia) { mejorDistancia = d; mejor = z; }
        }
        if (blancoAMano != null && blancoAMano != mejor && EnemyController.SigueVivo(blancoAMano, numeroDelBlanco)
            && SirveDeBlanco(blancoAMano, camara, desde, alcance)
            && mejorDistancia >= EnElPiso2(blancoAMano.transform.position - desde) * VentajaParaCambiar * VentajaParaCambiar)
            mejor = blancoAMano;
        if (mejor != null && mejor != blancoAMano) numeroDelBlanco = mejor.NumeroDeAparicion;
        blancoAMano = mejor;
        if (mejor == null) { PruebaDisparo.Soltar(joysticks.lookJoystick); return; }
        Vector3 hacia = mejor.transform.position - desde;
        pedidoAMano = new Vector2(hacia.x, hacia.z).normalized;
        PruebaDisparo.Apretar(joysticks.lookJoystick, pedidoAMano);
    }

    static bool SirveDeBlanco(EnemyController z, Camera camara, Vector3 desde, float alcance)
    {
        if (z == null || !z.Vivo) return false;
        float d = EnElPiso2(z.transform.position - desde);
        if (d < DistanciaMinimaAlBlanco * DistanciaMinimaAlBlanco || d > alcance * alcance) return false;
        if (camara == null) return true;
        Vector3 v = camara.WorldToViewportPoint(z.transform.position);
        return v.z > 0f && v.x > 0.06f && v.x < 0.94f && v.y > 0.06f && v.y < 0.94f;
    }

    static float EnElPiso2(Vector3 v)
    {
        return v.x * v.x + v.z * v.z;
    }

    static EnemyController blancoAMano;
    static int numeroDelBlanco;
    static Vector2 pedidoAMano;

    // Para medir hacia donde tira: el blanco elegido, donde cae en el cuadro vertical, lo que se le
    // pidio al joystick, lo que leyo PlayerJS y hacia donde mira el arma.
    static void AnotarPunteria(PlayerJS joysticks, Camera camara, Vector3 yo)
    {
        string blanco = "ninguno";
        if (blancoAMano != null)
        {
            Vector3 v = camara.WorldToViewportPoint(blancoAMano.transform.position);
            Vector3 rel = blancoAMano.transform.position - yo;
            blanco = blancoAMano.name + " #" + blancoAMano.NumeroDeAparicion + " vida " + blancoAMano.VidaActual.ToString("0") + "/"
                     + blancoAMano.VidaMaxima.ToString("0") + " a (" + rel.x.ToString("0.0") + ", " + rel.z.ToString("0.0") + ") en cuadro ("
                     + v.x.ToString("0.00") + ", " + v.y.ToString("0.00") + ")";
        }
        Vector3 arma = joysticks.thegun != null ? joysticks.thegun.transform.forward : Vector3.zero;
        Debug.Log("GrabarClips punteria: cuadro " + cuadro + ", blanco " + blanco + ", pedido " + pedidoAMano
                  + ", joystick " + (joysticks.lookJoystick != null ? joysticks.lookJoystick.Direction.ToString() : "-")
                  + ", PlayerJS " + joysticks.UltimaDireccionApuntada + ", arma (" + arma.x.ToString("0.00") + ", " + arma.z.ToString("0.00") + ")"
                  + ", automatico " + (joysticks.Blanco != null ? joysticks.Blanco.name : "no"));
    }

    static void GrabarSonido()
    {
        int muestras = AudioRenderer.GetSampleCountForCaptureFrame();
        int canales = AudioSettings.speakerMode == AudioSpeakerMode.Mono ? 1 : 2;
        if (muestras <= 0) return;
        var buffer = new NativeArray<float>(muestras * canales, Allocator.Temp);
        if (AudioRenderer.Render(buffer)) sonido.AddRange(buffer);
        buffer.Dispose();
    }

    static void Grabar(Camera camara, string carpeta)
    {
        if (textura == null)
        {
            textura = new RenderTexture(Ancho, Alto, 24) { antiAliasing = 4, hideFlags = HideFlags.HideAndDontSave };
            lectura = new Texture2D(Ancho, Alto, TextureFormat.RGB24, false) { hideFlags = HideFlags.HideAndDontSave };
        }
        var antes = camara.targetTexture;
        camara.targetTexture = textura;
        camara.Render();
        camara.targetTexture = antes;
        var activa = RenderTexture.active;
        RenderTexture.active = textura;
        lectura.ReadPixels(new Rect(0, 0, Ancho, Alto), 0, 0);
        lectura.Apply();
        RenderTexture.active = activa;
        File.WriteAllBytes(Path.Combine(carpeta, "f" + cuadro.ToString("0000") + ".jpg"), lectura.EncodeToJPG(95));
    }

    // Un WAV de 16 bits con lo que grabo AudioRenderer.
    static void EscribirSonido(string carpeta)
    {
        if (sonido == null || sonido.Count == 0) { Debug.Log("GrabarClips: sin sonido grabado"); return; }
        int canales = AudioSettings.speakerMode == AudioSpeakerMode.Mono ? 1 : 2;
        int frecuencia = AudioSettings.outputSampleRate;
        float pico = 0f;
        foreach (var s in sonido) pico = Mathf.Max(pico, Mathf.Abs(s));
        using (var archivo = new BinaryWriter(File.Create(Path.Combine(carpeta, "sonido.wav"))))
        {
            int datos = sonido.Count * 2;
            archivo.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            archivo.Write(36 + datos);
            archivo.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
            archivo.Write(16);
            archivo.Write((short)1);
            archivo.Write((short)canales);
            archivo.Write(frecuencia);
            archivo.Write(frecuencia * canales * 2);
            archivo.Write((short)(canales * 2));
            archivo.Write((short)16);
            archivo.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            archivo.Write(datos);
            foreach (var s in sonido) archivo.Write((short)Mathf.Clamp(Mathf.RoundToInt(s * 32767f), -32768, 32767));
        }
        Debug.Log("GrabarClips: sonido " + sonido.Count / canales + " muestras a " + frecuencia + " Hz, " + canales + " canales, pico " + pico.ToString("0.000"));
    }

    static void Terminar(string error)
    {
        if (error != null) Debug.LogError("GrabarClips: " + error);
        else Debug.Log("GrabarClips: grabados " + cuadro + " cuadros en " + Path.GetFullPath(Carpeta));
        if (grabandoSonido) { AudioRenderer.Stop(); grabandoSonido = false; }
        if (toma != null && error == null) EscribirSonido(Path.GetFullPath(Path.Combine(Carpeta, toma.nombre)));
        SessionState.SetBool(Clave + ".fallo", error != null);
        Time.captureFramerate = 0;
        SessionState.SetBool(Clave, false);
        PruebaDisparo.DevolverElTeclado(Clave);
        EditorPrefs.SetBool(EventoHalloween.ClaveForzarEnElEditor, SessionState.GetBool(Clave + ".forzarHalloween", false));
        EditorPrefs.SetBool(EventoHalloween.ClaveApagarEnElEditor, SessionState.GetBool(Clave + ".apagarHalloween", false));
        if (textura != null) { textura.Release(); Object.DestroyImmediate(textura); textura = null; }
        if (lectura != null) { Object.DestroyImmediate(lectura); lectura = null; }
        if (Camera.main != null) Camera.main.ResetAspect();
        if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
        else if (SessionState.GetBool(Clave + ".salir", false)) Salir(error != null ? 1 : 0);
    }

    // En -batchmode, de vuelta en modo edicion (donde RespaldoDelBanco devuelve el progreso), Unity
    // se cierra solo. Con delayCall: despues de que el respaldo haga lo suyo.
    static void AlCambiarDeModo(PlayModeStateChange cambio)
    {
        if (cambio != PlayModeStateChange.EnteredEditMode) return;
        if (!SessionState.GetBool(Clave + ".salir", false) || SessionState.GetBool(Clave, false)) return;
        SessionState.SetBool(Clave + ".salir", false);
        int codigo = SessionState.GetBool(Clave + ".fallo", false) ? 1 : 0;
        EditorApplication.delayCall += () => Salir(codigo);
    }

    static void Salir(int codigo)
    {
        if (Application.isBatchMode) EditorApplication.Exit(codigo);
    }
}
