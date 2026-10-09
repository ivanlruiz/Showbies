using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Banco de los obstaculos, en play: que ningun zombi se trabe contra un edificio, un auto, un
// cantero, un contenedor, una tumba o un arbol, ni termine adentro de uno, con la fisica de
// verdad y la horda empujandose (la prueba de logica lo simula sin fisica: EnemyController.Rodeo
// y Esquive). Ivan lo pidio el 9/10: que no se queden trabados en las paredes, y despues que
// todos los obstaculos tuvieran collider.
//
// Dos escenarios, cada uno con su entrada en el menu: la ciudad (WaveMode retomada en la oleada
// 25, con los edificios, los autos y las cosas ya puestos) y el cementerio (en la 15, con las
// tumbas y los arboles). El jugador, quieto e inmortal, en dos lugares con obstaculos entre el y
// los puntos de aparicion; los zombis que llegan a 2 m se sacan (asi no se amontonan encima). Uno
// esta trabado si en 2,5 s se desplazo menos de 0,75 m estando a mas de 4 m del jugador (pegado a
// algo, o yendo y viniendo), y trabado contra un obstaculo si ademas esta a menos de su radio y
// medio metro de uno: eso es lo que no puede pasar. Que no se acerque no alcanza: rodear un
// edificio lleva unos segundos de caminar de costado. Los parados en campo abierto se anotan
// aparte, con su velocidad y los zombis que tienen alrededor (la horda tambien frena).
//
// En el cementerio, ademas, el jefe pisa lo chico: se hace aparecer uno del otro lado de una
// manzana de tumbas, sin sus patrones (asi camina derecho), y se mira que no choque con las
// lapidas y las cruces (Physics.GetIgnoreCollision) y que pase por encima de alguna.
// Escribe Builds/prueba_paredes.txt o Builds/prueba_tumbas.txt.
[InitializeOnLoad]
public static class PruebaParedes
{
    const string Clave = "ShowBies.PruebaParedes";
    const string ClaveEscenario = "ShowBies.PruebaParedes.Escenario";
    const string Banco = "PruebaParedes";
    const string Escena = "Assets/Escenas/WaveMode.unity";
    const float PorLugar = 14f;          // segundos de juego en cada lugar
    const float Ventana = 2.5f;          // segundos en que un trabado se desplaza menos de...
    const float Desplazamiento = 0.75f;  // ...esto
    const double TopeTotal = 90.0;

    class Escenario
    {
        public string nombre, ruta, que;
        public int oleada;
        public Vector3[] lugares;
        public bool conJefe;
    }

    static readonly Escenario Ciudad = new Escenario
    {
        nombre = "Ciudad", ruta = "../Builds/prueba_paredes.txt", oleada = 25,
        que = "la ciudad (los edificios, los autos, los canteros y los contenedores)",
        lugares = new[] { new Vector3(24f, 0f, 40f), new Vector3(-36f, 0f, 24f) },
    };

    static readonly Escenario Cementerio = new Escenario
    {
        nombre = "Cementerio", ruta = "../Builds/prueba_tumbas.txt", oleada = 15, conJefe = true,
        que = "el cementerio (las tumbas y los arboles)",
        // Del otro lado de dos manzanas de tumbas, para los que vienen de los puntos de aparicion.
        lugares = new[] { new Vector3(26f, 0f, -28f), new Vector3(-20f, 0f, 28f) },
    };

    // El jefe del cementerio: nace al norte de la manzana de (26, -20) y camina al jugador, que
    // esta al sur, a traves de las tres filas.
    static readonly Vector3 SalidaDelJefe = new Vector3(26f, 0f, -9f);

    class Seguido
    {
        public EnemyController zombi;
        public int aparicion;
        public Vector3 donde;
        public float desde;
        public bool trabado;
    }

    static readonly Dictionary<int, Seguido> seguidos = new Dictionary<int, Seguido>();
    static Escenario escenario;
    static int lugar;
    static float lugarDesde;
    static double inicio;
    static bool empezo, terminado, listo;
    static int llegaron, trabados, contraObstaculos, adentro, vistos;
    static readonly StringBuilder detalle = new StringBuilder();
    static EnemyController jefe;
    static int aparicionDelJefe;
    static bool jefeIgnora;
    static int parejasDelJefe;
    static float jefeMasCercaDeUnaTumba;
    // La invocacion entre las tumbas (revision del 9/10): con el jefe parado en una manzana,
    // donde saldria cada invocado alrededor. El rayo chocaba las tumbas que el jefe pisa y
    // los dejaba adentro de su cuerpo.
    static bool anilloMedido;
    static float anilloMasCerca;
    static int anilloPuntos;

    static PruebaParedes()
    {
        EditorApplication.update += Tick;
    }

    [MenuItem("ShowBies/Pruebas/Paredes de la ciudad (play)")]
    // Publicos para correrlos por codigo (ver la trampa del registro de menus).
    public static void Arrancar()
    {
        Arrancar(Ciudad);
    }

    [MenuItem("ShowBies/Pruebas/Tumbas del cementerio (play)")]
    public static void ArrancarCementerio()
    {
        Arrancar(Cementerio);
    }

    static void Arrancar(Escenario cual)
    {
        if (!RespaldoDelBanco.PuedeArrancar(Banco)) return;
        RespaldoDelBanco.Guardar(Banco);
        PlayerSettings.runInBackground = true;
        Progreso.GuardarOleadaEnCurso(cual.oleada, 0);
        Progreso.Guardar();
        EditorSceneManager.OpenScene(Escena);
        SessionState.SetBool(Clave, true);
        SessionState.SetString(ClaveEscenario, cual.nombre);
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
            escenario = SessionState.GetString(ClaveEscenario, Ciudad.nombre) == Cementerio.nombre ? Cementerio : Ciudad;
            lugar = 0;
            llegaron = trabados = contraObstaculos = adentro = vistos = 0;
            jefe = null;
            jefeIgnora = false;
            parejasDelJefe = 0;
            jefeMasCercaDeUnaTumba = float.MaxValue;
            anilloMedido = false;
            anilloMasCerca = float.MaxValue;
            anilloPuntos = 0;
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

        // Que el decorado este puesto, con sus obstaculos.
        if (!listo)
        {
            bool puesto = escenario == Ciudad ? CapitulosDeEscenario.Huellas.Count > 0 : CapitulosDeEscenario.Redondos.Count > 0;
            if (!puesto || Time.time < 4f) return;
            listo = true;
            Poner(cuerpo, control, 0);
            if (escenario.conJefe) SacarAlJefe();
            return;
        }

        Vector3 yo = cuerpo.position;
        float ahora = Time.time;
        foreach (var z in Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None))
        {
            if (!z.Vivo) continue;
            Vector3 p = z.transform.position;
            if (z.EsJefe)
            {
                if (z == jefe && z.NumeroDeAparicion == aparicionDelJefe) SeguirAlJefe(z);
                continue;
            }
            float d = Vector3.Distance(new Vector3(p.x, 0f, p.z), new Vector3(yo.x, 0f, yo.z));
            float radio = EnemyController.RadioDelCuerpo(z.gameObject);
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
            float alObstaculo = AlObstaculo(p, false);
            if (revisar && !s.trabado && d > 4f && movido < Desplazamiento)
            {
                s.trabado = true;
                trabados++;
                bool contra = alObstaculo < radio + 0.5f;
                if (contra) contraObstaculos++;
                var rb = z.GetComponent<Rigidbody>();
                int cerca = 0;
                foreach (var otro in Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None))
                    if (otro != z && otro.Vivo && (otro.transform.position - p).sqrMagnitude < 1.5f * 1.5f) cerca++;
                Vector3 rumbo = EnemyController.Esquive(p, EnemyController.Rodeo(p, yo, CapitulosDeEscenario.Huellas, radio), CapitulosDeEscenario.Redondos, radio, false);
                if (trabados <= 8)
                    detalle.AppendLine("  " + (contra ? "TRABADO CONTRA UN OBSTACULO: " : "parado en campo abierto: ") + z.name + " en " + Plano(p)
                                       + " a " + d.ToString("0.0") + " m del jugador y " + alObstaculo.ToString("0.0")
                                       + " m del obstaculo mas cercano; velocidad " + (rb != null ? rb.linearVelocity.magnitude.ToString("0.0") : "?")
                                       + " (camina a " + (z.enemyType != null ? z.enemyType.velocidad.ToString("0.0") : "?") + "), " + cerca + " zombis a menos de 1,5 m"
                                       + "; se movio " + movido.ToString("0.00") + " m en " + Ventana + " s; jugador en " + Plano(yo)
                                       + "; el rodeo lo manda a " + Plano(rumbo));
            }
            if (alObstaculo < -0.2f)
            {
                adentro++;
                if (adentro <= 5) detalle.AppendLine("  adentro de un obstaculo: " + z.name + " en " + Plano(p));
            }
        }
        // Los que llegan se sacan, sin puntos ni monedas (el jefe no: no se despeja).
        int sacados = EnemyController.DespejarAlrededor(yo, 2f);
        llegaron += sacados;

        if (ahora - lugarDesde < PorLugar) return;
        lugar++;
        if (lugar >= escenario.lugares.Length) { Terminar(null); return; }
        Poner(cuerpo, control, lugar);
    }

    // Cuanto hay del punto al obstaculo mas cercano (negativo: adentro). El jefe no cuenta lo
    // que pisa.
    static float AlObstaculo(Vector3 p, bool esJefe)
    {
        float menor = float.MaxValue;
        foreach (var h in CapitulosDeEscenario.Huellas)
        {
            float dx = Mathf.Max(h.xMin - p.x, 0f, p.x - h.xMax);
            float dz = Mathf.Max(h.yMin - p.z, 0f, p.z - h.yMax);
            float fuera = Mathf.Sqrt(dx * dx + dz * dz);
            if (fuera <= 0f) fuera = -Mathf.Min(Mathf.Min(p.x - h.xMin, h.xMax - p.x), Mathf.Min(p.z - h.yMin, h.yMax - p.z));
            menor = Mathf.Min(menor, fuera);
        }
        foreach (var r in CapitulosDeEscenario.Redondos)
        {
            if (esJefe && r.chico) continue;
            menor = Mathf.Min(menor, Vector2.Distance(new Vector2(p.x, p.z), r.centro) - r.radio);
        }
        return menor;
    }

    // Un jefe sin sus patrones (camina derecho, como cualquier zombi), del otro lado de una
    // manzana de tumbas.
    static void SacarAlJefe()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Personajes/ZombiBOSS.prefab");
        jefe = EnemyController.Aparecer(prefab, SalidaDelJefe + Vector3.up * 2f);
        if (jefe == null) return;
        var patrones = jefe.GetComponent<JefePatrones>();
        if (patrones != null) patrones.enabled = false;
        jefe.EsJefe = true;
        aparicionDelJefe = jefe.NumeroDeAparicion;
        // Que no choque con ninguna lapida ni cruz del decorado puesto.
        jefeIgnora = CapitulosDeEscenario.Chicos.Count > 0;
        foreach (var propio in jefe.GetComponentsInChildren<Collider>())
        {
            foreach (var chico in CapitulosDeEscenario.Chicos)
            {
                parejasDelJefe++;
                jefeIgnora &= Physics.GetIgnoreCollision(propio, chico);
            }
        }
    }

    static void SeguirAlJefe(EnemyController z)
    {
        Vector2 p = new Vector2(z.transform.position.x, z.transform.position.z);
        foreach (var r in CapitulosDeEscenario.Redondos)
            if (r.chico) jefeMasCercaDeUnaTumba = Mathf.Min(jefeMasCercaDeUnaTumba, Vector2.Distance(p, r.centro));
        if (!anilloMedido && jefeMasCercaDeUnaTumba < 1f) MedirElAnillo(z);
        // Y nunca adentro de un arbol, que lo frena.
        float alArbol = AlObstaculo(z.transform.position, true);
        if (alArbol < -0.2f)
        {
            adentro++;
            if (adentro <= 5) detalle.AppendLine("  el jefe adentro de un arbol en " + Plano(z.transform.position));
        }
    }

    static void MedirElAnillo(EnemyController z)
    {
        anilloMedido = true;
        var patrones = z.GetComponent<JefePatrones>();
        if (patrones == null) return;
        const System.Reflection.BindingFlags Privado = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        var punto = typeof(JefePatrones).GetMethod("PuntoDelAnillo", Privado);
        if (punto == null) return;
        Vector3 centro = z.transform.position;
        centro.y = 0f;
        for (int i = 0; i < 24; i++)
        {
            float a = i * Mathf.PI * 2f / 24f;
            var p = (Vector3)punto.Invoke(patrones, new object[] { centro, new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) });
            anilloMasCerca = Mathf.Min(anilloMasCerca, Vector2.Distance(new Vector2(p.x, p.z), new Vector2(centro.x, centro.z)));
            anilloPuntos++;
        }
    }

    static void Poner(Rigidbody cuerpo, PlayerController control, int cual)
    {
        Vector3 donde = new Vector3(escenario.lugares[cual].x, cuerpo.position.y, escenario.lugares[cual].z);
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
        inf.AppendLine("Prueba de los obstaculos en " + escenario.que + " (WaveMode, oleada " + escenario.oleada + "): el jugador quieto en dos lugares, "
                       + PorLugar + " s en cada uno");
        inf.AppendLine();
        if (error != null) inf.AppendLine("ERROR: " + error);
        inf.AppendLine("Huellas: " + CapitulosDeEscenario.Huellas.Count + "; redondos: " + CapitulosDeEscenario.Redondos.Count + "; zombis seguidos: " + vistos
                       + "; llegaron al jugador: " + llegaron + "; trabados (menos de " + Desplazamiento + " m en " + Ventana + " s): " + trabados
                       + ", de esos contra un obstaculo: " + contraObstaculos + "; vistos adentro de un obstaculo: " + adentro);
        if (escenario.conJefe)
            inf.AppendLine("El jefe: " + parejasDelJefe + " parejas de colliders con lo chico; lo mas cerca que paso del centro de una tumba: "
                           + (jefeMasCercaDeUnaTumba < float.MaxValue ? jefeMasCercaDeUnaTumba.ToString("0.00") + " m" : "nunca"));
        inf.Append(detalle);
        inf.AppendLine();
        var ok = new List<bool> { error == null, listo, llegaron >= 10, contraObstaculos == 0, adentro == 0 };
        var que = new List<string>
        {
            "el banco llego hasta el final",
            "el decorado estaba puesto, con sus obstaculos",
            "los zombis llegaron al jugador (al menos 10)",
            "ningun zombi se quedo trabado contra un obstaculo (menos de " + Desplazamiento + " m en " + Ventana + " s, pegado a uno)",
            "ningun zombi entro a un obstaculo",
        };
        if (escenario.conJefe)
        {
            ok.Add(jefe != null && jefeIgnora);
            que.Add("el jefe no choca con las lapidas ni con las cruces (IgnoreCollision con todas)");
            ok.Add(jefeMasCercaDeUnaTumba < CapitulosDeEscenario.RadioDeTumba + 0.3f);
            que.Add("el jefe camino por encima de una tumba (su centro paso a menos de " + (CapitulosDeEscenario.RadioDeTumba + 0.3f).ToString("0.0") + " m de una)");
            float cuerpoDelJefe = jefe != null ? EnemyController.RadioDelCuerpo(jefe.gameObject) : 0f;
            ok.Add(anilloPuntos > 0 && anilloMasCerca >= cuerpoDelJefe + 0.45f);
            que.Add("entre las tumbas, ningun invocado sale adentro del jefe (" + anilloPuntos + " puntos del anillo; el mas cerca a "
                    + (anilloPuntos > 0 ? anilloMasCerca.ToString("0.00") : "?") + " m de su centro, el cuerpo mide " + cuerpoDelJefe.ToString("0.00") + ")");
        }
        bool todo = true;
        for (int i = 0; i < ok.Count; i++)
        {
            inf.AppendLine((ok[i] ? "OK  " : "FALLA  ") + que[i]);
            todo &= ok[i];
        }
        inf.AppendLine();
        inf.AppendLine("RESULTADO: " + (todo ? "TODO OK" : "HAY FALLAS"));
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(escenario.ruta)));
        File.WriteAllText(escenario.ruta, inf.ToString());
        SessionState.SetBool(Clave, false);
        EditorApplication.ExitPlaymode();
        Debug.Log(inf.ToString());
    }
}
