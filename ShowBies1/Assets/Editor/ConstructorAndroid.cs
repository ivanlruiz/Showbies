using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

// Builds de Android desde el menu Build o por linea de comandos con
// -executeMethod ConstructorAndroid.BuildApk / BuildAab.
//
//  - "Android APK": firmado con el keystore de debug, para probar en el telefono.
//  - "Android AAB (release)": firmado con el keystore de release, que es lo
//    que se sube a la Play Store. Lee ruta, alias y passwords de
//    ShowBies1/keystore.local (gitignoreado; ver keystore.local.example) para
//    no tipearlos en el editor ni guardarlos en ProjectSettings.
//
// Escribe el resultado en Builds/build_result.txt (en la raiz del repo, que
// esta gitignoreada) ademas de loguearlo, para poder saber como termino una
// build lanzada sin supervision. Por linea de comandos (-batchmode), una build
// que falla o que no llega a arrancar sale con codigo 1.
//
// Antes de compilar revisa lo que el repo sabe que se rompe sin avisar: la
// calidad de Android (CalidadDeAndroid), el orden de las escenas, y el paquete y
// el nombre que la APK cambia y devuelve (si el editor se cae en el medio, quedan
// cambiados en disco).
public static class ConstructorAndroid
{
    // El directorio de trabajo del editor es la carpeta del proyecto
    // (ShowBies1), asi que ../Builds cae en la raiz del repo.
    const string CarpetaSalida = "../Builds";
    const string RutaApk = CarpetaSalida + "/ShowBies.apk";
    const string RutaAab = CarpetaSalida + "/ShowBies.aab";
    const string RutaResultado = CarpetaSalida + "/build_result.txt";
    const string RutaKeystoreLocal = "keystore.local";
    const string SufijoPaquetePrueba = ".prueba";
    const string SufijoNombrePrueba = " (prueba)";

    // El paquete con que la app esta registrada en Play Console: una app publicada no lo
    // cambia nunca, y con otro Play no acepta el AAB.
    public const string PaqueteDePlay = "com.ivanruiz.showbies";

    // Las escenas del build, en el orden de la tabla del CLAUDE.md. Los indices estan
    // escritos en el codigo: con otro orden la build sale igual y la navegacion se rompe
    // en el telefono sin ningun aviso (lo mismo mira ProbarOrdenDeEscenas).
    public static readonly string[] EscenasEnOrden = { "Menu", "ShowBies1", "Perdiste", "WaveMode", "Tutorial" };

    // Si algo fallo en esta build: lo anota Fallar, o un reporte que no es Succeeded. Es
    // del editor y no del juego: vuelve a falso al empezar cada build.
    static bool fallo;

    [MenuItem("Build/Android APK")]
    public static void BuildApk()
    {
        fallo = false;
        ArmarApk();
        SalirSiFallo();
    }

    static void ArmarApk()
    {
        // El paquete y el nombre de prueba se ponen y se devuelven en el finally de abajo:
        // si el editor se cae en el medio quedan asi en disco, y la APK siguiente saldria
        // como ".prueba.prueba". Se corta antes de tocar nada.
        string paquete = PaqueteAndroid;
        string nombre = PlayerSettings.productName;
        string problema = ProblemaDeLaApk(paquete, nombre);
        if (problema != null)
        {
            Fallar(problema);
            return;
        }

        EditorUserBuildSettings.buildAppBundle = false;
        PlayerSettings.Android.useCustomKeystore = false;

        // La APK es para probar en el telefono: el anuncio falso (el cartel con la
        // barra) tiene que llegar si o si, sin importar como quedo el asset.
        var proveedorAnterior = FijarProveedorDeAnuncios(ConfigAnuncios.Proveedor.Falso);

        // Otro paquete y otro nombre: la de Play esta firmada con otra clave y Android no
        // deja instalar una encima de la otra (habia que desinstalar y se perdia el
        // progreso). Asi las dos conviven en el telefono, cada una con su progreso.
        PaqueteAndroid = paquete + SufijoPaquetePrueba;
        PlayerSettings.productName = nombre + SufijoNombrePrueba;
        try
        {
            Construir(RutaApk);
        }
        finally
        {
            PaqueteAndroid = paquete;
            PlayerSettings.productName = nombre;
            RestaurarProveedorDeAnuncios(proveedorAnterior);
            AssetDatabase.SaveAssets();
        }
    }

    // El paquete de Android (applicationIdentifier), con el que Play reconoce la app.
    public static string PaqueteAndroid
    {
        get { return PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android); }
        private set { PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, value); }
    }

    // Por que el AAB no puede salir con ese paquete y ese nombre, o null si puede. Va a
    // Play: con otro paquete Play no lo acepta, y con el nombre de la APK de prueba si,
    // y en los telefonos se veria "ShowBies (prueba)".
    public static string ProblemaDelAab(string paquete, string nombre)
    {
        if (paquete != PaqueteDePlay)
            return "el paquete de Android es '" + paquete + "' y el AAB tiene que salir con '" + PaqueteDePlay
                + "', el registrado en Play Console" + ComoDevolverlos;
        if (EsNombreDePrueba(nombre))
            return "el nombre del producto es '" + nombre + "', el de la APK de prueba" + ComoDevolverlos;
        return null;
    }

    // Por que la APK de prueba no puede salir, o null si puede: le suma el sufijo al
    // paquete y al nombre, y si ya lo tienen saldria un ".prueba.prueba", otra app mas en
    // el telefono.
    public static string ProblemaDeLaApk(string paquete, string nombre)
    {
        if (paquete != null && paquete.EndsWith(SufijoPaquetePrueba))
            return "el paquete de Android ya es el de prueba ('" + paquete + "')" + ComoDevolverlos;
        if (EsNombreDePrueba(nombre))
            return "el nombre del producto ya es el de prueba ('" + nombre + "')" + ComoDevolverlos;
        return null;
    }

    static bool EsNombreDePrueba(string nombre)
    {
        return nombre != null && nombre.TrimEnd().EndsWith(SufijoNombrePrueba.Trim());
    }

    const string ComoDevolverlos = ". Si lo dejo asi una APK que no termino (el editor se cerro en el medio), "
        + "revertir ProjectSettings/ProjectSettings.asset con git o corregirlo en Player Settings.";

    // Las rutas de las escenas prendidas del build: las que entran y las que numera
    // SceneManager.LoadScene.
    public static List<string> EscenasHabilitadas()
    {
        var habilitadas = new List<string>();
        foreach (var e in EditorBuildSettings.scenes)
            if (e != null && e.enabled) habilitadas.Add(e.path);
        return habilitadas;
    }

    // Por que esas escenas no sirven para la build, o null si son las del juego y en su
    // orden, sin ninguna de mas.
    public static string ProblemaDeEscenas(IList<string> rutas)
    {
        var nombres = new List<string>();
        if (rutas != null)
            foreach (string ruta in rutas) nombres.Add(Path.GetFileNameWithoutExtension(ruta));
        bool iguales = nombres.Count == EscenasEnOrden.Length;
        for (int i = 0; iguales && i < nombres.Count; i++) iguales = nombres[i] == EscenasEnOrden[i];
        if (iguales) return null;
        return "las escenas prendidas del build son " + (nombres.Count > 0 ? string.Join(", ", nombres) : "ninguna")
            + " y tienen que ser, en este orden, " + string.Join(", ", EscenasEnOrden)
            + ": los indices estan escritos en el codigo (ver la tabla de escenas en CLAUDE.md)";
    }

    // Cambia el proveedor del asset para esta build y devuelve el que habia, o null
    // si no hay asset. Al terminar se restaura, asi la build no deja el asset
    // cambiado en el repo.
    static ConfigAnuncios.Proveedor? FijarProveedorDeAnuncios(ConfigAnuncios.Proveedor cual)
    {
        var config = Resources.Load<ConfigAnuncios>(ConfigAnuncios.RutaEnResources);
        if (config == null) return null;

        var anterior = config.proveedor;
        if (anterior == cual) return anterior;

        config.proveedor = cual;
        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();
        return anterior;
    }

    static void RestaurarProveedorDeAnuncios(ConfigAnuncios.Proveedor? anterior)
    {
        if (anterior == null) return;

        var config = Resources.Load<ConfigAnuncios>(ConfigAnuncios.RutaEnResources);
        if (config == null || config.proveedor == anterior.Value) return;

        config.proveedor = anterior.Value;
        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Build/Android AAB (release)")]
    public static void BuildAab()
    {
        fallo = false;
        ArmarAab();
        SalirSiFallo();
    }

    static void ArmarAab()
    {
        var datos = LeerKeystoreLocal();
        if (datos == null) return;

        // El AAB es lo que se sube a la Play Store: el anuncio falso ahi seria una
        // pantalla de prueba en produccion. Se corta antes de compilar.
        var configAnuncios = Resources.Load<ConfigAnuncios>(ConfigAnuncios.RutaEnResources);
        if (configAnuncios != null && configAnuncios.proveedor == ConfigAnuncios.Proveedor.Falso)
        {
            Fallar("el proveedor de anuncios esta en Falso: no se sube a Play con el anuncio de prueba. "
                + "Cambialo en Assets/Anuncios/Resources/ConfigAnuncios.");
            return;
        }

        // El paquete y el nombre de Play: la APK los cambia y los devuelve en un finally,
        // pero si el editor se cae en el medio quedan cambiados en disco.
        string problema = ProblemaDelAab(PaqueteAndroid, PlayerSettings.productName);
        if (problema != null)
        {
            Fallar(problema);
            return;
        }

        var keystoreAnterior = PlayerSettings.Android.keystoreName;
        var aliasAnterior = PlayerSettings.Android.keyaliasName;
        // Los simbolos nativos van dentro del AAB: sin ellos, un crash de un tester llega a
        // Android vitals como direcciones sueltas en libil2cpp.so, y Play avisa en cada subida.
        var nivelSimbolos = UnityEditor.Android.UserBuildSettings.DebugSymbols.level;
        var formatoSimbolos = UnityEditor.Android.UserBuildSettings.DebugSymbols.format;
        UnityEditor.Android.UserBuildSettings.DebugSymbols.level = Unity.Android.Types.DebugSymbolLevel.SymbolTable;
        UnityEditor.Android.UserBuildSettings.DebugSymbols.format = Unity.Android.Types.DebugSymbolFormat.IncludeInBundle;
        EditorUserBuildSettings.buildAppBundle = true;
        PlayerSettings.Android.useCustomKeystore = true;
        PlayerSettings.Android.keystoreName = Path.GetFullPath(datos["keystore"]);
        PlayerSettings.Android.keystorePass = datos["storepass"];
        PlayerSettings.Android.keyaliasName = datos["alias"];
        PlayerSettings.Android.keyaliasPass = datos["keypass"];
        try
        {
            Construir(RutaAab);
        }
        finally
        {
            // Que no queden los passwords en memoria ni el keystore de release
            // en ProjectSettings (la ruta se serializa y ensuciaria el repo con
            // una ruta local): la build de APK sigue saliendo con el de debug.
            PlayerSettings.Android.keystorePass = "";
            PlayerSettings.Android.keyaliasPass = "";
            PlayerSettings.Android.keystoreName = keystoreAnterior;
            PlayerSettings.Android.keyaliasName = aliasAnterior;
            PlayerSettings.Android.useCustomKeystore = false;
            EditorUserBuildSettings.buildAppBundle = false;
            UnityEditor.Android.UserBuildSettings.DebugSymbols.level = nivelSimbolos;
            UnityEditor.Android.UserBuildSettings.DebugSymbols.format = formatoSimbolos;
            // La build escribe ProjectSettings a disco en el medio, con la ruta del keystore de
            // release: sin volver a guardar, el archivo queda con esa ruta local aunque en memoria
            // ya este restaurada (paso con las versiones 4 y 5).
            AssetDatabase.SaveAssets();
        }
    }

    static Dictionary<string, string> LeerKeystoreLocal()
    {
        Directory.CreateDirectory(CarpetaSalida);
        if (!File.Exists(RutaKeystoreLocal))
        {
            Fallar("falta " + RutaKeystoreLocal + " (copiar keystore.local.example y completarlo)");
            return null;
        }

        var datos = new Dictionary<string, string>();
        foreach (var linea in File.ReadAllLines(RutaKeystoreLocal))
        {
            var l = linea.Trim();
            if (l.Length == 0 || l.StartsWith("#")) continue;
            int i = l.IndexOf('=');
            if (i < 0) continue;
            datos[l.Substring(0, i).Trim()] = l.Substring(i + 1).Trim();
        }

        foreach (var clave in new[] { "keystore", "alias", "storepass", "keypass" })
        {
            if (!datos.ContainsKey(clave) || datos[clave].Length == 0)
            {
                Fallar("falta '" + clave + "' en " + RutaKeystoreLocal);
                return null;
            }
        }
        if (!File.Exists(datos["keystore"]))
        {
            Fallar("no existe el keystore " + Path.GetFullPath(datos["keystore"]));
            return null;
        }
        return datos;
    }

    static void Construir(string rutaSalida)
    {
        Directory.CreateDirectory(CarpetaSalida);
        if (File.Exists(RutaResultado)) File.Delete(RutaResultado);

        // Entrar y salir de play le borra a QualitySettings.asset el bloque que pone Android
        // en Medium, sin avisar: la build saldria en el nivel por defecto y el telefono iria
        // mucho mas lento (ver la trampa en CLAUDE.md). Se corta antes de compilar.
        if (!CalidadDeAndroid.EstaEnMedium())
        {
            Fallar("Android no esta en calidad Medium en ProjectSettings/QualitySettings.asset "
                + "(falta o cambio m_PerPlatformDefaultQuality): revertirlo con git, ver la trampa en CLAUDE.md");
            return;
        }

        var habilitadas = EscenasHabilitadas();
        string problemaDeEscenas = ProblemaDeEscenas(habilitadas);
        if (problemaDeEscenas != null)
        {
            Fallar(problemaDeEscenas);
            return;
        }

        try
        {
            BuildReport reporte = BuildPipeline.BuildPlayer(
                habilitadas.ToArray(), rutaSalida, BuildTarget.Android, BuildOptions.None);

            var s = reporte.summary;
            if (s.result != BuildResult.Succeeded) fallo = true;
            // La version y el versionCode, para saber que se armo sin abrir el editor: el
            // versionCode tiene que subir en cada subida a Play.
            string resumen =
                "resultado: " + s.result + "\n" +
                "version: " + PlayerSettings.bundleVersion + "\n" +
                "versionCode: " + PlayerSettings.Android.bundleVersionCode + "\n" +
                "errores: " + s.totalErrors + "\n" +
                "warnings: " + s.totalWarnings + "\n" +
                "duracion: " + s.totalTime + "\n" +
                "tamanio: " + s.totalSize + " bytes\n" +
                "salida: " + s.outputPath + "\n";
            File.WriteAllText(RutaResultado, resumen);
            Debug.Log("ConstructorAndroid: " + resumen);
        }
        catch (System.Exception e)
        {
            File.WriteAllText(RutaResultado, "EXCEPCION: " + e.Message + "\n" + e.StackTrace);
            throw;
        }
    }

    static void Fallar(string motivo)
    {
        fallo = true;
        // Puede fallar antes de Construir, que es la que crea la carpeta.
        Directory.CreateDirectory(CarpetaSalida);
        File.WriteAllText(RutaResultado, "resultado: Failed\n" + motivo + "\n");
        Debug.LogError("ConstructorAndroid: " + motivo);
    }

    // Por linea de comandos (-batchmode -executeMethod) Unity sale con 0 si el metodo no
    // tiro una excepcion, aunque la build haya fallado o no haya arrancado, y un script la
    // daba por buena. Se sale al final de BuildApk y BuildAab, despues de los finally que
    // devuelven el paquete, el nombre, el keystore y el proveedor de anuncios: salir en el
    // medio los dejaria cambiados en disco. Con el editor abierto no hace nada.
    static void SalirSiFallo()
    {
        if (fallo && Application.isBatchMode) EditorApplication.Exit(1);
    }
}
