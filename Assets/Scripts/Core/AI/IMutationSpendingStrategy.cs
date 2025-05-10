using FungusToast.Core.Mutations;
using FungusToast.Core.Players;
using System.Collections.Generic;

namespace FungusToast.Core.AI
{
    public interface IMutationSpendingStrategy
    {
        void SpendMutationPoints(Player player, List<Mutation> availableMutations);
    }
}
