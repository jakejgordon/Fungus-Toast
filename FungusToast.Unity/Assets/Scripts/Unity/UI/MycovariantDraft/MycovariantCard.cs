using FungusToast.Core.Mycovariants;
using FungusToast.Unity.UI;
using FungusToast.Unity.UI.Tooltips;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace Assets.Scripts.Unity.UI.MycovariantDraft
{
    public class MycovariantCard : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        private const float TitleFontSizeMin = 16f;
        private const float TitleFontSizeMax = 20f;
        private const float EffectFontSizeMin = 14f;
        private const float EffectFontSizeMax = 18f;
        private const float TypeBadgeWidth = 92f;
        private const float BadgeHeight = 24f;
        private const float ChooseButtonHeight = 40f;
        private const float CardEdgeInset = 10f;
        private const float MinimumEffectTextHeight = 80f;

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
        private Outline typeBadgeOutline;
        private TooltipTrigger typeBadgeTooltip;
        private GameObject chooseAffordanceRoot;
        private Image chooseAffordanceBackground;
        private bool pickInteractable;
        private bool pointerInside;
        private bool pointerHeld;

        public Mycovariant Mycovariant => mycovariant;

        /// <summary>The card's title and effect copy as set, before any comparison markup.</summary>
        public string Title { get; private set; } = string.Empty;
        public string Description { get; private set; } = string.Empty;

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
            Title = title ?? string.Empty;
            Description = description ?? string.Empty;

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

            RefreshBaitBadge();
            RefreshTypeBadge();
            EnsureChooseAffordance();
            RefreshChooseAffordance();
            SetActiveHighlight(false);

            ReserveRoomForChooseAffordance();

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
        /// Caps the effect text at the room left above the Choose strip. The text carries a
        /// ContentSizeFitter, so it would otherwise keep growing downwards and slide underneath the
        /// strip on a long description; with a fixed box, TMP's auto-sizing shrinks to fit instead,
        /// and <see cref="MeasureAutoSizedEffectFontSize"/> then reports that smaller size so all
        /// three cards still share one size.
        /// </summary>
        private void ReserveRoomForChooseAffordance()
        {
            if (effectText == null)
            {
                return;
            }

            var group = GetComponent<VerticalLayoutGroup>();
            var cardRect = transform as RectTransform;
            if (group == null || cardRect == null)
            {
                return;
            }

            // Measure the siblings that actually flow, so a hidden bait or type badge does not
            // reserve room it is not using.
            float usedByOthers = group.padding.top + group.padding.bottom;
            int flowChildCount = 0;
            foreach (RectTransform child in transform)
            {
                var childLayout = child.GetComponent<LayoutElement>();
                if (!child.gameObject.activeSelf || (childLayout != null && childLayout.ignoreLayout))
                {
                    continue;
                }

                flowChildCount++;
                if (child != effectText.rectTransform)
                {
                    usedByOthers += child.rect.height;
                }
            }

            usedByOthers += group.spacing * Mathf.Max(0, flowChildCount - 1);

            // The strip sits inside the bottom padding band, so only its own height and one gap
            // above it are unaccounted for.
            float available = cardRect.rect.height - usedByOthers - ChooseButtonHeight - group.spacing;
            if (available < MinimumEffectTextHeight)
            {
                return;
            }

            var fitter = effectText.GetComponent<ContentSizeFitter>();
            if (fitter != null)
            {
                fitter.enabled = false;
            }

            effectText.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, available);
        }

        /// <summary>
        /// Swaps in a marked-up copy of <see cref="Description"/> (the draft emphasizes numbers
        /// that differ between offered tiers). Call before measuring the effect font size, since
        /// bold digits are wider.
        /// </summary>
        public void SetEffectTextMarkup(string markedUpDescription)
        {
            if (effectText != null)
            {
                effectText.text = markedUpDescription ?? Description;
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
            pickInteractable = interactable;

            if (pickButton != null)
            {
                pickButton.interactable = interactable;
            }

            if (!interactable)
            {
                pointerInside = false;
                pointerHeld = false;
            }

            RefreshChooseAffordance();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            pointerInside = true;
            RefreshChooseAffordance();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            pointerInside = false;
            pointerHeld = false;
            RefreshChooseAffordance();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            pointerHeld = true;
            RefreshChooseAffordance();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            pointerHeld = false;
            RefreshChooseAffordance();
        }

        /// <summary>
        /// The strip is shown only while this card can actually be picked, so it never invites a
        /// click during an AI turn, and it mirrors the card's own hover and press state because the
        /// card - not the strip - is the control.
        /// </summary>
        private void RefreshChooseAffordance()
        {
            if (chooseAffordanceRoot == null)
            {
                return;
            }

            chooseAffordanceRoot.SetActive(pickInteractable);
            if (!pickInteractable)
            {
                return;
            }

            chooseAffordanceBackground.color = pointerHeld
                ? UIStyleTokens.Button.BackgroundPressed
                : pointerInside
                    ? UIStyleTokens.Button.BackgroundHover
                    : UIStyleTokens.Button.BackgroundDefault;
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
            var accent = isPassive ? UIStyleTokens.State.Info : UIStyleTokens.Accent.Moss;
            typeBadgeLabel.text = isPassive ? MycovariantTagCopy.PassiveLabel : MycovariantTagCopy.OneTimeLabel;
            typeBadgeBackground.color = UIStyleTokens.Badge.Fill(accent);
            typeBadgeOutline.effectColor = UIStyleTokens.Badge.Border(accent);
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

            typeBadgeRoot = new GameObject("TypeBadge", typeof(RectTransform), typeof(Image), typeof(Outline), typeof(TooltipTrigger));
            typeBadgeRoot.transform.SetParent(transform, false);

            // The card root is a VerticalLayoutGroup that does not control child sizes, so the badge
            // keeps the height set here and flows directly beneath the icon/name row.
            typeBadgeRoot.transform.SetSiblingIndex(1);

            var rect = typeBadgeRoot.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(TypeBadgeWidth, BadgeHeight);

            typeBadgeBackground = typeBadgeRoot.GetComponent<Image>();
            typeBadgeBackground.raycastTarget = true;
            typeBadgeTooltip = typeBadgeRoot.GetComponent<TooltipTrigger>();

            typeBadgeOutline = typeBadgeRoot.GetComponent<Outline>();
            typeBadgeOutline.effectDistance = new Vector2(1f, -1f);
            typeBadgeOutline.useGraphicAlpha = true;

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
            typeBadgeLabel.fontSizeMin = UIStyleTokens.Badge.MinimumLabelFontSize;
            typeBadgeLabel.fontSizeMax = UIStyleTokens.Badge.MaximumLabelFontSize;
            typeBadgeLabel.textWrappingMode = TextWrappingModes.NoWrap;
            typeBadgeLabel.color = UIStyleTokens.Badge.Label;
            typeBadgeLabel.raycastTarget = false;
        }

        /// <summary>
        /// Explicit pick affordance pinned to the card's bottom edge. It is deliberately NOT a
        /// Button: the whole card is the single control, and a real button here would be a second
        /// control for the same action, which both teaches that only the button is clickable and
        /// gives keyboard and screen-reader users two stops for one choice. Raycasts pass straight
        /// through to the card beneath, and the strip just reflects that card's state.
        /// </summary>
        private void EnsureChooseAffordance()
        {
            if (chooseAffordanceRoot != null)
            {
                return;
            }

            chooseAffordanceRoot = new GameObject("ChooseAffordance", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            chooseAffordanceRoot.transform.SetParent(transform, false);
            chooseAffordanceRoot.transform.SetAsLastSibling();

            // Anchored to the bottom rather than flowed, so the layout group above does not push it
            // up or down as the description grows and shrinks.
            chooseAffordanceRoot.GetComponent<LayoutElement>().ignoreLayout = true;

            var rect = chooseAffordanceRoot.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.offsetMin = new Vector2(CardEdgeInset, CardEdgeInset);
            rect.offsetMax = new Vector2(-CardEdgeInset, CardEdgeInset + ChooseButtonHeight);

            chooseAffordanceBackground = chooseAffordanceRoot.GetComponent<Image>();
            chooseAffordanceBackground.color = UIStyleTokens.Button.BackgroundDefault;
            chooseAffordanceBackground.raycastTarget = false;

            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(chooseAffordanceRoot.transform, false);
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

            baitBadgeRoot = new GameObject("BaitBadge", typeof(RectTransform), typeof(Image), typeof(Outline), typeof(TooltipTrigger));
            baitBadgeRoot.transform.SetParent(transform, false);
            baitBadgeRoot.transform.SetAsLastSibling();

            var rect = baitBadgeRoot.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-10f, -8f);
            rect.sizeDelta = new Vector2(72f, BadgeHeight);

            var background = baitBadgeRoot.GetComponent<Image>();
            background.color = UIStyleTokens.Badge.Fill(UIStyleTokens.State.Warning);
            background.raycastTarget = true;

            var badgeOutline = baitBadgeRoot.GetComponent<Outline>();
            badgeOutline.effectColor = UIStyleTokens.Badge.Border(UIStyleTokens.State.Warning);
            badgeOutline.effectDistance = new Vector2(1f, -1f);
            badgeOutline.useGraphicAlpha = true;

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
            label.enableAutoSizing = true;
            label.fontSizeMin = UIStyleTokens.Badge.MinimumLabelFontSize;
            label.fontSizeMax = UIStyleTokens.Badge.MaximumLabelFontSize;
            label.color = UIStyleTokens.Badge.Label;
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
