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
            ISimulationObserver observer)
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
            ISimulationObserver observer)
        {
            // Order matters here - some effects may depend on the results of others
            
            // 1. Necrotoxic Conversion (toxin death reclamation - should happen first to potentially reclaim cells)
            FungicideMutationProcessor.OnCellDeath_NecrotoxicConversion(eventArgs, board, players, rng, observer);
            
            // 2. Putrefactive Rejuvenation (age reduction from kills - should happen before cascades)
            FungicideMutationProcessor.OnCellDeath_PutrefactiveRejuvenation(eventArgs, board, players, observer);
            
            // 3. Putrefactive Cascade (directional kill chains - should happen last to avoid affecting other mutations)
            FungicideMutationProcessor.OnCellDeath_PutrefactiveCascade(eventArgs, board, players, rng, observer);
        }

        // Pre-Growth Phase Events
        /// <summary>
        /// Handles all mutation effects that trigger before the growth phase.
        /// Calls each mutation-specific handler in the appropriate order.
        /// </summary>
        public static void OnPreGrowthPhase(
            GameBoard board,
            List<Player> players,
            Random rng,
            ISimulationObserver observer)
        {
            // Order matters here - some effects may interact with others
            
            // 1. Mycotoxin Catabolism (toxin processing - should happen first to clear toxins before surge effects)
            GeneticDriftMutationProcessor.OnPreGrowthPhase_MycotoxinCatabolism(board, players, rng, board.CurrentRoundContext, observer);
            
            // 2. Chitin Fortification (surge effect - should happen after toxin processing to maximize benefit)
            MycelialSurgeMutationProcessor.OnPreGrowthPhase_ChitinFortification(board, players, rng, observer);
        }

        // Post-Growth Phase Events
        /// <summary>
        /// Handles all mutation effects that trigger after the growth phase.
        /// Calls each mutation-specific handler in the appropriate order.
        /// </summary>
        public static void OnPostGrowthPhase(
            GameBoard board,
            List<Player> players,
            Random rng,
            ISimulationObserver observer)
        {
            // Order matters here - some effects may depend on the results of others
            
            // 1. Regenerative Hyphae (reclaim own dead cells - should happen first to expand territory)
            CellularResilienceMutationProcessor.OnPostGrowthPhase_RegenerativeHyphae(board, players, rng, observer);
            
            // 2. Hyphal Vectoring (surge effect - should happen after reclamation to maximize impact)
            MycelialSurgeMutationProcessor.OnPostGrowthPhase_HyphalVectoring(board, players, rng, observer);
            
            // 3. Mimetic Resilience (surge effect - should happen after other effects to ensure accurate targeting)
            MycelialSurgeMutationProcessor.OnPostGrowthPhase_MimeticResilience(board, players, rng, observer);
        }

        // Decay Phase Events
        /// <summary>
        /// Handles all mutation effects that trigger during the decay phase.
        /// Calls each mutation-specific handler in the appropriate order.
        /// </summary>
        public static void OnDecayPhase(
            GameBoard board,
            List<Player> players,
            Dictionary<int, int> failedGrowthsByPlayerId,
            Random rng,
            ISimulationObserver observer)
        {
            // Order matters here - some effects may interact with others
            
            // 1. Sporocidal Bloom (spore effects - should happen first to place spores before other effects)
            FungicideMutationProcessor.OnDecayPhase_SporocidalBloom(board, players, rng, observer);
            
            // 2. Mycotoxin Tracer (spore effects based on failed growths - should happen after other spore effects)
            FungicideMutationProcessor.OnDecayPhase_MycotoxinTracer(board, players, failedGrowthsByPlayerId, rng, observer);
            
            // 3. Mycotoxin Potentiation (toxin aura deaths - should happen after spore placement)
            FungicideMutationProcessor.OnDecayPhase_MycotoxinPotentiation(board, players, rng, observer);
            
            // 4. Chemotactic Mycotoxins (relocate isolated toxins - should happen at the end after all toxin placement)
            MycovariantEffectProcessor.OnDecayPhase_ChemotacticMycotoxins(board, players, rng, observer);
        }

        // Mutation Phase Events
        public static void OnMutationPhaseStart_MutatorPhenotype(
            GameBoard board,
            List<Player> players,
            List<Mutation> allMutations,
            Random rng,
            int currentRound,
            ISimulationObserver observer)
        {
            GeneticDriftMutationProcessor.OnMutationPhaseStart_MutatorPhenotype(board, players, allMutations, rng, currentRound, observer);
        }

        public static void OnMutationPhaseStart_OntogenicRegression(
            GameBoard board,
            List<Player> players,
            List<Mutation> allMutations,
            Random rng,
            int currentRound,
            ISimulationObserver observer)
        {
            GeneticDriftMutationProcessor.OnMutationPhaseStart_OntogenicRegression(board, players, allMutations, rng, currentRound, observer);
        }

        // Special Events
        public static void OnNecrophyticBloomActivated(
            GameBoard board,
            List<Player> players,
            Random rng,
            ISimulationObserver observer)
        {
            GeneticDriftMutationProcessor.OnNecrophyticBloomActivated(board, players, rng, observer);
        }

        public static void OnToxinExpired_CatabolicRebirth(
            ToxinExpiredEventArgs eventArgs,
            GameBoard board,
            List<Player> players,
            Random rng,
            ISimulationObserver observer)
        {
            CellularResilienceMutationProcessor.OnToxinExpired_CatabolicRebirth(eventArgs, board, players, rng, observer);
        }

        #endregion
    }
}