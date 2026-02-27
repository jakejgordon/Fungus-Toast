using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FungusToast.Core.Board;
using FungusToast.Core.Players;
using System.Linq;
using TMPro;

namespace FungusToast.Unity.UI
{
    public class UI_EndGamePanel : MonoBehaviour
    {
        /* ─────────── Inspector ─────────── */
        [Header("UI References")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Transform resultsContainer;
        [SerializeField] private UI_GameEndPlayerResultsRow playerResultRowPrefab;
        [SerializeField] private Button continueButton; // campaign mid-run victory only
        [SerializeField] private Button exitButton; // always available to return to mode select
        [SerializeField] private TextMeshProUGUI outcomeLabel; // dynamic outcome messaging

        // Façade reference — set by GameManager so we don't need GameManager.Instance
        private GameUIManager gameUI;
        private System.Action onCampaignResume;
        private System.Action onExitToModeSelect;

        /// <summary>
        /// Call once after the panel is created to wire up dependencies without reaching
        /// into GameManager.Instance.
        /// </summary>
        public void SetDependencies(GameUIManager ui, System.Action campaignResume, System.Action exitToModeSelect)
        {
            gameUI = ui;
            onCampaignResume = campaignResume;
            onExitToModeSelect = exitToModeSelect;
        }

        /* ─────────── Unity ─────────── */
        private void Awake()
        {
            if (playerResultRowPrefab == null)
                Debug.LogError("UI_EndGamePanel: PlayerResultRowPrefab reference is missing!");

            if (continueButton != null)
                continueButton.onClick.AddListener(OnContinueCampaign);
            else
                Debug.LogWarning("UI_EndGamePanel: ContinueButton reference is missing (campaign mid-run victories will have no continue).");

            if (exitButton != null)
                exitButton.onClick.AddListener(OnExitToModeSelect);
            else
                Debug.LogWarning("UI_EndGamePanel: ExitButton reference is missing (player cannot exit results).");

            HideInstant();
        }

        /* ─────────── Public API (generic solo / hotseat) ─────────── */
        public void ShowResults(List<Player> ranked, GameBoard board)
        {
            ShowResultsInternal(ranked, board);
            // Solo / hotseat baseline: only exit button (continue hidden)
            if (continueButton != null) continueButton.gameObject.SetActive(false);
            if (exitButton != null) exitButton.gameObject.SetActive(true);
            if (outcomeLabel != null) outcomeLabel.text = ""; // no special messaging
        }

        /// <summary>
        /// Extended results display including campaign outcome context.
        /// </summary>
        public void ShowResultsWithOutcome(List<Player> ranked, GameBoard board, bool isCampaign, bool victory, bool finalLevel, bool hasNextLevel, int lostLevelDisplay)
        {
            ShowResultsInternal(ranked, board);
            if (!isCampaign)
            {
                // fallback to base behavior
                if (continueButton != null) continueButton.gameObject.SetActive(false);
                if (exitButton != null) exitButton.gameObject.SetActive(true);
                if (outcomeLabel != null) outcomeLabel.text = "";
                return;
            }

            // Campaign messaging
            if (outcomeLabel != null)
            {
                if (!victory)
                {
                    // defeat – show lost level index (1-based)
                    outcomeLabel.text = $"<color=#D63A3A><b>You aren't moldy enough! Campaign lost at level {lostLevelDisplay}.</b></color>";
                }
                else if (finalLevel)
                {
                    outcomeLabel.text = "<color=#32C832><b>You have won the campaign!</b></color>";
                }
                else
                {
                    // mid-run victory
                    outcomeLabel.text = "<color=#32C832><b>Your mold wins! Advance to the next level.</b></color>";
                }
            }

            // Buttons
            if (continueButton != null)
                continueButton.gameObject.SetActive(victory && !finalLevel && hasNextLevel);
            if (exitButton != null)
                exitButton.gameObject.SetActive(true);
        }

        /* ─────────── Internal Row Builder ─────────── */
        private void ShowResultsInternal(List<Player> ranked, GameBoard board)
        {
            /* clear previous rows */
            foreach (Transform child in resultsContainer)
                Destroy(child.gameObject);

            /* build rows */
            int rank = 1;
            foreach (var p in ranked)
            {
                var row = Instantiate(playerResultRowPrefab, resultsContainer);
                int living = board.GetAllCellsOwnedBy(p.PlayerId).Count(c => c.IsAlive);
                int dead = board.GetAllCellsOwnedBy(p.PlayerId).Count(c => !c.IsAlive);
                Sprite icon = gameUI != null
                    ? gameUI.PlayerUIBinder.GetIcon(p)
                    : GameManager.Instance.GameUI.PlayerUIBinder.GetIcon(p);

                row.Populate(rank, icon, p.PlayerName, living, dead);
                rank++;
            }

            gameObject.SetActive(true);
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            if (!gameObject.activeInHierarchy)
            {
                Debug.LogWarning("UI_EndGamePanel is still inactive – coroutine skipped.");
                return;
            }

            /* fade-in */
            StopAllCoroutines();
            StartCoroutine(FadeCanvasGroup(1f, 0.25f));
        }

        /* ─────────── Buttons / Helpers ─────────── */
        private void OnClose()
        {
            // legacy close (non-campaign) – keep ability to just hide panel
            HideInstant();

            // Re-enable the right sidebar so players can see summaries after closing results
            var sidebar = gameUI?.RightSidebar ?? GameManager.Instance?.GameUI?.RightSidebar;
            if (sidebar != null)
            {
                sidebar.gameObject.SetActive(true);
            }
        }

        private void OnContinueCampaign()
        {
            // Mid-run victory continue path
            HideInstant();
            if (onCampaignResume != null)
                onCampaignResume();
            else
                GameManager.Instance?.StartCampaignResume();
        }

        private void OnExitToModeSelect()
        {
            HideInstant();
            if (onExitToModeSelect != null)
                onExitToModeSelect();
            else
                GameManager.Instance?.ShowStartGamePanel();
        }

        private void HideInstant()
        {
            StopAllCoroutines();
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            gameObject.SetActive(false);
        }

        private IEnumerator FadeCanvasGroup(float targetAlpha, float duration)
        {
            float start = canvasGroup.alpha;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(start, targetAlpha, t / duration);
                yield return null;
            }
            canvasGroup.alpha = targetAlpha;
        }
    }
}
