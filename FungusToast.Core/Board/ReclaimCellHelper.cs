using FungusToast.Core.Mycovariants;
using FungusToast.Core.Config;
using FungusToast.Core.Metrics;
using FungusToast.Core.Players;
using FungusToast.Core.Growth;

namespace FungusToast.Core.Board
{
    /// <summary>
    /// Helper class for reclaiming dead cells with support for Reclamation Rhizomorphs retry logic.
    /// All reclamation attempts should go through this helper to ensure consistent behavior.
    /// </summary>
    public static class ReclaimCellHelper
    {
        /// <summary>
        /// Attempts to reclaim a dead cell for the specified player with a given success chance.
        /// If the first attempt fails and the player has Reclamation Rhizomorphs, they get a second chance.
        /// </summary>
        /// <param name="board">The game board</param>
        /// <param name="player">The player attempting to reclaim</param>
        /// <param name="tileId">The tile ID of the dead cell to reclaim</param>
        /// <param name="targetChance">The chance (0.0-1.0) for the reclamation to succeed</param>
        /// <param name="rng">Random number generator</param>
        /// <param name="observer">Optional simulation observer for tracking</param>
        /// <returns>True if reclamation succeeded (including retry), false otherwise</returns>
        public static bool TryReclaimDeadCell(
            GameBoard board,
            Player player,
            int tileId,
            float targetChance,
            Random rng,
            GrowthSource reclaimGrowthSource,
            ISimulationObserver? observer = null)
        {
            var tile = board.GetTileById(tileId);
            if (tile?.FungalCell == null || !tile.FungalCell.IsDead)
            {
                return false;
            }

            var cell = tile.FungalCell;
            
            // First attempt: roll against target chance
            if (rng.NextDouble() < targetChance)
            {
                return TryReclaimCellInternal(board, player.PlayerId, tileId, cell, reclaimGrowthSource);
            }

            // First attempt failed - check for Reclamation Rhizomorphs
            if (player.GetMycovariant(MycovariantIds.ReclamationRhizomorphsId) != null)
            {
                // Check if the second attempt succeeds (25% chance of getting a second attempt)
                if (rng.NextDouble() < MycovariantGameBalance.ReclamationRhizomorphsSecondAttemptChance)
                {
                    // Second attempt: roll against the same target chance
                    if (rng.NextDouble() < targetChance)
                    {
                        bool secondAttemptSucceeded = TryReclaimCellInternal(board, player.PlayerId, tileId, cell, reclaimGrowthSource);
                        if (secondAttemptSucceeded)
                        {
                            // Track the second attempt
                            observer?.RecordReclamationRhizomorphsSecondAttempt(player.PlayerId, 1);
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Internal method that performs the actual reclamation attempt.
        /// </summary>
        private static bool TryReclaimCellInternal(GameBoard board, int playerId, int tileId, FungalCell cell, GrowthSource reclaimGrowthSource)
        {
            // Check if the cell is still reclaimable (might have been reclaimed by someone else)
            if (!cell.IsReclaimable)
            {
                return false;
            }

            // Use the board's TryReclaimDeadCell method which properly handles events
            return board.TryReclaimDeadCell(playerId, tileId, reclaimGrowthSource);
        }
    }
}