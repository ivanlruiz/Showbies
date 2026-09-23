using UnityEngine;

// Le saca el color a lo que ve la camara, de a poco. Lo usa la oferta de revivir:
// al morir, el mundo se va quedando en blanco y negro mientras la ventanita, que
// es UI en overlay, queda a todo color.
//
// Se engancha a la camara en el momento y se suelta al terminar: es un blit de
// pantalla completa y en un telefono no vale la pena tenerlo prendido toda la
// partida para usarlo diez segundos.
//
// El material sale de Resources y no de Shader.Find: un shader que nadie usa en
// una escena no entra en la build, y en el telefono se veria rosa.
[RequireComponent(typeof(Camera))]
public class FiltroBlancoYNegro : MonoBehaviour
{
    public const string RutaEnResources = "BlancoYNegro";

    // 0 = como estaba, 1 = gris del todo.
    [Range(0f, 1f)] public float cantidad;

    private Material material;
    private bool avisoDeFaltante;

    // Devuelve el filtro de esa camara, creandolo si hace falta. Null si no hay
    // camara o si falta el material: el juego sigue, sin el efecto.
    public static FiltroBlancoYNegro Enganchar(Camera camara)
    {
        if (camara == null) return null;

        var filtro = camara.GetComponent<FiltroBlancoYNegro>();
        if (filtro == null) filtro = camara.gameObject.AddComponent<FiltroBlancoYNegro>();
        filtro.cantidad = 0f;
        filtro.enabled = true;
        return filtro;
    }

    // Como Enganchar, pero sin volver a cero: el que ya estaba sigue donde estaba. Lo
    // usa la derrota, que toma el gris que dejo la oferta de revivir y lo termina, en
    // vez de volver al color un cuadro y agrisar de nuevo.
    public static FiltroBlancoYNegro Tomar(Camera camara)
    {
        if (camara == null) return null;

        var filtro = camara.GetComponent<FiltroBlancoYNegro>();
        if (filtro == null)
        {
            filtro = camara.gameObject.AddComponent<FiltroBlancoYNegro>();
            filtro.cantidad = 0f;
        }
        filtro.enabled = true;
        return filtro;
    }

    public void Soltar()
    {
        cantidad = 0f;
        enabled = false;
    }

    private void OnDisable()
    {
        cantidad = 0f;
    }

    private void OnDestroy()
    {
        if (material != null) Destroy(material);
    }

    private void OnRenderImage(RenderTexture origen, RenderTexture destino)
    {
        if (cantidad <= 0f || Material == null)
        {
            Graphics.Blit(origen, destino);
            return;
        }

        Material.SetFloat("_Cantidad", Mathf.Clamp01(cantidad));
        Graphics.Blit(origen, destino, Material);
    }

    private Material Material
    {
        get
        {
            if (material != null) return material;

            var original = Resources.Load<Material>(RutaEnResources);
            if (original == null)
            {
                if (!avisoDeFaltante)
                {
                    avisoDeFaltante = true;
                    Debug.LogError("FiltroBlancoYNegro: falta Resources/" + RutaEnResources + ".");
                }
                return null;
            }

            // Una copia propia: el material de Resources es un asset y escribirle
            // _Cantidad lo dejaria modificado en el proyecto.
            material = new Material(original);
            return material;
        }
    }
}
