using FungusToast.Core.Board;
using FungusToast.Core.Phases;
using FungusToast.Core.Metrics;
using FungusToast.Core.Mutations;
using FungusToast.Core.Phases;
using FungusToast.Core.Players;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FungusToast.Core.Growth
{
    public static class GrowthEngine
    {
        public static Dictionary<int, int> ExecuteGrowthCycle(
            GameBoard board,
            List<Player> players,
            Random rng,
            RoundContext roundContext,
            ISimulationObserver? observer = null)
        {
            // Apply round-capped mutation effects first (e.g., Mycotoxin Catabolism)
            foreach (var player in players)
            {
                MutationEffectProcessor.ApplyMycotoxinCatabolism(
                    player, board, rng, roundContext, observer
                );
            }

            // Track failed growths for simulation analysis
            var failedGrowthsByPlayerId = players.ToDictionary(p => p.PlayerId, _ => 0);
            var activeFungalCells = board.AllLivingFungalCellsWithTiles().ToList();

            Shuffle(activeFungalCells, rng);

            foreach (var (tile, cell) in activeFungalCells)
            {
                if (!cell.OwnerPlayerId.HasValue)
                    continue;

                var owner = players[cell.OwnerPlayerId.Value];
                bool grewOrMoved = TryExpandFromTile(board, tile, owner, rng, observer);

                if (!grewOrMoved)
                    failedGrowthsByPlayerId[owner.PlayerId]++;
            }

            board.IncrementGrowthCycle();

            return failedGrowthsByPlayerId;
        }



        /// <summary>
        /// Attempts to grow or move from a single tile. Tracks orthogonal, diagonal (Tendril), Creeping Mold, and Necrohyphal Infiltration.
        /// </summary>
        private static bool TryExpandFromTile(
            GameBoard board,
            BoardTile sourceTile,
            Player owner,
            Random rng,
            ISimulationObserver? observer = null)
        {
            var sourceCell = sourceTile.FungalCell;
            if (sourceCell == null)
                return false; // No cell to expand from

            // Get base and surge growth chances
            (float baseChance, float surgeBonus) = MutationEffectProcessor.GetGrowthChancesWithHyphalSurge(owner);

            // 1. Collect all orthogonal and diagonal (with direction) targets
            var allTargets = new List<GrowthTarget>();

            // Orthogonal neighbors (normal growth)
            foreach (BoardTile tile in board.GetOrthogonalNeighbors(sourceTile.X, sourceTile.Y))
            {
                if (!tile.IsOccupied && tile.TileId != sourceTile.TileId)
                    allTargets.Add(new GrowthTarget(tile, baseChance, null, surgeBonus));
            }

            // Diagonal neighbors (Tendril growth)
            float multiplier = MutationEffectProcessor.GetTendrilDiagonalGrowthMultiplier(owner);
            var diagonalDirs = new (int dx, int dy, DiagonalDirection dir)[]
            {
                (-1,  1, DiagonalDirection.Northwest),
                ( 1,  1, DiagonalDirection.Northeast),
                ( 1, -1, DiagonalDirection.Southeast),
                (-1, -1, DiagonalDirection.Southwest),
            };
            foreach (var (dx, dy, dir) in diagonalDirs)
            {
                float chance = owner.GetDiagonalGrowthChance(dir) * multiplier;
                if (chance <= 0) continue;
                int nx = sourceTile.X + dx;
                int ny = sourceTile.Y + dy;
                var maybeTile = board.GetTile(nx, ny);
                if (maybeTile is { IsOccupied: false, TileId: var id } && id != sourceTile.TileId)
                    allTargets.Add(new GrowthTarget(maybeTile, chance, dir, 0f)); // Tendrils not affected by Hyphal Surge unless you want to include it
            }

            Shuffle(allTargets, rng);

            // 2. Attempt growth in each direction, reporting type for Tendril diagonals
            bool attemptedCreepingMold = false;

            foreach (var target in allTargets)
            {
                double roll = rng.NextDouble();

                // Only orthogonal targets get Hyphal Surge bonus
                if (target.SurgeBonus > 0f && target.DiagonalDirection == null)
                {
                    if (roll < target.Chance)
                    {
                        // Normal growth
                        PlaceCellAndTrack(target, owner, board, observer, false);
                        return true;
                    }
                    else if (roll < target.Chance + target.SurgeBonus)
                    {
                        // Hyphal Surge growth
                        PlaceCellAndTrack(target, owner, board, observer, true);
                        observer?.RecordHyphalSurgeGrowth(owner.PlayerId);
                        return true;
                    }
                }
                else
                {
                    // Tendril or other non-surge directions
                    if (roll < target.Chance)
                    {
                        PlaceCellAndTrack(target, owner, board, observer, false);

                        // Track Tendril mutation usage if this was a diagonal
                        if (target.DiagonalDirection.HasValue)
                        {
                            observer?.RecordTendrilGrowth(owner.PlayerId, target.DiagonalDirection.Value);
                        }

                        return true;
                    }
                }

                // If not successful, try Creeping Mold (once)
                if (!attemptedCreepingMold)
                {
                    attemptedCreepingMold = true;
                    if (MutationEffectProcessor.TryCreepingMoldMove(owner, sourceCell, sourceTile, target.Tile, rng, board))
                    {
                        observer?.RecordCreepingMoldMove(owner.PlayerId);
                        return true; // successful creeping mold
                    }
                }
            }

            // 3. Fallback: Try Necrohyphal Infiltration if enabled
            if (MutationEffectProcessor.TryNecrohyphalInfiltration(
                    board, sourceTile, sourceCell, owner, rng, observer))
            {
                return true; // successful necrohyphal infiltration
            }

            return false; // failed to grow, move, or infiltrate
        }

        private static void PlaceCellAndTrack(
            GrowthTarget target,
            Player owner,
            GameBoard board,
            ISimulationObserver? observer,
            bool isHyphalSurge)
        {
            var newCell = new FungalCell(owner.PlayerId, target.Tile.TileId);
            target.Tile.PlaceFungalCell(newCell);
            board.PlaceFungalCell(newCell);
            owner.AddControlledTile(target.Tile.TileId);

            if (isHyphalSurge)
            {
                observer?.RecordHyphalSurgeGrowth(owner.PlayerId);
            }
        }

        /// <summary>
        /// Helper for shuffling lists with a given RNG.
        /// </summary>
        private static void Shuffle<T>(List<T> list, Random rng)
        {
            for (int i = 0; i < list.Count; i++)
            {
                int j = rng.Next(i, list.Count);
                T temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }

        /// <summary>
        /// Encapsulates a potential growth target, including diagonal direction if applicable.
        /// </summary>
        private sealed class GrowthTarget
        {
            public BoardTile Tile { get; }
            public float Chance { get; }
            public DiagonalDirection? DiagonalDirection { get; }
            public float SurgeBonus { get; } // Only for orthogonal targets

            public GrowthTarget(BoardTile tile, float chance, DiagonalDirection? dir, float surgeBonus)
            {
                Tile = tile;
                Chance = chance;
                DiagonalDirection = dir;
                SurgeBonus = surgeBonus;
            }
        }
    }

    public enum DiagonalDirection
    {
        Northwest,
        Northeast,
        Southeast,
        Southwest
    }
}
