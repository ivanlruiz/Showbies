using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;

// Banco del saltador (ZombiSaltador, ZOMBIS.md), en play: WaveMode con las oleadas paradas, el camino del telefono sin
// auto-apuntado (que no lo mate), el jugador quieto y un saltador que sale a 12 m al norte, con mucha vida. Mide que se
// agache a 6 m con la franja apuntando al jugador; que salte a 14 m/s y le pegue 1,2 veces el golpe si el jugador se queda;
// que al caer se quede quieto y sin zarpazos; que no vuelva a saltar antes de 5 s; que si el jugador sale de la franja no
// le pegue; que el bate le corte el salto; y que congelado salte 3,2 m y se recupere el doble. Saca una foto con la franja
// (Builds/saltador.png) y escribe Builds/prueba_saltador.txt.
[InitializeOnLoad]
public static class PruebaSaltador
{
    const string Clave = "ShowBies.PruebaSaltador";
    const string Banco = "PruebaSaltador";
    const string Ruta = "../Builds/prueba_saltador.txt";
    const string Foto = "../Builds/saltador.png";
    const string Escena = "Assets/Escenas/WaveMode.unity";
    const double TopeTotal = 120.0;

    enum Fase { Preparar, Agacharse, Saltar, Caer, Recarga, Esquivar, EsquivarCaer, Bate, BateImpacto, Hielo, HieloCaer, Fin }

    static Fase fase;
    static float desde, agachoEn, saltoEn, cayoEn;
    static double inicio;
    static bool empezo, terminado, fotoSacada, zarpoRecuperando;
    static string error;
    static readonly List<KeyValuePair<string, bool>> oks = new List<KeyValuePair<string, bool>>();
    static readonly StringBuilder notas = new StringBuilder();
    static EnemyController zombi;
    static ZombiSaltador saltador;
    static int aparicion, golpesAntes, saltosAntes, cortadosAntes, zarpazosAntes;
    static float vidaAntes, velocidadMaxima, golpeEsperado;

    static PruebaSaltador()
    {
        EditorApplication.update += Tick;
    }

    [MenuItem("ShowBies/Pruebas/Saltador (play)")]
    // Publico para correrlo por codigo (ver la trampa del registro de menus).
    public static void Arrancar()
    {
        if (!RespaldoDelBanco.PuedeArrancar(Banco)) return;
        RespaldoDelBanco.Guardar(Banco);
        PruebaDisparo.ModoTelefono(Clave);
        PlayerSettings.runInBackground = true;
        PlayerJS.FijarAutoApuntar(false);
        var c = CatalogoMejoras.Instancia;
        Progreso.DepurarFijarNivel(c.bate.id, 1);
        Progreso.DepurarFijarNivel(c.vidaMaxima.id, 10);
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
            fotoSacada = false;
            error = null;
            oks.Clear();
            notas.Length = 0;
            fase = Fase.Preparar;
            desde = Time.time;
            inicio = EditorApplication.timeSinceStartup;
            return;
        }
        if (EditorApplication.timeSinceStartup - inicio > TopeTotal) { Terminar("se paso del tope de " + TopeTotal + " s (fase " + fase + ")"); return; }

        try
        {
            Avanzar();
        }
        catch (System.Exception e)
        {
            Terminar("excepcion: " + e);
        }
    }

    static void Ok(string que, bool bien)
    {
        oks.Add(new KeyValuePair<string, bool>(que, bien));
    }

    static float Distancia(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    static void Poner(PlayerController control, Rigidbody cuerpo, Vector3 donde)
    {
        donde.y = cuerpo.position.y;
        cuerpo.position = donde;
        cuerpo.linearVelocity = Vector3.zero;
        control.transform.position = donde;
    }

    static void Avanzar()
    {
        var vida = PlayerHealth.instance;
        if (vida == null || EventSystem.current == null) return;
        var control = vida.GetComponent<PlayerController>();
        var cuerpo = vida.GetComponent<Rigidbody>();
        float ahora = Time.time;
        if (zombi != null && fase > Fase.Preparar && fase < Fase.Fin && !EnemyController.SigueVivo(zombi, aparicion))
        {
            Terminar("el saltador murio en la fase " + fase);
            return;
        }

        switch (fase)
        {
            case Fase.Preparar:
            {
                if (ahora - desde < 1f) return;
                var oleadas = Object.FindAnyObjectByType<WaveManager>();
                if (oleadas != null) { oleadas.StopAllCoroutines(); oleadas.enabled = false; }
                foreach (var cajas in Object.FindObjectsByType<PowerUp>(FindObjectsSortMode.None)) cajas.enabled = false;
                EnemyController.DespejarAlrededor(Vector3.zero, 500f);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ConstructorZombis.RutaSaltador);
                if (prefab == null) { Terminar("no esta el prefab del saltador"); return; }
                Poner(control, cuerpo, new Vector3(0f, 0f, -6f));
                zombi = EnemyController.Aparecer(prefab, new Vector3(0f, 0f, 6f));
                if (zombi == null) { Terminar("no aparecio el saltador"); return; }
                zombi.multiplicadorVida = 1000f;
                // Un golpe grande, para que el x1,2 del salto se distinga en una vida entera (12 contra 10).
                zombi.multiplicadorDano = 10f;
                aparicion = zombi.NumeroDeAparicion;
                saltador = zombi.GetComponent<ZombiSaltador>();
                if (saltador == null) { Terminar("el prefab no tiene ZombiSaltador"); return; }
                vida.health = vida.maxHealth;
                fase = Fase.Agacharse;
                desde = ahora;
                return;
            }

            case Fase.Agacharse:
            {
                if (saltador.EstadoActual != ZombiSaltador.Estado.Agachado)
                {
                    if (ahora - desde > 6f) { Terminar("no se agacho nunca"); return; }
                    return;
                }
                agachoEn = ahora;
                float d = Distancia(zombi.transform.position, control.transform.position);
                Vector3 alJugador = control.transform.position - zombi.transform.position;
                alJugador.y = 0f;
                Ok("se agacha a unos 6 m (" + d.ToString("0.0") + " m)", d > 5.2f && d <= 6.3f);
                Ok("con la franja en el piso", saltador.FranjaPrendida);
                Ok("apuntando al jugador (" + Vector3.Angle(saltador.Rumbo, alJugador).ToString("0") + " grados)", Vector3.Angle(saltador.Rumbo, alJugador) < 5f);
                Ok("agachado no tira zarpazos", !zombi.puedeZarpar);
                if (!fotoSacada)
                {
                    fotoSacada = true;
                    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(Foto)));
                    ScreenCapture.CaptureScreenshot(Foto);
                }
                golpesAntes = zombi.GolpesDados;
                vidaAntes = vida.health;
                golpeEsperado = zombi.enemyType.daño * zombi.multiplicadorDano * saltador.golpeDelSalto;
                fase = Fase.Saltar;
                return;
            }

            case Fase.Saltar:
                if (saltador.EstadoActual == ZombiSaltador.Estado.Agachado)
                {
                    if (ahora - agachoEn > 2f) { Terminar("no salto"); return; }
                    return;
                }
                saltoEn = ahora;
                Ok("salta cuando termina el aviso (" + (ahora - agachoEn).ToString("0.00") + " s, " + saltador.aviso + ")", Mathf.Abs(ahora - agachoEn - saltador.aviso) < 0.1f);
                velocidadMaxima = 0f;
                fase = Fase.Caer;
                return;

            case Fase.Caer:
                if (saltador.EstadoActual == ZombiSaltador.Estado.Saltando || saltador.EstadoActual == ZombiSaltador.Estado.Agachado)
                {
                    // La velocidad se fija en el paso de fisica siguiente al de saltar: se mira la mayor del vuelo.
                    Vector3 v = zombi.GetComponent<Rigidbody>().linearVelocity;
                    v.y = 0f;
                    velocidadMaxima = Mathf.Max(velocidadMaxima, v.magnitude);
                    if (ahora - saltoEn > 2f) { Terminar("no cayo"); return; }
                    return;
                }
                cayoEn = ahora;
                Ok("salta a 14 m/s (" + velocidadMaxima.ToString("0.0") + ")", Mathf.Abs(velocidadMaxima - saltador.velocidadDelSalto) < 1f);
                Ok("si el jugador se queda, le pega una vez en el salto", zombi.GolpesDados == golpesAntes + 1);
                Ok("con 1,2 veces el golpe (le saco " + (vidaAntes - vida.health) + ", " + golpeEsperado.ToString("0.0") + ")", Mathf.Abs((vidaAntes - vida.health) - golpeEsperado) < 1f);
                Ok("al caer se queda quieto", saltador.EstadoActual == ZombiSaltador.Estado.Recuperando);
                zarpazosAntes = EnemyController.ZarpazosEmpezados;
                zarpoRecuperando = false;
                fase = Fase.Recarga;
                desde = ahora;
                return;

            case Fase.Recarga:
                if (saltador.EstadoActual == ZombiSaltador.Estado.Recuperando && EnemyController.ZarpazosEmpezados != zarpazosAntes) zarpoRecuperando = true;
                if (saltador.EstadoActual == ZombiSaltador.Estado.Recuperando) return;
                Ok("recuperandose no tira zarpazos", !zarpoRecuperando);
                Ok("se recupera en " + (ahora - cayoEn).ToString("0.00") + " s (" + saltador.recuperacion + ")", Mathf.Abs(ahora - cayoEn - saltador.recuperacion) < 0.1f);
                // Lejos, para que vuelva a acercarse, y esperar el proximo salto.
                Poner(control, cuerpo, zombi.transform.position + new Vector3(0f, 0f, -10f));
                saltosAntes = saltador.Saltos;
                fase = Fase.Esquivar;
                desde = ahora;
                return;

            case Fase.Esquivar:
                if (saltador.EstadoActual != ZombiSaltador.Estado.Agachado)
                {
                    if (ahora - desde > 12f) { Terminar("no se volvio a agachar"); return; }
                    return;
                }
                // La recarga se cuenta desde que salta: se vuelve a agachar recien a los 5 s del salto anterior.
                Ok("no vuelve a agacharse antes de la recarga (" + (ahora - saltoEn).ToString("0.0") + " s desde el salto, " + saltador.recarga + ")",
                   ahora - saltoEn >= saltador.recarga - 0.05f);
                // El jugador sale de la franja, de costado.
                Poner(control, cuerpo, control.transform.position + Vector3.Cross(Vector3.up, saltador.Rumbo) * 3f);
                golpesAntes = zombi.GolpesDados;
                vidaAntes = vida.health;
                fase = Fase.EsquivarCaer;
                desde = ahora;
                return;

            case Fase.EsquivarCaer:
                if (saltador.EstadoActual != ZombiSaltador.Estado.Recuperando)
                {
                    if (ahora - desde > 3f) { Terminar("no cayo en el esquive"); return; }
                    return;
                }
                Ok("si el jugador sale de la franja, no le pega", zombi.GolpesDados == golpesAntes && vida.health == vidaAntes);
                Ok("salta el largo entero en el aire (" + saltador.UltimoLargo.ToString("0.0") + " m, 5,6)", Mathf.Abs(saltador.UltimoLargo - saltador.velocidadDelSalto * saltador.duracionDelSalto) < 0.6f);
                fase = Fase.Bate;
                desde = ahora;
                return;

            case Fase.Bate:
            {
                if (saltador.EstadoActual != ZombiSaltador.Estado.Agachado)
                {
                    if (ahora - desde > 12f) { Terminar("no se agacho para probar el bate"); return; }
                    // Cerca, para que se agache.
                    if (Distancia(zombi.transform.position, control.transform.position) > 8f)
                        Poner(control, cuerpo, zombi.transform.position + new Vector3(0f, 0f, -7f));
                    return;
                }
                Vector3 donde = zombi.transform.position + saltador.Rumbo * 1.5f;
                Poner(control, cuerpo, donde);
                Vector3 alZombi = zombi.transform.position - donde;
                alZombi.y = 0f;
                cuerpo.rotation = Quaternion.LookRotation(alZombi);
                control.transform.rotation = cuerpo.rotation;
                saltosAntes = saltador.Saltos;
                cortadosAntes = saltador.Cortados;
                if (Bate.Instancia == null || !Bate.Instancia.Golpear()) { Terminar("el bate no arranco"); return; }
                fase = Fase.BateImpacto;
                desde = ahora;
                return;
            }

            case Fase.BateImpacto:
                if (ahora - desde < 0.9f) return;
                Ok("el bate le corta el salto y lo empuja", saltador.Cortados == cortadosAntes + 1 && saltador.Saltos == saltosAntes && Bate.Instancia.UltimosEmpujados == 1);
                zombi.Congelar(0.4f, 30f);
                Poner(control, cuerpo, zombi.transform.position + new Vector3(0f, 0f, -8f));
                fase = Fase.Hielo;
                desde = ahora;
                return;

            case Fase.Hielo:
                if (saltador.EstadoActual != ZombiSaltador.Estado.Agachado)
                {
                    if (ahora - desde > 15f) { Terminar("congelado no se agacho"); return; }
                    return;
                }
                Poner(control, cuerpo, control.transform.position + Vector3.Cross(Vector3.up, saltador.Rumbo) * 3f);
                fase = Fase.HieloCaer;
                desde = ahora;
                return;

            case Fase.HieloCaer:
                if (saltador.EstadoActual != ZombiSaltador.Estado.Recuperando)
                {
                    if (ahora - desde > 3f) { Terminar("congelado no cayo"); return; }
                    return;
                }
                if (cayoEn < desde) cayoEn = ahora;
                Ok("congelado salta 3,2 m (" + saltador.UltimoLargo.ToString("0.0") + ")", Mathf.Abs(saltador.UltimoLargo - saltador.largoDelSaltoCongelado) < 0.5f);
                fase = Fase.Fin;
                desde = ahora;
                return;

            case Fase.Fin:
                if (saltador.EstadoActual == ZombiSaltador.Estado.Recuperando)
                {
                    if (ahora - cayoEn > 4f) { Terminar("congelado no termino de recuperarse"); return; }
                    return;
                }
                Ok("congelado se recupera en " + (ahora - cayoEn).ToString("0.0") + " s (" + saltador.recuperacionCongelado + ")", Mathf.Abs(ahora - cayoEn - saltador.recuperacionCongelado) < 0.15f);
                Terminar(null);
                return;
        }
    }

    static void Terminar(string motivo)
    {
        if (terminado) return;
        terminado = true;
        if (error == null) error = motivo;

        var inf = new StringBuilder();
        inf.AppendLine("Prueba del saltador (WaveMode, camino del telefono, sin oleadas ni auto-apuntado)");
        inf.AppendLine();
        if (error != null) inf.AppendLine("ERROR: " + error);
        inf.Append(notas);
        inf.AppendLine();
        bool todo = error == null;
        inf.AppendLine((error == null ? "OK  " : "FALLA  ") + "el banco llego hasta el final");
        foreach (var ok in oks)
        {
            inf.AppendLine((ok.Value ? "OK  " : "FALLA  ") + ok.Key);
            todo &= ok.Value;
        }
        inf.AppendLine();
        inf.AppendLine("Foto: " + Foto);
        inf.AppendLine("RESULTADO: " + (todo ? "TODO OK" : "HAY FALLAS"));
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(Ruta)));
        File.WriteAllText(Ruta, inf.ToString());

        SessionState.SetBool(Clave, false);
        EditorApplication.ExitPlaymode();
        Debug.Log(inf.ToString());
    }
}
