using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Globalization;
using System;

namespace FungusToast.Unity.UI
{
    public class UI_GameEndPlayerResultsRow : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TextMeshProUGUI rankText;
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI livingText;
        private TextMeshProUGUI resistantText = null!;
        [SerializeField] private TextMeshProUGUI deadText;
        [SerializeField] private TextMeshProUGUI toxinText;
        [SerializeField] private Button detailsButton;
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
            if (resistantText != null) resistantText.color = UIStyleTokens.State.Success;
            if (deadText != null) deadText.color = UIStyleTokens.Text.Muted;
            if (toxinText != null) toxinText.color = UIStyleTokens.Text.Muted;

            ConfigureText(rankText, TextAlignmentOptions.Center, 23f, allowAutoSize: false);
            ConfigureText(nameText, TextAlignmentOptions.Left, 23f, allowAutoSize: true);
            ConfigureText(livingText, TextAlignmentOptions.Right, 21f, allowAutoSize: false);
            EnsureResistantText();
            ConfigureText(deadText, TextAlignmentOptions.Right, 21f, allowAutoSize: false);
            EnsureToxinText();
            EnsureDetailsButton();
        }

        /* -------- public API -------- */
        public void Populate(int rank, Sprite icon, string playerName, int living, int resistant, int dead, int toxins, Action onDetailsRequested = null)
        {
            rankText.text = rank.ToString();
            iconImage.sprite = icon;
            nameText.text = playerName;
            livingText.text = FormatCount(living);
            if (resistantText != null)
            {
                resistantText.text = FormatCount(resistant);
            }

            deadText.text = FormatCount(dead);
            if (toxinText != null)
            {
                toxinText.text = FormatCount(toxins);
            }

            ConfigureDetailsButton(onDetailsRequested);
        }

        private void EnsureResistantText()
        {
            if (resistantText != null || deadText == null)
            {
                return;
            }

            var clone = Instantiate(deadText.gameObject, deadText.transform.parent);
            clone.name = "UI_PlayerResultsResistantText";
            clone.transform.SetSiblingIndex(deadText.transform.GetSiblingIndex());

            resistantText = clone.GetComponent<TextMeshProUGUI>();
            if (resistantText != null)
            {
                resistantText.color = UIStyleTokens.State.Success;
                ConfigureText(resistantText, TextAlignmentOptions.Right, 21f, allowAutoSize: false);
                resistantText.text = string.Empty;
            }

            var layout = clone.GetComponent<LayoutElement>();
            if (layout == null)
            {
                layout = clone.AddComponent<LayoutElement>();
            }

            layout.preferredWidth = 140f;
            layout.flexibleWidth = -1f;
        }

        private void EnsureToxinText()
        {
            if (toxinText != null || deadText == null)
            {
                return;
            }

            var clone = Instantiate(deadText.gameObject, deadText.transform.parent);
            clone.name = "UI_PlayerResultsToxinText";
            clone.transform.SetSiblingIndex(deadText.transform.GetSiblingIndex() + 1);

            toxinText = clone.GetComponent<TextMeshProUGUI>();
            if (toxinText != null)
            {
                toxinText.color = UIStyleTokens.Text.Muted;
                ConfigureText(toxinText, TextAlignmentOptions.Right, 21f, allowAutoSize: false);
                toxinText.text = "";
            }

            var layout = clone.GetComponent<LayoutElement>();
            if (layout == null)
            {
                layout = clone.AddComponent<LayoutElement>();
            }

            layout.preferredWidth = 140f;
            layout.flexibleWidth = -1f;
        }

        private void EnsureDetailsButton()
        {
            if (detailsButton != null)
            {
                ApplyDetailsButtonStyle(detailsButton);
                return;
            }

            Transform parent = toxinText != null ? toxinText.transform.parent : transform;
            if (parent == null)
            {
                return;
            }

            var buttonObject = new GameObject("UI_PlayerResultsDetailsButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonObject.transform.SetParent(parent, false);
            buttonObject.transform.SetAsLastSibling();

            var image = buttonObject.GetComponent<Image>();
            image.raycastTarget = true;

            detailsButton = buttonObject.GetComponent<Button>();

            var layout = buttonObject.GetComponent<LayoutElement>();
            layout.preferredWidth = 132f;
            layout.minWidth = 116f;
            layout.preferredHeight = 42f;
            layout.minHeight = 38f;
            layout.flexibleWidth = -1f;

            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(buttonObject.transform, false);

            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(10f, 4f);
            labelRect.offsetMax = new Vector2(-10f, -4f);

            var label = labelObject.GetComponent<TextMeshProUGUI>();
            label.text = "Details";
            label.alignment = TextAlignmentOptions.Center;
            label.enableAutoSizing = true;
            label.fontSizeMax = 20f;
            label.fontSizeMin = 13f;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.raycastTarget = false;

            ApplyDetailsButtonStyle(detailsButton);
        }

        private void ConfigureDetailsButton(Action onDetailsRequested)
        {
            EnsureDetailsButton();
            if (detailsButton == null)
            {
                return;
            }

            detailsButton.onClick.RemoveAllListeners();

            bool hasAction = onDetailsRequested != null;
            detailsButton.gameObject.SetActive(hasAction);
            detailsButton.interactable = hasAction;

            if (!hasAction)
            {
                return;
            }

            detailsButton.onClick.AddListener(() => onDetailsRequested());
            var trigger = detailsButton.GetComponent<FungusToast.Unity.UI.Tooltips.TooltipTrigger>();
            if (trigger == null)
            {
                trigger = detailsButton.gameObject.AddComponent<FungusToast.Unity.UI.Tooltips.TooltipTrigger>();
            }

            trigger.SetStaticText("View this player's end-of-game build details.");
        }

        private static void ApplyDetailsButtonStyle(Button button)
        {
            if (button == null)
            {
                return;
            }

            UIStyleTokens.Button.ApplyPanelSecondaryStyle(button);

            var label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                label.text = "Details";
                label.alignment = TextAlignmentOptions.Center;
                label.enableAutoSizing = true;
                label.fontSizeMax = 20f;
                label.fontSizeMin = 13f;
                label.fontStyle = FontStyles.Bold;
                label.color = UIStyleTokens.Text.Primary;
            }
        }

        private static string FormatCount(int value)
        {
            return value.ToString("N0", CultureInfo.CurrentCulture);
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
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.overflowMode = TextOverflowModes.Ellipsis;
            }
        }
    }
}
