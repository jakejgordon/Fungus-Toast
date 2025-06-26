using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Death;
using FungusToast.Core.Metrics;
using FungusToast.Core.Mutations;
using FungusToast.Core.Mycovariants;
using FungusToast.Core.Phases;
using FungusToast.Core.Players;
using System;
using System.Collections.Generic;

namespace FungusToast.Core.Phases
{
    public static class TurnEngine
    {
        /// <summary>
        /// Assigns base, bonus, and mutation-derived points and triggers auto-upgrades and strategy spending.
        /// </summary>
        public static void AssignMutationPoints(GameBoard board, List<Player> players, List<Mutation> allMutations, Random rng, ISimulationObserver? simulationObserver = null)
        {
            foreach (var player in players)
            {
                player.AssignMutationPoints(players, rng, allMutations, simulationObserver);
                player.MutationStrategy?.SpendMutationPoints(player, allMutations, board, rng, simulationObserver);
            }
        }

        /// <summary>
        /// Executes a full multi-cycle growth phase, including mutation-based pre-growth effects.
        /// </summary>
        public static void RunGrowthPhase(GameBoard board, List<Player> players, Random rng, RoundContext roundContext, ISimulationObserver? observer = null)
        {
            var processor = new GrowthPhaseProcessor(board, players, rng, observer);

            for (int cycle = 0; cycle < GameBalance.TotalGrowthCycles; cycle++)
            {
                processor.ExecuteSingleCycle(roundContext);
            }

            // Apply post-growth reclaim effects
            board.OnPostGrowthPhase();

            // Apply post-growth surge mutation effect: Hyphal Vectoring
            MutationEffectProcessor.ProcessHyphalVectoring(board, players, rng, observer);
        }

        /// <summary>
        /// Executes the decay phase for all living fungal cells.
        /// </summary>
        public static void RunDecayPhase(
            GameBoard board,
            List<Player> players,
            Dictionary<int, int> failedGrowthsByPlayerId,
            Random rng,
            ISimulationObserver? simulationObserver = null)
        {
            DeathEngine.ExecuteDeathCycle(board, players, failedGrowthsByPlayerId, rng, simulationObserver);
        }
    }
}
