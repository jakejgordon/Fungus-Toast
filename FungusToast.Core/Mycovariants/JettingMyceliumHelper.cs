using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Players;
using System;
using System.Linq;

namespace FungusToast.Core.Mycovariants
{
    public static class JettingMyceliumHelper
    {
        public static int GetLivingLengthForMycovariant(int mycovariantId)
        {
            return mycovariantId switch
            {
                MycovariantIds.JettingMyceliumIIIId => MycovariantGameBalance.JettingMyceliumIIILivingCellTiles,
                MycovariantIds.JettingMyceliumIIId => MycovariantGameBalance.JettingMyceliumIILivingCellTiles,
                _ => MycovariantGameBalance.JettingMyceliumILivingCellTiles
            };
        }

        public static IReadOnlyList<int> GetToxinRowWidthsForMycovariant(int mycovariantId)
        {
            return mycovariantId switch
            {
                MycovariantIds.JettingMyceliumIIId => MycovariantGameBalance.JettingMyceliumIIToxinRowWidths,
                MycovariantIds.JettingMyceliumIIIId => MycovariantGameBalance.JettingMyceliumIIIToxinRowWidths,
                _ => MycovariantGameBalance.JettingMyceliumIToxinRowWidths
            };
        }

        public static int GetMaximumToxinWidthForMycovariant(int mycovariantId)
        {
            var rowWidths = GetToxinRowWidthsForMycovariant(mycovariantId);
            return rowWidths[rowWidths.Count - 1];
        }

        /// <summary>
        /// Evaluates the potential placement of Jetting Mycelium from a given source cell in a specific direction.
        /// Returns a score based on the expected outcomes using the new cone pattern.
        /// </summary>
        public static float EvaluatePlacement(FungalCell sourceCell, CardinalDirection direction, GameBoard board, Player player)
            => EvaluatePlacement(sourceCell, direction, board, player, MycovariantIds.JettingMyceliumIId);

        /// <summary>
        /// Evaluates the potential placement of Jetting Mycelium from a given source cell in a specific direction.
        /// Returns a score based on the expected outcomes for the specified tier.
        /// </summary>
        /// <param name="targetRankMultipliers">
        /// Optional per-player score multipliers keyed on current standing (see
        /// <see cref="BuildTargetRankMultipliers"/>). When null it is computed on demand; callers that
        /// evaluate many placements should build it once and pass it in.
        /// </param>
        public static float EvaluatePlacement(FungalCell sourceCell, CardinalDirection direction, GameBoard board, Player player, int mycovariantId, IReadOnlyDictionary<int, float>? targetRankMultipliers = null)
        {
            targetRankMultipliers ??= BuildTargetRankMultipliers(board);

            float TargetRankMultiplier(int? ownerPlayerId)
                => ownerPlayerId.HasValue && targetRankMultipliers.TryGetValue(ownerPlayerId.Value, out float multiplier)
                    ? multiplier
                    : 1f;

            int livingLength = GetLivingLengthForMycovariant(mycovariantId);
            var toxinRowWidths = GetToxinRowWidthsForMycovariant(mycovariantId);

            // Get the straight line for living cells
            var livingLine = board.GetTileLine(sourceCell.TileId, direction, livingLength, includeStartingTile: false);
            
            // Get the cone pattern for toxins
            var toxinCone = board.GetTileCone(sourceCell.TileId, direction, toxinRowWidths, livingLength);

            // Enemy damage is accumulated as rank-weighted score rather than a raw count, so that
            // hitting a highly ranked (leading) player is worth more than hitting a trailing one.
            float infestedScore = 0f;
            float poisonedScore = 0f;
            int reclaimed = 0;
            int wastedOnOwn = 0;

            // Evaluate living cell section (straight line)
            for (int i = 0; i < livingLine.Count && i < livingLength; i++)
            {
                var targetTile = board.GetTileById(livingLine[i]);
                if (targetTile == null || targetTile.IsBlocked) continue;

                var prevCell = targetTile.FungalCell;
                if (prevCell == null)
                {
                    // Empty tile - would be colonized (neutral, no points)
                }
                else if (prevCell.IsAlive)
                {
                    if (prevCell.IsResistant)
                    {
                        // Resistant cells cannot be infested.
                    }
                    else if (prevCell.OwnerPlayerId == player.PlayerId)
                    {
                        // Own living cell - wasted opportunity
                        wastedOnOwn++;
                    }
                    else
                    {
                        // Enemy living cell - would be infested
                        infestedScore += 5f * TargetRankMultiplier(prevCell.OwnerPlayerId);
                    }
                }
                else if (prevCell.IsDead && prevCell.OwnerPlayerId == player.PlayerId)
                {
                    // Own dead cell - would be reclaimed
                    reclaimed++;
                }
                // Other cases (enemy dead cells, toxins) are neutral
            }

            // Evaluate toxin section (cone pattern)
            foreach (int coneTileId in toxinCone)
            {
                var targetTile = board.GetTileById(coneTileId);
                if (targetTile == null || targetTile.IsBlocked) continue;

                var prevCell = targetTile.FungalCell;
                if (prevCell == null || prevCell.IsDead)
                {
                    // Empty or dead - would be toxified (neutral, no points)
                }
                else if (prevCell.IsAlive && !prevCell.IsResistant && prevCell.OwnerPlayerId != player.PlayerId)
                {
                    // Enemy living cell - would be killed and toxified
                    poisonedScore += 3f * TargetRankMultiplier(prevCell.OwnerPlayerId);
                }
                // Own living cells are not overwritten with toxin
            }

            // Calculate score based on the proposed scoring system. infestedScore and poisonedScore
            // already fold in the per-target rank weighting; reclaim of own dead cells is not enemy
            // damage and stays unweighted.
            float score = infestedScore + (reclaimed * 2f) + poisonedScore;
            // Penalize for wasting opportunities on own cells
            score -= wastedOnOwn * 2f;
            return Math.Max(0f, score);
        }

        /// <summary>
        /// Builds a per-player score multiplier used to bias Jetting Mycelium placement toward
        /// damaging higher-ranked players. Players are ranked by current living cell count: the
        /// trailing player maps to 1.0 and the leader maps to
        /// (1.0 + <see cref="MycovariantGameBalance.JettingMyceliumTargetRankScoreBonus"/>), with
        /// intermediate ranks interpolated linearly. Players tied on living cell count share the
        /// higher multiplier. Players with no living cells are omitted (they cannot be a target).
        /// </summary>
        public static Dictionary<int, float> BuildTargetRankMultipliers(GameBoard board)
        {
            var multipliers = new Dictionary<int, float>();
            float bonus = MycovariantGameBalance.JettingMyceliumTargetRankScoreBonus;
            if (bonus <= 0f)
                return multipliers;

            var ranked = BoardUtilities.GetPlayerBoardSummaries(board.Players, board)
                .Where(entry => entry.Value.LivingCells > 0)
                .OrderByDescending(entry => entry.Value.LivingCells)
                .ToList();

            if (ranked.Count == 0)
                return multipliers;

            if (ranked.Count == 1)
            {
                multipliers[ranked[0].Key] = 1f + bonus;
                return multipliers;
            }

            for (int i = 0; i < ranked.Count; i++)
            {
                int livingCells = ranked[i].Value.LivingCells;
                // Standard competition ranking: tied players take the best (lowest) rank index.
                int rankIndex = ranked.FindIndex(entry => entry.Value.LivingCells == livingCells);
                float fraction = 1f - (rankIndex / (float)(ranked.Count - 1));
                multipliers[ranked[i].Key] = 1f + (bonus * fraction);
            }

            return multipliers;
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
            => FindBestPlacement(player, board, direction, MycovariantIds.JettingMyceliumIId);

        /// <summary>
        /// Finds the best placement for Jetting Mycelium from any of the player's living cells in the given direction.
        /// </summary>
        public static (FungalCell sourceCell, float score)? FindBestPlacement(Player player, GameBoard board, CardinalDirection direction, int mycovariantId)
        {
            var livingCells = board.GetAllCellsOwnedBy(player.PlayerId)
                .Where(c => c.IsAlive)
                .ToList();

            if (livingCells.Count == 0) return null;

            var targetRankMultipliers = BuildTargetRankMultipliers(board);
            float bestScore = -1f;
            FungalCell? bestCell = null;

            foreach (var cell in livingCells)
            {
                float score = EvaluatePlacement(cell, direction, board, player, mycovariantId, targetRankMultipliers);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestCell = cell;
                }
            }

            return bestCell != null ? (bestCell, bestScore) : null;
        }

        /// <summary>
        /// Finds the best placement for Jetting Mycelium across every living source cell and cardinal direction.
        /// </summary>
        public static (FungalCell sourceCell, CardinalDirection direction, float score)? FindBestPlacement(Player player, GameBoard board)
            => FindBestPlacement(player, board, MycovariantIds.JettingMyceliumIId);

        /// <summary>
        /// Finds the best placement for Jetting Mycelium across every living source cell and cardinal direction.
        /// </summary>
        public static (FungalCell sourceCell, CardinalDirection direction, float score)? FindBestPlacement(Player player, GameBoard board, int mycovariantId)
        {
            var livingCells = board.GetAllCellsOwnedBy(player.PlayerId)
                .Where(c => c.IsAlive)
                .ToList();

            if (livingCells.Count == 0) return null;

            var targetRankMultipliers = BuildTargetRankMultipliers(board);
            float bestScore = -1f;
            FungalCell? bestCell = null;
            CardinalDirection bestDirection = CardinalDirection.North;

            foreach (CardinalDirection direction in Enum.GetValues(typeof(CardinalDirection)))
            {
                foreach (var cell in livingCells)
                {
                    float score = EvaluatePlacement(cell, direction, board, player, mycovariantId, targetRankMultipliers);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestCell = cell;
                        bestDirection = direction;
                    }
                }
            }

            return bestCell != null ? (bestCell, bestDirection, bestScore) : null;
        }
    }
}
