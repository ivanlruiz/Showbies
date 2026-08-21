using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Balas : MonoBehaviour
{
    TextMeshProUGUI texto;
    public PlayerController player;

    private int ultimoValorMostrado = int.MinValue;

    private void Start()
    {
        texto = GetComponent<TextMeshProUGUI>();

    }

    private void Update()
    {
        if(player == null)
            return;

        // Solo al cambiar: el string.Format por frame era una alocacion por frame.
        if (player.cantBalas == ultimoValorMostrado) return;
        ultimoValorMostrado = player.cantBalas;

        texto.text = string.Format("<size=100%>{0}</size>  <size=60%><voffset=1em>/{1}</voffset></size>", player.cantBalas, player.maxBalas) ;
    }
}
