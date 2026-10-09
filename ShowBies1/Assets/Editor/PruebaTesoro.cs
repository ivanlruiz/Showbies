using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Banco del zombi del tesoro, en play (revision del 9/10, mejora 8): WaveMode retomada en la
// oleada 3, con los zombis de la oleada muriendo apenas salen (asi nadie le pega al jugador).
// Saca un tesoro (WaveManager.SacarTesoro) y mira que no cuente en la oleada, que se aleje del
// jugador sin pegarle, que lleve su flecha si sale de la pantalla y que al matarlo suelte la
// lluvia de monedas entera y festeje. Despues saca otro con 2 s de vida y mira que se escape
// sin contar como muerte: las monedas y los puntos salen del mismo bloque de la muerte
// (EnemyController.DanoZombi), y escaparse no pasa por ahi. Las monedas del piso no sirven
// para mirarlo: los de la oleada siguen saliendo y muriendo, y en 2 s sueltan mas que una lluvia. Saca una foto (Builds/tesoro.png) y escribe
// Builds/prueba_tesoro.txt.
[InitializeOnLoad]
public static class PruebaTesoro
{
    const string Clave = "ShowBies.PruebaTesoro";
    const string Banco = "PruebaTesoro";
    const string Ruta = "../Builds/prueba_tesoro.txt";
    const string Foto = "../Builds/tesoro.png";
    const string Escena = "Assets/Escenas/WaveMode.unity";
    const int Oleada = 3;
    const double TopeTotal = 60.0;

    enum Paso { Esperar, Huye, Matar, Escapar, Listo }

    static double inicio;
    static bool empezo, terminado;
    static Paso paso;
    static float desde;
    static EnemyController primero, segundo;
    static float distanciaAlSalir, distanciaDespues, vidaAlSalir, vidaDespues;
    static bool cuentaEnLaOleada, tuvoFlecha, salioDeLaPantalla, festejo, foto;
    static int monedasAntes, monedasDespues, monedasEscapo, muertesAntesDelEscape, muertesDespuesDelEscape;
    static bool seEscapo, sinMonedasAlEscapar, cuentaBien;
    static string cuenta = "";

    static PruebaTesoro()
    {
        EditorApplication.update += Tick;
    }

    [MenuItem("ShowBies/Pruebas/Zombi del tesoro (play)")]
    public static void Arrancar()
    {
        if (!RespaldoDelBanco.PuedeArrancar(Banco)) return;
        RespaldoDelBanco.Guardar(Banco);
        PlayerSettings.runInBackground = true;
        Progreso.GuardarOleadaEnCurso(Oleada, 0);
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
            paso = Paso.Esperar;
            primero = segundo = null;
            cuentaEnLaOleada = tuvoFlecha = salioDeLaPantalla = festejo = foto = seEscapo = sinMonedasAlEscapar = cuentaBien = false;
            cuenta = "";
            distanciaAlSalir = distanciaDespues = vidaAlSalir = vidaDespues = -1f;
            monedasAntes = monedasDespues = monedasEscapo = muertesAntesDelEscape = muertesDespuesDelEscape = -1;
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

    static void Avanzar()
    {
        var vida = PlayerHealth.instance;
        var oleadas = Object.FindAnyObjectByType<WaveManager>();
        if (vida == null || oleadas == null || Camera.main == null) return;
        float t = Time.time;
        Vector3 jugador = vida.transform.position;

        // Los de la oleada se mueren apenas salen: asi nadie le pega al jugador.
        foreach (var z in Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None))
            if (z.Vivo && z.GetComponent<ZombiDelTesoro>() == null) z.DanoZombi(1e9f);

        switch (paso)
        {
            case Paso.Esperar:
                if (oleadas.OleadaActual < Oleada || oleadas.EnDescanso) return;
                primero = oleadas.SacarTesoro();
                if (primero == null) { Terminar("no salio el tesoro"); return; }
                distanciaAlSalir = Plano(primero.transform.position - jugador).magnitude;
                vidaAlSalir = vida.health;
                var enLaOleada = new List<EnemyController>();
                oleadas.FaltanDeLaOleada(enLaOleada);
                cuentaEnLaOleada = enLaOleada.Contains(primero);
                desde = t;
                paso = Paso.Huye;
                return;

            case Paso.Huye:
                if (!foto && t - desde > 0.4f)
                {
                    foto = true;
                    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(Foto)));
                    ScreenCapture.CaptureScreenshot(Foto);
                }
                Vector3 p = Camera.main.WorldToScreenPoint(primero.transform.position);
                bool fuera = p.z < 0f || p.x < 0f || p.y < 0f || p.x > Screen.width || p.y > Screen.height;
                if (fuera)
                {
                    salioDeLaPantalla = true;
                    foreach (var v in FlechasDelBorde.Vistas)
                        if (v.que == FlechasDelBorde.Que.Tesoro && v.objetivo == primero) tuvoFlecha = true;
                }
                if (t - desde < 2.5f) return;
                distanciaDespues = Plano(primero.transform.position - jugador).magnitude;
                vidaDespues = vida.health;
                monedasAntes = MonedasEnElPiso();
                primero.DanoZombi(1e9f);
                desde = t;
                paso = Paso.Matar;
                return;

            case Paso.Matar:
                if (t - desde < 0.3f) return;
                monedasDespues = MonedasEnElPiso();
                var componente = primero.GetComponent<ZombiDelTesoro>();
                festejo = componente != null && componente.Atrapado;
                segundo = oleadas.SacarTesoro();
                if (segundo == null) { Terminar("no salio el segundo tesoro"); return; }
                segundo.GetComponent<ZombiDelTesoro>().duracion = 2f;
                monedasEscapo = MonedasEnElPiso();
                muertesAntesDelEscape = Progreso.Matados("ZombiTesoro");
                desde = t;
                paso = Paso.Escapar;
                return;

            case Paso.Escapar:
                if (t - desde < 2.4f) return;
                var delSegundo = segundo.GetComponent<ZombiDelTesoro>();
                seEscapo = !segundo.Vivo && delSegundo.SeEscapo && !delSegundo.Atrapado;
                muertesDespuesDelEscape = Progreso.Matados("ZombiTesoro");
                sinMonedasAlEscapar = muertesDespuesDelEscape == muertesAntesDelEscape;
                // Que escaparse no deje la cuenta de vivos alta: un contador que no baja tapa al
                // generador para siempre. Se compara con los que de verdad estan vivos (los de la
                // oleada siguen saliendo y muriendo, asi que antes y despues no sirve).
                int deVerdad = 0;
                foreach (var z in Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None)) if (z.Vivo) deVerdad++;
                cuentaBien = EnemyController.ZombisVivos == deVerdad;
                cuenta = EnemyController.ZombisVivos + " en la cuenta y " + deVerdad + " vivos de verdad";
                paso = Paso.Listo;
                Terminar(null);
                return;
        }
    }

    static Vector3 Plano(Vector3 v)
    {
        v.y = 0f;
        return v;
    }

    // Las monedas que hay en el piso (o volando), sin los caramelos.
    static int MonedasEnElPiso()
    {
        int n = 0;
        foreach (var m in Object.FindObjectsByType<Moneda>(FindObjectsSortMode.None))
            if (m.isActiveAndEnabled && !m.caramelo) n++;
        return n;
    }

    static void Terminar(string error)
    {
        if (terminado) return;
        terminado = true;
        var enemigo = AssetDatabase.LoadAssetAtPath<Enemy>(ConstructorTesoro.RutaEnemigo);
        int minimo = enemigo != null ? enemigo.monedasMin : 10;
        var inf = new StringBuilder();
        inf.AppendLine("Prueba del zombi del tesoro (WaveMode, oleada " + Oleada + ")");
        inf.AppendLine();
        if (error != null) inf.AppendLine("ERROR: " + error);
        inf.AppendLine("Salio a " + distanciaAlSalir.ToString("0.0") + " m del jugador; a los 2,5 s estaba a " + distanciaDespues.ToString("0.0") +
                       " m. Vida del jugador: " + vidaAlSalir + " y " + vidaDespues + ". Salio de la pantalla: " + salioDeLaPantalla + ", con flecha: " + tuvoFlecha);
        inf.AppendLine("Monedas en el piso antes de matarlo: " + monedasAntes + ", despues: " + monedasDespues + " (suelta de " + minimo + " para arriba)");
        inf.AppendLine("El segundo se escapo: " + seEscapo + "; muertes de tesoros " + muertesAntesDelEscape + " antes y " + muertesDespuesDelEscape + " despues; " + cuenta);
        inf.AppendLine("Foto: " + Foto);
        inf.AppendLine();

        var ok = new List<bool>
        {
            error == null,
            !cuentaEnLaOleada,
            distanciaDespues > distanciaAlSalir + 3f,
            vidaDespues >= vidaAlSalir && vidaAlSalir > 0f,
            !salioDeLaPantalla || tuvoFlecha,
            monedasDespues - monedasAntes >= minimo,
            festejo,
            seEscapo,
            sinMonedasAlEscapar && muertesDespuesDelEscape == muertesAntesDelEscape && muertesAntesDelEscape >= 1,
            cuentaBien,
        };
        var que = new List<string>
        {
            "el banco llego hasta el final",
            "no cuenta en la oleada (la oleada no lo espera)",
            "huye: se aleja del jugador",
            "no le pega al jugador (no llega a tocarlo: lo que lo asegura es su golpe en 0 y sin zarpazos)",
            "si sale de la pantalla, tiene su flecha",
            "al matarlo suelta la lluvia de monedas entera",
            "y festeja (¡TESORO!)",
            "el que no se mata se escapa a su tiempo",
            "sin contar como muerte (sin monedas ni puntos, que salen de la muerte)",
            "y la cuenta de zombis vivos queda bien (ZombisVivos)",
        };
        bool todo = true;
        for (int i = 0; i < ok.Count; i++)
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
