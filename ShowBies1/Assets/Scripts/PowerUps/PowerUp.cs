using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PowerUp : MonoBehaviour
{
    [SerializeField]
    private GameObject PUBalas;
    [SerializeField]
    private GameObject PUVida;
    [SerializeField]
    private GameObject PUArma;

    [SerializeField]
    private float IntervaloPUBalas = 3.5f;
    [SerializeField]
    private float IntervaloPUVida = 3.5f;
    [SerializeField]
    private float IntervaloPUArma = 3.5f;

    // Lo que mide una caja con su brillo: no nace a menos de esto de lo que tapa un edificio.
    public const float MargenContraLosEdificios = 1f;
    // Cuantas veces se sortea el punto antes de dejarlo donde toco (ver PuntoDeAparicion).
    private const int IntentosParaNoCaerTapada = 10;

    void Start()
    {
        StartCoroutine(spawnPU(IntervaloPUBalas, PUBalas));
        StartCoroutine(spawnPU(IntervaloPUVida, PUVida));
        StartCoroutine(spawnPU(IntervaloPUArma, PUArma));
    }

    private IEnumerator spawnPU(float interval, GameObject powerup)
    {
        // Antes se rellamaba a si mismo con un StartCoroutine al final, creando
        // una corrutina nueva por spawn: el mismo patron que se saco del
        // generador de zombis. Un while hace lo mismo sin alocar.
        while (true)
        {
            yield return new WaitForSeconds(interval);
            // Con el jugador muerto la partida sigue detras de la derrota: una caja que nadie
            // puede agarrar nacia en cualquier lado, a veces al lado de HAS PERDIDO, soltando
            // su brillo encima del titulo (lo vio la revision de la derrota del 27/9).
            if (PlayerHealth.instance != null && PlayerHealth.instance.EstaMuerto) continue;
            Instantiate(powerup, PuntoDeAparicion(), Quaternion.identity);
        }
    }

    // Un punto al azar del mapa donde la caja se vea. En la ciudad los edificios no tienen
    // collider (los zombis se trabarian) y desde la camara el techo tapaba la caja entera:
    // una de cada seis nacia adentro de un edificio y vencia sin que nadie la viera. Si el
    // punto cae tapado se sortea otro, con un tope por si algun dia un decorado tapa casi
    // todo el mapa: ahi sale donde toco, como antes. Publico para la prueba de logica.
    public static Vector3 PuntoDeAparicion()
    {
        Vector3 punto = PuntoAlAzar();
        for (int intento = 1; intento < IntentosParaNoCaerTapada && CapitulosDeEscenario.Tapado(punto, MargenContraLosEdificios); intento++)
        {
            punto = PuntoAlAzar();
        }
        return punto;
    }

    private static Vector3 PuntoAlAzar()
    {
        // distancia en x, altura y distancia en z: el area de siempre
        return new Vector3(Random.Range(-48f, 48), Random.Range(0.5f, 0.5f), Random.Range(-45, 45));
    }
}
