using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioArma : MonoBehaviour
{
    public AudioSource AudioSource;
    public AudioClip clip;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetMouseButtonDown(0))
        {
            AudioSource.PlayOneShot(clip);
        }
        
    }
}
