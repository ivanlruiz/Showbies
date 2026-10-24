using System.Collections.Generic;
using UnityEngine;

// El acido del escupidor (ZombiEscupidor, ZOMBIS.md): el globo que vuela en arco hasta donde marco el anillo y el charco
// que deja al caer. El charco dura 4 s, da 0,25 s de gracia y despues le pega al jugador que lo pisa la mitad del golpe
// de un normal cada 0,5 s. Hay como mucho 3 charcos en el mapa: el cuarto borra el mas viejo (de Argon). Va aparte del
// escupidor porque lo sobrevive: el charco sigue aunque el que lo tiro ya este muerto.
//
// El globo no tiene collider: vuela por una parabola calculada, como la granada, y lo que cuenta es donde cae. El charco
// tampoco: el daño es una distancia al jugador, como el cobro de las monedas. Todo con tiempo escalado (la pausa lo
// congela) y reusado: los objetos no se crean ni se destruyen en cada escupitajo.
//
// No esta en las escenas: se arma la primera vez que alguien escupe, y se va con la escena.
public class AcidoDelEscupidor : MonoBehaviour
{
    public const int MaximoDeCharcos = 3;
    public const float AlturaDelCharco = 0.2f;   // por encima de la vereda, como los charcos de los faroles
    public const float TamanoDelGlobo = 0.35f;

    public struct Charco
    {
        public float radio, duracion, gracia, cadaCuanto, dano;
        public Color color;
    }

    private class Globo
    {
        public Transform cuerpo;
        public Vector3 desde, hasta;
        public float salio, dura, altura;
        public Charco charco;
        public Material materialCharco, materialBorde;
    }

    private class CharcoEnElPiso
    {
        public Transform cuerpo;
        public Renderer dibujo;
        public LineRenderer borde;
        public Charco datos;
        public float cayo, proximoGolpe;
        public Color colorDelBorde;
    }

    private static AcidoDelEscupidor instancia;
    private readonly List<Globo> enElAire = new List<Globo>();
    private readonly List<CharcoEnElPiso> enElPiso = new List<CharcoEnElPiso>();
    private readonly Stack<Transform> globosLibres = new Stack<Transform>();
    private readonly Stack<CharcoEnElPiso> charcosLibres = new Stack<CharcoEnElPiso>();
    private MaterialPropertyBlock propiedades;

    // Para las pruebas.
    public static int GlobosEnElAire { get { return instancia != null ? instancia.enElAire.Count : 0; } }
    public static int CharcosEnElPiso { get { return instancia != null ? instancia.enElPiso.Count : 0; } }
    public static int GolpesDados { get; private set; }
    public static Vector3 UltimoCharco { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetearEstadoCompartido()
    {
        instancia = null;
        GolpesDados = 0;
        UltimoCharco = Vector3.zero;
    }

    // Donde va el globo a la fraccion t del vuelo: derecho de 'desde' a 'hasta' y una parabola encima. Estatico para
    // probarlo sin escena.
    public static Vector3 EnElArco(Vector3 desde, Vector3 hasta, float altura, float t)
    {
        t = Mathf.Clamp01(t);
        Vector3 p = Vector3.Lerp(desde, hasta, t);
        p.y += 4f * altura * t * (1f - t);
        return p;
    }

    // Si el charco le pega ahora a alguien parado a 'distancia' de su centro (en el piso). Estatico para probarlo.
    public static bool Pega(float distancia, float radio, float ahora, float cayo, float gracia, float duracion)
    {
        return distancia <= radio && ahora >= cayo + gracia && ahora < cayo + duracion;
    }

    public static void Lanzar(Vector3 desde, Vector3 hasta, float velocidad, float altura, Charco charco,
                              Material materialGlobo, Material materialCharco, Material materialBorde)
    {
        if (instancia == null)
        {
            var go = new GameObject("AcidoDelEscupidor");
            instancia = go.AddComponent<AcidoDelEscupidor>();
        }
        instancia.Tirar(desde, hasta, velocidad, altura, charco, materialGlobo, materialCharco, materialBorde);
    }

    private void OnDestroy()
    {
        if (instancia == this) instancia = null;
    }

    private void Tirar(Vector3 desde, Vector3 hasta, float velocidad, float altura, Charco charco,
                       Material materialGlobo, Material materialCharco, Material materialBorde)
    {
        hasta.y = AlturaDelCharco;
        Vector3 recorrido = hasta - desde;
        recorrido.y = 0f;
        var globo = new Globo
        {
            cuerpo = globosLibres.Count > 0 ? globosLibres.Pop() : CrearGlobo(materialGlobo),
            desde = desde,
            hasta = hasta,
            salio = Time.time,
            dura = Mathf.Max(0.15f, recorrido.magnitude / Mathf.Max(0.1f, velocidad)),
            altura = altura,
            charco = charco,
            materialCharco = materialCharco,
            materialBorde = materialBorde,
        };
        globo.cuerpo.position = desde;
        globo.cuerpo.gameObject.SetActive(true);
        enElAire.Add(globo);
    }

    private Transform CrearGlobo(Material material)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "Escupitajo";
        // En el acto: un collider en el globo chocaria con todo.
        DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(transform, false);
        go.transform.localScale = Vector3.one * TamanoDelGlobo;
        var render = go.GetComponent<Renderer>();
        if (material != null) render.sharedMaterial = material;
        render.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        render.receiveShadows = false;
        go.layer = Personajes.Capa;
        return go.transform;
    }

    private CharcoEnElPiso CrearCharco(Material materialCharco, Material materialBorde)
    {
        var c = new CharcoEnElPiso();
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = "CharcoDeAcido";
        DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(transform, false);
        go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        c.cuerpo = go.transform;
        c.dibujo = go.GetComponent<Renderer>();
        if (materialCharco != null) c.dibujo.sharedMaterial = materialCharco;
        c.dibujo.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        c.dibujo.receiveShadows = false;

        // El borde marca justo hasta donde pega: el charco de luz se apaga hacia afuera y no lo dice. Suelto y no hijo
        // del charco, que esta escalado.
        var borde = new GameObject("BordeDelCharco");
        borde.transform.SetParent(transform, false);
        c.borde = borde.AddComponent<LineRenderer>();
        if (materialBorde != null) c.borde.material = materialBorde;
        c.borde.widthMultiplier = 0.12f;
        c.borde.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        c.borde.receiveShadows = false;
        c.borde.alignment = LineAlignment.TransformZ;
        borde.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
        return c;
    }

    private void Caer(Globo globo)
    {
        globosLibres.Push(globo.cuerpo);
        globo.cuerpo.gameObject.SetActive(false);

        // El cuarto borra el mas viejo.
        while (enElPiso.Count >= MaximoDeCharcos) Secar(0);

        var c = charcosLibres.Count > 0 ? charcosLibres.Pop() : CrearCharco(globo.materialCharco, globo.materialBorde);
        c.datos = globo.charco;
        c.cayo = Time.time;
        c.proximoGolpe = c.cayo + c.datos.gracia;
        Vector3 donde = globo.hasta;
        donde.y = AlturaDelCharco;
        c.cuerpo.position = donde;
        // El charco de luz se apaga hacia el borde: un poco mas grande que lo que pega.
        float lado = c.datos.radio * 2f * 1.35f;
        c.cuerpo.localScale = new Vector3(lado, lado, 1f);
        Granade.DibujarAnillo(c.borde, donde, c.datos.radio, 32);
        c.colorDelBorde = c.datos.color;
        c.cuerpo.gameObject.SetActive(true);
        c.borde.gameObject.SetActive(true);
        enElPiso.Add(c);
        UltimoCharco = donde;
        Efectos.CharcoDeAcido(donde, c.datos.color);
    }

    private void Secar(int i)
    {
        var c = enElPiso[i];
        enElPiso.RemoveAt(i);
        c.cuerpo.gameObject.SetActive(false);
        c.borde.gameObject.SetActive(false);
        charcosLibres.Push(c);
    }

    private void Update()
    {
        float ahora = Time.time;
        for (int i = enElAire.Count - 1; i >= 0; i--)
        {
            var globo = enElAire[i];
            float t = (ahora - globo.salio) / globo.dura;
            if (t >= 1f)
            {
                enElAire.RemoveAt(i);
                Caer(globo);
                continue;
            }
            globo.cuerpo.position = EnElArco(globo.desde, globo.hasta, globo.altura, t);
        }

        var vida = PlayerHealth.instance;
        Vector3 jugador = vida != null ? vida.transform.position : Vector3.zero;
        for (int i = enElPiso.Count - 1; i >= 0; i--)
        {
            var c = enElPiso[i];
            float vivido = ahora - c.cayo;
            if (vivido >= c.datos.duracion)
            {
                Secar(i);
                continue;
            }
            // Se apaga el ultimo medio segundo.
            float alfa = Mathf.Clamp01((c.datos.duracion - vivido) / 0.5f);
            Color borde = c.colorDelBorde;
            borde.a = 0.85f * alfa;
            c.borde.startColor = c.borde.endColor = borde;
            if (propiedades == null) propiedades = new MaterialPropertyBlock();
            c.dibujo.GetPropertyBlock(propiedades);
            propiedades.SetFloat(IdIntensidad, IntensidadDelCharco * alfa);
            c.dibujo.SetPropertyBlock(propiedades);

            if (vida == null || vida.EstaMuerto || ahora < c.proximoGolpe) continue;
            Vector3 d = jugador - c.cuerpo.position;
            d.y = 0f;
            if (!Pega(d.magnitude, c.datos.radio, ahora, c.cayo, c.datos.gracia, c.datos.duracion)) continue;
            c.proximoGolpe = ahora + c.datos.cadaCuanto;
            GolpesDados++;
            vida.TakeDamage(c.datos.dano, true, "ZombiEscupidor_acido");
        }
    }

    private static readonly int IdIntensidad = Shader.PropertyToID("_Intensidad");
    public const float IntensidadDelCharco = 1.6f;
}
