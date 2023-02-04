using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConditionalShow : MonoBehaviour
{
    public bool showOnAndroid;
    public bool showOnPC;
    // Start is called before the first frame update
    void Start()
    {
#if UNITY_ANDROID
        if (!showOnAndroid)
        {
            gameObject.SetActive(false);
        }
#endif

#if UNITY_STANDALONE
if (!showOnPC)
        {
            gameObject.SetActive(false);
        }
#endif
    }


}
