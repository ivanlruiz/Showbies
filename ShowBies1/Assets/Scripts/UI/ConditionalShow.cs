using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Prende o apaga el objeto segun la plataforma. Usa Plataforma.EsMovil, el
// mismo criterio que PlayerJS y PlayerController: antes esto miraba los
// defines de compilacion (#if UNITY_ANDROID / UNITY_STANDALONE) y el resto
// miraba la plataforma en runtime, y en el editor con target Android los
// joysticks aparecian pero no respondian.
public class ConditionalShow : MonoBehaviour
{
    public bool showOnAndroid;
    public bool showOnPC;

    void Start()
    {
        bool mostrar = Plataforma.EsMovil ? showOnAndroid : showOnPC;
        if (!mostrar)
        {
            gameObject.SetActive(false);
        }
    }
}
