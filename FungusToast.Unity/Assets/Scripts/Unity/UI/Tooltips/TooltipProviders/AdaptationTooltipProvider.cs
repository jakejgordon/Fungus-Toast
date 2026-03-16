using FungusToast.Core.Campaign;
using FungusToast.Core.Mycovariants;
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

    public class MycovariantTooltipProvider : UnityEngine.MonoBehaviour, ITooltipContentProvider
    {
        private Mycovariant mycovariant;

        public void Initialize(Mycovariant definition)
        {
            mycovariant = definition;
        }

        public string GetTooltipText()
        {
            if (mycovariant == null)
            {
                return "<b>Mycovariant</b>\nUnset";
            }

            return $"<b>{mycovariant.Name}</b>\n<i>Mycovariant · {mycovariant.Category}</i>\n\n{mycovariant.Description}";
        }
    }
}