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
using System;
using FungusToast.Unity.Campaign;

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
        [SerializeField] private Image outcomeBackdrop;

        // Façade reference — set by GameManager so we don't need GameManager.Instance
        private GameUIManager gameUI;
        private System.Action onCampaignResume;
        private System.Action onExitToModeSelect;
        private bool requiresAdaptationBeforeContinue;
        private readonly List<Component> legacyResultsHeaderCandidates = new();

        // Post-victory campaign testing controls (runtime-built to avoid scene dependency).
        private GameObject postVictoryTestingRoot;
        private Button postVictoryTestingToggleButton;
        private TMP_Dropdown postVictoryMycovariantDropdown;
        private GameObject postVictoryMycovariantRow;
        private Button postVictoryFastForwardButton;
        private Button postVictorySkipToEndButton;
        private Button postVictoryForcedResultButton;
        private bool postVictoryTestingEnabled;
        private bool postVictorySkipToEnd;
        private int postVictoryFastForwardRounds;
        private ForcedGameResultMode postVictoryForcedResult = ForcedGameResultMode.Natural;
        private int? postVictoryForcedMycovariantId;

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
            UIStyleTokens.Button.SetButtonLabelColor(continueButton, UIStyleTokens.Button.TextDefault);
            UIStyleTokens.Button.SetButtonLabelColor(exitButton, UIStyleTokens.Button.TextDefault);
            UIStyleTokens.Button.SetButtonLabelColor(playAgainButton, UIStyleTokens.Button.TextDefault);

            EnsureButtonLayout(continueButton);
            EnsureButtonLayout(exitButton);
            EnsureButtonLayout(playAgainButton);
            EnsureActionButtonsShareContainer();
            EnsureButtonContainerLayout();
            EnsurePostVictoryTestingControls();
            UpdatePostVictoryTestingLabels();

            if (outcomeLabel != null)
            {
                EnsureOutcomePlacement();
                outcomeLabel.color = UIStyleTokens.Text.Primary;
                outcomeLabel.enableAutoSizing = true;
                outcomeLabel.fontSizeMax = 52f;
                outcomeLabel.fontSizeMin = 24f;
                outcomeLabel.textWrappingMode = TextWrappingModes.Normal;
                outcomeLabel.overflowMode = TextOverflowModes.Overflow;
                outcomeLabel.alignment = TextAlignmentOptions.Center;

                if (outcomeLabel.rectTransform != null)
                {
                    var labelRect = outcomeLabel.rectTransform;
                    labelRect.anchorMin = new Vector2(0.08f, 0.86f);
                    labelRect.anchorMax = new Vector2(0.92f, 0.98f);
                    labelRect.pivot = new Vector2(0.5f, 0.5f);
                    labelRect.anchoredPosition = Vector2.zero;
                    labelRect.offsetMin = Vector2.zero;
                    labelRect.offsetMax = Vector2.zero;
                }
            }

            UIStyleTokens.ApplyNonButtonTextPalette(gameObject, headingSizeThreshold: 30f);
            ApplyControlReadabilityOverrides();
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
            ShowResultsInternal(ranked, board, useCampaignTopSpacer: false);
            SetLegacyResultsHeaderVisibility(true);
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
            int completedLevelDisplay,
            bool adaptationPending)
        {
            ShowResultsInternal(ranked, board, useCampaignTopSpacer: true);
            SetLegacyResultsHeaderVisibility(!isCampaign);
            requiresAdaptationBeforeContinue = false;
            if (!isCampaign)
            {
                // fallback to base behavior
                if (continueButton != null) continueButton.gameObject.SetActive(false);
                if (exitButton != null) exitButton.gameObject.SetActive(true);
                if (outcomeLabel != null) outcomeLabel.text = "";
                UpdatePostVictoryTestingVisibility(false);
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
                        $"<color=#{ToHex(UIStyleTokens.State.Success)}><b>Level {completedLevelDisplay} cleared</b></color>\n" +
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

            ApplyControlReadabilityOverrides();

            bool canContinueToNextLevel = victory && !finalLevel && hasNextLevel;
            UpdatePostVictoryTestingVisibility(canContinueToNextLevel);
        }

        /* ─────────── Internal Row Builder ─────────── */
        private void ShowResultsInternal(List<Player> ranked, GameBoard board, bool useCampaignTopSpacer)
        {
            /* clear previous rows */
            foreach (Transform child in resultsContainer)
                Destroy(child.gameObject);

            if (useCampaignTopSpacer)
            {
                BuildCampaignTopSpacer();
            }

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

        public void ShowCampaignPendingVictorySnapshot(CampaignVictorySnapshot snapshot)
        {
            if (snapshot == null)
            {
                return;
            }

            ShowSnapshotRows(snapshot);
            SetLegacyResultsHeaderVisibility(false);
            requiresAdaptationBeforeContinue = true;

            if (outcomeLabel != null)
            {
                outcomeLabel.text =
                    $"<color=#{ToHex(UIStyleTokens.State.Success)}><b>Level {snapshot.clearedLevelDisplay} cleared</b></color>\n" +
                    $"<size=28><color=#{ToHex(UIStyleTokens.Text.Secondary)}>Select an Adaptation to continue the campaign.</color></size>";
            }

            if (continueButton != null)
            {
                continueButton.gameObject.SetActive(true);
                SetButtonLabel(continueButton, "Select Adaptation");
            }

            if (playAgainButton != null)
            {
                playAgainButton.gameObject.SetActive(false);
            }

            if (exitButton != null)
            {
                exitButton.gameObject.SetActive(true);
            }

            UpdatePostVictoryTestingVisibility(true);
        }

        private void ShowSnapshotRows(CampaignVictorySnapshot snapshot)
        {
            foreach (Transform child in resultsContainer)
            {
                Destroy(child.gameObject);
            }

            BuildCampaignTopSpacer();
            BuildResultsHeader();

            for (int i = 0; i < snapshot.rows.Count; i++)
            {
                var rowData = snapshot.rows[i];
                var row = Instantiate(playerResultRowPrefab, resultsContainer);

                Sprite icon = gameUI != null
                    ? gameUI.PlayerUIBinder.GetPlayerIcon(rowData.playerId)
                    : GameManager.Instance?.GameUI?.PlayerUIBinder?.GetPlayerIcon(rowData.playerId);

                row.Populate(
                    rowData.rank,
                    icon,
                    rowData.playerName,
                    rowData.livingCells,
                    rowData.deadCells,
                    rowData.toxinCells);
            }

            gameObject.SetActive(true);
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
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
                    ApplyPostVictoryTestingSettings(manager);
                    if (onCampaignResume != null)
                        onCampaignResume();
                    else
                        manager?.StartCampaignResume();
                }
                return;
            }

            // Mid-run victory continue path
            ApplyPostVictoryTestingSettings(GameManager.Instance);
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

            UpdatePostVictoryTestingVisibility(continueButton != null && continueButton.gameObject.activeSelf);
            ApplyControlReadabilityOverrides();

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
            headerLayout.preferredHeight = 36f;

            CreateHeaderCell(header.transform, string.Empty, 60f, TextAlignmentOptions.Center, false);
            CreateHeaderCell(header.transform, string.Empty, 60f, TextAlignmentOptions.Center, false);
            CreateHeaderCell(header.transform, "Player", 260f, TextAlignmentOptions.Left, true);
            CreateHeaderCell(header.transform, "Alive", 140f, TextAlignmentOptions.Right, false);
            CreateHeaderCell(header.transform, "Dead", 140f, TextAlignmentOptions.Right, false);
            CreateHeaderCell(header.transform, "Toxins", 140f, TextAlignmentOptions.Right, false);
        }

        private void BuildCampaignTopSpacer()
        {
            if (resultsContainer == null)
            {
                return;
            }

            var spacer = new GameObject("UI_CampaignOutcomeTopSpacer", typeof(RectTransform), typeof(LayoutElement));
            spacer.transform.SetParent(resultsContainer, false);

            var layout = spacer.GetComponent<LayoutElement>();
            layout.preferredHeight = 118f;
            layout.minHeight = 110f;
            layout.flexibleHeight = 0f;
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
            label.color = UIStyleTokens.Text.Primary;
            label.fontStyle = FontStyles.Bold;
            label.fontSize = 21f;
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
            layout.preferredWidth = 380f;
            layout.minWidth = 320f;
            layout.flexibleWidth = 0f;

            var rect = button.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.5f, rect.anchorMin.y);
                rect.anchorMax = new Vector2(0.5f, rect.anchorMax.y);
                rect.pivot = new Vector2(0.5f, rect.pivot.y);
            }
        }

        private void EnsurePostVictoryTestingControls()
        {
            if (playAgainButton == null)
            {
                return;
            }

            if (postVictoryTestingRoot != null)
            {
                return;
            }

            var parent = playAgainButton.transform.parent;
            if (parent == null)
            {
                return;
            }

            postVictoryTestingRoot = new GameObject("UI_PostVictoryTestingRoot", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement), typeof(Image));
            postVictoryTestingRoot.transform.SetParent(parent, false);
            postVictoryTestingRoot.transform.SetSiblingIndex(playAgainButton.transform.GetSiblingIndex() + 2);

            var rootLayout = postVictoryTestingRoot.GetComponent<VerticalLayoutGroup>();
            rootLayout.childAlignment = TextAnchor.UpperCenter;
            rootLayout.childControlWidth = true;
            rootLayout.childControlHeight = true;
            rootLayout.childForceExpandWidth = false;
            rootLayout.childForceExpandHeight = false;
            rootLayout.spacing = 6f;
            rootLayout.padding = new RectOffset(10, 10, 8, 8);

            var rootElement = postVictoryTestingRoot.GetComponent<LayoutElement>();
            rootElement.preferredHeight = 246f;
            rootElement.minHeight = 56f;
            rootElement.preferredWidth = 470f;
            rootElement.minWidth = 360f;

            var rootBackground = postVictoryTestingRoot.GetComponent<Image>();
            var rootColor = UIStyleTokens.Surface.PanelPrimary;
            rootColor.a = 0.92f;
            rootBackground.color = rootColor;
            rootBackground.raycastTarget = false;

            postVictoryTestingToggleButton = CreatePostVictorySettingButton(postVictoryTestingRoot.transform, "UI_PostVictoryTestingToggle", OnPostVictoryTestingToggled);
            postVictoryMycovariantRow = CreatePostVictoryMycovariantRow(postVictoryTestingRoot.transform);
            postVictoryFastForwardButton = CreatePostVictorySettingButton(postVictoryTestingRoot.transform, "UI_PostVictoryFastForward", OnPostVictoryFastForwardCycle);
            postVictorySkipToEndButton = CreatePostVictorySettingButton(postVictoryTestingRoot.transform, "UI_PostVictorySkipToEnd", OnPostVictorySkipToEndToggled);
            postVictoryForcedResultButton = CreatePostVictorySettingButton(postVictoryTestingRoot.transform, "UI_PostVictoryForcedResult", OnPostVictoryForcedResultCycle);

            EnsurePostVictoryControlOrder();
            UpdatePostVictoryTestingVisibility(false);
        }

        private Button CreatePostVictorySettingButton(Transform newParent, string name, UnityEngine.Events.UnityAction action)
        {
            var template = exitButton != null ? exitButton : continueButton;
            if (template == null)
            {
                return null;
            }

            var clone = Instantiate(template.gameObject, newParent);
            clone.name = name;

            var button = clone.GetComponent<Button>();
            if (button == null)
            {
                return null;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
            button.interactable = true;

            UIStyleTokens.Button.ApplyStyle(button);

            EnsureButtonLayout(button);
            var layout = button.GetComponent<LayoutElement>();
            if (layout != null)
            {
                layout.preferredHeight = 42f;
                layout.minHeight = 40f;
                layout.preferredWidth = 440f;
                layout.minWidth = 320f;
            }

            var label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                label.enableAutoSizing = true;
                label.fontSizeMax = 28f;
                label.fontSizeMin = 18f;
                label.alignment = TextAlignmentOptions.Center;
                label.color = UIStyleTokens.Button.TextDefault;
            }

            return button;
        }

        private GameObject CreatePostVictoryMycovariantRow(Transform parent)
        {
            if (parent == null)
            {
                return null;
            }

            TMP_Dropdown template = FindObjectOfType<TMP_Dropdown>(includeInactive: true);
            if (template == null)
            {
                Debug.LogWarning("UI_EndGamePanel: Unable to create Mycovariant dropdown because no TMP_Dropdown template was found in scene.");
                return null;
            }

            var row = new GameObject("UI_PostVictoryMycovariantRow", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            row.transform.SetParent(parent, false);

            var rowLayout = row.GetComponent<VerticalLayoutGroup>();
            rowLayout.childControlHeight = true;
            rowLayout.childControlWidth = true;
            rowLayout.childForceExpandHeight = false;
            rowLayout.childForceExpandWidth = false;
            rowLayout.spacing = 4f;
            rowLayout.padding = new RectOffset(4, 4, 2, 2);

            var rowElement = row.GetComponent<LayoutElement>();
            rowElement.preferredHeight = 86f;
            rowElement.minHeight = 80f;

            var labelObj = new GameObject("UI_PostVictoryMycovariantLabel", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            labelObj.transform.SetParent(row.transform, false);
            var label = labelObj.GetComponent<TextMeshProUGUI>();
            label.text = "Forced Mycovariant";
            label.color = UIStyleTokens.Text.Primary;
            label.fontSize = 20f;
            label.enableAutoSizing = true;
            label.fontSizeMax = 20f;
            label.fontSizeMin = 15f;
            label.alignment = TextAlignmentOptions.Left;

            var labelLayout = labelObj.GetComponent<LayoutElement>();
            labelLayout.preferredHeight = 28f;
            labelLayout.minHeight = 24f;

            var dropdownObj = Instantiate(template.gameObject, row.transform);
            dropdownObj.name = "UI_PostVictoryMycovariantDropdown";
            postVictoryMycovariantDropdown = dropdownObj.GetComponent<TMP_Dropdown>();
            if (postVictoryMycovariantDropdown != null)
            {
                postVictoryMycovariantDropdown.onValueChanged.RemoveAllListeners();
                postVictoryMycovariantDropdown.onValueChanged.AddListener(OnPostVictoryMycovariantDropdownChanged);
            }

            var dropdownLayout = dropdownObj.GetComponent<LayoutElement>();
            if (dropdownLayout == null)
            {
                dropdownLayout = dropdownObj.AddComponent<LayoutElement>();
            }

            dropdownLayout.preferredHeight = 44f;
            dropdownLayout.minHeight = 40f;
            dropdownLayout.preferredWidth = 440f;
            dropdownLayout.minWidth = 320f;

            PopulatePostVictoryMycovariantDropdown();
            return row;
        }

        private void PopulatePostVictoryMycovariantDropdown()
        {
            if (postVictoryMycovariantDropdown == null)
            {
                return;
            }

            var options = new List<string> { "None" };
            var all = FungusToast.Core.Mycovariants.MycovariantRepository.All;
            for (int i = 0; i < all.Count; i++)
            {
                options.Add($"{all[i].Name} (ID: {all[i].Id})");
            }

            postVictoryMycovariantDropdown.ClearOptions();
            postVictoryMycovariantDropdown.AddOptions(options);
            postVictoryMycovariantDropdown.value = 0;
            postVictoryMycovariantDropdown.RefreshShownValue();

            if (postVictoryMycovariantDropdown.captionText != null)
            {
                postVictoryMycovariantDropdown.captionText.color = UIStyleTokens.Button.TextDefault;
            }

            if (postVictoryMycovariantDropdown.itemText != null)
            {
                postVictoryMycovariantDropdown.itemText.color = UIStyleTokens.Button.TextDefault;
            }

            ApplyDropdownReadability(postVictoryMycovariantDropdown);
        }

        private void OnPostVictoryMycovariantDropdownChanged(int index)
        {
            if (index <= 0)
            {
                postVictoryForcedMycovariantId = null;
                return;
            }

            var all = FungusToast.Core.Mycovariants.MycovariantRepository.All;
            int mapped = index - 1;
            if (mapped >= 0 && mapped < all.Count)
            {
                postVictoryForcedMycovariantId = all[mapped].Id;
            }
            else
            {
                postVictoryForcedMycovariantId = null;
            }
        }

        private void UpdatePostVictoryTestingVisibility(bool visible)
        {
            EnsurePostVictoryTestingControls();
            if (postVictoryTestingRoot == null)
            {
                return;
            }

            if (!visible)
            {
                postVictoryTestingRoot.SetActive(false);
                EnsurePostVictoryControlOrder();
                return;
            }

            SyncPostVictoryTestingDefaultsFromGameManager();
            postVictoryTestingRoot.SetActive(true);
            EnsurePostVictoryControlOrder();

            if (postVictoryFastForwardButton != null)
                postVictoryFastForwardButton.gameObject.SetActive(postVictoryTestingEnabled);

            if (postVictoryMycovariantRow != null)
                postVictoryMycovariantRow.SetActive(postVictoryTestingEnabled);

            if (postVictoryMycovariantDropdown != null)
                postVictoryMycovariantDropdown.interactable = postVictoryTestingEnabled;

            if (postVictorySkipToEndButton != null)
                postVictorySkipToEndButton.gameObject.SetActive(postVictoryTestingEnabled);

            if (postVictoryForcedResultButton != null)
                postVictoryForcedResultButton.gameObject.SetActive(postVictoryTestingEnabled && postVictorySkipToEnd);

            UpdatePostVictoryTestingLayoutHeight();
            UpdatePostVictoryTestingLabels();
            ApplyControlReadabilityOverrides();
        }

        private void EnsurePostVictoryControlOrder()
        {
            if (playAgainButton == null || postVictoryTestingRoot == null)
            {
                return;
            }

            var parent = playAgainButton.transform.parent;
            if (parent == null)
            {
                return;
            }

            if (continueButton != null && continueButton.transform.parent != parent)
            {
                continueButton.transform.SetParent(parent, false);
            }

            if (exitButton != null && exitButton.transform.parent != parent)
            {
                exitButton.transform.SetParent(parent, false);
            }

            int nextIndex = playAgainButton.transform.GetSiblingIndex() + 1;
            postVictoryTestingRoot.transform.SetSiblingIndex(nextIndex);
            nextIndex++;

            if (continueButton != null)
            {
                continueButton.transform.SetSiblingIndex(nextIndex);
                nextIndex++;
            }

            if (exitButton != null)
            {
                exitButton.transform.SetSiblingIndex(nextIndex);
            }
        }

        private void SyncPostVictoryTestingDefaultsFromGameManager()
        {
            var manager = GameManager.Instance;
            if (manager == null)
            {
                return;
            }

            postVictoryTestingEnabled = manager.IsTestingModeEnabled;
            postVictoryFastForwardRounds = Mathf.Max(0, manager.fastForwardRounds);
            postVictorySkipToEnd = manager.testingSkipToEndgameAfterFastForward;
            postVictoryForcedResult = manager.TestingForcedGameResult;
            postVictoryForcedMycovariantId = manager.TestingMycovariantId;

            if (!postVictorySkipToEnd && postVictoryForcedResult != ForcedGameResultMode.Natural)
            {
                postVictoryForcedResult = ForcedGameResultMode.Natural;
            }
        }

        private void UpdatePostVictoryTestingLabels()
        {
            SetButtonLabel(postVictoryTestingToggleButton, $"Development Testing: {(postVictoryTestingEnabled ? "On" : "Off")}");
            SetButtonLabel(postVictoryFastForwardButton, $"Fast Forward Rounds: {postVictoryFastForwardRounds}");
            SetButtonLabel(postVictorySkipToEndButton, $"Skip To End Game: {(postVictorySkipToEnd ? "On" : "Off")}");
            SetButtonLabel(postVictoryForcedResultButton, $"Forced Result: {FormatForcedResult(postVictoryForcedResult)}");

            if (postVictoryMycovariantDropdown != null)
            {
                int target = 0;
                if (postVictoryForcedMycovariantId.HasValue)
                {
                    var all = FungusToast.Core.Mycovariants.MycovariantRepository.All;
                    int found = all.FindIndex(m => m.Id == postVictoryForcedMycovariantId.Value);
                    if (found >= 0)
                    {
                        target = found + 1;
                    }
                }

                postVictoryMycovariantDropdown.SetValueWithoutNotify(target);
                postVictoryMycovariantDropdown.RefreshShownValue();
            }

            ApplyControlReadabilityOverrides();
        }

        private void OnPostVictoryTestingToggled()
        {
            postVictoryTestingEnabled = !postVictoryTestingEnabled;

            if (!postVictoryTestingEnabled)
            {
                postVictorySkipToEnd = false;
                postVictoryForcedResult = ForcedGameResultMode.Natural;
                postVictoryForcedMycovariantId = null;
            }

            if (postVictoryFastForwardButton != null)
                postVictoryFastForwardButton.gameObject.SetActive(postVictoryTestingEnabled);

            if (postVictoryMycovariantRow != null)
                postVictoryMycovariantRow.SetActive(postVictoryTestingEnabled);

            if (postVictoryMycovariantDropdown != null)
                postVictoryMycovariantDropdown.interactable = postVictoryTestingEnabled;

            if (postVictorySkipToEndButton != null)
                postVictorySkipToEndButton.gameObject.SetActive(postVictoryTestingEnabled);

            if (postVictoryForcedResultButton != null)
                postVictoryForcedResultButton.gameObject.SetActive(postVictoryTestingEnabled && postVictorySkipToEnd);

            UpdatePostVictoryTestingLayoutHeight();
            UpdatePostVictoryTestingLabels();
            ApplyControlReadabilityOverrides();
        }

        private void OnPostVictoryFastForwardCycle()
        {
            postVictoryFastForwardRounds = postVictoryFastForwardRounds switch
            {
                0 => 5,
                5 => 10,
                10 => 25,
                25 => 50,
                50 => 100,
                _ => 0
            };

            UpdatePostVictoryTestingLabels();
        }

        private void OnPostVictorySkipToEndToggled()
        {
            postVictorySkipToEnd = !postVictorySkipToEnd;
            if (!postVictorySkipToEnd)
            {
                postVictoryForcedResult = ForcedGameResultMode.Natural;
            }

            if (postVictoryForcedResultButton != null)
                postVictoryForcedResultButton.gameObject.SetActive(postVictoryTestingEnabled && postVictorySkipToEnd);

            UpdatePostVictoryTestingLayoutHeight();
            UpdatePostVictoryTestingLabels();
        }

        private void UpdatePostVictoryTestingLayoutHeight()
        {
            if (postVictoryTestingRoot == null)
            {
                return;
            }

            var rootElement = postVictoryTestingRoot.GetComponent<LayoutElement>();
            if (rootElement == null)
            {
                return;
            }

            float height = 16f; // top/bottom padding budget
            if (postVictoryTestingToggleButton != null && postVictoryTestingToggleButton.gameObject.activeSelf) height += 42f + 6f;
            if (postVictoryMycovariantRow != null && postVictoryMycovariantRow.activeSelf) height += 86f + 6f;
            if (postVictoryFastForwardButton != null && postVictoryFastForwardButton.gameObject.activeSelf) height += 42f + 6f;
            if (postVictorySkipToEndButton != null && postVictorySkipToEndButton.gameObject.activeSelf) height += 42f + 6f;
            if (postVictoryForcedResultButton != null && postVictoryForcedResultButton.gameObject.activeSelf) height += 42f + 6f;

            height = Mathf.Max(56f, height);
            rootElement.preferredHeight = height;
            rootElement.minHeight = height;
        }

        private void ApplyControlReadabilityOverrides()
        {
            UIStyleTokens.Button.SetButtonLabelColor(continueButton, UIStyleTokens.Button.TextDefault);
            UIStyleTokens.Button.SetButtonLabelColor(exitButton, UIStyleTokens.Button.TextDefault);
            UIStyleTokens.Button.SetButtonLabelColor(playAgainButton, UIStyleTokens.Button.TextDefault);

            UIStyleTokens.Button.SetButtonLabelColor(postVictoryTestingToggleButton, UIStyleTokens.Button.TextDefault);
            UIStyleTokens.Button.SetButtonLabelColor(postVictoryFastForwardButton, UIStyleTokens.Button.TextDefault);
            UIStyleTokens.Button.SetButtonLabelColor(postVictorySkipToEndButton, UIStyleTokens.Button.TextDefault);
            UIStyleTokens.Button.SetButtonLabelColor(postVictoryForcedResultButton, UIStyleTokens.Button.TextDefault);

            ApplyDropdownReadability(postVictoryMycovariantDropdown);
        }

        private static void ApplyDropdownReadability(TMP_Dropdown dropdown)
        {
            if (dropdown == null)
            {
                return;
            }

            if (dropdown.captionText != null)
            {
                dropdown.captionText.color = UIStyleTokens.Button.TextDefault;
            }

            if (dropdown.itemText != null)
            {
                dropdown.itemText.color = UIStyleTokens.Button.TextDefault;
            }

            var labels = dropdown.GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = 0; i < labels.Length; i++)
            {
                var label = labels[i];
                if (label == null)
                {
                    continue;
                }

                if (label.name.IndexOf("Placeholder", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    label.color = UIStyleTokens.Text.Disabled;
                }
                else
                {
                    label.color = UIStyleTokens.Button.TextDefault;
                }
            }
        }

        private void OnPostVictoryForcedResultCycle()
        {
            postVictoryForcedResult = postVictoryForcedResult switch
            {
                ForcedGameResultMode.Natural => ForcedGameResultMode.ForcedWin,
                ForcedGameResultMode.ForcedWin => ForcedGameResultMode.ForcedLoss,
                _ => ForcedGameResultMode.Natural
            };

            UpdatePostVictoryTestingLabels();
        }

        private void ApplyPostVictoryTestingSettings(GameManager manager)
        {
            if (manager == null)
            {
                return;
            }

            if (!postVictoryTestingEnabled)
            {
                return;
            }

            var forcedResult = postVictorySkipToEnd ? postVictoryForcedResult : ForcedGameResultMode.Natural;
            manager.EnableTestingMode(
                postVictoryForcedMycovariantId,
                postVictoryFastForwardRounds,
                postVictorySkipToEnd,
                forcedResult);
        }

        private static string FormatForcedResult(ForcedGameResultMode mode)
        {
            return mode switch
            {
                ForcedGameResultMode.ForcedWin => "Forced Win",
                ForcedGameResultMode.ForcedLoss => "Forced Loss",
                _ => "Natural"
            };
        }

        private void EnsureActionButtonsShareContainer()
        {
            if (playAgainButton == null)
            {
                return;
            }

            var primaryParent = playAgainButton.transform.parent;
            if (primaryParent == null)
            {
                return;
            }

            if (continueButton != null && continueButton.transform.parent != primaryParent)
            {
                continueButton.transform.SetParent(primaryParent, false);
            }

            if (exitButton != null && exitButton.transform.parent != primaryParent)
            {
                exitButton.transform.SetParent(primaryParent, false);
            }

            int nextIndex = playAgainButton.transform.GetSiblingIndex() + 1;
            if (continueButton != null)
            {
                continueButton.transform.SetSiblingIndex(nextIndex);
                nextIndex++;
            }

            if (exitButton != null)
            {
                exitButton.transform.SetSiblingIndex(nextIndex);
            }
        }

        private void EnsureButtonContainerLayout()
        {
            var parent = playAgainButton != null ? playAgainButton.transform.parent : null;
            if (parent == null)
            {
                return;
            }

            var vlg = parent.GetComponent<VerticalLayoutGroup>();
            if (vlg != null)
            {
                vlg.childControlWidth = true;
                vlg.childForceExpandWidth = false;
                vlg.childForceExpandHeight = false;
                vlg.childAlignment = TextAnchor.UpperCenter;
            }
        }

        private void EnsureOutcomePlacement()
        {
            if (outcomeLabel == null || resultsCardBackground == null)
            {
                return;
            }

            var desiredParent = resultsCardBackground.transform;
            if (outcomeLabel.transform.parent != desiredParent)
            {
                outcomeLabel.transform.SetParent(desiredParent, false);
                outcomeLabel.transform.SetAsLastSibling();
            }

            if (outcomeBackdrop == null)
            {
                var existing = desiredParent.Find("UI_EndGameOutcomeBackdrop");
                if (existing != null)
                {
                    outcomeBackdrop = existing.GetComponent<Image>();
                }
            }

            if (outcomeBackdrop == null)
            {
                var backdropObject = new GameObject("UI_EndGameOutcomeBackdrop", typeof(RectTransform), typeof(Image));
                backdropObject.transform.SetParent(desiredParent, false);
                backdropObject.transform.SetSiblingIndex(outcomeLabel.transform.GetSiblingIndex());
                outcomeBackdrop = backdropObject.GetComponent<Image>();
            }

            EnsureIgnoreParentLayout(outcomeLabel.gameObject);

            if (outcomeBackdrop != null)
            {
                EnsureIgnoreParentLayout(outcomeBackdrop.gameObject);

                var backdropColor = UIStyleTokens.Surface.PanelSecondary;
                backdropColor.a = 0.92f;
                outcomeBackdrop.color = backdropColor;

                var backdropRect = outcomeBackdrop.rectTransform;
                backdropRect.anchorMin = new Vector2(0.04f, 0.84f);
                backdropRect.anchorMax = new Vector2(0.96f, 0.995f);
                backdropRect.pivot = new Vector2(0.5f, 0.5f);
                backdropRect.anchoredPosition = Vector2.zero;
                backdropRect.offsetMin = Vector2.zero;
                backdropRect.offsetMax = Vector2.zero;

                // Keep backdrop behind label while staying above base card background.
                if (outcomeLabel != null)
                {
                    outcomeBackdrop.transform.SetSiblingIndex(Mathf.Max(0, outcomeLabel.transform.GetSiblingIndex() - 1));
                    outcomeLabel.transform.SetAsLastSibling();
                }
            }
        }

        private static void EnsureIgnoreParentLayout(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return;
            }

            var layoutElement = gameObject.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = gameObject.AddComponent<LayoutElement>();
            }

            layoutElement.ignoreLayout = true;
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

        private void SetLegacyResultsHeaderVisibility(bool visible)
        {
            CacheLegacyResultsHeaderCandidates();
            for (int i = 0; i < legacyResultsHeaderCandidates.Count; i++)
            {
                var candidate = legacyResultsHeaderCandidates[i];
                if (candidate == null)
                {
                    continue;
                }

                candidate.gameObject.SetActive(visible);
            }
        }

        private void CacheLegacyResultsHeaderCandidates()
        {
            if (legacyResultsHeaderCandidates.Count > 0)
            {
                return;
            }

            var tmpLabels = GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = 0; i < tmpLabels.Length; i++)
            {
                var label = tmpLabels[i];
                if (label == null || label == outcomeLabel)
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(label.text) &&
                    label.text.IndexOf("Game Results", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    legacyResultsHeaderCandidates.Add(label);
                }
            }

            var tmpLegacyLabels = GetComponentsInChildren<Text>(true);
            for (int i = 0; i < tmpLegacyLabels.Length; i++)
            {
                var label = tmpLegacyLabels[i];
                if (label == null)
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(label.text) &&
                    label.text.IndexOf("Game Results", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    legacyResultsHeaderCandidates.Add(label);
                }
            }
        }
    }
}
