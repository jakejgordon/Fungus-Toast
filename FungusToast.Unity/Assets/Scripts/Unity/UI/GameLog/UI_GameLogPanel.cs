using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Pool;
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
        private ObjectPool<UI_GameLogEntry> entryPool;
        private IGameLogManager logManager;
        private int activePlayerId = -1; // for player-specific filtering
        private bool subscribed = false; // prevent double subscription
        private bool pendingLayoutRebuild = false; // coalesce multiple adds per frame
        private int pendingBottomScrollFrames = 0;

        private void Awake()
        {
            ApplyStyle();

            if (clearButton != null)
                clearButton.onClick.AddListener(ClearLog);

            if (headerText != null && string.IsNullOrEmpty(headerText.text))
                headerText.text = defaultHeaderText;

            // Initialize the object pool for log entries.
            // defaultCapacity matches maxVisibleEntries; max is a safety cap.
            entryPool = new ObjectPool<UI_GameLogEntry>(
                createFunc: () =>
                {
                    var entry = Instantiate(entryPrefab, contentParent);
                    return entry;
                },
                actionOnGet: entry =>
                {
                    entry.transform.SetParent(contentParent, false);
                    entry.gameObject.SetActive(true);
                },
                actionOnRelease: entry =>
                {
                    entry.ResetForReuse();
                    entry.gameObject.SetActive(false);
                },
                actionOnDestroy: entry =>
                {
                    if (entry != null) Destroy(entry.gameObject);
                },
                collectionCheck: false,
                defaultCapacity: maxVisibleEntries,
                maxSize: maxVisibleEntries * 2
            );
        }

        private void ApplyStyle()
        {
            UIStyleTokens.ApplyPanelSurface(gameObject, UIStyleTokens.Surface.PanelPrimary);

            if (contentParent != null)
            {
                UIStyleTokens.ApplyPanelSurface(contentParent.gameObject, UIStyleTokens.Surface.PanelSecondary);
            }

            if (scrollRect != null)
            {
                ApplyImageColor(scrollRect.GetComponent<Image>(), UIStyleTokens.Surface.PanelPrimary);
                if (scrollRect.viewport != null)
                {
                    ApplyImageColor(scrollRect.viewport.GetComponent<Image>(), UIStyleTokens.Surface.PanelSecondary);
                }

                if (scrollRect.content != null)
                {
                    ApplyImageColor(scrollRect.content.GetComponent<Image>(), UIStyleTokens.Surface.PanelSecondary);
                }
            }

            if (headerText != null)
            {
                headerText.color = UIStyleTokens.Text.Primary;
            }

            if (clearButton != null)
            {
                UIStyleTokens.Button.ApplyStyle(clearButton);
            }

            UIStyleTokens.ApplyNonButtonTextPalette(gameObject, headingSizeThreshold: 22f);
        }

        private static void ApplyImageColor(Image image, Color color)
        {
            if (image != null)
            {
                image.color = color;
            }
        }

        private void LateUpdate()
        {
            if (pendingLayoutRebuild)
            {
                ForceLayoutRefreshImmediate();
                pendingLayoutRebuild = false;
            }

            if (pendingBottomScrollFrames > 0)
            {
                ForceLayoutRefreshImmediate();
                pendingBottomScrollFrames--;
            }
        }

        public void Initialize(IGameLogManager gameLogManager)
        {
            // Unsubscribe old manager if switching
            if (subscribed && logManager != null && !ReferenceEquals(logManager, gameLogManager))
            {
                logManager.OnNewLogEntry -= AddLogEntry;
                subscribed = false;
            }

            logManager = gameLogManager;
            if (logManager != null)
            {
                if (!subscribed)
                {
                    logManager.OnNewLogEntry += AddLogEntry;
                    subscribed = true;
                }

                // Clear existing visual list to avoid duplicates when re-initializing
                foreach (var e in entryUIs)
                    if (e != null) entryPool.Release(e);
                entryUIs.Clear();

                // Populate with current history once
                foreach (var entry in logManager.GetRecentEntries(maxVisibleEntries))
                {
                    AddLogEntry(entry);
                }
                QueueLayoutRefresh();
                QueueBottomScrollFollowup();
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
            activePlayerId = playerId;
            if (headerText != null)
                headerText.text = $"{playerName} Activity Log";
            if (isPlayerSpecificPanel)
            {
                RebuildForPlayerEntries(logManager?.GetRecentEntries(maxVisibleEntries) ?? Enumerable.Empty<GameLogEntry>());
            }
        }

        private void OnDestroy()
        {
            if (subscribed && logManager != null)
            {
                logManager.OnNewLogEntry -= AddLogEntry;
                subscribed = false;
            }
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
                if (activePlayerId < 0) return; // not yet bound
                if (entry.PlayerId.HasValue && entry.PlayerId.Value != activePlayerId) return;
            }

            CreateVisualEntry(entry);
        }

        private void CreateVisualEntry(GameLogEntry entry)
        {
            var entryUI = entryPool.Get();
            entryUI.SetEntry(entry);
            entryUIs.Add(entryUI);
            entryUI.FadeIn();

            while (entryUIs.Count > maxVisibleEntries)
            {
                var oldEntry = entryUIs[0];
                entryUIs.RemoveAt(0);
                if (oldEntry != null)
                    entryPool.Release(oldEntry);
            }

            QueueLayoutRefresh();
            QueueBottomScrollFollowup();
        }

        private void ClearLog()
        {
            foreach (var entryUI in entryUIs)
            {
                if (entryUI != null)
                    entryPool.Release(entryUI);
            }
            entryUIs.Clear();

            if (!isPlayerSpecificPanel && logManager != null)
                logManager.ClearLog();

            QueueLayoutRefresh();
            QueueBottomScrollFollowup();
        }

        private void RebuildForPlayerEntries(IEnumerable<GameLogEntry> entries)
        {
            foreach (var e in entryUIs)
                if (e != null) entryPool.Release(e);
            entryUIs.Clear();
            if (entries == null) return;
            foreach (var entry in entries.Where(e => !e.PlayerId.HasValue || e.PlayerId == activePlayerId).TakeLast(maxVisibleEntries))
                CreateVisualEntry(entry);
            QueueLayoutRefresh();
            QueueBottomScrollFollowup();
        }

        private void QueueLayoutRefresh() => pendingLayoutRebuild = true;

        private void QueueBottomScrollFollowup()
        {
            if (!autoScrollToBottom || scrollRect == null)
            {
                return;
            }

            pendingBottomScrollFrames = Mathf.Max(pendingBottomScrollFrames, 3);
        }

        private void ForceLayoutRefreshImmediate()
        {
            if (contentParent == null) return;

            Canvas.ForceUpdateCanvases();

            var contentRT = contentParent as RectTransform;
            if (contentRT != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentRT);

            if (scrollRect != null && scrollRect.viewport != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.viewport);

            if (scrollRect != null && scrollRect.content != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);

            if (autoScrollToBottom && scrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                scrollRect.StopMovement();
                scrollRect.verticalNormalizedPosition = 0f;
                scrollRect.velocity = Vector2.zero;
            }
        }

        public void SetAutoScroll(bool enabled) => autoScrollToBottom = enabled;
        public void ScrollToBottom()
        {
            if (scrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                scrollRect.StopMovement();
                scrollRect.verticalNormalizedPosition = 0f;
                scrollRect.velocity = Vector2.zero;
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
