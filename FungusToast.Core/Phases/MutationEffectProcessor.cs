// FungusToast.Core/Phases/MutationEffectProcessor.cs
using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Death;
using FungusToast.Core.Events;
using FungusToast.Core.Growth;
using FungusToast.Core.Metrics;
using FungusToast.Core.Mutations;
using FungusToast.Core.Phases;
using FungusToast.Core.Players;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FungusToast.Core.Phases
{
    /// <summary>
    /// DEPRECATED: This class has been split into category-specific processors.
    /// Use MutationEffectCoordinator instead, which delegates to:
    /// - GrowthMutationProcessor
    /// - CellularResilienceMutationProcessor  
    /// - FungicideMutationProcessor
    /// - GeneticDriftMutationProcessor
    /// - MycelialSurgeMutationProcessor
    /// 
    /// This class remains only for backward compatibility and delegates all calls to the coordinator.
    /// </summary>
    [Obsolete("Use MutationEffectCoordinator and category-specific processors instead")]
    public static class MutationEffectProcessor
    {
        public static DeathCalculationResult CalculateDeathChance(
            Player owner,
            FungalCell cell,
            GameBoard board,
            List<Player> allPlayers,
            double roll,
            Random rng,
            ISimulationObserver? observer = null) =>
            MutationEffectCoordinator.CalculateDeathChance(owner, cell, board, allPlayers, roll, rng, observer);

        public static void AdvanceOrResetCellAge(Player player, FungalCell cell) =>
            MutationEffectCoordinator.AdvanceOrResetCellAge(player, cell);

        public static void TryApplyMutatorPhenotype(
            Player player,
            List<Mutation> allMutations,
            Random rng,
            int currentRound,
            ISimulationObserver? observer = null) =>
            MutationEffectCoordinator.TryApplyMutatorPhenotype(player, allMutations, rng, currentRound, observer);

        public static float GetTendrilDiagonalGrowthMultiplier(Player player) =>
            MutationEffectCoordinator.GetTendrilDiagonalGrowthMultiplier(player);

        public static bool TryCreepingMoldMove(
            Player player,
            FungalCell sourceCell,
            BoardTile sourceTile,
            BoardTile targetTile,
            Random rng,
            GameBoard board,
            ISimulationObserver? observer = null) =>
            MutationEffectCoordinator.TryCreepingMoldMove(player, sourceCell, sourceTile, targetTile, rng, board, observer);

        public static float GetNecrophyticBloomDamping(float occupiedPercent) =>
            MutationEffectCoordinator.GetNecrophyticBloomDamping(occupiedPercent);

        public static void TriggerNecrophyticBloomInitialBurst(
            Player player,
            GameBoard board,
            Random rng,
            ISimulationObserver? observer = null) =>
            MutationEffectCoordinator.TriggerNecrophyticBloomInitialBurst(player, board, rng, observer);

        public static void TriggerNecrophyticBloomOnCellDeath(
           Player owner,
           GameBoard board,
           Random rng,
           float occupiedPercent,
           ISimulationObserver? observer = null) =>
            MutationEffectCoordinator.TriggerNecrophyticBloomOnCellDeath(owner, board, rng, occupiedPercent, observer);

        public static int ApplyMycotoxinTracer(
            Player player,
            GameBoard board,
            int failedGrowthsThisRound,
            List<Player> allPlayers,
            Random rng,
            ISimulationObserver? observer = null) =>
            MutationEffectCoordinator.ApplyMycotoxinTracer(player, board, failedGrowthsThisRound, allPlayers, rng, observer);

        public static void ApplyToxinAuraDeaths(GameBoard board,
                                         List<Player> players,
                                         Random rng,
                                         ISimulationObserver? simulationObserver = null) =>
            MutationEffectCoordinator.ApplyToxinAuraDeaths(board, players, rng, simulationObserver);

        public static int ApplyMycotoxinCatabolism(
            Player player,
            GameBoard board,
            Random rng,
            RoundContext roundContext,
            ISimulationObserver? observer = null) =>
            MutationEffectCoordinator.ApplyMycotoxinCatabolism(player, board, rng, roundContext, observer);

        public static bool TryNecrohyphalInfiltration(
            GameBoard board,
            BoardTile sourceTile,
            FungalCell sourceCell,
            Player owner,
            Random rng,
            ISimulationObserver? observer = null) =>
            MutationEffectCoordinator.TryNecrohyphalInfiltration(board, sourceTile, sourceCell, owner, rng, observer);

        public static (float baseChance, float surgeBonus) GetGrowthChancesWithHyphalSurge(Player player) =>
            MutationEffectCoordinator.GetGrowthChancesWithHyphalSurge(player);

        public static void ProcessHyphalVectoring(
            GameBoard board,
            List<Player> players,
            Random rng,
            ISimulationObserver? observer = null) =>
            MutationEffectCoordinator.ProcessHyphalVectoring(board, players, rng, observer);

        public static void OnNecrophyticBloomActivated(
            GameBoard board,
            List<Player> players,
            Random rng,
            ISimulationObserver? observer = null) =>
            MutationEffectCoordinator.OnNecrophyticBloomActivated(board, players, rng, observer);

        public static void OnMutationPhaseStart_MutatorPhenotype(
            GameBoard board,
            List<Player> players,
            List<Mutation> allMutations,
            Random rng,
            int currentRound,
            ISimulationObserver? observer = null) =>
            MutationEffectCoordinator.OnMutationPhaseStart_MutatorPhenotype(board, players, allMutations, rng, currentRound, observer);

        public static void OnToxinExpired_CatabolicRebirth(
            ToxinExpiredEventArgs eventArgs,
            GameBoard board,
            List<Player> players,
            Random rng,
            ISimulationObserver? observer = null) =>
            MutationEffectCoordinator.OnToxinExpired_CatabolicRebirth(eventArgs, board, players, rng, observer);

        public static void OnCellDeath_PutrefactiveRejuvenation(
            FungalCellDiedEventArgs eventArgs,
            GameBoard board,
            List<Player> players,
            ISimulationObserver? observer = null) =>
            MutationEffectCoordinator.OnCellDeath_PutrefactiveRejuvenation(eventArgs, board, players, observer);

        public static void OnCellDeath_PutrefactiveCascade(
            FungalCellDiedEventArgs eventArgs,
            GameBoard board,
            List<Player> players,
            Random rng,
            ISimulationObserver? observer = null) =>
            MutationEffectCoordinator.OnCellDeath_PutrefactiveCascade(eventArgs, board, players, rng, observer);

        public static void OnCellDeath_NecrotoxicConversion(
            FungalCellDiedEventArgs eventArgs,
            GameBoard board,
            List<Player> players,
            Random rng,
            ISimulationObserver? observer = null) =>
            MutationEffectCoordinator.OnCellDeath_NecrotoxicConversion(eventArgs, board, players, rng, observer);

        // Consolidated event handlers
        public static void OnCellDeath(
            FungalCellDiedEventArgs eventArgs,
            GameBoard board,
            List<Player> players,
            Random rng,
            ISimulationObserver? observer = null) =>
            MutationEffectCoordinator.OnCellDeath(eventArgs, board, players, rng, observer);

        public static void OnPreGrowthPhase(
            GameBoard board,
            List<Player> players,
            Random rng,
            RoundContext roundContext,
            ISimulationObserver? observer = null) =>
            MutationEffectCoordinator.OnPreGrowthPhase(board, players, rng, roundContext, observer);

        public static void OnPostGrowthPhase(
            GameBoard board,
            List<Player> players,
            Random rng,
            ISimulationObserver? observer = null) =>
            MutationEffectCoordinator.OnPostGrowthPhase(board, players, rng, observer);

        public static void OnDecayPhase(
            GameBoard board,
            List<Player> players,
            Random rng,
            ISimulationObserver? observer = null) =>
            MutationEffectCoordinator.OnDecayPhase(board, players, rng, observer);

        // Individual pre-growth phase methods (for backward compatibility)
        public static void OnPreGrowthPhase_MycotoxinCatabolism(
            GameBoard board,
            List<Player> players,
            Random rng,
            RoundContext roundContext,
            ISimulationObserver? observer = null) =>
            MutationEffectCoordinator.OnPreGrowthPhase_MycotoxinCatabolism(board, players, rng, roundContext, observer);

        public static void OnPreGrowthPhase_ChitinFortification(
            GameBoard board,
            List<Player> players,
            Random rng,
            ISimulationObserver? observer = null) =>
            MutationEffectCoordinator.OnPreGrowthPhase_ChitinFortification(board, players, rng, observer);

        // Individual post-growth phase methods (for backward compatibility)
        public static void OnPostGrowthPhase_RegenerativeHyphae(
            GameBoard board,
            List<Player> players,
            Random rng,
            ISimulationObserver? observer = null) =>
            MutationEffectCoordinator.OnPostGrowthPhase_RegenerativeHyphae(board, players, rng, observer);

        public static void OnPostGrowthPhase_HyphalVectoring(
            GameBoard board,
            List<Player> players,
            Random rng,
            ISimulationObserver? observer = null) =>
            MutationEffectCoordinator.OnPostGrowthPhase_HyphalVectoring(board, players, rng, observer);

        // Individual decay phase methods (for backward compatibility)
        public static void OnDecayPhase_SporocidalBloom(
            GameBoard board,
            List<Player> players,
            Random rng,
            ISimulationObserver? observer = null) =>
            MutationEffectCoordinator.OnDecayPhase_SporocidalBloom(board, players, rng, observer);

        public static void OnDecayPhase_MycotoxinTracer(
            GameBoard board,
            List<Player> players,
            Dictionary<int, int> failedGrowthsByPlayerId,
            Random rng,
            ISimulationObserver? observer = null) =>
            MutationEffectCoordinator.OnDecayPhase_MycotoxinTracer(board, players, failedGrowthsByPlayerId, rng, observer);

        public static void OnDecayPhase_MycotoxinPotentiation(
            GameBoard board,
            List<Player> players,
            Random rng,
            ISimulationObserver? observer = null) =>
            MutationEffectCoordinator.OnDecayPhase_MycotoxinPotentiation(board, players, rng, observer);
    }
}
