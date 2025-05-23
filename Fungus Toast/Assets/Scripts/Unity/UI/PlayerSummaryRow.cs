using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FungusToast.Unity.UI
{
    public class PlayerSummaryRow : MonoBehaviour
    {
        [SerializeField] private Image moldIconImage;
        [SerializeField] private TextMeshProUGUI livingCellsText;
        [SerializeField] private TextMeshProUGUI deadCellsText;

        public void SetIcon(Sprite sprite)
        {
            if (moldIconImage != null)
                moldIconImage.sprite = sprite;
        }

        public void SetCounts(string living, string dead)
        {
            if (livingCellsText != null)
                livingCellsText.text = $"Alive: {living}";
            if (deadCellsText != null)
                deadCellsText.text = $"Dead: {dead}";
        }
    }
}
