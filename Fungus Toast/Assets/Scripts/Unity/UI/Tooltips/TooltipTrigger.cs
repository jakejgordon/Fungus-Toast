using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace FungusToast.Unity.UI.Tooltips
{
    /// <summary>
    /// Attach to any UI element. Optionally provide static text or a dynamic provider.
    /// </summary>
    public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [TextArea]
        [SerializeField] private string staticText;
        [SerializeField] private MonoBehaviour dynamicProvider; // must implement ITooltipContentProvider
        [SerializeField] private float hoverDelay = 0.38f;
        [SerializeField] private bool useCustomDelay = false;
        [SerializeField] private int maxWidth = 400;
        [SerializeField] private bool isHelpIcon = false; // tap toggles on touch
        [SerializeField] private bool followPointer = false; // reserved for future use

        private bool pointerInside;
        private bool touchMode;
        private bool tooltipVisible;

        private void Awake()
        {
            touchMode = Input.touchSupported && (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            pointerInside = true;
            if (touchMode && !isHelpIcon)
                return; // use long press instead (not yet implemented for simplicity)
            float delay = useCustomDelay ? hoverDelay : (TooltipManager.Instance != null ? TooltipManager.Instance.showDelay : 0.35f);
            if (TooltipManager.Instance != null)
            {
                TooltipManager.Instance.showDelay = delay; // temporarily override
                TooltipManager.Instance.ShowAfterDelay(this, BuildRequest());
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            pointerInside = false;
            if (TooltipManager.Instance != null)
                TooltipManager.Instance.Cancel(this);
            tooltipVisible = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!touchMode) return;
            if (!isHelpIcon) return; // only toggle tap for help icons
            ToggleTouchTooltip();
        }

        public void OnPointerUp(PointerEventData eventData) { }

        private void ToggleTouchTooltip()
        {
            if (tooltipVisible)
            {
                TooltipManager.Instance.Cancel(this);
                tooltipVisible = false;
            }
            else
            {
                if (TooltipManager.Instance != null)
                {
                    TooltipManager.Instance.showDelay = 0f;
                    TooltipManager.Instance.ShowAfterDelay(this, BuildRequest());
                }
                tooltipVisible = true;
            }
        }

        private TooltipRequest BuildRequest()
        {
            System.Func<string> dyn = null;
            if (dynamicProvider != null && dynamicProvider is ITooltipContentProvider provider)
                dyn = provider.GetTooltipText;
            return new TooltipRequest
            {
                Anchor = transform as RectTransform,
                DynamicTextFunc = dyn,
                StaticText = staticText,
                MaxWidth = maxWidth > 0 ? maxWidth : null,
                FollowPointer = followPointer,
                PivotPreference = new Vector2(0f,1f)
            };
        }

        public void SetStaticText(string text) => staticText = text;
    }
}
