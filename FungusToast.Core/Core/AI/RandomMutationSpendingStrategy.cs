using FungusToast.Core.Mutations;
using FungusToast.Core.Players;
using FungusToast.Core.Board;

namespace FungusToast.Core.AI
{
    /// <summary>
    /// Fully random mutation spender. Makes no strategic choices.
    /// </summary>
    public class RandomMutationSpendingStrategy : IMutationSpendingStrategy
    {
        public string StrategyName { get; } = "LegacyRandom";
        public MutationTier? MaxTier => null;

        public bool? PrioritizeHighTier => false;

        public bool? UsesGrowth => true;

        public bool? UsesCellularResilience => true;

        public bool? UsesFungicide => true;

        public bool? UsesGeneticDrift => true;

        public void SpendMutationPoints(Player player, List<Mutation> allMutations, GameBoard board)
        {
            while (player.MutationPoints > 0)
            {
                if (!MutationSpendingHelper.TrySpendRandomly(player, allMutations))
                    break;
            }
        }
    }
}
