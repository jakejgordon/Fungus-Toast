using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Death;
using FungusToast.Core.Events;
using FungusToast.Core.Metrics;
using FungusToast.Core.Mutations;
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

            // TODO: Add additional event-driven rule subscriptions here.
            // e.g., Necrosporulation, Sporocidal Bloom, Creeping Mold, etc.
        }
    }
}
