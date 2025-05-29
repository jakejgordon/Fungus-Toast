using FungusToast.Core.Config;
using FungusToast.Core.Mutations;
using FungusToast.Core.Phases;
using FungusToast.Core.Players;
using FungusToast.Core.Metrics;

namespace FungusToast.Core.Death
{
    public static class DeathEngine
    {
        private static readonly Random rng = new();

        public static void ExecuteDeathCycle(
            GameBoard board,
            List<Player> players,
            ISporeDropObserver? observer = null)
        {
            var (allMutations, _) = MutationRepository.BuildFullMutationSet();
            var sporocidalBloom = allMutations[MutationIds.SporocidalBloom];

            foreach (var player in players)
            {
                MutationEffectProcessor.TryPlaceSporocidalSpores(player, board, rng, sporocidalBloom, observer);
            }

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

                if (player.ControlledTileIds.Count <= 1)
                    continue; // Don't kill last living cell

                double roll = rng.NextDouble();
                var (deathChance, reason) = MutationEffectProcessor.CalculateDeathChance(
                    player, cell, board, players, roll);

                if (reason.HasValue && roll < deathChance)
                {
                    cell.Kill(reason.Value);
                    player.ControlledTileIds.Remove(cell.TileId);
                    MutationEffectProcessor.TryTriggerSporeOnDeath(player, board, rng, observer);
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
