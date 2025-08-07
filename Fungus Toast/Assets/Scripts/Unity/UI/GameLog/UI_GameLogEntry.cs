using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace FungusToast.Unity.UI.GameLog
{
    public class UI_GameLogEntry : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private TextMeshProUGUI timestampText;
        [SerializeField] private Image backgroundImage;
        
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
                    GameLogCategory.Normal => new Color(0.1f, 0.1f, 0.1f, 0.2f), // Subtle dark gray
                    // Enhanced background contrast for better readability
                    GameLogCategory.Lucky => new Color(0.1f, 0.6f, 0.1f, 0.5f), // More visible green tint
                    GameLogCategory.Unlucky => new Color(0.6f, 0.1f, 0.1f, 0.5f), // More visible red tint
                    _ => new Color(0.1f, 0.1f, 0.1f, 0.2f) // Default to normal
                };
                backgroundImage.color = bgColor;
            }
        }
        
        public void FadeIn()
        {
            // Get or create canvas group once
            var canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            
            // Don't try to start coroutines on inactive GameObjects
            if (!gameObject.activeInHierarchy)
            {
                // If we're not active, just set alpha to 1 immediately for when we become active later
                canvasGroup.alpha = 1f;
                return;
            }
            
            // Simple fade-in animation using Unity's built-in CanvasGroup
            canvasGroup.alpha = 0f;
            
            // Simple coroutine-based fade instead of LeanTween
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