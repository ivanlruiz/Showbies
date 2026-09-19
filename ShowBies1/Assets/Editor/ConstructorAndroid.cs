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
// build lanzada sin supervision.
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

    [MenuItem("Build/Android APK")]
    public static void BuildApk()
    {
        EditorUserBuildSettings.buildAppBundle = false;
        PlayerSettings.Android.useCustomKeystore = false;

        // La APK es para probar en el telefono: el anuncio falso (el cartel con la
        // barra) tiene que llegar si o si, sin importar como quedo el asset.
        var proveedorAnterior = FijarProveedorDeAnuncios(ConfigAnuncios.Proveedor.Falso);

        // Otro paquete y otro nombre: la de Play esta firmada con otra clave y Android no
        // deja instalar una encima de la otra (habia que desinstalar y se perdia el
        // progreso). Asi las dos conviven en el telefono, cada una con su progreso.
        string paquete = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android);
        string nombre = PlayerSettings.productName;
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, paquete + SufijoPaquetePrueba);
        PlayerSettings.productName = nombre + " (prueba)";
        try
        {
            Construir(RutaApk);
        }
        finally
        {
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, paquete);
            PlayerSettings.productName = nombre;
            RestaurarProveedorDeAnuncios(proveedorAnterior);
            AssetDatabase.SaveAssets();
        }
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

        var habilitadas = new List<string>();
        foreach (var e in EditorBuildSettings.scenes)
            if (e.enabled) habilitadas.Add(e.path);

        try
        {
            BuildReport reporte = BuildPipeline.BuildPlayer(
                habilitadas.ToArray(), rutaSalida, BuildTarget.Android, BuildOptions.None);

            var s = reporte.summary;
            string resumen =
                "resultado: " + s.result + "\n" +
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
        File.WriteAllText(RutaResultado, "resultado: Failed\n" + motivo + "\n");
        Debug.LogError("ConstructorAndroid: " + motivo);
    }
}
