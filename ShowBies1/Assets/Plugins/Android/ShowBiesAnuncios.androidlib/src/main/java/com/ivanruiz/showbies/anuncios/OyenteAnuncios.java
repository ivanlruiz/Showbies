package com.ivanruiz.showbies.anuncios;

// Lo que el juego escucha del puente (PuenteAnuncios): un aviso de texto por cada cosa que
// pasa. Es una interfaz y no una clase abstracta a proposito: del lado de C# la implementa
// un AndroidJavaProxy (PuenteAdMobAndroid), que solo puede implementar interfaces.
//
// "lugar" es el de LugarAnuncio, o "" si el aviso no es de un video (el consentimiento).
public interface OyenteAnuncios {
    void alEvento(String lugar, String evento, String dato);
}
