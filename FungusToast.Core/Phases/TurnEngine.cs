using System;
using System.Collections.Generic;
using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Death;
using FungusToast.Core.Metrics;
using FungusToast.Core.Mutations;
using FungusToast.Core.Mycovariants;
using FungusToast.Core.Players;

namespace FungusToast.Core.Phases
{
    public static class TurnEngine
    {
        /// <summary>
        /// Assigns base, bonus, and mutation-derived points and triggers auto-upgrades and strategy spending.
        /// </summary>
        public static void AssignMutationPoints(
            GameBoard board,
            List<Player> players,
            List<Mutation> allMutations,
            Random rng,
            ISimulationObserver simulationObserver)
        {
            // Fire MutationPhaseStart event for Mutator Phenotype and other mutation phase effects
            board.OnMutationPhaseStart();

            foreach (var player in players)
            {
                player.AssignMutationPoints(players, rng, board, simulationObserver, allMutations);
                player.MutationStrategy?.SpendMutationPoints(player, allMutations, board, rng, simulationObserver);
            }
        }

        /// <summary>
        /// Executes a full multi-cycle growth phase, including mutation-based pre-growth effects.
        /// </summary>
        public static void RunGrowthPhase(
            GameBoard board,
            List<Player> players,
            Random rng,
            ISimulationObserver observer)
        {
            board.OnPreGrowthPhase();
            var processor = new GrowthPhaseProcessor(board, players, rng, observer);

            for (int cycle = 0; cycle < GameBalance.TotalGrowthCycles; cycle++)
            {
                processor.ExecuteSingleCycle(board.CurrentRoundContext);
            }

            // Fire post-growth phase event (listeners may buffer animations / effects)
            board.OnPostGrowthPhase();

            // Apply Hyphal Resistance Transfer effect after the initial post-growth callbacks
            MycovariantEffectProcessor.OnPostGrowthPhase_HyphalResistanceTransfer(board, players, rng, observer);

            // Signal that all logical post-growth effects (including HRT) are complete
            board.OnPostGrowthPhaseCompleted();
        }

        /// <summary>
        /// Executes the decay phase for all living fungal cells.
        /// </summary>
        public static void RunDecayPhase(
            GameBoard board,
            List<Player> players,
            Dictionary<int, int> failedGrowthsByPlayerId,
            Random rng,
            ISimulationObserver simulationObserver)
        {
            DeathEngine.ExecuteDeathCycle(board, failedGrowthsByPlayerId, rng, simulationObserver);
            // Fire post-decay phase hook for UI/log aggregation
            board.OnPostDecayPhase();
        }
    }
}
