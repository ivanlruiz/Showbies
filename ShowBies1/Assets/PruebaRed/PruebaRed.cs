using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

// PRUEBA DE RENDIMIENTO PARA EL CO-OP (fase 0 de COOP.md). Es un ensayo para tirar:
// vive en su propia carpeta, no lo usa el juego y se borra entero cuando se decida.
//
// La pregunta que contesta: el telefono ya corre 35 zombis a 30-60 FPS, pero sincronizarlos
// suma deserializar 35 transforms 30 veces por segundo, que en las pruebas de la comunidad
// aparece como lo mas caro de Netcode. ¿Entra o no entra?
//
// Por eso mide la DIFERENCIA en el mismo aparato y con los mismos bichos:
//
//   SOLO      los N bichos moviendose, sin nada de red        -> el piso
//   ANFITRION los simula y ademas los serializa para el otro  -> el costo de mandar
//   CLIENTE   los recibe y los deserializa                    -> el costo de recibir
//
// El numero que decide es CLIENTE contra SOLO: esa resta es lo que cuesta la red.
//
// Los bichos imitan al zombi en lo que pesa para la fisica y la red: Rigidbody, capsula y
// una malla. No tienen el modelo con esqueleto ni el Animator, que ya estan medidos y no
// cambian con la red.
public class PruebaRed : MonoBehaviour
{
    public GameObject bichoPrefab;
    public int cantidad = 35;
    public float velocidad = 5f;
    public float radioDelMapa = 22f;

    private readonly List<Transform> locales = new List<Transform>();
    private readonly List<Rigidbody> cuerpos = new List<Rigidbody>();
    private string modo = "nada";
    private string ip = "192.168.1.2";   // la PC de Ivan; se puede editar en pantalla

    // La media de FPS de la ultima ventana, que es lo que se anota.
    private float acumulado;
    private int cuadros;
    private float fps;
    private float peor = 9999f;
    private float ventana;
    private const float Ventana = 1f;

    private Vector3 objetivo;
    private float cambiarObjetivoEn;

    // Modo automatico, para medir desde afuera sin tocar botones:
    //   PruebaRed.exe -modo solo|anfitrion|cliente -cantidad 35 -ip 127.0.0.1 -segundos 20
    // Mide, escribe el resultado y cierra.
    private float midoHasta = -1f;
    private string etiqueta = "";
    private bool automatico;

    private void Start()
    {
        Application.targetFrameRate = 60;      // igual que el juego, si no Android lo clava en 30
        QualitySettings.vSyncCount = 0;
        objetivo = Vector3.zero;

        string m = Arg("-modo");

#if UNITY_EDITOR
        // En el editor el modo lo deja el menu en SessionState, que sobrevive a la recarga
        // de dominio. Se lee ACA y no desde un playModeStateChanged del editor: ese evento
        // se dispara antes de que el [InitializeOnLoadMethod] llegue a suscribirse, asi que
        // el anfitrion nunca arrancaba y no habia ningun error que lo delatara.
        // El bloque es chico y adentro de un metodo: no toca campos serializados, que es
        // lo que rompe las escenas cuando se envuelve una clase entera.
        if (string.IsNullOrEmpty(m))
        {
            string delEditor = UnityEditor.SessionState.GetString("PruebaRedModo", "");
            if (!string.IsNullOrEmpty(delEditor))
            {
                UnityEditor.SessionState.SetString("PruebaRedModo", "");
                ArrancarDesdeElEditor(delEditor);
                return;
            }
        }
#endif

        if (string.IsNullOrEmpty(m)) return;

        automatico = true;
        int n;
        if (int.TryParse(Arg("-cantidad"), out n)) cantidad = n;
        string unaIp = Arg("-ip");
        if (!string.IsNullOrEmpty(unaIp)) ip = unaIp;
        float segundos;
        if (!float.TryParse(Arg("-segundos"), out segundos)) segundos = 20f;

        etiqueta = m + " x" + cantidad;
        // Un respiro antes de empezar a contar: los primeros cuadros de una escena recien
        // cargada son basura y arruinarian la media.
        Invoke(m == "cliente" ? "AutoCliente" : (m == "anfitrion" ? "AutoAnfitrion" : "AutoSolo"), 1.5f);
        midoHasta = Time.unscaledTime + 4f + segundos;
    }

    // La llama el menu del editor: la PC hace de anfitrion sin pasar por la linea de
    // comandos, que en el editor no existe.
    public void ArrancarDesdeElEditor(string modo)
    {
        if (modo == "anfitrion") EmpezarAnfitrion();
        else if (modo == "cliente") EmpezarCliente();
        else EmpezarSolo();
    }

    private void AutoSolo() { EmpezarSolo(); Reiniciar(); }
    private void AutoAnfitrion() { EmpezarAnfitrion(); Reiniciar(); }
    private void AutoCliente() { EmpezarCliente(); Reiniciar(); }

    // Los primeros segundos no cuentan: los bichos estan naciendo y el cliente conectando.
    private void Reiniciar()
    {
        Invoke("EmpezarAContar", 2.5f);
    }

    private void EmpezarAContar()
    {
        peor = 9999f; acumulado = 0f; cuadros = 0; ventana = 0f;
        medias.Clear();
    }

    private readonly List<float> medias = new List<float>();

    private static string Arg(string nombre)
    {
        var a = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < a.Length - 1; i++) if (a[i] == nombre) return a[i + 1];
        return null;
    }

    private void Terminar()
    {
        float media = 0f;
        foreach (var f in medias) media += f;
        media = medias.Count > 0 ? media / medias.Count : 0f;
        medias.Sort();
        float p5 = medias.Count > 0 ? medias[Mathf.Clamp(Mathf.RoundToInt(medias.Count * 0.05f), 0, medias.Count - 1)] : 0f;

        string linea = string.Format("{0,-18} media {1,5:0.0} FPS   peor segundo {2,5:0.0}   p5 {3,5:0.0}   ventanas {4}   objetos {5}",
                                     etiqueta, media, peor >= 9999f ? 0f : peor, p5, medias.Count, ObjetosEnEscena());
        Debug.Log("PRUEBARED " + linea);
        try
        {
            string ruta = System.IO.Path.Combine(Application.persistentDataPath, "prueba_red.txt");
            System.IO.File.AppendAllText(ruta, linea + System.Environment.NewLine);
            Debug.Log("PRUEBARED escrito en " + ruta);
        }
        catch (System.Exception e) { Debug.Log("PRUEBARED no pudo escribir: " + e.Message); }
        Application.Quit();
    }

    private void Update()
    {
        // FPS de verdad: tiempo sin escalar y media por ventana.
        acumulado += Time.unscaledDeltaTime;
        cuadros++;
        ventana += Time.unscaledDeltaTime;
        if (ventana >= Ventana)
        {
            fps = cuadros / acumulado;
            if (fps < peor) peor = fps;
            if (automatico) medias.Add(fps);
            acumulado = 0f; cuadros = 0; ventana = 0f;
        }

        if (automatico && midoHasta > 0f && Time.unscaledTime >= midoHasta)
        {
            midoHasta = -1f;
            Terminar();
        }

        // El objetivo se mueve, para que los bichos no queden quietos amontonados.
        if (Time.time >= cambiarObjetivoEn)
        {
            cambiarObjetivoEn = Time.time + 3f;
            objetivo = new Vector3(Random.Range(-radioDelMapa, radioDelMapa), 0f,
                                   Random.Range(-radioDelMapa, radioDelMapa));
        }
    }

    private void FixedUpdate()
    {
        // Solo manda el que los simula: en SOLO y ANFITRION. El cliente los recibe.
        bool simulo = modo == "solo" || (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer);
        if (!simulo) return;

        for (int i = 0; i < cuerpos.Count; i++)
        {
            var rb = cuerpos[i];
            if (rb == null) continue;
            // Igual que EnemyController: mira al objetivo a su altura y pisa solo la
            // velocidad horizontal, asi la gravedad sigue actuando.
            Vector3 hacia = objetivo - rb.position;
            hacia.y = 0f;
            if (hacia.sqrMagnitude > 0.01f) hacia.Normalize();
            var v = rb.linearVelocity;
            rb.linearVelocity = new Vector3(hacia.x * velocidad, v.y, hacia.z * velocidad);
        }
    }

    // --- los tres modos -------------------------------------------------------------

    private void Soltar()
    {
        foreach (var t in locales) if (t != null) Destroy(t.gameObject);
        locales.Clear();
        cuerpos.Clear();
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            NetworkManager.Singleton.Shutdown();
        peor = 9999f;
    }

    private Vector3 DondeNace(int i)
    {
        float a = i * Mathf.PI * 2f / Mathf.Max(1, cantidad);
        float r = radioDelMapa * 0.7f;
        return new Vector3(Mathf.Cos(a) * r, 1.2f, Mathf.Sin(a) * r);
    }

    private void EmpezarSolo()
    {
        Soltar();
        modo = "solo";
        for (int i = 0; i < cantidad; i++)
        {
            var go = Instantiate(bichoPrefab, DondeNace(i), Quaternion.identity);
            // Sin red: se le saca lo de Netcode para que no pese ni un poco.
            var no = go.GetComponent<NetworkObject>();
            var nt = go.GetComponent<NetworkTransform>();
            if (nt != null) Destroy(nt);
            if (no != null) Destroy(no);
            Anotar(go);
        }
    }

    private void EmpezarAnfitrion()
    {
        Soltar();
        modo = "anfitrion";
        if (!NetworkManager.Singleton.StartHost()) { modo = "fallo al abrir"; return; }
        for (int i = 0; i < cantidad; i++)
        {
            var go = Instantiate(bichoPrefab, DondeNace(i), Quaternion.identity);
            go.GetComponent<NetworkObject>().Spawn();
            Anotar(go);
        }
    }

    private void EmpezarCliente()
    {
        Soltar();
        modo = "cliente";
        var t = NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
        t.SetConnectionData(ip.Trim(), 7777);
        if (!NetworkManager.Singleton.StartClient()) modo = "fallo al conectar";
    }

    private void Anotar(GameObject go)
    {
        locales.Add(go.transform);
        var rb = go.GetComponent<Rigidbody>();
        if (rb != null) cuerpos.Add(rb);
    }

    // --- la pantalla ----------------------------------------------------------------

    private void OnGUI()
    {
        float e = Mathf.Max(1f, Screen.height / 720f);        // que se lea en el telefono
        GUI.skin.label.fontSize = Mathf.RoundToInt(18 * e);
        GUI.skin.button.fontSize = Mathf.RoundToInt(18 * e);
        GUI.skin.textField.fontSize = Mathf.RoundToInt(18 * e);

        float an = 200 * e, al = 55 * e, m = 12 * e;
        float x = m, y = m;

        GUI.Box(new Rect(0, 0, an * 2.2f + m * 2, al * 7.6f), GUIContent.none);

        GUI.Label(new Rect(x, y, an * 2f, al),
                  string.Format("{0:0} FPS    (peor {1:0})", fps, peor >= 9999f ? 0f : peor));
        y += al;
        GUI.Label(new Rect(x, y, an * 2f, al),
                  string.Format("modo: {0}    bichos: {1}", modo, ObjetosEnEscena()));
        y += al;

        if (GUI.Button(new Rect(x, y, an, al), "- 5")) cantidad = Mathf.Max(5, cantidad - 5);
        if (GUI.Button(new Rect(x + an + m, y, an, al), "+ 5")) cantidad += 5;
        y += al + m;

        if (GUI.Button(new Rect(x, y, an, al), "SOLO (sin red)")) EmpezarSolo();
        if (GUI.Button(new Rect(x + an + m, y, an, al), "ANFITRION")) EmpezarAnfitrion();
        y += al + m;

        ip = GUI.TextField(new Rect(x, y, an, al), ip);
        if (GUI.Button(new Rect(x + an + m, y, an, al), "CLIENTE")) EmpezarCliente();
        y += al + m;

        if (GUI.Button(new Rect(x, y, an, al), "PARAR")) { Soltar(); modo = "nada"; }
        GUI.Label(new Rect(x + an + m, y, an * 1.2f, al), "objetivo: " + cantidad);
    }

    private int ObjetosEnEscena()
    {
        if (modo == "cliente" && NetworkManager.Singleton != null)
        {
            int n = 0;
            foreach (var o in NetworkManager.Singleton.SpawnManager.SpawnedObjectsList) n++;
            return n;
        }
        return locales.Count;
    }
}
