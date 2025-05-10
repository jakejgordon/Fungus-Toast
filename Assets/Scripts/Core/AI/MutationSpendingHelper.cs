using System.Collections.Generic;
using System.Linq;
using FungusToast.Core.Mutations;
using FungusToast.Core.Players;
using UnityEngine;

namespace FungusToast.Core.AI
{
    public static class MutationSpendingHelper
    {
        public static bool TrySpendRandomly(Player player, List<Mutation> allMutations)
        {
            var eligible = allMutations
                .Where(m => player.CanUpgrade(m))
                .OrderBy(_ => Random.value)
                .ToList();

            if (eligible.Count == 0)
                return false;

            var selected = eligible.First();
            return player.TryUpgradeMutation(selected);
        }
    }
}