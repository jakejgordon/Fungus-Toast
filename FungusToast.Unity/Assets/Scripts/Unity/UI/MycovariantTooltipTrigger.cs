using Assets.Scripts.Unity.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace FungusToast.Unity.UI
{
    /// <summary>
    /// Attach to any UI element (e.g., mycovariant badge/icon) to show a tooltip on hover.
    /// </summary>
    public class MycovariantTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private string header;
        private string body;

        public void SetText(string header, string body)
        {
            this.header = header;
            this.body = body;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            MycovariantTooltipPanel.Instance.ShowTooltip(header, body, transform as RectTransform);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            MycovariantTooltipPanel.Instance.HideTooltip();
        }
    }
}
