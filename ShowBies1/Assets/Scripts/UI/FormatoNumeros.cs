using System;
using System.Globalization;

// Numeros para mostrar en pantalla: enteros con punto de miles hasta 99.999 y
// despues compactos (123 K, 4,5 M), para que un contador no desborde el HUD
// cuando crezcan los numeros del modo incremental. Arma los separadores a mano
// en vez de pedir la cultura es-AR, que no esta garantizada en todas las builds.
public static class FormatoNumeros
{
    public static string Compacto(double valor)
    {
        long entero = (long)Math.Floor(Math.Max(0, valor));
        if (entero < 100000) return ConPuntos(entero);
        if (entero < 1000000) return (entero / 1000) + " K";

        double millones = entero / 1000000.0;
        if (millones < 1000) return UnDecimal(millones) + " M";
        return UnDecimal(millones / 1000.0) + " MM";
    }

    private static string ConPuntos(long valor)
    {
        string digitos = valor.ToString(CultureInfo.InvariantCulture);
        var armado = new System.Text.StringBuilder();
        for (int i = 0; i < digitos.Length; i++)
        {
            if (i > 0 && (digitos.Length - i) % 3 == 0) armado.Append('.');
            armado.Append(digitos[i]);
        }
        return armado.ToString();
    }

    private static string UnDecimal(double valor)
    {
        return (Math.Floor(valor * 10) / 10).ToString("0.#", CultureInfo.InvariantCulture).Replace('.', ',');
    }
}
