using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Transitions : MonoBehaviour
{
    
    public Animator anim;
    // Start is called before the first frame update
    void Start()
    {
        anim = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {

        /*if (Mathf.Abs(Input.GetAxis("Horizontal")) > 0.1f || Mathf.Abs(Input.GetAxis("Vertical")) > 0.1f)
        {
            Debug.Log("La condición es verdadera");
            anim.SetBool("run", true);
        }
        else anim.SetBool("run", false);*/



        /*if (Input.GetMouseButtonDown(0))
        {
            anim.SetBool("shoot", true);

        } else
            anim.SetBool("shoot", false);*/

    
        
       

    }

    
}
