using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Arma los decorados de los capitulos de las oleadas (ver CapitulosDeEscenario) con
// formas simples, sin modelos, y todos de noche con su neon (desde el 25/9, pedido de Ivan:
// "que todo el juego tenga esa tematica"):
//  - la **pradera** (faroles de neon celestes y magenta, matas y la cerca de neon del borde),
//    que tambien es el decorado del modo libre y del tutorial;
//  - el **cementerio** (lapidas, cruces, algunas de neon, arboles pelados, la reja del borde
//    con su linea violeta y faroles verdes y violetas);
//  - la **ciudad** (manzanas con vereda, edificios en el borde con carteles de neon, autos
//    con luz de color debajo, faroles, contenedores y las lineas del carril en neon).
// Todo sin colliders: los zombis van derecho al jugador y se trabarian con cualquier
// obstaculo, y los edificios van lejos del centro, que la camara mira desde arriba y
// taparian la partida.
//
// **El neon no es luz de verdad**: cada tubo es un material que no recibe luz, con un halo
// aditivo de frente a la camara, y lo que alumbra el piso es un charco pintado (ver Charco).
// Asi se ve igual en el telefono, que va con una sola luz por pixel, y cuesta cuadrados.
//
// Cada uno se guarda como prefab con sus materiales en Assets/Escenarios/<nombre>; volver
// a correrlo lo rehace igual, porque la semilla es fija. **Poner la noche** (PonerLaNoche)
// deja la noche de la pradera guardada en las tres escenas de juego.
public static class ConstructorEscenarios
{
    const string Carpeta = "Assets/Escenarios/Cementerio";
    const string RutaPrefab = "Assets/Prefabs/Escenarios/Cementerio.prefab";
    const string CarpetaCiudad = "Assets/Escenarios/Ciudad";
    const string RutaPrefabCiudad = "Assets/Prefabs/Escenarios/Ciudad.prefab";
    const string CarpetaPradera = "Assets/Escenarios/Pradera";
    public const string RutaPrefabPradera = "Assets/Prefabs/Escenarios/Pradera.prefab";
    const string RutaPisoDeDia = "Assets/Materiales/prototype_512x512_green2.mat";
    public const string RutaPisoPradera = CarpetaPradera + "/PisoPradera.mat";
    static readonly string[] EscenasDeJuego = { "Assets/Escenas/WaveMode.unity", "Assets/Escenas/ShowBies1.unity", "Assets/Escenas/Tutorial.unity" };

    // Cuantos faroles de cada decorado llevan luz de verdad (solo por vertice, tiñe a los que
    // pasan cerca); el resto, el tubo, el halo y el charco en el piso.
    const int MaxLuces = 6;
    const int MaxLucesPradera = 4;

    // La camara del juego no gira (CamaraJugador): 70 grados hacia abajo, mirando a +Z. Los
    // halos van de frente a ella y los carteles, en la cara que ella ve. La prueba de logica
    // compara este giro con el de las tres escenas.
    public static readonly Quaternion GiroDeLaCamara = Quaternion.Euler(70f, 0f, 0f);

    // Los colores del neon: los de la interfaz (ConstructorUI) y el violeta del cementerio.
    static readonly Color Violeta = new Color(0.65f, 0.3f, 1f);

    // El farol de neon: a que altura va el tubo, cuanto alumbra su luz de verdad y el charco.
    const float AlturaFarolNeon = 2.45f, AlcanceFarolNeon = 7.2f;
    // El radio del charco, en alcances de la luz: mas afuera casi no alumbra, y cada
    // charco es un cuadrado que se pinta encima del piso.
    public const float CharcoPorAlcance = 0.45f;
    // A que altura del piso va el charco. En la ciudad, por encima del cordon (0,18 m): si
    // no, la vereda lo tapa y la luz se corta en la esquina.
    const float AlturaCharco = 0.03f, AlturaCharcoCiudad = 0.2f;

    // La noche de cada capitulo: el cielo (y la niebla, que es del mismo color), la luna, la
    // luz ambiente y donde empieza y termina la niebla. La de la pradera queda guardada en las
    // escenas; las otras dos, en CapitulosDeEscenario de WaveMode.
    public static readonly Color CieloPradera = new Color(0.02f, 0.04f, 0.1f);
    static readonly Color LunaPradera = new Color(0.55f, 0.66f, 1f);
    const float IntensidadLunaPradera = 0.5f;
    static readonly Color AmbientePradera = new Color(0.15f, 0.19f, 0.3f);
    const float NieblaInicioPradera = 16f, NieblaFinPradera = 46f;

    // La luz de relleno de los personajes (ver Personajes): de frente, como la camara.
    static readonly Color ColorRelleno = new Color(0.8f, 0.85f, 1f);
    const float IntensidadRelleno = 0.95f;

    // --- La pradera: el capitulo 1 (oleadas 1-10), el modo libre y el tutorial -------------

    [MenuItem("ShowBies/Escenarios/Armar pradera")]
    public static void ArmarPradera()
    {
        Directory.CreateDirectory(CarpetaPradera);
        Directory.CreateDirectory(Path.GetDirectoryName(RutaPrefabPradera));

        var poste = MaterialEn(CarpetaPradera, "Poste", new Color(0.12f, 0.14f, 0.2f), 0.3f);
        // Mas claras que el pasto: mas oscuras, desde arriba se leian como pozos.
        var mata = MaterialEn(CarpetaPradera, "Mata", new Color(0.24f, 0.5f, 0.4f), 0.05f);
        var tallo = MaterialEn(CarpetaPradera, "Tallo", new Color(0.08f, 0.2f, 0.12f), 0.05f);
        var celeste = NeonDe(CarpetaPradera, "Celeste", ConstructorUI.Celeste, 1.3f);
        var magenta = NeonDe(CarpetaPradera, "Magenta", ConstructorUI.Magenta, 1.3f);
        var flores = new[] { celeste, magenta, NeonDe(CarpetaPradera, "Amarillo", ConstructorUI.Amarillo, 1f), NeonDe(CarpetaPradera, "Verde", ConstructorUI.Verde, 1f) };
        PisoDePradera();

        var raiz = new GameObject("Pradera");
        var azar = new System.Random(5);
        float Azar(float min, float max) => min + (float)azar.NextDouble() * (max - min);

        // Los faroles: cuatro alrededor del centro, que se ven al empezar, y el resto en
        // ronda, lejos unos de otros. Solo los cuatro del centro llevan luz de verdad.
        var faroles = Grupo(raiz, "Faroles");
        var puestos = new System.Collections.Generic.List<Vector3>();
        int luces = 0;
        foreach (var p in new[] { new Vector2(-9, -7), new Vector2(9, -7), new Vector2(-9, 7), new Vector2(9, 7) })
        {
            var donde = new Vector3(p.x, 0f, p.y);
            FarolNeon(faroles, donde, puestos.Count % 2 == 0 ? celeste : magenta, poste, luces < MaxLucesPradera);
            luces++;
            puestos.Add(donde);
        }
        for (int intento = 0; intento < 400 && puestos.Count < 16; intento++)
        {
            float angulo = Azar(0f, Mathf.PI * 2f), radio = Azar(16f, 43f);
            var donde = new Vector3(Mathf.Cos(angulo) * radio, 0f, Mathf.Sin(angulo) * radio);
            if (!LejosDeTodos(donde, puestos, 12f)) continue;
            FarolNeon(faroles, donde, puestos.Count % 2 == 0 ? celeste : magenta, poste, false);
            puestos.Add(donde);
        }

        // Matas oscuras, sueltas: le dan relieve al pasto, que de noche es liso.
        var matas = Grupo(raiz, "Matas");
        for (int intento = 0, hechas = 0; intento < 400 && hechas < 26; intento++)
        {
            float angulo = Azar(0f, Mathf.PI * 2f), radio = Azar(6f, 45f);
            var donde = new Vector3(Mathf.Cos(angulo) * radio, 0f, Mathf.Sin(angulo) * radio);
            if (!LejosDeTodos(donde, puestos, 3f)) continue;
            var g = Grupo(matas, "Mata");
            g.transform.SetPositionAndRotation(donde, Quaternion.Euler(0f, Azar(0f, 360f), 0f));
            float ancho = Azar(1.2f, 2.2f), alto = Azar(0.5f, 0.9f);
            Local(g, PrimitiveType.Sphere, new Vector3(0f, alto * 0.35f, 0f), Quaternion.identity, new Vector3(ancho, alto, ancho * Azar(0.7f, 1f)), mata);
            hechas++;
        }

        // Flores de neon en matas de tres a cinco, por todo el pasto: los puntos de color de
        // donde se juega, que los faroles quedan en los bordes de la pantalla.
        var jardin = Grupo(raiz, "Flores");
        for (int intento = 0, grupos = 0; intento < 400 && grupos < 22; intento++)
        {
            float angulo = Azar(0f, Mathf.PI * 2f), radio = Azar(4f, 45f);
            var centro = new Vector3(Mathf.Cos(angulo) * radio, 0f, Mathf.Sin(angulo) * radio);
            if (!LejosDeTodos(centro, puestos, 3.5f)) continue;
            var color = flores[grupos % flores.Length];
            int cuantas = azar.Next(3, 6);
            for (int i = 0; i < cuantas; i++)
            {
                var g = Grupo(jardin, "Flor");
                g.transform.position = centro + new Vector3(Azar(-0.9f, 0.9f), 0f, Azar(-0.9f, 0.9f));
                float alto = Azar(0.22f, 0.4f);
                Local(g, PrimitiveType.Cylinder, new Vector3(0f, alto * 0.5f, 0f), Quaternion.identity, new Vector3(0.03f, alto * 0.5f, 0.03f), tallo);
                Local(g, PrimitiveType.Sphere, new Vector3(0f, alto, 0f), Quaternion.identity, Vector3.one * 0.16f, color.tubo);
                Halo(g, new Vector3(0f, alto, 0f), 0.9f, color.haloSuave);
            }
            grupos++;
        }

        Cerca(raiz, poste, magenta);

        PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefabPradera);
        Object.DestroyImmediate(raiz);
        AssetDatabase.SaveAssets();
        Debug.Log("ConstructorEscenarios: pradera armada en " + RutaPrefabPradera + " con " + puestos.Count + " faroles");
    }

    static bool LejosDeTodos(Vector3 donde, System.Collections.Generic.List<Vector3> puestos, float distancia)
    {
        foreach (var p in puestos)
            if ((p - donde).sqrMagnitude < distancia * distancia) return false;
        return true;
    }

    // La cerca del borde: postes oscuros cada 4 m y una linea de neon que los une, con su
    // resplandor en el pasto. Es lo que marca de noche hasta donde se puede ir.
    static void Cerca(GameObject raiz, Material poste, Neon neon)
    {
        var cerca = Grupo(raiz, "Cerca");
        const float Borde = 48f, Paso = 4f;
        for (int lado = 0; lado < 4; lado++)
        {
            bool enX = lado < 2;
            float fijo = (lado % 2 == 0 ? -1 : 1) * Borde;
            for (float t = -Borde; t <= Borde + 0.01f; t += Paso)
            {
                var pos = enX ? new Vector3(t, 0f, fijo) : new Vector3(fijo, 0f, t);
                // Los postes de las esquinas los pone el lado en X.
                bool esquina = !enX && Mathf.Abs(Mathf.Abs(t) - Borde) < 0.01f;
                if (!esquina)
                    Pieza(cerca, PrimitiveType.Cube, pos + Vector3.up * 0.65f, Quaternion.identity, new Vector3(0.14f, 1.3f, 0.14f), poste);
                if (t > Borde - 0.01f) continue;
                // El tramo de neon hasta el poste siguiente.
                var medio = pos + (enX ? new Vector3(Paso * 0.5f, 1.15f, 0f) : new Vector3(0f, 1.15f, Paso * 0.5f));
                var escala = enX ? new Vector3(Paso, 0.07f, 0.07f) : new Vector3(0.07f, 0.07f, Paso);
                Pieza(cerca, PrimitiveType.Cube, medio, Quaternion.identity, escala, neon.tubo).name = "Neon";
            }
            // El resplandor en el piso, en tramos que se pisan: el charco se apaga hacia sus
            // puntas, y uno solo de 96 m brillaria solo en el medio.
            for (float t = -Borde + 4f; t <= Borde - 4f + 0.01f; t += 8f)
            {
                var g = Grupo(cerca, "Resplandor");
                g.transform.position = enX ? new Vector3(t, 0f, fijo) : new Vector3(fijo, 0f, t);
                Brillo(g, new Vector3(0f, AlturaCharco, 0f), Quaternion.Euler(90f, enX ? 0f : 90f, 0f), new Vector3(11f, 3.5f, 1f), neon.resplandor);
            }
        }
    }

    // El piso de la pradera de noche: el mismo pasto del dia, teñido de azul verdoso.
    static void PisoDePradera()
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(RutaPisoPradera);
        if (mat == null)
        {
            mat = new Material(AssetDatabase.LoadAssetAtPath<Material>(RutaPisoDeDia));
            AssetDatabase.CreateAsset(mat, RutaPisoPradera);
        }
        mat.CopyPropertiesFromMaterial(AssetDatabase.LoadAssetAtPath<Material>(RutaPisoDeDia));
        mat.color = new Color(0.42f, 0.6f, 0.66f);
        EditorUtility.SetDirty(mat);
    }

    // --- El cementerio: el capitulo 2 (oleadas 11-20) --------------------------------

    [MenuItem("ShowBies/Escenarios/Armar cementerio")]
    public static void ArmarCementerio()
    {
        Directory.CreateDirectory(Carpeta);
        Directory.CreateDirectory(Path.GetDirectoryName(RutaPrefab));

        var piedra = Material("Lapida", new Color(0.52f, 0.54f, 0.58f), 0.1f);
        var madera = Material("Madera", new Color(0.2f, 0.15f, 0.12f), 0.05f);
        var hierro = Material("Hierro", new Color(0.12f, 0.12f, 0.14f), 0.3f);
        var verde = NeonDe(Carpeta, "Verde", ConstructorUI.Verde, 1.2f);
        var violeta = NeonDe(Carpeta, "Violeta", Violeta, 1.3f);
        PisoDeTierra();

        var raiz = new GameObject("Cementerio");
        var azar = new System.Random(7);
        float Azar(float min, float max) => min + (float)azar.NextDouble() * (max - min);

        // Las tumbas, en manzanas con filas, lejos del centro donde arranca el jugador.
        var tumbas = Grupo(raiz, "Tumbas");
        Vector2[] manzanas = { new Vector2(-26, -20), new Vector2(26, -20), new Vector2(-26, 22), new Vector2(26, 22), new Vector2(0, 34), new Vector2(0, -34) };
        foreach (var centro in manzanas)
        {
            for (int fila = 0; fila < 3; fila++)
            {
                for (int col = 0; col < 5; col++)
                {
                    if (azar.NextDouble() < 0.12) continue;
                    var pos = new Vector3(centro.x + (col - 2) * 2.4f + Azar(-0.3f, 0.3f), 0f, centro.y + (fila - 1) * 3.2f + Azar(-0.3f, 0.3f));
                    var giro = Quaternion.Euler(Azar(-6f, 6f), Azar(-12f, 12f), Azar(-6f, 6f));
                    if (azar.NextDouble() < 0.25) Cruz(tumbas, pos, giro, piedra);
                    else Lapida(tumbas, pos, giro, piedra, Azar(0.8f, 1.1f));
                }
            }
        }

        // Tumbas sueltas por todo el mapa, tambien cerca del centro: la camara ve unos
        // 36 x 20 m alrededor del jugador, y sin esto el cementerio no se veia al empezar.
        // Algunas de las cruces son de neon, verde o violeta, con su resplandor.
        int deNeon = 0;
        for (int i = 0; i < 40; i++)
        {
            float angulo = Azar(0f, Mathf.PI * 2f), radio = Azar(4.5f, 45f);
            var pos = new Vector3(Mathf.Cos(angulo) * radio, 0f, Mathf.Sin(angulo) * radio);
            var giro = Quaternion.Euler(Azar(-8f, 8f), Azar(0f, 360f), Azar(-8f, 8f));
            if (azar.NextDouble() < 0.3)
            {
                if (azar.NextDouble() < 0.6) CruzDeNeon(tumbas, pos, giro, deNeon++ % 2 == 0 ? verde : violeta);
                else Cruz(tumbas, pos, giro, piedra);
            }
            else Lapida(tumbas, pos, giro, piedra, Azar(0.7f, 1.1f));
        }

        // Arboles pelados, sueltos, fuera del centro.
        var arboles = Grupo(raiz, "Arboles");
        for (int i = 0; i < 12; i++)
        {
            float angulo = Azar(0f, Mathf.PI * 2f), radio = Azar(9f, 44f);
            Arbol(arboles, new Vector3(Mathf.Cos(angulo) * radio, 0f, Mathf.Sin(angulo) * radio), Azar(0f, 360f), madera, azar);
        }

        // La reja del borde, con la baranda de arriba en neon violeta y su resplandor.
        var reja = Grupo(raiz, "Reja");
        const float Borde = 48f, Paso = 3f;
        for (int lado = 0; lado < 4; lado++)
        {
            bool enX = lado < 2;
            float fijo = (lado % 2 == 0 ? -1 : 1) * Borde;
            for (float t = -Borde; t <= Borde + 0.01f; t += Paso)
            {
                var pos = enX ? new Vector3(t, 0f, fijo) : new Vector3(fijo, 0f, t);
                Pieza(reja, PrimitiveType.Cube, pos + Vector3.up * 0.9f, Quaternion.identity, new Vector3(0.15f, 1.8f, 0.15f), hierro);
            }
            foreach (float alto in new[] { 0.6f, 1.5f })
            {
                var pos = enX ? new Vector3(0f, alto, fijo) : new Vector3(fijo, alto, 0f);
                var escala = enX ? new Vector3(Borde * 2f, 0.08f, 0.08f) : new Vector3(0.08f, 0.08f, Borde * 2f);
                Pieza(reja, PrimitiveType.Cube, pos, Quaternion.identity, escala, alto > 1f ? violeta.tubo : hierro);
            }
            for (float t = -Borde + 4f; t <= Borde - 4f + 0.01f; t += 8f)
            {
                var g = Grupo(reja, "Resplandor");
                g.transform.position = enX ? new Vector3(t, 0f, fijo) : new Vector3(fijo, 0f, t);
                Brillo(g, new Vector3(0f, AlturaCharco, 0f), Quaternion.Euler(90f, enX ? 0f : 90f, 0f), new Vector3(11f, 3.5f, 1f), violeta.resplandor);
            }
        }

        // Cuatro faroles de neon, verdes y violetas, en diagonal alrededor del centro.
        var faroles = Grupo(raiz, "Faroles");
        int i2 = 0;
        foreach (var p in new[] { new Vector2(-9, -7), new Vector2(9, -7), new Vector2(-9, 7), new Vector2(9, 7) })
            FarolNeon(faroles, new Vector3(p.x, 0f, p.y), i2++ % 2 == 0 ? verde : violeta, hierro, true);

        PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
        Object.DestroyImmediate(raiz);
        AssetDatabase.SaveAssets();
        Debug.Log("ConstructorEscenarios: cementerio armado en " + RutaPrefab + " con " + deNeon + " cruces de neon");
    }

    // --- La ciudad: el capitulo 3 (oleadas 21-30) ------------------------------------

    [MenuItem("ShowBies/Escenarios/Armar ciudad")]
    public static void ArmarCiudad()
    {
        Directory.CreateDirectory(CarpetaCiudad);
        Directory.CreateDirectory(Path.GetDirectoryName(RutaPrefabCiudad));

        var vereda = MaterialEn(CarpetaCiudad, "Vereda", new Color(0.3f, 0.3f, 0.35f), 0.35f);
        var cordon = MaterialEn(CarpetaCiudad, "Cordon", new Color(0.45f, 0.45f, 0.5f), 0.2f);
        var pared = MaterialEn(CarpetaCiudad, "Pared", new Color(0.18f, 0.17f, 0.24f), 0.05f);
        var ventana = MaterialEn(CarpetaCiudad, "Ventana", new Color(1f, 0.82f, 0.45f), 0.2f);
        Emitir(ventana, new Color(1f, 0.72f, 0.3f) * 1.6f);
        var ventanaCian = MaterialEn(CarpetaCiudad, "VentanaCian", new Color(0.4f, 0.95f, 1f), 0.2f);
        Emitir(ventanaCian, new Color(0f, 0.8f, 1f) * 1.4f);
        var ventanaMagenta = MaterialEn(CarpetaCiudad, "VentanaMagenta", new Color(1f, 0.5f, 0.9f), 0.2f);
        Emitir(ventanaMagenta, new Color(1f, 0.2f, 0.8f) * 1.4f);
        var ventanas = new[] { ventana, ventana, ventana, ventana, ventanaCian, ventanaMagenta };
        var poste = MaterialEn(CarpetaCiudad, "Poste", new Color(0.14f, 0.14f, 0.16f), 0.3f);
        var contenedor = MaterialEn(CarpetaCiudad, "Contenedor", new Color(0.16f, 0.35f, 0.2f), 0.15f);
        var rojo = MaterialEn(CarpetaCiudad, "AutoRojo", new Color(0.5f, 0.12f, 0.12f), 0.35f);
        var azul = MaterialEn(CarpetaCiudad, "AutoAzul", new Color(0.13f, 0.24f, 0.45f), 0.35f);
        var blanco = MaterialEn(CarpetaCiudad, "AutoBlanco", new Color(0.62f, 0.62f, 0.6f), 0.35f);
        var vidrio = MaterialEn(CarpetaCiudad, "Vidrio", new Color(0.1f, 0.14f, 0.18f), 0.6f);
        var rueda = MaterialEn(CarpetaCiudad, "Rueda", new Color(0.07f, 0.07f, 0.08f), 0.1f);
        var autos = new[] { rojo, azul, blanco };
        var magenta = NeonDe(CarpetaCiudad, "Magenta", ConstructorUI.Magenta, 1.3f);
        var celeste = NeonDe(CarpetaCiudad, "Celeste", ConstructorUI.Celeste, 1.3f);
        var amarillo = NeonDe(CarpetaCiudad, "Amarillo", ConstructorUI.Amarillo, 1.1f);
        var verde = NeonDe(CarpetaCiudad, "Verde", ConstructorUI.Verde, 1.1f);
        var neones = new[] { magenta, celeste, amarillo, verde };
        var fuente = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fuentes/Bangers SDF.asset");
        var textos = new Material[neones.Length];
        for (int i = 0; i < neones.Length; i++) textos[i] = MaterialDeCartel(neones[i]);
        PisoDeAsfalto();

        var raiz = new GameObject("Ciudad");
        var azar = new System.Random(11);
        float Azar(float min, float max) => min + (float)azar.NextDouble() * (max - min);

        // La cuadricula: manzanas de 14 x 14 con calles de 10 en el medio, corridas media
        // manzana para que **el cruce quede en el centro**: ahi arranca el jugador, en la
        // calle y con cuatro esquinas alrededor. Parado sobre una vereda lisa de 14 x 14
        // no se entendia que fuera una ciudad.
        const float Paso = 24f, Manzana = 14f;
        var manzanas = Grupo(raiz, "Manzanas");
        var faroles = Grupo(raiz, "Faroles");
        var cosas = Grupo(raiz, "Cosas");
        var carteles = Grupo(raiz, "Carteles");
        string[] letreros = { "BAR", "24H", "MOTEL", "PIZZA", "CLUB", "HOTEL", "ZOMBIS", "DINER", "ARCADE", "SHOW", "CAFE", "NEON" };
        int luces = 0, cartel = 0, farol = 0;

        for (int ix = -2; ix <= 1; ix++)
        {
            for (int iz = -2; iz <= 1; iz++)
            {
                var centro = new Vector3(ix * Paso + Paso * 0.5f, 0f, iz * Paso + Paso * 0.5f);
                bool esElCentro = centro.magnitude < Paso;   // las cuatro esquinas del cruce
                Vereda(manzanas, centro, Manzana, vereda, cordon);

                // Los edificios, solo lejos: desde arriba, uno cerca taparia la partida. Cada
                // uno con su cartel de neon en la cara que ve la camara.
                if (centro.magnitude > 30f)
                {
                    float alto = Azar(3.5f, 6.5f);
                    Edificio(manzanas, centro, Manzana, alto, pared, ventanas, azar);
                    int n = cartel % neones.Length;
                    Cartel(carteles, centro + new Vector3(0f, 0f, -(Manzana - 3f) * 0.5f), Mathf.Min(alto - 1f, 3f), letreros[cartel % letreros.Length], neones[n], pared, textos[n], fuente);
                    cartel++;
                }
                else
                {
                    // Cerca del centro, cosas bajas que no tapan.
                    int cuantas = azar.Next(3, 6);
                    for (int i = 0; i < cuantas; i++)
                    {
                        var donde = centro + new Vector3(Azar(-5.5f, 5.5f), 0f, Azar(-5.5f, 5.5f));
                        if (donde.magnitude < 7f) continue;    // nada encima del jugador
                        if (azar.NextDouble() < 0.5) Contenedor(cosas, donde, Azar(0f, 360f), contenedor);
                        else Cantero(cosas, donde, Azar(0f, 360f), cordon, contenedor);
                    }
                    // Las dos de arriba del cruce, un letrero bajo parado en la vereda: son los
                    // que se ven al empezar. Cortos y hacia el medio, que arriba en las puntas
                    // estan los numeros del HUD.
                    if (centro.z > 0f)
                    {
                        int n = centro.x < 0f ? 0 : 1;
                        var pie = new Vector3(Mathf.Sign(centro.x) * 6f, 0f, centro.z - Manzana * 0.5f + 0.8f);
                        Letrero(carteles, pie, centro.x < 0f ? "BAR" : "24H", neones[n], poste, pared, textos[n], fuente);
                        cartel++;
                    }
                }

                // Un farol en cada esquina de manzana, mirando a la calle. Solo los seis
                // mas cercanos llevan luz de verdad: en el telefono cada una cuesta.
                foreach (var esquina in new[] { new Vector2(-1, -1), new Vector2(1, -1), new Vector2(-1, 1), new Vector2(1, 1) })
                {
                    var donde = centro + new Vector3(esquina.x * (Manzana * 0.5f + 1.2f), 0f, esquina.y * (Manzana * 0.5f + 1.2f));
                    // Los de la manzana del centro van siempre: son los que alumbran
                    // donde se juega. Los de afuera, la mitad de las veces.
                    if (!esElCentro && azar.NextDouble() < 0.45) continue;
                    // El tope vale para todos: cada luz puntual cuesta en el telefono, y
                    // dejando pasar las del centro sin contarlas salian dieciseis.
                    bool conLuz = luces < MaxLuces && (esElCentro || donde.magnitude < 28f);
                    FarolNeon(faroles, donde, farol++ % 2 == 0 ? magenta : celeste, poste, conLuz, AlturaCharcoCiudad);
                    if (conLuz) luces++;
                }
            }
        }

        // Las lineas del medio de cada calle, en neon amarillo, con su resplandor en tramos.
        var lineas = Grupo(raiz, "Lineas");
        for (int i = -2; i <= 2; i++)
        {
            float fijo = i * Paso;
            if (Mathf.Abs(fijo) > 50f) continue;
            for (float t2 = -48f; t2 <= 48f; t2 += 4f)
            {
                Pieza(lineas, PrimitiveType.Cube, new Vector3(t2, 0.02f, fijo), Quaternion.identity, new Vector3(2f, 0.04f, 0.22f), amarillo.tubo);
                Pieza(lineas, PrimitiveType.Cube, new Vector3(fijo, 0.02f, t2), Quaternion.identity, new Vector3(0.22f, 0.04f, 2f), amarillo.tubo);
            }
            for (float t2 = -44f; t2 <= 44f; t2 += 11f)
            {
                var gx = Grupo(lineas, "Resplandor");
                gx.transform.position = new Vector3(t2, 0f, fijo);
                Brillo(gx, new Vector3(0f, AlturaCharco, 0f), Quaternion.Euler(90f, 0f, 0f), new Vector3(13f, 2.4f, 1f), amarillo.resplandor);
                var gz = Grupo(lineas, "Resplandor");
                gz.transform.position = new Vector3(fijo, 0f, t2);
                Brillo(gz, new Vector3(0f, AlturaCharco, 0f), Quaternion.Euler(90f, 90f, 0f), new Vector3(13f, 2.4f, 1f), amarillo.resplandor);
            }
        }

        // Autos estacionados contra el cordon, con una luz de color debajo.
        var flota = Grupo(raiz, "Autos");
        for (int i = 0; i < 22; i++)
        {
            bool enX = azar.NextDouble() < 0.5;
            float calle = azar.Next(-2, 3) * Paso;
            float largo = Azar(-44f, 44f);
            var donde = enX ? new Vector3(largo, 0f, calle + (azar.NextDouble() < 0.5 ? -3.2f : 3.2f))
                            : new Vector3(calle + (azar.NextDouble() < 0.5 ? -3.2f : 3.2f), 0f, largo);
            if (donde.magnitude < 12f) continue;   // no encima del jugador
            Auto(flota, donde, enX ? 90f : 0f, autos[azar.Next(autos.Length)], vidrio, rueda, neones[azar.Next(neones.Length)]);
        }

        PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefabCiudad);
        Object.DestroyImmediate(raiz);
        AssetDatabase.SaveAssets();
        Debug.Log("ConstructorEscenarios: ciudad armada en " + RutaPrefabCiudad + " con " + luces + " faroles con luz y " + cartel + " carteles");
    }

    // La manzana: la vereda elevada y su cordon.
    static void Vereda(GameObject padre, Vector3 centro, float lado, Material vereda, Material cordon)
    {
        var g = Grupo(padre, "Manzana");
        g.transform.position = centro;
        Local(g, PrimitiveType.Cube, new Vector3(0f, 0.07f, 0f), Quaternion.identity, new Vector3(lado, 0.14f, lado), vereda);
        float mitad = lado * 0.5f;
        foreach (var lado2 in new[] { new Vector2(0, 1), new Vector2(0, -1), new Vector2(1, 0), new Vector2(-1, 0) })
        {
            var pos = new Vector3(lado2.x * mitad, 0.09f, lado2.y * mitad);
            var escala = lado2.x != 0 ? new Vector3(0.3f, 0.18f, lado + 0.3f) : new Vector3(lado + 0.3f, 0.18f, 0.3f);
            Local(g, PrimitiveType.Cube, pos, Quaternion.identity, escala, cordon);
        }
    }

    // Un edificio bajo con sus ventanas encendidas, algunas de color.
    static void Edificio(GameObject padre, Vector3 centro, float lado, float alto, Material pared, Material[] ventanas, System.Random azar)
    {
        var g = Grupo(padre, "Edificio");
        g.transform.position = centro;
        float ancho = lado - 3f;
        Local(g, PrimitiveType.Cube, new Vector3(0f, alto * 0.5f, 0f), Quaternion.identity, new Vector3(ancho, alto, ancho), pared);
        // Unas ventanas prendidas en las cuatro caras, a la altura de cada piso.
        int pisos = Mathf.Max(1, Mathf.FloorToInt(alto / 1.6f));
        for (int piso = 0; piso < pisos; piso++)
        {
            float y = 0.9f + piso * 1.6f;
            if (y > alto - 0.4f) break;
            for (int cara = 0; cara < 4; cara++)
            {
                if (azar.NextDouble() < 0.45) continue;
                float angulo = cara * 90f * Mathf.Deg2Rad;
                var normal = new Vector3(Mathf.Sin(angulo), 0f, Mathf.Cos(angulo));
                var lateral = new Vector3(normal.z, 0f, -normal.x);
                float corrimiento = (float)(azar.NextDouble() - 0.5) * (ancho - 2.4f);
                var pos = normal * (ancho * 0.5f + 0.03f) + lateral * corrimiento + Vector3.up * y;
                var escala = Vector3.Scale(new Vector3(1.1f, 0.8f, 1.1f), new Vector3(Mathf.Abs(lateral.x) + 0.06f, 1f, Mathf.Abs(lateral.z) + 0.06f));
                Local(g, PrimitiveType.Cube, pos, Quaternion.identity, escala, ventanas[azar.Next(ventanas.Length)]);
            }
        }
    }

    // Un cartel de neon en la cara sur de un edificio, la que ve la camara del juego (que mira
    // siempre hacia +Z; en la otra se veria de atras): el tablero oscuro, el marco de neon, el
    // texto con el halo de su color, un resplandor grande y el reflejo en la calle de enfrente.
    // 'cara' es el punto de la pared a la altura del piso.
    static void Cartel(GameObject padre, Vector3 cara, float altura, string texto, Neon neon, Material pared, Material materialTexto, TMP_FontAsset fuente)
    {
        var g = Grupo(padre, "Cartel");
        g.transform.position = cara + new Vector3(0f, 0f, -0.1f);
        Tablero(g, new Vector3(0f, altura, 0f), texto, neon, pared, materialTexto, fuente);
        // El reflejo, estirado hacia la camara: la vereda y la calle de delante, mojadas.
        Brillo(g, new Vector3(0f, AlturaCharcoCiudad, -3.5f), Quaternion.Euler(90f, 0f, 0f), new Vector3(1.3f + 0.9f * texto.Length + 2f, 7f, 1f), neon.charco);
    }

    // Un letrero parado en la vereda, con su poste: los de cerca del centro, sin edificio.
    static void Letrero(GameObject padre, Vector3 pie, string texto, Neon neon, Material poste, Material pared, Material materialTexto, TMP_FontAsset fuente)
    {
        var g = Grupo(padre, "Cartel");
        g.transform.position = pie;
        Local(g, PrimitiveType.Cylinder, new Vector3(0f, 0.7f, 0.2f), Quaternion.identity, new Vector3(0.14f, 0.7f, 0.14f), poste);
        Tablero(g, new Vector3(0f, 2.1f, 0f), texto, neon, pared, materialTexto, fuente);
        Brillo(g, new Vector3(0f, AlturaCharcoCiudad, -2.5f), Quaternion.Euler(90f, 0f, 0f), new Vector3(1.3f + 0.9f * texto.Length + 2f, 6f, 1f), neon.charco);
    }

    // El tablero de un cartel: vertical, de frente a la camara (su texto se lee desde -Z).
    static void Tablero(GameObject g, Vector3 centro, string texto, Neon neon, Material pared, Material materialTexto, TMP_FontAsset fuente)
    {
        float largo = 1.3f + 0.9f * texto.Length;
        const float Alto = 1.6f;
        Local(g, PrimitiveType.Cube, centro, Quaternion.identity, new Vector3(largo, Alto, 0.12f), pared);
        Local(g, PrimitiveType.Cube, centro + new Vector3(0f, Alto * 0.5f, -0.08f), Quaternion.identity, new Vector3(largo, 0.07f, 0.05f), neon.tubo);
        Local(g, PrimitiveType.Cube, centro + new Vector3(0f, -Alto * 0.5f, -0.08f), Quaternion.identity, new Vector3(largo, 0.07f, 0.05f), neon.tubo);
        Local(g, PrimitiveType.Cube, centro + new Vector3(-largo * 0.5f, 0f, -0.08f), Quaternion.identity, new Vector3(0.07f, Alto, 0.05f), neon.tubo);
        Local(g, PrimitiveType.Cube, centro + new Vector3(largo * 0.5f, 0f, -0.08f), Quaternion.identity, new Vector3(0.07f, Alto, 0.05f), neon.tubo);

        var go = new GameObject("Texto");
        go.transform.SetParent(g.transform, false);
        go.transform.localPosition = centro + new Vector3(0f, 0f, -0.1f);
        var tmp = go.AddComponent<TextMeshPro>();
        tmp.font = fuente;
        tmp.fontSharedMaterial = materialTexto;
        tmp.enableVertexGradient = false;
        tmp.color = Color.Lerp(neon.color, Color.white, 0.45f);
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.fontSize = 12f;
        tmp.rectTransform.sizeDelta = new Vector2(largo, Alto);
        tmp.text = texto;
        var render = go.GetComponent<MeshRenderer>();
        render.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        render.receiveShadows = false;

        Halo(g, centro + new Vector3(0f, 0f, -0.3f), largo + 3f, neon.haloSuave);
    }

    // El material del texto de un cartel: el de los titulos de neon, con el halo de su color.
    static Material MaterialDeCartel(Neon neon)
    {
        string ruta = CarpetaCiudad + "/Cartel" + neon.nombre + ".mat";
        var baseNeon = AssetDatabase.LoadAssetAtPath<Material>("Assets/Fuentes/Bangers SDF - Neon.mat");
        var mat = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (mat == null)
        {
            mat = new Material(baseNeon);
            AssetDatabase.CreateAsset(mat, ruta);
        }
        mat.shader = baseNeon.shader;
        mat.CopyPropertiesFromMaterial(baseNeon);
        mat.shaderKeywords = baseNeon.shaderKeywords;
        mat.SetColor("_UnderlayColor", new Color(neon.color.r, neon.color.g, neon.color.b, 0.9f));
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static void Auto(GameObject padre, Vector3 pos, float rumbo, Material color, Material vidrio, Material rueda, Neon abajo)
    {
        var g = Grupo(padre, "Auto");
        g.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, rumbo, 0f));
        Local(g, PrimitiveType.Cube, new Vector3(0f, 0.55f, 0f), Quaternion.identity, new Vector3(1.8f, 0.7f, 4.2f), color);
        Local(g, PrimitiveType.Cube, new Vector3(0f, 1.1f, -0.2f), Quaternion.identity, new Vector3(1.6f, 0.6f, 2.2f), vidrio);
        foreach (var r in new[] { new Vector2(-0.95f, 1.3f), new Vector2(0.95f, 1.3f), new Vector2(-0.95f, -1.3f), new Vector2(0.95f, -1.3f) })
            Local(g, PrimitiveType.Cylinder, new Vector3(r.x, 0.33f, r.y), Quaternion.Euler(0f, 0f, 90f), new Vector3(0.66f, 0.12f, 0.66f), rueda);
        // La luz de color de abajo, que se asoma alrededor del auto.
        Brillo(g, new Vector3(0f, 0.04f, 0f), Quaternion.Euler(90f, 0f, 0f), new Vector3(3.2f, 5.6f, 1f), abajo.resplandor);
    }

    static void Contenedor(GameObject padre, Vector3 pos, float rumbo, Material material)
    {
        var g = Grupo(padre, "Contenedor");
        g.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, rumbo, 0f));
        Local(g, PrimitiveType.Cube, new Vector3(0f, 0.55f, 0f), Quaternion.identity, new Vector3(1.6f, 1.1f, 1.1f), material);
        Local(g, PrimitiveType.Cube, new Vector3(0f, 1.15f, 0f), Quaternion.Euler(0f, 0f, 4f), new Vector3(1.7f, 0.12f, 1.2f), material);
    }

    static void Cantero(GameObject padre, Vector3 pos, float rumbo, Material borde, Material planta)
    {
        var g = Grupo(padre, "Cantero");
        g.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, rumbo, 0f));
        Local(g, PrimitiveType.Cube, new Vector3(0f, 0.2f, 0f), Quaternion.identity, new Vector3(2.2f, 0.4f, 2.2f), borde);
        Local(g, PrimitiveType.Sphere, new Vector3(0f, 0.75f, 0f), Quaternion.identity, new Vector3(1.6f, 1.1f, 1.6f), planta);
    }

    // El asfalto mojado: oscuro y con brillo, para que la luna y los charcos se lean encima.
    static void PisoDeAsfalto()
    {
        string ruta = CarpetaCiudad + "/PisoCiudad.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Standard"));
            AssetDatabase.CreateAsset(mat, ruta);
        }
        mat.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Five Seamless Tileable Ground Textures/Textures/Grey Stones.png");
        // Mas repetida que la del cementerio: el asfalto es de grano mas fino.
        mat.mainTextureScale = new Vector2(70f, 70f);
        mat.color = new Color(0.32f, 0.31f, 0.4f);
        mat.SetFloat("_Glossiness", 0.55f);
        mat.SetFloat("_Metallic", 0f);
        EditorUtility.SetDirty(mat);
    }

    // --- Las piezas de neon ------------------------------------------------------------

    // Un color de neon con sus materiales: el tubo que brilla (no recibe luz), el halo de
    // frente a la camara (fuerte y suave), el charco que pinta en el piso y el resplandor
    // mas tenue de las lineas largas (la cerca, la reja, las lineas del carril).
    class Neon
    {
        public string nombre;
        public Color color;
        public Material tubo, halo, haloSuave, charco, resplandor;
    }

    static Neon NeonDe(string carpeta, string nombre, Color color, float intensidadCharco)
    {
        return new Neon
        {
            nombre = nombre,
            color = color,
            tubo = MaterialPlano(carpeta, "Neon" + nombre, Color.Lerp(color, Color.white, 0.3f)),
            halo = MaterialDeBrillo(carpeta, "Halo" + nombre, color, 1.4f, 0.25f, 4f),
            haloSuave = MaterialDeBrillo(carpeta, "HaloSuave" + nombre, color, 0.7f, 0.25f, 4f),
            charco = MaterialDeBrillo(carpeta, "Charco" + nombre, color, intensidadCharco, 0.35f, 5f),
            resplandor = MaterialDeBrillo(carpeta, "Resplandor" + nombre, color, 0.5f, 0.5f, 3f),
        };
    }

    // Un farol de neon: el poste oscuro, el tubo acostado que brilla (de costado para la
    // camara), su halo y el charco de su color en el piso. La luz de verdad, si lleva, va
    // solo por vertice: tiñe a los que pasan cerca, y el piso lo pinta el charco.
    static GameObject FarolNeon(GameObject padre, Vector3 pos, Neon neon, Material poste, bool conLuz, float alturaCharco = AlturaCharco)
    {
        var g = Grupo(padre, "Farol");
        g.transform.position = pos;
        Local(g, PrimitiveType.Cylinder, new Vector3(0f, AlturaFarolNeon * 0.5f, 0f), Quaternion.identity, new Vector3(0.12f, AlturaFarolNeon * 0.5f, 0.12f), poste);
        Local(g, PrimitiveType.Cylinder, new Vector3(0f, AlturaFarolNeon, 0f), Quaternion.Euler(0f, 0f, 90f), new Vector3(0.14f, 0.55f, 0.14f), neon.tubo);
        Halo(g, new Vector3(0f, AlturaFarolNeon, 0f), 2f, neon.halo);
        Charco(g, new Vector3(0f, alturaCharco, 0f), AlcanceFarolNeon, neon.charco);
        if (conLuz)
        {
            var luzGo = new GameObject("Luz");
            luzGo.transform.SetParent(g.transform, false);
            luzGo.transform.localPosition = Vector3.up * AlturaFarolNeon;
            Luz(luzGo, neon.color, AlcanceFarolNeon, 1.4f);
        }
        return g;
    }

    // Una cruz del cementerio hecha de tubos de neon, con su halo y su charco. El grupo va
    // derecho y solo el cuerpo se inclina: con todo inclinado, el charco quedaba medio
    // enterrado y se veia como una franja cortada en el piso (lo vio Ivan el 25/9).
    static void CruzDeNeon(GameObject padre, Vector3 pos, Quaternion giro, Neon neon)
    {
        var g = Grupo(padre, "Cruz");
        g.transform.position = pos;
        var cuerpo = Grupo(g, "Cuerpo");
        cuerpo.transform.localRotation = giro;
        Local(cuerpo, PrimitiveType.Cube, new Vector3(0f, 0.75f, 0f), Quaternion.identity, new Vector3(0.12f, 1.5f, 0.12f), neon.tubo);
        Local(cuerpo, PrimitiveType.Cube, new Vector3(0f, 1.05f, 0f), Quaternion.identity, new Vector3(0.75f, 0.12f, 0.12f), neon.tubo);
        Halo(g, new Vector3(0f, 0.95f, 0f), 1.8f, neon.halo);
        Charco(g, new Vector3(0f, AlturaCharco, 0f), 4f, neon.charco);
    }

    // Un halo de frente a la camara del juego: un cuadrado aditivo con el shader del charco
    // (que se apaga desde el centro), parado como la camara lo ve.
    static void Halo(GameObject grupo, Vector3 posicionLocal, float lado, Material material)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = "Halo";
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(grupo.transform, false);
        go.transform.localPosition = posicionLocal;
        go.transform.rotation = GiroDeLaCamara;
        go.transform.localScale = new Vector3(lado, lado, 1f);
        SinSombra(go.GetComponent<MeshRenderer>(), material);
    }

    // Un cuadrado aditivo acostado (o como se lo gire): los resplandores largos.
    static void Brillo(GameObject grupo, Vector3 posicionLocal, Quaternion giroLocal, Vector3 escala, Material material)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = "Brillo";
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(grupo.transform, false);
        go.transform.localPosition = posicionLocal;
        go.transform.localRotation = giroLocal;
        go.transform.localScale = escala;
        SinSombra(go.GetComponent<MeshRenderer>(), material);
    }

    static void SinSombra(MeshRenderer render, Material material)
    {
        render.sharedMaterial = material;
        render.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        render.receiveShadows = false;
        render.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        render.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
    }

    // La luz de un farol, **solo por vertice** (ForceVertex): tiñe a los zombis y al jugador
    // que pasan cerca, igual en el editor que en el telefono. El piso no lo alumbra ella sino
    // su charco. Con el modo en Auto, en el telefono (calidad Medium, una luz por pixel, que
    // se lleva la luna) ya caia a vertice, y el piso tiene un vertice cada diez metros: no
    // se veia nada. En el editor, en Ultra, alumbraba por pixel y se veia bien, que es por
    // lo que no se noto.
    static void Luz(GameObject go, Color color, float alcance, float intensidad)
    {
        var luz = go.AddComponent<Light>();
        luz.type = LightType.Point;
        luz.color = color;
        luz.range = alcance;
        luz.intensity = intensidad;
        luz.shadows = LightShadows.None;
        luz.renderMode = LightRenderMode.ForceVertex;
    }

    // El charco de luz de un farol en el piso: un cuadrado acostado con el shader
    // ShowBies/CharcoDeLuz, que se apaga desde el centro como una luz puntual colgada encima.
    // Es lo que hace que el farol alumbre el piso en el telefono, y cuesta un cuadrado
    // aditivo sin textura. Va adentro del grupo del farol, asi sale del piso con el. Se llama
    // "Charco": el halo usa el mismo shader, y la prueba lo distingue por el nombre.
    static void Charco(GameObject grupo, Vector3 bajoLaLuz, float alcance, Material material)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = "Charco";
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(grupo.transform, false);
        go.transform.localPosition = bajoLaLuz;
        go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        float diametro = alcance * CharcoPorAlcance * 2f;
        go.transform.localScale = new Vector3(diametro, diametro, 1f);
        SinSombra(go.GetComponent<MeshRenderer>(), material);
    }

    // Un material con el shader del charco. 'altura' y 'caida' van en radios del cuadrado:
    // con la altura baja el centro brilla mas, y con la caida alta se apaga antes.
    static Material MaterialDeBrillo(string carpeta, string nombre, Color color, float intensidad, float altura, float caida)
    {
        var shader = Shader.Find("ShowBies/CharcoDeLuz");
        string ruta = carpeta + "/" + nombre + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, ruta);
        }
        mat.shader = shader;
        mat.SetColor("_Color", color);
        mat.SetFloat("_Intensidad", intensidad);
        mat.SetFloat("_Altura", altura);
        mat.SetFloat("_Caida", caida);
        mat.enableInstancing = true;
        EditorUtility.SetDirty(mat);
        return mat;
    }

    // Un tubo de neon: color plano, sin luz, con la niebla (Unlit/Color la trae).
    static Material MaterialPlano(string carpeta, string nombre, Color color)
    {
        var shader = Shader.Find("Unlit/Color");
        string ruta = carpeta + "/" + nombre + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, ruta);
        }
        mat.shader = shader;
        mat.color = color;
        mat.enableInstancing = true;
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static void Emitir(Material mat, Color color)
    {
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", color);
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        EditorUtility.SetDirty(mat);
    }

    static Material MaterialEn(string carpeta, string nombre, Color color, float brillo)
    {
        string ruta = carpeta + "/" + nombre + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Standard"));
            AssetDatabase.CreateAsset(mat, ruta);
        }
        mat.color = color;
        mat.SetFloat("_Glossiness", brillo);
        mat.SetFloat("_Metallic", 0f);
        mat.enableInstancing = true;
        EditorUtility.SetDirty(mat);
        return mat;
    }

    // La tierra del cementerio, de noche con un tinte violeta. El menu la usa tambien (su
    // fondo es de noche, con la tierra del cementerio: ver FondoMenu).
    static void PisoDeTierra()
    {
        string ruta = Carpeta + "/PisoCementerio.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Standard"));
            AssetDatabase.CreateAsset(mat, ruta);
        }
        mat.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Five Seamless Tileable Ground Textures/Textures/Brown Stony.png");
        // El plano mide 100 m: repetida 45 veces, cada piedra queda del tamanio de un pie.
        mat.mainTextureScale = new Vector2(45f, 45f);
        mat.color = new Color(0.42f, 0.39f, 0.5f);
        mat.SetFloat("_Glossiness", 0.05f);
        mat.SetFloat("_Metallic", 0f);
        EditorUtility.SetDirty(mat);
    }

    static Material Material(string nombre, Color color, float brillo)
    {
        return MaterialEn(Carpeta, nombre, color, brillo);
    }

    static GameObject Grupo(GameObject padre, string nombre)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(padre.transform, false);
        return go;
    }

    static GameObject Pieza(GameObject padre, PrimitiveType tipo, Vector3 posicion, Quaternion rotacion, Vector3 escala, Material material)
    {
        var go = GameObject.CreatePrimitive(tipo);
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(padre.transform, true);
        go.transform.SetPositionAndRotation(posicion, rotacion);
        go.transform.localScale = escala;
        go.GetComponent<MeshRenderer>().sharedMaterial = material;
        return go;
    }

    // Una lapida: la losa y un cilindro acostado que le redondea la punta. Cada una es
    // un grupo propio, asi CapitulosDeEscenario las hace salir del piso de a una.
    static void Lapida(GameObject padre, Vector3 pos, Quaternion giro, Material piedra, float alto)
    {
        var g = Grupo(padre, "Lapida");
        g.transform.SetPositionAndRotation(pos, giro);
        Local(g, PrimitiveType.Cube, new Vector3(0f, alto * 0.5f, 0f), Quaternion.identity, new Vector3(0.9f, alto, 0.25f), piedra);
        Local(g, PrimitiveType.Cylinder, new Vector3(0f, alto, 0f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.9f, 0.125f, 0.9f), piedra);
    }

    static void Cruz(GameObject padre, Vector3 pos, Quaternion giro, Material piedra)
    {
        var g = Grupo(padre, "Cruz");
        g.transform.SetPositionAndRotation(pos, giro);
        Local(g, PrimitiveType.Cube, new Vector3(0f, 0.75f, 0f), Quaternion.identity, new Vector3(0.22f, 1.5f, 0.22f), piedra);
        Local(g, PrimitiveType.Cube, new Vector3(0f, 1.05f, 0f), Quaternion.identity, new Vector3(0.85f, 0.22f, 0.22f), piedra);
    }

    static void Arbol(GameObject padre, Vector3 pos, float rumbo, Material madera, System.Random azar)
    {
        var g = Grupo(padre, "Arbol");
        g.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, rumbo, 0f));
        float alto = 3f + (float)azar.NextDouble() * 1.5f;
        Local(g, PrimitiveType.Cylinder, new Vector3(0f, alto * 0.5f, 0f), Quaternion.identity, new Vector3(0.4f, alto * 0.5f, 0.4f), madera);
        for (int i = 0; i < 4; i++)
        {
            float altura = alto * (0.55f + 0.12f * i);
            float giro = i * 97f;
            var rot = Quaternion.Euler(0f, giro, 45f + 10f * (i % 2));
            var dir = rot * Vector3.up;
            Local(g, PrimitiveType.Cylinder, new Vector3(0f, altura, 0f) + dir * 0.6f, rot, new Vector3(0.14f, 0.7f, 0.14f), madera);
        }
    }

    static void Local(GameObject grupo, PrimitiveType tipo, Vector3 posicion, Quaternion rotacion, Vector3 escala, Material material)
    {
        var go = GameObject.CreatePrimitive(tipo);
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(grupo.transform, false);
        go.transform.localPosition = posicion;
        go.transform.localRotation = rotacion;
        go.transform.localScale = escala;
        go.GetComponent<MeshRenderer>().sharedMaterial = material;
    }

    // --- La noche en las escenas -------------------------------------------------------

    // Deja la noche de la pradera guardada en las tres escenas de juego (el cielo, la luna, la
    // luz ambiente, la niebla y el piso), la luz de relleno de los personajes y el decorado: en
    // WaveMode como capitulo 1 de CapitulosDeEscenario, con la noche del cementerio y de la
    // ciudad, y en el modo libre y el tutorial, puesto en la escena. La niebla queda guardada en
    // la escena y no solo en el codigo: el stripping de shaders mira la de las escenas del build.
    [MenuItem("ShowBies/Escenarios/Poner la noche")]
    public static void PonerLaNoche()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("ConstructorEscenarios: en play no");
            return;
        }
        NombrarLaCapa();
        var pradera = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabPradera);
        var piso = AssetDatabase.LoadAssetAtPath<Material>(RutaPisoPradera);
        if (pradera == null || piso == null)
        {
            Debug.LogError("ConstructorEscenarios: falta la pradera; correr Armar pradera antes");
            return;
        }

        foreach (var ruta in EscenasDeJuego)
        {
            var escena = EditorSceneManager.OpenScene(ruta, OpenSceneMode.Single);
            Camera camara = null;
            Light luna = null;
            MeshRenderer pisoDeLaEscena = null;
            GameObject relleno = null, decorado = null;
            foreach (var raiz in escena.GetRootGameObjects())
            {
                if (raiz.name == "Relleno") relleno = raiz;
                if (PrefabUtility.GetCorrespondingObjectFromSource(raiz) == pradera) decorado = raiz;
                foreach (var c in raiz.GetComponentsInChildren<Camera>(true))
                    if (c.CompareTag("MainCamera")) camara = c;
                foreach (var l in raiz.GetComponentsInChildren<Light>(true))
                    if (l.type == LightType.Directional && raiz.name != "Relleno" && luna == null) luna = l;
                foreach (var r in raiz.GetComponentsInChildren<MeshRenderer>(true))
                {
                    var m = r.sharedMaterial;
                    if (m != null && (m.name == "prototype_512x512_green2" || m == piso)) pisoDeLaEscena = r;
                }
            }
            var capitulos = Object.FindFirstObjectByType<CapitulosDeEscenario>(FindObjectsInactive.Include);
            if (capitulos != null)
            {
                if (capitulos.sol != null) luna = capitulos.sol;
                if (capitulos.piso != null) pisoDeLaEscena = capitulos.piso as MeshRenderer;
                if (capitulos.camara != null) camara = capitulos.camara;
            }
            if (camara == null || luna == null || pisoDeLaEscena == null)
            {
                Debug.LogError("ConstructorEscenarios: " + ruta + " no tiene camara, luna o piso");
                continue;
            }

            camara.backgroundColor = CieloPradera;
            camara.clearFlags = CameraClearFlags.SolidColor;
            EditorUtility.SetDirty(camara);
            luna.color = LunaPradera;
            luna.intensity = IntensidadLunaPradera;
            EditorUtility.SetDirty(luna);
            pisoDeLaEscena.sharedMaterial = piso;
            EditorUtility.SetDirty(pisoDeLaEscena);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = AmbientePradera;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = CieloPradera;
            RenderSettings.fogStartDistance = NieblaInicioPradera;
            RenderSettings.fogEndDistance = NieblaFinPradera;

            // La luz de relleno: solo los personajes, de frente como la camara, sin sombras y
            // por vertice (la de pixel es de la luna).
            if (relleno == null) relleno = new GameObject("Relleno");
            var luz = relleno.GetComponent<Light>();
            if (luz == null) luz = relleno.AddComponent<Light>();
            luz.type = LightType.Directional;
            relleno.transform.SetPositionAndRotation(Vector3.zero, camara.transform.rotation);
            luz.color = ColorRelleno;
            luz.intensity = IntensidadRelleno;
            luz.shadows = LightShadows.None;
            luz.renderMode = LightRenderMode.ForceVertex;
            luz.cullingMask = 1 << Personajes.Capa;
            EditorUtility.SetDirty(luz);

            if (capitulos != null)
            {
                NocheDeLosCapitulos(capitulos, pradera);
            }
            else
            {
                if (decorado == null)
                {
                    decorado = (GameObject)PrefabUtility.InstantiatePrefab(pradera, escena);
                    decorado.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                }
                if (decorado.GetComponent<DecoradoFijo>() == null) decorado.AddComponent<DecoradoFijo>();
            }

            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
        }
        EditorSceneManager.OpenScene("Assets/Escenas/Menu.unity", OpenSceneMode.Single);
        Debug.Log("ConstructorEscenarios: la noche quedo en las escenas de juego");
    }

    // El capitulo 1 lleva la pradera (sus colores los lee de la escena), y el cementerio y la
    // ciudad, su noche con el neon de cada uno.
    static void NocheDeLosCapitulos(CapitulosDeEscenario capitulos, GameObject pradera)
    {
        if (capitulos.escenarios == null || capitulos.escenarios.Length < 3)
        {
            Debug.LogError("ConstructorEscenarios: CapitulosDeEscenario no tiene los tres escenarios");
            return;
        }
        capitulos.escenarios[0].decorado = pradera;
        var cementerio = capitulos.escenarios[1];
        cementerio.cielo = new Color(0.05f, 0.03f, 0.09f);
        cementerio.luz = new Color(0.62f, 0.8f, 0.72f);
        cementerio.intensidadLuz = 0.45f;
        cementerio.ambiente = new Color(0.14f, 0.14f, 0.22f);
        cementerio.conNiebla = true;
        cementerio.nieblaInicio = 16f;
        cementerio.nieblaFin = 46f;
        var ciudad = capitulos.escenarios[2];
        ciudad.cielo = new Color(0.09f, 0.02f, 0.08f);
        ciudad.luz = new Color(0.72f, 0.6f, 1f);
        ciudad.intensidadLuz = 0.45f;
        ciudad.ambiente = new Color(0.18f, 0.13f, 0.26f);
        ciudad.conNiebla = true;
        ciudad.nieblaInicio = 18f;
        ciudad.nieblaFin = 50f;
        EditorUtility.SetDirty(capitulos);
    }

    // La capa de los personajes con su nombre en el TagManager (Personajes.Capa).
    static void NombrarLaCapa()
    {
        var manager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var capas = manager.FindProperty("layers");
        var capa = capas.GetArrayElementAtIndex(Personajes.Capa);
        if (capa.stringValue == "Personajes") return;
        if (!string.IsNullOrEmpty(capa.stringValue))
        {
            Debug.LogError("ConstructorEscenarios: la capa " + Personajes.Capa + " ya se llama " + capa.stringValue);
            return;
        }
        capa.stringValue = "Personajes";
        manager.ApplyModifiedPropertiesWithoutUndo();
    }
}
