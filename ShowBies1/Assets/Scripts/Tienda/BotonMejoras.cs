using UnityEngine;

// El botón MEJORAS del menú y de la derrota. Muestra en una insignia cuántas
// compras alcanzan las monedas y, si tiene un aviso, lo dice con palabras; el
// botón respira sólo si hay algo para comprar.
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
    public string formatoAviso = "¡Te alcanza para {0} mejoras!";
    public string formatoAvisoUna = "¡Te alcanza para una mejora!";
    public BotonJugoso jugo;

    private const float DuracionGolpe = 0.3f;
    private const float EscalaGolpe = 1.4f;

    private int revisionVista;
    private bool avisoTapado;
    private int comprasMostradas = -1;               // -1: todavía no se mostró nada
    private float tiempoGolpe = -1f;                 // negativo: quieta
    private Vector3 escalaBaseInsignia = Vector3.one;

    private void Awake()
    {
        if (insignia != null) escalaBaseInsignia = insignia.transform.localScale;
    }

    private void OnEnable()
    {
        Actualizar();
    }

    private void OnDisable()
    {
        tiempoGolpe = -1f;
        if (insignia != null) insignia.transform.localScale = escalaBaseInsignia;
    }

    private void Update()
    {
        // La oferta de duplicar de la derrota ocupa el renglon del aviso: mientras
        // esta, el aviso se calla, y vuelve cuando se resuelve (con mas monedas si
        // el jugador cobro).
        if (Progreso.Revision != revisionVista || avisoTapado != OfertaDeDuplicar.TapaElAviso) Actualizar();

        if (tiempoGolpe < 0f || insignia == null) return;

        tiempoGolpe += Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);
        float t = tiempoGolpe / DuracionGolpe;
        if (t >= 1f)
        {
            tiempoGolpe = -1f;
            insignia.transform.localScale = escalaBaseInsignia;
            return;
        }
        insignia.transform.localScale = escalaBaseInsignia * (1f + (EscalaGolpe - 1f) * CurvasUI.Campana(t));
    }

    private void Actualizar()
    {
        revisionVista = Progreso.Revision;
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
                aviso.text = compras == 1 ? formatoAvisoUna : string.Format(formatoAviso, compras);
        }

        if (jugo != null) jugo.respirar = hay;
    }
}
