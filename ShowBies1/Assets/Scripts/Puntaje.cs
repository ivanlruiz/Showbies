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
   


    // Lo static sobrevive al cambio de escena y a entrar en play sin recargar el dominio:
    // sin esto instance quedaba apuntando al Puntaje destruido de la partida anterior, que
    // con el == de Unity da null pero con "is null" o "?." no.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetearEstadoCompartido()
    {
        instance = null;
    }

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

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }


    public void UpdateKillCounterUI()
    {

        // La etiqueta chica y el numero grande: en el HUD lo que se lee de reojo
        // es el numero, no la palabra.
        contadorKill_TMP.text = Textos.Formato("hud_puntos", FormatoNumeros.Compacto(contadorKill));
        
    }

    
}
