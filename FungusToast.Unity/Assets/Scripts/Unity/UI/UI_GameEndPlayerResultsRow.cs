using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FungusToast.Unity.UI
{
    public class UI_GameEndPlayerResultsRow : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TextMeshProUGUI rankText;
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI livingText;
        [SerializeField] private TextMeshProUGUI deadText;

        /* -------- public API -------- */
        public void Populate(int rank, Sprite icon, string playerName, int living, int dead)
        {
            rankText.text = rank.ToString();
            iconImage.sprite = icon;
            nameText.text = playerName;
            livingText.text = $"Alive: {living}";
            deadText.text = $"Dead:  {dead}";
        }
    }
}
