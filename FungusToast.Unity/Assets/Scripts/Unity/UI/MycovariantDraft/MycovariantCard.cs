using FungusToast.Core.Mycovariants;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Assets.Scripts.Unity.UI.MycovariantDraft
{
    public class MycovariantCard : MonoBehaviour
    {
        private const float TitleFontSizeMin = 16f;
        private const float TitleFontSizeMax = 20f;

        public Image iconImage;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI effectText;
        public Button pickButton; // Covers card

        // Optionally set these via inspector for designer flexibility
        [Header("Highlight Settings")]
        public Color highlightColor = new Color(1f, 0.93f, 0.25f, 1f); // Bright gold/yellow
        public float highlightAlpha = 1f;

        private Mycovariant mycovariant;
        private System.Action<Mycovariant> onPicked;

        // Cache the outline
        private Outline outline;

        public Mycovariant Mycovariant => mycovariant;

        private void Awake()
        {
            outline = GetComponent<Outline>();
            // Defensive: Outline might be missing on prefab, that's fine.
        }

        public void SetMycovariant(Mycovariant mycovariant, System.Action<Mycovariant> onPicked)
        {
            this.mycovariant = mycovariant;
            this.onPicked = onPicked;

            SetChoiceContent(
                mycovariant,
                mycovariant.Name,
                mycovariant.Description,
                MycovariantArtRepository.GetIcon(mycovariant),
                () => this.onPicked?.Invoke(mycovariant));

            SetActiveHighlight(false);
        }

        public void SetChoiceContent(Mycovariant boundMycovariant, string title, string description, Sprite icon, System.Action onClick)
        {
            mycovariant = boundMycovariant;
            SetCardContent(title, description, icon, onClick);
        }

        public void SetCardContent(string title, string description, Sprite icon, System.Action onClick)
        {
            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.enabled = icon != null;
                iconImage.preserveAspect = true;
                iconImage.color = Color.white;
            }

            if (nameText != null)
            {
                nameText.enableAutoSizing = true;
                nameText.fontSizeMin = TitleFontSizeMin;
                nameText.fontSizeMax = TitleFontSizeMax;
                nameText.textWrappingMode = TextWrappingModes.Normal;
                nameText.overflowMode = TextOverflowModes.Ellipsis;
                nameText.alignment = TextAlignmentOptions.MidlineLeft;
                nameText.text = title;
            }

            if (effectText != null)
            {
                effectText.text = description;
            }

            if (pickButton != null)
            {
                pickButton.onClick.RemoveAllListeners();
                pickButton.onClick.AddListener(() => onClick?.Invoke());
            }

            SetActiveHighlight(false);

            // Force layout rebuild to fix text overlap issues
            // This ensures proper text positioning whenever card content is updated
            Canvas.ForceUpdateCanvases();
            var rectTransform = GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
            }
        }

        /// <summary>
        /// Highlights or unhighlights the card to indicate it's the human's active pick.
        /// </summary>
        public void SetActiveHighlight(bool highlight)
        {
            if (outline != null)
            {
                outline.enabled = highlight;
                if (highlight)
                {
                    var c = highlightColor;
                    c.a = highlightAlpha;
                    outline.effectColor = c;
                }
                // Optionally, set a duller color if not highlighted, or just leave as-is
            }
            else
            {
                // Fallback: iconImage color shift for feedback if no outline
                iconImage.color = highlight
                    ? new Color(1f, 1f, 0.8f, 1f)
                    : Color.white;
            }
        }
    }
}
