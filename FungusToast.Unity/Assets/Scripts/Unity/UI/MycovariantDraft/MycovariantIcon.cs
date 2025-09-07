using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FungusToast.Core.Mycovariants;
using Assets.Scripts.Unity.UI.MycovariantDraft;
using FungusToast.Core.Players;
using Assets.Scripts.Unity.UI;

namespace FungusToast.Unity.UI.MycovariantDraft
{
    public class MycovariantIcon : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private MycovariantTooltipTrigger tooltip;

        public void SetMycovariant(PlayerMycovariant myco)
        {
            var def = myco.Mycovariant; // The Mycovariant definition

            iconImage.sprite = MycovariantArtRepository.GetIcon(def.Type);
            tooltip.SetText(def.Name, def.Description);
        }

    }
}
