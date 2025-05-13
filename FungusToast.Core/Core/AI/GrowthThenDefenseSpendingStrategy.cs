using System.Collections.Generic;
using System.Linq;
using FungusToast.Core.Mutations;
using FungusToast.Core.Players;

namespace FungusToast.Core.AI
{
    /// <summary>
    /// Simple AI: spend points on Growth upgrades first, then Cellular-Resilience,
    /// and finally fall back to a random pick if nothing else is affordable.
    /// </summary>
    public class GrowthThenDefenseSpendingStrategy : IMutationSpendingStrategy
    {
        public void SpendMutationPoints(Player player, List<Mutation> allMutations)
        {
            if (player == null || allMutations == null || allMutations.Count == 0)
                return;

            bool spent;
            do
            {
                spent = false;

                /* --- 1) Prioritise Growth mutations ---------------------- */
                foreach (var m in allMutations
                                 .Where(m => m.Category == MutationCategory.Growth && player.CanUpgrade(m)))
                {
                    if (player.TryUpgradeMutation(m))
                    {
                        spent = true;
                        break;
                    }
                }

                /* --- 2) If none, try Cellular Resilience ---------------- */
                if (!spent)
                {
                    foreach (var m in allMutations
                                     .Where(m => m.Category == MutationCategory.CellularResilience && player.CanUpgrade(m)))
                    {
                        if (player.TryUpgradeMutation(m))
                        {
                            spent = true;
                            break;
                        }
                    }
                }

                /* --- 3) Fallback: random affordable mutation ------------- */
                if (!spent)
                {
                    spent = MutationSpendingHelper.TrySpendRandomly(player, allMutations);
                }

            } while (spent && player.MutationPoints > 0);
        }
    }
}
