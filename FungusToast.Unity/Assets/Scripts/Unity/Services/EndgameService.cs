using System;
using System.Collections.Generic;
using System.Linq;
using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Players;
using FungusToast.Unity.Campaign;
using FungusToast.Unity.UI;
using UnityEngine;

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
        private readonly Func<Dictionary<(int playerId, int mutationId), List<int>>> getFirstUpgradeRounds;
        private readonly Func<bool> getTestingModeEnabled;
        private readonly Func<ForcedGameResultMode> getForcedGameResultMode;

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
            Func<Dictionary<(int playerId, int mutationId), List<int>>> getFirstUpgradeRounds,
            Func<bool> getTestingModeEnabled,
            Func<ForcedGameResultMode> getForcedGameResultMode)
        {
            this.ui = ui;
            this.getBoard = getBoard;
            this.getHumanPlayer = getHumanPlayer;
            this.getGameMode = getGameMode;
            this.getCampaignController = getCampaignController;
            this.getCampaignProgression = getCampaignProgression;
            this.getFirstUpgradeRounds = getFirstUpgradeRounds;
            this.getTestingModeEnabled = getTestingModeEnabled;
            this.getForcedGameResultMode = getForcedGameResultMode;
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
                ui.EndGamePanel.ShowResultsWithOutcome(
                    ranked, board, true, humanWon,
                    finalLevelPreAdvance && humanWon, hasNextLevel,
                    humanWon ? completedLevelDisplay : lostLevelDisplay,
                    completedLevelDisplay,
                    campaignController.State.pendingAdaptationSelection);
            }
            else
            {
                ui.EndGamePanel.ShowResults(ranked, board);
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
}
