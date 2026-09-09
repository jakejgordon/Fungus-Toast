#nullable enable

using FungusToast.Core.Players;
using FungusToast.Unity.UI.Tooltips;
using UnityEngine;
using UnityEngine.EventSystems;

namespace FungusToast.Unity.UI.PlayerInspector
{
    /// <summary>
    /// Turns a player's mold icon into the entry point for <see cref="PlayerInspectorPanel"/>.
    /// Hover still shows the cheap, non-blocking text tooltip; clicking swaps it for the
    /// interactive panel, which is the only surface that can host hoverable trait icons.
    /// </summary>
    public sealed class PlayerInspectorLauncher : MonoBehaviour, IPointerClickHandler
    {
        private const string HintText = "Click to inspect";

        private Player? player;
        private TooltipTrigger? tooltipTrigger;
        private Canvas? hostCanvas;
        private bool wasPanelOpen;

        public void Initialize(Player targetPlayer, TooltipTrigger? trigger)
        {
            player = targetPlayer;
            tooltipTrigger = trigger;
            hostCanvas = GetComponentInParent<Canvas>();
            tooltipTrigger?.SetHintLine(HintText);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (player == null)
            {
                return;
            }

            bool opened = PlayerInspectorPanel.Toggle(player, transform as RectTransform, hostCanvas);
            ApplyTooltipSuppression(opened);
        }

        private void Update()
        {
            // The panel closes on its own (close button, anchor destroyed, another row clicked),
            // and it has no back-reference here, so track its state to release the hover tooltip.
            bool isPanelOpen = PlayerInspectorPanel.IsOpenFor(transform as RectTransform);
            if (isPanelOpen == wasPanelOpen)
            {
                return;
            }

            ApplyTooltipSuppression(isPanelOpen);
        }

        private void ApplyTooltipSuppression(bool isPanelOpen)
        {
            wasPanelOpen = isPanelOpen;
            tooltipTrigger?.SetSuppressed(isPanelOpen);
        }

        private void OnDisable()
        {
            if (wasPanelOpen)
            {
                PlayerInspectorPanel.Close();
            }

            ApplyTooltipSuppression(false);
        }
    }
}
