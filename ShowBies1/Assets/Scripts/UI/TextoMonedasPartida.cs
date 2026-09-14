using TMPro;
using UnityEngine;

// En la pantalla de derrota: cuantas monedas dejo la partida y cuantas hay en total.
public class TextoMonedasPartida : MonoBehaviour
{
    public TMP_Text texto;

    private void Start()
    {
        texto.text = "+" + FormatoNumeros.Compacto(Progreso.MonedasDeLaPartida) + " monedas  (total "
            + FormatoNumeros.Compacto(Progreso.Monedas) + ")";
    }
}
