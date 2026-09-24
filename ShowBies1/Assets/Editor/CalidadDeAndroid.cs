using System.IO;
using UnityEngine;

// El nivel de calidad con el que sale la build de Android, leido del bloque
// m_PerPlatformDefaultQuality de ProjectSettings/QualitySettings.asset.
//
// Existe por la trampa de ProjectSettings (CLAUDE.md): entrar y salir de play desde el
// editor re-serializa QualitySettings.asset y le borra ese bloque, sin avisar nada, y la
// build de Android sale en el nivel por defecto en vez de Medium: el telefono va mucho mas
// lento. Lo miran la build (ConstructorAndroid), la prueba de logica y las fotos de los
// faroles.
public static class CalidadDeAndroid
{
    const string Ruta = "ProjectSettings/QualitySettings.asset";

    // El indice guardado para Android, o -1 si el bloque o la linea faltan.
    public static int Guardada()
    {
        try
        {
            bool enElBloque = false;
            foreach (string linea in File.ReadAllLines(Ruta))
            {
                if (linea.Contains("m_PerPlatformDefaultQuality")) { enElBloque = true; continue; }
                if (!enElBloque) continue;
                string limpia = linea.Trim();
                if (limpia.StartsWith("Android:"))
                {
                    int nivel;
                    if (int.TryParse(limpia.Substring("Android:".Length).Trim(), out nivel) && nivel >= 0 && nivel < QualitySettings.names.Length)
                        return nivel;
                    return -1;
                }
                if (!linea.StartsWith("    ")) break;
            }
        }
        catch (IOException)
        {
        }
        return -1;
    }

    // El nivel que tiene que tener Android (ver Rendimiento en movil en CLAUDE.md).
    public static int Medium
    {
        get { return System.Array.IndexOf(QualitySettings.names, "Medium"); }
    }

    public static bool EstaEnMedium()
    {
        int medium = Medium;
        return medium >= 0 && Guardada() == medium;
    }
}
