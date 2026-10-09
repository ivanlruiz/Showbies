using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Arma el zombi del tesoro (ZombiDelTesoro, mejora 8 de la revision del 9/10): su Enemy, la piel
// dorada (la del normal, ShowBies/PielDeZombi, con mas brillo propio), los materiales de su
// brillo (los del charco de los faroles, como las cajas) y el prefab, que sale del zombi normal:
// mas chico, con la piel dorada, las particulas amarillas al morir, el paso de lo que corre y
// el componente que lo hace huir. El prefab va en Prefabs/Personajes/Resources: WaveManager lo
// carga solo y la escena no lo cablea.
//
// El Enemy se crea una sola vez: despues es balance (vida, velocidad, monedas) y se toca en el
// asset, como los otros cinco. Lo demas se rehace cada vez. No se edita a mano: se vuelve a
// correr ShowBies > Zombis > Armar el zombi del tesoro.
public static class ConstructorTesoro
{
    public const string RutaPrefab = "Assets/Prefabs/Personajes/Resources/ZombiTesoro.prefab";
    public const string RutaEnemigo = "Assets/Zombies/ZombiTesoro.asset";
    public const string RutaPiel = "Assets/Materiales/ZombiTesoroPiel.mat";
    const string RutaNormal = "Assets/Prefabs/Personajes/Zombi.prefab";
    const string RutaPielNormal = "Assets/Materiales/ZombiNormalPiel.mat";
    const string RutaParticulas = "Assets/Prefabs/Particulas/ExplosionAmarilla.prefab";
    const string CarpetaMateriales = "Assets/Materiales";

    public static readonly Color Dorado = new Color(1f, 0.82f, 0.25f);
    public const float Escala = 0.42f;          // el normal mide 0.5: un duende, mas chico
    public const float BrilloDeLaPiel = 0.8f;   // el de los otros es 0,5

    [MenuItem("ShowBies/Zombis/Armar el zombi del tesoro")]
    public static void Armar()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogError("ConstructorTesoro: no en play"); return; }

        // El Enemy: solo si no esta (despues es balance).
        var enemigo = AssetDatabase.LoadAssetAtPath<Enemy>(RutaEnemigo);
        if (enemigo == null)
        {
            enemigo = ScriptableObject.CreateInstance<Enemy>();
            enemigo.hp = 15;
            enemigo.daño = 0;
            enemigo.velocidad = 7f;
            enemigo.puntos = 15;
            enemigo.monedasMin = 14;
            enemigo.monedasMax = 20;
            AssetDatabase.CreateAsset(enemigo, RutaEnemigo);
        }

        // La piel: la del normal, dorada y con mas brillo.
        var pielNormal = AssetDatabase.LoadAssetAtPath<Material>(RutaPielNormal);
        if (pielNormal == null) { Debug.LogError("ConstructorTesoro: falta " + RutaPielNormal); return; }
        var piel = AssetDatabase.LoadAssetAtPath<Material>(RutaPiel);
        if (piel == null)
        {
            piel = new Material(pielNormal);
            AssetDatabase.CreateAsset(piel, RutaPiel);
        }
        piel.shader = pielNormal.shader;
        piel.CopyPropertiesFromMaterial(pielNormal);
        piel.SetColor("_Color", Dorado);
        if (piel.HasProperty("_Brillo")) piel.SetFloat("_Brillo", BrilloDeLaPiel);
        EditorUtility.SetDirty(piel);

        // El brillo: un halo y un charco, como los de las cajas.
        var halo = ConstructorEscenarios.MaterialDeBrillo(CarpetaMateriales, "TesoroHalo", Dorado, 1.5f, 0.3f, 3.5f);
        var charco = ConstructorEscenarios.MaterialDeBrillo(CarpetaMateriales, "TesoroCharco", Dorado, 1f, 0.35f, 4.5f);

        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Personajes/Resources"))
            AssetDatabase.CreateFolder("Assets/Prefabs/Personajes", "Resources");

        var normal = AssetDatabase.LoadAssetAtPath<GameObject>(RutaNormal);
        if (normal == null) { Debug.LogError("ConstructorTesoro: falta " + RutaNormal); return; }
        var vista = EditorSceneManager.NewPreviewScene();
        try
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(normal, vista);
            // Suelto del normal (el modelo sigue siendo el del pack): un cambio al normal no lo
            // tiene que cambiar sin pasar por aca.
            PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
            go.name = "ZombiTesoro";
            go.transform.localScale = Vector3.one * Escala;

            var zombi = go.GetComponent<EnemyController>();
            var so = new SerializedObject(zombi);
            so.FindProperty("enemyType").objectReferenceValue = enemigo;
            so.FindProperty("deathParticles").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ParticleSystem>(RutaParticulas);
            // Corre (ritmo 1) con el paso de lo que avanza: el normal va a 5 con paso 1 y escala 0,5.
            so.FindProperty("ritmoDeAndar").floatValue = 1f;
            so.FindProperty("velocidadDeAnimacion").floatValue = enemigo.velocidad / 5f * (0.5f / Escala);
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
                if (!cambio) continue;
                render.sharedMaterials = materiales;
                PrefabUtility.RecordPrefabInstancePropertyModifications(render);
            }

            var tesoro = go.GetComponent<ZombiDelTesoro>();
            if (tesoro == null) tesoro = go.AddComponent<ZombiDelTesoro>();
            tesoro.materialHalo = halo;
            tesoro.materialCharco = charco;
            tesoro.color = Dorado;

            PrefabUtility.SaveAsPrefabAsset(go, RutaPrefab);
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(vista);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("ConstructorTesoro: listo en " + RutaPrefab);
    }
}
