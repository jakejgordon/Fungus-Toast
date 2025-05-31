using FungusToast.Core.Board;
using FungusToast.Core.Core.Metrics;
using FungusToast.Core.Mutations;
using FungusToast.Core.Phases;
using FungusToast.Core.Players;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace FungusToast.Core.Growth
{
    public static class GrowthEngine
    {
        public static void ExecuteGrowthCycle(GameBoard board, List<Player> players, Random rng, IGrowthObserver? observer = null)
        {
            List<(BoardTile tile, FungalCell cell)> activeFungalCells = new();

            foreach (var tile in board.AllTiles())
            {
                tile.DecrementToxinTimer();

                if (tile.IsAlive)
                    activeFungalCells.Add((tile, tile.FungalCell!));
            }

            Shuffle(activeFungalCells, rng);

            foreach (var (tile, cell) in activeFungalCells)
            {
                var owner = players[cell.OwnerPlayerId];
                TryExpandFromTile(board, tile, cell, owner, rng, observer);
            }
        }


        private static void TryExpandFromTile(GameBoard board, BoardTile sourceTile, FungalCell sourceCell, Player owner, Random rng, IGrowthObserver? observer)
        {
            List<(BoardTile tile, float chance)> allTargets = new();

            // Orthogonal directions
            foreach (BoardTile tile in board.GetOrthogonalNeighbors(sourceTile.X, sourceTile.Y))
            {
                if (!tile.IsOccupied && tile.TileId != sourceTile.TileId)
                {
                    allTargets.Add((tile, owner.GetEffectiveGrowthChance()));
                }
                else if (tile.TileId == sourceTile.TileId)
                {
                    Console.WriteLine($"⚠️ Skipping self in orthogonal neighbors: TileId {tile.TileId}");
                }
            }

            // Diagonal directions with mutation-based multiplier
            float multiplier = MutationEffectProcessor.GetDiagonalGrowthMultiplier(owner);

            var diagonals = new (int dx, int dy, float chance)[]
            {
        (-1,  1, owner.GetDiagonalGrowthChance(DiagonalDirection.Northwest) * multiplier),
        ( 1,  1, owner.GetDiagonalGrowthChance(DiagonalDirection.Northeast) * multiplier),
        ( 1, -1, owner.GetDiagonalGrowthChance(DiagonalDirection.Southeast) * multiplier),
        (-1, -1, owner.GetDiagonalGrowthChance(DiagonalDirection.Southwest) * multiplier),
            };

            foreach (var (dx, dy, chance) in diagonals)
            {
                if (chance <= 0)
                    continue;

                int nx = sourceTile.X + dx;
                int ny = sourceTile.Y + dy;
                var maybeTile = board.GetTile(nx, ny);

                if (maybeTile is { } tile)
                {
                    if (!tile.IsOccupied && tile.TileId != sourceTile.TileId)
                    {
                        allTargets.Add((tile, chance));
                    }
                    else if (tile.TileId == sourceTile.TileId)
                    {
                        Console.WriteLine($"⚠️ Skipping self in diagonal neighbors: TileId {tile.TileId}");
                    }
                }
            }

            // Shuffle and attempt to grow
            Shuffle(allTargets, rng);

            bool attemptedCreepingMold = false;

            foreach ((BoardTile neighbor, float chance) in allTargets)
            {
                float roll = (float)rng.NextDouble();
                if (roll <= chance)
                {
                    if (!neighbor.IsOccupied)
                    {
                        int tileId = neighbor.TileId;
                        var newCell = new FungalCell(owner.PlayerId, tileId);
                        neighbor.PlaceFungalCell(newCell);
                        board.RegisterCell(newCell);
                        owner.AddControlledTile(tileId);
                    }
                    break; // successful growth ends turn
                }
                else if (!attemptedCreepingMold && !neighbor.IsOccupied)
                {
                    attemptedCreepingMold = true;
                    bool crept = MutationEffectProcessor.TryCreepingMoldMove(owner, sourceCell, sourceTile, neighbor, rng, board);

                    if (crept)
                    {
                        observer?.RecordCreepingMoldMove(owner.PlayerId);
                        break; // successful move ends turn
                    }
                }
            }
        }

        private static void Shuffle<T>(List<T> list, Random rng)
        {
            for (int i = 0; i < list.Count; i++)
            {
                int j = rng.Next(i, list.Count);
                (list[i], list[j]) = (list[j], list[i]);
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
