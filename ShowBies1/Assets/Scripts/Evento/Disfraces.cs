using UnityEngine;

// Los disfraces de Halloween (EventoHalloween): la cabeza de calabaza y el sombrero de bruja
// de los zombis, y el sombrero de calabaza del jugador. Son prefabs de Resources/Halloween que
// arma ConstructorHalloween con formas simples, y van colgados del hueso de la cabeza: siguen
// la animacion, el desplome y el festejo sin tocar nada mas.
//
// Cada prefab trae en su raiz la posicion, el giro y la escala que lleva respecto del hueso
// de la cabeza: el constructor los mide sobre el modelo (los cinco zombis usan el mismo, con
// otra escala, y el jugador el suyo). Por eso se instancian con el hueso de padre y sin tocar
// la raiz.
//
// La cabeza se busca por el nombre del hueso ("Bip001 Head" en los modelos de ToonyTiny) y no
// con HumanBodyBones: el menu viste a sus zombis antes de prenderlos, con el Animator sin
// arrancar, y el rapido trae un segundo Animator sin avatar.
public static class Disfraces
{
    // Desde cuantos puntos un zombi es el jefe (ZombiBOSS da 100): al armarse todavia no
    // se sabe si lo marcaron como jefe.
    public const int PuntosDelJefe = 100;

    // De cada diez zombis, tres con cabeza de calabaza y tres con sombrero de bruja. El jefe,
    // siempre con sombrero: es la bruja de la horda.
    public const float ConCalabaza = 0.3f;
    public const float ConSombrero = 0.3f;

    public const string NombreDelDisfraz = "Disfraz";
    public const string HuesoDeLaCabeza = "Head";   // el final del nombre: "Bip001 Head", no "HeadNub"

    public const string RutaCabezaDeCalabaza = "Halloween/CabezaDeCalabaza";
    public const string RutaSombreroDeBruja = "Halloween/SombreroDeBruja";
    public const string RutaSombreroDeCalabaza = "Halloween/SombreroDeCalabaza";

    private static GameObject cabezaDeCalabaza;
    private static GameObject sombreroDeBruja;
    private static GameObject sombreroDeCalabaza;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetearEstadoCompartido()
    {
        cabezaDeCalabaza = null;
        sombreroDeBruja = null;
        sombreroDeCalabaza = null;
    }

    // Disfraza un zombi (o lo deja como esta, segun el sorteo). Devuelve el disfraz o null.
    // Un zombi que ya tiene uno no se viste dos veces.
    public static GameObject Vestir(GameObject zombi, bool jefe)
    {
        return Vestir(zombi, jefe, Random.value);
    }

    public static GameObject Vestir(GameObject zombi, bool jefe, float azar)
    {
        if (zombi == null) return null;
        var cabeza = Cabeza(zombi.transform);
        if (cabeza == null || cabeza.Find(NombreDelDisfraz) != null) return null;

        if (jefe || (azar >= ConCalabaza && azar < ConCalabaza + ConSombrero))
            return Colgar(Cargar(ref sombreroDeBruja, RutaSombreroDeBruja), cabeza);
        if (azar >= ConCalabaza) return null;

        // La calabaza es la cabeza: la del zombi se esconde (es una malla rigida colgada del
        // hueso, como en todos los modelos de ToonyTiny). Antes del destello, que solo junta
        // los renderers prendidos.
        foreach (var r in cabeza.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
        return Colgar(Cargar(ref cabezaDeCalabaza, RutaCabezaDeCalabaza), cabeza);
    }

    // El sombrero de calabaza del jugador, en la cabeza de su modelo.
    public static GameObject PonerSombrero(Transform cabeza)
    {
        if (cabeza == null || cabeza.Find(NombreDelDisfraz) != null) return null;
        return Colgar(Cargar(ref sombreroDeCalabaza, RutaSombreroDeCalabaza), cabeza);
    }

    private static GameObject Colgar(GameObject prefab, Transform cabeza)
    {
        if (prefab == null) return null;
        var disfraz = Object.Instantiate(prefab, cabeza, false);
        disfraz.name = NombreDelDisfraz;
        return disfraz;
    }

    private static GameObject Cargar(ref GameObject cache, string ruta)
    {
        if (cache == null)
        {
            cache = Resources.Load<GameObject>(ruta);
            if (cache == null) Debug.LogError("Disfraces: falta Resources/" + ruta);
        }
        return cache;
    }

    // El hueso de la cabeza, por nombre: el primero que aparezca recorriendo en profundidad
    // cuyo nombre termine en "Head" y que no se dibuje (una malla podria llamarse asi).
    public static Transform Cabeza(Transform raiz)
    {
        if (raiz == null) return null;
        if (raiz.name.EndsWith(HuesoDeLaCabeza, System.StringComparison.OrdinalIgnoreCase) && raiz.GetComponent<Renderer>() == null)
            return raiz;
        for (int i = 0; i < raiz.childCount; i++)
        {
            var encontrada = Cabeza(raiz.GetChild(i));
            if (encontrada != null) return encontrada;
        }
        return null;
    }
}
