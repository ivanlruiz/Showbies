using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RestartScene : MonoBehaviour
{
#if UNITY_STANDALONE
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.R)) SceneManager.LoadScene(1);

        if (Input.GetKeyUp(KeyCode.Escape))
        {
            Application.Quit();
        }
    }
#endif
}
