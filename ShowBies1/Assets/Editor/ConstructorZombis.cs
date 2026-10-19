using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Arma los zombis con habilidad (ZOMBIS.md, de Argon): su Enemy, la piel, las piezas que los distinguen desde arriba y
// el prefab, que sale del zombi normal (el mismo modelo del pack, sin clips nuevos), y los suma a la mezcla del
// WaveManager de WaveMode con su oleada, su peso y su tope. Hoy, el escupidor, el saltador, el chaman y el blindado.
//
// El Enemy se crea una sola vez: despues es balance y se toca en el asset, como los otros. Lo demas se rehace cada vez.
// No se edita a mano: se vuelve a correr ShowBies > Zombis > Armar el escupidor (o el que sea).
public static class ConstructorZombis
{
    const string RutaNormal = "Assets/Prefabs/Personajes/Zombi.prefab";
    const string RutaPielNormal = "Assets/Materiales/ZombiNormalPiel.mat";
    const string RutaJefe = "Assets/Prefabs/Personajes/ZombiBOSS.prefab";
    const string CarpetaMateriales = "Assets/Materiales";
    const string EscenaOleadas = "Assets/Escenas/WaveMode.unity";

    // --- El escupidor -----------------------------------------------------------------------------

    public const string RutaEscupidor = "Assets/Prefabs/Personajes/ZombiEscupidor.prefab";
    public const string RutaEnemigoEscupidor = "Assets/Zombies/ZombiEscupidor.asset";
    public const string RutaPielEscupidor = "Assets/Materiales/ZombiEscupidorPiel.mat";
    const string RutaParticulasEscupidor = "Assets/Prefabs/Particulas/ExplosionAmarilla.prefab";

    public static readonly Color AmarilloAcido = new Color(0.83f, 1f, 0f);   // #D4FF00, de Argon
    public static readonly Vector3 FormaDelEscupidor = new Vector3(0.9f, 0.9f, 1.15f);
    // El cementerio estrena al escupidor (el reparto por capitulo de Argon, que eligio Ivan el 18/10).
    public const int EscupidorDesde = 12;
    // Como mucho 3 en el cementerio y la ciudad, y 2 desde la 31, donde convive con los otros cuatro especiales (de Argon,
    // 18/10). x desde, y hasta (0, sin fin), z el tope de vivos.
    public static readonly Vector3Int[] TramosDelEscupidor = { new Vector3Int(EscupidorDesde, 30, 3), new Vector3Int(31, 0, 2) };
    public const float EscupidorPeso = 0.15f;
    public const int EscupidorMaxVivos = 3;
    // Los sacos de acido: sobre los hombros, a los costados de la cabeza, de lo que miden en el mundo. En la espalda o
    // detras de la nuca la cabeza del modelo (grande y larga) los tapaba desde la camara del juego.
    public const float LadoDelSaco = 0.22f;
    // Mas claro que la piel: del mismo amarillo, se fundian con ella.
    public static readonly Color ColorDeLosSacos = new Color(1f, 1f, 0.55f);

    [MenuItem("ShowBies/Zombis/Armar el escupidor")]
    public static void ArmarEscupidor()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogError("ConstructorZombis: no en play"); return; }

        // El Enemy: solo si no esta. Del normal (vida 5, golpe 1, velocidad 5, 1-3 monedas): vida x1,8, el golpe es el
        // del charco (la mitad, en el componente), velocidad x0,95, 8 puntos, monedas x3, 2 de PODER.
        var enemigo = AssetDatabase.LoadAssetAtPath<Enemy>(RutaEnemigoEscupidor);
        if (enemigo == null)
        {
            enemigo = ScriptableObject.CreateInstance<Enemy>();
            enemigo.hp = 9;
            enemigo.daño = 1;
            enemigo.velocidad = 4.75f;
            enemigo.puntos = 8;
            enemigo.monedasMin = 5;
            enemigo.monedasMax = 7;
            enemigo.experiencia = 2;
            AssetDatabase.CreateAsset(enemigo, RutaEnemigoEscupidor);
        }

        var piel = Piel(RutaPielEscupidor, AmarilloAcido);
        if (piel == null) return;
        var acido = PlanoConInstancing("AcidoEscupidor", AmarilloAcido);
        var sacoMaterial = PlanoConInstancing("SacoEscupidor", ColorDeLosSacos);
        var charco = ConstructorEscenarios.MaterialDeBrillo(CarpetaMateriales, "AcidoCharco", AmarilloAcido, AcidoDelEscupidor.IntensidadDelCharco, 0.35f, 4f);
        var jefe = AssetDatabase.LoadAssetAtPath<GameObject>(RutaJefe);
        var aviso = jefe != null && jefe.GetComponent<JefePatrones>() != null ? jefe.GetComponent<JefePatrones>().materialAviso : null;
        var esfera = AssetDatabase.LoadAssetAtPath<Mesh>(ConstructorArmas.RutaEsferaBaja);
        if (aviso == null || esfera == null) { Debug.LogError("ConstructorZombis: falta la linea del jefe o la esfera baja"); return; }

        var vista = EditorSceneManager.NewPreviewScene();
        try
        {
            var go = DesdeElNormal(vista, "ZombiEscupidor", enemigo, piel, RutaParticulasEscupidor);
            if (go == null) return;
            var modelo = Modelo(go);
            modelo.localScale = Vector3.Scale(modelo.localScale, FormaDelEscupidor);

            // Los sacos: dos esferas amarillas que brillan solas, sobre los hombros y detras de la nuca, colgadas del
            // cuello para que se muevan con el torso. Se ponen en el mundo con el modelo en reposo y despues se cuelgan
            // del hueso.
            var columna = Hueso(go.transform, "Bip001 Neck");
            if (columna == null) { Debug.LogError("ConstructorZombis: el modelo no tiene Bip001 Neck"); return; }
            var sacos = new Transform[2];
            for (int i = 0; i < 2; i++)
            {
                var saco = new GameObject(i == 0 ? "SacoIzquierdo" : "SacoDerecho");
                saco.AddComponent<MeshFilter>().sharedMesh = esfera;
                var render = saco.AddComponent<MeshRenderer>();
                render.sharedMaterial = sacoMaterial;
                render.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                render.receiveShadows = false;
                Vector3 donde = columna.position - go.transform.forward * 0.06f + Vector3.up * 0.02f
                                + go.transform.right * (i == 0 ? -0.21f : 0.21f);
                saco.transform.SetParent(columna, true);
                saco.transform.position = donde;
                saco.transform.rotation = Quaternion.identity;
                // La esfera baja mide 1 de diametro: el lado en el mundo, sin la escala que hereda del hueso.
                Vector3 heredada = columna.lossyScale;
                saco.transform.localScale = new Vector3(LadoDelSaco / Mathf.Abs(heredada.x), LadoDelSaco / Mathf.Abs(heredada.y), LadoDelSaco / Mathf.Abs(heredada.z));
                saco.layer = Personajes.Capa;
                sacos[i] = saco.transform;
            }

            var escupidor = go.GetComponent<ZombiEscupidor>();
            if (escupidor == null) escupidor = go.AddComponent<ZombiEscupidor>();
            escupidor.materialAviso = aviso;
            escupidor.materialCharco = charco;
            escupidor.materialAcido = acido;
            escupidor.colorAcido = AmarilloAcido;
            escupidor.sacos = sacos;

            PrefabUtility.SaveAsPrefabAsset(go, RutaEscupidor);
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(vista);
        }
        AssetDatabase.SaveAssets();

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaEscupidor);
        SumarALaMezcla(prefab, TramosDelEscupidor, EscupidorPeso);
        Debug.Log("ConstructorZombis: el escupidor, listo en " + RutaEscupidor);
    }

    // --- El saltador ------------------------------------------------------------------------------

    public const string RutaSaltador = "Assets/Prefabs/Personajes/ZombiSaltador.prefab";
    public const string RutaEnemigoSaltador = "Assets/Zombies/ZombiSaltador.asset";
    public const string RutaPielSaltador = "Assets/Materiales/ZombiSaltadorPiel.mat";
    const string RutaParticulasSaltador = "Assets/Prefabs/Particulas/ExplosionAzul.prefab";

    public static readonly Color AzulNoche = new Color(0.157f, 0.165f, 0.42f);   // #282A6B, de Argon
    public static readonly Color CremaDelSaltador = new Color(0.98f, 0.93f, 0.78f);   // los ojos, las crestas y el aviso
    public static readonly Vector3 FormaDelSaltador = new Vector3(0.85f, 1.15f, 1.15f);
    // El cementerio (15 a 20); descansa en la ciudad y vuelve en la horda total, desde la 31.
    public static readonly Vector2Int[] TramosDelSaltador = { new Vector2Int(15, 20), new Vector2Int(31, 0) };
    public const float SaltadorPeso = 0.15f;
    public const int SaltadorMaxVivos = 4;

    [MenuItem("ShowBies/Zombis/Armar el saltador")]
    public static void ArmarSaltador()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogError("ConstructorZombis: no en play"); return; }

        // Del normal: vida x1,4, el golpe del salto x1,2 (en el componente), velocidad x1,1, 10 puntos, monedas x2,5, 2 de
        // PODER.
        var enemigo = AssetDatabase.LoadAssetAtPath<Enemy>(RutaEnemigoSaltador);
        if (enemigo == null)
        {
            enemigo = ScriptableObject.CreateInstance<Enemy>();
            enemigo.hp = 7;
            enemigo.daño = 1;
            enemigo.velocidad = 5.5f;
            enemigo.puntos = 10;
            enemigo.monedasMin = 4;
            enemigo.monedasMax = 6;
            enemigo.experiencia = 2;
            AssetDatabase.CreateAsset(enemigo, RutaEnemigoSaltador);
        }

        var piel = Piel(RutaPielSaltador, AzulNoche);
        if (piel == null) return;
        var crema = PlanoConInstancing("CremaSaltador", CremaDelSaltador);
        var jefe = AssetDatabase.LoadAssetAtPath<GameObject>(RutaJefe);
        var aviso = jefe != null && jefe.GetComponent<JefePatrones>() != null ? jefe.GetComponent<JefePatrones>().materialAviso : null;
        var esfera = AssetDatabase.LoadAssetAtPath<Mesh>(ConstructorArmas.RutaEsferaBaja);
        var cubo = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
        if (aviso == null || esfera == null || cubo == null) { Debug.LogError("ConstructorZombis: falta la linea del jefe, la esfera baja o el cubo"); return; }

        var vista = EditorSceneManager.NewPreviewScene();
        try
        {
            var go = DesdeElNormal(vista, "ZombiSaltador", enemigo, piel, RutaParticulasSaltador);
            if (go == null) return;
            var modelo = Modelo(go);
            modelo.localScale = Vector3.Scale(modelo.localScale, FormaDelSaltador);

            // Los ojos grandes, adelante de la cabeza, y dos crestas hacia atras arriba: medidos de la cabeza del modelo.
            var cabeza = Hueso(go.transform, "Bip001 Head");
            Renderer dibujoDeLaCabeza = null;
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                if (r.name.StartsWith("Z_head")) dibujoDeLaCabeza = r;
            if (cabeza == null || dibujoDeLaCabeza == null) { Debug.LogError("ConstructorZombis: no encontre la cabeza del modelo"); return; }
            Bounds caja = dibujoDeLaCabeza.bounds;
            Vector3 adelante = go.transform.forward, arriba = Vector3.up, costado = go.transform.right;
            for (int i = 0; i < 2; i++)
            {
                float lado = i == 0 ? -1f : 1f;
                Pieza(cabeza, "Ojo" + (i == 0 ? "Izquierdo" : "Derecho"), esfera, crema,
                      caja.center + adelante * caja.extents.z * 0.8f + arriba * caja.extents.y * 0.15f + costado * lado * caja.extents.x * 0.45f,
                      Quaternion.identity, Vector3.one * 0.11f);
                // Un cubo girado 45 grados y medio hundido en la cabeza: asoma un triangulo hacia atras.
                Pieza(cabeza, "Cresta" + (i == 0 ? "Izquierda" : "Derecha"), cubo, crema,
                      caja.center + arriba * caja.extents.y * 0.85f - adelante * caja.extents.z * 0.35f + costado * lado * caja.extents.x * 0.35f,
                      Quaternion.LookRotation(adelante, arriba) * Quaternion.Euler(-55f, 0f, 45f), new Vector3(0.05f, 0.16f, 0.16f));
            }

            var saltador = go.GetComponent<ZombiSaltador>();
            if (saltador == null) saltador = go.AddComponent<ZombiSaltador>();
            saltador.materialAviso = aviso;
            saltador.colorAviso = CremaDelSaltador;

            PrefabUtility.SaveAsPrefabAsset(go, RutaSaltador);
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(vista);
        }
        AssetDatabase.SaveAssets();

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaSaltador);
        SumarALaMezcla(prefab, TramosDelSaltador, SaltadorPeso, SaltadorMaxVivos);
        Debug.Log("ConstructorZombis: el saltador, listo en " + RutaSaltador);
    }

    // --- El chaman --------------------------------------------------------------------------------

    public const string RutaChaman = "Assets/Prefabs/Personajes/ZombiChaman.prefab";
    public const string RutaEnemigoChaman = "Assets/Zombies/ZombiChaman.asset";
    public const string RutaPielChaman = "Assets/Materiales/ZombiChamanPiel.mat";
    const string RutaParticulasChaman = "Assets/Prefabs/Particulas/ExplosionBlanca.prefab";

    public static readonly Color HuesoPalido = new Color(0.902f, 0.875f, 0.827f);   // #E6DFD3, de Argon
    public static readonly Color Rubi = new Color(1f, 0.09f, 0.27f);                 // #FF1744, de Argon
    public static readonly Color MaderaDelTotem = new Color(0.42f, 0.27f, 0.15f);
    public static readonly Vector3 FormaDelChaman = new Vector3(0.9f, 1.25f, 0.9f);
    // El cementerio (18 a 20); descansa en la ciudad y vuelve en la horda total, desde la 31.
    public static readonly Vector2Int[] TramosDelChaman = { new Vector2Int(18, 20), new Vector2Int(31, 0) };
    public const float ChamanPeso = 0.10f;
    public const int ChamanMaxVivos = 2;
    // El totem, de lo que mide en el mundo: un palo que sale de la cabeza, el travesaño de la T y el cristal arriba.
    // Mas grande que lo de Argon: a 40 px un cubo de 14 cm es un punto.
    public static readonly Vector3 PaloDelTotem = new Vector3(0.05f, 0.34f, 0.05f);
    public static readonly Vector3 TravesanoDelTotem = new Vector3(0.34f, 0.06f, 0.06f);
    public const float LadoDelCristal = 0.24f;

    [MenuItem("ShowBies/Zombis/Armar el chaman")]
    public static void ArmarChaman()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogError("ConstructorZombis: no en play"); return; }

        // Del normal: vida x2,2, golpe x1, velocidad x0,9, 15 puntos, monedas x4, 3 de PODER.
        var enemigo = AssetDatabase.LoadAssetAtPath<Enemy>(RutaEnemigoChaman);
        if (enemigo == null)
        {
            enemigo = ScriptableObject.CreateInstance<Enemy>();
            enemigo.hp = 11;
            enemigo.daño = 1;
            enemigo.velocidad = 4.5f;
            enemigo.puntos = 15;
            enemigo.monedasMin = 7;
            enemigo.monedasMax = 9;
            enemigo.experiencia = 3;
            AssetDatabase.CreateAsset(enemigo, RutaEnemigoChaman);
        }

        var piel = Piel(RutaPielChaman, HuesoPalido);
        if (piel == null) return;
        var rubi = PlanoConInstancing("RubiChaman", Rubi);
        var madera = PlanoConInstancing("MaderaChaman", MaderaDelTotem);
        var jefe = AssetDatabase.LoadAssetAtPath<GameObject>(RutaJefe);
        var aviso = jefe != null && jefe.GetComponent<JefePatrones>() != null ? jefe.GetComponent<JefePatrones>().materialAviso : null;
        var cubo = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
        if (aviso == null || cubo == null) { Debug.LogError("ConstructorZombis: falta la linea del jefe o el cubo"); return; }

        var vista = EditorSceneManager.NewPreviewScene();
        try
        {
            var go = DesdeElNormal(vista, "ZombiChaman", enemigo, piel, RutaParticulasChaman);
            if (go == null) return;
            var modelo = Modelo(go);
            modelo.localScale = Vector3.Scale(modelo.localScale, FormaDelChaman);

            // El totem en T sobre la cabeza, medido de la cabeza del modelo, con el cristal que gira arriba.
            var cabeza = Hueso(go.transform, "Bip001 Head");
            Renderer dibujoDeLaCabeza = null;
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                if (r.name.StartsWith(Disfraces.MallaDeLaCabeza)) dibujoDeLaCabeza = r;
            if (cabeza == null || dibujoDeLaCabeza == null) { Debug.LogError("ConstructorZombis: no encontre la cabeza del modelo"); return; }
            Bounds caja = dibujoDeLaCabeza.bounds;
            Vector3 arriba = Vector3.up;
            Quaternion derecho = Quaternion.LookRotation(go.transform.forward, arriba);
            Vector3 baseDelPalo = new Vector3(caja.center.x, caja.max.y - 0.04f, caja.center.z) - go.transform.forward * caja.extents.z * 0.2f;
            Pieza(cabeza, "PaloDelTotem", cubo, madera, baseDelPalo + arriba * (PaloDelTotem.y * 0.5f), derecho, PaloDelTotem);
            Pieza(cabeza, "TravesanoDelTotem", cubo, madera, baseDelPalo + arriba * PaloDelTotem.y, derecho, TravesanoDelTotem);
            var cristal = Pieza(cabeza, "CristalDelTotem", cubo, rubi, baseDelPalo + arriba * (PaloDelTotem.y + LadoDelCristal * 0.85f),
                                Quaternion.Euler(45f, 0f, 35f), Vector3.one * LadoDelCristal);

            var chaman = go.GetComponent<ZombiChaman>();
            if (chaman == null) chaman = go.AddComponent<ZombiChaman>();
            chaman.materialAviso = aviso;
            chaman.materialRubi = rubi;
            chaman.mallaDeLaMarca = cubo;
            chaman.colorRubi = Rubi;
            chaman.cristal = cristal;

            PrefabUtility.SaveAsPrefabAsset(go, RutaChaman);
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(vista);
        }
        AssetDatabase.SaveAssets();

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaChaman);
        SumarALaMezcla(prefab, TramosDelChaman, ChamanPeso, ChamanMaxVivos);
        Debug.Log("ConstructorZombis: el chaman, listo en " + RutaChaman);
    }

    // --- El blindado -------------------------------------------------------------------------------

    public const string RutaBlindado = "Assets/Prefabs/Personajes/ZombiBlindado.prefab";
    public const string RutaEnemigoBlindado = "Assets/Zombies/ZombiBlindado.asset";
    public const string RutaPielBlindado = "Assets/Materiales/ZombiBlindadoPiel.mat";
    const string RutaParticulasBlindado = "Assets/Prefabs/Particulas/ExplosionAzul.prefab";
    const string RutaTanque = "Assets/Prefabs/Personajes/ZombiTanque.prefab";

    public static readonly Color GrisPizarra = new Color(0.29f, 0.322f, 0.365f);       // #4A525D, de Argon
    public static readonly Color NegroDelEscudo = new Color(0.102f, 0.114f, 0.141f);   // #1A1D24, de Argon
    public static readonly Color Ambar = new Color(1f, 0.702f, 0f);                    // #FFB300, de Argon
    public static readonly Vector3 FormaDelBlindado = new Vector3(1.2f, 1.05f, 1.05f);
    // La ciudad estrena al blindado (21) y sigue en la horda total.
    public static readonly Vector3Int[] TramosDelBlindado = { new Vector3Int(21, 0, 3) };
    public const float BlindadoPeso = 0.15f;
    // El escudo, en el mundo. Los 75 x 90 cm de Argon son para un cuerpo de persona, y este zombi mide un metro: con las
    // proporciones de la hoja de Nano Banana (de la barbilla a las canillas, del ancho de los hombros) queda en 50 x 58,
    // con el borde de arriba y la mirilla ambar en la misma proporcion. El casco si es el de Argon: la cabeza mide 33 cm.
    public static readonly Vector3 PlacaDelEscudo = new Vector3(0.5f, 0.58f, 0.06f);
    public static readonly Vector3 BordeDelEscudo = new Vector3(0.53f, 0.075f, 0.08f);
    public static readonly Vector3 MirillaDelEscudo = new Vector3(0.33f, 0.09f, 0.075f);
    public const float EscudoAdelante = 0.25f;     // del centro del cuerpo a la placa

    [MenuItem("ShowBies/Zombis/Armar el blindado")]
    public static void ArmarBlindado()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogError("ConstructorZombis: no en play"); return; }

        // Del normal: vida x3,2, golpe x1,4 (en el componente), velocidad x0,75, 12 puntos, monedas x3, 2 de PODER.
        var enemigo = AssetDatabase.LoadAssetAtPath<Enemy>(RutaEnemigoBlindado);
        if (enemigo == null)
        {
            enemigo = ScriptableObject.CreateInstance<Enemy>();
            enemigo.hp = 16;
            enemigo.daño = 1;
            enemigo.velocidad = 3.75f;
            enemigo.puntos = 12;
            enemigo.monedasMin = 5;
            enemigo.monedasMax = 7;
            enemigo.experiencia = 2;
            AssetDatabase.CreateAsset(enemigo, RutaEnemigoBlindado);
        }

        var piel = Piel(RutaPielBlindado, GrisPizarra);
        if (piel == null) return;
        var negro = PlanoConInstancing("NegroBlindado", NegroDelEscudo);
        var ambar = PlanoConInstancing("AmbarBlindado", Ambar);
        var cubo = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
        var tanque = AssetDatabase.LoadAssetAtPath<GameObject>(RutaTanque);
        var cuerpoDelTanque = tanque != null ? tanque.GetComponent<Rigidbody>() : null;
        if (cubo == null || cuerpoDelTanque == null) { Debug.LogError("ConstructorZombis: falta el cubo o el tanque"); return; }

        var vista = EditorSceneManager.NewPreviewScene();
        try
        {
            var go = DesdeElNormal(vista, "ZombiBlindado", enemigo, piel, RutaParticulasBlindado);
            if (go == null) return;
            var modelo = Modelo(go);
            modelo.localScale = Vector3.Scale(modelo.localScale, FormaDelBlindado);
            // Pesado como el tanque: no se aparta del jugador, el chaman se refugia detras y el bate lo frena.
            go.GetComponent<Rigidbody>().mass = cuerpoDelTanque.mass;

            var cabeza = Hueso(go.transform, "Bip001 Head");
            Renderer dibujoDeLaCabeza = null;
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                if (r.name.StartsWith(Disfraces.MallaDeLaCabeza)) dibujoDeLaCabeza = r;
            if (cabeza == null || dibujoDeLaCabeza == null) { Debug.LogError("ConstructorZombis: no encontre la cabeza del modelo"); return; }
            Bounds caja = dibujoDeLaCabeza.bounds;
            Vector3 adelante = go.transform.forward, arriba = Vector3.up;
            Quaternion derecho = Quaternion.LookRotation(adelante, arriba);

            // El casco: un cubo que tapa la mitad de arriba de la cabeza (hasta las cejas) y asoma 3 cm, con la visera
            // ambar abajo en el frente: la barra que se lee desde la camara.
            Vector3 casco = new Vector3(caja.size.x * 1.08f, 0.2f, caja.size.z * 1.02f);
            Vector3 centroDelCasco = new Vector3(caja.center.x, caja.max.y + 0.03f - casco.y * 0.5f, caja.center.z);
            Pieza(cabeza, "Casco", cubo, negro, centroDelCasco, derecho, casco);
            Pieza(cabeza, "Visera", cubo, ambar,
                  centroDelCasco + adelante * (casco.z * 0.5f + 0.005f) - arriba * (casco.y * 0.5f - 0.05f), derecho,
                  new Vector3(casco.x * 1.02f, 0.07f, 0.05f));

            // El escudo: un pivote hijo de la raiz a la altura del pecho, y la placa adelante. La guardia gira el pivote
            // (ZombiBlindado). De la raiz y no del torso: el modelo esta estirado distinto en cada eje, y una pieza que
            // gira colgada de un hueso asi se deforma; y la raiz no se balancea al correr.
            float arribaDeLaPlaca = caja.min.y + 0.07f;
            float centroDeLaPlaca = arribaDeLaPlaca - PlacaDelEscudo.y * 0.5f;
            var pivote = new GameObject("Escudo").transform;
            pivote.SetParent(go.transform, false);
            pivote.position = new Vector3(go.transform.position.x, centroDeLaPlaca, go.transform.position.z);
            pivote.rotation = derecho;
            Vector3 centro = pivote.position + adelante * EscudoAdelante;
            var placa = Pieza(pivote, "Placa", cubo, negro, centro, derecho, PlacaDelEscudo);
            Pieza(pivote, "Borde", cubo, ambar, centro + arriba * (PlacaDelEscudo.y * 0.5f) + adelante * 0.005f, derecho, BordeDelEscudo);
            Pieza(pivote, "Mirilla", cubo, ambar, centro + arriba * (PlacaDelEscudo.y * 0.2f) + adelante * 0.01f, derecho, MirillaDelEscudo);

            var blindado = go.GetComponent<ZombiBlindado>();
            if (blindado == null) blindado = go.AddComponent<ZombiBlindado>();
            blindado.pivote = pivote;
            blindado.placa = placa;
            blindado.colorAmbar = Ambar;

            PrefabUtility.SaveAsPrefabAsset(go, RutaBlindado);
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(vista);
        }
        AssetDatabase.SaveAssets();

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaBlindado);
        SumarALaMezcla(prefab, TramosDelBlindado, BlindadoPeso);
        Debug.Log("ConstructorZombis: el blindado, listo en " + RutaBlindado);
    }

    // Una pieza colgada de un hueso, puesta en el mundo con el modelo en reposo y con el tamaño en el mundo (sin la escala
    // que hereda del hueso), en la capa de los personajes y sin sombra.
    static Transform Pieza(Transform hueso, string nombre, Mesh malla, Material material, Vector3 donde, Quaternion giro, Vector3 tamano)
    {
        var go = new GameObject(nombre);
        go.AddComponent<MeshFilter>().sharedMesh = malla;
        var render = go.AddComponent<MeshRenderer>();
        render.sharedMaterial = material;
        render.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        render.receiveShadows = false;
        go.transform.SetParent(hueso, true);
        go.transform.position = donde;
        go.transform.rotation = giro;
        Vector3 heredada = hueso.lossyScale;
        go.transform.localScale = new Vector3(tamano.x / Mathf.Abs(heredada.x), tamano.y / Mathf.Abs(heredada.y), tamano.z / Mathf.Abs(heredada.z));
        go.layer = Personajes.Capa;
        return go.transform;
    }

    // --- Lo de todos ------------------------------------------------------------------------------

    // Un color plano sin luz, como el de los neones, pero con ShowBies/ColorPlano (el de las balas): Unlit/Color no tiene
    // variantes de instancing, y las piezas colgadas de los huesos (los sacos, los ojos, las crestas, el totem) y las
    // marcas del chaman eran una llamada de dibujo cada una, sin batching dinamico en Android (auditoria del 18/10).
    public const string ShaderPlanoConInstancing = "ShowBies/ColorPlano";

    static Material PlanoConInstancing(string nombre, Color color)
    {
        var mat = ConstructorEscenarios.MaterialPlano(CarpetaMateriales, nombre, color);
        var shader = Shader.Find(ShaderPlanoConInstancing);
        if (shader == null) { Debug.LogError("ConstructorZombis: falta el shader " + ShaderPlanoConInstancing); return mat; }
        mat.shader = shader;
        mat.color = color;
        mat.enableInstancing = true;
        EditorUtility.SetDirty(mat);
        return mat;
    }

    // La piel del normal (ShowBies/PielDeZombi) con otro color, y el borde de luz del mismo color aclarado un 35 % hacia
    // el blanco, como los demas (ConstructorHeroe).
    static Material Piel(string ruta, Color color)
    {
        var pielNormal = AssetDatabase.LoadAssetAtPath<Material>(RutaPielNormal);
        if (pielNormal == null) { Debug.LogError("ConstructorZombis: falta " + RutaPielNormal); return null; }
        var piel = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (piel == null)
        {
            piel = new Material(pielNormal);
            AssetDatabase.CreateAsset(piel, ruta);
        }
        piel.shader = pielNormal.shader;
        piel.CopyPropertiesFromMaterial(pielNormal);
        piel.SetColor("_Color", color);
        if (piel.HasProperty("_ColorBorde")) piel.SetColor("_ColorBorde", Color.Lerp(color, Color.white, 0.35f));
        EditorUtility.SetDirty(piel);
        return piel;
    }

    // Una copia suelta del normal (un cambio al normal no lo tiene que cambiar sin pasar por aca), con su Enemy, su piel,
    // sus particulas y el paso de lo que corre.
    static GameObject DesdeElNormal(UnityEngine.SceneManagement.Scene vista, string nombre, Enemy enemigo, Material piel, string particulas)
    {
        var normal = AssetDatabase.LoadAssetAtPath<GameObject>(RutaNormal);
        var pielNormal = AssetDatabase.LoadAssetAtPath<Material>(RutaPielNormal);
        if (normal == null || pielNormal == null) { Debug.LogError("ConstructorZombis: falta " + RutaNormal); return null; }
        var go = (GameObject)PrefabUtility.InstantiatePrefab(normal, vista);
        PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
        go.name = nombre;

        var zombi = go.GetComponent<EnemyController>();
        var so = new SerializedObject(zombi);
        so.FindProperty("enemyType").objectReferenceValue = enemigo;
        so.FindProperty("deathParticles").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ParticleSystem>(particulas);
        so.FindProperty("ritmoDeAndar").floatValue = 1f;
        // El normal va a 5 con paso 1 y la misma escala.
        so.FindProperty("velocidadDeAnimacion").floatValue = enemigo.velocidad / 5f;
        so.ApplyModifiedPropertiesWithoutUndo();

        foreach (var render in go.GetComponentsInChildren<Renderer>(true))
        {
            var materiales = render.sharedMaterials;
            bool cambio = false;
            for (int i = 0; i < materiales.Length; i++)
            {
                if (materiales[i] != pielNormal) continue;
                materiales[i] = piel;
                cambio = true;
            }
            if (cambio) render.sharedMaterials = materiales;
        }
        return go;
    }

    // El hijo que se ve: el del Animator con controller (como EnemyController.BuscarElModelo).
    static Transform Modelo(GameObject go)
    {
        foreach (var animador in go.GetComponentsInChildren<Animator>(true))
            if (animador.transform != go.transform && animador.runtimeAnimatorController != null) return animador.transform;
        return go.transform;
    }

    static Transform Hueso(Transform raiz, string nombre)
    {
        foreach (var t in raiz.GetComponentsInChildren<Transform>(true))
            if (t.name == nombre) return t;
        return null;
    }

    // Lo pone en los tipos del WaveManager de WaveMode, una entrada por tramo de oleadas (x desde, y hasta; 0 sin fin: un
    // tipo que descansa un capitulo va dos veces), sacando las que tenia, y guarda la escena. Se niega si la escena tiene
    // cambios sin guardar: no los guarda por accidente.
    static void SumarALaMezcla(GameObject prefab, Vector2Int[] tramos, float peso, int maxVivos)
    {
        var conTope = new Vector3Int[tramos.Length];
        for (int i = 0; i < tramos.Length; i++) conTope[i] = new Vector3Int(tramos[i].x, tramos[i].y, maxVivos);
        SumarALaMezcla(prefab, conTope, peso);
    }

    // Lo mismo, con un tope de vivos por tramo (z).
    static void SumarALaMezcla(GameObject prefab, Vector3Int[] tramos, float peso)
    {
        var escena = EditorSceneManager.GetSceneByPath(EscenaOleadas);
        bool abierta = escena.isLoaded;
        if (abierta && escena.isDirty) { Debug.LogError("ConstructorZombis: WaveMode tiene cambios sin guardar; guardala o descartalos y volve a correrlo"); return; }
        if (!abierta) escena = EditorSceneManager.OpenScene(EscenaOleadas, OpenSceneMode.Additive);
        try
        {
            WaveManager oleadas = null;
            foreach (var raiz in escena.GetRootGameObjects())
            {
                oleadas = raiz.GetComponentInChildren<WaveManager>(true);
                if (oleadas != null) break;
            }
            if (oleadas == null) { Debug.LogError("ConstructorZombis: WaveMode no tiene WaveManager"); return; }
            var so = new SerializedObject(oleadas);
            var tipos = so.FindProperty("tipos");
            for (int i = tipos.arraySize - 1; i >= 0; i--)
                if (tipos.GetArrayElementAtIndex(i).FindPropertyRelative("prefab").objectReferenceValue == prefab) tipos.DeleteArrayElementAtIndex(i);
            foreach (var tramo in tramos)
            {
                tipos.arraySize++;
                var tipo = tipos.GetArrayElementAtIndex(tipos.arraySize - 1);
                tipo.FindPropertyRelative("prefab").objectReferenceValue = prefab;
                tipo.FindPropertyRelative("desdeOleada").intValue = tramo.x;
                tipo.FindPropertyRelative("hastaOleada").intValue = tramo.y;
                tipo.FindPropertyRelative("peso").floatValue = peso;
                tipo.FindPropertyRelative("maxVivos").intValue = tramo.z;
                tipo.FindPropertyRelative("especial").boolValue = true;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
        }
        finally
        {
            if (!abierta) EditorSceneManager.CloseScene(escena, true);
        }
    }
}
