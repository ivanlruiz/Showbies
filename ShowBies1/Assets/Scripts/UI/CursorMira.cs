using UnityEngine;

// En PC, mientras se juega, el puntero del mouse es una mira: el arma apunta al punto del
// piso que queda debajo del mouse, y el centro de la mira es ese punto. Con el juego
// congelado (la pausa o el HAS MUERTO) vuelve la flecha, que es para tocar botones. En
// movil no hay puntero y no hace nada.
//
// Va en la raiz del prefab MenuPausa, que esta en las tres escenas de juego. El puntero es
// de toda la aplicacion y sobrevive al cambio de escena: al apagarse (se descarga la
// escena, se sale de play) deja la flecha de siempre, asi el menu y la derrota no heredan
// la mira.
public class CursorMira : MonoBehaviour
{
    [Tooltip("Importada como Cursor (legible, RGBA32 y sin mipmaps), que es lo que pide Cursor.SetCursor.")]
    public Texture2D textura;
    [Tooltip("El punto que apunta, en pixeles desde la esquina de arriba a la izquierda: el centro de la mira.")]
    public Vector2 centro = new Vector2(32f, 32f);

    private bool puesta;

    private void Update()
    {
        bool mira = textura != null && !Plataforma.EsMovil && !MenuPausa.JuegoCongelado;
        if (mira == puesta) return;
        puesta = mira;
        if (mira) Cursor.SetCursor(textura, centro, CursorMode.Auto);
        else Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }

    private void OnDisable()
    {
        if (!puesta) return;
        puesta = false;
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }
}
