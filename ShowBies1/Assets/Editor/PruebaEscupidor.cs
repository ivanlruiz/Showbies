using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;

// Banco del escupidor (ZombiEscupidor y AcidoDelEscupidor, ZOMBIS.md), en play: WaveMode con las oleadas paradas, el
// camino del telefono con el auto-apuntado prendido, y un escupidor que sale a 16 m al norte del jugador quieto, con
// mucha vida. Mide que se plante lejos (a 9,5 m, o donde se lo vea) y que el auto-apuntado no le tire; que avise con el
// anillo donde esta el jugador, escupa, el globo caiga ahi y el charco pegue la mitad del golpe cada medio segundo
// despues de la gracia; que afuera del charco no pegue; que no haya mas de 3 charcos; que retroceda si el jugador se le
// acerca; que el bate le corte el escupitajo; y que congelado tarde 5 s entre uno y otro. Saca una foto con el anillo y
// el charco (Builds/escupidor.png) y escribe Builds/prueba_escupidor.txt.
[InitializeOnLoad]
public static class PruebaEscupidor
{
    const string Clave = "ShowBies.PruebaEscupidor";
    const string Banco = "PruebaEscupidor";
    const string Ruta = "../Builds/prueba_escupidor.txt";
    const string Foto = "../Builds/escupidor.png";
    const string Escena = "Assets/Escenas/WaveMode.unity";
    const double TopeTotal = 120.0;

    enum Fase { Preparar, Llegar, Plantado, Aviso, Vuelo, Charco, Afuera, Tope, Retroceder, Bate, BateImpacto, Hielo, Fin }

    static Fase fase;
    static float desde, avisoEn, cayoEn;
    static double inicio;
    static bool empezo, terminado, fotoSacada;
    static string error;
    static readonly List<KeyValuePair<string, bool>> oks = new List<KeyValuePair<string, bool>>();
    static readonly StringBuilder notas = new StringBuilder();
    static EnemyController zombi;
    static ZombiEscupidor escupidor;
    static int aparicion, escupitajosAntes, golpesAntes, cortadosAntes;
    static float vidaAntes, distanciaPlantado, dispararon, distanciaAntes;
    static readonly List<float> momentos = new List<float>();

    static PruebaEscupidor()
    {
        EditorApplication.update += Tick;
    }

    [MenuItem("ShowBies/Pruebas/Escupidor (play)")]
    // Publico para correrlo por codigo (ver la trampa del registro de menus).
    public static void Arrancar()
    {
        if (!RespaldoDelBanco.PuedeArrancar(Banco)) return;
        RespaldoDelBanco.Guardar(Banco);
        PruebaDisparo.ModoTelefono(Clave);
        PlayerSettings.runInBackground = true;
        PlayerJS.FijarAutoApuntar(true);
        var c = CatalogoMejoras.Instancia;
        Progreso.DepurarFijarNivel(c.bate.id, 1);
        Progreso.DepurarFijarNivel(c.danoBala.id, 0);
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
            momentos.Clear();
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
            Terminar("el escupidor murio en la fase " + fase);
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
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ConstructorZombis.RutaEscupidor);
                if (prefab == null) { Terminar("no esta el prefab del escupidor"); return; }
                Poner(control, cuerpo, new Vector3(0f, 0f, -6f));
                zombi = EnemyController.Aparecer(prefab, new Vector3(0f, 0f, 10f));
                if (zombi == null) { Terminar("no aparecio el escupidor"); return; }
                zombi.multiplicadorVida = 1000f;
                // Un golpe grande, para que la mitad del charco se distinga en una vida entera (5 por golpe, no 0,5).
                zombi.multiplicadorDano = 10f;
                aparicion = zombi.NumeroDeAparicion;
                escupidor = zombi.GetComponent<ZombiEscupidor>();
                if (escupidor == null) { Terminar("el prefab no tiene ZombiEscupidor"); return; }
                vida.health = vida.maxHealth;
                dispararon = 0f;
                fase = Fase.Llegar;
                desde = ahora;
                return;
            }

            case Fase.Llegar:
                if (escupidor.EstadoActual == ZombiEscupidor.Estado.Acercandose || escupidor.EstadoActual == ZombiEscupidor.Estado.Rodeando)
                {
                    if (ahora - desde > 8f) { Terminar("no se planto nunca"); return; }
                    return;
                }
                distanciaPlantado = Distancia(zombi.transform.position, control.transform.position);
                Ok("se planta a 9,5 m, afuera del auto-apuntado (" + distanciaPlantado.ToString("0.0") + " m)", distanciaPlantado > 8.5f && distanciaPlantado < 10.2f);
                {
                    Vector3 v = Camera.main.WorldToViewportPoint(zombi.transform.position + Vector3.up * (zombi.AlturaDeLaCabeza * 0.5f));
                    Ok("y a la vista, lejos del HUD de arriba (" + v.x.ToString("0.00") + ", " + v.y.ToString("0.00") + ")", ZombiEscupidor.SeVeEn(v));
                }
                fase = Fase.Plantado;
                desde = ahora;
                return;

            case Fase.Plantado:
                if (control.theGun.isFiring) dispararon += Time.deltaTime;
                if (escupidor.EstadoActual != ZombiEscupidor.Estado.Avisando)
                {
                    if (ahora - desde > 4f) { Terminar("no aviso nunca"); return; }
                    return;
                }
                Ok("el auto-apuntado no le tira mientras esta lejos (" + dispararon.ToString("0.00") + " s disparando)", distanciaPlantado < 7.5f || dispararon < 0.05f);
                avisoEn = ahora;
                escupitajosAntes = escupidor.Escupitajos;
                Ok("avisa con el anillo en el piso", escupidor.AnilloPrendido);
                Ok("el anillo marca donde esta el jugador (" + Distancia(escupidor.Objetivo, control.transform.position).ToString("0.00") + " m)",
                   Distancia(escupidor.Objetivo, control.transform.position) < 0.4f);
                fase = Fase.Aviso;
                return;

            case Fase.Aviso:
                if (escupidor.Escupitajos == escupitajosAntes)
                {
                    if (ahora - avisoEn > 2f) { Terminar("no escupio despues de avisar"); return; }
                    return;
                }
                Ok("escupe cuando termina el aviso (" + (ahora - avisoEn).ToString("0.00") + " s, " + escupidor.aviso + ")", Mathf.Abs(ahora - avisoEn - escupidor.aviso) < 0.12f);
                Ok("el anillo se apaga al escupir", !escupidor.AnilloPrendido);
                Ok("sale un globo", AcidoDelEscupidor.GlobosEnElAire == 1);
                fase = Fase.Vuelo;
                desde = ahora;
                return;

            case Fase.Vuelo:
                if (AcidoDelEscupidor.CharcosEnElPiso == 0)
                {
                    if (ahora - desde > 3f) { Terminar("el globo no cayo"); return; }
                    return;
                }
                cayoEn = ahora;
                float vuelo = ahora - desde;
                float esperado = Distancia(zombi.transform.position, escupidor.Objetivo) / escupidor.velocidadDelEscupitajo;
                Ok("el globo vuela lo que tarda a 11 m/s (" + vuelo.ToString("0.00") + " s, unos " + esperado.ToString("0.00") + ")", Mathf.Abs(vuelo - esperado) < 0.25f);
                Ok("cae donde marco el anillo (" + Distancia(AcidoDelEscupidor.UltimoCharco, escupidor.Objetivo).ToString("0.00") + " m)",
                   Distancia(AcidoDelEscupidor.UltimoCharco, escupidor.Objetivo) < 0.05f);
                golpesAntes = AcidoDelEscupidor.GolpesDados;
                vidaAntes = vida.health;
                if (!fotoSacada)
                {
                    fotoSacada = true;
                    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(Foto)));
                    ScreenCapture.CaptureScreenshot(Foto);
                }
                fase = Fase.Charco;
                return;

            case Fase.Charco:
            {
                // El jugador no se movio: esta adentro. La gracia, y despues un golpe cada medio segundo.
                int golpes = AcidoDelEscupidor.GolpesDados - golpesAntes;
                if (golpes > momentos.Count) momentos.Add(ahora - cayoEn);
                if (ahora - cayoEn < 2.1f) return;
                notas.AppendLine("golpes del charco a los " + string.Join(", ", momentos.ConvertAll(m => m.ToString("0.00"))) + " s de caer");
                Ok("respeta la gracia: el primer golpe a los " + (momentos.Count > 0 ? momentos[0].ToString("0.00") : "-") + " s (" + escupidor.graciaDelCharco + ")",
                   momentos.Count > 0 && momentos[0] >= escupidor.graciaDelCharco - 0.03f && momentos[0] < escupidor.graciaDelCharco + 0.1f);
                Ok("pega cada medio segundo: " + golpes + " golpes en 2,1 s (4 o 5)", golpes >= 4 && golpes <= 5);
                float sacado = vidaAntes - vida.health;
                float porGolpe = zombi.enemyType.daño * zombi.multiplicadorDano * escupidor.parteDelGolpe;
                Ok("cada golpe es la mitad del de un normal: le saco " + sacado.ToString("0") + " (" + golpes + " x " + porGolpe.ToString("0.00") + ")",
                   Mathf.Abs(sacado - golpes * porGolpe) < 1f);
                // Afuera del charco.
                Poner(control, cuerpo, control.transform.position + Vector3.right * 3f);
                golpesAntes = AcidoDelEscupidor.GolpesDados;
                fase = Fase.Afuera;
                desde = ahora;
                return;
            }

            case Fase.Afuera:
                if (ahora - desde < 1.2f) return;
                Ok("afuera del charco no pega", AcidoDelEscupidor.GolpesDados == golpesAntes);
                // El tope: cuatro charcos seguidos dejan tres.
                for (int i = 0; i < 4; i++)
                {
                    var charco = new AcidoDelEscupidor.Charco { radio = 1.1f, duracion = 4f, gracia = 0.25f, cadaCuanto = 0.5f, dano = 0f, color = escupidor.colorAcido };
                    Vector3 donde = new Vector3(-12f + i * 3f, 0f, -14f);
                    AcidoDelEscupidor.Lanzar(donde + Vector3.up, donde, 30f, 0.5f, charco, escupidor.materialAcido, escupidor.materialCharco, escupidor.materialAviso);
                }
                fase = Fase.Tope;
                desde = ahora;
                return;

            case Fase.Tope:
                if (ahora - desde < 0.6f) return;
                Ok("como mucho 3 charcos (" + AcidoDelEscupidor.CharcosEnElPiso + ")", AcidoDelEscupidor.CharcosEnElPiso == AcidoDelEscupidor.MaximoDeCharcos);
                // Que retroceda: el jugador a 3 m de el.
                Vector3 haciaElJugador = (control.transform.position - zombi.transform.position);
                haciaElJugador.y = 0f;
                Poner(control, cuerpo, zombi.transform.position + haciaElJugador.normalized * 3f);
                distanciaAntes = Distancia(zombi.transform.position, control.transform.position);
                fase = Fase.Retroceder;
                desde = ahora;
                return;

            case Fase.Retroceder:
            {
                if (ahora - desde < 1f) return;
                float d = Distancia(zombi.transform.position, control.transform.position);
                Vector3 mira = zombi.transform.forward;
                Vector3 alJugador = control.transform.position - zombi.transform.position;
                alJugador.y = 0f;
                Ok("si el jugador se le acerca, retrocede (de " + distanciaAntes.ToString("0.0") + " a " + d.ToString("0.0") + " m en 1 s)", d > distanciaAntes + 1f);
                Ok("retrocede mirando al jugador", Vector3.Angle(mira, alJugador) < 20f);
                fase = Fase.Bate;
                desde = ahora;
                return;
            }

            case Fase.Bate:
            {
                // Esperar un aviso con el jugador pegado, de frente, y batearlo.
                if (escupidor.EstadoActual != ZombiEscupidor.Estado.Avisando)
                {
                    if (ahora - desde > 8f) { Terminar("no aviso para probar el bate"); return; }
                    return;
                }
                Vector3 desdeElZombi = zombi.transform.forward;
                Vector3 donde = zombi.transform.position + desdeElZombi * 1.5f;
                Poner(control, cuerpo, donde);
                Vector3 alZombi = zombi.transform.position - donde;
                alZombi.y = 0f;
                cuerpo.rotation = Quaternion.LookRotation(alZombi);
                control.transform.rotation = cuerpo.rotation;
                escupitajosAntes = escupidor.Escupitajos;
                cortadosAntes = escupidor.Cortados;
                bool pego = Bate.Instancia != null && Bate.Instancia.Golpear();
                if (!pego) { Terminar("el bate no arranco (comprado: " + (Bate.Instancia != null && Bate.Instancia.Desbloqueado) + ")"); return; }
                fase = Fase.BateImpacto;
                desde = ahora;
                return;
            }

            case Fase.BateImpacto:
                if (ahora - desde < 0.9f) return;
                Ok("el bate le corta el escupitajo", escupidor.Cortados == cortadosAntes + 1 && escupidor.Escupitajos == escupitajosAntes);
                Ok("y lo empuja (es liviano)", Bate.Instancia.UltimosEmpujados == 1);
                // Congelado: el proximo intervalo.
                zombi.Congelar(0.4f, 30f);
                momentos.Clear();
                escupitajosAntes = escupidor.Escupitajos;
                Poner(control, cuerpo, new Vector3(0f, 0f, -6f));
                fase = Fase.Hielo;
                desde = ahora;
                return;

            case Fase.Hielo:
                if (escupidor.Escupitajos > escupitajosAntes + momentos.Count) momentos.Add(ahora);
                if (momentos.Count < 2)
                {
                    if (ahora - desde > 20f) { Terminar("congelado no escupio dos veces"); return; }
                    return;
                }
                float intervalo = momentos[1] - momentos[0];
                Ok("congelado tarda " + intervalo.ToString("0.0") + " s entre escupitajos (" + escupidor.cadaCuantoCongelado + ")", Mathf.Abs(intervalo - escupidor.cadaCuantoCongelado) < 0.3f);
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
        inf.AppendLine("Prueba del escupidor (WaveMode, camino del telefono, sin oleadas)");
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
