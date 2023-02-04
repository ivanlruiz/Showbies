using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using TMPro;
using UnityEngine;

public class Balas : MonoBehaviour
{
    TextMeshProUGUI texto;
    public PlayerController player;
    private void Start()
    {
        texto = GetComponent<TextMeshProUGUI>();

    }

    private void Update()
    {
        if(player == null)
            return;
        texto.text = string.Format("<size=100%>{0}</size>  <size=60%><voffset=1em>/{1}</voffset></size>", player.cantBalas, player.maxBalas) ;
    }
}
