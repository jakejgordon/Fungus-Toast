using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Players;
using System;
using System.Linq;

namespace FungusToast.Core.Mycovariants
{
    public static class JettingMyceliumHelper
    {
        /// <summary>
        /// Evaluates the potential placement of Jetting Mycelium from a given source cell in a specific direction.
        /// Returns a score based on the expected outcomes.
        /// </summary>
        public static float EvaluatePlacement(FungalCell sourceCell, CardinalDirection direction, GameBoard board, Player player)
        {
            int livingLength = MycovariantGameBalance.JettingMyceliumNumberOfLivingCellTiles;
            int toxinCount = MycovariantGameBalance.JettingMyceliumNumberOfToxinTiles;
            int totalLength = livingLength + toxinCount;

            var line = board.GetTileLine(sourceCell.TileId, direction, totalLength, includeStartingTile: false);

            int infested = 0;
            int reclaimed = 0;
            int poisoned = 0;
            int wastedOnOwn = 0;

            // Evaluate living cell section (first livingLength tiles)
            for (int i = 0; i < line.Count && i < livingLength; i++)
            {
                var targetTile = board.GetTileById(line[i]);
                if (targetTile == null) continue;

                var prevCell = targetTile.FungalCell;
                if (prevCell == null)
                {
                    // Empty tile - would be colonized (neutral, no points)
                }
                else if (prevCell.IsAlive)
                {
                    if (prevCell.OwnerPlayerId == player.PlayerId)
                    {
                        // Own living cell - wasted opportunity
                        wastedOnOwn++;
                    }
                    else
                    {
                        // Enemy living cell - would be infested
                        infested++;
                    }
                }
                else if (prevCell.IsDead && prevCell.OwnerPlayerId == player.PlayerId)
                {
                    // Own dead cell - would be reclaimed
                    reclaimed++;
                }
                // Other cases (enemy dead cells, toxins) are neutral
            }

            // Evaluate toxin section (remaining tiles)
            for (int i = livingLength; i < line.Count && i < totalLength; i++)
            {
                var targetTile = board.GetTileById(line[i]);
                if (targetTile == null) continue;

                var prevCell = targetTile.FungalCell;
                if (prevCell == null || prevCell.IsDead)
                {
                    // Empty or dead - would be toxified (neutral, no points)
                }
                else if (prevCell.IsAlive && prevCell.OwnerPlayerId != player.PlayerId)
                {
                    // Enemy living cell - would be killed and toxified
                    poisoned++;
                }
                // Own living cells are not overwritten with toxin
            }

            // Calculate score based on the proposed scoring system
            float score = (infested * 5f) + (reclaimed * 2f) + (poisoned * 3f);
            // Penalize for wasting opportunities on own cells
            score -= wastedOnOwn * 2f;
            return Math.Max(0f, score);
        }

        /// <summary>
        /// Converts a placement score to an AIScore for the mycovariant.
        /// </summary>
        public static float ScoreToAIScore(float placementScore)
        {
            if (placementScore == 0) return 3f;
            if (placementScore < 5) return 4f;
            if (placementScore < 10) return 5f;
            if (placementScore < 12) return 6f;
            if (placementScore < 14) return 7f;
            if (placementScore < 18) return 8f;
            if (placementScore < 22) return 9f;
            return 10f;
        }

        /// <summary>
        /// Finds the best placement for Jetting Mycelium from any of the player's living cells in the given direction.
        /// </summary>
        public static (FungalCell sourceCell, float score)? FindBestPlacement(Player player, GameBoard board, CardinalDirection direction)
        {
            var livingCells = board.GetAllCellsOwnedBy(player.PlayerId)
                .Where(c => c.IsAlive)
                .ToList();

            if (livingCells.Count == 0) return null;

            float bestScore = -1f;
            FungalCell? bestCell = null;

            foreach (var cell in livingCells)
            {
                float score = EvaluatePlacement(cell, direction, board, player);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestCell = cell;
                }
            }

            return bestCell != null ? (bestCell, bestScore) : null;
        }
    }
} 