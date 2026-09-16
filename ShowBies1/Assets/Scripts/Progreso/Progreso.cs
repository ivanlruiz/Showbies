using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Lo que el jugador conserva entre partidas: las monedas, la mejor oleada
// completada y el nivel de cada mejora. Vive en un JSON en persistentDataPath y
// no en PlayerPrefs porque es estado estructurado que crece con las mejoras.
//
// Las monedas se suman en memoria en el momento y se guardan en disco en puntos
// seguros: al completar una oleada, al pausar (que tambien pasa cuando la app
// pierde el foco, antes de que Android pueda matarla), al morir y al cerrar. Una
// compra, en cambio, se guarda en el acto: es una decision del jugador y no se
// puede perder porque la app muera en el menu.
//
// Quien muestra algo de aca (la tienda, los contadores, la insignia) mira Revision
// en vez de suscribirse a un evento: sube con cada cambio y alcanza con compararla
// con la ultima que se vio.
public static class Progreso
{
    // Una entrada por mejora comprada, por id. Es una lista y no un diccionario
    // porque JsonUtility no serializa diccionarios.
    [Serializable]
    private class NivelDeMejora
    {
        public string id;
        public int nivel;
    }

    // Cuantas veces se uso un lugar de anuncio en el dia guardado. Lista y no
    // diccionario, como los niveles: JsonUtility no serializa diccionarios.
    [Serializable]
    private class UsoDeLugar
    {
        public string lugar;
        public int cantidad;
    }

    [Serializable]
    private class EstadoAnuncios
    {
        public int dia;                  // aaaammdd local; 0 = todavia ninguno
        public int fallasPremiadas;      // del dia guardado
        public List<UsoDeLugar> usos = new List<UsoDeLugar>();
    }

    [Serializable]
    private class Datos
    {
        // Sin inicializador a proposito: un JSON sin "version" (de antes de que
        // existiera) tiene que leerse como 0, no como la version actual.
        public int version;
        public double monedas;
        public int mejorOleada;
        public List<NivelDeMejora> mejoras = new List<NivelDeMejora>();

        // v3, para los anuncios. Los que faltan en un JSON viejo quedan con estos
        // valores, asi que migrar de la 2 a la 3 no necesita nada mas.
        public int partidasTerminadas;
        public double segundosJugados;
        public bool ofrecerVideos = true;
        public EstadoAnuncios anuncios = new EstadoAnuncios();
    }

    // 1: monedas y mejor oleada. 2: suma los niveles de las mejoras. 3: suma lo que
    // necesitan los anuncios (partidas, tiempo jugado, interruptor y topes del dia).
    public const int VersionActual = 3;

    private const string NombreArchivo = "progreso.json";

    private static Datos datos;

    // El archivo es de una version mas nueva que este build: se usa, pero Guardar
    // no lo toca (ver Cargar).
    private static bool soloLectura;

    // Carpeta que reemplaza a persistentDataPath en las pruebas de editor, para no
    // tocar el progreso real. Null = la de siempre.
    private static string carpetaPruebas;

    // La partida cuyo premio de duplicar ya se cobro; -1 si ninguna.
    private static int partidaDuplicada = -1;

    // Lo que se gano en la partida en curso, o en la ultima si ya termino.
    public static double MonedasDeLaPartida { get; private set; }

    // Sube con cada partida que empieza. No se guarda: sirve para que un premio
    // que se cobra una vez por partida (el x2 de la derrota) no se cobre dos.
    public static int NumeroDePartida { get; private set; }

    // Lo que duro la ultima partida. No se guarda: lo lee la pantalla de derrota,
    // que es otra escena y ya no tiene al jugador para preguntarle.
    public static float SegundosDeLaUltimaPartida { get; private set; }

    // Sube con cada cambio de monedas o niveles.
    public static int Revision { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetearEstadoCompartido()
    {
        Application.quitting -= Guardar;
        datos = null;
        soloLectura = false;
        MonedasDeLaPartida = 0;
        NumeroDePartida = 0;
        SegundosDeLaUltimaPartida = 0;
        partidaDuplicada = -1;
        Revision = 0;
        carpetaPruebas = null;
    }

    public static bool SoloLectura
    {
        get { Cargar(); return soloLectura; }
    }

    public static double Monedas
    {
        get { Cargar(); return datos.monedas; }
    }

    // Las monedas que se pueden gastar. Las fraccionarias (el modo libre y el
    // crecimiento por oleada dan valores con decimales) se acumulan pero no se
    // gastan hasta completar la unidad. El 1e-6 absorbe el error de sumar muchas
    // fracciones: 44,9999999 cuenta como 45.
    public static long MonedasEnteras
    {
        get
        {
            double enteras = Math.Floor(Monedas + 1e-6);
            return enteras >= long.MaxValue ? long.MaxValue : (long)enteras;
        }
    }

    public static int MejorOleada
    {
        get { Cargar(); return datos.mejorOleada; }
    }

    public static string RutaArchivo
    {
        get { return Ruta(); }
    }

    public static void EmpezarPartida()
    {
        Cargar();
        MonedasDeLaPartida = 0;
        NumeroDePartida++;
    }

    // Al morir, desde el mismo bloque de PlayerHealth que ya guarda. Cuenta la
    // partida y el tiempo jugado: los anuncios no se ofrecen en la primera partida
    // ni en una de dos segundos.
    public static void TerminarPartida(float segundos)
    {
        Cargar();
        if (segundos < 0f || float.IsNaN(segundos) || float.IsInfinity(segundos)) segundos = 0f;
        SegundosDeLaUltimaPartida = segundos;
        datos.partidasTerminadas++;
        datos.segundosJugados += segundos;
    }

    public static int PartidasTerminadas
    {
        get { Cargar(); return datos.partidasTerminadas; }
    }

    public static double SegundosJugados
    {
        get { Cargar(); return datos.segundosJugados; }
    }

    // El interruptor "Ofrecer videos" del menu. Guarda en el acto: es una decision
    // del jugador, como una compra.
    public static bool OfrecerVideos
    {
        get { Cargar(); return datos.ofrecerVideos; }
        set
        {
            Cargar();
            if (datos.ofrecerVideos == value) return;
            datos.ofrecerVideos = value;
            Revision++;
            Guardar();
        }
    }

    public static void Sumar(double cantidad)
    {
        // !(cantidad > 0) tambien descarta NaN, que pasaria un "cantidad <= 0".
        if (!(cantidad > 0) || double.IsInfinity(cantidad)) return;
        Cargar();
        datos.monedas += cantidad;
        MonedasDeLaPartida += cantidad;
        Revision++;
    }

    // La UNICA entrada de monedas que no es jugar. Sumar es para lo que se gana
    // matando zombis y por el bono de oleada (Moneda.cs y WaveManager.cs); esto es
    // para los premios: el x2 de un video y lo que venga despues. Se separan para
    // que un premio nunca cuente como monedas ganadas jugando.
    public static void CobrarPremio(string motivo, double cantidad, bool mostrarEnLaPartida)
    {
        if (!(cantidad > 0) || double.IsInfinity(cantidad)) return;
        Cargar();
        datos.monedas += cantidad;
        if (mostrarEnLaPartida) MonedasDeLaPartida += cantidad;
        Revision++;
        Guardar();
        Debug.Log("Progreso: premio \"" + motivo + "\" de " + cantidad.ToString("0.##") + " monedas");
    }

    // El x2 de la derrota: duplica lo que dejo la partida, una sola vez por partida.
    // Devuelve si se cobro.
    public static bool DuplicarMonedasDeLaPartida()
    {
        Cargar();
        if (partidaDuplicada == NumeroDePartida || MonedasDeLaPartida < 1) return false;

        partidaDuplicada = NumeroDePartida;
        CobrarPremio("duplicar_derrota", MonedasDeLaPartida, true);
        return true;
    }

    public static bool YaSeDuplicoLaPartida
    {
        get { return partidaDuplicada == NumeroDePartida; }
    }

    // Los topes de anuncios son por dia local, guardado como aaaammdd: es un entero
    // comparable y JsonUtility no serializa DateTime.
    public static int DiaDeHoy()
    {
        DateTime ahora = DateTime.Now;
        return ahora.Year * 10000 + ahora.Month * 100 + ahora.Day;
    }

    // Si el reloj volvio atras (un dia menor que el guardado) los topes NO se
    // reinician: si no, alcanzaria con cambiar la fecha del telefono. Estatico para
    // probarlo sin escena.
    public static bool EsDiaNuevo(int diaGuardado, int hoy)
    {
        return hoy > diaGuardado;
    }

    public static int UsosDeHoy(string lugar)
    {
        if (string.IsNullOrEmpty(lugar)) return 0;
        Cargar();
        PonerAlDiaLosAnuncios();
        UsoDeLugar uso = BuscarUso(lugar);
        return uso != null ? uso.cantidad : 0;
    }

    public static void RegistrarUsoDeAnuncio(string lugar)
    {
        if (string.IsNullOrEmpty(lugar)) return;
        Cargar();
        PonerAlDiaLosAnuncios();
        UsoDeLugar uso = BuscarUso(lugar);
        if (uso == null)
        {
            uso = new UsoDeLugar { lugar = lugar, cantidad = 0 };
            datos.anuncios.usos.Add(uso);
        }
        uso.cantidad++;
        Guardar();
    }

    public static int FallasPremiadasHoy
    {
        get { Cargar(); PonerAlDiaLosAnuncios(); return datos.anuncios.fallasPremiadas; }
    }

    public static void RegistrarFallaPremiada()
    {
        Cargar();
        PonerAlDiaLosAnuncios();
        datos.anuncios.fallasPremiadas++;
        Guardar();
    }

    private static void PonerAlDiaLosAnuncios()
    {
        int hoy = DiaDeHoy();
        if (!EsDiaNuevo(datos.anuncios.dia, hoy)) return;

        datos.anuncios.dia = hoy;
        datos.anuncios.fallasPremiadas = 0;
        datos.anuncios.usos.Clear();
    }

    private static UsoDeLugar BuscarUso(string lugar)
    {
        List<UsoDeLugar> usos = datos.anuncios.usos;
        for (int i = 0; i < usos.Count; i++)
        {
            if (usos[i] != null && usos[i].lugar == lugar) return usos[i];
        }
        return null;
    }

    public static void RegistrarOleadaCompletada(int oleada)
    {
        Cargar();
        if (oleada > datos.mejorOleada) datos.mejorOleada = oleada;
    }

    public static int Nivel(string id)
    {
        if (string.IsNullOrEmpty(id)) return 0;
        Cargar();
        NivelDeMejora entrada = Buscar(datos.mejoras, id);
        return entrada != null ? entrada.nivel : 0;
    }

    public static bool PuedePagar(double precio)
    {
        return MonedasEnteras >= precio;
    }

    public static EstadoMejora Estado(Mejora mejora)
    {
        if (mejora == null || string.IsNullOrEmpty(mejora.id)) return EstadoMejora.Invalida;

        int nivel = Nivel(mejora.id);
        if (mejora.EnTope(nivel)) return EstadoMejora.EnTope;
        if (!PuedePagar(mejora.Precio(nivel))) return EstadoMejora.SinMonedas;
        return EstadoMejora.Comprable;
    }

    // No toca MonedasDeLaPartida: esa cuenta es lo que se gano jugando, y la
    // pantalla de derrota la muestra aunque despues se gaste en la tienda.
    public static ResultadoCompra Comprar(Mejora mejora)
    {
        switch (Estado(mejora))
        {
            case EstadoMejora.Invalida: return ResultadoCompra.Invalida;
            case EstadoMejora.EnTope: return ResultadoCompra.EnTope;
            case EstadoMejora.SinMonedas: return ResultadoCompra.SinMonedas;
        }

        int nivel = Nivel(mejora.id);
        double precio = mejora.Precio(nivel);

        // Con 44,9999999 monedas se puede pagar 45 (ver MonedasEnteras): el
        // Max evita que quede un saldo negativo de una millonesima.
        datos.monedas = Math.Max(0, datos.monedas - precio);
        ObtenerOCrear(mejora.id).nivel = nivel + 1;
        Revision++;
        Guardar();
        return ResultadoCompra.Comprada;
    }

    // Para las herramientas de editor y las pruebas.
    public static void DepurarFijarMonedas(double monedas)
    {
        Cargar();
        datos.monedas = double.IsNaN(monedas) || double.IsInfinity(monedas) ? 0 : Math.Max(0, monedas);
        Revision++;
        Guardar();
    }

    public static void DepurarFijarNivel(string id, int nivel)
    {
        if (string.IsNullOrEmpty(id))
        {
            Debug.LogWarning("Progreso: DepurarFijarNivel con id vacio.");
            return;
        }
        Cargar();
        ObtenerOCrear(id).nivel = Math.Max(0, nivel);
        Revision++;
        Guardar();
    }

    // Pisa tambien un archivo de una version mas nueva: es una orden explicita y el
    // original quedo respaldado como .futuro.bak al cargarlo.
    public static void ReiniciarTodo()
    {
        Cargar();
        datos = new Datos { version = VersionActual };
        soloLectura = false;
        MonedasDeLaPartida = 0;
        Revision++;
        Guardar();
    }

    // Cambia la carpeta del archivo (null vuelve a persistentDataPath). Primero
    // guarda lo cargado en la carpeta vigente, asi una prueba no se lleva
    // cambios del progreso real ni los deja en la carpeta temporal.
    public static void UsarCarpetaDePruebas(string carpeta)
    {
        Guardar();
        carpetaPruebas = string.IsNullOrEmpty(carpeta) ? null : carpeta;
        datos = null;
        MonedasDeLaPartida = 0;
        Revision++;
    }

    // Primero un .tmp y despues la copia: si la app muere a mitad de escritura,
    // queda al menos una de las dos versiones entera.
    public static void Guardar()
    {
        if (datos == null || soloLectura) return;

        string ruta = Ruta();
        string temporal = ruta + ".tmp";
        try
        {
            File.WriteAllText(temporal, JsonUtility.ToJson(datos, true));
            File.Copy(temporal, ruta, true);
            File.Delete(temporal);
        }
        catch (Exception e)
        {
            Debug.LogWarning("Progreso: no se pudo guardar en " + ruta + ": " + e.Message);
        }
    }

    // Si el principal esta roto se aparta como .roto antes de que el primer
    // Guardar lo pise, y se prueba el .tmp de un guardado interrumpido. Si lo
    // que se leyo es de una version vieja, se respalda como .v<N>.bak antes de
    // migrarlo: si la migracion tuviera un error, el original sigue en disco.
    private static void Cargar()
    {
        if (datos != null) return;

        string ruta = Ruta();
        Datos leidos = Leer(ruta);
        string rutaLeida = leidos != null ? ruta : null;

        if (leidos == null)
        {
            if (File.Exists(ruta)) Respaldar(ruta, ruta + ".roto");
            leidos = Leer(ruta + ".tmp");
            if (leidos != null) rutaLeida = ruta + ".tmp";
        }

        soloLectura = false;
        if (leidos == null)
        {
            leidos = new Datos { version = VersionActual };
        }
        else if (leidos.version < VersionActual)
        {
            Respaldar(rutaLeida, ruta + ".v" + Math.Max(0, leidos.version) + ".bak");
        }
        else if (leidos.version > VersionActual)
        {
            // De un build mas nuevo (otra rama, o volver atras una version). Se juega
            // con los campos que este build entiende, pero no se escribe nada: el
            // primer Guardar reescribiria el archivo con esta version y perderia lo
            // que agrego la nueva.
            Respaldar(rutaLeida, ruta + ".v" + leidos.version + ".futuro.bak");
            soloLectura = true;
            Debug.LogWarning("Progreso: " + rutaLeida + " es de la version " + leidos.version + " y este build llega a la " +
                             VersionActual + ". Se usa sin guardar cambios para no borrar lo que agrego la version nueva.");
        }

        Normalizar(leidos);
        datos = leidos;
        Application.quitting -= Guardar;
        Application.quitting += Guardar;
    }

    private static Datos Leer(string ruta)
    {
        try
        {
            return File.Exists(ruta) ? JsonUtility.FromJson<Datos>(File.ReadAllText(ruta)) : null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    // No pisa un respaldo que ya exista: el primero es el que tiene el original.
    private static void Respaldar(string origen, string destino)
    {
        try
        {
            if (!File.Exists(destino)) File.Copy(origen, destino);
        }
        catch (Exception e)
        {
            Debug.LogWarning("Progreso: no se pudo respaldar " + origen + " en " + destino + ": " + e.Message);
        }
    }

    // Deja los datos en un estado valido, sea cual sea el archivo que se leyo (a
    // mano, de una version vieja o a medio escribir). Se puede llamar dos veces.
    // Los ids que ningun catalogo conoce se conservan: pueden ser de una mejora
    // que se saco de la tienda por ahora, y borrarlos perderia lo comprado.
    private static void Normalizar(Datos d)
    {
        if (d.mejoras == null) d.mejoras = new List<NivelDeMejora>();
        if (double.IsNaN(d.monedas) || double.IsInfinity(d.monedas) || d.monedas < 0) d.monedas = 0;
        if (d.mejorOleada < 0) d.mejorOleada = 0;
        if (d.partidasTerminadas < 0) d.partidasTerminadas = 0;
        if (double.IsNaN(d.segundosJugados) || double.IsInfinity(d.segundosJugados) || d.segundosJugados < 0) d.segundosJugados = 0;
        if (d.anuncios == null) d.anuncios = new EstadoAnuncios();
        if (d.anuncios.usos == null) d.anuncios.usos = new List<UsoDeLugar>();
        if (d.anuncios.dia < 0) d.anuncios.dia = 0;
        if (d.anuncios.fallasPremiadas < 0) d.anuncios.fallasPremiadas = 0;
        for (int i = d.anuncios.usos.Count - 1; i >= 0; i--)
        {
            UsoDeLugar uso = d.anuncios.usos[i];
            if (uso == null || string.IsNullOrEmpty(uso.lugar)) d.anuncios.usos.RemoveAt(i);
            else if (uso.cantidad < 0) uso.cantidad = 0;
        }

        var limpias = new List<NivelDeMejora>(d.mejoras.Count);
        for (int i = 0; i < d.mejoras.Count; i++)
        {
            NivelDeMejora entrada = d.mejoras[i];
            if (entrada == null || string.IsNullOrEmpty(entrada.id)) continue;

            int nivel = Math.Max(0, entrada.nivel);
            NivelDeMejora existente = Buscar(limpias, entrada.id);
            if (existente == null)
            {
                limpias.Add(new NivelDeMejora { id = entrada.id, nivel = nivel });
            }
            else if (nivel > existente.nivel)
            {
                // Repetido: gana el mayor, para no quitarle al jugador algo que pago.
                existente.nivel = nivel;
            }
        }

        d.mejoras = limpias;
        d.version = VersionActual;
    }

    private static NivelDeMejora Buscar(List<NivelDeMejora> lista, string id)
    {
        for (int i = 0; i < lista.Count; i++)
        {
            if (lista[i].id == id) return lista[i];
        }
        return null;
    }

    private static NivelDeMejora ObtenerOCrear(string id)
    {
        Cargar();
        NivelDeMejora entrada = Buscar(datos.mejoras, id);
        if (entrada == null)
        {
            entrada = new NivelDeMejora { id = id, nivel = 0 };
            datos.mejoras.Add(entrada);
        }
        return entrada;
    }

    private static string Ruta()
    {
        if (carpetaPruebas == null) return Path.Combine(Application.persistentDataPath, NombreArchivo);

        try
        {
            Directory.CreateDirectory(carpetaPruebas);
        }
        catch (Exception e)
        {
            Debug.LogWarning("Progreso: no se pudo crear la carpeta " + carpetaPruebas + ": " + e.Message);
        }
        return Path.Combine(carpetaPruebas, NombreArchivo);
    }
}
