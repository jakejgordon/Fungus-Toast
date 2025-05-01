using System.Collections.Generic;
using FungusToast.Core.Board;
using FungusToast.Core.Players;
using UnityEngine;

namespace FungusToast.Core.Growth
{
    public static class GrowthEngine
    {
        public static void ExecuteGrowthCycle(GameBoard board, List<Player> players)
        {
            List<(BoardTile tile, FungalCell fungalCell)> activeFungalCells = new List<(BoardTile, FungalCell)>();

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
            List<BoardTile> neighbors = board.GetOrthogonalNeighbors(sourceTile.X, sourceTile.Y);

            foreach (BoardTile neighbor in neighbors)
            {
                if (!neighbor.IsOccupied)
                {
                    float roll = Random.Range(0f, 1f);
                    if (roll <= owner.GrowthChance)
                    {
                        int tileId = neighbor.Y * board.Width + neighbor.X;
                        var newCell = new FungalCell(owner.PlayerId, tileId);

                        neighbor.PlaceFungalCell(newCell);
                        board.RegisterCell(newCell); // Ensure it's trackable via GameBoard.GetCell

                        break; // Only one growth per fungal cell per cycle
                    }
                }
            }
        }


        private static void Shuffle<T>(List<T> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                int j = Random.Range(i, list.Count);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
