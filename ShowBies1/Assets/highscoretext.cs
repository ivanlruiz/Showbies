using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class highscoretext : MonoBehaviour
{
    public TextMeshProUGUI texto;
    void Start()
    {
        texto.text = "Highscore: " + PlayerPrefs.GetInt("HighScore").ToString();

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
