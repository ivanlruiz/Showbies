using System.Collections.Generic;
using UnityEngine;

// Los textos del juego en todos los idiomas, leidos de una tabla:
// Assets/Idioma/Resources/Textos.txt. Una fila por texto, separada por TAB, con
// una columna por idioma. Se abre con cualquier planilla, y sumar un idioma es
// sumar una columna (y un valor en Lengua).
//
// Casi ningun texto es una frase suelta: son plantillas con {0}, {1} y rich text
// ("<size=55%>COINS</size>  {0}"). Se traduce la plantilla entera, formato
// incluido, asi cada idioma puede acomodar su propio orden y sus tamanios.
//
// Un id que falta no rompe nada pero se ve: sale "[id]" en pantalla y un aviso en
// la consola. Un texto vacio en un idioma cae al ingles, que es el idioma base.
public static class Textos
{
    public const string RutaEnResources = "Textos";

    private static Dictionary<string, string[]> tabla;
    private static HashSet<string> avisados;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetearEstadoCompartido()
    {
        tabla = null;
        avisados = null;
    }

    public static string De(string id)
    {
        if (string.IsNullOrEmpty(id)) return "";
        Cargar();

        string[] fila;
        if (tabla.TryGetValue(id, out fila))
        {
            string texto = fila[(int)Idioma.Actual];
            if (!string.IsNullOrEmpty(texto)) return texto;

            string ingles = fila[(int)Lengua.Ingles];
            if (!string.IsNullOrEmpty(ingles))
            {
                Avisar(id, "no tiene texto en " + Idioma.Codigo(Idioma.Actual) + ": se usa el ingles");
                return ingles;
            }
        }

        Avisar(id, "no esta en la tabla de textos");
        return "[" + id + "]";
    }

    public static string Formato(string id, object a)
    {
        return string.Format(De(id), a);
    }

    public static string Formato(string id, object a, object b)
    {
        return string.Format(De(id), a, b);
    }

    public static string Formato(string id, object a, object b, object c)
    {
        return string.Format(De(id), a, b, c);
    }

    // --- para las pruebas ---------------------------------------------------------

    public static IEnumerable<string> Ids
    {
        get
        {
            Cargar();
            return tabla.Keys;
        }
    }

    // El texto crudo de un idioma, sin caer al ingles ni avisar.
    public static string Crudo(string id, Lengua lengua)
    {
        Cargar();
        string[] fila;
        return tabla.TryGetValue(id, out fila) ? fila[(int)lengua] : null;
    }

    // Vuelve a leer el archivo: lo usan las pruebas y el editor despues de tocarlo.
    public static void Recargar()
    {
        tabla = null;
        avisados = null;
    }

    // --- lectura -------------------------------------------------------------------

    private static void Cargar()
    {
        if (tabla != null) return;
        tabla = new Dictionary<string, string[]>();

        var archivo = Resources.Load<TextAsset>(RutaEnResources);
        if (archivo == null)
        {
            Debug.LogError("Textos: falta Resources/" + RutaEnResources + ". Todos los textos van a salir como [id].");
            return;
        }

        Leer(archivo.text, tabla);
        Resources.UnloadAsset(archivo);
    }

    // Publico y sin Unity en el medio para poder probarlo con un texto armado. Los
    // problemas (una columna que falta, un id repetido) van a la consola, o a
    // 'alProblema' si las pruebas quieren contarlos.
    public static void Leer(string contenido, Dictionary<string, string[]> destino,
                            System.Action<string> alProblema = null)
    {
        if (alProblema == null) alProblema = Debug.LogError;

        int cantidadDeLenguas = System.Enum.GetValues(typeof(Lengua)).Length;
        int[] columnaDe = null;   // indice de columna por Lengua

        string[] lineas = contenido.Split('\n');
        for (int n = 0; n < lineas.Length; n++)
        {
            string linea = lineas[n].TrimEnd('\r');
            if (linea.Length == 0 || linea[0] == '#') continue;

            string[] celdas = linea.Split('\t');

            // La primera fila util es la cabecera: dice en que columna esta cada idioma.
            if (columnaDe == null)
            {
                columnaDe = new int[cantidadDeLenguas];
                for (int l = 0; l < cantidadDeLenguas; l++)
                {
                    string codigo = Idioma.Codigo((Lengua)l);
                    columnaDe[l] = System.Array.IndexOf(celdas, codigo);
                    if (columnaDe[l] < 0) alProblema("Textos: la tabla no tiene la columna \"" + codigo + "\".");
                }
                continue;
            }

            string id = celdas[0].Trim();
            if (id.Length == 0) continue;

            if (destino.ContainsKey(id))
            {
                alProblema("Textos: el id \"" + id + "\" esta repetido en la linea " + (n + 1) + ".");
                continue;
            }

            var fila = new string[cantidadDeLenguas];
            for (int l = 0; l < cantidadDeLenguas; l++)
            {
                int c = columnaDe[l];
                fila[l] = c >= 0 && c < celdas.Length ? Desescapar(celdas[c]) : "";
            }
            destino[id] = fila;
        }
    }

    // Una celda no puede tener un salto de linea de verdad: se escribe "\n".
    private static string Desescapar(string celda)
    {
        return celda.Replace("\\n", "\n");
    }

    private static void Avisar(string id, string problema)
    {
        if (avisados == null) avisados = new HashSet<string>();
        if (!avisados.Add(id)) return;
        Debug.LogWarning("Textos: \"" + id + "\" " + problema + ".");
    }
}
