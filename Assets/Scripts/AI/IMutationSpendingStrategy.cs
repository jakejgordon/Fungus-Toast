using FungusToast.Core.Mutations;
using FungusToast.Core.Players;
using System.Collections.Generic;
using UnityEngine;

public interface IMutationSpendingStrategy
{
    void SpendMutationPoints(Player player, List<Mutation> availableMutations);
}