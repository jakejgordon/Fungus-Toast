using System;
using System.Collections.Generic;
using System.Linq;
using FungusToast.Core.Mutations;
using FungusToast.Core.Players;

namespace FungusToast.Core.AI
{
    public static class MutationSpendingHelper
    {
        private static readonly Random rng = new();

        public static bool TrySpendRandomly(Player player, List<Mutation> allMutations)
        {
            var eligible = allMutations
                .Where(m => player.CanUpgrade(m))
                .OrderBy(_ => rng.NextDouble())
                .ToList();

            if (eligible.Count == 0)
                return false;

            var selected = eligible[0];
            return player.TryUpgradeMutation(selected);
        }
    }
}
