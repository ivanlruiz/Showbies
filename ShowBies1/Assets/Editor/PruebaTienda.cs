using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;

// Banco de la tienda de mejoras (el prefab Tienda de Menu.unity), en play. Verifica tres
// cosas que cambio la auditoria de la nube sin poder probarlas:
//
//  1. Al abrir la tienda con el boton MEJORAS la fila arranca al principio, con la tarjeta
//     del danio (dano_bala) entera dentro de la lista: antes abria centrada y el danio
//     quedaba afuera. Se mira en tres aperturas: la primera (con la guia prendida, que no
//     tiene que necesitar mover la fila) y dos mas despues de cerrar con VOLVER dejando la
//     fila corrida hasta el final, para que no salga bien por casualidad.
//  2. El toque que frena la fila mientras se desliza no compra (TarjetaMejora.ToqueQueFrena)
//     y un toque con la fila quieta si. Los toques se simulan como los manda el modulo de
//     entrada del EventSystem: el raycast de lo que hay debajo del dedo, el pointerDown por
//     la jerarquia, el initializePotentialDrag al ScrollRect (que es lo que la frena) y, al
//     levantar, el pointerUp y el click solo si el toque sigue siendo elegible. Tampoco compra
//     un toque que empieza con la tienda recien abierta (TiendaMejoras.SinComprarAlAbrir): el
//     dedo que iba a cerrar el cartel de la recompensa diaria y cae sobre una tarjeta cuando la
//     tienda se abre sola (superauditoria del 29/9).
//  3. La guia de la primera compra (GuiaPrimeraCompra): con un progreso que nunca compro, la
//     flecha senala desde abajo el boton de comprar del danio; al comprarlo pasa a A JUGAR,
//     desde su izquierda. Se mide a donde apunta, que la flecha y el cartel queden dentro de
//     la pantalla y que en A JUGAR no pisen los botones de comprar, y se sacan dos capturas.
//
// Estado que prepara, en modo edicion y guardado en disco antes de entrar en play (al entrar
// se recarga el dominio y Progreso vuelve a leer el archivo): Progreso.ReiniciarTodo (ninguna
// partida, mejor oleada 0, todas las mejoras en 0: la guia se prende y la recompensa diaria
// no sale, porque espera a la primera partida terminada) y 1.000.000 de monedas. Al terminar
// quedan compradas dano_bala en 1 y un nivel de la tarjeta que se toco con la fila quieta.
// Escribe el progreso.json REAL del editor: RespaldoDelBanco lo copia antes y lo devuelve
// despues, con el editor ya de vuelta en modo edicion (al salir de play Progreso guarda). No
// toca PlayerPrefs ni EditorPrefs.
//
// Escribe Builds/prueba_tienda.txt y las capturas Builds/tienda_guia_comprar.png,
// Builds/tienda_guia_jugar.png y Builds/tienda_apertura.png.
//
// OJO: entrar y salir de play hace que Unity vuelva a serializar ProjectSettings (ver la
// trampa en CLAUDE.md): despues de correrlo, revertir lo que no se toco a proposito.
[InitializeOnLoad]
public static class PruebaTienda
{
    const string Clave = "ShowBies.PruebaTienda";
    const string Ruta = "../Builds/prueba_tienda.txt";
    const string Escena = "Assets/Escenas/Menu.unity";
    const string IdDano = "dano_bala";

    const double MonedasDePrueba = 1000000;
    const double TopePorPaso = 10.0;
    const double TopeTotal = 120.0;

    // El empujon a la fila, en unidades del canvas por segundo hacia el final. Con la
    // desaceleracion del ScrollRect (0,135 por segundo) tarda 1,7 s en bajar de 60, que es
    // desde donde un toque ya compra (TiendaMejoras.VelocidadParaFrenar), y recorre unas 750 u
    // de las ~1.170 que la fila tiene escondidas en 16:9.
    const float VelocidadDelEmpujon = 1500f;
    const int IntentosDeEmpujon = 3;

    // Cuanto se puede correr la flecha de donde la pone la guia, en unidades del panel.
    const float ToleranciaGuia = 4f;

    enum Paso
    {
        Esperando,          // el menu termina de cargar; toca MEJORAS
        PrimeraAbierta,     // mide la fila a los 0,3 s, antes de que aparezca la guia
        GuiaComprar,        // la guia ya senala el danio: la mide y la fotografia
        ApoyarEnElDano,     // apoya el dedo en el boton del danio, con la fila quieta
        SoltarElDano,
        GuiaJugar,          // la guia paso a A JUGAR: la mide y la fotografia
        CerrarPrimera,      // corre la fila al final y toca VOLVER
        AbrirSegunda,       // toca MEJORAS
        SegundaAbierta,     // mide la fila y fotografia la tienda
        Empujar,            // le da velocidad a la fila
        Frenar,             // la toca mientras se desliza
        SoltarFrenando,
        TocarQuieta,        // mide que no compro y toca la misma tarjeta con la fila quieta
        SoltarQuieta,
        MedirCompra,        // mide que compro, corre la fila al final y toca VOLVER
        AbrirTercera,       // toca MEJORAS y, en el mismo cuadro, apoya el dedo en DANIO
        SoltarRecienAbierta,
        TerceraAbierta,     // mide que no compro y mide la fila
        CerrarTercera,      // toca VOLVER
        Listo,
    }

    // Lo que se mide de la fila en cada apertura.
    class Apertura
    {
        public string nombre;
        public float antes = -1f;           // donde estaba la fila al cerrar (normalizada); -1 si no se cerro antes
        public bool abierta, scrollPrendido, listaEnPantalla;
        public float posicion = -1f;        // horizontalNormalizedPosition del ScrollRect
        public float anchoFila, anchoVista;
        public float izquierda, derecha;    // los bordes de la tarjeta del danio, desde el borde izquierdo de la lista
    }

    // Lo que se mide de la guia en cada uno de sus dos pasos.
    class MedidaGuia
    {
        public bool falta;                  // no se encontro la flecha o el cartel de la guia
        public bool activa;
        public float alfa = -1f;
        public float giro;                  // grados de la flecha: 90 apunta arriba, 0 a la derecha
        public string texto = "", textoEsperado = "";
        public float corrimiento;           // del centro del objetivo, a lo largo del borde que senala
        public float hueco;                 // del borde del objetivo al centro de la flecha
        public bool flechaEnPantalla, cartelEnPantalla, cartelDelLadoBien;
        public string pisa = "";            // los botones de comprar que pisan la flecha o el cartel
        public Rect flecha, cartel, pantalla;
    }

    static Paso paso;
    static double inicio, pasoDesde;
    static int cuadroDelPaso;
    static bool terminado;

    // Lo del juego que se usa en todos los pasos.
    static TiendaMejoras tienda;
    static TarjetaMejora tarjetaDano, tarjetaTocada;
    static RectTransform raizGuia, flechaGuia;
    static CanvasGroup grupoGuia;
    static TMP_Text cartelGuia;

    // El dedo que esta apoyado, como lo guarda el modulo de entrada entre el apoyar y el levantar.
    static PointerEventData dedo;
    static GameObject debajoDelDedo;
    static readonly List<RaycastResult> resultados = new List<RaycastResult>();
    static readonly Vector3[] esquinas = new Vector3[4];

    // Lo medido.
    static double monedasAlEmpezar;
    static bool nuncaComproAlEmpezar, diariaOcupada;
    static int partidasAlEmpezar;
    static readonly bool[] abrio = new bool[3];
    static readonly bool[] cerro = new bool[3];
    static readonly float[] finalAlCerrar = { -1f, -1f };
    static Apertura primera, primeraConGuia, segunda, tercera;
    static bool guiaAntesDeMedir;
    static MedidaGuia guiaComprar, guiaJugar;
    static bool guiaEscondidaAlCerrar, guiaEnLaSegunda = true;
    // La compra que ensenia la guia.
    static bool filaMovianteAntesDeLaGuia, clicGuia;
    static double monedasAntesGuia, monedasDespuesGuia, precioGuia;
    static int nivelAntesGuia = -1, nivelDespuesGuia = -1;
    static string caidaGuia = "";
    // El toque que frena.
    static bool scrollPrendido, filaDeslizandose, elegibleTrasFrenar = true, clicFrenando = true, empujonEnLaMismaVuelta;
    static int intentosDeEmpujon;
    static float posAlEmpujar, movioAntesDelToque, velocidadAlTocar, velocidadTrasApoyar = float.NaN, velocidadAntesDeSoltar = float.NaN;
    static float posAlApoyar, posDespuesDeFrenar;
    static string idTocada = "", caidaFrenar = "", manejadores = "";
    static double monedasAntesFrenar, monedasDespuesFrenar;
    static int nivelAntesFrenar = -1, nivelDespuesFrenar = -2;
    // El toque con la fila quieta.
    static bool filaQuietaAlTocar, elegibleQuieta, clicQuieta;
    static float velocidadQuieta = float.NaN;
    static double monedasAntesQuieta, monedasDespuesQuieta, precioQuieta;
    static int nivelAntesQuieta = -1, nivelDespuesQuieta = -1;
    static string caidaQuieta = "";
    // El toque con la tienda recien abierta.
    static bool recienAbiertaAlTocar, elegibleRecienAbierta = true, clicRecienAbierta = true;
    static double monedasAntesRecien, monedasDespuesRecien;
    static int nivelAntesRecien = -1, nivelDespuesRecien = -2;
    static string caidaRecien = "";
    static readonly List<string> capturas = new List<string>();

    // Los errores y excepciones que salten mientras corre. No se ponen en cero al empezar:
    // la recarga de dominio al entrar en play ya los deja en cero, y asi cuentan tambien los
    // que salten mientras carga el menu.
    static readonly StringBuilder excepciones = new StringBuilder();
    static int cuantasExcepciones;

    static PruebaTienda()
    {
        EditorApplication.update += Tick;
        Application.logMessageReceived += AnotarExcepcion;
        EditorApplication.playModeStateChanged += AlCambiarDeModo;
    }

    static void AnotarExcepcion(string mensaje, string pila, LogType tipo)
    {
        if (!SessionState.GetBool(Clave, false)) return;
        if (tipo != LogType.Exception && tipo != LogType.Error) return;
        cuantasExcepciones++;
        if (cuantasExcepciones <= 3) excepciones.AppendLine("  " + mensaje + System.Environment.NewLine + pila);
    }

    // Al volver a modo edicion, Progreso se queda en memoria con el progreso del banco (salir de
    // play no recarga el dominio): la herramienta de editor que lo use despues guardaria eso
    // encima del progreso que RespaldoDelBanco devuelve a su lugar. Se lo descarta para que
    // la proxima lectura sea la del disco.
    static void AlCambiarDeModo(PlayModeStateChange cambio)
    {
        if (cambio != PlayModeStateChange.EnteredEditMode || !SessionState.GetBool(Clave + ".olvidarProgreso", false)) return;
        SessionState.SetBool(Clave + ".olvidarProgreso", false);
        FieldInfo campo = typeof(Progreso).GetField("datos", BindingFlags.NonPublic | BindingFlags.Static);
        if (campo != null) campo.SetValue(null, null);
        else Debug.LogWarning("PruebaTienda: no encontre Progreso.datos; el progreso del banco sigue en memoria hasta la proxima entrada a play.");
    }

    [MenuItem("ShowBies/Pruebas/Tienda de mejoras (play)")]
    // Publico para poder correrlo por codigo (ver la trampa del registro de menus).
    public static void Arrancar()
    {
        if (!RespaldoDelBanco.PuedeArrancar("PruebaTienda")) return;
        RespaldoDelBanco.Guardar("PruebaTienda");
        PlayerSettings.runInBackground = true;

        // El progreso: de cero (ninguna compra ni partida: la guia de la primera compra se
        // prende y la recompensa diaria no tapa el menu) y con monedas de sobra. Los dos
        // guardan en el acto, que es lo que lee el juego al entrar en play.
        Progreso.ReiniciarTodo();
        Progreso.DepurarFijarMonedas(MonedasDePrueba);

        EditorSceneManager.OpenScene(Escena);
        SessionState.SetBool(Clave, true);
        SessionState.SetBool(Clave + ".empezo", false);
        EditorApplication.EnterPlaymode();
    }

    static void Tick()
    {
        if (!SessionState.GetBool(Clave, false)) return;
        if (!EditorApplication.isPlaying) return;
        if (!RespaldoDelBanco.SigueArmado("PruebaTienda", Clave)) return;

        double ahora = EditorApplication.timeSinceStartup;
        if (!SessionState.GetBool(Clave + ".empezo", false))
        {
            SessionState.SetBool(Clave + ".empezo", true);
            Reiniciar(ahora);
            return;
        }

        if (ahora - inicio > TopeTotal)
        {
            Terminar("se paso del tope total de " + TopeTotal + " s, en el paso " + paso);
            return;
        }
        if (ahora - pasoDesde > TopePorPaso)
        {
            Terminar("se trabo en el paso " + paso + " (mas de " + TopePorPaso + " s)" + DetalleDelPaso());
            return;
        }

        try
        {
            Avanzar(ahora);
        }
        catch (System.Exception e)
        {
            Terminar("excepcion del banco en el paso " + paso + ": " + e);
        }
    }

    static void Reiniciar(double ahora)
    {
        inicio = ahora;
        terminado = false;
        tienda = null;
        tarjetaDano = tarjetaTocada = null;
        raizGuia = flechaGuia = null;
        grupoGuia = null;
        cartelGuia = null;
        dedo = null;
        debajoDelDedo = null;
        capturas.Clear();
        Pasar(Paso.Esperando, ahora);
    }

    static void Avanzar(double ahora)
    {
        switch (paso)
        {
            case Paso.Esperando:
            {
                if (tienda == null) tienda = Object.FindAnyObjectByType<TiendaMejoras>(FindObjectsInactive.Include);
                if (tienda == null || EventSystem.current == null || Espero(ahora, 2.0)) return;
                string falta = QueFalta();
                if (falta != null) { Terminar(falta); return; }

                monedasAlEmpezar = Progreso.Monedas;
                nuncaComproAlEmpezar = PrimeraVez.NuncaCompro;
                partidasAlEmpezar = Progreso.PartidasTerminadas;
                diariaOcupada = VentanaRecompensaDiaria.Ocupada;
                if (monedasAlEmpezar < MonedasDePrueba - 1 || !nuncaComproAlEmpezar)
                {
                    Terminar("el progreso preparado no llego a play: " + monedasAlEmpezar.ToString("0") + " monedas, nunca compro " + nuncaComproAlEmpezar);
                    return;
                }
                if (tienda.Abierta) { Terminar("la tienda ya estaba abierta al cargar el menu"); return; }

                abrio[0] = Abrir();
                if (!abrio[0]) { Terminar("el boton MEJORAS no abrio la tienda"); return; }
                Pasar(Paso.PrimeraAbierta, ahora);
                return;
            }

            case Paso.PrimeraAbierta:
                // Antes de la demora de la guia (0,9 s): lo que dejo Abrir, sin que nadie mas lo toque.
                if (Espero(ahora, 0.3)) return;
                primera = MedirApertura("Primera apertura, a los 0,3 s", -1f);
                guiaAntesDeMedir = raizGuia != null && raizGuia.gameObject.activeSelf;
                Pasar(Paso.GuiaComprar, ahora);
                return;

            case Paso.GuiaComprar:
                // La guia espera 0,9 s desde que se abre la tienda; las tarjetas terminan de
                // entrar a los 0,72 s. Aca van 1,6 s.
                if (Espero(ahora, 1.3)) return;
                primeraConGuia = MedirApertura("Primera apertura, con la guia ya a la vista", -1f);
                guiaComprar = MedirGuia(true);
                Capturar("tienda_guia_comprar");
                Pasar(Paso.ApoyarEnElDano, ahora);
                return;

            case Paso.ApoyarEnElDano:
                // La captura se escribe al final del cuadro: se espera antes de comprar.
                if (Espero(ahora, 0.4)) return;
                filaMovianteAntesDeLaGuia = tienda.FilaEnMovimiento;
                monedasAntesGuia = Progreso.Monedas;
                nivelAntesGuia = Progreso.Nivel(IdDano);
                precioGuia = tarjetaDano.Mejora.Precio(nivelAntesGuia);
                Apoyar(tarjetaDano.boton.gameObject, out caidaGuia);
                Pasar(Paso.SoltarElDano, ahora);
                return;

            case Paso.SoltarElDano:
                if (Espero(ahora, 0.1)) return;
                clicGuia = Levantar();
                Pasar(Paso.GuiaJugar, ahora);
                return;

            case Paso.GuiaJugar:
                // Ya termino la sacudida de la compra (0,15 s) y la guia paso a A JUGAR en el
                // cuadro siguiente a la compra.
                if (Espero(ahora, 0.8)) return;
                monedasDespuesGuia = Progreso.Monedas;
                nivelDespuesGuia = Progreso.Nivel(IdDano);
                guiaJugar = MedirGuia(false);
                Capturar("tienda_guia_jugar");
                Pasar(Paso.CerrarPrimera, ahora);
                return;

            case Paso.CerrarPrimera:
                if (Espero(ahora, 0.4)) return;
                finalAlCerrar[0] = LlevarLaFilaAlFinal();
                cerro[0] = Cerrar();
                Pasar(Paso.AbrirSegunda, ahora);
                return;

            case Paso.AbrirSegunda:
                if (Espero(ahora, 0.4)) return;
                // La guia se esconde sola con la tienda cerrada (su LateUpdate).
                guiaEscondidaAlCerrar = raizGuia != null && !raizGuia.gameObject.activeSelf;
                abrio[1] = Abrir();
                if (!abrio[1]) { Terminar("el boton MEJORAS no abrio la tienda la segunda vez"); return; }
                Pasar(Paso.SegundaAbierta, ahora);
                return;

            case Paso.SegundaAbierta:
                // Con las tarjetas ya adentro, para que la captura muestre la fila entera.
                if (Espero(ahora, 1.0)) return;
                segunda = MedirApertura("Segunda apertura", finalAlCerrar[0]);
                Capturar("tienda_apertura");
                Pasar(Paso.Empujar, ahora);
                return;

            case Paso.Empujar:
                // Aca ya paso la demora de la guia (1,4 s desde que se abrio): tiene que seguir
                // escondida, porque ya se compro.
                if (Espero(ahora, 0.4)) return;
                guiaEnLaSegunda = raizGuia != null && raizGuia.gameObject.activeSelf;
                scrollPrendido = tienda.scroll.enabled;
                Empujar(ahora);
                return;

            case Paso.Frenar:
            {
                if (Espero(ahora, 0.0)) return;   // dos cuadros: que el ScrollRect la mueva un poco
                bool enMovimiento = tienda.FilaEnMovimiento;
                if (!enMovimiento && intentosDeEmpujon < IntentosDeEmpujon)
                {
                    // Paso demasiado entre dos vueltas del editor y la fila ya freno (o llego al
                    // final): otra vez desde el principio.
                    tienda.scroll.StopMovement();
                    tienda.scroll.horizontalNormalizedPosition = 0f;
                    Empujar(ahora);
                    return;
                }
                if (!enMovimiento)
                {
                    // Ultimo recurso, con un editor que da vueltas muy de a poco: el empujon y el
                    // toque en la misma vuelta. Prueba igual lo que decide el toque, pero sin ver
                    // a la fila moverse antes.
                    tienda.scroll.StopMovement();
                    tienda.scroll.horizontalNormalizedPosition = 0f;
                    posAlEmpujar = tienda.contenedor.anchoredPosition.x;
                    tienda.scroll.velocity = new Vector2(-VelocidadDelEmpujon, 0f);
                    empujonEnLaMismaVuelta = true;
                    enMovimiento = tienda.FilaEnMovimiento;
                }
                filaDeslizandose = enMovimiento;
                velocidadAlTocar = tienda.scroll.velocity.x;
                movioAntesDelToque = tienda.contenedor.anchoredPosition.x - posAlEmpujar;
                tarjetaTocada = TarjetaEnElMedio();
                if (tarjetaTocada == null) { Terminar("no hay ninguna tarjeta comprable a la vista para tocar"); return; }
                idTocada = tarjetaTocada.Mejora.id;
                monedasAntesFrenar = Progreso.Monedas;
                nivelAntesFrenar = Progreso.Nivel(idTocada);
                manejadores = Manejadores(tarjetaTocada.boton.gameObject);
                Apoyar(tarjetaTocada.boton.gameObject, out caidaFrenar);
                // En el mismo cuadro del toque: ToqueQueFrena le saco el click y el ScrollRect
                // puso su velocidad en cero (initializePotentialDrag).
                elegibleTrasFrenar = dedo.eligibleForClick;
                velocidadTrasApoyar = tienda.scroll.velocity.x;
                posAlApoyar = tienda.contenedor.anchoredPosition.x;
                Pasar(Paso.SoltarFrenando, ahora);
                return;
            }

            case Paso.SoltarFrenando:
                if (Espero(ahora, 0.1)) return;
                velocidadAntesDeSoltar = tienda.scroll.velocity.x;
                clicFrenando = Levantar();
                Pasar(Paso.TocarQuieta, ahora);
                return;

            case Paso.TocarQuieta:
                if (Espero(ahora, 0.5)) return;
                posDespuesDeFrenar = tienda.contenedor.anchoredPosition.x;
                monedasDespuesFrenar = Progreso.Monedas;
                nivelDespuesFrenar = Progreso.Nivel(idTocada);

                // Ahora el mismo toque, sobre la misma tarjeta, con la fila quieta.
                velocidadQuieta = tienda.scroll.velocity.x;
                filaQuietaAlTocar = !tienda.FilaEnMovimiento;
                monedasAntesQuieta = Progreso.Monedas;
                nivelAntesQuieta = Progreso.Nivel(idTocada);
                precioQuieta = tarjetaTocada.Mejora.Precio(nivelAntesQuieta);
                Apoyar(tarjetaTocada.boton.gameObject, out caidaQuieta);
                elegibleQuieta = dedo.eligibleForClick;
                Pasar(Paso.SoltarQuieta, ahora);
                return;

            case Paso.SoltarQuieta:
                if (Espero(ahora, 0.1)) return;
                clicQuieta = Levantar();
                Pasar(Paso.MedirCompra, ahora);
                return;

            case Paso.MedirCompra:
                if (Espero(ahora, 0.8)) return;
                monedasDespuesQuieta = Progreso.Monedas;
                nivelDespuesQuieta = Progreso.Nivel(idTocada);
                finalAlCerrar[1] = LlevarLaFilaAlFinal();
                cerro[1] = Cerrar();
                Pasar(Paso.AbrirTercera, ahora);
                return;

            case Paso.AbrirTercera:
                if (Espero(ahora, 0.4)) return;
                abrio[2] = Abrir();
                if (!abrio[2]) { Terminar("el boton MEJORAS no abrio la tienda la tercera vez"); return; }
                // Como el dedo que cae sobre la tienda que se abre sola despues de la diaria.
                recienAbiertaAlTocar = tienda.RecienAbierta;
                monedasAntesRecien = Progreso.Monedas;
                nivelAntesRecien = Progreso.Nivel(IdDano);
                Apoyar(tarjetaDano.boton.gameObject, out caidaRecien);
                elegibleRecienAbierta = dedo.eligibleForClick;
                Pasar(Paso.SoltarRecienAbierta, ahora);
                return;

            case Paso.SoltarRecienAbierta:
                if (Espero(ahora, 0.1)) return;
                clicRecienAbierta = Levantar();
                Pasar(Paso.TerceraAbierta, ahora);
                return;

            case Paso.TerceraAbierta:
                if (Espero(ahora, 0.5)) return;
                monedasDespuesRecien = Progreso.Monedas;
                nivelDespuesRecien = Progreso.Nivel(IdDano);
                tercera = MedirApertura("Tercera apertura", finalAlCerrar[1]);
                Pasar(Paso.CerrarTercera, ahora);
                return;

            case Paso.CerrarTercera:
                if (Espero(ahora, 0.3)) return;
                cerro[2] = Cerrar();
                Pasar(Paso.Listo, ahora);
                return;

            case Paso.Listo:
                if (Espero(ahora, 0.3)) return;
                Terminar(null);
                return;
        }
    }

    static void Pasar(Paso siguiente, double ahora)
    {
        paso = siguiente;
        pasoDesde = ahora;
        cuadroDelPaso = Time.frameCount;
    }

    // Si todavia no pasaron esos segundos reales, o dos cuadros del juego, desde que empezo el paso.
    static bool Espero(double ahora, double segundos)
    {
        return ahora - pasoDesde < segundos || Time.frameCount - cuadroDelPaso < 2;
    }

    static string DetalleDelPaso()
    {
        if (paso != Paso.Esperando) return "";
        if (tienda == null) return ": no aparecio la TiendaMejoras en Menu";
        if (EventSystem.current == null) return ": no hay EventSystem";
        return ": el juego no avanza cuadros (ver la trampa de Run In Background en CLAUDE.md)";
    }

    // Lo que el banco necesita de la tienda. Tambien busca la tarjeta del danio y las piezas de
    // la guia (la guia que falta no para el banco: fallan sus chequeos).
    static string QueFalta()
    {
        if (tienda.botonAbrir == null) return "la tienda no tiene el boton MEJORAS (botonAbrir)";
        if (tienda.botonVolver == null) return "la tienda no tiene el boton VOLVER (botonVolver)";
        if (tienda.botonJugar == null) return "la tienda no tiene el boton A JUGAR (botonJugar)";
        if (tienda.panel == null || tienda.scroll == null || tienda.viewport == null || tienda.contenedor == null)
            return "a la tienda le falta el panel, el ScrollRect, la lista o la fila";
        tarjetaDano = BuscarTarjeta(IdDano);
        if (tarjetaDano == null) return "no hay tarjeta de " + IdDano + " en la tienda";
        if (tarjetaDano.boton == null || tarjetaDano.raizBoton == null) return "la tarjeta del danio no tiene su boton";

        // La guia arma su flecha en su Start, colgando del panel.
        raizGuia = tienda.panel.transform.Find("GuiaPrimeraCompra") as RectTransform;
        if (raizGuia != null)
        {
            flechaGuia = raizGuia.Find("Flecha") as RectTransform;
            grupoGuia = raizGuia.GetComponent<CanvasGroup>();
            Transform cartel = raizGuia.Find("Cartel");
            cartelGuia = cartel != null ? cartel.GetComponent<TMP_Text>() : null;
        }
        return null;
    }

    static TarjetaMejora BuscarTarjeta(string id)
    {
        foreach (TarjetaMejora t in tienda.Tarjetas)
            if (t != null && t.Mejora != null && t.Mejora.id == id) return t;
        return null;
    }

    // La tarjeta comprable cuyo boton esta mas cerca del medio de la lista: donde toca el
    // dedo que frena una fila que pasa.
    static TarjetaMejora TarjetaEnElMedio()
    {
        Rect vista = tienda.viewport.rect;
        TarjetaMejora mejor = null;
        float mejorDistancia = float.MaxValue;
        foreach (TarjetaMejora t in tienda.Tarjetas)
        {
            if (t == null || t.Mejora == null || t.boton == null || t.Estado != EstadoMejora.Comprable) continue;
            var rt = (RectTransform)t.boton.transform;
            float x = tienda.viewport.InverseTransformPoint(rt.TransformPoint(rt.rect.center)).x;
            if (x < vista.xMin + 100f || x > vista.xMax - 100f) continue;
            float distancia = Mathf.Abs(x - vista.center.x);
            if (distancia < mejorDistancia)
            {
                mejorDistancia = distancia;
                mejor = t;
            }
        }
        return mejor;
    }

    static void Empujar(double ahora)
    {
        posAlEmpujar = tienda.contenedor.anchoredPosition.x;
        tienda.scroll.velocity = new Vector2(-VelocidadDelEmpujon, 0f);
        intentosDeEmpujon++;
        Pasar(Paso.Frenar, ahora);
    }

    // Como la abre y la cierra el jugador: un toque sobre MEJORAS y sobre VOLVER.
    static bool Abrir()
    {
        Tocar(tienda.botonAbrir.gameObject);
        return tienda.Abierta && tienda.panel.activeSelf;
    }

    static bool Cerrar()
    {
        Tocar(tienda.botonVolver.gameObject);
        return !tienda.Abierta && !tienda.panel.activeSelf;
    }

    // Deja la fila en el final, para ver despues que la apertura siguiente la devuelve al
    // principio. El ScrollRect conserva donde quedo su contenido al apagarse el panel.
    static float LlevarLaFilaAlFinal()
    {
        tienda.scroll.StopMovement();
        tienda.scroll.horizontalNormalizedPosition = 1f;
        return tienda.scroll.horizontalNormalizedPosition;
    }

    static void Tocar(GameObject boton)
    {
        string caida;
        Apoyar(boton, out caida);
        Levantar();
    }

    // Un dedo que se apoya en el medio del boton, con los mismos pasos que el modulo de
    // entrada del EventSystem al apoyar (PointerInputModule/StandaloneInputModule): lo que hay
    // debajo sale de un raycast; el pointerDown sube por la jerarquia hasta el primero que lo
    // atiende (ahi estan el Button, BotonJugoso y ToqueQueFrena, que decide si el toque puede
    // comprar); y el initializePotentialDrag va al que arrastra, que es el ScrollRect de la
    // fila: es lo que la frena. Si el raycast no cae dentro del boton (el editor podria no
    // darle al raycast el tamanio de la Game view), el toque va al boton directo, y 'caida' lo
    // dice.
    static void Apoyar(GameObject boton, out string caida)
    {
        var rt = (RectTransform)boton.transform;
        Canvas canvas = boton.GetComponentInParent<Canvas>();
        if (canvas != null) canvas = canvas.rootCanvas;
        Camera camara = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;

        dedo = new PointerEventData(EventSystem.current)
        {
            button = PointerEventData.InputButton.Left,
            position = RectTransformUtility.WorldToScreenPoint(camara, rt.TransformPoint(rt.rect.center)),
        };

        GameObject debajo = boton;
        caida = "nada: se toco el boton directo";
        resultados.Clear();
        EventSystem.current.RaycastAll(dedo, resultados);
        if (resultados.Count > 0)
        {
            GameObject tocado = resultados[0].gameObject;
            bool dentro = tocado != null && tocado.transform.IsChildOf(boton.transform);
            caida = Camino(tocado) + (dentro ? "" : " (fuera del boton: se toco el boton directo)");
            if (dentro)
            {
                debajo = tocado;
                dedo.pointerCurrentRaycast = resultados[0];
            }
        }

        dedo.eligibleForClick = true;
        dedo.delta = Vector2.zero;
        dedo.dragging = false;
        dedo.useDragThreshold = true;
        dedo.pressPosition = dedo.position;
        dedo.pointerPressRaycast = dedo.pointerCurrentRaycast;

        GameObject presionado = ExecuteEvents.ExecuteHierarchy(debajo, dedo, ExecuteEvents.pointerDownHandler);
        if (presionado == null) presionado = ExecuteEvents.GetEventHandler<IPointerClickHandler>(debajo);
        dedo.pointerPress = presionado;
        dedo.rawPointerPress = debajo;
        dedo.clickTime = Time.unscaledTime;
        dedo.pointerDrag = ExecuteEvents.GetEventHandler<IDragHandler>(debajo);
        if (dedo.pointerDrag != null) ExecuteEvents.Execute(dedo.pointerDrag, dedo, ExecuteEvents.initializePotentialDrag);
        debajoDelDedo = debajo;
    }

    // Levanta el dedo sin haberlo arrastrado: el pointerUp al que se apreto y el click solo si
    // el que atiende el click es el mismo y el toque sigue siendo elegible. Devuelve si hubo click.
    static bool Levantar()
    {
        if (dedo == null) return false;
        if (dedo.pointerPress != null) ExecuteEvents.Execute(dedo.pointerPress, dedo, ExecuteEvents.pointerUpHandler);
        GameObject conClic = debajoDelDedo != null ? ExecuteEvents.GetEventHandler<IPointerClickHandler>(debajoDelDedo) : null;
        bool clic = dedo.pointerPress != null && dedo.pointerPress == conClic && dedo.eligibleForClick;
        if (clic) ExecuteEvents.Execute(dedo.pointerPress, dedo, ExecuteEvents.pointerClickHandler);
        dedo.eligibleForClick = false;
        dedo.pointerPress = null;
        dedo.rawPointerPress = null;
        dedo.pointerDrag = null;
        return clic;
    }

    static string Manejadores(GameObject go)
    {
        var nombres = new StringBuilder();
        foreach (IPointerDownHandler c in go.GetComponents<IPointerDownHandler>())
            nombres.Append(nombres.Length > 0 ? ", " : "").Append(c.GetType().Name);
        return nombres.ToString();
    }

    static string Camino(GameObject go)
    {
        if (go == null) return "(nada)";
        string camino = go.name;
        Transform t = go.transform.parent;
        for (int i = 0; i < 2 && t != null; i++, t = t.parent) camino = t.name + "/" + camino;
        return camino;
    }

    static void Capturar(string nombre)
    {
        string ruta = Path.GetFullPath("../Builds/" + nombre + ".png");
        Directory.CreateDirectory(Path.GetDirectoryName(ruta));
        ScreenCapture.CaptureScreenshot(ruta);
        capturas.Add(nombre + ".png");
    }

    // La fila y la tarjeta del danio: donde arranco el ScrollRect y los bordes de la tarjeta en
    // la lista (el viewport, que recorta con su RectMask2D). La raiz de la tarjeta la ubica el
    // layout y no se anima: lo que entra animado es su contenido.
    static Apertura MedirApertura(string nombre, float antes)
    {
        var a = new Apertura { nombre = nombre, antes = antes };
        a.abierta = tienda.Abierta;
        a.scrollPrendido = tienda.scroll.enabled;
        a.posicion = tienda.scroll.horizontalNormalizedPosition;
        Rect vista = tienda.viewport.rect;
        a.anchoFila = tienda.contenedor.rect.width;
        a.anchoVista = vista.width;
        ((RectTransform)tarjetaDano.transform).GetWorldCorners(esquinas);
        a.izquierda = tienda.viewport.InverseTransformPoint(esquinas[0]).x - vista.xMin;
        a.derecha = tienda.viewport.InverseTransformPoint(esquinas[2]).x - vista.xMin;
        a.listaEnPantalla = Dentro(EnElMundo(tienda.viewport), Pantalla());
        return a;
    }

    // Al principio (si la fila se desplaza) y con la tarjeta del danio entera en la lista, que
    // esta dentro de la pantalla.
    static bool Bien(Apertura a)
    {
        return a != null && a.abierta
            && (!a.scrollPrendido || a.posicion <= 0.001f)
            && a.izquierda >= -1f && a.derecha <= a.anchoVista + 1f
            && a.listaEnPantalla;
    }

    // La guia, medida contra lo que senala: el boton de comprar del danio (desde abajo) o
    // A JUGAR (desde la izquierda). La posicion es la de verdad (el centro de la flecha pasado
    // al espacio del panel), no la que la guia cree que le puso.
    static MedidaGuia MedirGuia(bool comprar)
    {
        var m = new MedidaGuia();
        if (raizGuia == null || flechaGuia == null || cartelGuia == null)
        {
            m.falta = true;
            return m;
        }
        m.activa = raizGuia.gameObject.activeInHierarchy;
        m.alfa = grupoGuia != null ? grupoGuia.alpha : 1f;
        m.giro = Mathf.DeltaAngle(0f, flechaGuia.localEulerAngles.z);
        m.texto = cartelGuia.text;
        m.textoEsperado = Textos.De(comprar ? "guia_compra" : "guia_a_jugar");

        RectTransform objetivo = comprar ? tarjetaDano.raizBoton : (RectTransform)tienda.botonJugar.transform;
        var panel = (RectTransform)raizGuia.parent;
        Rect r = EnElEspacioDe(panel, objetivo);
        Vector3 centro = panel.InverseTransformPoint(raizGuia.position);
        if (comprar)
        {
            m.corrimiento = centro.x - r.center.x;
            m.hueco = r.yMin - centro.y;
        }
        else
        {
            m.corrimiento = centro.y - r.center.y;
            m.hueco = r.xMin - centro.x;
        }

        m.pantalla = Pantalla();
        m.flecha = EnElMundo(flechaGuia);
        m.cartel = TextoEnElMundo(cartelGuia);
        m.flechaEnPantalla = Dentro(m.flecha, m.pantalla);
        m.cartelEnPantalla = Dentro(m.cartel, m.pantalla);
        // Comprando, el cartel va a la derecha de la flecha; en A JUGAR, a su izquierda.
        m.cartelDelLadoBien = comprar ? m.cartel.center.x > m.flecha.center.x : m.cartel.center.x < m.flecha.center.x;

        // Los botones de comprar que se ven en la lista y que la flecha o el cartel pisan.
        Rect lista = EnElMundo(tienda.viewport);
        var pisa = new StringBuilder();
        foreach (TarjetaMejora t in tienda.Tarjetas)
        {
            if (t == null || t.Mejora == null || t.raizBoton == null) continue;
            Rect b = EnElMundo(t.raizBoton);
            if (!b.Overlaps(lista)) continue;
            Rect visible = Rect.MinMaxRect(Mathf.Max(b.xMin, lista.xMin), Mathf.Max(b.yMin, lista.yMin),
                                           Mathf.Min(b.xMax, lista.xMax), Mathf.Min(b.yMax, lista.yMax));
            if (visible.Overlaps(m.flecha) || visible.Overlaps(m.cartel)) pisa.Append(t.Mejora.id).Append(' ');
        }
        m.pisa = pisa.ToString().Trim();
        return m;
    }

    static bool ApuntaBien(MedidaGuia m, float giroEsperado)
    {
        return m != null && !m.falta && m.activa && m.alfa > 0.99f
            && Mathf.Abs(Mathf.DeltaAngle(m.giro, giroEsperado)) < 1f
            && m.texto == m.textoEsperado
            && Mathf.Abs(m.corrimiento) <= ToleranciaGuia
            && m.hueco >= GuiaPrimeraCompra.Separacion - ToleranciaGuia
            && m.hueco <= GuiaPrimeraCompra.Separacion + GuiaPrimeraCompra.Rebote + ToleranciaGuia
            && m.cartelDelLadoBien;
    }

    static bool EnPantalla(MedidaGuia m)
    {
        return m != null && !m.falta && m.flechaEnPantalla && m.cartelEnPantalla;
    }

    // La pantalla, en el espacio del mundo de la UI: el canvas raiz de la tienda, que es overlay
    // y la cubre entera. No Screen.width: desde el update del editor podria ser otra ventana.
    static Rect Pantalla()
    {
        Canvas canvas = tienda.GetComponentInParent<Canvas>();
        if (canvas != null) canvas = canvas.rootCanvas;
        return canvas != null ? EnElMundo((RectTransform)canvas.transform) : new Rect(0f, 0f, Screen.width, Screen.height);
    }

    static Rect EnElMundo(RectTransform rt)
    {
        rt.GetWorldCorners(esquinas);
        return Envolver(esquinas[0], esquinas[1], esquinas[2], esquinas[3]);
    }

    static Rect EnElEspacioDe(RectTransform espacio, RectTransform rt)
    {
        rt.GetWorldCorners(esquinas);
        return Envolver(espacio.InverseTransformPoint(esquinas[0]), espacio.InverseTransformPoint(esquinas[1]),
                        espacio.InverseTransformPoint(esquinas[2]), espacio.InverseTransformPoint(esquinas[3]));
    }

    static Rect Envolver(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {
        return Rect.MinMaxRect(Mathf.Min(Mathf.Min(a.x, b.x), Mathf.Min(c.x, d.x)), Mathf.Min(Mathf.Min(a.y, b.y), Mathf.Min(c.y, d.y)),
                               Mathf.Max(Mathf.Max(a.x, b.x), Mathf.Max(c.x, d.x)), Mathf.Max(Mathf.Max(a.y, b.y), Mathf.Max(c.y, d.y)));
    }

    // Lo que ocupan las letras del cartel (su rectangulo mide 560 y el texto es mas corto);
    // sin texto generado todavia, el rectangulo.
    static Rect TextoEnElMundo(TMP_Text texto)
    {
        Bounds b = texto.textBounds;
        if (b.size.x <= 0f || b.size.y <= 0f) return EnElMundo(texto.rectTransform);
        return Envolver(texto.transform.TransformPoint(b.min), texto.transform.TransformPoint(new Vector3(b.min.x, b.max.y, 0f)),
                        texto.transform.TransformPoint(b.max), texto.transform.TransformPoint(new Vector3(b.max.x, b.min.y, 0f)));
    }

    static bool Dentro(Rect adentro, Rect afuera)
    {
        const float tolerancia = 1f;
        return adentro.xMin >= afuera.xMin - tolerancia && adentro.xMax <= afuera.xMax + tolerancia
            && adentro.yMin >= afuera.yMin - tolerancia && adentro.yMax <= afuera.yMax + tolerancia;
    }

    static string F(double valor, string formato = "0.##")
    {
        return valor.ToString(formato);
    }

    static void EscribirApertura(StringBuilder inf, Apertura a)
    {
        if (a == null) return;
        inf.AppendLine(a.nombre + ": " + (a.antes >= 0f ? "la fila quedo en " + F(a.antes, "0.00") + " al cerrar; " : "")
                       + "abre en " + F(a.posicion, "0.000") + " (scroll prendido: " + a.scrollPrendido + "); DANIO va de "
                       + F(a.izquierda, "0") + " a " + F(a.derecha, "0") + " u en una lista de " + F(a.anchoVista, "0")
                       + " u (la fila mide " + F(a.anchoFila, "0") + " u); la lista entera en pantalla: " + a.listaEnPantalla);
    }

    static void EscribirGuia(StringBuilder inf, string titulo, MedidaGuia m)
    {
        if (m == null)
        {
            inf.AppendLine(titulo + ": no se llego a medir");
            return;
        }
        if (m.falta)
        {
            inf.AppendLine(titulo + ": no se encontro la guia (GuiaPrimeraCompra/Flecha o Cartel colgando del panel)");
            return;
        }
        inf.AppendLine(titulo + ": prendida " + m.activa + ", alfa " + F(m.alfa, "0.00") + ", giro de la flecha " + F(m.giro, "0")
                       + " grados, corrida " + F(m.corrimiento, "0.0") + " u del centro, a " + F(m.hueco, "0.0")
                       + " u del borde (de " + GuiaPrimeraCompra.Separacion + " a " + (GuiaPrimeraCompra.Separacion + GuiaPrimeraCompra.Rebote)
                       + " con el rebote); cartel \"" + m.texto + "\" (esperado \"" + m.textoEsperado + "\"), del lado que va: " + m.cartelDelLadoBien);
        inf.AppendLine("  en pantalla (" + F(m.pantalla.width, "0") + " x " + F(m.pantalla.height, "0") + "): flecha de x " + F(m.flecha.xMin, "0")
                       + " a " + F(m.flecha.xMax, "0") + ", y " + F(m.flecha.yMin, "0") + " a " + F(m.flecha.yMax, "0") + " (" + m.flechaEnPantalla
                       + "); cartel de x " + F(m.cartel.xMin, "0") + " a " + F(m.cartel.xMax, "0") + ", y " + F(m.cartel.yMin, "0") + " a "
                       + F(m.cartel.yMax, "0") + " (" + m.cartelEnPantalla + "); botones de comprar que pisa: "
                       + (m.pisa.Length > 0 ? m.pisa : "ninguno"));
    }

    static void Terminar(string error)
    {
        if (terminado) return;
        terminado = true;

        var inf = new StringBuilder();
        inf.AppendLine("Prueba de la tienda de mejoras (Menu)");
        inf.AppendLine();
        if (error != null) inf.AppendLine("ERROR: " + error);
        inf.AppendLine("Estado preparado: progreso de cero (Progreso.ReiniciarTodo) con " + F(MonedasDePrueba, "0") + " monedas. En play: "
                       + F(monedasAlEmpezar, "0") + " monedas, nunca compro: " + nuncaComproAlEmpezar + ", partidas terminadas: "
                       + partidasAlEmpezar + ", recompensa diaria encima del menu: " + diariaOcupada);
        inf.AppendLine("MEJORAS abrio la tienda: " + abrio[0] + ", " + abrio[1] + ", " + abrio[2] + "; VOLVER la cerro: "
                       + cerro[0] + ", " + cerro[1] + ", " + cerro[2]);
        EscribirApertura(inf, primera);
        inf.AppendLine("  (la guia ya estaba a la vista al medirla: " + guiaAntesDeMedir + ")");
        EscribirApertura(inf, primeraConGuia);
        EscribirApertura(inf, segunda);
        EscribirApertura(inf, tercera);
        EscribirGuia(inf, "Guia senalando DANIO", guiaComprar);
        inf.AppendLine("Compra que ensenia la guia (toque sobre DANIO con la fila quieta, cayo en " + caidaGuia + "): fila en movimiento "
                       + filaMovianteAntesDeLaGuia + ", click " + clicGuia + ", nivel " + nivelAntesGuia + " -> " + nivelDespuesGuia
                       + ", monedas " + F(monedasAntesGuia) + " -> " + F(monedasDespuesGuia) + " (precio " + F(precioGuia) + ")");
        EscribirGuia(inf, "Guia senalando A JUGAR", guiaJugar);
        inf.AppendLine("La guia con la tienda cerrada: " + (guiaEscondidaAlCerrar ? "escondida" : "prendida o sin encontrar")
                       + "; en la segunda apertura, a los 1,4 s: " + (guiaEnLaSegunda ? "prendida o sin encontrar" : "escondida"));
        inf.AppendLine("Toque que frena, sobre " + idTocada + " (cayo en " + caidaFrenar + "; atienden el toque en su boton: " + manejadores
                       + "): scroll prendido " + scrollPrendido + ", empujones " + intentosDeEmpujon
                       + (empujonEnLaMismaVuelta ? " (el ultimo en la misma vuelta del editor que el toque)" : "") + ", la fila se movio "
                       + F(movioAntesDelToque, "0") + " u antes del toque, a " + F(velocidadAlTocar, "0") + " u/s (FilaEnMovimiento "
                       + filaDeslizandose + ")");
        inf.AppendLine("  al apoyar: elegible para click " + elegibleTrasFrenar + ", velocidad " + F(velocidadTrasApoyar, "0.0")
                       + " u/s; antes de soltar " + F(velocidadAntesDeSoltar, "0.0") + " u/s; despues de soltar se movio "
                       + F(posDespuesDeFrenar - posAlApoyar, "0.0") + " u; click " + clicFrenando + "; nivel " + nivelAntesFrenar + " -> "
                       + nivelDespuesFrenar + ", monedas " + F(monedasAntesFrenar) + " -> " + F(monedasDespuesFrenar));
        inf.AppendLine("Toque con la fila quieta, sobre " + idTocada + " (cayo en " + caidaQuieta + "): velocidad " + F(velocidadQuieta, "0.0")
                       + " u/s, FilaEnMovimiento " + !filaQuietaAlTocar + ", elegible para click " + elegibleQuieta + ", click " + clicQuieta
                       + "; nivel " + nivelAntesQuieta + " -> " + nivelDespuesQuieta + ", monedas " + F(monedasAntesQuieta) + " -> "
                       + F(monedasDespuesQuieta) + " (precio " + F(precioQuieta) + ")");
        inf.AppendLine("Toque sobre DANIO en el cuadro en que se abre la tienda (cayo en " + caidaRecien + "): RecienAbierta "
                       + recienAbiertaAlTocar + ", elegible para click " + elegibleRecienAbierta + ", click " + clicRecienAbierta
                       + "; nivel " + nivelAntesRecien + " -> " + nivelDespuesRecien + ", monedas " + F(monedasAntesRecien) + " -> "
                       + F(monedasDespuesRecien));
        inf.AppendLine("Capturas en Builds/: " + (capturas.Count > 0 ? string.Join(", ", capturas) : "ninguna"));
        inf.AppendLine("Errores y excepciones durante la prueba: " + cuantasExcepciones);
        if (cuantasExcepciones > 0) inf.Append(excepciones);
        inf.AppendLine();

        bool[] ok =
        {
            error == null && cuantasExcepciones == 0,
            abrio[0] && abrio[1] && abrio[2],
            cerro[0] && cerro[1] && cerro[2],
            Bien(primera) && Bien(primeraConGuia),
            Bien(segunda) && segunda.antes >= 0.99f,
            Bien(tercera) && tercera.antes >= 0.99f,
            ApuntaBien(guiaComprar, 90f),
            EnPantalla(guiaComprar),
            !filaMovianteAntesDeLaGuia && clicGuia && nivelDespuesGuia == nivelAntesGuia + 1
                && System.Math.Abs(monedasAntesGuia - monedasDespuesGuia - precioGuia) < 0.5,
            ApuntaBien(guiaJugar, 0f),
            EnPantalla(guiaJugar) && guiaJugar.pisa.Length == 0,
            guiaEscondidaAlCerrar && !guiaEnLaSegunda,
            scrollPrendido && filaDeslizandose,
            !clicFrenando && !elegibleTrasFrenar && nivelAntesFrenar >= 0 && nivelDespuesFrenar == nivelAntesFrenar
                && System.Math.Abs(monedasDespuesFrenar - monedasAntesFrenar) < 1e-6,
            Mathf.Abs(velocidadTrasApoyar) < 1f && Mathf.Abs(posDespuesDeFrenar - posAlApoyar) < 2f,
            filaQuietaAlTocar && clicQuieta && nivelAntesQuieta >= 0 && nivelDespuesQuieta == nivelAntesQuieta + 1
                && System.Math.Abs(monedasAntesQuieta - monedasDespuesQuieta - precioQuieta) < 0.5,
            recienAbiertaAlTocar && !elegibleRecienAbierta && !clicRecienAbierta && nivelAntesRecien >= 0
                && nivelDespuesRecien == nivelAntesRecien && System.Math.Abs(monedasDespuesRecien - monedasAntesRecien) < 1e-6,
        };
        string[] que =
        {
            "el banco llego hasta el final, sin errores ni excepciones",
            "el boton MEJORAS abre la tienda (las tres veces)",
            "VOLVER la cierra (las tres veces)",
            "primera apertura: la fila arranca al principio con DANIO entera en la lista, y la guia no tiene que moverla",
            "segunda apertura, tras cerrar con VOLVER con la fila al final: arranca al principio con DANIO entera en la lista",
            "tercera apertura, igual: arranca al principio con DANIO entera en la lista",
            "la guia senala el boton de comprar de DANIO desde abajo, con la flecha hacia arriba y su cartel a la derecha",
            "la flecha y el cartel de la guia que senala DANIO quedan dentro de la pantalla",
            "un toque sobre DANIO con la fila quieta la compra (nivel 0 a 1, cobra su precio)",
            "al comprar, la guia pasa a A JUGAR desde su izquierda, con la flecha hacia la derecha y su cartel a la izquierda",
            "la flecha y el cartel en A JUGAR quedan dentro de la pantalla y no pisan ningun boton de comprar",
            "la guia se va al cerrar y no vuelve en la apertura siguiente (ya se compro)",
            "la fila se estaba deslizando cuando llego el toque (FilaEnMovimiento)",
            "el toque que frena la fila no compra: sin click, y ni las monedas ni el nivel de la tarjeta cambian",
            "el toque frena la fila: la velocidad queda en cero y no se mueve mas",
            "con la fila quieta, un toque sobre la misma tarjeta la compra (nivel +1, cobra su precio)",
            "un toque que empieza en el cuadro en que se abre la tienda no compra (el dedo de la diaria)",
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
        // Al volver a modo edicion se descarta el progreso que Progreso tiene en memoria (ver AlCambiarDeModo).
        SessionState.SetBool(Clave + ".olvidarProgreso", true);
        EditorApplication.ExitPlaymode();
        PlayerSettings.runInBackground = false;
        AssetDatabase.SaveAssets();
        Debug.Log(inf.ToString());
    }
}
