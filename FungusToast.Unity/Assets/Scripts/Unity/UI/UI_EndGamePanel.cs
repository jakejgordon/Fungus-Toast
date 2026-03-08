using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FungusToast.Core.Board;
using FungusToast.Core.Players;
using System.Linq;
using TMPro;
using FungusToast.Unity.UI.Tooltips;
using System.Globalization;

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
        [SerializeField] private Button playAgainButton; // solo / hotseat replay
        [SerializeField] private TextMeshProUGUI outcomeLabel; // dynamic outcome messaging
        [SerializeField] private Image panelBackground;
        [SerializeField] private Image resultsCardBackground;

        // Façade reference — set by GameManager so we don't need GameManager.Instance
        private GameUIManager gameUI;
        private System.Action onCampaignResume;
        private System.Action onExitToModeSelect;
        private bool requiresAdaptationBeforeContinue;

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

            ApplyStyle();
            ApplyTooltips();

            if (continueButton != null)
                continueButton.onClick.AddListener(OnContinueCampaign);
            else
                Debug.LogWarning("UI_EndGamePanel: ContinueButton reference is missing (campaign mid-run victories will have no continue).");

            if (playAgainButton != null)
                playAgainButton.onClick.AddListener(OnReturnToMainMenu);
            else
                Debug.LogWarning("UI_EndGamePanel: PlayAgainButton reference is missing (player cannot return to main menu).");

            if (exitButton != null)
                exitButton.onClick.AddListener(OnExitGame);
            else
                Debug.LogWarning("UI_EndGamePanel: ExitButton reference is missing (player cannot exit results).");

            HideInstant();
        }

        private void ApplyStyle()
        {
            if (panelBackground == null)
            {
                panelBackground = GetComponent<Image>();
            }

            if (panelBackground != null)
            {
                panelBackground.color = UIStyleTokens.Surface.OverlayDim;
            }

            if (resultsCardBackground != null)
            {
                resultsCardBackground.color = UIStyleTokens.Surface.PanelPrimary;
            }

            UIStyleTokens.Button.ApplyStyle(continueButton, useSelectedAsNormal: true);
            UIStyleTokens.Button.ApplyStyle(exitButton);
            UIStyleTokens.Button.ApplyStyle(playAgainButton, useSelectedAsNormal: true);

            EnsureButtonLayout(continueButton);
            EnsureButtonLayout(exitButton);
            EnsureButtonLayout(playAgainButton);

            if (outcomeLabel != null)
            {
                outcomeLabel.color = UIStyleTokens.Text.Primary;
            }

            UIStyleTokens.ApplyNonButtonTextPalette(gameObject, headingSizeThreshold: 30f);
        }

        private void ApplyTooltips()
        {
            EnsureTooltip(playAgainButton, "Return to the main menu to start a new game.");
            EnsureTooltip(exitButton, "Close the game.");
            EnsureTooltip(continueButton, "Advance to the next campaign level.");
        }

        /* ─────────── Public API (generic solo / hotseat) ─────────── */
        public void ShowResults(List<Player> ranked, GameBoard board)
        {
            ShowResultsInternal(ranked, board);
            // Solo / hotseat baseline: only exit button (continue hidden)
            if (continueButton != null) continueButton.gameObject.SetActive(false);
            if (exitButton != null) exitButton.gameObject.SetActive(true);
            if (playAgainButton != null) playAgainButton.gameObject.SetActive(true);
            if (outcomeLabel != null) outcomeLabel.text = ""; // no special messaging
        }

        /// <summary>
        /// Extended results display including campaign outcome context.
        /// </summary>
        public void ShowResultsWithOutcome(
            List<Player> ranked,
            GameBoard board,
            bool isCampaign,
            bool victory,
            bool finalLevel,
            bool hasNextLevel,
            int lostLevelDisplay,
            bool adaptationPending)
        {
            ShowResultsInternal(ranked, board);
            requiresAdaptationBeforeContinue = false;
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
                    outcomeLabel.text =
                        $"<color=#{ToHex(UIStyleTokens.State.Danger)}><b>Campaign lost</b></color>\n" +
                        $"<size=28><color=#{ToHex(UIStyleTokens.Text.Secondary)}>Level {lostLevelDisplay}</color></size>";
                }
                else if (finalLevel)
                {
                    outcomeLabel.text =
                        $"<color=#{ToHex(UIStyleTokens.State.Success)}><b>Campaign complete</b></color>\n" +
                        $"<size=28><color=#{ToHex(UIStyleTokens.Text.Secondary)}>Congratulations you mycelial mastermind! You won the campaign!</color></size>";
                }
                else
                {
                    // mid-run victory
                    outcomeLabel.text =
                        $"<color=#{ToHex(UIStyleTokens.State.Success)}><b>Level cleared</b></color>\n" +
                        $"<size=28><color=#{ToHex(UIStyleTokens.Text.Secondary)}>Select an Adaptation to continue the campaign.</color></size>";
                }
            }

            // Buttons
            requiresAdaptationBeforeContinue = adaptationPending;
            if (continueButton != null)
                continueButton.gameObject.SetActive(victory && !finalLevel && hasNextLevel);
            if (exitButton != null)
                exitButton.gameObject.SetActive(true);
            if (playAgainButton != null)
                playAgainButton.gameObject.SetActive(!requiresAdaptationBeforeContinue);

            if (continueButton != null)
            {
                SetButtonLabel(continueButton, requiresAdaptationBeforeContinue ? "Select Adaptation" : "Continue Campaign");
            }
        }

        /* ─────────── Internal Row Builder ─────────── */
        private void ShowResultsInternal(List<Player> ranked, GameBoard board)
        {
            /* clear previous rows */
            foreach (Transform child in resultsContainer)
                Destroy(child.gameObject);

            BuildResultsHeader();

            var summaries = BoardUtilities.GetPlayerBoardSummaries(ranked, board);

            /* build rows */
            int rank = 1;
            foreach (var p in ranked)
            {
                var row = Instantiate(playerResultRowPrefab, resultsContainer);
                var summary = summaries[p.PlayerId];
                Sprite icon = gameUI != null
                    ? gameUI.PlayerUIBinder.GetIcon(p)
                    : GameManager.Instance.GameUI.PlayerUIBinder.GetIcon(p);

                row.Populate(rank, icon, p.PlayerName, summary.LivingCells, summary.DeadCells, summary.ToxinCells);
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
            if (requiresAdaptationBeforeContinue)
            {
                var manager = GameManager.Instance;
                HideInstant();

                bool started = manager != null && manager.TryStartCampaignAdaptationDraft(OnCampaignAdaptationSelected);
                if (!started)
                {
                    requiresAdaptationBeforeContinue = false;
                    if (onCampaignResume != null)
                        onCampaignResume();
                    else
                        manager?.StartCampaignResume();
                }
                return;
            }

            // Mid-run victory continue path
            HideInstant();
            if (onCampaignResume != null)
                onCampaignResume();
            else
                GameManager.Instance?.StartCampaignResume();
        }

        private void OnCampaignAdaptationSelected()
        {
            requiresAdaptationBeforeContinue = false;

            if (outcomeLabel != null)
            {
                outcomeLabel.text =
                    $"<color=#{ToHex(UIStyleTokens.State.Success)}><b>Adaptation secured</b></color>\n" +
                    $"<size=28><color=#{ToHex(UIStyleTokens.Text.Secondary)}>Continue when you are ready for the next level.</color></size>";
            }

            if (continueButton != null)
            {
                continueButton.gameObject.SetActive(true);
                SetButtonLabel(continueButton, "Continue Campaign");
            }

            if (exitButton != null)
            {
                exitButton.gameObject.SetActive(true);
            }

            if (playAgainButton != null)
            {
                playAgainButton.gameObject.SetActive(true);
            }

            gameObject.SetActive(true);
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        private void OnReturnToMainMenu()
        {
            HideInstant();
            if (onExitToModeSelect != null)
                onExitToModeSelect();
            else
                GameManager.Instance?.ReturnToMainMenu();
        }

        private void OnExitGame()
        {
            HideInstant();
            GameManager.Instance?.QuitGame();
        }

        private void HideInstant()
        {
            StopAllCoroutines();
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            gameObject.SetActive(false);
        }

        private static void SetButtonLabel(Button button, string text)
        {
            if (button == null)
            {
                return;
            }

            var label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                label.text = text;
                return;
            }

            var legacyLabel = button.GetComponentInChildren<Text>(true);
            if (legacyLabel != null)
            {
                legacyLabel.text = text;
            }
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

        private void BuildResultsHeader()
        {
            if (resultsContainer == null)
            {
                return;
            }

            var header = new GameObject("UI_GameEndResultsHeaderRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            header.transform.SetParent(resultsContainer, false);

            var layout = header.GetComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.spacing = 14f;
            layout.padding = new RectOffset(10, 10, 2, 2);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var headerLayout = header.GetComponent<LayoutElement>();
            headerLayout.preferredHeight = 30f;

            CreateHeaderCell(header.transform, string.Empty, 60f, TextAlignmentOptions.Center, false);
            CreateHeaderCell(header.transform, string.Empty, 60f, TextAlignmentOptions.Center, false);
            CreateHeaderCell(header.transform, "Player", 260f, TextAlignmentOptions.Left, true);
            CreateHeaderCell(header.transform, "Alive", 140f, TextAlignmentOptions.Right, false);
            CreateHeaderCell(header.transform, "Dead", 140f, TextAlignmentOptions.Right, false);
            CreateHeaderCell(header.transform, "Toxins", 140f, TextAlignmentOptions.Right, false);
        }

        private void CreateHeaderCell(Transform parent, string text, float preferredWidth, TextAlignmentOptions alignment, bool flexible)
        {
            var cell = new GameObject($"UI_GameEndHeader_{text}", typeof(RectTransform), typeof(LayoutElement), typeof(TextMeshProUGUI));
            cell.transform.SetParent(parent, false);

            var layout = cell.GetComponent<LayoutElement>();
            layout.preferredWidth = preferredWidth;
            layout.flexibleWidth = flexible ? 1f : -1f;

            var label = cell.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.color = UIStyleTokens.Text.Secondary;
            label.fontStyle = FontStyles.Bold;
            label.fontSize = 20f;
            label.alignment = alignment;
            label.enableAutoSizing = true;
            label.fontSizeMax = 20f;
            label.fontSizeMin = 14f;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
        }

        private static void EnsureButtonLayout(Button button)
        {
            if (button == null)
            {
                return;
            }

            var layout = button.GetComponent<LayoutElement>();
            if (layout == null)
            {
                layout = button.gameObject.AddComponent<LayoutElement>();
            }

            layout.preferredHeight = 56f;
            layout.minHeight = 52f;
            layout.preferredWidth = 240f;
        }

        private static void EnsureTooltip(Button button, string text)
        {
            if (button == null)
            {
                return;
            }

            var trigger = button.GetComponent<TooltipTrigger>();
            if (trigger == null)
            {
                trigger = button.gameObject.AddComponent<TooltipTrigger>();
            }

            trigger.SetStaticText(text);
        }

        private static string ToHex(Color color)
        {
            return ColorUtility.ToHtmlStringRGB(color);
        }
    }
}
