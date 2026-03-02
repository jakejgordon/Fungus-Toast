using UnityEngine;
using TMPro; // If using TextMeshPro for text fields
using UnityEngine.UI;

namespace Assets.Scripts.Unity.UI
{
    /// <summary>
    /// Singleton panel for displaying tooltips on the UI.
    /// Must be present in your Canvas hierarchy.
    /// </summary>
    public class MycovariantTooltipPanel : MonoBehaviour
    {
        public static MycovariantTooltipPanel Instance { get; private set; }

        [SerializeField] private GameObject panelRoot; // The panel object to enable/disable
        [SerializeField] private TextMeshProUGUI headerText;
        [SerializeField] private TextMeshProUGUI bodyText;
        [SerializeField] private Image panelBackground;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            ApplyStyle();
            HideTooltip();
        }

        private void ApplyStyle()
        {
            if (panelRoot != null)
            {
                FungusToast.Unity.UI.UIStyleTokens.ApplyPanelSurface(panelRoot, FungusToast.Unity.UI.UIStyleTokens.Surface.PanelSecondary);
            }

            if (panelBackground == null && panelRoot != null)
            {
                panelBackground = panelRoot.GetComponent<Image>();
            }

            if (panelBackground != null)
            {
                panelBackground.color = FungusToast.Unity.UI.UIStyleTokens.Surface.PanelSecondary;
            }

            if (headerText != null)
            {
                headerText.color = FungusToast.Unity.UI.UIStyleTokens.Text.Primary;
            }

            if (bodyText != null)
            {
                bodyText.color = FungusToast.Unity.UI.UIStyleTokens.Text.Secondary;
            }
        }

        /// <summary>
        /// Show the tooltip with the given text, anchored to the UI element (optional).
        /// </summary>
        public void ShowTooltip(string header, string body, RectTransform anchor = null)
        {
            ApplyStyle();
            headerText.text = header;
            bodyText.text = body;
            panelRoot.SetActive(true);

            // Optional: Position panel near anchor, or follow mouse
            // For simplicity, you can skip or improve this later
        }

        public void HideTooltip()
        {
            panelRoot.SetActive(false);
        }
    }
}
