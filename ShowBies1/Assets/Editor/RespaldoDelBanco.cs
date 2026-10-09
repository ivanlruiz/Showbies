using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// Los bancos en play juegan con el progreso real del editor (progreso.json y los PlayerPrefs):
// algunos lo preparan a su gusto (de cero, con un millon de monedas, en la oleada 11) y jugar
// lo cambia (una partida mas, la oleada en curso, el record). Este respaldo lo guarda antes de
// que el banco toque nada y lo devuelve al volver a modo edicion, asi cualquier banco se corre
// desde el menu sin perder el progreso. Hasta el 27/9 lo tenia que hacer a mano quien lo
// corria, y los bancos de antes lo dejaban como terminaba la prueba.
//
// La copia va a Library/, que no esta en git ni en Temp (Windows limpia Temp: se llevo tres
// .bak de una copia hecha ahi). Una marca dice que hay una copia sin devolver: si el editor se
// cae en medio de un banco, se devuelve al abrirlo otra vez.
//
// Devuelve en modo edicion y no antes: al salir de play, Progreso guarda lo que tiene
// (Application.quitting) y pisaria lo devuelto.
[InitializeOnLoad]
public static class RespaldoDelBanco
{
    const string Carpeta = "Library/ShowBies/RespaldoDelBanco";
    const string Marca = "pendiente.txt";      // adentro, el banco que hizo la copia
    const string ArchivoDePrefs = "prefs.json";
    const string Patron = "progreso.json*";     // el principal y sus .tmp, .anterior, .roto y .bak

    // Las claves de PlayerPrefs del juego (ver Persistencia en CLAUDE.md). Si se agrega una,
    // va aca tambien.
    static readonly string[] Enteros =
        { "Score", "HighScore", "HighScore_1", "HighScore_3", "HighScore_4", "UltimoModo", "TutorialCompletado", "TemaOscuro" };
    static readonly string[] Reales = { "VolumenEfectos", "VolumenMusica" };
    static readonly string[] Cadenas = { "Idioma", "ResenaPedidaEn" };

    [System.Serializable]
    class Pref
    {
        public string clave;
        public string tipo;       // "entero", "real" o "cadena"
        public bool habia;
        public int entero;
        public float real;
        public string cadena;
    }

    [System.Serializable]
    class Prefs
    {
        public List<Pref> lista = new List<Pref>();
        // Lo del editor que los bancos cambian mientras corren. runInBackground lo prenden
        // todos para que el juego corra con Unity atras y lo apagan en play, al terminar, y
        // eso no queda: salia del banco prendido, y es un ajuste del proyecto (ProjectSettings).
        public bool enSegundoPlano;
        public bool tecladoEnElEditor;
    }

    static RespaldoDelBanco()
    {
        EditorApplication.playModeStateChanged += AlCambiarDeModo;
        // Un banco que no llego a devolver (el editor se cayo en play): al volver a abrirlo.
        // Tambien corre al recargar el dominio al entrar en play, y ahi no hace nada.
        EditorApplication.delayCall += () =>
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode) Restaurar();
        };
    }

    static string CarpetaCompleta { get { return Path.GetFullPath(Carpeta); } }

    // Si hay una copia sin devolver.
    public static bool Pendiente { get { return File.Exists(Path.Combine(CarpetaCompleta, Marca)); } }

    // El banco de la copia sin devolver, leido de la marca una vez por dominio: lo pregunta cada
    // banco en cada vuelta del editor. Null sin copia.
    static string bancoDeLaMarca;
    static bool marcaLeida;

    static string BancoDeLaMarca
    {
        get
        {
            if (!marcaLeida)
            {
                string marca = Path.Combine(CarpetaCompleta, Marca);
                bancoDeLaMarca = File.Exists(marca) ? File.ReadAllText(marca) : null;
                marcaLeida = true;
            }
            return bancoDeLaMarca;
        }
    }

    // Si la copia sin devolver es de ese banco.
    public static bool EsDe(string banco)
    {
        return BancoDeLaMarca == banco;
    }

    // Lo primero de cada banco, en su entrada de menu: ni en play ni con escenas sin guardar
    // (el banco las cerraria sin preguntar: ver EscenasSinGuardar). Avisa por que no arranca;
    // antes varios se negaban callados.
    public static bool PuedeArrancar(string banco)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning(banco + ": se arranca desde modo edicion, no en play.");
            return false;
        }
        return !EscenasSinGuardar.Hay(banco);
    }

    // Un banco sin su respaldo no corre. Cada banco se arma con una clave de SessionState y la
    // apaga al terminar; cortado a mano (Stop, un error de compilacion), la clave quedaba
    // prendida, este respaldo devolvia el progreso y borraba su marca, y el banco se adueñaba
    // del Play siguiente sin respaldo: mataba al jugador, olvidaba la oleada del progreso real y
    // pisaba su informe (superauditoria del 29/9). Lo llama cada banco en su Tick, ya en play:
    // si la copia no es suya, apaga su clave, avisa y no corre.
    public static bool SigueArmado(string banco, string clave)
    {
        if (EsDe(banco))
        {
            // Un banco no quiere un zombi del tesoro al azar (uno de cada cuatro desde la oleada
            // 3): huye, no cuenta en la oleada y hacia fallar de vez en cuando lo que mira a todos
            // los zombis. El que lo necesita (PruebaTesoro) lo saca a mano.
            WaveManager.SinTesoroAutomatico = true;
            return true;
        }
        SessionState.SetBool(clave, false);
        Debug.LogWarning(banco + ": quedo armado de una corrida cortada, sin su respaldo; no corre en este Play. Se vuelve a arrancar desde su menu.");
        return false;
    }

    // Lo llama cada banco al principio de Arrancar, antes de preparar nada.
    public static void Guardar(string banco)
    {
        // En play no: lo que hay en disco ya es lo de la partida, y devolverlo seria devolver eso.
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("RespaldoDelBanco: " + banco + " se arranca desde modo edicion; no se guardo nada.");
            return;
        }
        // Si quedo una copia sin devolver, la buena es esa: se devuelve antes de hacer otra.
        if (Pendiente) Restaurar();

        string carpeta = CarpetaCompleta;
        if (Directory.Exists(carpeta)) Directory.Delete(carpeta, true);
        Directory.CreateDirectory(carpeta);

        string origen = Application.persistentDataPath;
        if (Directory.Exists(origen))
        {
            foreach (var archivo in Directory.GetFiles(origen, Patron))
                File.Copy(archivo, Path.Combine(carpeta, Path.GetFileName(archivo)), true);
        }

        var prefs = new Prefs();
        foreach (var clave in Enteros)
            prefs.lista.Add(new Pref { clave = clave, tipo = "entero", habia = PlayerPrefs.HasKey(clave), entero = PlayerPrefs.GetInt(clave, 0) });
        foreach (var clave in Reales)
            prefs.lista.Add(new Pref { clave = clave, tipo = "real", habia = PlayerPrefs.HasKey(clave), real = PlayerPrefs.GetFloat(clave, 0f) });
        foreach (var clave in Cadenas)
            prefs.lista.Add(new Pref { clave = clave, tipo = "cadena", habia = PlayerPrefs.HasKey(clave), cadena = PlayerPrefs.GetString(clave, "") });
        prefs.enSegundoPlano = PlayerSettings.runInBackground;
        prefs.tecladoEnElEditor = EditorPrefs.GetBool(Plataforma.ClaveTecladoEnElEditor, false);
        File.WriteAllText(Path.Combine(carpeta, ArchivoDePrefs), JsonUtility.ToJson(prefs, true));

        // La marca va ultima: si esta, la copia esta entera.
        File.WriteAllText(Path.Combine(carpeta, Marca), banco);
        bancoDeLaMarca = banco;
        marcaLeida = true;
    }

    static void AlCambiarDeModo(PlayModeStateChange cambio)
    {
        if (cambio == PlayModeStateChange.EnteredEditMode) Restaurar();
    }

    // Devuelve el progreso y los PlayerPrefs como estaban antes del banco, y borra lo que el
    // banco haya dejado de mas (un .anterior, un .tmp). No hace nada si no hay copia.
    public static void Restaurar()
    {
        string carpeta = CarpetaCompleta;
        string marca = Path.Combine(carpeta, Marca);
        if (!File.Exists(marca) || EditorApplication.isPlayingOrWillChangePlaymode) return;
        string banco = File.ReadAllText(marca);

        string destino = Application.persistentDataPath;
        Directory.CreateDirectory(destino);
        var respaldados = new HashSet<string>();
        foreach (var archivo in Directory.GetFiles(carpeta, Patron)) respaldados.Add(Path.GetFileName(archivo));
        foreach (var archivo in Directory.GetFiles(destino, Patron))
            if (!respaldados.Contains(Path.GetFileName(archivo))) File.Delete(archivo);
        foreach (var nombre in respaldados)
            File.Copy(Path.Combine(carpeta, nombre), Path.Combine(destino, nombre), true);

        string rutaPrefs = Path.Combine(carpeta, ArchivoDePrefs);
        if (File.Exists(rutaPrefs))
        {
            var prefs = JsonUtility.FromJson<Prefs>(File.ReadAllText(rutaPrefs));
            foreach (var p in prefs.lista)
            {
                if (!p.habia) { PlayerPrefs.DeleteKey(p.clave); continue; }
                if (p.tipo == "entero") PlayerPrefs.SetInt(p.clave, p.entero);
                else if (p.tipo == "real") PlayerPrefs.SetFloat(p.clave, p.real);
                else PlayerPrefs.SetString(p.clave, p.cadena);
            }
            PlayerPrefs.Save();
            PlayerSettings.runInBackground = prefs.enSegundoPlano;
            EditorPrefs.SetBool(Plataforma.ClaveTecladoEnElEditor, prefs.tecladoEnElEditor);
        }

        // Lo que Progreso tiene en memoria es lo del banco: que lo vuelva a leer del disco.
        var campo = typeof(Progreso).GetField("datos", BindingFlags.NonPublic | BindingFlags.Static);
        if (campo != null) campo.SetValue(null, null);
        else Debug.LogWarning("RespaldoDelBanco: no se encontro Progreso.datos; el progreso en memoria sigue siendo el del banco hasta recargar el dominio.");

        File.Delete(marca);
        bancoDeLaMarca = null;
        marcaLeida = true;
        Debug.Log("RespaldoDelBanco: el progreso y los PlayerPrefs volvieron a como estaban antes de " + banco + ".");
    }
}
