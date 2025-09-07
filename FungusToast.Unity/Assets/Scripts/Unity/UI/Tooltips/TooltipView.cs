using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FungusToast.Unity.UI.Tooltips
{
    /// <summary>
    /// Runtime instance handling sizing and text assignment. Created from prefab and reused.
    /// </summary>
    public class TooltipView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI text;
        [SerializeField] private RectTransform background;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private LayoutElement layoutElement;

        public RectTransform RectTransform => transform as RectTransform;

        public void SetText(string value, int? maxWidth)
        {
            if (text == null) return;
            text.richText = true;
            text.text = value;
            if (layoutElement != null)
            {
                if (maxWidth.HasValue)
                {
                    layoutElement.enabled = true;
                    layoutElement.preferredWidth = maxWidth.Value;
                }
                else
                {
                    layoutElement.enabled = false;
                }
            }
            Canvas.ForceUpdateCanvases();
        }

        public void ShowImmediate()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = false;
            }
            gameObject.SetActive(true);
        }

        public void HideImmediate()
        {
            gameObject.SetActive(false);
        }
    }
}
