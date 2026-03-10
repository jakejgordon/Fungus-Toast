using FungusToast.Core.Board;
using FungusToast.Core.Campaign;
using FungusToast.Core.Config;
using FungusToast.Core.Death;
using FungusToast.Core.Events;
using FungusToast.Core.Metrics;
using FungusToast.Core.Players;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FungusToast.Core.Phases
{
    public static class AdaptationEffectProcessor
    {
        public static void OnToxinPlaced(
            ToxinPlacedEventArgs eventArgs,
            GameBoard board,
            List<Player> players,
            Random rng,
            ISimulationObserver observer)
        {
            if (eventArgs == null || eventArgs.Neutralized || eventArgs.PlacingPlayerId < 0)
            {
                return;
            }

            var owner = players.FirstOrDefault(player => player.PlayerId == eventArgs.PlacingPlayerId);
            if (owner == null || !owner.HasAdaptation(AdaptationIds.MycotoxicLash))
            {
                return;
            }

            float killChance = Math.Clamp(AdaptationGameBalance.MycotoxicLashToxinDropKillChance, 0f, 1f);
            if (killChance <= 0f || rng.NextDouble() >= killChance)
            {
                return;
            }

            var targetTile = board.GetAdjacentLivingTiles(eventArgs.TileId, excludePlayerId: owner.PlayerId).FirstOrDefault();
            if (targetTile?.FungalCell == null)
            {
                return;
            }

            board.KillFungalCell(targetTile.FungalCell, DeathReason.Poisoned, owner.PlayerId, eventArgs.TileId);
            observer.RecordAttributedKill(owner.PlayerId, DeathReason.Poisoned, 1);
        }

        public static void OnPostDecayPhase(
            GameBoard board,
            List<Player> players,
            Random rng,
            ISimulationObserver observer)
        {
            if (board.CurrentRound != AdaptationGameBalance.ConidialRelayTriggerRound)
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

            int sourceTileId = player.StartingTileId.Value;

            var candidates = board.AllTiles()
                .Where(tile => !tile.IsOccupied)
                .OrderBy(_ => rng.NextDouble())
                .ToList();

            foreach (var candidate in candidates)
            {
                if (board.TryRelocateStartingSpore(player, candidate.TileId))
                {
                    board.OnSpecialBoardEventTriggered(
                        new SpecialBoardEventArgs(
                            SpecialBoardEventKind.ConidialRelayTriggered,
                            player.PlayerId,
                            sourceTileId,
                            candidate.TileId));
                    return true;
                }
            }

            return false;
        }
    }
}