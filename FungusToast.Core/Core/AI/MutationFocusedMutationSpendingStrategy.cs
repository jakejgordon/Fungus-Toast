using System;
using System.Collections.Generic;
using System.Linq;
using FungusToast.Core.Mutations;
using FungusToast.Core.Players;

namespace FungusToast.Core.AI
{
    public class MutationFocusedMutationSpendingStrategy : IMutationSpendingStrategy
    {
        private static readonly Random rng = new();
        private readonly SmartRandomMutationSpendingStrategy fallbackStrategy = new();

        public void SpendMutationPoints(Player player, List<Mutation> allMutations)
        {
            if (player == null || allMutations == null || player.MutationPoints <= 0)
                return;

            // 1. Prioritize Genetic Drift mutations
            var geneticDriftMutations = allMutations
                .Where(m => m.Category == MutationCategory.GeneticDrift)
                .Where(player.CanUpgrade)
                .OrderBy(m => GetTier(m)) // lower tiers first
                .ThenBy(m => m.Id)        // stable sort
                .ToList();

            foreach (var mutation in geneticDriftMutations)
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
            fallbackStrategy.SpendMutationPoints(player, allMutations);
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
