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
    public static readonly Quaternion GiroEnLaMano = Quaternion.Euler(0f, 0f, 90f);

    private Transform instancia, antebrazo, muñeco;
    private int tirosVistos;
    private float patada;

    private Transform fogonazo;
    private Renderer renderDelFogonazo;
    private MaterialPropertyBlock propiedades;
    private float fogonazoDesde = -1f;
    private float fogonazoHasta = -1f;
    private float tamañoDelFogonazo = 1f;
    private float giroDelFogonazo;
    private static readonly int IdGiro = Shader.PropertyToID("_Giro");
    private static readonly int IdColor = Shader.PropertyToID("_Color");
    private static readonly int IdPuntas = Shader.PropertyToID("_Puntas");
    private static readonly int IdAnillo = Shader.PropertyToID("_Anillo");
    private static readonly int IdRadioAnillo = Shader.PropertyToID("_RadioAnillo");

    // --- El arma que cambia con el daño (1.6.0, etapa 3 de las cartas) ----------------------
    //
    // Un arma por tramo del daño (el de GunController.TramoDelArma, sin la furia), como la bala:
    // pistola, revolver, escopeta, rifle y arma de energia. No son cinco modelos (de la revision
    // de arte de Gemini, 12/10: a 15 px, desde arriba, un revolver y una pistola son la misma
    // mancha): es la misma pistola gris con el cañon mas largo (y mas ancho en la escopeta), una
    // luz del color del tramo encima, y sobre todo el fogonazo y el sonido de cada una. Al subir de
    // arma en la partida, un festejo con su nombre (Efectos.ArmaNueva); al empezar, no.
    public const int Armas = 5;
    public static readonly string[] IdsDeLasArmas = { "arma_pistola", "arma_revolver", "arma_escopeta", "arma_rifle", "arma_energia" };
    // El cañon, desde el frente de la corredera, y su ancho, en unidades de la mano (la corredera
    // mide 0,3). La pistola es la de siempre.
    public static readonly float[] LargoDelCanon = { 0.04f, 0.13f, 0.26f, 0.5f, 0.34f };
    public static readonly float[] AnchoDelCanon = { 0.045f, 0.05f, 0.095f, 0.04f, 0.065f };
    // El fogonazo de cada una: su largo (por largoDelFogonazo), lo ancho que es, las puntas (0 es
    // un disco), lo que dura, y si es un anillo que crece (la de energia, sin fuego).
    public static readonly float[] LargoDelFogonazoPorArma = { 0.6f, 0.75f, 1.05f, 1.15f, 0.95f };
    public static readonly float[] AnchoDelFogonazoPorArma = { 1f, 1f, 1.25f, 0.6f, 1f };
    public static readonly float[] PuntasPorArma = { 0f, 0f, 7f, 4f, 0f };
    public static readonly float[] DuracionPorArma = { 0.05f, 0.05f, 0.1f, 0.04f, 0.12f };
    public static readonly bool[] AnilloPorArma = { false, false, false, false, true };
    // Lo que dice el constructor de la pistola (ConstructorArmas): donde termina la corredera y
    // a que altura va el cañon.
    public const float FrenteDeLaCorredera = 0.225f;
    public const float AlturaDelCanon = 0.075f;
    // El fuego de la pistola es el de siempre; las demas, del color de su bala.
    public static readonly Color FuegoDeLaPistola = new Color(1f, 0.72f, 0.25f);

    private int tramoPuesto = -1;
    private bool festejoPendiente;
    private MaterialPropertyBlock propiedadesDeLaLuz;

    public int TramoPuesto { get { return tramoPuesto; } }
    public int ArmasNuevas { get; private set; }

    public static Color ColorDelArma(int tramo)
    {
        return BulletController.ColoresPorTramo[Mathf.Clamp(tramo, 0, Armas - 1)];
    }

    // Viste la pistola para un tramo: el largo y el ancho del cañon, la boca en la punta y la luz
    // de su color. Estatico: lo usan la partida y las fotos (FotosDeLasArmas).
    public static void VestirArma(Transform pistola, int tramo, MaterialPropertyBlock propiedades)
    {
        if (pistola == null) return;
        tramo = Mathf.Clamp(tramo, 0, Armas - 1);
        float largo = LargoDelCanon[tramo], ancho = AnchoDelCanon[tramo];
        var canon = pistola.Find("Canon");
        if (canon != null)
        {
            // Un cilindro acostado: su alto (2 unidades) va a lo largo del cañon.
            canon.localPosition = new Vector3(0f, AlturaDelCanon, FrenteDeLaCorredera + largo * 0.5f);
            canon.localScale = new Vector3(ancho, largo * 0.5f, ancho);
        }
        var boca = pistola.Find("Boca");
        if (boca != null) boca.localPosition = new Vector3(0f, AlturaDelCanon, FrenteDeLaCorredera + largo + 0.005f);
        var luz = pistola.Find("LED");
        if (luz != null && propiedades != null)
        {
            propiedades.SetColor(IdColor, ColorDelArma(tramo));
            foreach (var render in luz.GetComponentsInChildren<Renderer>(true))
                render.SetPropertyBlock(propiedades);
        }
    }

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

        propiedadesDeLaLuz = new MaterialPropertyBlock();
        antebrazo = animador.GetBoneTransform(HumanBodyBones.RightLowerArm);
        muñeco = animador.transform;
        tirosVistos = arma.TirosDisparados;

        // La remera blanca, el aura y la sombra falsa (AspectoDelHeroe): aca, que es donde se
        // encuentra el modelo.
        gameObject.AddComponent<AspectoDelHeroe>().Vestir(animador.transform);
    }

    // Despues del Animator, que escribe los huesos en cada cuadro: lo que se toca antes se
    // pierde.
    private void LateUpdate()
    {
        if (instancia == null) return;

        // El arma del tramo de ahora; al subir en la partida, el festejo (al empezar, no). Se viste
        // en el acto, pero el festejo espera a que el juego ande: el tramo sube al elegir una carta
        // de daño, con la eleccion todavia congelando la pantalla.
        int tramo = arma.TramoDelArma;
        if (tramo != tramoPuesto)
        {
            festejoPendiente = tramoPuesto >= 0 && tramo > tramoPuesto;
            tramoPuesto = tramo;
            VestirArma(instancia, tramo, propiedadesDeLaLuz);
        }
        if (festejoPendiente && !MenuPausa.JuegoCongelado)
        {
            festejoPendiente = false;
            ArmasNuevas++;
            Efectos.ArmaNueva(Boca != null ? Boca.position : transform.position, ColorDelArma(tramo), Textos.De(IdsDeLasArmas[tramo]));
        }

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
        int tramo = Mathf.Clamp(tramoPuesto, 0, Armas - 1);
        // La duracion del inspector es la de la pistola; las demas, en proporcion.
        fogonazoDesde = Time.unscaledTime;
        fogonazoHasta = fogonazoDesde + duracionDelFogonazo * DuracionPorArma[tramo] / DuracionPorArma[0];
        tamañoDelFogonazo = Random.Range(0.8f, 1.15f);
        giroDelFogonazo = Random.Range(0f, Mathf.PI * 2f);
        PintarFogonazo(renderDelFogonazo, propiedades, tramo, giroDelFogonazo, 0f);
        fogonazo.gameObject.SetActive(true);
    }

    private void AcomodarElFogonazo()
    {
        if (fogonazo == null || !fogonazo.gameObject.activeSelf) return;
        var camara = Camera.main;
        if (camara == null || Time.unscaledTime >= fogonazoHasta)
        {
            fogonazo.gameObject.SetActive(false);
            return;
        }
        int tramo = Mathf.Clamp(tramoPuesto, 0, Armas - 1);
        AcomodarFogonazo(fogonazo, Boca, camara.transform, tramo, largoDelFogonazo * tamañoDelFogonazo);
        // La de energia: el anillo crece mientras dura.
        if (AnilloPorArma[tramo])
            PintarFogonazo(renderDelFogonazo, propiedades, tramo, giroDelFogonazo, Mathf.InverseLerp(fogonazoDesde, fogonazoHasta, Time.unscaledTime));
    }

    // El fogonazo de un arma: su color, sus puntas o su anillo (t es cuanto del fogonazo paso,
    // de 0 a 1: el anillo crece). Estatico: lo usan la partida y las fotos (FotosDeLasArmas).
    public static void PintarFogonazo(Renderer render, MaterialPropertyBlock propiedades, int tramo, float giro, float t)
    {
        if (render == null || propiedades == null) return;
        tramo = Mathf.Clamp(tramo, 0, Armas - 1);
        propiedades.SetFloat(IdGiro, giro);
        propiedades.SetColor(IdColor, tramo == 0 ? FuegoDeLaPistola : ColorDelArma(tramo));
        propiedades.SetFloat(IdPuntas, PuntasPorArma[tramo]);
        propiedades.SetFloat(IdAnillo, AnilloPorArma[tramo] ? 1f : 0f);
        propiedades.SetFloat(IdRadioAnillo, Mathf.Lerp(0.25f, 0.9f, t));
        render.SetPropertyBlock(propiedades);
    }

    // De frente a la camara, estirado hacia donde apunta el cañon y corrido hacia adelante:
    // el fuego sale de la boca, no la rodea. "metros" es el largo del de la pistola.
    public static void AcomodarFogonazo(Transform fogonazo, Transform boca, Transform camara, int tramo, float metros)
    {
        tramo = Mathf.Clamp(tramo, 0, Armas - 1);
        Vector3 cañon = boca.forward;
        Vector3 frente = camara.forward;
        Vector3 largo = Vector3.ProjectOnPlane(cañon, frente);
        largo = largo.sqrMagnitude > 0.0001f ? largo.normalized : camara.right;
        metros *= LargoDelFogonazoPorArma[tramo];
        bool anillo = AnilloPorArma[tramo];
        fogonazo.SetPositionAndRotation(boca.position + cañon * (metros * (anillo ? 0.2f : 0.35f)),
                                        Quaternion.LookRotation(frente, Vector3.Cross(frente, largo)));
        // El padre viene escalado con la mano: el tamaño se lleva a metros.
        float escalaDelPadre = Mathf.Max(0.0001f, fogonazo.parent.lossyScale.x);
        float ancho = anillo ? 1f : 0.7f * AnchoDelFogonazoPorArma[tramo];
        fogonazo.localScale = new Vector3(metros, metros * ancho, 1f) / escalaDelPadre;
    }
}
