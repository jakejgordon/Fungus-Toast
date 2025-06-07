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
        public static Dictionary<int, int> ExecuteGrowthCycle(
            GameBoard board,
            List<Player> players,
            Random rng,
            IGrowthObserver? observer = null)
        {
            // Expire toxins before growth begins
            board.ExpireToxinTiles(board.CurrentGrowthCycle);

            foreach (var player in players)
            {
                MutationEffectProcessor.ApplyMycotoxinCatabolism(player, board, rng, observer);
            }

            var failedGrowthsByPlayerId = players.ToDictionary(p => p.PlayerId, _ => 0);
            var activeFungalCells = board.AllLivingFungalCellsWithTiles().ToList();

            Shuffle(activeFungalCells, rng);

            foreach (var (tile, cell) in activeFungalCells)
            {
                if (!cell.OwnerPlayerId.HasValue)
                    continue;

                var owner = players[cell.OwnerPlayerId.Value];
                bool grewOrMoved = TryExpandFromTile(board, tile, cell, owner, rng, observer);

                if (!grewOrMoved)
                    failedGrowthsByPlayerId[owner.PlayerId]++;
            }

            board.IncrementGrowthCycle();

            return failedGrowthsByPlayerId;
        }



        private static bool TryExpandFromTile(
            GameBoard board,
            BoardTile sourceTile,
            FungalCell sourceCell,
            Player owner,
            Random rng,
            IGrowthObserver? observer)
        {
            var allTargets = new List<(BoardTile tile, float chance)>();

            foreach (BoardTile tile in board.GetOrthogonalNeighbors(sourceTile.X, sourceTile.Y))
            {
                if (!tile.IsOccupied && tile.TileId != sourceTile.TileId)
                    allTargets.Add((tile, owner.GetEffectiveGrowthChance()));
            }

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
                if (chance <= 0) continue;
                int nx = sourceTile.X + dx;
                int ny = sourceTile.Y + dy;
                var maybeTile = board.GetTile(nx, ny);
                if (maybeTile is { IsOccupied: false, TileId: var id } && id != sourceTile.TileId)
                    allTargets.Add((maybeTile, chance));
            }

            Shuffle(allTargets, rng);

            bool attemptedCreepingMold = false;

            foreach ((BoardTile neighbor, float chance) in allTargets)
            {
                if (rng.NextDouble() <= chance)
                {
                    var newCell = new FungalCell(owner.PlayerId, neighbor.TileId);
                    neighbor.PlaceFungalCell(newCell);
                    board.PlaceFungalCell(newCell);
                    owner.AddControlledTile(neighbor.TileId);
                    return true; // successful growth
                }
                else if (!attemptedCreepingMold)
                {
                    attemptedCreepingMold = true;
                    if (MutationEffectProcessor.TryCreepingMoldMove(owner, sourceCell, sourceTile, neighbor, rng, board))
                    {
                        observer?.RecordCreepingMoldMove(owner.PlayerId);
                        return true; // successful creeping mold
                    }
                }
            }

            return false; // failed to grow or move
        }


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

    }



    public enum DiagonalDirection
    {
        Northwest,
        Northeast,
        Southeast,
        Southwest
    }
}
