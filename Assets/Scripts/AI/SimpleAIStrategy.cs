using FungusToast.Core.Mutations;
using FungusToast.Core.Players;
using System.Collections.Generic;
using System.Linq;

public class SimpleAIStrategy : IMutationSpendingStrategy
{
    public void SpendMutationPoints(Player player, List<Mutation> availableMutations)
    {
        if (availableMutations == null || availableMutations.Count == 0)
            return;

        var affordable = availableMutations
            .Where(m => player.CanUpgrade(m))
            .OrderBy(m => m.PointsPerUpgrade)
            .ToList();

        foreach (var mutation in affordable)
        {
            player.TryUpgradeMutation(mutation);
        }
    }
}