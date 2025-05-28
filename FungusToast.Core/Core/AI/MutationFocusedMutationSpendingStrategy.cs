using System;
using System.Collections.Generic;
using System.Linq;
using FungusToast.Core.Board;
using FungusToast.Core.Mutations;
using FungusToast.Core.Players;

namespace FungusToast.Core.AI
{
    /// <summary>
    /// Focuses mutation spending on the Genetic Drift category, prioritizing lower-tier upgrades first.
    /// Falls back to the SmartRandom strategy if points remain.
    /// </summary>
    public class MutationFocusedMutationSpendingStrategy : MutationSpendingStrategyBase
    {
        private readonly SmartRandomMutationSpendingStrategy fallbackStrategy = new();

        public override void SpendMutationPoints(Player player, List<Mutation> allMutations, GameBoard board)
        {
            if (player == null || allMutations == null || player.MutationPoints <= 0)
                return;

            // 1. Prioritize Genetic Drift mutations, lower tier first
            var driftMutations = allMutations
                .Where(m => m.Category == MutationCategory.GeneticDrift && player.CanUpgrade(m))
                .OrderBy(m => GetTier(m))
                .ThenBy(m => m.Id) // stable fallback order
                .ToList();

            foreach (var mutation in driftMutations)
            {
                while (player.MutationPoints >= mutation.PointsPerUpgrade && player.CanUpgrade(mutation))
                {
                    bool success = player.TryUpgradeMutation(mutation);
                    if (!success) break;
                }

                if (player.MutationPoints <= 0)
                    return;
            }

            // 2. Fallback to SmartRandom for remaining points
            fallbackStrategy.SpendMutationPoints(player, allMutations, board);
        }

        private int GetTier(Mutation mutation)
        {
            return (mutation.Prerequisites.Count == 0) ? 1 : 1 + GetMaxTierDepth(mutation);
        }

        private int GetMaxTierDepth(Mutation mutation)
        {
            if (mutation.Prerequisites.Count == 0) return 0;
            return 1 + mutation.Prerequisites.Max(p => GetMaxTierDepth(p));
        }

        private int GetMaxTierDepth(MutationPrerequisite prereq)
        {
            var parent = MutationRegistry.GetById(prereq.MutationId);
            if (parent == null || parent.Prerequisites.Count == 0) return 0;
            return 1 + parent.Prerequisites.Max(p => GetMaxTierDepth(p));
        }
    }
}
