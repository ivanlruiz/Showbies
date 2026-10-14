using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Banco de las cartas de la partida (1.6.0, etapa 1), en play: WaveMode retomada en la oleada 5
// con PODER 2 (una carta de vida y una de daño) y una semilla fija. Mira que la partida retomada
// traiga sus cartas (la vida maxima, el daño del arma, la barra con sus dos iconos, encima de la
// vida), que matar un zombi sume su experiencia con su chispa, que la eleccion congele el juego
// con las tres cartas del sorteo y no deje pausar, que un toque en la primera fraccion de segundo
// no elija y uno despues si (y que se note en el arma o el jugador), que el jefe deje la carta
// dorada aunque muera en plena pausa de impacto y que valga doble, que dos PODER de golpe den
// dos elecciones seguidas, que con la pausa abierta no se abra, y que al empezar la oleada
// siguiente se guarden las cartas como estan. Los bancos no abren la eleccion sola (SinCartasAutomaticas):
// este la abre a mano (EleccionDeCartas.AbrirAMano). Saca dos fotos (Builds/cartas_barra.png y
// Builds/cartas_eleccion.png) y escribe Builds/prueba_cartas.txt.
[InitializeOnLoad]
public static class PruebaCartas
{
    const string Clave = "ShowBies.PruebaCartas";
    const string Banco = "PruebaCartas";
    const string Ruta = "../Builds/prueba_cartas.txt";
    const string FotoBarra = "../Builds/cartas_barra.png";
    const string FotoEleccion = "../Builds/cartas_eleccion.png";
    const string Escena = "Assets/Escenas/WaveMode.unity";
    const int Oleada = 5;
    const int Semilla = 424242;
    const double TopeTotal = 120.0;
    // Lo que se espera para elegir: pasado lo que la eleccion ignora los toques.
    const float ParaElegir = EleccionDeCartas.SinElegirAlAbrir + 0.4f;
    // Lo que tarda en irse: la salida (0,4 s), el respiro antes de poder abrir otra (0,25 s) y
    // un margen. Con menos, abrir la siguiente justo al terminar el respiro fallaba a veces.
    const float ParaCerrar = 0.95f;

    static double inicio;
    static bool empezo, terminado;
    static int paso;
    static float desde;
    static readonly List<bool> oks = new List<bool>();
    static readonly List<string> ques = new List<string>();
    static readonly StringBuilder notas = new StringBuilder();

    // Lo que se anota entre un paso y el siguiente.
    static List<string> esperadas = new List<string>();
    static int doradaEsperada;
    static string elegida;
    static int vecesAntes;
    static float danoAntes, cadenciaAntes, criticoAntes, velocidadAntes, imanAntes, granadaAntes, furiaAntes;
    static int vidaAntes, saludAntes;
    static int oleadaAlElegir;

    static PruebaCartas()
    {
        EditorApplication.update += Tick;
    }

    [MenuItem("ShowBies/Pruebas/Cartas de la partida (play)")]
    public static void Arrancar()
    {
        if (!RespaldoDelBanco.PuedeArrancar(Banco)) return;
        RespaldoDelBanco.Guardar(Banco);
        // Con el HUD del telefono (los joysticks y la granada), que es contra lo que se mide la
        // barra; sin auto-apuntado, que mataria zombis por su cuenta y sumaria experiencia.
        PruebaDisparo.ModoTelefono(Clave);
        PlayerJS.FijarAutoApuntar(false);
        PlayerSettings.runInBackground = true;
        Progreso.GuardarOleadaEnCurso(Oleada, 0);
        var estado = new EstadoPoder { poder = 2, experiencia = 0, pendientes = 0, semilla = Semilla };
        estado.cartas.Add(new CartaElegida { id = PoderDeLaPartida.Vida, veces = 1 });
        estado.cartas.Add(new CartaElegida { id = PoderDeLaPartida.Dano, veces = 1 });
        Progreso.GuardarPoderEnCurso(estado);
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
            paso = 0;
            oks.Clear();
            ques.Clear();
            notas.Length = 0;
            inicio = EditorApplication.timeSinceStartup;
            desde = Time.unscaledTime;
            return;
        }
        if (EditorApplication.timeSinceStartup - inicio > TopeTotal) { Terminar("se paso del tope de " + TopeTotal + " s, en el paso " + paso); return; }
        try
        {
            Avanzar();
        }
        catch (System.Exception e)
        {
            Terminar("excepcion en el paso " + paso + ": " + e);
        }
    }

    static void Anotar(bool ok, string que)
    {
        oks.Add(ok);
        ques.Add(que);
    }

    static void Siguiente()
    {
        paso++;
        desde = Time.unscaledTime;
    }

    static void Avanzar()
    {
        var vida = PlayerHealth.instance;
        var oleadas = Object.FindAnyObjectByType<WaveManager>();
        var gun = Object.FindAnyObjectByType<GunController>();
        var pausa = Object.FindAnyObjectByType<MenuPausa>();
        var eleccion = EleccionDeCartas.Instancia;
        if (vida == null || oleadas == null || gun == null || pausa == null || eleccion == null) return;
        float t = Time.unscaledTime - desde;

        // Los zombis de la oleada se van (cuentan como muertos): la oleada termina sola y nadie
        // le pega al jugador. La vida se llena despues de mirarla, en el primer paso.
        EnemyController.DespejarAlrededor(vida.transform.position, 500f);
        if (paso > 0) vida.health = vida.maxHealth;

        switch (paso)
        {
            case 0:
                // La barra se arma en Start y se acomoda cada medio segundo.
                if (t < 1.2f || GameObject.Find("BarraDePoder") == null) return;
                MirarLoRetomado(vida, gun);
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(FotoBarra)));
                ScreenCapture.CaptureScreenshot(FotoBarra);
                Siguiente();
                return;

            case 1:
            {
                // La foto sale al final del cuadro: se espera a que se tome antes de abrir nada.
                if (t < 0.5f) return;
                // Un zombi muerto por el camino de las balas: su experiencia y su chispa.
                var tipo = oleadas.tipos != null && oleadas.tipos.Length > 0 ? oleadas.tipos[0].prefab : null;
                if (tipo == null) { Terminar("el WaveManager no tiene tipos"); return; }
                var z = EnemyController.Aparecer(tipo, vida.transform.position + new Vector3(6f, 0f, 0f));
                int xp = z.enemyType.experiencia;
                int antes = PoderDeLaPartida.Experiencia;
                z.DanoZombi(1e9f);
                Anotar(xp > 0 && PoderDeLaPartida.Experiencia == antes + xp,
                       "matar un " + z.enemyType.name + " suma su experiencia (" + xp + ")");
                Anotar(GameObject.Find("ChispaDeExperiencia") != null, "y vuela una chispa hacia el jugador");
                Siguiente();
                return;
            }

            case 2:
                // Lo que falta para el PODER 3: una eleccion, que se abre a mano.
                PoderDeLaPartida.Sumar(PoderDeLaPartida.CostoActual - PoderDeLaPartida.Experiencia);
                Anotar(PoderDeLaPartida.Poder == 3 && PoderDeLaPartida.Pendientes == 1, "al llenar la barra se sube al PODER 3 con una eleccion pendiente");
                esperadas = PoderDeLaPartida.Ofrecer(out doradaEsperada);
                Anotar(eleccion.AbrirAMano() && EleccionDeCartas.Activa, "la eleccion se abre");
                Siguiente();
                return;

            case 3:
            {
                if (t < 0.2f) return;
                var cartas = Cartas();
                Anotar(Time.timeScale == 0f && MenuPausa.JuegoCongelado, "con la eleccion abierta el juego queda congelado");
                Anotar(MismasCartas(cartas, esperadas) && Distintas(cartas),
                       "salen las tres del sorteo, distintas (" + string.Join(", ", esperadas) + ")");
                Anotar(Mathf.Abs(cartas.Count - esperadas.Count) == 0 && doradaEsperada < 0 && CuantasDoradas(cartas) == 0, "sin jefe, ninguna dorada");
                var titulo = TituloDeLaEleccion();
                Anotar(titulo != null && titulo.text == Textos.Formato("poder_titulo", 3), "el titulo dice el PODER que se elige: \"" + (titulo != null ? titulo.text : "") + "\"");
                notas.AppendLine("Material del titulo: " + (titulo != null && titulo.fontSharedMaterial != null ? titulo.fontSharedMaterial.name : "ninguno"));
                pausa.Pausar();
                Anotar(!MenuPausa.Pausado, "con la eleccion abierta no se puede pausar");
                if (cartas.Count > 0) cartas[0].onClick.Invoke();
                Anotar(EleccionDeCartas.Activa && PoderDeLaPartida.Pendientes == 1, "un toque en la primera fraccion de segundo no elige");
                Siguiente();
                return;
            }

            case 4:
                // Con las tres ya quietas: la foto.
                if (t < ParaElegir - 0.3f) return;
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(FotoEleccion)));
                ScreenCapture.CaptureScreenshot(FotoEleccion);
                Siguiente();
                return;

            case 5:
            {
                if (t < 0.3f) return;
                var cartas = Cartas();
                var boton = Preferida(cartas);
                if (boton == null) { Terminar("no hay cartas para elegir"); return; }
                elegida = IdDe(boton);
                AnotarAntes(vida, gun);
                boton.onClick.Invoke();
                Anotar(PoderDeLaPartida.Pendientes == 0 && PoderDeLaPartida.Veces(elegida) == vecesAntes + 1,
                       "un toque despues elige: " + elegida + " pasa de " + vecesAntes + " a " + PoderDeLaPartida.Veces(elegida));
                Siguiente();
                return;
            }

            case 6:
                if (t < ParaCerrar) return;
                Anotar(!EleccionDeCartas.Activa && Time.timeScale == 1f, "al elegir se cierra y el juego vuelve a andar");
                Anotar(SeNota(elegida, vida, gun), "la carta elegida (" + elegida + ") se nota en el arma o el jugador");
                Anotar(IconosDeLaBarra() == ContarElegidas(), "la barra muestra un icono por carta (" + IconosDeLaBarra() + ")");
                Siguiente();
                return;

            case 7:
            {
                // El jefe, muerto por el camino de las balas: deja la dorada y, con su experiencia,
                // la eleccion. Se abre enseguida, en plena pausa de impacto, como en la partida.
                if (oleadas.jefe == null) { Terminar("el WaveManager no tiene jefe"); return; }
                var jefe = EnemyController.Aparecer(oleadas.jefe, vida.transform.position + new Vector3(9f, 0f, 0f));
                jefe.EsJefe = true;
                jefe.DanoZombi(1e12f);
                Anotar(PoderDeLaPartida.DoradaPendiente, "el jefe deja la carta dorada para la proxima eleccion");
                if (PoderDeLaPartida.Pendientes == 0)
                    PoderDeLaPartida.Sumar(PoderDeLaPartida.CostoActual - PoderDeLaPartida.Experiencia);
                esperadas = PoderDeLaPartida.Ofrecer(out doradaEsperada);
                Anotar(eleccion.AbrirAMano(), "la eleccion del jefe se abre en plena pausa de impacto");
                Siguiente();
                return;
            }

            case 8:
            {
                if (t < 1.2f) return;
                Anotar(EleccionDeCartas.Activa && Time.timeScale == 0f, "la pausa de impacto del jefe no descongela la eleccion");
                var cartas = Cartas();
                Anotar(CuantasDoradas(cartas) == 1 && doradaEsperada >= 0 && doradaEsperada < cartas.Count && EsDorada(cartas[doradaEsperada]),
                       "una sola carta es la dorada, la del sorteo");
                var tituloDelJefe = TituloDeLaEleccion();
                Anotar(tituloDelJefe != null && tituloDelJefe.text == Textos.De("carta_bono_jefe"),
                       "con la dorada el titulo dice \"" + (tituloDelJefe != null ? tituloDelJefe.text : "") + "\"");
                var dorada = cartas.Find(EsDorada);
                if (dorada == null) { Terminar("no hay carta dorada"); return; }
                elegida = IdDe(dorada);
                vecesAntes = PoderDeLaPartida.Veces(elegida);
                var carta = CatalogoCartas.Instancia != null ? CatalogoCartas.Instancia.Buscar(elegida) : null;
                int tope = carta != null && carta.tope > 0 ? carta.tope : int.MaxValue;
                dorada.onClick.Invoke();
                Anotar(PoderDeLaPartida.Veces(elegida) == Mathf.Min(tope, vecesAntes + 2) && !PoderDeLaPartida.DoradaPendiente,
                       "la dorada vale doble: " + elegida + " pasa de " + vecesAntes + " a " + PoderDeLaPartida.Veces(elegida) + " (tope " + tope + ")");
                Siguiente();
                return;
            }

            case 9:
                if (t < ParaCerrar) return;
                Anotar(!EleccionDeCartas.Activa && Time.timeScale == 1f, "despues de la dorada el juego vuelve a andar");
                // Dos PODER de golpe: dos elecciones, una detras de la otra.
                PoderDeLaPartida.Sumar(PoderDeLaPartida.CostoActual - PoderDeLaPartida.Experiencia + PoderDeLaPartida.Costo(PoderDeLaPartida.Poder + 1));
                Anotar(PoderDeLaPartida.Pendientes == 2, "dos PODER de golpe dejan dos elecciones");
                Anotar(eleccion.AbrirAMano(), "se abre la primera");
                Siguiente();
                return;

            case 10:
                if (t < ParaElegir) return;
                Elegir();
                Siguiente();
                return;

            case 11:
                if (t < ParaCerrar) return;
                Anotar(PoderDeLaPartida.Pendientes == 1 && eleccion.AbrirAMano(), "despues se abre la segunda");
                Siguiente();
                return;

            case 12:
                if (t < ParaElegir) return;
                Elegir();
                Siguiente();
                return;

            case 13:
            {
                if (t < ParaCerrar) return;
                Anotar(PoderDeLaPartida.Pendientes == 0 && !EleccionDeCartas.Activa && Time.timeScale == 1f, "y despues de las dos no queda ninguna");
                // Con la pausa abierta no se abre; al reanudar, si.
                PoderDeLaPartida.Sumar(PoderDeLaPartida.CostoActual - PoderDeLaPartida.Experiencia);
                pausa.Pausar();
                bool enPausa = eleccion.AbrirAMano();
                Anotar(MenuPausa.Pausado && !enPausa, "con la pausa abierta la eleccion espera");
                pausa.Reanudar();
                Anotar(eleccion.AbrirAMano(), "al reanudar se abre");
                Siguiente();
                return;
            }

            case 14:
                if (t < ParaElegir) return;
                Elegir();
                oleadaAlElegir = oleadas.OleadaActual;
                Siguiente();
                return;

            case 15:
            {
                // Al empezar la oleada siguiente se guardan las cartas como estan. Se espera una
                // que empiece despues de la ultima eleccion: la foto es de ese momento.
                if (oleadas.OleadaActual <= oleadaAlElegir) return;
                var guardado = Progreso.PoderEnCurso;
                Anotar(guardado.poder == PoderDeLaPartida.Poder && guardado.pendientes == PoderDeLaPartida.Pendientes
                       && guardado.semilla == Semilla && MismasElegidas(guardado),
                       "al empezar la oleada " + oleadas.OleadaActual + " se guardan el PODER " + guardado.poder + " y sus " + guardado.cartas.Count + " cartas");
                Terminar(null);
                return;
            }
        }
    }

    // La partida retomada en la oleada 5: el PODER, las cartas, lo que hacen y la barra.
    static void MirarLoRetomado(PlayerHealth vida, GunController gun)
    {
        Anotar(PoderDeLaPartida.Activo && PoderDeLaPartida.Poder == 2 && PoderDeLaPartida.Semilla == Semilla,
               "la partida retomada trae el PODER 2 y su semilla");
        Anotar(PoderDeLaPartida.Veces(PoderDeLaPartida.Vida) == 1 && PoderDeLaPartida.Veces(PoderDeLaPartida.Dano) == 1, "y sus dos cartas");

        var campo = typeof(PlayerHealth).GetField("vidaMaximaDeLaMejora", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        int deLaMejora = campo != null ? (int)campo.GetValue(vida) : -1;
        int esperada = Mathf.RoundToInt(deLaMejora * PoderDeLaPartida.MultiplicadorVida);
        Anotar(deLaMejora > 0 && vida.maxHealth == esperada && vida.health == vida.maxHealth,
               "la vida maxima lleva la carta de vida y arranca llena (" + deLaMejora + " -> " + vida.maxHealth + ", vida " + vida.health + ")");
        // Una carta de daño: x1,20 (14/10; era x1,25).
        Anotar(Mathf.Abs(gun.DanoPorTiro - gun.DanoPorBala * gun.MultiplicadorDanoFuria * 1.2f) < 1e-3f,
               "el daño de cada tiro lleva la carta de daño (" + gun.DanoPorBala + " -> " + gun.DanoPorTiro + ")");

        var guardado = Progreso.PoderEnCurso;
        Anotar(guardado.poder == 2 && MismasElegidas(guardado), "al empezar la oleada retomada se guardo lo mismo");

        var barra = GameObject.Find("BarraDePoder");
        var bloque = barra != null ? (RectTransform)barra.transform.Find("AreaSegura/Bloque") : null;
        var texto = bloque != null ? bloque.Find("Poder") : null;
        var poder = texto != null ? texto.GetComponent<TMP_Text>() : null;
        Anotar(poder != null && poder.text == Textos.Formato("poder_barra", 2), "la barra dice \"" + (poder != null ? poder.text : "") + "\"");
        Anotar(IconosDeLaBarra() == 2, "y tiene dos iconos (" + IconosDeLaBarra() + ")");

        // Fina, a todo el ancho del area segura y pegada abajo (de Gemini, 12/10); el "PODER N" y
        // los iconos, encima, sin pisar el numero de la vida, los joysticks ni la granada.
        var laBarra = bloque != null ? (RectTransform)bloque.Find("Barra") : null;
        if (laBarra != null && poder != null && vida.healthTMP != null)
        {
            var area = Screen.safeArea;
            Rect r = EnPantalla(laBarra);
            notas.AppendLine("Barra en pantalla: x " + r.xMin.ToString("0") + "-" + r.xMax.ToString("0") + ", y " + r.yMin.ToString("0") + "-" + r.yMax.ToString("0")
                             + "; area segura x " + area.xMin.ToString("0") + "-" + area.xMax.ToString("0") + ", y desde " + area.yMin.ToString("0")
                             + "; pantalla " + Screen.width + "x" + Screen.height);
            Anotar(Mathf.Abs(r.yMin - area.yMin) <= 2f && r.width >= area.width * 0.95f && r.height <= 20f,
                   "la barra va fina, a todo el ancho del area segura y pegada abajo");

            var b = vida.healthTMP.textBounds;
            var t = vida.healthTMP.rectTransform;
            Vector3 a0 = t.TransformPoint(b.min), a1 = t.TransformPoint(b.max);
            var letras = Rect.MinMaxRect(Mathf.Min(a0.x, a1.x), Mathf.Min(a0.y, a1.y), Mathf.Max(a0.x, a1.x), Mathf.Max(a0.y, a1.y));
            var lo = new List<Rect> { EnPantalla(poder.rectTransform) };
            var fila = bloque.Find("Cartas");
            if (fila != null) foreach (RectTransform hijo in fila) lo.Add(EnPantalla(hijo));
            var estorbos = new List<KeyValuePair<string, Rect>> { new KeyValuePair<string, Rect>("el numero de la vida", letras) };
            foreach (var nombre in new[] { "MoveJoystick", "ShootJoystick", "BotonGranada" })
            {
                var go = GameObject.Find(nombre);
                if (go != null && go.activeInHierarchy) estorbos.Add(new KeyValuePair<string, Rect>(nombre, EnPantalla((RectTransform)go.transform)));
            }
            string pisa = null;
            foreach (var mio in lo)
                foreach (var e in estorbos)
                    if (mio.Overlaps(e.Value)) pisa = e.Key;
            Anotar(pisa == null, "el PODER y los iconos no pisan " + (pisa ?? "el numero de la vida, los joysticks ni la granada"));
        }
        else
        {
            Anotar(false, "la barra va pegada abajo (no se encontro la barra, el texto o el numero de la vida)");
        }
    }

    // Lo que ocupa un RectTransform de un canvas overlay, en pixeles de la pantalla.
    static Rect EnPantalla(RectTransform rt)
    {
        var e = new Vector3[4];
        rt.GetWorldCorners(e);
        return Rect.MinMaxRect(e[0].x, e[0].y, e[2].x, e[2].y);
    }

    static void AnotarAntes(PlayerHealth vida, GunController gun)
    {
        vecesAntes = PoderDeLaPartida.Veces(elegida);
        danoAntes = gun.DanoPorTiro;
        cadenciaAntes = gun.TirosPorSegundo;
        criticoAntes = gun.ProbabilidadCriticoEnLaPartida;
        vidaAntes = vida.maxHealth;
        saludAntes = vida.health;
        velocidadAntes = PoderDeLaPartida.MultiplicadorVelocidad;
        imanAntes = PoderDeLaPartida.ImanExtra;
        granadaAntes = PoderDeLaPartida.MultiplicadorRecargaGranada;
        furiaAntes = PoderDeLaPartida.SegundosFuriaExtra;
    }

    static bool SeNota(string id, PlayerHealth vida, GunController gun)
    {
        switch (id)
        {
            case PoderDeLaPartida.Dano: return gun.DanoPorTiro > danoAntes * 1.1f;
            case PoderDeLaPartida.Cadencia: return gun.TirosPorSegundo > cadenciaAntes * 1.1f || gun.TirosPorSegundo >= gun.maxTirosPorSegundo - 0.01f;
            case PoderDeLaPartida.Critico: return gun.ProbabilidadCriticoEnLaPartida > criticoAntes + 0.05f || gun.ProbabilidadCriticoEnLaPartida >= 1f;
            case PoderDeLaPartida.Vida:
                // Cura solo lo que suma (14/10, de Argon): hasta el 14/10 llenaba la barra.
                return vida.maxHealth > vidaAntes && vida.health == Mathf.Min(vida.maxHealth, saludAntes + vida.maxHealth - vidaAntes);
            case PoderDeLaPartida.Velocidad: return PoderDeLaPartida.MultiplicadorVelocidad > velocidadAntes;
            case PoderDeLaPartida.Iman: return PoderDeLaPartida.ImanExtra > imanAntes;
            case PoderDeLaPartida.GranadaRapida: return PoderDeLaPartida.MultiplicadorRecargaGranada < granadaAntes;
            case PoderDeLaPartida.FuriaLarga: return PoderDeLaPartida.SegundosFuriaExtra > furiaAntes;
            default: return true;
        }
    }

    // Elige la primera que se ofrece, si ya se puede.
    static void Elegir()
    {
        var cartas = Cartas();
        if (cartas.Count > 0) cartas[0].onClick.Invoke();
    }

    // Las cartas en pantalla, en el orden en que se ofrecen. Hay dos objetos que se llaman
    // EleccionDeCartas (el componente y el lienzo): se buscan los botones.
    static List<Button> Cartas()
    {
        var lista = new List<Button>();
        foreach (var b in Object.FindObjectsByType<Button>(FindObjectsSortMode.None))
            if (b.name.StartsWith("Carta_") && b.gameObject.activeInHierarchy) lista.Add(b);
        lista.Sort((a, b) => a.transform.localPosition.x.CompareTo(b.transform.localPosition.x));
        return lista;
    }

    static string IdDe(Button b)
    {
        return b.name.Substring("Carta_".Length);
    }

    static bool EsDorada(Button b)
    {
        return b.transform.Find("Dorada") != null;
    }

    static int CuantasDoradas(List<Button> cartas)
    {
        int n = 0;
        foreach (var b in cartas) if (EsDorada(b)) n++;
        return n;
    }

    static bool MismasCartas(List<Button> cartas, List<string> ids)
    {
        if (cartas.Count != ids.Count) return false;
        for (int i = 0; i < ids.Count; i++) if (IdDe(cartas[i]) != ids[i]) return false;
        return true;
    }

    static bool Distintas(List<Button> cartas)
    {
        var vistos = new HashSet<string>();
        foreach (var b in cartas) if (!vistos.Add(IdDe(b))) return false;
        return true;
    }

    // Una de las que mueven un numero del arma o del jugador, para ver que se note.
    static Button Preferida(List<Button> cartas)
    {
        string[] orden = { PoderDeLaPartida.Dano, PoderDeLaPartida.Cadencia, PoderDeLaPartida.Critico, PoderDeLaPartida.Vida };
        foreach (var id in orden)
            foreach (var b in cartas)
                if (IdDe(b) == id) return b;
        return cartas.Count > 0 ? cartas[0] : null;
    }

    static TMP_Text TituloDeLaEleccion()
    {
        foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (c.name != "EleccionDeCartas") continue;
            var t = c.transform.Find("Panel/Titulo");
            if (t != null) return t.GetComponent<TMP_Text>();
        }
        return null;
    }

    static int IconosDeLaBarra()
    {
        var barra = GameObject.Find("BarraDePoder");
        var fila = barra != null ? barra.transform.Find("AreaSegura/Bloque/Cartas") : null;
        if (fila == null) return -1;
        // Los destruidos en este cuadro siguen contando hasta el final: se cuentan los que quedan.
        int n = 0;
        foreach (Transform hijo in fila) if (hijo.gameObject.activeSelf) n++;
        return n;
    }

    static int ContarElegidas()
    {
        int n = 0;
        foreach (var par in PoderDeLaPartida.Elegidas) if (par.Value > 0) n++;
        return n;
    }

    static bool MismasElegidas(EstadoPoder guardado)
    {
        if (guardado == null || guardado.cartas == null || guardado.cartas.Count != ContarElegidas()) return false;
        foreach (var c in guardado.cartas)
            if (c == null || PoderDeLaPartida.Veces(c.id) != c.veces) return false;
        return true;
    }

    static void Terminar(string error)
    {
        if (terminado) return;
        terminado = true;
        var inf = new StringBuilder();
        inf.AppendLine("Prueba de las cartas de la partida (WaveMode retomada en la oleada " + Oleada + ", PODER 2 con vida y daño, semilla " + Semilla + ")");
        inf.AppendLine();
        if (error != null) inf.AppendLine("ERROR: " + error);
        inf.Append(notas);
        inf.AppendLine("Cartas al terminar: " + Resumen());
        inf.AppendLine();
        bool todo = error == null;
        for (int i = 0; i < oks.Count; i++)
        {
            inf.AppendLine((oks[i] ? "OK  " : "FALLA  ") + ques[i]);
            todo &= oks[i];
        }
        inf.AppendLine();
        inf.AppendLine("RESULTADO: " + (todo ? "TODO OK" : "HAY FALLAS"));
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(Ruta)));
        File.WriteAllText(Ruta, inf.ToString());
        SessionState.SetBool(Clave, false);
        EditorApplication.ExitPlaymode();
        Debug.Log(inf.ToString());
    }

    static string Resumen()
    {
        var partes = new List<string>();
        foreach (var par in PoderDeLaPartida.Elegidas) partes.Add(par.Key + " x" + par.Value);
        return "PODER " + PoderDeLaPartida.Poder + ": " + string.Join(", ", partes);
    }
}
