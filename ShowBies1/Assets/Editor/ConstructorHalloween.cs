using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Arma lo que se ve del evento de Halloween (EventoHalloween) con formas simples, como los
// decorados de noche (ConstructorEscenarios): las calabazas del decorado (la grande es un
// farol, con la cara que brilla, su halo y su charco), los disfraces de los zombis (la cabeza
// de calabaza y el sombrero de bruja), el sombrero de calabaza del jugador y el caramelo, que
// es una copia de la moneda con otro modelo. Todo va a Assets/Halloween/Resources/Halloween,
// porque lo pone el juego por codigo (InstaladorHalloween, Disfraces) sin cablear nada.
//
// Los disfraces se miden sobre el modelo: los vertices que mueve el hueso de la cabeza dan su
// caja, y la raiz de cada prefab queda con la posicion, el giro y la escala que lleva respecto
// de ese hueso. Los cinco zombis usan el mismo modelo con otra escala, asi que una medida
// sirve para todos. Se vuelve a correr para cambiar algo: no se editan a mano.
public static class ConstructorHalloween
{
    const string Carpeta = "Assets/Halloween";
    const string CarpetaMateriales = Carpeta + "/Materiales";
    const string CarpetaPrefabs = Carpeta + "/Resources/Halloween";
    const string ModeloZombi = "Assets/ToonyTinyPeople/TT_demo/models/TT_demo_zombie.FBX";
    const string ModeloJugador = "Assets/ToonyTinyPeople/TT_demo/models/TT_demo_male_A.FBX";
    const string RutaMoneda = "Assets/Prefabs/Moneda.prefab";

    static readonly Color Naranja = new Color(1f, 0.45f, 0.05f);
    static readonly Color NaranjaClaro = new Color(1f, 0.62f, 0.15f);
    static readonly Color ColorCara = new Color(1f, 0.85f, 0.3f);
    static readonly Color VerdeTallo = new Color(0.22f, 0.33f, 0.08f);
    static readonly Color VioletaBruja = new Color(0.2f, 0.07f, 0.3f);
    static readonly Color Lima = new Color(0.6f, 1f, 0.2f);
    static readonly Color VioletaCaramelo = new Color(0.72f, 0.32f, 1f);

    // La cara y su halo, en el decorado: a cuanto del piso y que tan grande.
    const float AlturaCharco = 0.2f;   // por encima del cordon de la ciudad, como la sangre
    const float AlcanceCharco = 4f;

    class Materiales
    {
        public Material piel, pielChica, tallo, cara, halo, charco, bruja, cinta, hebilla, relleno, envoltorio, raya;
    }

    [MenuItem("ShowBies/Halloween/Armar calabazas, disfraces y caramelo")]
    public static void Armar()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("ConstructorHalloween: no se arma en play");
            return;
        }
        Directory.CreateDirectory(CarpetaMateriales);
        Directory.CreateDirectory(CarpetaPrefabs);
        AssetDatabase.Refresh();

        var m = ArmarMateriales();
        Guardar(Calabaza(m, true), "Calabaza");
        Guardar(Calabaza(m, false), "CalabazaChica");

        // La calabaza reemplaza a la cabeza (Disfraces la esconde): del ancho de la cabeza y
        // casi de su alto, un poco mas abajo para que apoye en el cuello.
        var cabezaDeCalabaza = CabezaDeCalabaza(m);
        Colocar(cabezaDeCalabaza, ModeloZombi, caja => new Pose3(caja.center - Vector3.up * caja.size.y * 0.05f, Mathf.Max(caja.size.x, caja.size.z) * 1.02f));
        Guardar(cabezaDeCalabaza, "CabezaDeCalabaza");

        var bruja = SombreroDeBruja(m);
        Colocar(bruja, ModeloZombi, caja => new Pose3(new Vector3(caja.center.x, caja.max.y - caja.size.y * 0.2f, caja.center.z), caja.size.x * 0.95f));
        Guardar(bruja, "SombreroDeBruja");

        var sombrero = SombreroDeCalabaza(m);
        // El ancho y no el largo: la cabeza del jugador trae la visera de la gorra.
        Colocar(sombrero, ModeloJugador, caja => new Pose3(new Vector3(caja.center.x, caja.max.y - caja.size.y * 0.06f, caja.center.z), caja.size.x * 1.1f));
        Guardar(sombrero, "SombreroDeCalabaza");

        ArmarCaramelo(m);
        AssetDatabase.SaveAssets();
        Debug.Log("ConstructorHalloween: listo en " + CarpetaPrefabs);
    }

    // --- Materiales ----------------------------------------------------------------------

    static Materiales ArmarMateriales()
    {
        var m = new Materiales();
        m.piel = ConstructorEscenarios.MaterialEn(CarpetaMateriales, "CalabazaPiel", Naranja, 0.35f);
        // Un poco de brillo propio: de noche, sin luz cerca, la calabaza no se apaga del todo.
        ConstructorEscenarios.Emitir(m.piel, Naranja * 0.22f);
        m.pielChica = ConstructorEscenarios.MaterialEn(CarpetaMateriales, "CalabazaPielChica", NaranjaClaro, 0.3f);
        ConstructorEscenarios.Emitir(m.pielChica, NaranjaClaro * 0.15f);
        m.tallo = ConstructorEscenarios.MaterialEn(CarpetaMateriales, "CalabazaTallo", VerdeTallo, 0.1f);
        m.cara = ConstructorEscenarios.MaterialPlano(CarpetaMateriales, "CalabazaCara", ColorCara);
        m.halo = ConstructorEscenarios.MaterialDeBrillo(CarpetaMateriales, "CalabazaHalo", new Color(1f, 0.5f, 0.05f), 1.2f, 0.25f, 4f);
        m.charco = ConstructorEscenarios.MaterialDeBrillo(CarpetaMateriales, "CalabazaCharco", new Color(1f, 0.45f, 0.05f), 1.1f, 0.35f, 5f);
        m.bruja = ConstructorEscenarios.MaterialEn(CarpetaMateriales, "BrujaTela", VioletaBruja, 0.2f);
        ConstructorEscenarios.Emitir(m.bruja, VioletaBruja * 0.35f);
        m.cinta = ConstructorEscenarios.MaterialPlano(CarpetaMateriales, "BrujaCinta", Lima);
        m.hebilla = ConstructorEscenarios.MaterialPlano(CarpetaMateriales, "BrujaHebilla", ColorCara);
        m.relleno = ConstructorEscenarios.MaterialPlano(CarpetaMateriales, "CarameloRelleno", new Color(1f, 0.55f, 0.1f));
        m.envoltorio = ConstructorEscenarios.MaterialPlano(CarpetaMateriales, "CarameloEnvoltorio", VioletaCaramelo);
        m.raya = ConstructorEscenarios.MaterialPlano(CarpetaMateriales, "CarameloRaya", new Color(1f, 0.95f, 0.85f));
        return m;
    }

    // --- Las piezas ------------------------------------------------------------------

    static GameObject Pieza(Transform padre, PrimitiveType tipo, Vector3 posicion, Vector3 giro, Vector3 escala, Material material, bool sombra = true)
    {
        var go = GameObject.CreatePrimitive(tipo);
        Object.DestroyImmediate(go.GetComponent<Collider>());
        // La esfera de Unity tiene 515 vertices: una calabaza son nueve, y en la partida hay
        // mas de cien calabazas juntadas en el mismo lote. La baja tiene 150.
        if (tipo == PrimitiveType.Sphere) go.GetComponent<MeshFilter>().sharedMesh = EsferaBaja();
        go.transform.SetParent(padre, false);
        go.transform.localPosition = posicion;
        go.transform.localRotation = Quaternion.Euler(giro);
        go.transform.localScale = escala;
        var r = go.GetComponent<MeshRenderer>();
        r.sharedMaterial = material;
        r.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        r.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        if (!sombra)
        {
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
        }
        return go;
    }

    static Transform Grupo(Transform padre, string nombre, Vector3 posicion, Vector3 giro)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        go.transform.localPosition = posicion;
        go.transform.localRotation = Quaternion.Euler(giro);
        return go.transform;
    }

    // Una calabaza de ocho gajos con la base en y = 0: elipsoides alargados hacia afuera,
    // separados un poco del centro para que se marquen las canaletas, y uno en el medio que
    // tapa los huecos de arriba.
    static void Gajos(Transform padre, float radio, float alto, Material piel, bool sombra = true)
    {
        const int cuantos = 8;
        for (int i = 0; i < cuantos; i++)
        {
            float angulo = 360f * i / cuantos;
            float rad = angulo * Mathf.Deg2Rad;
            Pieza(padre, PrimitiveType.Sphere, new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad)) * radio * 0.35f + Vector3.up * alto * 0.5f,
                  new Vector3(0f, angulo, 0f), new Vector3(radio * 0.85f, alto, radio * 1.3f), piel, sombra);
        }
        Pieza(padre, PrimitiveType.Sphere, Vector3.up * alto * 0.5f, Vector3.zero, new Vector3(radio * 1.5f, alto * 0.95f, radio * 1.5f), piel, sombra);
    }

    // El tallo torcido y una hoja.
    static void Tallo(Transform padre, float radio, float alto, Material tallo)
    {
        Pieza(padre, PrimitiveType.Cylinder, new Vector3(0.02f * radio, alto + radio * 0.1f, 0f), new Vector3(0f, 0f, -16f),
              new Vector3(radio * 0.16f, radio * 0.16f, radio * 0.16f), tallo);
        Pieza(padre, PrimitiveType.Sphere, new Vector3(radio * 0.3f, alto + radio * 0.02f, radio * 0.1f), new Vector3(0f, 30f, 12f),
              new Vector3(radio * 0.42f, radio * 0.04f, radio * 0.22f), tallo);
    }

    // La cara tallada: ojos y nariz de rombo y una sonrisa en dos tramos, de un color que
    // brilla sin luz. 'frente' es +1 si mira a +Z y -1 si mira a -Z; 'inclinacion' la levanta
    // hacia la camara de arriba. Va sobre la superficie del elipsoide de la calabaza (semiejes
    // radio y alto/2) donde la normal es la de la cara.
    static void Cara(Transform padre, float radio, float alto, float frente, float inclinacion, Material material)
    {
        float a = inclinacion * Mathf.Deg2Rad;
        float A = radio, B = alto * 0.5f;
        float largo = Mathf.Sqrt(A * A * Mathf.Cos(a) * Mathf.Cos(a) + B * B * Mathf.Sin(a) * Mathf.Sin(a));
        float z = A * A * Mathf.Cos(a) / largo;
        float y = B * B * Mathf.Sin(a) / largo;
        var cara = Grupo(padre, "Cara", new Vector3(0f, B + y, z * frente), new Vector3(-inclinacion, frente > 0f ? 0f : 180f, 0f));
        float e = radio;
        Pieza(cara, PrimitiveType.Cube, new Vector3(-0.27f, 0.1f, 0f) * e, new Vector3(0f, 0f, 45f), new Vector3(0.24f * e, 0.24f * e, 0.22f * e), material, false);
        Pieza(cara, PrimitiveType.Cube, new Vector3(0.27f, 0.1f, 0f) * e, new Vector3(0f, 0f, 45f), new Vector3(0.24f * e, 0.24f * e, 0.22f * e), material, false);
        Pieza(cara, PrimitiveType.Cube, new Vector3(0f, -0.06f, 0f) * e, new Vector3(0f, 0f, 45f), new Vector3(0.12f * e, 0.12f * e, 0.22f * e), material, false);
        Pieza(cara, PrimitiveType.Cube, new Vector3(-0.17f, -0.25f, 0f) * e, new Vector3(0f, 0f, -14f), new Vector3(0.38f * e, 0.11f * e, 0.22f * e), material, false);
        Pieza(cara, PrimitiveType.Cube, new Vector3(0.17f, -0.25f, 0f) * e, new Vector3(0f, 0f, 14f), new Vector3(0.38f * e, 0.11f * e, 0.22f * e), material, false);
    }

    // Un cuadrado aditivo con el shader del charco: de frente a la camara (el halo, que el juego
    // vuelve a girar al ponerlo) o acostado en el piso (el charco).
    static void Brillo(Transform padre, string nombre, Vector3 posicion, Vector3 giro, float lado, Material material)
    {
        var go = Pieza(padre, PrimitiveType.Quad, posicion, giro, new Vector3(lado, lado, 1f), material, false);
        go.name = nombre;
    }

    // Una esfera de diametro 1, como la de Unity, con 14 meridianos y 9 paralelos. Es un
    // asset, asi las prefabs la referencian.
    static Mesh esferaBaja;

    static Mesh EsferaBaja()
    {
        if (esferaBaja != null) return esferaBaja;
        string ruta = Carpeta + "/EsferaBaja.asset";
        var malla = AssetDatabase.LoadAssetAtPath<Mesh>(ruta);
        bool nueva = malla == null;
        if (nueva) malla = new Mesh();
        malla.Clear();
        malla.name = "EsferaBaja";
        const int meridianos = 14, paralelos = 9;
        var vertices = new List<Vector3>();
        var normales = new List<Vector3>();
        var uvs = new List<Vector2>();
        for (int j = 0; j <= paralelos; j++)
        {
            float v = j / (float)paralelos;
            float fi = v * Mathf.PI;
            for (int i = 0; i <= meridianos; i++)
            {
                float u = i / (float)meridianos;
                float tita = u * Mathf.PI * 2f;
                var n = new Vector3(Mathf.Sin(fi) * Mathf.Cos(tita), Mathf.Cos(fi), Mathf.Sin(fi) * Mathf.Sin(tita));
                vertices.Add(n * 0.5f);
                normales.Add(n);
                uvs.Add(new Vector2(u, 1f - v));
            }
        }
        var triangulos = new List<int>();
        for (int j = 0; j < paralelos; j++)
        {
            for (int i = 0; i < meridianos; i++)
            {
                int a = j * (meridianos + 1) + i, b = a + meridianos + 1;
                Triangulo(triangulos, vertices, a, b, a + 1);
                Triangulo(triangulos, vertices, a + 1, b, b + 1);
            }
        }
        malla.SetVertices(vertices);
        malla.SetNormals(normales);
        malla.SetUVs(0, uvs);
        malla.SetTriangles(triangulos, 0);
        malla.RecalculateBounds();
        malla.RecalculateTangents();
        if (nueva) AssetDatabase.CreateAsset(malla, ruta);
        else EditorUtility.SetDirty(malla);
        esferaBaja = malla;
        return malla;
    }

    // Un triangulo con la cara hacia afuera: en Unity el frente es el orden horario visto
    // desde afuera, y el producto cruz de ese orden apunta hacia quien mira. Los de los
    // polos, con dos vertices iguales, no van.
    static void Triangulo(List<int> lista, List<Vector3> v, int a, int b, int c)
    {
        Vector3 normal = Vector3.Cross(v[b] - v[a], v[c] - v[a]);
        if (normal.sqrMagnitude < 1e-10f) return;
        Vector3 centro = (v[a] + v[b] + v[c]) / 3f;
        if (Vector3.Dot(normal, centro) < 0f) { int t = b; b = c; c = t; }
        lista.Add(a); lista.Add(b); lista.Add(c);
    }

    // --- Las prefabs -----------------------------------------------------------------

    // La del decorado: la grande es un farol, con la cara hacia la camara (-Z), su halo y su
    // charco; la chica, la calabaza sola.
    static GameObject Calabaza(Materiales m, bool grande)
    {
        var raiz = new GameObject(grande ? "Calabaza" : "CalabazaChica");
        float radio = grande ? 0.45f : 0.28f;
        float alto = grande ? 0.55f : 0.32f;
        Gajos(raiz.transform, radio, alto, grande ? m.piel : m.pielChica);
        Tallo(raiz.transform, radio, alto, m.tallo);
        if (grande)
        {
            Cara(raiz.transform, radio, alto, -1f, 40f, m.cara);
            Brillo(raiz.transform, "Halo", new Vector3(0f, alto * 0.75f, -radio * 0.4f), new Vector3(70f, 0f, 0f), 1.6f, m.halo);
            Brillo(raiz.transform, "Charco", new Vector3(0f, AlturaCharco, 0f), new Vector3(90f, 0f, 0f),
                   AlcanceCharco * ConstructorEscenarios.CharcoPorAlcance * 2f, m.charco);
        }
        return raiz;
    }

    // La cabeza de calabaza de los zombis: centrada en el origen, de diametro 1 (la escala de la
    // raiz la lleva al tamanio de la cabeza), con la cara que brilla hacia adelante (+Z).
    static GameObject CabezaDeCalabaza(Materiales m)
    {
        var raiz = new GameObject("CabezaDeCalabaza");
        const float radio = 0.5f, alto = 0.85f;
        var cuerpo = Grupo(raiz.transform, "Calabaza", Vector3.down * alto * 0.5f, Vector3.zero);
        Gajos(cuerpo, radio, alto, m.piel);
        Tallo(cuerpo, radio, alto, m.tallo);
        Cara(cuerpo, radio, alto, 1f, 18f, m.cara);
        return raiz;
    }

    // El sombrero de bruja: el ala, la copa en cuatro cilindros que se achican y se tuercen
    // hacia atras, la cinta de neon lima y la hebilla. El origen es el medio del ala y 1 es
    // el ancho de la cabeza.
    static GameObject SombreroDeBruja(Materiales m)
    {
        var raiz = new GameObject("SombreroDeBruja");
        var t = raiz.transform;
        Pieza(t, PrimitiveType.Cylinder, Vector3.zero, Vector3.zero, new Vector3(1.55f, 0.02f, 1.55f), m.bruja);
        Pieza(t, PrimitiveType.Cylinder, new Vector3(0f, 0.16f, 0f), Vector3.zero, new Vector3(0.95f, 0.16f, 0.95f), m.bruja);
        Pieza(t, PrimitiveType.Cylinder, new Vector3(0f, 0.08f, 0f), Vector3.zero, new Vector3(0.98f, 0.05f, 0.98f), m.cinta);
        Pieza(t, PrimitiveType.Cube, new Vector3(0f, 0.08f, 0.49f), Vector3.zero, new Vector3(0.16f, 0.12f, 0.04f), m.hebilla, false);
        Pieza(t, PrimitiveType.Cylinder, new Vector3(0f, 0.42f, -0.03f), new Vector3(-6f, 0f, 0f), new Vector3(0.7f, 0.13f, 0.7f), m.bruja);
        Pieza(t, PrimitiveType.Cylinder, new Vector3(0f, 0.64f, -0.09f), new Vector3(-16f, 0f, 0f), new Vector3(0.42f, 0.12f, 0.42f), m.bruja);
        Pieza(t, PrimitiveType.Cylinder, new Vector3(0f, 0.82f, -0.2f), new Vector3(-32f, 0f, 0f), new Vector3(0.24f, 0.1f, 0.24f), m.bruja);
        Pieza(t, PrimitiveType.Cylinder, new Vector3(0f, 0.92f, -0.36f), new Vector3(-60f, 0f, 0f), new Vector3(0.11f, 0.09f, 0.11f), m.bruja);
        return raiz;
    }

    // El sombrero de calabaza del jugador: media calabaza de gajos encima de la cabeza (la otra
    // mitad queda adentro), con el tallo, la hoja y un zarcillo de neon. El origen es el centro
    // de la calabaza y 1 su diametro.
    static GameObject SombreroDeCalabaza(Materiales m)
    {
        var raiz = new GameObject("SombreroDeCalabaza");
        const float radio = 0.5f, alto = 0.6f;
        var cuerpo = Grupo(raiz.transform, "Calabaza", Vector3.down * alto * 0.5f, Vector3.zero);
        Gajos(cuerpo, radio, alto, m.piel);
        Tallo(cuerpo, radio * 1.3f, alto, m.tallo);
        // El zarcillo: una curva de bolitas lima que sale del tallo.
        for (int i = 0; i < 6; i++)
        {
            float a = i * 0.9f;
            var p = new Vector3(-0.08f - Mathf.Cos(a) * 0.08f * (1f + i * 0.15f), alto + 0.08f + i * 0.015f, Mathf.Sin(a) * 0.08f * (1f + i * 0.15f));
            Pieza(cuerpo, PrimitiveType.Sphere, p, Vector3.zero, Vector3.one * (0.06f - i * 0.006f), m.cinta, false);
        }
        return raiz;
    }

    // El caramelo: una copia de la moneda, con el centro naranja con rayas y las dos puntas
    // del envoltorio violeta. Gira como la moneda, alrededor del alto: el largo va en X.
    static void ArmarCaramelo(Materiales m)
    {
        var moneda = AssetDatabase.LoadAssetAtPath<GameObject>(RutaMoneda);
        if (moneda == null)
        {
            Debug.LogError("ConstructorHalloween: falta " + RutaMoneda);
            return;
        }
        var copia = (GameObject)PrefabUtility.InstantiatePrefab(moneda);
        PrefabUtility.UnpackPrefabInstance(copia, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        copia.name = "Caramelo";
        var viejo = copia.transform.Find("Modelo");
        if (viejo != null) Object.DestroyImmediate(viejo.gameObject);

        var modelo = Grupo(copia.transform, "Modelo", Vector3.zero, Vector3.zero);
        Pieza(modelo, PrimitiveType.Sphere, Vector3.zero, Vector3.zero, new Vector3(0.36f, 0.25f, 0.25f), m.relleno, false);
        for (int i = -1; i <= 1; i++)
            Pieza(modelo, PrimitiveType.Cylinder, new Vector3(i * 0.09f, 0f, 0f), new Vector3(0f, 0f, 90f + 25f), new Vector3(0.262f, 0.012f, 0.262f), m.raya, false);
        foreach (float lado in new[] { -1f, 1f })
        {
            Pieza(modelo, PrimitiveType.Sphere, new Vector3(lado * 0.17f, 0f, 0f), Vector3.zero, new Vector3(0.07f, 0.09f, 0.09f), m.envoltorio, false);
            Pieza(modelo, PrimitiveType.Cube, new Vector3(lado * 0.23f, 0f, 0f), new Vector3(45f, 0f, 0f), new Vector3(0.08f, 0.17f, 0.17f), m.envoltorio, false);
        }

        var componente = copia.GetComponent<Moneda>();
        componente.caramelo = true;
        // La quinta de la bemol: suena distinto de la escalera de las monedas, en la misma escala.
        componente.afinacion += 7f;
        Guardar(copia, "Caramelo");
    }

    static void Guardar(GameObject raiz, string nombre)
    {
        PrefabUtility.SaveAsPrefabAsset(raiz, CarpetaPrefabs + "/" + nombre + ".prefab");
        Object.DestroyImmediate(raiz);
    }

    // --- La medida de la cabeza ------------------------------------------------------

    struct Pose3
    {
        public Vector3 centro;   // en el marco del modelo, desde el hueso de la cabeza
        public float escala;
        public Pose3(Vector3 centro, float escala) { this.centro = centro; this.escala = escala; }
    }

    // Pone en la raiz del disfraz la posicion, el giro y la escala que lleva respecto del hueso
    // de la cabeza del modelo: 'ubicar' recibe la caja de la cabeza (en metros, en los ejes de la
    // raiz del modelo y desde el hueso) y devuelve donde va el origen del disfraz y su escala.
    static void Colocar(GameObject disfraz, string rutaModelo, System.Func<Bounds, Pose3> ubicar)
    {
        Bounds caja;
        Transform hueso;
        var modelo = MedirCabeza(rutaModelo, out caja, out hueso);
        if (modelo == null) return;
        var pose = ubicar(caja);
        Quaternion giro = modelo.transform.rotation;
        Vector3 enElMundo = hueso.position + giro * pose.centro;
        disfraz.transform.localPosition = hueso.InverseTransformPoint(enElMundo);
        disfraz.transform.localRotation = Quaternion.Inverse(hueso.rotation) * giro;
        disfraz.transform.localScale = Vector3.one * (pose.escala / Mathf.Max(1e-6f, hueso.lossyScale.x));
        Debug.Log("ConstructorHalloween: " + disfraz.name + " sobre " + Path.GetFileName(rutaModelo) + ": cabeza " + caja.size.ToString("F3") +
                  ", escala del hueso " + hueso.lossyScale.x.ToString("F4"));
        Object.DestroyImmediate(modelo);
    }

    // Instancia el modelo y junta los vertices que mueve sobre todo el hueso de la cabeza (o
    // alguno de sus hijos), en el marco de arriba. Devuelve el modelo instanciado, a destruir.
    public static GameObject MedirCabeza(string rutaModelo, out Bounds caja, out Transform hueso)
    {
        caja = new Bounds();
        hueso = null;
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(rutaModelo);
        if (asset == null)
        {
            Debug.LogError("ConstructorHalloween: falta " + rutaModelo);
            return null;
        }
        var modelo = (GameObject)PrefabUtility.InstantiatePrefab(asset);
        modelo.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        hueso = Disfraces.Cabeza(modelo.transform);
        if (hueso == null)
        {
            Debug.LogError("ConstructorHalloween: " + rutaModelo + " no tiene hueso de la cabeza");
            Object.DestroyImmediate(modelo);
            return null;
        }

        Quaternion haciaElMarco = Quaternion.Inverse(modelo.transform.rotation);
        bool hay = false;
        foreach (var piel in modelo.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            var malla = piel.sharedMesh;
            if (malla == null) continue;
            var huesos = piel.bones;
            var pesos = malla.boneWeights;
            var horneada = new Mesh();
            piel.BakeMesh(horneada, true);
            var vertices = horneada.vertices;
            for (int i = 0; i < vertices.Length && i < pesos.Length; i++)
            {
                int indice = Dominante(pesos[i]);
                if (indice < 0 || indice >= huesos.Length || huesos[indice] == null) continue;
                if (huesos[indice] != hueso && !huesos[indice].IsChildOf(hueso)) continue;
                Vector3 mundo = piel.transform.position + piel.transform.rotation * vertices[i];
                Vector3 p = haciaElMarco * (mundo - hueso.position);
                if (!hay) { caja = new Bounds(p, Vector3.zero); hay = true; }
                else caja.Encapsulate(p);
            }
            Object.DestroyImmediate(horneada);
        }
        // Las cabezas de ToonyTiny se cambian por otras: pueden ser mallas rigidas colgadas
        // del hueso, sin piel.
        foreach (var filtro in modelo.GetComponentsInChildren<MeshFilter>(true))
        {
            if (filtro.sharedMesh == null || !filtro.transform.IsChildOf(hueso)) continue;
            foreach (var v in filtro.sharedMesh.vertices)
            {
                Vector3 p = haciaElMarco * (filtro.transform.TransformPoint(v) - hueso.position);
                if (!hay) { caja = new Bounds(p, Vector3.zero); hay = true; }
                else caja.Encapsulate(p);
            }
        }
        if (!hay)
        {
            var nombres = new List<string>();
            foreach (var r in modelo.GetComponentsInChildren<Renderer>(true))
                nombres.Add(r.name + "(" + r.GetType().Name + ", padre " + r.transform.parent.name + ")");
            Debug.LogError("ConstructorHalloween: renderers de " + rutaModelo + ": " + string.Join(", ", nombres));
        }
        if (!hay)
        {
            Debug.LogError("ConstructorHalloween: ningun vertice de " + rutaModelo + " sigue a la cabeza");
            Object.DestroyImmediate(modelo);
            return null;
        }
        return modelo;
    }

    static int Dominante(BoneWeight p)
    {
        int indice = p.boneIndex0;
        float peso = p.weight0;
        if (p.weight1 > peso) { indice = p.boneIndex1; peso = p.weight1; }
        if (p.weight2 > peso) { indice = p.boneIndex2; peso = p.weight2; }
        if (p.weight3 > peso) { indice = p.boneIndex3; }
        return indice;
    }

    // --- El evento en el editor ------------------------------------------------------

    const string MenuForzar = "ShowBies/Halloween/Forzar en el editor";

    [MenuItem(MenuForzar)]
    static void Forzar()
    {
        bool prendido = !EditorPrefs.GetBool(EventoHalloween.ClaveForzarEnElEditor, false);
        EditorPrefs.SetBool(EventoHalloween.ClaveForzarEnElEditor, prendido);
        Debug.Log("Halloween en el editor: " + (prendido ? "prendido" : "apagado (solo en las fechas)"));
    }

    [MenuItem(MenuForzar, true)]
    static bool ForzarValidar()
    {
        Menu.SetChecked(MenuForzar, EditorPrefs.GetBool(EventoHalloween.ClaveForzarEnElEditor, false));
        return true;
    }
}
