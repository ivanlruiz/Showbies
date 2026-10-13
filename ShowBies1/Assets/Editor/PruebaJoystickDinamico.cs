using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

// Banco de los JOYSTICKS DINAMICOS, en play (12/10, de Gemini): el joystick aparece donde se apoya
// el pulgar (JoystickDinamico). En WaveMode con el progreso de cero (asi sale la guia de la primera
// partida), las oleadas paradas y el jugador quieto en el centro, con el camino del telefono y los
// toques puestos a mano (JoystickDinamico.DedoDePrueba: el editor no tiene dedos):
//  - instalados, sin tomar toques propios, el de mover invisible y el de disparo al 40 % (con el
//    auto-apuntado);
//  - la guia: el pulgar fantasma en el medio de la zona, con el joystick fantasma y su cartel;
//  - un toque en la zona izquierda pone ahi el joystick con la palanca al medio, y se ve;
//  - arrastrar mueve al jugador, y mas alla del radio el joystick sigue al pulgar; la guia se va;
//  - soltar lo devuelve a su rincon, invisible;
//  - el de disparo: aparece entero, apunta y dispara; al soltar vuelve al 40 %;
//  - la granada y la pausa se quedan con sus toques; la vida, que no atiende nada, no;
//  - la pausa suelta el joystick;
//  - con la opcion apagada (y la escena recargada) son los fijos de siempre.
// Escribe Builds/prueba_joystick_dinamico.txt.
[InitializeOnLoad]
public static class PruebaJoystickDinamico
{
    const string Clave = "ShowBies.PruebaJoystickDinamico";
    const string Banco = "PruebaJoystickDinamico";
    const string Ruta = "../Builds/prueba_joystick_dinamico.txt";
    const string Escena = "Assets/Escenas/WaveMode.unity";
    const double TopeTotal = 90.0;

    enum Fase { Preparar, Reposo, Apoyar, Arrastrar, Lejos, Soltar, Disparo, SoltarDisparo, Exclusion, Pausa, Fijos, MirarFijos }

    static Fase fase;
    static float desde;
    static double inicio;
    static bool empezo, terminado;
    static string error;
    static readonly List<KeyValuePair<string, bool>> oks = new List<KeyValuePair<string, bool>>();
    static Vector2 toqueIzquierdo, toqueDerecho;
    static Vector3 dondeEmpezo;
    static bool seVioAlTocar;
    static bool primerCuadro;   // el primer cuadro de cada fase

    static PruebaJoystickDinamico()
    {
        EditorApplication.update += Tick;
    }

    [MenuItem("ShowBies/Pruebas/Joysticks dinamicos (play)")]
    // Publico para correrlo por codigo (ver la trampa del registro de menus).
    public static void Arrancar()
    {
        if (!RespaldoDelBanco.PuedeArrancar(Banco)) return;
        RespaldoDelBanco.Guardar(Banco);
        PruebaDisparo.ModoTelefono(Clave);
        PlayerSettings.runInBackground = true;
        // De cero, para que salga la guia de la primera partida (el respaldo lo devuelve).
        Progreso.ReiniciarTodo();
        Progreso.Guardar();
        JoystickDinamico.Fijar(true);
        PlayerJS.FijarAutoApuntar(true);
        PlayerPrefs.Save();
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
            seVioAlTocar = false;
            fase = Fase.Preparar;
            desde = Time.unscaledTime;
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

    static void Ok(string caso, bool bien)
    {
        oks.Add(new KeyValuePair<string, bool>(caso, bien));
    }

    static Vector2 EnPantalla(Component c)
    {
        var canvas = c.GetComponentInParent<Canvas>();
        Camera camara = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        var rt = (RectTransform)c.transform;
        return RectTransformUtility.WorldToScreenPoint(camara, rt.TransformPoint(rt.rect.center));
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
        var izquierdo = joysticks.DinamicoDeMover;
        var derecho = joysticks.DinamicoDeDisparo;
        // En tiempo real: la pausa congela el escalado.
        float t = Time.unscaledTime - desde;
        float tReal = t;

        switch (fase)
        {
            case Fase.Preparar:
            {
                if (t < 1f) return;
                var oleadas = Object.FindAnyObjectByType<WaveManager>();
                if (oleadas != null) { oleadas.StopAllCoroutines(); oleadas.enabled = false; }
                foreach (var cajas in Object.FindObjectsByType<PowerUp>(FindObjectsSortMode.None)) cajas.enabled = false;
                EnemyController.DespejarAlrededor(Vector3.zero, 500f);
                Vector3 centro = new Vector3(0f, cuerpo.position.y, -6f);
                cuerpo.position = centro;
                cuerpo.linearVelocity = Vector3.zero;
                control.transform.position = centro;
                // La granada, comprada, para ver que se queda con sus toques.
                control.GranadaDesbloqueada = true;

                bool instalados = izquierdo != null && derecho != null;
                Ok("con la opcion prendida, los dos joysticks son dinamicos", instalados);
                if (!instalados) { Terminar("no se instalaron los joysticks dinamicos"); return; }
                var grupoIzq = joysticks.moveJoystick.GetComponent<CanvasGroup>();
                var grupoDer = joysticks.lookJoystick.GetComponent<CanvasGroup>();
                Ok("los joysticks no toman toques propios (los reparte JoystickDinamico)",
                   grupoIzq != null && !grupoIzq.blocksRaycasts && grupoDer != null && !grupoDer.blocksRaycasts);

                // Un punto libre de cada zona: a un tercio de su ancho desde el medio y a la mitad de su alto.
                Rect zonaIzq = izquierdo.ZonaEnPantalla();
                Rect zonaDer = derecho.ZonaEnPantalla();
                toqueIzquierdo = new Vector2(zonaIzq.xMin + zonaIzq.width * 0.6f, zonaIzq.yMin + zonaIzq.height * 0.55f);
                toqueDerecho = new Vector2(zonaDer.xMin + zonaDer.width * 0.4f, zonaDer.yMin + zonaDer.height * 0.55f);
                Pasar(Fase.Reposo);
                return;
            }

            case Fase.Reposo:
            {
                if (tReal < 0.5f) return;
                Ok("en reposo, el de mover no se ve (alfa " + joysticks.AlfaDelJoystickDeMover.ToString("0.00") + ")",
                   joysticks.AlfaDelJoystickDeMover < 0.01f);
                Ok("en reposo, el de disparo espera al 40 % con el auto-apuntado (alfa " + joysticks.AlfaDelJoystickDeDisparo.ToString("0.00") + ")",
                   Mathf.Abs(joysticks.AlfaDelJoystickDeDisparo - PlayerJS.AlfaDelJoystickEnAutomatico) < 0.02f);
                Ok("un toque libre en cada zona es de su joystick, y no del otro",
                   izquierdo.EsMio(toqueIzquierdo) && derecho.EsMio(toqueDerecho) && !izquierdo.EsMio(toqueDerecho) && !derecho.EsMio(toqueIzquierdo));
                Ok("arriba del 70 % del alto no es de ninguno",
                   !izquierdo.EsMio(new Vector2(toqueIzquierdo.x, Screen.height * 0.85f)) && !derecho.EsMio(new Vector2(toqueDerecho.x, Screen.height * 0.85f)));

                // La guia de la primera partida, en el medio de la zona de mover.
                var guia = joysticks.moveJoystick.transform.parent.Find("GuiaPrimeraPartida");
                bool fantasma = guia != null && guia.Find("JoystickFantasma") != null;
                var cartel = guia != null ? guia.GetComponentInChildren<TMPro.TMP_Text>(true) : null;
                Ok("la guia esta en el medio de la zona de mover, con el joystick fantasma",
                   guia != null && fantasma && ((RectTransform)guia).anchorMin == JoystickDinamico.ZonaIzquierda.center);
                Ok("la guia dice que se arrastra en cualquier parte", cartel != null && cartel.text == Textos.De("guia_mover_dinamico"));

                dondeEmpezo = control.transform.position;
                izquierdo.Apoyar(toqueIzquierdo, JoystickDinamico.DedoDePrueba);
                Pasar(Fase.Apoyar);
                return;
            }

            case Fase.Apoyar:
            {
                if (primerCuadro)
                {
                    primerCuadro = false;
                    Vector2 centro = EnPantalla(joysticks.moveJoystick);
                    Ok("al apoyar, el centro del joystick queda bajo el pulgar (a " + (centro - toqueIzquierdo).magnitude.ToString("0.0") + " px)",
                       (centro - toqueIzquierdo).magnitude < 1.5f);
                    Ok("al apoyar, la palanca arranca al medio", joysticks.moveJoystick.Direction == Vector2.zero);
                }
                if (tReal < 0.1f) return;
                Ok("con el pulgar encima se ve entero", joysticks.AlfaDelJoystickDeMover > 0.99f);
                izquierdo.Arrastrar(toqueIzquierdo + new Vector2(60f, 0f));
                Pasar(Fase.Arrastrar);
                return;
            }

            case Fase.Arrastrar:
            {
                izquierdo.Arrastrar(toqueIzquierdo + new Vector2(60f, 0f));
                if (t < 0.6f) return;
                Ok("arrastrar a la derecha empuja la palanca a la derecha (" + joysticks.moveJoystick.Direction + ")",
                   joysticks.moveJoystick.Direction.x > 0.3f && Mathf.Abs(joysticks.moveJoystick.Direction.y) < 0.05f);
                Ok("y el jugador camina al este (" + (control.transform.position.x - dondeEmpezo.x).ToString("0.00") + " m)",
                   control.transform.position.x - dondeEmpezo.x > 1f);
                Pasar(Fase.Lejos);
                return;
            }

            case Fase.Lejos:
            {
                Vector2 lejos = toqueIzquierdo + new Vector2(0f, 600f);
                izquierdo.Arrastrar(lejos);
                var raiz = (RectTransform)joysticks.moveJoystick.transform;
                var canvas = raiz.GetComponentInParent<Canvas>();
                float radio = raiz.sizeDelta.x * 0.5f * canvas.scaleFactor;
                Vector2 centro = EnPantalla(joysticks.moveJoystick);
                Ok("mas alla del radio, el joystick sigue al pulgar y lo deja en el borde (" + (lejos - centro).magnitude.ToString("0.0")
                   + " px, radio " + radio.ToString("0.0") + ")", Mathf.Abs((lejos - centro).magnitude - radio) < 1.5f);
                Ok("con la palanca entera hacia arriba (" + joysticks.moveJoystick.Direction + ")", joysticks.moveJoystick.Direction.y > 0.99f);
                Pasar(Fase.Soltar);
                return;
            }

            case Fase.Soltar:
            {
                if (primerCuadro)
                {
                    primerCuadro = false;
                    izquierdo.Soltar();
                    return;
                }
                if (tReal < 0.15f) return;
                // La guia se va de a poco al usar el joystick (un tercio de segundo).
                var guia = joysticks.moveJoystick.transform.parent.Find("GuiaPrimeraPartida");
                if (guia != null && tReal < 0.8f) return;
                Ok("al soltar vuelve a su rincon", ((RectTransform)joysticks.moveJoystick.transform).anchoredPosition == izquierdo.Casa);
                Ok("al soltar la palanca vuelve al medio", joysticks.moveJoystick.Direction == Vector2.zero);
                Ok("al soltar no se ve", joysticks.AlfaDelJoystickDeMover < 0.01f);
                Ok("la guia se fue al usar el joystick", guia == null);

                derecho.Apoyar(toqueDerecho, JoystickDinamico.DedoDePrueba);
                derecho.Arrastrar(toqueDerecho + new Vector2(-80f, 0f));
                Pasar(Fase.Disparo);
                return;
            }

            case Fase.Disparo:
            {
                derecho.Arrastrar(toqueDerecho + new Vector2(-80f, 0f));
                if (joysticks.AlfaDelJoystickDeDisparo > 0.99f) seVioAlTocar = true;
                if (t < 0.6f) return;
                Ok("el de disparo, al tocarlo, se ve entero", seVioAlTocar);
                Ok("apunta hacia donde se arrastra (el oeste)", control.transform.forward.x < -0.9f);
                Ok("y dispara", joysticks.thegun.isFiring);
                derecho.Soltar();
                Pasar(Fase.SoltarDisparo);
                return;
            }

            case Fase.SoltarDisparo:
            {
                if (tReal < 0.4f) return;
                Ok("al soltar el de disparo vuelve a su rincon", ((RectTransform)joysticks.lookJoystick.transform).anchoredPosition == derecho.Casa);
                Ok("y vuelve al 40 % (alfa " + joysticks.AlfaDelJoystickDeDisparo.ToString("0.00") + ")",
                   Mathf.Abs(joysticks.AlfaDelJoystickDeDisparo - PlayerJS.AlfaDelJoystickEnAutomatico) < 0.02f);
                Pasar(Fase.Exclusion);
                return;
            }

            case Fase.Exclusion:
            {
                var granada = Object.FindAnyObjectByType<JoystickGranada>();
                var grupoGranada = granada != null ? granada.GetComponent<CanvasGroup>() : null;
                if (granada != null && grupoGranada != null && !grupoGranada.blocksRaycasts && t < 1f) return;
                bool granadaVisible = granada != null && (grupoGranada == null || grupoGranada.blocksRaycasts);
                Vector2 enLaGranada = granadaVisible ? EnPantalla(granada) : Vector2.zero;
                Ok("la granada se queda con sus toques (en " + enLaGranada + ")",
                   granadaVisible && JoystickDinamico.AtiendeAlgo(enLaGranada) && !derecho.EsMio(enLaGranada) && !izquierdo.EsMio(enLaGranada));
                var furia = Object.FindAnyObjectByType<BotonFuria>();
                if (furia != null && furia.boton != null && furia.boton.gameObject.activeInHierarchy)
                {
                    Vector2 enLaFuria = EnPantalla(furia.boton);
                    Ok("la furia se queda con sus toques", JoystickDinamico.AtiendeAlgo(enLaFuria) && !derecho.EsMio(enLaFuria));
                }
                // La vida, abajo al centro, es un texto que no atiende nada: no tapa la zona.
                if (vida.healthTMP != null)
                {
                    Vector2 enLaVida = EnPantalla(vida.healthTMP) + new Vector2(-20f, 0f);
                    bool tapa = !izquierdo.EsMio(enLaVida) && !derecho.EsMio(enLaVida);
                    Ok("la vida no tapa la zona aunque reciba toques (raycastTarget " + vida.healthTMP.raycastTarget + ")", !tapa);
                }

                izquierdo.Apoyar(toqueIzquierdo, JoystickDinamico.DedoDePrueba);
                Pasar(Fase.Pausa);
                return;
            }

            case Fase.Pausa:
            {
                var menu = Object.FindAnyObjectByType<MenuPausa>();
                if (menu == null) { Terminar("no hay MenuPausa"); return; }
                if (tReal < 0.1f) return;
                if (!MenuPausa.Pausado)
                {
                    menu.Pausar();
                    desde = Time.unscaledTime;
                    return;
                }
                if (tReal < 0.2f) return;
                Ok("la pausa suelta el joystick", !izquierdo.Tocado && joysticks.moveJoystick.Direction == Vector2.zero);
                menu.Reanudar();
                JoystickDinamico.Fijar(false);
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                Pasar(Fase.Fijos);
                return;
            }

            case Fase.Fijos:
                if (tReal < 1.5f) return;
                Pasar(Fase.MirarFijos);
                return;

            case Fase.MirarFijos:
            {
                if (tReal < 0.3f) return;
                Ok("con la opcion apagada, no hay dinamicos", joysticks.DinamicoDeMover == null && joysticks.DinamicoDeDisparo == null);
                var grupoIzq = joysticks.moveJoystick.GetComponent<CanvasGroup>();
                Ok("con la opcion apagada, el de mover se ve en su rincon y toma sus toques",
                   joysticks.AlfaDelJoystickDeMover > 0.99f && (grupoIzq == null || grupoIzq.blocksRaycasts));
                Terminar(null);
                return;
            }
        }
    }

    static void Pasar(Fase siguiente)
    {
        fase = siguiente;
        desde = Time.unscaledTime;
        primerCuadro = true;
    }

    static void Terminar(string motivo)
    {
        if (terminado) return;
        terminado = true;
        if (error == null) error = motivo;
        oks.Insert(0, new KeyValuePair<string, bool>("el banco llego hasta el final", error == null));

        var inf = new StringBuilder();
        inf.AppendLine("Prueba de los joysticks dinamicos (WaveMode de cero, sin oleadas, camino del telefono)");
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
