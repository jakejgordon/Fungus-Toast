using FungusToast.Core.Config;
using FungusToast.Core.Mutations;
using FungusToast.Core.Phases;
using FungusToast.Core.Players;
using FungusToast.Core.Metrics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FungusToast.Core.Death
{
    public static class DeathEngine
    {
        private static readonly Random rng = new();
        private static bool bloomActivated = false;

        public static void ExecuteDeathCycle(
            GameBoard board,
            List<Player> players,
            ISporeDropObserver? observer = null)
        {
            var (allMutations, _) = MutationRepository.BuildFullMutationSet();
            var sporocidalBloom = allMutations[MutationIds.SporocidalBloom];
            var necrophyticBloom = allMutations[MutationIds.NecrophyticBloom];

            // 🔁 Drop toxic spores from living cells (Sporocidal Bloom)
            foreach (var player in players)
            {
                MutationEffectProcessor.TryPlaceSporocidalSpores(player, board, rng, sporocidalBloom, observer);
            }

            // 💀 Check board occupation for Necrophytic Bloom activation
            float occupiedPercent = (float)(board.GetAllCells().Count) / (GameBalance.BoardWidth * GameBalance.BoardHeight);

            if (!bloomActivated && occupiedPercent >= 0.2f)
            {
                bloomActivated = true;

                foreach (var player in players)
                {
                    if (player.GetMutationLevel(MutationIds.NecrophyticBloom) > 0)
                    {
                        MutationEffectProcessor.HandleNecrophyticBloomSporeDrop(
                            player, board, rng, occupiedPercent, observer);
                    }
                }
            }

            // ☠️ Begin death evaluation of all living cells
            var livingTiles = board.AllTiles()
                                   .Where(t => t.FungalCell != null && t.FungalCell.IsAlive)
                                   .ToList();

            foreach (var tile in livingTiles)
            {
                var cell = tile.FungalCell!;
                var player = players.FirstOrDefault(p => p.PlayerId == cell.OwnerPlayerId);

                if (player == null)
                {
                    Console.WriteLine($"[Warning] No player found for PlayerId {cell.OwnerPlayerId}");
                    continue;
                }

                // 🔐 Don’t kill last living cell
                if (player.ControlledTileIds.Count <= 1)
                    continue;

                double roll = rng.NextDouble();
                var (deathChance, reason) = MutationEffectProcessor.CalculateDeathChance(
                    player, cell, board, players, roll);

                if (reason.HasValue && roll < deathChance)
                {
                    cell.Kill(reason.Value);
                    player.ControlledTileIds.Remove(cell.TileId);
                    MutationEffectProcessor.TryTriggerSporeOnDeath(player, board, rng, observer);

                    if (bloomActivated && player.GetMutationLevel(MutationIds.NecrophyticBloom) > 0)
                    {
                        MutationEffectProcessor.HandleNecrophyticBloomSporeDrop(
                            player, board, rng, occupiedPercent, observer);
                    }
                }
                else
                {
                    MutationEffectProcessor.AdvanceOrResetCellAge(player, cell);
                }
            }
        }

        public static bool IsCellSurrounded(int tileId, GameBoard board)
        {
            var cell = board.GetCell(tileId);
            if (cell == null) return false;

            foreach (int nId in board.GetAdjacentTileIds(tileId))
            {
                var n = board.GetCell(nId);
                if (n == null || !n.IsAlive) return false;
            }

            return true;
        }
    }
}
