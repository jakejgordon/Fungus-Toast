using FungusToast.Core.Mycovariants;
using FungusToast.Unity.UI;
using FungusToast.Unity.UI.Tooltips;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Assets.Scripts.Unity.UI.MycovariantDraft
{
    public class MycovariantCard : MonoBehaviour
    {
        private const float TitleFontSizeMin = 16f;
        private const float TitleFontSizeMax = 20f;
        private const float EffectFontSizeMin = 14f;
        private const float EffectFontSizeMax = 18f;
        private const float TypeBadgeWidth = 82f;
        private const float BadgeHeight = 22f;
        private const float ChooseButtonHeight = 40f;
        private const float CardEdgeInset = 10f;

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
        private GameObject baitBadgeRoot;
        private GameObject typeBadgeRoot;
        private TextMeshProUGUI typeBadgeLabel;
        private Image typeBadgeBackground;
        private TooltipTrigger typeBadgeTooltip;
        private Button chooseButton;
        private System.Action chooseAction;

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
                TMPOverflowUtility.SetSafeEllipsis(nameText);
                nameText.alignment = TextAlignmentOptions.MidlineLeft;
                nameText.text = title;
            }

            if (effectText != null)
            {
                effectText.enableAutoSizing = true;
                effectText.fontSizeMin = EffectFontSizeMin;
                effectText.fontSizeMax = EffectFontSizeMax;
                effectText.textWrappingMode = TextWrappingModes.Normal;
                TMPOverflowUtility.SetSafeEllipsis(effectText);
                effectText.alignment = TextAlignmentOptions.TopLeft;
                effectText.text = description;
            }

            if (pickButton != null)
            {
                pickButton.onClick.RemoveAllListeners();
                pickButton.onClick.AddListener(() => onClick?.Invoke());
            }

            chooseAction = onClick;
            RefreshBaitBadge();
            RefreshTypeBadge();
            EnsureChooseButton();
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
        /// Re-runs auto-sizing on the effect text and returns the point size it settled on.
        /// The draft controller feeds the smallest result across the visible cards back into
        /// <see cref="ApplyEffectFontSize"/> so three cards read as a set instead of a short
        /// card at 18pt next to a long one at 14pt.
        /// </summary>
        public float MeasureAutoSizedEffectFontSize()
        {
            if (effectText == null)
            {
                return EffectFontSizeMax;
            }

            effectText.enableAutoSizing = true;
            effectText.fontSizeMin = EffectFontSizeMin;
            effectText.fontSizeMax = EffectFontSizeMax;
            effectText.ForceMeshUpdate(ignoreActiveState: true, forceTextReparsing: true);
            return effectText.fontSize;
        }

        public void ApplyEffectFontSize(float fontSize)
        {
            if (effectText == null)
            {
                return;
            }

            effectText.enableAutoSizing = false;
            effectText.fontSize = Mathf.Clamp(fontSize, EffectFontSizeMin, EffectFontSizeMax);
        }

        /// <summary>
        /// Keeps the whole-card pick button and the explicit Choose button in the same state so a
        /// card cannot look actionable while picks are blocked.
        /// </summary>
        public void SetPickInteractable(bool interactable)
        {
            if (pickButton != null)
            {
                pickButton.interactable = interactable;
            }

            if (chooseButton != null)
            {
                chooseButton.interactable = interactable;
            }
        }

        /// <summary>
        /// One-time versus persistent is the highest-level axis for comparing draft options, but it
        /// was only readable from the opening words of the prose. Passive Mycovariants keep paying
        /// off; every other type resolves once, during the draft.
        /// </summary>
        private void RefreshTypeBadge()
        {
            if (mycovariant == null)
            {
                if (typeBadgeRoot != null)
                {
                    typeBadgeRoot.SetActive(false);
                }

                return;
            }

            EnsureTypeBadge();

            bool isPassive = mycovariant.Type == MycovariantType.Passive;
            typeBadgeLabel.text = isPassive ? MycovariantTagCopy.PassiveLabel : MycovariantTagCopy.OneTimeLabel;
            typeBadgeBackground.color = isPassive ? UIStyleTokens.State.Info : UIStyleTokens.Accent.Moss;
            typeBadgeTooltip.SetStaticText(
                isPassive ? MycovariantTagCopy.PassiveTooltip : MycovariantTagCopy.OneTimeTooltip);
            typeBadgeRoot.SetActive(true);
        }

        private void EnsureTypeBadge()
        {
            if (typeBadgeRoot != null)
            {
                return;
            }

            typeBadgeRoot = new GameObject("TypeBadge", typeof(RectTransform), typeof(Image), typeof(TooltipTrigger));
            typeBadgeRoot.transform.SetParent(transform, false);

            // The card root is a VerticalLayoutGroup that does not control child sizes, so the badge
            // keeps the height set here and flows directly beneath the icon/name row.
            typeBadgeRoot.transform.SetSiblingIndex(1);

            var rect = typeBadgeRoot.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(TypeBadgeWidth, BadgeHeight);

            typeBadgeBackground = typeBadgeRoot.GetComponent<Image>();
            typeBadgeBackground.raycastTarget = true;
            typeBadgeTooltip = typeBadgeRoot.GetComponent<TooltipTrigger>();

            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(typeBadgeRoot.transform, false);
            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(6f, 2f);
            labelRect.offsetMax = new Vector2(-6f, -2f);

            typeBadgeLabel = labelObject.GetComponent<TextMeshProUGUI>();
            typeBadgeLabel.alignment = TextAlignmentOptions.Center;
            typeBadgeLabel.enableAutoSizing = true;
            typeBadgeLabel.fontSizeMin = 11f;
            typeBadgeLabel.fontSizeMax = 13f;
            typeBadgeLabel.textWrappingMode = TextWrappingModes.NoWrap;
            typeBadgeLabel.color = UIStyleTokens.Text.OnAccent;
            typeBadgeLabel.raycastTarget = false;
        }

        /// <summary>
        /// Explicit pick affordance pinned to the card's bottom edge. The whole card has always been
        /// clickable and still is; this only makes that readable, and it puts the blank lower region
        /// of a fixed-height card to use once the description is short.
        /// </summary>
        private void EnsureChooseButton()
        {
            if (chooseButton != null)
            {
                return;
            }

            var buttonObject = new GameObject("ChooseButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonObject.transform.SetParent(transform, false);
            buttonObject.transform.SetAsLastSibling();

            // Anchored to the bottom rather than flowed, so the layout group above does not push it
            // up or down as the description grows and shrinks.
            buttonObject.GetComponent<LayoutElement>().ignoreLayout = true;

            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.offsetMin = new Vector2(CardEdgeInset, CardEdgeInset);
            rect.offsetMax = new Vector2(-CardEdgeInset, CardEdgeInset + ChooseButtonHeight);

            var background = buttonObject.GetComponent<Image>();
            background.color = UIStyleTokens.Button.BackgroundDefault;

            chooseButton = buttonObject.GetComponent<Button>();
            chooseButton.targetGraphic = background;
            chooseButton.colors = new ColorBlock
            {
                normalColor = UIStyleTokens.Button.BackgroundDefault,
                highlightedColor = UIStyleTokens.Button.BackgroundHover,
                pressedColor = UIStyleTokens.Button.BackgroundPressed,
                selectedColor = UIStyleTokens.Button.BackgroundDefault,
                disabledColor = UIStyleTokens.Button.BackgroundDisabled,
                colorMultiplier = 1f,
                fadeDuration = 0.1f,
            };
            chooseButton.onClick.AddListener(() => chooseAction?.Invoke());

            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(buttonObject.transform, false);
            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            var label = labelObject.GetComponent<TextMeshProUGUI>();
            label.text = MycovariantTagCopy.ChooseLabel;
            label.alignment = TextAlignmentOptions.Center;
            label.fontStyle = FontStyles.Bold;
            label.enableAutoSizing = true;
            label.fontSizeMin = 14f;
            label.fontSizeMax = 18f;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.color = UIStyleTokens.Button.TextDefault;
            label.raycastTarget = false;
        }

        private void RefreshBaitBadge()
        {
            bool shouldShow = mycovariant != null && mycovariant.IsBait;

            if (!shouldShow)
            {
                if (baitBadgeRoot != null)
                {
                    baitBadgeRoot.SetActive(false);
                }

                return;
            }

            EnsureBaitBadge();
            baitBadgeRoot.SetActive(true);
        }

        private void EnsureBaitBadge()
        {
            if (baitBadgeRoot != null)
            {
                return;
            }

            baitBadgeRoot = new GameObject("BaitBadge", typeof(RectTransform), typeof(Image), typeof(TooltipTrigger));
            baitBadgeRoot.transform.SetParent(transform, false);
            baitBadgeRoot.transform.SetAsLastSibling();

            var rect = baitBadgeRoot.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-10f, -8f);
            rect.sizeDelta = new Vector2(72f, 24f);

            var background = baitBadgeRoot.GetComponent<Image>();
            background.color = UIStyleTokens.State.Warning;
            background.raycastTarget = true;

            var trigger = baitBadgeRoot.GetComponent<TooltipTrigger>();
            trigger.SetStaticText(MycovariantTagCopy.BaitTooltip);

            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(baitBadgeRoot.transform, false);
            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(6f, 2f);
            labelRect.offsetMax = new Vector2(-6f, -2f);

            var label = labelObject.GetComponent<TextMeshProUGUI>();
            label.text = MycovariantTagCopy.BaitLabel;
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 13f;
            label.enableAutoSizing = true;
            label.fontSizeMin = 11f;
            label.fontSizeMax = 13f;
            label.color = UIStyleTokens.Text.OnAccent;
            label.raycastTarget = false;
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
