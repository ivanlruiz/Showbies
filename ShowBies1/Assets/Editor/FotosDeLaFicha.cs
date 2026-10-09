using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;

// Las capturas de la ficha de Google Play, sacadas del juego de verdad: un banco en play que
// juega solo (los joysticks con los mismos eventos que un dedo, como PruebaDisparo), con el HUD
// del telefono, y saca rafagas de fotos de 1920 x 1080 en calidad alta. Recorre varias tomas,
// cada una en su propio Play: la pradera con la horda y una granada, la oleada del jefe, la
// furia, el cementerio, la ciudad, y el menu con la tienda, las misiones y los logros. Despues
// se eligen a mano las mejores de Builds/ficha_capturas/.
//
// La foto no depende del tamanio de la ventana Game: la camara dibuja en una RenderTexture y los
// canvas en overlay pasan un instante a ScreenSpaceCamera con esa camara (la UI en overlay no pasa
// por ninguna camara). El progreso, los PlayerPrefs, "Teclado y mouse en el editor" y Halloween
// forzado vuelven a como estaban: RespaldoDelBanco se guarda antes de cada toma y se devuelve al
// salir de cada Play. El jugador no muere (la vida se rellena en cada cuadro).
[InitializeOnLoad]
public static class FotosDeLaFicha
{
    const string Clave = "ShowBies.FotosDeLaFicha";
    const string Banco = "FotosDeLaFicha";
    const string Carpeta = "../Builds/ficha_capturas";
    const int Ancho = 1920, Alto = 1080;
    const string Oleadas = "Assets/Escenas/WaveMode.unity";
    const string Menu = "Assets/Escenas/Menu.unity";

    enum Truco { Nada, Granada, Furia, Jefe, Menu }

    class Toma
    {
        public string nombre;
        public string escena;
        public int oleada;        // la oleada en curso con que arranca
        public float espera;      // segundos de juego antes de la primera foto
        public int fotos;
        public float cada;
        public Truco truco;
    }

    static readonly Toma[] Tomas =
    {
        new Toma { nombre = "pradera", escena = Oleadas, oleada = 9, espera = 14f, fotos = 10, cada = 0.6f, truco = Truco.Granada },
        new Toma { nombre = "jefe", escena = Oleadas, oleada = 10, espera = 4f, fotos = 40, cada = 0.3f, truco = Truco.Jefe },
        new Toma { nombre = "furia", escena = Oleadas, oleada = 8, espera = 12f, fotos = 8, cada = 0.35f, truco = Truco.Furia },
        new Toma { nombre = "cementerio", escena = Oleadas, oleada = 16, espera = 14f, fotos = 10, cada = 0.6f, truco = Truco.Granada },
        new Toma { nombre = "ciudad", escena = Oleadas, oleada = 24, espera = 14f, fotos = 10, cada = 0.6f, truco = Truco.Granada },
        new Toma { nombre = "menu", escena = Menu, oleada = 0, espera = 3f, fotos = 4, cada = 2.5f, truco = Truco.Menu },
    };

    static float desde;
    static float proximaFoto;
    static int sacadas;
    static float trucoEn = -1f;
    static float angulo;

    static FotosDeLaFicha()
    {
        EditorApplication.update += Tick;
    }

    [MenuItem("ShowBies/Pruebas/Fotos de la ficha de Play (play)")]
    // Publico para correrlo por codigo (ver la trampa del registro de menus).
    public static void Arrancar()
    {
        if (!RespaldoDelBanco.PuedeArrancar(Banco)) return;
        Directory.CreateDirectory(Path.GetFullPath(Carpeta));
        foreach (var vieja in Directory.GetFiles(Path.GetFullPath(Carpeta), "*.png")) File.Delete(vieja);
        // Sin Halloween: las capturas son del juego de siempre (las del evento van aparte).
        SessionState.SetBool(Clave + ".halloween", EditorPrefs.GetBool(EventoHalloween.ClaveForzarEnElEditor, false));
        EditorPrefs.SetBool(EventoHalloween.ClaveForzarEnElEditor, false);
        // La escena que estaba abierta vuelve al final.
        SessionState.SetString(Clave + ".escena", EditorSceneManager.GetActiveScene().path);
        SessionState.SetBool(Clave, true);
        SessionState.SetInt(Clave + ".toma", 0);
        Preparar(0);
    }

    // Arma la toma: el respaldo, el progreso a su gusto y su escena, y entra en Play.
    static void Preparar(int indice)
    {
        var toma = Tomas[indice];
        RespaldoDelBanco.Guardar(Banco);
        PruebaDisparo.ModoTelefono(Clave);
        PlayerSettings.runInBackground = true;

        // Un jugador que ya jugo y compro: sin la guia de la primera partida y con el HUD lleno.
        HerramientasProgreso.FijarNivelesDePrueba();
        Progreso.TerminarPartida(600f);
        Progreso.TerminarPartida(600f);
        Progreso.RegistrarOleadaCompletada(Mathf.Max(23, toma.oleada - 1));
        HerramientasProgreso.SumarMonedas(18460);
        if (toma.oleada > 0) Progreso.GuardarOleadaEnCurso(toma.oleada, toma.oleada * 160);
        // La recompensa de hoy, ya cobrada: que el menu no abra con su ventana.
        Progreso.RegistrarRecompensaDiaria(Progreso.DiaDeHoy(), 4);
        Progreso.Guardar();

        EditorSceneManager.OpenScene(toma.escena);
        SessionState.SetBool(Clave + ".empezo", false);
        EditorApplication.EnterPlaymode();
    }

    static void Tick()
    {
        if (!SessionState.GetBool(Clave, false)) return;
        int indice = SessionState.GetInt(Clave + ".toma", 0);

        // Entre una toma y otra, en modo edicion: la siguiente, o el final.
        if (!EditorApplication.isPlayingOrWillChangePlaymode)
        {
            if (!SessionState.GetBool(Clave + ".termino", false)) return;
            SessionState.SetBool(Clave + ".termino", false);
            if (indice + 1 < Tomas.Length)
            {
                SessionState.SetInt(Clave + ".toma", indice + 1);
                Preparar(indice + 1);
            }
            else Terminar();
            return;
        }
        if (!EditorApplication.isPlaying) return;
        if (!RespaldoDelBanco.SigueArmado(Banco, Clave)) return;

        var toma = Tomas[indice];
        if (!SessionState.GetBool(Clave + ".empezo", false))
        {
            SessionState.SetBool(Clave + ".empezo", true);
            desde = Time.unscaledTime;
            proximaFoto = desde + toma.espera;
            sacadas = 0;
            trucoEn = -1f;
            return;
        }

        if (toma.truco == Truco.Menu) TickDelMenu(toma);
        else TickDeLaPartida(toma);

        if (sacadas >= toma.fotos)
        {
            SessionState.SetBool(Clave + ".termino", true);
            EditorApplication.ExitPlaymode();
        }
    }

    // En la partida: rellena la vida, apunta al zombi mas cercano y dispara, camina en ronda
    // alrededor del centro, y segun la toma tira una granada a un grupo o prende la furia.
    static void TickDeLaPartida(Toma toma)
    {
        var vida = PlayerHealth.instance;
        if (vida == null) return;
        vida.health = vida.maxHealth;
        var control = vida.GetComponent<PlayerController>();
        var joysticks = vida.GetComponent<PlayerJS>();
        if (control == null || joysticks == null || EventSystem.current == null) return;

        Vector3 yo = control.transform.position;
        EnemyController cercano = null;
        float mejor = float.MaxValue;
        foreach (var z in Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None))
        {
            if (!z.Vivo) continue;
            float d = (z.transform.position - yo).sqrMagnitude;
            if (d < mejor) { mejor = d; cercano = z; }
        }
        if (cercano != null)
        {
            Vector3 hacia = cercano.transform.position - yo;
            PruebaDisparo.Apretar(joysticks.lookJoystick, new Vector2(hacia.x, hacia.z).normalized);
        }
        else PruebaDisparo.Soltar(joysticks.lookJoystick);

        // En ronda, despacio, alrededor de un punto un poco al sur del centro: se ve correr.
        angulo += Time.unscaledDeltaTime * 0.6f;
        Vector3 meta = new Vector3(Mathf.Cos(angulo), 0f, Mathf.Sin(angulo)) * 3f;
        Vector3 ir = meta - yo;
        ir.y = 0f;
        if (ir.magnitude > 0.5f) PruebaDisparo.Apretar(joysticks.moveJoystick, new Vector2(ir.x, ir.z).normalized);
        else PruebaDisparo.Soltar(joysticks.moveJoystick);

        float ahora = Time.unscaledTime;
        if (ahora < proximaFoto) return;

        // El truco va justo antes de las fotos: la granada tarda en caer, la furia en arrancar.
        if (trucoEn < 0f && toma.truco == Truco.Granada && control.GranadaLista && EnemyController.ZombisVivos >= 8)
        {
            var lanzar = typeof(PlayerController).GetMethod("LanzarGranadaA", BindingFlags.Instance | BindingFlags.NonPublic);
            Vector3 grupo = Grupo(yo);
            if (lanzar != null && grupo != Vector3.zero)
            {
                lanzar.Invoke(control, new object[] { grupo });
                trucoEn = ahora;
                proximaFoto = ahora + 0.45f;
                return;
            }
        }
        if (trucoEn < 0f && toma.truco == Truco.Furia)
        {
            var furia = vida.GetComponent<Furia>();
            if (furia != null && furia.Activar())
            {
                trucoEn = ahora;
                proximaFoto = ahora + 0.25f;
                return;
            }
        }
        // El jefe: rafaga apenas aparece, para pescar el anillo y el salto.
        if (toma.truco == Truco.Jefe && EnemyController.Jefes.Count == 0)
        {
            proximaFoto = ahora + 0.2f;
            return;
        }
        // Sin horda en pantalla la foto no sirve: se espera a que haya.
        if (toma.truco != Truco.Jefe && EnemyController.ZombisVivos < 10 && ahora - desde < toma.espera + 30f)
        {
            proximaFoto = ahora + 0.2f;
            return;
        }

        Foto(toma.nombre + "_" + sacadas.ToString("00"));
        sacadas++;
        // Despues de la granada, cuadros seguidos para pescar la explosion.
        float cada = trucoEn >= 0f && ahora - trucoEn < 1.6f ? 0.12f : toma.cada;
        proximaFoto = ahora + cada;
    }

    // El centro del grupo mas grande de zombis a entre 4 y 11 m: donde cae la granada.
    static Vector3 Grupo(Vector3 yo)
    {
        Vector3 mejor = Vector3.zero;
        int masVecinos = 0;
        var zombis = Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
        foreach (var z in zombis)
        {
            if (!z.Vivo) continue;
            Vector3 p = z.transform.position;
            float d = Vector3.Distance(new Vector3(p.x, 0f, p.z), new Vector3(yo.x, 0f, yo.z));
            if (d < 4f || d > 11f) continue;
            int vecinos = 0;
            foreach (var otro in zombis)
                if (otro.Vivo && (otro.transform.position - p).sqrMagnitude < 9f) vecinos++;
            if (vecinos > masVecinos) { masVecinos = vecinos; mejor = new Vector3(p.x, 0f, p.z); }
        }
        return masVecinos >= 3 ? mejor : Vector3.zero;
    }

    // El menu: la portada, la tienda, las misiones y los logros, de a una.
    static void TickDelMenu(Toma toma)
    {
        float ahora = Time.unscaledTime;
        if (ahora < proximaFoto) return;
        string[] nombres = { "menu_portada", "menu_tienda", "menu_misiones", "menu_logros" };
        if (sacadas < nombres.Length) Foto(nombres[sacadas]);
        sacadas++;
        CerrarVentanas();
        if (sacadas == 1) { var tienda = Object.FindFirstObjectByType<TiendaMejoras>(FindObjectsInactive.Include); if (tienda != null) tienda.Abrir(); }
        if (sacadas == 2) { var v = Object.FindFirstObjectByType<VentanaMisiones>(); if (v != null) v.Abrir(); }
        if (sacadas == 3) { var v = Object.FindFirstObjectByType<VentanaLogros>(); if (v != null) v.Abrir(); }
        proximaFoto = ahora + toma.cada;
    }

    static void CerrarVentanas()
    {
        var tienda = Object.FindFirstObjectByType<TiendaMejoras>(FindObjectsInactive.Include);
        if (tienda != null && tienda.Abierta) tienda.Cerrar();
        var misiones = Object.FindFirstObjectByType<VentanaMisiones>();
        if (misiones != null && VentanaMisiones.Abierta) misiones.Cerrar();
        var logros = Object.FindFirstObjectByType<VentanaLogros>();
        if (logros != null && VentanaLogros.Abierta) logros.Cerrar();
    }

    // La camara del juego a una RenderTexture de 1920 x 1080, con los canvas en overlay pasados
    // un instante a ScreenSpaceCamera, en su orden de siempre.
    static void Foto(string nombre)
    {
        var camara = Camera.main;
        if (camara == null) return;
        var rt = new RenderTexture(Ancho, Alto, 24) { antiAliasing = 4, hideFlags = HideFlags.HideAndDontSave };
        int calidad = QualitySettings.GetQualityLevel();
        QualitySettings.SetQualityLevel(QualitySettings.names.Length - 1, false);
        var antes = camara.targetTexture;
        int mascara = camara.cullingMask;
        camara.targetTexture = rt;
        camara.cullingMask |= 1 << LayerMask.NameToLayer("UI");

        var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        var cambiados = new System.Collections.Generic.List<Canvas>();
        foreach (var c in canvases)
        {
            if (!c.isRootCanvas || c.renderMode != RenderMode.ScreenSpaceOverlay || !c.isActiveAndEnabled) continue;
            c.renderMode = RenderMode.ScreenSpaceCamera;
            c.worldCamera = camara;
            c.planeDistance = camara.nearClipPlane + 0.02f;
            cambiados.Add(c);
        }
        Canvas.ForceUpdateCanvases();
        camara.Render();
        camara.Render();

        var activa = RenderTexture.active;
        RenderTexture.active = rt;
        var foto = new Texture2D(Ancho, Alto, TextureFormat.RGB24, false);
        foto.ReadPixels(new Rect(0, 0, Ancho, Alto), 0, 0);
        foto.Apply();
        RenderTexture.active = activa;

        foreach (var c in cambiados)
        {
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.worldCamera = null;
        }
        camara.targetTexture = antes;
        camara.cullingMask = mascara;
        QualitySettings.SetQualityLevel(calidad, false);
        rt.Release();
        Object.DestroyImmediate(rt);

        File.WriteAllBytes(Path.GetFullPath(Carpeta + "/" + nombre + ".png"), foto.EncodeToPNG());
        Object.DestroyImmediate(foto);
    }

    static void Terminar()
    {
        SessionState.SetBool(Clave, false);
        EditorPrefs.SetBool(EventoHalloween.ClaveForzarEnElEditor, SessionState.GetBool(Clave + ".halloween", false));
        // "Teclado y mouse en el editor" y runInBackground ya los devolvio el respaldo al salir
        // del ultimo Play.
        AssetDatabase.SaveAssets();
        string escena = SessionState.GetString(Clave + ".escena", "");
        if (!string.IsNullOrEmpty(escena) && File.Exists(escena)) EditorSceneManager.OpenScene(escena);
        Debug.Log("FotosDeLaFicha: listas en " + Path.GetFullPath(Carpeta));
    }
}
