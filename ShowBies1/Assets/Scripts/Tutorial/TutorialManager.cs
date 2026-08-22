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

    private enum Paso { Moverse, Disparar, Granada, Pickups, Arma, Fin }
    private Paso paso;

    private Vector3 posicionInicial;
    private readonly List<GameObject> objetosDelPaso = new List<GameObject>();
    private bool esperandoQueVenzaLaMejora;

    private void Start()
    {
        panelFinal.SetActive(false);
        posicionInicial = jugador.transform.position;
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
                // Se completa al TIRAR la granada, que es lo que se enseña; los
                // zombis que queden los limpia el paso siguiente.
                if (FindFirstObjectByType<Granade>() != null)
                    Entrar(Paso.Pickups);
                break;

            case Paso.Pickups:
                if (AlgunObjetoDelPasoDesaparecio())
                    Entrar(Paso.Arma);
                break;

            case Paso.Arma:
                if (!esperandoQueVenzaLaMejora)
                {
                    if (jugador.theGun.MejoraActiva)
                    {
                        esperandoQueVenzaLaMejora = true;
                        textoInstruccion.text = Texto(
                            "Mirá el reloj arriba a la derecha: la cadencia mejorada dura unos segundos.\nEl cargador más grande, en cambio, queda para siempre.",
                            "Mirá el reloj arriba a la derecha: la cadencia mejorada dura unos segundos.\nEl cargador más grande, en cambio, queda para siempre.");
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
                textoInstruccion.text = Texto(
                    "Movete con W, A, S y D.",
                    "Movete con el joystick de la izquierda.");
                break;

            case Paso.Disparar:
                textoInstruccion.text = Texto(
                    "Apuntá con el mouse y mantené el click izquierdo para disparar.\n¡Viene un zombi!",
                    "Apuntá y dispará con el joystick de la derecha.\n¡Viene un zombi!");
                Spawnear(zombiPrefab, DireccionAlAzar() * distanciaSpawnZombi);
                break;

            case Paso.Granada:
                textoInstruccion.text = Texto(
                    "Cuando vengan varios juntos, tirá una granada con ESPACIO.",
                    "Cuando vengan varios juntos, tirá una granada con el botón G.");
                var centro = DireccionAlAzar() * distanciaSpawnZombi;
                Spawnear(zombiPrefab, centro);
                Spawnear(zombiPrefab, centro + new Vector3(1.5f, 0f, 0f));
                Spawnear(zombiPrefab, centro + new Vector3(0f, 0f, 1.5f));
                break;

            case Paso.Pickups:
                // Limpiar lo que haya quedado del paso de la granada
                foreach (var z in FindObjectsByType<EnemyController>(FindObjectsSortMode.None))
                    Destroy(z.gameObject);
                textoInstruccion.text = Texto(
                    "Cada tanto aparecen cajas: la de balas recarga el cargador y la de vida te cura.\nAgarrá una.",
                    "Cada tanto aparecen cajas: la de balas recarga el cargador y la de vida te cura.\nAgarrá una.");
                Spawnear(puBalasPrefab, new Vector3(4f, 0f, 2f));
                Spawnear(puVidaPrefab, new Vector3(-4f, 0f, 2f));
                break;

            case Paso.Arma:
                esperandoQueVenzaLaMejora = false;
                textoInstruccion.text = Texto(
                    "La caja de arma mejora el arma: cargador más grande y dispara mucho más rápido.\nAgarrala.",
                    "La caja de arma mejora el arma: cargador más grande y dispara mucho más rápido.\nAgarrala.");
                Spawnear(puArmaPrefab, new Vector3(0f, 0f, 4f));
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

    // Posiciones relativas al jugador, a la altura del piso.
    private void Spawnear(GameObject prefab, Vector3 desplazamiento)
    {
        if (prefab == null) return;
        var pos = jugador.transform.position + desplazamiento;
        pos.y = 0.5f;
        var go = Instantiate(prefab, pos, Quaternion.identity);

        // Los pickups del tutorial no caducan: que esperen a que el jugador los agarre.
        var caducidad = go.GetComponent<PickupCaducidad>();
        if (caducidad != null) caducidad.enabled = false;

        objetosDelPaso.Add(go);
    }

    private bool AlgunObjetoDelPasoDesaparecio()
    {
        foreach (var go in objetosDelPaso)
            if (go == null) return true;
        return false;
    }

    private void LimpiarObjetosDelPaso()
    {
        foreach (var go in objetosDelPaso)
            if (go != null) Destroy(go);
        objetosDelPaso.Clear();
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
    public void IrAJugar() { SceneManager.LoadScene(1); }
    public void IrAlMenu() { SceneManager.LoadScene(0); }
}
