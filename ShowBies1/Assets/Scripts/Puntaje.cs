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

    private Enemy enemy;
    [HideInInspector]
    public int contadorKill;
   


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
        UpdateKillCounterUI();
    }


    public void UpdateKillCounterUI()
    {

        // La etiqueta chica y el numero grande: en el HUD lo que se lee de reojo
        // es el numero, no la palabra.
        contadorKill_TMP.text = "<size=55%>PUNTOS</size>  " + FormatoNumeros.Compacto(contadorKill);
        
    }

    
}
