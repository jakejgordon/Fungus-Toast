using System.Collections.Generic;
using System.Linq;
using FungusToast.Core.Mutations;
using FungusToast.Core.Players;

namespace FungusToast.AI
{
    public class GrowthThenDefenseSpendingStrategy : IMutationSpendingStrategy
    {
        public void SpendMutationPoints(Player player, List<Mutation> allMutations)
        {
            bool spent;

            do
            {
                spent = false;

                var growth = allMutations
                    .Where(m => m.Category == MutationCategory.Growth && player.CanUpgrade(m));
                foreach (var m in growth)
                {
                    if (player.TryUpgradeMutation(m))
                    {
                        spent = true;
                        break;
                    }
                }

                if (!spent)
                {
                    var defense = allMutations
                        .Where(m => m.Category == MutationCategory.CellularResilience && player.CanUpgrade(m));
                    foreach (var m in defense)
                    {
                        if (player.TryUpgradeMutation(m))
                        {
                            spent = true;
                            break;
                        }
                    }
                }

                if (!spent)
                {
                    spent = MutationSpendingHelper.TrySpendRandomly(player, allMutations);
                }

            } while (spent && player.MutationPoints > 0);
        }
    }
}