using UnityEditor;
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

    [MenuItem("Build/Android APK")]
    public static void BuildApk()
    {
        EditorUserBuildSettings.buildAppBundle = false;
        PlayerSettings.Android.useCustomKeystore = false;
        Construir(RutaApk);
    }

    [MenuItem("Build/Android AAB (release)")]
    public static void BuildAab()
    {
        var datos = LeerKeystoreLocal();
        if (datos == null) return;

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
            // como default: la build de APK sigue saliendo con el de debug.
            PlayerSettings.Android.keystorePass = "";
            PlayerSettings.Android.keyaliasPass = "";
            PlayerSettings.Android.useCustomKeystore = false;
            EditorUserBuildSettings.buildAppBundle = false;
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
