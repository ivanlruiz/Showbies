using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Banco del arma que cambia con el daño (1.6.0, etapa 3 de las cartas; ArmaEnLaMano), en play:
// WaveMode retomado en la oleada 5 con cuatro cartas de daño, sin oleadas ni cajas.
//   - al empezar (y al retomar, con las cartas) el arma es la del tramo, sin festejo;
//   - bajar de tramo no festeja; subir, uno por escalon, con la boca en la punta del cañon nuevo
//     y el arma tirando desde ahi;
//   - la furia sube la bala y no el arma;
//   - disparando con cada una, el fogonazo es el suyo (las puntas, el anillo) y sale de la boca.
// Escribe Builds/prueba_armas.txt y una foto con el cartel de la escopeta (prueba_armas.png).
[InitializeOnLoad]
public static class PruebaArmas
{
    const string Clave = "ShowBies.PruebaArmas";
    const string Banco = "PruebaArmas";
    const string Ruta = "../Builds/prueba_armas.txt";
    const string Foto = "../Builds/prueba_armas.png";
    const string Escena = "Assets/Escenas/WaveMode.unity";
    const double TopeTotal = 90.0;
    const int CartasDeDano = 4;

    enum Paso { Preparar, Bajar, Subir, Furia, Disparo, Fin }

    static Paso paso;
    static int sub;
    static float desde;
    static double inicio;
    static bool empezo, terminado;
    static string error;
    static readonly List<bool> oks = new List<bool>();
    static readonly List<string> ques = new List<string>();
    static readonly StringBuilder notas = new StringBuilder();

    static int fogonazosAntes, tirosAntes, armasNuevasAntes;
    static bool vioPrendido, fogonazoBien;
    static float peorSalida;

    static PruebaArmas()
    {
        EditorApplication.update += Tick;
    }

    [MenuItem("ShowBies/Pruebas/Armas por tramo (play)")]
    // Publico para correrlo por codigo (ver la trampa del registro de menus).
    public static void Arrancar()
    {
        if (!RespaldoDelBanco.PuedeArrancar(Banco)) return;
        RespaldoDelBanco.Guardar(Banco);
        PlayerJS.FijarAutoApuntar(false);
        PlayerSettings.runInBackground = true;
        // Una partida retomada con cartas de daño: el arma tiene que arrancar ya en su tramo.
        var poder = new EstadoPoder { semilla = 7 };
        poder.cartas.Add(new CartaElegida { id = PoderDeLaPartida.Dano, veces = CartasDeDano });
        Progreso.GuardarOleadaEnCurso(5, 0);
        Progreso.GuardarPoderEnCurso(poder);
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
            ques.Clear();
            notas.Length = 0;
            paso = Paso.Preparar;
            sub = 0;
            desde = Time.time;
            inicio = EditorApplication.timeSinceStartup;
            return;
        }
        if (EditorApplication.timeSinceStartup - inicio > TopeTotal) { Terminar("se paso del tope de " + TopeTotal + " s, en " + paso + "/" + sub); return; }
        try
        {
            Avanzar();
        }
        catch (System.Exception e)
        {
            Terminar("excepcion en " + paso + "/" + sub + ": " + e);
        }
    }

    static void Anotar(bool ok, string que)
    {
        oks.Add(ok);
        ques.Add(que);
    }

    static void Pasar(Paso siguiente)
    {
        paso = siguiente;
        sub = 0;
        desde = Time.time;
    }

    static void Sub()
    {
        sub++;
        desde = Time.time;
    }

    // El daño que cae justo en el tramo (1, 2, 4, 8, 16).
    static float DanoDelTramo(int tramo)
    {
        return Mathf.Pow(2f, tramo);
    }

    // La boca en la punta del cañon del tramo puesto.
    static bool BocaEnLaPunta(ArmaEnLaMano mano)
    {
        if (mano.Boca == null) return false;
        var canon = mano.Boca.parent.Find("Canon");
        if (canon == null) return false;
        float punta = canon.localPosition.z + canon.localScale.y + 0.005f;
        int tramo = mano.TramoPuesto;
        return Mathf.Abs(mano.Boca.localPosition.z - punta) < 1e-3f &&
               Mathf.Abs(canon.localScale.y * 2f - ArmaEnLaMano.LargoDelCanon[tramo]) < 1e-3f;
    }

    static void Avanzar()
    {
        var vida = PlayerHealth.instance;
        if (vida == null) return;
        var control = vida.GetComponent<PlayerController>();
        var mano = vida.GetComponent<ArmaEnLaMano>();
        var arma = control != null ? control.theGun : null;
        if (control == null || mano == null || arma == null) { Terminar("el jugador no tiene control, arma en la mano o arma"); return; }
        float t = Time.time - desde;
        vida.health = vida.maxHealth;

        switch (paso)
        {
            case Paso.Preparar:
            {
                if (t < 1f) return;
                var oleadas = Object.FindAnyObjectByType<WaveManager>();
                if (oleadas != null) { oleadas.StopAllCoroutines(); oleadas.enabled = false; }
                foreach (var cajas in Object.FindObjectsByType<PowerUp>(FindObjectsSortMode.None)) cajas.enabled = false;
                EnemyController.DespejarAlrededor(Vector3.zero, 500f);
                arma.FijarFuria(1f, 1f);
                notas.AppendLine("Al empezar: daño " + arma.DanoPorBala.ToString("0.##") + " x" + PoderDeLaPartida.MultiplicadorDano.ToString("0.##") +
                                 " de las cartas, tramo " + arma.TramoDelArma);
                Anotar(PoderDeLaPartida.MultiplicadorDano > 1.01f, "retomada: volvieron las " + CartasDeDano + " cartas de daño");
                Anotar(arma.TramoDelArma >= 1 && mano.TramoPuesto == arma.TramoDelArma,
                       "al retomar, el arma ya es la del tramo con las cartas (" + mano.TramoPuesto + ")");
                Anotar(mano.ArmasNuevas == 0, "al empezar no hay festejo de arma nueva");
                Anotar(BocaEnLaPunta(mano) && arma.boca == mano.Boca, "la boca en la punta de su cañon, y el arma tira desde ahi");
                // Sin las cartas: el daño de la prueba es el de la mejora.
                PoderDeLaPartida.Empezar(new EstadoPoder { semilla = 7 });
                Pasar(Paso.Bajar);
                return;
            }

            case Paso.Bajar:
                if (sub == 0)
                {
                    arma.FijarDanoPorBala(DanoDelTramo(0));
                    Sub();
                    return;
                }
                if (t < 0.15f) return;
                Anotar(mano.TramoPuesto == 0, "con 1 de daño, la pistola");
                Anotar(mano.ArmasNuevas == 0, "bajar de arma no festeja");
                Anotar(BocaEnLaPunta(mano), "la pistola, con la boca en su punta");
                Pasar(Paso.Subir);
                return;

            case Paso.Subir:
            {
                // sub 0 a 3 suben a los tramos 1 a 4, de a uno.
                int tramo = sub / 2 + 1;
                if (tramo > 4) { Pasar(Paso.Furia); return; }
                if (sub % 2 == 0)
                {
                    arma.FijarDanoPorBala(DanoDelTramo(tramo));
                    Sub();
                    return;
                }
                if (t < 0.15f) return;
                Anotar(mano.TramoPuesto == tramo && mano.ArmasNuevas == tramo,
                       "sube a " + Textos.De(ArmaEnLaMano.IdsDeLasArmas[tramo]) + " con un festejo (" + mano.ArmasNuevas + ")");
                Anotar(BocaEnLaPunta(mano) && arma.boca == mano.Boca, "con la boca en la punta del cañon nuevo (" + tramo + ")");
                if (tramo == 2) ScreenCapture.CaptureScreenshot(Path.GetFullPath(Foto));
                Sub();
                return;
            }

            case Paso.Furia:
                if (sub == 0)
                {
                    armasNuevasAntes = mano.ArmasNuevas;
                    arma.FijarDanoPorBala(DanoDelTramo(2));
                    Sub();
                    return;
                }
                if (sub == 1)
                {
                    if (t < 0.15f) return;
                    arma.FijarFuria(2f, 2f);
                    Sub();
                    return;
                }
                if (t < 0.3f) return;
                Anotar(mano.TramoPuesto == 2 && BulletController.Tramo(arma.DanoPorTiro) == 3,
                       "con la furia la bala sube un tramo y el arma no (" + mano.TramoPuesto + ")");
                Anotar(mano.ArmasNuevas == armasNuevasAntes, "ni bajar a la escopeta ni la furia festejan");
                arma.FijarFuria(1f, 1f);
                Pasar(Paso.Disparo);
                return;

            case Paso.Disparo:
            {
                // Dos subpasos por arma: prepararla y dispararle medio segundo.
                int tramo = sub / 2;
                if (tramo > 4)
                {
                    control.enabled = true;
                    var js = control.GetComponent<PlayerJS>();
                    if (js != null) js.enabled = true;
                    Anotar(mano.ArmasNuevas == armasNuevasAntes + 4, "volver a subir de la pistola a la de energia festeja cuatro veces");
                    Terminar(null);
                    return;
                }
                if (sub % 2 == 0)
                {
                    if (t < 0.2f) return;
                    // Quien lee el input pisa isFiring en cada cuadro: se apaga mientras dispara la prueba.
                    control.enabled = false;
                    var js = control.GetComponent<PlayerJS>();
                    if (js != null) js.enabled = false;
                    arma.FijarDanoPorBala(DanoDelTramo(tramo));
                    control.cantBalas = 1000;
                    fogonazosAntes = mano.Fogonazos;
                    tirosAntes = arma.TirosDisparados;
                    vioPrendido = false;
                    fogonazoBien = true;
                    peorSalida = 0f;
                    arma.isFiring = true;
                    Sub();
                    return;
                }
                if (mano.FogonazoPrendido)
                {
                    vioPrendido = true;
                    var render = mano.Boca.Find("Fogonazo").GetComponent<Renderer>();
                    var bloque = new MaterialPropertyBlock();
                    render.GetPropertyBlock(bloque);
                    fogonazoBien &= Mathf.Abs(bloque.GetFloat("_Puntas") - ArmaEnLaMano.PuntasPorArma[tramo]) < 1e-4f &&
                                    Mathf.Abs(bloque.GetFloat("_Anillo") - (ArmaEnLaMano.AnilloPorArma[tramo] ? 1f : 0f)) < 1e-4f;
                }
                // Desde el segundo tiro: el primero sale en el cuadro en que cambio el arma, antes de
                // que la boca pase a la punta del cañon nuevo.
                if (arma.TirosDisparados > tirosAntes + 1)
                    peorSalida = Mathf.Max(peorSalida, Vector3.Distance(arma.UltimaSalida, mano.Boca.position));
                if (t < 0.6f) return;
                arma.isFiring = false;
                string nombre = Textos.De(ArmaEnLaMano.IdsDeLasArmas[tramo]);
                notas.AppendLine(nombre + ": " + (arma.TirosDisparados - tirosAntes) + " tiros, " + (mano.Fogonazos - fogonazosAntes) +
                                 " fogonazos, la bala sale a " + peorSalida.ToString("0.00") + " m de la boca");
                // Un fogonazo por cuadro con tiros: dos tiros del mismo cuadro comparten uno.
                int tiros = arma.TirosDisparados - tirosAntes, fogonazos = mano.Fogonazos - fogonazosAntes;
                Anotar(mano.TramoPuesto == tramo && tiros > 0 && fogonazos > 0 && fogonazos <= tiros && fogonazos >= tiros / 2,
                       nombre + " dispara, con su fogonazo en cada tiro");
                Anotar(vioPrendido && fogonazoBien, nombre + ": el fogonazo es el suyo (puntas, anillo)");
                Anotar(peorSalida < 0.3f, nombre + ": las balas salen de su boca");
                Sub();
                return;
            }
        }
    }

    static void Terminar(string motivo)
    {
        if (terminado) return;
        terminado = true;
        if (error == null) error = motivo;

        var inf = new StringBuilder();
        inf.AppendLine("Prueba del arma por tramo (WaveMode retomado en la 5 con cartas de daño, sin oleadas)");
        inf.AppendLine();
        if (error != null) inf.AppendLine("ERROR: " + error);
        inf.Append(notas);
        inf.AppendLine();
        bool todo = error == null;
        for (int i = 0; i < oks.Count; i++)
        {
            inf.AppendLine((oks[i] ? "OK  " : "FALLA  ") + ques[i]);
            todo &= oks[i];
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
