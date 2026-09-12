using FungusToast.Core.Players;
using FungusToast.Unity.UI.MutationTree;

namespace FungusToast.Unity.UI.Tooltips.TooltipProviders
{
    /// <summary>
    /// Hover text for an active-surge icon tile. Reads the live
    /// <see cref="Player.ActiveSurgeInfo"/> on every request so the rounds-remaining line stays
    /// current while the tile it sits on is reused across round ticks.
    /// </summary>
    public class SurgeTooltipProvider : UnityEngine.MonoBehaviour, ITooltipContentProvider
    {
        private Player.ActiveSurgeInfo surge;

        public void Initialize(Player.ActiveSurgeInfo activeSurge)
        {
            surge = activeSurge;
        }

        public string GetTooltipText() => SurgePresentation.BuildTooltip(surge);
    }
}
