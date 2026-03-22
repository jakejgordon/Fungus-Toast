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
        private sealed class SubscriptionSet
        {
            public EventHandler<FungalCellDiedEventArgs> CellDeathMutationHandler;
            public EventHandler<FungalCellDiedEventArgs> CellDeathMycovariantHandler;
            public GameBoard.PreGrowthPhaseEventHandler PreGrowthPhaseHandler;
            public GameBoard.PostGrowthPhaseEventHandler PostGrowthPhaseHandler;
            public GameBoard.PostGrowthPhaseCompletedEventHandler PostGrowthPhaseCompletedHandler;
            public GameBoard.DecayPhaseEventHandler DecayPhaseHandler;
            public GameBoard.PostDecayPhaseEventHandler PostDecayPhaseHandler;
            public GameBoard.CellColonizedEventHandler CellColonizedHandler;
            public GameBoard.CellInfestedEventHandler CellInfestedHandler;
            public GameBoard.CellReclaimedEventHandler CellReclaimedHandler;
            public GameBoard.CellOvergrownEventHandler CellOvergrownHandler;
            public GameBoard.NecrophyticBloomActivatedEventHandler NecrophyticBloomActivatedHandler;
            public GameBoard.MutationPhaseStartEventHandler MutationPhaseStartHandler;
            public GameBoard.ToxinPlacedEventHandler ToxinPlacedHandler;
            public GameBoard.ToxinExpiredEventHandler ToxinExpiredHandler;
        }

        private static readonly Dictionary<GameBoard, SubscriptionSet> subscriptionsByBoard = new();

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
            ISimulationObserver observer)
        {
            if (board == null)
            {
                return;
            }

            UnsubscribeAll(board);

            var allMutations = MutationRegistry.GetAll().ToList();
            var subscriptions = new SubscriptionSet();

            // All mutation-based cell death effects (consolidated)
            subscriptions.CellDeathMutationHandler = (sender, args) =>
            {
                MutationEffectCoordinator.OnCellDeath(args, board, players, rng, observer);
            };
            board.CellDeath += subscriptions.CellDeathMutationHandler;

            // Mycovariant-based cell death effects (separate since they're not mutations)
            subscriptions.CellDeathMycovariantHandler = (sender, args) =>
            {
                MycovariantEffectProcessor.OnCellDeath_SeptalAlarm(board, args, players, rng, observer);
                MycovariantEffectProcessor.OnCellDeath_NecrophoricAdaptation(board, args.OwnerPlayerId, args.TileId, players, rng, observer);
            };
            board.CellDeath += subscriptions.CellDeathMycovariantHandler;

            // All mutation-based pre-growth phase effects (consolidated)
            subscriptions.PreGrowthPhaseHandler = () =>
            {
                MutationEffectCoordinator.OnPreGrowthPhase(board, players, rng, observer);
                // Corner Conduit should run AFTER toxin cleaning (Mycotoxin Catabolism) and surge prep
                MycovariantEffectProcessor.OnPreGrowthPhase_CornerConduit(board, players, rng, observer);
                // Aggressotropic Conduit runs after Corner Conduit (enemy-targeting path)
                MycovariantEffectProcessor.OnPreGrowthPhase_AggressotropicConduit(board, players, rng, observer);
            };
            board.PreGrowthPhase += subscriptions.PreGrowthPhaseHandler;

            // All mutation-based post-growth phase effects (consolidated)
            subscriptions.PostGrowthPhaseHandler = () =>
            {
                MutationEffectCoordinator.OnPostGrowthPhase(board, players, rng, observer);
            };
            board.PostGrowthPhase += subscriptions.PostGrowthPhaseHandler;

            subscriptions.PostGrowthPhaseCompletedHandler = () =>
            {
                AdaptationEffectProcessor.OnPostGrowthPhaseCompleted(board, players, rng, observer);
            };
            board.PostGrowthPhaseCompleted += subscriptions.PostGrowthPhaseCompletedHandler;

            // All mutation-based decay phase effects (consolidated)
            subscriptions.DecayPhaseHandler = (failedGrowthsByPlayerId) =>
            {
                MutationEffectCoordinator.OnDecayPhase(board, players, failedGrowthsByPlayerId, rng, observer);
            };
            board.DecayPhase += subscriptions.DecayPhaseHandler;

            subscriptions.PostDecayPhaseHandler = () =>
            {
                AdaptationEffectProcessor.OnPostDecayPhase(board, players, rng, observer);
            };
            board.PostDecayPhase += subscriptions.PostDecayPhaseHandler;

            subscriptions.CellColonizedHandler = (playerId, tileId, source) =>
            {
                AdaptationEffectProcessor.OnLivingCellEstablished(playerId, tileId, source, board, players, observer);
            };
            board.CellColonized += subscriptions.CellColonizedHandler;

            subscriptions.CellInfestedHandler = (playerId, tileId, oldOwnerId, source) =>
            {
                AdaptationEffectProcessor.OnLivingCellEstablished(playerId, tileId, source, board, players, observer);
            };
            board.CellInfested += subscriptions.CellInfestedHandler;

            subscriptions.CellReclaimedHandler = (playerId, tileId, source) =>
            {
                AdaptationEffectProcessor.OnLivingCellEstablished(playerId, tileId, source, board, players, observer);
            };
            board.CellReclaimed += subscriptions.CellReclaimedHandler;

            subscriptions.CellOvergrownHandler = (playerId, tileId, oldOwnerId, source) =>
            {
                AdaptationEffectProcessor.OnLivingCellEstablished(playerId, tileId, source, board, players, observer);
            };
            board.CellOvergrown += subscriptions.CellOvergrownHandler;

            // Necrophytic Bloom (initial burst on activation)
            subscriptions.NecrophyticBloomActivatedHandler = () =>
            {
                MutationEffectCoordinator.OnNecrophyticBloomActivated(board, players, rng, observer);
            };
            board.NecrophyticBloomActivatedEvent += subscriptions.NecrophyticBloomActivatedHandler;

            // Mutator Phenotype (mutation phase start auto-upgrade effect)
            subscriptions.MutationPhaseStartHandler = () =>
            {
                AdaptationEffectProcessor.OnMutationPhaseStart(board, players, rng, observer);
                MutationEffectCoordinator.OnMutationPhaseStart_MutatorPhenotype(board, players, allMutations, rng, observer);
                MutationEffectCoordinator.OnMutationPhaseStart_OntogenicRegression(board, players, allMutations, rng, observer);
                MutationEffectCoordinator.OnMutationPhaseStart_AdaptiveExpression(board, players, rng, observer);
                MutationEffectCoordinator.OnMutationPhaseStart_AnabolicInversion(board, players, rng, observer);
            };
            board.MutationPhaseStart += subscriptions.MutationPhaseStartHandler;

            // Neutralizing Mantle (toxin placement neutralization)
            subscriptions.ToxinPlacedHandler = (sender, args) =>
            {
                MycovariantEffectProcessor.OnToxinPlaced_NeutralizingMantle(args, board, players, rng, observer);
                MycovariantEffectProcessor.OnToxinPlaced_EnduringToxaphores(args, board, players, observer);
                AdaptationEffectProcessor.OnToxinPlaced(args, board, players, rng, observer);
            };
            board.ToxinPlaced += subscriptions.ToxinPlacedHandler;

            // Catabolic Rebirth (toxin expiration resurrection effect)
            subscriptions.ToxinExpiredHandler = (sender, args) =>
            {
                MutationEffectCoordinator.OnToxinExpired_CatabolicRebirth(args, board, players, rng, observer);
                AdaptationEffectProcessor.OnToxinExpired(args, board, players, rng, observer);
            };
            board.ToxinExpired += subscriptions.ToxinExpiredHandler;

            subscriptionsByBoard[board] = subscriptions;

            // TODO: Add additional event-driven rule subscriptions here.
            // e.g., Necrosporulation, Sporocidal Bloom, Creeping Mold, etc.
        }

        public static void UnsubscribeAll(GameBoard board)
        {
            if (board == null || !subscriptionsByBoard.TryGetValue(board, out var subscriptions))
            {
                return;
            }

            board.CellDeath -= subscriptions.CellDeathMutationHandler;
            board.CellDeath -= subscriptions.CellDeathMycovariantHandler;
            board.PreGrowthPhase -= subscriptions.PreGrowthPhaseHandler;
            board.PostGrowthPhase -= subscriptions.PostGrowthPhaseHandler;
            board.PostGrowthPhaseCompleted -= subscriptions.PostGrowthPhaseCompletedHandler;
            board.DecayPhase -= subscriptions.DecayPhaseHandler;
            board.PostDecayPhase -= subscriptions.PostDecayPhaseHandler;
            board.CellColonized -= subscriptions.CellColonizedHandler;
            board.CellInfested -= subscriptions.CellInfestedHandler;
            board.CellReclaimed -= subscriptions.CellReclaimedHandler;
            board.CellOvergrown -= subscriptions.CellOvergrownHandler;
            board.NecrophyticBloomActivatedEvent -= subscriptions.NecrophyticBloomActivatedHandler;
            board.MutationPhaseStart -= subscriptions.MutationPhaseStartHandler;
            board.ToxinPlaced -= subscriptions.ToxinPlacedHandler;
            board.ToxinExpired -= subscriptions.ToxinExpiredHandler;
            subscriptionsByBoard.Remove(board);
        }
    }
}
