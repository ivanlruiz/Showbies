using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;

// Banco del blindado (ZombiBlindado, ZOMBIS.md), en play: WaveMode con las oleadas paradas, el camino del telefono sin
// auto-apuntado y un blindado que sale a 9 m al este del jugador, mirando al norte, con mucha vida. Mide que se de vuelta
// a 120 grados por segundo como mucho (50 congelado); que las balas de verdad que le entran de frente (la pistola, un
// perdigon de la escopeta, el rifle) peguen el 20 %, y la de energia, de costado y por la espalda, enteras; que con una
// perforante pase todo; que la granada, el bate, la quemadura y la explosiva atraviesen el escudo; que a 1,5 m abra la
// guardia, que con la guardia abierta pase todo, y que el zarpazo salga recien 0,5 s despues, pegando x1,4; que el bate
// lo frene, le arranque el escudo (que sale volando y se va) y lo deje como uno comun; y que al morir suelte el escudo.
// Saca tres fotos (Builds/blindado.png, blindado_guardia.png y blindado_roto.png) y escribe Builds/prueba_blindado.txt.
[InitializeOnLoad]
public static class PruebaBlindado
{
    const string Clave = "ShowBies.PruebaBlindado";
    const string Banco = "PruebaBlindado";
    const string Ruta = "../Builds/prueba_blindado.txt";
    const string Foto = "../Builds/blindado.png";
    const string FotoGuardia = "../Builds/blindado_guardia.png";
    const string FotoRoto = "../Builds/blindado_roto.png";
    const string Escena = "Assets/Escenas/WaveMode.unity";
    const double TopeTotal = 150.0;

    enum Fase { Preparar, Giro, Tiros, Tiro, Perforante, Origenes, GiroCongelado, Volver, Guardia, Cerrar, Bate, BateVuela, BateDespues, Muerte, Fin }

    // Un tiro de la lista: con que arma, de que angulo respecto del frente (0 de frente, 180 por la espalda) y que parte
    // del daño tiene que entrar.
    struct Tiro
    {
        public string nombre;
        public int arma;
        public float factor;
        public float angulo;
        public float entra;
    }

    static readonly Tiro[] Tiros =
    {
        new Tiro { nombre = "la pistola de frente", arma = Proyectiles.Pistola, factor = 1f, angulo = 0f, entra = 0.2f },
        new Tiro { nombre = "un perdigon de la escopeta de frente", arma = Proyectiles.Escopeta, factor = 1f / 3f, angulo = 0f, entra = 0.2f },
        new Tiro { nombre = "el rifle de frente", arma = Proyectiles.Rifle, factor = 1f, angulo = 0f, entra = 0.2f },
        new Tiro { nombre = "la pistola a 40 grados (adentro del arco)", arma = Proyectiles.Pistola, factor = 1f, angulo = 40f, entra = 0.2f },
        new Tiro { nombre = "la de energia de frente", arma = Proyectiles.Energia, factor = 1f, angulo = 0f, entra = 1f },
        new Tiro { nombre = "la pistola a 55 grados (afuera del arco)", arma = Proyectiles.Pistola, factor = 1f, angulo = 55f, entra = 1f },
        new Tiro { nombre = "la pistola por la espalda", arma = Proyectiles.Pistola, factor = 1f, angulo = 180f, entra = 1f },
    };

    static Fase fase;
    static float desde, abiertaEn, maxGiro, yawAntes, tiempoAntes, vidaAntes, danoDelTiro;
    static double inicio;
    static bool empezo, terminado, vioCerrada, fotoGuardia, fotoRoto;
    static string error;
    static readonly List<KeyValuePair<string, bool>> oks = new List<KeyValuePair<string, bool>>();
    static EnemyController zombi;
    static ZombiBlindado blindado;
    static BulletController bala;
    static int aparicion, cualTiro, bloqueadosAntes, zarpazosAntes;
    static float esperado;

    static PruebaBlindado()
    {
        EditorApplication.update += Tick;
    }

    [MenuItem("ShowBies/Pruebas/Blindado (play)")]
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
            fotoGuardia = false;
            fotoRoto = false;
            esperado = 0f;
            error = null;
            oks.Clear();
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

    static void Poner(PlayerController control, Rigidbody cuerpo, Vector3 donde)
    {
        donde.y = cuerpo.position.y;
        cuerpo.position = donde;
        cuerpo.linearVelocity = Vector3.zero;
        control.transform.position = donde;
    }

    static void Mirar(PlayerController control, Rigidbody cuerpo, Vector3 hacia)
    {
        hacia.y = 0f;
        if (hacia.sqrMagnitude < 1e-4f) return;
        cuerpo.rotation = Quaternion.LookRotation(hacia);
        control.transform.rotation = cuerpo.rotation;
    }

    static float Yaw(Transform t)
    {
        return t.eulerAngles.y;
    }

    // Lo que falta para mirar al jugador, en grados.
    static float LeFalta(PlayerController control)
    {
        Vector3 alJugador = control.transform.position - zombi.transform.position;
        alJugador.y = 0f;
        Vector3 frente = zombi.transform.forward;
        frente.y = 0f;
        return Vector3.Angle(frente, alJugador);
    }

    static void Congelar(bool si)
    {
        // Congelar con una duracion minima lo suelta en el paso siguiente.
        zombi.Congelar(1f, si ? 600f : 0.001f);
    }

    // Una bala de verdad del arma del jugador, a 3 m del blindado, desde "angulo" grados de su frente, apuntandole.
    static BulletController Disparar(GunController arma, int cual, float angulo, float factor)
    {
        Vector3 frente = zombi.transform.forward;
        frente.y = 0f;
        frente.Normalize();
        Vector3 desdeDonde = Quaternion.AngleAxis(angulo, Vector3.up) * frente;
        Vector3 centro = zombi.transform.position;
        Vector3 salida = centro + desdeDonde * 3f;
        salida.y = centro.y;
        return arma.SacarBala(salida, Quaternion.LookRotation(-desdeDonde), 0f, false, cual, factor, 0, 1f);
    }

    static void Avanzar()
    {
        var vida = PlayerHealth.instance;
        if (vida == null || EventSystem.current == null) return;
        var control = vida.GetComponent<PlayerController>();
        var cuerpo = vida.GetComponent<Rigidbody>();
        var arma = control.theGun;
        float ahora = Time.time;
        if (zombi != null && fase > Fase.Preparar && fase < Fase.Muerte && !EnemyController.SigueVivo(zombi, aparicion))
        {
            Terminar("el blindado murio en la fase " + fase);
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
                // Sin cartas: la perforante se prende a mano.
                PoderDeLaPartida.Empezar(new EstadoPoder { semilla = 7 });
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ConstructorZombis.RutaBlindado);
                if (prefab == null) { Terminar("no esta el prefab del blindado"); return; }
                Poner(control, cuerpo, new Vector3(0f, 0f, -6f));
                zombi = EnemyController.Aparecer(prefab, new Vector3(9f, 0f, -6f));
                if (zombi == null) { Terminar("no aparecio el blindado"); return; }
                zombi.multiplicadorVida = 1000f;
                aparicion = zombi.NumeroDeAparicion;
                blindado = zombi.GetComponent<ZombiBlindado>();
                if (blindado == null) { Terminar("el prefab no tiene ZombiBlindado"); return; }
                Ok("es pesado y lo lleva la persecucion de siempre (no se aparta, el chaman lo potencia y se refugia detras)",
                   zombi.EsPesado && !zombi.SeMuevePorSuCuenta);
                Ok("con el escudo a la vista, su charco ambar y la guardia cerrada",
                   blindado.EscudoALaVista && blindado.charco != null && blindado.charco.enabled && !blindado.Roto && !blindado.GuardiaAbierta);
                maxGiro = 0f;
                yawAntes = Yaw(zombi.transform);
                tiempoAntes = ahora;
                fase = Fase.Giro;
                desde = ahora;
                return;
            }

            case Fase.Giro:
            {
                // Sale mirando al norte y el jugador esta al oeste: se da vuelta a 120 grados por segundo como mucho.
                if (ahora - tiempoAntes >= 0.1f)
                {
                    float giro = Mathf.Abs(Mathf.DeltaAngle(yawAntes, Yaw(zombi.transform))) / (ahora - tiempoAntes);
                    maxGiro = Mathf.Max(maxGiro, giro);
                    yawAntes = Yaw(zombi.transform);
                    tiempoAntes = ahora;
                }
                if (LeFalta(control) > 3f)
                {
                    if (ahora - desde > 3f) { Terminar("no se dio vuelta hacia el jugador (le faltan " + LeFalta(control).ToString("0") + " grados)"); return; }
                    return;
                }
                Ok("se da vuelta a 120 grados por segundo como mucho (" + maxGiro.ToString("0") + ", en " + (ahora - desde).ToString("0.00") + " s)",
                   maxGiro <= 126f && maxGiro > 80f && ahora - desde > 0.6f);
                Congelar(true);
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(Foto)));
                ScreenCapture.CaptureScreenshot(Foto);
                cualTiro = 0;
                fase = Fase.Tiros;
                desde = ahora;
                return;
            }

            case Fase.Tiros:
            {
                if (ahora - desde < 0.2f) return;
                if (cualTiro >= Tiros.Length)
                {
                    fase = Fase.Perforante;
                    desde = ahora;
                    return;
                }
                var t = Tiros[cualTiro];
                vidaAntes = zombi.VidaActual;
                bloqueadosAntes = blindado.Bloqueados;
                bala = Disparar(arma, t.arma, t.angulo, t.factor);
                danoDelTiro = bala.danoAplicado;
                fase = Fase.Tiro;
                desde = ahora;
                return;
            }

            case Fase.Tiro:
            {
                var t = Tiros[cualTiro];
                if (bala.gameObject.activeSelf && Mathf.Approximately(zombi.VidaActual, vidaAntes))
                {
                    if (ahora - desde > 1.5f) { Terminar("la bala de " + t.nombre + " no le pego"); return; }
                    return;
                }
                float entro = vidaAntes - zombi.VidaActual;
                bool paro = blindado.Bloqueados > bloqueadosAntes;
                Ok(t.nombre + ": entra el " + Mathf.RoundToInt(t.entra * 100f) + " % (" + entro.ToString("0.00") + " de " + danoDelTiro.ToString("0.00") + ")"
                   + (paro ? ", la paro el escudo" : ""),
                   Mathf.Abs(entro - danoDelTiro * t.entra) < 0.01f && paro == (t.entra < 1f));
                cualTiro++;
                fase = Fase.Tiros;
                desde = ahora;
                return;
            }

            case Fase.Perforante:
            {
                // Con una perforante, toda bala atraviesa el escudo (de Argon).
                if (esperado == 0f)
                {
                    if (ahora - desde < 0.2f) return;
                    var estado = new EstadoPoder { semilla = 7 };
                    estado.cartas.Add(new CartaElegida { id = PoderDeLaPartida.Perforante, veces = 1 });
                    PoderDeLaPartida.Empezar(estado);
                    vidaAntes = zombi.VidaActual;
                    bloqueadosAntes = blindado.Bloqueados;
                    bala = Disparar(arma, Proyectiles.Pistola, 0f, 1f);
                    danoDelTiro = bala.danoAplicado;
                    esperado = 1f;
                    desde = ahora;
                    return;
                }
                if (Mathf.Approximately(zombi.VidaActual, vidaAntes))
                {
                    if (ahora - desde > 1.7f) { Terminar("la bala con la perforante no le pego"); return; }
                    return;
                }
                float entro = vidaAntes - zombi.VidaActual;
                Ok("con una perforante, la pistola de frente entra entera (" + entro.ToString("0.00") + " de " + danoDelTiro.ToString("0.00") + ")",
                   Mathf.Abs(entro - danoDelTiro) < 0.01f && blindado.Bloqueados == bloqueadosAntes);
                PoderDeLaPartida.Empezar(new EstadoPoder { semilla = 7 });
                esperado = 0f;
                fase = Fase.Origenes;
                desde = ahora;
                return;
            }

            case Fase.Origenes:
            {
                if (ahora - desde < 0.3f) return;
                // Lo que no es una bala comun atraviesa el escudo; el rumbo, de frente.
                Vector3 deFrente = -zombi.transform.forward;
                bool todos = true;
                var notas = new StringBuilder();
                foreach (var origen in new[] { OrigenDelDano.Granada, OrigenDelDano.Bate, OrigenDelDano.Quemadura, OrigenDelDano.Explosiva, OrigenDelDano.Otro })
                {
                    float antes = zombi.VidaActual;
                    zombi.DanoZombi(4f, false, deFrente, null, origen);
                    float entro = antes - zombi.VidaActual;
                    notas.Append(origen + " " + entro.ToString("0.0") + " ");
                    todos &= Mathf.Abs(entro - 4f) < 0.01f;
                }
                Ok("la granada, el bate, la quemadura y la explosiva atraviesan el escudo (" + notas.ToString().Trim() + ")", todos);
                float antesBala = zombi.VidaActual;
                zombi.DanoZombi(4f, false, deFrente, null, OrigenDelDano.Bala);
                Ok("y una bala comun por el mismo camino, el 20 % (" + (antesBala - zombi.VidaActual).ToString("0.00") + " de 4)",
                   Mathf.Abs(antesBala - zombi.VidaActual - 0.8f) < 0.01f);
                // Congelado se da vuelta a 50 grados por segundo: el jugador pasa al este.
                Poner(control, cuerpo, zombi.transform.position + new Vector3(8f, 0f, 0f));
                maxGiro = 0f;
                yawAntes = Yaw(zombi.transform);
                tiempoAntes = ahora;
                fase = Fase.GiroCongelado;
                desde = ahora;
                return;
            }

            case Fase.GiroCongelado:
            {
                if (ahora - tiempoAntes >= 0.1f)
                {
                    float giro = Mathf.Abs(Mathf.DeltaAngle(yawAntes, Yaw(zombi.transform))) / (ahora - tiempoAntes);
                    maxGiro = Mathf.Max(maxGiro, giro);
                    yawAntes = Yaw(zombi.transform);
                    tiempoAntes = ahora;
                }
                if (ahora - desde < 1.2f) return;
                Ok("congelado se da vuelta a 50 grados por segundo como mucho (" + maxGiro.ToString("0") + ")", maxGiro <= 54f && maxGiro > 30f);
                Ok("y le sigue faltando para mirar al jugador: se lo puede rodear (" + LeFalta(control).ToString("0") + " grados)", LeFalta(control) > 60f);
                // El jugador vuelve al oeste, a 5 m, y el blindado camina hacia el.
                Poner(control, cuerpo, zombi.transform.position + new Vector3(-5f, 0f, 0f));
                Mirar(control, cuerpo, zombi.transform.position - control.transform.position);
                Congelar(false);
                vioCerrada = false;
                abiertaEn = -1f;
                zarpazosAntes = EnemyController.ZarpazosEmpezados;
                fase = Fase.Guardia;
                desde = ahora;
                return;
            }

            case Fase.Guardia:
            {
                float d = Vector3.Distance(Vector3.Scale(zombi.transform.position, new Vector3(1, 0, 1)), Vector3.Scale(control.transform.position, new Vector3(1, 0, 1)));
                if (!blindado.GuardiaAbierta)
                {
                    if (d > 2f) vioCerrada = true;
                    if (EnemyController.ZarpazosEmpezados != zarpazosAntes) { Terminar("tiro un zarpazo con la guardia cerrada"); return; }
                    if (ahora - desde > 6f) { Terminar("no abrio la guardia (a " + d.ToString("0.0") + " m)"); return; }
                    return;
                }
                if (abiertaEn < 0f)
                {
                    abiertaEn = ahora;
                    Ok("lejos va con la guardia cerrada, y la abre a 1,5 m (a " + d.ToString("0.00") + " m)", vioCerrada && d <= 1.6f);
                    Ok("y recien abierta no zarpa", !zombi.puedeZarpar);
                    Ok("el zarpazo pega x1,4 (" + zombi.DanoPorGolpe.ToString("0.00") + ")",
                       Mathf.Abs(zombi.DanoPorGolpe - zombi.enemyType.daño * zombi.multiplicadorDano * 1.4f) < 1e-4f);
                    // Con la guardia abierta, el pecho esta descubierto: una bala de frente entra entera.
                    vidaAntes = zombi.VidaActual;
                    bloqueadosAntes = blindado.Bloqueados;
                    float antes = zombi.VidaActual;
                    zombi.DanoZombi(4f, false, -zombi.transform.forward, null, OrigenDelDano.Bala);
                    Ok("con la guardia abierta, una bala de frente entra entera (" + (antes - zombi.VidaActual).ToString("0.00") + " de 4)",
                       Mathf.Abs(antes - zombi.VidaActual - 4f) < 0.01f && blindado.Bloqueados == bloqueadosAntes);
                    return;
                }
                if (!fotoGuardia && ahora - abiertaEn > 0.25f)
                {
                    fotoGuardia = true;
                    ScreenCapture.CaptureScreenshot(FotoGuardia);
                }
                if (EnemyController.ZarpazosEmpezados == zarpazosAntes)
                {
                    if (ahora - abiertaEn > 2.5f) { Terminar("con la guardia abierta no tiro el zarpazo"); return; }
                    return;
                }
                Ok("el zarpazo sale 0,5 s despues de abrir la guardia (" + (ahora - abiertaEn).ToString("0.00") + " s)",
                   ahora - abiertaEn >= blindado.aviso - 0.03f && ahora - abiertaEn < blindado.aviso + 0.6f);
                // El jugador se aleja: cierra la guardia.
                Poner(control, cuerpo, zombi.transform.position + (control.transform.position - zombi.transform.position).normalized * 6f);
                Congelar(true);
                fase = Fase.Cerrar;
                desde = ahora;
                return;
            }

            case Fase.Cerrar:
                if (ahora - desde < 0.3f) return;
                Ok("lejos del jugador cierra la guardia", !blindado.GuardiaAbierta);
                if (LeFalta(control) > 3f)
                {
                    if (ahora - desde > 5f) { Terminar("no volvio a mirar al jugador"); return; }
                    return;
                }
                // El bate, a 1,5 m y de frente.
                {
                    Vector3 frente = zombi.transform.forward;
                    frente.y = 0f;
                    Poner(control, cuerpo, zombi.transform.position + frente.normalized * 1.6f);
                    Mirar(control, cuerpo, zombi.transform.position - control.transform.position);
                    bloqueadosAntes = blindado.Bloqueados;
                    if (Bate.Instancia == null || !Bate.Instancia.Golpear()) { Terminar("el bate no arranco"); return; }
                }
                fase = Fase.Bate;
                desde = ahora;
                return;

            case Fase.Bate:
                if (!blindado.Roto)
                {
                    if (ahora - desde > 1f) { Terminar("el bate no le rompio el escudo"); return; }
                    return;
                }
                Ok("el bate le arranca el escudo y lo frena", blindado.Roto && zombi.Frenado);
                Ok("y el escudo sale volando (a la vista), con el charco apagado en el acto", blindado.EscudoALaVista && !blindado.charco.enabled);
                fase = Fase.BateVuela;
                desde = ahora;
                return;

            case Fase.BateVuela:
                if (!fotoRoto && ahora - desde > 0.15f)
                {
                    fotoRoto = true;
                    ScreenCapture.CaptureScreenshot(FotoRoto);
                }
                if (ahora - desde < 0.9f) return;
                Ok("el escudo se va", !blindado.EscudoALaVista);
                Ok("sin escudo, gira derecho y puede zarpar como uno comun",
                   float.IsPositiveInfinity(blindado.GiroPorSegundo) && zombi.puedeZarpar);
                {
                    float antes = zombi.VidaActual;
                    zombi.DanoZombi(4f, false, -zombi.transform.forward, null, OrigenDelDano.Bala);
                    Ok("sin escudo, una bala de frente entra entera (" + (antes - zombi.VidaActual).ToString("0.00") + " de 4)",
                       Mathf.Abs(antes - zombi.VidaActual - 4f) < 0.01f && blindado.Bloqueados == bloqueadosAntes);
                }
                fase = Fase.Muerte;
                desde = ahora;
                return;

            case Fase.Muerte:
            {
                // Otro, con el escudo puesto, que muere: lo suelta.
                if (ahora - desde < 0.2f) return;
                if (zombi == null || EnemyController.SigueVivo(zombi, aparicion))
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ConstructorZombis.RutaBlindado);
                    EnemyController.DespejarAlrededor(Vector3.zero, 500f);
                    zombi = EnemyController.Aparecer(prefab, control.transform.position + new Vector3(5f, 0f, 0f));
                    if (zombi == null) { Terminar("no aparecio el segundo blindado"); return; }
                    aparicion = zombi.NumeroDeAparicion;
                    blindado = zombi.GetComponent<ZombiBlindado>();
                    Ok("el que sale del pool vuelve con el escudo y su charco", blindado.EscudoALaVista && blindado.charco.enabled && !blindado.Roto);
                    zombi.DanoZombi(1e9f);
                    desde = ahora;
                    return;
                }
                if (ahora - desde < 0.15f) return;
                Ok("al morir suelta el escudo (vuela)", blindado.Roto && blindado.EscudoALaVista);
                fase = Fase.Fin;
                desde = ahora;
                return;
            }

            case Fase.Fin:
                if (ahora - desde < 0.9f) return;
                Ok("y se va", !blindado.EscudoALaVista);
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
        inf.AppendLine("Prueba del blindado (WaveMode, camino del telefono, sin oleadas ni auto-apuntado)");
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
        inf.AppendLine("Fotos: " + Foto + ", " + FotoGuardia + ", " + FotoRoto);
        inf.AppendLine("RESULTADO: " + (todo ? "TODO OK" : "HAY FALLAS"));
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(Ruta)));
        File.WriteAllText(Ruta, inf.ToString());

        SessionState.SetBool(Clave, false);
        EditorApplication.ExitPlaymode();
        Debug.Log(inf.ToString());
    }
}
