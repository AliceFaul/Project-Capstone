using UnityEngine;
using UnityEngine.UI;

public class CanvasCreator
{
    public GameObject Create(bool isWorldSpace)
    {
        GameObject canvasGo = new GameObject("Canvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>();

        if (isWorldSpace)
        {
            canvas.renderMode = RenderMode.WorldSpace;
            var rect = canvasGo.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(800, 600);
            rect.localScale = new Vector3(0.008f, 0.008f, 0.008f);
            canvas.sortingOrder = 100; // Above all
        }
        else
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 99;
        }
        
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = 1;
        scaler.referencePixelsPerUnit = 100;

        canvasGo.AddComponent<GraphicRaycaster>();
        return canvasGo;
    }
}