using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Banco del cartel de lo que se cobra solo, en play (revision del 9/10, mejora 5): el menu
// sin la recompensa diaria y con dos cobros anotados (las misiones del dia y el desafio de
// la semana). Mira que el cartel salga con su titulo y un renglon por cobro, que quede
// dentro de la pantalla y debajo de los botones redondos de las esquinas, que mientras esta
// la reseña no tome el menu por tranquilo (CartelDeCobros.Abierto) y que se vaya solo.
// Saca una foto (Builds/cobros_solos.png) y escribe Builds/prueba_cobros_solos.txt.
[InitializeOnLoad]
public static class PruebaCobrosSolos
{
    const string Clave = "ShowBies.PruebaCobrosSolos";
    const string Banco = "PruebaCobrosSolos";
    const string Ruta = "../Builds/prueba_cobros_solos.txt";
    const string Foto = "../Builds/cobros_solos.png";
    const string Escena = "Assets/Escenas/Menu.unity";
    const double TopeTotal = 40.0;

    static double inicio;
    static bool empezo, terminado, anoto, midio;
    static float anotoEn = -1f, abiertoEn = -1f, cerradoEn = -1f;
    static int renglones;
    static string titulo = "", textos = "", medidas = "";
    static bool dentro, debajoDeLasEsquinas, encimaDeMejoras;

    static PruebaCobrosSolos()
    {
        EditorApplication.update += Tick;
    }

    [MenuItem("ShowBies/Pruebas/Premios cobrados solos (play)")]
    public static void Arrancar()
    {
        if (!RespaldoDelBanco.PuedeArrancar(Banco)) return;
        RespaldoDelBanco.Guardar(Banco);
        PlayerSettings.runInBackground = true;
        // Sin la ventana de la diaria: el cartel la espera. Y lo que el menu cobraria solo al
        // abrirse (misiones o semana de otro dia en el progreso del editor) se cierra aca,
        // antes del play: si no, sale en un cartel propio antes que el de la prueba.
        Progreso.RegistrarRecompensaDiaria(Progreso.DiaDeHoy(), 1);
        MisionesDiarias.Asegurar();
        DesafioSemanal.Asegurar();
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
            terminado = anoto = midio = dentro = debajoDeLasEsquinas = encimaDeMejoras = false;
            anotoEn = abiertoEn = cerradoEn = -1f;
            renglones = 0;
            titulo = textos = medidas = "";
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
        float t = Time.unscaledTime;
        if (!anoto)
        {
            // Lo que el menu ya haya cobrado solo al abrirse (el progreso del editor) se va con
            // estos: el cartel los junta.
            CobrosSolos.Anotar(CobrosSolos.Origen.Misiones, 1250);
            CobrosSolos.Anotar(CobrosSolos.Origen.Semanal, 8000);
            anoto = true;
            anotoEn = t;
            return;
        }

        if (abiertoEn < 0f)
        {
            if (CartelDeCobros.Abierto) abiertoEn = t;
            return;
        }

        // Con la entrada terminada: lo que dice y donde quedo.
        if (!midio && t - abiertoEn > 0.8f)
        {
            midio = true;
            var cartel = GameObject.Find("CartelDeCobros/Lienzo/Cartel");
            if (cartel == null) { Terminar("no se encontro el cartel"); return; }
            var lineas = new List<string>();
            foreach (var texto in cartel.GetComponentsInChildren<TMP_Text>())
            {
                lineas.Add(texto.text);
                if (texto.name == "Titulo") titulo = texto.text;
                if (texto.name.StartsWith("Cobro")) renglones++;
            }
            textos = string.Join(" | ", lineas);

            Rect delCartel = EnPantalla((RectTransform)cartel.transform);
            dentro = delCartel.xMin >= 0f && delCartel.xMax <= Screen.width && delCartel.yMin >= 0f && delCartel.yMax <= Screen.height;
            // Los botones redondos de las esquinas: los que cuelgan del borde de arriba.
            float bajoLasEsquinas = float.MaxValue;
            string cuales = "";
            foreach (var boton in Object.FindObjectsByType<Button>(FindObjectsSortMode.None))
            {
                var rt = (RectTransform)boton.transform;
                if (!boton.gameObject.activeInHierarchy || rt.anchorMin.y < 0.99f || rt.IsChildOf(cartel.transform)) continue;
                if (boton.GetComponentInParent<Canvas>().rootCanvas.sortingOrder >= CartelDeCobros.OrdenDelCanvas) continue;
                Rect r = EnPantalla(rt);
                if (r.yMax < Screen.height * 0.6f) continue;
                bajoLasEsquinas = Mathf.Min(bajoLasEsquinas, r.yMin);
                cuales += boton.name + " ";
            }
            debajoDeLasEsquinas = bajoLasEsquinas < float.MaxValue && delCartel.yMax < bajoLasEsquinas;
            var mejoras = Object.FindAnyObjectByType<BotonMejoras>();
            float arribaDeMejoras = mejoras != null ? EnPantalla((RectTransform)mejoras.transform).yMax : -1f;
            encimaDeMejoras = mejoras != null && delCartel.yMin > arribaDeMejoras;
            medidas = "pantalla " + Screen.width + "x" + Screen.height + "; cartel de x " + delCartel.xMin.ToString("0") + " a " +
                      delCartel.xMax.ToString("0") + " y de y " + delCartel.yMin.ToString("0") + " a " + delCartel.yMax.ToString("0") +
                      "; los botones de las esquinas (" + cuales.Trim() + ") bajan hasta y " + bajoLasEsquinas.ToString("0") +
                      "; MEJORAS llega hasta y " + arribaDeMejoras.ToString("0");
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(Foto)));
            ScreenCapture.CaptureScreenshot(Foto);
            return;
        }

        if (midio && cerradoEn < 0f && !CartelDeCobros.Abierto)
        {
            cerradoEn = t;
            Terminar(null);
        }
    }

    // El rectangulo en pixeles de pantalla: en un canvas overlay las esquinas del mundo ya lo son.
    static Rect EnPantalla(RectTransform rt)
    {
        var esquinas = new Vector3[4];
        rt.GetWorldCorners(esquinas);
        return Rect.MinMaxRect(esquinas[0].x, esquinas[0].y, esquinas[2].x, esquinas[2].y);
    }

    static void Terminar(string error)
    {
        if (terminado) return;
        terminado = true;
        float duro = abiertoEn >= 0f && cerradoEn >= 0f ? cerradoEn - abiertoEn : -1f;
        var inf = new StringBuilder();
        inf.AppendLine("Prueba del cartel de los premios cobrados solos (el menu, con las misiones y el desafio semanal anotados)");
        inf.AppendLine();
        if (error != null) inf.AppendLine("ERROR: " + error);
        inf.AppendLine("Salio a los " + (abiertoEn >= 0f && anotoEn >= 0f ? (abiertoEn - anotoEn).ToString("0.00") : "?") +
                       " s de anotar y duro " + duro.ToString("0.00") + " s");
        inf.AppendLine("Textos: " + textos);
        inf.AppendLine("Medidas: " + medidas);
        inf.AppendLine("Foto: " + Foto);
        inf.AppendLine();

        var ok = new List<bool>
        {
            error == null,
            titulo == Textos.De("cobro_solo_titulo"),
            renglones >= 2 && textos.Contains(Textos.Formato("cobro_solo_misiones", FormatoNumeros.Compacto(1250))) &&
                textos.Contains(Textos.Formato("cobro_solo_semanal", FormatoNumeros.Compacto(8000))),
            dentro,
            debajoDeLasEsquinas,
            encimaDeMejoras,
            duro >= CartelDeCobros.Duracion && duro <= CartelDeCobros.Duracion + 1.5f,
            !CobrosSolos.Hay,
        };
        var que = new List<string>
        {
            "el banco llego hasta el final",
            "sale ¡PREMIOS COBRADOS!",
            "con un renglon por cobro y lo que pago cada uno",
            "el cartel queda dentro de la pantalla",
            "y debajo de los botones redondos de las esquinas",
            "y encima de MEJORAS",
            "se va solo despues de " + CartelDeCobros.Duracion + " s",
            "y no queda nada para mostrar de nuevo",
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
