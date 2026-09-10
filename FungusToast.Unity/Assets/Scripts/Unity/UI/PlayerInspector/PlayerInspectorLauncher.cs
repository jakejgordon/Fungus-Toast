#nullable enable

using FungusToast.Core.Players;
using FungusToast.Unity.Input;
using FungusToast.Unity.UI.Tooltips;
using UnityEngine;
using UnityEngine.EventSystems;

namespace FungusToast.Unity.UI.PlayerInspector
{
    /// <summary>
    /// Makes a player's mold icon drive <see cref="PlayerInspectorPanel"/>: hover previews it
    /// after the same delay the shared tooltip uses, pointer-exit closes the preview, and a click
    /// pins it. This replaces the old <c>TooltipTrigger</c> on the icon rather than sitting beside
    /// one, so there is exactly one surface per icon and nothing to fight over.
    /// </summary>
    public sealed class PlayerInspectorLauncher : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        private const float FallbackHoverDelaySeconds = 0.35f;

        private Player? player;
        private Canvas? hostCanvas;
        private bool touchMode;
        private bool isHovering;
        private float previewDueTime;
        private bool previewShown;

        public void Initialize(Player targetPlayer)
        {
            player = targetPlayer;
            hostCanvas = GetComponentInParent<Canvas>();
            touchMode = UnityInputAdapter.IsTouchSupportedOnCurrentPlatform();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (touchMode)
            {
                return; // no hover on touch; tap pins instead, matching TooltipTrigger
            }

            isHovering = true;
            previewShown = false;
            float delay = TooltipManager.Instance != null ? TooltipManager.Instance.showDelay : FallbackHoverDelaySeconds;
            previewDueTime = Time.unscaledTime + delay;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovering = false;
            if (previewShown)
            {
                previewShown = false;
                PlayerInspectorPanel.EndPreview(transform as RectTransform);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (player == null)
            {
                return;
            }

            isHovering = false;
            previewShown = false;
            PlayerInspectorPanel.TogglePin(player, transform as RectTransform, hostCanvas);
        }

        private void Update()
        {
            if (!isHovering || previewShown || player == null || Time.unscaledTime < previewDueTime)
            {
                return;
            }

            previewShown = true;
            PlayerInspectorPanel.Preview(player, transform as RectTransform, hostCanvas);
        }

        private void OnDisable()
        {
            isHovering = false;
            previewShown = false;
            if (PlayerInspectorPanel.IsOpenFor(transform as RectTransform))
            {
                PlayerInspectorPanel.Close();
            }
        }
    }
}
