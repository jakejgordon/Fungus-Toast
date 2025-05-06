using System.Collections.Generic;
using UnityEngine;
using FungusToast.Core.Board;
using FungusToast.Core.Players;
using FungusToast.Core.Config;

namespace FungusToast.Core.Growth
{
    public static class DeathEngine
    {
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

        int playerLivingCells = player.ControlledTileIds.Count;
        if (playerLivingCells <= 1)
            continue;

        float baseChance = GameBalance.BaseDeathChance;
        float ageModifier = cell.GrowthCycleAge * GameBalance.AgeDeathFactor;
        float defenseBonus = player.GetEffectiveSelfDeathChance();
        float pressure = GetEnemyPressure(players, player, cell, board);

        float finalChance = baseChance + ageModifier + pressure - defenseBonus;
        finalChance = Mathf.Clamp01(finalChance);

        float roll = (float)rng.NextDouble();
        if (roll < finalChance)
        {
            cell.Kill();
            player.ControlledTileIds.Remove(cell.TileId);
            Debug.Log($"💀 Cell at ({tile.X},{tile.Y}) owned by Player {player.PlayerId} died. Age={cell.GrowthCycleAge}, FinalChance={finalChance:P2}, Roll={roll:P2}");
        }
        else
        {
            int threshold = player.GetSelfAgeResetThreshold();
            if (cell.GrowthCycleAge >= threshold)
            {
                cell.ResetGrowthCycleAge();
                Debug.Log($"♻️ Cell at ({tile.X},{tile.Y}) rejuvenated (age reset to 0). Previous Age={threshold}");
            }
            else
            {
                cell.IncrementGrowthAge();
            }

        }
    }
}


        private static float GetEnemyPressure(List<Player> allPlayers, Player currentPlayer, FungalCell targetCell, GameBoard board)
        {
            float pressure = 0f;

            foreach (var enemy in allPlayers)
            {
                if (enemy.PlayerId != currentPlayer.PlayerId)
                {
                    pressure += enemy.GetOffensiveDecayModifierAgainst(targetCell, board);
                }
            }

            return pressure;
        }

        public static bool IsCellSurrounded(int tileId, GameBoard board)
        {
            var cell = board.GetCell(tileId);
            if (cell == null) return false;

            var neighborIds = board.GetAdjacentTileIds(tileId);
            foreach (int neighborId in neighborIds)
            {
                var neighborCell = board.GetCell(neighborId);
                if (neighborCell == null || !neighborCell.IsAlive)
                    return false;
            }

            return true;
        }
    }
}
