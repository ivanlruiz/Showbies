using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Banco de los impactos, en play (16/10): si las balas le pegan a un zombi quieto segun lo cerca
// que este y con cada arma, disparando por el camino del telefono (el joystick de disparo, PlayerJS
// y GunController, como juega cualquiera). Salio de los clips para redes: apuntando a zombis a 1-3
// m, durante un tercio de segundo el zombi seguia con la vida entera. WaveMode sin oleadas, el
// jugador quieto mirando al este y un zombi normal quieto (el hielo al 100 %) a cada distancia, con
// mucha vida, un segundo de fuego por caso. Se corre con el tiempo fijo a 30 cuadros por segundo
// (lo que da el Redmi) o a 60. Escribe Builds/prueba_impactos_<fps>.txt.
[InitializeOnLoad]
public static class PruebaImpactos
{
    const string Clave = "ShowBies.PruebaImpactos";
    const string Banco = "PruebaImpactos";
    const string Escena = "Assets/Escenas/WaveMode.unity";
    const string PrefabNormal = "Assets/Prefabs/Personajes/Zombi.prefab";
    const double TopeTotal = 400.0;

    static readonly float[] Distancias = { 0.4f, 0.7f, 1.0f, 1.5f, 2.0f, 3.0f, 5.0f, 8.0f };
    // El daño por bala de la mejora que da cada arma de la mano (ver BulletController.DesdeDano).
    static readonly float[] DanoPorArma = { 1f, 3f, 6f, 11f, 17f };
    static readonly string[] NombreDelArma = { "pistola", "revolver", "escopeta", "rifle", "energia" };
    const float SegundosDeFuego = 1f;
    const float TirosPorSegundo = 10f;

    static int arma, distancia, fase;
    static float desde;
    static double inicio;
    static bool empezo;
    static EnemyController zombi;
    static float vidaAntes;
    static int tirosAntes;
    static Vector3 centro;
    static readonly StringBuilder informe = new StringBuilder();
    static readonly List<float[]> filas = new List<float[]>();

    static PruebaImpactos()
    {
        EditorApplication.update += Tick;
    }

    [MenuItem("ShowBies/Pruebas/Impactos de cerca (play, 30 fps)")]
    public static void Arrancar30() { SessionState.SetBool(Clave + ".alReves", false); Arrancar(30); }

    [MenuItem("ShowBies/Pruebas/Impactos de cerca (play, 60 fps)")]
    public static void Arrancar60() { SessionState.SetBool(Clave + ".alReves", false); Arrancar(60); }

    [MenuItem("ShowBies/Pruebas/Impactos de la pistola (play, 30 fps)")]
    public static void ArrancarPistola() { SessionState.SetBool(Clave + ".alReves", false); Arrancar(30, 1); }

    [MenuItem("ShowBies/Pruebas/Impactos al reves (play, 30 fps)")]
    public static void ArrancarAlReves() { SessionState.SetBool(Clave + ".alReves", true); Arrancar(30); }

    // Solo las primeras "armas" (1 = la pistola), para mirar un caso.
    public static void Arrancar(int fps, int armas)
    {
        SessionState.SetInt(Clave + ".hastaArma", armas);
        ArrancarConArmas(fps);
    }

    public static void Arrancar(int fps)
    {
        SessionState.SetInt(Clave + ".hastaArma", DanoPorArma.Length);
        ArrancarConArmas(fps);
    }

    static void ArrancarConArmas(int fps)
    {
        if (!RespaldoDelBanco.PuedeArrancar(Banco)) return;
        RespaldoDelBanco.Guardar(Banco);
        PlayerJS.FijarAutoApuntar(false);
        PruebaDisparo.ModoTelefono(Clave);
        PlayerSettings.runInBackground = true;
        Progreso.GuardarOleadaEnCurso(1, 0);
        Progreso.GuardarPoderEnCurso(new EstadoPoder());
        Progreso.Guardar();
        EditorSceneManager.OpenScene(Escena);
        SessionState.SetInt(Clave + ".fps", fps);
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
            arma = distancia = fase = 0;
            desde = Time.time;
            inicio = EditorApplication.timeSinceStartup;
            informe.Length = 0;
            filas.Clear();
            zombi = null;
            Time.captureFramerate = SessionState.GetInt(Clave + ".fps", 30);
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

    // El arma del caso: en orden, o al reves (la pistola al final), para separar lo que es del arma
    // de lo que es del momento de la partida.
    static int OrdenDelArma(int i)
    {
        return SessionState.GetBool(Clave + ".alReves", false) ? DanoPorArma.Length - 1 - i : i;
    }

    static void Avanzar()
    {
        var vida = PlayerHealth.instance;
        if (vida == null) return;
        var control = vida.GetComponent<PlayerController>();
        var joysticks = vida.GetComponent<PlayerJS>();
        var cuerpo = vida.GetComponent<Rigidbody>();
        var gun = control != null ? control.theGun : null;
        if (control == null || joysticks == null || cuerpo == null || gun == null) return;
        vida.health = vida.maxHealth;
        control.cantBalas = Mathf.Max(control.cantBalas, 999);
        float t = Time.time - desde;

        // Preparar: sin oleadas ni cajas, el jugador quieto en la pradera.
        if (fase == 0)
        {
            if (t < 1f) return;
            var oleadas = Object.FindAnyObjectByType<WaveManager>();
            if (oleadas != null) { oleadas.StopAllCoroutines(); oleadas.enabled = false; }
            foreach (var cajas in Object.FindObjectsByType<PowerUp>(FindObjectsSortMode.None)) cajas.enabled = false;
            EnemyController.DespejarAlrededor(Vector3.zero, 500f);
            centro = new Vector3(0f, cuerpo.position.y, -6f);
            fase = 1;
            desde = Time.time;
            return;
        }

        cuerpo.position = centro;
        cuerpo.linearVelocity = Vector3.zero;

        // Poner el caso: el arma y el zombi a su distancia, al este.
        if (fase == 1)
        {
            if (t < 0.6f) { PruebaDisparo.Soltar(joysticks.lookJoystick); return; }   // que se vayan las balas de antes
            if (zombi != null) EnemyController.DespejarAlrededor(centro, 500f);
            gun.FijarDanoPorBala(DanoPorArma[OrdenDelArma(arma)]);
            gun.FijarTirosPorSegundo(TirosPorSegundo);
            var molde = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabNormal);
            Vector3 donde = centro + Vector3.right * Distancias[distancia];
            donde.y = 0f;
            zombi = EnemyController.Aparecer(molde, donde);
            if (zombi == null) { Terminar("no aparecio el zombi"); return; }
            zombi.multiplicadorVida = 1000f;
            zombi.multiplicadorDano = 0f;
            zombi.Congelar(1f, 600f);
            vidaAntes = zombi.VidaActual;
            fase = 2;
            desde = Time.time;
            return;
        }

        // Apuntar al este hasta que el arma este de frente, y despues un segundo de fuego.
        if (fase == 2)
        {
            PruebaDisparo.Apretar(joysticks.lookJoystick, Vector2.right);
            if (t < 0.2f) return;
            tirosAntes = gun.TirosDisparados;
            vidaAntes = zombi.VidaActual;
            fase = 3;
            desde = Time.time;
            return;
        }

        if (fase == 3)
        {
            PruebaDisparo.Apretar(joysticks.lookJoystick, Vector2.right);
            // Por donde pasan las balas y donde estan los colliders del zombi, el primer tercio de
            // segundo del caso de 3 m de cada arma.
            if (Mathf.Abs(Distancias[distancia] - 3f) < 0.01f && t < 0.34f)
            {
                var sb = new StringBuilder("  " + NombreDelArma[OrdenDelArma(arma)] + " t " + t.ToString("0.00") + ": balas");
                foreach (var b in Object.FindObjectsByType<BulletController>(FindObjectsSortMode.None))
                {
                    var c = b.GetComponent<BoxCollider>();
                    if (c == null || !c.enabled || !b.gameObject.activeInHierarchy) continue;
                    var bb = c.bounds;
                    sb.Append(" [" + (bb.center - centro).ToString("0.00") + " tam " + bb.size.ToString("0.00") + (c.isTrigger ? " trigger" : "") + "]");
                }
                foreach (var col in zombi.GetComponentsInChildren<Collider>())
                {
                    if (!col.enabled) continue;
                    var zb = col.bounds;
                    sb.Append(" | zombi " + col.name + " " + (zb.min - centro).ToString("0.00") + " a " + (zb.max - centro).ToString("0.00"));
                }
                if (gun.boca != null) sb.Append(" | boca " + (gun.boca.position - centro).ToString("0.00"));
                sb.Append(" | firePoint " + (gun.firePoint != null ? (gun.firePoint.position - centro).ToString("0.00") + " mira " + gun.firePoint.forward.ToString("0.00") : "-"));
                sb.Append(" | arma mira " + gun.transform.forward.ToString("0.00") + ", jugador mira " + vida.transform.forward.ToString("0.00"));
                sb.Append(" | joystick " + joysticks.lookJoystick.Direction.ToString("0.00") + ", PlayerJS " + joysticks.UltimaDireccionApuntada.ToString("0.00")
                          + ", congelado " + MenuPausa.JuegoCongelado + ", movil " + Plataforma.EsMovil + ", isFiring " + gun.isFiring
                          + ", padre del firePoint " + (gun.firePoint != null && gun.firePoint.parent != null ? gun.firePoint.parent.name : "-"));
                informe.AppendLine(sb.ToString());
            }
            if (t < SegundosDeFuego) return;
            PruebaDisparo.Soltar(joysticks.lookJoystick);
            int tiros = gun.TirosDisparados - tirosAntes;
            float perdio = vidaAntes - zombi.VidaActual;
            float porTiro = gun.DanoPorTiro;
            Vector3 dondeQuedo = zombi.transform.position - centro;
            filas.Add(new[] { OrdenDelArma(arma), Distancias[distancia], tiros, perdio, porTiro, new Vector2(dondeQuedo.x, dondeQuedo.z).magnitude });
            distancia++;
            if (distancia >= Distancias.Length) { distancia = 0; arma++; }
            int hasta = SessionState.GetInt(Clave + ".hastaArma", DanoPorArma.Length);
            if (arma >= Mathf.Min(hasta, DanoPorArma.Length)) { Terminar(null); return; }
            fase = 1;
            desde = Time.time;
        }
    }

    static void Terminar(string error)
    {
        int fps = SessionState.GetInt(Clave + ".fps", 30);
        informe.AppendLine("Impactos de cerca: un zombi normal quieto a cada distancia, un segundo de fuego por el joystick (" + fps + " cuadros por segundo)");
        informe.AppendLine();
        if (error != null) informe.AppendLine("ERROR: " + error);
        informe.AppendLine("arma       distancia  tiros  vida perdida  daño por tiro  perdida/(tiros*daño)  quedo a");
        foreach (var f in filas)
        {
            float esperado = f[2] * f[4];
            informe.AppendLine(NombreDelArma[(int)f[0]].PadRight(10) + " " + f[1].ToString("0.0").PadLeft(6) + " m  " + f[2].ToString("0").PadLeft(5)
                               + "  " + f[3].ToString("0.0").PadLeft(12) + "  " + f[4].ToString("0.0").PadLeft(13)
                               + "  " + (esperado > 0f ? f[3] / esperado : 0f).ToString("0.00").PadLeft(20) + "  " + f[5].ToString("0.0") + " m");
        }
        // Lo que se exige: con cada arma que no reparte (la escopeta abre en abanico y empuja al zombi
        // con sus perdigones), de 0,7 a 5 m, que le llegue al menos la mitad de lo tirado. Con el rumbo
        // viejo del cuerpo (antes del 16/10) la pistola, primera en la sesion, no le pegaba nada.
        int fallas = 0;
        foreach (var f in filas)
        {
            if ((int)f[0] == 2 || f[1] < 0.69f || f[1] > 5.01f) continue;
            float esperado = f[2] * f[4];
            if (esperado <= 0f || f[3] / esperado < 0.5f)
            {
                fallas++;
                informe.AppendLine("FALLA: " + NombreDelArma[(int)f[0]] + " a " + f[1].ToString("0.0") + " m le llega " + (esperado > 0f ? f[3] / esperado : 0f).ToString("0.00"));
            }
        }
        informe.AppendLine();
        informe.AppendLine(error == null && fallas == 0 && filas.Count > 0 ? "RESULTADO: TODO OK" : "RESULTADO: HAY FALLAS");
        Directory.CreateDirectory("../Builds");
        File.WriteAllText("../Builds/prueba_impactos_" + fps + ".txt", informe.ToString());
        Debug.Log("PruebaImpactos: listo, en Builds/prueba_impactos_" + fps + ".txt" + (error != null ? " (con error)" : ""));
        Time.captureFramerate = 0;
        SessionState.SetBool(Clave, false);
        PruebaDisparo.DevolverElTeclado(Clave);
        if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
    }
}
