using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

namespace FungusToast.Unity.UI.GameLog
{
    public class UI_GameLogEntry : MonoBehaviour
    {
        private const float TimestampMinimumWidth = 34f;
        private const float TimestampMinimumHeight = 20f;
        private const float TimestampTopInset = 2f;

        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private TextMeshProUGUI timestampText;
        [SerializeField] private Image backgroundImage;
        [Header("Auto Height Settings")] 
        [SerializeField] private LayoutElement layoutElement; // optional, assign on prefab root
        [SerializeField] private float verticalPadding = 4f; // extra padding added to calculated height
        [SerializeField] private float minHeight = 32f; // baseline single-line height
        [SerializeField] private float extraSafetyPadding = 4f; // prevents last line clipping
        [SerializeField] private float timestampSpacing = 14f;
        [SerializeField] private float minimumReservedTimestampWidth = TimestampMinimumWidth;
        private bool deferredScheduled = false;
        private bool startsRoundGroup;
        private Image categoryAccent;
        private Image roundSeparator;

        public int DisplayedRound { get; private set; }

        private void Awake()
        {
            ApplyReadabilityStyle();
            EnsureInformationAccents();
        }

        private void ApplyReadabilityStyle()
        {
            if (messageText != null)
            {
                messageText.fontSize = UIStyleTokens.Typography.CaptionMinimum;
                messageText.enableAutoSizing = false;
            }

            if (timestampText != null)
            {
                timestampText.fontSize = UIStyleTokens.Typography.MicroMinimum;
                timestampText.enableAutoSizing = false;

                RectTransform timestampRect = timestampText.rectTransform;
                float width = Mathf.Max(timestampRect.sizeDelta.x, TimestampMinimumWidth);
                float height = Mathf.Max(timestampRect.sizeDelta.y, TimestampMinimumHeight);
                timestampRect.sizeDelta = new Vector2(width, height);
                timestampRect.anchoredPosition = new Vector2(
                    timestampRect.anchoredPosition.x,
                    -(TimestampTopInset + (height * 0.5f)));
            }
        }

        public void SetEntry(GameLogEntry entry, bool beginsRoundGroup = false)
        {
            DisplayedRound = entry.Round;
            startsRoundGroup = beginsRoundGroup;

            if (messageText != null)
            {
                messageText.text = entry.Message;
                messageText.color = GameLogColorSchemes.GetTextColor(entry.Category);
            }
            
            ApplyRoundGroupPresentation();

            ApplyMessageLayoutSpacing();
            
            // Set background color based on category
            if (backgroundImage != null)
            {
                backgroundImage.color = UIStyleTokens.WithAlpha(UIStyleTokens.Surface.PanelSecondary, 0.48f);
            }

            if (categoryAccent != null)
                categoryAccent.color = GameLogColorSchemes.GetTextColor(entry.Category);
            ApplyRoundGroupPresentation();

            UpdateDynamicHeightImmediate();
            // Schedule a deferred recalculation (width can finalize after first layout pass)
            if (!deferredScheduled && gameObject.activeInHierarchy)
                StartCoroutine(DeferredHeightRecalc());
        }

        public void SetRoundGroupStart(bool value)
        {
            startsRoundGroup = value;
            ApplyRoundGroupPresentation();
            UpdateDynamicHeightImmediate();
        }

        private void ApplyRoundGroupPresentation()
        {
            if (timestampText != null)
            {
                timestampText.text = startsRoundGroup ? $"ROUND {DisplayedRound}" : string.Empty;
                timestampText.color = startsRoundGroup ? UIStyleTokens.Text.Primary : UIStyleTokens.Text.Muted;
                timestampText.fontStyle = startsRoundGroup ? FontStyles.Bold : FontStyles.Normal;
                timestampText.textWrappingMode = TextWrappingModes.NoWrap;
            }

            if (roundSeparator != null)
                roundSeparator.gameObject.SetActive(startsRoundGroup);
        }
        
        private void UpdateDynamicHeightImmediate()
        {
            if (messageText == null) return;
            if (layoutElement == null) layoutElement = GetComponent<LayoutElement>();
            if (layoutElement == null) return; // still optional

            ApplyMessageLayoutSpacing();

            // Ensure TMP has generated geometry for current text
            messageText.ForceMeshUpdate();

            // Determine available width for text (current rect width may still be 0 first frame)
            float availableWidth = messageText.rectTransform.rect.width - GetReservedTimestampWidth();
            if (availableWidth <= 0f)
            {
                // Try parent width as fallback
                var parentRT = messageText.rectTransform.parent as RectTransform;
                if (parentRT != null) availableWidth = parentRT.rect.width - GetReservedTimestampWidth();
            }
            if (availableWidth <= 0f) availableWidth = 460f; // sane fallback

            // Constrained preferred size (height) for current width
            var preferredValues = messageText.GetPreferredValues(messageText.text, availableWidth, 0f);
            float preferredHeight = preferredValues.y;

            float target = Mathf.Max(
                minHeight,
                Mathf.Ceil(preferredHeight) + Mathf.Max(verticalPadding, 8f) + Mathf.Max(extraSafetyPadding, 4f));

            if (Mathf.Abs(layoutElement.preferredHeight - target) > 0.5f)
            {
                layoutElement.preferredHeight = target;
                LayoutRebuilder.MarkLayoutForRebuild(transform as RectTransform);
            }
        }

        private void ApplyMessageLayoutSpacing()
        {
            if (messageText == null)
            {
                return;
            }

            messageText.textWrappingMode = TextWrappingModes.Normal;
            messageText.overflowMode = TextOverflowModes.Overflow;

            float reservedWidth = GetReservedTimestampWidth();
            Vector4 margin = messageText.margin;
            if (Mathf.Abs(margin.z - reservedWidth) > 0.5f)
            {
                messageText.margin = new Vector4(margin.x, margin.y, reservedWidth, margin.w);
            }
        }

        private float GetReservedTimestampWidth()
        {
            if (timestampText == null || !startsRoundGroup)
            {
                return 0f;
            }

            timestampText.ForceMeshUpdate();
            float preferredWidth = timestampText.GetPreferredValues(timestampText.text, 0f, 0f).x;
            return Mathf.Max(minimumReservedTimestampWidth, preferredWidth + timestampSpacing);
        }

        private void EnsureInformationAccents()
        {
            if (categoryAccent == null)
            {
                categoryAccent = CreateAccent("CategoryAccent", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(4f, 0f));
            }

            if (roundSeparator == null)
            {
                roundSeparator = CreateAccent("RoundSeparator", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 2f));
                roundSeparator.color = UIStyleTokens.Accent.Spore;
                roundSeparator.raycastTarget = false;
            }
        }

        private Image CreateAccent(string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 size)
        {
            var accentObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            accentObject.transform.SetParent(transform, false);
            accentObject.transform.SetAsFirstSibling();
            var rect = accentObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = Vector2.zero;
            var image = accentObject.GetComponent<Image>();
            image.raycastTarget = false;
            return image;
        }

        private IEnumerator DeferredHeightRecalc()
        {
            deferredScheduled = true;
            // Wait one frame so parent layout / horizontal groups settle widths
            yield return null;
            UpdateDynamicHeightImmediate();
            deferredScheduled = false;
        }
        
        public void FadeIn()
        {
            var canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            
            if (!gameObject.activeInHierarchy)
            {
                canvasGroup.alpha = 1f;
                return;
            }
            
            canvasGroup.alpha = 0f;
            StartCoroutine(FadeInCoroutine(canvasGroup));
        }

        /// <summary>
        /// Prepares this entry for return to the pool. Stops running coroutines
        /// and resets visual state so it is clean for the next use.
        /// </summary>
        public void ResetForReuse()
        {
            StopAllCoroutines();
            deferredScheduled = false;

            if (messageText != null)
                messageText.text = string.Empty;
            if (timestampText != null)
                timestampText.text = string.Empty;

            DisplayedRound = 0;
            startsRoundGroup = false;
            if (categoryAccent != null)
                categoryAccent.color = Color.clear;
            if (roundSeparator != null)
                roundSeparator.gameObject.SetActive(false);

            var canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup != null)
                canvasGroup.alpha = 1f;
        }
        
        private System.Collections.IEnumerator FadeInCoroutine(CanvasGroup canvasGroup)
        {
            float duration = 0.3f;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
                yield return null;
            }
            
            canvasGroup.alpha = 1f;
        }
    }
}
