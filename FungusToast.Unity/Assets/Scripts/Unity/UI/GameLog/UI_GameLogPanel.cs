using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FungusToast.Unity.UI.GameLog
{
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
        [SerializeField] private bool isPlayerSpecificPanel = false; // set true for per-player log (can be forced at runtime)

        private readonly List<UI_GameLogEntry> entryUIs = new();
        private IGameLogManager logManager;
        private int activePlayerId = -1; // for player-specific filtering

        private void Awake()
        {
            if (clearButton != null)
                clearButton.onClick.AddListener(ClearLog);

            if (headerText != null && string.IsNullOrEmpty(headerText.text))
                headerText.text = defaultHeaderText;
        }

        public void Initialize(IGameLogManager gameLogManager)
        {
            logManager = gameLogManager;
            if (logManager != null)
            {
                logManager.OnNewLogEntry += AddLogEntry;
                foreach (var entry in logManager.GetRecentEntries())
                {
                    AddLogEntry(entry);
                }
            }
        }

        /// <summary>
        /// Force this panel into player-specific filtering mode at runtime (safety for misconfigured inspector).
        /// </summary>
        public void EnablePlayerSpecificFiltering()
        {
            if (!isPlayerSpecificPanel)
            {
                isPlayerSpecificPanel = true;
                Debug.Log("[UI_GameLogPanel] Player-specific filtering enabled at runtime.");
                // Rebuild with current player filter if already set
                if (activePlayerId >= 0 && logManager != null)
                {
                    RebuildForPlayerEntries(logManager.GetRecentEntries(maxVisibleEntries));
                }
            }
        }

        public void SetHeaderText(string text)
        {
            if (headerText != null)
                headerText.text = text;
        }

        public void SetActivePlayer(int playerId, string playerName)
        {
            // Always allow header update (even if panel not flagged) so misconfigured inspector still works
            activePlayerId = playerId;
            if (headerText != null)
                headerText.text = $"{playerName} Activity Log";
            if (isPlayerSpecificPanel)
            {
                Debug.Log($"[UI_GameLogPanel] Switching active player filter -> {playerName} (Id={playerId})");
                RebuildForPlayerEntries(logManager?.GetRecentEntries(maxVisibleEntries) ?? Enumerable.Empty<GameLogEntry>());
            }
        }

        private void OnDestroy()
        {
            if (logManager != null)
                logManager.OnNewLogEntry -= AddLogEntry;
        }

        public void AddLogEntry(GameLogEntry entry)
        {
            if (entryPrefab == null || contentParent == null)
            {
                Debug.LogError("UI_GameLogPanel: Missing prefab or content parent references!");
                return;
            }

            if (isPlayerSpecificPanel)
            {
                // Only display entries relevant to the active player
                if (activePlayerId < 0) return; // not yet bound
                if (entry.PlayerId.HasValue && entry.PlayerId.Value != activePlayerId) return;
            }

            CreateVisualEntry(entry);
        }

        private void CreateVisualEntry(GameLogEntry entry)
        {
            var entryUI = Instantiate(entryPrefab, contentParent);
            entryUI.SetEntry(entry);
            entryUIs.Add(entryUI);
            entryUI.FadeIn();

            while (entryUIs.Count > maxVisibleEntries)
            {
                var oldEntry = entryUIs[0];
                entryUIs.RemoveAt(0);
                if (oldEntry != null)
                    Destroy(oldEntry.gameObject);
            }

            if (autoScrollToBottom && scrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                scrollRect.verticalNormalizedPosition = 0f;
            }
        }

        private void ClearLog()
        {
            foreach (var entryUI in entryUIs)
            {
                if (entryUI != null)
                    Destroy(entryUI.gameObject);
            }
            entryUIs.Clear();

            if (!isPlayerSpecificPanel && logManager != null)
                logManager.ClearLog();
        }

        private void RebuildForPlayerEntries(IEnumerable<GameLogEntry> entries)
        {
            foreach (var e in entryUIs)
                if (e != null) Destroy(e.gameObject);
            entryUIs.Clear();
            if (entries == null) return;
            foreach (var entry in entries.Where(e => !e.PlayerId.HasValue || e.PlayerId == activePlayerId).TakeLast(maxVisibleEntries))
                CreateVisualEntry(entry);
        }

        public void SetAutoScroll(bool enabled) => autoScrollToBottom = enabled;
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
