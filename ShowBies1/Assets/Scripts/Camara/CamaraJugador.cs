using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CamaraJugador : MonoBehaviour
{

    public GameObject personaje;

    private Vector3 posicionRelativa;


    // Start is called before the first frame update


    

    
    void Start()
    {
        posicionRelativa = transform.position - personaje.transform.position;



    }
    

    private void Update()
    {
        // Al morir, PlayerHealth destruye al jugador y recien despues carga la
        // escena de Perdiste. En ese hueco esto seguia leyendo un objeto muerto y
        // tiraba MissingReferenceException en cada frame.
        if (personaje == null) return;

        transform.position = personaje.transform.position + posicionRelativa;
    }
}
            
    // Update is called once per frame
    
