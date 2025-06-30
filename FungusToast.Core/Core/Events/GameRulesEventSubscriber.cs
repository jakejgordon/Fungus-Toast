using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Death;
using FungusToast.Core.Events;
using FungusToast.Core.Metrics;
using FungusToast.Core.Mutations;
using FungusToast.Core.Mycovariants;
using FungusToast.Core.Phases;
using FungusToast.Core.Players;
using System;
using System.Collections.Generic;

namespace FungusToast.Core.Events
{
    /// <summary>
    /// Subscribes to GameBoard events and applies core mutation rule effects.
    /// Call <see cref="SubscribeAll"/> during setup (Unity or simulation).
    /// </summary>
    public static class GameRulesEventSubscriber
    {
        /// <summary>
        /// Subscribes all mutation-related rule handlers to board events.
        /// </summary>
        /// <param name="board">The GameBoard instance.</param>
        /// <param name="players">List of Player objects.</param>
        /// <param name="rng">Random number generator.</param>
        /// <param name="observer">
        /// Optional simulation observer for analytics/reporting. Pass <c>null</c> in Unity.
        /// </param>
        public static void SubscribeAll(
            GameBoard board,
            List<Player> players,
            Random rng,
            ISimulationObserver? observer = null)
        {
            // Necrotoxic Conversion (toxin death triggers adjacent enemy mutation effect)
            board.CellDeath += (sender, args) =>
            {
                MutationEffectProcessor.OnCellDeath_NecrotoxicConversion(args, board, players, rng, observer);
            };

            // Regenerative Hyphae (post-growth phase reclaim effect)
            board.PostGrowthPhase += () =>
            {
                MutationEffectProcessor.OnPostGrowthPhase_RegenerativeHyphae(board, players, rng, observer);
            };

            // Hyphal Vectoring (post-growth phase surge effect)
            board.PostGrowthPhase += () =>
            {
                MutationEffectProcessor.OnPostGrowthPhase_HyphalVectoring(board, players, rng, observer);
            };

            // Sporocidal Bloom (decay phase spore effects)
            board.DecayPhase += () =>
            {
                MutationEffectProcessor.OnDecayPhase_SporocidalBloom(board, players, rng, observer);
            };

            // Mycotoxin Potentiation (decay phase toxin aura deaths)
            board.DecayPhase += () =>
            {
                MutationEffectProcessor.OnDecayPhase_MycotoxinPotentiation(board, players, rng, observer);
            };

            // Mycotoxin Tracer (decay phase spore effects with failed growth data)
            board.DecayPhaseWithFailedGrowths += (failedGrowthsByPlayerId) =>
            {
                MutationEffectProcessor.OnDecayPhase_MycotoxinTracer(board, players, failedGrowthsByPlayerId, rng, observer);
            };

            // Mycotoxin Catabolism (pre-growth cycle toxin processing)
            board.PreGrowthCycle += () =>
            {
                var roundContext = new RoundContext();
                MutationEffectProcessor.OnPreGrowthCycle_MycotoxinCatabolism(board, players, rng, roundContext, observer);
            };

            // Necrophytic Bloom (initial burst on activation)
            board.NecrophyticBloomActivatedEvent += () =>
            {
                MutationEffectProcessor.OnNecrophyticBloomActivated(board, players, rng, observer);
            };

            // Mutator Phenotype (mutation phase start auto-upgrade effect)
            board.MutationPhaseStart += () =>
            {
                var allMutations = MutationRepository.BuildFullMutationSet().Item1.Values.ToList();
                MutationEffectProcessor.OnMutationPhaseStart_MutatorPhenotype(board, players, allMutations, rng, board.CurrentRound, observer);
            };

            // Neutralizing Mantle (toxin placement neutralization)
            board.ToxinPlaced += (sender, args) =>
            {
                MycovariantEffectProcessor.OnToxinPlaced_NeutralizingMantle(args, board, players, rng, observer);
            };

            // Catabolic Rebirth (toxin expiration resurrection effect)
            board.ToxinExpired += (sender, args) =>
            {
                MutationEffectProcessor.OnToxinExpired_CatabolicRebirth(args, board, players, rng, observer);
            };

            // TODO: Add additional event-driven rule subscriptions here.
            // e.g., Necrosporulation, Sporocidal Bloom, Creeping Mold, etc.
        }
    }
}
