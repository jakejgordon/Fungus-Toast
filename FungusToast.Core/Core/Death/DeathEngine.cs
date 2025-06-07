using System;
using System.Collections.Generic;
using System.Linq;
using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Mutations;
using FungusToast.Core.Players;
using FungusToast.Core.Phases;   // MutationEffectProcessor
using FungusToast.Core.Metrics;

namespace FungusToast.Core.Death
{
    /// <summary>
    /// Orchestrates the Decay Phase for the entire board.
    /// Performs no mutation mathematics—delegates that to MutationEffectProcessor.
    /// </summary>
    public static class DeathEngine
    {
        private static readonly Random Rng = new();

        // Tracks whether the 20 %-occupied trigger for Necrophytic Bloom has fired.
        private static bool necrophyticActivated = false;

        public static void ExecuteDeathCycle(
            GameBoard board,
            List<Player> players,
            Dictionary<int, int> failedGrowthsByPlayerId,
            ISporeDropObserver? observer = null)
        {
            // Expire toxins before growth begins
            board.ExpireToxinTiles(board.CurrentGrowthCycle);
            List<Player> shuffledPlayers = players.OrderBy(_ => Rng.NextDouble()).ToList();

            var (allMutations, _) = MutationRepository.BuildFullMutationSet();
            Mutation sporocidalBloom = allMutations[MutationIds.SporocidalBloom];

            ApplyPerTurnSporeEffects(shuffledPlayers, board, sporocidalBloom, failedGrowthsByPlayerId, observer);
            ApplyNecrophyticBloomTrigger(shuffledPlayers, board, observer);
            MutationEffectProcessor.ApplyToxinAuraDeaths(board, players, Rng, observer);
            EvaluateProbabilisticDeaths(board, shuffledPlayers, observer);
        }

        private static void ApplyPerTurnSporeEffects(
            List<Player> players,
            GameBoard board,
            Mutation sporocidalBloom,
            Dictionary<int, int> failedGrowthsByPlayerId,
            ISporeDropObserver? observer)
        {
            foreach (var p in players)
            {
                MutationEffectProcessor.TryPlaceSporocidalSpores(p, board, Rng, sporocidalBloom, observer);

                int failedGrowths = failedGrowthsByPlayerId.TryGetValue(p.PlayerId, out var v) ? v : 0;
                MutationEffectProcessor.ApplyMycotoxinTracer(p, board, failedGrowths, Rng, observer);
            }
        }

        private static void ApplyNecrophyticBloomTrigger(
            List<Player> players,
            GameBoard board,
            ISporeDropObserver? observer)
        {
            float occupiedPercent = board.GetOccupiedTileRatio();

            if (!necrophyticActivated && occupiedPercent >= 0.20f)
            {
                necrophyticActivated = true;

                foreach (var p in players)
                {
                    if (p.GetMutationLevel(MutationIds.NecrophyticBloom) > 0)
                    {
                        MutationEffectProcessor.HandleNecrophyticBloomSporeDrop(
                            p, board, Rng, occupiedPercent, observer);
                    }
                }
            }
        }

        private static void EvaluateProbabilisticDeaths(
            GameBoard board,
            List<Player> players,
            ISporeDropObserver? observer)
        {
            var livingCellCounts = players.ToDictionary(
                p => p.PlayerId,
                p => board.GetAllCellsOwnedBy(p.PlayerId).Count(c => c.IsAlive));

            List<BoardTile> livingTiles = board.AllTiles()
                .Where(t => t.FungalCell is { IsAlive: true })
                .ToList();

            foreach (BoardTile tile in livingTiles)
            {
                FungalCell cell = tile.FungalCell!;
                Player owner = players.First(p => p.PlayerId == cell.OwnerPlayerId);

                if (livingCellCounts[owner.PlayerId] <= 1)
                    continue;

                double roll = Rng.NextDouble();
                (float _, DeathReason? reason) =
                    MutationEffectProcessor.CalculateDeathChance(owner, cell, board, players, roll);

                if (reason.HasValue)
                {
                    cell.Kill(reason.Value);
                    owner.RemoveControlledTile(cell.TileId);
                    livingCellCounts[owner.PlayerId]--;

                    MutationEffectProcessor.TryTriggerSporeOnDeath(owner, board, Rng, observer);

                    if (necrophyticActivated &&
                        owner.GetMutationLevel(MutationIds.NecrophyticBloom) > 0)
                    {
                        float occupiedPercent = board.GetOccupiedTileRatio();
                        MutationEffectProcessor.HandleNecrophyticBloomSporeDrop(
                            owner, board, Rng, occupiedPercent, observer);
                    }
                }
                else
                {
                    MutationEffectProcessor.AdvanceOrResetCellAge(owner, cell);
                }
            }
        }

        /// <summary>
        /// True if every neighboring tile (orthogonal and diagonal) of <paramref name="tileId"/> is occupied by a living cell.
        /// Used by Putrefactive Mycotoxin & Encysted Spore logic.
        /// </summary>
        public static bool IsCellSurrounded(int tileId, GameBoard board)
        {
            var adjacentTileIds = board.GetAdjacentTileIds(tileId);

            foreach (int neighborId in adjacentTileIds)
            {
                var neighbor = board.GetTileById(neighborId);

                if (neighbor == null || neighbor.FungalCell == null)
                    return false;
            }

            return true;
        }
    }
}
