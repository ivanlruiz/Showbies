using UnityEngine;
using TMPro;

// El puntaje de la partida que acaba de terminar, en la pantalla de derrota.
//
// Si el puntaje empata con el record del modo es porque lo acaba de romper
// (PlayerHealth guarda el record antes de cargar esta escena), y entonces el
// texto lo grita en vez de decir un numero mas: enterarte de que rompiste tu
// marca es la mitad de la gracia de morir. En ese caso el texto del record se
// apaga solo, para no decir dos veces el mismo numero.
public class Score : MonoBehaviour
{
    public TextMeshProUGUI text;
    public Color colorRecord = new Color(1f, 0.85f, 0.25f, 1f);

    private void Start()
    {
        string puntaje = FormatoNumeros.Compacto(PlayerPrefs.GetInt("Score"));
        bool esRecord = HuboRecordNuevo();

        text.text = esRecord
            ? Textos.Formato("derrota_record_nuevo", puntaje)
            : Textos.Formato("derrota_puntos", puntaje);
        if (esRecord) text.color = colorRecord;
    }

    // Lo mira tambien el texto del record, para callarse cuando esto es cierto.
    public static bool HuboRecordNuevo()
    {
        // Lo anota PlayerHealth al superar el record: comparar aca el puntaje con el
        // record guardado no distingue un record nuevo de un empate.
        return PlayerPrefs.GetInt("Score") > 0 && PlayerHealth.RecordNuevo;
    }
}
