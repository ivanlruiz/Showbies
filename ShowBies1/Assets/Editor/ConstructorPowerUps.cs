using System.IO;
using UnityEditor;
using UnityEngine;

// Los dibujos de las tres cajas (pedido de Ivan, 9/10: que se entienda que es cada una). Hasta
// ahi eran un cubo o un cuadrado chato de 0,4 m de un color, con particulas: de noche se leian
// como puntitos sueltos. Cada una pasa a ser un icono con formas simples (como las calabazas),
// flotando y de frente a la camara del juego (que mira desde arriba a 70 grados: un dibujo
// parado se veria aplastado a un tercio de su alto), con un halo y un charco de su color, como
// los faroles de noche: un corazon rojo la vida, tres balas doradas la municion y un rayo celeste
// la del arma (Ivan los eligio entre maquetas: el corazon antes que un botiquin y el rayo antes
// que una pistola). Lo anima AspectoDeCaja. No se edita a mano: lo arma ShowBies > Power-ups >
// Armar las cajas, que se vuelve a correr para cambiarlas.
public static class ConstructorPowerUps
{
    const string Carpeta = "Assets/PowerUps";
    const string CarpetaMateriales = Carpeta + "/Materiales";

    internal static readonly Color Rojo = new Color(1f, 0.22f, 0.36f);
    internal static readonly Color Oro = new Color(1f, 0.78f, 0.22f);
    internal static readonly Color OroOscuro = new Color(0.62f, 0.42f, 0.08f);
    internal static readonly Color Cobre = new Color(1f, 0.48f, 0.2f);
    internal static readonly Color Celeste = new Color(0.1f, 0.9f, 1f);

    // Donde va el centro del dibujo sobre la raiz de la caja (que nace a 0,5 m del piso), y el
    // charco y el anillo, sobre el piso: a 0,21 m, por encima del cordon de la ciudad.
    internal const float AlturaDelDibujo = 0.45f;
    internal const float AlturaDelPiso = -0.29f;
    // El tamanio del dibujo: hecho de 0,7 m, a la escala de la camara del juego medía unos 60 px,
    // menos que las particulas de las cajas de antes.
    internal const float Escala = 1.6f;
    // El collider de la caja (un trigger): el de antes, 3 de lado con la escala de 0,4.
    const float LadoDelTrigger = 1.2f;

    [MenuItem("ShowBies/Power-ups/Armar las cajas")]
    public static void Armar()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("ConstructorPowerUps: no se arma en play");
            return;
        }
        Vestir("Assets/Prefabs/PUVida.prefab", "Vida", Rojo, DibujarCorazon);
        Vestir("Assets/Prefabs/PUBalas.prefab", "Balas", Oro, DibujarBalas);
        Vestir("Assets/Prefabs/PUArma.prefab", "Arma", Celeste, DibujarRayo);
        AssetDatabase.SaveAssets();
        Debug.Log("ConstructorPowerUps: listas las tres cajas");
    }

    // Saca lo de antes (el cubo, el cuadrado chato con la textura y las particulas, o lo de una
    // vuelta anterior de este constructor) y le pone el dibujo, el halo, el charco y AspectoDeCaja.
    // Quedan la etiqueta, la capa, el trigger (del mismo tamanio en el mundo) y PickupCaducidad.
    static void Vestir(string ruta, string nombre, Color color, System.Action<Transform> dibujar)
    {
        var raiz = PrefabUtility.LoadPrefabContents(ruta);
        try
        {
            for (int i = raiz.transform.childCount - 1; i >= 0; i--)
            {
                var hijo = raiz.transform.GetChild(i).gameObject;
                if (hijo.name == "Modelo" || hijo.name == "Halo" || hijo.name == "Charco") Object.DestroyImmediate(hijo);
            }
            foreach (var ps in raiz.GetComponents<ParticleSystem>()) Object.DestroyImmediate(ps);
            foreach (var pr in raiz.GetComponents<ParticleSystemRenderer>()) Object.DestroyImmediate(pr);
            var malla = raiz.GetComponent<MeshRenderer>();
            if (malla != null) Object.DestroyImmediate(malla);
            var filtro = raiz.GetComponent<MeshFilter>();
            if (filtro != null) Object.DestroyImmediate(filtro);

            // Sin giro: el de vida venia girado -90 grados y el del arma 70 (los cubos de antes). El
            // juego las crea sin giro igual (PowerUp, TutorialManager), pero el dibujo y el halo
            // ya miran a la camara por su cuenta.
            raiz.transform.localRotation = Quaternion.identity;
            raiz.transform.localScale = Vector3.one;
            var trigger = raiz.GetComponent<BoxCollider>();
            if (trigger != null)
            {
                trigger.isTrigger = true;
                trigger.center = Vector3.zero;
                trigger.size = Vector3.one * LadoDelTrigger;
            }

            dibujar(Modelo(raiz.transform));
            Brillos(raiz.transform, nombre, color);
            var aspecto = raiz.GetComponent<AspectoDeCaja>();
            if (aspecto == null) aspecto = raiz.AddComponent<AspectoDeCaja>();
            aspecto.modelo = raiz.transform.Find("Modelo");
            aspecto.color = color;
            // El de la linea del jefe (Sprites/Default, que ya esta en la build): pinta las dos caras
            // y toma el color de la linea, que pone AspectoDeCaja.
            aspecto.materialAnillo = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
            aspecto.alturaAnillo = AlturaDelPiso;
            PrefabUtility.SaveAsPrefabAsset(raiz, ruta);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(raiz);
        }
    }

    // --- Los dibujos -----------------------------------------------------------------
    //
    // Cada uno en el plano XY de 'padre', con el frente hacia -Z y unos 0,7 m de ancho: quien lo
    // pone gira el padre con el giro de la camara (ConstructorEscenarios.GiroDeLaCamara).

    internal static void DibujarCorazon(Transform padre)
    {
        var rojo = Plano("VidaRojo", Rojo);
        Pieza(padre, PrimitiveType.Sphere, new Vector3(-0.15f, 0.08f, 0f), Vector3.zero, new Vector3(0.36f, 0.36f, 0.2f), rojo);
        Pieza(padre, PrimitiveType.Sphere, new Vector3(0.15f, 0.08f, 0f), Vector3.zero, new Vector3(0.36f, 0.36f, 0.2f), rojo);
        Pieza(padre, PrimitiveType.Cube, new Vector3(0f, -0.07f, 0f), new Vector3(0f, 0f, 45f), new Vector3(0.36f, 0.36f, 0.19f), rojo);
        Pieza(padre, PrimitiveType.Sphere, new Vector3(-0.19f, 0.15f, -0.1f), Vector3.zero, new Vector3(0.08f, 0.08f, 0.02f), Plano("VidaBrillo", Color.white));
    }

    internal static void DibujarBalas(Transform padre)
    {
        var oro = Plano("BalaOro", Oro);
        var culote = Plano("BalaCulote", OroOscuro);
        var punta = Plano("BalaPunta", Cobre);
        foreach (float x in new[] { -0.21f, 0f, 0.21f })
        {
            Pieza(padre, PrimitiveType.Cylinder, new Vector3(x, -0.08f, 0f), Vector3.zero, new Vector3(0.14f, 0.15f, 0.14f), oro);
            Pieza(padre, PrimitiveType.Cylinder, new Vector3(x, -0.22f, -0.002f), Vector3.zero, new Vector3(0.155f, 0.018f, 0.155f), culote);
            Pieza(padre, PrimitiveType.Sphere, new Vector3(x, 0.1f, 0f), Vector3.zero, new Vector3(0.138f, 0.22f, 0.138f), punta);
        }
    }

    internal static void DibujarRayo(Transform padre)
    {
        var celeste = Plano("ArmaRayo", Celeste);
        Tramo(padre, new Vector2(0.15f, 0.34f), new Vector2(-0.07f, 0.01f), 0.12f, celeste);
        Tramo(padre, new Vector2(-0.13f, 0.02f), new Vector2(0.13f, -0.02f), 0.1f, celeste);
        Tramo(padre, new Vector2(0.07f, -0.01f), new Vector2(-0.15f, -0.34f), 0.12f, celeste);
    }

    // El halo de frente a la camara, detras del dibujo, y el charco en el piso: los dos con el
    // shader del charco, del color de la caja.
    internal static void Brillos(Transform raiz, string nombre, Color color)
    {
        var halo = ConstructorEscenarios.MaterialDeBrillo(Materiales(), nombre + "Halo", color, 1.5f, 0.3f, 3.5f);
        var charco = ConstructorEscenarios.MaterialDeBrillo(Materiales(), nombre + "Charco", color, 1f, 0.35f, 4.5f);
        Vector3 detras = ConstructorEscenarios.GiroDeLaCamara * new Vector3(0f, 0f, 0.2f);
        var h = Pieza(raiz, PrimitiveType.Quad, new Vector3(0f, AlturaDelDibujo, 0f) + detras, ConstructorEscenarios.GiroDeLaCamara.eulerAngles,
                      new Vector3(2.6f, 2.6f, 1f), halo);
        h.name = "Halo";
        var c = Pieza(raiz, PrimitiveType.Quad, new Vector3(0f, AlturaDelPiso, 0f), new Vector3(90f, 0f, 0f), new Vector3(3.2f, 3.2f, 1f), charco);
        c.name = "Charco";
    }

    // Un objeto vacio con el giro de la camara, en la altura del dibujo: el padre de las piezas.
    internal static Transform Modelo(Transform raiz)
    {
        var go = new GameObject("Modelo");
        go.layer = Personajes.Capa;
        go.transform.SetParent(raiz, false);
        go.transform.localPosition = new Vector3(0f, AlturaDelDibujo, 0f);
        go.transform.localRotation = ConstructorEscenarios.GiroDeLaCamara;
        go.transform.localScale = Vector3.one * Escala;
        return go.transform;
    }

    // --- Las piezas ------------------------------------------------------------------

    static string Materiales()
    {
        Directory.CreateDirectory(CarpetaMateriales);
        return CarpetaMateriales;
    }

    static Material Plano(string nombre, Color color)
    {
        return ConstructorEscenarios.MaterialPlano(Materiales(), nombre, color);
    }

    // Un tramo recto entre dos puntos del plano, de ese grosor.
    static void Tramo(Transform padre, Vector2 a, Vector2 b, float grosor, Material material)
    {
        Vector2 d = b - a;
        float angulo = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
        Pieza(padre, PrimitiveType.Cube, (Vector3)((a + b) * 0.5f), new Vector3(0f, 0f, angulo), new Vector3(d.magnitude + grosor * 0.5f, grosor, 0.1f), material);
    }

    // Sin collider, sin sombra y en la capa de los personajes, como lo demas de la caja.
    static GameObject Pieza(Transform padre, PrimitiveType tipo, Vector3 posicion, Vector3 giro, Vector3 escala, Material material)
    {
        var go = GameObject.CreatePrimitive(tipo);
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(padre, false);
        go.transform.localPosition = posicion;
        go.transform.localRotation = Quaternion.Euler(giro);
        go.transform.localScale = escala;
        var r = go.GetComponent<MeshRenderer>();
        r.sharedMaterial = material;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        r.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        r.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        go.layer = Personajes.Capa;
        return go;
    }
}
