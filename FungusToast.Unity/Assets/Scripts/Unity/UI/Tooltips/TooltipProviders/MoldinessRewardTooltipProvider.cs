using FungusToast.Unity.Campaign;
using FungusToast.Unity.UI.Tooltips;
using UnityEngine;

namespace FungusToast.Unity.UI.Tooltips.TooltipProviders
{
    public class MoldinessRewardTooltipProvider : MonoBehaviour, ITooltipContentProvider
    {
        private string tooltipText;

        public void Initialize(MoldinessUnlockDefinition definition)
        {
            Initialize(definition, 1, 0);
        }

        public void Initialize(MoldinessUnlockDefinition definition, int ownedCount)
        {
            Initialize(definition, ownedCount, 0);
        }

        public void Initialize(MoldinessUnlockDefinition definition, int ownedCount, int currentCarryoverCapacity)
        {
            tooltipText = BuildTooltipText(definition, ownedCount, currentCarryoverCapacity);
        }

        public string GetTooltipText() => tooltipText ?? string.Empty;

        private static string BuildTooltipText(MoldinessUnlockDefinition definition, int ownedCount, int currentCarryoverCapacity)
        {
            if (definition == null)
            {
                return string.Empty;
            }

            int safeOwnedCount = Mathf.Max(1, ownedCount);
            string title = safeOwnedCount > 1
                ? $"<b>{definition.DisplayName} x{safeOwnedCount}</b>"
                : $"<b>{definition.DisplayName}</b>";

            return definition.Type switch
            {
                MoldinessUnlockType.IncreaseFailedRunAdaptationCarryover =>
                    $"{title}\n\n" +
                    "Permanent Campaign Upgrade\n\n" +
                    $"{definition.Description}\n\n" +
                    (currentCarryoverCapacity > 0
                        ? $"Current carryover capacity: {currentCarryoverCapacity}\n\n"
                        : $"Claim effect: +{Mathf.Max(1, definition.StackAmount)} carryover capacity\n\n") +
                    $"On a failed campaign run, you may preserve up to {Mathf.Max(1, currentCarryoverCapacity > 0 ? currentCarryoverCapacity : definition.StackAmount)} non-starting adaptations from the run you just lost. Those preserved adaptations are then added to the start of your next fresh campaign run.",
                _ =>
                    $"{title}\n\n{definition.Description}"
            };
        }
    }
}
