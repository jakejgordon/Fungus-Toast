using UnityEngine;
using UnityEngine.UI;

public class SidebarResizer : MonoBehaviour
{
    [Tooltip("Fraction of the total screen width to use for the sidebar (e.g., 0.2 = 20%)")]
    [Range(0.1f, 0.5f)]
    public float sidebarWidthFraction = 0.2f; // 20% of the screen width

    private RectTransform rectTransform;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        ResizeSidebar();
    }

    void ResizeSidebar()
    {
        float screenWidth = Screen.width;
        float newWidth = screenWidth * sidebarWidthFraction;

        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, newWidth);
    }
}
