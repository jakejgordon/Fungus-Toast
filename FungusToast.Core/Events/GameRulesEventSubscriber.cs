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
            public SubscriptionSet(
                EventHandler<FungalCellDiedEventArgs> cellDeathMutationHandler,
                EventHandler<FungalCellDiedEventArgs> cellDeathMycovariantHandler,
                GameBoard.PreGrowthPhaseEventHandler preGrowthPhaseHandler,
                GameBoard.PostGrowthPhaseEventHandler postGrowthPhaseHandler,
                GameBoard.PostGrowthPhaseCompletedEventHandler postGrowthPhaseCompletedHandler,
                GameBoard.DecayPhaseEventHandler decayPhaseHandler,
                GameBoard.PostDecayPhaseEventHandler postDecayPhaseHandler,
                GameBoard.CellColonizedEventHandler cellColonizedHandler,
                GameBoard.CellInfestedEventHandler cellInfestedHandler,
                GameBoard.CellReclaimedEventHandler cellReclaimedHandler,
                GameBoard.CellOvergrownEventHandler cellOvergrownHandler,
                GameBoard.NecrophyticBloomActivatedEventHandler necrophyticBloomActivatedHandler,
                GameBoard.MutationPhaseStartEventHandler mutationPhaseStartHandler,
                GameBoard.ToxinPlacedEventHandler toxinPlacedHandler,
                GameBoard.ToxinExpiredEventHandler toxinExpiredHandler)
            {
                CellDeathMutationHandler = cellDeathMutationHandler;
                CellDeathMycovariantHandler = cellDeathMycovariantHandler;
                PreGrowthPhaseHandler = preGrowthPhaseHandler;
                PostGrowthPhaseHandler = postGrowthPhaseHandler;
                PostGrowthPhaseCompletedHandler = postGrowthPhaseCompletedHandler;
                DecayPhaseHandler = decayPhaseHandler;
                PostDecayPhaseHandler = postDecayPhaseHandler;
                CellColonizedHandler = cellColonizedHandler;
                CellInfestedHandler = cellInfestedHandler;
                CellReclaimedHandler = cellReclaimedHandler;
                CellOvergrownHandler = cellOvergrownHandler;
                NecrophyticBloomActivatedHandler = necrophyticBloomActivatedHandler;
                MutationPhaseStartHandler = mutationPhaseStartHandler;
                ToxinPlacedHandler = toxinPlacedHandler;
                ToxinExpiredHandler = toxinExpiredHandler;
            }

            public EventHandler<FungalCellDiedEventArgs> CellDeathMutationHandler { get; }
            public EventHandler<FungalCellDiedEventArgs> CellDeathMycovariantHandler { get; }
            public GameBoard.PreGrowthPhaseEventHandler PreGrowthPhaseHandler { get; }
            public GameBoard.PostGrowthPhaseEventHandler PostGrowthPhaseHandler { get; }
            public GameBoard.PostGrowthPhaseCompletedEventHandler PostGrowthPhaseCompletedHandler { get; }
            public GameBoard.DecayPhaseEventHandler DecayPhaseHandler { get; }
            public GameBoard.PostDecayPhaseEventHandler PostDecayPhaseHandler { get; }
            public GameBoard.CellColonizedEventHandler CellColonizedHandler { get; }
            public GameBoard.CellInfestedEventHandler CellInfestedHandler { get; }
            public GameBoard.CellReclaimedEventHandler CellReclaimedHandler { get; }
            public GameBoard.CellOvergrownEventHandler CellOvergrownHandler { get; }
            public GameBoard.NecrophyticBloomActivatedEventHandler NecrophyticBloomActivatedHandler { get; }
            public GameBoard.MutationPhaseStartEventHandler MutationPhaseStartHandler { get; }
            public GameBoard.ToxinPlacedEventHandler ToxinPlacedHandler { get; }
            public GameBoard.ToxinExpiredEventHandler ToxinExpiredHandler { get; }
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

            // All mutation-based cell death effects (consolidated)
            EventHandler<FungalCellDiedEventArgs> cellDeathMutationHandler = (sender, args) =>
            {
                MutationEffectCoordinator.OnCellDeath(args, board, players, rng, observer);
            };
            board.CellDeath += cellDeathMutationHandler;

            // Mycovariant-based cell death effects (separate since they're not mutations)
            EventHandler<FungalCellDiedEventArgs> cellDeathMycovariantHandler = (sender, args) =>
            {
                MycovariantEffectProcessor.OnCellDeath_SeptalAlarm(board, args, players, rng, observer);
                MycovariantEffectProcessor.OnCellDeath_NecrophoricAdaptation(board, args.OwnerPlayerId, args.TileId, players, rng, observer);
            };
            board.CellDeath += cellDeathMycovariantHandler;

            // All mutation-based pre-growth phase effects (consolidated)
            GameBoard.PreGrowthPhaseEventHandler preGrowthPhaseHandler = () =>
            {
                MutationEffectCoordinator.OnPreGrowthPhase(board, players, rng, observer);
                // Corner Conduit should run AFTER toxin cleaning (Mycotoxin Catabolism) and surge prep
                MycovariantEffectProcessor.OnPreGrowthPhase_CornerConduit(board, players, rng, observer);
                // Aggressotropic Conduit runs after Corner Conduit (enemy-targeting path)
                MycovariantEffectProcessor.OnPreGrowthPhase_AggressotropicConduit(board, players, rng, observer);
            };
            board.PreGrowthPhase += preGrowthPhaseHandler;

            // All mutation-based post-growth phase effects (consolidated)
            GameBoard.PostGrowthPhaseEventHandler postGrowthPhaseHandler = () =>
            {
                MutationEffectCoordinator.OnPostGrowthPhase(board, players, rng, observer);
            };
            board.PostGrowthPhase += postGrowthPhaseHandler;

            GameBoard.PostGrowthPhaseCompletedEventHandler postGrowthPhaseCompletedHandler = () =>
            {
                AdaptationEffectProcessor.OnPostGrowthPhaseCompleted(board, players, rng, observer);
            };
            board.PostGrowthPhaseCompleted += postGrowthPhaseCompletedHandler;

            // All mutation-based decay phase effects (consolidated)
            GameBoard.DecayPhaseEventHandler decayPhaseHandler = (failedGrowthsByPlayerId) =>
            {
                MutationEffectCoordinator.OnDecayPhase(board, players, failedGrowthsByPlayerId, rng, observer);
            };
            board.DecayPhase += decayPhaseHandler;

            GameBoard.PostDecayPhaseEventHandler postDecayPhaseHandler = () =>
            {
                AdaptationEffectProcessor.OnPostDecayPhase(board, players, rng, observer);
            };
            board.PostDecayPhase += postDecayPhaseHandler;

            GameBoard.CellColonizedEventHandler cellColonizedHandler = (playerId, tileId, source) =>
            {
                AdaptationEffectProcessor.OnLivingCellEstablished(playerId, tileId, source, board, players, observer);
            };
            board.CellColonized += cellColonizedHandler;

            GameBoard.CellInfestedEventHandler cellInfestedHandler = (playerId, tileId, oldOwnerId, source) =>
            {
                AdaptationEffectProcessor.OnLivingCellEstablished(playerId, tileId, source, board, players, observer);
            };
            board.CellInfested += cellInfestedHandler;

            GameBoard.CellReclaimedEventHandler cellReclaimedHandler = (playerId, tileId, source) =>
            {
                AdaptationEffectProcessor.OnLivingCellEstablished(playerId, tileId, source, board, players, observer);
            };
            board.CellReclaimed += cellReclaimedHandler;

            GameBoard.CellOvergrownEventHandler cellOvergrownHandler = (playerId, tileId, oldOwnerId, source) =>
            {
                AdaptationEffectProcessor.OnLivingCellEstablished(playerId, tileId, source, board, players, observer);
            };
            board.CellOvergrown += cellOvergrownHandler;

            // Necrophytic Bloom (initial burst on activation)
            GameBoard.NecrophyticBloomActivatedEventHandler necrophyticBloomActivatedHandler = () =>
            {
                MutationEffectCoordinator.OnNecrophyticBloomActivated(board, players, rng, observer);
            };
            board.NecrophyticBloomActivatedEvent += necrophyticBloomActivatedHandler;

            // Mutator Phenotype (mutation phase start auto-upgrade effect)
            GameBoard.MutationPhaseStartEventHandler mutationPhaseStartHandler = () =>
            {
                AdaptationEffectProcessor.OnMutationPhaseStart(board, players, rng, observer);
                MutationEffectCoordinator.OnMutationPhaseStart_MutatorPhenotype(board, players, allMutations, rng, observer);
                MutationEffectCoordinator.OnMutationPhaseStart_OntogenicRegression(board, players, allMutations, rng, observer);
                MutationEffectCoordinator.OnMutationPhaseStart_AdaptiveExpression(board, players, rng, observer);
                MutationEffectCoordinator.OnMutationPhaseStart_AnabolicInversion(board, players, rng, observer);
            };
            board.MutationPhaseStart += mutationPhaseStartHandler;

            // Neutralizing Mantle (toxin placement neutralization)
            GameBoard.ToxinPlacedEventHandler toxinPlacedHandler = (sender, args) =>
            {
                MycovariantEffectProcessor.OnToxinPlaced_NeutralizingMantle(args, board, players, rng, observer);
                MycovariantEffectProcessor.OnToxinPlaced_EnduringToxaphores(args, board, players, observer);
                AdaptationEffectProcessor.OnToxinPlaced(args, board, players, rng, observer);
            };
            board.ToxinPlaced += toxinPlacedHandler;

            // Catabolic Rebirth (toxin expiration resurrection effect)
            GameBoard.ToxinExpiredEventHandler toxinExpiredHandler = (sender, args) =>
            {
                MutationEffectCoordinator.OnToxinExpired_CatabolicRebirth(args, board, players, rng, observer);
                AdaptationEffectProcessor.OnToxinExpired(args, board, players, rng, observer);
            };
            board.ToxinExpired += toxinExpiredHandler;

            var subscriptions = new SubscriptionSet(
                cellDeathMutationHandler,
                cellDeathMycovariantHandler,
                preGrowthPhaseHandler,
                postGrowthPhaseHandler,
                postGrowthPhaseCompletedHandler,
                decayPhaseHandler,
                postDecayPhaseHandler,
                cellColonizedHandler,
                cellInfestedHandler,
                cellReclaimedHandler,
                cellOvergrownHandler,
                necrophyticBloomActivatedHandler,
                mutationPhaseStartHandler,
                toxinPlacedHandler,
                toxinExpiredHandler);

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
