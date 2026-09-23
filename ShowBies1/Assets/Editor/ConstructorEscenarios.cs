using System.IO;
using UnityEditor;
using UnityEngine;

// Arma los decorados de los capitulos de las oleadas (ver CapitulosDeEscenario) con
// formas simples, sin modelos: el **cementerio** (lapidas, cruces, arboles pelados, la
// reja del borde y faroles) y la **ciudad de noche** (manzanas con vereda, edificios en
// el borde, autos, faroles, contenedores y semaforos). Todo sin colliders: los zombis van
// derecho al jugador y se trabarian con cualquier obstaculo, y los edificios van lejos del
// centro, que la camara mira desde arriba y taparian la partida.
//
// Cada uno se guarda como prefab con sus materiales en Assets/Escenarios/<nombre>; volver
// a correrlo lo rehace igual, porque la semilla es fija.
public static class ConstructorEscenarios
{
    const string Carpeta = "Assets/Escenarios/Cementerio";
    const string RutaPrefab = "Assets/Prefabs/Escenarios/Cementerio.prefab";
    const string CarpetaCiudad = "Assets/Escenarios/Ciudad";
    // Cuantos faroles de la ciudad llevan luz de verdad; el resto, el vidrio brillante y
    // el charco en el piso.
    const int MaxLuces = 6;
    const string RutaPrefabCiudad = "Assets/Prefabs/Escenarios/Ciudad.prefab";

    // Los faroles: su luz y el charco que pintan en el piso (ver Charco).
    static readonly Color ColorFarolCementerio = new Color(1f, 0.72f, 0.4f);
    const float AlturaLuzCementerio = 3.1f, AlcanceLuzCementerio = 11f;
    static readonly Color ColorFarolCiudad = new Color(1f, 0.72f, 0.42f);
    const float AlturaLuzCiudad = 3.5f, AlcanceLuzCiudad = 18f;
    // El radio del charco, en alcances de la luz: mas afuera casi no alumbra, y cada
    // charco es un cuadrado que se pinta encima del piso.
    public const float CharcoPorAlcance = 0.45f;
    // Cuanto alumbra cada charco. Medido contra la luz por pixel de antes, en el editor
    // (ShowBies > Escenarios > Fotos de los faroles): lo que sube el brillo del piso a metro
    // y medio del pie del farol.
    const float IntensidadCharcoCementerio = 0.6f, IntensidadCharcoCiudad = 0.8f;
    // A que altura del piso va el charco. En la ciudad, por encima del cordon (0,18 m): si
    // no, la vereda lo tapa y la luz se corta en la esquina.
    const float AlturaCharcoCementerio = 0.03f, AlturaCharcoCiudad = 0.2f;

    [MenuItem("ShowBies/Escenarios/Armar cementerio")]
    public static void ArmarCementerio()
    {
        Directory.CreateDirectory(Carpeta);
        Directory.CreateDirectory(Path.GetDirectoryName(RutaPrefab));

        var piedra = Material("Lapida", new Color(0.52f, 0.54f, 0.58f), 0.1f);
        var madera = Material("Madera", new Color(0.2f, 0.15f, 0.12f), 0.05f);
        var hierro = Material("Hierro", new Color(0.12f, 0.12f, 0.14f), 0.3f);
        var farol = Material("Farol", new Color(1f, 0.85f, 0.55f), 0.2f);
        farol.EnableKeyword("_EMISSION");
        farol.SetColor("_EmissionColor", new Color(1f, 0.7f, 0.35f) * 2.2f);
        farol.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        EditorUtility.SetDirty(farol);
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
        for (int i = 0; i < 40; i++)
        {
            float angulo = Azar(0f, Mathf.PI * 2f), radio = Azar(4.5f, 45f);
            var pos = new Vector3(Mathf.Cos(angulo) * radio, 0f, Mathf.Sin(angulo) * radio);
            var giro = Quaternion.Euler(Azar(-8f, 8f), Azar(0f, 360f), Azar(-8f, 8f));
            if (azar.NextDouble() < 0.3) Cruz(tumbas, pos, giro, piedra);
            else Lapida(tumbas, pos, giro, piedra, Azar(0.7f, 1.1f));
        }

        // Arboles pelados, sueltos, fuera del centro.
        var arboles = Grupo(raiz, "Arboles");
        for (int i = 0; i < 12; i++)
        {
            float angulo = Azar(0f, Mathf.PI * 2f), radio = Azar(9f, 44f);
            Arbol(arboles, new Vector3(Mathf.Cos(angulo) * radio, 0f, Mathf.Sin(angulo) * radio), Azar(0f, 360f), madera, azar);
        }

        // La reja del borde.
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
                Pieza(reja, PrimitiveType.Cube, pos, Quaternion.identity, escala, hierro);
            }
        }

        // Cuatro faroles con luz calida, en diagonal alrededor del centro.
        var faroles = Grupo(raiz, "Faroles");
        var charco = MaterialDeCharco(Carpeta, ColorFarolCementerio, IntensidadCharcoCementerio, AlturaLuzCementerio, AlcanceLuzCementerio);
        foreach (var p in new[] { new Vector2(-9, -7), new Vector2(9, -7), new Vector2(-9, 7), new Vector2(9, 7) })
        {
            var f = Grupo(faroles, "Farol");
            f.transform.position = new Vector3(p.x, 0f, p.y);
            Pieza(f, PrimitiveType.Cube, f.transform.position + Vector3.up * 1.6f, Quaternion.identity, new Vector3(0.18f, 3.2f, 0.18f), hierro);
            Pieza(f, PrimitiveType.Sphere, f.transform.position + Vector3.up * 3.3f, Quaternion.identity, Vector3.one * 0.55f, farol);
            Charco(f, new Vector3(0f, AlturaCharcoCementerio, 0f), AlcanceLuzCementerio, charco);
            var luzGo = new GameObject("Luz");
            luzGo.transform.SetParent(f.transform, false);
            luzGo.transform.localPosition = Vector3.up * AlturaLuzCementerio;
            Luz(luzGo, ColorFarolCementerio, AlcanceLuzCementerio, 1.6f);
        }

        PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
        Object.DestroyImmediate(raiz);
        AssetDatabase.SaveAssets();
        Debug.Log("ConstructorEscenarios: cementerio armado en " + RutaPrefab);
    }

    // --- La ciudad de noche: el capitulo 3 (oleadas 21-30) ----------------------------

    [MenuItem("ShowBies/Escenarios/Armar ciudad")]
    public static void ArmarCiudad()
    {
        Directory.CreateDirectory(CarpetaCiudad);
        Directory.CreateDirectory(Path.GetDirectoryName(RutaPrefabCiudad));

        var vereda = MaterialEn(CarpetaCiudad, "Vereda", new Color(0.42f, 0.42f, 0.45f), 0.1f);
        var cordon = MaterialEn(CarpetaCiudad, "Cordon", new Color(0.62f, 0.62f, 0.64f), 0.1f);
        var linea = MaterialEn(CarpetaCiudad, "LineaBlanca", new Color(0.85f, 0.83f, 0.7f), 0.05f);
        var pared = MaterialEn(CarpetaCiudad, "Pared", new Color(0.26f, 0.26f, 0.32f), 0.05f);
        var ventana = MaterialEn(CarpetaCiudad, "Ventana", new Color(1f, 0.82f, 0.45f), 0.2f);
        Emitir(ventana, new Color(1f, 0.72f, 0.3f) * 1.6f);
        var poste = MaterialEn(CarpetaCiudad, "Poste", new Color(0.14f, 0.14f, 0.16f), 0.3f);
        var luzFarol = MaterialEn(CarpetaCiudad, "LuzFarol", new Color(1f, 0.88f, 0.6f), 0.2f);
        Emitir(luzFarol, new Color(1f, 0.66f, 0.28f) * 2.4f);
        var charco = MaterialDeCharco(CarpetaCiudad, ColorFarolCiudad, IntensidadCharcoCiudad, AlturaLuzCiudad, AlcanceLuzCiudad);
        var contenedor = MaterialEn(CarpetaCiudad, "Contenedor", new Color(0.16f, 0.35f, 0.2f), 0.15f);
        var rojo = MaterialEn(CarpetaCiudad, "AutoRojo", new Color(0.5f, 0.12f, 0.12f), 0.35f);
        var azul = MaterialEn(CarpetaCiudad, "AutoAzul", new Color(0.13f, 0.24f, 0.45f), 0.35f);
        var blanco = MaterialEn(CarpetaCiudad, "AutoBlanco", new Color(0.62f, 0.62f, 0.6f), 0.35f);
        var vidrio = MaterialEn(CarpetaCiudad, "Vidrio", new Color(0.1f, 0.14f, 0.18f), 0.6f);
        var rueda = MaterialEn(CarpetaCiudad, "Rueda", new Color(0.07f, 0.07f, 0.08f), 0.1f);
        var autos = new[] { rojo, azul, blanco };
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
        int luces = 0;

        for (int ix = -2; ix <= 1; ix++)
        {
            for (int iz = -2; iz <= 1; iz++)
            {
                var centro = new Vector3(ix * Paso + Paso * 0.5f, 0f, iz * Paso + Paso * 0.5f);
                bool esElCentro = centro.magnitude < Paso;   // las cuatro esquinas del cruce
                Vereda(manzanas, centro, Manzana, vereda, cordon);

                // Los edificios, solo lejos: desde arriba, uno cerca taparia la partida.
                if (centro.magnitude > 30f)
                {
                    Edificio(manzanas, centro, Manzana, Azar(3.5f, 6.5f), pared, ventana, azar);
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
                    Farol(faroles, donde, poste, luzFarol, charco, conLuz);
                    if (conLuz) luces++;
                }
            }
        }

        // Las lineas blancas del medio de cada calle.
        var lineas = Grupo(raiz, "Lineas");
        for (int i = -2; i <= 2; i++)
        {
            float fijo = i * Paso;
            if (Mathf.Abs(fijo) > 50f) continue;
            for (float t2 = -48f; t2 <= 48f; t2 += 4f)
            {
                Pieza(lineas, PrimitiveType.Cube, new Vector3(t2, 0.02f, fijo), Quaternion.identity, new Vector3(2f, 0.04f, 0.22f), linea);
                Pieza(lineas, PrimitiveType.Cube, new Vector3(fijo, 0.02f, t2), Quaternion.identity, new Vector3(0.22f, 0.04f, 2f), linea);
            }
        }

        // Autos estacionados contra el cordon.
        var flota = Grupo(raiz, "Autos");
        for (int i = 0; i < 22; i++)
        {
            bool enX = azar.NextDouble() < 0.5;
            float calle = azar.Next(-2, 3) * Paso;
            float largo = Azar(-44f, 44f);
            var donde = enX ? new Vector3(largo, 0f, calle + (azar.NextDouble() < 0.5 ? -3.2f : 3.2f))
                            : new Vector3(calle + (azar.NextDouble() < 0.5 ? -3.2f : 3.2f), 0f, largo);
            if (donde.magnitude < 12f) continue;   // no encima del jugador
            Auto(flota, donde, enX ? 90f : 0f, autos[azar.Next(autos.Length)], vidrio, rueda);
        }

        PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefabCiudad);
        Object.DestroyImmediate(raiz);
        AssetDatabase.SaveAssets();
        Debug.Log("ConstructorEscenarios: ciudad armada en " + RutaPrefabCiudad + " con " + luces + " faroles con luz");
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

    // Un edificio bajo con sus ventanas encendidas.
    static void Edificio(GameObject padre, Vector3 centro, float lado, float alto, Material pared, Material ventana, System.Random azar)
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
                Local(g, PrimitiveType.Cube, pos, Quaternion.identity, escala, ventana);
            }
        }
    }

    static void Farol(GameObject padre, Vector3 pos, Material poste, Material luzMat, Material charco, bool conLuz)
    {
        var g = Grupo(padre, "Farol");
        g.transform.position = pos;
        Local(g, PrimitiveType.Cube, new Vector3(0f, 2f, 0f), Quaternion.identity, new Vector3(0.16f, 4f, 0.16f), poste);
        Local(g, PrimitiveType.Cube, new Vector3(0.5f, 3.95f, 0f), Quaternion.identity, new Vector3(1.1f, 0.14f, 0.14f), poste);
        Local(g, PrimitiveType.Cube, new Vector3(1f, 3.82f, 0f), Quaternion.identity, new Vector3(0.6f, 0.2f, 0.4f), luzMat);
        // El charco va en todos, con luz o sin ella: es lo que se ve alumbrado en el piso, y
        // no cuesta como una luz.
        Charco(g, new Vector3(1f, AlturaCharcoCiudad, 0f), AlcanceLuzCiudad, charco);
        if (!conLuz) return;

        var luzGo = new GameObject("Luz");
        luzGo.transform.SetParent(g.transform, false);
        luzGo.transform.localPosition = new Vector3(1f, AlturaLuzCiudad, 0f);
        Luz(luzGo, ColorFarolCiudad, AlcanceLuzCiudad, 3.2f);
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
    // ShowBies/CharcoDeLuz, que se apaga como la luz puntual que cuelga encima. Es lo que
    // hace que el farol alumbre el piso en el telefono, y cuesta un cuadrado aditivo sin
    // textura. Va adentro del grupo del farol, asi sale del piso con el.
    static void Charco(GameObject farol, Vector3 bajoLaLuz, float alcance, Material material)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = "Charco";
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(farol.transform, false);
        go.transform.localPosition = bajoLaLuz;
        go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        float diametro = alcance * CharcoPorAlcance * 2f;
        go.transform.localScale = new Vector3(diametro, diametro, 1f);
        var render = go.GetComponent<MeshRenderer>();
        render.sharedMaterial = material;
        render.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        render.receiveShadows = false;
        render.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        render.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
    }

    // Uno por escenario, con la luz de sus faroles: la altura y la caida se miden en radios
    // del charco, que es la unidad del shader.
    static Material MaterialDeCharco(string carpeta, Color color, float intensidad, float alturaLuz, float alcance)
    {
        var shader = Shader.Find("ShowBies/CharcoDeLuz");
        string ruta = carpeta + "/CharcoDeLuz.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, ruta);
        }
        mat.shader = shader;
        float radio = alcance * CharcoPorAlcance;
        mat.SetColor("_Color", color);
        mat.SetFloat("_Intensidad", intensidad);
        mat.SetFloat("_Altura", alturaLuz / radio);
        // La caida de las luces puntuales de Unity, 1 / (1 + 25 (d / alcance)^2), con la
        // distancia en radios del charco.
        mat.SetFloat("_Caida", 25f * CharcoPorAlcance * CharcoPorAlcance);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static void Auto(GameObject padre, Vector3 pos, float rumbo, Material color, Material vidrio, Material rueda)
    {
        var g = Grupo(padre, "Auto");
        g.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, rumbo, 0f));
        Local(g, PrimitiveType.Cube, new Vector3(0f, 0.55f, 0f), Quaternion.identity, new Vector3(1.8f, 0.7f, 4.2f), color);
        Local(g, PrimitiveType.Cube, new Vector3(0f, 1.1f, -0.2f), Quaternion.identity, new Vector3(1.6f, 0.6f, 2.2f), vidrio);
        foreach (var r in new[] { new Vector2(-0.95f, 1.3f), new Vector2(0.95f, 1.3f), new Vector2(-0.95f, -1.3f), new Vector2(0.95f, -1.3f) })
            Local(g, PrimitiveType.Cylinder, new Vector3(r.x, 0.33f, r.y), Quaternion.Euler(0f, 0f, 90f), new Vector3(0.66f, 0.12f, 0.66f), rueda);
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
        mat.color = new Color(0.46f, 0.46f, 0.5f);
        mat.SetFloat("_Glossiness", 0.12f);
        mat.SetFloat("_Metallic", 0f);
        EditorUtility.SetDirty(mat);
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
        mat.color = new Color(0.5f, 0.5f, 0.55f);
        mat.SetFloat("_Glossiness", 0.05f);
        mat.SetFloat("_Metallic", 0f);
        EditorUtility.SetDirty(mat);
    }

    static Material Material(string nombre, Color color, float brillo)
    {
        string ruta = Carpeta + "/" + nombre + ".mat";
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
}
