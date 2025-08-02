using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FungusToast.Unity.UI.GameLog
{
    /// <summary>
    /// Generic, reusable UI panel for displaying game log entries.
    /// Can be used with any log manager that implements IGameLogManager interface.
    /// </summary>
    public class UI_GameLogPanel : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private Transform contentParent;
        [SerializeField] private UI_GameLogEntry entryPrefab;
        [SerializeField] private Button clearButton;
        [SerializeField] private TextMeshProUGUI headerText;
        
        [Header("Settings")]
        [SerializeField] private int maxVisibleEntries = 30;
        [SerializeField] private bool autoScrollToBottom = true;
        [SerializeField] private string defaultHeaderText = "Game Log";
        
        private List<UI_GameLogEntry> entryUIs = new List<UI_GameLogEntry>();
        private IGameLogManager logManager;
        
        private void Awake()
        {
            if (clearButton != null)
                clearButton.onClick.AddListener(ClearLog);
                
            if (headerText != null && string.IsNullOrEmpty(headerText.text))
                headerText.text = defaultHeaderText;
        }
        
        /// <summary>
        /// Initialize with any log manager that implements IGameLogManager
        /// </summary>
        public void Initialize(IGameLogManager gameLogManager)
        {
            logManager = gameLogManager;
            
            if (logManager != null)
            {
                logManager.OnNewLogEntry += AddLogEntry;
                
                // Add any existing entries
                foreach (var entry in logManager.GetRecentEntries())
                {
                    AddLogEntry(entry);
                }
            }
        }
        
        /// <summary>
        /// Set the header text for this log panel
        /// </summary>
        public void SetHeaderText(string text)
        {
            if (headerText != null)
                headerText.text = text;
        }
        
        private void OnDestroy()
        {
            if (logManager != null)
            {
                logManager.OnNewLogEntry -= AddLogEntry;
            }
        }
        
        public void AddLogEntry(GameLogEntry entry)
        {
            if (entryPrefab == null || contentParent == null)
            {
                Debug.LogError("UI_GameLogPanel: Missing prefab or content parent references!");
                return;
            }
            
            // Create new UI entry
            var entryUI = Instantiate(entryPrefab, contentParent);
            entryUI.SetEntry(entry);
            entryUIs.Add(entryUI);
            
            // Add fade-in effect
            entryUI.FadeIn();
            
            // Remove old entries if over limit
            while (entryUIs.Count > maxVisibleEntries)
            {
                var oldEntry = entryUIs[0];
                entryUIs.RemoveAt(0);
                if (oldEntry != null)
                    Destroy(oldEntry.gameObject);
            }
            
            // Auto-scroll to bottom
            if (autoScrollToBottom && scrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                scrollRect.verticalNormalizedPosition = 0f;
            }
        }
        
        private void ClearLog()
        {
            // Clear UI entries
            foreach (var entryUI in entryUIs)
            {
                if (entryUI != null)
                    Destroy(entryUI.gameObject);
            }
            entryUIs.Clear();
            
            // Clear log manager
            if (logManager != null)
                logManager.ClearLog();
        }
        
        public void SetAutoScroll(bool enabled)
        {
            autoScrollToBottom = enabled;
        }
        
        public void ScrollToBottom()
        {
            if (scrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                scrollRect.verticalNormalizedPosition = 0f;
            }
        }
        
        public void ScrollToTop()
        {
            if (scrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                scrollRect.verticalNormalizedPosition = 1f;
            }
        }
    }
}