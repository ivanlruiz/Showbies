using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

// El boton MODO LIBRE del panel de modos. Bloqueado se ve gris, con el nombre y
// abajo lo que falta ("LLEGA A LA OLEADA 12"), y tocarlo lo hace temblar en vez de
// cargar la escena. No se apaga el Button: un boton que no responde al toque parece
// roto, uno que tiembla dice que hay algo que hacer antes.
//
// El texto lo escribe este componente y no el TextoTraducido del hijo, que se
// apaga: los dos pisarian el mismo TMP cada vez que cambia el idioma.
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
        if (fondo != null) fondo.color = libre ? colorLibre : colorBloqueado;
        if (traducido != null) traducido.enabled = libre;
        if (texto == null) return;

        if (libre)
        {
            texto.text = Textos.De("modo_libre");
        }
        else
        {
            texto.text = Textos.De("modo_libre") + "\n<size=62%>"
                + Textos.Formato("modo_libre_bloqueado", ModoLibre.OleadaParaDesbloquear) + "</size>";
        }
    }

    public void Jugar()
    {
        if (!ModoLibre.Desbloqueado)
        {
            if (jugoso != null) jugoso.Sacudir();
            return;
        }
        Time.timeScale = 1f;
        SceneManager.LoadScene(TiendaMejoras.EscenaModoLibre);
    }
}
