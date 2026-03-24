using System;
using System.Collections.Generic;
using System.Linq;
using FungusToast.Core.Board;
using FungusToast.Core.Growth;
using FungusToast.Core.Metrics;
using FungusToast.Core.Mutations;
using FungusToast.Core.Players;

namespace FungusToast.Core.AI
{
    public static class MutationSpendingHelper
    {
        private static readonly Random rng = new();

        public static bool TrySpendRandomly(Player player, List<Mutation> allMutations, GameBoard board, ISimulationObserver simulationObserver, int currentRound)
        {
            var eligible = allMutations
                .Where(m => player.CanUpgrade(m, currentRound))
                .OrderBy(_ => rng.NextDouble())
                .ToList();

            foreach (var selected in eligible)
            {
                if (TryUpgradeWithTargeting(player, selected, board, simulationObserver, currentRound))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool TryUpgradeWithTargeting(Player player, Mutation mutation, GameBoard board, ISimulationObserver simulationObserver, int currentRound)
        {
            if (mutation.Id == MutationIds.ChemotacticBeacon)
            {
                int projectedLevel = Math.Min(player.GetMutationLevel(mutation.Id) + 1, mutation.MaxLevel);
                int? targetTileId = ChemotacticBeaconHelper.TrySelectAITargetTile(player, board, projectedLevel, mutation.SurgeDuration);
                return targetTileId.HasValue
                    && player.TryActivateTargetedSurge(mutation, board, targetTileId.Value, simulationObserver, currentRound);
            }

            return player.TryUpgradeMutation(mutation, simulationObserver, currentRound);
        }
    }
}
