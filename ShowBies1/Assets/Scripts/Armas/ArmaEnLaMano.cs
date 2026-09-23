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

    // Los ejes de la mano en las poses de pistola del pack: +Z del contenedor es el frente
    // del muñeco y -X es arriba (medido muestreando los clips). La pistola tiene el cañon
    // en +Z y arriba en +Y, asi que va girada un cuarto de vuelta.
    private static readonly Quaternion GiroEnLaMano = Quaternion.Euler(0f, 0f, 90f);

    private Transform instancia, antebrazo, muñeco;
    private int tirosVistos;
    private float patada;

    public Transform Boca { get; private set; }

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
        Boca = instancia.Find("Boca");
        arma.boca = Boca;

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
        }
        // Se apaga con exp(-k dt): tarda lo mismo a 30 FPS que a 120.
        patada *= Mathf.Exp(-recuperacion * Time.deltaTime);
        if (patada < 0.001f) patada = 0f;

        instancia.localPosition = Vector3.back * (retroceso * patada);
        instancia.localRotation = GiroEnLaMano;
        if (patada > 0f && antebrazo != null)
            antebrazo.rotation = Quaternion.AngleAxis(-levantada * patada, muñeco.right) * antebrazo.rotation;
    }
}
