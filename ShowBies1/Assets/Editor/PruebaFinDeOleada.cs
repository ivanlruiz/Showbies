using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Banco del final de la oleada y de los avisos de progreso, en play (revision del 9/10, mejoras
// 1 y 2): WaveMode retomada en la oleada 5 con la mejor marca en la 4 y sin monedas. Se van
// despejando los zombis que salen (cuentan como muertos), y a mitad de la oleada se suman
// monedas para una mejora. Mira que al terminar la oleada salga "¡OLEADA 5 SUPERADA!" con
// su bono, que el tiempo casi se pare un instante, que vuelen las monedas del bono, que
// despues venga el cartel de la oleada 6, que salgan "¡NUEVO RECORD!" y "¡TE LLEGA PARA..."
// y que el MEJORAS de la pausa tenga su insignia. Como los zombis se despejan antes de
// llegar, la oleada sale sin un golpe: PERFECTA, con el bono doble (mejora 4). Escribe
// Builds/prueba_fin_de_oleada.txt.
[InitializeOnLoad]
public static class PruebaFinDeOleada
{
    const string Clave = "ShowBies.PruebaFinDeOleada";
    const string Banco = "PruebaFinDeOleada";
    const string Ruta = "../Builds/prueba_fin_de_oleada.txt";
    const string Escena = "Assets/Escenas/WaveMode.unity";
    const int Oleada = 5;
    const double TopeTotal = 90.0;

    static double inicio;
    static bool empezo, terminado, sumoMonedas, vioSuperada, vioSiguiente, pauso, perfecta;
    static float superadaEn = -1f, siguienteEn = -1f, pausoEn;
    static float menorEscalaDeTiempo = 1f;
    static int monedasVolando;
    static string textoSuperada = "", textoSiguiente = "";
    static readonly List<string> avisos = new List<string>();
    static bool insigniaPrendida;
    static string numeroDeLaInsignia = "";

    static PruebaFinDeOleada()
    {
        EditorApplication.update += Tick;
    }

    [MenuItem("ShowBies/Pruebas/Fin de oleada y avisos (play)")]
    public static void Arrancar()
    {
        if (!RespaldoDelBanco.PuedeArrancar(Banco)) return;
        RespaldoDelBanco.Guardar(Banco);
        PlayerSettings.runInBackground = true;
        Progreso.DepurarFijarMonedas(0);
        FijarMejorOleada(Oleada - 1);
        Progreso.GuardarOleadaEnCurso(Oleada, 0);
        Progreso.Guardar();
        EditorSceneManager.OpenScene(Escena);
        SessionState.SetBool(Clave, true);
        empezo = false;
        EditorApplication.EnterPlaymode();
    }

    // La mejor marca solo sube (RegistrarOleadaCompletada): para la prueba se baja a mano. El
    // respaldo del banco la devuelve al terminar.
    static void FijarMejorOleada(int oleada)
    {
        const System.Reflection.BindingFlags Estatico = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic;
        var campo = typeof(Progreso).GetField("datos", Estatico);
        var datos = campo != null ? campo.GetValue(null) : null;
        var mejor = datos != null ? datos.GetType().GetField("mejorOleada") : null;
        if (mejor != null) mejor.SetValue(datos, oleada);
    }

    static void Tick()
    {
        if (!SessionState.GetBool(Clave, false)) return;
        if (!EditorApplication.isPlaying) return;
        if (!RespaldoDelBanco.SigueArmado(Banco, Clave)) return;

        if (!empezo)
        {
            empezo = true;
            terminado = sumoMonedas = vioSuperada = vioSiguiente = pauso = insigniaPrendida = perfecta = false;
            superadaEn = siguienteEn = -1f;
            menorEscalaDeTiempo = 1f;
            monedasVolando = 0;
            textoSuperada = textoSiguiente = numeroDeLaInsignia = "";
            avisos.Clear();
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
        if (vida == null || oleadas == null || oleadas.cartelOleada == null) return;
        vida.health = vida.maxHealth;
        float t = Time.unscaledTime;

        // Los zombis que salen se van (cuentan como muertos), asi la oleada termina sola.
        if (!pauso) EnemyController.DespejarAlrededor(vida.transform.position, 500f);

        // A mitad de la oleada, monedas para una mejora.
        if (!sumoMonedas && oleadas.OleadaActual == Oleada && !oleadas.EnDescanso)
        {
            sumoMonedas = true;
            Progreso.Sumar(5000);
        }

        var cartel = oleadas.cartelOleada;
        string superada = Textos.Formato("cartel_oleada_superada", Oleada);
        string siguiente = Textos.Formato("cartel_oleada", Oleada + 1);
        if (cartel.gameObject.activeInHierarchy)
        {
            if (!vioSuperada && cartel.text.StartsWith(superada))
            {
                vioSuperada = true;
                superadaEn = t;
                perfecta = oleadas.UltimaFuePerfecta;
                textoSuperada = cartel.text.Replace("\n", " / ");
            }
            if (vioSuperada && !vioSiguiente && cartel.text == siguiente)
            {
                vioSiguiente = true;
                siguienteEn = t;
                textoSiguiente = cartel.text;
            }
        }
        if (vioSuperada && t - superadaEn < 0.6f) menorEscalaDeTiempo = Mathf.Min(menorEscalaDeTiempo, Time.timeScale);
        var efectos = GameObject.Find("MonedasDelBono");
        if (efectos != null)
        {
            int volando = 0;
            foreach (var img in efectos.GetComponentsInChildren<UnityEngine.UI.Image>()) if (img.name == "Moneda") volando++;
            monedasVolando = Mathf.Max(monedasVolando, volando);
        }
        foreach (var texto in Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None))
        {
            if (texto.name != "MisionCumplida") continue;
            string linea = texto.text.Split('\n')[0];
            if (!avisos.Contains(linea)) avisos.Add(linea);
        }

        // Ya en la oleada 6 y con los avisos vistos: la pausa, con la insignia de MEJORAS.
        bool vioLosAvisos = avisos.Exists(a => a.Contains(Textos.De("aviso_record"))) && avisos.Exists(EsElDeLaMejora);
        if (vioSiguiente && !pauso && (vioLosAvisos || t - siguienteEn > 9f) && !oleadas.EnDescanso)
        {
            var pausa = Object.FindAnyObjectByType<MenuPausa>();
            if (pausa == null) { Terminar("no hay MenuPausa"); return; }
            pausa.Pausar();
            pauso = true;
            pausoEn = t;
            return;
        }
        // La insignia la prende MenuPausa en su Update: unos cuadros despues de abrir.
        if (pauso && t - pausoEn > 0.3f)
        {
            var pausa = Object.FindAnyObjectByType<MenuPausa>();
            var boton = pausa != null && pausa.panel != null ? pausa.panel.transform.Find("BotonMejoras") : null;
            var insignia = boton != null ? boton.Find("Insignia") : null;
            insigniaPrendida = insignia != null && insignia.gameObject.activeInHierarchy;
            var numero = insignia != null ? insignia.GetComponentInChildren<TMP_Text>() : null;
            numeroDeLaInsignia = numero != null ? numero.text : "";
            pausa.Reanudar();
            Terminar(null);
        }
    }

    // "¡TE LLEGA PARA UNA MEJORA!" o "...PARA N MEJORAS!": lo que va antes del numero.
    static bool EsElDeLaMejora(string aviso)
    {
        string una = Textos.De("aviso_compras_una");
        string varias = Textos.Formato("aviso_compras_varias", "#");
        string antes = varias.Substring(0, Mathf.Max(0, varias.IndexOf('#')));
        return aviso == una || (antes.Length > 0 && aviso.StartsWith(antes));
    }

    static void Terminar(string error)
    {
        if (terminado) return;
        terminado = true;
        var inf = new StringBuilder();
        inf.AppendLine("Prueba del final de la oleada y de los avisos de progreso (WaveMode, oleada " + Oleada + ", mejor marca " + (Oleada - 1) + ")");
        inf.AppendLine();
        if (error != null) inf.AppendLine("ERROR: " + error);
        inf.AppendLine("Cartel al terminar: \"" + textoSuperada + "\"; despues: \"" + textoSiguiente + "\" a los "
                       + (siguienteEn >= 0f && superadaEn >= 0f ? (siguienteEn - superadaEn).ToString("0.00") : "?") + " s");
        inf.AppendLine("Sin golpes en la oleada (perfecta): " + perfecta);
        inf.AppendLine("Escala de tiempo mas baja al terminar: " + menorEscalaDeTiempo.ToString("0.00") + "; monedas del bono volando: " + monedasVolando);
        inf.AppendLine("Avisos vistos: " + string.Join(" | ", avisos));
        inf.AppendLine("Insignia de MEJORAS en la pausa: " + (insigniaPrendida ? "prendida, dice " + numeroDeLaInsignia : "apagada"));
        inf.AppendLine();

        var ok = new List<bool>
        {
            error == null,
            vioSuperada && textoSuperada.Contains("+" + WaveManager.Bono(4, Oleada, perfecta)),
            vioSuperada && perfecta && textoSuperada.Contains("+" + WaveManager.Bono(4, Oleada, true)) && textoSuperada.Contains("PERFECTA"),
            menorEscalaDeTiempo <= 0.1f,
            monedasVolando > 0,
            vioSiguiente && siguienteEn - superadaEn > 1f && siguienteEn - superadaEn < 2.5f,
            avisos.Exists(a => a.Contains(Textos.De("aviso_record"))),
            avisos.Exists(EsElDeLaMejora),
            insigniaPrendida && numeroDeLaInsignia.Length > 0 && numeroDeLaInsignia != "0",
        };
        var que = new List<string>
        {
            "el banco llego hasta el final",
            "al terminar la oleada sale ¡OLEADA " + Oleada + " SUPERADA! con su bono",
            "sin un golpe sale ¡PERFECTA! con el bono doble",
            "el tiempo casi se para un instante (pausa de impacto)",
            "las monedas del bono vuelan al contador",
            "despues, en el mismo descanso, el cartel de la oleada " + (Oleada + 1),
            "sale ¡NUEVO RECORD! al pasar la mejor marca",
            "sale el aviso de que alcanza para una mejora",
            "el MEJORAS de la pausa tiene su insignia",
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
