using System;
using System.Collections.Generic;
using System.Linq;
using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Growth;
using FungusToast.Core.Metrics;
using FungusToast.Core.Mutations;
using FungusToast.Core.Players;
using FungusToast.Core.Phases;

namespace FungusToast.Core.AI
{
    public static class MutationSpendingHelper
    {
        public static bool ShouldReserveLatentPolymorphismPoints(Player player, int currentRound)
        {
            // The round cap is exclusive: with a cap of 75, playable rounds 72-74 are the final three.
            return player.GetMutationLevel(MutationIds.LatentPolymorphism) > 0
                && currentRound < GameBalance.MaxNumberOfRoundsBeforeGameEndTrigger
                    - GameBalance.LatentPolymorphismAiReserveDisabledFinalRounds;
        }

        public static void BankLatentPolymorphismReserveIfNeeded(
            Player player,
            int currentRound,
            ISimulationObserver simulationObserver)
        {
            if (!ShouldReserveLatentPolymorphismPoints(player, currentRound)
                || player.WantsToBankPointsThisTurn
                || player.MutationPoints < GameBalance.LatentPolymorphismAiMinimumBankedPoints)
            {
                return;
            }

            int pointsBanked = player.MutationPoints;
            player.WantsToBankPointsThisTurn = true;
            simulationObserver.RecordBankedPoints(player.PlayerId, pointsBanked);
            AdaptationEffectProcessor.OnMutationPointsBanked(player, pointsBanked);
            GeneticDriftMutationProcessor.OnMutationPointsBanked_LatentPolymorphism(player, pointsBanked, simulationObserver);
        }

        public static bool TrySpendRandomly(
            Player player,
            List<Mutation> allMutations,
            GameBoard board,
            Random rng,
            ISimulationObserver simulationObserver,
            int currentRound)
        {
            var eligible = allMutations
                .Where(m => player.CanUpgrade(m, currentRound, board))
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
            int cost = player.GetMutationPointCost(mutation);
            if (ShouldReserveLatentPolymorphismPoints(player, currentRound)
                && player.MutationPoints - cost < GameBalance.LatentPolymorphismAiMinimumBankedPoints)
            {
                return false;
            }

            if (mutation.Id == MutationIds.NecroticClearance
                && !MycelialSurgeMutationProcessor.HasNecroticClearanceEligibleTargets(player, board))
            {
                return false;
            }

            if (mutation.Id == MutationIds.ChemotacticBeacon)
            {
                int projectedLevel = Math.Min(player.GetMutationLevel(mutation.Id) + 1, mutation.MaxLevel);
                int? targetTileId = ChemotacticBeaconHelper.TrySelectAITargetTile(player, board, projectedLevel, mutation.SurgeDuration);
                return targetTileId.HasValue
                    && player.TryActivateTargetedSurge(mutation, board, targetTileId.Value, simulationObserver, currentRound);
            }

            return player.TryUpgradeMutation(mutation, simulationObserver, currentRound, board);
        }
    }
}
