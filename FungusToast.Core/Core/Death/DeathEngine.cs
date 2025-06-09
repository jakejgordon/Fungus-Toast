using System;
using System.Collections.Generic;
using System.Linq;
using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Mutations;
using FungusToast.Core.Players;
using FungusToast.Core.Phases;   // MutationEffectProcessor
using FungusToast.Core.Metrics;
using FungusToast.Core.Core.Metrics;

namespace FungusToast.Core.Death
{
    /// <summary>
    /// Orchestrates the Decay Phase for the entire board.
    /// Performs no mutation mathematics—delegates that to MutationEffectProcessor.
    /// </summary>
    public static class DeathEngine
    {
        private static readonly Random Rng = new();

        // Tracks whether the 20%-occupied trigger for Necrophytic Bloom has fired.
        private static bool necrophyticActivated = false;

        public static void ExecuteDeathCycle(
            GameBoard board,
            List<Player> players,
            Dictionary<int, int> failedGrowthsByPlayerId,
            ISporeDropObserver? sporeDropObserver = null,
            IGrowthAndDecayObserver? growthAndDecayObserver = null)
        {
            // Expire toxins before growth begins
            board.ExpireToxinTiles(board.CurrentGrowthCycle);
            List<Player> shuffledPlayers = players.OrderBy(_ => Rng.NextDouble()).ToList();

            var (allMutations, _) = MutationRepository.BuildFullMutationSet();
            Mutation sporocidalBloom = allMutations[MutationIds.SporocidalBloom];

            ApplyPerTurnSporeEffects(shuffledPlayers, board, sporocidalBloom, failedGrowthsByPlayerId, sporeDropObserver);
            ApplyNecrophyticBloomTrigger(shuffledPlayers, board, sporeDropObserver);
            MutationEffectProcessor.ApplyToxinAuraDeaths(board, players, Rng, sporeDropObserver);
            EvaluateProbabilisticDeaths(board, shuffledPlayers, sporeDropObserver, growthAndDecayObserver);
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

        /// <summary>
        /// Handles the 20% board occupancy trigger for Necrophytic Bloom. 
        /// For each player with the mutation, fires an initial burst of spores for *each* dead cell they have.
        /// </summary>
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
                        MutationEffectProcessor.TriggerNecrophyticBloomInitialBurst(
                            p, board, Rng, observer);
                    }
                }
            }
        }

        /// <summary>
        /// Handles all living cells, rolling for probabilistic deaths. After Necrophytic Bloom activates,
        /// each cell death for a player with the mutation triggers per-death spores with damping.
        /// </summary>
        private static void EvaluateProbabilisticDeaths(
            GameBoard board,
            List<Player> players,
            ISporeDropObserver? sporeDropObserver = null,
            IGrowthAndDecayObserver? growthAndDecayObserver = null)
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

                    // Try Necrotoxic Conversion for toxin-based kills
                    MutationEffectProcessor.TryNecrotoxicConversion(
                        cell, board, players, Rng, growthAndDecayObserver);

                    if (reason.Value == DeathReason.PutrefactiveMycotoxin && growthAndDecayObserver != null)
                    {
                        AttributePutrefactiveMycotoxinKill(cell, board, players, growthAndDecayObserver);
                    }

                    MutationEffectProcessor.TryTriggerSporeOnDeath(owner, board, Rng, sporeDropObserver);

                    // --- PER-DEATH Necrophytic Bloom effect ---
                    if (necrophyticActivated &&
                        owner.GetMutationLevel(MutationIds.NecrophyticBloom) > 0)
                    {
                        float occupiedPercent = board.GetOccupiedTileRatio();
                        MutationEffectProcessor.TriggerNecrophyticBloomOnCellDeath(
                            owner, board, Rng, occupiedPercent, sporeDropObserver);
                    }
                }
                else
                {
                    MutationEffectProcessor.AdvanceOrResetCellAge(owner, cell);
                }
            }
        }

        private static void AttributePutrefactiveMycotoxinKill(
             FungalCell deadCell,
             GameBoard board,
             List<Player> players,
             IGrowthAndDecayObserver? observer = null)
        {
            if (observer == null) return;  // Safely do nothing if not tracking

            foreach (var neighbor in board.GetAdjacentTiles(deadCell.TileId))
            {
                var neighborCell = neighbor.FungalCell;
                if (neighborCell == null || !neighborCell.IsAlive) continue;
                int neighborOwnerId = neighborCell.OwnerPlayerId ?? -1;
                if (neighborOwnerId == deadCell.OwnerPlayerId) continue;

                var enemyPlayer = players.FirstOrDefault(p => p.PlayerId == neighborOwnerId);
                if (enemyPlayer == null) continue;

                if (enemyPlayer.GetMutationEffect(MutationType.AdjacentFungicide) > 0f)
                {
                    observer.RecordPutrefactiveMycotoxinKill(neighborOwnerId, 1);
                }
            }
        }



    }
}
