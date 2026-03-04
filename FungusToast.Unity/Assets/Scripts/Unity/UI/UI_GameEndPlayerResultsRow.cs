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
        [SerializeField] private Image rowBackground;

        private void Awake()
        {
            if (rowBackground != null)
            {
                var c = UIStyleTokens.Surface.PanelSecondary;
                c.a = 0.45f;
                rowBackground.color = c;
            }

            if (rankText != null) rankText.color = UIStyleTokens.Text.Primary;
            if (nameText != null) nameText.color = UIStyleTokens.Text.Primary;
            if (livingText != null) livingText.color = UIStyleTokens.Text.Secondary;
            if (deadText != null) deadText.color = UIStyleTokens.Text.Muted;
        }

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
