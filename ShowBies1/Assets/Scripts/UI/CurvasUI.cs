using UnityEngine;

// Curvas de animacion para la UI. Todas reciben t entre 0 y 1 (lo que venga de
// afuera se limita) y las de "salida" arrancan rapido y frenan al llegar, que es lo
// que hace que un elemento se sienta con peso en vez de moverse a velocidad fija.
public static class CurvasUI
{
    // Frena suave al final.
    public static float SalidaCubica(float t)
    {
        t = Mathf.Clamp01(t);
        float u = 1f - t;
        return 1f - u * u * u;
    }

    // Se pasa un poco del destino (hasta ~1,1) y vuelve: las entradas con rebote.
    public static float SalidaAtras(float t)
    {
        t = Mathf.Clamp01(t);
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        float u = t - 1f;
        return 1f + c3 * u * u * u + c1 * u * u;
    }

    // Llega enseguida y oscila alrededor del destino cada vez menos.
    public static float SalidaElastica(float t)
    {
        t = Mathf.Clamp01(t);
        if (t <= 0f) return 0f;
        if (t >= 1f) return 1f;
        const float c4 = 2f * Mathf.PI / 3f;
        return Mathf.Pow(2f, -10f * t) * Mathf.Sin((10f * t - 0.75f) * c4) + 1f;
    }

    // Sube y vuelve a 0: para golpes de escala que terminan donde empezaron.
    public static float Campana(float t)
    {
        t = Mathf.Clamp01(t);
        return Mathf.Sin(Mathf.PI * t);
    }

    // Va y viene entre -1 y 1 apagandose hasta 0: para sacudidas.
    public static float Oscilacion(float t, float oscilaciones)
    {
        t = Mathf.Clamp01(t);
        return Mathf.Sin(2f * Mathf.PI * oscilaciones * t) * (1f - t);
    }
}
