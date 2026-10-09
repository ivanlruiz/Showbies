using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Un escenario de los capitulos: como se ve el mundo y que decorado tiene. El primero de
// la lista es "lo que trae la escena" (la pradera): sus colores, su niebla y su piso se leen
// al empezar y no se cargan a mano.
[System.Serializable]
public class EscenarioDeCapitulo
{
    [Tooltip("El id del nombre en la tabla: escenario_pradera, escenario_cementerio, escenario_ciudad.")]
    public string idTexto = "escenario_pradera";

    [Tooltip("El prefab que sale del piso al entrar. Vacio: el capitulo no tiene decorado.")]
    public GameObject decorado;

    [Tooltip("El piso. Vacio: el que trae la escena.")]
    public Material piso;

    public Color cielo = new Color(0.66f, 0.86f, 0.96f, 1f);
    public Color luz = Color.white;
    public float intensidadLuz = 1.25f;
    public Vector3 rotacionLuz = new Vector3(50f, -30f, 0f);
    public Color ambiente = new Color(0.55f, 0.55f, 0.55f, 1f);

    [Tooltip("Sin niebla, queda lejisimos; con niebla se acerca y tapa el borde del mapa.")]
    public bool conNiebla;
    public float nieblaInicio = 16f;
    public float nieblaFin = 52f;
}

// Los capitulos de las oleadas, pedido de Ivan: cada 10 oleadas cambia el escenario. Van
// en el orden de `escenarios` y despues vuelven a empezar: 1-10 la pradera, 11-20 el
// cementerio, 21-30 la ciudad y 31-40 otra vez la pradera. Los tres son de noche, cada uno
// con su neon (desde el 25/9; hasta ahi la pradera era de dia).
//
// Al pasar de capitulo, en el descanso de la oleada: el cartel "CAPITULO 3 / LA CIUDAD",
// la luz, el cielo, la luz ambiente y la niebla se funden de un escenario al otro, el
// piso cambia a mitad del fundido y el decorado nuevo sale del suelo mientras el viejo se
// va en lo oscuro. Una partida retomada en la 25 arranca directamente en la ciudad.
//
// Los decorados son prefabs hechos con formas simples (ConstructorEscenarios, en el
// editor), sin colliders: se los pone esto al armarlos, a los edificios (PonerParedes) y a
// los obstaculos (PonerObstaculos). Cada uno se arma una sola vez en la partida y despues se
// prende y se apaga, que son cientos de objetos. Cuando termina de salir se junta en pocos draw calls (StaticBatchingUtility) y
// desde ahi las piezas ya no se mueven por separado, asi que **la primera vez sale cada
// pieza sola del piso y las siguientes sale el decorado entero**.
//
// El decorado del capitulo que viene se arma **apagado, unos segundos despues de entrar
// al anterior** (PrepararElSiguiente): instanciar ochocientos objetos en el frame del
// cambio era un tiron justo en el momento del evento.
//
// Va en WaveMode.
public class CapitulosDeEscenario : MonoBehaviour
{
    public WaveManager oleadas;
    public int oleadasPorCapitulo = 10;

    [Tooltip("En orden. El primero es lo que trae la escena: sus colores y su piso se leen al empezar.")]
    public EscenarioDeCapitulo[] escenarios;

    public float duracionFundido = 2.5f;

    [Header("Lo que se pinta")]
    public Renderer piso;
    public Light sol;
    public Camera camara;

    [Header("El cartel")]
    public Canvas canvas;
    public TMP_FontAsset fuente;
    public Material materialContorno;
    public Color colorCartel = new Color(1f, 0.85f, 0.3f);
    public float duracionCartel = 3.2f;

    private const float Hundido = 2.5f;      // cuanto se hunde el decorado para salir del piso

    // El cartel del capitulo: donde sale, desde el centro de la pantalla, y su letra. La
    // prueba de logica mide que no se pise con los que pueden salir a la vez.
    public const float AlturaDelCartel = 385f;
    public const float LetraDelCartel = 84f;
    public const float LetraDelNombre = 0.6f;   // del titulo

    // Lo puesto de cada escenario: se arma la primera vez que toca y se reusa.
    private class Puesta
    {
        public GameObject objeto;
        public bool combinado;
        public readonly List<Transform> piezas = new List<Transform>();
        public readonly List<float> alturas = new List<float>();
        public readonly List<float> demoras = new List<float>();
        public float saliendoDesde = -1f;
        public readonly List<Rect> tapado = new List<Rect>();   // lo que tapan sus edificios (ver Tapado)
        public readonly List<Rect> huellas = new List<Rect>();  // y lo que ocupan, con los obstaculos de caja (ver Huellas)
        public readonly List<Redondo> redondos = new List<Redondo>();  // los obstaculos redondos (ver Redondos)
        public readonly List<Collider> chicos = new List<Collider>();  // los que pisa el jefe (ver Chicos)
    }

    private Puesta[] puestas;
    private int capitulo = -1;
    private int indiceDesde, indiceHacia;
    private float mezcla = 1f;               // 0 = el de indiceDesde, 1 = el de indiceHacia
    private TMP_Text cartel;
    private float cartelDesde;
    private Color ambienteDeLaEscena = Color.gray;

    private void Start()
    {
        if (camara == null) camara = Camera.main;
        ambienteDeLaEscena = RenderSettings.ambientLight;
        if (escenarios == null || escenarios.Length == 0) return;

        // El primero es lo que ya hay en la escena: asi el capitulo 1 se ve igual que
        // siempre sin cargar los mismos colores dos veces.
        var dia = escenarios[0];
        if (camara != null) dia.cielo = camara.backgroundColor;
        if (sol != null)
        {
            dia.luz = sol.color;
            dia.intensidadLuz = sol.intensity;
            dia.rotacionLuz = sol.transform.rotation.eulerAngles;
        }
        dia.ambiente = ambienteDeLaEscena;
        if (piso != null) dia.piso = piso.sharedMaterial;
        // La niebla tambien sale de la escena: la pradera es de noche y la trae guardada.
        dia.conNiebla = RenderSettings.fog;
        dia.nieblaInicio = RenderSettings.fogStartDistance;
        dia.nieblaFin = RenderSettings.fogEndDistance;

        puestas = new Puesta[escenarios.Length];
        for (int i = 0; i < puestas.Length; i++) puestas[i] = new Puesta();
    }

    private void OnDestroy()
    {
        // La niebla y la luz ambiente son de la aplicacion: el menu no hereda la noche.
        RenderSettings.fog = false;
        RenderSettings.ambientLight = ambienteDeLaEscena;
        loTapado.Clear();
        huellasPuestas.Clear();
        redondosPuestos.Clear();
        chicosPuestos.Clear();
        if (puestas == null) return;
        foreach (var puesta in puestas) if (puesta.objeto != null) Destroy(puesta.objeto);
    }

    // Si el cartel del capitulo esta en pantalla. Lo mira la pausa: su boton, arriba al
    // centro en el telefono, tapa la primera linea del cartel, y mientras dura se desvanece.
    public bool CartelEnPantalla
    {
        get { return cartel != null; }
    }

    public static int CapituloDe(int oleada, int oleadasPorCapitulo)
    {
        return oleada <= 0 ? 0 : (oleada - 1) / Mathf.Max(1, oleadasPorCapitulo);
    }

    // Que escenario le toca a ese capitulo: van en orden y vuelven a empezar.
    public int EscenarioDe(int capituloDeLaOleada)
    {
        if (escenarios == null || escenarios.Length == 0) return 0;
        int largo = escenarios.Length;
        return ((capituloDeLaOleada % largo) + largo) % largo;
    }

    private void Update()
    {
        if (oleadas == null || oleadas.OleadaActual <= 0 || puestas == null) return;

        int nuevo = CapituloDe(oleadas.OleadaActual, oleadasPorCapitulo);
        if (nuevo != capitulo)
        {
            bool primero = capitulo < 0;
            capitulo = nuevo;
            int destino = EscenarioDe(capitulo);

            if (primero)
            {
                // Retomada o recien empezada: sin fundido ni piezas saliendo.
                indiceDesde = indiceHacia = destino;
                mezcla = 1f;
                if (escenarios[destino].decorado != null) Poner(destino, false);
                Aplicar();
            }
            else if (destino != indiceHacia)
            {
                indiceDesde = indiceHacia;
                indiceHacia = destino;
                mezcla = 0f;
            }
            if (capitulo > 0) MostrarCartel(destino);
            // El que viene, armado y apagado, para que el cambio no cueste nada.
            StopAllCoroutines();
            StartCoroutine(PrepararElSiguiente());
        }

        if (mezcla < 1f)
        {
            bool antes = mezcla >= 0.5f;
            mezcla = Mathf.MoveTowards(mezcla, 1f, Time.deltaTime / Mathf.Max(0.01f, duracionFundido));
            // El cambio de piso y de decorado pasa en lo oscuro, a mitad del fundido.
            if (!antes && mezcla >= 0.5f)
            {
                Sacar(indiceDesde);
                Poner(indiceHacia, true);
            }
            Aplicar();
        }

        AnimarDecorado();
        AnimarCartel();
    }

    // Arma (apagado) el decorado del capitulo siguiente, un rato despues de entrar al
    // actual: asi el Instantiate no cae en el frame del cambio, con el cartel y el fundido.
    private System.Collections.IEnumerator PrepararElSiguiente()
    {
        yield return new WaitForSeconds(6f);
        int siguiente = EscenarioDe(capitulo + 1);
        if (siguiente == indiceHacia) yield break;
        var escenario = escenarios[siguiente];
        var puesta = puestas[siguiente];
        if (escenario.decorado == null || puesta.objeto != null) yield break;
        Armar(escenario, puesta, Mirada());
    }

    private void Aplicar()
    {
        var a = escenarios[indiceDesde];
        var b = escenarios[indiceHacia];
        float m = mezcla;

        Color cielo = Color.Lerp(a.cielo, b.cielo, m);
        if (camara != null) camara.backgroundColor = cielo;
        if (sol != null)
        {
            sol.color = Color.Lerp(a.luz, b.luz, m);
            sol.intensity = Mathf.Lerp(a.intensidadLuz, b.intensidadLuz, m);
            sol.transform.rotation = Quaternion.Slerp(Quaternion.Euler(a.rotacionLuz), Quaternion.Euler(b.rotacionLuz), m);
        }
        RenderSettings.ambientLight = Color.Lerp(a.ambiente, b.ambiente, m);

        // El que no tiene niebla la manda lejisimos, asi pasar de uno con niebla a uno sin
        // ella se ve como que se abre, y no como un corte.
        // Con el fundido terminado manda el de destino: si no, al volver a un capitulo
        // de dia la niebla quedaba prendida con el rango lejisimos, al pedo.
        RenderSettings.fog = m >= 1f ? b.conNiebla : (a.conNiebla || b.conNiebla);
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = cielo;
        RenderSettings.fogStartDistance = Mathf.Lerp(a.conNiebla ? a.nieblaInicio : 300f, b.conNiebla ? b.nieblaInicio : 300f, m);
        RenderSettings.fogEndDistance = Mathf.Lerp(a.conNiebla ? a.nieblaFin : 600f, b.conNiebla ? b.nieblaFin : 600f, m);

        if (piso != null)
        {
            var material = (m >= 0.5f ? b : a).piso;
            if (material != null) piso.sharedMaterial = material;
        }
    }

    // --- Los decorados ---------------------------------------------------------------

    private void Poner(int indice, bool saliendo)
    {
        var escenario = escenarios[indice];
        var puesta = puestas[indice];
        if (escenario.decorado == null) return;
        if (puesta.objeto == null) Armar(escenario, puesta, Mirada());
        puesta.objeto.SetActive(true);
        // Desde que empieza a salir del piso: una caja que nace ahora queda adentro.
        loTapado.Clear();
        loTapado.AddRange(puesta.tapado);
        huellasPuestas.Clear();
        huellasPuestas.AddRange(puesta.huellas);
        redondosPuestos.Clear();
        redondosPuestos.AddRange(puesta.redondos);
        chicosPuestos.Clear();
        chicosPuestos.AddRange(puesta.chicos);
        SacarAlJugador();
        // Prendido el decorado, el jefe que haya pisa lo chico: Unity olvida que no chocan
        // cuando se apaga uno de los dos colliders.
        foreach (var jefe in EnemyController.Jefes) jefe.PisarLoChico(chicosPuestos);

        if (!saliendo)
        {
            Aterrizar(puesta);
            return;
        }

        if (puesta.combinado)
        {
            // Ya esta pegado en una sola malla y las piezas no se mueven por separado:
            // sale el decorado entero de una.
            var p = puesta.objeto.transform.position;
            p.y = -Hundido;
            puesta.objeto.transform.position = p;
        }
        else
        {
            for (int i = 0; i < puesta.piezas.Count; i++)
            {
                var p = puesta.piezas[i].localPosition;
                p.y = puesta.alturas[i] - Hundido;
                puesta.piezas[i].localPosition = p;
            }
        }
        puesta.saliendoDesde = Time.time;
        CamaraJugador.Temblar(0.35f);
    }

    private static void Armar(EscenarioDeCapitulo escenario, Puesta puesta, Vector3 mirada)
    {
        puesta.objeto = Instantiate(escenario.decorado);
        puesta.objeto.SetActive(false);
        foreach (Transform grupo in puesta.objeto.transform)
        {
            foreach (Transform pieza in grupo)
            {
                puesta.piezas.Add(pieza);
                puesta.alturas.Add(pieza.localPosition.y);
                puesta.demoras.Add(Random.Range(0f, 1.4f));
            }
        }
        // Recien instanciado: todo en su lugar y sin juntar todavia.
        LoQueTapanLosEdificios(puesta.objeto, mirada, puesta.tapado, puesta.huellas);
        PonerParedes(puesta.objeto);
        PonerObstaculos(puesta.objeto, puesta.huellas, puesta.redondos, puesta.chicos);
    }

    // Todo en su lugar y, la primera vez, pegado en una sola malla.
    private static void Aterrizar(Puesta puesta)
    {
        puesta.saliendoDesde = -1f;
        var raiz = puesta.objeto.transform.position;
        raiz.y = 0f;
        puesta.objeto.transform.position = raiz;
        if (puesta.combinado) return;

        for (int i = 0; i < puesta.piezas.Count; i++)
        {
            var p = puesta.piezas[i].localPosition;
            p.y = puesta.alturas[i];
            puesta.piezas[i].localPosition = p;
        }
        Juntar(puesta.objeto);
        puesta.combinado = true;
    }

    // Junta un decorado en pocos draw calls, menos los carteles de la ciudad: la malla de un
    // texto la arma TextMeshPro, y juntada con las demas la pisaria al volver a escribirla.
    public static void Juntar(GameObject decorado)
    {
        var piezas = new List<GameObject>();
        foreach (var filtro in decorado.GetComponentsInChildren<MeshFilter>(true))
        {
            if (filtro.GetComponent<TMP_Text>() != null) continue;
            piezas.Add(filtro.gameObject);
        }
        StaticBatchingUtility.Combine(piezas.ToArray(), decorado);
    }

    private void Sacar(int indice)
    {
        var puesta = puestas[indice];
        if (puesta.objeto != null) puesta.objeto.SetActive(false);
        puesta.saliendoDesde = -1f;
        // Se saca siempre el que esta puesto (el de antes del fundido).
        loTapado.Clear();
        huellasPuestas.Clear();
        redondosPuestos.Clear();
        chicosPuestos.Clear();
    }

    private void AnimarDecorado()
    {
        foreach (var puesta in puestas)
        {
            if (puesta.saliendoDesde < 0f || puesta.objeto == null) continue;
            float t = Time.time - puesta.saliendoDesde;

            if (puesta.combinado)
            {
                float f = Mathf.Clamp01(t / 1.2f);
                var p = puesta.objeto.transform.position;
                p.y = -Hundido * (1f - CurvasUI.SalidaAtras(f));
                puesta.objeto.transform.position = p;
                if (f >= 1f) Aterrizar(puesta);
                continue;
            }

            bool termino = true;
            for (int i = 0; i < puesta.piezas.Count; i++)
            {
                float f = Mathf.Clamp01((t - puesta.demoras[i]) / 0.5f);
                if (f < 1f) termino = false;
                var p = puesta.piezas[i].localPosition;
                p.y = puesta.alturas[i] - Hundido * (1f - CurvasUI.SalidaAtras(f));
                puesta.piezas[i].localPosition = p;
            }
            if (termino) Aterrizar(puesta);
        }
    }

    // --- Lo que tapan los edificios ---------------------------------------------------
    //
    // Los decorados no tienen colliders, salvo las paredes de los edificios (ver
    // PonerParedes), y desde la camara del juego (arriba y atras del jugador, mirando siempre hacia el
    // mismo lado) el techo tapa todo lo que queda adentro y una franja del piso del lado de
    // atras. Una de cada seis cajas nacia adentro de un edificio y vencia sin que nadie la
    // viera, y las monedas de un zombi que moria cruzandolo caian adentro. Aca queda, en el
    // piso (x, z), lo que tapan los edificios del decorado que esta puesto: lo miran
    // PowerUp (Tapado) y Moneda (LoTapado). Sin decorado, o en uno sin edificios, no hay nada.

    // El nombre del grupo de cada edificio en el prefab (ConstructorEscenarios.Edificio).
    private const string Edificio = "Edificio";

    private static readonly List<Rect> loTapado = new List<Rect>();
    private static readonly List<Rect> huellasPuestas = new List<Rect>();
    private static readonly List<Redondo> redondosPuestos = new List<Redondo>();
    private static readonly List<Collider> chicosPuestos = new List<Collider>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetearEstadoCompartido()
    {
        loTapado.Clear();
        huellasPuestas.Clear();
        redondosPuestos.Clear();
        chicosPuestos.Clear();
    }

    // Un rectangulo por edificio: x es x y y es z.
    public static IReadOnlyList<Rect> LoTapado => loTapado;

    // Lo que ocupa en el piso cada edificio (sin la franja que tapa el techo) y cada obstaculo
    // de caja (los autos, los canteros y los contenedores, ver PonerObstaculos): lo que los
    // zombis rodean (EnemyController.Rodeo), porque los atravesaban. Primero los edificios, en
    // el orden de LoTapado.
    public static IReadOnlyList<Rect> Huellas => huellasPuestas;

    // Los obstaculos redondos del decorado puesto (las lapidas, las cruces y los troncos): los
    // zombis no los rodean, los esquivan resbalando, que en algo redondo y suelto alcanza.
    public static IReadOnlyList<Redondo> Redondos => redondosPuestos;

    // Los colliders de lo que pisa el jefe (las lapidas y las cruces: ver
    // EnemyController.PisarLoChico).
    public static IReadOnlyList<Collider> Chicos => chicosPuestos;

    [System.Serializable]
    public struct Redondo
    {
        public Vector2 centro;   // x, z
        public float radio;
        public bool chico;       // lo pisa el jefe

        public Redondo(Vector2 centro, float radio, bool chico)
        {
            this.centro = centro;
            this.radio = radio;
            this.chico = chico;
        }
    }

    // Si un obstaculo del decorado puesto (un edificio, una caja o algo redondo) ocupa ese
    // punto del piso, agrandado en 'margen'.
    public static bool Ocupado(Vector3 punto, float margen)
    {
        for (int i = 0; i < huellasPuestas.Count; i++)
        {
            Rect zona = huellasPuestas[i];
            if (punto.x > zona.xMin - margen && punto.x < zona.xMax + margen &&
                punto.z > zona.yMin - margen && punto.z < zona.yMax + margen) return true;
        }
        for (int i = 0; i < redondosPuestos.Count; i++)
        {
            float alcance = redondosPuestos[i].radio + margen;
            if ((new Vector2(punto.x, punto.z) - redondosPuestos[i].centro).sqrMagnitude < alcance * alcance) return true;
        }
        return false;
    }

    // Si un edificio del decorado puesto tapa ese punto del piso, agrandando lo tapado en
    // 'margen' (lo que mide lo que se quiere ver).
    public static bool Tapado(Vector3 punto, float margen = 0f)
    {
        for (int i = 0; i < loTapado.Count; i++)
        {
            Rect zona = loTapado[i];
            if (punto.x >= zona.xMin - margen && punto.x <= zona.xMax + margen &&
                punto.z >= zona.yMin - margen && punto.z <= zona.yMax + margen) return true;
        }
        return false;
    }

    // Para la prueba de logica, sin escena: fija lo tapado, las huellas y lo redondo a mano
    // (null lo vacia).
    public static void UsarParaPruebas(List<Rect> tapado, List<Rect> huellas = null, List<Redondo> redondos = null)
    {
        loTapado.Clear();
        if (tapado != null) loTapado.AddRange(tapado);
        huellasPuestas.Clear();
        if (huellas != null) huellasPuestas.AddRange(huellas);
        redondosPuestos.Clear();
        if (redondos != null) redondosPuestos.AddRange(redondos);
        chicosPuestos.Clear();
    }

    // Lo que tapan los edificios de un decorado con la camara mirando hacia 'mirada': la
    // huella de cada uno y, del lado contrario a la camara, el piso que esconde su techo (el
    // rayo que pasa rozando el borde del techo llega al piso tanto mas alla). Se mide con las
    // mallas y no con Renderer.bounds, que en un objeto apagado viene vacio (el decorado se
    // arma apagado), y antes de juntarlo con StaticBatchingUtility, que les cambia la malla.
    // En 'huellas', si viene, la huella sola de cada uno.
    public static void LoQueTapanLosEdificios(GameObject decorado, Vector3 mirada, List<Rect> tapado, List<Rect> huellas = null)
    {
        tapado.Clear();
        if (huellas != null) huellas.Clear();
        if (decorado == null) return;
        foreach (Transform grupo in decorado.transform)
        {
            foreach (Transform pieza in grupo)
            {
                if (pieza.name != Edificio) continue;
                Bounds caja;
                if (!CajaDe(pieza, Matrix4x4.identity, out caja)) continue;

                Rect zona = Rect.MinMaxRect(caja.min.x, caja.min.z, caja.max.x, caja.max.z);
                if (huellas != null) huellas.Add(zona);
                if (mirada.y < -0.01f)
                {
                    float porAltura = caja.max.y / -mirada.y;
                    float dx = mirada.x * porAltura, dz = mirada.z * porAltura;
                    zona.xMin += Mathf.Min(0f, dx);
                    zona.xMax += Mathf.Max(0f, dx);
                    zona.yMin += Mathf.Min(0f, dz);
                    zona.yMax += Mathf.Max(0f, dz);
                }
                tapado.Add(zona);
            }
        }
    }

    // Las paredes de los edificios de la ciudad: una caja por edificio, del tamaño de su
    // cuerpo, en un hijo del grupo del edificio (sube del piso con el; StaticBatchingUtility
    // solo junta las mallas). Hasta el 8/10 no tenian ninguna y todo los atravesaba (lo dijeron
    // en Discord): ahora el jugador choca, los zombis los rodean (EnemyController.Rodeo) y a
    // los que empuja la horda los frena la pared, y los invocados del jefe salen del lado de la
    // calle (JefePatrones.RadioLibre mira los colliders fijos). Las balas los siguen cruzando:
    // no tienen Rigidbody, y contra un collider fijo no pasa nada.
    public const string Pared = "Pared";

    public static void PonerParedes(GameObject decorado)
    {
        if (decorado == null) return;
        foreach (Transform grupo in decorado.transform)
        {
            foreach (Transform pieza in grupo)
            {
                if (pieza.name != Edificio || pieza.Find(Pared) != null) continue;
                Bounds caja;
                if (!CajaDe(pieza, pieza.worldToLocalMatrix, out caja)) continue;
                var pared = new GameObject(Pared);
                pared.transform.SetParent(pieza, false);
                var collider = pared.AddComponent<BoxCollider>();
                collider.center = caja.center;
                collider.size = caja.size;
            }
        }
    }

    // Los obstaculos del decorado (pedido de Ivan, 9/10: "poner colliders en todos los
    // obstaculos que haya en el mapa"; hasta ahi todo los atravesaba), en un hijo de cada
    // pieza, como las paredes:
    //  - Los autos, los canteros y los contenedores llevan una caja del tamaño de lo que se ve
    //    (sin la luz de color de abajo del auto, que es un cuadrado de 3,2 x 5,6 m en el piso)
    //    y su huella va a 'huellas': los zombis los rodean como a un edificio
    //    (EnemyController.Rodeo).
    //  - Las lapidas, las cruces y los troncos llevan un cilindro y van a 'redondos': no se
    //    rodean, que contra algo redondo y suelto el zombi que empuja resbala hacia un costado.
    //    Las lapidas y las cruces van ademas a 'chicos', que el jefe pisa (ver
    //    EnemyController.PisarLoChico): entre dos no pasaria.
    // Todos miden AlturaDeLosObstaculos, mas de lo que se ve: los rayos que buscan paredes
    // salen de la altura del centro de quien los tira (el anillo de la invocacion del jefe,
    // JefePatrones.RadioLibre, a 2 m) y un auto mide 1,4. Arriba no hay nada que choque: la
    // granada y el salto del jefe van sin fisica. Que esten sueltos (dos mas cerca que lo que
    // mide un zombi forman un rincon donde se traba) lo cuida ConstructorEscenarios.
    public const string Obstaculo = "Obstaculo";
    public const float AlturaDeLosObstaculos = 2.5f;
    public const float RadioDeTumba = 0.4f;    // la losa mide 0,9 x 0,25: asoman las puntas
    public const float RadioDeTronco = 0.25f;  // sin las ramas, que van por encima de las cabezas
    private static readonly string[] DeCaja = { "Auto", "Cantero", "Contenedor" };

    public static void PonerObstaculos(GameObject decorado, List<Rect> huellas, List<Redondo> redondos, List<Collider> chicos)
    {
        if (decorado == null) return;
        foreach (Transform grupo in decorado.transform)
        {
            foreach (Transform pieza in grupo)
            {
                if (pieza.Find(Obstaculo) != null) continue;
                bool esTumba = pieza.name == "Lapida" || pieza.name == "Cruz";
                if (System.Array.IndexOf(DeCaja, pieza.name) >= 0)
                {
                    Bounds caja, enElMundo;
                    if (!CajaDe(pieza, pieza.worldToLocalMatrix, out caja, true) || !CajaDe(pieza, Matrix4x4.identity, out enElMundo, true)) continue;
                    var go = new GameObject(Obstaculo);
                    go.transform.SetParent(pieza, false);
                    var collider = go.AddComponent<BoxCollider>();
                    float alto = Mathf.Max(caja.max.y, AlturaDeLosObstaculos);
                    collider.center = new Vector3(caja.center.x, alto * 0.5f, caja.center.z);
                    collider.size = new Vector3(caja.size.x, alto, caja.size.z);
                    if (huellas != null) huellas.Add(Rect.MinMaxRect(enElMundo.min.x, enElMundo.min.z, enElMundo.max.x, enElMundo.max.z));
                }
                else if (esTumba || pieza.name == "Arbol")
                {
                    float radio = esTumba ? RadioDeTumba : RadioDeTronco;
                    var go = new GameObject(Obstaculo);
                    go.transform.SetParent(pieza, false);
                    var cilindro = go.AddComponent<CapsuleCollider>();
                    cilindro.direction = 1;
                    cilindro.radius = radio;
                    cilindro.height = AlturaDeLosObstaculos;
                    cilindro.center = new Vector3(0f, AlturaDeLosObstaculos * 0.5f, 0f);
                    if (redondos != null) redondos.Add(new Redondo(new Vector2(pieza.position.x, pieza.position.z), radio, esTumba));
                    if (esTumba && chicos != null) chicos.Add(cilindro);
                }
            }
        }
    }

    // El jugador parado donde sale un edificio o un obstaculo (en la pradera ahi es campo
    // abierto) pasa al lugar libre mas cercano, en lo mas oscuro del fundido, antes de que la
    // pared suba con el adentro y la fisica lo escupa. Lo mismo las cajas que haya en el piso,
    // que si no quedarian adentro de un auto hasta vencer.
    public const float MargenDelJugador = 1f;

    private static void SacarAlJugador()
    {
        var salud = PlayerHealth.instance;
        if (salud != null)
        {
            Vector3 donde = salud.transform.position;
            Vector3 afuera = FueraDeLosObstaculos(donde, MargenDelJugador);
            if (afuera != donde)
            {
                var cuerpo = salud.GetComponentInParent<Rigidbody>();
                if (cuerpo != null) cuerpo.position = afuera;
                salud.transform.position = afuera;
            }
        }
        foreach (var caja in FindObjectsByType<PickupCaducidad>(FindObjectsSortMode.None))
        {
            Vector3 donde = caja.transform.position;
            Vector3 afuera = FueraDeLosObstaculos(donde, PowerUp.MargenContraLosEdificios);
            if (afuera != donde) caja.transform.position = afuera;
        }
    }

    // El punto, o si cae en un obstaculo del decorado puesto agrandado en 'margen', el lugar
    // libre mas cercano. Casi siempre alcanza con salir del que se pisa, pero con un margen
    // grande dos tumbas vecinas se lo pasan de una a la otra: ahi se busca en anillos.
    public static Vector3 FueraDeLosObstaculos(Vector3 punto, float margen)
    {
        Vector3 inicio = punto;
        for (int vuelta = 0; vuelta < 6; vuelta++)
        {
            Vector3 antes = punto;
            punto = FueraDeLasHuellas(punto, margen);
            punto = FueraDeLoRedondo(punto, margen);
            if (punto == antes) return punto;
        }
        if (!Ocupado(punto, margen)) return punto;
        for (float lejos = 0.5f; lejos <= 6f; lejos += 0.5f)
        {
            for (int i = 0; i < 16; i++)
            {
                float angulo = i * Mathf.PI / 8f;
                var otro = inicio + new Vector3(Mathf.Cos(angulo) * lejos, 0f, Mathf.Sin(angulo) * lejos);
                if (!Ocupado(otro, margen)) return otro;
            }
        }
        return punto;
    }

    // Afuera del cilindro de lo redondo que pisa, agrandado en 'margen', para el lado de donde
    // esta (justo en el centro, hacia +x).
    private static Vector3 FueraDeLoRedondo(Vector3 punto, float margen)
    {
        for (int i = 0; i < redondosPuestos.Count; i++)
        {
            var r = redondosPuestos[i];
            float alcance = r.radio + margen;
            Vector2 desde = new Vector2(punto.x, punto.z) - r.centro;
            if (desde.sqrMagnitude >= alcance * alcance) continue;
            Vector2 hacia = desde.sqrMagnitude > 1e-6f ? desde.normalized : Vector2.right;
            Vector2 afuera = r.centro + hacia * (alcance + 0.01f);
            punto.x = afuera.x;
            punto.z = afuera.y;
        }
        return punto;
    }

    // El punto, o si cae en una huella (un edificio o una caja) agrandada en 'margen', el borde
    // mas cercano de ella.
    public static Vector3 FueraDeLasHuellas(Vector3 punto, float margen)
    {
        for (int i = 0; i < huellasPuestas.Count; i++)
        {
            Rect zona = huellasPuestas[i];
            float oeste = zona.xMin - margen, este = zona.xMax + margen;
            float sur = zona.yMin - margen, norte = zona.yMax + margen;
            if (punto.x <= oeste || punto.x >= este || punto.z <= sur || punto.z >= norte) continue;

            float hastaOeste = punto.x - oeste, hastaEste = este - punto.x;
            float hastaSur = punto.z - sur, hastaNorte = norte - punto.z;
            float menor = Mathf.Min(Mathf.Min(hastaOeste, hastaEste), Mathf.Min(hastaSur, hastaNorte));
            if (menor == hastaOeste) punto.x = oeste;
            else if (menor == hastaEste) punto.x = este;
            else if (menor == hastaSur) punto.z = sur;
            else punto.z = norte;
            return punto;
        }
        return punto;
    }

    // La caja de las mallas de una pieza, llevadas con 'hacia' (al mundo o a la pieza). Con
    // 'soloLoSolido', sin los brillos (el charco y el halo de los faroles, la luz de debajo de
    // los autos), que son cuadrados pintados y no se chocan.
    private static bool CajaDe(Transform pieza, Matrix4x4 hacia, out Bounds caja, bool soloLoSolido = false)
    {
        caja = default;
        bool hay = false;
        foreach (var filtro in pieza.GetComponentsInChildren<MeshFilter>(true))
        {
            if (filtro.sharedMesh == null) continue;
            if (soloLoSolido && EsUnBrillo(filtro)) continue;
            Bounds b = EnElMundo(filtro.sharedMesh.bounds, hacia * filtro.transform.localToWorldMatrix);
            if (hay) caja.Encapsulate(b);
            else { caja = b; hay = true; }
        }
        return hay;
    }

    private static bool EsUnBrillo(MeshFilter filtro)
    {
        var r = filtro.GetComponent<Renderer>();
        return r != null && r.sharedMaterial != null && r.sharedMaterial.shader != null && r.sharedMaterial.shader.name == "ShowBies/CharcoDeLuz";
    }

    // La caja de una malla en el mundo, sin armar sus ocho esquinas.
    private static Bounds EnElMundo(Bounds local, Matrix4x4 m)
    {
        Vector3 e = local.extents;
        var extension = new Vector3(
            Mathf.Abs(m.m00) * e.x + Mathf.Abs(m.m01) * e.y + Mathf.Abs(m.m02) * e.z,
            Mathf.Abs(m.m10) * e.x + Mathf.Abs(m.m11) * e.y + Mathf.Abs(m.m12) * e.z,
            Mathf.Abs(m.m20) * e.x + Mathf.Abs(m.m21) * e.y + Mathf.Abs(m.m22) * e.z);
        return new Bounds(m.MultiplyPoint3x4(local.center), extension * 2f);
    }

    // Hacia donde mira la camara del juego, que no gira (el temblor la rota sobre ese eje).
    private Vector3 Mirada()
    {
        return camara != null ? camara.transform.forward : Vector3.down;
    }

    // --- El cartel -------------------------------------------------------------------

    private void MostrarCartel(int indiceEscenario)
    {
        if (canvas == null) return;
        if (cartel != null) Destroy(cartel.gameObject);
        var go = new GameObject("CartelCapitulo", typeof(RectTransform), typeof(TextMeshProUGUI));
        var rt = (RectTransform)go.transform;
        rt.SetParent(canvas.transform, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        // Arriba de todo: al medio sale el cartel de la oleada, al mismo tiempo.
        rt.anchoredPosition = new Vector2(0f, AlturaDelCartel);
        rt.sizeDelta = new Vector2(1400f, 200f);
        cartel = go.GetComponent<TextMeshProUGUI>();
        if (fuente != null) cartel.font = fuente;
        if (materialContorno != null) cartel.fontSharedMaterial = materialContorno;
        string nombre = Textos.De(escenarios[indiceEscenario].idTexto);
        cartel.text = Textos.Formato("capitulo_titulo", capitulo + 1) + "\n<size=" + Mathf.RoundToInt(LetraDelNombre * 100f) + "%>" + nombre + "</size>";
        cartel.fontSize = LetraDelCartel;
        cartel.color = colorCartel;
        cartel.alignment = TextAlignmentOptions.Center;
        cartel.textWrappingMode = TextWrappingModes.NoWrap;
        cartel.raycastTarget = false;
        cartelDesde = Time.unscaledTime;
        // Sin jingle propio: el capitulo cambia siempre con una oleada nueva, y el de su
        // cartel ya sono. Este salia un cuadro despues (la oleada cambia en la corrutina
        // del WaveManager, despues de este Update), y en el telefono, con ese cuadro
        // guardando el progreso, pasaba la separacion de 30 ms y se oia como un eco.
    }

    private void AnimarCartel()
    {
        if (cartel == null) return;
        float edad = Time.unscaledTime - cartelDesde;
        float entrada = Mathf.Clamp01(edad / 0.4f);
        float salida = Mathf.Clamp01((edad - (duracionCartel - 0.35f)) / 0.35f);
        cartel.rectTransform.localScale = Vector3.one * CurvasUI.SalidaAtras(entrada) * (1f - salida);
        if (edad >= duracionCartel)
        {
            Destroy(cartel.gameObject);
            cartel = null;
        }
    }
}
