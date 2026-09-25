using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

// El boton MODO LIBRE del panel de modos. Bloqueado se ve gris, con el nombre y
// abajo lo que falta ("LLEGA A LA OLEADA 12"), y tocarlo lo hace temblar en vez de
// cargar la escena. No se apaga el Button: un boton que no responde al toque parece
// roto, uno que tiembla dice que hay algo que hacer antes. Recien desbloqueado, y
// hasta la primera partida en el libre, dice "¡NUEVO!" abajo: antes el unico cambio
// era el color.
//
// Con la segunda linea el texto lo escribe este componente y no el TextoTraducido del
// hijo, que se apaga: los dos pisarian el mismo TMP cada vez que cambia el idioma.
public class BotonModoLibre : MonoBehaviour
{
    public Image fondo;
    public TMP_Text texto;
    public BotonJugoso jugoso;
    public Color colorBloqueado = new Color(0.45f, 0.45f, 0.52f, 1f);

    private Color colorLibre;
    private TextoTraducido traducido;
    private int revisionProgreso = -1;
    private int revisionIdioma = -1;

    private void Awake()
    {
        if (fondo != null) colorLibre = fondo.color;
        if (texto != null) traducido = texto.GetComponent<TextoTraducido>();
    }

    private void OnEnable()
    {
        Pintar();
    }

    private void Update()
    {
        if (revisionProgreso != Progreso.Revision || revisionIdioma != Idioma.Revision) Pintar();
    }

    private void Pintar()
    {
        revisionProgreso = Progreso.Revision;
        revisionIdioma = Idioma.Revision;

        bool libre = ModoLibre.Desbloqueado;
        bool nuevo = libre && NuncaJugoElLibre();
        if (fondo != null) fondo.color = libre ? colorLibre : colorBloqueado;
        if (traducido != null) traducido.enabled = libre && !nuevo;
        if (texto == null) return;

        if (nuevo)
        {
            texto.text = Textos.De("modo_libre") + "\n<size=62%>" + Textos.De("modo_libre_nuevo") + "</size>";
        }
        else if (libre)
        {
            texto.text = Textos.De("modo_libre");
        }
        else
        {
            texto.text = Textos.De("modo_libre") + "\n<size=62%>"
                + Textos.Formato("modo_libre_bloqueado", ModoLibre.OleadaParaDesbloquear) + "</size>";
        }
    }

    // Sin record en el libre: lo escribe al morir la primera partida que suma algun punto
    // ahi (PlayerHealth), y quien jugo el libre antes de que se bloqueara ya lo tiene. Aca
    // solo se lee.
    private static bool NuncaJugoElLibre()
    {
        return !PlayerPrefs.HasKey(PlayerHealth.ClaveRecord(TiendaMejoras.EscenaModoLibre));
    }

    public void Jugar()
    {
        if (!ModoLibre.Desbloqueado)
        {
            if (jugoso != null) jugoso.Sacudir();
            return;
        }
        Time.timeScale = 1f;
        // Por ModoLibre.EscenaPara, como todo lo que carga el libre (ver CLAUDE.md): si
        // cambia la regla de adonde mandar al que lo pide, este boton, que es el camino
        // principal, se entera. El temblor de arriba queda: es la respuesta del boton.
        SceneManager.LoadScene(ModoLibre.EscenaPara(TiendaMejoras.EscenaModoLibre));
    }
}
