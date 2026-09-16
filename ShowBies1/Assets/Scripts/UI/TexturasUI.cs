using UnityEngine;

// Texturas de decoracion hechas en codigo, para no sumar assets: el resplandor y
// los rayos de fondo de la tienda. Son blancas con el dibujo en el alfa, asi el
// color lo pone la RawImage que las muestra.
//
// Cada llamado crea una textura nueva y quien la pide es duenio de ella: tiene que
// destruirla (OnDestroy) o queda en memoria hasta el proximo cambio de escena.
public static class TexturasUI
{
    // Un circulo que se desvanece del centro al borde.
    public static Texture2D Resplandor(int lado)
    {
        lado = Mathf.Max(2, lado);
        var pixeles = new Color32[lado * lado];
        float centro = lado * 0.5f;

        for (int y = 0; y < lado; y++)
        {
            for (int x = 0; x < lado; x++)
            {
                float d = Distancia(x, y, centro);
                float alfa = Mathf.SmoothStep(0f, 1f, 1f - d);
                pixeles[y * lado + x] = Blanco(alfa);
            }
        }

        return Crear(lado, pixeles, "Resplandor");
    }

    // 'cantidad' rayos que salen del centro y se apagan hacia el borde. Con una
    // cantidad entera el coseno empalma en la costura del atan2, sin una raya.
    public static Texture2D Rayos(int lado, int cantidad)
    {
        lado = Mathf.Max(2, lado);
        cantidad = Mathf.Max(1, cantidad);
        var pixeles = new Color32[lado * lado];
        float centro = lado * 0.5f;

        for (int y = 0; y < lado; y++)
        {
            for (int x = 0; x < lado; x++)
            {
                float d = Distancia(x, y, centro);
                float angulo = Mathf.Atan2(y + 0.5f - centro, x + 0.5f - centro);
                float alfa = (0.5f + 0.5f * Mathf.Cos(cantidad * angulo)) * Mathf.Pow(1f - d, 1.5f);
                pixeles[y * lado + x] = Blanco(alfa);
            }
        }

        return Crear(lado, pixeles, "Rayos");
    }

    // Un circulo lleno con el borde suavizado. Para el fondo del boton de video.
    public static Texture2D Circulo(int lado)
    {
        lado = Mathf.Max(2, lado);
        var pixeles = new Color32[lado * lado];
        float centro = lado * 0.5f;
        // El suavizado es de un pixel, expresado en radios.
        float borde = 1f / centro;

        for (int y = 0; y < lado; y++)
        {
            for (int x = 0; x < lado; x++)
            {
                float d = Distancia(x, y, centro);
                pixeles[y * lado + x] = Blanco(Mathf.InverseLerp(1f, 1f - borde, d));
            }
        }

        return Crear(lado, pixeles, "Circulo");
    }

    // Un anillo de 'grosor' (0 a 1, en radios). Es el sprite del reloj que se cierra
    // alrededor del boton de video: con Image.Type.Filled y Radial360 se vacia solo.
    public static Texture2D Anillo(int lado, float grosor)
    {
        lado = Mathf.Max(2, lado);
        grosor = Mathf.Clamp(grosor, 0.02f, 1f);
        var pixeles = new Color32[lado * lado];
        float centro = lado * 0.5f;
        float borde = 1f / centro;
        float interior = 1f - grosor;

        for (int y = 0; y < lado; y++)
        {
            for (int x = 0; x < lado; x++)
            {
                float d = Distancia(x, y, centro);
                float afuera = Mathf.InverseLerp(1f, 1f - borde, d);
                float adentro = Mathf.InverseLerp(interior - borde, interior, d);
                pixeles[y * lado + x] = Blanco(Mathf.Min(afuera, adentro));
            }
        }

        return Crear(lado, pixeles, "Anillo");
    }

    // El triangulo de "play", apuntando a la derecha y centrado en su ancho visual:
    // un triangulo centrado en la caja se ve corrido a la izquierda.
    public static Texture2D Play(int lado)
    {
        lado = Mathf.Max(4, lado);
        var pixeles = new Color32[lado * lado];

        for (int y = 0; y < lado; y++)
        {
            for (int x = 0; x < lado; x++)
            {
                // u de 0 (izquierda) a 1 (derecha), v de -1 a 1 (centro en 0).
                float u = (x + 0.5f) / lado;
                float v = ((y + 0.5f) / lado) * 2f - 1f;
                // Ancho de la mitad del triangulo a esa altura: 1 en la base, 0 en la punta.
                float mitad = 1f - u;
                float alfa = Mathf.Abs(v) <= mitad ? 1f : 0f;
                pixeles[y * lado + x] = Blanco(alfa);
            }
        }

        return Crear(lado, pixeles, "Play");
    }

    // Distancia del centro del pixel al centro de la textura, en radios y hasta 1.
    private static float Distancia(int x, int y, float centro)
    {
        float dx = x + 0.5f - centro;
        float dy = y + 0.5f - centro;
        return Mathf.Min(1f, Mathf.Sqrt(dx * dx + dy * dy) / centro);
    }

    private static Color32 Blanco(float alfa)
    {
        return new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Clamp01(alfa) * 255f));
    }

    private static Texture2D Crear(int lado, Color32[] pixeles, string nombre)
    {
        // Sin mipmaps (se ve siempre a un tamanio parecido) y en Clamp, para que el
        // filtrado no traiga el borde opuesto al borde.
        var textura = new Texture2D(lado, lado, TextureFormat.RGBA32, false);
        textura.name = nombre;
        textura.wrapMode = TextureWrapMode.Clamp;
        textura.filterMode = FilterMode.Bilinear;
        textura.SetPixels32(pixeles);
        // No hace falta leerla desde la CPU despues: se libera la copia.
        textura.Apply(false, true);
        return textura;
    }
}
