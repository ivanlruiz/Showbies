using UnityEngine;

public class CanvasHelper : MonoBehaviour
{
    public Canvas rootCanvas;
    public RectTransform rect;

    private Rect ultimoSafeArea;
    private Rect ultimoPixelRect;

    void Awake()
    {
        DoRunLogic();
    }

    void DoRunLogic()
    {
        var safeArea = Screen.safeArea;

        var anchorMin = safeArea.position;
        var anchorMax = safeArea.position + safeArea.size;
        anchorMin.x /= rootCanvas.pixelRect.width;
        anchorMin.y /= rootCanvas.pixelRect.height;
        anchorMax.x /= rootCanvas.pixelRect.width;
        anchorMax.y /= rootCanvas.pixelRect.height;

        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
    }

    void Update()
    {
        // Recalcular las anclas por frame fuerza layout al pedo. El safe area
        // solo cambia al rotar o redimensionar: con mirar eso alcanza.
        if (Screen.safeArea == ultimoSafeArea && rootCanvas.pixelRect == ultimoPixelRect) return;

        ultimoSafeArea = Screen.safeArea;
        ultimoPixelRect = rootCanvas.pixelRect;
        DoRunLogic();
    }
}