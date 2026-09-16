using System;

// El proveedor de cuando no hay anuncios: nunca tiene un video listo, asi que el
// juego no ofrece nada y no aparece ningun boton. Es el que se usa en Windows y el
// que queda si falta la configuracion.
public class ProveedorNulo : IProveedorAnuncios
{
    public string Nombre { get { return "nulo"; } }

    public void Inicializar()
    {
    }

    public bool Listo(string lugar)
    {
        return false;
    }

    public void Mostrar(string lugar, Action<ResultadoAnuncio> alTerminar)
    {
        if (alTerminar != null) alTerminar(ResultadoAnuncio.NoDisponible);
    }
}
