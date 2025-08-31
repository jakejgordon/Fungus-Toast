using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Growth;
using FungusToast.Core.Metrics;
using FungusToast.Core.Mutations;
using FungusToast.Core.Players;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FungusToast.Core.Phases
{
    /// <summary>
    /// Handles all mutation effects related to the Growth category.
    /// </summary>
    public static class GrowthMutationProcessor
    {
        /// <summary>
        /// Gets the diagonal growth multiplier for tendril mutations (Mycotropic Induction).
        /// </summary>
        public static float GetTendrilDiagonalGrowthMultiplier(Player player)
        {
            return 1f + player.GetMutationEffect(MutationType.TendrilDirectionalMultiplier);
        }

        /// <summary>
        /// Attempts to move a living fungal cell (Creeping Mold mutation effect) from source to target tile.
        /// </summary>
        public static bool TryCreepingMoldMove(
            Player player,
            FungalCell sourceCell,
            BoardTile sourceTile,
            BoardTile targetTile,
            Random rng,
            GameBoard board,
            ISimulationObserver observer)
        {
            bool hasMaxCreepingMold = player.PlayerMutations.TryGetValue(MutationIds.CreepingMold, out var cm) &&
                                      cm.CurrentLevel == GameBalance.CreepingMoldMaxLevel;
            bool targetIsToxin = targetTile.FungalCell != null && targetTile.FungalCell.IsToxin;
            bool specialToxinJumpCase = hasMaxCreepingMold && targetIsToxin;

            // Only allow occupied tiles if it's the special toxin jump case
            if (targetTile.IsOccupied && !specialToxinJumpCase) return false;

            float moveChance =
                cm != null ? cm.CurrentLevel * GameBalance.CreepingMoldMoveChancePerLevel : 0f;

            // Handle the special toxin jump case
            if (specialToxinJumpCase) {
                // Only allow for cardinal directions
                int dx = targetTile.X - sourceTile.X;
                int dy = targetTile.Y - sourceTile.Y;
                bool isCardinal = (dx == 0 && Math.Abs(dy) == 1) || (dy == 0 && Math.Abs(dx) == 1);
                if (!isCardinal) return false;

                // Compute the tile beyond the toxin in the same direction
                int jumpX = targetTile.X + dx;
                int jumpY = targetTile.Y + dy;
                var jumpTile = board.GetTile(jumpX, jumpY);
                if (jumpTile != null && !jumpTile.IsOccupied && (jumpTile.FungalCell == null || !jumpTile.FungalCell.IsToxin)) {
                    if (rng.NextDouble() <= moveChance) {
                        int sourceOpen = board.GetOrthogonalNeighbors(sourceTile.X, sourceTile.Y)
                                                .Count(n => !n.IsOccupied);
                        int targetOpen = board.GetOrthogonalNeighbors(jumpTile.X, jumpTile.Y)
                                                .Count(n => !n.IsOccupied);
                        if (targetOpen >= sourceOpen && targetOpen >= 2) {
                            CreateCreepingMoldCell(player, sourceCell, sourceTile, jumpTile, board);
                            observer.RecordCreepingMoldToxinJump(player.PlayerId);
                            return true;
                        }
                    }
                }
                return false;
            }

            // Standard Creeping Mold move
            if (rng.NextDouble() > moveChance) return false;

            // Count open (unoccupied) orthogonal neighbors for source and target
            int sourceOpenStandard = board.GetOrthogonalNeighbors(sourceTile.X, sourceTile.Y)
                                  .Count(n => !n.IsOccupied);
            int targetOpenStandard = board.GetOrthogonalNeighbors(targetTile.X, targetTile.Y)
                                  .Count(n => !n.IsOccupied);

            // Only allow the move if the target is at least as open as the source,
            // and the target has at least 2 open sides. This prevents mold from
            // sliding into dead ends or more enclosed spaces, encouraging spreading
            // into open areas and keeping the mutation balanced and fun.
            if (targetOpenStandard < sourceOpenStandard || targetOpenStandard < 2) return false;

            CreateCreepingMoldCell(player, sourceCell, sourceTile, targetTile, board);
            return true;
        }

        /// <summary>
        /// Gets the base growth chance and surge bonus for Hyphal Surge.
        /// </summary>
        public static (float baseChance, float surgeBonus) GetGrowthChancesWithHyphalSurge(Player player)
        {
            float baseChance = GameBalance.BaseGrowthChance + player.GetMutationEffect(MutationType.GrowthChance);

            int hyphalSurgeId = MutationIds.HyphalSurge;
            float surgeBonus = 0f;
            if (player.IsSurgeActive(hyphalSurgeId))
            {
                int surgeLevel = player.GetMutationLevel(hyphalSurgeId);
                surgeBonus = surgeLevel * GameBalance.HyphalSurgeEffectPerLevel;
                surgeBonus = player.GetMutationEffect(MutationType.HyphalSurge);
            }
            return (baseChance, surgeBonus);
        }

        // Helper for creating and moving a Creeping Mold cell
        private static void CreateCreepingMoldCell(Player player, FungalCell sourceCell, BoardTile sourceTile, BoardTile targetTile, GameBoard board)
        {
            // 1) Remove control and the source cell first to avoid transient double-ownership or stale highlights
            board.RemoveControlFromPlayer(sourceCell.TileId);
            sourceTile.RemoveFungalCell();

            // 2) Place the new living cell at the target
            var newCell = new FungalCell(player.PlayerId, targetTile.TileId, GrowthSource.CreepingMold);
            targetTile.PlaceFungalCell(newCell); // Delegate placement to the target tile
        }
    }
}
