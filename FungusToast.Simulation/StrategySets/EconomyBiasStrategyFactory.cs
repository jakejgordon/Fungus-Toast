using FungusToast.Core.AI;
using FungusToast.Core.Mutations;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FungusToast.Simulation.StrategySets
{
    public static class EconomyBiasStrategyFactory
    {
        public static List<IMutationSpendingStrategy> CreateEconomyBiasStrategies(
            int playerCount,
            Random rnd,
            List<int>? targetMutationIds = null,
            int surgeAttemptTurnFrequency = 10,
            bool prioritizeHighTier = true,
            MutationTier maxTier = MutationTier.Tier10
        )
        {
            var biases = Enum.GetValues(typeof(EconomyBias)).Cast<EconomyBias>();
            var baseStrategies = biases.Select(bias =>
                new ParameterizedSpendingStrategy(
                    strategyName: $"Economy Bias: {bias}",
                    prioritizeHighTier: prioritizeHighTier,
                    surgeAttemptTurnFrequency: surgeAttemptTurnFrequency,
                    maxTier: maxTier,
                    targetMutationIds: targetMutationIds ?? new List<int> { MutationIds.HyperadaptiveDrift, MutationIds.CreepingMold },
                    economyBias: bias
                )
            ).Cast<IMutationSpendingStrategy>().ToList();

            // Fill with RandomMutationSpendingStrategy if needed
            return StrategySetUtils.FillWithRandomStrategiesAndShuffle(baseStrategies, playerCount, rnd);
        }
    }
}
