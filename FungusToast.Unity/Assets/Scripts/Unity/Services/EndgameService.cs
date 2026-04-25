using System;
using System.Collections.Generic;
using System.Linq;
using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Growth;
using FungusToast.Core.Mutations;
using FungusToast.Core.Players;
using FungusToast.Unity.Campaign;
using FungusToast.Unity.Endgame;
using FungusToast.Unity.Input;
using FungusToast.Unity.Grid;
using FungusToast.Unity.Phases;
using FungusToast.Unity.UI;
using FungusToast.Unity.UI.GameStart;
using FungusToast.Unity.UI.MycovariantDraft;
using FungusToast.Unity.UI.Tooltips;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FungusToast.Unity
{
    /// <summary>
    /// Encapsulates endgame detection, countdown tracking, and final results display.
    /// Extracted from GameManager to reduce its responsibilities.
    /// </summary>
    public class EndgameService
    {
        private const int ImmediateFinalRoundCountdown = 1;

        private readonly GameUIManager ui;
        private readonly Func<GameBoard> getBoard;
        private readonly Func<Player> getHumanPlayer;
        private readonly Func<GameMode> getGameMode;
        private readonly Func<CampaignController> getCampaignController;
        private readonly Func<CampaignProgression> getCampaignProgression;
        private readonly Func<EndgamePlayerStatisticsSnapshot> getEndgamePlayerStatistics;
        private readonly Func<Dictionary<(int playerId, int mutationId), List<int>>> getFirstUpgradeRounds;
        private readonly Func<bool> getTestingModeEnabled;
        private readonly Func<ForcedGameResultMode> getForcedGameResultMode;
        private readonly Func<bool> getForceMoldinessRewards;

        private bool isCountdownActive;
        private int roundsRemainingUntilGameEnd;

        public bool GameEnded { get; private set; }

        public EndgameService(
            GameUIManager ui,
            Func<GameBoard> getBoard,
            Func<Player> getHumanPlayer,
            Func<GameMode> getGameMode,
            Func<CampaignController> getCampaignController,
            Func<CampaignProgression> getCampaignProgression,
            Func<EndgamePlayerStatisticsSnapshot> getEndgamePlayerStatistics,
            Func<Dictionary<(int playerId, int mutationId), List<int>>> getFirstUpgradeRounds,
            Func<bool> getTestingModeEnabled,
            Func<ForcedGameResultMode> getForcedGameResultMode,
            Func<bool> getForceMoldinessRewards)
        {
            this.ui = ui;
            this.getBoard = getBoard;
            this.getHumanPlayer = getHumanPlayer;
            this.getGameMode = getGameMode;
            this.getCampaignController = getCampaignController;
            this.getCampaignProgression = getCampaignProgression;
            this.getEndgamePlayerStatistics = getEndgamePlayerStatistics;
            this.getFirstUpgradeRounds = getFirstUpgradeRounds;
            this.getTestingModeEnabled = getTestingModeEnabled;
            this.getForcedGameResultMode = getForcedGameResultMode;
            this.getForceMoldinessRewards = getForceMoldinessRewards;
        }

        /// <summary>
        /// Resets all endgame state for a new game.
        /// </summary>
        public void Reset()
        {
            GameEnded = false;
            isCountdownActive = false;
            roundsRemainingUntilGameEnd = 0;
            ui.RightSidebar?.SetEndgameCountdownText(null);
        }

        /// <summary>
        /// When testing fast-forward lands on a board that already satisfies the endgame
        /// occupancy threshold, arm the countdown so the next interactive round is the last one.
        /// </summary>
        public bool TryArmImmediateFinalRoundCountdown()
        {
            if (GameEnded)
            {
                return false;
            }

            var board = getBoard();
            if (board == null || !board.ShouldTriggerEndgame())
            {
                return false;
            }

            bool alreadyArmedForFinalRound = isCountdownActive
                && roundsRemainingUntilGameEnd == ImmediateFinalRoundCountdown;

            isCountdownActive = true;
            roundsRemainingUntilGameEnd = ImmediateFinalRoundCountdown;

            if (!alreadyArmedForFinalRound)
            {
                UpdateCountdownUI();
            }

            return true;
        }

        /// <summary>
        /// Checks whether the endgame occupancy threshold has been met and manages
        /// the countdown. Call this once at the end of each round.
        /// </summary>
        public void CheckForEndgameCondition()
        {
            var board = getBoard();
            if (!isCountdownActive && board.ShouldTriggerEndgame())
            {
                isCountdownActive = true;
                roundsRemainingUntilGameEnd = GameBalance.TurnsAfterEndGameTileOccupancyThresholdMet;
                UpdateCountdownUI();
            }
            else if (isCountdownActive)
            {
                roundsRemainingUntilGameEnd--;
                if (roundsRemainingUntilGameEnd <= 0)
                {
                    EndGame();
                }
                else
                {
                    UpdateCountdownUI();
                }
            }
        }

        private void UpdateCountdownUI()
        {
            if (!isCountdownActive)
            {
                ui.RightSidebar?.SetEndgameCountdownText(null);
                return;
            }
            if (roundsRemainingUntilGameEnd == 1)
            {
                string dangerHex = ToHex(UIStyleTokens.State.Danger);
                ui.RightSidebar?.SetEndgameCountdownText($"<b><color=#{dangerHex}>Final round!</color></b>");
                ui.GameLogRouter?.OnEndgameTriggered(1);
            }
            else
            {
                string warningHex = ToHex(UIStyleTokens.State.Warning);
                ui.RightSidebar?.SetEndgameCountdownText($"<b><color=#{warningHex}>Endgame in {roundsRemainingUntilGameEnd} rounds</color></b>");
                ui.GameLogRouter?.OnEndgameTriggered(roundsRemainingUntilGameEnd);
            }
        }

        /// <summary>
        /// Forces the game to end immediately. Safe to call multiple times.
        /// </summary>
        public void EndGame()
        {
            if (GameEnded) return;
            GameEnded = true;

            var board = getBoard();
            var humanPlayer = getHumanPlayer();
            var endgamePlayerStatistics = getEndgamePlayerStatistics();
            var campaignController = getCampaignController();
            var campaignProgression = getCampaignProgression();

            ui.GameLogManager?.OnLogSegmentStart("None");
            ui.MutationUIManager.SetSpendPointsButtonInteractable(false);
            ui.PhaseBanner?.HideImmediate();

            var ranked = board.Players
                .OrderByDescending(p => board.GetAllCellsOwnedBy(p.PlayerId).Count(c => c.IsAlive))
                .ThenByDescending(p => board.GetAllCellsOwnedBy(p.PlayerId).Count(c => !c.IsAlive))
                .ToList();

            var winner = ranked.FirstOrDefault();
            if (winner != null)
            {
                ui.GameLogRouter?.OnGameEnd(winner.PlayerName);
            }

            bool isCampaign = getGameMode() == GameMode.Campaign
                              && campaignController != null
                              && campaignController.HasActiveRun;
            int lostLevelDisplay = isCampaign ? (campaignController.State.levelIndex + 1) : 0;
            int completedLevelDisplay = isCampaign ? (campaignController.State.levelIndex + 1) : 0;
            bool naturalHumanWon = isCampaign && humanPlayer != null && winner != null
                            && winner.PlayerId == humanPlayer.PlayerId;
            bool humanWon = ApplyForcedCampaignResultOverride(naturalHumanWon, isCampaign);
            bool finalLevelPreAdvance = isCampaign
                                        && campaignController.State.levelIndex >= (campaignProgression.MaxLevels - 1);

            if (isCampaign && humanWon && !finalLevelPreAdvance)
            {
                var summaries = BoardUtilities.GetPlayerBoardSummaries(ranked, board);
                var snapshot = new CampaignVictorySnapshot
                {
                    clearedLevelDisplay = completedLevelDisplay
                };

                for (int i = 0; i < ranked.Count; i++)
                {
                    var player = ranked[i];
                    var summary = summaries[player.PlayerId];
                    snapshot.rows.Add(new CampaignVictoryPlayerRow
                    {
                        rank = i + 1,
                        playerId = player.PlayerId,
                        playerName = player.PlayerName,
                        livingCells = summary.LivingCells,
                        resistantCells = summary.ResistantCells,
                        deadCells = summary.DeadCells,
                        toxinCells = summary.ToxinCells
                    });
                }

                campaignController.SetPendingVictorySnapshot(snapshot);
            }

            if (isCampaign)
            {
                campaignController.OnGameFinished(humanWon);
            }

            bool hasNextLevel = isCampaign && humanWon && !finalLevelPreAdvance
                                && campaignController.State.levelIndex < campaignProgression.MaxLevels;

            ui.MutationUIManager.gameObject.SetActive(false);
            ui.RightSidebar.gameObject.SetActive(true);
            ui.LeftSidebar.gameObject.SetActive(false);
            ui.EndGamePanel.gameObject.SetActive(true);

            if (isCampaign)
            {
                bool shouldForceMoldinessRewardForTesting = humanWon
                    && getTestingModeEnabled()
                    && getForcedGameResultMode() == ForcedGameResultMode.ForcedWin
                    && getForceMoldinessRewards();

                if (shouldForceMoldinessRewardForTesting)
                {
                    campaignController.TryQueueForcedMoldinessRewardForTesting(new System.Random(campaignController.State?.seed ?? 0), 3);
                }

                var carryoverOptions = !humanWon && campaignController.IsAwaitingDefeatCarryoverSelection
                    ? campaignController.GetPendingDefeatCarryoverOptions()
                    : Array.Empty<FungusToast.Core.Campaign.AdaptationDefinition>();
                int carryoverCapacity = !humanWon && campaignController.IsAwaitingDefeatCarryoverSelection
                    ? Mathf.Max(0, campaignController.State?.moldiness?.failedRunAdaptationCarryoverCount ?? 0)
                    : 0;
                ui.EndGamePanel.ShowResultsWithOutcome(
                    ranked, board, endgamePlayerStatistics, true, humanWon,
                    finalLevelPreAdvance && humanWon, hasNextLevel,
                    humanWon ? completedLevelDisplay : lostLevelDisplay,
                    completedLevelDisplay,
                    campaignController.State.pendingAdaptationSelection,
                    campaignController.PendingVictorySnapshot,
                    carryoverOptions,
                    carryoverCapacity);
            }
            else
            {
                ui.EndGamePanel.ShowResults(ranked, board, endgamePlayerStatistics);
            }

            var firstUpgradeRounds = getFirstUpgradeRounds();
            foreach (var ((pid, mid), rounds) in firstUpgradeRounds)
            {
                double avg = rounds.Average();
                int min = rounds.Min();
                int max = rounds.Max();
                Console.WriteLine($"Player {pid} | Mutation {mid} | Avg First Acquired: {avg:F1} | Min: {min} | Max: {max}");
            }
        }

        private static string ToHex(Color color)
        {
            return ColorUtility.ToHtmlStringRGB(color);
        }

        private bool ApplyForcedCampaignResultOverride(bool naturalHumanWon, bool isCampaign)
        {
            if (!isCampaign || !getTestingModeEnabled())
            {
                return naturalHumanWon;
            }

            return getForcedGameResultMode() switch
            {
                ForcedGameResultMode.ForcedWin => true,
                ForcedGameResultMode.ForcedLoss => false,
                _ => naturalHumanWon
            };
        }
    }

    public sealed class PauseMenuService
    {
        private readonly GameObject hostObject;
        private readonly GameUIManager gameUIManager;
        private readonly Func<bool> canOpenPauseMenu;
        private readonly Func<bool> tryCancelActiveSelection;
        private readonly Action onBeforeOpen;
        private readonly Action onReturnToMainMenuRequested;
        private readonly Action onExitRequested;
        private readonly Action onNextTrackRequested;
        private readonly Func<string> getCurrentTrackName;
        private readonly Func<string> getNextTrackName;

        private UI_PauseMenuPanel panel;

        public PauseMenuService(
            GameObject hostObject,
            GameUIManager gameUIManager,
            Func<bool> canOpenPauseMenu,
            Func<bool> tryCancelActiveSelection,
            Action onBeforeOpen,
            Action onReturnToMainMenuRequested,
            Action onExitRequested,
            Action onNextTrackRequested,
            Func<string> getCurrentTrackName,
            Func<string> getNextTrackName)
        {
            this.hostObject = hostObject;
            this.gameUIManager = gameUIManager;
            this.canOpenPauseMenu = canOpenPauseMenu;
            this.tryCancelActiveSelection = tryCancelActiveSelection;
            this.onBeforeOpen = onBeforeOpen;
            this.onReturnToMainMenuRequested = onReturnToMainMenuRequested;
            this.onExitRequested = onExitRequested;
            this.onNextTrackRequested = onNextTrackRequested;
            this.getCurrentTrackName = getCurrentTrackName;
            this.getNextTrackName = getNextTrackName;
        }

        public bool IsOpen { get; private set; }

        public void Initialize()
        {
            panel = hostObject.GetComponent<UI_PauseMenuPanel>();
            if (panel == null)
            {
                panel = hostObject.AddComponent<UI_PauseMenuPanel>();
            }

            panel.SetDependencies(
                gameUIManager,
                Open,
                ResumeGameplay,
                onReturnToMainMenuRequested,
                onExitRequested,
                onNextTrackRequested,
                getCurrentTrackName,
                getNextTrackName);

            gameUIManager?.RegisterPauseMenuPanel(panel);
            panel.SetGameplayVisibility(false);
        }

        public void SetGameplayVisibility(bool isVisible)
        {
            panel?.SetGameplayVisibility(isVisible);
        }

        public void HandleInput()
        {
            if (!Application.isPlaying || !UnityInputAdapter.WasEscapePressedThisFrame())
            {
                return;
            }

            if (IsOpen)
            {
                if (panel != null && panel.IsConfirming)
                {
                    panel.CancelPendingAction();
                    return;
                }

                ResumeGameplay();
                return;
            }

            if (tryCancelActiveSelection())
            {
                return;
            }

            if (canOpenPauseMenu())
            {
                Open();
            }
        }

        public void Open()
        {
            if (!canOpenPauseMenu())
            {
                return;
            }

            tryCancelActiveSelection();

            onBeforeOpen?.Invoke();
            IsOpen = true;
            Time.timeScale = 0f;
            panel?.Show();
        }

        public void ResumeGameplay()
        {
            ForceClose();
        }

        public void ForceClose()
        {
            IsOpen = false;
            Time.timeScale = 1f;
            panel?.Hide();
        }
    }

    public sealed class SelectionPromptService
    {
        private readonly GameObject selectionPromptPanel;
        private readonly TextMeshProUGUI selectionPromptText;

        private Button selectionPromptCancelButton;
        private TextMeshProUGUI selectionPromptCancelButtonText;

        public SelectionPromptService(
            GameObject selectionPromptPanel,
            TextMeshProUGUI selectionPromptText,
            Button selectionPromptCancelButton,
            TextMeshProUGUI selectionPromptCancelButtonText)
        {
            this.selectionPromptPanel = selectionPromptPanel;
            this.selectionPromptText = selectionPromptText;
            this.selectionPromptCancelButton = selectionPromptCancelButton;
            this.selectionPromptCancelButtonText = selectionPromptCancelButtonText;
        }

        public void Show(string message, bool showCancelButton = false, string cancelButtonLabel = "Cancel", Action onCancel = null)
        {
            EnsureSelectionPromptCancelButton();
            if (selectionPromptPanel == null)
            {
                return;
            }

            selectionPromptPanel.SetActive(true);
            if (selectionPromptText != null)
            {
                selectionPromptText.text = message;
            }

            ConfigureSelectionPromptCancelButton(showCancelButton, cancelButtonLabel, onCancel);
            ConfigureSelectionPromptTextLayout(showCancelButton);
            ConfigureSelectionPromptRaycasts(showCancelButton);
        }

        public void Hide()
        {
            ConfigureSelectionPromptCancelButton(false, "Cancel", null);
            ConfigureSelectionPromptTextLayout(false);
            ConfigureSelectionPromptRaycasts(false);
            selectionPromptPanel?.SetActive(false);
        }

        private void EnsureSelectionPromptCancelButton()
        {
            if (selectionPromptPanel == null)
            {
                return;
            }

            EnsureSelectionPromptBackgroundGraphic();

            if (selectionPromptCancelButton == null)
            {
                selectionPromptCancelButton = selectionPromptPanel.GetComponentInChildren<Button>(true);
            }

            if (selectionPromptCancelButton == null)
            {
                var buttonObject = new GameObject("UI_SelectionPromptCancelButton", typeof(RectTransform), typeof(Image), typeof(Button));
                buttonObject.layer = selectionPromptPanel.layer;
                buttonObject.transform.SetParent(selectionPromptPanel.transform, false);

                var rectTransform = buttonObject.GetComponent<RectTransform>();
                rectTransform.anchorMin = new Vector2(1f, 0.5f);
                rectTransform.anchorMax = new Vector2(1f, 0.5f);
                rectTransform.pivot = new Vector2(1f, 0.5f);
                rectTransform.anchoredPosition = new Vector2(-18f, 0f);
                rectTransform.sizeDelta = new Vector2(170f, 38f);

                var image = buttonObject.GetComponent<Image>();
                image.color = new Color(0.16f, 0.12f, 0.1f, 0.92f);

                selectionPromptCancelButton = buttonObject.GetComponent<Button>();
                var colors = selectionPromptCancelButton.colors;
                colors.normalColor = image.color;
                colors.highlightedColor = new Color(0.26f, 0.2f, 0.16f, 1f);
                colors.pressedColor = new Color(0.12f, 0.09f, 0.07f, 1f);
                colors.selectedColor = colors.highlightedColor;
                colors.disabledColor = new Color(0.16f, 0.12f, 0.1f, 0.45f);
                selectionPromptCancelButton.colors = colors;

                var labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                labelObject.layer = selectionPromptPanel.layer;
                labelObject.transform.SetParent(buttonObject.transform, false);

                var labelRect = labelObject.GetComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;

                selectionPromptCancelButtonText = labelObject.GetComponent<TextMeshProUGUI>();
                selectionPromptCancelButtonText.fontSize = 22f;
                selectionPromptCancelButtonText.fontStyle = FontStyles.Bold;
                selectionPromptCancelButtonText.alignment = TextAlignmentOptions.Center;
                selectionPromptCancelButtonText.color = new Color(0.97f, 0.94f, 0.86f, 1f);
            }

            if (selectionPromptCancelButtonText == null && selectionPromptCancelButton != null)
            {
                selectionPromptCancelButtonText = selectionPromptCancelButton.GetComponentInChildren<TextMeshProUGUI>(true);
            }

            if (selectionPromptText != null)
            {
                selectionPromptText.alignment = TextAlignmentOptions.Center;
            }

            ConfigureSelectionPromptRaycasts(selectionPromptCancelButton != null
                && selectionPromptCancelButton.gameObject.activeSelf
                && selectionPromptCancelButton.interactable);
        }

        private void EnsureSelectionPromptBackgroundGraphic()
        {
            if (selectionPromptPanel == null)
            {
                return;
            }

            if (selectionPromptPanel.GetComponent<Graphic>() != null)
            {
                return;
            }

            var blocker = selectionPromptPanel.AddComponent<Image>();
            blocker.color = new Color(1f, 1f, 1f, 0f);
        }

        private void ConfigureSelectionPromptCancelButton(bool visible, string cancelButtonLabel, Action onCancel)
        {
            if (selectionPromptCancelButton == null)
            {
                return;
            }

            selectionPromptCancelButton.onClick.RemoveAllListeners();
            selectionPromptCancelButton.gameObject.SetActive(visible);
            selectionPromptCancelButton.interactable = visible;

            if (selectionPromptCancelButtonText != null)
            {
                selectionPromptCancelButtonText.text = cancelButtonLabel;
            }

            if (visible && onCancel != null)
            {
                selectionPromptCancelButton.onClick.AddListener(() => onCancel());
            }
        }

        private void ConfigureSelectionPromptTextLayout(bool cancelButtonVisible)
        {
            if (selectionPromptText == null)
            {
                return;
            }

            selectionPromptText.alignment = TextAlignmentOptions.Center;
            selectionPromptText.margin = cancelButtonVisible
                ? new Vector4(18f, 0f, 200f, 0f)
                : Vector4.zero;
        }

        private void ConfigureSelectionPromptRaycasts(bool cancelButtonVisible)
        {
            if (selectionPromptPanel == null)
            {
                return;
            }

            var promptGraphics = selectionPromptPanel.GetComponentsInChildren<Graphic>(true);
            foreach (var graphic in promptGraphics)
            {
                if (graphic == null)
                {
                    continue;
                }

                graphic.raycastTarget = false;
            }

            var backgroundGraphic = selectionPromptPanel.GetComponent<Graphic>();
            if (backgroundGraphic != null)
            {
                backgroundGraphic.raycastTarget = true;
            }

            if (selectionPromptCancelButton == null)
            {
                return;
            }

            var cancelGraphics = selectionPromptCancelButton.GetComponentsInChildren<Graphic>(true);
            foreach (var graphic in cancelGraphics)
            {
                if (graphic == null)
                {
                    continue;
                }

                graphic.raycastTarget = cancelButtonVisible;
            }
        }
    }

    public sealed class GameTransitionService
    {
        private readonly GameUIManager gameUIManager;
        private readonly GameObject modeSelectPanel;
        private readonly UI_StartGamePanel startGamePanel;
        private readonly Func<GameBoard> getBoard;
        private readonly Action stopGameplayMusic;
        private readonly Action unsubscribeFromPlayerMutationEvents;
        private readonly Action<GameBoard> unsubscribeBoardSubscribers;
        private readonly Action stopAllCoroutines;
        private readonly MycovariantDraftController mycovariantDraftController;
        private readonly GrowthPhaseRunner growthPhaseRunner;
        private readonly DecayPhaseRunner decayPhaseRunner;
        private readonly GridVisualizer gridVisualizer;
        private readonly SpecialEventPresentationService specialEventPresentationService;
        private readonly PostGrowthVisualSequence postGrowthVisualSequence;
        private readonly PauseMenuService pauseMenuService;
        private readonly Action resetManagerStateForMenuReturn;

        public GameTransitionService(
            GameUIManager gameUIManager,
            GameObject modeSelectPanel,
            UI_StartGamePanel startGamePanel,
            Func<GameBoard> getBoard,
            Action stopGameplayMusic,
            Action unsubscribeFromPlayerMutationEvents,
            Action<GameBoard> unsubscribeBoardSubscribers,
            Action stopAllCoroutines,
            MycovariantDraftController mycovariantDraftController,
            GrowthPhaseRunner growthPhaseRunner,
            DecayPhaseRunner decayPhaseRunner,
            GridVisualizer gridVisualizer,
            SpecialEventPresentationService specialEventPresentationService,
            PostGrowthVisualSequence postGrowthVisualSequence,
            PauseMenuService pauseMenuService,
            Action resetManagerStateForMenuReturn)
        {
            this.gameUIManager = gameUIManager;
            this.modeSelectPanel = modeSelectPanel;
            this.startGamePanel = startGamePanel;
            this.getBoard = getBoard;
            this.stopGameplayMusic = stopGameplayMusic;
            this.unsubscribeFromPlayerMutationEvents = unsubscribeFromPlayerMutationEvents;
            this.unsubscribeBoardSubscribers = unsubscribeBoardSubscribers;
            this.stopAllCoroutines = stopAllCoroutines;
            this.mycovariantDraftController = mycovariantDraftController;
            this.growthPhaseRunner = growthPhaseRunner;
            this.decayPhaseRunner = decayPhaseRunner;
            this.gridVisualizer = gridVisualizer;
            this.specialEventPresentationService = specialEventPresentationService;
            this.postGrowthVisualSequence = postGrowthVisualSequence;
            this.pauseMenuService = pauseMenuService;
            this.resetManagerStateForMenuReturn = resetManagerStateForMenuReturn;
        }

        public void ResetRuntimeStateForGameTransition()
        {
            var currentBoard = getBoard();
            stopGameplayMusic?.Invoke();
            unsubscribeFromPlayerMutationEvents?.Invoke();

            if (currentBoard != null)
            {
                postGrowthVisualSequence?.ResetForGameTransition(currentBoard);
                unsubscribeBoardSubscribers?.Invoke(currentBoard);
            }

            stopAllCoroutines?.Invoke();
            mycovariantDraftController?.ResetForGameTransition();
            growthPhaseRunner?.ResetForGameTransition();
            decayPhaseRunner?.ResetForGameTransition();
            gridVisualizer?.ResetForGameTransition();
            specialEventPresentationService?.Reset();
            gameUIManager?.MutationUIManager?.ResetForNewGameState();
            gameUIManager?.MutationTreeToastPresenter?.ResetForGameTransition();
            gameUIManager?.GameLogManager?.ResetForGameTransition();
            gameUIManager?.GlobalGameLogManager?.ResetForGameTransition();
            gameUIManager?.ClearBoard();
            gameUIManager?.PlayerUIBinder?.ClearIcons();
            gameUIManager?.GameLogRouter?.DisableSilentMode();
            TooltipManager.Instance?.CancelAll();
        }

        public void ShowStartGamePanel()
        {
            pauseMenuService?.ForceClose();

            if (gameUIManager != null)
            {
                gameUIManager.LoadingScreen?.gameObject.SetActive(false);
                gameUIManager.LeftSidebar?.gameObject.SetActive(false);
                gameUIManager.RightSidebar?.gameObject.SetActive(false);
                gameUIManager.MutationUIManager?.gameObject.SetActive(false);
                gameUIManager.EndGamePanel?.gameObject.SetActive(false);
                pauseMenuService?.SetGameplayVisibility(false);

                gameUIManager.GlobalGameLogManager?.ClearLog();
                gameUIManager.GameLogManager?.ClearLog();
                if (gameUIManager.GlobalGameLogManager != null && gameUIManager.GlobalGameLogPanel != null)
                {
                    gameUIManager.GlobalGameLogPanel.Initialize(gameUIManager.GlobalGameLogManager);
                }

                if (gameUIManager.GameLogManager != null && gameUIManager.GameLogPanel != null)
                {
                    gameUIManager.GameLogPanel.Initialize(gameUIManager.GameLogManager);
                }
            }

            if (modeSelectPanel != null)
            {
                modeSelectPanel.SetActive(true);
                if (startGamePanel != null)
                {
                    startGamePanel.gameObject.SetActive(false);
                }
            }
            else if (startGamePanel != null)
            {
                startGamePanel.gameObject.SetActive(true);
            }
        }

        public void ReturnToMainMenu()
        {
            pauseMenuService?.ForceClose();
            ResetRuntimeStateForGameTransition();
            resetManagerStateForMenuReturn?.Invoke();
            gridVisualizer?.ClearAllHighlights();
            gridVisualizer?.ClearHoverEffect();
            ShowStartGamePanel();
        }

        public void QuitGame()
        {
            pauseMenuService?.ForceClose();
            stopGameplayMusic?.Invoke();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }

    public sealed class GameStartService
    {
        private readonly Func<CampaignController> getCampaignController;
        private readonly GridVisualizer gridVisualizer;
        private readonly GameUIManager gameUIManager;
        private readonly GameObject modeSelectPanel;
        private readonly UI_StartGamePanel startGamePanel;
        private readonly MycovariantDraftController mycovariantDraftController;
        private readonly Action<GameMode> setGameMode;
        private readonly Func<GameMode> getGameMode;
        private readonly Func<System.Random> getRng;
        private readonly Func<bool> getTestingModeEnabled;
        private readonly Func<string> getForcedAdaptationId;
        private readonly Action<int, int> setBoardDimensions;
        private readonly Action<int, IReadOnlyList<int>> setHotseatConfig;
        private readonly Action<int> applyConfiguredPlayerMoldAssignments;
        private readonly Action<int> initializeGame;
        private readonly Action stopAllCoroutines;

        public GameStartService(
            Func<CampaignController> getCampaignController,
            GridVisualizer gridVisualizer,
            GameUIManager gameUIManager,
            GameObject modeSelectPanel,
            UI_StartGamePanel startGamePanel,
            MycovariantDraftController mycovariantDraftController,
            Action<GameMode> setGameMode,
            Func<GameMode> getGameMode,
            Func<System.Random> getRng,
            Func<bool> getTestingModeEnabled,
            Func<string> getForcedAdaptationId,
            Action<int, int> setBoardDimensions,
            Action<int, IReadOnlyList<int>> setHotseatConfig,
            Action<int> applyConfiguredPlayerMoldAssignments,
            Action<int> initializeGame,
            Action stopAllCoroutines)
        {
            this.getCampaignController = getCampaignController;
            this.gridVisualizer = gridVisualizer;
            this.gameUIManager = gameUIManager;
            this.modeSelectPanel = modeSelectPanel;
            this.startGamePanel = startGamePanel;
            this.mycovariantDraftController = mycovariantDraftController;
            this.setGameMode = setGameMode;
            this.getGameMode = getGameMode;
            this.getRng = getRng;
            this.getTestingModeEnabled = getTestingModeEnabled;
            this.getForcedAdaptationId = getForcedAdaptationId;
            this.setBoardDimensions = setBoardDimensions;
            this.setHotseatConfig = setHotseatConfig;
            this.applyConfiguredPlayerMoldAssignments = applyConfiguredPlayerMoldAssignments;
            this.initializeGame = initializeGame;
            this.stopAllCoroutines = stopAllCoroutines;
        }

        public bool HasCampaignSave()
        {
            return getCampaignController() != null && CampaignSaveService.Exists();
        }

        public bool IsCampaignAwaitingAdaptationSelection()
        {
            var campaignController = getCampaignController();
            return getGameMode() == GameMode.Campaign
                && campaignController != null
                && campaignController.IsAwaitingAdaptationSelection;
        }

        public bool HasPendingCampaignMoldinessUnlock()
        {
            var campaignController = getCampaignController();
            return getGameMode() == GameMode.Campaign
                && campaignController != null
                && campaignController.HasPendingMoldinessUnlockChoice;
        }

        public bool HasPendingCampaignMoldinessUnlockOnSavedRun()
        {
            var campaignController = getCampaignController();
            if (campaignController == null || !CampaignSaveService.Exists())
            {
                return false;
            }

            campaignController.Resume();
            return campaignController.HasPendingMoldinessUnlockChoice
                && campaignController.TryGetPendingMoldinessRewardSnapshot(out var pendingSnapshot)
                && pendingSnapshot != null;
        }

        public bool TryStartCampaignAdaptationDraft(Action onSelectionComplete)
        {
            var campaignController = getCampaignController();
            if (getGameMode() != GameMode.Campaign || campaignController == null || !campaignController.IsAwaitingAdaptationSelection)
            {
                return false;
            }

            if (campaignController.HasPendingMoldinessUnlockChoice)
            {
                Debug.Log("[GameManager] Cannot start campaign adaptation draft while a moldiness reward choice is still pending.");
                return false;
            }

            var choices = campaignController.GetAdaptationDraftChoices(
                getRng(),
                3,
                getTestingModeEnabled() ? getForcedAdaptationId() : string.Empty);
            if (choices.Count == 0)
            {
                Debug.Log("[GameManager] No remaining adaptations; advancing campaign level without reward.");
                bool advanced = campaignController.TryAdvanceWithoutAdaptationReward();
                if (advanced)
                {
                    onSelectionComplete?.Invoke();
                }

                return advanced;
            }

            if (mycovariantDraftController == null)
            {
                Debug.LogError("[GameManager] Cannot start campaign adaptation draft: MycovariantDraftController is missing.");
                return false;
            }

            mycovariantDraftController.StartCampaignAdaptationDraft(
                choices,
                selected =>
                {
                    bool applied = campaignController.TrySelectAdaptationAndAdvance(selected.Id);
                    if (!applied)
                    {
                        Debug.LogError($"[GameManager] Failed to apply selected adaptation '{selected.Id}'.");
                        return;
                    }

                    onSelectionComplete?.Invoke();
                },
                false,
                false,
                null);

            return true;
        }

        public void StartHotseatGame(int numberOfPlayers)
        {
            setGameMode(GameMode.Hotseat);
            gridVisualizer?.ClearBoardMediumOverride();
            gridVisualizer?.ClearPlayerMoldAssignments();
            initializeGame(numberOfPlayers);
        }

        public void StartCampaignNew(int humanMoldIndex = 0)
        {
            var campaignController = getCampaignController();
            if (campaignController == null)
            {
                Debug.LogError("[GameManager] Cannot start campaign: CampaignProgression not assigned.");
                return;
            }

            bool isTestingLevelOverride = GameManager.Instance != null
                && GameManager.Instance.IsTestingModeEnabled
                && GameManager.Instance.TestingCampaignLevelIndex > 0;

            if (!isTestingLevelOverride && FungusToast.Unity.Campaign.CampaignSaveService.Exists())
            {
                campaignController.Resume();
                if (campaignController.IsAwaitingDefeatCarryoverSelection)
                {
                    setGameMode(GameMode.Campaign);
                    ShowPendingCampaignDefeatCarryoverScreen(campaignController, UI.DefeatCarryoverEntryMode.DeferredResumePrompt);
                    return;
                }
            }

            int? levelOverride = GameManager.Instance != null && GameManager.Instance.IsTestingModeEnabled
                ? GameManager.Instance.TestingCampaignLevelIndex
                : null;
            IReadOnlyList<string> forcedStartingAdaptationIds = GameManager.Instance != null && GameManager.Instance.IsTestingModeEnabled
                ? GameManager.Instance.TestingForcedStartingAdaptationIds
                : null;
            campaignController.StartNew(humanMoldIndex, levelOverride, forcedStartingAdaptationIds);
            setGameMode(GameMode.Campaign);
            StartCampaignGameplay(campaignController);
        }

        public void StartCampaignResume()
        {
            var campaignController = getCampaignController();
            if (campaignController == null)
            {
                Debug.LogError("[GameManager] Cannot resume campaign: CampaignProgression not assigned.");
                return;
            }

            campaignController.Resume();
            setGameMode(GameMode.Campaign);

            if (campaignController.IsAwaitingDefeatCarryoverSelection)
            {
                ShowPendingCampaignDefeatCarryoverScreen(campaignController, UI.DefeatCarryoverEntryMode.DeferredResumePrompt);
                return;
            }

            if (campaignController.HasPendingMoldinessUnlockChoice
                && campaignController.TryGetPendingMoldinessRewardSnapshot(out var pendingMoldinessSnapshot)
                && pendingMoldinessSnapshot != null)
            {
                ShowPendingCampaignMoldinessRewardScreen(campaignController, pendingMoldinessSnapshot);
                return;
            }

            if (campaignController.IsAwaitingAdaptationSelection
                && campaignController.TryGetPendingVictorySnapshot(out var pendingSnapshot)
                && pendingSnapshot != null)
            {
                ShowPendingCampaignVictoryScreen(campaignController, pendingSnapshot);
                return;
            }

            StartCampaignGameplay(campaignController);
        }

        private void StartCampaignGameplay(CampaignController campaignController)
        {
            IReadOnlyList<string> forcedStartingAdaptationIds = GameManager.Instance != null && GameManager.Instance.IsTestingModeEnabled
                ? GameManager.Instance.TestingForcedStartingAdaptationIds
                : Array.Empty<string>();
            campaignController?.SetTemporaryTestingAdaptationIds(forcedStartingAdaptationIds);

            var preset = campaignController.CurrentBoardPreset;
            if (preset != null)
            {
                setBoardDimensions(preset.boardWidth, preset.boardHeight);
            }

            gridVisualizer?.SetBoardMedium(preset?.boardMedium);
            int totalPlayers = 1 + campaignController.GetCurrentAiPlayerCount();
            setHotseatConfig(1, new[] { campaignController.HumanMoldIndex });
            initializeGame(totalPlayers);
        }

        private void ShowPendingCampaignVictoryScreen(CampaignController campaignController, CampaignVictorySnapshot snapshot)
        {
            if (snapshot == null || gameUIManager?.EndGamePanel == null)
            {
                Debug.LogWarning("[GameManager] Pending campaign victory snapshot missing; continuing directly into gameplay.");
                StartCampaignGameplay(campaignController);
                return;
            }

            stopAllCoroutines?.Invoke();
            mycovariantDraftController?.StopAllCoroutines();

            modeSelectPanel?.SetActive(false);
            startGamePanel?.gameObject.SetActive(false);
            var campaignPanel = modeSelectPanel != null ? modeSelectPanel.transform.Find("UI_CampaignPanel") : null;
            if (campaignPanel != null)
            {
                campaignPanel.gameObject.SetActive(false);
            }

            gameUIManager.LoadingScreen?.gameObject.SetActive(false);
            gameUIManager.LeftSidebar?.gameObject.SetActive(false);
            gameUIManager.RightSidebar?.gameObject.SetActive(true);
            gameUIManager.MutationUIManager?.gameObject.SetActive(false);
            PreparePendingCampaignSnapshotPresentation(campaignController, snapshot);

            gameUIManager.EndGamePanel.ShowCampaignPendingVictorySnapshot(snapshot);
        }

        private void ShowPendingCampaignDefeatCarryoverScreen(
            CampaignController campaignController,
            UI.DefeatCarryoverEntryMode entryMode = UI.DefeatCarryoverEntryMode.ImmediateLossScreen)
        {
            if (campaignController == null || gameUIManager?.EndGamePanel == null)
            {
                Debug.LogWarning("[GameManager] Pending campaign defeat carryover screen could not be shown.");
                return;
            }

            stopAllCoroutines?.Invoke();
            mycovariantDraftController?.StopAllCoroutines();

            modeSelectPanel?.SetActive(false);
            startGamePanel?.gameObject.SetActive(false);
            var campaignPanel = modeSelectPanel != null ? modeSelectPanel.transform.Find("UI_CampaignPanel") : null;
            if (campaignPanel != null)
            {
                campaignPanel.gameObject.SetActive(false);
            }

            gameUIManager.LoadingScreen?.gameObject.SetActive(false);
            gameUIManager.LeftSidebar?.gameObject.SetActive(false);
            gameUIManager.RightSidebar?.gameObject.SetActive(true);
            gameUIManager.MutationUIManager?.gameObject.SetActive(false);
            PreparePendingCampaignSnapshotPresentation(campaignController, null);

            var carryoverOptions = campaignController.GetPendingDefeatCarryoverOptions();
            int carryoverCapacity = Mathf.Max(0, campaignController.State?.moldiness?.failedRunAdaptationCarryoverCount ?? 0);
            gameUIManager.EndGamePanel.ShowCampaignPendingDefeatCarryoverSelection(carryoverOptions, carryoverCapacity, entryMode);
        }

        public void ShowPendingCampaignMoldinessRewardFromMainMenu()
        {
            var campaignController = getCampaignController();
            if (campaignController == null)
            {
                Debug.LogError("[GameManager] Cannot show pending campaign moldiness reward: CampaignProgression not assigned.");
                return;
            }

            campaignController.Resume();
            setGameMode(GameMode.Campaign);

            if (!campaignController.HasPendingMoldinessUnlockChoice
                || !campaignController.TryGetPendingMoldinessRewardSnapshot(out var pendingSnapshot)
                || pendingSnapshot == null)
            {
                Debug.LogWarning("[GameManager] No pending campaign moldiness reward is available from main menu.");
                return;
            }

            bool returnToCampaignMenuAfterSelection = !campaignController.IsAwaitingAdaptationSelection;
            ShowPendingCampaignMoldinessRewardScreen(campaignController, pendingSnapshot, returnToCampaignMenuAfterSelection);
        }

        private void ShowPendingCampaignMoldinessRewardScreen(CampaignController campaignController, CampaignVictorySnapshot snapshot)
        {
            ShowPendingCampaignMoldinessRewardScreen(campaignController, snapshot, false);
        }

        private void ShowPendingCampaignMoldinessRewardScreen(CampaignController campaignController, CampaignVictorySnapshot snapshot, bool returnToCampaignMenuAfterSelection)
        {
            if (campaignController == null || snapshot == null || gameUIManager?.EndGamePanel == null)
            {
                Debug.LogWarning("[GameManager] Pending campaign moldiness reward screen could not be shown.");
                return;
            }

            stopAllCoroutines?.Invoke();
            mycovariantDraftController?.StopAllCoroutines();

            modeSelectPanel?.SetActive(false);
            startGamePanel?.gameObject.SetActive(false);
            var campaignPanel = modeSelectPanel != null ? modeSelectPanel.transform.Find("UI_CampaignPanel") : null;
            if (campaignPanel != null)
            {
                campaignPanel.gameObject.SetActive(false);
            }

            gameUIManager.LoadingScreen?.gameObject.SetActive(false);
            gameUIManager.LeftSidebar?.gameObject.SetActive(false);
            gameUIManager.RightSidebar?.gameObject.SetActive(false);
            gameUIManager.MutationUIManager?.gameObject.SetActive(false);
            PreparePendingCampaignSnapshotPresentation(campaignController, snapshot);

            var offers = campaignController.GetPendingMoldinessUnlockOffers(getRng(), 3);
            gameUIManager.EndGamePanel.ShowCampaignPendingMoldinessRewardSelection(snapshot, offers, returnToCampaignMenuAfterSelection);
        }

        private void PreparePendingCampaignSnapshotPresentation(CampaignController campaignController, CampaignVictorySnapshot snapshot)
        {
            gameUIManager?.ClearBoard();
            gridVisualizer?.SetBoardMedium(campaignController?.CurrentBoardPreset?.boardMedium);

            var playerBinder = gameUIManager?.PlayerUIBinder;
            playerBinder?.ClearIcons();

            if (campaignController == null || snapshot?.rows == null || snapshot.rows.Count == 0 || gridVisualizer == null)
            {
                return;
            }

            int totalPlayers = Mathf.Max(1, snapshot.rows.Count);
            setHotseatConfig(1, new[] { campaignController.HumanMoldIndex });
            applyConfiguredPlayerMoldAssignments?.Invoke(totalPlayers);

            foreach (var row in snapshot.rows)
            {
                var icon = gridVisualizer.GetTileForPlayer(row.playerId)?.sprite;
                if (icon != null)
                {
                    playerBinder?.AssignIcon(row.playerId, icon);
                }
            }
        }
    }

    public sealed class PlayerPerspectiveService
    {
        private readonly GameUIManager gameUIManager;

        public PlayerPerspectiveService(GameUIManager gameUIManager)
        {
            this.gameUIManager = gameUIManager;
        }

        public void InitializeGameplayPerspective(Player humanPlayer, GameBoard board, IReadOnlyList<Player> players, GridVisualizer gridVisualizer)
        {
            if (humanPlayer == null || gameUIManager == null || board == null)
            {
                return;
            }

            InitializeHumanSidebarUi(humanPlayer, board);
            gameUIManager.RightSidebar?.SetGridVisualizer(gridVisualizer);
            gameUIManager.RightSidebar?.SetBoard(board);
            gameUIManager.RightSidebar?.InitializePlayerSummaries(board.Players);
            gameUIManager.RightSidebar?.SetPerspectivePlayer(humanPlayer);
            gameUIManager.RightSidebar?.InitializeRandomDecayChanceTooltip(board, humanPlayer);
            gameUIManager.RightSidebar?.UpdateRandomDecayChance(board.CurrentRound);
        }

        public void SetActiveHumanPlayer(Player player, GameBoard board, int humanPlayerCount, Action<Player> setPrimaryHuman)
        {
            if (player == null)
            {
                return;
            }

            var logManager = gameUIManager?.GameLogManager;
            var playerLogPanel = gameUIManager?.GameLogPanel;

            logManager?.SetActiveHumanPlayer(player.PlayerId, board);
            logManager?.EmitPendingSegmentSummariesFor(player.PlayerId);

            if (humanPlayerCount > 1)
            {
                playerLogPanel?.EnablePlayerSpecificFiltering();
            }

            playerLogPanel?.SetActivePlayer(player.PlayerId, player.PlayerName);
            setPrimaryHuman?.Invoke(player);
            gameUIManager?.RightSidebar?.SetPerspectivePlayer(player);
        }

        private void InitializeHumanSidebarUi(Player humanPlayer, GameBoard board)
        {
            gameUIManager.MoldProfileRoot?.Initialize(humanPlayer, board?.Players);

            if (gameUIManager.MutationUIManager != null)
            {
                gameUIManager.MutationUIManager.ReinitializeForPlayer(humanPlayer, keepPanelClosed: true);
                gameUIManager.MutationUIManager.SetSpendPointsButtonVisible(true);
                gameUIManager.MutationUIManager.RefreshSpendPointsButtonUI();
                gameUIManager.MutationUIManager.SetSpendPointsButtonInteractable(false);
            }
        }
    }

    public sealed class PlayerMoldAssignmentService
    {
        private readonly GridVisualizer gridVisualizer;
        private readonly Func<int> getConfiguredHumanPlayerCount;
        private readonly Func<IReadOnlyList<int>> getConfiguredHumanMoldIndices;

        public PlayerMoldAssignmentService(
            GridVisualizer gridVisualizer,
            Func<int> getConfiguredHumanPlayerCount,
            Func<IReadOnlyList<int>> getConfiguredHumanMoldIndices)
        {
            this.gridVisualizer = gridVisualizer;
            this.getConfiguredHumanPlayerCount = getConfiguredHumanPlayerCount;
            this.getConfiguredHumanMoldIndices = getConfiguredHumanMoldIndices;
        }

        public void ApplyConfiguredPlayerMoldAssignments(int totalPlayers)
        {
            if (gridVisualizer == null)
            {
                return;
            }

            var assignments = ResolveConfiguredPlayerMoldAssignments(totalPlayers);
            if (assignments.Count == 0)
            {
                gridVisualizer.ClearPlayerMoldAssignments();
                return;
            }

            gridVisualizer.SetPlayerMoldAssignments(assignments);
        }

        private List<int> ResolveConfiguredPlayerMoldAssignments(int totalPlayers)
        {
            var assignments = new List<int>();
            int availableMolds = gridVisualizer.PlayerMoldTileCount;
            if (availableMolds <= 0 || totalPlayers <= 0)
            {
                return assignments;
            }

            int humanCount = Mathf.Clamp(getConfiguredHumanPlayerCount(), 1, totalPlayers);
            var remainingMolds = Enumerable.Range(0, availableMolds).ToList();

            for (int humanIndex = 0; humanIndex < humanCount; humanIndex++)
            {
                int moldIndex = TakeConfiguredOrFallbackHumanMoldIndex(humanIndex, remainingMolds);
                assignments.Add(moldIndex);
                remainingMolds.Remove(moldIndex);
            }

            for (int playerIndex = humanCount; playerIndex < totalPlayers; playerIndex++)
            {
                if (remainingMolds.Count > 0)
                {
                    assignments.Add(remainingMolds[0]);
                    remainingMolds.RemoveAt(0);
                    continue;
                }

                assignments.Add(playerIndex % availableMolds);
            }

            return assignments;
        }

        private int TakeConfiguredOrFallbackHumanMoldIndex(int humanIndex, List<int> remainingMolds)
        {
            if (remainingMolds == null || remainingMolds.Count == 0)
            {
                return 0;
            }

            var configuredHumanMoldIndices = getConfiguredHumanMoldIndices();
            if (humanIndex < configuredHumanMoldIndices.Count)
            {
                int configuredMoldIndex = configuredHumanMoldIndices[humanIndex];
                if (remainingMolds.Contains(configuredMoldIndex))
                {
                    return configuredMoldIndex;
                }
            }

            return remainingMolds[0];
        }
    }
}

namespace FungusToast.Unity.Endgame
{
    public sealed class EndgamePlayerStatistics
    {
        public static EndgamePlayerStatistics Zero { get; } = new EndgamePlayerStatistics(
            spentMutationPoints: 0,
            tilesColonized: 0,
            tilesToxified: 0,
            cellsReclaimed: 0,
            cellsOvergrown: 0,
            cellsInfested: 0,
            cellsPoisoned: 0);

        public int SpentMutationPoints { get; }
        public int TilesColonized { get; }
        public int TilesToxified { get; }
        public int CellsReclaimed { get; }
        public int CellsOvergrown { get; }
        public int CellsInfested { get; }
        public int CellsPoisoned { get; }

        public EndgamePlayerStatistics(
            int spentMutationPoints,
            int tilesColonized,
            int tilesToxified,
            int cellsReclaimed,
            int cellsOvergrown,
            int cellsInfested,
            int cellsPoisoned)
        {
            SpentMutationPoints = spentMutationPoints;
            TilesColonized = tilesColonized;
            TilesToxified = tilesToxified;
            CellsReclaimed = cellsReclaimed;
            CellsOvergrown = cellsOvergrown;
            CellsInfested = cellsInfested;
            CellsPoisoned = cellsPoisoned;
        }
    }

    public sealed class EndgamePlayerStatisticsSnapshot
    {
        public static EndgamePlayerStatisticsSnapshot Empty { get; } = new EndgamePlayerStatisticsSnapshot(new Dictionary<int, EndgamePlayerStatistics>());

        private readonly IReadOnlyDictionary<int, EndgamePlayerStatistics> statisticsByPlayerId;

        public EndgamePlayerStatisticsSnapshot(IReadOnlyDictionary<int, EndgamePlayerStatistics> statisticsByPlayerId)
        {
            this.statisticsByPlayerId = statisticsByPlayerId ?? new Dictionary<int, EndgamePlayerStatistics>();
        }

        public EndgamePlayerStatistics GetPlayerStatistics(int playerId)
        {
            return statisticsByPlayerId.TryGetValue(playerId, out var statistics)
                ? statistics
                : EndgamePlayerStatistics.Zero;
        }
    }

    public sealed class EndgamePlayerStatisticsTracker
    {
        private sealed class MutableEndgamePlayerStatistics
        {
            public int SpentMutationPoints;
            public int TilesColonized;
            public int TilesToxified;
            public int CellsReclaimed;
            public int CellsOvergrown;
            public int CellsInfested;
            public int CellsPoisoned;

            public EndgamePlayerStatistics ToSnapshot()
            {
                return new EndgamePlayerStatistics(
                    SpentMutationPoints,
                    TilesColonized,
                    TilesToxified,
                    CellsReclaimed,
                    CellsOvergrown,
                    CellsInfested,
                    CellsPoisoned);
            }
        }

        private readonly Dictionary<int, MutableEndgamePlayerStatistics> statisticsByPlayerId = new();
        private GameBoard subscribedBoard;

        public void Reset()
        {
            Detach();
            statisticsByPlayerId.Clear();
        }

        public void Attach(GameBoard board)
        {
            if (ReferenceEquals(subscribedBoard, board))
            {
                EnsurePlayersInitialized(board?.Players);
                return;
            }

            Detach();
            subscribedBoard = board;
            if (subscribedBoard == null)
            {
                return;
            }

            EnsurePlayersInitialized(subscribedBoard.Players);
            subscribedBoard.CellColonized += OnCellColonized;
            subscribedBoard.CellToxified += OnCellToxified;
            subscribedBoard.CellReclaimed += OnCellReclaimed;
            subscribedBoard.CellOvergrown += OnCellOvergrown;
            subscribedBoard.CellInfested += OnCellInfested;
            subscribedBoard.CellPoisoned += OnCellPoisoned;
        }

        public void Detach()
        {
            if (subscribedBoard == null)
            {
                return;
            }

            subscribedBoard.CellColonized -= OnCellColonized;
            subscribedBoard.CellToxified -= OnCellToxified;
            subscribedBoard.CellReclaimed -= OnCellReclaimed;
            subscribedBoard.CellOvergrown -= OnCellOvergrown;
            subscribedBoard.CellInfested -= OnCellInfested;
            subscribedBoard.CellPoisoned -= OnCellPoisoned;
            subscribedBoard = null;
        }

        public void RecordMutationPointsSpent(int playerId, MutationTier mutationTier, int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            EnsurePlayerInitialized(playerId).SpentMutationPoints += amount;
        }

        public EndgamePlayerStatisticsSnapshot CreateSnapshot(IEnumerable<Player> players)
        {
            EnsurePlayersInitialized(players);

            var snapshot = new Dictionary<int, EndgamePlayerStatistics>();
            foreach (var entry in statisticsByPlayerId)
            {
                snapshot[entry.Key] = entry.Value.ToSnapshot();
            }

            return new EndgamePlayerStatisticsSnapshot(snapshot);
        }

        private void OnCellColonized(int playerId, int tileId, GrowthSource source)
        {
            EnsurePlayerInitialized(playerId).TilesColonized++;
        }

        private void OnCellToxified(int playerId, int tileId, GrowthSource source)
        {
            EnsurePlayerInitialized(playerId).TilesToxified++;
        }

        private void OnCellReclaimed(int playerId, int tileId, GrowthSource source)
        {
            EnsurePlayerInitialized(playerId).CellsReclaimed++;
        }

        private void OnCellOvergrown(int playerId, int tileId, int oldOwnerId, GrowthSource source)
        {
            EnsurePlayerInitialized(playerId).CellsOvergrown++;
        }

        private void OnCellInfested(int playerId, int tileId, int oldOwnerId, GrowthSource source)
        {
            EnsurePlayerInitialized(playerId).CellsInfested++;
        }

        private void OnCellPoisoned(int playerId, int tileId, int oldOwnerId, GrowthSource source)
        {
            EnsurePlayerInitialized(playerId).CellsPoisoned++;
        }

        private void EnsurePlayersInitialized(IEnumerable<Player> players)
        {
            if (players == null)
            {
                return;
            }

            foreach (var player in players)
            {
                if (player != null)
                {
                    EnsurePlayerInitialized(player.PlayerId);
                }
            }
        }

        private MutableEndgamePlayerStatistics EnsurePlayerInitialized(int playerId)
        {
            if (!statisticsByPlayerId.TryGetValue(playerId, out var statistics))
            {
                statistics = new MutableEndgamePlayerStatistics();
                statisticsByPlayerId[playerId] = statistics;
            }

            return statistics;
        }
    }
}
