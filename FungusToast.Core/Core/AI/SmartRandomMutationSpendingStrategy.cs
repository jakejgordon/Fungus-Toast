using System;
using System.Collections.Generic;
using System.Linq;
using FungusToast.Core.Core.Mutations;
using FungusToast.Core.Mutations;
using FungusToast.Core.Players;

namespace FungusToast.Core.AI
{
    public class SmartRandomMutationSpendingStrategy : IMutationSpendingStrategy
    {
        private static readonly Random rng = new();

        public void SpendMutationPoints(Player player, List<Mutation> allMutations)
        {
            if (player == null || allMutations == null || player.MutationPoints <= 0)
                return;

            // 1. Group available mutations by tier
            var upgradableMutations = allMutations
                .Where(m => player.CanUpgrade(m))
                .GroupBy(m => m.Tier)
                .ToDictionary(g => g.Key, g => g.ToList());

            if (upgradableMutations.Count == 0)
                return;

            int maxTier = upgradableMutations.Keys.Max(t => (int)t);
            int totalPoints = player.MutationPoints;

            // 2. Compute tier weights (higher tiers get more)
            var tierWeights = new Dictionary<MutationTier, double>();
            double totalWeight = 0;

            foreach (var tier in upgradableMutations.Keys)
            {
                double weight = 1.0 / (1 + (maxTier - (int)tier));
                tierWeights[tier] = weight;
                totalWeight += weight;
            }

            // 3. Allocate points per tier
            var pointsByTier = new Dictionary<MutationTier, int>();
            int remainingPoints = totalPoints;

            foreach (var tier in tierWeights.OrderByDescending(kv => kv.Key))
            {
                int allocated = (int)Math.Ceiling(
                    (tier.Value / totalWeight) * totalPoints
                );

                allocated = Math.Min(allocated, remainingPoints);
                pointsByTier[tier.Key] = allocated;
                remainingPoints -= allocated;
            }

            // 4. Spend points per tier (higher first)
            foreach (var tier in pointsByTier.OrderByDescending(kv => kv.Key))
            {
                var tierMutations = upgradableMutations[tier.Key];

                while (pointsByTier[tier.Key] > 0 && player.MutationPoints > 0)
                {
                    var options = tierMutations
                        .Where(player.CanUpgrade)
                        .ToList();

                    if (options.Count == 0) break;

                    var pick = options[rng.Next(options.Count)];

                    if (player.TryUpgradeMutation(pick))
                    {
                        pointsByTier[tier.Key] -= pick.PointsPerUpgrade;
                    }
                    else
                    {
                        break; // avoid infinite loop
                    }
                }
            }
        }
    }
}
