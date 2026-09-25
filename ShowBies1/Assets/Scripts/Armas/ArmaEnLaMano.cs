using UnityEngine;

// La pistola en la mano del muñeco, pedido de Ivan. El modelo del pack (TT_demo_male_A)
// traia un bate en la mano, y con las poses de pistola lo levantaba delante de la cara;
// las balas salian de un cubito (el objeto Gun) que flotaba a la altura del pecho. Asi,
// corriendo y disparando no se leia que estuviera disparando.
//
// Al arrancar esconde el bate y el cubito, pone la pistola (la arma ConstructorArmas) en
// la mano derecha y le pasa su boca al arma: las balas y el fogonazo salen de ahi, con la
// direccion de siempre, la del Gun, que es hacia donde se apunta (la mano se mueve con la
// animacion y no sirve para apuntar). En cada tiro la pistola patea: el antebrazo se
// levanta y el arma va para atras, y se recupera sola.
//
// El modelo no esta en el prefab Jugador sino en cada escena, agregado a su instancia:
// por eso se busca al arrancar en vez de cablearse.
public class ArmaEnLaMano : MonoBehaviour
{
    public GameObject pistola;
    public GunController arma;

    [Header("La patada de cada tiro")]
    [Tooltip("Cuanto va hacia atras la pistola, en unidades de la mano (la pistola mide 0,34).")]
    public float retroceso = 0.1f;
    [Tooltip("Cuanto se levanta el antebrazo, en grados.")]
    public float levantada = 16f;
    [Tooltip("Que tan rapido se recupera: mas alto, mas seca.")]
    public float recuperacion = 22f;

    // El fogonazo, pedido de Ivan para que el tiro se lea desde la camara del juego, donde
    // la pistola mide unos 15 px: una estrella de fuego en la boca en cada tiro (el shader
    // ShowBies/Fogonazo), de frente a la camara y estirada hacia donde apunta el cañon, con
    // tamaño y puntas al azar. Dura un par de cuadros: con la cadencia alta titila, que es
    // como se lee una rafaga.
    [Header("El fogonazo de cada tiro")]
    [Tooltip("Cuanto dura, en segundos sin escalar.")]
    public float duracionDelFogonazo = 0.05f;
    [Tooltip("Su largo, en metros, a lo largo del cañon. Con 0,5 media unos 20 px en la camara del juego.")]
    public float largoDelFogonazo = 0.65f;

    // Los ejes de la mano en las poses de pistola del pack: +Z del contenedor es el frente
    // del muñeco y -X es arriba (medido muestreando los clips). La pistola tiene el cañon
    // en +Z y arriba en +Y, asi que va girada un cuarto de vuelta.
    private static readonly Quaternion GiroEnLaMano = Quaternion.Euler(0f, 0f, 90f);

    private Transform instancia, antebrazo, muñeco;
    private int tirosVistos;
    private float patada;

    private Transform fogonazo;
    private Renderer renderDelFogonazo;
    private MaterialPropertyBlock propiedades;
    private float fogonazoHasta = -1f;
    private float tamañoDelFogonazo = 1f;
    private static readonly int IdGiro = Shader.PropertyToID("_Giro");

    public Transform Boca { get; private set; }

    // Cuantos fogonazos salieron y si hay uno prendido. Para las pruebas.
    public int Fogonazos { get; private set; }
    public bool FogonazoPrendido { get { return fogonazo != null && fogonazo.gameObject.activeSelf; } }

    private void Start()
    {
        var animador = GetComponentInChildren<Animator>();
        if (animador == null || pistola == null || arma == null) return;
        Transform mano = animador.isHuman ? animador.GetBoneTransform(HumanBodyBones.RightHand) : null;
        if (mano == null) return;

        // El contenedor que trae el modelo para lo que lleva en la mano, y adentro el bate.
        Transform contenedor = mano.Find("R_hand_container");
        if (contenedor == null) contenedor = mano;
        foreach (Transform hijo in contenedor)
            hijo.gameObject.SetActive(false);

        foreach (var render in arma.GetComponentsInChildren<Renderer>(true))
            render.enabled = false;

        instancia = Instantiate(pistola, contenedor).transform;
        instancia.localPosition = Vector3.zero;
        instancia.localRotation = GiroEnLaMano;
        instancia.localScale = Vector3.one;
        Personajes.PonerEnLaCapa(instancia.gameObject);
        Boca = instancia.Find("Boca");
        arma.boca = Boca;
        fogonazo = Boca != null ? Boca.Find("Fogonazo") : null;
        if (fogonazo != null)
        {
            renderDelFogonazo = fogonazo.GetComponent<Renderer>();
            propiedades = new MaterialPropertyBlock();
            fogonazo.gameObject.SetActive(false);
        }

        antebrazo = animador.GetBoneTransform(HumanBodyBones.RightLowerArm);
        muñeco = animador.transform;
        tirosVistos = arma.TirosDisparados;
    }

    // Despues del Animator, que escribe los huesos en cada cuadro: lo que se toca antes se
    // pierde.
    private void LateUpdate()
    {
        if (instancia == null) return;

        if (arma.TirosDisparados != tirosVistos)
        {
            tirosVistos = arma.TirosDisparados;
            patada = 1f;
            PrenderElFogonazo();
        }
        // Se apaga con exp(-k dt): tarda lo mismo a 30 FPS que a 120.
        patada *= Mathf.Exp(-recuperacion * Time.deltaTime);
        if (patada < 0.001f) patada = 0f;

        instancia.localPosition = Vector3.back * (retroceso * patada);
        instancia.localRotation = GiroEnLaMano;
        if (patada > 0f && antebrazo != null)
            antebrazo.rotation = Quaternion.AngleAxis(-levantada * patada, muñeco.right) * antebrazo.rotation;

        // Despues de la patada, que mueve la boca.
        AcomodarElFogonazo();
    }

    private void PrenderElFogonazo()
    {
        if (fogonazo == null) return;
        Fogonazos++;
        fogonazoHasta = Time.unscaledTime + duracionDelFogonazo;
        tamañoDelFogonazo = Random.Range(0.8f, 1.15f);
        if (renderDelFogonazo != null)
        {
            propiedades.SetFloat(IdGiro, Random.Range(0f, Mathf.PI * 2f));
            renderDelFogonazo.SetPropertyBlock(propiedades);
        }
        fogonazo.gameObject.SetActive(true);
    }

    // De frente a la camara, estirado hacia donde apunta el cañon y corrido hacia adelante:
    // el fuego sale de la boca, no la rodea.
    private void AcomodarElFogonazo()
    {
        if (fogonazo == null || !fogonazo.gameObject.activeSelf) return;
        var camara = Camera.main;
        if (camara == null || Time.unscaledTime >= fogonazoHasta)
        {
            fogonazo.gameObject.SetActive(false);
            return;
        }

        Vector3 cañon = Boca.forward;
        Vector3 frente = camara.transform.forward;
        Vector3 largo = Vector3.ProjectOnPlane(cañon, frente);
        largo = largo.sqrMagnitude > 0.0001f ? largo.normalized : camara.transform.right;
        float metros = largoDelFogonazo * tamañoDelFogonazo;
        fogonazo.SetPositionAndRotation(Boca.position + cañon * (metros * 0.35f),
                                        Quaternion.LookRotation(frente, Vector3.Cross(frente, largo)));
        // El padre viene escalado con la mano: el tamaño se lleva a metros.
        float escalaDelPadre = Mathf.Max(0.0001f, fogonazo.parent.lossyScale.x);
        fogonazo.localScale = new Vector3(metros, metros * 0.7f, 1f) / escalaDelPadre;
    }
}
