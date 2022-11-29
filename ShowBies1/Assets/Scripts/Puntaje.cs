using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class Puntaje : MonoBehaviour
{   
    public static Puntaje instance;
    [SerializeField]
    TextMeshProUGUI contadorKill_TMP; 
    [SerializeField]
    TextMeshProUGUI vida_TMP;
    [HideInInspector]

    public int contadorKill;
    public int vida;


    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }


    public void UpdateKillCounterUI()
    {
        contadorKill_TMP.text = contadorKill.ToString();
        
    }

    public void Vida()
    {
        vida_TMP.text = vida.ToString();
    }
}
