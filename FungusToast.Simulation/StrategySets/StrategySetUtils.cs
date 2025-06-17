using System;
using System.Collections.Generic;
using System.Linq;
using FungusToast.Core.AI;

namespace FungusToast.Simulation.StrategySets
{
    public static class StrategySetUtils
    {
        public static List<IMutationSpendingStrategy> FillWithRandomStrategiesAndShuffle(
            List<IMutationSpendingStrategy> baseStrategies,
            int playerCount,
            Random rnd)
        {
            var result = new List<IMutationSpendingStrategy>(playerCount);

            // Add each base strategy once
            result.AddRange(baseStrategies);

            // Fill remaining slots with RandomMutationSpendingStrategy
            int remainder = playerCount - baseStrategies.Count;
            for (int i = 0; i < remainder; i++)
                result.Add(new RandomMutationSpendingStrategy());

            // Shuffle the list for fairness
            return result.OrderBy(_ => rnd.Next()).ToList();
        }
    }
}
