using System;
using System.Collections;
using System.Linq;
using FungusToast.Unity.Input;
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
        private const float TooltipWidthScale = 1.2f;
        [SerializeField] private bool isHelpIcon = false; // tap toggles on touch
        [SerializeField] private bool followPointer = false; // reserved for future use
        [SerializeField] private TooltipPlacement placement = TooltipPlacement.Auto; // NEW: developer-selected placement
        [SerializeField] private float autoPlacementOffsetX = 0f;
        [SerializeField] private bool pinOnClick = false; // if true, clicking the element pins/unpins the tooltip

        private bool touchMode;
        private bool tooltipVisible;
        private bool isPinned = false;
        private string hintLine;
        private bool suppressed;

        /// <summary>True while this trigger is holding the shared tooltip open via click-to-pin.</summary>
        public bool IsPinned => isPinned;

        private void Awake()
        {
            touchMode = UnityInputAdapter.IsTouchSupportedOnCurrentPlatform();

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
            isPinned = false;
        }

        private void OnDestroy()
        {
            if (TooltipManager.Instance != null)
                TooltipManager.Instance.Cancel(this);
            tooltipVisible = false;
            isPinned = false;
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

        public void SetAutoPlacementOffsetX(float value)
        {
            autoPlacementOffsetX = value;
        }

        public void SetMaxWidth(int value)
        {
            maxWidth = Mathf.Max(0, value);
        }

        public void SetHoverDelay(float seconds)
        {
            hoverDelay = Mathf.Max(0f, seconds);
            useCustomDelay = true;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (suppressed)
                return; // a richer surface (the pinned player inspector) already shows this content
            if (touchMode && !isHelpIcon)
                return; // use long press instead (not yet implemented for simplicity)
            float delay = useCustomDelay ? hoverDelay : (TooltipManager.Instance != null ? TooltipManager.Instance.showDelay : 0.35f);
            if (TooltipManager.Instance != null)
            {
                TooltipManager.Instance.ShowAfterDelay(this, BuildRequest(), delay);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (isPinned) return; // pinned tooltips stay visible until clicked again
            if (TooltipManager.Instance != null)
                TooltipManager.Instance.Cancel(this);
            tooltipVisible = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (touchMode && isHelpIcon)
            {
                ToggleTouchTooltip();
                return;
            }
            if (!touchMode && pinOnClick)
            {
                TogglePin();
                return;
            }
        }

        public void OnPointerUp(PointerEventData eventData) { }

        /// <summary>
        /// Called by <see cref="TooltipManager"/> when another source takes over the single shared
        /// tooltip view. Without this the pinned flag outlived the visible tooltip, so the next
        /// click on a pinned element only cleared invisible state and appeared to do nothing.
        /// </summary>
        internal void NotifyTooltipReleased()
        {
            isPinned = false;
            tooltipVisible = false;
        }

        private void TogglePin()
        {
            if (isPinned)
            {
                isPinned = false;
                if (TooltipManager.Instance != null)
                    TooltipManager.Instance.Cancel(this);
                tooltipVisible = false;
            }
            else
            {
                isPinned = true;
                if (TooltipManager.Instance != null)
                {
                    TooltipManager.Instance.ShowAfterDelay(this, BuildRequest(), 0f);
                }
                tooltipVisible = true;
            }
        }

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
                    TooltipManager.Instance.ShowAfterDelay(this, BuildRequest(), 0f);
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

            if (pinOnClick && !touchMode)
            {
                // The manager re-resolves text every frame while visible, so the hint tracks the
                // pin state live and gives click-to-pin the affordance it was missing.
                Func<string> body = dyn ?? (() => staticText);
                dyn = () => AppendHint(body(), isPinned ? "Pinned — click again to unpin" : "Click to pin");
            }
            else if (!string.IsNullOrEmpty(hintLine) && !touchMode)
            {
                Func<string> body = dyn ?? (() => staticText);
                string hint = hintLine;
                dyn = () => AppendHint(body(), hint);
            }

            return new TooltipRequest
            {
                Anchor = transform as RectTransform,
                DynamicTextFunc = dyn,
                StaticText = staticText,
                MaxWidth = maxWidth > 0 ? Mathf.RoundToInt(maxWidth * TooltipWidthScale) : (int?)null,
                FollowPointer = followPointer,
                PivotPreference = new Vector2(0f, 1f),
                Placement = placement,
                AutoPlacementOffsetX = autoPlacementOffsetX
            };
        }

        private static string AppendHint(string body, string hint)
        {
            string hintColor = ColorUtility.ToHtmlStringRGB(UIStyleTokens.Text.Muted);
            return $"{body}\n<color=#{hintColor}><i>{hint}</i></color>";
        }

        public void SetStaticText(string text) => staticText = text;
        public void SetPinOnClick(bool value) => pinOnClick = value;

        /// <summary>
        /// Appends a muted affordance line to the tooltip, for an element whose click does
        /// something other than pin the shared tooltip. Ignored while <c>pinOnClick</c> is set,
        /// which supplies its own live pin hint.
        /// </summary>
        public void SetHintLine(string value) => hintLine = value;

        /// <summary>
        /// Stops this trigger from showing the shared tooltip, and hides it if it is currently
        /// this trigger's. Used while a richer surface already displays the same content.
        /// </summary>
        public void SetSuppressed(bool value)
        {
            suppressed = value;
            if (!suppressed)
                return;

            isPinned = false;
            tooltipVisible = false;
            if (TooltipManager.Instance != null)
                TooltipManager.Instance.Cancel(this);
        }
    }
}
