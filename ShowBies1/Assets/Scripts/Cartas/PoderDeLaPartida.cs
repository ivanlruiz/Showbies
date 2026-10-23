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
    // Las elecciones de la preparacion de un capitulo (las tiradas dobles), mientras dura: 0 = no hay.
    public int preparacion;
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

    // La curva: pasar del PODER L al siguiente cuesta round(28 x 1,12^L). Con la mezcla de zombis
    // de WaveMode y oleadas de 10 + 2n, la primera carta sale en la oleada 3 y las cuatro primeras
    // hasta la 8 (un minuto y medio). Arrancaba en 8 hasta el 12/10 (en el telefono Ivan tenia tres
    // cartas a mitad de la oleada 2), en 12 hasta el 14/10, cuando la 1.6.0 quedo demasiado facil
    // en las primeras partidas, y en 14 x 1,15^L hasta el 16/10 (Ivan: "no jugue ni un minuto de
    // partida y ya tengo 4 cartas"). Las tres las cerro Argon (Gemini) con la simulacion
    // (simulaciones/cartas_1.6.0, resultados_arranque_v3.md y resultados_poder.md): esta espacia el
    // principio y deja casi igual el juego largo (27 cartas por partida, el muro en 59 y 69 para el
    // jugador medio y el alto).
    public const int PrimerCosto = 28;
    public const double CrecimientoDelCosto = 1.12;
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

    // --- La preparacion: las tiradas dobles ---------------------------------------------------

    // Empezar en el capitulo 2 o 3 (Capitulos, de Argon, 21/10): en lugar de las cartas que se habrian juntado hasta ahi,
    // una rafaga de elecciones al pisar el mapa (PREPARACION 1/3...), cada una de dos copias: 3 en la 11 y 6 en la 21.
    // Cada tercera es la dorada del jefe que se salto, y vale dos como las demas: siempre 6 o 12 cartas. El PODER queda en
    // el doble de las elecciones, con la barra en 0, asi la carta siguiente cuesta lo mismo que en una partida corrida.
    public const int EleccionesPorCapitulo = 3;
    public const int CopiasEnLaPreparacion = 2;

    public static int EleccionesDePreparacion(int capitulo)
    {
        return Math.Max(0, capitulo - 1) * EleccionesPorCapitulo;
    }

    // WaveManager, al empezar una partida en un capitulo. Antes del guardado de la primera oleada: retomarla vuelve a
    // ofrecer las mismas elecciones.
    public static void EmpezarPreparacion(int capitulo)
    {
        int elecciones = EleccionesDePreparacion(capitulo);
        if (!Activo || elecciones <= 0) return;
        estado.poder = elecciones * CopiasEnLaPreparacion;
        estado.experiencia = 0;
        estado.pendientes = elecciones;
        estado.preparacion = elecciones;
        estado.dorada = false;
        Revision++;
    }

    public static bool EnPreparacion
    {
        get { return Activo && estado.preparacion > 0 && estado.pendientes > 0; }
    }

    // La eleccion de la preparacion que toca (de 1 a TotalDePreparacion), o 0 fuera de ella.
    public static int EleccionDePreparacion
    {
        get { return EnPreparacion ? estado.preparacion - estado.pendientes + 1 : 0; }
    }

    public static int TotalDePreparacion { get { return estado.preparacion; } }

    // La tercera y la sexta son las doradas de los jefes de la 10 y la 20.
    public static bool PreparacionConDorada(int eleccion)
    {
        return eleccion > 0 && eleccion % EleccionesPorCapitulo == 0;
    }

    // Cuantas copias suma la eleccion que se esta haciendo: dos en la preparacion, dos la dorada y una las demas.
    public static int CopiasDeLaEleccion(bool dorada)
    {
        return EnPreparacion ? CopiasEnLaPreparacion : dorada ? 2 : 1;
    }

    // La copia que no entro en la elegida (la de tope 1, o la que estaba a una del tope) va a otra al azar entre las que
    // se pueden, con la semilla de la partida y la eleccion: retomar da la misma. Sin ninguna, nada. Pura, para probarla.
    public static string CartaDeRelleno(IList<Carta> cartas, Func<Carta, bool> disponible, string elegida, int semilla, int eleccion)
    {
        var candidatas = new List<string>();
        if (cartas != null)
            foreach (var c in cartas)
                if (c != null && c.id != elegida && disponible(c) && !candidatas.Contains(c.id)) candidatas.Add(c.id);
        if (candidatas.Count == 0) return null;
        candidatas.Sort(StringComparer.Ordinal);
        var rng = new System.Random(unchecked(semilla * 19349663 ^ (eleccion + 101) * 83492791));
        return candidatas[rng.Next(candidatas.Count)];
    }

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

    // Las tres de la eleccion pendiente, con lo comprado de verdad. En la preparacion, la dorada es la de cada tercera.
    public static List<string> Ofrecer(out int dorada)
    {
        var catalogo = CatalogoCartas.Instancia;
        int eligiendo = Math.Max(0, estado.poder - estado.pendientes);
        bool conDorada = EnPreparacion ? PreparacionConDorada(EleccionDePreparacion) : estado.dorada;
        return Sortear(catalogo != null ? catalogo.cartas : null, DisponibleAhora, estado.semilla, eligiendo, out dorada, conDorada);
    }

    // Si la carta puede salir ahora, con lo comprado de verdad.
    private static bool DisponibleAhora(Carta c)
    {
        return Disponible(c, Veces(c != null ? c.id : null), CatalogoMejoras.GranadaDesbloqueada, Progreso.Nivel("furia") > 0);
    }

    // Elige una de las que se ofrecieron: la suma (la dorada vale dos veces, sin pasar del tope)
    // y gasta una eleccion. Devuelve cuantas veces se sumaron.
    public static int Elegir(string id, bool esDorada)
    {
        string relleno;
        return Elegir(id, esDorada, out relleno);
    }

    // En la preparacion, cada eleccion vale dos copias, y la que no entra en la elegida va a otra al azar ('relleno', o
    // null): asi siempre suma dos.
    public static int Elegir(string id, bool esDorada, out string relleno)
    {
        relleno = null;
        if (!Activo || string.IsNullOrEmpty(id) || estado.pendientes <= 0) return 0;
        bool preparando = EnPreparacion;
        int eleccion = EleccionDePreparacion;
        int copias = CopiasDeLaEleccion(esDorada);
        estado.pendientes--;
        if (esDorada && !preparando) estado.dorada = false;
        int sumadas = 0;
        if (id != Curacion)
        {
            sumadas = Sumar(id, copias);
            if (preparando && sumadas < copias)
            {
                var catalogo = CatalogoCartas.Instancia;
                relleno = CartaDeRelleno(catalogo != null ? catalogo.cartas : null, DisponibleAhora, id, estado.semilla, eleccion);
                if (relleno != null && Sumar(relleno, copias - sumadas) <= 0) relleno = null;
            }
        }
        if (preparando && estado.pendientes == 0) estado.preparacion = 0;
        Revision++;
        return sumadas;
    }

    // Suma copias de una carta sin pasar de su tope; devuelve cuantas entraron.
    private static int Sumar(string id, int copias)
    {
        var catalogo = CatalogoCartas.Instancia;
        var carta = catalogo != null ? catalogo.Buscar(id) : null;
        int tope = carta != null && carta.tope > 0 ? carta.tope : int.MaxValue;
        int antes = Veces(id);
        int despues = Math.Min(tope, antes + copias);
        if (despues > 0) veces[id] = despues;
        return despues - antes;
    }

    // Las cartas que se tienen, en el orden en que se eligieron por primera vez: el HUD.
    public static IEnumerable<KeyValuePair<string, int>> Elegidas { get { return veces; } }
}
