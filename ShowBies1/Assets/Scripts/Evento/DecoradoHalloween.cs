using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Las calabazas de Halloween (EventoHalloween): por todo el mapa en las partidas y alrededor
// del camino de los zombis en el menu. Las prefabs las arma ConstructorHalloween: la grande
// es un farol, con la cara tallada que brilla, su halo y el charco naranja en el piso (como
// los faroles de noche: ninguna luz de verdad), y la chica es solo la calabaza.
//
// Sin colliders (los zombis van derecho al jugador y se trabarian) y juntadas en pocos draw
// calls al ponerlas, como los decorados de los capitulos. Las pone InstaladorHalloween al
// cargar la escena, con una semilla fija: siempre en el mismo lugar.
public static class DecoradoHalloween
{
    public const string RutaCalabaza = "Halloween/Calabaza";
    public const string RutaCalabazaChica = "Halloween/CalabazaChica";
    public const string NombreDelGrupo = "Halloween";
    public const int Semilla = 1031;

    // El mapa: las paredes invisibles estan en +-49; las calabazas, adentro, en una grilla
    // con un poco de azar para que no se lea como grilla. Ninguna donde arranca el jugador.
    public const float Borde = 44f;
    public const float Paso = 9f;
    public const float LibreAlCentro = 6f;

    // La cara mira a la camara (hacia el sur, -Z) con este giro como mucho para cada lado.
    public const float GiroMaximo = 35f;

    // Las calabazas grandes de la partida, con un poco de azar en la escala.
    public struct Lugar
    {
        public Vector3 posicion;
        public float giro;      // grados en Y, desde mirar a la camara
        public float escala;
        public bool chica;
    }

    // Donde van en una partida: la grilla, con algunas chicas al lado de las grandes.
    public static List<Lugar> LugaresDeLaPartida()
    {
        var azar = new System.Random(Semilla);
        var lugares = new List<Lugar>();
        for (float x = -Borde; x <= Borde + 0.01f; x += Paso)
        {
            for (float z = -Borde; z <= Borde + 0.01f; z += Paso)
            {
                if (azar.NextDouble() < 0.2) continue;
                var p = new Vector3(x + Entre(azar, -4f, 4f), 0f, z + Entre(azar, -4f, 4f));
                p.x = Mathf.Clamp(p.x, -Borde, Borde);
                p.z = Mathf.Clamp(p.z, -Borde, Borde);
                if (new Vector2(p.x, p.z).magnitude < LibreAlCentro) continue;
                lugares.Add(new Lugar { posicion = p, giro = Entre(azar, -GiroMaximo, GiroMaximo), escala = Entre(azar, 0.85f, 1.2f) });
                if (azar.NextDouble() < 0.4)
                {
                    float angulo = Entre(azar, 0f, 360f) * Mathf.Deg2Rad;
                    var chica = p + new Vector3(Mathf.Cos(angulo), 0f, Mathf.Sin(angulo)) * Entre(azar, 1.1f, 1.6f);
                    lugares.Add(new Lugar { posicion = chica, giro = Entre(azar, -180f, 180f), escala = Entre(azar, 0.8f, 1.1f), chica = true });
                }
            }
        }
        return lugares;
    }

    // En el menu: a los costados del camino de los zombis (de z 0,5 a 7, ver FondoMenu), lejos
    // del centro de arriba, donde esta el titulo, y del de abajo, donde estan los botones. Las
    // grandes miran a la camara del menu (en z -6,5), con un poco de giro.
    public static readonly Vector3 CamaraDelMenu = new Vector3(0f, 3.4f, -6.5f);

    public static List<Lugar> LugaresDelMenu()
    {
        var lugares = new List<Lugar>
        {
            new Lugar { posicion = new Vector3(-5.2f, 0f, -0.6f), giro = 8f, escala = 1.1f },
            new Lugar { posicion = new Vector3(-6.4f, 0f, 0.4f), giro = -40f, escala = 0.9f, chica = true },
            new Lugar { posicion = new Vector3(5.4f, 0f, -0.2f), giro = -8f, escala = 1.15f },
            new Lugar { posicion = new Vector3(6.5f, 0f, 1.1f), giro = 60f, escala = 0.85f, chica = true },
            new Lugar { posicion = new Vector3(-8.5f, 0f, 9f), giro = 5f, escala = 1.3f },
            new Lugar { posicion = new Vector3(8.8f, 0f, 9.5f), giro = -5f, escala = 1.3f },
            new Lugar { posicion = new Vector3(-11f, 0f, 14f), giro = 0f, escala = 1.4f },
            new Lugar { posicion = new Vector3(11.5f, 0f, 15f), giro = 0f, escala = 1.4f },
        };
        for (int i = 0; i < lugares.Count; i++)
        {
            var l = lugares[i];
            if (!l.chica) l.giro += HaciaLaCamara(l.posicion, CamaraDelMenu);
            lugares[i] = l;
        }
        return lugares;
    }

    // El giro en Y que deja la cara (que mira a -Z) de frente a la camara.
    public static float HaciaLaCamara(Vector3 posicion, Vector3 camara)
    {
        Vector3 d = camara - posicion;
        return Mathf.Atan2(-d.x, -d.z) * Mathf.Rad2Deg;
    }

    public static GameObject EnLaPartida(Scene escena)
    {
        return Poner(escena, LugaresDeLaPartida(), Quaternion.Euler(70f, 0f, 0f));
    }

    // El halo de cada farol mira a la camara del menu, que FondoMenu acomoda en su Start.
    public static GameObject EnElMenu(Scene escena, Quaternion giroDeLaCamara)
    {
        return Poner(escena, LugaresDelMenu(), giroDeLaCamara);
    }

    private static GameObject Poner(Scene escena, List<Lugar> lugares, Quaternion giroDeLaCamara)
    {
        var grande = Resources.Load<GameObject>(RutaCalabaza);
        var chica = Resources.Load<GameObject>(RutaCalabazaChica);
        if (grande == null || chica == null)
        {
            Debug.LogError("DecoradoHalloween: faltan las calabazas en Resources/Halloween");
            return null;
        }

        var grupo = new GameObject(NombreDelGrupo);
        if (escena.IsValid()) SceneManager.MoveGameObjectToScene(grupo, escena);
        foreach (var lugar in lugares)
        {
            var calabaza = Object.Instantiate(lugar.chica ? chica : grande, grupo.transform);
            // La cara del prefab mira a -Z, hacia la camara.
            calabaza.transform.SetPositionAndRotation(lugar.posicion, Quaternion.Euler(0f, lugar.giro, 0f));
            calabaza.transform.localScale = Vector3.one * lugar.escala;
            // Los halos, de frente a la camara aunque la calabaza este girada.
            foreach (var t in calabaza.GetComponentsInChildren<Transform>(true))
                if (t.name == "Halo") t.rotation = giroDeLaCamara;
        }
        // De noche, la luz de relleno solo alumbra la capa de los personajes: asi la calabaza
        // se lee naranja y no como una mancha oscura.
        Personajes.PonerEnLaCapa(grupo);
        CapitulosDeEscenario.Juntar(grupo);
        return grupo;
    }

    private static float Entre(System.Random azar, float desde, float hasta)
    {
        return desde + (float)azar.NextDouble() * (hasta - desde);
    }
}
