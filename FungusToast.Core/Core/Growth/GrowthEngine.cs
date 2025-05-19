using FungusToast.Core.Board;
using FungusToast.Core.Mutations;
using FungusToast.Core.Phases;
using FungusToast.Core.Players;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace FungusToast.Core.Growth
{
    public static class GrowthEngine
    {
        public static void ExecuteGrowthCycle(GameBoard board, List<Player> players, Random rng)
        {
            var initialLivingCells = board
                .GetAllCells()
                .Where(cell => cell.IsAlive)
                .ToList();

            List<(BoardTile tile, FungalCell cell)> activeFungalCells = new();

            foreach (var cell in initialLivingCells)
            {
                var tile = board.GetTileById(cell.TileId);
                if (tile != null)
                {
                    activeFungalCells.Add((tile, cell));
                }
            }

            Shuffle(activeFungalCells, rng);

            foreach (var (tile, cell) in activeFungalCells)
            {
                var owner = players[cell.OwnerPlayerId];
                TryExpandFromTile(board, tile, owner, rng);
            }
        }



        private static void TryExpandFromTile(GameBoard board, BoardTile sourceTile, Player owner, Random rng)
        {
            List<(BoardTile tile, float chance)> allTargets = new();

            // Orthogonal directions
            foreach (BoardTile tile in board.GetOrthogonalNeighbors(sourceTile.X, sourceTile.Y))
            {
                if (!tile.IsOccupied)
                {
                    allTargets.Add((tile, owner.GetEffectiveGrowthChance()));
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
                int nx = sourceTile.X + dx;
                int ny = sourceTile.Y + dy;
                var maybeTile = board.GetTile(nx, ny);
                if (maybeTile is { } tile && !tile.IsOccupied && chance > 0)
                {
                    allTargets.Add((tile, chance));
                }
            }

            // Shuffle and attempt to grow
            Shuffle(allTargets, rng);

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
                        Console.WriteLine($"[Growth] {sourceTile.TileId} grew into {tileId} with roll {roll:0.000} <= chance {chance:0.000}");
                    }
                    else
                    {
                        Console.WriteLine($"⚠️ Growth blocked — target tile {neighbor.TileId} was already occupied.");
                    }

                    break; // Only attempt one successful growth per cycle
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
