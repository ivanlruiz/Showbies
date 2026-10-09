using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Banco de lo que la superauditoria del 29/9 encontro en la pausa y en la noche, en play, en
// WaveMode con una partida guardada en la oleada 11 (al retomarla sale el cartel del capitulo 2,
// y el boton de pausa, que lo tapaba, tiene que desvanecerse mientras dura y volver despues):
//
//  1. Los halos de los botones de la pausa no se llevan toques de los vecinos: un raycast como
//     el del EventSystem justo adentro del borde de abajo de cada boton cae en ese boton, en el
//     de arriba del siguiente cae en el siguiente, y en el hueco entre los dos no cae en ninguno
//     (antes, el halo de REINICIAR se quedaba con el hueco y con el borde de CONTINUAR). Desde la
//     1.5.0 las oleadas tienen MEJORAS entre CONTINUAR y REINICIAR, y el panel se aprieta: los
//     cuatro botones, el titulo y los volumenes no se pisan.
//  2. REINICIAR pregunta antes de borrar la partida (MenuPausa.Reiniciar): no olvida la oleada,
//     el atras (CerrarConfirmacion, que es lo que hace Escape) vuelve a la pausa sin reanudar,
//     SEGUIR JUGANDO reanuda, y REINICIAR de la confirmacion recarga la escena y empieza de la
//     1. En la 1 ya no pregunta. (La R de PC, que tambien pasaba por aca, se saco el 6/10.)
//  3. Una foto de noche, con la calidad del editor, de las tres cajas y el chorro de balas al
//     lado del jugador (la bala sin luz y las cajas en la capa de la luz de relleno), otra de
//     la pausa y otra de la confirmacion.
//  4. MEJORAS (en la 1, despues de reiniciar) anota el modo, no olvida la oleada en curso y
//     carga el menu con la tienda abierta (o la recompensa diaria primero, si hay).
//
// El progreso y los PlayerPrefs del editor vuelven como estaban al salir de play
// (RespaldoDelBanco). Escribe Builds/prueba_reiniciar.txt y las fotos
// Builds/noche_cajas_balas.png, Builds/pausa_mejoras.png y Builds/pausa_reiniciar.png.
//
// OJO: entrar y salir de play hace que Unity vuelva a serializar ProjectSettings (ver la
// trampa en CLAUDE.md): despues de correrlo, revertir lo que no se toco a proposito.
[InitializeOnLoad]
public static class PruebaReiniciar
{
    const string Clave = "ShowBies.PruebaReiniciar";
    const string Ruta = "../Builds/prueba_reiniciar.txt";
    const string Escena = "Assets/Escenas/WaveMode.unity";
    // La 11: al retomarla sale el cartel del capitulo 2, que el boton de pausa tapaba.
    const int OleadaGuardada = 11;
    const double TopePorPaso = 15.0;
    const double TopeTotal = 90.0;

    enum Paso
    {
        Esperando,          // la partida retoma la 5: pone las cajas y dispara
        FotoNoche,          // fotografia las cajas y las balas
        Pausar,             // deja de disparar y pausa
        FotoPausa,          // fotografia la pausa y mide los toques de los bordes y lo que se pisa
        TocarReiniciar,     // toca REINICIAR
        FotoConfirmacion,
        Atras,              // lo que hace Escape con la confirmacion abierta
        Seguir,             // REINICIAR otra vez y SEGUIR JUGANDO
        Reabrir,            // pausa y REINICIAR, para volver a la confirmacion
        ReiniciarSi,        // REINICIAR de la confirmacion
        DespuesDeReiniciar, // la escena recargada, en la 1
        TocarMejoras,       // pausa y MEJORAS
        EnLaTienda,         // el menu, con la tienda abierta
        Listo,
    }

    static Paso paso;
    static double inicio, pasoDesde;
    static int cuadroDelPaso;
    static bool terminado;

    static MenuPausa menu;
    static readonly List<RaycastResult> resultados = new List<RaycastResult>();
    static readonly Vector3[] esquinas = new Vector3[4];
    static readonly List<string> capturas = new List<string>();

    // Lo medido.
    static int oleadaAlEmpezar = -1, cajasPuestas;
    static bool disparaba;
    static readonly StringBuilder toques = new StringBuilder();
    static bool toquesBien, hayMejoras, textoMejorasBien, sinPisarse;
    static string pisados = "";
    static bool mejorasCargoElMenu, mejorasAbrioLaTienda, mejorasDiariaPrimero, mejorasAnotoElModo, mejorasNoOlvido, mejorasSinPausa;
    static int oleadaAntesDeMejoras = -1;
    static bool confirmoAlTocar, pausaTapadaAlConfirmar, seguiaPausado, oleadaIgualAlConfirmar, mismaEscenaAlConfirmar;
    static bool avisoConLaOleada;
    static string avisoTexto = "";
    static int lineasDelTitulo = -1;
    static float huecoTituloAviso = float.NaN;
    static bool cartelAlEmpezar, cartelDespues = true;
    static double sinCartelDesde = -1.0;
    static float alfaConCartel = float.NaN, alfaSinCartel = float.NaN;
    static bool atrasVolvioALaPausa, atrasSeguiaPausado;
    static bool seguirReanudo, seguirSinConfirmacion, oleadaIgualAlSeguir;
    static int escenaAntes, oleadaDespues = -1;
    static bool recargo, sinPausaDespues, noPreguntaEnLaUno;

    static readonly StringBuilder excepciones = new StringBuilder();
    static int cuantasExcepciones;

    static PruebaReiniciar()
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

    [MenuItem("ShowBies/Pruebas/Reiniciar y la noche (play)")]
    // Publico para poder correrlo por codigo (ver la trampa del registro de menus).
    public static void Arrancar()
    {
        if (!RespaldoDelBanco.PuedeArrancar("PruebaReiniciar")) return;
        RespaldoDelBanco.Guardar("PruebaReiniciar");
        PlayerSettings.runInBackground = true;
        // Una partida de oleadas guardada en la 5: es lo que REINICIAR borraria.
        Progreso.GuardarOleadaEnCurso(OleadaGuardada, 50);
        Progreso.Guardar();
        EditorSceneManager.OpenScene(Escena);
        SessionState.SetBool(Clave, true);
        SessionState.SetBool(Clave + ".empezo", false);
        EditorApplication.EnterPlaymode();
    }

    static void Tick()
    {
        if (!SessionState.GetBool(Clave, false)) return;
        if (!EditorApplication.isPlaying) return;
        if (!RespaldoDelBanco.SigueArmado("PruebaReiniciar", Clave)) return;

        double ahora = EditorApplication.timeSinceStartup;
        if (!SessionState.GetBool(Clave + ".empezo", false))
        {
            SessionState.SetBool(Clave + ".empezo", true);
            inicio = ahora;
            terminado = false;
            Pasar(Paso.Esperando, ahora);
            return;
        }
        if (ahora - inicio > TopeTotal) { Terminar("se paso del tope total de " + TopeTotal + " s, en el paso " + paso); return; }
        if (ahora - pasoDesde > TopePorPaso) { Terminar("se trabo en el paso " + paso); return; }

        try
        {
            Avanzar(ahora);
        }
        catch (System.Exception e)
        {
            Terminar("excepcion del banco en el paso " + paso + ": " + e);
        }
    }

    static void Avanzar(double ahora)
    {
        switch (paso)
        {
            case Paso.Esperando:
            {
                if (menu == null) menu = Object.FindAnyObjectByType<MenuPausa>();
                if (menu == null || PlayerHealth.instance == null || EventSystem.current == null || Espero(ahora, 1.5)) return;
                oleadaAlEmpezar = Progreso.OleadaEnCurso;
                // El cartel del capitulo esta en pantalla y el boton de pausa, desvanecido.
                var capitulos = Object.FindAnyObjectByType<CapitulosDeEscenario>();
                cartelAlEmpezar = capitulos != null && capitulos.CartelEnPantalla;
                alfaConCartel = AlfaDelBotonDePausa();
                PonerCajas();
                Disparar(true);
                Pasar(Paso.FotoNoche, ahora);
                return;
            }

            case Paso.FotoNoche:
                Disparar(true);
                if (Espero(ahora, 1.0)) return;
                disparaba = Object.FindObjectsByType<BulletController>(FindObjectsSortMode.None).Length > 0;
                Capturar("noche_cajas_balas");
                Pasar(Paso.Pausar, ahora);
                return;

            case Paso.Pausar:
                // La captura se escribe al final del cuadro.
                if (Espero(ahora, 0.3)) return;
                Disparar(false);
                menu.Pausar();
                Pasar(Paso.FotoPausa, ahora);
                return;

            case Paso.FotoPausa:
            {
                if (Espero(ahora, 0.4)) return;
                var botones = new List<RectTransform>();
                foreach (string nombre in new[] { "BotonContinuar", "BotonMejoras", "BotonReiniciar", "BotonMenu" })
                {
                    var b = (RectTransform)menu.panel.transform.Find(nombre);
                    if (b != null && b.gameObject.activeInHierarchy) botones.Add(b);
                }
                var mejoras = menu.panel.transform.Find("BotonMejoras");
                hayMejoras = mejoras != null && botones.Count == 4;
                var textoMejoras = mejoras != null ? mejoras.GetComponentInChildren<TMPro.TMP_Text>(true) : null;
                textoMejorasBien = textoMejoras != null && textoMejoras.text == Textos.De("menu_mejoras");
                MedirToques(botones);
                MedirLoQueSePisa(botones);
                Capturar("pausa_mejoras");
                Pasar(Paso.TocarReiniciar, ahora);
                return;
            }

            case Paso.TocarReiniciar:
            {
                if (Espero(ahora, 0.3)) return;
                var reiniciar = (RectTransform)menu.panel.transform.Find("BotonReiniciar");
                escenaAntes = SceneManager.GetActiveScene().handle;
                reiniciar.GetComponent<Button>().onClick.Invoke();
                confirmoAlTocar = menu.ConfirmandoReiniciar;
                pausaTapadaAlConfirmar = !menu.panel.activeSelf;
                seguiaPausado = MenuPausa.Pausado && Time.timeScale == 0f;
                oleadaIgualAlConfirmar = Progreso.OleadaEnCurso == oleadaAlEmpezar;
                Pasar(Paso.FotoConfirmacion, ahora);
                return;
            }

            case Paso.FotoConfirmacion:
            {
                if (Espero(ahora, 0.5)) return;
                mismaEscenaAlConfirmar = SceneManager.GetActiveScene().handle == escenaAntes;
                var aviso = BuscarEnLaConfirmacion("Aviso");
                avisoTexto = aviso != null ? aviso.GetComponent<TMPro.TMP_Text>().text : "(sin aviso)";
                avisoConLaOleada = avisoTexto.Contains(oleadaAlEmpezar.ToString());
                var titulo = BuscarEnLaConfirmacion("Titulo");
                if (titulo != null && aviso != null)
                {
                    var tt = titulo.GetComponent<TMPro.TMP_Text>();
                    var ta = aviso.GetComponent<TMPro.TMP_Text>();
                    tt.ForceMeshUpdate();
                    ta.ForceMeshUpdate();
                    lineasDelTitulo = tt.textInfo.lineCount;
                    // En el espacio de la ventana: lo mas bajo de las letras del titulo contra lo
                    // mas alto de las del aviso.
                    float bajoTitulo = tt.rectTransform.anchoredPosition.y + tt.textBounds.min.y;
                    float altoAviso = ta.rectTransform.anchoredPosition.y + ta.textBounds.max.y;
                    huecoTituloAviso = bajoTitulo - altoAviso;
                }
                Capturar("pausa_reiniciar");
                Pasar(Paso.Atras, ahora);
                return;
            }

            case Paso.Atras:
            {
                if (Espero(ahora, 0.4)) return;
                // Antes, que se vaya el cartel del capitulo (3,2 s, en tiempo sin escalar: la
                // pausa no lo congela), y medio segundo mas para que el boton vuelva.
                var capitulosAhora = Object.FindAnyObjectByType<CapitulosDeEscenario>();
                if (capitulosAhora != null && capitulosAhora.CartelEnPantalla) { sinCartelDesde = -1.0; return; }
                if (sinCartelDesde < 0.0) { sinCartelDesde = ahora; return; }
                if (ahora - sinCartelDesde < 0.5) return;
                cartelDespues = false;
                alfaSinCartel = AlfaDelBotonDePausa();
                menu.CerrarConfirmacion();
                atrasVolvioALaPausa = !menu.ConfirmandoReiniciar && menu.panel.activeSelf;
                atrasSeguiaPausado = MenuPausa.Pausado && Time.timeScale == 0f;
                Pasar(Paso.Seguir, ahora);
                return;
            }

            case Paso.Seguir:
            {
                if (Espero(ahora, 0.3)) return;
                ((RectTransform)menu.panel.transform.Find("BotonReiniciar")).GetComponent<Button>().onClick.Invoke();
                var seguir = BuscarEnLaConfirmacion("BotonSeguir");
                if (seguir == null) { Terminar("la confirmacion no tiene BotonSeguir"); return; }
                seguir.GetComponent<Button>().onClick.Invoke();
                seguirReanudo = !MenuPausa.Pausado && Time.timeScale == 1f && !menu.panel.activeSelf;
                seguirSinConfirmacion = !menu.ConfirmandoReiniciar;
                oleadaIgualAlSeguir = Progreso.OleadaEnCurso == oleadaAlEmpezar;
                Pasar(Paso.Reabrir, ahora);
                return;
            }

            case Paso.Reabrir:
                if (Espero(ahora, 0.3)) return;
                menu.Pausar();
                ((RectTransform)menu.panel.transform.Find("BotonReiniciar")).GetComponent<Button>().onClick.Invoke();
                Pasar(Paso.ReiniciarSi, ahora);
                return;

            case Paso.ReiniciarSi:
            {
                if (Espero(ahora, 0.3)) return;
                var si = BuscarEnLaConfirmacion("BotonReiniciarSi");
                if (si == null) { Terminar("la confirmacion no tiene BotonReiniciarSi"); return; }
                escenaAntes = SceneManager.GetActiveScene().handle;
                si.GetComponent<Button>().onClick.Invoke();
                menu = null;
                Pasar(Paso.DespuesDeReiniciar, ahora);
                return;
            }

            case Paso.DespuesDeReiniciar:
                // La oleada 1 se guarda sola al empezar.
                if (Espero(ahora, 2.0)) return;
                recargo = SceneManager.GetActiveScene().handle != escenaAntes;
                oleadaDespues = Progreso.OleadaEnCurso;
                sinPausaDespues = !MenuPausa.Pausado && Time.timeScale == 1f;
                noPreguntaEnLaUno = WaveManager.OleadaQueSePierdeAlReiniciar == 0;
                Pasar(Paso.TocarMejoras, ahora);
                return;

            case Paso.TocarMejoras:
            {
                if (Espero(ahora, 0.3)) return;
                menu = Object.FindAnyObjectByType<MenuPausa>();
                if (menu == null) { Terminar("no hay pausa despues de reiniciar"); return; }
                menu.Pausar();
                var mejoras = menu.panel.transform.Find("BotonMejoras");
                if (mejoras == null) { Terminar("la pausa recargada no tiene BotonMejoras"); return; }
                oleadaAntesDeMejoras = Progreso.OleadaEnCurso;
                PlayerPrefs.SetInt("UltimoModo", 1);
                mejoras.GetComponent<Button>().onClick.Invoke();
                menu = null;
                mejorasAnotoElModo = PlayerPrefs.GetInt("UltimoModo", 0) == TiendaMejoras.EscenaOleadas;
                Pasar(Paso.EnLaTienda, ahora);
                return;
            }

            case Paso.EnLaTienda:
            {
                // La tienda espera a la recompensa diaria, si hay.
                if (Espero(ahora, 1.5)) return;
                mejorasCargoElMenu = SceneManager.GetActiveScene().buildIndex == TiendaMejoras.EscenaMenu;
                var tienda = Object.FindAnyObjectByType<TiendaMejoras>();
                mejorasAbrioLaTienda = tienda != null && tienda.Abierta;
                mejorasDiariaPrimero = VentanaRecompensaDiaria.Ocupada;
                if (!mejorasAbrioLaTienda && !mejorasDiariaPrimero && ahora - pasoDesde < 5.0) return;
                mejorasNoOlvido = Progreso.OleadaEnCurso == oleadaAntesDeMejoras && oleadaAntesDeMejoras > 0;
                mejorasSinPausa = !MenuPausa.Pausado && Time.timeScale == 1f && !AudioListener.pause;
                Pasar(Paso.Listo, ahora);
                return;
            }

            case Paso.Listo:
                Terminar(null);
                return;
        }
    }

    // Las tres cajas en fila, a tres metros por delante del jugador: fuera de su alcance y
    // dentro de la foto.
    static void PonerCajas()
    {
        Vector3 jugador = PlayerHealth.instance.transform.position;
        string[] nombres = { "PUBalas", "PUVida", "PUArma" };
        for (int i = 0; i < nombres.Length; i++)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/" + nombres[i] + ".prefab");
            if (prefab == null) continue;
            Object.Instantiate(prefab, new Vector3(jugador.x - 3f + 3f * i, 0.5f, jugador.z + 3.5f), Quaternion.identity);
            cajasPuestas++;
        }
    }

    // Un chorro de balas desde la boca de la pistola, sacadas del pool como las saca el arma:
    // querer disparar (FijarDisparo) lo pisaria el joystick o el mouse en cada cuadro.
    static double ultimaBala;

    static void Disparar(bool quiere)
    {
        if (!quiere || EditorApplication.timeSinceStartup - ultimaBala < 0.06) return;
        var control = PlayerHealth.instance != null ? PlayerHealth.instance.GetComponent<PlayerController>() : null;
        var arma = control != null ? control.theGun : null;
        if (arma == null || arma.bala == null || arma.firePoint == null) return;
        Transform salida = arma.boca != null ? arma.boca : arma.firePoint;
        BulletController.Obtener(arma.bala, salida.position, arma.firePoint.rotation);
        ultimaBala = EditorApplication.timeSinceStartup;
    }

    // Toques de verdad (el raycast del EventSystem), para cada par de botones vecinos (de arriba
    // abajo): a 2 u adentro del borde de abajo del de arriba, en el medio del hueco y a 2 u
    // adentro del borde de arriba del de abajo.
    static void MedirToques(List<RectTransform> botones)
    {
        toques.Clear();
        toquesBien = botones.Count >= 2;
        for (int i = 0; i + 1 < botones.Count; i++)
        {
            RectTransform arriba = botones[i], abajo = botones[i + 1];
            float escala = arriba.lossyScale.y;
            Rect a = EnPantalla(arriba), b = EnPantalla(abajo);
            GameObject enBordeA = Tocar(new Vector2(a.center.x, a.yMin + 2f * escala));
            GameObject enHueco = Tocar(new Vector2(a.center.x, (a.yMin + b.yMax) * 0.5f));
            GameObject enBordeB = Tocar(new Vector2(b.center.x, b.yMax - 2f * escala));
            bool bien = enBordeA != null && enBordeA.transform.IsChildOf(arriba)
                        && (enHueco == null || (!enHueco.transform.IsChildOf(arriba) && !enHueco.transform.IsChildOf(abajo)))
                        && enBordeB != null && enBordeB.transform.IsChildOf(abajo);
            toquesBien &= bien;
            toques.Append((bien ? "  bien " : "  MAL ") + arriba.name + "/" + abajo.name + ": abajo de " + arriba.name + " -> " + Camino(enBordeA)
                          + "; hueco -> " + Camino(enHueco) + "; arriba de " + abajo.name + " -> " + Camino(enBordeB) + System.Environment.NewLine);
        }
    }

    // Que no se pisen, en la pantalla: los botones entre si y con los volumenes, y las letras del
    // titulo con el primer boton.
    static void MedirLoQueSePisa(List<RectTransform> botones)
    {
        var cajas = new List<KeyValuePair<string, Rect>>();
        foreach (var b in botones) cajas.Add(new KeyValuePair<string, Rect>(b.name, EnPantalla(b)));
        foreach (Transform hijo in menu.panel.transform)
            if (hijo.name.StartsWith("Volumen_") && hijo.gameObject.activeInHierarchy)
                cajas.Add(new KeyValuePair<string, Rect>(hijo.name, EnPantalla((RectTransform)hijo)));
        var titulo = menu.panel.transform.Find("Titulo");
        var textoTitulo = titulo != null ? titulo.GetComponent<TMPro.TMP_Text>() : null;
        if (textoTitulo != null && botones.Count > 0)
        {
            textoTitulo.ForceMeshUpdate();
            Bounds letras = textoTitulo.textBounds;
            Vector3 abajo = textoTitulo.rectTransform.TransformPoint(letras.min);
            Vector3 arriba = textoTitulo.rectTransform.TransformPoint(letras.max);
            cajas.Add(new KeyValuePair<string, Rect>("las letras del titulo", Rect.MinMaxRect(abajo.x, abajo.y, arriba.x, arriba.y)));
        }
        var lista = new StringBuilder();
        for (int i = 0; i < cajas.Count; i++)
            for (int j = i + 1; j < cajas.Count; j++)
                if (cajas[i].Value.Overlaps(cajas[j].Value)) lista.Append(cajas[i].Key + " con " + cajas[j].Key + "; ");
        pisados = lista.Length > 0 ? lista.ToString() : "nada";
        sinPisarse = lista.Length == 0 && cajas.Count >= botones.Count + 2;
    }

    // El alfa del boton de pausa (su CanvasGroup, que pone MenuPausa). Aunque en PC el boton
    // este apagado, el grupo sigue su cuenta.
    static float AlfaDelBotonDePausa()
    {
        var boton = menu != null ? menu.transform.Find("AreaSegura/BotonPausa") : null;
        var grupo = boton != null ? boton.GetComponent<CanvasGroup>() : null;
        return grupo != null ? grupo.alpha : float.NaN;
    }

    static GameObject Tocar(Vector2 punto)
    {
        var dedo = new PointerEventData(EventSystem.current) { position = punto };
        resultados.Clear();
        EventSystem.current.RaycastAll(dedo, resultados);
        return resultados.Count > 0 ? resultados[0].gameObject : null;
    }

    static Rect EnPantalla(RectTransform rt)
    {
        rt.GetWorldCorners(esquinas);
        return Rect.MinMaxRect(esquinas[0].x, esquinas[0].y, esquinas[2].x, esquinas[2].y);
    }

    static Transform BuscarEnLaConfirmacion(string nombre)
    {
        var confirmacion = menu.panel.transform.parent.Find("PanelReiniciar");
        return confirmacion != null ? confirmacion.Find(nombre) : null;
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

    static void Terminar(string error)
    {
        if (terminado) return;
        terminado = true;

        var inf = new StringBuilder();
        inf.AppendLine("Prueba de REINICIAR en la pausa y de la noche (WaveMode, partida guardada en la " + OleadaGuardada + ")");
        inf.AppendLine();
        if (error != null) inf.AppendLine("ERROR: " + error);
        inf.AppendLine("Al empezar: oleada en curso " + oleadaAlEmpezar + ", cajas puestas " + cajasPuestas + ", balas en el aire " + disparaba);
        inf.AppendLine("Boton de pausa: con el cartel del capitulo (" + cartelAlEmpezar + ") alfa " + alfaConCartel.ToString("0.00")
                       + "; despues (cartel " + cartelDespues + ") alfa " + alfaSinCartel.ToString("0.00"));
        inf.AppendLine("Pausa: MEJORAS " + hayMejoras + " (texto bien " + textoMejorasBien + "); se pisan: " + pisados);
        inf.AppendLine("Toques entre botones vecinos:");
        inf.Append(toques);
        inf.AppendLine("REINICIAR: confirmacion " + confirmoAlTocar + ", pausa tapada " + pausaTapadaAlConfirmar + ", pausado " + seguiaPausado
                       + ", oleada igual " + oleadaIgualAlConfirmar + ", misma escena " + mismaEscenaAlConfirmar + "; aviso \"" + avisoTexto + "\"");
        inf.AppendLine("  titulo en " + lineasDelTitulo + " renglones, " + huecoTituloAviso.ToString("0") + " u por encima del aviso");
        inf.AppendLine("Atras: volvio a la pausa " + atrasVolvioALaPausa + ", pausado " + atrasSeguiaPausado);
        inf.AppendLine("SEGUIR JUGANDO: reanudo " + seguirReanudo + ", sin confirmacion " + seguirSinConfirmacion + ", oleada igual " + oleadaIgualAlSeguir);
        inf.AppendLine("REINICIAR de la confirmacion: recargo " + recargo + ", oleada despues " + oleadaDespues + ", sin pausa " + sinPausaDespues
                       + ", en la 1 no pregunta " + noPreguntaEnLaUno);
        inf.AppendLine("MEJORAS: menu " + mejorasCargoElMenu + ", tienda abierta " + mejorasAbrioLaTienda + " (o la diaria primero " + mejorasDiariaPrimero
                       + "), modo anotado " + mejorasAnotoElModo + ", oleada guardada " + oleadaAntesDeMejoras + " igual " + mejorasNoOlvido
                       + ", sin pausa " + mejorasSinPausa);
        inf.AppendLine("Capturas en Builds/: " + (capturas.Count > 0 ? string.Join(", ", capturas) : "ninguna"));
        inf.AppendLine("Errores y excepciones durante la prueba: " + cuantasExcepciones);
        if (cuantasExcepciones > 0) inf.Append(excepciones);
        inf.AppendLine();

        bool[] ok =
        {
            error == null && cuantasExcepciones == 0,
            oleadaAlEmpezar == OleadaGuardada,
            hayMejoras && textoMejorasBien,
            toquesBien,
            sinPisarse,
            confirmoAlTocar && pausaTapadaAlConfirmar && seguiaPausado && oleadaIgualAlConfirmar && mismaEscenaAlConfirmar,
            avisoConLaOleada,
            lineasDelTitulo == 1 && huecoTituloAviso > 0f,
            atrasVolvioALaPausa && atrasSeguiaPausado,
            seguirReanudo && seguirSinConfirmacion && oleadaIgualAlSeguir,
            recargo && oleadaDespues == 1 && sinPausaDespues,
            noPreguntaEnLaUno,
            cajasPuestas == 3 && disparaba,
            cartelAlEmpezar && alfaConCartel < 0.5f && !cartelDespues && alfaSinCartel > 0.99f,
            mejorasCargoElMenu && (mejorasAbrioLaTienda || mejorasDiariaPrimero) && mejorasAnotoElModo && mejorasNoOlvido && mejorasSinPausa,
        };
        string[] que =
        {
            "el banco llego hasta el final, sin errores ni excepciones",
            "la partida retomo la oleada guardada",
            "la pausa de las oleadas tiene MEJORAS, con su texto",
            "cada toque justo adentro del borde de un boton cae en ese boton, y en el hueco entre dos, en ninguno",
            "los cuatro botones, las letras del titulo y los volumenes no se pisan",
            "REINICIAR pregunta: tapa la pausa, sigue pausado y no olvida la oleada ni recarga",
            "el aviso dice la oleada que se pierde",
            "el titulo entra en un renglon y no pisa el aviso",
            "el atras con la confirmacion abierta vuelve a la pausa sin reanudar",
            "SEGUIR JUGANDO reanuda y no olvida la oleada",
            "REINICIAR de la confirmacion recarga la escena y empieza de la 1, sin pausa",
            "en la oleada 1 REINICIAR ya no pregunta",
            "la foto de noche tiene las tres cajas y balas en el aire",
            "el boton de pausa se desvanece con el cartel del capitulo y vuelve cuando se va",
            "MEJORAS carga el menu con la tienda abierta (o la diaria primero), anota las oleadas y no olvida la partida",
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
        Disparar(false);
        EditorApplication.ExitPlaymode();
        Debug.Log(inf.ToString());
    }
}
