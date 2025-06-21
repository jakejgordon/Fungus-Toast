using FungusToast.Core.Mycovariants;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Assets.Scripts.Unity.UI.MycovariantDraft
{
    public class MycovariantCard : MonoBehaviour
    {
        public Image iconImage;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI effectText;
        public Button pickButton; // Covers card

        private Mycovariant mycovariant;
        private System.Action<Mycovariant> onPicked;

        public void SetMycovariant(Mycovariant mycovariant, System.Action<Mycovariant> onPicked)
        {
            this.mycovariant = mycovariant;
            this.onPicked = onPicked;
            iconImage.sprite = MycovariantArtRepository.GetIcon(mycovariant.Type);
            nameText.text = mycovariant.Name;
            effectText.text = mycovariant.Description;
            pickButton.onClick.RemoveAllListeners();
            pickButton.onClick.AddListener(() => this.onPicked?.Invoke(mycovariant));
        }
    }
}
