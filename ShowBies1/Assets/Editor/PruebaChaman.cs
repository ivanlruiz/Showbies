using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;

// Banco del chaman (ZombiChaman, ZOMBIS.md), en play: WaveMode con las oleadas paradas, el camino del telefono sin
// auto-apuntado, el jugador quieto y un chaman que sale a 12 m al este, con mucha vida. Mide que se plante a 7 m, a la
// vista; que con aliados cerca se frene y avise 0,5 s con el aro creciendo; que potencie a los 4 mas cercanos adentro de
// 4,5 m (no al quinto ni al de afuera) por 3 s, con su marca, corriendo un 25 % mas y recibiendo un 25 % menos; que pulse
// cada 4 s; que el bate le corte el pulso; que congelado no pulse; que al morir el bono se apague en todos en el acto; y
// que se ponga detras de un tanque. Saca dos fotos (Builds/chaman_aviso.png y chaman_marcas.png) y escribe
// Builds/prueba_chaman.txt.
[InitializeOnLoad]
public static class PruebaChaman
{
    const string Clave = "ShowBies.PruebaChaman";
    const string Banco = "PruebaChaman";
    const string Ruta = "../Builds/prueba_chaman.txt";
    const string FotoAviso = "../Builds/chaman_aviso.png";
    const string FotoMarcas = "../Builds/chaman_marcas.png";
    const string Escena = "Assets/Escenas/WaveMode.unity";
    const string RutaNormal = "Assets/Prefabs/Personajes/Zombi.prefab";
    const string RutaTanque = "Assets/Prefabs/Personajes/ZombiTanque.prefab";
    const double TopeTotal = 150.0;

    enum Fase { Preparar, Llegar, Esperar, Aviso, Pulso, Marcas, Dura, Intervalo, Bate, BateImpacto, Hielo, HieloPulso, Muerte, Refugio, Fin }

    static Fase fase;
    static float desde, avisoEn, pulsoEn, ultimoPulsoEn;
    static double inicio;
    static bool empezo, terminado, fotoAviso;
    static string error;
    static readonly List<KeyValuePair<string, bool>> oks = new List<KeyValuePair<string, bool>>();
    static EnemyController zombi, caminante, quinto, afuera, tanque;
    static readonly List<EnemyController> cercanos = new List<EnemyController>();
    static ZombiChaman chaman;
    static int aparicion, pulsosAntes, cortadosAntes;
    static bool vioCaminar, avisoCaminando;
    static Vector3 lugar;

    static PruebaChaman()
    {
        EditorApplication.update += Tick;
    }

    [MenuItem("ShowBies/Pruebas/Chaman (play)")]
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
            fotoAviso = false;
            error = null;
            oks.Clear();
            cercanos.Clear();
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

    // Un normal quieto (el hielo al 100 %, salvo 'lentitud'), con mucha vida.
    static EnemyController Aliado(GameObject prefab, Vector3 donde, float lentitud)
    {
        var a = EnemyController.Aparecer(prefab, donde);
        if (a == null) return null;
        a.multiplicadorVida = 1000f;
        a.Congelar(lentitud, 120f);
        return a;
    }

    static void Avanzar()
    {
        var vida = PlayerHealth.instance;
        if (vida == null || EventSystem.current == null) return;
        var control = vida.GetComponent<PlayerController>();
        var cuerpo = vida.GetComponent<Rigidbody>();
        float ahora = Time.time;
        if (zombi != null && fase > Fase.Preparar && fase < Fase.Muerte && !EnemyController.SigueVivo(zombi, aparicion))
        {
            Terminar("el chaman murio en la fase " + fase);
            return;
        }
        vida.health = vida.maxHealth;

        switch (fase)
        {
            case Fase.Preparar:
            {
                if (ahora - desde < 1f) return;
                var oleadas = Object.FindAnyObjectByType<WaveManager>();
                if (oleadas != null) { oleadas.StopAllCoroutines(); oleadas.enabled = false; }
                foreach (var cajas in Object.FindObjectsByType<PowerUp>(FindObjectsSortMode.None)) cajas.enabled = false;
                EnemyController.DespejarAlrededor(Vector3.zero, 500f);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ConstructorZombis.RutaChaman);
                if (prefab == null) { Terminar("no esta el prefab del chaman"); return; }
                Poner(control, cuerpo, new Vector3(0f, 0f, -6f));
                zombi = EnemyController.Aparecer(prefab, new Vector3(12f, 0f, -6f));
                if (zombi == null) { Terminar("no aparecio el chaman"); return; }
                zombi.multiplicadorVida = 1000f;
                aparicion = zombi.NumeroDeAparicion;
                chaman = zombi.GetComponent<ZombiChaman>();
                if (chaman == null) { Terminar("el prefab no tiene ZombiChaman"); return; }
                fase = Fase.Llegar;
                desde = ahora;
                return;
            }

            case Fase.Llegar:
            {
                if (chaman.EstadoActual != ZombiChaman.Estado.Plantado)
                {
                    if (ahora - desde > 8f) { Terminar("no se planto nunca (" + chaman.EstadoActual + ")"); return; }
                    return;
                }
                float d = Distancia(zombi.transform.position, control.transform.position);
                Ok("sin aliados cerca no pulsa", chaman.Pulsos == 0);
                Ok("se planta a 7 m (" + d.ToString("0.0") + " m)", d > 6.3f && d < 7.6f);
                Vector3 v = Camera.main.WorldToViewportPoint(zombi.transform.position + Vector3.up * (zombi.AlturaDeLaCabeza * 0.5f));
                Ok("a la vista (" + v.x.ToString("0.00") + ", " + v.y.ToString("0.00") + ")", ZombiEscupidor.SeVeEn(v));
                Ok("con el aro rubi en los pies", chaman.AroPrendido && Mathf.Abs(chaman.RadioDelAroAhora - chaman.radioDelAro) < 0.01f);

                // Los aliados alrededor: tres cerca (y uno que camina, al empezar el aviso), un quinto mas lejos adentro
                // del radio y uno afuera.
                var normal = AssetDatabase.LoadAssetAtPath<GameObject>(RutaNormal);
                lugar = zombi.transform.position;
                Vector3 lejos = (lugar - control.transform.position);
                lejos.y = 0f;
                lejos.Normalize();
                Vector3 costado = Vector3.Cross(Vector3.up, lejos);
                cercanos.Clear();
                cercanos.Add(Aliado(normal, lugar + costado * 1.2f, 1f));
                cercanos.Add(Aliado(normal, lugar - costado * 2.4f, 1f));
                cercanos.Add(Aliado(normal, lugar + lejos * 3.2f, 1f));
                quinto = Aliado(normal, lugar + costado * 4.1f, 1f);
                afuera = Aliado(normal, lugar + lejos * 6f, 1f);
                fase = Fase.Esperar;
                desde = ahora;
                return;
            }

            case Fase.Esperar:
            {
                if (chaman.EstadoActual != ZombiChaman.Estado.Avisando)
                {
                    if (ahora - desde > 6f) { Terminar("con aliados cerca no aviso nunca"); return; }
                    return;
                }
                avisoEn = ahora;
                pulsosAntes = chaman.Pulsos;
                Ok("plantado, con aliados cerca, avisa", true);
                // El que camina: frenado a la mitad por el hielo, para medir el bono.
                var normal = AssetDatabase.LoadAssetAtPath<GameObject>(RutaNormal);
                Vector3 lejos = (lugar - control.transform.position);
                lejos.y = 0f;
                lejos.Normalize();
                caminante = Aliado(normal, lugar + lejos * 1f + Vector3.Cross(Vector3.up, lejos) * -1f, 0.5f);
                cercanos.Add(caminante);
                fase = Fase.Aviso;
                return;
            }

            case Fase.Aviso:
                if (!fotoAviso && ahora - avisoEn > 0.3f)
                {
                    fotoAviso = true;
                    Ok("el aro crece con el aviso (" + chaman.RadioDelAroAhora.ToString("0.0") + " m)", chaman.RadioDelAroAhora > chaman.radioDelAro + 1f);
                    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(FotoAviso)));
                    ScreenCapture.CaptureScreenshot(FotoAviso);
                }
                if (chaman.Pulsos == pulsosAntes)
                {
                    if (ahora - avisoEn > 2f) { Terminar("no pulso despues de avisar"); return; }
                    return;
                }
                pulsoEn = ahora;
                ultimoPulsoEn = ahora;
                Ok("pulsa cuando termina el aviso (" + (ahora - avisoEn).ToString("0.00") + " s, " + chaman.aviso + ")", Mathf.Abs(ahora - avisoEn - chaman.aviso) < 0.1f);
                fase = Fase.Pulso;
                return;

            case Fase.Pulso:
            {
                bool losCuatro = chaman.UltimosPotenciados == 4;
                foreach (var a in cercanos) losCuatro &= a != null && a.Potenciado && a.PotenciadoPor == zombi;
                Ok("potencia a los 4 mas cercanos (" + chaman.UltimosPotenciados + ")", losCuatro);
                Ok("no al quinto ni al de afuera", !quinto.Potenciado && !afuera.Potenciado);
                Ok("ni a si mismo", !zombi.Potenciado);
                // Lo que le entra: un 25 % menos al potenciado.
                float antes = cercanos[0].VidaActual;
                cercanos[0].DanoZombi(4f);
                float entro = antes - cercanos[0].VidaActual;
                float antesAfuera = afuera.VidaActual;
                afuera.DanoZombi(4f);
                float entroAfuera = antesAfuera - afuera.VidaActual;
                Ok("al potenciado le entra un 25 % menos (" + entro.ToString("0.00") + " de 4, y al otro " + entroAfuera.ToString("0.00") + ")",
                   Mathf.Abs(entro - 3f) < 0.01f && Mathf.Abs(entroAfuera - 4f) < 0.01f);
                fase = Fase.Marcas;
                desde = ahora;
                return;
            }

            case Fase.Marcas:
            {
                if (ahora - desde < 0.15f) return;
                Ok("con su marca rubi arriba (" + chaman.MarcasALaVista + ")", chaman.MarcasALaVista == 4);
                Vector3 v = caminante.GetComponent<Rigidbody>().linearVelocity;
                v.y = 0f;
                float esperada = caminante.enemyType.velocidad * 0.5f * chaman.velocidadConElBono;
                Ok("y corre un 25 % mas (" + v.magnitude.ToString("0.00") + " m/s, " + esperada.ToString("0.00") + ")", Mathf.Abs(v.magnitude - esperada) < 0.3f);
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(FotoMarcas)));
                ScreenCapture.CaptureScreenshot(FotoMarcas);
                fase = Fase.Dura;
                return;
            }

            case Fase.Dura:
                if (cercanos[1].Potenciado)
                {
                    if (ahora - pulsoEn > 5f) { Terminar("el bono no se terminaba"); return; }
                    return;
                }
                Ok("el bono dura 3 s (" + (ahora - pulsoEn).ToString("0.00") + ")", Mathf.Abs(ahora - pulsoEn - chaman.duracionDelBono) < 0.15f);
                // El que camina ya hizo lo suyo: que no se le pegue al jugador.
                caminante.DanoZombi(1e7f);
                cercanos.Remove(caminante);
                fase = Fase.Intervalo;
                desde = ahora;
                pulsosAntes = chaman.Pulsos;
                return;

            case Fase.Intervalo:
                if (chaman.MarcasALaVista != 0 && ahora - desde > 0.2f && chaman.Pulsos == pulsosAntes)
                {
                    Terminar("quedaron marcas con el bono terminado");
                    return;
                }
                if (chaman.Pulsos == pulsosAntes)
                {
                    if (ahora - pulsoEn > 7f) { Terminar("no volvio a pulsar"); return; }
                    return;
                }
                Ok("pulsa cada 4 s (" + (ahora - ultimoPulsoEn).ToString("0.00") + ")", Mathf.Abs(ahora - ultimoPulsoEn - chaman.cadaCuanto) < 0.15f);
                ultimoPulsoEn = ahora;
                fase = Fase.Bate;
                desde = ahora;
                return;

            case Fase.Bate:
            {
                if (chaman.EstadoActual != ZombiChaman.Estado.Avisando)
                {
                    if (ahora - desde > 6f) { Terminar("no aviso para probar el bate"); return; }
                    return;
                }
                Vector3 alJugador = control.transform.position - zombi.transform.position;
                alJugador.y = 0f;
                Vector3 donde = zombi.transform.position + alJugador.normalized * 1.5f;
                Poner(control, cuerpo, donde);
                Vector3 alZombi = zombi.transform.position - donde;
                alZombi.y = 0f;
                cuerpo.rotation = Quaternion.LookRotation(alZombi);
                control.transform.rotation = cuerpo.rotation;
                pulsosAntes = chaman.Pulsos;
                cortadosAntes = chaman.Cortados;
                if (Bate.Instancia == null || !Bate.Instancia.Golpear()) { Terminar("el bate no arranco"); return; }
                fase = Fase.BateImpacto;
                desde = ahora;
                return;
            }

            case Fase.BateImpacto:
                if (ahora - desde < 0.9f) return;
                Ok("el bate le corta el pulso", chaman.Cortados == cortadosAntes + 1 && chaman.Pulsos == pulsosAntes);
                Poner(control, cuerpo, new Vector3(0f, 0f, -6f));
                zombi.Congelar(0.4f, 5f);
                fase = Fase.Hielo;
                desde = ahora;
                return;

            case Fase.Hielo:
                if (ahora - desde < 4.6f)
                {
                    if (chaman.Pulsos != pulsosAntes || chaman.EstadoActual == ZombiChaman.Estado.Avisando)
                    {
                        Ok("congelado no pulsa", false);
                        fase = Fase.HieloPulso;
                        desde = ahora;
                    }
                    return;
                }
                Ok("congelado no pulsa", chaman.Pulsos == pulsosAntes);
                fase = Fase.HieloPulso;
                desde = ahora;
                return;

            case Fase.HieloPulso:
                if (chaman.Pulsos == pulsosAntes)
                {
                    if (ahora - desde > 4f) { Terminar("al descongelarse no volvio a pulsar"); return; }
                    return;
                }
                Ok("al descongelarse vuelve a pulsar", !zombi.Congelado);
                fase = Fase.Muerte;
                desde = ahora;
                return;

            case Fase.Muerte:
            {
                var suyos = new List<EnemyController>();
                foreach (var a in cercanos) if (a != null && a.PotenciadoPor == zombi) suyos.Add(a);
                if (quinto != null && quinto.PotenciadoPor == zombi) suyos.Add(quinto);
                zombi.DanoZombi(1e9f);
                bool apagados = true;
                foreach (var a in suyos) apagados &= !a.Potenciado && a.PotenciadoPor == null;
                Ok("al morir el chaman, el bono se apaga en todos en el acto (" + suyos.Count + " potenciados)", suyos.Count > 0 && !zombi.Vivo && apagados);
                fase = Fase.Refugio;
                desde = ahora;
                tanque = null;
                return;
            }

            case Fase.Refugio:
            {
                if (tanque == null)
                {
                    if (ahora - desde < 0.2f) return;
                    Ok("y se van las marcas", chaman.MarcasALaVista == 0 && !chaman.AroPrendido);
                    EnemyController.DespejarAlrededor(Vector3.zero, 500f);
                    var prefabTanque = AssetDatabase.LoadAssetAtPath<GameObject>(RutaTanque);
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ConstructorZombis.RutaChaman);
                    tanque = Aliado(prefabTanque, new Vector3(6.5f, 0f, -6f), 1f);
                    // Lejos del tanque, para que pulse caminando hacia el (lo tiene al alcance a mitad de camino).
                    zombi = EnemyController.Aparecer(prefab, new Vector3(14f, 0f, 0f));
                    vioCaminar = false;
                    avisoCaminando = false;
                    if (tanque == null || zombi == null) { Terminar("no aparecieron el tanque o el segundo chaman"); return; }
                    zombi.multiplicadorVida = 1000f;
                    aparicion = zombi.NumeroDeAparicion;
                    chaman = zombi.GetComponent<ZombiChaman>();
                    desde = ahora;
                    return;
                }
                Vector3 refugio = ZombiChaman.DetrasDe(tanque.transform.position, control.transform.position, chaman.detrasDelPesado);
                {
                    Vector3 v = zombi.GetComponent<Rigidbody>().linearVelocity;
                    v.y = 0f;
                    if (chaman.Pulsos == 0 && chaman.EstadoActual != ZombiChaman.Estado.Avisando && v.magnitude > 1f) vioCaminar = true;
                    if (chaman.Pulsos == 0 && chaman.EstadoActual == ZombiChaman.Estado.Avisando && vioCaminar && v.magnitude < 0.2f) avisoCaminando = true;
                }
                bool llego = chaman.EstadoActual == ZombiChaman.Estado.Plantado && Distancia(zombi.transform.position, refugio) < 0.5f;
                if (!llego)
                {
                    if (ahora - desde > 6f) { Terminar("no se puso detras del tanque (" + chaman.EstadoActual + ", a " + Distancia(zombi.transform.position, refugio).ToString("0.0") + " m)"); return; }
                    return;
                }
                Vector3 alTanque = tanque.transform.position - control.transform.position;
                Vector3 alChaman = zombi.transform.position - control.transform.position;
                alTanque.y = 0f;
                alChaman.y = 0f;
                Ok("con un tanque cerca, se pone detras de el (" + Vector3.Angle(alTanque, alChaman).ToString("0") + " grados, a "
                   + alChaman.magnitude.ToString("0.0") + " m)", Vector3.Angle(alTanque, alChaman) < 10f && alChaman.magnitude > alTanque.magnitude + 1f);
                Ok("y al tanque lo potencia (camina con la persecucion de siempre)", ZombiChaman.Potenciable(tanque, zombi));
                Ok("caminando, se frena para pulsar", vioCaminar && avisoCaminando);
                Terminar(null);
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
        inf.AppendLine("Prueba del chaman (WaveMode, camino del telefono, sin oleadas ni auto-apuntado)");
        inf.AppendLine();
        if (error != null) inf.AppendLine("ERROR: " + error);
        bool todo = error == null;
        inf.AppendLine((error == null ? "OK  " : "FALLA  ") + "el banco llego hasta el final");
        foreach (var ok in oks)
        {
            inf.AppendLine((ok.Value ? "OK  " : "FALLA  ") + ok.Key);
            todo &= ok.Value;
        }
        inf.AppendLine();
        inf.AppendLine("Fotos: " + FotoAviso + ", " + FotoMarcas);
        inf.AppendLine("RESULTADO: " + (todo ? "TODO OK" : "HAY FALLAS"));
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(Ruta)));
        File.WriteAllText(Ruta, inf.ToString());

        SessionState.SetBool(Clave, false);
        EditorApplication.ExitPlaymode();
        Debug.Log(inf.ToString());
    }
}
