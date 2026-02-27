using System;
using System.Collections.Generic;
using System.Linq;
using FungusToast.Core.Board;
using FungusToast.Core.Mutations;
using FungusToast.Core.Phases;
using FungusToast.Core.Players;
using FungusToast.Core.Metrics;
using FungusToast.Unity.UI;

namespace FungusToast.Unity
{
    /// <summary>
    /// Handles mutation point assignment and AI spending logic.
    /// Extracted from GameManager to reduce its responsibilities.
    /// </summary>
    public class MutationPointService
    {
        private readonly GameUIManager ui;
        private readonly Func<GameBoard> getBoard;
        private readonly Func<MutationManager> getMutationManager;
        private readonly Func<System.Random> getRng;
        private readonly Func<bool> isFastForwarding;
        private readonly Func<bool> isTesting;
        private readonly Func<int> getFastForwardRounds;
        private readonly Action startGrowthPhase;

        public MutationPointService(
            GameUIManager ui,
            Func<GameBoard> getBoard,
            Func<MutationManager> getMutationManager,
            Func<System.Random> getRng,
            Func<bool> isFastForwarding,
            Func<bool> isTesting,
            Func<int> getFastForwardRounds,
            Action startGrowthPhase)
        {
            this.ui = ui;
            this.getBoard = getBoard;
            this.getMutationManager = getMutationManager;
            this.getRng = getRng;
            this.isFastForwarding = isFastForwarding;
            this.isTesting = isTesting;
            this.getFastForwardRounds = getFastForwardRounds;
            this.startGrowthPhase = startGrowthPhase;
        }

        /// <summary>
        /// Assigns mutation points to all players for the current round
        /// via the core TurnEngine.
        /// </summary>
        public void AssignMutationPoints()
        {
            var board = getBoard();
            var all = getMutationManager().AllMutations.Values.ToList();
            var localRng = new System.Random();

            TurnEngine.AssignMutationPoints(board, board.Players, all, localRng, ui.GameLogRouter);
            ui.MutationUIManager?.RefreshAllMutationButtons();
            ui.MoldProfileRoot?.Refresh();
        }

        /// <summary>
        /// Auto-spends mutation points for all AI players (and optionally humans
        /// during fast-forward / testing mode), then triggers the growth phase.
        /// </summary>
        public void SpendAllMutationPointsForAIPlayers()
        {
            var board = getBoard();
            var rng = getRng();
            var all = getMutationManager().GetAllMutations().ToList();

            bool includeHumans = isFastForwarding() || (isTesting() && getFastForwardRounds() > 0);

            foreach (var p in board.Players)
            {
                if (p.PlayerType == PlayerTypeEnum.AI
                    || (includeHumans && p.PlayerType == PlayerTypeEnum.Human))
                {
                    p.MutationStrategy?.SpendMutationPoints(p, all, board, rng, ui.GameLogRouter);
                }
            }

            startGrowthPhase();
        }
    }
}
