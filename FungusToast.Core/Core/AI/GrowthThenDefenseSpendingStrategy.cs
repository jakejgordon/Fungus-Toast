using FungusToast.Core.Core.Mutations;
using FungusToast.Core.Mutations;
using FungusToast.Core.Players;

namespace FungusToast.Core.AI
{
    /// <summary>
    /// Improved AI: invest modestly in tier 1 growth (≤30), then prioritize any higher-tier unlocked mutations,
    /// especially in Growth and Cellular Resilience. Fallback to random upgrades if nothing is available.
    /// </summary>
    public class GrowthThenDefenseSpendingStrategy : MutationSpendingStrategyBase
    {
        public override string StrategyName { get; } = "LegacyGrowthThenDefense";

        public override MutationTier? MaxTier => null;

        public override bool? PrioritizeHighTier => false;

        public override bool? UsesGrowth => true;

        public override bool? UsesCellularResilience => true;

        public override bool? UsesFungicide => false;

        public override bool? UsesGeneticDrift => false;

        private const int MaxMycelialBloomLevel = 30;
        private const int MycelialBloomId = MutationIds.MycelialBloom;

        public override void SpendMutationPoints(Player player, List<Mutation> allMutations, GameBoard board)
        {
            if (player == null || allMutations == null || allMutations.Count == 0)
                return;

            bool spent;
            do
            {
                spent = false;

                // 1) Prioritize higher-tier unlocked mutations with prerequisites
                foreach (var m in allMutations
                             .Where(m => m.Prerequisites.Any() && player.CanUpgrade(m))
                             .OrderByDescending(m => m.EffectPerLevel))
                {
                    if (player.TryUpgradeMutation(m))
                    {
                        spent = true;
                        break;
                    }
                }

                // 2) Level up Mycelial Bloom to the cap
                var bloom = allMutations.FirstOrDefault(m => m.Id == MycelialBloomId);
                if (!spent && bloom != null &&
                    player.CanUpgrade(bloom) &&
                    player.GetMutationLevel(MycelialBloomId) < MaxMycelialBloomLevel)
                {
                    if (player.TryUpgradeMutation(bloom))
                    {
                        spent = true;
                    }
                }

                // 3) Fill in early Growth mutations (excluding Bloom)
                if (!spent)
                {
                    foreach (var m in allMutations
                                 .Where(m => m.Category == MutationCategory.Growth &&
                                             m.Id != MycelialBloomId &&
                                             player.CanUpgrade(m)))
                    {
                        if (player.TryUpgradeMutation(m))
                        {
                            spent = true;
                            break;
                        }
                    }
                }

                // 4) Try Cellular Resilience upgrades
                if (!spent)
                {
                    foreach (var m in allMutations
                                 .Where(m => m.Category == MutationCategory.CellularResilience &&
                                             player.CanUpgrade(m)))
                    {
                        if (player.TryUpgradeMutation(m))
                        {
                            spent = true;
                            break;
                        }
                    }
                }

                // 5) Fallback: random upgrade if nothing else is valid
                if (!spent)
                {
                    spent = MutationSpendingHelper.TrySpendRandomly(player, allMutations);
                }

            } while (spent && player.MutationPoints > 0);
        }
    }
}
