using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Arma los zombis con habilidad (ZOMBIS.md, de Argon): su Enemy, la piel, las piezas que los distinguen desde arriba y
// el prefab, que sale del zombi normal (el mismo modelo del pack, sin clips nuevos), y los suma a la mezcla del
// WaveManager de WaveMode con su oleada, su peso y su tope. Hoy, el escupidor.
//
// El Enemy se crea una sola vez: despues es balance y se toca en el asset, como los otros. Lo demas se rehace cada vez.
// No se edita a mano: se vuelve a correr ShowBies > Zombis > Armar el escupidor.
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
        var acido = ConstructorEscenarios.MaterialPlano(CarpetaMateriales, "AcidoEscupidor", AmarilloAcido);
        var sacoMaterial = ConstructorEscenarios.MaterialPlano(CarpetaMateriales, "SacoEscupidor", ColorDeLosSacos);
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
        SumarALaMezcla(prefab, new[] { new Vector2Int(EscupidorDesde, 0) }, EscupidorPeso, EscupidorMaxVivos);
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
        var crema = ConstructorEscenarios.MaterialPlano(CarpetaMateriales, "CremaSaltador", CremaDelSaltador);
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
                tipo.FindPropertyRelative("maxVivos").intValue = maxVivos;
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
