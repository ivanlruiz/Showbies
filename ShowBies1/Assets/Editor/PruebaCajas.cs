using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Banco de las cajas (los power-ups) en play, del 9/10: lo que la prueba de logica no ve porque
// corre en el juego. En WaveMode, sin oleadas: tres cajas delante del jugador; que el dibujo flote
// con su salto ya hecho, que el anillo este lleno y se vacie con el tiempo (la del arma vive 12 s
// en la prueba), y que al agarrar cada una salga su cartel ("+N VIDA", "CADENCIA x1,5", "x3") y el
// indicador del HUD tome su color y diga el multiplicador. Saca fotos en Builds/cajas_*.png y
// escribe Builds/prueba_cajas.txt.
[InitializeOnLoad]
public static class PruebaCajas
{
    const string Clave = "ShowBies.PruebaCajas";
    const string Banco = "PruebaCajas";
    const string Ruta = "../Builds/prueba_cajas.txt";
    const string Escena = "Assets/Escenas/WaveMode.unity";
    const double TopeTotal = 60.0;

    enum Paso { Preparar, Mirar, Vaciar, AgarrarVida, AgarrarBalas, AgarrarArma, Listo }

    static Paso paso;
    static float desde;
    static double inicio;
    static bool empezo, terminado;
    static GameObject vida, balas, arma;
    static int anilloAlEmpezar = -1, anilloDespues = -1;
    static bool tresConAnillo, saltoHecho;
    static int vidaAntes, vidaDespues;
    static string cartelVida = "", cartelBalas = "", cartelArma = "";
    static bool cartelVidaBien, cartelBalasBien, cartelArmaBien;
    static float cadenciaBalas, cadenciaArma;
    static string indicadorBalas = "", indicadorArma = "";
    static bool colorBalasBien, colorArmaBien;
    static string error;

    static PruebaCajas()
    {
        EditorApplication.update += Tick;
    }

    [MenuItem("ShowBies/Pruebas/Cajas (play)")]
    // Publico para correrlo por codigo (ver la trampa del registro de menus).
    public static void Arrancar()
    {
        if (!RespaldoDelBanco.PuedeArrancar(Banco)) return;
        RespaldoDelBanco.Guardar(Banco);
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
            paso = Paso.Preparar;
            desde = Time.time;
            inicio = EditorApplication.timeSinceStartup;
            return;
        }
        if (EditorApplication.timeSinceStartup - inicio > TopeTotal) { Terminar("se paso del tope de " + TopeTotal + " s en " + paso); return; }
        try
        {
            Avanzar();
        }
        catch (System.Exception e)
        {
            Terminar("excepcion en " + paso + ": " + e);
        }
    }

    static void Avanzar()
    {
        var salud = PlayerHealth.instance;
        if (salud == null) return;
        var cuerpo = salud.GetComponent<Rigidbody>();
        var control = salud.GetComponent<PlayerController>();
        float t = Time.time - desde;

        switch (paso)
        {
            case Paso.Preparar:
            {
                if (t < 1f) return;
                var oleadas = Object.FindAnyObjectByType<WaveManager>();
                if (oleadas != null) { oleadas.StopAllCoroutines(); oleadas.enabled = false; }
                foreach (var p in Object.FindObjectsByType<PowerUp>(FindObjectsSortMode.None)) p.enabled = false;
                EnemyController.DespejarAlrededor(Vector3.zero, 500f);
                Mover(cuerpo, control, new Vector3(0f, 0f, -6f));
                salud.health = 30;
                vida = Poner("PUVida", new Vector3(-3f, 0.5f, -3f), 30f);
                balas = Poner("PUBalas", new Vector3(0f, 0.5f, -3f), 30f);
                arma = Poner("PUArma", new Vector3(3f, 0.5f, -3f), 12f);
                if (vida == null || balas == null || arma == null) { Terminar("no se pudieron poner las cajas"); return; }
                Pasar(Paso.Mirar);
                return;
            }

            case Paso.Mirar:
            {
                if (t < 0.8f) return;
                // Casi lleno: ya paso casi un segundo (49 puntos es la vuelta entera).
                tresConAnillo = Anillo(vida) >= 47 && Anillo(balas) >= 47 && Anillo(arma) >= 44;
                anilloAlEmpezar = Anillo(arma);
                var aspecto = vida.GetComponent<AspectoDeCaja>();
                float escala = aspecto != null && aspecto.modelo != null ? aspecto.modelo.localScale.x : 0f;
                saltoHecho = Mathf.Abs(escala - ConstructorEscalaDelDibujo) < ConstructorEscalaDelDibujo * 0.1f;
                ScreenCapture.CaptureScreenshot(Path.GetFullPath("../Builds/cajas_en_el_piso.png"));
                Pasar(Paso.Vaciar);
                return;
            }

            case Paso.Vaciar:
                if (t < 2.5f) return;
                anilloDespues = Anillo(arma);
                vidaAntes = salud.health;
                Mover(cuerpo, control, vida.transform.position);
                Pasar(Paso.AgarrarVida);
                return;

            case Paso.AgarrarVida:
                if (t < 0.35f) return;
                vidaDespues = salud.health;
                cartelVidaBien = vida == null && vidaDespues > vidaAntes && HayCartel(Textos.Formato("powerup_vida", vidaDespues - vidaAntes), out cartelVida);
                Mover(cuerpo, control, balas.transform.position);
                Pasar(Paso.AgarrarBalas);
                return;

            case Paso.AgarrarBalas:
            {
                if (t < 0.35f) return;
                cadenciaBalas = control.theGun.MultiplicadorCadencia;
                cartelBalasBien = balas == null && HayCartel(Textos.Formato("powerup_balas", FormatoNumeros.ConDecimales(control.multiplicadorCadenciaPUBalas, 1)), out cartelBalas);
                Color c;
                indicadorBalas = Indicador(out c);
                colorBalasBien = Parecido(c, ConstructorPowerUps.Oro);
                ScreenCapture.CaptureScreenshot(Path.GetFullPath("../Builds/cajas_cartel_balas.png"));
                Mover(cuerpo, control, arma.transform.position);
                Pasar(Paso.AgarrarArma);
                return;
            }

            case Paso.AgarrarArma:
            {
                if (t < 0.45f) return;
                cadenciaArma = control.theGun.MultiplicadorCadencia;
                cartelArmaBien = arma == null && HayCartel(Textos.Formato("powerup_arma", FormatoNumeros.Compacto(control.maxBalas),
                                                                          FormatoNumeros.ConDecimales(control.multiplicadorCadenciaPUArma, 1)), out cartelArma);
                Color c;
                indicadorArma = Indicador(out c);
                colorArmaBien = Parecido(c, ConstructorPowerUps.Celeste);
                ScreenCapture.CaptureScreenshot(Path.GetFullPath("../Builds/cajas_cartel_arma.png"));
                Pasar(Paso.Listo);
                return;
            }

            case Paso.Listo:
                // La captura se escribe al final del cuadro.
                if (t < 0.3f) return;
                Terminar(null);
                return;
        }
    }

    // La escala del dibujo en el prefab (ConstructorPowerUps.Escala), para ver que el salto de
    // entrada termino.
    static float ConstructorEscalaDelDibujo
    {
        get { return ConstructorPowerUps.Escala; }
    }

    static GameObject Poner(string nombre, Vector3 donde, float vidaDeLaCaja)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/" + nombre + ".prefab");
        if (prefab == null) return null;
        var go = Object.Instantiate(prefab, donde, Quaternion.identity);
        var caducidad = go.GetComponent<PickupCaducidad>();
        if (caducidad != null) caducidad.vida = vidaDeLaCaja;
        return go;
    }

    static void Mover(Rigidbody cuerpo, PlayerController control, Vector3 donde)
    {
        var p = new Vector3(donde.x, cuerpo.position.y, donde.z);
        cuerpo.position = p;
        cuerpo.linearVelocity = Vector3.zero;
        control.transform.position = p;
    }

    static int Anillo(GameObject caja)
    {
        var anillo = caja != null ? caja.transform.Find("Anillo") : null;
        var linea = anillo != null ? anillo.GetComponent<LineRenderer>() : null;
        return linea != null ? linea.positionCount : -1;
    }

    // Si entre los carteles en pantalla esta el esperado (el de la caja anterior puede seguir
    // ahi: dura 1,4 s). En 'vistos', todos, para el informe.
    static bool HayCartel(string esperado, out string vistos)
    {
        bool esta = false;
        var lista = new StringBuilder();
        foreach (var n in Object.FindObjectsByType<NumeroFlotante>(FindObjectsSortMode.None))
        {
            if (!n.gameObject.activeInHierarchy) continue;
            string texto = n.GetComponent<TMPro.TMP_Text>().text;
            if (texto.Length <= 4) continue;
            esta |= texto == esperado;
            lista.Append("[" + texto.Replace('\n', ' ') + "] ");
        }
        vistos = lista.Length > 0 ? lista.ToString() : "(ninguno)";
        return esta;
    }

    static string Indicador(out Color color)
    {
        color = Color.clear;
        var indicador = Object.FindAnyObjectByType<IndicadorMejoraCadencia>();
        if (indicador == null || !indicador.contenido.activeSelf) return "(apagado)";
        color = indicador.relleno.color;
        var multiplicador = indicador.contenido.GetComponentsInChildren<TMPro.TMP_Text>(true);
        foreach (var m in multiplicador)
            if (m.name == "Multiplicador") return m.text;
        return "(sin multiplicador)";
    }

    static bool Parecido(Color a, Color b)
    {
        return Mathf.Abs(a.r - b.r) < 0.05f && Mathf.Abs(a.g - b.g) < 0.05f && Mathf.Abs(a.b - b.b) < 0.05f;
    }

    static void Pasar(Paso siguiente)
    {
        paso = siguiente;
        desde = Time.time;
    }

    static void Terminar(string motivo)
    {
        if (terminado) return;
        terminado = true;
        error = motivo;
        var inf = new StringBuilder();
        inf.AppendLine("Prueba de las cajas en play (WaveMode, sin oleadas)");
        inf.AppendLine();
        if (error != null) inf.AppendLine("ERROR: " + error);
        inf.AppendLine("Anillo: " + anilloAlEmpezar + " puntos al empezar, " + anilloDespues + " despues de 2,5 s (la del arma vive 12 s); el salto de entrada termino " + saltoHecho);
        inf.AppendLine("Vida: " + vidaAntes + " -> " + vidaDespues + ", cartel \"" + cartelVida + "\"");
        inf.AppendLine("Balas: cadencia x" + cadenciaBalas + ", cartel \"" + cartelBalas + "\", indicador \"" + indicadorBalas + "\"");
        inf.AppendLine("Arma: cadencia x" + cadenciaArma + ", cartel \"" + cartelArma + "\", indicador \"" + indicadorArma + "\"");
        inf.AppendLine();
        bool[] ok =
        {
            error == null,
            tresConAnillo && saltoHecho,
            anilloDespues > 1 && anilloDespues < anilloAlEmpezar,
            cartelVidaBien,
            cartelBalasBien && Mathf.Approximately(cadenciaBalas, 1.5f),
            cartelArmaBien && Mathf.Approximately(cadenciaArma, 3f),
            colorBalasBien && indicadorBalas == Textos.Formato("hud_multiplicador", FormatoNumeros.ConDecimales(1.5, 1)),
            colorArmaBien && indicadorArma == Textos.Formato("hud_multiplicador", FormatoNumeros.ConDecimales(3, 1)),
        };
        string[] que =
        {
            "el banco llego hasta el final, sin excepciones",
            "las tres cajas tienen el anillo lleno y el dibujo ya salio",
            "el anillo se vacia con el tiempo que le queda",
            "la vida cura y su cartel dice cuanto",
            "las balas suben la cadencia x1,5 y su cartel lo dice",
            "el rayo sube la cadencia x3 y su cartel dice el cargador y el x3",
            "el indicador del HUD es dorado con las balas y dice x1,5",
            "el indicador del HUD es celeste con el rayo y dice x3",
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
        SessionState.SetBool(Clave, false);
        EditorApplication.ExitPlaymode();
        Debug.Log(inf.ToString());
    }
}
