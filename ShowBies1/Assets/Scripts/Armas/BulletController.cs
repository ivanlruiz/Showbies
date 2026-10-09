using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletController : MonoBehaviour
{
    public int velocidad;
    public float lifeTime;
    public int dañoDar;   // respaldo del prefab: el daño de verdad lo pone el arma en cada tiro

    // El daño de esta bala, puesto en cada tiro por GunController desde la mejora
    // de daño. No se serializa: si se guardara en el prefab, el primer tiro de la
    // partida siguiente arrancaría con el daño de la anterior.
    [System.NonSerialized] public float danoAplicado;
    [System.NonSerialized] public bool critico;   // lo decide el arma al dispararla; cambia el numero que se ve

    // Pool de balas.
    //
    // Son de 4 a 20 tiros por segundo con la mejora, hasta 60 con la caja de arma
    // y mas con la furia, con un techo de 120. Cada una era un Instantiate y un
    // Destroy: de lejos la mayor fuente de basura del juego.
    private static readonly Stack<BulletController> pool = new Stack<BulletController>();

    private float lifeTimeInicial;
    private bool enUso;

    // Lo que se compra se ve en el tiro (revision del 9/10, mejora 6): la bala cambia de color
    // y crece por tramos del daño que lleva cada tiro (la mejora por la furia), y la critica
    // sale roja y mas grande desde el arma. Los tramos se duplican (1, 2, 4, 8 y 16): la
    // primera compra ya cambia el color, y la furia, que pega x2, sube justo un tramo. El
    // cuarto es magenta y no rojo, como decia el informe: el rojo es de la critica, y una bala
    // roja tiene que leerse como una sola cosa. Un material por tramo, armado una vez a partir
    // del de la bala y compartido por todas (asi no se rompe el batching); Bullet.mat no se
    // toca, que tambien pinta el brillo de la caja de balas. Solo crece lo que se ve: el
    // collider se achica en la misma proporcion, y la bala pega igual que antes.
    public static readonly float[] DesdeDano = { 0f, 2f, 4f, 8f, 16f };
    public static readonly Color[] ColoresPorTramo =
    {
        new Color(1f, 1f, 1f),            // blanca
        new Color(1f, 0.882f, 0.302f),    // amarilla, la de la tienda
        new Color(1f, 0.624f, 0.11f),     // naranja
        new Color(1f, 0.169f, 0.839f),    // magenta, la de los titulos de neon
        new Color(0.62f, 0.33f, 1f),      // violeta
    };
    public static readonly float[] TamanioPorTramo = { 1f, 1.2f, 1.45f, 1.7f, 2f };
    public static readonly Color ColorCritico = new Color(1f, 0.231f, 0.361f);   // el rojo del neon
    public const float TamanioCritico = 1.3f;   // por encima del de su tramo

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

    // Viste la bala para el tiro: el color y el tamaño de su tramo, o los de la critica.
    // 'danoPorTiro' es el de la mejora con la furia, sin la critica.
    public void Vestir(float danoPorTiro, bool esCritica)
    {
        Preparar();
        int tramo = Tramo(danoPorTiro);
        float tamanio = TamanioPorTramo[tramo] * (esCritica ? TamanioCritico : 1f);
        transform.localScale = escalaBase * tamanio;
        if (caja != null) caja.size = cajaBase / tamanio;
        if (dibujo == null) return;
        ArmarMateriales(dibujo.sharedMaterial);
        if (materialesPorTramo != null) dibujo.sharedMaterial = esCritica ? materialCritico : materialesPorTramo[tramo];
    }

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
    }

    private static void ArmarMateriales(Material baseDeLaBala)
    {
        if (materialesPorTramo != null && materialesPorTramo[0] != null && materialCritico != null) return;
        if (baseDeLaBala == null) return;
        materialesPorTramo = new Material[ColoresPorTramo.Length];
        for (int i = 0; i < materialesPorTramo.Length; i++)
            materialesPorTramo[i] = new Material(baseDeLaBala) { name = "BalaTramo" + i, color = ColoresPorTramo[i] };
        materialCritico = new Material(baseDeLaBala) { name = "BalaCritica", color = ColorCritico };
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
        transform.Translate(Vector3.forward * velocidad * Time.deltaTime);

        lifeTime -= Time.deltaTime;
        if (lifeTime < 0)
        {
            Devolver();
        }
    }

    void OnCollisionEnter(Collision other)
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
        EnemyController zombi = other.gameObject.GetComponentInParent<EnemyController>(true);
        if (zombi == null) return;

        // La direccion de la bala, para que el cadaver salga despedido hacia donde
        // iba el tiro y no siempre igual.
        zombi.DanoZombi(danoAplicado, critico, transform.forward);
        Devolver();
    }
}
