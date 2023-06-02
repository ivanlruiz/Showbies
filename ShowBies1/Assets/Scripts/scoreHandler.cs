using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class scoreHandler : MonoBehaviour
{
    public GameObject currentScore;
    public GameObject highScore;

    private TextMeshPro currentScoreText;
    private TextMeshPro highScoreText;

    private void Start()
    {
        currentScoreText = currentScore.GetComponent<TextMeshPro>();
        highScoreText = highScore.GetComponent<TextMeshPro>();

        currentScoreText.text = PlayerPrefs.GetString("currentScore");
    }
}
