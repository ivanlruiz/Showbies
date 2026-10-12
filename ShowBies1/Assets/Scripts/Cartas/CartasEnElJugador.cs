using System.Collections.Generic;
using UnityEngine;

// Lo que las cartas de reglas ponen alrededor del jugador (1.6.0, etapa 2): el anillo del escudo
// en el piso mientras esta listo (el bloqueo lo hace PlayerHealth), y los drones, que orbitan y
// le tiran solos al zombi mas cercano que se ve, con las balas del arma (y lo que les ponen las
// cartas). Lo agrega EleccionDeCartas al jugador al empezar la partida.
//
// Como todo lo que cambia la partida por su cuenta, no hace nada con el jugador muerto (y sin
// jugador muerto no hay drones) ni con el juego congelado: la pausa, la oferta de revivir, la
// derrota o la eleccion de cartas.
public class CartasEnElJugador : MonoBehaviour
{
    public const float RadioDelAnillo = 1.1f;
    public const float RadioDeLaOrbita = 1.5f;
    public const float AlturaDelDron = 1.7f;
    public const float GiroDeLaOrbita = 110f;     // grados por segundo
    public const float LadoDelDron = 0.32f;
    public const float LadoDeLaLuzDelDron = 1.2f;
    public static readonly Color ColorDelDron = new Color(1f, 0.38f, 0.78f);

    private PlayerHealth vida;
    private GunController arma;
    private Camera camara;
    private LineRenderer anillo;

    private class Dron
    {
        public Transform cuerpo;
        public Transform luz;
        public float proximoTiro;
    }

    private readonly List<Dron> drones = new List<Dron>();
    private Material materialDelDron, materialDeLaLuz;
    private float angulo;

    // Lo miran las pruebas.
    public int DronesPuestos { get { return drones.Count; } }
    public int TirosDeLosDrones { get; private set; }
    public bool AnilloVisible { get { return anillo != null && anillo.gameObject.activeSelf; } }

    private void Start()
    {
        vida = GetComponent<PlayerHealth>();
        var control = GetComponent<PlayerController>();
        arma = control != null ? control.theGun : null;
        camara = Camera.main;
        // El anillo, una copia del indicador de la granada (como el de apuntarla), celeste.
        if (control != null && control.granadaPrefab != null && control.granadaPrefab.Indicador != null)
        {
            anillo = Instantiate(control.granadaPrefab.Indicador);
            anillo.name = "AnilloDelEscudo";
            anillo.startColor = PlayerHealth.ColorDelEscudo;
            anillo.endColor = PlayerHealth.ColorDelEscudo;
            anillo.gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (anillo != null) Destroy(anillo.gameObject);
        foreach (var d in drones) if (d.cuerpo != null) Destroy(d.cuerpo.gameObject);
        drones.Clear();
        if (materialDelDron != null) Destroy(materialDelDron);
        if (materialDeLaLuz != null) Destroy(materialDeLaLuz);
    }

    private void LateUpdate()
    {
        bool enJuego = PoderDeLaPartida.Activo && vida != null && !vida.EstaMuerto;
        bool congelado = MenuPausa.JuegoCongelado;

        // El anillo del escudo, mientras esta listo, latiendo apenas.
        bool conAnillo = enJuego && vida.EscudoListo;
        if (anillo != null)
        {
            if (anillo.gameObject.activeSelf != conAnillo) anillo.gameObject.SetActive(conAnillo);
            if (conAnillo)
            {
                Granade.DibujarAnillo(anillo, transform.position, RadioDelAnillo);
                Color c = PlayerHealth.ColorDelEscudo;
                c.a = 0.65f + 0.3f * Mathf.Sin(Time.unscaledTime * 4f);
                anillo.startColor = c;
                anillo.endColor = c;
            }
        }

        // Los drones: tantos como veces la carta, repartidos en la orbita.
        int quiero = enJuego ? PoderDeLaPartida.Drones : 0;
        while (drones.Count < quiero) drones.Add(CrearDron());
        while (drones.Count > quiero)
        {
            var sobra = drones[drones.Count - 1];
            if (sobra.cuerpo != null) Destroy(sobra.cuerpo.gameObject);
            drones.RemoveAt(drones.Count - 1);
        }
        if (drones.Count == 0) return;

        if (!congelado) angulo = Mathf.Repeat(angulo + GiroDeLaOrbita * Time.deltaTime, 360f);
        Vector3 centro = transform.position;
        float cadencia = PoderDeLaPartida.TirosPorSegundoDelDron;
        for (int i = 0; i < drones.Count; i++)
        {
            var d = drones[i];
            Vector3 lado = Quaternion.Euler(0f, angulo + 360f * i / drones.Count, 0f) * Vector3.forward * RadioDeLaOrbita;
            Vector3 donde = new Vector3(centro.x + lado.x, AlturaDelDron + 0.12f * Mathf.Sin(Time.time * 3f + i), centro.z + lado.z);
            d.cuerpo.position = donde;
            if (d.luz != null) d.luz.position = new Vector3(donde.x, BulletController.AlturaDeLaLuz, donde.z);

            if (congelado || arma == null || cadencia <= 0f || Time.time < d.proximoTiro) continue;
            d.proximoTiro = Time.time + 1f / cadencia;
            Tirar(donde);
        }
    }

    private Dron CrearDron()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "Dron";
        // En el acto, como la luz de la bala: un collider chocaria con los zombis.
        DestroyImmediate(go.GetComponent<Collider>());
        go.transform.localScale = Vector3.one * LadoDelDron;
        var r = go.GetComponent<Renderer>();
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        if (materialDelDron == null)
        {
            var sombreado = Shader.Find("Unlit/Color");
            if (sombreado != null) materialDelDron = new Material(sombreado) { name = "Dron", color = ColorDelDron };
        }
        if (materialDelDron != null) r.sharedMaterial = materialDelDron;

        // Un charco de su color en el piso, como las balas (el shader de los faroles).
        Transform luz = null;
        var molde = arma != null && arma.bala != null ? arma.bala.materialLuz : null;
        if (molde != null)
        {
            if (materialDeLaLuz == null) materialDeLaLuz = new Material(molde) { name = "LuzDelDron", color = ColorDelDron };
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Luz";
            DestroyImmediate(quad.GetComponent<Collider>());
            quad.transform.SetParent(go.transform, false);
            quad.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            quad.transform.localScale = Vector3.one * (LadoDeLaLuzDelDron / LadoDelDron);
            var rl = quad.GetComponent<Renderer>();
            rl.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rl.receiveShadows = false;
            rl.sharedMaterial = materialDeLaLuz;
            luz = quad.transform;
        }
        return new Dron { cuerpo = go.transform, luz = luz, proximoTiro = Time.time + 0.5f };
    }

    // Al zombi vivo mas cercano que se ve, a la altura de los tiros del arma.
    private void Tirar(Vector3 desde)
    {
        var blanco = MasCercano(desde);
        if (blanco == null) return;
        float altura = arma.firePoint != null ? arma.firePoint.position.y : desde.y;
        Vector3 salida = new Vector3(desde.x, altura, desde.z);
        Vector3 hacia = blanco.transform.position - salida;
        hacia.y = 0f;
        if (hacia.sqrMagnitude < 1e-4f) return;
        arma.SacarBala(salida, Quaternion.LookRotation(hacia), 0f, false);
        TirosDeLosDrones++;
        Efectos.Disparo(salida, ColorDelDron);
    }

    private EnemyController MasCercano(Vector3 desde)
    {
        if (camara == null) camara = Camera.main;
        float alcance2 = PoderDeLaPartida.AlcanceDelDron * PoderDeLaPartida.AlcanceDelDron;
        EnemyController mejor = null;
        float mejor2 = float.MaxValue;
        var vivos = EnemyController.Vivos;
        for (int i = 0; i < vivos.Count; i++)
        {
            var zombi = vivos[i];
            if (zombi == null || !zombi.Vivo) continue;
            Vector3 d = zombi.transform.position - desde;
            d.y = 0f;
            float d2 = d.sqrMagnitude;
            if (d2 > alcance2 || d2 >= mejor2 || !SeVe(zombi.transform.position)) continue;
            mejor2 = d2;
            mejor = zombi;
        }
        return mejor;
    }

    private bool SeVe(Vector3 punto)
    {
        if (camara == null) return true;
        Vector3 v = camara.WorldToViewportPoint(punto);
        return v.z > 0f && v.x > 0f && v.x < 1f && v.y > 0f && v.y < 1f;
    }
}
