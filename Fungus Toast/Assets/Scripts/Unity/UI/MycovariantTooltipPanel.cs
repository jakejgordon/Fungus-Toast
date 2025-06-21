using UnityEngine;
using TMPro; // If using TextMeshPro for text fields

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

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            HideTooltip();
        }

        /// <summary>
        /// Show the tooltip with the given text, anchored to the UI element (optional).
        /// </summary>
        public void ShowTooltip(string header, string body, RectTransform anchor = null)
        {
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
