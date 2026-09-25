using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

// Tutorial jugable: una instruccion por vez, y cada paso se completa HACIENDO
// la accion, no apretando "siguiente". Vive en la escena Tutorial, que es una
// copia de ShowBies1 sin generador de zombis ni spawner de power-ups: aca los
// enemigos y los pickups los pone este script cuando el paso lo pide.
//
// Los textos salen segun Plataforma.EsMovil (teclado/mouse o joysticks), asi
// que el mismo tutorial sirve en PC y en el telefono.
public class TutorialManager : MonoBehaviour
{
    [Header("Referencias")]
    public PlayerController jugador;
    public TMP_Text textoInstruccion;
    public GameObject panelFinal;

    [Header("Prefabs que usa el tutorial")]
    public GameObject zombiPrefab;      // ZombiNormal: muere de un tiro
    public GameObject puBalasPrefab;
    public GameObject puVidaPrefab;
    public GameObject puArmaPrefab;

    [Header("Ajustes")]
    public float distanciaParaMoverse = 5f;
    public float distanciaSpawnZombi = 14f;

    // Hasta donde puede nacer algo, en metros desde el centro del mapa, en x y en z. El
    // piso de Tutorial.unity es de 100 x 100 y las paredes invisibles dejan adentro unos
    // 49 m; con margen, para que nada nazca pegado a una pared.
    public const float LimiteDelMapa = 44f;

    // Lo que va alrededor de un punto: las dos cajas del paso 4 a sus costados y los
    // zombis del grupo de la granada pegados a el. Al acotar el punto se les deja lugar.
    private const float CostadoDeLasCajas = 4f;
    private const float SeparacionDelGrupo = 1.5f;

    private enum Paso { Moverse, Disparar, Granada, Pickups, Arma, Fin }
    private Paso paso;

    private Vector3 posicionInicial;
    private readonly List<GameObject> objetosDelPaso = new List<GameObject>();
    private bool esperandoQueVenzaLaMejora;

    // La granada del paso de la granada: la que se tira en ese paso, y no una que venia
    // volando del anterior.
    private Granade granadaDeAntes;
    private Granade granadaDelPaso;
    private bool granadaTirada;

    private void Start()
    {
        panelFinal.SetActive(false);
        posicionInicial = jugador.transform.position;
        // La granada se compra en la tienda, pero el tutorial la ensenia: aca esta siempre.
        jugador.GranadaDesbloqueada = true;
        Entrar(Paso.Moverse);
    }

    private void Update()
    {
        // Inmortal durante el tutorial: nadie se muere aprendiendo.
        var vida = PlayerHealth.instance;
        if (vida != null) vida.health = vida.maxHealth;

        switch (paso)
        {
            case Paso.Moverse:
                if (Vector3.Distance(jugador.transform.position, posicionInicial) >= distanciaParaMoverse)
                    Entrar(Paso.Disparar);
                break;

            case Paso.Disparar:
                if (EnemyController.ZombisVivos == 0)
                    Entrar(Paso.Granada);
                break;

            case Paso.Granada:
                // Se completa cuando la granada tirada en este paso ya exploto (al explotar
                // se destruye), pegue o no. Hasta el 25/9 se completaba al tirarla: el paso
                // siguiente sacaba a los zombis en el acto y la granada estallaba un segundo
                // despues sobre el pasto vacio, justo lo contrario de lo que ensenia el paso.
                // Los que mato terminan de caerse solos (ver Sacar).
                if (!granadaTirada)
                {
                    var granada = FindFirstObjectByType<Granade>();
                    if (granada != null && granada != granadaDeAntes)
                    {
                        granadaDelPaso = granada;
                        granadaTirada = true;
                    }
                }
                else if (granadaDelPaso == null)
                {
                    Entrar(Paso.Pickups);
                }
                break;

            case Paso.Pickups:
                if (AlgunObjetoDelPasoDesaparecio())
                    Entrar(Paso.Arma);
                break;

            case Paso.Arma:
                // Espera a que se agarre SU caja, como el paso de las cajas. MejoraActiva la
                // prende cualquier caja: con la de balas del paso anterior su reloj seguia
                // corriendo, el paso saltaba en el acto al texto del reloj (que promete un
                // cargador grande que la de balas no da) y al vencer se llevaba la caja de
                // arma sin que nadie la tocara. Al agarrarla, la de arma reinicia el reloj:
                // el que se senala es el suyo.
                if (!esperandoQueVenzaLaMejora)
                {
                    if (AlgunObjetoDelPasoDesaparecio())
                    {
                        esperandoQueVenzaLaMejora = true;
                        textoInstruccion.text = Textos.De("tut_reloj");
                    }
                }
                else if (!jugador.theGun.MejoraActiva)
                {
                    Entrar(Paso.Fin);
                }
                break;
        }
    }

    private void Entrar(Paso nuevo)
    {
        LimpiarObjetosDelPaso();
        paso = nuevo;

        switch (paso)
        {
            case Paso.Moverse:
                textoInstruccion.text = Texto(Textos.De("tut_mover_pc"), Textos.De("tut_mover_movil"));
                break;

            case Paso.Disparar:
                textoInstruccion.text = Texto(Textos.De("tut_disparar_pc"), Textos.De("tut_disparar_movil"));
                Spawnear(zombiPrefab, CercaDelJugador(DireccionAlAzar() * distanciaSpawnZombi, 0f));
                break;

            case Paso.Granada:
                textoInstruccion.text = Texto(Textos.De("tut_granada_pc"), Textos.De("tut_granada_movil"));
                granadaDeAntes = FindFirstObjectByType<Granade>();
                granadaDelPaso = null;
                granadaTirada = false;
                // El centro se acota una sola vez, con lugar para los otros dos: acotando
                // cada zombi por su lado, contra una pared el grupo se podia partir.
                var centro = CercaDelJugador(DireccionAlAzar() * distanciaSpawnZombi, SeparacionDelGrupo);
                Spawnear(zombiPrefab, centro);
                Spawnear(zombiPrefab, centro + new Vector3(SeparacionDelGrupo, 0f, 0f));
                Spawnear(zombiPrefab, centro + new Vector3(0f, 0f, SeparacionDelGrupo));
                break;

            case Paso.Pickups:
                // Lo que haya quedado del paso de la granada (ver Sacar)
                foreach (var z in FindObjectsByType<EnemyController>(FindObjectsSortMode.None))
                    Sacar(z.gameObject);
                textoInstruccion.text = Textos.De("tut_cajas");
                // A los costados de un punto delante del jugador, que se corre hacia adentro
                // lo que haga falta para que entren las dos: acotando cada caja por su lado,
                // contra la pared de un costado las dos caian en el mismo lugar.
                var medio = CercaDelJugador(new Vector3(0f, 0f, 2f), CostadoDeLasCajas);
                Spawnear(puBalasPrefab, medio + new Vector3(CostadoDeLasCajas, 0f, 0f));
                Spawnear(puVidaPrefab, medio - new Vector3(CostadoDeLasCajas, 0f, 0f));
                break;

            case Paso.Arma:
                esperandoQueVenzaLaMejora = false;
                textoInstruccion.text = Textos.De("tut_caja_arma");
                Spawnear(puArmaPrefab, CercaDelJugador(new Vector3(0f, 0f, 4f), 0f));
                break;

            case Paso.Fin:
                // El panel final ya dice "eso es todo": el de instrucciones sobra.
                textoInstruccion.transform.parent.gameObject.SetActive(false);
                PlayerPrefs.SetInt("TutorialCompletado", 1);
                PlayerPrefs.Save();
                panelFinal.SetActive(true);
                break;
        }
    }

    // En ese lugar del mapa, a la altura del piso.
    private void Spawnear(GameObject prefab, Vector3 lugar)
    {
        if (prefab == null) return;
        lugar.y = 0.5f;
        var go = Instantiate(prefab, lugar, Quaternion.identity);

        // Los pickups del tutorial no caducan: que esperen a que el jugador los agarre.
        var caducidad = go.GetComponent<PickupCaducidad>();
        if (caducidad != null) caducidad.enabled = false;

        objetosDelPaso.Add(go);
    }

    // A "desplazamiento" del jugador y adentro del mapa, dejando "margen" alrededor para
    // lo que va pegado al punto.
    private Vector3 CercaDelJugador(Vector3 desplazamiento, float margen)
    {
        return PuntoDelMapa(jugador.transform.position, desplazamiento, LimiteDelMapa - margen);
    }

    // "desplazamiento" desde "desde", en el piso y adentro del cuadrado de -limite a +limite.
    // En cada eje, si de ese lado no hay lugar va del otro lado a la misma distancia (pegado
    // a la pared norte, la caja que iba 4 m al norte va 4 m al sur, y el zombi que venia de
    // afuera viene de adentro), y si tampoco entra se recorta al borde, lo que solo pasa con
    // el jugador ya afuera del cuadrado y lo aleja todavia mas: nada nace mas cerca de el
    // de lo pedido. Hasta el 25/9 se sumaba sin mirar las paredes: una caja del otro lado,
    // que en el tutorial no caduca, trababa el paso, y un zombi caia al vacio, lo sacaba el
    // kill-Z y el paso de disparar se daba por hecho. Estatica para la prueba.
    public static Vector3 PuntoDelMapa(Vector3 desde, Vector3 desplazamiento, float limite)
    {
        limite = Mathf.Max(0f, limite);
        return new Vector3(EnElEje(desde.x, desplazamiento.x, limite), 0f,
                           EnElEje(desde.z, desplazamiento.z, limite));
    }

    private static float EnElEje(float desde, float desplazamiento, float limite)
    {
        float punto = desde + desplazamiento;
        if (Mathf.Abs(punto) > limite) punto = desde - desplazamiento;
        return Mathf.Clamp(punto, -limite, limite);
    }

    private bool AlgunObjetoDelPasoDesaparecio()
    {
        foreach (var go in objetosDelPaso)
            if (go == null) return true;
        return false;
    }

    private void LimpiarObjetosDelPaso()
    {
        foreach (var go in objetosDelPaso) Sacar(go);
        objetosDelPaso.Clear();
    }

    // Lo que queda de un paso se va al empezar el siguiente. Un zombi muerto no: el
    // cadaver termina de desplomarse y se va solo (los del tutorial no salen del pool, y
    // al terminar se destruyen). Hasta el 25/9 se destruia al cuadro siguiente, y ni el
    // primer zombi que se mata ni los de la granada llegaban a caerse. Uno vivo se va con
    // sus particulas de muerte y sin puntos ni monedas, como los que despeja el revivir:
    // con un Destroy seco se esfumaba.
    private static void Sacar(GameObject go)
    {
        if (go == null) return;
        var zombi = go.GetComponent<EnemyController>();
        if (zombi != null)
        {
            if (!zombi.Vivo) return;
            Efectos.ParticulasDeMuerte(zombi.deathParticles, zombi.transform.position);
        }
        // Apagado en el acto y no al final del cuadro con el Destroy: asi el barrido de
        // zombis que viene despues no lo vuelve a encontrar, y no echa particulas dos veces.
        go.SetActive(false);
        Destroy(go);
    }

    private Vector3 DireccionAlAzar()
    {
        float angulo = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(angulo), 0f, Mathf.Sin(angulo));
    }

    private static string Texto(string pc, string movil)
    {
        return Plataforma.EsMovil ? movil : pc;
    }

    // Botones del panel final
    // Al libre si ya lo tiene; a un jugador nuevo, a las oleadas.
    public void IrAJugar() { SceneManager.LoadScene(ModoLibre.EscenaPara(TiendaMejoras.EscenaModoLibre)); }
    public void IrAlMenu() { SceneManager.LoadScene(0); }
}
