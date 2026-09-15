using System.Collections.Generic;
using UnityEngine;

// La mancha de sangre que deja un zombi al morir, en el piso, durante un rato.
//
// Sale de un pool, como los zombis y las balas: antes cada muerte hacia un
// Instantiate de la mancha y un Destroy diferido, y con decenas de muertes por
// minuto era basura para el recolector en el telefono. El componente se agrega
// solo a la copia la primera vez, asi el prefab de la mancha no cambia.
public class ManchaDeSangre : MonoBehaviour
{
    // Apenas sobre el piso (que esta en Y = 0), para que no parpadee contra el.
    private const float AlturaSobreElPiso = 0.1f;

    // Por prefab, como el pool de zombis: hoy todos los zombis usan la misma mancha,
    // pero cada uno tiene su campo en el inspector.
    private static readonly Dictionary<GameObject, Stack<ManchaDeSangre>> pool = new Dictionary<GameObject, Stack<ManchaDeSangre>>();

    // Los static sobreviven al cambio de escena (y al "enter play mode" sin domain
    // reload); las manchas guardadas no, y se descartan al sacarlas.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetearEstadoCompartido()
    {
        pool.Clear();
    }

    private GameObject prefabDeOrigen;
    private SpriteRenderer dibujo;
    private float restante;
    private bool enUso;

    // Pone una mancha acostada en el piso bajo "posicion", con ese sprite, y la
    // levanta a los "duracion" segundos de tiempo escalado (la pausa la congela,
    // como hacia el Destroy diferido).
    public static void Poner(GameObject prefab, Vector3 posicion, Sprite sprite, float duracion)
    {
        if (prefab == null) return;

        ManchaDeSangre mancha = null;
        Stack<ManchaDeSangre> pila;
        if (pool.TryGetValue(prefab, out pila))
        {
            while (mancha == null && pila.Count > 0) mancha = pila.Pop();
        }

        if (mancha == null)
        {
            var nueva = Instantiate(prefab);
            mancha = nueva.GetComponent<ManchaDeSangre>();
            if (mancha == null) mancha = nueva.AddComponent<ManchaDeSangre>();
            mancha.prefabDeOrigen = prefab;
            mancha.dibujo = nueva.GetComponent<SpriteRenderer>();
        }

        mancha.transform.SetPositionAndRotation(new Vector3(posicion.x, AlturaSobreElPiso, posicion.z), Quaternion.Euler(90f, 0f, 0f));
        if (mancha.dibujo != null) mancha.dibujo.sprite = sprite;
        mancha.restante = duracion;
        mancha.enUso = true;
        mancha.gameObject.SetActive(true);
    }

    private void Update()
    {
        restante -= Time.deltaTime;
        if (restante <= 0f) Devolver();
    }

    private void Devolver()
    {
        // Sin esta guarda la misma mancha podria entrar dos veces en la pila.
        if (!enUso) return;

        enUso = false;
        gameObject.SetActive(false);

        Stack<ManchaDeSangre> pila;
        if (!pool.TryGetValue(prefabDeOrigen, out pila))
        {
            pila = new Stack<ManchaDeSangre>();
            pool[prefabDeOrigen] = pila;
        }
        pila.Push(this);
    }
}
