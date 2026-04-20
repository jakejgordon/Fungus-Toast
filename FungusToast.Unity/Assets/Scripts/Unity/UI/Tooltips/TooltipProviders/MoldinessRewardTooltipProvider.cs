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
            Initialize(definition, 1);
        }

        public void Initialize(MoldinessUnlockDefinition definition, int ownedCount)
        {
            tooltipText = BuildTooltipText(definition, ownedCount);
        }

        public string GetTooltipText() => tooltipText ?? string.Empty;

        private static string BuildTooltipText(MoldinessUnlockDefinition definition, int ownedCount)
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
                    $"Current stacks: {safeOwnedCount}\n\n" +
                    $"Your Spores in Reserve capacity is currently increased by {safeOwnedCount}. On a failed campaign run, you may preserve that many non-starting adaptations from the run you just lost. Those preserved adaptations are then added to the start of your next fresh campaign run.",
                _ =>
                    $"{title}\n\n{definition.Description}"
            };
        }
    }
}
