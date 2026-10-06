// Lo que un proveedor con consentimiento (hoy, AdMob con UMP) le ofrece al juego aparte de
// los videos: el cartel de consentimiento de Europa, el Reino Unido y Suiza, y el boton
// PRIVACIDAD de las opciones para cambiar lo que se eligio. Va aparte de IProveedorAnuncios
// porque el nulo, el falso y el de las pruebas no tienen nada de esto. El juego no lo usa
// directo: pasa por ServicioAnuncios.
public interface IConsentimientoAnuncios
{
    // UMP dice que hay que pedirlo: en Europa, sin respuesta todavia. Mientras tanto ahi no
    // se piden anuncios.
    bool ConsentimientoRequerido { get; }

    // El cartel de UMP esta pedido o en pantalla.
    bool ConsentimientoEnPantalla { get; }

    // Hay que ofrecer una forma de cambiar el consentimiento: el boton PRIVACIDAD.
    bool PrivacidadRequerida { get; }

    void PedirConsentimiento();

    void MostrarPrivacidad();
}
