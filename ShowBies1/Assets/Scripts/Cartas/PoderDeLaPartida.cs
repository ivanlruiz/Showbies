using System;
using System.Collections.Generic;
using UnityEngine;

// Una carta elegida en la partida, y cuantas veces. Se guarda con la partida en curso.
[Serializable]
public class CartaElegida
{
    public string id;
    public int veces;
}

// Lo de la partida que se guarda al empezar cada oleada, con los puntos (Progreso, v8): el PODER,
// la experiencia que se lleva hacia el siguiente, las elecciones que quedaron sin hacer, la
// semilla del sorteo, si la proxima eleccion trae la carta dorada, y las cartas.
[Serializable]
public class EstadoPoder
{
    public int poder;
    public int experiencia;
    public int pendientes;
    public int semilla;
    public bool dorada;
    // Las muertes que lleva la carta de vampiro hacia la proxima cura.
    public int bajasDelVampiro;
    public List<CartaElegida> cartas = new List<CartaElegida>();

    public EstadoPoder Copia()
    {
        var c = (EstadoPoder)MemberwiseClone();
        c.cartas = new List<CartaElegida>();
        if (cartas != null)
            foreach (var e in cartas)
                if (e != null) c.cartas.Add(new CartaElegida { id = e.id, veces = e.veces });
        return c;
    }
}

// El PODER de la partida (1.6.0, ver CARTAS.md): matar zombis da experiencia (Enemy.experiencia,
// desde DanoZombi), al llenar la barra se sube un PODER y queda una eleccion pendiente, que
// EleccionDeCartas abre en cuanto se puede. Las cartas elegidas viven aca, y el arma, el jugador,
// la furia y las monedas leen sus multiplicadores: nadie las "aplica", asi retomar una partida no
// depende del orden en que arranca cada cosa.
//
// Solo hay PODER en una partida del libre o de las oleadas (Activo, que prende EleccionDeCartas al
// instalarse). Fuera de eso (el menu, el tutorial, la prueba de logica) todo vale neutro.
public static class PoderDeLaPartida
{
    // Los ids se guardan en el JSON: no se renombran.
    public const string Dano = "dano";
    public const string Cadencia = "cadencia";
    public const string Critico = "critico";
    public const string Vida = "vida";
    public const string Velocidad = "velocidad";
    public const string Iman = "iman";
    public const string GranadaRapida = "granada_rapida";
    public const string FuriaLarga = "furia_larga";
    // Las que cambian las reglas (etapa 2, elegidas por Gemini el 12/10 pensando en el Redmi:
    // sin Rebote, y la Electrica paso a ser Quemadura).
    public const string Perforante = "perforante";
    public const string DisparoDoble = "disparo_doble";
    public const string HaciaAtras = "hacia_atras";
    public const string Explosiva = "explosiva";
    public const string Hielo = "hielo";
    public const string Quemadura = "quemadura";
    public const string Dron = "dron";
    public const string Vampiro = "vampiro";
    public const string Escudo = "escudo";
    // La de reserva: sale sola cuando todas las demas llegaron a su tope. Llena la vida.
    public const string Curacion = "curacion";

    // Lo fijo de las cartas de reglas; lo que se balancea por vez esta en el valor de cada asset.
    public const float DanoAlPerforar = 0.5f;          // cada zombi que atraviesa, la mitad del anterior
    public const float AbanicoDelDisparoDoble = 8f;    // grados entre bala y bala
    public const float RadioDeLaExplosiva = 1.5f;      // y medio metro mas por cada vez despues de la primera
    public const float RadioPorVezDeLaExplosiva = 0.5f;
    public const float DuracionDelHielo = 2f;
    public const float DuracionDeLaQuemadura = 3f;
    public const float QuemaduraPorSegundo = 0.1f;     // del daño de la bala que la prendio
    public const float AlcanceDelDron = 12f;
    // El vampiro cura cada 20 muertes (era 15 hasta el 14/10: con la cura de las cajas y la
    // carta de vida, el jugador se curaba mas rapido de lo que le pegaban; de Argon).
    public const int BajasParaElVampiro = 20;
    // Con el disparo doble cada bala adelante pega esto del tiro (14/10, de Argon): una bala de
    // mas al 100 % duplicaba el daño con una sola carta en la oleada 2. Con una carta, 2 x 0,7;
    // con dos, 3 x 0,7. Tambien los perdigones de la escopeta.
    public const float ParteDelDisparoDoble = 0.7f;

    // La curva: pasar del PODER L al siguiente cuesta round(14 x 1,15^L). Con la mezcla de zombis
    // de WaveMode y oleadas de 10 + 2n, la primera carta sale al empezar la oleada 2, y hay 9
    // hasta la oleada 10, 15 hasta la 20 y 23 hasta la 40. Arrancaba en 8 hasta el 12/10 (en el
    // telefono Ivan tenia tres cartas a mitad de la oleada 2) y en 12 hasta el 14/10, cuando la
    // 1.6.0 quedo demasiado facil en las primeras partidas: lo cerro Argon (Gemini) con la
    // simulacion (simulaciones/cartas_1.6.0, resultados_arranque_v3.md).
    public const int PrimerCosto = 14;
    public const double CrecimientoDelCosto = 1.15;
    public const int CartasPorEleccion = 3;

    private static EstadoPoder estado = new EstadoPoder();
    private static readonly Dictionary<string, int> veces = new Dictionary<string, int>();

    // Hay PODER en esta escena (una partida del libre o de las oleadas).
    public static bool Activo { get; private set; }

    // Los bancos en play y MedirPartida lo prenden: matan zombis sin parar y una eleccion los
    // dejaria congelados con tres cartas en pantalla. La experiencia se suma igual.
    public static bool SinCartasAutomaticas { get; set; }

    // Sube con cada cambio: la barra y el HUD lo miran en vez de suscribirse.
    public static int Revision { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetearEstadoCompartido()
    {
        Terminar();
        SinCartasAutomaticas = false;
        Revision = 0;
    }

    // --- La partida -------------------------------------------------------------------------

    // Empieza una partida: de cero, con otra semilla, o como estaba guardada al empezar la oleada
    // que se retoma.
    public static void Empezar(EstadoPoder guardado)
    {
        estado = guardado != null ? guardado.Copia() : new EstadoPoder { semilla = UnityEngine.Random.Range(1, int.MaxValue) };
        if (estado.cartas == null) estado.cartas = new List<CartaElegida>();
        estado.poder = Math.Max(0, estado.poder);
        estado.experiencia = Math.Max(0, estado.experiencia);
        estado.pendientes = Mathf.Clamp(estado.pendientes, 0, estado.poder);
        if (estado.semilla == 0) estado.semilla = UnityEngine.Random.Range(1, int.MaxValue);
        veces.Clear();
        foreach (var e in estado.cartas)
            if (e != null && !string.IsNullOrEmpty(e.id) && e.veces > 0) veces[e.id] = e.veces;
        Activo = true;
        Revision++;
    }

    // Fuera de una partida: todo neutro.
    public static void Terminar()
    {
        estado = new EstadoPoder();
        veces.Clear();
        Activo = false;
        Revision++;
    }

    // Lo que se guarda al empezar cada oleada (WaveManager), con las cartas como estan.
    public static EstadoPoder Instantanea()
    {
        var copia = estado.Copia();
        copia.cartas.Clear();
        foreach (var par in veces) copia.cartas.Add(new CartaElegida { id = par.Key, veces = par.Value });
        return copia;
    }

    // --- La experiencia ---------------------------------------------------------------------

    public static int Poder { get { return estado.poder; } }
    public static int Experiencia { get { return estado.experiencia; } }
    public static int Pendientes { get { return estado.pendientes; } }
    public static bool DoradaPendiente { get { return estado.dorada; } }
    public static int Semilla { get { return estado.semilla; } }

    public static int Costo(int poder)
    {
        return (int)Math.Round(PrimerCosto * Math.Pow(CrecimientoDelCosto, Math.Max(0, poder)));
    }

    public static int CostoActual { get { return Costo(estado.poder); } }

    // Desde DanoZombi, en el bloque de la muerte. Puede subir mas de un PODER de golpe.
    public static void Sumar(int experiencia)
    {
        if (!Activo || experiencia <= 0) return;
        estado.experiencia += experiencia;
        while (estado.experiencia >= Costo(estado.poder))
        {
            estado.experiencia -= Costo(estado.poder);
            estado.poder++;
            estado.pendientes++;
        }
        Revision++;
    }

    // Al morir un jefe: la proxima eleccion trae una carta dorada, que vale doble.
    public static void MarcarDorada()
    {
        if (!Activo) return;
        estado.dorada = true;
        Revision++;
    }

    // --- Las cartas -------------------------------------------------------------------------

    public static int Veces(string id)
    {
        int n;
        return id != null && veces.TryGetValue(id, out n) ? n : 0;
    }

    // Lo que vale cada vez, del asset; sin catalogo, el numero del diseño.
    private static float Valor(string id, float porDefecto)
    {
        var catalogo = CatalogoCartas.Instancia;
        var carta = catalogo != null ? catalogo.Buscar(id) : null;
        return carta != null ? carta.valor : porDefecto;
    }

    private static float Multiplica(string id, float porDefecto)
    {
        int n = Activo ? Veces(id) : 0;
        return n > 0 ? Mathf.Pow(Valor(id, porDefecto), n) : 1f;
    }

    private static float Suma(string id, float porDefecto)
    {
        int n = Activo ? Veces(id) : 0;
        return n > 0 ? Valor(id, porDefecto) * n : 0f;
    }

    public static float MultiplicadorDano { get { return Multiplica(Dano, 1.2f); } }
    public static float MultiplicadorCadencia { get { return Multiplica(Cadencia, 1.15f); } }
    public static float CriticoExtra { get { return Suma(Critico, 0.1f); } }
    public static float MultiplicadorVida { get { return Multiplica(Vida, 1.2f); } }
    public static float MultiplicadorVelocidad { get { return 1f + Suma(Velocidad, 0.1f); } }
    public static float ImanExtra { get { return Suma(Iman, 2f); } }
    public static float MultiplicadorRecargaGranada { get { return Multiplica(GranadaRapida, 0.7f); } }
    public static float SegundosFuriaExtra { get { return Suma(FuriaLarga, 2f); } }

    // Las de reglas. Todas neutras fuera de una partida, como las de numeros.
    public static int Perforaciones { get { return Mathf.RoundToInt(Suma(Perforante, 1f)); } }
    public static int BalasExtraAdelante { get { return Mathf.RoundToInt(Suma(DisparoDoble, 1f)); } }
    public static int BalasAtras { get { return Mathf.RoundToInt(Suma(HaciaAtras, 1f)); } }
    public static float FraccionDeLaExplosion { get { return Suma(Explosiva, 0.3f); } }
    public static float RadioDeLaExplosion
    {
        get { int n = Activo ? Veces(Explosiva) : 0; return n > 0 ? RadioDeLaExplosiva + RadioPorVezDeLaExplosiva * (n - 1) : 0f; }
    }
    // Cuanto frena el hielo: 40 % con una, 64 % con dos (se compone, nunca para del todo).
    public static float LentitudDelHielo
    {
        get { int n = Activo ? Veces(Hielo) : 0; return n > 0 ? 1f - Mathf.Pow(1f - Valor(Hielo, 0.4f), n) : 0f; }
    }
    public static float ProbabilidadDeQuemadura { get { return Mathf.Clamp01(Suma(Quemadura, 0.25f)); } }
    public static int Drones { get { return Activo ? Veces(Dron) : 0; } }
    public static float TirosPorSegundoDelDron { get { return Drones > 0 ? Valor(Dron, 1f) : 0f; } }
    public static float CuraDelVampiro { get { return Suma(Vampiro, 0.03f); } }
    public static bool ConEscudo { get { return Activo && Veces(Escudo) > 0; } }
    public static float RecargaDelEscudo { get { return Valor(Escudo, 15f); } }

    // Una muerte para el vampiro: devuelve si toca curar (cada BajasParaElVampiro).
    public static bool ContarBajaDelVampiro()
    {
        if (!Activo || Veces(Vampiro) <= 0) return false;
        estado.bajasDelVampiro++;
        if (estado.bajasDelVampiro < BajasParaElVampiro) return false;
        estado.bajasDelVampiro = 0;
        return true;
    }

    public static int BajasDelVampiro { get { return estado.bajasDelVampiro; } }

    // Si la carta puede salir ahora: no llego a su tope y tiene lo que necesita comprado.
    public static bool Disponible(Carta carta, int vecesQueTiene, bool conGranada, bool conFuria)
    {
        if (carta == null || string.IsNullOrEmpty(carta.id) || carta.id == Curacion) return false;
        if (carta.tope <= 0 || vecesQueTiene >= carta.tope) return false;
        if (carta.requiere == Carta.Requisito.Granada && !conGranada) return false;
        if (carta.requiere == Carta.Requisito.Furia && !conFuria) return false;
        return true;
    }

    // Las tres de una eleccion: sin repetir, entre las disponibles, con la semilla de la partida y
    // el PODER que se esta eligiendo (salir y volver no las cambia). Sin ninguna disponible, la de
    // reserva. Estatico y puro para probarlo sin escena.
    public static List<string> Sortear(IList<Carta> cartas, Func<Carta, bool> disponible, int semilla, int poder, out int dorada, bool conDorada)
    {
        var rng = new System.Random(unchecked(semilla * 73856093 ^ (poder + 1) * 19349663));
        var candidatas = new List<string>();
        if (cartas != null)
            foreach (var c in cartas)
                if (disponible(c) && !candidatas.Contains(c.id)) candidatas.Add(c.id);
        candidatas.Sort(StringComparer.Ordinal);   // el orden del asset no cambia el sorteo

        var elegidas = new List<string>();
        while (elegidas.Count < CartasPorEleccion && candidatas.Count > 0)
        {
            int i = rng.Next(candidatas.Count);
            elegidas.Add(candidatas[i]);
            candidatas.RemoveAt(i);
        }
        if (elegidas.Count == 0) elegidas.Add(Curacion);
        dorada = conDorada && elegidas[0] != Curacion ? rng.Next(elegidas.Count) : -1;
        return elegidas;
    }

    // Las tres de la eleccion pendiente, con lo comprado de verdad.
    public static List<string> Ofrecer(out int dorada)
    {
        var catalogo = CatalogoCartas.Instancia;
        bool conGranada = CatalogoMejoras.GranadaDesbloqueada;
        bool conFuria = Progreso.Nivel("furia") > 0;
        int eligiendo = Math.Max(0, estado.poder - estado.pendientes);
        return Sortear(catalogo != null ? catalogo.cartas : null,
                       c => Disponible(c, Veces(c != null ? c.id : null), conGranada, conFuria),
                       estado.semilla, eligiendo, out dorada, estado.dorada);
    }

    // Elige una de las que se ofrecieron: la suma (la dorada vale dos veces, sin pasar del tope)
    // y gasta una eleccion. Devuelve cuantas veces se sumaron.
    public static int Elegir(string id, bool esDorada)
    {
        if (!Activo || string.IsNullOrEmpty(id) || estado.pendientes <= 0) return 0;
        estado.pendientes--;
        if (esDorada) estado.dorada = false;
        int sumadas = 0;
        if (id != Curacion)
        {
            var catalogo = CatalogoCartas.Instancia;
            var carta = catalogo != null ? catalogo.Buscar(id) : null;
            int tope = carta != null && carta.tope > 0 ? carta.tope : int.MaxValue;
            int antes = Veces(id);
            int despues = Math.Min(tope, antes + (esDorada ? 2 : 1));
            sumadas = despues - antes;
            if (despues > 0) veces[id] = despues;
        }
        Revision++;
        return sumadas;
    }

    // Las cartas que se tienen, en el orden en que se eligieron por primera vez: el HUD.
    public static IEnumerable<KeyValuePair<string, int>> Elegidas { get { return veces; } }
}
