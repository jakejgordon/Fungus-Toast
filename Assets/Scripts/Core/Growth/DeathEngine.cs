using System.Collections.Generic;
using UnityEngine;
using FungusToast.Core.Board;
using FungusToast.Core.Players;

namespace FungusToast.Core.Growth
{
    public static class DeathEngine
    {
        public static float BaseDeathChance = 0.0005f;

        public static void ExecuteDeathCycle(GameBoard board, List<Player> players)
        {
            List<BoardTile> allLivingCells = new List<BoardTile>();

            foreach (var tile in board.AllTiles())
            {
                if (tile.FungalCell != null && tile.FungalCell.IsAlive)
                {
                    allLivingCells.Add(tile);
                }
            }

            System.Random rng = new System.Random();

            foreach (var tile in allLivingCells)
            {
                var cell = tile.FungalCell;
                var player = players.Find(p => p.PlayerId == cell.OwnerPlayerId);
                if (player == null)
                {
                    Debug.LogWarning($"No player found for PlayerId {cell.OwnerPlayerId}");
                    continue;
                }

                // Ensure player retains at least one cell
                int playerLivingCells = CountLivingCells(board, player.PlayerId);
                if (playerLivingCells <= 1)
                    continue;

                float baseChance = BaseDeathChance;
                float ageModifier = cell.GrowthCycleAge * BaseDeathChance;
                float defenseBonus = player.GetEffectiveSelfDeathChance(); // reduces death chance
                float enemyPressure = GetEnemyPressure(players, player);  // increases death chance

                float finalChance = baseChance + ageModifier + enemyPressure - defenseBonus;
                finalChance = Mathf.Clamp01(finalChance);

                float roll = (float)rng.NextDouble();
                if (roll < finalChance)
                {
                    cell.Kill();
                    Debug.Log($"\u2620 Cell at ({tile.X},{tile.Y}) owned by Player {player.PlayerId} died. FinalChance={finalChance:P2}, Roll={roll:P2}");
                }
                else
                {
                    cell.IncrementGrowthAge();
                }
            }
        }

        private static int CountLivingCells(GameBoard board, int playerId)
        {
            int count = 0;
            foreach (var tile in board.AllTiles())
            {
                if (tile.FungalCell != null &&
                    tile.FungalCell.OwnerPlayerId == playerId &&
                    tile.FungalCell.IsAlive)
                {
                    count++;
                }
            }
            return count;
        }

        private static float GetEnemyPressure(List<Player> allPlayers, Player currentPlayer)
        {
            float pressure = 0f;
            foreach (var enemy in allPlayers)
            {
                if (enemy.PlayerId != currentPlayer.PlayerId)
                {
                    pressure += enemy.GetEffectiveDeathChanceFrom(currentPlayer);
                }
            }
            return pressure;
        }
    }
}
