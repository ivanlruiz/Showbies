using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Fotos de los capitulos de noche, con la calidad del telefono y con la del editor, y cuanto
// alumbra cada farol el piso en cada una. Sin play y sin tocar la escena abierta: arma cada
// escenario en una escena de vista previa, con el piso, la luna, la luz ambiente y la niebla
// que le pone CapitulosDeEscenario, y la camara del juego.
//
// Existe porque hasta el 23/9 los faroles alumbraban el piso solo en el editor: en el
// telefono, con la calidad Medium, las luces puntuales caian a luz por vertice y el piso
// quedaba oscuro, y nada lo avisaba (ver el charco de luz en ConstructorEscenarios).
//
// Cuanto alumbra un farol es lo que sube el brillo del piso a metro y medio de su pie, hacia
// la camara, entre la foto con los faroles prendidos y la foto con todos apagados (luces y
// charcos). Escribe Builds/prueba_faroles.txt y las fotos en Builds/faroles_*.png.
public static class FotosDeLosFaroles
{
    const string Ruta = "../Builds/prueba_faroles.txt";
    const string Escena = "Assets/Escenas/WaveMode.unity";
    const int Ancho = 1280, Alto = 720;

    // Lo minimo que tiene que subir el brillo del piso bajo cada farol en el telefono, y la
    // parte de lo que se ve en el editor que tiene que llegar al telefono. Con las luces por
    // pixel de antes: en el editor +0,08 en el cementerio y +0,37 en la ciudad, y en el
    // telefono +0,005 y +0,06.
    const float MinimoEnElTelefono = 0.06f;
    const float ParteDelEditor = 0.6f;

    [MenuItem("ShowBies/Escenarios/Fotos de los faroles")]
    public static void Sacar()
    {
        var inf = new StringBuilder();
        inf.AppendLine("Los faroles de noche, con la calidad del telefono y con la del editor");
        inf.AppendLine();

        Scene waveMode = SceneManager.GetSceneByPath(Escena);
        bool abriYo = false;
        if (!waveMode.isLoaded)
        {
            waveMode = EditorSceneManager.OpenScene(Escena, OpenSceneMode.Additive);
            abriYo = true;
        }

        int calidadDelEditor = QualitySettings.GetQualityLevel();
        int calidadDelTelefono = CalidadDeAndroid();
        var rt = new RenderTexture(Ancho, Alto, 24);
        bool todo = true;
        try
        {
            CapitulosDeEscenario capitulos = null;
            Camera camaraDelJuego = null;
            foreach (var raiz in waveMode.GetRootGameObjects())
            {
                if (capitulos == null) capitulos = raiz.GetComponentInChildren<CapitulosDeEscenario>(true);
                foreach (var c in raiz.GetComponentsInChildren<Camera>(true))
                    if (c.CompareTag("MainCamera")) camaraDelJuego = c;
            }
            if (capitulos == null || camaraDelJuego == null)
            {
                inf.AppendLine("ERROR: WaveMode no tiene CapitulosDeEscenario o camara principal");
                todo = false;
            }
            else
            {
                inf.AppendLine("Calidad del telefono: " + QualitySettings.names[calidadDelTelefono] + "; del editor: " + QualitySettings.names[calidadDelEditor]);
                for (int i = 0; i < capitulos.escenarios.Length; i++)
                {
                    var escenario = capitulos.escenarios[i];
                    if (escenario.decorado == null) continue;
                    if (i == 0) escenario = LoQueTraeLaEscena(escenario, capitulos, camaraDelJuego, waveMode);
                    todo &= Fotografiar(escenario, camaraDelJuego, calidadDelTelefono, calidadDelEditor, rt, inf);
                }
            }
        }
        finally
        {
            QualitySettings.SetQualityLevel(calidadDelEditor, false);
            rt.Release();
            Object.DestroyImmediate(rt);
            if (abriYo) EditorSceneManager.CloseScene(waveMode, true);
        }

        inf.AppendLine();
        inf.AppendLine("RESULTADO: " + (todo ? "TODO OK" : "HAY FALLAS"));
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(Ruta)));
        File.WriteAllText(Ruta, inf.ToString());
        Debug.Log(inf.ToString());
    }

    // El primer escenario es "lo que trae la escena" (la pradera): CapitulosDeEscenario lee al
    // empezar su piso, su cielo, su luna, su luz ambiente y su niebla de WaveMode, y en la lista
    // quedan los valores de fabrica, sin piso. Aca se leen igual: sin esto la pradera salia con
    // el piso rosa de "sin material" y de dia.
    static EscenarioDeCapitulo LoQueTraeLaEscena(EscenarioDeCapitulo primero, CapitulosDeEscenario capitulos, Camera camara, Scene escena)
    {
        var e = new EscenarioDeCapitulo { idTexto = primero.idTexto, decorado = primero.decorado };
        e.piso = capitulos.piso != null ? capitulos.piso.sharedMaterial : primero.piso;
        e.cielo = camara.backgroundColor;
        if (capitulos.sol != null)
        {
            e.luz = capitulos.sol.color;
            e.intensidadLuz = capitulos.sol.intensity;
            e.rotacionLuz = capitulos.sol.transform.rotation.eulerAngles;
        }
        // La luz ambiente y la niebla son de cada escena: se leen con ella activa.
        var activa = SceneManager.GetActiveScene();
        SceneManager.SetActiveScene(escena);
        e.ambiente = RenderSettings.ambientLight;
        e.conNiebla = RenderSettings.fog;
        e.nieblaInicio = RenderSettings.fogStartDistance;
        e.nieblaFin = RenderSettings.fogEndDistance;
        SceneManager.SetActiveScene(activa);
        return e;
    }

    static bool Fotografiar(EscenarioDeCapitulo escenario, Camera camaraDelJuego, int calidadDelTelefono, int calidadDelEditor,
                            RenderTexture rt, StringBuilder inf)
    {
        var escena = EditorSceneManager.NewPreviewScene();
        try
        {
            // El piso del juego: el plano de 100 m con el piso del capitulo.
            var piso = GameObject.CreatePrimitive(PrimitiveType.Plane);
            piso.transform.localScale = new Vector3(10f, 1f, 10f);
            piso.GetComponent<Renderer>().sharedMaterial = escenario.piso;
            SceneManager.MoveGameObjectToScene(piso, escena);

            var luna = new GameObject("Luna").AddComponent<Light>();
            luna.type = LightType.Directional;
            luna.color = escenario.luz;
            luna.intensity = escenario.intensidadLuz;
            luna.transform.rotation = Quaternion.Euler(escenario.rotacionLuz);
            luna.shadows = LightShadows.Hard;
            SceneManager.MoveGameObjectToScene(luna.gameObject, escena);

            var decorado = Object.Instantiate(escenario.decorado);
            SceneManager.MoveGameObjectToScene(decorado, escena);

            // Cada farol, con lo que lo hace alumbrar: sus luces y su charco.
            var faroles = new List<Transform>();
            var lucesYCharcos = new List<Behaviour>();
            var charcos = new List<Renderer>();
            foreach (Transform t in decorado.GetComponentsInChildren<Transform>(true))
            {
                if (t.name != "Farol") continue;
                faroles.Add(t);
                foreach (var luz in t.GetComponentsInChildren<Light>(true)) lucesYCharcos.Add(luz);
                foreach (var render in t.GetComponentsInChildren<Renderer>(true))
                {
                    var material = render.sharedMaterial;
                    if (material != null && material.shader != null && material.shader.name == "ShowBies/CharcoDeLuz") charcos.Add(render);
                }
            }

            var camara = new GameObject("Camara").AddComponent<Camera>();
            SceneManager.MoveGameObjectToScene(camara.gameObject, escena);
            camara.scene = escena;
            camara.transform.SetPositionAndRotation(camaraDelJuego.transform.position, camaraDelJuego.transform.rotation);
            camara.fieldOfView = camaraDelJuego.fieldOfView;
            camara.nearClipPlane = camaraDelJuego.nearClipPlane;
            camara.farClipPlane = camaraDelJuego.farClipPlane;
            camara.clearFlags = CameraClearFlags.SolidColor;
            camara.backgroundColor = escenario.cielo;
            camara.allowHDR = false;
            camara.allowMSAA = false;
            camara.renderingPath = RenderingPath.Forward;
            camara.targetTexture = rt;
            camara.aspect = (float)Ancho / Alto;
            camara.enabled = false;

            // La luz ambiente y la niebla van a la escena de vista previa, no a la abierta.
            Unsupported.SetOverrideLightingSettings(escena);
            try
            {
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.ambientLight = escenario.ambiente;
                RenderSettings.fog = escenario.conNiebla;
                RenderSettings.fogMode = FogMode.Linear;
                RenderSettings.fogColor = escenario.cielo;
                RenderSettings.fogStartDistance = escenario.nieblaInicio;
                RenderSettings.fogEndDistance = escenario.nieblaFin;

                float[] enElTelefono = Medir(calidadDelTelefono, "telefono", escenario, camara, rt, faroles, lucesYCharcos, charcos);
                float[] enElEditor = Medir(calidadDelEditor, "editor", escenario, camara, rt, faroles, lucesYCharcos, charcos);

                bool alumbra = true, igual = true;
                int vistos = 0;
                var linea = new StringBuilder();
                for (int i = 0; i < faroles.Count; i++)
                {
                    if (float.IsNaN(enElTelefono[i])) continue;   // fuera de cuadro
                    vistos++;
                    linea.Append(" [" + faroles[i].position.x.ToString("0") + "," + faroles[i].position.z.ToString("0") + ": telefono +"
                                 + enElTelefono[i].ToString("0.000") + ", editor +" + enElEditor[i].ToString("0.000") + "]");
                    if (enElTelefono[i] < MinimoEnElTelefono) alumbra = false;
                    if (enElTelefono[i] < enElEditor[i] * ParteDelEditor) igual = false;
                }
                inf.AppendLine();
                inf.AppendLine(escenario.idTexto + ": " + faroles.Count + " faroles, " + vistos + " en cuadro;" + linea);
                bool ok = vistos > 0 && alumbra && igual;
                inf.AppendLine((vistos > 0 && alumbra ? "OK  " : "FALLA  ") + escenario.idTexto + ": en el telefono cada farol sube el brillo del piso al menos +" + MinimoEnElTelefono);
                inf.AppendLine((igual ? "OK  " : "FALLA  ") + escenario.idTexto + ": el telefono ve al menos el " + (ParteDelEditor * 100f).ToString("0") + " % de lo que se ve en el editor");
                return ok;
            }
            finally
            {
                Unsupported.RestoreOverrideLightingSettings();
            }
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(escena);
        }
    }

    // Cuanto sube el brillo del piso cerca de cada farol con esa calidad (NaN si queda fuera
    // de cuadro), y la foto con los faroles prendidos.
    static float[] Medir(int calidad, string cual, EscenarioDeCapitulo escenario, Camera camara, RenderTexture rt,
                         List<Transform> faroles, List<Behaviour> luces, List<Renderer> charcos)
    {
        QualitySettings.SetQualityLevel(calidad, false);
        Prender(luces, charcos, true);
        var con = Foto(camara, rt);
        Prender(luces, charcos, false);
        var sin = Foto(camara, rt);
        Prender(luces, charcos, true);
        File.WriteAllBytes(Path.GetFullPath("../Builds/faroles_" + escenario.idTexto + "_" + cual + ".png"), con.EncodeToPNG());

        var medidas = new float[faroles.Count];
        for (int i = 0; i < faroles.Count; i++)
        {
            Vector3 pie = PieDeLaLuz(faroles[i]) - new Vector3(0f, 0f, 1.5f);
            Vector3 enPantalla = camara.WorldToScreenPoint(pie);
            bool fuera = enPantalla.z <= 0f || enPantalla.x < 10f || enPantalla.y < 10f || enPantalla.x > Ancho - 10f || enPantalla.y > Alto - 10f;
            medidas[i] = fuera ? float.NaN : Brillo(con, enPantalla) - Brillo(sin, enPantalla);
        }
        Object.DestroyImmediate(con);
        Object.DestroyImmediate(sin);
        return medidas;
    }

    // Donde da la luz del farol en el piso: debajo de su luz o, si no tiene, de su charco.
    static Vector3 PieDeLaLuz(Transform farol)
    {
        var luz = farol.GetComponentInChildren<Light>(true);
        Vector3 p = luz != null ? luz.transform.position : farol.position;
        if (luz == null)
        {
            foreach (var render in farol.GetComponentsInChildren<Renderer>(true))
                if (render.name == "Charco") p = render.transform.position;
        }
        return new Vector3(p.x, 0f, p.z);
    }

    static void Prender(List<Behaviour> luces, List<Renderer> charcos, bool prendidos)
    {
        foreach (var luz in luces) luz.enabled = prendidos;
        foreach (var charco in charcos) charco.enabled = prendidos;
    }

    static Texture2D Foto(Camera camara, RenderTexture rt)
    {
        camara.Render();
        var antes = RenderTexture.active;
        RenderTexture.active = rt;
        var foto = new Texture2D(Ancho, Alto, TextureFormat.RGB24, false);
        foto.ReadPixels(new Rect(0, 0, Ancho, Alto), 0, 0);
        foto.Apply();
        RenderTexture.active = antes;
        return foto;
    }

    // El brillo percibido, promediado en un cuadrado de 9 x 9 pixeles.
    static float Brillo(Texture2D foto, Vector3 centro)
    {
        float suma = 0f;
        int n = 0;
        for (int dx = -4; dx <= 4; dx++)
        {
            for (int dy = -4; dy <= 4; dy++)
            {
                Color c = foto.GetPixel(Mathf.Clamp((int)centro.x + dx, 0, Ancho - 1), Mathf.Clamp((int)centro.y + dy, 0, Alto - 1));
                suma += 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b;
                n++;
            }
        }
        return suma / n;
    }

    // El nivel de calidad con el que sale la build de Android (CalidadDeAndroid). Si falta
    // (entrar y salir de play lo borra, ver la trampa en CLAUDE.md), el que se llama Medium.
    static int CalidadDeAndroid()
    {
        int guardada = global::CalidadDeAndroid.Guardada();
        return guardada >= 0 ? guardada : global::CalidadDeAndroid.Medium;
    }
}
