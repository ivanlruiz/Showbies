using System.Collections.Generic;
using UnityEngine;

// Las mejoras que existen y lo que rinden con los niveles comprados. Vive en
// Resources (Assets/Mejoras/Resources/CatalogoMejoras.asset) para que lo encuentre
// cualquiera sin cablearlo en el inspector: el arma, la vida, los generadores y la
// tienda estan en escenas distintas y todos necesitan lo mismo.
//
// Si el catalogo falta, todo queda en los valores base (el nivel 0 de los assets)
// con un LogError: el juego sigue andando, sin mejoras.
[CreateAssetMenu(fileName = "CatalogoMejoras", menuName = "ShowBies/Catalogo de mejoras")]
public class CatalogoMejoras : ScriptableObject
{
    public const string RutaEnResources = "CatalogoMejoras";

    // Lo que rinde el jugador sin ninguna mejora, igual que el nivel 0 de los
    // assets: 1 de dano por bala, 4 tiros por segundo, 80 de vida y sin iman.
    // Arranca flojo a proposito: lo que lo hace fuerte son las compras.
    public const float DanoPorBalaSinCatalogo = 1f;
    public const float TirosPorSegundoSinCatalogo = 4f;
    public const int VidaMaximaSinCatalogo = 80;
    public const float RadioImanSinCatalogo = 0f;

    // Techo de la cuenta de ComprasPosibles: la insignia muestra "99+" y con
    // muchas monedas no vale la pena seguir sumando.
    public const int MaximoComprasContadas = 99;

    [Header("Las que usa el juego")]
    public Mejora danoBala;
    public Mejora cadencia;
    public Mejora vidaMaxima;
    public Mejora iman;
    public Mejora botin;

    [Header("Las tarjetas de la tienda, en orden")]
    public Mejora[] enTienda;

    private static CatalogoMejoras instancia;
    private static bool avisoDeFaltante;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetearEstadoCompartido()
    {
        instancia = null;
        avisoDeFaltante = false;
    }

    // Cacheado despues de la primera carga. Si no esta, se reintenta en cada
    // llamada (por si el asset aparece, como al crearlo en el editor) pero el
    // error se loguea una sola vez, porque los getters se consultan seguido.
    public static CatalogoMejoras Instancia
    {
        get
        {
            if (instancia == null)
            {
                instancia = Resources.Load<CatalogoMejoras>(RutaEnResources);
                if (instancia == null && !avisoDeFaltante)
                {
                    avisoDeFaltante = true;
                    Debug.LogError("CatalogoMejoras: no se encontro Resources/" + RutaEnResources
                        + ". Las mejoras quedan sin efecto.");
                }
            }
            return instancia;
        }
    }

    public static float DanoPorBala
    {
        get
        {
            CatalogoMejoras catalogo = Instancia;
            if (catalogo == null || catalogo.danoBala == null) return DanoPorBalaSinCatalogo;
            return (float)catalogo.danoBala.Valor(Progreso.Nivel(catalogo.danoBala.id));
        }
    }

    public static float TirosPorSegundo
    {
        get
        {
            CatalogoMejoras catalogo = Instancia;
            if (catalogo == null || catalogo.cadencia == null) return TirosPorSegundoSinCatalogo;
            return (float)catalogo.cadencia.Valor(Progreso.Nivel(catalogo.cadencia.id));
        }
    }

    public static int VidaMaxima
    {
        get
        {
            CatalogoMejoras catalogo = Instancia;
            if (catalogo == null || catalogo.vidaMaxima == null) return VidaMaximaSinCatalogo;
            return Mathf.RoundToInt((float)catalogo.vidaMaxima.Valor(Progreso.Nivel(catalogo.vidaMaxima.id)));
        }
    }

    // Lo que crece la vida sobre su base. La cura de las cajas escala con esto,
    // para que curen la misma fraccion de la vida mejorada.
    public static float MultiplicadorVida
    {
        get
        {
            CatalogoMejoras catalogo = Instancia;
            if (catalogo == null || catalogo.vidaMaxima == null) return 1f;
            return (float)catalogo.vidaMaxima.Multiplicador(Progreso.Nivel(catalogo.vidaMaxima.id));
        }
    }

    public static float MultiplicadorBotin
    {
        get
        {
            CatalogoMejoras catalogo = Instancia;
            if (catalogo == null || catalogo.botin == null) return 1f;
            return (float)catalogo.botin.Valor(Progreso.Nivel(catalogo.botin.id));
        }
    }

    // Los metros desde los que las monedas del piso vuelan solas al jugador. 0 sin
    // la mejora: se agarran igual al pasarles por encima (Moneda.distanciaDeCobro).
    public static float RadioIman
    {
        get
        {
            CatalogoMejoras catalogo = Instancia;
            if (catalogo == null || catalogo.iman == null) return RadioImanSinCatalogo;
            return (float)catalogo.iman.Valor(Progreso.Nivel(catalogo.iman.id));
        }
    }

    // Cuantas compras se pueden hacer seguidas con las monedas de ahora, siempre
    // comprando la mas barata. No es "cuantas mejoras cuestan menos que lo que
    // tengo": con 100 monedas se pagan iman (30) y dano (40), pero no ademas
    // vida (40), aunque cada una por separado alcance. Asi la insignia promete
    // compras que de verdad se pueden hacer. Los niveles se simulan en una copia.
    public static int ComprasPosibles()
    {
        CatalogoMejoras catalogo = Instancia;
        if (catalogo == null || catalogo.enTienda == null) return 0;

        Mejora[] mejoras = catalogo.enTienda;
        int[] niveles = new int[mejoras.Length];
        for (int i = 0; i < mejoras.Length; i++)
        {
            if (mejoras[i] != null) niveles[i] = Progreso.Nivel(mejoras[i].id);
        }

        long disponibles = Progreso.MonedasEnteras;
        double gastado = 0;
        int compras = 0;
        while (compras < MaximoComprasContadas)
        {
            int masBarata = -1;
            double precioMasBarata = 0;
            for (int i = 0; i < mejoras.Length; i++)
            {
                Mejora mejora = mejoras[i];
                if (mejora == null || mejora.EnTope(niveles[i])) continue;

                double precio = mejora.Precio(niveles[i]);
                if (masBarata < 0 || precio < precioMasBarata)
                {
                    masBarata = i;
                    precioMasBarata = precio;
                }
            }

            if (masBarata < 0 || gastado + precioMasBarata > disponibles) break;

            gastado += precioMasBarata;
            niveles[masBarata]++;
            compras++;
        }
        return compras;
    }

    // Un texto por problema; vacia si el catalogo esta bien armado. La usan las
    // pruebas de editor, porque un catalogo roto no da error: deja mejoras neutras.
    public List<string> Validar()
    {
        var problemas = new List<string>();

        if (enTienda == null || enTienda.Length == 0)
        {
            problemas.Add("enTienda esta vacio");
        }
        else
        {
            for (int i = 0; i < enTienda.Length; i++)
            {
                if (enTienda[i] == null)
                {
                    problemas.Add("enTienda[" + i + "] es nulo");
                    continue;
                }
                for (int j = 0; j < i; j++)
                {
                    if (enTienda[j] == enTienda[i])
                    {
                        problemas.Add("enTienda[" + i + "] repite " + enTienda[i].name);
                        break;
                    }
                }
            }
        }

        RevisarTipada(danoBala, "danoBala", problemas);
        RevisarTipada(cadencia, "cadencia", problemas);
        RevisarTipada(vidaMaxima, "vidaMaxima", problemas);
        RevisarTipada(iman, "iman", problemas);
        RevisarTipada(botin, "botin", problemas);

        // Ids vacios o repetidos entre todas las mejoras distintas del catalogo:
        // dos mejoras con el mismo id compartirian el nivel guardado.
        var revisadas = new List<Mejora>();
        SumarSiFalta(revisadas, danoBala);
        SumarSiFalta(revisadas, cadencia);
        SumarSiFalta(revisadas, vidaMaxima);
        SumarSiFalta(revisadas, iman);
        SumarSiFalta(revisadas, botin);
        if (enTienda != null)
        {
            for (int i = 0; i < enTienda.Length; i++) SumarSiFalta(revisadas, enTienda[i]);
        }

        for (int i = 0; i < revisadas.Count; i++)
        {
            Mejora mejora = revisadas[i];
            if (string.IsNullOrEmpty(mejora.id))
            {
                problemas.Add(mejora.name + " no tiene id");
                continue;
            }
            for (int j = 0; j < i; j++)
            {
                if (revisadas[j].id == mejora.id)
                {
                    problemas.Add(mejora.name + " repite el id \"" + mejora.id + "\" de " + revisadas[j].name);
                    break;
                }
            }
        }

        return problemas;
    }

    private void RevisarTipada(Mejora mejora, string campo, List<string> problemas)
    {
        if (mejora == null)
        {
            problemas.Add("falta la referencia " + campo);
            return;
        }
        if (enTienda == null || System.Array.IndexOf(enTienda, mejora) < 0)
        {
            problemas.Add(campo + " (" + mejora.name + ") no esta en enTienda");
        }
    }

    private static void SumarSiFalta(List<Mejora> lista, Mejora mejora)
    {
        if (mejora != null && !lista.Contains(mejora)) lista.Add(mejora);
    }
}
