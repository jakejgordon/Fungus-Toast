using FungusToast.Core.Mutations;
using FungusToast.Core.Players;

namespace FungusToast.Core.AI
{
    /// <summary>
    /// Fully random mutation spender. Makes no strategic choices.
    /// </summary>
    public class RandomMutationSpendingStrategy : IMutationSpendingStrategy
    {
        public string StrategyName { get; } = "LegacyRandom";

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
