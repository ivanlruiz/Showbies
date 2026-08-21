using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.IO;

// Construye el APK de Android desde el menu (Build > Android APK) o por linea
// de comandos con -executeMethod ConstructorAndroid.BuildApk.
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
    const string RutaResultado = CarpetaSalida + "/build_result.txt";

    [MenuItem("Build/Android APK")]
    public static void BuildApk()
    {
        Directory.CreateDirectory(CarpetaSalida);
        if (File.Exists(RutaResultado)) File.Delete(RutaResultado);

        var escenas = EditorBuildSettings.scenes;
        var habilitadas = new System.Collections.Generic.List<string>();
        foreach (var e in escenas)
            if (e.enabled) habilitadas.Add(e.path);

        try
        {
            BuildReport reporte = BuildPipeline.BuildPlayer(
                habilitadas.ToArray(), RutaApk, BuildTarget.Android, BuildOptions.None);

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
}
