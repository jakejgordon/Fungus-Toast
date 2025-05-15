using System;
using System.Collections.Generic;
using System.Linq;
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
                .GroupBy(m => GetTier(m))
                .ToDictionary(g => g.Key, g => g.ToList());

            if (upgradableMutations.Count == 0)
                return;

            int maxTier = upgradableMutations.Keys.Max();
            int totalPoints = player.MutationPoints;

            // 2. Compute tier weights (higher tier gets more)
            var tierWeights = new Dictionary<int, double>();
            double totalWeight = 0;

            foreach (var tier in upgradableMutations.Keys)
            {
                double weight = 1.0 / (1 + (maxTier - tier)); // new softer weight curve
                tierWeights[tier] = weight;
                totalWeight += weight;
            }


            // 3. Compute point allocation per tier
            var pointsByTier = new Dictionary<int, int>();
            int remainingPoints = totalPoints;

            foreach (var tier in tierWeights.OrderByDescending(kv => kv.Key)) // spend high tiers first
            {
                int allocated = (int)Math.Ceiling((tier.Value / totalWeight) * totalPoints);
                allocated = Math.Min(allocated, remainingPoints);
                pointsByTier[tier.Key] = allocated;
                remainingPoints -= allocated;
            }

            // 4. Spend per tier (higher first)
            foreach (var tier in pointsByTier.OrderByDescending(kv => kv.Key))
            {
                var tierMutations = upgradableMutations[tier.Key];
                while (pointsByTier[tier.Key] > 0 && player.MutationPoints > 0)
                {
                    var options = tierMutations.Where(player.CanUpgrade).ToList();
                    if (options.Count == 0) break;

                    var pick = options[rng.Next(options.Count)];
                    if (player.TryUpgradeMutation(pick))
                    {
                        pointsByTier[tier.Key] -= pick.PointsPerUpgrade;
                    }
                    else
                    {
                        break; // prevent infinite loop if nothing is upgradable
                    }
                }
            }
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
            // We assume the mutation tree is static — so it's safe to do:
            var parent = MutationRegistry.GetById(prereq.MutationId);
            if (parent == null || parent.Prerequisites.Count == 0) return 0;
            return 1 + parent.Prerequisites.Max(p => GetMaxTierDepth(p));
        }
    }
}
