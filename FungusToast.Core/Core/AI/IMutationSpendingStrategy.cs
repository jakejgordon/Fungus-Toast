using FungusToast.Core.Mutations;
using FungusToast.Core.Players;

namespace FungusToast.Core.AI
{
    public interface IMutationSpendingStrategy
    {
        string StrategyName { get; }
        MutationTier? MaxTier { get; }
        bool? PrioritizeHighTier { get; }
        bool? UsesGrowth { get; }
        bool? UsesCellularResilience { get; }
        bool? UsesFungicide { get; }
        bool? UsesGeneticDrift { get; }

        void SpendMutationPoints(Player player, List<Mutation> allMutations, GameBoard board);
    }
}
