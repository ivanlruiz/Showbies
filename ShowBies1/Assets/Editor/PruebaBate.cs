using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Banco del bate (Bate, 1.7.0), en play: WaveMode con el bate, la granada y la furia compradas, las
// oleadas paradas, el camino del telefono y sin auto-apuntado. El jugador mirando al norte y seis
// zombis puestos a mano y congelados del todo (el hielo al 100 %, asi no caminan y lo que se mueven
// es el empujon): un normal adelante, un veloz adelante a la derecha, un tanque adelante a la
// izquierda, uno chico pegado a la derecha que muere del golpe, uno detras y uno adelante lejos.
// Se toca el boton del HUD por el mismo camino que un dedo (el raycast del EventSystem, que tiene
// que caer en el boton) y se mide: que el daño entre cuando el bate baja y no al apretar, que pegue
// a los del cono con el daño de la bala por tres y no a los de afuera, que los livianos salgan
// despedidos unos tres metros y el tanque se quede quieto un segundo, que mientras dura se vea el
// bate y no se dispare, que vuelva la pistola, la recarga de cuatro segundos (el boton cuenta) y,
// otra vez listo, que al jefe le pegue sin moverlo. Mira tambien que el boton no pise la granada,
// la furia ni los joysticks. Saca una foto en el golpe (Builds/bate.png) y escribe
// Builds/prueba_bate.txt.
[InitializeOnLoad]
public static class PruebaBate
{
    const string Clave = "ShowBies.PruebaBate";
    const string Banco = "PruebaBate";
    const string Ruta = "../Builds/prueba_bate.txt";
    const string Foto = "../Builds/bate.png";
    const string Escena = "Assets/Escenas/WaveMode.unity";
    const double TopeTotal = 60.0;
    const float VidaPorDiez = 10f;

    enum Fase { Preparar, Asentar, Golpear, Impacto, Despues, Recarga, Jefe, JefeImpacto, Fin }

    class Puesto
    {
        public string nombre, prefab;
        public float distancia, angulo, multiplicadorVida = VidaPorDiez;
        public bool entra, muere, liviano, pesado;
        public EnemyController zombi;
        public int aparicion;
        public Vector3 antes;
        public float vidaAntes, vidaEnElGolpe, movido;
        public bool vivoDespues, frenadoEnElGolpe, empujadoEnElGolpe;
    }

    static readonly List<Puesto> puestos = new List<Puesto>
    {
        new Puesto { nombre = "normal adelante", prefab = "Zombi", distancia = 1.6f, angulo = 0f, entra = true, liviano = true },
        new Puesto { nombre = "veloz adelante a la derecha", prefab = "ZombiFASTER", distancia = 2.2f, angulo = 30f, entra = true, liviano = true },
        new Puesto { nombre = "tanque adelante a la izquierda", prefab = "ZombiTanque", distancia = 2.3f, angulo = -50f, entra = true, pesado = true },
        new Puesto { nombre = "chico pegado a la derecha (muere)", prefab = "Zombi", distancia = 1.3f, angulo = 50f, entra = true, muere = true, multiplicadorVida = 0.5f },
        new Puesto { nombre = "normal detras", prefab = "Zombi", distancia = 1.7f, angulo = 180f },
        new Puesto { nombre = "normal adelante lejos", prefab = "Zombi", distancia = 4.0f, angulo = -20f },
    };

    static Fase fase;
    static float desde, apretadoEn, impactoEn;
    static double inicio;
    static bool empezo, terminado, fotoSacada;
    static string error;
    static readonly List<KeyValuePair<string, bool>> oks = new List<KeyValuePair<string, bool>>();
    static readonly StringBuilder notas = new StringBuilder();
    static int tirosAntes;
    static bool disparoDuranteElGolpe, bateVistoDuranteElGolpe, estadoBatazoVisto, pistolaEscondida;
    static EnemyController jefe;
    static float vidaDelJefeAntes;
    static Vector3 jefeAntes;

    static PruebaBate()
    {
        EditorApplication.update += Tick;
    }

    [MenuItem("ShowBies/Pruebas/Bate (play)")]
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
        Progreso.DepurarFijarNivel(c.granada.id, 1);
        Progreso.DepurarFijarNivel(c.furia.id, 1);
        Progreso.DepurarFijarNivel(c.danoBala.id, 0);
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
            disparoDuranteElGolpe = bateVistoDuranteElGolpe = estadoBatazoVisto = pistolaEscondida = false;
            jefe = null;
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

    static void Ok(string que, bool bien)
    {
        oks.Add(new KeyValuePair<string, bool>(que, bien));
    }

    static void Avanzar()
    {
        var vida = PlayerHealth.instance;
        if (vida == null || EventSystem.current == null) return;
        vida.health = vida.maxHealth;
        var control = vida.GetComponent<PlayerController>();
        var cuerpo = vida.GetComponent<Rigidbody>();
        var bate = Bate.Instancia;
        var arma = vida.GetComponent<ArmaEnLaMano>();

        switch (fase)
        {
            case Fase.Preparar:
            {
                if (Time.time - desde < 1f) return;
                var oleadas = Object.FindAnyObjectByType<WaveManager>();
                if (oleadas != null) { oleadas.StopAllCoroutines(); oleadas.enabled = false; }
                foreach (var cajas in Object.FindObjectsByType<PowerUp>(FindObjectsSortMode.None)) cajas.enabled = false;
                EnemyController.DespejarAlrededor(Vector3.zero, 500f);

                if (!Ok2("el jugador tiene el bate, comprado", bate != null && bate.Desbloqueado && bate.jugador == control)) { Terminar("sin bate"); return; }
                var boton = BotonBate.Puesto;
                if (!Ok2("el boton del bate esta en el HUD, prendido", boton != null && boton.gameObject.activeInHierarchy)) { Terminar("sin boton"); return; }
                MirarElHud(boton);

                // El jugador al sur del centro, mirando al norte.
                Vector3 centro = new Vector3(0f, cuerpo.position.y, -4f);
                cuerpo.position = centro;
                cuerpo.linearVelocity = Vector3.zero;
                cuerpo.rotation = Quaternion.identity;
                control.transform.SetPositionAndRotation(centro, Quaternion.identity);
                foreach (var p in puestos)
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Personajes/" + p.prefab + ".prefab");
                    if (prefab == null) { Terminar("no esta el prefab " + p.prefab); return; }
                    float a = p.angulo * Mathf.Deg2Rad;
                    Vector3 donde = new Vector3(centro.x + Mathf.Sin(a) * p.distancia, 0f, centro.z + Mathf.Cos(a) * p.distancia);
                    p.zombi = EnemyController.Aparecer(prefab, donde);
                    if (p.zombi == null) { Terminar("no aparecio el " + p.nombre); return; }
                    p.zombi.multiplicadorVida = p.multiplicadorVida;
                    p.aparicion = p.zombi.NumeroDeAparicion;
                    p.zombi.Congelar(1f, 30f);
                }
                fase = Fase.Asentar;
                desde = Time.time;
                return;
            }

            case Fase.Asentar:
                cuerpo.rotation = Quaternion.identity;
                control.transform.rotation = Quaternion.identity;
                if (Time.time - desde < 0.4f) return;
                foreach (var p in puestos)
                {
                    p.antes = p.zombi.transform.position;
                    p.vidaAntes = p.zombi.VidaActual;
                    Vector3 hacia = p.antes - control.transform.position;
                    hacia.y = 0f;
                    notas.AppendLine(p.nombre + ": a " + hacia.magnitude.ToString("0.00") + " m, " + Vector3.SignedAngle(Vector3.forward, hacia, Vector3.up).ToString("0")
                                     + " grados, vida " + p.vidaAntes.ToString("0.0") + ", radio " + p.zombi.RadioDeSuCuerpo.ToString("0.00"));
                }
                tirosAntes = control.theGun.TirosDisparados;
                {
                    // Un dedo sobre el centro del boton: el raycast del EventSystem tiene que caer en el.
                    var boton = BotonBate.Puesto;
                    Vector2 pantalla = RectTransformUtility.WorldToScreenPoint(null, boton.transform.position);
                    var evento = new PointerEventData(EventSystem.current) { position = pantalla };
                    var resultados = new List<RaycastResult>();
                    EventSystem.current.RaycastAll(evento, resultados);
                    GameObject primero = resultados.Count > 0 ? resultados[0].gameObject : null;
                    Ok("un toque en el centro del boton cae en el boton (" + (primero != null ? primero.name : "nada") + ")",
                       primero != null && primero.GetComponentInParent<BotonBate>() == boton);
                    Ok("el joystick dinamico no se lleva ese toque", JoystickDinamico.AtiendeAlgo(pantalla));
                    if (primero != null) ExecuteEvents.ExecuteHierarchy(primero, evento, ExecuteEvents.pointerDownHandler);
                }
                apretadoEn = Time.time;
                Ok("al tocarlo arranca el golpe", bate.Batazos == 1 && bate.EnCurso);
                Ok("enseguida no pega todavia (el daño espera al bate)", bate.Impactos == 0);
                fase = Fase.Golpear;
                return;

            case Fase.Golpear:
            {
                // Querer disparar mientras dura: el embudo de las dos plataformas lo corta.
                if (bate.EnCurso)
                {
                    control.FijarDisparo(true);
                    if (control.theGun.isFiring) disparoDuranteElGolpe = true;
                    control.FijarDisparo(false);
                }
                if (arma != null && arma.BateALaVista) bateVistoDuranteElGolpe = true;
                if (arma != null && arma.Boca != null)
                {
                    var r = arma.Boca.parent.GetComponentInChildren<Renderer>();
                    if (r != null && !r.enabled) pistolaEscondida = true;
                }
                var anim = control.trans != null ? control.trans.anim : null;
                int capa = anim != null ? anim.GetLayerIndex(Bate.CapaDelBate) : -1;
                if (capa >= 0 && (anim.GetCurrentAnimatorStateInfo(capa).IsName("Batazo") || anim.GetNextAnimatorStateInfo(capa).IsName("Batazo")))
                    estadoBatazoVisto = true;
                if (bate.Impactos == 0)
                {
                    if (Time.time - apretadoEn > 1f) { Terminar("el golpe no conecto nunca"); return; }
                    return;
                }
                impactoEn = Time.time;
                foreach (var p in puestos)
                {
                    p.vidaEnElGolpe = p.zombi.Vivo && p.zombi.NumeroDeAparicion == p.aparicion ? p.zombi.VidaActual : 0f;
                    p.empujadoEnElGolpe = p.zombi.Vivo && p.zombi.Empujado;
                    p.frenadoEnElGolpe = p.zombi.Vivo && p.zombi.Frenado;
                }
                Ok("el arco del golpe se ve en el piso", bate.ArcoPrendido);
                if (!fotoSacada)
                {
                    fotoSacada = true;
                    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(Foto)));
                    ScreenCapture.CaptureScreenshot(Foto);
                }
                fase = Fase.Impacto;
                return;
            }

            case Fase.Impacto:
            {
                float demora = impactoEn - apretadoEn;
                Ok("el daño entra cuando baja el bate, " + demora.ToString("0.000") + " s despues de apretar (" + Bate.DemoraDelImpacto.ToString("0.000") + ")",
                   demora >= Bate.DemoraDelImpacto - 0.03f && demora <= Bate.DemoraDelImpacto + 0.06f);
                Ok("mientras dura se ve el bate en la mano", bateVistoDuranteElGolpe);
                Ok("y la pistola no", pistolaEscondida);
                Ok("el torso tira el batazo (capa " + Bate.CapaDelBate + ")", estadoBatazoVisto);
                Ok("mientras dura no se dispara", !disparoDuranteElGolpe && control.theGun.TirosDisparados == tirosAntes);
                float dano = Bate.Instancia.multiplicadorDano * control.theGun.DanoSinFuria;
                Ok("le pego a " + bate.UltimosTocados + " (los cuatro del cono)", bate.UltimosTocados == 4);
                foreach (var p in puestos)
                {
                    float sacado = p.vidaAntes - p.vidaEnElGolpe;
                    if (p.muere) Ok(p.nombre + ": muere", !EnemyController.SigueVivo(p.zombi, p.aparicion));
                    else if (p.entra) Ok(p.nombre + ": le saca el daño de la bala por tres (" + sacado.ToString("0.00") + " de " + dano.ToString("0.00") + ")", Mathf.Abs(sacado - dano) < 0.01f);
                    else Ok(p.nombre + ": no lo toca (" + sacado.ToString("0.00") + ")", sacado < 0.001f);
                    if (p.liviano) Ok(p.nombre + ": sale despedido", p.empujadoEnElGolpe);
                    if (p.pesado) Ok(p.nombre + ": se frena, sin empujon", p.frenadoEnElGolpe && !p.empujadoEnElGolpe);
                }
                fase = Fase.Despues;
                desde = Time.time;
                return;
            }

            case Fase.Despues:
            {
                if (Time.time - apretadoEn < Bate.DuracionDelGolpe + 0.25f) return;
                Ok("vuelve la pistola", arma == null || !arma.BateALaVista);
                var r = arma != null && arma.Boca != null ? arma.Boca.parent.GetComponentInChildren<Renderer>() : null;
                Ok("con sus renderers prendidos", r == null || r.enabled);
                control.FijarDisparo(true);
                Ok("y se puede disparar de nuevo", control.theGun.isFiring);
                control.FijarDisparo(false);
                Ok("recien pego no se puede otra vez", !bate.Golpear() && bate.Batazos == 1);
                var boton = BotonBate.Puesto;
                Ok("el boton cuenta la recarga", boton.segundos != null && boton.segundos.gameObject.activeSelf);
                foreach (var p in puestos)
                {
                    if (p.muere) continue;
                    Vector3 d = p.zombi.transform.position - p.antes;
                    d.y = 0f;
                    p.movido = d.magnitude;
                    if (p.liviano)
                    {
                        Vector3 afuera = p.antes - control.transform.position;
                        afuera.y = 0f;
                        float alejado = Vector3.Dot(d, afuera.normalized);
                        Ok(p.nombre + ": se alejo unos tres metros (" + alejado.ToString("0.00") + ")", alejado > 2.3f && alejado < 3.4f);
                    }
                    else if (p.pesado) Ok(p.nombre + ": no se movio (" + p.movido.ToString("0.00") + " m)", p.movido < 0.2f);
                    else Ok(p.nombre + ": no se movio (" + p.movido.ToString("0.00") + " m)", p.movido < 0.1f);
                }
                Ok("la cuenta de vivos coincide con la lista", EnemyController.ZombisVivos == EnemyController.Vivos.Count);
                fase = Fase.Recarga;
                desde = Time.time;
                return;
            }

            case Fase.Recarga:
            {
                var tanque = puestos[2];
                if (Time.time - impactoEn > 1.2f && tanque.zombi.Frenado) { Ok("el tanque se suelta al segundo", false); }
                if (bate.RestanteEnfriamiento > 0f)
                {
                    if (Time.time - apretadoEn > bate.enfriamiento + 0.5f) { Terminar("no se recargo"); return; }
                    return;
                }
                float recarga = Time.time - apretadoEn;
                Ok("se recarga en " + recarga.ToString("0.00") + " s (" + bate.enfriamiento + ")", Mathf.Abs(recarga - bate.enfriamiento) < 0.1f);
                Ok("el tanque ya no esta frenado", !tanque.zombi.Frenado);
                Ok("el boton deja de contar", !BotonBate.Puesto.segundos.gameObject.activeSelf);
                // El jefe, adelante.
                foreach (var p in puestos)
                    if (EnemyController.SigueVivo(p.zombi, p.aparicion)) p.zombi.Retirar();
                var prefabJefe = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Personajes/ZombiBOSS.prefab");
                if (prefabJefe == null) { Terminar("no esta el jefe"); return; }
                cuerpo.rotation = Quaternion.identity;
                control.transform.rotation = Quaternion.identity;
                jefe = EnemyController.Aparecer(prefabJefe, control.transform.position + new Vector3(0f, 0f, 2.6f));
                jefe.EsJefe = true;
                jefe.multiplicadorVida = 1f;
                fase = Fase.Jefe;
                desde = Time.time;
                return;
            }

            case Fase.Jefe:
                cuerpo.rotation = Quaternion.identity;
                control.transform.rotation = Quaternion.identity;
                if (Time.time - desde < 0.15f) return;
                vidaDelJefeAntes = jefe.VidaActual;
                jefeAntes = jefe.transform.position;
                Ok("otra vez listo, pega", bate.Golpear());
                fase = Fase.JefeImpacto;
                desde = Time.time;
                return;

            case Fase.JefeImpacto:
                if (bate.Impactos < 2)
                {
                    if (Time.time - desde > 1f) { Terminar("el segundo golpe no conecto"); return; }
                    return;
                }
                Ok("al jefe le pega (" + (vidaDelJefeAntes - jefe.VidaActual).ToString("0.00") + ")", bate.UltimosTocados == 1 && jefe.VidaActual < vidaDelJefeAntes);
                Ok("pero no lo empuja ni lo frena", bate.UltimosEmpujados == 0 && bate.UltimosFrenados == 0 && !jefe.Empujado && !jefe.Frenado);
                if (jefe.Vivo) jefe.Retirar();
                Terminar(null);
                return;
        }
    }

    static bool Ok2(string que, bool bien)
    {
        Ok(que, bien);
        return bien;
    }

    // Que el boton no pise lo que esta en el rincon del pulgar que dispara.
    static void MirarElHud(BotonBate boton)
    {
        Rect suyo = EnPantalla((RectTransform)boton.transform);
        notas.AppendLine("boton del bate en pantalla: " + suyo + " (pantalla " + Screen.width + " x " + Screen.height + ")");
        var granada = Object.FindAnyObjectByType<JoystickGranada>();
        var furia = Object.FindAnyObjectByType<BotonFuria>();
        var js = Object.FindAnyObjectByType<PlayerJS>();
        var otros = new List<KeyValuePair<string, RectTransform>>();
        if (granada != null) otros.Add(new KeyValuePair<string, RectTransform>("la granada", (RectTransform)granada.transform));
        if (furia != null && furia.boton != null) otros.Add(new KeyValuePair<string, RectTransform>("la furia", (RectTransform)furia.boton.transform));
        if (js != null && js.lookJoystick != null) otros.Add(new KeyValuePair<string, RectTransform>("el joystick de disparo en su rincon", (RectTransform)js.lookJoystick.transform));
        if (js != null && js.moveJoystick != null) otros.Add(new KeyValuePair<string, RectTransform>("el joystick de mover en su rincon", (RectTransform)js.moveJoystick.transform));
        foreach (var otro in otros)
        {
            Rect r = EnPantalla(otro.Value);
            Ok("el boton no pisa " + otro.Key + " (" + r + ")", !suyo.Overlaps(r));
        }
        Ok("el boton entra en la pantalla", suyo.xMin >= 0f && suyo.yMin >= 0f && suyo.xMax <= Screen.width && suyo.yMax <= Screen.height);
        Ok("es mas chico que el de la granada", granada == null || suyo.width < EnPantalla((RectTransform)granada.transform).width);
    }

    static Rect EnPantalla(RectTransform rect)
    {
        var esquinas = new Vector3[4];
        rect.GetWorldCorners(esquinas);
        return Rect.MinMaxRect(esquinas[0].x, esquinas[0].y, esquinas[2].x, esquinas[2].y);
    }

    static void Terminar(string motivo)
    {
        if (terminado) return;
        terminado = true;
        if (error == null) error = motivo;

        var inf = new StringBuilder();
        inf.AppendLine("Prueba del bate (WaveMode, camino del telefono, sin oleadas)");
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
