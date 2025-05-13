using System.Collections.Generic;
using FungusToast.Core.Mutations;
using FungusToast.Core.Players;

namespace FungusToast.Core.AI
{
    public class RandomMutationSpendingStrategy : IMutationSpendingStrategy
    {
        public void SpendMutationPoints(Player player, List<Mutation> allMutations)
        {
            while (player.MutationPoints > 0)
            {
                if (!MutationSpendingHelper.TrySpendRandomly(player, allMutations))
                    break;
            }
        }
    }
}