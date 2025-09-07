using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Unity.UI.MycovariantDraft
{
    public class PlayerIconCellUI : MonoBehaviour
    {
        [SerializeField] private Image highlightBackground;
        [SerializeField] private Image iconImage;

        public Image HighlightBackground => highlightBackground;
        public Image IconImage => iconImage;
    }
}
