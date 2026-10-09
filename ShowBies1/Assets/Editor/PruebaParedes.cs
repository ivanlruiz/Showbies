using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Banco de las paredes de la ciudad, en play: que ningun zombi se trabe contra un edificio ni
// termine adentro de uno, con la fisica de verdad y la horda empujandose (la prueba de logica lo
// simula sin fisica: EnemyController.Rodeo). Ivan lo pidio el 9/10: que no se queden trabados
// en las paredes.
//
// WaveMode retomada en la oleada 25 (la ciudad, con los edificios ya puestos y sus paredes). El
// jugador, quieto e inmortal, en dos calles con edificios entre el y los puntos de aparicion; los
// zombis que llegan a 2 m se sacan (asi no se amontonan encima). Uno esta trabado si en 2,5 s se
// desplazo menos de 0,75 m estando a mas de 4 m del jugador (pegado a algo, o yendo y viniendo), y
// trabado contra una pared si ademas esta a menos de su radio y medio metro de un edificio: eso es
// lo que no puede pasar. Que no se acerque no alcanza: rodear un edificio lleva unos segundos de
// caminar de costado. Los parados en campo abierto se anotan aparte, con su velocidad y los zombis
// que tienen alrededor (la horda tambien frena). Escribe Builds/prueba_paredes.txt.
[InitializeOnLoad]
public static class PruebaParedes
{
    const string Clave = "ShowBies.PruebaParedes";
    const string Banco = "PruebaParedes";
    const string Ruta = "../Builds/prueba_paredes.txt";
    const string Escena = "Assets/Escenas/WaveMode.unity";
    const int Oleada = 25;
    const float PorLugar = 14f;          // segundos de juego en cada lugar
    const float Ventana = 2.5f;          // segundos en que un trabado se desplaza menos de...
    const float Desplazamiento = 0.75f;  // ...esto
    const double TopeTotal = 90.0;

    static readonly Vector3[] Lugares = { new Vector3(24f, 0f, 40f), new Vector3(-36f, 0f, 24f) };

    class Seguido
    {
        public EnemyController zombi;
        public int aparicion;
        public Vector3 donde;
        public float desde;
        public bool trabado;
    }

    static readonly Dictionary<int, Seguido> seguidos = new Dictionary<int, Seguido>();
    static int lugar;
    static float lugarDesde;
    static double inicio;
    static bool empezo, terminado, listo;
    static int llegaron, trabados, contraParedes, adentro, vistos;
    static readonly StringBuilder detalle = new StringBuilder();

    static PruebaParedes()
    {
        EditorApplication.update += Tick;
    }

    [MenuItem("ShowBies/Pruebas/Paredes de la ciudad (play)")]
    // Publico para correrlo por codigo (ver la trampa del registro de menus).
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
            listo = false;
            lugar = 0;
            llegaron = trabados = contraParedes = adentro = vistos = 0;
            seguidos.Clear();
            detalle.Clear();
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
        if (vida == null) return;
        vida.health = vida.maxHealth;
        var cuerpo = vida.GetComponent<Rigidbody>();
        var control = vida.GetComponent<PlayerController>();

        // Que la ciudad este puesta, con sus paredes.
        if (!listo)
        {
            if (CapitulosDeEscenario.Huellas.Count == 0 || Time.time < 4f) return;
            listo = true;
            Poner(cuerpo, control, 0);
            return;
        }

        Vector3 yo = cuerpo.position;
        float ahora = Time.time;
        foreach (var z in Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None))
        {
            if (!z.Vivo || z.EsJefe) continue;
            Vector3 p = z.transform.position;
            float d = Vector3.Distance(new Vector3(p.x, 0f, p.z), new Vector3(yo.x, 0f, yo.z));
            if (!seguidos.TryGetValue(z.NumeroDeAparicion, out var s))
            {
                s = new Seguido { zombi = z, aparicion = z.NumeroDeAparicion, donde = p, desde = ahora };
                seguidos[z.NumeroDeAparicion] = s;
                vistos++;
            }
            bool revisar = ahora - s.desde >= Ventana;
            float movido = 0f;
            if (revisar)
            {
                movido = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(s.donde.x, s.donde.z));
                s.donde = p;
                s.desde = ahora;
            }
            if (revisar && !s.trabado && d > 4f && movido < Desplazamiento)
            {
                s.trabado = true;
                trabados++;
                float aLaPared = float.MaxValue;
                foreach (var h in CapitulosDeEscenario.Huellas)
                {
                    float dx = Mathf.Max(h.xMin - p.x, 0f, p.x - h.xMax);
                    float dz = Mathf.Max(h.yMin - p.z, 0f, p.z - h.yMax);
                    aLaPared = Mathf.Min(aLaPared, Mathf.Sqrt(dx * dx + dz * dz));
                }
                bool contraLaPared = aLaPared < EnemyController.RadioDelCuerpo(z.gameObject) + 0.5f;
                if (contraLaPared) contraParedes++;
                var rb = z.GetComponent<Rigidbody>();
                int cerca = 0;
                foreach (var otro in Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None))
                    if (otro != z && otro.Vivo && (otro.transform.position - p).sqrMagnitude < 1.5f * 1.5f) cerca++;
                if (trabados <= 8)
                    detalle.AppendLine("  " + (contraLaPared ? "TRABADO CONTRA UNA PARED: " : "parado en campo abierto: ") + z.name + " en (" + p.x.ToString("0.0") + ", "
                                       + p.z.ToString("0.0") + ") a " + d.ToString("0.0") + " m del jugador y " + aLaPared.ToString("0.0")
                                       + " m del edificio mas cercano; velocidad " + (rb != null ? rb.linearVelocity.magnitude.ToString("0.0") : "?")
                                       + " (camina a " + (z.enemyType != null ? z.enemyType.velocidad.ToString("0.0") : "?") + "), " + cerca + " zombis a menos de 1,5 m"
                                       + "; se movio " + movido.ToString("0.00") + " m en " + Ventana + " s; jugador en (" + yo.x.ToString("0.0") + ", " + yo.z.ToString("0.0")
                                       + "); el rodeo lo manda a " + Plano(EnemyController.Rodeo(p, yo, CapitulosDeEscenario.Huellas, EnemyController.RadioDelCuerpo(z.gameObject))));
            }
            foreach (var h in CapitulosDeEscenario.Huellas)
            {
                if (p.x > h.xMin + 0.2f && p.x < h.xMax - 0.2f && p.z > h.yMin + 0.2f && p.z < h.yMax - 0.2f)
                {
                    adentro++;
                    if (adentro <= 5) detalle.AppendLine("  adentro de un edificio: " + z.name + " en (" + p.x.ToString("0.0") + ", " + p.z.ToString("0.0") + ")");
                }
            }
        }
        // Los que llegan se sacan, sin puntos ni monedas.
        int sacados = EnemyController.DespejarAlrededor(yo, 2f);
        llegaron += sacados;

        if (ahora - lugarDesde < PorLugar) return;
        lugar++;
        if (lugar >= Lugares.Length) { Terminar(null); return; }
        Poner(cuerpo, control, lugar);
    }

    static void Poner(Rigidbody cuerpo, PlayerController control, int cual)
    {
        Vector3 donde = new Vector3(Lugares[cual].x, cuerpo.position.y, Lugares[cual].z);
        cuerpo.position = donde;
        cuerpo.linearVelocity = Vector3.zero;
        control.transform.position = donde;
        lugarDesde = Time.time;
        // Los que venian hacia el lugar de antes empiezan de nuevo la cuenta: dar la vuelta no es
        // trabarse.
        foreach (var s in seguidos.Values)
        {
            s.desde = Time.time;
            if (s.zombi != null) s.donde = s.zombi.transform.position;
        }
    }

    static string Plano(Vector3 v)
    {
        return "(" + v.x.ToString("0.0") + ", " + v.z.ToString("0.0") + ")";
    }

    static void Terminar(string error)
    {
        if (terminado) return;
        terminado = true;
        var inf = new StringBuilder();
        inf.AppendLine("Prueba de las paredes de la ciudad (WaveMode, oleada " + Oleada + "): el jugador quieto en dos calles, " + PorLugar + " s en cada una");
        inf.AppendLine();
        if (error != null) inf.AppendLine("ERROR: " + error);
        inf.AppendLine("Edificios: " + CapitulosDeEscenario.Huellas.Count + "; zombis seguidos: " + vistos + "; llegaron al jugador: " + llegaron
                       + "; trabados (menos de " + Desplazamiento + " m en " + Ventana + " s): " + trabados + ", de esos contra una pared: " + contraParedes
                       + "; vistos adentro de un edificio: " + adentro);
        inf.Append(detalle);
        inf.AppendLine();
        bool[] ok = { error == null, CapitulosDeEscenario.Huellas.Count > 0, llegaron >= 10, contraParedes == 0, adentro == 0 };
        string[] que =
        {
            "el banco llego hasta el final",
            "la ciudad estaba puesta, con sus edificios",
            "los zombis llegaron al jugador (al menos 10)",
            "ningun zombi se quedo trabado contra una pared (menos de " + Desplazamiento + " m en " + Ventana + " s, pegado a un edificio)",
            "ningun zombi entro a un edificio",
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
