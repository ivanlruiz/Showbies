using UnityEngine;

// El botón MEJORAS del menú y de la derrota. Muestra en una insignia cuántas
// compras alcanzan las monedas y, si tiene un aviso, lo dice con palabras. El
// botón se queda quieto: lo que late es la insignia, y sólo si hay algo para
// comprar (pedido de Ivan: que llame la atención el número, no el botón).
//
// La cuenta es CatalogoMejoras.ComprasPosibles: compras reales encadenadas de la
// más barata a la siguiente, no monedas divididas por el precio más bajo, así la
// insignia no promete una compra que después no alcanza.
//
// Sin eventos: recalcula al activarse y cuando cambia Progreso.Revision, que
// sube con cada compra y cada moneda sumada.
public class BotonMejoras : MonoBehaviour
{
    public GameObject insignia;
    public TMPro.TMP_Text textoInsignia;
    public TMPro.TMP_Text aviso;
    public BotonJugoso jugo;

    private const float DuracionGolpe = 0.3f;
    private const float EscalaGolpe = 1.4f;
    private const float PeriodoLatido = 1.4f;       // segundos entre dos latidos
    private const float DuracionLatido = 0.35f;
    private const float EscalaLatido = 1.25f;
    private const float GiroLatido = 10f;           // grados del bamboleo

    private int revisionVista;
    private int idiomaVisto = -1;
    private bool avisoTapado;
    private int comprasMostradas = -1;               // -1: todavía no se mostró nada
    private float tiempoGolpe = -1f;                 // negativo: quieta
    private Vector3 escalaBaseInsignia = Vector3.one;
    private Quaternion rotacionBaseInsignia = Quaternion.identity;
    private float relojLatido;

    private void Awake()
    {
        if (insignia != null)
        {
            escalaBaseInsignia = insignia.transform.localScale;
            rotacionBaseInsignia = insignia.transform.localRotation;
        }
    }

    private void OnEnable()
    {
        Actualizar();
    }

    private void OnDisable()
    {
        tiempoGolpe = -1f;
        relojLatido = 0f;
        if (insignia != null)
        {
            insignia.transform.localScale = escalaBaseInsignia;
            insignia.transform.localRotation = rotacionBaseInsignia;
        }
    }

    private void Update()
    {
        // La oferta de duplicar de la derrota ocupa el renglon del aviso: mientras
        // esta, el aviso se calla, y vuelve cuando se resuelve (con mas monedas si
        // el jugador cobro).
        if (Progreso.Revision != revisionVista || avisoTapado != OfertaDeDuplicar.TapaElAviso
            || idiomaVisto != Idioma.Revision) Actualizar();

        if (insignia == null || !insignia.activeSelf) return;
        float dt = Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);

        // El golpe (cambió la cuenta) manda sobre el latido.
        if (tiempoGolpe >= 0f)
        {
            tiempoGolpe += dt;
            float t = tiempoGolpe / DuracionGolpe;
            if (t >= 1f)
            {
                tiempoGolpe = -1f;
                relojLatido = 0f;
                Pose(1f, 0f);
                return;
            }
            Pose(1f + (EscalaGolpe - 1f) * CurvasUI.Campana(t), 0f);
            return;
        }

        relojLatido = (relojLatido + dt) % PeriodoLatido;
        float fase = relojLatido / DuracionLatido;
        if (fase >= 1f) { Pose(1f, 0f); return; }
        float campana = CurvasUI.Campana(fase);
        Pose(1f + (EscalaLatido - 1f) * campana, Mathf.Sin(fase * Mathf.PI * 2f) * GiroLatido * campana);
    }

    private void Pose(float escala, float grados)
    {
        insignia.transform.localScale = escalaBaseInsignia * escala;
        insignia.transform.localRotation = rotacionBaseInsignia * Quaternion.Euler(0f, 0f, grados);
    }

    private void Actualizar()
    {
        revisionVista = Progreso.Revision;
        idiomaVisto = Idioma.Revision;
        avisoTapado = OfertaDeDuplicar.TapaElAviso;
        int compras = CatalogoMejoras.ComprasPosibles();

        // La primera vez muestra lo que hay sin festejar; después, cada cambio
        // (volver de la tienda habiendo comprado) hace saltar la insignia.
        if (comprasMostradas >= 0 && compras != comprasMostradas) tiempoGolpe = 0f;
        bool cambio = compras != comprasMostradas;
        comprasMostradas = compras;

        bool hay = compras > 0;
        if (insignia != null && insignia.activeSelf != hay) insignia.SetActive(hay);
        if (textoInsignia != null && cambio)
            textoInsignia.text = compras >= 99 ? "99+" : compras.ToString();

        if (aviso != null)
        {
            bool mostrarAviso = hay && !avisoTapado;
            if (aviso.gameObject.activeSelf != mostrarAviso) aviso.gameObject.SetActive(mostrarAviso);
            if (mostrarAviso)
                aviso.text = compras == 1 ? Textos.De("aviso_compras_una") : Textos.Formato("aviso_compras_varias", compras);
        }

        if (jugo != null) jugo.respirar = false;
    }
}
