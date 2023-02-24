using UnityEngine;

public class CanvasHelper : MonoBehaviour
{
    public Canvas rootCanvas;
    public RectTransform rect;

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
        DoRunLogic();
    }
}