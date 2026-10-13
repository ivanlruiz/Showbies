using UnityEngine;

// Lo que tira cada arma de la mano (1.6.0, etapa 3 de las cartas; ArmaEnLaMano y
// GunController.TramoDelArma). Pedido de Ivan (12/10: "que no sean todos cubitos"; para la
// escopeta se imaginaba proyectiles muy chiquitos que recorren poca distancia), diseñado por
// Gemini en la misma revision de arte:
//
//   pistola   la bala de siempre, a 15 m/s y 1,5 s (22 m).
//   revolver  una bala mas grande (1,75x) y rapida, a 25 m/s y 1 s: pesada y contundente.
//   escopeta  cuatro perdigones chicos (0,75x) en abanico (-12, -4, 4 y 12 grados), a 12 m/s y
//             0,6 s: llegan a 7 m, y obligan a jugar de cerca.
//   rifle     una trazadora larga y fina (4x de largo, 0,5x de ancho), a 35 m/s y 0,8 s.
//   energia   una onda ancha (una esfera aplastada, 3x de ancho), lenta, a 12 m/s y 1,8 s, que
//             atraviesa dos zombis sin perder daño.
//
// Las medidas son proporciones de la bala de la pistola (0,09 m: Ivan las pidio mas chicas el
// 9/10). El revolver y la escopeta tiran al 70 % de la cadencia y el rifle al 120 %, para que
// cada arma tenga su ritmo, pero el daño de cada bala lo compensa: el daño por segundo es el
// de las mejoras con cualquier arma (una compra nunca lo puede bajar). La escopeta, ademas,
// reparte un tiro entre sus perdigones a un tercio cada uno: con los cuatro adentro pega el
// 133 %, y de lejos, con dos, el 67 % (premia acercarse). Las cartas van en cada perdigon; el
// disparo doble le suma dos perdigones a los costados (a 20 grados, y 8 mas por carta) y la de
// atras tira uno solo, gordo, con el daño del tiro entero.
public static class Proyectiles
{
    public const int Pistola = 0, Revolver = 1, Escopeta = 2, Rifle = 3, Energia = 4;

    public static readonly int[] Velocidad = { 15, 25, 12, 35, 12 };
    public static readonly float[] Vida = { 1.5f, 1f, 0.6f, 0.8f, 1.8f };
    public static readonly float[] Cadencia = { 1f, 0.7f, 0.7f, 1.2f, 1f };
    // Lo que se ve, en proporcion a la bala de la pistola (x el ancho, y el alto, z el largo).
    public static readonly Vector3[] Forma =
    {
        new Vector3(1f, 1f, 1f),
        new Vector3(1.75f, 1.75f, 1.75f),
        new Vector3(0.75f, 0.75f, 0.75f),
        new Vector3(0.5f, 0.5f, 4f),
        new Vector3(3f, 0.8f, 1.5f),
    };
    public static readonly bool[] Redonda = { false, false, false, false, true };
    public static readonly int[] PerforacionesPropias = { 0, 0, 0, 0, 2 };

    public static readonly float[] AngulosDeLaEscopeta = { -12f, -4f, 4f, 12f };
    public const float PartePorPerdigon = 1f / 3f;
    public const float AnguloDelDobleEnLaEscopeta = 20f;
    public const float AnguloPorCartaDeMas = 8f;
    public const float GordoDeAtras = 1.5f;   // el perdigon de la carta de atras, sobre el de la escopeta
    // El perdigon que lleva el charco de luz (uno solo por tiro, mas grande y tenue: con uno por
    // perdigon, la escopeta llenaba el piso de transparencias y el Redmi no las aguanta).
    public const int PerdigonConLuz = 1;

    // La critica: blanca y la mitad mas grande, con el charco del color de su tramo y una estela
    // de chispas (el rojo de antes se confundia con el magenta y el violeta de los tramos 4 y 5,
    // y con la sangre).
    public const float TamanioCritico = 1.5f;
    public static readonly Color ColorCritico = Color.white;
    public const float EstelaCadaMetros = 0.4f;

    public static int Arma(int tramoDelArma)
    {
        return Mathf.Clamp(tramoDelArma, 0, Velocidad.Length - 1);
    }

    // Hasta donde llega: lo usa el auto-apuntado, que no le tira a lo que no alcanza.
    public static float Alcance(int arma)
    {
        arma = Arma(arma);
        return Velocidad[arma] * Vida[arma];
    }

    // Cuantas balas salen hacia adelante en un tiro, con las cartas de disparo doble.
    public static int BalasAdelante(int arma, int cartasDeMas)
    {
        return Arma(arma) == Escopeta ? AngulosDeLaEscopeta.Length + 2 * cartasDeMas : 1 + cartasDeMas;
    }

    // El "perdigon" de las balas que no llevan charco: las de mas del disparo doble y la de atras.
    public const int SinLuz = -1;

    // El de la bala i del abanico del disparo doble (0 a "extra"): la del medio lleva el charco.
    public static int PerdigonDelAbanico(int i, int extra)
    {
        return i == extra / 2 ? 0 : SinLuz;
    }

    // Si la bala lleva el charco de luz: el rifle, que tira muy seguido, no; la escopeta, en un
    // solo perdigon; y de un tiro con las cartas de mas balas, solo la del medio (13/10, la
    // auditoria de rendimiento: con el disparo doble y la de atras eran cuatro charcos por tiro,
    // y con la cadencia alta el piso se llenaba de transparencias, como con la escopeta).
    public static bool LlevaLuz(int arma, int perdigon)
    {
        if (perdigon < 0) return false;
        arma = Arma(arma);
        if (arma == Rifle) return false;
        if (arma == Escopeta) return perdigon == PerdigonConLuz;
        return true;
    }
}
