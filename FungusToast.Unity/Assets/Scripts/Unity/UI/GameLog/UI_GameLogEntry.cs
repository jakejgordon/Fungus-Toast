using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

namespace FungusToast.Unity.UI.GameLog
{
    public class UI_GameLogEntry : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private TextMeshProUGUI timestampText;
        [SerializeField] private Image backgroundImage;
        [Header("Auto Height Settings")] 
        [SerializeField] private LayoutElement layoutElement; // optional, assign on prefab root
        [SerializeField] private float verticalPadding = 4f; // extra padding added to calculated height
        [SerializeField] private float minHeight = 24f; // baseline single-line height
        [SerializeField] private float extraSafetyPadding = 2f; // prevents last line clipping
        private bool deferredScheduled = false;

        public void SetEntry(GameLogEntry entry)
        {
            if (messageText != null)
            {
                messageText.text = entry.Message;
                messageText.color = entry.TextColor;
            }
            
            if (timestampText != null)
            {
                timestampText.text = $"R{entry.Round}";
                timestampText.color = Color.black;
            }
            
            // Set background color based on category
            if (backgroundImage != null)
            {
                Color bgColor = entry.Category switch
                {
                    GameLogCategory.Normal => new Color(0.1f, 0.1f, 0.1f, 0.2f),
                    GameLogCategory.Lucky => new Color(0.1f, 0.6f, 0.1f, 0.5f),
                    GameLogCategory.Unlucky => new Color(0.6f, 0.1f, 0.1f, 0.5f),
                    _ => new Color(0.1f, 0.1f, 0.1f, 0.2f)
                };
                backgroundImage.color = bgColor;
            }

            UpdateDynamicHeightImmediate();
            // Schedule a deferred recalculation (width can finalize after first layout pass)
            if (!deferredScheduled && gameObject.activeInHierarchy)
                StartCoroutine(DeferredHeightRecalc());
        }
        
        private void UpdateDynamicHeightImmediate()
        {
            if (messageText == null) return;
            if (layoutElement == null) layoutElement = GetComponent<LayoutElement>();
            if (layoutElement == null) return; // still optional

            // Ensure TMP has generated geometry for current text
            messageText.ForceMeshUpdate();

            // Determine available width for text (current rect width may still be 0 first frame)
            float availableWidth = messageText.rectTransform.rect.width;
            if (availableWidth <= 0f)
            {
                // Try parent width as fallback
                var parentRT = messageText.rectTransform.parent as RectTransform;
                if (parentRT != null) availableWidth = parentRT.rect.width;
            }
            if (availableWidth <= 0f) availableWidth = 500f; // sane fallback

            // Constrained preferred size (height) for current width
            var preferredValues = messageText.GetPreferredValues(messageText.text, availableWidth, 0f);
            float preferredHeight = preferredValues.y;

            float target = Mathf.Max(minHeight, Mathf.Ceil(preferredHeight) + verticalPadding + extraSafetyPadding);

            if (Mathf.Abs(layoutElement.preferredHeight - target) > 0.5f)
            {
                layoutElement.preferredHeight = target;
                LayoutRebuilder.MarkLayoutForRebuild(transform as RectTransform);
            }
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
