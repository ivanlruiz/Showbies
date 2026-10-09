using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Banco de las flechas del borde, en play (revision del 9/10, mejora 7): WaveMode retomada en
// la oleada 3. Pone una caja de balas lejos al este, dos zombis lejos (quietos y sumados a la
// oleada) y un jefe lejos al norte, y mata a los demas zombis de la oleada a medida que salen.
// Mira que mientras falten mas de 3 no haya flechas de zombis, que con los dos ultimos haya una
// flecha hacia cada uno, una hacia la caja (de su color) y una mas grande hacia el jefe, todas
// en el borde y apuntando a lo suyo; que al traer la caja cerca su flecha se vaya, y que en la
// pausa no haya ninguna. Saca una foto (Builds/flechas.png) y escribe Builds/prueba_flechas.txt.
[InitializeOnLoad]
public static class PruebaFlechas
{
    const string Clave = "ShowBies.PruebaFlechas";
    const string Banco = "PruebaFlechas";
    const string Ruta = "../Builds/prueba_flechas.txt";
    const string Foto = "../Builds/flechas.png";
    const string Escena = "Assets/Escenas/WaveMode.unity";
    const int Oleada = 3;
    const double TopeTotal = 60.0;

    static double inicio;
    static bool empezo, terminado, puso, midio, acerco, pauso;
    static float midioEn, acercoEn, pausoEn;
    static GameObject caja;
    static readonly List<EnemyController> mios = new List<EnemyController>();
    static EnemyController jefe;
    static bool flechasDeZombiAntes;
    static int faltanAlMedir;
    static string medidas = "";
    static bool cajaBien, zombisBien, jefeBien, cajaCercaSinFlecha, pausaSinFlechas;

    static PruebaFlechas()
    {
        EditorApplication.update += Tick;
    }

    [MenuItem("ShowBies/Pruebas/Flechas del borde (play)")]
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
            terminado = puso = midio = acerco = pauso = false;
            flechasDeZombiAntes = cajaBien = zombisBien = jefeBien = cajaCercaSinFlecha = pausaSinFlechas = false;
            caja = null;
            jefe = null;
            mios.Clear();
            faltanAlMedir = -1;
            medidas = "";
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
        vida.health = vida.maxHealth;
        float t = Time.unscaledTime;
        Vector3 jugador = vida.transform.position;

        // Lo que se pone una vez, con la oleada ya armada (al empezarla se vacia su lista).
        if (!puso)
        {
            if (oleadas.OleadaActual < Oleada) return;
            puso = true;
            var prefabCaja = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PUBalas.prefab");
            caja = Object.Instantiate(prefabCaja, jugador + new Vector3(38f, 0.5f, 0f), Quaternion.identity);
            Quieto(Poner(oleadas, oleadas.tipos[0].prefab, jugador + new Vector3(-38f, 0f, 22f)));
            Quieto(Poner(oleadas, oleadas.tipos[0].prefab, jugador + new Vector3(18f, 0f, -36f)));
            jefe = EnemyController.Aparecer(oleadas.jefe, jugador + new Vector3(0f, 0f, 40f));
            if (jefe != null)
            {
                jefe.EsJefe = true;
                Quieto(jefe);
            }
            return;
        }

        // Los demas zombis de la oleada se mueren apenas salen.
        foreach (var z in Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None))
            if (z.Vivo && z != jefe && !mios.Contains(z)) z.DanoZombi(1e9f);

        var vivos = new List<EnemyController>();
        int faltan = oleadas.FaltanDeLaOleada(vivos);
        if (faltan > FlechasDelBorde.UltimosZombis)
        {
            foreach (var v in FlechasDelBorde.Vistas) if (v.que == FlechasDelBorde.Que.Zombi) flechasDeZombiAntes = true;
            return;
        }
        // Que queden solo los dos de la prueba (el ultimo que salio todavia no murio).
        if (faltan > mios.Count) return;

        if (!midio)
        {
            if (oleadas.EnDescanso) return;
            midio = true;
            midioEn = t;
            faltanAlMedir = faltan;
            Medir();
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(Foto)));
            ScreenCapture.CaptureScreenshot(Foto);
            return;
        }

        if (!acerco && t - midioEn > 0.5f)
        {
            acerco = true;
            acercoEn = t;
            caja.transform.position = jugador + new Vector3(3f, 0.5f, 2.5f);
            return;
        }

        if (acerco && !pauso && t - acercoEn > 0.3f)
        {
            cajaCercaSinFlecha = true;
            foreach (var v in FlechasDelBorde.Vistas) if (v.objetivo == caja.GetComponent<PickupCaducidad>()) cajaCercaSinFlecha = false;
            var pausa = Object.FindAnyObjectByType<MenuPausa>();
            if (pausa == null) { Terminar("no hay MenuPausa"); return; }
            pausa.Pausar();
            pauso = true;
            pausoEn = t;
            return;
        }

        if (pauso && t - pausoEn > 0.3f)
        {
            pausaSinFlechas = FlechasDelBorde.Vistas.Count == 0;
            Object.FindAnyObjectByType<MenuPausa>().Reanudar();
            Terminar(null);
        }
    }

    static EnemyController Poner(WaveManager oleadas, GameObject prefab, Vector3 donde)
    {
        var z = EnemyController.Aparecer(prefab, donde);
        if (z == null) return null;
        oleadas.SumarALaOleada(z);
        mios.Add(z);
        return z;
    }

    // Quieto: con la posicion congelada la persecucion pisa la velocidad pero no lo mueve (y
    // sin pasarlo a kinematic, que se queja de cada velocidad que le ponen).
    static void Quieto(EnemyController z)
    {
        if (z == null) return;
        var cuerpo = z.GetComponent<Rigidbody>();
        if (cuerpo != null) cuerpo.constraints = RigidbodyConstraints.FreezeAll;
    }

    // Cada flecha, contra donde esta lo que señala: en el borde y apuntando hacia eso.
    static void Medir()
    {
        var camara = Camera.main;
        var sb = new StringBuilder();
        var pick = caja != null ? caja.GetComponent<PickupCaducidad>() : null;
        cajaBien = Apunta(camara, pick, caja != null ? caja.transform.position + Vector3.up * 0.5f : Vector3.zero, FlechasDelBorde.Que.Caja, sb);
        if (cajaBien)
        {
            Color esperado = AspectoDeCaja.ColorDe(pick, Color.white);
            foreach (var v in FlechasDelBorde.Vistas)
                if (v.objetivo == pick) cajaBien &= v.color == esperado;
        }
        zombisBien = mios.Count == 2;
        foreach (var z in mios) zombisBien &= z != null && Apunta(camara, z, z.transform.position + Vector3.up, FlechasDelBorde.Que.Zombi, sb);
        jefeBien = jefe != null && Apunta(camara, jefe, jefe.transform.position + Vector3.up * 1.5f, FlechasDelBorde.Que.Jefe, sb);
        if (jefeBien)
            foreach (var v in FlechasDelBorde.Vistas)
                if (v.objetivo == jefe) jefeBien &= Mathf.Approximately(v.tamanio, FlechasDelBorde.TamanioJefe);
        var flechas = GameObject.Find("FlechasDelBorde");
        if (flechas != null)
            foreach (RectTransform hijo in flechas.transform)
                if (hijo.gameObject.activeSelf)
                    sb.Append("[flecha en " + hijo.anchoredPosition.ToString("0") + " girada " + hijo.localEulerAngles.z.ToString("0") +
                              ", escala " + hijo.localScale.x.ToString("0.00") + "] ");
        medidas = sb.ToString();
    }

    static bool Apunta(Camera camara, Object objetivo, Vector3 mundo, FlechasDelBorde.Que que, StringBuilder sb)
    {
        Vector3 p = camara.WorldToScreenPoint(mundo);
        // Lo que queda detras del plano de la camara (bien al sur del jugador) sale espejado.
        if (p.z < 0f) p = new Vector3(Screen.width - p.x, Screen.height - p.y, -p.z);
        foreach (var v in FlechasDelBorde.Vistas)
        {
            if (v.objetivo != objetivo || v.que != que) continue;
            // Desde donde quedo la flecha: si se corrio para no tapar el HUD, apunta desde ahi.
            float esperado = Mathf.Atan2(p.y - v.punto.y, p.x - v.punto.x) * Mathf.Rad2Deg;
            float error = Mathf.Abs(Mathf.DeltaAngle(v.angulo, esperado));
            bool enElBorde = Mathf.Min(v.punto.x, Screen.width - v.punto.x, v.punto.y, Screen.height - v.punto.y) < 140f;
            sb.Append(que + " en (" + v.punto.x.ToString("0") + ", " + v.punto.y.ToString("0") + ") a " + v.angulo.ToString("0") +
                      " grados, esperado " + esperado.ToString("0") + "; ");
            return error < 2f && enElBorde;
        }
        sb.Append(que + ": sin flecha; ");
        return false;
    }

    static void Terminar(string error)
    {
        if (terminado) return;
        terminado = true;
        var inf = new StringBuilder();
        inf.AppendLine("Prueba de las flechas del borde (WaveMode, oleada " + Oleada + ", una caja, dos zombis y un jefe lejos)");
        inf.AppendLine();
        if (error != null) inf.AppendLine("ERROR: " + error);
        inf.AppendLine("Faltaban " + faltanAlMedir + " zombis al medir. " + medidas);
        inf.AppendLine("Foto: " + Foto);
        inf.AppendLine();

        var ok = new List<bool>
        {
            error == null,
            !flechasDeZombiAntes,
            cajaBien,
            zombisBien,
            jefeBien,
            cajaCercaSinFlecha,
            pausaSinFlechas,
        };
        var que = new List<string>
        {
            "el banco llego hasta el final",
            "con mas de " + FlechasDelBorde.UltimosZombis + " zombis por matar, ninguna flecha de zombis",
            "la caja lejos tiene su flecha, de su color, en el borde y apuntandole",
            "con los dos ultimos zombis, una flecha hacia cada uno",
            "el jefe tiene la suya, mas grande",
            "con la caja a la vista, su flecha se va",
            "en la pausa no hay flechas",
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
