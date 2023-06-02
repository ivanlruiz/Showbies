using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class Score : MonoBehaviour
{
    public TextMeshProUGUI text;
    // Start is called before the first frame update
    void Start()
    {
        text.text = "Your score: "  + PlayerPrefs.GetInt("Score").ToString(); 
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
