using System.Collections.Generic;
using System.Linq;
using FungusToast.Core.Mutations;
using FungusToast.Core.Players;

namespace FungusToast.Game
{
    public class RandomMutationSpendingStrategy : IMutationSpendingStrategy
    {
        private static readonly System.Random rng = new System.Random();

        public void SpendMutationPoints(Player player, List<Mutation> allMutations)
        {
            if (player == null || allMutations == null || allMutations.Count == 0)
                return;

            // Keep trying to spend points until we can't
            bool spent;
            do
            {
                spent = false;

                // Filter mutations the player is eligible to upgrade
                List<Mutation> eligibleMutations = allMutations
                    .Where(m => player.CanUpgrade(m))
                    .ToList();

                if (eligibleMutations.Count > 0)
                {
                    // Pick one at random
                    Mutation selected = eligibleMutations[rng.Next(eligibleMutations.Count)];

                    // Spend a point
                    if (player.TryUpgradeMutation(selected))
                    {
                        spent = true;
                    }
                }

            } while (spent && player.MutationPoints > 0);
        }
    }
}
