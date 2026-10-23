using UnityEngine;

// Empezar las oleadas desde un capitulo (1.7.0; lo pidio Ivan el 19/10, "guardar los capitulos, seguir donde estabas", y
// lo diseño Argon el 21/10): completar la oleada 10 deja empezar en la 11, y completar la 20, en la 21. Solo los tres
// primeros: mas adelante serian 17 a 21 cartas antes del primer tiro.
//
// Quien carga WaveMode para una partida nueva (el selector, OTRA VEZ de la derrota, REINICIAR, el ¡A JUGAR! de la tienda)
// fija antes ParaEmpezar, y WaveManager lo toma al arrancar. Sin fijar, la 1: el editor, los bancos y lo que no sabe de
// capitulos arrancan como siempre. Una partida que se retoma no lo mira.
public static class Capitulos
{
    public const int OleadasPorCapitulo = 10;
    public const int Elegibles = 3;

    // El capitulo con que empieza la proxima partida de oleadas que no se retoma. 0 = sin fijar (la 1).
    public static int ParaEmpezar { get; set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetearEstadoCompartido()
    {
        ParaEmpezar = 0;
    }

    public static int OleadaInicial(int capitulo)
    {
        return (Mathf.Max(1, capitulo) - 1) * OleadasPorCapitulo + 1;
    }

    // El capitulo de una oleada, contando desde 1 (la 1 a la 10 son el 1).
    public static int CapituloDe(int oleada)
    {
        return oleada <= 0 ? 1 : (oleada - 1) / OleadasPorCapitulo + 1;
    }

    // Cuantos se pueden elegir con esa mejor oleada completada: el 1 siempre, el 2 con la 10 y el 3 con la 20.
    public static int Desbloqueados(int mejorOleada)
    {
        return Mathf.Clamp(1 + Mathf.Max(0, mejorOleada) / OleadasPorCapitulo, 1, Elegibles);
    }

    public static int Acotar(int capitulo, int mejorOleada)
    {
        return Mathf.Clamp(capitulo, 1, Desbloqueados(mejorOleada));
    }

    // El capitulo que cierra esa oleada con ¡CAPITULO N SUPERADO!: la 10 y la 20, las que destraban uno para empezar.
    // Las demas, 0 (de Argon: la 30 y las siguientes siguen con la oleada superada de siempre).
    public static int QueCierra(int oleada)
    {
        if (oleada <= 0 || oleada % OleadasPorCapitulo != 0) return 0;
        int capitulo = oleada / OleadasPorCapitulo;
        return capitulo < Elegibles ? capitulo : 0;
    }

    // Lo que paga el punto de control la primera vez en la vida: 250 en la 10 (destraba el 2) y 500 en la 20.
    public static int PremioDelPuntoDeControl(int capituloDestrabado)
    {
        return capituloDestrabado == 2 ? 250 : capituloDestrabado == 3 ? 500 : 0;
    }

    // Las siguientes veces, cruzar de capitulo cura esta parte de la vida maxima.
    public const float CuraAlCruzar = 0.5f;

    // Si completar esa oleada destraba un capitulo: devuelve cual, o 0.
    public static int QueDestraba(int mejorAntes, int oleadaCompletada)
    {
        int antes = Desbloqueados(mejorAntes);
        int despues = Desbloqueados(Mathf.Max(mejorAntes, oleadaCompletada));
        return despues > antes ? despues : 0;
    }

    // El capitulo de la ultima partida (o el que destrabo), con lo que se puede elegir ahora; sin una partida anterior, el
    // mas alto destrabado (de Argon, 22/10). Es al que vuelven OTRA VEZ, REINICIAR y el ¡A JUGAR! de la tienda.
    public static int ParaVolver
    {
        get
        {
            int mejor = Progreso.MejorOleada;
            int ultimo = Progreso.CapituloParaVolver;
            return ultimo > 0 ? Acotar(ultimo, mejor) : Desbloqueados(mejor);
        }
    }

    // Lo toma WaveManager al arrancar una partida nueva, y lo deja sin fijar.
    public static int TomarParaEmpezar()
    {
        int capitulo = ParaEmpezar > 0 ? ParaEmpezar : 1;
        ParaEmpezar = 0;
        return Acotar(capitulo, Progreso.MejorOleada);
    }
}
