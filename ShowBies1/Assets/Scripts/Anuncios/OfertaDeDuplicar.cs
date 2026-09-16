using UnityEngine;
using UnityEngine.UI;
using TMPro;

// El boton "VER VIDEO Y DUPLICAR" de la pantalla de derrota: el unico lugar del
// juego donde hoy entra un anuncio. Va en la escena Perdiste, con el boton y el
// texto cableados en el inspector.
//
// Como se ofrece, que es lo que importa:
// - **Opt-in y nunca en la accion.** La derrota es una pausa natural; el jugador
//   ya termino de jugar y decide si mira.
// - **El premio se anuncia exacto antes de mirar** ("+1.234 monedas"), no como
//   sorpresa.
// - **Cerrar el video no castiga:** el boton se apaga y la pantalla queda igual.
// - **Si no se puede ofrecer, el boton no existe**, no aparece en gris: un boton
//   que no hace nada es peor que ninguno.
public class OfertaDeDuplicar : MonoBehaviour
{
    [Tooltip("El objeto entero de la oferta: arranca apagado y se prende sólo si hay video.")]
    public GameObject raiz;
    public Button boton;
    public TMP_Text etiqueta;

    [Tooltip("El contador de monedas de la derrota, para que el número suba cuando se cobra.")]
    public TextoMonedasPartida contador;

    [Tooltip("Se prende si la sesión ya fue larga. Opcional.")]
    public GameObject avisoDeDescanso;

    // Mientras la oferta esta en pantalla ocupa el renglon del aviso de compras
    // ("¡Te alcanza para N mejoras!"), que es el de abajo del contador. Lo mira
    // BotonMejoras para no escribir los dos encima. Es static porque el aviso lo
    // maneja otro componente, en otro objeto.
    public static bool TapaElAviso { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetearEstadoCompartido()
    {
        TapaElAviso = false;
    }

    private void Start()
    {
        if (raiz != null) raiz.SetActive(false);
        TapaElAviso = false;
        if (avisoDeDescanso != null) avisoDeDescanso.SetActive(MostrarDescanso());

        if (!SePuedeOfrecer()) return;

        if (etiqueta != null)
        {
            etiqueta.text = "VER VIDEO: +" + FormatoNumeros.Compacto(Progreso.MonedasDeLaPartida) + " MONEDAS";
        }
        if (boton != null)
        {
            boton.onClick.RemoveListener(Apretar);
            boton.onClick.AddListener(Apretar);
        }
        if (raiz != null) raiz.SetActive(true);
        TapaElAviso = true;
    }

    // Lo mismo que PuedeOfrecer, mas las dos condiciones propias del x2: que la
    // partida haya durado algo y haya dejado monedas. Sin ellas, morir a proposito
    // en diez segundos seria la forma rapida de farmear videos.
    private static bool SePuedeOfrecer()
    {
        ConfigAnuncios config = ServicioAnuncios.ConfigEnUso;
        if (config == null) return false;
        if (Progreso.YaSeDuplicoLaPartida) return false;
        if (Progreso.MonedasDeLaPartida < config.monedasMinimasParaDuplicar) return false;
        if (Progreso.SegundosDeLaUltimaPartida < config.segundosDeLaPartidaMinimos) return false;

        return ServicioAnuncios.PuedeOfrecer(LugarAnuncio.DuplicarDerrota);
    }

    // El aviso de descanso: a partir de una hora de sesion, la derrota lo sugiere.
    // No bloquea nada ni corta la partida siguiente.
    private static bool MostrarDescanso()
    {
        ConfigAnuncios config = ConfigAnuncios.Instancia;
        if (config == null || config.minutosParaAvisoDeDescanso <= 0f) return false;

        return Time.realtimeSinceStartup >= config.minutosParaAvisoDeDescanso * 60f;
    }

    private void Apretar()
    {
        if (boton != null) boton.interactable = false;

        bool lanzado = ServicioAnuncios.Mostrar(LugarAnuncio.DuplicarDerrota, Cobrar, NoSeCobro);
        // Si ni siquiera arranco (dejo de haber video entre el Start y el toque), la
        // oferta se va sin ruido.
        if (!lanzado) NoSeCobro();
    }

    private void Cobrar()
    {
        if (!Progreso.DuplicarMonedasDeLaPartida())
        {
            NoSeCobro();
            return;
        }

        Esconder();
        // El jugo lo pone el contador: vuelve a contar con sus ticks hasta el numero
        // nuevo. Efectos no esta en la escena de derrota.
        if (contador != null) contador.Duplicar();
    }

    private void NoSeCobro()
    {
        Esconder();
    }

    private void Esconder()
    {
        if (raiz != null) raiz.SetActive(false);
        TapaElAviso = false;
    }
}
