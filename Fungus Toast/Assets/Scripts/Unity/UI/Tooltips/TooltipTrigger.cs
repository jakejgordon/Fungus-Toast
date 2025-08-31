using System;
using System.Collections;
using System.Linq;
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
        [SerializeField] private TooltipPlacement placement = TooltipPlacement.Auto; // NEW: developer-selected placement

        private bool touchMode;
        private bool tooltipVisible;

        private void Awake()
        {
            touchMode = Input.touchSupported && (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer);

            // Auto-resolve a provider from attached components if not explicitly set
            if (dynamicProvider == null)
            {
                var resolved = GetComponents<MonoBehaviour>()
                    .FirstOrDefault(mb => mb is ITooltipContentProvider);
                if (resolved != null)
                {
                    dynamicProvider = resolved;
                }
            }
        }

        private void OnDisable()
        {
            // If this source is currently displayed, hide it when object is disabled
            if (TooltipManager.Instance != null)
                TooltipManager.Instance.Cancel(this);
            tooltipVisible = false;
        }

        private void OnDestroy()
        {
            if (TooltipManager.Instance != null)
                TooltipManager.Instance.Cancel(this);
            tooltipVisible = false;
        }

        /// <summary>
        /// Assign a dynamic provider at runtime. The provider must implement ITooltipContentProvider.
        /// </summary>
        public void SetDynamicProvider(MonoBehaviour provider)
        {
            if (provider == null)
            {
                dynamicProvider = null;
                return;
            }

            if (provider is not ITooltipContentProvider)
            {
                var assignedType = provider.GetType().Name;
                throw new InvalidOperationException(
                    $"TooltipTrigger on '{name}': Assigned 'provider' of type '{assignedType}' does not implement {nameof(ITooltipContentProvider)}.");
            }

            dynamicProvider = provider;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
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

        private ITooltipContentProvider ResolveProviderOrNull()
        {
            // Prefer explicitly assigned provider if valid
            if (dynamicProvider is ITooltipContentProvider p)
                return p;

            // Otherwise, try to find any component on this GameObject implementing the interface
            var found = GetComponents<MonoBehaviour>()
                .FirstOrDefault(mb => mb is ITooltipContentProvider) as ITooltipContentProvider;

            // Cache it back into dynamicProvider for future calls
            if (found != null && dynamicProvider == null)
            {
                dynamicProvider = found as MonoBehaviour;
            }

            return found;
        }

        private TooltipRequest BuildRequest()
        {
            System.Func<string> dyn = null;

            // If StaticText is empty, a valid dynamic provider is required
            bool hasStatic = !string.IsNullOrEmpty(staticText);
            if (!hasStatic)
            {
                var provider = ResolveProviderOrNull();
                if (provider == null)
                {
                    // Provide a helpful error that mentions what we found on this object
                    var components = string.Join(", ", GetComponents<MonoBehaviour>().Select(c => c.GetType().Name));
                    throw new InvalidOperationException(
                        $"TooltipTrigger on '{name}' requires either non-empty Static Text or a component implementing {nameof(ITooltipContentProvider)}. Found components: [{components}]");
                }

                dyn = provider.GetTooltipText;
            }

            return new TooltipRequest
            {
                Anchor = transform as RectTransform,
                DynamicTextFunc = dyn,
                StaticText = staticText,
                MaxWidth = maxWidth > 0 ? maxWidth : (int?)null,
                FollowPointer = followPointer,
                PivotPreference = new Vector2(0f, 1f),
                Placement = placement
            };
        }

        public void SetStaticText(string text) => staticText = text;
    }
}
