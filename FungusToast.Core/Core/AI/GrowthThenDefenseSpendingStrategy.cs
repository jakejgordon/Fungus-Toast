using System.Collections.Generic;
using System.Linq;
using FungusToast.Core.Mutations;
using FungusToast.Core.Players;

namespace FungusToast.Core.AI
{
    /// <summary>
    /// Improved AI: invest modestly in tier 1 growth (≤30), then prioritize any higher-tier unlocked mutations,
    /// especially in Growth and Cellular Resilience. Fallback to random upgrades if nothing is available.
    /// </summary>
    public class GrowthThenDefenseSpendingStrategy : IMutationSpendingStrategy
    {
        private const int MaxMycelialBloomLevel = 30;
        private const int MycelialBloomId = MutationIds.MycelialBloom;

        public void SpendMutationPoints(Player player, List<Mutation> allMutations)
        {
            if (player == null || allMutations == null || allMutations.Count == 0)
                return;

            bool spent;
            do
            {
                spent = false;

                // --- 1) Prioritize higher-tier mutations (any category), prerequisites must be met ---
                foreach (var m in allMutations
                                 .Where(m => m.Prerequisites.Any() && player.CanUpgrade(m))
                                 .OrderByDescending(m => m.EffectPerLevel)) // prefer stronger effects first
                {
                    if (player.TryUpgradeMutation(m))
                    {
                        spent = true;
                        break;
                    }
                }

                // --- 2) Invest in Mycelial Bloom up to max threshold ---
                var bloom = allMutations.FirstOrDefault(m => m.Id == MycelialBloomId);
                if (!spent && bloom != null && player.CanUpgrade(bloom) &&
                    player.GetMutationLevel(MycelialBloomId) < MaxMycelialBloomLevel)
                {
                    if (player.TryUpgradeMutation(bloom))
                    {
                        spent = true;
                    }
                }

                // --- 3) Fill in early Growth mutations (excluding Bloom) if affordable ---
                if (!spent)
                {
                    foreach (var m in allMutations
                                     .Where(m => m.Category == MutationCategory.Growth && m.Id != MycelialBloomId && player.CanUpgrade(m)))
                    {
                        if (player.TryUpgradeMutation(m))
                        {
                            spent = true;
                            break;
                        }
                    }
                }

                // --- 4) Try Cellular Resilience upgrades ---
                if (!spent)
                {
                    foreach (var m in allMutations
                                     .Where(m => m.Category == MutationCategory.CellularResilience && player.CanUpgrade(m)))
                    {
                        if (player.TryUpgradeMutation(m))
                        {
                            spent = true;
                            break;
                        }
                    }
                }

                // --- 5) Fallback: random upgrade if nothing else is valid ---
                if (!spent)
                {
                    spent = MutationSpendingHelper.TrySpendRandomly(player, allMutations);
                }

            } while (spent && player.MutationPoints > 0);
        }
    }
}
