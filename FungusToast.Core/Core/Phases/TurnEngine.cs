using System;
using System.Collections.Generic;
using System.Linq;
using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Death;
using FungusToast.Core.Growth;
using FungusToast.Core.Mutations;
using FungusToast.Core.Players;

namespace FungusToast.Core.Phases
{
    public static class TurnEngine
    {
        /// <summary>
        /// Assigns base, bonus, and mutation-derived points and triggers auto-upgrades and strategy spending.
        /// </summary>
        public static void AssignMutationPoints(List<Player> players, List<Mutation> allMutations, Random rng)
        {
            foreach (var p in players)
            {
                p.AssignMutationPoints(players, rng, allMutations);
                p.MutationStrategy?.SpendMutationPoints(p, allMutations);
            }
        }

        /// <summary>
        /// Executes a full multi-cycle growth phase, including mutation-based effects.
        /// </summary>
        public static void RunGrowthPhase(GameBoard board, List<Player> players)
        {
            var processor = new GrowthPhaseProcessor(board, players);
            var rng = new Random();

            for (int cycle = 0; cycle < GameBalance.TotalGrowthCycles; cycle++)
            {
                processor.ExecuteSingleCycle();
                ApplyRegenerativeHyphaeReclaims(board, players, rng);
            }
        }

        /// <summary>
        /// Executes the decay phase for all living fungal cells.
        /// </summary>
        public static void RunDecayPhase(GameBoard board, List<Player> players)
        {
            DeathEngine.ExecuteDeathCycle(board, players);
        }

        /// <summary>
        /// Applies Regenerative Hyphae effects during growth: reclaim adjacent dead cells previously owned.
        /// </summary>
        private static void ApplyRegenerativeHyphaeReclaims(GameBoard board, List<Player> players, Random rng)
        {
            foreach (var player in players)
            {
                int level = player.GetMutationLevel(MutationIds.RegenerativeHyphae);
                if (level <= 0)
                    continue;

                float reclaimChance = GameBalance.RegenerativeHyphaeReclaimChance * level;
                var playerCells = board.GetAllCellsOwnedBy(player.PlayerId);

                foreach (var cell in playerCells)
                {
                    var (x, y) = board.GetXYFromTileId(cell.TileId);
                    var neighbors = board.GetOrthogonalNeighbors(x, y);

                    foreach (var neighbor in neighbors)
                    {
                        var deadCell = neighbor.FungalCell;
                        if (deadCell == null || deadCell.IsAlive)
                            continue;

                        if (deadCell.OriginalOwnerPlayerId != player.PlayerId)
                            continue;

                        if (rng.NextDouble() < reclaimChance)
                        {
                            deadCell.Reclaim(player.PlayerId);
                            board.RegisterCell(deadCell);
                        }
                    }
                }
            }
        }
    }
}
