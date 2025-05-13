using System;
using System.Collections.Generic;
using FungusToast.Core.Board;
using FungusToast.Core.Players;
using FungusToast.Core.Mutations;

namespace FungusToast.Core.Growth
{
    public static class GrowthEngine
    {
        private static readonly Random rng = new();

        public static void ExecuteGrowthCycle(GameBoard board, List<Player> players)
        {
            List<(BoardTile tile, FungalCell fungalCell)> activeFungalCells = new();

            // Step 1: Collect all living fungal cells
            foreach (BoardTile tile in board.AllTiles())
            {
                if (tile.FungalCell != null && tile.FungalCell.IsAlive)
                {
                    activeFungalCells.Add((tile, tile.FungalCell));
                }
            }

            // Step 2: Shuffle list
            Shuffle(activeFungalCells);

            // Step 3: Process each fungal cell
            foreach (var (tile, fungalCell) in activeFungalCells)
            {
                Player owner = players[fungalCell.OwnerPlayerId];
                TryExpandFromTile(board, tile, owner);
            }
        }

        private static void TryExpandFromTile(GameBoard board, BoardTile sourceTile, Player owner)
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

            float multiplier = 1f + owner.GetMutationEffect(MutationType.TendrilDirectionalMultiplier);

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
                BoardTile tile = board.GetTile(nx, ny);
                if (tile != null && !tile.IsOccupied && chance > 0)
                {
                    allTargets.Add((tile, chance));
                }
            }

            // Attempt to grow into one random valid neighbor
            Shuffle(allTargets);

            foreach ((BoardTile neighbor, float chance) in allTargets)
            {
                float roll = (float)rng.NextDouble();
                if (roll <= chance)
                {
                    int tileId = neighbor.Y * board.Width + neighbor.X;
                    var newCell = new FungalCell(owner.PlayerId, tileId);
                    neighbor.PlaceFungalCell(newCell);
                    board.RegisterCell(newCell);
                    owner.ControlledTileIds.Add(tileId);
                    break;
                }
            }
        }

        private static void Shuffle<T>(List<T> list)
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
