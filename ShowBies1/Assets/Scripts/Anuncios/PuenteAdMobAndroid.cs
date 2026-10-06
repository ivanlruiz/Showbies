using System;
using UnityEngine;

// El puente de verdad de ProveedorAdMob: llama a PuenteAnuncios (Java, en
// Plugins/Android/ShowBiesAnuncios.androidlib) por JNI, como PedidoDeResena con la reseña.
// Solo en Android: ServicioAnuncios no lo crea en otra plataforma, y se decide en runtime,
// sin #if (ver la trampa en CLAUDE.md).
//
// Ninguna llamada puede tirar una excepcion hacia el juego: si Java falla, se avisa lo que
// habria avisado el puente (el video no estaba, el cartel se cerro), asi nadie queda
// esperando.
public class PuenteAdMobAndroid : ProveedorAdMob.IPuente
{
    public const string ClasePuente = "com.ivanruiz.showbies.anuncios.PuenteAnuncios";
    public const string InterfazOyente = "com.ivanruiz.showbies.anuncios.OyenteAnuncios";

    private Action<string, string, string> alEvento;
    // Se guarda para que el recolector no suelte el proxy mientras Java lo usa.
    private Oyente oyente;

    public void Iniciar(Action<string, string, string> alEvento, string[] lugares, string[] bloques,
                        string clasificacion, bool simularEuropa)
    {
        this.alEvento = alEvento;
        oyente = new Oyente(alEvento);
        Llamar("iniciar", new object[] { oyente, lugares, bloques, clasificacion, simularEuropa });
    }

    public void Mostrar(string lugar)
    {
        if (!Llamar("mostrar", new object[] { lugar })) Avisar(lugar, "terminado", "no_disponible");
    }

    public void OlvidarElQueSeMuestra()
    {
        try
        {
            using (var puente = new AndroidJavaClass(ClasePuente)) puente.CallStatic("olvidarElQueSeMuestra");
        }
        catch (Exception e)
        {
            Debug.LogWarning("AdMob: no se pudo olvidar el video: " + e.Message);
        }
    }

    public void PedirConsentimiento()
    {
        if (!Llamar("pedirConsentimiento", new object[0])) Avisar("", "consentimiento_cerrado", "");
    }

    public void MostrarPrivacidad()
    {
        if (!Llamar("mostrarPrivacidad", new object[0])) Avisar("", "consentimiento_cerrado", "");
    }

    // Llama a un metodo estatico del puente con la actividad de Unity delante de 'resto'. Los
    // argumentos van en un object[] explicito: un string[] suelto se tomaria como la lista
    // entera.
    private bool Llamar(string metodo, object[] resto)
    {
        try
        {
            using (var jugador = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var actividad = jugador.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var puente = new AndroidJavaClass(ClasePuente))
            {
                var argumentos = new object[resto.Length + 1];
                argumentos[0] = actividad;
                Array.Copy(resto, 0, argumentos, 1, resto.Length);
                puente.CallStatic(metodo, argumentos);
            }
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning("AdMob: fallo " + metodo + ": " + e.Message);
            return false;
        }
    }

    private void Avisar(string lugar, string evento, string dato)
    {
        if (alEvento != null) alEvento(lugar, evento, dato);
    }

    private class Oyente : AndroidJavaProxy
    {
        private readonly Action<string, string, string> avisar;

        public Oyente(Action<string, string, string> avisar) : base(InterfazOyente)
        {
            this.avisar = avisar;
        }

        // El nombre y la firma son los de la interfaz de Java. Nadie lo llama desde C#: Unity
        // lo busca por nombre cuando Java llama al proxy, y con la limpieza de codigo mas alta
        // el linker lo sacaria sin ningun error. Preserve lo impide. Llega en el hilo de
        // Android: lo que hace (ProveedorAdMob.AlEvento) solo encola.
        [UnityEngine.Scripting.Preserve]
        public void alEvento(string lugar, string evento, string dato)
        {
            if (avisar != null) avisar(lugar, evento, dato);
        }
    }
}
