using System;
using UnityEngine;

public enum CrecimientoEfecto { Aditivo, Multiplicativo }
public enum FormatoValor { Entero, UnDecimal, Multiplicador }
public enum EstadoMejora { Comprable, SinMonedas, EnTope, Invalida }
public enum ResultadoCompra { Comprada, SinMonedas, EnTope, Invalida }

// Una mejora de la tienda: que es, cuanto cuesta cada nivel y cuanto rinde. Es un
// asset y no codigo para poder balancear precios y efectos sin recompilar, como
// los Enemy. El nivel comprado NO vive aca sino en Progreso, guardado por id: el
// asset es igual para todos los jugadores y el nivel es de cada uno.
//
// Las cuentas usan double porque los precios crecen exponencial y un float pierde
// las unidades mucho antes de llegar al techo.
[CreateAssetMenu(fileName = "Mejora", menuName = "ShowBies/Mejora")]
public class Mejora : ScriptableObject
{
    [Tooltip("Con este id se guarda el nivel en progreso.json. No cambiarlo despues de publicar: "
        + "los jugadores perderian los niveles que compraron.")]
    public string id;
    [Tooltip("Solo para reconocerla en el inspector. Lo que ve el jugador sale de la tabla de textos: "
        + "mejora_<id>_nombre y mejora_<id>_unidad, en Assets/Idioma/Resources/Textos.txt.")]
    public string nombre;
    [Tooltip("Solo para el inspector, como el nombre: la unidad que se ve sale de mejora_<id>_unidad.")]
    public string unidad;
    public Color color = Color.white;
    public Sprite icono;
    [Tooltip("Letra o signo que se muestra en el icono mientras no haya sprite.")]
    public string simbolo;

    [Header("Precio")]
    public double precioInicial = 45;
    [Tooltip("Cada nivel cuesta esto por el anterior.")]
    public double crecimientoPrecio = 1.45;
    [Tooltip("0 = sin tope.")]
    public int nivelMaximo;

    [Header("Efecto")]
    public CrecimientoEfecto crecimiento = CrecimientoEfecto.Aditivo;
    [Tooltip("Aditivo: el multiplicador suma esto por nivel. Multiplicativo: se multiplica por (1 + esto) por nivel.")]
    public double efectoPorNivel = 0.15;
    [Tooltip("El valor en nivel 0 (en nivel 1 si arrancaEnCero), que el multiplicador escala.")]
    public double valorBase = 1;
    [Tooltip("Sin comprarla no hay efecto (vale 0) y el nivel 1 vale valorBase. Para mejoras que se desbloquean, como el iman.")]
    public bool arrancaEnCero;
    public FormatoValor formato = FormatoValor.UnDecimal;

    // Techo del precio: sin tope, un crecimiento exponencial termina en infinito
    // y un precio infinito rompe las comparaciones y el texto.
    public const double PrecioMaximo = 9e15;

    public bool TieneTope
    {
        get { return nivelMaximo > 0; }
    }

    public bool EnTope(int nivel)
    {
        return TieneTope && nivel >= nivelMaximo;
    }

    // El nivel que cuenta para el efecto: un nivel guardado por encima del tope
    // (porque se bajo el tope en el asset) rinde lo mismo que el tope.
    public int NivelEfectivo(int nivel)
    {
        return Mathf.Max(0, TieneTope ? Mathf.Min(nivel, nivelMaximo) : nivel);
    }

    // El precio de pasar de este nivel al siguiente. Redondeado al entero mas
    // cercano; el +1e-9 hace que 90 * 1,45 (130,4999... en double) de 131 y no 130.
    public double Precio(int nivel)
    {
        double crudo = precioInicial * Math.Pow(crecimientoPrecio, Math.Max(0, nivel));
        return Math.Min(PrecioMaximo, Math.Floor(crudo + 0.5 + 1e-9));
    }

    public double Multiplicador(int nivel)
    {
        int n = NivelEfectivo(nivel);
        if (crecimiento == CrecimientoEfecto.Multiplicativo) return Math.Pow(1 + efectoPorNivel, n);
        return 1 + efectoPorNivel * n;
    }

    // Con arrancaEnCero todo corre un nivel: el 0 no tiene efecto y el 1 vale lo que
    // valdria el 0. El tope se aplica antes de correrlo, asi un nivel guardado por
    // encima del tope no pasa del maximo.
    public double Valor(int nivel)
    {
        int n = NivelEfectivo(nivel);
        if (arrancaEnCero) return n == 0 ? 0 : valorBase * Multiplicador(n - 1);
        return valorBase * Multiplicador(n);
    }

    public string TextoValor(int nivel)
    {
        double valor = Valor(nivel);
        switch (formato)
        {
            case FormatoValor.Entero: return FormatoNumeros.ConDecimales(valor, 0);
            case FormatoValor.Multiplicador: return "×" + FormatoNumeros.ConDecimales(valor, 1);
            default: return FormatoNumeros.ConDecimales(valor, 1);
        }
    }

    // Un precio menor a 1 o un crecimiento menor a 1 harian mejoras gratis o que
    // se abaratan; un efecto negativo, una mejora que empeora.
    private void OnValidate()
    {
        precioInicial = Math.Max(1, precioInicial);
        crecimientoPrecio = Math.Max(1, crecimientoPrecio);
        efectoPorNivel = Math.Max(0, efectoPorNivel);
        nivelMaximo = Mathf.Max(0, nivelMaximo);
    }
}
