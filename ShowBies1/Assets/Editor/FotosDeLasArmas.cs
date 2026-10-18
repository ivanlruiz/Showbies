using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Las cinco armas de la mano (ArmaEnLaMano, etapa 3 de las cartas), disparando, para elegirlas y
// para ver que se distingan: de izquierda a derecha la pistola, el revolver, la escopeta, el
// rifle y la de energia. Sin play y sin tocar la escena abierta. Escribe en Builds/armas/:
//
//   cerca.png          las cinco de cerca, sobre un fondo oscuro: el cañon, la luz y el fogonazo.
//   <capitulo>.png     el heroe con cada una, de noche, con la camara y la calidad del telefono
//                      (FotosDeLaHorda.ArmarNoche), como se ve jugando.
//   <capitulo>_x2.png  el centro de la anterior, al doble, para ver los pixeles.
public static class FotosDeLasArmas
{
    const string Escena = "Assets/Escenas/WaveMode.unity";
    const string Carpeta = "../Builds/armas";
    const int Ancho = 1920, Alto = 1080;
    const float EscalaDelModelo = 0.6f;   // 1,2 dentro del jugador, que esta a 0,5
    const float Separacion = 2.4f;
    // Hacia donde miran los heroes: de costado a la camara, que el cañon se vea a lo largo.
    const float Rumbo = 90f;

    struct Pose
    {
        public GameObject objeto;
        public AnimationClip clip;
        public float momento;
    }

    static readonly List<Pose> poses = new List<Pose>();
    static readonly List<Transform> pistolas = new List<Transform>();

    [MenuItem("ShowBies/Armas/Fotos de las armas")]
    public static void Sacar()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogError("FotosDeLasArmas: no en play"); return; }
        Scene waveMode = SceneManager.GetSceneByPath(Escena);
        bool abriYo = false;
        if (!waveMode.isLoaded)
        {
            waveMode = EditorSceneManager.OpenScene(Escena, OpenSceneMode.Additive);
            abriYo = true;
        }
        int calidadDelEditor = QualitySettings.GetQualityLevel();
        var rt = new RenderTexture(Ancho, Alto, 24);
        try
        {
            CapitulosDeEscenario capitulos = null;
            Camera camaraDelJuego = null;
            Light relleno = null;
            foreach (var raiz in waveMode.GetRootGameObjects())
            {
                if (capitulos == null) capitulos = raiz.GetComponentInChildren<CapitulosDeEscenario>(true);
                foreach (var c in raiz.GetComponentsInChildren<Camera>(true))
                    if (c.CompareTag("MainCamera")) camaraDelJuego = c;
                if (raiz.name == "Relleno") relleno = raiz.GetComponent<Light>();
            }
            if (capitulos == null || camaraDelJuego == null || relleno == null) { Debug.LogError("FotosDeLasArmas: a WaveMode le falta algo"); return; }
            int telefono = CalidadDeAndroid.Guardada() >= 0 ? CalidadDeAndroid.Guardada() : CalidadDeAndroid.Medium;
            QualitySettings.SetQualityLevel(telefono, false);
            Directory.CreateDirectory(Path.GetFullPath(Carpeta));

            DeCerca(rt);
            for (int i = 0; i < capitulos.escenarios.Length; i++)
            {
                var escenario = capitulos.escenarios[i];
                if (escenario.decorado == null) continue;
                if (i == 0) escenario = FotosDeLaHorda.LoQueTraeLaEscena(escenario, capitulos, camaraDelJuego, waveMode);
                EnLaPartida(escenario, camaraDelJuego, relleno, rt);
            }
        }
        finally
        {
            if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
            QualitySettings.SetQualityLevel(calidadDelEditor, false);
            rt.Release();
            Object.DestroyImmediate(rt);
            if (abriYo) EditorSceneManager.CloseScene(waveMode, true);
        }
        Debug.Log("FotosDeLasArmas: listas en " + Path.GetFullPath(Carpeta));
    }

    // Las cinco en fila, de costado y un poco desde arriba, disparando.
    static void DeCerca(RenderTexture rt)
    {
        var escena = EditorSceneManager.NewPreviewScene();
        try
        {
            var luz = new GameObject("Luz").AddComponent<Light>();
            luz.type = LightType.Directional;
            luz.intensity = 1.1f;
            luz.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            SceneManager.MoveGameObjectToScene(luz.gameObject, escena);

            var camara = new GameObject("Camara").AddComponent<Camera>();
            SceneManager.MoveGameObjectToScene(camara.gameObject, escena);
            camara.scene = escena;
            camara.clearFlags = CameraClearFlags.SolidColor;
            camara.backgroundColor = new Color(0.07f, 0.07f, 0.1f);
            camara.fieldOfView = 30f;
            camara.allowHDR = false;
            camara.allowMSAA = false;
            camara.targetTexture = rt;
            camara.aspect = (float)rt.width / rt.height;
            camara.enabled = false;
            // Mirando al medio de la fila, desde lejos: desde el 18/10 cada arma tiene su forma y las mas
            // grandes (la escopeta, el rifle, la de energia) se pisaban con la separacion de antes.
            var giro = Quaternion.Euler(22f, 0f, 0f);
            camara.transform.SetPositionAndRotation(new Vector3(-0.05f, -0.22f, 0f) - giro * Vector3.forward * 4.3f, giro);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ConstructorArmas.RutaPistola);
            for (int tramo = 0; tramo < ArmaEnLaMano.Armas; tramo++)
            {
                var p = ((GameObject)PrefabUtility.InstantiatePrefab(prefab, escena)).transform;
                // El cañon hacia la derecha de la foto, y en fila de arriba abajo.
                p.SetPositionAndRotation(new Vector3(-0.45f, 0.62f - tramo * 0.42f, 0f), Quaternion.Euler(0f, 90f, 0f));
                p.localScale = Vector3.one * 0.9f;
                ArmaEnLaMano.VestirArma(p, tramo, new MaterialPropertyBlock());
                Disparando(p, camara.transform, tramo, 0.5f, 0.3f);
            }

            var foto = FotosDeLaHorda.Foto(camara, rt);
            File.WriteAllBytes(Path.GetFullPath(Carpeta + "/cerca.png"), foto.EncodeToPNG());
            Object.DestroyImmediate(foto);
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(escena);
        }
    }

    static void EnLaPartida(EscenarioDeCapitulo escenario, Camera camaraDelJuego, Light rellenoDeLaEscena, RenderTexture rt)
    {
        var escena = EditorSceneManager.NewPreviewScene();
        try
        {
            Light relleno;
            var camara = FotosDeLaHorda.ArmarNoche(escena, escenario, camaraDelJuego, rellenoDeLaEscena, rt, out relleno);
            poses.Clear();
            pistolas.Clear();
            for (int tramo = 0; tramo < ArmaEnLaMano.Armas; tramo++)
                Heroe(escena, new Vector3((tramo - (ArmaEnLaMano.Armas - 1) * 0.5f) * Separacion, 0f, -1.2f), tramo);

            AnimationMode.StartAnimationMode();
            AnimationMode.BeginSampling();
            foreach (var p in poses)
                if (p.clip != null) AnimationMode.SampleAnimationClip(p.objeto, p.clip, p.momento * p.clip.length);
            AnimationMode.EndSampling();
            // Despues de la pose, que mueve la mano: el fogonazo sale de donde quedo la boca.
            for (int tramo = 0; tramo < pistolas.Count; tramo++)
                Disparando(pistolas[tramo], camara.transform, tramo, 0.5f, LargoDelFogonazo);

            FotosDeLaHorda.PonerLaLuz(escena, escenario);
            try
            {
                var foto = FotosDeLaHorda.Foto(camara, rt);
                File.WriteAllBytes(Path.GetFullPath(Carpeta + "/" + escenario.idTexto + ".png"), foto.EncodeToPNG());
                var cerca = AlDoble(foto);
                File.WriteAllBytes(Path.GetFullPath(Carpeta + "/" + escenario.idTexto + "_x2.png"), cerca.EncodeToPNG());
                Object.DestroyImmediate(foto);
                Object.DestroyImmediate(cerca);
            }
            finally
            {
                Unsupported.RestoreOverrideLightingSettings();
                AnimationMode.StopAnimationMode();
            }
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(escena);
        }
    }

    // El heroe de la partida (la remera blanca de AspectoDelHeroe) con la pistola del tramo en la
    // mano, en la pose de disparar.
    static void Heroe(Scene escena, Vector3 donde, int tramo)
    {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ToonyTinyPeople/TT_demo/models/TT_demo_male_A.FBX");
        if (asset == null) return;
        // El clip pisa el giro de la raiz al muestrearlo: lo que gira es un pivote.
        var pivote = new GameObject("Heroe");
        SceneManager.MoveGameObjectToScene(pivote, escena);
        pivote.transform.SetPositionAndRotation(donde, Quaternion.Euler(0f, Rumbo, 0f));
        pivote.transform.localScale = Vector3.one * EscalaDelModelo;
        var modelo = (GameObject)PrefabUtility.InstantiatePrefab(asset, escena);
        modelo.transform.SetParent(pivote.transform, false);
        modelo.transform.localPosition = Vector3.zero;
        modelo.transform.localRotation = Quaternion.identity;
        Personajes.PonerEnLaCapa(modelo);
        var remera = Resources.Load<Material>("HeroeRemeraBlanca");
        foreach (var r in modelo.GetComponentsInChildren<Renderer>(true))
        {
            if (r.name.Contains("baseballbat")) { r.enabled = false; continue; }
            if (remera != null && r.sharedMaterial != null && r.sharedMaterial.name == "TT_demo") r.sharedMaterial = remera;
        }
        var animador = modelo.GetComponent<Animator>();
        if (animador == null) return;
        animador.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        poses.Add(new Pose { objeto = animador.gameObject, clip = Clip("m_pistol_shoot"), momento = 0.35f });
        var mano = animador.isHuman ? animador.GetBoneTransform(HumanBodyBones.RightHand) : null;
        var contenedor = mano != null ? mano.Find("R_hand_container") : null;
        if (contenedor == null) contenedor = mano;
        var pistola = AssetDatabase.LoadAssetAtPath<GameObject>(ConstructorArmas.RutaPistola);
        if (contenedor == null || pistola == null) return;
        foreach (Transform hijo in contenedor) hijo.gameObject.SetActive(false);
        var p = (GameObject)PrefabUtility.InstantiatePrefab(pistola, escena);
        p.transform.SetParent(contenedor, false);
        p.transform.localPosition = Vector3.zero;
        p.transform.localRotation = ArmaEnLaMano.GiroEnLaMano;
        p.transform.localScale = Vector3.one;
        Personajes.PonerEnLaCapa(p);
        ArmaEnLaMano.VestirArma(p.transform, tramo, new MaterialPropertyBlock());
        pistolas.Add(p.transform);
    }

    // El largo del fogonazo de la pistola en la partida (ArmaEnLaMano.largoDelFogonazo, del
    // componente del jugador).
    const float LargoDelFogonazo = 0.65f;

    // Prende el fogonazo como en un tiro, a mitad de su vida (el anillo de la de energia, a medio
    // crecer). De cerca va mas chico: en la partida mide metros y la pistola, centimetros.
    static void Disparando(Transform pistola, Transform camara, int tramo, float t, float metros)
    {
        var boca = pistola.Find("Boca");
        var fogonazo = boca != null ? boca.Find("Fogonazo") : null;
        if (fogonazo == null) return;
        fogonazo.gameObject.SetActive(true);
        ArmaEnLaMano.AcomodarFogonazo(fogonazo, boca, camara, tramo, metros);
        float giro = ArmaEnLaMano.GiroPorArma[tramo] >= 0f ? ArmaEnLaMano.GiroPorArma[tramo] : 0.4f;
        ArmaEnLaMano.PintarFogonazo(fogonazo.GetComponent<Renderer>(), new MaterialPropertyBlock(), tramo, giro, t);
    }

    // El centro de la foto, al doble y sin suavizar.
    static Texture2D AlDoble(Texture2D foto)
    {
        int w = foto.width / 2, h = foto.height / 2;
        int x0 = (foto.width - w) / 2, y0 = (foto.height - h) / 2;
        var resultado = new Texture2D(w * 2, h * 2, TextureFormat.RGB24, false);
        var origen = foto.GetPixels(x0, y0, w, h);
        var destino = new Color[w * 2 * h * 2];
        for (int y = 0; y < h * 2; y++)
            for (int x = 0; x < w * 2; x++)
                destino[y * w * 2 + x] = origen[(y / 2) * w + x / 2];
        resultado.SetPixels(destino);
        resultado.Apply();
        return resultado;
    }

    static AnimationClip Clip(string nombre)
    {
        foreach (var guid in AssetDatabase.FindAssets(nombre + " t:AnimationClip"))
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(guid)))
            {
                var clip = o as AnimationClip;
                if (clip != null && clip.name == nombre) return clip;
            }
        return null;
    }
}
