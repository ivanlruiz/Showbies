using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Banco del tutorial (Tutorial.unity, TutorialManager). Mira las tres cosas que se cambiaron
// sin probarlas:
//
// 1. El paso de la granada termina cuando EXPLOTA la granada tirada en ese paso, no al
//    tirarla. Se tira por el camino del juego (PlayerController.TirarGranadaApuntada, lo que
//    llama el boton G al soltar) dos veces: una al grupo de zombis del paso, que tiene que
//    caerles encima y matar (los muertos terminan de caerse solos y solo ellos dan puntos),
//    y otra lejos del grupo, con la escena recargada: el paso tiene que avanzar igual al
//    explotar, y los tres zombis que quedan se van con sus particulas de muerte y SIN puntos
//    (TutorialManager.Sacar).
// 2. En el paso de las cajas, agarrar la de balas no saltea la de arma ni muestra el texto
//    del reloj ("el cargador se queda para siempre"): el paso sigue pidiendo la caja de arma
//    aunque la cadencia de la de balas este activa, y cuando esa cadencia vence la caja de
//    arma sigue en el piso. Despues se agarra la de arma y el tutorial tiene que terminar.
// 3. Todo nace del lado de adentro de las paredes (TutorialManager.PuntoDelMapa, +-44): con el
//    jugador en una esquina (las paredes invisibles estan en +-49), el zombi del paso 2, los
//    tres de la granada, las dos cajas y la caja de arma.
//
// Corre dos partidas del tutorial: la primera con el jugador en las esquinas y la granada al
// grupo; la segunda, con la escena recargada, con el jugador cerca del centro, la granada
// lejos del grupo y el paso de las cajas entero hasta el final.
//
// Los pasos 1 y 2 se cumplen por codigo: moverse es llevar al jugador (el paso mira la
// distancia al punto de partida) y disparar es matar al zombi con DanoZombi, el mismo camino
// que una bala. Para medir donde nacen los zombis sin que caminen, los pasos 2 y 3 de la
// primera partida se disparan con Time.timeScale en 0 (Update corre, la fisica no), y el
// banco lo devuelve a 1 al medirlos (y al terminar, por las dudas).
//
// Escribe Builds/prueba_tutorial.txt y dos capturas: Builds/tutorial_granada.png y
// Builds/tutorial_caja_arma.png.
//
// OJO: entrar y salir de play hace que Unity vuelva a serializar ProjectSettings (ver la
// trampa en CLAUDE.md): despues de correrlo, revertir lo que no se toco a proposito.
[InitializeOnLoad]
public static class PruebaTutorial
{
    const string Clave = "ShowBies.PruebaTutorial";
    const string Ruta = "../Builds/prueba_tutorial.txt";
    const string Escena = "Assets/Escenas/Tutorial.unity";
    const string ClaveCompletado = "TutorialCompletado";

    // Topes en segundos reales: el total, y los de cada paso en TopeDelPaso.
    const double TopeTotal = 300.0;

    // Las esquinas: a 1 m de las paredes invisibles, del lado de adentro. En Tutorial.unity
    // la cara de adentro de las paredes queda entre 49,2 y 49,7 m del centro, y el jugador
    // tiene 0,25 m de radio.
    static readonly Vector3 EsquinaA = new Vector3(48f, 0f, 48f);
    static readonly Vector3 EsquinaB = new Vector3(-48f, 0f, 48f);
    // Desde aca se da por hecho que el jugador estaba contra una pared al nacer algo.
    const float ContraLaPared = 46.5f;
    const float Tolerancia = 0.05f;
    // Las dos cajas del paso 4 van a 4 m a cada lado de un punto: 8 m entre las dos.
    const float SeparacionDeLasCajas = 7.9f;

    // Lo privado que mira el banco. Si cambia un nombre, lo dice al arrancar en vez de medir
    // cualquier cosa.
    const BindingFlags Privado = BindingFlags.NonPublic | BindingFlags.Instance;
    static readonly FieldInfo campoPaso = typeof(TutorialManager).GetField("paso", Privado);
    static readonly FieldInfo campoObjetos = typeof(TutorialManager).GetField("objetosDelPaso", Privado);
    static readonly FieldInfo campoEsperando = typeof(TutorialManager).GetField("esperandoQueVenzaLaMejora", Privado);
    static readonly FieldInfo campoGranadaTirada = typeof(TutorialManager).GetField("granadaTirada", Privado);
    static readonly FieldInfo campoGranadaDelPaso = typeof(TutorialManager).GetField("granadaDelPaso", Privado);
    static readonly FieldInfo campoVuelo = typeof(Granade).GetField("tiempoDeVuelo", Privado);
    // Lo que llama la fisica al tocar una caja: se usa si caminar encima no la agarra, y en
    // la esquina del paso 5, donde el jugador tiene que estar lejos de la caja.
    static readonly MethodInfo metodoTrigger = typeof(PlayerController).GetMethod("OnTriggerEnter", Privado, null, new[] { typeof(Collider) }, null);

    enum Paso
    {
        // Primera partida: las esquinas y la granada al grupo.
        EsperarEscena1, Esquina, MatarZombi1, TirarAlGrupo, EsperarExplosion1, DespuesDeExplotar1,
        OtraEsquina, EsperarCadaveres,
        // Segunda partida, con la escena recargada: la granada afuera y las cajas hasta el final.
        EsperarEscena2, Moverse2, MatarZombi2, TirarAfuera, EsperarExplosion2, DespuesDeExplotar2,
        AgarrarBalas, ConCajaDeArma, EsperarQueVenzaBalas, AgarrarArma, ConReloj, EsperarFin, Listo
    }

    // Lo que se mide de cada granada.
    class CasoGranada
    {
        public readonly List<EnemyController> zombis = new List<EnemyController>();
        public readonly List<Vector3> dondeNacieron = new List<Vector3>();
        public readonly List<EnemyController> cadaveres = new List<EnemyController>();
        public int puntosPorZombi = 1;
        public ParticleSystem particulas;              // las de muerte del zombi del paso (el prefab)
        public Granade granada;
        public bool tirada;
        public float vuelo = 0.8f;                     // tiempoDeVuelo del prefab de la granada
        public float distanciaAlGrupo = -1f, distanciaDelTiro = -1f;
        public float tiradaEn = -1f, exploto = -1f, cambioEn = -1f;   // Time.time
        public double explotoReal = -1, cambioReal = -1;               // tiempo real
        public bool avanzoConLaGranadaViva, reconocioLaGranada;
        public int vivosEnVuelo = -1;
        public int muertosPorLaGranada = -1, sacados = -1, vivosDespues = -1;
        public int cadaveresA03 = -1, cadaveresA25 = -1, sacadosA03 = -1;
        public int puntosAntes = -1, puntosDespues = -1;
        public int particulasAntes = -1, particulasDespues = -1;
        public int zombisVivosDespues = -1;
    }

    static Paso paso;
    static double inicio, desdePaso;
    static TutorialManager tm, tmViejo;
    static bool congelado, terminado, esMovil;
    static float duracionMejora = 10f;

    static CasoGranada alGrupo, afuera;

    // Las paredes (primera partida).
    static Vector3 jugadorPaso2, jugadorPaso3, jugadorPaso4, jugadorPaso5, zombiPaso2, cajaPaso5;
    static readonly List<Vector3> zombisPaso3 = new List<Vector3>();
    static readonly List<Vector3> cajasPaso4 = new List<Vector3>();
    static bool midioPaso2, midioPaso3, midioPaso4, midioPaso5;

    // Las cajas (segunda partida).
    static GameObject cajaBalas, cajaArma;
    static bool reflexionBalas, reflexionArma, textoCajas;
    static string agarreBalasPor, agarreArmaPor;
    static float agarreBalasEn, agarreArmaEn;                // Time.time
    static string pasoConCajaDeArma, pasoAlVencer, pasoConReloj;
    static bool textoCajaArma, esperandoConCajaDeArma, cajaArmaEnElPiso, cadenciaDeBalasActiva;
    static double vencioBalasEn;
    static float segundosDeLaCadenciaDeBalas;
    static bool textoCajaArmaAlVencer, esperandoAlVencer, cajaArmaAlVencer;
    static bool relojAntesDeTiempo;
    static bool textoReloj, esperandoConReloj, cadenciaDeArmaActiva, cargadorAgrandado;
    static int maxBalasConReloj, cargadorMejorado;
    static bool llegoAlFin, panelFinalPrendido, instruccionApagada;
    static int tutorialCompletado;
    static float segundosHastaElFin;

    // Las excepciones y errores que salten mientras corre.
    static readonly StringBuilder excepciones = new StringBuilder();
    static int cuantasExcepciones;

    static PruebaTutorial()
    {
        EditorApplication.update += Tick;
        Application.logMessageReceived += AnotarExcepcion;
    }

    static void AnotarExcepcion(string mensaje, string pila, LogType tipo)
    {
        if (!SessionState.GetBool(Clave, false)) return;
        if (tipo != LogType.Exception && tipo != LogType.Error) return;
        cuantasExcepciones++;
        if (cuantasExcepciones <= 3) excepciones.AppendLine("  " + mensaje + System.Environment.NewLine + pila);
    }

    [MenuItem("ShowBies/Pruebas/Tutorial (play)")]
    // Publico para poder correrlo por codigo (ver la trampa del registro de menus).
    public static void Arrancar()
    {
        if (!RespaldoDelBanco.PuedeArrancar("PruebaTutorial")) return;
        RespaldoDelBanco.Guardar("PruebaTutorial");
        // El tutorial lo anota al terminar: se borra para ver que lo anote. RespaldoDelBanco
        // guarda los PlayerPrefs antes y los devuelve despues.
        PlayerPrefs.DeleteKey(ClaveCompletado);
        PlayerPrefs.Save();
        PlayerSettings.runInBackground = true;
        EditorSceneManager.OpenScene(Escena);
        SessionState.SetBool(Clave, true);
        SessionState.SetBool(Clave + ".empezo", false);
        EditorApplication.EnterPlaymode();
    }

    static void Tick()
    {
        if (!SessionState.GetBool(Clave, false)) return;
        if (!EditorApplication.isPlaying) return;
        if (!RespaldoDelBanco.SigueArmado("PruebaTutorial", Clave)) return;

        double ahora = EditorApplication.timeSinceStartup;
        // Entrar en play recarga el dominio (EditorSettings: sin opciones de entrada rapida),
        // asi que lo estatico se arma recien en el primer tick en play.
        if (!SessionState.GetBool(Clave + ".empezo", false))
        {
            SessionState.SetBool(Clave + ".empezo", true);
            Reiniciar(ahora);
            return;
        }
        if (terminado) return;

        if (ahora - inicio > TopeTotal)
        {
            Terminar("se paso el tope total de " + TopeTotal.ToString("0") + " s, en el paso " + paso);
            return;
        }
        if (ahora - desdePaso > TopeDelPaso(paso))
        {
            Terminar("se trabo en el paso " + paso + " (tope " + TopeDelPaso(paso).ToString("0") + " s): esperaba " + QueEspera(paso)
                     + "; el tutorial esta en \"" + PasoDelTutorial() + "\"");
            return;
        }

        try
        {
            Avanzar(ahora);
        }
        catch (System.Exception e)
        {
            Terminar("el banco tiro una excepcion en el paso " + paso + ": " + e);
        }
    }

    static void Reiniciar(double ahora)
    {
        inicio = desdePaso = ahora;
        paso = Paso.EsperarEscena1;
        tm = tmViejo = null;
        congelado = terminado = esMovil = false;
        duracionMejora = 10f;
        alGrupo = new CasoGranada();
        afuera = new CasoGranada();

        jugadorPaso2 = jugadorPaso3 = jugadorPaso4 = jugadorPaso5 = zombiPaso2 = cajaPaso5 = Vector3.zero;
        zombisPaso3.Clear();
        cajasPaso4.Clear();
        midioPaso2 = midioPaso3 = midioPaso4 = midioPaso5 = false;

        cajaBalas = cajaArma = null;
        reflexionBalas = reflexionArma = textoCajas = false;
        agarreBalasPor = agarreArmaPor = "";
        agarreBalasEn = agarreArmaEn = -1f;
        pasoConCajaDeArma = pasoAlVencer = pasoConReloj = "";
        textoCajaArma = esperandoConCajaDeArma = cajaArmaEnElPiso = cadenciaDeBalasActiva = false;
        vencioBalasEn = -1;
        segundosDeLaCadenciaDeBalas = -1f;
        textoCajaArmaAlVencer = esperandoAlVencer = cajaArmaAlVencer = false;
        relojAntesDeTiempo = false;
        textoReloj = esperandoConReloj = cadenciaDeArmaActiva = cargadorAgrandado = false;
        maxBalasConReloj = cargadorMejorado = -1;
        llegoAlFin = panelFinalPrendido = instruccionApagada = false;
        tutorialCompletado = -1;
        segundosHastaElFin = -1f;

        excepciones.Length = 0;
        cuantasExcepciones = 0;

        Directory.CreateDirectory(Path.GetFullPath("../Builds"));
        PlayerPrefs.DeleteKey(ClaveCompletado);

        var faltan = new List<string>();
        if (campoPaso == null) faltan.Add("TutorialManager.paso");
        if (campoObjetos == null) faltan.Add("TutorialManager.objetosDelPaso");
        if (campoEsperando == null) faltan.Add("TutorialManager.esperandoQueVenzaLaMejora");
        if (campoGranadaTirada == null) faltan.Add("TutorialManager.granadaTirada");
        if (campoGranadaDelPaso == null) faltan.Add("TutorialManager.granadaDelPaso");
        if (metodoTrigger == null) faltan.Add("PlayerController.OnTriggerEnter");
        if (faltan.Count > 0) Terminar("no se encontro por reflexion: " + string.Join(", ", faltan));
    }

    static void Pasar(Paso siguiente, double ahora)
    {
        paso = siguiente;
        desdePaso = ahora;
    }

    static double TopeDelPaso(Paso p)
    {
        switch (p)
        {
            case Paso.EsperarEscena1:
            case Paso.EsperarEscena2:
                return 20.0;
            case Paso.TirarAlGrupo:
            case Paso.TirarAfuera:
                return 3.0;
            case Paso.EsperarExplosion1:
            case Paso.EsperarExplosion2:
                return 10.0;
            case Paso.EsperarCadaveres:
                return 6.0;
            // Lo que dura la cadencia de una caja es tiempo del juego: con cuadros largos el
            // tiempo del juego va mas lento que el real (Time.maximumDeltaTime).
            case Paso.EsperarQueVenzaBalas:
            case Paso.EsperarFin:
                return duracionMejora * 2.0 + 10.0;
            default:
                return 5.0;
        }
    }

    static string QueEspera(Paso p)
    {
        switch (p)
        {
            case Paso.EsperarEscena1: return "que cargue el tutorial (TutorialManager en Moverse, con su texto)";
            case Paso.Esquina: return "que el tutorial pase a Disparar al llevar al jugador a la esquina";
            case Paso.MatarZombi1:
            case Paso.MatarZombi2: return "que el tutorial pase a Granada al matar al zombi del paso 2";
            case Paso.TirarAlGrupo:
            case Paso.TirarAfuera: return "tirar la granada";
            case Paso.EsperarExplosion1:
            case Paso.EsperarExplosion2: return "que la granada explote y el tutorial pase a Pickups";
            case Paso.DespuesDeExplotar1:
            case Paso.DespuesDeExplotar2: return "medir lo que quedo despues de la explosion";
            case Paso.OtraEsquina: return "que el tutorial pase a Arma al agarrar la caja de balas desde la otra esquina";
            case Paso.EsperarCadaveres: return "que pasen 2,5 s desde la explosion";
            case Paso.EsperarEscena2: return "que se recargue el tutorial (otro TutorialManager en Moverse, con su texto)";
            case Paso.Moverse2: return "que el tutorial pase a Disparar al mover al jugador 6 m";
            case Paso.AgarrarBalas: return "que el jugador agarre la caja de balas";
            case Paso.ConCajaDeArma: return "medir el paso de la caja de arma";
            case Paso.EsperarQueVenzaBalas: return "que venza la cadencia de la caja de balas (GunController.MejoraActiva en falso)";
            case Paso.AgarrarArma: return "que el jugador agarre la caja de arma";
            case Paso.ConReloj: return "medir el texto del reloj";
            case Paso.EsperarFin: return "que venza la cadencia de la caja de arma y el tutorial pase a Fin";
            default: return "terminar";
        }
    }

    static void Avanzar(double ahora)
    {
        switch (paso)
        {
            // ---------- Primera partida: las esquinas y la granada al grupo ----------

            case Paso.EsperarEscena1:
            {
                if (ahora - desdePaso < 1.0) return;
                var t = Tutorial();
                if (!EscenaLista(t)) return;
                esMovil = Plataforma.EsMovil;
                duracionMejora = t.jugador.theGun.duracionMejora;
                cargadorMejorado = t.jugador.cargadorMejorado;
                // Congelado: el zombi del paso 2 y los tres del paso 3 no caminan hasta que se
                // los mide (Update corre, la fisica no). Se devuelve al medir el paso 3.
                Congelar(true);
                // Moverse se cumple llevando al jugador a la esquina, que esta a mas de 5 m.
                Llevar(t.jugador, EsquinaA);
                jugadorPaso2 = t.jugador.transform.position;
                Pasar(Paso.Esquina, ahora);
                return;
            }

            case Paso.Esquina:
            {
                if (PasoDelTutorial() != "Disparar") return;
                var zombi = PrimerZombiDelPaso();
                if (zombi == null) { Terminar("el paso de disparar no puso su zombi"); return; }
                zombiPaso2 = zombi.transform.position;
                midioPaso2 = true;
                // Disparar se cumple matandolo por el mismo camino que una bala.
                zombi.DanoZombi(100000f);
                Pasar(Paso.MatarZombi1, ahora);
                return;
            }

            case Paso.MatarZombi1:
            {
                if (PasoDelTutorial() != "Granada") return;
                if (!AnotarElGrupo(alGrupo)) { Terminar("el paso de la granada puso " + alGrupo.zombis.Count + " zombis en vez de 3"); return; }
                jugadorPaso3 = tm.jugador.transform.position;
                zombisPaso3.AddRange(alGrupo.dondeNacieron);
                midioPaso3 = true;
                Congelar(false);
                Pasar(Paso.TirarAlGrupo, ahora);
                return;
            }

            case Paso.TirarAlGrupo:
                // Un momento con la fisica andando, y al grupo, por el camino del boton G.
                if (ahora - desdePaso < 0.15) return;
                Tirar(alGrupo, true);
                if (!alGrupo.tirada) { Terminar("no salio la granada al grupo (PlayerController.GranadaLista dio falso?)"); return; }
                Pasar(Paso.EsperarExplosion1, ahora);
                return;

            case Paso.EsperarExplosion1:
                if (SeguirLaGranada(alGrupo, ahora))
                {
                    // Las cajas del paso 4 nacieron en el mismo cuadro, con el jugador en la esquina.
                    jugadorPaso4 = tm.jugador.transform.position;
                    foreach (var go in ObjetosDelPaso())
                        if (go != null) cajasPaso4.Add(go.transform.position);
                    midioPaso4 = true;
                    Pasar(Paso.DespuesDeExplotar1, ahora);
                    return;
                }
                // Que los zombis no lo saquen de la esquina: las cajas nacen segun donde este.
                if (Plano(tm.jugador.transform.position - EsquinaA).magnitude > 0.3f) Llevar(tm.jugador, EsquinaA);
                return;

            case Paso.DespuesDeExplotar1:
            {
                double pasado = ahora - alGrupo.cambioReal;
                // Los que mato la granada se estan cayendo: el tutorial no los saca.
                if (alGrupo.cadaveresA03 < 0 && pasado >= 0.3)
                {
                    alGrupo.cadaveresA03 = Cuantos(alGrupo.cadaveres, true);
                    Capturar("tutorial_granada");
                }
                if (pasado < 0.5) return;
                alGrupo.puntosDespues = Puntos();
                alGrupo.zombisVivosDespues = EnemyController.ZombisVivos;

                // El paso 5 contra otra pared: el jugador a la otra esquina, y la caja de balas
                // se agarra llamando al OnTriggerEnter del jugador, que es lo que haria la
                // fisica al tocarla (caminando hasta ella dejaria de estar contra la pared).
                var balas = CajaDelPaso("PUBalas");
                if (balas == null) { Terminar("el paso de las cajas no puso la caja de balas (primera partida)"); return; }
                Llevar(tm.jugador, EsquinaB);
                jugadorPaso5 = tm.jugador.transform.position;
                if (!AgarrarConElTrigger(tm.jugador, balas)) { Terminar("no se pudo llamar a PlayerController.OnTriggerEnter"); return; }
                Pasar(Paso.OtraEsquina, ahora);
                return;
            }

            case Paso.OtraEsquina:
            {
                if (PasoDelTutorial() != "Arma") return;
                var arma = CajaDelPaso("PUArma");
                if (arma == null) { Terminar("el paso de la caja de arma no puso su caja (primera partida)"); return; }
                cajaPaso5 = arma.transform.position;
                midioPaso5 = true;
                Pasar(Paso.EsperarCadaveres, ahora);
                return;
            }

            case Paso.EsperarCadaveres:
                // El desplome dura 1,4 s del juego: a los 2,5 s reales ya no tiene que quedar ninguno.
                if (ahora - alGrupo.cambioReal < 2.5) return;
                alGrupo.cadaveresA25 = Cuantos(alGrupo.cadaveres, false);
                Recargar(ahora);
                return;

            // ---------- Segunda partida: la granada afuera y las cajas ----------

            case Paso.EsperarEscena2:
            {
                if (ahora - desdePaso < 1.0) return;
                var nuevo = Object.FindFirstObjectByType<TutorialManager>();
                // Con la carga en modo Single, la escena vieja se va recien en el cuadro siguiente.
                if (nuevo == null || object.ReferenceEquals(nuevo, tmViejo)) return;
                tm = nuevo;
                if (!EscenaLista(tm)) return;
                // Moverse: 6 m hacia el centro del mapa (el paso pide 5).
                Vector3 desde = tm.jugador.transform.position;
                Vector3 haciaElCentro = Plano(-desde);
                Vector3 direccion = haciaElCentro.magnitude > 1f ? haciaElCentro.normalized : Vector3.right;
                Llevar(tm.jugador, desde + direccion * 6f);
                Pasar(Paso.Moverse2, ahora);
                return;
            }

            case Paso.Moverse2:
            {
                if (PasoDelTutorial() != "Disparar") return;
                var zombi = PrimerZombiDelPaso();
                if (zombi == null) { Terminar("el paso de disparar no puso su zombi (segunda partida)"); return; }
                zombi.DanoZombi(100000f);
                Pasar(Paso.MatarZombi2, ahora);
                return;
            }

            case Paso.MatarZombi2:
                if (PasoDelTutorial() != "Granada") return;
                if (!AnotarElGrupo(afuera)) { Terminar("el paso de la granada puso " + afuera.zombis.Count + " zombis en vez de 3 (segunda partida)"); return; }
                Pasar(Paso.TirarAfuera, ahora);
                return;

            case Paso.TirarAfuera:
                if (ahora - desdePaso < 0.15) return;
                Tirar(afuera, false);
                if (!afuera.tirada) { Terminar("no salio la granada afuera (PlayerController.GranadaLista dio falso?)"); return; }
                Pasar(Paso.EsperarExplosion2, ahora);
                return;

            case Paso.EsperarExplosion2:
                if (!SeguirLaGranada(afuera, ahora)) return;
                Pasar(Paso.DespuesDeExplotar2, ahora);
                return;

            case Paso.DespuesDeExplotar2:
            {
                double pasado = ahora - afuera.cambioReal;
                // Las particulas de muerte viven 0,3 s: el maximo del primer medio segundo.
                if (pasado <= 0.5) afuera.particulasDespues = Mathf.Max(afuera.particulasDespues, Particulas(afuera.particulas));
                if (afuera.sacadosA03 < 0 && pasado >= 0.3)
                {
                    afuera.sacadosA03 = 0;
                    foreach (var z in afuera.zombis)
                        if (z == null || !z.gameObject.activeSelf) afuera.sacadosA03++;
                    afuera.zombisVivosDespues = EnemyController.ZombisVivos;
                }
                if (pasado < 0.5) return;
                afuera.puntosDespues = Puntos();
                textoCajas = Instruccion() == Textos.De("tut_cajas");

                // La caja de balas se agarra caminando encima: el jugador sobre la caja y la
                // fisica hace el resto (si no la agarra en 1 s, por el mismo metodo, a mano).
                cajaBalas = CajaDelPaso("PUBalas");
                if (cajaBalas == null) { Terminar("el paso de las cajas no puso la caja de balas (segunda partida)"); return; }
                Llevar(tm.jugador, cajaBalas.transform.position);
                Pasar(Paso.AgarrarBalas, ahora);
                return;
            }

            case Paso.AgarrarBalas:
                if (cajaBalas != null && cajaBalas.activeSelf)
                {
                    if (!reflexionBalas && ahora - desdePaso >= 1.0)
                    {
                        reflexionBalas = true;
                        if (!AgarrarConElTrigger(tm.jugador, cajaBalas)) { Terminar("no se pudo llamar a PlayerController.OnTriggerEnter"); return; }
                    }
                    return;
                }
                agarreBalasPor = reflexionBalas ? "llamando a PlayerController.OnTriggerEnter (caminar encima no la agarro en 1 s)" : "caminando encima";
                agarreBalasEn = Time.time;
                Pasar(Paso.ConCajaDeArma, ahora);
                return;

            case Paso.ConCajaDeArma:
                MirarElRelojAntesDeTiempo();
                if (ahora - desdePaso < 0.5) return;
                pasoConCajaDeArma = PasoDelTutorial();
                cajaArma = CajaDelPaso("PUArma");
                textoCajaArma = Instruccion() == Textos.De("tut_caja_arma");
                esperandoConCajaDeArma = Esperando();
                cajaArmaEnElPiso = cajaArma != null && cajaArma.activeSelf;
                cadenciaDeBalasActiva = tm.jugador.theGun.MejoraActiva;
                Capturar("tutorial_caja_arma");
                if (cajaArma == null) { Terminar("agarrar la caja de balas no dejo la caja de arma en el piso"); return; }
                Pasar(Paso.EsperarQueVenzaBalas, ahora);
                return;

            case Paso.EsperarQueVenzaBalas:
                MirarElRelojAntesDeTiempo();
                if (vencioBalasEn < 0)
                {
                    if (tm.jugador.theGun.MejoraActiva) return;
                    vencioBalasEn = ahora;
                    segundosDeLaCadenciaDeBalas = Time.time - agarreBalasEn;
                    return;
                }
                // Un segundo despues de vencer: antes, ahi el paso se daba por terminado y se
                // llevaba la caja de arma sin que nadie la tocara.
                if (ahora - vencioBalasEn < 1.0) return;
                pasoAlVencer = PasoDelTutorial();
                textoCajaArmaAlVencer = Instruccion() == Textos.De("tut_caja_arma");
                esperandoAlVencer = Esperando();
                cajaArmaAlVencer = cajaArma != null && cajaArma.activeSelf;
                if (!cajaArmaAlVencer) { Terminar("al vencer la cadencia de la caja de balas se fue la caja de arma sin que nadie la tocara"); return; }
                Llevar(tm.jugador, cajaArma.transform.position);
                Pasar(Paso.AgarrarArma, ahora);
                return;

            case Paso.AgarrarArma:
                if (cajaArma != null && cajaArma.activeSelf)
                {
                    MirarElRelojAntesDeTiempo();
                    if (!reflexionArma && ahora - desdePaso >= 1.0)
                    {
                        reflexionArma = true;
                        if (!AgarrarConElTrigger(tm.jugador, cajaArma)) { Terminar("no se pudo llamar a PlayerController.OnTriggerEnter"); return; }
                    }
                    return;
                }
                agarreArmaPor = reflexionArma ? "llamando a PlayerController.OnTriggerEnter (caminar encima no la agarro en 1 s)" : "caminando encima";
                agarreArmaEn = Time.time;
                Pasar(Paso.ConReloj, ahora);
                return;

            case Paso.ConReloj:
                if (ahora - desdePaso < 0.5) return;
                pasoConReloj = PasoDelTutorial();
                textoReloj = Instruccion() == Textos.De("tut_reloj");
                esperandoConReloj = Esperando();
                cadenciaDeArmaActiva = tm.jugador.theGun.MejoraActiva;
                maxBalasConReloj = tm.jugador.maxBalas;
                cargadorAgrandado = tm.jugador.maxBalas >= tm.jugador.cargadorMejorado;
                Pasar(Paso.EsperarFin, ahora);
                return;

            case Paso.EsperarFin:
                if (PasoDelTutorial() != "Fin") return;
                llegoAlFin = true;
                segundosHastaElFin = Time.time - agarreArmaEn;
                panelFinalPrendido = tm.panelFinal != null && tm.panelFinal.activeSelf;
                instruccionApagada = tm.textoInstruccion != null && tm.textoInstruccion.transform.parent != null
                                     && !tm.textoInstruccion.transform.parent.gameObject.activeSelf;
                tutorialCompletado = PlayerPrefs.GetInt(ClaveCompletado, 0);
                Pasar(Paso.Listo, ahora);
                Terminar(null);
                return;
        }
    }

    // ---------- La granada ----------

    // Tira la granada de este paso por el camino del boton G (TirarGranadaApuntada), con la
    // palanca que la lleva a esa distancia: al grupo, adonde el grupo va a estar cuando caiga
    // (los zombis vienen derecho al jugador mientras vuela); afuera, lo mas lejos posible del
    // lado contrario.
    static void Tirar(CasoGranada caso, bool alGrupo)
    {
        var jugador = tm.jugador;
        Vector3 desde = Plano(jugador.transform.position);
        Vector3 centro = Vector3.zero;
        int vivos = 0;
        foreach (var z in caso.zombis)
        {
            if (z == null || !z.Vivo) continue;
            centro += Plano(z.transform.position);
            vivos++;
        }
        Vector3 haciaElGrupo = vivos > 0 ? centro / vivos - desde : Plano(jugador.transform.forward);
        caso.distanciaAlGrupo = haciaElGrupo.magnitude;
        Vector3 direccion = haciaElGrupo.sqrMagnitude > 0.0001f ? haciaElGrupo.normalized : Vector3.forward;

        caso.vuelo = campoVuelo != null && jugador.granadaPrefab != null ? (float)campoVuelo.GetValue(jugador.granadaPrefab) : 0.8f;
        float minima = jugador.distanciaMinimaGranada, maxima = jugador.distanciaMaximaGranada;
        float distancia;
        if (alGrupo)
        {
            var primero = caso.zombis.Count > 0 ? caso.zombis[0] : null;
            float velocidad = primero != null && primero.enemyType != null ? primero.enemyType.velocidad : 5f;
            distancia = Mathf.Clamp(caso.distanciaAlGrupo - velocidad * caso.vuelo, minima, maxima);
        }
        else
        {
            direccion = -direccion;
            distancia = maxima;
        }

        // La palanca: la direccion en la pantalla (arriba es +Z) por cuanto se arrastro, de 0
        // a 1. En 0 tiraria hacia donde mira el jugador, que en PC es el mouse.
        float cuanto = maxima > minima ? Mathf.InverseLerp(minima, maxima, distancia) : 1f;
        cuanto = Mathf.Max(cuanto, 0.01f);
        caso.distanciaDelTiro = Mathf.Lerp(minima, maxima, cuanto);

        caso.puntosAntes = Puntos();
        caso.particulasAntes = Particulas(caso.particulas);
        caso.vivosEnVuelo = Vivos(caso.zombis);
        caso.tiradaEn = Time.time;
        jugador.TirarGranadaApuntada(new Vector2(direccion.x, direccion.z) * cuanto);
        // LanzarGranadaA la instancia en el acto: es la unica granada de la escena.
        caso.granada = Object.FindFirstObjectByType<Granade>();
        caso.tirada = caso.granada != null;
    }

    // Sigue la granada tirada. Devuelve verdadero en el primer tick en que el tutorial ya paso
    // a las cajas, con lo que quedo de los zombis del paso clasificado: el cadaver de uno que
    // mato la granada sigue prendido mientras se desploma (1,4 s), y uno que saco el tutorial
    // se apaga en el acto y se destruye al final del cuadro.
    static bool SeguirLaGranada(CasoGranada caso, double ahora)
    {
        string enElTutorial = PasoDelTutorial();
        if (caso.granada != null)
        {
            if (enElTutorial != "Granada") caso.avanzoConLaGranadaViva = true;
            var delPaso = campoGranadaDelPaso.GetValue(tm) as Granade;
            if ((bool)campoGranadaTirada.GetValue(tm) && delPaso != null && delPaso == caso.granada) caso.reconocioLaGranada = true;
            caso.vivosEnVuelo = Vivos(caso.zombis);
            caso.particulasAntes = Particulas(caso.particulas);
        }
        else if (caso.explotoReal < 0)
        {
            // Al explotar se destruye: el primer tick en que ya no esta.
            caso.explotoReal = ahora;
            caso.exploto = Time.time;
        }
        if (enElTutorial != "Pickups") return false;

        caso.cambioReal = ahora;
        caso.cambioEn = Time.time;
        caso.muertosPorLaGranada = caso.sacados = caso.vivosDespues = 0;
        foreach (var z in caso.zombis)
        {
            if (z == null || !z.gameObject.activeSelf) caso.sacados++;
            else if (z.Vivo) caso.vivosDespues++;
            else
            {
                caso.muertosPorLaGranada++;
                caso.cadaveres.Add(z);
            }
        }
        caso.particulasDespues = Particulas(caso.particulas);
        return true;
    }

    static bool AnotarElGrupo(CasoGranada caso)
    {
        caso.zombis.Clear();
        caso.dondeNacieron.Clear();
        foreach (var go in ObjetosDelPaso())
        {
            var z = go != null ? go.GetComponent<EnemyController>() : null;
            if (z == null) continue;
            caso.zombis.Add(z);
            caso.dondeNacieron.Add(z.transform.position);
        }
        if (caso.zombis.Count == 0) return false;
        var primero = caso.zombis[0];
        caso.puntosPorZombi = primero.enemyType != null ? primero.enemyType.puntos : 1;
        caso.particulas = primero.deathParticles;
        return caso.zombis.Count == 3;
    }

    // Las particulas de muerte vivas de ese prefab: Efectos las emite en una copia por
    // escena ("<prefab> (compartida)", hija de Efectos), o instancia el prefab si no sirve
    // para eso ("<prefab>(Clone)").
    static int Particulas(ParticleSystem prefab)
    {
        if (prefab == null) return -1;
        string compartida = prefab.name + " (compartida)", clon = prefab.name + "(Clone)";
        int n = 0;
        foreach (var sistema in Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None))
            if (sistema.name == compartida || sistema.name == clon) n += sistema.particleCount;
        return n;
    }

    static bool AvanzoAlExplotar(CasoGranada c)
    {
        return c.tirada && !c.avanzoConLaGranadaViva && c.explotoReal >= 0 && c.cambioReal >= 0 && c.cambioReal - c.explotoReal <= 0.5;
    }

    // ---------- El tutorial ----------

    static TutorialManager Tutorial()
    {
        if (tm == null) tm = Object.FindFirstObjectByType<TutorialManager>();
        return tm;
    }

    // El nombre del paso del enum privado Paso: Moverse, Disparar, Granada, Pickups, Arma o Fin.
    static string PasoDe(TutorialManager t)
    {
        if (t == null || campoPaso == null) return "";
        object valor = campoPaso.GetValue(t);
        return valor != null ? valor.ToString() : "";
    }

    static string PasoDelTutorial()
    {
        return PasoDe(Tutorial());
    }

    // Recien cuando corrio su Start: en Moverse y con el texto de moverse puesto.
    static bool EscenaLista(TutorialManager t)
    {
        if (t == null || t.jugador == null || t.jugador.theGun == null || t.textoInstruccion == null) return false;
        if (Puntaje.instance == null || PlayerHealth.instance == null) return false;
        if (PasoDe(t) != "Moverse") return false;
        string texto = t.textoInstruccion.text;
        return texto == Textos.De("tut_mover_pc") || texto == Textos.De("tut_mover_movil");
    }

    // Una copia de lo que puso el paso actual.
    static List<GameObject> ObjetosDelPaso()
    {
        var t = Tutorial();
        var lista = t != null && campoObjetos != null ? campoObjetos.GetValue(t) as List<GameObject> : null;
        return lista != null ? new List<GameObject>(lista) : new List<GameObject>();
    }

    static EnemyController PrimerZombiDelPaso()
    {
        foreach (var go in ObjetosDelPaso())
        {
            var z = go != null ? go.GetComponent<EnemyController>() : null;
            if (z != null) return z;
        }
        return null;
    }

    static GameObject CajaDelPaso(string etiqueta)
    {
        foreach (var go in ObjetosDelPaso())
            if (go != null && go.CompareTag(etiqueta)) return go;
        return null;
    }

    static bool Esperando()
    {
        var t = Tutorial();
        return t != null && campoEsperando != null && (bool)campoEsperando.GetValue(t);
    }

    static string Instruccion()
    {
        var t = Tutorial();
        return t != null && t.textoInstruccion != null ? t.textoInstruccion.text : "";
    }

    // Antes de agarrar la caja de arma el paso no puede estar esperando el reloj ni mostrar
    // su texto (el que promete el cargador que se queda para siempre).
    static void MirarElRelojAntesDeTiempo()
    {
        if (Esperando() || Instruccion() == Textos.De("tut_reloj")) relojAntesDeTiempo = true;
    }

    // ---------- El jugador y la escena ----------

    // Lleva al jugador a ese punto del piso, a la altura en que esta. El transform en el acto
    // (el tutorial lo lee en su Update, y con el tiempo congelado la fisica no lo copiaria) y
    // el cuerpo tambien, quieto.
    static void Llevar(PlayerController jugador, Vector3 punto)
    {
        var t = jugador.transform;
        Vector3 destino = new Vector3(punto.x, t.position.y, punto.z);
        var cuerpo = jugador.GetComponent<Rigidbody>();
        if (cuerpo != null)
        {
            cuerpo.position = destino;
            cuerpo.linearVelocity = Vector3.zero;
        }
        t.position = destino;
    }

    // Lo que llama la fisica cuando el jugador toca la caja (el collider esta en la raiz de
    // la caja, con su tag).
    static bool AgarrarConElTrigger(PlayerController jugador, GameObject caja)
    {
        if (metodoTrigger == null || caja == null) return false;
        var colision = caja.GetComponent<Collider>();
        if (colision == null) colision = caja.GetComponentInChildren<Collider>();
        if (colision == null) return false;
        metodoTrigger.Invoke(jugador, new object[] { colision });
        return true;
    }

    static void Congelar(bool si)
    {
        if (si)
        {
            Time.timeScale = 0f;
            congelado = true;
        }
        else if (congelado)
        {
            Time.timeScale = 1f;
            congelado = false;
        }
    }

    static void Recargar(double ahora)
    {
        Congelar(false);
        tmViejo = tm;
        tm = null;
        EditorSceneManager.LoadSceneInPlayMode(Escena, new LoadSceneParameters(LoadSceneMode.Single));
        Pasar(Paso.EsperarEscena2, ahora);
    }

    static int Puntos()
    {
        return Puntaje.instance != null ? Puntaje.instance.contadorKill : -1;
    }

    static int Vivos(List<EnemyController> zombis)
    {
        int n = 0;
        foreach (var z in zombis)
            if (z != null && z.Vivo) n++;
        return n;
    }

    // Los que siguen existiendo; con soloPrendidos, ademas prendidos.
    static int Cuantos(List<EnemyController> zombis, bool soloPrendidos)
    {
        int n = 0;
        foreach (var z in zombis)
            if (z != null && (!soloPrendidos || z.gameObject.activeSelf)) n++;
        return n;
    }

    static void Capturar(string nombre)
    {
        ScreenCapture.CaptureScreenshot(Path.GetFullPath("../Builds/" + nombre + ".png"));
    }

    static Vector3 Plano(Vector3 v)
    {
        return new Vector3(v.x, 0f, v.z);
    }

    static float DelCentro(Vector3 p)
    {
        return Mathf.Max(Mathf.Abs(p.x), Mathf.Abs(p.z));
    }

    static bool Adentro(Vector3 p)
    {
        return DelCentro(p) <= TutorialManager.LimiteDelMapa + Tolerancia;
    }

    static string XZ(Vector3 p)
    {
        return "(" + p.x.ToString("0.00") + "; " + p.z.ToString("0.00") + ")";
    }

    static string SiNo(bool b)
    {
        return b ? "si" : "no";
    }

    // ---------- El informe ----------

    static void Terminar(string error)
    {
        if (terminado) return;
        terminado = true;
        // Lo que cambio el banco: el tiempo congelado de los pasos 2 y 3.
        congelado = false;
        Time.timeScale = 1f;

        // Las paredes.
        bool paso3Adentro = zombisPaso3.Count == 3;
        foreach (var p in zombisPaso3) paso3Adentro &= Adentro(p);
        bool paso4Adentro = cajasPaso4.Count == 2;
        foreach (var p in cajasPaso4) paso4Adentro &= Adentro(p);
        float separacionCajas = cajasPaso4.Count == 2 ? Vector3.Distance(Plano(cajasPaso4[0]), Plano(cajasPaso4[1])) : -1f;
        float masLejos = 0f;
        if (midioPaso2) masLejos = Mathf.Max(masLejos, DelCentro(zombiPaso2));
        foreach (var p in zombisPaso3) masLejos = Mathf.Max(masLejos, DelCentro(p));
        foreach (var p in cajasPaso4) masLejos = Mathf.Max(masLejos, DelCentro(p));
        if (midioPaso5) masLejos = Mathf.Max(masLejos, DelCentro(cajaPaso5));
        bool contraLaPared = midioPaso2 && midioPaso3 && midioPaso4 && midioPaso5
                             && DelCentro(jugadorPaso2) >= ContraLaPared && DelCentro(jugadorPaso3) >= ContraLaPared
                             && DelCentro(jugadorPaso4) >= ContraLaPared && DelCentro(jugadorPaso5) >= ContraLaPared;

        var inf = new StringBuilder();
        inf.AppendLine("Prueba del tutorial (Tutorial.unity): la granada, las cajas y lo que nace contra las paredes");
        inf.AppendLine();
        if (error != null) inf.AppendLine("ERROR: " + error);
        inf.AppendLine("Controles: " + (esMovil ? "telefono (joysticks)" : "teclado y mouse") + "; cadencia de las cajas: "
                       + duracionMejora.ToString("0.0") + " s; limite del mapa: " + TutorialManager.LimiteDelMapa + " m");

        inf.AppendLine("Paso 2, con el jugador en " + XZ(jugadorPaso2) + ": el zombi nacio en " + (midioPaso2 ? XZ(zombiPaso2) : "-"));
        var z3 = new StringBuilder();
        foreach (var p in zombisPaso3) z3.Append((z3.Length > 0 ? ", " : "") + XZ(p));
        inf.AppendLine("Paso 3, con el jugador en " + XZ(jugadorPaso3) + ": los zombis nacieron en " + (z3.Length > 0 ? z3.ToString() : "-"));
        var c4 = new StringBuilder();
        foreach (var p in cajasPaso4) c4.Append((c4.Length > 0 ? ", " : "") + XZ(p));
        inf.AppendLine("Paso 4, con el jugador en " + XZ(jugadorPaso4) + ": las cajas nacieron en " + (c4.Length > 0 ? c4.ToString() : "-")
                       + " (separadas " + separacionCajas.ToString("0.00") + " m)");
        inf.AppendLine("Paso 5, con el jugador en " + XZ(jugadorPaso5) + ": la caja de arma nacio en " + (midioPaso5 ? XZ(cajaPaso5) : "-"));
        inf.AppendLine("Lo mas lejos del centro que nacio algo: " + masLejos.ToString("0.00") + " m (limite " + TutorialManager.LimiteDelMapa
                       + "; las paredes, a unos 49)");

        InformarGranada(inf, "Granada al grupo", alGrupo);
        InformarGranada(inf, "Granada afuera", afuera);

        inf.AppendLine("Cajas: al llegar al paso, el texto de las cajas: " + SiNo(textoCajas) + "; la de balas se agarro " + (agarreBalasPor != "" ? agarreBalasPor : "-"));
        inf.AppendLine("  a los 0,5 s: paso \"" + pasoConCajaDeArma + "\", texto de la caja de arma: " + SiNo(textoCajaArma)
                       + ", esperando el reloj: " + SiNo(esperandoConCajaDeArma) + ", caja de arma en el piso: " + SiNo(cajaArmaEnElPiso)
                       + ", cadencia de la de balas activa: " + SiNo(cadenciaDeBalasActiva));
        inf.AppendLine("  la cadencia de la de balas vencio a los " + segundosDeLaCadenciaDeBalas.ToString("0.00") + " s del juego; 1 s despues: paso \""
                       + pasoAlVencer + "\", texto de la caja de arma: " + SiNo(textoCajaArmaAlVencer) + ", esperando el reloj: "
                       + SiNo(esperandoAlVencer) + ", caja de arma en el piso: " + SiNo(cajaArmaAlVencer));
        inf.AppendLine("  antes de agarrar la de arma se vio el texto del reloj o el paso esperandolo: " + SiNo(relojAntesDeTiempo));
        inf.AppendLine("  la de arma se agarro " + (agarreArmaPor != "" ? agarreArmaPor : "-") + "; a los 0,5 s: paso \"" + pasoConReloj
                       + "\", texto del reloj: " + SiNo(textoReloj) + ", esperando el reloj: " + SiNo(esperandoConReloj)
                       + ", cadencia activa: " + SiNo(cadenciaDeArmaActiva) + ", cargador " + maxBalasConReloj + " (el mejorado es " + cargadorMejorado + ")");
        inf.AppendLine("  fin a los " + segundosHastaElFin.ToString("0.00") + " s del juego de agarrar la de arma: llego " + SiNo(llegoAlFin)
                       + ", panel final " + SiNo(panelFinalPrendido) + ", instruccion apagada " + SiNo(instruccionApagada)
                       + ", " + ClaveCompletado + " = " + tutorialCompletado);
        inf.AppendLine("Errores y excepciones durante la prueba: " + cuantasExcepciones);
        if (cuantasExcepciones > 0) inf.Append(excepciones);
        inf.AppendLine();

        var g = alGrupo;
        var a = afuera;
        bool[] ok =
        {
            error == null && cuantasExcepciones == 0,

            g.tirada && !g.avanzoConLaGranadaViva && g.reconocioLaGranada,
            AvanzoAlExplotar(g),
            g.muertosPorLaGranada >= 1,
            g.muertosPorLaGranada >= 1 && g.cadaveresA03 == g.muertosPorLaGranada && g.cadaveresA25 == 0,
            g.puntosAntes >= 0 && g.muertosPorLaGranada >= 0 && g.puntosDespues == g.puntosAntes + g.muertosPorLaGranada * g.puntosPorZombi
                && g.vivosDespues == 0 && g.zombisVivosDespues == 0,

            a.tirada && !a.avanzoConLaGranadaViva && a.reconocioLaGranada && a.cambioEn >= 0f && a.cambioEn - a.tiradaEn >= a.vuelo - 0.05f,
            AvanzoAlExplotar(a),
            a.vivosEnVuelo == 3 && a.muertosPorLaGranada == 0,
            a.sacadosA03 == 3 && a.vivosDespues == 0 && a.zombisVivosDespues == 0,
            a.puntosAntes >= 0 && a.puntosDespues == a.puntosAntes,
            a.particulasAntes >= 0 && a.particulasDespues > a.particulasAntes,

            pasoConCajaDeArma == "Arma" && textoCajaArma && !esperandoConCajaDeArma,
            cajaArmaEnElPiso && cadenciaDeBalasActiva,
            vencioBalasEn >= 0 && pasoAlVencer == "Arma" && textoCajaArmaAlVencer && !esperandoAlVencer && cajaArmaAlVencer,
            agarreArmaPor != "" && !relojAntesDeTiempo,
            pasoConReloj == "Arma" && textoReloj && esperandoConReloj && cadenciaDeArmaActiva && cargadorAgrandado,
            llegoAlFin && panelFinalPrendido && instruccionApagada && tutorialCompletado == 1,

            contraLaPared,
            midioPaso2 && Adentro(zombiPaso2),
            midioPaso3 && paso3Adentro,
            midioPaso4 && paso4Adentro && separacionCajas >= SeparacionDeLasCajas,
            midioPaso5 && Adentro(cajaPaso5),
        };
        string[] que =
        {
            "el banco llego hasta el final, sin errores ni excepciones",

            "granada al grupo: el tutorial reconoce la granada tirada y el paso no avanza mientras vuela",
            "granada al grupo: el paso avanza cuando explota, enseguida",
            "granada al grupo: cae sobre los zombis del paso y mata al menos uno (antes se iban al tirarla)",
            "granada al grupo: los que mato se terminan de caer solos (siguen a los 0,3 s del cambio de paso y a los 2,5 s ya no estan)",
            "granada al grupo: solo dan puntos los que mato la granada, y no queda ningun zombi vivo",

            "granada afuera: el tutorial la reconoce y el paso no avanza mientras vuela (tarda al menos el vuelo)",
            "granada afuera: el paso avanza cuando explota, enseguida, aunque no mate a nadie",
            "granada afuera: los tres zombis siguen ahi mientras vuela y la granada no mata a ninguno",
            "granada afuera: al cambiar de paso los tres se van y no queda ningun zombi vivo",
            "granada afuera: los que se van no dan puntos",
            "granada afuera: los que se van echan sus particulas de muerte",

            "cajas: agarrar la de balas pasa a pedir la caja de arma, con su texto y sin el del reloj",
            "cajas: con la cadencia de la de balas activa, la caja de arma espera en el piso",
            "cajas: al vencer la cadencia de la de balas el paso sigue pidiendo la caja de arma y la caja sigue ahi",
            "cajas: el texto del reloj no sale antes de agarrar la caja de arma",
            "cajas: agarrar la de arma muestra el texto del reloj, agranda el cargador y prende la cadencia",
            "cajas: al vencer la cadencia de la de arma el tutorial termina (panel final y " + ClaveCompletado + " = 1)",

            "paredes: el jugador estaba contra la pared (a " + ContraLaPared + " m o mas del centro) al nacer lo de los pasos 2 a 5",
            "paredes: el zombi del paso 2 nace adentro (+-" + TutorialManager.LimiteDelMapa + ")",
            "paredes: los tres zombis del paso de la granada nacen adentro (+-" + TutorialManager.LimiteDelMapa + ")",
            "paredes: las dos cajas del paso 4 nacen adentro (+-" + TutorialManager.LimiteDelMapa + ") y separadas",
            "paredes: la caja de arma del paso 5 nace adentro (+-" + TutorialManager.LimiteDelMapa + ")",
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
        PlayerSettings.runInBackground = false;
        AssetDatabase.SaveAssets();
        Debug.Log(inf.ToString());
    }

    static void InformarGranada(StringBuilder inf, string nombre, CasoGranada c)
    {
        inf.AppendLine(nombre + ": el grupo a " + c.distanciaAlGrupo.ToString("0.00") + " m, tirada a " + c.distanciaDelTiro.ToString("0.00")
                       + " m (vuelo " + c.vuelo.ToString("0.00") + " s); exploto a " + (c.exploto >= 0f ? (c.exploto - c.tiradaEn).ToString("0.00") : "-")
                       + " s del juego y el paso cambio a " + (c.cambioEn >= 0f ? (c.cambioEn - c.tiradaEn).ToString("0.00") : "-")
                       + " s (" + (c.cambioReal >= 0 && c.explotoReal >= 0 ? (c.cambioReal - c.explotoReal).ToString("0.00") : "-")
                       + " s reales despues de explotar); la reconocio el tutorial: " + SiNo(c.reconocioLaGranada)
                       + "; avanzo con la granada en el aire: " + SiNo(c.avanzoConLaGranadaViva));
        inf.AppendLine("  vivos mientras volaba: " + c.vivosEnVuelo + " de 3; al cambiar de paso: muertos por la granada " + c.muertosPorLaGranada
                       + ", sacados por el tutorial " + c.sacados + ", vivos " + c.vivosDespues + "; cadaveres a los 0,3 s: " + c.cadaveresA03
                       + ", a los 2,5 s: " + c.cadaveresA25 + "; sacados a los 0,3 s: " + c.sacadosA03 + "; zombis vivos despues: " + c.zombisVivosDespues);
        inf.AppendLine("  puntos: antes " + c.puntosAntes + ", despues " + c.puntosDespues + " (esperados " + c.puntosAntes + " + "
                       + c.muertosPorLaGranada + " x " + c.puntosPorZombi + "); particulas de muerte: antes " + c.particulasAntes
                       + ", despues " + c.particulasDespues);
    }
}
