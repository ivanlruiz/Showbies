using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Banco de los zombis con habilidad en el modo libre (GeneradorZombis.especiales, ZOMBIS.md), en play: ShowBies1 con el
// camino del telefono (el techo de 35) y el auto-apuntado, el jugador quieto en el centro y sin recibir daño, y los
// niveles acelerados (uno cada 4 s en vez de 45). Mide que cada especial salga recien desde su nivel (2 a 6), que nunca
// haya mas de WaveManager.MaxEspecialesVivos a la vez ni mas de su tope de cada uno, y que el escupidor se plante lejos
// (9,5 m) hasta su tercer escupitajo y despues a 6,5 m, adentro del auto-apuntado. Y que cada uno salga a lo sumo
// DemoraMaxima despues de desbloquearse: con el techo de todos lleno (el 82 % del tiempo, con el jugador quieto) los
// comunes se llevaban cada lugar libre, y el chaman y el hinchado salian un minuto tarde o nunca. El informe dice
// cuanto estuvo lleno cada techo y cuantos especiales hubo en promedio. Escribe Builds/prueba_libre_especiales.txt.
[InitializeOnLoad]
public static class PruebaEspecialesEnElLibre
{
    const string Clave = "ShowBies.PruebaEspecialesEnElLibre";
    const string Banco = "PruebaEspecialesEnElLibre";
    const string Ruta = "../Builds/prueba_libre_especiales.txt";
    const string Escena = "Assets/Escenas/ShowBies1.unity";
    const double TopeTotal = 150.0;
    const float SegundosPorNivel = 4f;

    static bool empezo, terminado, preparado;
    static float desde;
    static double inicio;
    static string error;
    static readonly List<KeyValuePair<string, bool>> oks = new List<KeyValuePair<string, bool>>();
    static readonly StringBuilder notas = new StringBuilder();

    static GeneradorZombis generador;
    static readonly Dictionary<GameObject, int> primerNivel = new Dictionary<GameObject, int>();
    static readonly Dictionary<GameObject, int> masVivos = new Dictionary<GameObject, int>();
    static int masEspeciales;
    // Cuanto despues de desbloquearse (en segundos de juego) salio cada uno, y que tope dejaba afuera a los que faltan:
    // el de todos los zombis (los comunes ocupan el lugar que se libera en un cuarto de segundo, y los especiales prueban
    // cada 4 s) o el de los especiales. Desde que se desbloquea el ultimo.
    static readonly Dictionary<GameObject, float> desbloqueado = new Dictionary<GameObject, float>();
    static readonly Dictionary<GameObject, float> primeraVez = new Dictionary<GameObject, float>();
    static readonly Dictionary<GameObject, float> ocupacion = new Dictionary<GameObject, float>();
    static float ultimoTiempo, medido, generalLleno, especialesLlenos, ambosLlenos, especialesPorTiempo, todosPorTiempo;
    // En el banco los niveles duran 4 s: el que sale en su proxima prueba (cada 4 s) mas lo que tarda en morir alguno.
    const float DemoraMaxima = 12f;
    // Las distancias del escupidor plantado, para la mediana: la horda empuja contra el jugador quieto y la explosion del
    // hinchado despide a todos 2,5 m, asi que los extremos son ruido.
    static readonly List<float> lejos = new List<float>(), cerca = new List<float>();
    // Cuando cada escupidor (por su numero de aparicion) llego a su tercer escupitajo: en ese mismo cuadro sigue plantado
    // lejos, y recien en el paso de fisica siguiente sale a acercarse. Lo de cerca se mide un rato despues.
    static readonly Dictionary<int, float> tercerEscupitajo = new Dictionary<int, float>();
    const float DespuesDelTercero = 1.5f;

    static readonly FieldInfo invulnerable = typeof(PlayerHealth).GetField("invulnerableHasta", BindingFlags.Instance | BindingFlags.NonPublic);
    static readonly FieldInfo escupitajos = typeof(ZombiEscupidor).GetField("escupitajosDeEstaAparicion", BindingFlags.Instance | BindingFlags.NonPublic);

    static PruebaEspecialesEnElLibre()
    {
        EditorApplication.update += Tick;
    }

    [MenuItem("ShowBies/Pruebas/Especiales en el modo libre (play)")]
    // Publico para correrlo por codigo (ver la trampa del registro de menus).
    public static void Arrancar()
    {
        if (!RespaldoDelBanco.PuedeArrancar(Banco)) return;
        RespaldoDelBanco.Guardar(Banco);
        PruebaDisparo.ModoTelefono(Clave);
        PlayerSettings.runInBackground = true;
        PlayerJS.FijarAutoApuntar(true);
        var c = CatalogoMejoras.Instancia;
        // Que el automatico mate rapido (asi los lugares rotan) y sin la furia ni el bate de por medio.
        Progreso.DepurarFijarNivel(c.danoBala.id, 12);
        Progreso.DepurarFijarNivel(c.vidaMaxima.id, 10);
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
            preparado = false;
            error = null;
            oks.Clear();
            notas.Length = 0;
            primerNivel.Clear();
            masVivos.Clear();
            masEspeciales = 0;
            desbloqueado.Clear();
            primeraVez.Clear();
            ocupacion.Clear();
            medido = generalLleno = especialesLlenos = ambosLlenos = especialesPorTiempo = todosPorTiempo = 0f;
            ultimoTiempo = Time.time;
            lejos.Clear();
            cerca.Clear();
            tercerEscupitajo.Clear();
            desde = Time.time;
            inicio = EditorApplication.timeSinceStartup;
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
        if (vida == null) return;
        if (!preparado)
        {
            if (Time.time - desde < 0.5f) return;
            generador = Object.FindAnyObjectByType<GeneradorZombis>();
            if (generador == null) { Terminar("ShowBies1 no tiene GeneradorZombis"); return; }
            Ok("el modo libre esta andando (GeneradorZombis.EnElLibre)", GeneradorZombis.EnElLibre);
            Ok("el generador tiene los cinco especiales", generador.especiales != null && generador.especiales.Length == 5);
            generador.segundosPorNivel = SegundosPorNivel;
            foreach (var cajas in Object.FindObjectsByType<PowerUp>(FindObjectsSortMode.None)) cajas.enabled = false;
            preparado = true;
            return;
        }

        // Inmortal y quieto en el centro: lo que se mide es el generador, no si sobrevive.
        if (invulnerable != null) invulnerable.SetValue(vida, Time.time + 10f);
        int nivel = generador.NivelActual;

        int especiales = WaveManager.EspecialesVivos(generador.especiales, EnemyController.VivosDe);
        masEspeciales = Mathf.Max(masEspeciales, especiales);
        float dt = Time.time - ultimoTiempo;
        ultimoTiempo = Time.time;
        int ultimoNivel = 0;
        foreach (var tipo in generador.especiales) ultimoNivel = Mathf.Max(ultimoNivel, tipo.desdeOleada);
        if (nivel >= ultimoNivel && dt > 0f)
        {
            bool general = EnemyController.ZombisVivos + EnemyController.Reservados >= generador.maxZombisVivos;
            bool suyos = especiales >= WaveManager.MaxEspecialesVivos;
            medido += dt;
            especialesPorTiempo += especiales * dt;
            todosPorTiempo += EnemyController.ZombisVivos * dt;
            if (general) generalLleno += dt;
            if (suyos) especialesLlenos += dt;
            if (general && suyos) ambosLlenos += dt;
        }
        foreach (var tipo in generador.especiales)
        {
            int vivos = EnemyController.VivosDe(tipo.prefab);
            if (nivel >= tipo.desdeOleada && !desbloqueado.ContainsKey(tipo.prefab)) desbloqueado[tipo.prefab] = Time.time;
            if (vivos > 0 && !primeraVez.ContainsKey(tipo.prefab)) primeraVez[tipo.prefab] = Time.time;
            if (nivel >= ultimoNivel)
            {
                float antesOcupado;
                ocupacion.TryGetValue(tipo.prefab, out antesOcupado);
                ocupacion[tipo.prefab] = antesOcupado + vivos * dt;
            }
            if (vivos > 0 && !primerNivel.ContainsKey(tipo.prefab)) primerNivel[tipo.prefab] = nivel;
            int antes;
            masVivos.TryGetValue(tipo.prefab, out antes);
            masVivos[tipo.prefab] = Mathf.Max(antes, vivos);
        }

        // El escupidor: plantado, lejos hasta su tercer escupitajo y cerca despues.
        foreach (var e in Object.FindObjectsByType<ZombiEscupidor>(FindObjectsSortMode.None))
        {
            var z = e.GetComponent<EnemyController>();
            if (z == null || !z.Vivo || e.EstadoActual != ZombiEscupidor.Estado.Plantado) continue;
            Vector3 d = e.transform.position - vida.transform.position;
            d.y = 0f;
            float distancia = d.magnitude;
            int tiros = escupitajos != null ? (int)escupitajos.GetValue(e) : 0;
            if (tiros < e.escupitajosEnElLibre)
            {
                lejos.Add(distancia);
            }
            else
            {
                float llego;
                if (!tercerEscupitajo.TryGetValue(z.NumeroDeAparicion, out llego)) tercerEscupitajo[z.NumeroDeAparicion] = llego = Time.time;
                if (Time.time - llego < DespuesDelTercero) continue;
                cerca.Add(distancia);
            }
        }

        bool todos = primerNivel.Count == generador.especiales.Length;
        bool escupidorVisto = lejos.Count > 30 && cerca.Count > 30;
        if (todos && escupidorVisto && nivel >= 12) { Terminar(null); return; }
        if (nivel >= 25) Terminar(todos ? "el escupidor no llego a plantarse lejos y cerca" : "no salieron los cinco especiales");
    }

    static float Mediana(List<float> valores)
    {
        if (valores.Count == 0) return 0f;
        var ordenados = new List<float>(valores);
        ordenados.Sort();
        return ordenados[ordenados.Count / 2];
    }

    static void Terminar(string motivo)
    {
        if (terminado) return;
        terminado = true;
        if (error == null) error = motivo;

        if (generador != null && generador.especiales != null)
        {
            foreach (var tipo in generador.especiales)
            {
                string nombre = tipo.prefab != null ? tipo.prefab.name : "?";
                int primero;
                bool salio = primerNivel.TryGetValue(tipo.prefab, out primero);
                int mas;
                masVivos.TryGetValue(tipo.prefab, out mas);
                Ok(nombre + " salio desde el nivel " + (salio ? primero.ToString() : "-") + " (desde el " + tipo.desdeOleada + ")",
                   salio && primero >= tipo.desdeOleada);
                Ok(nombre + ": como mucho " + mas + " vivos a la vez (tope " + tipo.maxVivos + ")", mas <= tipo.maxVivos);
            }
        }
        Ok("como mucho " + masEspeciales + " especiales vivos a la vez (tope " + WaveManager.MaxEspecialesVivos + ")", masEspeciales <= WaveManager.MaxEspecialesVivos);
        float medianaLejos = Mediana(lejos), medianaCerca = Mediana(cerca);
        Ok("el escupidor, antes del tercer escupitajo, plantado a " + medianaLejos.ToString("0.0") + " m de mediana (9,5; " + lejos.Count + " muestras)",
           lejos.Count > 0 && medianaLejos > 8.8f && medianaLejos < 10.1f);
        Ok("despues, a " + medianaCerca.ToString("0.0") + " m de mediana (6,5, adentro de los 7,5 del auto-apuntado; " + cerca.Count + " muestras)",
           cerca.Count > 0 && medianaCerca < 7.3f);
        notas.AppendLine("Nivel al terminar: " + (generador != null ? generador.NivelActual : 0) + " (uno cada " + SegundosPorNivel + " s).");
        if (generador != null && generador.especiales != null)
        {
            foreach (var tipo in generador.especiales)
            {
                float d, p, o;
                bool hay = desbloqueado.TryGetValue(tipo.prefab, out d);
                bool salio = primeraVez.TryGetValue(tipo.prefab, out p);
                ocupacion.TryGetValue(tipo.prefab, out o);
                notas.AppendLine(tipo.prefab.name + ": en promedio " + (medido > 0f ? (o / medido).ToString("0.0") : "-") + " vivos desde que estan los cinco");
                Ok(tipo.prefab.name + " salio " + (salio && hay ? (p - d).ToString("0.0") + " s" : "-") + " despues de desbloquearse (como mucho " + DemoraMaxima + ")",
                   salio && hay && p - d <= DemoraMaxima);
            }
        }
        if (medido > 0f)
            notas.AppendLine("Desde que estan los cinco (" + medido.ToString("0") + " s): el tope de todos (" + generador.maxZombisVivos + ") lleno el "
                             + (100f * generalLleno / medido).ToString("0") + " % del tiempo, el de los especiales (" + WaveManager.MaxEspecialesVivos
                             + ") el " + (100f * especialesLlenos / medido).ToString("0") + " % y los dos a la vez el "
                             + (100f * ambosLlenos / medido).ToString("0") + " %. En promedio, " + (especialesPorTiempo / medido).ToString("0.0")
                             + " especiales y " + (todosPorTiempo / medido).ToString("0.0") + " zombis vivos.");

        var inf = new StringBuilder();
        inf.AppendLine("Prueba de los especiales en el modo libre (ShowBies1, camino del telefono)");
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
        inf.AppendLine("RESULTADO: " + (todo ? "TODO OK" : "HAY FALLAS"));
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(Ruta)));
        File.WriteAllText(Ruta, inf.ToString());

        SessionState.SetBool(Clave, false);
        EditorApplication.ExitPlaymode();
        Debug.Log(inf.ToString());
    }
}
