using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;

// Banco del AUTO-APUNTAR, en play (10/10, de la revision de Gemini): en el telefono, sin el pulgar
// en el joystick de disparo, el jugador apunta solo al zombi vivo mas cercano que se ve y dispara
// (PlayerJS). En WaveMode, con las oleadas paradas y el mapa despejado, el jugador quieto en el
// centro y zombis puestos a mano:
//  - uno a la vista: lo apunta, dispara y lo mata;
//  - uno lejos, fuera de la pantalla: no le tira;
//  - el joystick manda: con uno a la vista, apuntar al otro lado gira y dispara para alla, y al
//    soltar vuelve a apuntarle solo;
//  - apagado no dispara solo.
// En cada cuadro, la lista de vivos (EnemyController.Vivos) tiene que ser la cuenta de vivos.
// Escribe Builds/prueba_auto_apuntar.txt.
[InitializeOnLoad]
public static class PruebaAutoApuntar
{
    const string Clave = "ShowBies.PruebaAutoApuntar";
    const string Banco = "PruebaAutoApuntar";
    const string Ruta = "../Builds/prueba_auto_apuntar.txt";
    const string Escena = "Assets/Escenas/WaveMode.unity";
    const double TopeTotal = 90.0;

    enum Fase { Preparar, AlaVista, Lejos, ElJoystickManda, Soltar, Apagado, Fin }

    static Fase fase;
    static float desde;
    static double inicio;
    static bool empezo, terminado;
    static EnemyController zombi;
    static int numeroDelZombi;
    static string error;
    static readonly List<KeyValuePair<string, bool>> oks = new List<KeyValuePair<string, bool>>();
    static int cuadrosConLaListaMal, cuadrosMirados;
    static bool loApunto, disparoAlVerlo, loMato;
    static bool lejosSinBlanco = true, lejosSinDisparar = true;
    static bool joystickSinBlanco = true, joystickHaciaElOeste, joystickDispara;
    static bool alSoltarLoApunta;
    static bool apagadoSinDisparar = true, apagadoSinBlanco = true;
    // El automatico tira a la mitad y el joystick se ve apagado; a mano, entero (12/10, de Gemini).
    static bool mitadAlVerlo, joystickApagadoAlVerlo, enteraAMano, joystickPrendidoAMano;

    static PruebaAutoApuntar()
    {
        EditorApplication.update += Tick;
    }

    [MenuItem("ShowBies/Pruebas/Auto-apuntar (play)")]
    // Publico para correrlo por codigo (ver la trampa del registro de menus).
    public static void Arrancar()
    {
        if (!RespaldoDelBanco.PuedeArrancar(Banco)) return;
        RespaldoDelBanco.Guardar(Banco);
        PruebaDisparo.ModoTelefono(Clave);
        PlayerSettings.runInBackground = true;
        Progreso.GuardarOleadaEnCurso(1, 0);
        Progreso.Guardar();
        EditorSceneManager.OpenScene(Escena);
        SessionState.SetBool(Clave, true);
        empezo = false;
        EditorApplication.EnterPlaymode();
    }

    static void Tick()
    {
        if (!SessionState.GetBool(Clave, false)) return;
        if (!EditorApplication.isPlaying) return;
        if (!RespaldoDelBanco.SigueArmado(Banco, Clave)) return;

        if (!empezo)
        {
            empezo = true;
            terminado = false;
            error = null;
            oks.Clear();
            cuadrosConLaListaMal = cuadrosMirados = 0;
            loApunto = disparoAlVerlo = loMato = false;
            lejosSinBlanco = lejosSinDisparar = true;
            joystickSinBlanco = true;
            joystickHaciaElOeste = joystickDispara = false;
            alSoltarLoApunta = false;
            apagadoSinDisparar = apagadoSinBlanco = true;
            mitadAlVerlo = joystickApagadoAlVerlo = enteraAMano = joystickPrendidoAMano = false;
            fase = Fase.Preparar;
            desde = Time.time;
            inicio = EditorApplication.timeSinceStartup;
            return;
        }
        if (EditorApplication.timeSinceStartup - inicio > TopeTotal) { Terminar("se paso del tope de " + TopeTotal + " s"); return; }

        try
        {
            Avanzar();
        }
        catch (System.Exception e)
        {
            Terminar("excepcion: " + e);
        }
    }

    static GameObject Prefab()
    {
        return AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Personajes/Zombi.prefab");
    }

    static EnemyController Poner(Vector3 donde)
    {
        var z = EnemyController.Aparecer(Prefab(), donde);
        if (z != null) numeroDelZombi = z.NumeroDeAparicion;
        return z;
    }

    static void Avanzar()
    {
        var vida = PlayerHealth.instance;
        if (vida == null || EventSystem.current == null) return;
        vida.health = vida.maxHealth;
        var control = vida.GetComponent<PlayerController>();
        var joysticks = vida.GetComponent<PlayerJS>();
        var cuerpo = vida.GetComponent<Rigidbody>();
        if (control == null || joysticks == null || cuerpo == null || joysticks.thegun == null)
        {
            Terminar("el jugador no tiene control, joysticks, arma o cuerpo");
            return;
        }

        // La lista y la cuenta, en cada cuadro.
        cuadrosMirados++;
        if (EnemyController.Vivos.Count != EnemyController.ZombisVivos) cuadrosConLaListaMal++;

        // Quieto en el centro, un poco al sur, durante toda la prueba.
        PruebaDisparo.Soltar(joysticks.moveJoystick);
        Vector3 centro = new Vector3(0f, cuerpo.position.y, -6f);
        bool disparando = joysticks.thegun.isFiring;
        float t = Time.time - desde;

        switch (fase)
        {
            case Fase.Preparar:
                if (t < 1f) return;
                var oleadas = Object.FindAnyObjectByType<WaveManager>();
                if (oleadas != null) { oleadas.StopAllCoroutines(); oleadas.enabled = false; }
                foreach (var cajas in Object.FindObjectsByType<PowerUp>(FindObjectsSortMode.None)) cajas.enabled = false;
                EnemyController.DespejarAlrededor(Vector3.zero, 500f);
                cuerpo.position = centro;
                cuerpo.linearVelocity = Vector3.zero;
                control.transform.position = centro;
                PlayerJS.FijarAutoApuntar(true);
                PruebaDisparo.Soltar(joysticks.lookJoystick);
                zombi = Poner(new Vector3(6f, 0f, -6f));
                if (zombi == null) { Terminar("no aparecio el zombi"); return; }
                Pasar(Fase.AlaVista);
                return;

            case Fase.AlaVista:
                if (joysticks.Blanco == zombi) loApunto = true;
                if (loApunto && disparando) disparoAlVerlo = true;
                if (loApunto && disparando && joysticks.thegun.MultiplicadorAutomatico == GunController.CadenciaDelAutomatico) mitadAlVerlo = true;
                if (loApunto && Mathf.Abs(joysticks.AlfaDelJoystickDeDisparo - PlayerJS.AlfaDelJoystickEnAutomatico) < 0.02f) joystickApagadoAlVerlo = true;
                if (!EnemyController.SigueVivo(zombi, numeroDelZombi)) loMato = true;
                if (!loMato && t < 6f) return;
                EnemyController.DespejarAlrededor(Vector3.zero, 500f);
                zombi = Poner(new Vector3(30f, 0f, 30f));
                Pasar(Fase.Lejos);
                return;

            case Fase.Lejos:
                // Que llegue el apagado del disparo del cuadro anterior.
                if (t < 0.2f) return;
                if (joysticks.Blanco != null) lejosSinBlanco = false;
                if (disparando) lejosSinDisparar = false;
                if (t < 1.2f) return;
                EnemyController.DespejarAlrededor(Vector3.zero, 500f);
                zombi = Poner(new Vector3(6f, 0f, -6f));
                Pasar(Fase.ElJoystickManda);
                return;

            case Fase.ElJoystickManda:
                PruebaDisparo.Apretar(joysticks.lookJoystick, Vector2.left);
                if (t < 0.2f) return;
                if (joysticks.Blanco != null) joystickSinBlanco = false;
                if (control.transform.forward.x < -0.9f) joystickHaciaElOeste = true;
                if (disparando) joystickDispara = true;
                if (t > 0.5f && disparando && joysticks.thegun.MultiplicadorAutomatico == 1f) enteraAMano = true;
                if (t > 0.5f && joysticks.AlfaDelJoystickDeDisparo > 0.98f) joystickPrendidoAMano = true;
                if (t < 1f) return;
                PruebaDisparo.Soltar(joysticks.lookJoystick);
                Pasar(Fase.Soltar);
                return;

            case Fase.Soltar:
                if (joysticks.Blanco != null && joysticks.Blanco.Vivo) alSoltarLoApunta = true;
                if (!alSoltarLoApunta && t < 2f) return;
                EnemyController.DespejarAlrededor(Vector3.zero, 500f);
                PlayerJS.FijarAutoApuntar(false);
                zombi = Poner(new Vector3(6f, 0f, -6f));
                Pasar(Fase.Apagado);
                return;

            case Fase.Apagado:
                if (t < 0.2f) return;
                if (joysticks.Blanco != null) apagadoSinBlanco = false;
                if (disparando) apagadoSinDisparar = false;
                if (t < 1f) return;
                Terminar(null);
                return;
        }
    }

    static void Pasar(Fase siguiente)
    {
        fase = siguiente;
        desde = Time.time;
    }

    static void Terminar(string motivo)
    {
        if (terminado) return;
        terminado = true;
        if (error == null) error = motivo;

        oks.Add(new KeyValuePair<string, bool>("el banco llego hasta el final", error == null));
        oks.Add(new KeyValuePair<string, bool>("a la vista: lo apunta solo", loApunto));
        oks.Add(new KeyValuePair<string, bool>("a la vista: dispara", disparoAlVerlo));
        oks.Add(new KeyValuePair<string, bool>("a la vista: lo mata", loMato));
        oks.Add(new KeyValuePair<string, bool>("el automatico tira a la mitad de la cadencia", mitadAlVerlo));
        oks.Add(new KeyValuePair<string, bool>("con el automatico el joystick de disparo se ve apagado", joystickApagadoAlVerlo));
        oks.Add(new KeyValuePair<string, bool>("lejos y fuera de la pantalla: no lo apunta", lejosSinBlanco));
        oks.Add(new KeyValuePair<string, bool>("lejos y fuera de la pantalla: no dispara", lejosSinDisparar));
        oks.Add(new KeyValuePair<string, bool>("el joystick manda: sin blanco mientras se apunta", joystickSinBlanco));
        oks.Add(new KeyValuePair<string, bool>("el joystick manda: gira hacia donde se apunta (el oeste)", joystickHaciaElOeste));
        oks.Add(new KeyValuePair<string, bool>("el joystick manda: dispara", joystickDispara));
        oks.Add(new KeyValuePair<string, bool>("el joystick manda: con la cadencia entera", enteraAMano));
        oks.Add(new KeyValuePair<string, bool>("el joystick manda: se ve prendido", joystickPrendidoAMano));
        oks.Add(new KeyValuePair<string, bool>("al soltar el joystick vuelve a apuntarle solo", alSoltarLoApunta));
        oks.Add(new KeyValuePair<string, bool>("apagado: no lo apunta", apagadoSinBlanco));
        oks.Add(new KeyValuePair<string, bool>("apagado: no dispara solo", apagadoSinDisparar));
        oks.Add(new KeyValuePair<string, bool>("la lista de vivos coincide con la cuenta en cada cuadro (" + cuadrosMirados + " cuadros, "
                                               + cuadrosConLaListaMal + " mal)", cuadrosMirados > 0 && cuadrosConLaListaMal == 0));

        var inf = new StringBuilder();
        inf.AppendLine("Prueba del auto-apuntar (WaveMode, sin oleadas, el jugador quieto en el centro)");
        inf.AppendLine();
        if (error != null) inf.AppendLine("ERROR: " + error);
        bool todo = true;
        foreach (var ok in oks)
        {
            inf.AppendLine((ok.Value ? "OK  " : "FALLA  ") + ok.Key);
            todo &= ok.Value;
        }
        inf.AppendLine();
        inf.AppendLine("RESULTADO: " + (todo ? "TODO OK" : "HAY FALLAS"));
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(Ruta)));
        File.WriteAllText(Ruta, inf.ToString());

        SessionState.SetBool(Clave, false);
        EditorApplication.ExitPlaymode();
        Debug.Log(inf.ToString());
    }
}
