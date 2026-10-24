using System;
using System.IO;
using System.Security;
using System.Text;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEngine;

// Lo que haria el plugin de Gradle de google-services, a mano (1.7.1): Firebase lee al arrancar google_app_id, el
// numero del proyecto, la clave y compania de unos recursos de texto, y el plugin los saca de google-services.json.
// Aca no hay plugin (ni el de Unity, que trae el EDM4U): al generar el proyecto de Gradle se escribe ese archivo de
// recursos en unityLibrary con los datos del paquete que se esta armando, el de Play o el .prueba (la APK de prueba
// cambia el paquete mientras dura). Assets no se toca. El json es Firebase/google-services.json, versionado: Google
// lo documenta como no secreto (la clave solo sirve para ese paquete), no hay que rotarla.
public class FirebaseEnElBuild : IPostGenerateGradleAndroidProject
{
    public const string RutaDelJson = "Firebase/google-services.json";
    public const string Archivo = "firebase_valores.xml";

    public int callbackOrder { get { return 0; } }

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        string paquete = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android);
        string error;
        string xml = File.Exists(RutaDelJson) ? ValoresXml(File.ReadAllText(RutaDelJson), paquete, out error)
                                              : FaltaElJson(out error);
        if (xml == null) throw new BuildFailedException("FirebaseEnElBuild: " + error);
        string carpeta = Path.Combine(path, "src", "main", "res", "values");
        Directory.CreateDirectory(carpeta);
        File.WriteAllText(Path.Combine(carpeta, Archivo), xml, new UTF8Encoding(false));
        Debug.Log("FirebaseEnElBuild: recursos de Firebase para " + paquete);
    }

    // Para que ConstructorAndroid corte antes de compilar: null si el json tiene el paquete.
    public static string Problema(string paquete)
    {
        string error;
        if (!File.Exists(RutaDelJson)) return FaltaElJson(out error) ?? error;
        return ValoresXml(File.ReadAllText(RutaDelJson), paquete, out error) == null ? error : null;
    }

    private static string FaltaElJson(out string error)
    {
        error = "falta " + RutaDelJson + " (se baja de la consola de Firebase, proyecto showbies)";
        return null;
    }

    // Los recursos para un paquete, o null con el motivo. Estatico y puro, para la prueba de logica.
    public static string ValoresXml(string json, string paquete, out string error)
    {
        error = null;
        Raiz raiz;
        try { raiz = JsonUtility.FromJson<Raiz>(json); }
        catch (Exception e) { error = "google-services.json no se puede leer: " + e.Message; return null; }
        if (raiz == null || raiz.project_info == null || raiz.client == null) { error = "google-services.json sin proyecto o sin apps"; return null; }

        Cliente cliente = null;
        foreach (var c in raiz.client)
            if (c != null && c.client_info != null && c.client_info.android_client_info != null
                && c.client_info.android_client_info.package_name == paquete) cliente = c;
        if (cliente == null) { error = "google-services.json no tiene la app " + paquete + " (registrarla en la consola de Firebase)"; return null; }
        string clave = cliente.api_key != null && cliente.api_key.Length > 0 ? cliente.api_key[0].current_key : null;
        if (string.IsNullOrEmpty(cliente.client_info.mobilesdk_app_id) || string.IsNullOrEmpty(clave)
            || string.IsNullOrEmpty(raiz.project_info.project_number) || string.IsNullOrEmpty(raiz.project_info.project_id))
        {
            error = "google-services.json incompleto para " + paquete;
            return null;
        }

        var sb = new StringBuilder();
        sb.Append("<?xml version=\"1.0\" encoding=\"utf-8\"?>\n");
        sb.Append("<!-- Lo escribe FirebaseEnElBuild desde Firebase/google-services.json: no se edita. -->\n");
        sb.Append("<resources>\n");
        Cadena(sb, "google_app_id", cliente.client_info.mobilesdk_app_id);
        Cadena(sb, "gcm_defaultSenderId", raiz.project_info.project_number);
        Cadena(sb, "google_api_key", clave);
        Cadena(sb, "google_crash_reporting_api_key", clave);
        Cadena(sb, "project_id", raiz.project_info.project_id);
        if (!string.IsNullOrEmpty(raiz.project_info.storage_bucket)) Cadena(sb, "google_storage_bucket", raiz.project_info.storage_bucket);
        sb.Append("</resources>\n");
        return sb.ToString();
    }

    private static void Cadena(StringBuilder sb, string nombre, string valor)
    {
        sb.Append("    <string name=\"").Append(nombre).Append("\" translatable=\"false\">")
          .Append(SecurityElement.Escape(valor)).Append("</string>\n");
    }

    // La forma de google-services.json que hace falta (JsonUtility no lee diccionarios: clases con los mismos nombres).
    [Serializable] private class Raiz { public Proyecto project_info; public Cliente[] client; }
    [Serializable] private class Proyecto { public string project_number; public string project_id; public string storage_bucket; }
    [Serializable] private class Cliente { public InfoCliente client_info; public Clave[] api_key; }
    [Serializable] private class InfoCliente { public string mobilesdk_app_id; public InfoAndroid android_client_info; }
    [Serializable] private class InfoAndroid { public string package_name; }
    [Serializable] private class Clave { public string current_key; }
}
