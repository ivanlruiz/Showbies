using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CamaraJugador : MonoBehaviour
{

    public GameObject personaje;

    private Vector3 posicionRelativa;


    // Start is called before the first frame update
    void Start()
    {
        
       
       
        
    }
    void FixedUpdate()
    {
        if (personaje == null) return;

        posicionRelativa = transform.position - personaje.transform.position;


    }

    private void Update()
    {
        transform.position = personaje.transform.position + posicionRelativa;
    }
}
            
    // Update is called once per frame
    
