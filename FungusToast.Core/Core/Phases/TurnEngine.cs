using System;
using System.Collections.Generic;
using System.Linq;
using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Death;
using FungusToast.Core.Growth;
using FungusToast.Core.Mutations;
using FungusToast.Core.Players;

namespace FungusToast.Core.Phases
{
    public static class TurnEngine
    {
        /// <summary>
        /// Assigns base, bonus, and mutation-derived points and triggers auto-upgrades and strategy spending.
        /// </summary>
        public static void AssignMutationPoints(List<Player> players, List<Mutation> allMutations, Random rng)
        {
            foreach (var p in players)
            {
                p.AssignMutationPoints(players, rng, allMutations);
                p.MutationStrategy?.SpendMutationPoints(p, allMutations);
            }
        }

        /// <summary>
        /// Executes a full multi-cycle growth phase.
        /// </summary>
        public static void RunGrowthPhase(GameBoard board, List<Player> players)
        {
            var processor = new GrowthPhaseProcessor(board, players);

            for (int cycle = 0; cycle < GameBalance.TotalGrowthCycles; cycle++)
            {
                processor.ExecuteSingleCycle();
            }
        }

        /// <summary>
        /// Executes the decay phase for all living fungal cells.
        /// </summary>
        public static void RunDecayPhase(GameBoard board, List<Player> players)
        {
            DeathEngine.ExecuteDeathCycle(board, players);
        }
    }
}
