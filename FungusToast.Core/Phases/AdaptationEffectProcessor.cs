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

            var targetTile = board.GetOrthogonalNeighbors(eventArgs.TileId)
                .FirstOrDefault(tile =>
                    tile.FungalCell != null
                    && tile.FungalCell.IsAlive
                    && tile.FungalCell.OwnerPlayerId != owner.PlayerId);
            if (targetTile?.FungalCell == null)
            {
                return;
            }

            board.KillFungalCell(targetTile.FungalCell, DeathReason.MycotoxicLash, owner.PlayerId, eventArgs.TileId);
            observer.RecordAttributedKill(owner.PlayerId, DeathReason.MycotoxicLash, 1);
            board.OnSpecialBoardEventTriggered(
                new SpecialBoardEventArgs(
                    SpecialBoardEventKind.MycotoxicLashTriggered,
                    owner.PlayerId,
                    eventArgs.TileId,
                    targetTile.TileId,
                    new[] { targetTile.TileId }));
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