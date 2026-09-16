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

    // La claqueta de cine, para el boton de video: un marco redondeado con la banda
    // de rayas diagonales arriba. Es el icono "movie" de Material Symbols dibujado a
    // mano, en las mismas proporciones (su viewBox es de 960 x 960).
    //
    // Se resuelve por muestreo: cada pixel se prueba en 4 x 4 puntos y el alfa es la
    // fraccion que cayo adentro. Es la forma corta de tener bordes suaves en
    // diagonales sin escribir un rasterizador.
    public static Texture2D Claqueta(int lado)
    {
        lado = Mathf.Max(8, lado);
        var pixeles = new Color32[lado * lado];
        const int Muestras = 4;

        for (int y = 0; y < lado; y++)
        {
            for (int x = 0; x < lado; x++)
            {
                int adentro = 0;
                for (int sy = 0; sy < Muestras; sy++)
                {
                    for (int sx = 0; sx < Muestras; sx++)
                    {
                        float u = (x + (sx + 0.5f) / Muestras) / lado * 960f;
                        // El SVG tiene la y hacia abajo y la textura hacia arriba.
                        float v = 960f - (y + (sy + 0.5f) / Muestras) / lado * 960f;
                        if (EnLaClaqueta(u, v)) adentro++;
                    }
                }
                pixeles[y * lado + x] = Blanco(adentro / (float)(Muestras * Muestras));
            }
        }

        return Crear(lado, pixeles, "Claqueta");
    }

    // Coordenadas del icono: x de 0 a 960 y de 0 (arriba) a 960 (abajo). El cuerpo
    // va de 160 a 800 en y, con la banda de rayas en el primer tercio.
    private static bool EnLaClaqueta(float x, float y)
    {
        const float Izquierda = 80f, Derecha = 880f, Arriba = 160f, Abajo = 800f;
        const float HuecoIzq = 160f, HuecoDer = 800f, HuecoArr = 400f, HuecoAba = 720f;
        const float FinDeLaBanda = 320f;

        if (!EnRectanguloRedondeado(x, y, Izquierda, Arriba, Derecha, Abajo, 56f)) return false;

        // Abajo de la banda el icono es solo el marco.
        if (y > FinDeLaBanda && x > HuecoIzq && x < HuecoDer && y > HuecoArr && y < HuecoAba) return false;

        if (y <= FinDeLaBanda)
        {
            // Rayas diagonales: 80 de ancho cada 160, corridas medio ancho por cada
            // dos de alto, que es la inclinacion del icono original.
            float corrida = x - 0.5f * (FinDeLaBanda - y);
            float fase = Mathf.Repeat(corrida - Izquierda, 160f);
            if (fase >= 80f) return false;
        }

        return true;
    }

    private static bool EnRectanguloRedondeado(float x, float y, float x0, float y0, float x1, float y1, float radio)
    {
        if (x < x0 || x > x1 || y < y0 || y > y1) return false;

        // Solo las esquinas: el centro de la esquina esta a 'radio' de los dos bordes.
        float cx = Mathf.Clamp(x, x0 + radio, x1 - radio);
        float cy = Mathf.Clamp(y, y0 + radio, y1 - radio);
        float dx = x - cx;
        float dy = y - cy;
        return dx * dx + dy * dy <= radio * radio;
    }

    // El globo del selector de idioma: un circulo con meridianos y paralelos, como el
    // icono "language" de Material Symbols. Resuelto por muestreo, igual que la
    // claqueta, para que las curvas no salgan escalonadas.
    public static Texture2D Globo(int lado)
    {
        lado = Mathf.Max(8, lado);
        var pixeles = new Color32[lado * lado];
        const int Muestras = 4;

        for (int y = 0; y < lado; y++)
        {
            for (int x = 0; x < lado; x++)
            {
                int adentro = 0;
                for (int sy = 0; sy < Muestras; sy++)
                {
                    for (int sx = 0; sx < Muestras; sx++)
                    {
                        float u = (x + (sx + 0.5f) / Muestras) / lado * 2f - 1f;
                        float v = (y + (sy + 0.5f) / Muestras) / lado * 2f - 1f;
                        if (EnElGlobo(u, v)) adentro++;
                    }
                }
                pixeles[y * lado + x] = Blanco(adentro / (float)(Muestras * Muestras));
            }
        }

        return Crear(lado, pixeles, "Globo");
    }

    // Coordenadas de -1 a 1 con el centro en 0.
    private static bool EnElGlobo(float x, float y)
    {
        const float Grosor = 0.085f;
        const float Borde = 0.92f;
        const float MitadDelGrosor = Grosor * 0.5f;

        float r = Mathf.Sqrt(x * x + y * y);
        if (r > Borde) return false;
        if (r > Borde - Grosor) return true;                                  // el contorno
        if (Mathf.Abs(x) < MitadDelGrosor) return true;                        // el meridiano del medio
        if (Mathf.Abs(y) < MitadDelGrosor) return true;                        // el ecuador
        if (Mathf.Abs(Mathf.Abs(y) - 0.46f) < MitadDelGrosor) return true;     // los dos paralelos

        // Los meridianos curvos: una elipse angosta. La distancia se aproxima
        // escalando por el semieje chico, que alcanza para un trazo parejo.
        const float SemiejeX = 0.42f;
        float semiejeY = Borde - MitadDelGrosor;
        float e = Mathf.Sqrt((x * x) / (SemiejeX * SemiejeX) + (y * y) / (semiejeY * semiejeY));
        return Mathf.Abs(e - 1f) * SemiejeX < MitadDelGrosor;
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
