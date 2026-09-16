using UnityEngine;
using TMPro;

// El record del modo que se acaba de jugar, en la pantalla de derrota. Si la
// partida ES el record nuevo, este texto se apaga: lo dice el puntaje (ver Score).
public class highscoretext : MonoBehaviour
{
    public TextMeshProUGUI texto;

    private void Start()
    {
        if (Score.HuboRecordNuevo())
        {
            gameObject.SetActive(false);
            return;
        }

        int modo = PlayerPrefs.GetInt("UltimoModo", 1);
        string record = FormatoNumeros.Compacto(PlayerPrefs.GetInt(PlayerHealth.ClaveRecord(modo)));
        texto.text = Textos.Formato("derrota_record", record);
    }
}
