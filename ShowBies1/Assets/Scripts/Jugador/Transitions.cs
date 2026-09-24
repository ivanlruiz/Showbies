using UnityEngine;

// Solo guarda el Animator del modelo para PlayerController (trans.anim).
public class Transitions : MonoBehaviour
{
    public Animator anim;

    void Start()
    {
        anim = GetComponent<Animator>();
    }
}
