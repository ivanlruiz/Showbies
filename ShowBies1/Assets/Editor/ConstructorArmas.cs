using System.IO;
using UnityEditor;
using UnityEngine;

// Arma las armas que el muñeco tiene en la mano (ver ArmaEnLaMano), con formas simples
// y sin modelos, como los decorados: un prefab con las cinco adentro, una por tramo del daño, y
// ArmaEnLaMano prende la que toca. Pedido de Ivan: el modelo del pack traia un bate en
// la mano, y con las poses de pistola lo levantaba delante de la cara; las balas salian de
// un cubito que flotaba a la altura del pecho, y corriendo y disparando no se leia que
// estuviera disparando.
//
// Los ejes son los de la mano del modelo en las poses de pistola: +Z es el cañon (hacia
// adelante), +Y es arriba y el origen es el puño, donde va la empuñadura. La "Boca" es el
// punto del que salen las balas y el fogonazo; cada arma marca el suyo con su "Punta", y
// ArmaEnLaMano lleva la Boca ahi.
//
// Se guarda como prefab con sus materiales; volver a correrlo la rehace igual.
public static class ConstructorArmas
{
    public const string RutaPistola = "Assets/Prefabs/Pistola.prefab";
    const string CarpetaMateriales = "Assets/Materiales";

    // La luz de la bala (BulletController.materialLuz, pedido de Ivan, 9/10): el material del
    // charco de los faroles, blanco (cada tramo lo tiñe con su color en runtime), puesto en el
    // prefab de la bala. La malla redonda de la onda del arma de energia (mallaRedonda: la
    // esfera baja de Halloween, de 150 vertices; la de Unity tiene 515, y con la cadencia alta
    // hay decenas en pantalla); las demas son el cubo del prefab. Y la base de los materiales de
    // cada tramo (materialPlano: ShowBies/ColorPlano, con instancing, 13/10). Se vuelve a correr
    // si se cambian.
    public const string RutaLuzDeLaBala = "Assets/Materiales/BalaLuz.mat";
    public const string RutaBalaPlana = "Assets/Materiales/BalaPlana.mat";
    public const string RutaEsferaBaja = "Assets/Halloween/EsferaBaja.asset";

    [MenuItem("ShowBies/Armas/Armar la luz de la bala")]
    public static void ArmarLuzDeLaBala()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogError("ConstructorArmas: no en play"); return; }
        var material = ConstructorEscenarios.MaterialDeBrillo("Assets/Materiales", "BalaLuz", Color.white, 1.1f, 0.35f, 4.5f);
        const string rutaBala = "Assets/Prefabs/Bullet.prefab";
        var contenido = PrefabUtility.LoadPrefabContents(rutaBala);
        try
        {
            var bala = contenido.GetComponent<BulletController>();
            if (bala == null) { Debug.LogError("ConstructorArmas: el prefab de la bala no tiene BulletController"); return; }
            bala.materialLuz = material;
            var esfera = AssetDatabase.LoadAssetAtPath<Mesh>(RutaEsferaBaja);
            if (esfera == null) { Debug.LogError("ConstructorArmas: falta " + RutaEsferaBaja + " (la arma ShowBies > Halloween)"); return; }
            bala.mallaRedonda = esfera;
            bala.materialPlano = MaterialPlanoDeLaBala();
            PrefabUtility.SaveAsPrefabAsset(contenido, rutaBala);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contenido);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("ConstructorArmas: la bala tiene su luz (" + RutaLuzDeLaBala + ") y su material plano (" + RutaBalaPlana + ")");
    }

    // Blanco: cada tramo lo tiñe con su color en runtime (BulletController.ArmarMateriales).
    static Material MaterialPlanoDeLaBala()
    {
        var shader = Shader.Find("ShowBies/ColorPlano");
        var mat = AssetDatabase.LoadAssetAtPath<Material>(RutaBalaPlana);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, RutaBalaPlana);
        }
        mat.shader = shader;
        mat.color = Color.white;
        mat.enableInstancing = true;
        EditorUtility.SetDirty(mat);
        return mat;
    }

    // Las cinco armas, una por tramo del daño (18/10, pedido de Ivan: "que las armas tengan diseño,
    // que no sean todas iguales"; las diseño Argon, pieza por pieza, en el chat de diseño). Hasta ahi
    // eran la misma pistola gris con el cañon mas largo, y desde arriba, a 15 px, el ojo solo veia
    // un cuadradito de color y un palito. Cada una tiene una silueta propia vista desde arriba (la
    // pistola, un ladrillo corto; el revolver, un tambor redondo que sobresale a los costados; la
    // escopeta, dos cañones gruesos lado a lado; el rifle, una vara larga con culata, cargador, mira
    // y freno de boca; la de energia, una horquilla blanca con una esfera en el medio), su color de
    // cuerpo y su luz de neon con otra forma.
    //
    // Las medidas son las de Argon, en centimetros, por Cm: el juego las agranda (el heroe es un
    // muñeco cabezon y el arma se ve a 15-20 px), y con este factor la pistola mide lo de antes.
    // Dos cambios: el tambor del revolver va parado (acostado, como lo pidio, desde arriba se veria
    // un rectangulo, y lo que el queria era "un circulo amarillo inconfundible"), y la luz de cada
    // arma es del color de su tramo (el de la bala) y no el de su lista, que es casi el mismo.
    public const float Cm = 0.0165f;

    enum Forma { Caja, Cilindro, CilindroParado, Esfera }

    struct Parte
    {
        public string nombre, material;
        public Forma forma;
        public Vector3 medidas, posicion, giro;   // cm y grados; un cilindro: (diametro, largo, -)
        public bool luz;
        public Parte(string nombre, Forma forma, Vector3 medidas, Vector3 posicion, Vector3 giro, string material, bool luz = false)
        {
            this.nombre = nombre; this.forma = forma; this.medidas = medidas; this.posicion = posicion;
            this.giro = giro; this.material = material; this.luz = luz;
        }
    }

    static Parte P(string nombre, Forma forma, float x, float y, float z, float px, float py, float pz, string material, float giroX = 0f, float giroY = 0f)
    {
        return new Parte(nombre, forma, new Vector3(x, y, z), new Vector3(px, py, pz), new Vector3(giroX, giroY, 0f), material);
    }

    static Parte Luz(string nombre, Forma forma, float x, float y, float z, float px, float py, float pz)
    {
        return new Parte(nombre, forma, new Vector3(x, y, z), new Vector3(px, py, pz), Vector3.zero, null, true);
    }

    // Los colores del cuerpo (Argon): el carbon de las empuñaduras y los cañones es el mismo en todas.
    static readonly (string nombre, string hex, float brillo)[] Colores =
    {
        ("ArmaCarbon", "#26282E", 0.35f),
        ("ArmaPistolaAcero", "#5A6270", 0.55f),
        ("ArmaRevolverPlata", "#A8B2C0", 0.7f),
        ("ArmaRevolverCacha", "#6B3E26", 0.25f),
        ("ArmaEscopetaMadera", "#8C4A27", 0.25f),
        ("ArmaEscopetaBronce", "#4A3B32", 0.45f),
        ("ArmaRifleVerde", "#33403A", 0.3f),
        ("ArmaRifleNegro", "#1E2024", 0.35f),
        ("ArmaEnergiaTitanio", "#DCE4EC", 0.6f),
        ("ArmaEnergiaMango", "#221F30", 0.35f),
    };

    // Pieza por pieza, en el orden de ArmaEnLaMano.ModelosDeLasArmas, y la boca de cada una (cm).
    static readonly Parte[][] Piezas =
    {
        new[]   // pistola: corta y compacta, un ladrillo blanco pegado a la mano
        {
            P("Empuñadura", Forma.Caja, 5f, 13f, 6f, 0f, -5f, -2f, "ArmaCarbon", -15f),
            P("Corredera", Forma.Caja, 7f, 7f, 16f, 0f, 2f, 4f, "ArmaPistolaAcero"),
            Luz("Placa", Forma.Caja, 7.4f, 4.5f, 12f, 0f, 3.5f, 3.5f),
            P("Canon", Forma.Cilindro, 3.5f, 4f, 0f, 0f, 2f, 13.5f, "ArmaCarbon"),
            P("Mira", Forma.Caja, 2f, 1.8f, 2f, 0f, 6f, 10.5f, "ArmaCarbon"),
        },
        new[]   // revolver: un tambor amarillo que sobresale a los costados y la nariz fina adelante
        {
            P("Cacha", Forma.Caja, 5f, 14f, 6f, 0f, -5.5f, -3f, "ArmaRevolverCacha", -24f),
            P("Armazon", Forma.Caja, 5.5f, 6.5f, 11f, 0f, 1.5f, 2f, "ArmaRevolverPlata"),
            Luz("Tambor", Forma.CilindroParado, 11.5f, 7.5f, 0f, 0f, 2f, 3.5f),
            P("Canon", Forma.Caja, 4.5f, 7.5f, 15f, 0f, 2.5f, 14.5f, "ArmaRevolverPlata"),
            Luz("Puente", Forma.Caja, 2.8f, 1.5f, 13f, 0f, 6.6f, 14.5f),
            P("Martillo", Forma.Caja, 2f, 3f, 3.5f, 0f, 5.5f, -4f, "ArmaCarbon", 30f),
        },
        new[]   // escopeta: dos cañones gruesos lado a lado, doble de ancha en la punta
        {
            P("Culata", Forma.Caja, 5.5f, 11f, 13f, 0f, -3.5f, -5.5f, "ArmaEscopetaMadera", -35f),
            P("Bascula", Forma.Caja, 11.5f, 7f, 10f, 0f, 2f, 2f, "ArmaEscopetaBronce"),
            P("CanonIzquierdo", Forma.Cilindro, 5.5f, 20f, 0f, -3.1f, 3f, 16f, "ArmaCarbon"),
            P("CanonDerecho", Forma.Cilindro, 5.5f, 20f, 0f, 3.1f, 3f, 16f, "ArmaCarbon"),
            P("Guardamanos", Forma.Caja, 10.5f, 4.5f, 12f, 0f, -0.8f, 13f, "ArmaEscopetaMadera"),
            Luz("TiraIzquierda", Forma.Caja, 3.2f, 2f, 15f, -3.1f, 6f, 11f),
            Luz("TiraDerecha", Forma.Caja, 3.2f, 2f, 15f, 3.1f, 6f, 11f),
        },
        new[]   // rifle: una vara larguisima con culata, cargador, mira y freno de boca
        {
            P("Empuñadura", Forma.Caja, 4.5f, 12f, 5.5f, 0f, -5f, -1f, "ArmaRifleNegro", -12f),
            P("Culata", Forma.Caja, 5f, 8.5f, 10f, 0f, 0.5f, -8.5f, "ArmaRifleVerde"),
            P("Cuerpo", Forma.Caja, 6.5f, 7.5f, 22f, 0f, 2f, 7f, "ArmaRifleVerde"),
            P("Cargador", Forma.Caja, 4f, 13f, 6f, 0f, -5.5f, 11f, "ArmaRifleNegro", 18f),
            Luz("Mira", Forma.Cilindro, 5f, 11f, 0f, 0f, 8.2f, 4.5f),
            P("Canon", Forma.Cilindro, 3f, 15f, 0f, 0f, 2.5f, 25.5f, "ArmaRifleNegro"),
            Luz("Tira", Forma.Caja, 2.5f, 1.5f, 12f, 0f, 6f, 13f),
            P("Freno", Forma.Caja, 5.5f, 4.5f, 4.5f, 0f, 2.5f, 34f, "ArmaRifleVerde"),
        },
        new[]   // energia: una horquilla blanca abierta con una esfera violeta en el medio
        {
            P("Empuñadura", Forma.Caja, 5f, 12f, 6f, 0f, -5f, -1f, "ArmaEnergiaMango", -15f),
            P("Generador", Forma.Caja, 10.5f, 9f, 11f, 0f, 2.5f, 1f, "ArmaEnergiaTitanio"),
            Luz("Nucleo", Forma.Esfera, 10f, 10f, 10f, 0f, 3f, 11.5f),
            P("RielIzquierdo", Forma.Caja, 3f, 6.5f, 20f, -5.5f, 3f, 16f, "ArmaEnergiaTitanio", 0f, -3f),
            P("RielDerecho", Forma.Caja, 3f, 6.5f, 20f, 5.5f, 3f, 16f, "ArmaEnergiaTitanio", 0f, 3f),
            Luz("BobinaTrasera", Forma.Caja, 14.5f, 2.2f, 2.5f, 0f, 6.5f, 9f),
            Luz("BobinaDelantera", Forma.Caja, 14.5f, 2.2f, 2.5f, 0f, 6.5f, 20f),
        },
    };

    public static readonly Vector3[] BocaDeCadaArma =
    {
        new Vector3(0f, 2f, 16f), new Vector3(0f, 2.5f, 22.5f), new Vector3(0f, 3f, 26.5f),
        new Vector3(0f, 2.5f, 36.5f), new Vector3(0f, 3f, 27f),
    };

    [MenuItem("ShowBies/Armas/Armar la pistola")]
    public static void ArmarPistola()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(RutaPistola));
        var materiales = new System.Collections.Generic.Dictionary<string, Material>();
        foreach (var c in Colores)
        {
            ColorUtility.TryParseHtmlString(c.hex, out Color color);
            materiales[c.nombre] = Material(c.nombre, color, c.brillo);
        }
        // La luz de neon de cada arma: blanca; ArmaEnLaMano la tiñe del color del tramo, la hace latir
        // en cada tiro y la pone roja con la furia.
        var luz = MaterialDeLaLuz();
        var esfera = AssetDatabase.LoadAssetAtPath<Mesh>(RutaEsferaBaja);
        if (esfera == null) { Debug.LogError("ConstructorArmas: falta " + RutaEsferaBaja + " (la arma ShowBies > Halloween)"); return; }

        var raiz = new GameObject("Pistola");
        for (int i = 0; i < Piezas.Length; i++)
        {
            var modelo = new GameObject(ArmaEnLaMano.ModelosDeLasArmas[i]);
            modelo.transform.SetParent(raiz.transform, false);
            var led = new GameObject("LED");
            led.transform.SetParent(modelo.transform, false);
            foreach (var parte in Piezas[i])
            {
                var padre = parte.luz ? led : modelo;
                var material = parte.luz ? luz : materiales[parte.material];
                var go = Armar(padre, parte, material, esfera);
                if (parte.luz) go.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            var punta = new GameObject("Punta");
            punta.transform.SetParent(modelo.transform, false);
            punta.transform.localPosition = BocaDeCadaArma[i] * Cm;
            modelo.SetActive(i == 0);
        }

        var boca = new GameObject("Boca");
        boca.transform.SetParent(raiz.transform, false);
        boca.transform.localPosition = BocaDeCadaArma[0] * Cm;

        // El fogonazo: apagado, lo prende ArmaEnLaMano en cada tiro y lo pone de frente a
        // la camara, estirado hacia donde apunta el cañon.
        var fogonazo = GameObject.CreatePrimitive(PrimitiveType.Quad);
        fogonazo.name = "Fogonazo";
        Object.DestroyImmediate(fogonazo.GetComponent<Collider>());
        fogonazo.transform.SetParent(boca.transform, false);
        var render = fogonazo.GetComponent<MeshRenderer>();
        render.sharedMaterial = MaterialDelFogonazo();
        render.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        render.receiveShadows = false;
        render.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        render.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        fogonazo.SetActive(false);

        PrefabUtility.SaveAsPrefabAsset(raiz, RutaPistola);
        Object.DestroyImmediate(raiz);
        AssetDatabase.SaveAssets();
        Debug.Log("ConstructorArmas: las cinco armas armadas en " + RutaPistola);
    }

    // Una pieza, sin collider. Un cilindro de Unity mide 1 x 2 x 1 con el largo en Y: acostado en Z
    // va girado 90° en X y con el largo a la mitad; parado, igual sin girar. La esfera es la baja de
    // Halloween (150 vertices), medida para que tenga el diametro pedido.
    static GameObject Armar(GameObject padre, Parte parte, Material material, Mesh esfera)
    {
        GameObject go;
        Vector3 escala;
        Quaternion giro = Quaternion.Euler(parte.giro);
        switch (parte.forma)
        {
            case Forma.Cilindro:
                go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                escala = new Vector3(parte.medidas.x, parte.medidas.y * 0.5f, parte.medidas.x);
                giro = giro * Quaternion.Euler(90f, 0f, 0f);
                break;
            case Forma.CilindroParado:
                go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                escala = new Vector3(parte.medidas.x, parte.medidas.y * 0.5f, parte.medidas.x);
                break;
            case Forma.Esfera:
                go = new GameObject();
                go.AddComponent<MeshFilter>().sharedMesh = esfera;
                go.AddComponent<MeshRenderer>();
                float diametro = Mathf.Max(0.0001f, esfera.bounds.size.x);
                escala = parte.medidas / diametro;
                break;
            default:
                go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                escala = parte.medidas;
                break;
        }
        go.name = parte.nombre;
        var collider = go.GetComponent<Collider>();
        if (collider != null) Object.DestroyImmediate(collider);
        go.transform.SetParent(padre.transform, false);
        go.transform.localPosition = parte.posicion * Cm;
        go.transform.localRotation = giro;
        go.transform.localScale = escala * Cm;
        go.GetComponent<MeshRenderer>().sharedMaterial = material;
        return go;
    }

    // Unlit/Color, que ya usan las escenas (el neon de los decorados): la luz no depende de la
    // luna ni del relleno.
    static Material MaterialDeLaLuz()
    {
        string ruta = CarpetaMateriales + "/PistolaLuz.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Unlit/Color"));
            AssetDatabase.CreateAsset(mat, ruta);
        }
        mat.shader = Shader.Find("Unlit/Color");
        mat.color = Color.white;
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static Material MaterialDelFogonazo()
    {
        var shader = Shader.Find("ShowBies/Fogonazo");
        string ruta = CarpetaMateriales + "/Fogonazo.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, ruta);
        }
        mat.shader = shader;
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static Material Material(string nombre, Color color, float brillo)
    {
        string ruta = CarpetaMateriales + "/" + nombre + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Standard"));
            AssetDatabase.CreateAsset(mat, ruta);
        }
        mat.color = color;
        mat.SetFloat("_Glossiness", brillo);
        mat.SetFloat("_Metallic", 0f);
        EditorUtility.SetDirty(mat);
        return mat;
    }
}
