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
            // All mutation-based cell death effects (consolidated)
            board.CellDeath += (sender, args) =>
            {
                MutationEffectCoordinator.OnCellDeath(args, board, players, rng, observer);
            };

            // Mycovariant-based cell death effects (separate since they're not mutations)
            board.CellDeath += (sender, args) =>
            {
                MycovariantEffectProcessor.OnCellDeath_NecrophoricAdaptation(board, args.OwnerPlayerId, args.TileId, players, rng, observer);
            };

            // All mutation-based pre-growth phase effects (consolidated)
            board.PreGrowthPhase += () =>
            {
                MutationEffectCoordinator.OnPreGrowthPhase(board, players, rng, observer);
            };

            // All mutation-based post-growth phase effects (consolidated)
            board.PostGrowthPhase += () =>
            {
                MutationEffectCoordinator.OnPostGrowthPhase(board, players, rng, observer);
            };

            // All mutation-based decay phase effects (consolidated)
            board.DecayPhase += (failedGrowthsByPlayerId) =>
            {
                MutationEffectCoordinator.OnDecayPhase(board, players, failedGrowthsByPlayerId, rng, observer);
            };

            // Necrophytic Bloom (initial burst on activation)
            board.NecrophyticBloomActivatedEvent += () =>
            {
                MutationEffectCoordinator.OnNecrophyticBloomActivated(board, players, rng, observer);
            };

            // Mutator Phenotype (mutation phase start auto-upgrade effect)
            board.MutationPhaseStart += () =>
            {
                var allMutations = MutationRepository.BuildFullMutationSet().Item1.Values.ToList();
                MutationEffectCoordinator.OnMutationPhaseStart_MutatorPhenotype(board, players, allMutations, rng, board.CurrentRound, observer);
            };

            // Neutralizing Mantle (toxin placement neutralization)
            board.ToxinPlaced += (sender, args) =>
            {
                MycovariantEffectProcessor.OnToxinPlaced_NeutralizingMantle(args, board, players, rng, observer);
                MycovariantEffectProcessor.OnToxinPlaced_EnduringToxaphores(args, board, players, observer);
            };

            // Catabolic Rebirth (toxin expiration resurrection effect)
            board.ToxinExpired += (sender, args) =>
            {
                MutationEffectCoordinator.OnToxinExpired_CatabolicRebirth(args, board, players, rng, observer);
            };

            // TODO: Add additional event-driven rule subscriptions here.
            // e.g., Necrosporulation, Sporocidal Bloom, Creeping Mold, etc.
        }
    }
}
