using System;

// Como termino un video.
public enum ResultadoAnuncio
{
    Recompensado,     // se vio entero: hay que dar el premio
    Cerrado,          // lo cerro antes: sin premio, sin castigo
    NoDisponible,     // no habia video para mostrar
    FallaAlMostrar    // empezo a mostrarse y algo se rompio
}

// Quien muestra los videos. El juego habla siempre con ServicioAnuncios, que a su
// vez habla con un proveedor: asi cambiar de red de anuncios (o poner el falso de
// las pruebas) es escribir otra clase y no tocar el juego.
//
// El aviso puede llegar desde otro hilo y mas de una vez: el servicio lo encola y
// lo resuelve en el hilo principal, una sola vez (ver ServicioAnuncios).
public interface IProveedorAnuncios
{
    string Nombre { get; }

    void Inicializar();

    // Si hay un video cargado para ese lugar.
    bool Listo(string lugar);

    void Mostrar(string lugar, Action<ResultadoAnuncio> alTerminar);
}
