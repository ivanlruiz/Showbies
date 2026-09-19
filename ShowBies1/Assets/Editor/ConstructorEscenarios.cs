using System.IO;
using UnityEditor;
using UnityEngine;

// Arma el decorado del cementerio (el capitulo de noche de las oleadas, ver
// CapitulosDeEscenario) con formas simples, sin modelos: lapidas, cruces, arboles pelados,
// la reja del borde y cuatro faroles con luz calida. Todo sin colliders: los zombis van
// derecho al jugador y se trabarian con cualquier obstaculo. Se guarda como prefab y sus
// materiales en Assets/Escenarios/Cementerio; volver a correrlo lo rehace igual (la
// semilla es fija).
public static class ConstructorEscenarios
{
    const string Carpeta = "Assets/Escenarios/Cementerio";
    const string RutaPrefab = "Assets/Prefabs/Escenarios/Cementerio.prefab";

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
        foreach (var p in new[] { new Vector2(-9, -7), new Vector2(9, -7), new Vector2(-9, 7), new Vector2(9, 7) })
        {
            var f = Grupo(faroles, "Farol");
            f.transform.position = new Vector3(p.x, 0f, p.y);
            Pieza(f, PrimitiveType.Cube, f.transform.position + Vector3.up * 1.6f, Quaternion.identity, new Vector3(0.18f, 3.2f, 0.18f), hierro);
            Pieza(f, PrimitiveType.Sphere, f.transform.position + Vector3.up * 3.3f, Quaternion.identity, Vector3.one * 0.55f, farol);
            var luzGo = new GameObject("Luz");
            luzGo.transform.SetParent(f.transform, false);
            luzGo.transform.localPosition = Vector3.up * 3.1f;
            var luz = luzGo.AddComponent<Light>();
            luz.type = LightType.Point;
            luz.color = new Color(1f, 0.72f, 0.4f);
            luz.range = 11f;
            luz.intensity = 1.6f;
            luz.shadows = LightShadows.None;
        }

        PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
        Object.DestroyImmediate(raiz);
        AssetDatabase.SaveAssets();
        Debug.Log("ConstructorEscenarios: cementerio armado en " + RutaPrefab);
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
