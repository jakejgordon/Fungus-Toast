using FungusToast.Core.Mutations;
using FungusToast.Core.Players;
using System.Collections.Generic;

namespace FungusToast.Core.AI
{
    public interface IMutationSpendingStrategy
    {
        string StrategyName { get; }
        void SpendMutationPoints(Player player, List<Mutation> allMutations, GameBoard board);
    }
}
