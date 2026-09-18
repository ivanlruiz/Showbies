using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// OJO: nada de este archivo va adentro de un #if UNITY_ANDROID.
//
// Antes la clase entera estaba envuelta en uno, asi que en un editor con target
// Windows compilaba VACIA. Unity guarda las escenas segun los campos que el
// script tiene en ese momento, y al no tener ninguno los borraba: cada vez que
// se guardaba una escena desde Windows se perdia el cableado de los joysticks.
// Ya paso, por eso hubo que volver a asignar las referencias a mano.
//
// Ahora el codigo compila igual en las dos plataformas y quien decide es
// Application.isMobilePlatform en runtime. Ademas de arreglar el guardado, eso
// hace que compilar en Windows valide tambien la build de Android.
public class PlayerJS : MonoBehaviour
{
    public PlayerController player;
    public GunController thegun;

    public FixedJoystick moveJoystick;
    public FixedJoystick lookJoystick;

    // Para la granada, que en movil sale hacia donde se venia apuntando.
    public Vector3 UltimaDireccionApuntada { get; private set; }
    private float ultimoApuntadoEn = float.NegativeInfinity;

    public bool ApuntoHaceMenosDe(float segundos)
    {
        return Time.time - ultimoApuntadoEn <= segundos;
    }

    void Update()
    {
        if (!Plataforma.EsMovil || MenuPausa.JuegoCongelado) return;

        UpdateMoveJoystick();
        UpdateShootJoystick();
    }

    void UpdateMoveJoystick()
    {
        if (player == null || moveJoystick == null) return;

        player.Move(new Vector2(moveJoystick.Horizontal, moveJoystick.Vertical));
    }

    void UpdateShootJoystick()
    {
        if (player == null || thegun == null || lookJoystick == null) return;

        float hoz = lookJoystick.Horizontal;
        float ver = lookJoystick.Vertical;

        if (hoz == 0 && ver == 0)
        {
            thegun.isFiring = false;
            return;
        }

        UltimaDireccionApuntada = new Vector3(hoz, 0f, ver).normalized;
        ultimoApuntadoEn = Time.time;

        Vector3 lookAtPosition = transform.position + new Vector3(hoz, 0, ver);
        thegun.transform.LookAt(lookAtPosition);

        thegun.isFiring = player.cantBalas > 0;
    }
}
