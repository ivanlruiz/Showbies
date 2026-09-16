using UnityEngine;
using TMPro;

// El record del modo que se acaba de jugar, en la pantalla de derrota. Si la
// partida ES el record nuevo, este texto se apaga: lo dice el puntaje (ver Score).
public class highscoretext : MonoBehaviour
{
    public TextMeshProUGUI texto;

    [Tooltip("{0} es el récord.")]
    public string formato = "<size=55%>RÉCORD</size>  {0}";

    private void Start()
    {
        if (Score.HuboRecordNuevo())
        {
            gameObject.SetActive(false);
            return;
        }

        int modo = PlayerPrefs.GetInt("UltimoModo", 1);
        texto.text = string.Format(formato, FormatoNumeros.Compacto(PlayerPrefs.GetInt(PlayerHealth.ClaveRecord(modo))));
    }
}
