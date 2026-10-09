using UnityEngine;

// Como se ve una caja (un power-up) en el piso (pedido de Ivan, 9/10): el dibujo flota, se mece y
// late de frente a la camara, sale con un salto, y un anillo en el piso se va vaciando con el
// tiempo que le queda (PickupCaducidad); antes solo parpadeaba los ultimos 3 s. El dibujo, el
// halo y el charco los arma ConstructorPowerUps en el prefab; el anillo lo arma este componente
// en Awake, asi PickupCaducidad, que en Start junta los renderers para el parpadeo del final, lo
// hace parpadear tambien.
public class AspectoDeCaja : MonoBehaviour
{
    public Transform modelo;
    public Color color = Color.white;    // el de la caja: su cartel y sus chispas al agarrarla
    public Material materialAnillo;
    public float flotar = 0.1f;          // metros, arriba y abajo
    public float periodo = 1.4f;         // segundos de una subida y bajada
    public float vaiven = 10f;           // grados, de lado a lado
    public float latido = 0.06f;         // cuanto crece y se achica
    public float duracionSalto = 0.35f;  // lo que tarda en salir
    public float radioAnillo = 0.95f;
    public float anchoAnillo = 0.08f;
    public float alturaAnillo = -0.29f;  // sobre la raiz: a 0,21 m del piso, por encima del cordon

    private const int Segmentos = 48;
    private LineRenderer anillo;
    private PickupCaducidad caducidad;
    private Vector3 posicionBase;
    private Quaternion giroBase;
    private Vector3 escalaBase;
    private float nacio;
    private float fase;
    private int puntosPuestos = -1;

    // El color de la caja que se agarra, o 'siNo' si no tiene este componente (una caja vieja).
    public static Color ColorDe(Component caja, Color siNo)
    {
        var aspecto = caja != null ? caja.GetComponent<AspectoDeCaja>() : null;
        return aspecto != null ? aspecto.color : siNo;
    }

    private void Awake()
    {
        caducidad = GetComponent<PickupCaducidad>();
        if (modelo != null)
        {
            posicionBase = modelo.localPosition;
            giroBase = modelo.localRotation;
            escalaBase = modelo.localScale;
        }
        // Que no latan todas a la vez.
        fase = Random.value * periodo;
        nacio = Time.time;
        ArmarAnillo();
    }

    private void ArmarAnillo()
    {
        if (materialAnillo == null) return;
        var go = new GameObject("Anillo");
        go.layer = gameObject.layer;
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, alturaAnillo, 0f);
        // Acostado: con TransformZ la cinta mira hacia arriba, a la camara.
        go.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        anillo = go.AddComponent<LineRenderer>();
        anillo.useWorldSpace = false;
        anillo.alignment = LineAlignment.TransformZ;
        // El material de sprites de Unity, como la linea del jefe: pinta las dos caras (con
        // Unlit/Color la cinta acostada miraba para abajo y no se veia) y toma el color de la linea.
        anillo.sharedMaterial = materialAnillo;
        anillo.startColor = anillo.endColor = color;
        anillo.widthMultiplier = anchoAnillo;
        anillo.numCapVertices = 2;
        anillo.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        anillo.receiveShadows = false;
        Dibujar(1f);
    }

    private void Update()
    {
        if (modelo != null)
        {
            float vivo = Time.time - nacio;
            float ciclo = (vivo + fase) / Mathf.Max(0.01f, periodo) * Mathf.PI * 2f;
            float salto = CurvasUI.SalidaAtras(Mathf.Clamp01(vivo / Mathf.Max(0.01f, duracionSalto)));
            modelo.localPosition = posicionBase + Vector3.up * (Mathf.Sin(ciclo) * flotar);
            modelo.localRotation = giroBase * Quaternion.Euler(0f, 0f, Mathf.Sin(ciclo * 0.5f) * vaiven);
            modelo.localScale = escalaBase * (salto * (1f + Mathf.Sin(ciclo * 2f) * latido));
        }
        if (anillo != null) Dibujar(caducidad != null ? caducidad.Restante01 : 1f);
    }

    // El arco de lo que queda, desde arriba en la pantalla (hacia +Z, que es para donde mira la
    // camara) y en el sentido de las agujas del reloj. Se vuelve a escribir solo cuando cambia de
    // tramo: un tramo es 1/48 de la vuelta, uno cada 0,6 s con los 30 s de vida.
    private void Dibujar(float queda)
    {
        int puntos = Mathf.Clamp(Mathf.CeilToInt(Segmentos * Mathf.Clamp01(queda)), 0, Segmentos) + 1;
        if (puntos == puntosPuestos) return;
        puntosPuestos = puntos;
        if (puntos < 2)
        {
            anillo.positionCount = 0;
            return;
        }
        anillo.positionCount = puntos;
        for (int i = 0; i < puntos; i++)
        {
            // En el plano XY del anillo, que esta acostado: su -Y es el +Z del mundo.
            float a = -Mathf.PI * 0.5f + i * (Mathf.PI * 2f / Segmentos);
            anillo.SetPosition(i, new Vector3(Mathf.Cos(a) * radioAnillo, Mathf.Sin(a) * radioAnillo, 0f));
        }
    }
}
