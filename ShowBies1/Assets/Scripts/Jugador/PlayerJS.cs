using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerJS : MonoBehaviour
{
#if UNITY_ANDROID
    public PlayerController player;
    public GunController thegun;
    //Move
    public RectTransform handle;
    public RectTransform background;
    public FixedJoystick moveJoystick;

    public FixedJoystick lookJoystick;

    

    
    void Update()
    {
#if UNITY_ANDROID
        UpdateMoveJoystick();
        UpdateShootJoystick();
#endif
    }
    void UpdateMoveJoystick()
    {
        player.Move(new Vector2(moveJoystick.Horizontal,moveJoystick.Vertical));       
    }
    void UpdateShootJoystick()
    {
        float hoz = lookJoystick.Horizontal;
        float ver = lookJoystick.Vertical;         
       
        Vector3 lookAtPosition = transform.position + new Vector3 (hoz, 0, ver);
        //Debug.LogFormat("hoz : {0} ver : {1}", hoz, ver);                             04:31 24/2/2023
        //Debug.DrawLine(player.transform.position, lookAtPosition, Color.blue, 500);   04:31 24/2/2023

        if (hoz == 0 && ver == 0)
        {
            thegun.isFiring = false;
            return;
        }

        thegun.transform.LookAt(lookAtPosition);

            if (player.cantBalas > 0)
            {

                thegun.isFiring = true;

            }
            
    }

   
#endif
}
