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

    // Valores con decimales, como los de las mejoras (5,8 de dano, 21,6 tiros por
    // segundo): coma decimal, sin ceros de mas ("20", no "20,0") y redondeo al
    // mas cercano. El +1e-9 esta porque 5 * 1,15 da 5,7499999... en double y sin
    // el empujon mostraria "5,7" en vez de "5,8". Desde 1.000 se muestra entero
    // con puntos, y desde 100.000 compacto, igual que el resto de los numeros.
    public static string ConDecimales(double valor, int decimales)
    {
        if (double.IsNaN(valor) || double.IsInfinity(valor)) return "0";
        if (valor < 0)
        {
            string positivo = ConDecimales(-valor, decimales);
            return positivo == "0" ? positivo : "-" + positivo;
        }

        int d = Math.Max(0, Math.Min(3, decimales));
        if (valor >= 100000) return Compacto(valor);

        double escala = Math.Pow(10, d);
        double redondeado = Math.Floor(valor * escala + 0.5 + 1e-9) / escala;
        if (redondeado >= 1000) return ConPuntos((long)Math.Floor(valor + 0.5 + 1e-9));

        // "0.###" ya saca los ceros de la cola; el recorte cubre el caso en que
        // el double redondeado arrastre digitos de mas (5,8000000001).
        string texto = redondeado.ToString("0.###", CultureInfo.InvariantCulture);
        int punto = texto.IndexOf('.');
        if (punto >= 0)
        {
            texto = texto.Substring(0, Math.Min(texto.Length, punto + 1 + d)).TrimEnd('0').TrimEnd('.');
        }
        return texto.Replace('.', ',');
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
