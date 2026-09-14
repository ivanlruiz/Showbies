using UnityEngine;
using UnityEngine.UI;

// Borde rojo que late cuando le pegan al jugador. La textura (transparente en el
// centro, opaca en los bordes) se genera al arrancar, asi no hace falta un asset.
// Va estirada sobre todo el HUD y no recibe toques.
[RequireComponent(typeof(RawImage))]
public class VinetaDanio : MonoBehaviour
{
    public Color color = new Color(1f, 0f, 0.05f, 0.7f);
    public float duracion = 0.4f;
    public int resolucion = 64;

    private static VinetaDanio activa;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetearEstadoCompartido()
    {
        activa = null;
    }

    public static void Pulso(float fuerza = 1f)
    {
        if (activa != null) activa.intensidad = Mathf.Max(activa.intensidad, Mathf.Clamp01(fuerza));
    }

    private RawImage imagen;
    private Texture2D textura;
    private float intensidad;

    private void Awake()
    {
        activa = this;
        imagen = GetComponent<RawImage>();
        imagen.raycastTarget = false;
        textura = CrearTextura(resolucion);
        imagen.texture = textura;
        Aplicar();
    }

    private void OnDestroy()
    {
        if (activa == this) activa = null;
        if (textura != null) Destroy(textura);
    }

    private void Update()
    {
        if (intensidad <= 0f) return;

        // Sin escalar: la pausa de impacto no tiene que dejarla prendida.
        intensidad = Mathf.Max(0f, intensidad - Time.unscaledDeltaTime / duracion);
        Aplicar();
    }

    private void Aplicar()
    {
        Color c = color;
        c.a *= intensidad;
        imagen.color = c;
    }

    private static Texture2D CrearTextura(int lado)
    {
        var tex = new Texture2D(lado, lado, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        var pixeles = new Color32[lado * lado];
        for (int y = 0; y < lado; y++)
        {
            for (int x = 0; x < lado; x++)
            {
                float dx = (x + 0.5f) / lado * 2f - 1f;
                float dy = (y + 0.5f) / lado * 2f - 1f;
                float d = Mathf.Sqrt(dx * dx + dy * dy) / Mathf.Sqrt(2f);
                float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.35f, 1f, d));
                pixeles[y * lado + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
        }
        tex.SetPixels32(pixeles);
        tex.Apply();
        return tex;
    }
}
