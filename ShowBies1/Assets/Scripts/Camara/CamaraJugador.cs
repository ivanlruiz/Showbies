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
        
        transform.position = personaje.transform.position + posicionRelativa;

        
    }
}
            
    // Update is called once per frame
    
