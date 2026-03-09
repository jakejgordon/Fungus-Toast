using FungusToast.Core.Campaign;
using FungusToast.Unity.UI.Tooltips;

namespace FungusToast.Unity.UI.Tooltips.TooltipProviders
{
    public class AdaptationTooltipProvider : UnityEngine.MonoBehaviour, ITooltipContentProvider
    {
        private AdaptationDefinition adaptation;

        public void Initialize(AdaptationDefinition definition)
        {
            adaptation = definition;
        }

        public string GetTooltipText()
        {
            if (adaptation == null)
            {
                return "<b>Campaign Adaptation</b>\nUnset";
            }

            return $"<b>{adaptation.Name}</b>\n<i>Campaign Adaptation</i>\n\n{adaptation.Description}";
        }
    }
}