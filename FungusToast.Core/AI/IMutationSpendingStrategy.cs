using FungusToast.Core.Mutations;
using FungusToast.Core.Players;
using FungusToast.Core.Board;
using FungusToast.Core.Metrics;
using FungusToast.Core.Mycovariants;

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
        bool? UsesSubstrateEcology => null;
        /// <summary>
        /// Mutations intentionally excluded by an experimental strategy.  Random auto-upgrades
        /// must respect this set so a controlled treatment cannot reacquire its removed lever.
        /// </summary>
        IReadOnlyCollection<int> ExcludedMutationIds => Array.Empty<int>();

        /// <summary>
        /// Selects this strategy's mycovariant draft pick. Every strategy
        /// implementation must declare its own behavior so adding a new
        /// mutation spender cannot silently fall back to random drafting.
        /// </summary>
        Mycovariant SelectMycovariantFromChoices(
            Player player,
            List<Mycovariant> choices,
            GameBoard board,
            Random rnd);

        void SpendMutationPoints(Player player, List<Mutation> allMutations, GameBoard board,
            Random rnd, ISimulationObserver simulationObserver);
    }
}
