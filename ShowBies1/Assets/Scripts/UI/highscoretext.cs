using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class highscoretext : MonoBehaviour
{
    public TextMeshProUGUI texto;
    void Start()
    {
        // El record del modo que se acaba de jugar.
        int modo = PlayerPrefs.GetInt("UltimoModo", 1);
        texto.text = "Highscore: " + PlayerPrefs.GetInt(PlayerHealth.ClaveRecord(modo)).ToString();

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
