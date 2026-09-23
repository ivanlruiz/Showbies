using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;

// Banco del disparo con los joysticks, el camino del telefono. Entra en play en WaveMode
// con el editor en modo telefono (target Android y sin "Teclado y mouse en el editor") y
// maneja los joysticks de verdad, con los mismos eventos que un dedo: OnPointerDown y
// OnPointerUp sobre el FixedJoystick.
//
// Mira lo que se rompio en silencio hasta el 23/9: en el telefono el muñeco no disparaba
// nunca, porque la animacion solo la prendia el camino de PC y PlayerJS prendia el arma
// sola (salian balas y el muñeco quedaba parado). Y que agarrar una caja sin soltar el
// disparo, habiendose quedado sin balas, dispare en el acto.
//
// Escribe Builds/prueba_disparo.txt.
//
// OJO: entrar y salir de play hace que Unity vuelva a serializar ProjectSettings (ver la
// trampa en CLAUDE.md): despues de correrlo, revertir lo que no se toco a proposito.
[InitializeOnLoad]
public static class PruebaDisparo
{
    const string Clave = "ShowBies.PruebaDisparo";
    const string Ruta = "../Builds/prueba_disparo.txt";
    static readonly int IdDisparar = Animator.StringToHash("m_pistol_shoot");
    static readonly int IdQuieto = Animator.StringToHash("m_pistol_idle_A");
    static readonly int IdCorrer = Animator.StringToHash("m_pistol_run");

    enum Paso { Esperando, Apuntando, Soltado, SinBalas, ConCaja, Corriendo, Listo }

    static Paso paso;
    static float desde;
    static int balasAlApuntar, balasAlSoltar, balasSinBalas;
    static bool disparoQuieto, estadoDisparar, arranco;
    static bool soltoLaAnimacion, dejoDeTirar, estadoQuietoAlSoltar;
    static bool sinBalasNoAnima, conCajaTira, conCajaAnima;
    static bool corriendoCorre, corriendoDispara;
    static string estadoCorriendo = "";
    static readonly StringBuilder excepciones = new StringBuilder();
    static int cuantasExcepciones;

    static PruebaDisparo()
    {
        EditorApplication.update += Tick;
        Application.logMessageReceived += AnotarExcepcion;
    }

    static void AnotarExcepcion(string mensaje, string pila, LogType tipo)
    {
        if (!SessionState.GetBool(Clave, false)) return;
        if (tipo != LogType.Exception && tipo != LogType.Error) return;
        cuantasExcepciones++;
        if (cuantasExcepciones <= 3) excepciones.AppendLine("  " + mensaje + System.Environment.NewLine + pila);
    }

    [MenuItem("ShowBies/Pruebas/Disparo con el joystick (play)")]
    // Publico para poder correrlo por codigo (ver la trampa del registro de menus).
    public static void Arrancar()
    {
        // El camino del telefono: con "Teclado y mouse en el editor" prendido, PlayerJS no
        // corre. Se apaga mientras dura y se devuelve como estaba.
        SessionState.SetBool(Clave + ".teclado", EditorPrefs.GetBool(Plataforma.ClaveTecladoEnElEditor, false));
        EditorPrefs.SetBool(Plataforma.ClaveTecladoEnElEditor, false);
        Plataforma.OlvidarPreferenciaDelEditor();

        PlayerSettings.runInBackground = true;
        EditorSceneManager.OpenScene("Assets/Escenas/WaveMode.unity");
        SessionState.SetBool(Clave, true);
        SessionState.SetBool(Clave + ".empezo", false);
        EditorApplication.EnterPlaymode();
    }

    static void Tick()
    {
        if (!SessionState.GetBool(Clave, false)) return;
        if (!EditorApplication.isPlaying) return;

        if (!SessionState.GetBool(Clave + ".empezo", false))
        {
            SessionState.SetBool(Clave + ".empezo", true);
            paso = Paso.Esperando;
            desde = Time.time;
            arranco = disparoQuieto = estadoDisparar = false;
            soltoLaAnimacion = dejoDeTirar = estadoQuietoAlSoltar = false;
            sinBalasNoAnima = conCajaTira = conCajaAnima = false;
            corriendoCorre = corriendoDispara = false;
            estadoCorriendo = "";
            excepciones.Length = 0;
            cuantasExcepciones = 0;
            return;
        }

        var vida = PlayerHealth.instance;
        if (vida == null) return;
        vida.health = Mathf.Max(vida.health, 60);   // que la horda no lo mate a mitad de la prueba
        var control = vida.GetComponent<PlayerController>();
        var joysticks = vida.GetComponent<PlayerJS>();
        var animador = control != null && control.trans != null ? control.trans.GetComponent<Animator>() : null;
        if (control == null || joysticks == null || animador == null || EventSystem.current == null)
        {
            if (Time.time - desde > 10f) Terminar("no aparecieron el jugador, sus joysticks o su Animator");
            return;
        }

        float pasado = Time.time - desde;
        switch (paso)
        {
            case Paso.Esperando:
                if (pasado < 2f) return;
                arranco = Plataforma.EsMovil;
                if (!arranco) { Terminar("el editor no esta en modo telefono: el target tiene que ser Android"); return; }
                control.cantBalas = Mathf.Max(control.cantBalas, 100);
                balasAlApuntar = control.cantBalas;
                Apretar(joysticks.lookJoystick, new Vector2(1f, 0f));
                Pasar(Paso.Apuntando);
                return;

            case Paso.Apuntando:
                // Quieto y apuntando: sale el tiro y el muñeco dispara.
                if (pasado < 1.2f) return;
                disparoQuieto = animador.GetBool("shoot") && control.cantBalas < balasAlApuntar;
                estadoDisparar = EstadoActual(animador) == IdDisparar;
                Soltar(joysticks.lookJoystick);
                Pasar(Paso.Soltado);
                balasAlSoltar = -1;
                return;

            case Paso.Soltado:
                // Lo que tiro hasta que PlayerJS vio el joystick suelto, que es el cuadro
                // siguiente; de ahi en mas, nada.
                if (balasAlSoltar < 0 || pasado < 0.15f) { balasAlSoltar = control.cantBalas; return; }
                if (pasado < 0.8f) return;
                soltoLaAnimacion = !animador.GetBool("shoot");
                dejoDeTirar = control.cantBalas == balasAlSoltar;
                estadoQuietoAlSoltar = EstadoActual(animador) == IdQuieto;
                // Sin balas y apuntando: ni tiros ni animacion.
                control.cantBalas = 0;
                Apretar(joysticks.lookJoystick, new Vector2(0f, 1f));
                Pasar(Paso.SinBalas);
                return;

            case Paso.SinBalas:
                if (pasado < 0.6f) return;
                sinBalasNoAnima = !animador.GetBool("shoot");
                // Llega una caja sin soltar el joystick: dispara en el acto.
                control.cantBalas = 100;
                balasSinBalas = control.cantBalas;
                Pasar(Paso.ConCaja);
                return;

            case Paso.ConCaja:
                if (pasado < 0.6f) return;
                conCajaTira = control.cantBalas < balasSinBalas;
                conCajaAnima = animador.GetBool("shoot");
                // Corriendo y apuntando para otro lado, que es como se juega en el telefono.
                Apretar(joysticks.moveJoystick, new Vector2(1f, 0f));
                Pasar(Paso.Corriendo);
                return;

            case Paso.Corriendo:
                if (pasado < 1.2f) return;
                corriendoCorre = animador.GetBool("run");
                corriendoDispara = animador.GetBool("shoot");
                int estado = EstadoActual(animador);
                estadoCorriendo = estado == IdCorrer ? "correr" : estado == IdDisparar ? "disparar" : estado == IdQuieto ? "quieto" : "otro";
                Soltar(joysticks.moveJoystick);
                Soltar(joysticks.lookJoystick);
                Pasar(Paso.Listo);
                return;

            case Paso.Listo:
                if (pasado < 0.3f) return;
                Terminar(null);
                return;
        }
    }

    static void Pasar(Paso siguiente)
    {
        paso = siguiente;
        desde = Time.time;
    }

    // El estado al que va, si esta en transicion: es el que se esta viendo entrar.
    static int EstadoActual(Animator animador)
    {
        var info = animador.IsInTransition(0) ? animador.GetNextAnimatorStateInfo(0) : animador.GetCurrentAnimatorStateInfo(0);
        return info.shortNameHash;
    }

    // Un dedo sobre el joystick, empujando hasta el borde en esa direccion: el mismo evento
    // que manda el EventSystem al tocarlo.
    static void Apretar(Joystick joystick, Vector2 direccion)
    {
        if (joystick == null) return;
        var canvas = joystick.GetComponentInParent<Canvas>();
        Camera camara = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        Vector2 centro = RectTransformUtility.WorldToScreenPoint(camara, joystick.transform.position);
        var dedo = new PointerEventData(EventSystem.current) { position = centro + direccion * 4000f };
        joystick.OnPointerDown(dedo);
    }

    static void Soltar(Joystick joystick)
    {
        if (joystick == null) return;
        joystick.OnPointerUp(new PointerEventData(EventSystem.current));
    }

    static void Terminar(string error)
    {
        var inf = new StringBuilder();
        inf.AppendLine("Prueba del disparo con los joysticks (WaveMode, camino del telefono)");
        inf.AppendLine();
        if (error != null) inf.AppendLine("ERROR: " + error);
        inf.AppendLine("Corriendo y apuntando: estado del Animator \"" + estadoCorriendo + "\", run " + corriendoCorre + ", shoot " + corriendoDispara);
        inf.AppendLine("Errores y excepciones durante la prueba: " + cuantasExcepciones);
        if (cuantasExcepciones > 0) inf.Append(excepciones);
        inf.AppendLine();

        bool[] ok =
        {
            error == null && cuantasExcepciones == 0,
            disparoQuieto,
            estadoDisparar,
            soltoLaAnimacion && dejoDeTirar,
            estadoQuietoAlSoltar,
            sinBalasNoAnima,
            conCajaTira && conCajaAnima,
            corriendoCorre && corriendoDispara,
        };
        string[] que =
        {
            "el banco llego hasta el final, sin errores ni excepciones",
            "apuntando con el joystick salen balas y la animacion de disparo se prende",
            "el Animator entra en el estado de disparar",
            "al soltar el joystick deja de tirar y se apaga la animacion",
            "y el muñeco vuelve a quedarse quieto",
            "sin balas no hace la animacion de disparar",
            "con una caja, sin soltar el joystick, dispara en el acto",
            "corriendo y apuntando a la vez, corre y quiere disparar",
        };
        bool todo = true;
        for (int i = 0; i < ok.Length; i++)
        {
            inf.AppendLine((ok[i] ? "OK  " : "FALLA  ") + que[i]);
            todo &= ok[i];
        }
        inf.AppendLine();
        inf.AppendLine("RESULTADO: " + (todo ? "TODO OK" : "HAY FALLAS"));

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(Ruta)));
        File.WriteAllText(Ruta, inf.ToString());
        Debug.Log(inf.ToString());

        SessionState.SetBool(Clave, false);
        EditorApplication.ExitPlaymode();
        PlayerSettings.runInBackground = false;
        EditorPrefs.SetBool(Plataforma.ClaveTecladoEnElEditor, SessionState.GetBool(Clave + ".teclado", false));
        Plataforma.OlvidarPreferenciaDelEditor();
        AssetDatabase.SaveAssets();
    }
}
