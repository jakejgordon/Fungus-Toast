using FungusToast.Core.Mutations;
using FungusToast.Core.Players;
using FungusToast.Core.Board;
using FungusToast.Core.Metrics;

namespace FungusToast.Core.AI
{
    /// <summary>
    /// Fully random mutation spender. Makes no strategic choices.
    /// </summary>
    public class RandomMutationSpendingStrategy : IMutationSpendingStrategy
    {
        public string StrategyName { get; }

        public MutationTier? MaxTier => null;

        public bool? PrioritizeHighTier => false;

        public bool? UsesGrowth => true;

        public bool? UsesCellularResilience => true;

        public bool? UsesFungicide => true;

        public bool? UsesGeneticDrift => true;

        public RandomMutationSpendingStrategy(string? strategyName = null)
        {
            StrategyName = strategyName ?? "LegacyRandom";
        }

        public void SpendMutationPoints(Player player, List<Mutation> allMutations, GameBoard board,
            Random rnd, ISimulationObserver observer)
        {
            while (player.MutationPoints > 0)
            {
                if (!MutationSpendingHelper.TrySpendRandomly(player, allMutations, observer, board.CurrentRound))
                    break;
            }
        }
    }
}
