using System.Collections.Generic;
using System.IO;
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
// de grabar corre una previa con el heroe inmortal, para que la horda llegue.
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
        public float furiaEn = -1f;        // segundos de la toma
        public float morirEn = -1f;        // segundos de la toma; hasta ahi la vida baja sola
        // La vida maxima durante la toma: con la de verdad (80), en la oleada 25 dos zarpazos en el
        // mismo cuadro lo mataban antes de tiempo. Con mas, la vida que baja sola manda.
        public int vidaMaxima = -1;
    }

    static readonly string[] Mejoras = { "dano_bala", "cadencia", "criticos", "vida_maxima", "granada", "iman", "botin", "furia" };
    static readonly int[] AlTope = { 16, 16, 8, 20, 1, 13, 15, 1 };

    static Toma toma;
    static RenderTexture textura;
    static Texture2D lectura;
    static int cuadro;
    static bool empezo, furiaPrendida, murio;
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

    static void Arrancar(Toma nueva)
    {
        if (!RespaldoDelBanco.PuedeArrancar(Banco)) { Salir(1); return; }
        RespaldoDelBanco.Guardar(Banco);
        PruebaDisparo.ModoTelefono(Clave);
        PlayerSettings.runInBackground = true;
        SessionState.SetBool(Clave + ".forzarHalloween", EditorPrefs.GetBool(EventoHalloween.ClaveForzarEnElEditor, false));
        SessionState.SetBool(Clave + ".apagarHalloween", EditorPrefs.GetBool(EventoHalloween.ClaveApagarEnElEditor, false));
        EditorPrefs.SetBool(EventoHalloween.ClaveForzarEnElEditor, nueva.halloween);
        EditorPrefs.SetBool(EventoHalloween.ClaveApagarEnElEditor, !nueva.halloween);

        for (int i = 0; i < Mejoras.Length; i++) Progreso.DepurarFijarNivel(Mejoras[i], nueva.alTope ? AlTope[i] : 0);
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
        bool quieto = toma.quietoEnLaPrevia && t < 0f;
        PlayerJS.FijarAutoApuntar(!quieto);

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
            if (toma.apuntarAMano) ApuntarAlMasCercano(joysticks, vida.transform.position);
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
                      + ", vida " + vida.health + ", jugador en " + vida.transform.position + ", fov " + camara.fieldOfView.ToString("0.0"));
        cuadro++;
        if (cuadro >= toma.cuadros) Terminar(null);
    }

    static void Mover(PlayerJS joysticks, float t)
    {
        if (joysticks.moveJoystick == null) return;
        Vector2 direccion;
        switch (toma.recorrido)
        {
            case "quieto": PruebaDisparo.Soltar(joysticks.moveJoystick); return;
            case "norte": direccion = t < 0f ? Vector2.zero : Vector2.up; break;
            default:
                float angulo = Time.time * Mathf.PI * 2f / 10f;
                direccion = new Vector2(Mathf.Cos(angulo), Mathf.Sin(angulo));
                break;
        }
        if (direccion == Vector2.zero) { PruebaDisparo.Soltar(joysticks.moveJoystick); return; }
        Empujar(joysticks.moveJoystick, direccion * toma.paso);
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
        EnemyController mejor = null;
        float mejorDistancia = AlcanceAMano * AlcanceAMano;
        foreach (var z in EnemyController.Vivos)
        {
            if (!SirveDeBlanco(z, camara, desde)) continue;
            float d = EnElPiso2(z.transform.position - desde);
            if (d < mejorDistancia) { mejorDistancia = d; mejor = z; }
        }
        if (blancoAMano != null && blancoAMano != mejor && EnemyController.SigueVivo(blancoAMano, numeroDelBlanco)
            && SirveDeBlanco(blancoAMano, camara, desde)
            && mejorDistancia >= EnElPiso2(blancoAMano.transform.position - desde) * VentajaParaCambiar * VentajaParaCambiar)
            mejor = blancoAMano;
        if (mejor != null && mejor != blancoAMano) numeroDelBlanco = mejor.NumeroDeAparicion;
        blancoAMano = mejor;
        if (mejor == null) { PruebaDisparo.Soltar(joysticks.lookJoystick); return; }
        Vector3 hacia = mejor.transform.position - desde;
        pedidoAMano = new Vector2(hacia.x, hacia.z).normalized;
        PruebaDisparo.Apretar(joysticks.lookJoystick, pedidoAMano);
    }

    static bool SirveDeBlanco(EnemyController z, Camera camara, Vector3 desde)
    {
        if (z == null || !z.Vivo) return false;
        float d = EnElPiso2(z.transform.position - desde);
        if (d < DistanciaMinimaAlBlanco * DistanciaMinimaAlBlanco || d > AlcanceAMano * AlcanceAMano) return false;
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
