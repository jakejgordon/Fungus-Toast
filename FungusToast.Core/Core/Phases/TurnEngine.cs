using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Core.Metrics; // Needed for IGrowthObserver
using FungusToast.Core.Death;
using FungusToast.Core.Metrics;
using FungusToast.Core.Mutations;
using FungusToast.Core.Players;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FungusToast.Core.Phases
{
    public static class TurnEngine
    {
        /// <summary>
        /// Assigns base, bonus, and mutation-derived points and triggers auto-upgrades and strategy spending.
        /// </summary>
        public static void AssignMutationPoints(GameBoard board, List<Player> players, List<Mutation> allMutations, Random rng, IMutationPointObserver? mutationPointsObserver = null)
        {
            foreach (var player in players)
            {
                player.AssignMutationPoints(players, rng, allMutations, mutationPointsObserver);
                player.MutationStrategy?.SpendMutationPoints(player, allMutations, board);
            }
        }

        /// <summary>
        /// Executes a full multi-cycle growth phase, including mutation-based pre-growth effects.
        /// </summary>
        public static void RunGrowthPhase(GameBoard board, List<Player> players, Random rng, IGrowthAndDecayObserver? observer = null)
        {
            var processor = new GrowthPhaseProcessor(board, players, rng, observer);

            for (int cycle = 0; cycle < GameBalance.TotalGrowthCycles; cycle++)
            {
                processor.ExecuteSingleCycle();
            }

            MutationEffectProcessor.ApplyRegenerativeHyphaeReclaims(board, players, rng);
        }

        /// <summary>
        /// Executes the decay phase for all living fungal cells.
        /// </summary>
        public static void RunDecayPhase(GameBoard board, List<Player> players, Dictionary<int, int> failedGrowthsByPlayerId, ISporeDropObserver? sporeDropObserver = null, IGrowthAndDecayObserver? growthAndDecayObserver = null)
        {
            DeathEngine.ExecuteDeathCycle(board, players, failedGrowthsByPlayerId, sporeDropObserver, growthAndDecayObserver);
        }

    }
}
