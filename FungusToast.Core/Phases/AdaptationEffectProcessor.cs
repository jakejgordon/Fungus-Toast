using FungusToast.Core.Board;
using FungusToast.Core.Campaign;
using FungusToast.Core.Metrics;
using FungusToast.Core.Players;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FungusToast.Core.Phases
{
    public static class AdaptationEffectProcessor
    {
        public const float MycotoxicHaloKillAuraBonus = 0.02f;

        public static void OnPostDecayPhase(
            GameBoard board,
            List<Player> players,
            Random rng,
            ISimulationObserver observer)
        {
            if (board.CurrentRound != 10)
            {
                return;
            }

            foreach (var player in players)
            {
                var adaptation = player.GetAdaptation(AdaptationIds.ConidialRelay);
                if (adaptation == null || adaptation.HasTriggered)
                {
                    continue;
                }

                if (TryApplyConidialRelay(player, board, rng))
                {
                    adaptation.MarkTriggered();
                }
            }
        }

        private static bool TryApplyConidialRelay(Player player, GameBoard board, Random rng)
        {
            if (!player.StartingTileId.HasValue)
            {
                return false;
            }

            var candidates = board.AllTiles()
                .Where(tile => !tile.IsOccupied)
                .OrderBy(_ => rng.NextDouble())
                .ToList();

            foreach (var candidate in candidates)
            {
                if (board.TryRelocateStartingSpore(player, candidate.TileId))
                {
                    return true;
                }
            }

            return false;
        }
    }
}