using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Death;
using FungusToast.Core.Events;
using FungusToast.Core.Metrics;
using FungusToast.Core.Mutations;
using FungusToast.Core.Players;
using System;
using System.Collections.Generic;

namespace FungusToast.Core.Phases
{
    /// <summary>
    /// Coordinates calls to all category-specific mutation processors.
    /// This replaces the original monolithic MutationEffectProcessor.
    /// </summary>
    public static class MutationEffectCoordinator
    {
        /// <summary>
        /// Calculates death chance for a cell, including all mutation effects.
        /// </summary>
        public static DeathCalculationResult CalculateDeathChance(
            Player owner,
            FungalCell cell,
            GameBoard board,
            List<Player> allPlayers,
            double roll,
            Random rng,
            ISimulationObserver? observer = null)
        {
            float harmonyReduction = owner.GetMutationEffect(MutationType.DefenseSurvival);
            float ageDelay = owner.GetMutationEffect(MutationType.SelfAgeResetThreshold);

            float baseChance = Math.Max(0f, GameBalance.BaseDeathChance - harmonyReduction);

            float ageComponent = cell.GrowthCycleAge > ageDelay
                ? (cell.GrowthCycleAge - ageDelay) * GameBalance.AgeDeathFactorPerGrowthCycle
                : 0f;

            float ageChance = Math.Max(0f, ageComponent - harmonyReduction);

            float totalFallbackChance = Math.Clamp(baseChance + ageChance, 0f, 1f);
            float thresholdRandom = baseChance;
            float thresholdAge = baseChance + ageChance;

            if (roll < totalFallbackChance)
            {
                if (roll < thresholdRandom)
                {
                    return DeathCalculationResult.Death(totalFallbackChance, DeathReason.Randomness);
                }

                return DeathCalculationResult.Death(totalFallbackChance, DeathReason.Age);
            }

            // Check Putrefactive Mycotoxin (Fungicide category)
            if (FungicideMutationProcessor.CheckPutrefactiveMycotoxin(cell, board, allPlayers, roll, out float pmChance, out int? killerPlayerId, out int? attackerTileId, rng, observer))
            {
                return DeathCalculationResult.Death(pmChance, DeathReason.PutrefactiveMycotoxin, killerPlayerId, attackerTileId);
            }

            return DeathCalculationResult.NoDeath(totalFallbackChance);
        }

        #region Phase Event Orchestration

        // Cell Death Events
        /// <summary>
        /// Handles all mutation effects that trigger on cell death.
        /// Calls each mutation-specific handler in the appropriate order.
        /// Note: This only handles mutation-based effects. Mycovariant effects should be handled separately.
        /// </summary>
        public static void OnCellDeath(
            FungalCellDiedEventArgs eventArgs,
            GameBoard board,
            List<Player> players,
            Random rng,
            ISimulationObserver? observer = null)
        {
            // Order matters here - some effects may depend on the results of others
            
            // 1. Necrotoxic Conversion (toxin death reclamation - should happen first to potentially reclaim cells)
            FungicideMutationProcessor.OnCellDeath_NecrotoxicConversion(eventArgs, board, players, rng, observer);
            
            // 2. Putrefactive Rejuvenation (age reduction from kills - should happen before cascades)
            FungicideMutationProcessor.OnCellDeath_PutrefactiveRejuvenation(eventArgs, board, players, observer);
            
            // 3. Putrefactive Cascade (directional kill chains - should happen last to avoid affecting other mutations)
            FungicideMutationProcessor.OnCellDeath_PutrefactiveCascade(eventArgs, board, players, rng, observer);
        }

        // Mutation Phase Events
        public static void OnMutationPhaseStart_MutatorPhenotype(
            GameBoard board,
            List<Player> players,
            List<Mutation> allMutations,
            Random rng,
            int currentRound,
            ISimulationObserver? observer = null)
        {
            GeneticDriftMutationProcessor.OnMutationPhaseStart_MutatorPhenotype(board, players, allMutations, rng, currentRound, observer);
        }

        // Pre-Growth Phase Events
        public static void OnPreGrowthPhase_MycotoxinCatabolism(
            GameBoard board,
            List<Player> players,
            Random rng,
            RoundContext roundContext,
            ISimulationObserver? observer = null)
        {
            GeneticDriftMutationProcessor.OnPreGrowthPhase_MycotoxinCatabolism(board, players, rng, roundContext, observer);
        }

        public static void OnPreGrowthPhase_ChitinFortification(
            GameBoard board,
            List<Player> players,
            Random rng,
            ISimulationObserver? observer = null)
        {
            MycelialSurgeMutationProcessor.OnPreGrowthPhase_ChitinFortification(board, players, rng, observer);
        }

        // Post-Growth Phase Events  
        public static void OnPostGrowthPhase_RegenerativeHyphae(
            GameBoard board,
            List<Player> players,
            Random rng,
            ISimulationObserver? observer = null)
        {
            CellularResilienceMutationProcessor.OnPostGrowthPhase_RegenerativeHyphae(board, players, rng, observer);
        }

        public static void OnPostGrowthPhase_HyphalVectoring(
            GameBoard board,
            List<Player> players,
            Random rng,
            ISimulationObserver? observer = null)
        {
            MycelialSurgeMutationProcessor.OnPostGrowthPhase_HyphalVectoring(board, players, rng, observer);
        }

        // Decay Phase Events
        public static void OnDecayPhase_SporocidalBloom(
            GameBoard board,
            List<Player> players,
            Random rng,
            ISimulationObserver? observer = null)
        {
            FungicideMutationProcessor.OnDecayPhase_SporocidalBloom(board, players, rng, observer);
        }

        public static void OnDecayPhase_MycotoxinTracer(
            GameBoard board,
            List<Player> players,
            Dictionary<int, int> failedGrowthsByPlayerId,
            Random rng,
            ISimulationObserver? observer = null)
        {
            FungicideMutationProcessor.OnDecayPhase_MycotoxinTracer(board, players, failedGrowthsByPlayerId, rng, observer);
        }

        public static void OnDecayPhase_MycotoxinPotentiation(
            GameBoard board,
            List<Player> players,
            Random rng,
            ISimulationObserver? observer = null)
        {
            FungicideMutationProcessor.OnDecayPhase_MycotoxinPotentiation(board, players, rng, observer);
        }

        // Special Events
        public static void OnNecrophyticBloomActivated(
            GameBoard board,
            List<Player> players,
            Random rng,
            ISimulationObserver? observer = null)
        {
            GeneticDriftMutationProcessor.OnNecrophyticBloomActivated(board, players, rng, observer);
        }

        public static void OnToxinExpired_CatabolicRebirth(
            ToxinExpiredEventArgs eventArgs,
            GameBoard board,
            List<Player> players,
            Random rng,
            ISimulationObserver? observer = null)
        {
            CellularResilienceMutationProcessor.OnToxinExpired_CatabolicRebirth(eventArgs, board, players, rng, observer);
        }

        #endregion

        #region Individual Cell Death Methods (for backward compatibility)

        public static void OnCellDeath_NecrotoxicConversion(
            FungalCellDiedEventArgs eventArgs,
            GameBoard board,
            List<Player> players,
            Random rng,
            ISimulationObserver? observer = null)
        {
            FungicideMutationProcessor.OnCellDeath_NecrotoxicConversion(eventArgs, board, players, rng, observer);
        }

        public static void OnCellDeath_PutrefactiveRejuvenation(
            FungalCellDiedEventArgs eventArgs,
            GameBoard board,
            List<Player> players,
            ISimulationObserver? observer = null)
        {
            FungicideMutationProcessor.OnCellDeath_PutrefactiveRejuvenation(eventArgs, board, players, observer);
        }

        public static void OnCellDeath_PutrefactiveCascade(
            FungalCellDiedEventArgs eventArgs,
            GameBoard board,
            List<Player> players,
            Random rng,
            ISimulationObserver? observer = null)
        {
            FungicideMutationProcessor.OnCellDeath_PutrefactiveCascade(eventArgs, board, players, rng, observer);
        }

        #endregion

        #region Delegation Methods

        // Growth category methods
        public static float GetTendrilDiagonalGrowthMultiplier(Player player) =>
            GrowthMutationProcessor.GetTendrilDiagonalGrowthMultiplier(player);

        public static bool TryCreepingMoldMove(
            Player player,
            FungalCell sourceCell,
            BoardTile sourceTile,
            BoardTile targetTile,
            Random rng,
            GameBoard board,
            ISimulationObserver? observer = null) =>
            GrowthMutationProcessor.TryCreepingMoldMove(player, sourceCell, sourceTile, targetTile, rng, board, observer);

        public static (float baseChance, float surgeBonus) GetGrowthChancesWithHyphalSurge(Player player) =>
            GrowthMutationProcessor.GetGrowthChancesWithHyphalSurge(player);

        // Cellular Resilience category methods
        public static void AdvanceOrResetCellAge(Player player, FungalCell cell) =>
            CellularResilienceMutationProcessor.AdvanceOrResetCellAge(player, cell);

        public static bool TryNecrohyphalInfiltration(
            GameBoard board,
            BoardTile sourceTile,
            FungalCell sourceCell,
            Player owner,
            Random rng,
            ISimulationObserver? observer = null) =>
            CellularResilienceMutationProcessor.TryNecrohyphalInfiltration(board, sourceTile, sourceCell, owner, rng, observer);

        // Genetic Drift category methods
        public static void TryApplyMutatorPhenotype(
            Player player,
            List<Mutation> allMutations,
            Random rng,
            int currentRound,
            ISimulationObserver? observer = null) =>
            GeneticDriftMutationProcessor.TryApplyMutatorPhenotype(player, allMutations, rng, currentRound, observer);

        public static int ApplyMycotoxinCatabolism(
            Player player,
            GameBoard board,
            Random rng,
            RoundContext roundContext,
            ISimulationObserver? observer = null) =>
            GeneticDriftMutationProcessor.ApplyMycotoxinCatabolism(player, board, rng, roundContext, observer);

        public static float GetNecrophyticBloomDamping(float occupiedPercent) =>
            GeneticDriftMutationProcessor.GetNecrophyticBloomDamping(occupiedPercent);

        public static void TriggerNecrophyticBloomInitialBurst(
            Player player,
            GameBoard board,
            Random rng,
            ISimulationObserver? observer = null) =>
            GeneticDriftMutationProcessor.TriggerNecrophyticBloomInitialBurst(player, board, rng, observer);

        public static void TriggerNecrophyticBloomOnCellDeath(
           Player owner,
           GameBoard board,
           Random rng,
           float occupiedPercent,
           ISimulationObserver? observer = null) =>
            GeneticDriftMutationProcessor.TriggerNecrophyticBloomOnCellDeath(owner, board, rng, occupiedPercent, observer);

        // Fungicide category methods
        public static void ApplyToxinAuraDeaths(GameBoard board,
                                         List<Player> players,
                                         Random rng,
                                         ISimulationObserver? simulationObserver = null) =>
            FungicideMutationProcessor.ApplyToxinAuraDeaths(board, players, rng, simulationObserver);

        public static int ApplyMycotoxinTracer(
            Player player,
            GameBoard board,
            int failedGrowthsThisRound,
            List<Player> allPlayers,
            Random rng,
            ISimulationObserver? observer = null) =>
            FungicideMutationProcessor.ApplyMycotoxinTracer(player, board, failedGrowthsThisRound, allPlayers, rng, observer);

        // Mycelial Surge category methods
        public static void ProcessHyphalVectoring(
            GameBoard board,
            List<Player> players,
            Random rng,
            ISimulationObserver? observer = null) =>
            MycelialSurgeMutationProcessor.ProcessHyphalVectoring(board, players, rng, observer);

        #endregion
    }
}