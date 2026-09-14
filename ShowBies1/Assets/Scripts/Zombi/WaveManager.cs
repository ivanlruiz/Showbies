using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Un tipo de zombi dentro de las oleadas: desde que oleada entra en la mezcla y
// cuanto pesa en ella. Los pesos son relativos entre los tipos ya activos.
[System.Serializable]
public class TipoEnOleada
{
    public GameObject prefab;
    public int desdeOleada = 1;
    public float peso = 1f;
}

// Oleadas de verdad: cada una termina cuando mueren todos sus zombis, y antes de
// cada una hay un descanso con el cartel "Oleada N". La cantidad crece con la
// oleada, los tipos se suman a la mezcla a medida que se avanza y cada tantas
// oleadas sale un jefe ademas de los demas.
//
// Antes las oleadas eran por tiempo (la siguiente salia aunque quedaran zombis
// vivos) y cada 5 oleadas el tipo de zombi se reemplazaba en vez de sumarse:
// desde la oleada 20 solo salian jefes.
public class WaveManager : MonoBehaviour
{
    [Header("Zombis")]
    public TipoEnOleada[] tipos;
    public GameObject jefe;
    public int jefeCadaOleadas = 10;
    public Transform[] spawnPoints;          // se elige uno al azar por zombi

    [Header("Ritmo")]
    public int zombisBase = 6;               // zombis por oleada = zombisBase + zombisPorOleada * oleada
    public int zombisPorOleada = 2;
    public float intervaloEntreApariciones = 0.8f;
    public float descansoEntreOleadas = 3f;
    public int maxZombisVivos = 60;          // techo de poblacion: si esta lleno, la oleada espera
    public int maxZombisVivosMovil = 35;     // en movil cada zombi cuesta mas; ver GeneradorZombis

    [Header("HUD")]
    public TMP_Text textoOleada;             // "Oleada N" y "Zombis muertos/total" en el HUD
    public TMP_Text cartelOleada;            // cartel grande que se prende durante el descanso

    public int OleadaActual { get; private set; }

    private readonly List<GameObject> zombisDeLaOleada = new List<GameObject>();
    private int zombisEnLaOleada;
    private int muertosMostrados = -1;
    private int oleadaMostrada = -1;

    private void Start()
    {
        if (Plataforma.EsMovil) maxZombisVivos = maxZombisVivosMovil;
        if (cartelOleada != null) cartelOleada.gameObject.SetActive(false);
        StartCoroutine(Jugar());
    }

    // Una sola corrutina para toda la partida. Relanzar una por oleada desde
    // Update ya salio mal: se arrancaba una nueva en cada frame de la espera.
    private IEnumerator Jugar()
    {
        while (true)
        {
            OleadaActual++;
            int cantidad = zombisBase + zombisPorOleada * OleadaActual;
            bool conJefe = jefe != null && jefeCadaOleadas > 0 && OleadaActual % jefeCadaOleadas == 0;
            zombisEnLaOleada = cantidad + (conJefe ? 1 : 0);
            zombisDeLaOleada.Clear();
            ActualizarHud();
            yield return Descanso();

            if (conJefe)
            {
                Aparecer(jefe);
            }

            for (int i = 0; i < cantidad; i++)
            {
                while (EnemyController.ZombisVivos >= maxZombisVivos)
                {
                    yield return null;
                }

                Aparecer(ElegirTipo());
                yield return new WaitForSeconds(intervaloEntreApariciones);
            }

            while (QuedanZombisDeLaOleada())
            {
                yield return null;
            }
        }
    }

    private void Update()
    {
        ActualizarHud();
    }

    // Los zombis de la oleada que ya no estan cuentan como muertos, tambien los
    // caidos por el kill-Z: la oleada termina cuando no queda ninguno, y el
    // contador tiene que llegar al total justo en ese momento.
    private void ActualizarHud()
    {
        if (textoOleada == null) return;

        int muertos = 0;
        for (int i = 0; i < zombisDeLaOleada.Count; i++)
        {
            if (zombisDeLaOleada[i] == null) muertos++;
        }

        // Solo al cambiar: armar el texto por frame aloca por frame.
        if (muertos == muertosMostrados && OleadaActual == oleadaMostrada) return;
        muertosMostrados = muertos;
        oleadaMostrada = OleadaActual;
        textoOleada.text = "Oleada " + OleadaActual + "\n<size=75%>Zombis " + muertos + "/" + zombisEnLaOleada + "</size>";
    }

    private IEnumerator Descanso()
    {
        if (cartelOleada != null)
        {
            cartelOleada.text = "Oleada " + OleadaActual;
            cartelOleada.gameObject.SetActive(true);
        }

        yield return new WaitForSeconds(descansoEntreOleadas);

        if (cartelOleada != null) cartelOleada.gameObject.SetActive(false);
    }

    private GameObject ElegirTipo()
    {
        float total = 0f;
        foreach (var tipo in tipos)
        {
            if (tipo.prefab != null && OleadaActual >= tipo.desdeOleada) total += tipo.peso;
        }
        if (total <= 0f) return null;

        float sorteo = Random.value * total;
        foreach (var tipo in tipos)
        {
            if (tipo.prefab == null || OleadaActual < tipo.desdeOleada) continue;
            sorteo -= tipo.peso;
            if (sorteo <= 0f) return tipo.prefab;
        }
        return null;
    }

    private void Aparecer(GameObject prefab)
    {
        if (prefab == null || spawnPoints.Length == 0) return;

        Transform punto = spawnPoints[Random.Range(0, spawnPoints.Length)];
        if (punto == null) return;

        zombisDeLaOleada.Add(Instantiate(prefab, punto.position, Quaternion.identity));
    }

    // Los zombis muertos, o caidos por el kill-Z, quedan en la lista como null.
    private bool QuedanZombisDeLaOleada()
    {
        for (int i = 0; i < zombisDeLaOleada.Count; i++)
        {
            if (zombisDeLaOleada[i] != null) return true;
        }
        return false;
    }
}
