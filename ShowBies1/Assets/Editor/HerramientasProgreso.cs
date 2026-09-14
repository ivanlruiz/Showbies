using System.IO;
using UnityEditor;
using UnityEngine;

// Atajos para probar la tienda y las mejoras sin jugar horas: sumar monedas,
// poner niveles conocidos y volver a cero. Escriben el progreso.json REAL del
// editor (persistentDataPath), el mismo que lee el juego en play.
//
// Funcionan en edicion y en play. En play los niveles nuevos se aplican recien
// al recargar la escena (AplicarMejoras corre en el Awake del jugador), y la
// tienda abierta se refresca sola porque Progreso sube la Revision.
//
// Los metodos publicos no abren dialogos para poder llamarlos por linea de
// comandos o desde un RunCommand; el unico que pregunta es el item del menu
// "Reiniciar todo".
public static class HerramientasProgreso
{
    // Los niveles de "Niveles de prueba": los mismos que usa el plan de pruebas
    // en modo play (daño 6 por bala, 14 tiros/s, 180 de vida, iman de 5 m,
    // botin x2,5).
    const int NivelDanoDePrueba = 5;
    const int NivelCadenciaDePrueba = 10;
    const int NivelVidaDePrueba = 5;
    const int NivelImanDePrueba = 6;
    const int NivelBotinDePrueba = 15;

    [MenuItem("ShowBies/Progreso/Sumar 1.000 monedas")]
    static void SumarMilMonedas()
    {
        SumarMonedas(1000);
    }

    [MenuItem("ShowBies/Progreso/Sumar 100.000 monedas")]
    static void SumarCienMilMonedas()
    {
        SumarMonedas(100000);
    }

    [MenuItem("ShowBies/Progreso/Niveles de prueba (5, 10, 5, 6, 15)")]
    static void NivelesDePruebaDesdeMenu()
    {
        FijarNivelesDePrueba();
    }

    [MenuItem("ShowBies/Progreso/Niveles en cero")]
    static void NivelesEnCeroDesdeMenu()
    {
        NivelesEnCero();
    }

    [MenuItem("ShowBies/Progreso/Reiniciar todo")]
    static void ReiniciarConDialogo()
    {
        bool confirmado = EditorUtility.DisplayDialog(
            "Reiniciar progreso",
            "Se pierden las monedas, la mejor oleada y los niveles de todas las mejoras de\n" +
            Progreso.RutaArchivo + "\n\nNo se puede deshacer.",
            "Reiniciar", "Cancelar");
        if (confirmado) ReiniciarSinPreguntar();
    }

    [MenuItem("ShowBies/Progreso/Abrir carpeta")]
    static void AbrirCarpeta()
    {
        // Sin partida jugada el archivo todavia no existe: se abre la carpeta.
        string ruta = Progreso.RutaArchivo;
        EditorUtility.RevealInFinder(File.Exists(ruta) ? ruta : Path.GetDirectoryName(ruta));
    }

    // Por DepurarFijarMonedas y no por Sumar: Sumar cuenta como monedas ganadas en
    // la partida (MonedasDeLaPartida), y esto no se gano jugando.
    public static void SumarMonedas(double cantidad)
    {
        Progreso.DepurarFijarMonedas(Progreso.Monedas + cantidad);
        Debug.Log("HerramientasProgreso: ahora hay " + FormatoNumeros.Compacto(Progreso.Monedas) +
                  " monedas en " + Progreso.RutaArchivo);
    }

    public static void FijarNivelesDePrueba()
    {
        FijarNiveles(NivelDanoDePrueba, NivelCadenciaDePrueba, NivelVidaDePrueba, NivelImanDePrueba, NivelBotinDePrueba);
    }

    public static void NivelesEnCero()
    {
        FijarNiveles(0, 0, 0, 0, 0);
    }

    public static void ReiniciarSinPreguntar()
    {
        Progreso.ReiniciarTodo();
        Debug.Log("HerramientasProgreso: progreso reiniciado en " + Progreso.RutaArchivo);
    }

    // Con los ids que tienen los assets del catalogo y no con ids escritos aca:
    // si alguien renombra un id, esto sigue apuntando a la mejora de verdad.
    static void FijarNiveles(int dano, int cadencia, int vida, int iman, int botin)
    {
        var catalogo = CatalogoMejoras.Instancia;
        if (catalogo == null)
        {
            Debug.LogError("HerramientasProgreso: no hay catalogo en Resources/" + CatalogoMejoras.RutaEnResources);
            return;
        }

        Fijar(catalogo.danoBala, "danoBala", dano);
        Fijar(catalogo.cadencia, "cadencia", cadencia);
        Fijar(catalogo.vidaMaxima, "vidaMaxima", vida);
        Fijar(catalogo.iman, "iman", iman);
        Fijar(catalogo.botin, "botin", botin);

        Debug.Log("HerramientasProgreso: niveles daño " + dano + ", cadencia " + cadencia + ", vida " + vida +
                  ", iman " + iman + ", botin " + botin + " en " + Progreso.RutaArchivo +
                  (EditorApplication.isPlaying ? " (se aplican al recargar la escena)" : ""));
    }

    static void Fijar(Mejora mejora, string campo, int nivel)
    {
        if (mejora == null || string.IsNullOrEmpty(mejora.id))
        {
            Debug.LogError("HerramientasProgreso: el catalogo no tiene '" + campo + "' (o no tiene id)");
            return;
        }
        Progreso.DepurarFijarNivel(mejora.id, nivel);
    }
}
