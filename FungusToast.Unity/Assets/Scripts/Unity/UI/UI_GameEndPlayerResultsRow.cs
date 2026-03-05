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
            if (rowBackground == null)
            {
                rowBackground = GetComponent<Image>();
                if (rowBackground == null)
                {
                    rowBackground = gameObject.AddComponent<Image>();
                }
            }

            if (rowBackground != null)
            {
                var c = UIStyleTokens.Surface.PanelSecondary;
                c.a = 0.6f;
                rowBackground.color = c;
                rowBackground.raycastTarget = false;
            }

            if (rankText != null) rankText.color = UIStyleTokens.Text.Primary;
            if (nameText != null) nameText.color = UIStyleTokens.Text.Primary;
            if (livingText != null) livingText.color = UIStyleTokens.Text.Secondary;
            if (deadText != null) deadText.color = UIStyleTokens.Text.Muted;

            ConfigureText(rankText, TextAlignmentOptions.Center, 26f, allowAutoSize: false);
            ConfigureText(nameText, TextAlignmentOptions.Left, 26f, allowAutoSize: true);
            ConfigureText(livingText, TextAlignmentOptions.Right, 24f, allowAutoSize: false);
            ConfigureText(deadText, TextAlignmentOptions.Right, 24f, allowAutoSize: false);
        }

        /* -------- public API -------- */
        public void Populate(int rank, Sprite icon, string playerName, int living, int dead)
        {
            rankText.text = rank.ToString();
            iconImage.sprite = icon;
            nameText.text = playerName;
            livingText.text = $"Alive {living}";
            deadText.text = $"Dead {dead}";
        }

        private static void ConfigureText(TextMeshProUGUI label, TextAlignmentOptions alignment, float fontSize, bool allowAutoSize)
        {
            if (label == null)
            {
                return;
            }

            label.alignment = alignment;
            label.enableAutoSizing = allowAutoSize;
            if (!allowAutoSize)
            {
                label.fontSize = fontSize;
                label.enableWordWrapping = false;
                label.overflowMode = TextOverflowModes.Ellipsis;
            }
        }
    }
}
