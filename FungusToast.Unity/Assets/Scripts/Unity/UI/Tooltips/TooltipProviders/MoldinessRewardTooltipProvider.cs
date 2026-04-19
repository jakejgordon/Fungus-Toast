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
            tooltipText = BuildTooltipText(definition);
        }

        public string GetTooltipText() => tooltipText ?? string.Empty;

        private static string BuildTooltipText(MoldinessUnlockDefinition definition)
        {
            if (definition == null)
            {
                return string.Empty;
            }

            return definition.Type switch
            {
                MoldinessUnlockType.IncreaseFailedRunAdaptationCarryover =>
                    $"<b>{definition.DisplayName}</b>\n\n" +
                    "Permanent Campaign Upgrade\n\n" +
                    $"{definition.Description}\n\n" +
                    "Each time you take this reward, your Spores in Reserve capacity increases by 1. On a failed campaign run, you may preserve that many non-starting adaptations from the run you just lost. Those preserved adaptations are then added to the start of your next fresh campaign run.",
                _ =>
                    $"<b>{definition.DisplayName}</b>\n\n{definition.Description}"
            };
        }
    }
}
