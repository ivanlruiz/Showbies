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
// sola (salian balas y el muñeco quedaba parado). Que agarrar una caja sin soltar el
// disparo, habiendose quedado sin balas, dispare en el acto. Y lo del disparo corriendo
// (pedido de Ivan): la pistola en la mano en vez del bate, las balas saliendo de su boca,
// el brazo apuntando mientras las piernas corren y el cuerpo mirando hacia donde se apunta.
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
    static readonly int IdQuieto = Animator.StringToHash("Quieto");
    static readonly int IdCorrer = Animator.StringToHash("Correr");
    static readonly int IdNada = Animator.StringToHash("Nada");
    static readonly int IdApuntar = Animator.StringToHash("Apuntar");

    enum Paso { Esperando, Apuntando, Soltado, SinBalas, ConCaja, Corriendo, Listo }

    static Paso paso;
    static float desde;
    static int balasAlApuntar, balasAlSoltar, balasSinBalas;
    static bool disparoQuieto, brazoApunta, arranco;
    static bool soltoLaAnimacion, dejoDeTirar, brazoBajoAlSoltar;
    static bool sinBalasNoAnima, conCajaTira, conCajaAnima;
    static bool corriendoCorre, corriendoDispara, piernasCorren, brazoApuntaCorriendo;
    static bool pistolaEnLaMano, balaDeLaBoca;
    // El fogonazo: cuantos salieron mientras disparaba y si se apago al soltar.
    static int fogonazosAntes, fogonazosAlDisparar;
    static bool fogonazoApagado;
    static float anguloAlApuntar = -1f, distanciaALaBoca = -1f;
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
        // Dos arranques seguidos pisarian lo que se anoto del teclado.
        if (SessionState.GetBool(Clave, false) || EditorApplication.isPlayingOrWillChangePlaymode) return;
        // El camino del telefono: con "Teclado y mouse en el editor" prendido, PlayerJS no
        // corre. Se apaga mientras dura y se devuelve como estaba.
        ModoTelefono(Clave);
        PlayerSettings.runInBackground = true;
        EditorSceneManager.OpenScene("Assets/Escenas/WaveMode.unity");
        SessionState.SetBool(Clave, true);
        SessionState.SetBool(Clave + ".empezo", false);
        EditorApplication.EnterPlaymode();
    }

    // Apaga "Teclado y mouse en el editor" y anota como estaba, con la clave del banco.
    public static void ModoTelefono(string clave)
    {
        SessionState.SetBool(clave + ".teclado", EditorPrefs.GetBool(Plataforma.ClaveTecladoEnElEditor, false));
        EditorPrefs.SetBool(Plataforma.ClaveTecladoEnElEditor, false);
        Plataforma.OlvidarPreferenciaDelEditor();
    }

    public static void DevolverElTeclado(string clave)
    {
        EditorPrefs.SetBool(Plataforma.ClaveTecladoEnElEditor, SessionState.GetBool(clave + ".teclado", false));
        Plataforma.OlvidarPreferenciaDelEditor();
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
            arranco = disparoQuieto = brazoApunta = false;
            soltoLaAnimacion = dejoDeTirar = brazoBajoAlSoltar = false;
            sinBalasNoAnima = conCajaTira = conCajaAnima = false;
            corriendoCorre = corriendoDispara = piernasCorren = brazoApuntaCorriendo = false;
            pistolaEnLaMano = balaDeLaBoca = false;
            fogonazosAntes = fogonazosAlDisparar = 0;
            fogonazoApagado = false;
            anguloAlApuntar = distanciaALaBoca = -1f;
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
        var enLaMano = vida.GetComponent<ArmaEnLaMano>();
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
                pistolaEnLaMano = PistolaEnLaMano(enLaMano, control);
                fogonazosAntes = enLaMano != null ? enLaMano.Fogonazos : 0;
                control.cantBalas = Mathf.Max(control.cantBalas, 100);
                balasAlApuntar = control.cantBalas;
                Apretar(joysticks.lookJoystick, new Vector2(1f, 0f));
                Pasar(Paso.Apuntando);
                return;

            case Paso.Apuntando:
                // Quieto y apuntando: sale el tiro, el muñeco dispara y la bala sale de la boca.
                if (pasado < 1.2f) return;
                disparoQuieto = animador.GetBool("shoot") && control.cantBalas < balasAlApuntar;
                fogonazosAlDisparar = enLaMano != null ? enLaMano.Fogonazos - fogonazosAntes : 0;
                brazoApunta = EstadoActual(animador, 1) == IdApuntar;
                if (enLaMano != null && enLaMano.Boca != null)
                {
                    distanciaALaBoca = Vector3.Distance(control.theGun.UltimaSalida, enLaMano.Boca.position);
                    balaDeLaBoca = control.theGun.boca == enLaMano.Boca && distanciaALaBoca < 0.3f;
                }
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
                fogonazoApagado = enLaMano != null && !enLaMano.FogonazoPrendido;
                dejoDeTirar = control.cantBalas == balasAlSoltar;
                brazoBajoAlSoltar = EstadoActual(animador, 1) == IdNada;
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
                // Corriendo para un lado y apuntando para otro, que es como se juega en el
                // telefono: camina hacia la derecha y apunta hacia arriba de la pantalla.
                Apretar(joysticks.moveJoystick, new Vector2(1f, 0f));
                Pasar(Paso.Corriendo);
                return;

            case Paso.Corriendo:
                if (pasado < 1.2f) return;
                corriendoCorre = animador.GetBool("run");
                corriendoDispara = animador.GetBool("shoot");
                int piernas = EstadoActual(animador, 0), brazo = EstadoActual(animador, 1);
                piernasCorren = piernas == IdCorrer;
                brazoApuntaCorriendo = brazo == IdApuntar;
                estadoCorriendo = (piernas == IdCorrer ? "correr" : piernas == IdQuieto ? "quieto" : "otro") + " / "
                                  + (brazo == IdApuntar ? "apuntar" : brazo == IdNada ? "nada" : "otro");
                anguloAlApuntar = Vector3.Angle(vida.transform.forward, Vector3.forward);
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

    // La pistola en la mano derecha, el bate del modelo escondido y el cubito del arma
    // (lo que antes se veia) sin dibujar.
    static bool PistolaEnLaMano(ArmaEnLaMano enLaMano, PlayerController control)
    {
        if (enLaMano == null || enLaMano.Boca == null) return false;
        foreach (var render in control.theGun.GetComponentsInChildren<Renderer>(true))
            if (render.enabled) return false;
        foreach (var render in control.trans.GetComponentsInChildren<Renderer>())
            if (render.name.Contains("baseballbat")) return false;
        return true;
    }

    static void Pasar(Paso siguiente)
    {
        paso = siguiente;
        desde = Time.time;
    }

    // El estado al que va, si esta en transicion: es el que se esta viendo entrar.
    static int EstadoActual(Animator animador, int capa)
    {
        var info = animador.IsInTransition(capa) ? animador.GetNextAnimatorStateInfo(capa) : animador.GetCurrentAnimatorStateInfo(capa);
        return info.shortNameHash;
    }

    // Un dedo sobre el joystick, empujando hasta el borde en esa direccion: el mismo evento
    // que manda el EventSystem al tocarlo.
    public static void Apretar(Joystick joystick, Vector2 direccion)
    {
        if (joystick == null) return;
        var canvas = joystick.GetComponentInParent<Canvas>();
        Camera camara = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        Vector2 centro = RectTransformUtility.WorldToScreenPoint(camara, joystick.transform.position);
        var dedo = new PointerEventData(EventSystem.current) { position = centro + direccion * 4000f };
        joystick.OnPointerDown(dedo);
    }

    public static void Soltar(Joystick joystick)
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
        inf.AppendLine("Corriendo y apuntando: piernas / brazo \"" + estadoCorriendo + "\", run " + corriendoCorre + ", shoot " + corriendoDispara
                       + "; el cuerpo queda a " + anguloAlApuntar.ToString("0") + " grados de donde apunta");
        inf.AppendLine("La ultima bala salio a " + distanciaALaBoca.ToString("0.00") + " m de la boca de la pistola; fogonazos mientras disparaba: "
                       + fogonazosAlDisparar);
        inf.AppendLine("Errores y excepciones durante la prueba: " + cuantasExcepciones);
        if (cuantasExcepciones > 0) inf.Append(excepciones);
        inf.AppendLine();

        bool[] ok =
        {
            error == null && cuantasExcepciones == 0,
            pistolaEnLaMano,
            disparoQuieto,
            brazoApunta,
            balaDeLaBoca,
            fogonazosAlDisparar > 0 && fogonazoApagado,
            soltoLaAnimacion && dejoDeTirar,
            brazoBajoAlSoltar,
            sinBalasNoAnima,
            conCajaTira && conCajaAnima,
            corriendoCorre && corriendoDispara,
            piernasCorren && brazoApuntaCorriendo,
            anguloAlApuntar >= 0f && anguloAlApuntar < 10f,
        };
        string[] que =
        {
            "el banco llego hasta el final, sin errores ni excepciones",
            "la pistola esta en la mano, sin el bate ni el cubito de antes",
            "apuntando con el joystick salen balas y la animacion de disparo se prende",
            "el brazo pasa a apuntar",
            "las balas salen de la boca de la pistola",
            "cada tiro prende el fogonazo, y al soltar se apaga",
            "al soltar el joystick deja de tirar y se apaga la animacion",
            "y el brazo deja de apuntar",
            "sin balas no hace la animacion de disparar",
            "con una caja, sin soltar el joystick, dispara en el acto",
            "corriendo y apuntando a la vez, corre y quiere disparar",
            "corriendo, las piernas corren y el brazo apunta",
            "corriendo para un lado, el cuerpo mira hacia donde apunta",
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
        DevolverElTeclado(Clave);
        AssetDatabase.SaveAssets();
    }
}
