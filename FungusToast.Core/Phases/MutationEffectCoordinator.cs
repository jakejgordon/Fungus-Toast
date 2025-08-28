using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Death;
using FungusToast.Core.Events;
using FungusToast.Core.Metrics;
using FungusToast.Core.Mutations;
using FungusToast.Core.Players;
using FungusToast.Core.Phases;
using System;
using System.Collections.Generic;
using System.Linq;

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
            // Homeostatic Harmony (random + age reduction)
            float harmonyReduction = owner.GetMutationEffect(MutationType.DefenseSurvival);

            // Chronoresilient Cytoplasm: increases age threshold before age-based death risk begins.
            // (MutationType.AgeAndRandomnessDecayResistance is the effect bucket for this.)
            float addedThreshold = owner.GetMutationEffect(MutationType.AgeAndRandomnessDecayResistance);
            float ageRiskThreshold = GameBalance.AgeAtWhichDecayChanceIncreases + addedThreshold; // treat as float to allow fractional future tuning

            // Random component (cannot go below zero)
            float randomChance = Math.Max(0f, GameBalance.BaseRandomDecayChance - harmonyReduction);

            // Age component only after threshold is exceeded
            float ageComponent = cell.GrowthCycleAge > ageRiskThreshold
                ? (cell.GrowthCycleAge - ageRiskThreshold) * GameBalance.AgeDeathFactorPerGrowthCycle
                : 0f;
            float ageChance = Math.Max(0f, ageComponent - harmonyReduction);

            float totalChance = Math.Clamp(randomChance + ageChance, 0f, 1f);
            float thresholdRandom = randomChance; // first segment of cumulative range
            float thresholdAge = randomChance + ageChance; // (== totalChance)

            if (roll < totalChance)
            {
                if (roll < thresholdRandom)
                {
                    return DeathCalculationResult.Death(totalChance, DeathReason.Randomness);
                }
                return DeathCalculationResult.Death(totalChance, DeathReason.Age);
            }

            // Putrefactive Mycotoxin kill check (resolved only if base death fails)
            if (FungicideMutationProcessor.CheckPutrefactiveMycotoxin(cell, board, allPlayers, roll, out float pmChance, out int? killerPlayerId, out int? attackerTileId, rng, observer))
            {
                return DeathCalculationResult.Death(pmChance, DeathReason.PutrefactiveMycotoxin, killerPlayerId, attackerTileId);
            }

            return DeathCalculationResult.NoDeath(totalChance);
        }

        #region Phase Event Orchestration

        // Cell Death Events
        /// <summary>
        /// Handles all mutation effects that trigger on cell death during the decay phase.
        /// Calls each mutation-specific handler in the appropriate order.
        /// Note: This only handles mutation-based effects. Mycovariant effects should be handled separately.
        /// </summary>
        public static void OnCellDeath(
            FungalCellDiedEventArgs eventArgs,
            GameBoard board,
            List<Player> players,
            Random rng,
            ISimulationObserver observer,
            DecayPhaseContext decayPhaseContext)
        {
            // Order matters here - some effects may depend on the results of others

            // 1. Necrotoxic Conversion (toxin death reclamation - should happen first to potentially reclaim cells)
            FungicideMutationProcessor.OnCellDeath_NecrotoxicConversion(eventArgs, board, players, rng, observer);

            // 2. Putrefactive Rejuvenation (age reduction from kills - should happen before cascades)
            FungicideMutationProcessor.OnCellDeath_PutrefactiveRejuvenation(eventArgs, board, players, observer);

            // 3. Putrefactive Cascade (directional kill chains - should happen last to avoid affecting other mutations)
            FungicideMutationProcessor.OnCellDeath_PutrefactiveCascade(eventArgs, board, players, rng, observer);

            // 4. Necrophytic Bloom per-death trigger (if activated) - use cached ratio for performance
            if (board.NecrophyticBloomActivated)
            {
                var owner = players.FirstOrDefault(p => p.PlayerId == eventArgs.OwnerPlayerId);
                if (owner != null && owner.GetMutationLevel(MutationIds.NecrophyticBloom) > 0)
                {
                    // Use cached occupied percent and decay phase context for optimized competitive targeting
                    GeneticDriftMutationProcessor.TriggerNecrophyticBloomOnCellDeath(owner, board, players, rng, board.CachedOccupiedTileRatio, observer, decayPhaseContext);
                }
            }
        }

        /// <summary>
        /// Handles all mutation effects that trigger on cell death (general case without decay phase context).
        /// This version is used for general event handling and tries to use cached context if available.
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

            // 4. Necrophytic Bloom per-death trigger (if activated) - use cached context if available
            if (board.NecrophyticBloomActivated)
            {
                var owner = players.FirstOrDefault(p => p.PlayerId == eventArgs.OwnerPlayerId);
                if (owner != null && owner.GetMutationLevel(MutationIds.NecrophyticBloom) > 0)
                {
                    if (board.CachedDecayPhaseContext != null)
                    {
                        // Use cached context for optimal performance
                        GeneticDriftMutationProcessor.TriggerNecrophyticBloomOnCellDeath(owner, board, players, rng, board.CachedOccupiedTileRatio, observer, board.CachedDecayPhaseContext);
                    }
                    else
                    {
                        // Create a temporary context for this call - not optimal but needed for compatibility
                        var tempContext = new DecayPhaseContext(board, players);
                        GeneticDriftMutationProcessor.TriggerNecrophyticBloomOnCellDeath(owner, board, players, rng, board.CachedOccupiedTileRatio, observer, tempContext);
                    }
                }
            }
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
        /// Uses the cached DecayPhaseContext from the GameBoard.
        /// </summary>
        public static void OnDecayPhase(
            GameBoard board,
            List<Player> players,
            Dictionary<int, int> failedGrowthsByPlayerId,
            Random rng,
            ISimulationObserver observer)
        {
            // Require cached decay phase context - should be populated by DeathEngine
            if (board.CachedDecayPhaseContext == null)
            {
                throw new InvalidOperationException("CachedDecayPhaseContext must be populated before calling OnDecayPhase. Ensure UpdateCachedDecayPhaseContext() is called first.");
            }
            
            var decayPhaseContext = board.CachedDecayPhaseContext;
            
            // Order matters here - some effects may interact with others
            
            // 1. Sporocidal Bloom (spore effects - should happen first to place spores before other effects)
            FungicideMutationProcessor.OnDecayPhase_SporicidalBloom(board, players, rng, observer, decayPhaseContext);
            
            // 2. Mycotoxin Tracer (spore effects based on failed growths - should happen after other spore effects)
            FungicideMutationProcessor.OnDecayPhase_MycotoxinTracer(board, players, failedGrowthsByPlayerId, rng, observer, decayPhaseContext);
            
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

        public static void OnMutationPhaseStart_AdaptiveExpression(
            GameBoard board,
            List<Player> players,
            Random rng,
            ISimulationObserver observer)
        {
            GeneticDriftMutationProcessor.OnMutationPhaseStart_AdaptiveExpression(board, players, rng, observer);
        }

        public static void OnMutationPhaseStart_AnabolicInversion(
            GameBoard board,
            List<Player> players,
            Random rng,
            ISimulationObserver observer)
        {
            GeneticDriftMutationProcessor.OnMutationPhaseStart_AnabolicInversion(board, players, rng, observer);
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