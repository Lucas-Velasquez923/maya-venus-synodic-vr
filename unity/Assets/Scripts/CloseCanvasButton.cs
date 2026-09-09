using UnityEngine;

public class CloseCanvasButton : MonoBehaviour
{
    // ?Inspector????????Canvas
    public GameObject targetCanvas;

    // ???????Button?OnClick??
    public void CloseCanvas()
    {
        if (targetCanvas != null)
        {
            targetCanvas.SetActive(false);
        }
    }
}