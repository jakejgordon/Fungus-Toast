using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Players;
using System;
using System.Linq;

namespace FungusToast.Core.Mycovariants
{
    public static class CytolyticBurstHelper
    {
        /// <summary>
        /// Finds the best toxin to explode for Cytolytic Burst based on a simple scoring model.
        /// Returns the tileId of the best toxin and its score, or null if no valid toxins exist.
        /// Scoring: +1 point for each enemy living cell in radius, -3 points for each friendly living cell in radius.
        /// </summary>
        public static (int tileId, int score)? FindBestToxinToExplode(Player player, GameBoard board)
        {
            var playerToxins = board.GetAllCellsOwnedBy(player.PlayerId)
                .Where(c => c.IsToxin)
                .ToList();

            if (playerToxins.Count == 0)
                return null;

            int bestScore = int.MinValue;
            int bestTileId = -1;

            foreach (var toxin in playerToxins)
            {
                int score = CalculateExplosionScore(player, board, toxin.TileId);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestTileId = toxin.TileId;
                }
            }

            return bestTileId == -1 ? null : (bestTileId, bestScore);
        }

        /// <summary>
        /// Calculates the explosion score for a specific toxin location.
        /// +1 point for each enemy living cell in radius, -3 points for each friendly living cell in radius.
        /// </summary>
        public static int CalculateExplosionScore(Player player, GameBoard board, int toxinTileId)
        {
            var (centerX, centerY) = board.GetXYFromTileId(toxinTileId);
            int radius = MycovariantGameBalance.CytolyticBurstRadius;
            int score = 0;

            // Check all tiles within radius using Manhattan distance
            for (int x = Math.Max(0, centerX - radius); x <= Math.Min(board.Width - 1, centerX + radius); x++)
            {
                for (int y = Math.Max(0, centerY - radius); y <= Math.Min(board.Height - 1, centerY + radius); y++)
                {
                    // Calculate Manhattan distance
                    int distance = Math.Abs(x - centerX) + Math.Abs(y - centerY);
                    if (distance > radius) continue;

                    var tile = board.GetTile(x, y);
                    if (tile?.FungalCell == null || !tile.FungalCell.IsAlive) continue;

                    if (tile.FungalCell.OwnerPlayerId == player.PlayerId)
                    {
                        // Friendly living cell: -3 points
                        score -= 3;
                    }
                    else
                    {
                        // Enemy living cell: +1 point
                        score += 1;
                    }
                }
            }

            return score;
        }
    }
}