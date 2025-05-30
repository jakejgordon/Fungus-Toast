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
            ISporeDropObserver? observer = null)
        {
            // -----------------------------------------------------------------
            // 1.  Per-turn spore effects (handled in processor)
            // -----------------------------------------------------------------
            var (allMutations, _) = MutationRepository.BuildFullMutationSet();
            Mutation sporocidalBloom = allMutations[MutationIds.SporocidalBloom];

            foreach (var p in players)
            {
                MutationEffectProcessor.TryPlaceSporocidalSpores(
                    p, board, Rng, sporocidalBloom, observer);
            }

            // -----------------------------------------------------------------
            // 2.  Necrophytic Bloom trigger (20 % board occupancy)
            // -----------------------------------------------------------------
            float occupiedPercent =
                (float)board.GetAllCells().Count /
                (GameBalance.BoardWidth * GameBalance.BoardHeight);

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

            // -----------------------------------------------------------------
            // 3.  Evaluate death for every *living* cell
            // -----------------------------------------------------------------
            List<BoardTile> livingTiles = board.AllTiles()
                .Where(t => t.FungalCell is { IsAlive: true })
                .ToList();

            foreach (BoardTile tile in livingTiles)
            {
                FungalCell cell = tile.FungalCell!;
                Player owner = players.First(p => p.PlayerId == cell.OwnerPlayerId);

                // Preserve the colony’s last cell.
                if (owner.ControlledTileIds.Count <= 1) continue;

                double roll = Rng.NextDouble();

                (float _, DeathReason? reason) =
                    MutationEffectProcessor.CalculateDeathChance(
                        owner, cell, board, players, roll);

                if (reason.HasValue)
                {
                    cell.Kill(reason.Value);
                    owner.RemoveControlledTile(cell.TileId);

                    // On-death spore drops & Necrophytic reactive spores
                    MutationEffectProcessor.TryTriggerSporeOnDeath(owner, board, Rng, observer);

                    if (necrophyticActivated &&
                        owner.GetMutationLevel(MutationIds.NecrophyticBloom) > 0)
                    {
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
        /// True if every orthogonal neighbour of <paramref name="tileId"/> is alive.
        /// Used by Putrefactive Mycotoxin & Encysted Spore logic.
        /// </summary>
        public static bool IsCellSurrounded(int tileId, GameBoard board)
        {
            FungalCell? cell = board.GetCell(tileId);
            if (cell == null) return false;

            foreach (int nId in board.GetAdjacentTileIds(tileId))
            {
                FungalCell? n = board.GetCell(nId);
                if (n == null || !n.IsAlive) return false;
            }

            return true;
        }
    }
}
