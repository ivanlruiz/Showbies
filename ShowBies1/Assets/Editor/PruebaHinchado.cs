using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;

// Banco del hinchado (ZombiHinchado, ZOMBIS.md), en play: WaveMode con las oleadas paradas, el camino del telefono sin
// auto-apuntado. Mide que camine hasta 1,6 m del jugador sin tirar zarpazos, se clave, se infle 0,75 s y reviente
// pegandole 2 golpes, muriendo con sus puntos y sin cadaver; que con la vida en cero se infle en vez de morir (y no
// reciba nada mas), congelado 1,1 s; que la explosion le saque el 35 % de la vida maxima a los de adentro (al jefe el 4 %,
// al blindado tambien, sin el escudo), empuje a los livianos 2,5 m y a los pesados la mitad y los frene, mate a los que
// tenia flojos (con sus puntos) y no toque a los de afuera ni al jugador lejos; la cadena entre dos hinchados; que el
// bate patee 4 m al inflado sin cortarle la cuenta, y empuje 3 m al que camina. Saca una foto del aviso
// (Builds/hinchado_aviso.png) y escribe Builds/prueba_hinchado.txt.
[InitializeOnLoad]
public static class PruebaHinchado
{
    const string Clave = "ShowBies.PruebaHinchado";
    const string Banco = "PruebaHinchado";
    const string Ruta = "../Builds/prueba_hinchado.txt";
    const string Foto = "../Builds/hinchado_aviso.png";
    const string Escena = "Assets/Escenas/WaveMode.unity";
    const string RutaNormal = "Assets/Prefabs/Personajes/Zombi.prefab";
    const string RutaTanque = "Assets/Prefabs/Personajes/ZombiTanque.prefab";
    const string RutaJefe = "Assets/Prefabs/Personajes/ZombiBOSS.prefab";
    const double TopeTotal = 160.0;

    enum Fase { Preparar, Clavarse, Reventar, Controlado, ControladoReventar, ControladoEmpujes, Cadena, CadenaSegundo, Bate, BateImpacto, BateReventar, BateCaminando, BateCaminandoImpacto, Fin }

    static Fase fase;
    static float desde, inflandoEn, vidaDelJugador;
    static double inicio;
    static bool empezo, terminado, foto;
    static string error;
    static readonly List<KeyValuePair<string, bool>> oks = new List<KeyValuePair<string, bool>>();
    static EnemyController zombi, otro, n1, n2, tanque, blindado, jefe, afuera;
    static ZombiHinchado hinchado, segundo;
    static int aparicion, puntosAntes, zarpazosAntes;
    static readonly Dictionary<EnemyController, float> vidas = new Dictionary<EnemyController, float>();
    static readonly Dictionary<EnemyController, Vector3> lugares = new Dictionary<EnemyController, Vector3>();
    static Vector3 lugarDelHinchado, rumboDelBate;

    static PruebaHinchado()
    {
        EditorApplication.update += Tick;
    }

    [MenuItem("ShowBies/Pruebas/Hinchado (play)")]
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
            foto = false;
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

    static float EnElPiso(Vector3 a, Vector3 b)
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

    static EnemyController Sacar(string ruta, Vector3 donde, float multiplicadorVida, bool quieto)
    {
        var molde = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
        var z = EnemyController.Aparecer(molde, donde);
        if (z == null) throw new System.Exception("no aparecio " + ruta);
        z.multiplicadorVida = multiplicadorVida;
        if (quieto) z.Congelar(1f, 600f);
        return z;
    }

    static void Anotar(EnemyController z)
    {
        vidas[z] = z.VidaActual;
        lugares[z] = z.transform.position;
    }

    static float Perdio(EnemyController z)
    {
        return vidas[z] - z.VidaActual;
    }

    static float SeMovio(EnemyController z)
    {
        return EnElPiso(lugares[z], z.transform.position);
    }

    static void Avanzar()
    {
        var vida = PlayerHealth.instance;
        if (vida == null || EventSystem.current == null) return;
        var control = vida.GetComponent<PlayerController>();
        var cuerpo = vida.GetComponent<Rigidbody>();
        float ahora = Time.time;
        // La vida del jugador se llena en cada cuadro, salvo mientras se mide lo que le saca la explosion.
        if (fase != Fase.Reventar) vida.health = vida.maxHealth;

        switch (fase)
        {
            case Fase.Preparar:
            {
                if (ahora - desde < 1f) return;
                var oleadas = Object.FindAnyObjectByType<WaveManager>();
                if (oleadas != null) { oleadas.StopAllCoroutines(); oleadas.enabled = false; }
                foreach (var cajas in Object.FindObjectsByType<PowerUp>(FindObjectsSortMode.None)) cajas.enabled = false;
                EnemyController.DespejarAlrededor(Vector3.zero, 500f);
                PoderDeLaPartida.Empezar(new EstadoPoder { semilla = 7 });
                Poner(control, cuerpo, new Vector3(0f, 0f, -6f));
                zombi = Sacar(ConstructorZombis.RutaHinchado, new Vector3(8f, 0f, -6f), 1000f, false);
                aparicion = zombi.NumeroDeAparicion;
                hinchado = zombi.GetComponent<ZombiHinchado>();
                if (hinchado == null) { Terminar("el prefab no tiene ZombiHinchado"); return; }
                Ok("liviano y se mueve por su cuenta (el bate lo empuja, el chaman no lo potencia)",
                   !zombi.EsPesado && zombi.SeMuevePorSuCuenta && zombi.ValeTirarle);
                zarpazosAntes = EnemyController.ZarpazosEmpezados;
                fase = Fase.Clavarse;
                desde = ahora;
                return;
            }

            case Fase.Clavarse:
            {
                if (!hinchado.Inflando)
                {
                    if (ahora - desde > 6f) { Terminar("no se clavo nunca (a " + EnElPiso(zombi.transform.position, control.transform.position).ToString("0.0") + " m)"); return; }
                    return;
                }
                float d = EnElPiso(zombi.transform.position, control.transform.position);
                Ok("camina hasta el jugador y se clava a 1,6 m (a " + d.ToString("0.00") + " m)", d <= 1.75f && d > 0.8f);
                Ok("sin tirar zarpazos", EnemyController.ZarpazosEmpezados == zarpazosAntes);
                Ok("con el aviso en el piso, y el auto-apuntado ya no le tira", hinchado.AvisoPrendido && !zombi.ValeTirarle);
                inflandoEn = ahora;
                vidaDelJugador = vida.health;
                puntosAntes = Puntaje.instance.contadorKill;
                fase = Fase.Reventar;
                return;
            }

            case Fase.Reventar:
            {
                if (hinchado.Reventones == 0)
                {
                    if (ahora - inflandoEn > 2f) { Terminar("no revento"); return; }
                    return;
                }
                float esperado = zombi.enemyType.daño * zombi.multiplicadorDano * hinchado.golpeAlJugador;
                Ok("revienta a los 0,75 s (" + (ahora - inflandoEn).ToString("0.00") + ")", Mathf.Abs(ahora - inflandoEn - hinchado.aviso) < 0.1f);
                Ok("y al jugador adentro le pega 2 golpes (" + (vidaDelJugador - vida.health).ToString("0.00") + " de " + esperado.ToString("0.00") + ")",
                   Mathf.Abs(vidaDelJugador - vida.health - esperado) < 0.01f && hinchado.UltimoLePegoAlJugador);
                Ok("muere con sus puntos (+" + (Puntaje.instance.contadorKill - puntosAntes) + ") y sin cadaver",
                   !EnemyController.SigueVivo(zombi, aparicion) && Puntaje.instance.contadorKill - puntosAntes == zombi.enemyType.puntos
                   && !zombi.gameObject.activeSelf);
                fase = Fase.Controlado;
                desde = ahora;
                return;
            }

            case Fase.Controlado:
            {
                if (ahora - desde < 0.5f) return;
                EnemyController.DespejarAlrededor(Vector3.zero, 500f);
                Poner(control, cuerpo, new Vector3(0f, 0f, -6f));
                Vector3 h = new Vector3(6f, 0f, -6f);
                zombi = Sacar(ConstructorZombis.RutaHinchado, h, 1000f, true);
                hinchado = zombi.GetComponent<ZombiHinchado>();
                n1 = Sacar(RutaNormal, h + new Vector3(1.3f, 0f, 0.3f), 100f, true);
                n2 = Sacar(RutaNormal, h + new Vector3(-0.5f, 0f, 1.4f), 1f, true);
                n2.DanoZombi(4f);
                tanque = Sacar(RutaTanque, h + new Vector3(-1.7f, 0f, -0.5f), 100f, true);
                blindado = Sacar(ConstructorZombis.RutaBlindado, h + new Vector3(0.3f, 0f, -1.9f), 100f, true);
                jefe = Sacar(RutaJefe, h + new Vector3(2f, 0f, 1.4f), 1f, true);
                var patrones = jefe.GetComponent<JefePatrones>();
                if (patrones != null) patrones.enabled = false;
                jefe.EsJefe = true;
                afuera = Sacar(RutaNormal, h + new Vector3(-2.5f, 0f, 3.2f), 100f, true);
                foreach (var z in new[] { n1, n2, tanque, blindado, jefe, afuera }) Anotar(z);
                puntosAntes = Puntaje.instance.contadorKill;
                vidaDelJugador = vida.health;
                // Con la vida en cero no muere: se infla. Congelado, cuenta 1,1 s.
                zombi.DanoZombi(1e6f);
                Ok("con la vida en cero no muere: se infla", hinchado.Inflando && zombi.Vivo && zombi.VidaActual <= 0f);
                zombi.DanoZombi(5f);
                Ok("y mientras se infla no le entra nada mas", hinchado.Inflando && zombi.Vivo);
                Ok("congelado, la cuenta es de 1,1 s (" + hinchado.CuentaDeEsteAviso + ")", Mathf.Abs(hinchado.CuentaDeEsteAviso - hinchado.avisoCongelado) < 1e-4f);
                inflandoEn = ahora;
                fase = Fase.ControladoReventar;
                return;
            }

            case Fase.ControladoReventar:
            {
                if (!foto && ahora - inflandoEn > 0.55f)
                {
                    foto = true;
                    Ok("el disco del aviso se va llenando (" + hinchado.RadioDelDiscoAhora.ToString("0.0") + " m de " + hinchado.radio + ")",
                       hinchado.RadioDelDiscoAhora > 0.8f && hinchado.RadioDelDiscoAhora < hinchado.radio);
                    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(Foto)));
                    ScreenCapture.CaptureScreenshot(Foto);
                }
                if (hinchado.Reventones == 0)
                {
                    if (ahora - inflandoEn > 2.5f) { Terminar("el inflado no revento"); return; }
                    return;
                }
                Ok("revienta a los 1,1 s (" + (ahora - inflandoEn).ToString("0.00") + ")", Mathf.Abs(ahora - inflandoEn - hinchado.avisoCongelado) < 0.1f);
                float parte = hinchado.parteDeLaVida;
                Ok("al normal le saca el 35 % de su vida maxima (" + Perdio(n1).ToString("0.0") + " de " + n1.VidaMaxima.ToString("0.0") + ")",
                   Mathf.Abs(Perdio(n1) - n1.VidaMaxima * parte) < 0.01f);
                Ok("al tanque tambien (" + Perdio(tanque).ToString("0.0") + ")", Mathf.Abs(Perdio(tanque) - tanque.VidaMaxima * parte) < 0.01f);
                Ok("y al blindado, sin el escudo (" + Perdio(blindado).ToString("0.0") + ")",
                   Mathf.Abs(Perdio(blindado) - blindado.VidaMaxima * parte) < 0.01f && blindado.Blindado.Bloqueados == 0);
                Ok("al jefe, el 4 % (" + Perdio(jefe).ToString("0.0") + " de " + jefe.VidaMaxima.ToString("0") + ")",
                   Mathf.Abs(Perdio(jefe) - jefe.VidaMaxima * hinchado.parteDelJefe) < 0.01f);
                Ok("al de afuera y al jugador lejos, nada", Perdio(afuera) < 1e-4f && Mathf.Abs(vida.health - vidaDelJugador) < 1e-4f && !hinchado.UltimoLePegoAlJugador);
                Ok("mata al que tenia flojo, y paga sus puntos y los suyos (+" + (Puntaje.instance.contadorKill - puntosAntes) + ")",
                   !n2.Vivo && hinchado.UltimosMuertos >= 1
                   && Puntaje.instance.contadorKill - puntosAntes == zombi.enemyType.puntos + n2.enemyType.puntos);
                desde = ahora;
                fase = Fase.ControladoEmpujes;
                return;
            }

            case Fase.ControladoEmpujes:
            {
                if (ahora - desde < 0.45f) return;
                Ok("al liviano lo empuja 2,5 m (" + SeMovio(n1).ToString("0.00") + ")", SeMovio(n1) > 1.9f && SeMovio(n1) < 3.1f);
                Ok("al tanque, la mitad, y lo frena (" + SeMovio(tanque).ToString("0.00") + ")", SeMovio(tanque) > 0.7f && SeMovio(tanque) < 1.6f && tanque.Frenado);
                Ok("al jefe no lo mueve (" + SeMovio(jefe).ToString("0.00") + ")", SeMovio(jefe) < 0.2f);
                Ok("al de afuera tampoco", SeMovio(afuera) < 0.05f);
                // La cadena: dos hinchados pegados; el segundo, con menos del 35 % de su vida.
                EnemyController.DespejarAlrededor(Vector3.zero, 500f);
                zombi = Sacar(ConstructorZombis.RutaHinchado, new Vector3(5f, 0f, -6f), 1f, true);
                hinchado = zombi.GetComponent<ZombiHinchado>();
                otro = Sacar(ConstructorZombis.RutaHinchado, new Vector3(6.4f, 0f, -6f), 1f, true);
                segundo = otro.GetComponent<ZombiHinchado>();
                otro.DanoZombi(otro.VidaMaxima - 3f);
                Ok("con vida le queda, no se infla", !segundo.Inflando && otro.Vivo);
                zombi.DanoZombi(1e6f);
                inflandoEn = ahora;
                fase = Fase.Cadena;
                return;
            }

            case Fase.Cadena:
                if (hinchado.Reventones == 0)
                {
                    if (ahora - inflandoEn > 2.5f) { Terminar("el primero de la cadena no revento"); return; }
                    return;
                }
                Ok("en cadena: la explosion deja al segundo en cero y se infla", segundo.Inflando && otro.Vivo);
                inflandoEn = ahora;
                fase = Fase.CadenaSegundo;
                return;

            case Fase.CadenaSegundo:
                if (segundo.Reventones == 0)
                {
                    if (ahora - inflandoEn > 2.5f) { Terminar("el segundo de la cadena no revento"); return; }
                    return;
                }
                Ok("y revienta despues, con su cuenta (" + (ahora - inflandoEn).ToString("0.00") + " s)", ahora - inflandoEn > 0.6f && !otro.Vivo);
                fase = Fase.Bate;
                desde = ahora;
                return;

            case Fase.Bate:
            {
                if (ahora - desde < 0.3f) return;
                EnemyController.DespejarAlrededor(Vector3.zero, 500f);
                Poner(control, cuerpo, new Vector3(0f, 0f, -6f));
                rumboDelBate = Vector3.right;
                cuerpo.rotation = Quaternion.LookRotation(rumboDelBate);
                control.transform.rotation = cuerpo.rotation;
                zombi = Sacar(ConstructorZombis.RutaHinchado, new Vector3(1.4f, 0f, -6f), 1000f, true);
                hinchado = zombi.GetComponent<ZombiHinchado>();
                zombi.DanoZombi(1e6f);
                inflandoEn = ahora;
                lugarDelHinchado = zombi.transform.position;
                if (Bate.Instancia == null || !Bate.Instancia.Golpear()) { Terminar("el bate no arranco"); return; }
                fase = Fase.BateImpacto;
                desde = ahora;
                return;
            }

            case Fase.BateImpacto:
            {
                if (ahora - desde < 0.6f) return;
                float d = EnElPiso(lugarDelHinchado, zombi.transform.position);
                Vector3 hacia = zombi.transform.position - lugarDelHinchado;
                hacia.y = 0f;
                Ok("el bate patea 4 m al inflado (" + d.ToString("0.00") + " m), para donde iba el golpe",
                   d > 3.2f && d < 4.6f && Vector3.Angle(hacia, rumboDelBate) < 25f);
                Ok("sin cortarle la cuenta, con el aviso detras", hinchado.Inflando && hinchado.AvisoPrendido);
                fase = Fase.BateReventar;
                return;
            }

            case Fase.BateReventar:
                if (hinchado.Reventones == 0)
                {
                    if (ahora - inflandoEn > 2.5f) { Terminar("el pateado no revento"); return; }
                    return;
                }
                Ok("y revienta donde cayo, a su tiempo (" + (ahora - inflandoEn).ToString("0.00") + " s)",
                   Mathf.Abs(ahora - inflandoEn - hinchado.avisoCongelado) < 0.12f);
                fase = Fase.BateCaminando;
                desde = ahora;
                return;

            case Fase.BateCaminando:
            {
                // El bate recarga 4 s.
                if (Bate.Instancia == null || Bate.Instancia.RestanteEnfriamiento > 0f || Bate.Instancia.EnCurso)
                {
                    if (ahora - desde > 6f) { Terminar("el bate no volvio a estar listo"); return; }
                    return;
                }
                EnemyController.DespejarAlrededor(Vector3.zero, 500f);
                zombi = Sacar(ConstructorZombis.RutaHinchado, control.transform.position + rumboDelBate * 2.2f, 1000f, true);
                hinchado = zombi.GetComponent<ZombiHinchado>();
                lugarDelHinchado = zombi.transform.position;
                if (!Bate.Instancia.Golpear()) { Terminar("el bate no arranco la segunda vez"); return; }
                fase = Fase.BateCaminandoImpacto;
                desde = ahora;
                return;
            }

            case Fase.BateCaminandoImpacto:
            {
                if (ahora - desde < 0.6f) return;
                float d = EnElPiso(lugarDelHinchado, zombi.transform.position);
                Ok("caminando es liviano: el bate lo empuja 3 m y no se infla (" + d.ToString("0.00") + " m)", d > 2.3f && d < 3.6f && !hinchado.Inflando);
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
        inf.AppendLine("Prueba del hinchado (WaveMode, camino del telefono, sin oleadas ni auto-apuntado)");
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
        inf.AppendLine("Foto: " + Foto);
        inf.AppendLine("RESULTADO: " + (todo ? "TODO OK" : "HAY FALLAS"));
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(Ruta)));
        File.WriteAllText(Ruta, inf.ToString());

        SessionState.SetBool(Clave, false);
        EditorApplication.ExitPlaymode();
        Debug.Log(inf.ToString());
    }
}
