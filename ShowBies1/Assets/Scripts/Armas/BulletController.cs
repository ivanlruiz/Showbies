using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletController : MonoBehaviour
{
    public int velocidad;
    public float lifeTime;
    public int dañoDar;   // respaldo del prefab: el daño de verdad lo pone el arma en cada tiro
    public Material materialLuz;   // el charco de luz del piso (ShowBies/CharcoDeLuz): lo pone ConstructorArmas
    public Mesh mallaRedonda;      // la de la onda del arma de energia (la esfera baja): la pone ConstructorArmas
    // La base de los materiales de cada tramo: ShowBies/ColorPlano, con instancing (Unlit/Color no
    // lo tiene, y en Android no hay batching dinamico: cada bala era una llamada de dibujo). Lo
    // pone ConstructorArmas. Sin el, la base es el material del prefab (Bullet.mat), como antes.
    public Material materialPlano;

    // El daño de esta bala, puesto en cada tiro por GunController desde la mejora
    // de daño. No se serializa: si se guardara en el prefab, el primer tiro de la
    // partida siguiente arrancaría con el daño de la anterior.
    [System.NonSerialized] public float danoAplicado;
    [System.NonSerialized] public bool critico;   // lo decide el arma al dispararla; cambia el numero que se ve

    // Lo que le ponen las cartas de la partida (1.6.0, etapa 2), decidido al dispararla
    // (GunController.SacarBala): cuantos zombis mas atraviesa, la explosion al pegar, y si
    // congela o quema al que toca. Como el daño y el critico, vuelve a cero en Obtener.
    [System.NonSerialized] public int perforacionesRestantes;
    // Las que atraviesa por su arma, sin perder daño (la de energia: Proyectiles.PerforacionesPropias).
    [System.NonSerialized] public int perforacionesSinPerder;
    [System.NonSerialized] public float fraccionDeExplosion;
    [System.NonSerialized] public float radioDeExplosion;
    [System.NonSerialized] public float lentitudDelHielo;
    [System.NonSerialized] public float quemaduraPorSegundo;

    // A quien ya toco: los tres colliders del mismo zombi, o el que acaba de atravesar, no
    // cuentan dos veces.
    private readonly List<EnemyController> tocados = new List<EnemyController>(4);
    private static readonly Collider[] enLaExplosion = new Collider[64];
    private static readonly HashSet<EnemyController> danadosPorLaExplosion = new HashSet<EnemyController>();

    // Pool de balas.
    //
    // Son de 4 a 20 tiros por segundo con la mejora, hasta 60 con la caja de arma
    // y mas con la furia, con un techo de 120. Cada una era un Instantiate y un
    // Destroy: de lejos la mayor fuente de basura del juego.
    private static readonly Stack<BulletController> pool = new Stack<BulletController>();

    private float lifeTimeInicial;
    private bool enUso;

    // Lo que se compra se ve en el tiro (revision del 9/10, mejora 6): la bala cambia de color
    // por tramos del daño que lleva cada tiro (la mejora por la furia). Los tramos se duplican
    // (1, 2, 4, 8 y 16): la primera compra ya cambia el color, y la furia, que pega x2, sube
    // justo un tramo. La forma y el tamaño son del arma de la mano (Proyectiles, desde el
    // 12/10: antes todas eran el mismo cubo, que crecia con el tramo), y la critica sale blanca
    // y la mitad mas grande, con una estela. Un material por tramo, armado una vez a partir del
    // de la bala y compartido por todas (asi no se rompe el batching); Bullet.mat no se toca,
    // que tambien pinta el brillo de la caja de balas. Solo crece lo que se ve: el collider se
    // achica en la misma proporcion, y la bala pega igual con cualquier forma.
    public static readonly float[] DesdeDano = { 0f, 2f, 4f, 8f, 16f };
    public static readonly Color[] ColoresPorTramo =
    {
        new Color(1f, 1f, 1f),            // blanca
        new Color(1f, 0.882f, 0.302f),    // amarilla, la de la tienda
        new Color(1f, 0.624f, 0.11f),     // naranja
        new Color(1f, 0.169f, 0.839f),    // magenta, la de los titulos de neon
        new Color(0.62f, 0.33f, 1f),      // violeta
    };

    // Mas chica que antes, y con luz (pedido de Ivan, 9/10). Lo que se ve, al 70 %: el collider
    // se agranda en la misma proporcion y pega igual. Y cada bala alumbra el piso debajo de ella
    // con un charco de su color (el shader de los faroles, aditivo y sin textura), que crece con
    // la bala: el chorro de balas pinta el piso por donde pasa. El rifle no lleva (tira muy
    // seguido), y la escopeta uno solo por tiro, mas grande y tenue (Proyectiles.LlevaLuz): con
    // uno por perdigon el piso se llenaba de transparencias. Un material por tramo, compartido,
    // como los de la bala.
    public const float EscalaVisual = 0.7f;
    public const float LadoDeLaLuz = 1.3f;      // metros, con la bala de la pistola
    public const float AlturaDeLaLuz = 0.2f;    // por encima de la vereda de la ciudad, como los charcos
    public const float LadoDeLaLuzDeLaEscopeta = 2.6f;   // la del perdigon del medio: cubre la rafaga
    public const float LuzTenue = 0.55f;                 // y mas apagada
    // El charco crece con la bala hasta aca: el de la onda de energia (3 veces la pistola) media
    // 2,25 m, y con muchas en pantalla el aditivo pasaba de una pantalla entera de relleno en el
    // Redmi (auditoria de rendimiento de la etapa 4, 13/10). El revolver (1,72 m) queda igual.
    public const float LadoMaximoDeLaLuz = 1.8f;

    private static Material[] lucesPorTramo;
    private static Material[] lucesTenuesPorTramo;
    private Transform luz;
    private Renderer dibujoDeLaLuz;

    private MeshFilter filtro;
    private Mesh mallaBase;
    private float recorridoDeLaEstela;
    public int ArmaPuesta { get; private set; }
    public bool LlevaLuz { get { return luz != null && luz.gameObject.activeSelf; } }

    private static Material[] materialesPorTramo;
    private static Material materialCritico;

    private Vector3 escalaBase;
    private Vector3 cajaBase;
    private BoxCollider caja;
    private Renderer dibujo;
    private bool preparada;

    // El static sobrevive al cambio de escena, pero las balas guardadas no.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetearPool()
    {
        pool.Clear();
        materialesPorTramo = null;
        materialCritico = null;
        lucesPorTramo = null;
        lucesTenuesPorTramo = null;
    }

    // El tramo de un daño por tiro: 0 hasta 2, 1 hasta 4, 2 hasta 8, 3 hasta 16 y 4 de ahi
    // para arriba.
    public static int Tramo(float dano)
    {
        int tramo = 0;
        for (int i = 1; i < DesdeDano.Length; i++)
            if (dano + 1e-4f >= DesdeDano[i]) tramo = i;
        return tramo;
    }

    // Viste la bala de la pistola. 'danoPorTiro' es el de la mejora con la furia, sin la critica.
    public void Vestir(float danoPorTiro, bool esCritica)
    {
        Vestir(danoPorTiro, esCritica, Proyectiles.Pistola, true, 1f);
    }

    // Viste la bala de un arma (Proyectiles): su forma y su tamaño ('escala' agranda la que lo
    // pide, como el perdigon de atras), el color del tramo de su daño o, si es critica, blanca y
    // la mitad mas grande, la malla redonda de la de energia y su charco de luz, si lo lleva.
    public void Vestir(float danoPorTiro, bool esCritica, int arma, bool conLuz, float escala)
    {
        Preparar();
        arma = Proyectiles.Arma(arma);
        ArmaPuesta = arma;
        int tramo = Tramo(danoPorTiro);
        Vector3 forma = Proyectiles.Forma[arma] * (escala * (esCritica ? Proyectiles.TamanioCritico : 1f) * EscalaVisual);
        transform.localScale = Vector3.Scale(escalaBase, forma);
        if (caja != null) caja.size = new Vector3(cajaBase.x / forma.x, cajaBase.y / forma.y, cajaBase.z / forma.z);
        if (filtro != null) filtro.sharedMesh = Proyectiles.Redonda[arma] && mallaRedonda != null ? mallaRedonda : mallaBase;
        PonerLaLuz(tramo, arma, Mathf.Max(forma.x, forma.z) / EscalaVisual, conLuz);
        recorridoDeLaEstela = 0f;
        if (dibujo == null) return;
        ArmarMateriales(materialPlano != null ? materialPlano : dibujo.sharedMaterial);
        if (materialesPorTramo != null) dibujo.sharedMaterial = esCritica ? materialCritico : materialesPorTramo[tramo];
    }

    // El charco del piso: debajo de la bala, acostado, del tamaño de la bala y el color de su
    // tramo (tambien la critica). Es hijo de la bala, asi la sigue sin costo; se mide en el mundo
    // en cada eje, que la bala cambia de escala y no siempre pareja (la trazadora del rifle).
    private void PonerLaLuz(int tramo, int arma, float tamanio, bool conLuz)
    {
        if (luz == null || materialLuz == null) return;
        if (luz.gameObject.activeSelf != conLuz) luz.gameObject.SetActive(conLuz);
        if (!conLuz) return;
        Vector3 escala = transform.localScale;
        if (escala.x <= 0f || escala.y <= 0f || escala.z <= 0f) return;
        bool escopeta = arma == Proyectiles.Escopeta;
        float lado = escopeta ? LadoDeLaLuzDeLaEscopeta : Mathf.Min(LadoDeLaLuz * Mathf.Sqrt(tamanio), LadoMaximoDeLaLuz);
        // Acostada (girada 90 grados en X): su alto va a lo largo de la bala.
        luz.localScale = new Vector3(lado / escala.x, lado / escala.z, 1f);
        luz.localPosition = new Vector3(0f, (AlturaDeLaLuz - transform.position.y) / escala.y, 0f);
        if (lucesPorTramo == null || lucesPorTramo[0] == null || lucesTenuesPorTramo == null || lucesTenuesPorTramo[0] == null)
        {
            lucesPorTramo = new Material[ColoresPorTramo.Length];
            lucesTenuesPorTramo = new Material[ColoresPorTramo.Length];
            for (int i = 0; i < lucesPorTramo.Length; i++)
            {
                lucesPorTramo[i] = new Material(materialLuz) { name = "LuzTramo" + i, color = ColoresPorTramo[i], enableInstancing = true };
                Color tenue = ColoresPorTramo[i] * LuzTenue;
                tenue.a = 1f;
                lucesTenuesPorTramo[i] = new Material(materialLuz) { name = "LuzTenueTramo" + i, color = tenue, enableInstancing = true };
            }
        }
        dibujoDeLaLuz.sharedMaterial = escopeta ? lucesTenuesPorTramo[tramo] : lucesPorTramo[tramo];
    }

    public Transform Luz { get { return luz; } }

    public Material MaterialPuesto { get { return dibujo != null ? dibujo.sharedMaterial : null; } }

    // Lo de la bala recien salida del prefab, antes de vestirla: sin Awake, asi tambien
    // anda en el editor (la prueba de logica).
    private void Preparar()
    {
        if (preparada) return;
        preparada = true;
        escalaBase = transform.localScale;
        caja = GetComponent<BoxCollider>();
        if (caja != null) cajaBase = caja.size;
        dibujo = GetComponent<Renderer>();
        filtro = GetComponent<MeshFilter>();
        if (filtro != null) mallaBase = filtro.sharedMesh;
        if (materialLuz != null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "Luz";
            // En el acto: un collider en la bala chocaria con los zombis por su cuenta.
            DestroyImmediate(go.GetComponent<Collider>());
            go.layer = gameObject.layer;
            luz = go.transform;
            luz.SetParent(transform, false);
            luz.localRotation = Quaternion.Euler(90f, 0f, 0f);
            dibujoDeLaLuz = go.GetComponent<Renderer>();
            dibujoDeLaLuz.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            dibujoDeLaLuz.receiveShadows = false;
            dibujoDeLaLuz.sharedMaterial = materialLuz;
        }
    }

    private static void ArmarMateriales(Material baseDeLaBala)
    {
        if (materialesPorTramo != null && materialesPorTramo[0] != null && materialCritico != null) return;
        if (baseDeLaBala == null) return;
        materialesPorTramo = new Material[ColoresPorTramo.Length];
        // Con instancing: todas las balas de un tramo salen en pocas llamadas de dibujo.
        for (int i = 0; i < materialesPorTramo.Length; i++)
            materialesPorTramo[i] = new Material(baseDeLaBala) { name = "BalaTramo" + i, color = ColoresPorTramo[i], enableInstancing = true };
        materialCritico = new Material(baseDeLaBala) { name = "BalaCritica", color = Proyectiles.ColorCritico, enableInstancing = true };
    }

    public static BulletController Obtener(BulletController prefab, Vector3 posicion, Quaternion rotacion)
    {
        BulletController bala = null;

        // Al cambiar de escena las balas dormidas se destruyen y en la pila
        // quedan referencias muertas. Descartarlas antes de usarlas.
        while (bala == null && pool.Count > 0) bala = pool.Pop();

        if (bala == null)
        {
            bala = Instantiate(prefab, posicion, rotacion);
            bala.lifeTimeInicial = prefab.lifeTime;
        }
        else
        {
            bala.transform.SetPositionAndRotation(posicion, rotacion);
            bala.gameObject.SetActive(true);
        }

        bala.lifeTime = bala.lifeTimeInicial;
        // El del prefab por si quien la pide no pone otro; GunController lo pisa.
        bala.danoAplicado = prefab.dañoDar;
        bala.critico = false;
        bala.perforacionesRestantes = 0;
        bala.perforacionesSinPerder = 0;
        bala.fraccionDeExplosion = 0f;
        bala.radioDeExplosion = 0f;
        bala.lentitudDelHielo = 0f;
        bala.quemaduraPorSegundo = 0f;
        bala.tocados.Clear();
        // La que perforo en su vida anterior quedo como trigger: vuelve a chocar.
        bala.Preparar();
        if (bala.caja != null) bala.caja.isTrigger = false;
        bala.enUso = true;
        return bala;
    }

    // Mueve la bala lo que habría recorrido si hubiera salido "segundos" antes.
    // Con varios tiros en el mismo frame, cada uno sale adelantado según su atraso
    // y el chorro queda parejo en vez de amontonado en la boca del arma.
    public void Adelantar(float segundos)
    {
        if (!(segundos > 0f)) return;

        transform.Translate(Vector3.forward * velocidad * segundos);
        lifeTime -= segundos;
    }

    private void Devolver()
    {
        // Sin esta guarda, dos colisiones en el mismo paso de fisica meterian
        // la misma bala dos veces en la pila y se entregaria a dos disparos.
        if (!enUso) return;

        enUso = false;
        gameObject.SetActive(false);
        pool.Push(this);
    }

    // Update is called once per frame
    void Update()
    {
        float paso = velocidad * Time.deltaTime;
        transform.Translate(Vector3.forward * paso);

        // La critica deja una estela de chispas (de las de la escena, con un techo por cuadro).
        if (critico)
        {
            recorridoDeLaEstela += paso;
            if (recorridoDeLaEstela >= Proyectiles.EstelaCadaMetros)
            {
                recorridoDeLaEstela = 0f;
                Efectos.Estela(transform.position);
            }
        }

        lifeTime -= Time.deltaTime;
        if (lifeTime < 0)
        {
            Devolver();
        }
    }

    void OnCollisionEnter(Collision other)
    {
        Pegar(other.gameObject);
    }

    // La que perfora pasa a trigger despues del primer zombi: chocando, la fisica empujaria a
    // cada zombi que atraviesa mientras esta adentro de el. El trigger llega igual, porque el
    // zombi tiene Rigidbody.
    void OnTriggerEnter(Collider other)
    {
        Pegar(other.gameObject);
    }

    private void Pegar(GameObject otro)
    {
        // Los eventos de un mismo paso de fisica se despachan todos aunque el
        // primero apague la bala: sin esta guarda, una bala que toca a dos
        // zombis superpuestos daña a los dos.
        if (!enUso) return;

        // GetComponentInParent y no GetComponent: los zombis tienen dos hitboxes
        // hijas ("Cube") ademas del capsule de la raiz, y el componente vive en
        // la raiz. Con GetComponent, una bala que pegaba en la hitbox hija
        // rebotaba sin hacer daño.
        //
        // Antes ademas habia cinco if identicos por tag que sumaban puntos ACA,
        // o sea por impacto y no por muerte. Los puntos los da EnemyController.
        //
        // Con includeInactive: un zombi que murio en este mismo paso ya volvio
        // apagado al pool, y sin eso la bala no lo encontraba y seguia de largo
        // hasta el de atras. Choca y se gasta como antes; el daño lo ignora su guarda.
        EnemyController zombi = otro.GetComponentInParent<EnemyController>(true);
        if (zombi == null) return;
        // Uno ya tocado (otro de sus colliders, o el que acaba de atravesar) no cuenta. Un cadaver
        // de este mismo paso si: la bala se gasta igual, y tambien una perforacion.
        if (tocados.Contains(zombi)) return;
        tocados.Add(zombi);

        // La direccion de la bala, para que el cadaver salga despedido hacia donde
        // iba el tiro y no siempre igual.
        zombi.DanoZombi(danoAplicado, critico, transform.forward);
        if (lentitudDelHielo > 0f) zombi.Congelar(lentitudDelHielo, PoderDeLaPartida.DuracionDelHielo);
        if (quemaduraPorSegundo > 0f) zombi.Quemar(quemaduraPorSegundo, PoderDeLaPartida.DuracionDeLaQuemadura);
        if (fraccionDeExplosion > 0f && radioDeExplosion > 0f) Explotar(zombi);

        // La de energia atraviesa por su cuenta, sin perder daño; despues, las de la carta.
        if (perforacionesSinPerder > 0)
        {
            perforacionesSinPerder--;
            if (caja != null) caja.isTrigger = true;
            return;
        }
        if (perforacionesRestantes > 0)
        {
            perforacionesRestantes--;
            danoAplicado *= PoderDeLaPartida.DanoAlPerforar;
            if (caja != null) caja.isTrigger = true;
            return;
        }
        Devolver();
    }

    // La explosiva: una parte del daño a los que estan cerca del que toco, una vez a cada uno,
    // sin critico y empujando para afuera, como la granada.
    private void Explotar(EnemyController tocado)
    {
        Vector3 centro = tocado.transform.position;
        float dano = danoAplicado * fraccionDeExplosion;
        Efectos.ExplosionChica(centro + Vector3.up, ColorDeLaExplosion);
        int n = Physics.OverlapSphereNonAlloc(centro, radioDeExplosion, enLaExplosion);
        danadosPorLaExplosion.Clear();
        danadosPorLaExplosion.Add(tocado);
        for (int i = 0; i < n; i++)
        {
            var zombi = enLaExplosion[i].GetComponentInParent<EnemyController>();
            if (zombi == null || !danadosPorLaExplosion.Add(zombi)) continue;
            Vector3 afuera = zombi.transform.position - centro;
            afuera.y = 0f;
            zombi.DanoZombi(dano, false, afuera.sqrMagnitude > 1e-4f ? afuera.normalized : Vector3.zero, ColorDeLaExplosion);
        }
    }

    public static readonly Color ColorDeLaExplosion = new Color(1f, 0.62f, 0.2f);
}
