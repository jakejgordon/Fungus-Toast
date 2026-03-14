using FungusToast.Core.Board;
using FungusToast.Core.Campaign;
using FungusToast.Core.Config;
using FungusToast.Core.Death;
using FungusToast.Core.Events;
using FungusToast.Core.Growth;
using FungusToast.Core.Metrics;
using FungusToast.Core.Mutations;
using FungusToast.Core.Players;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FungusToast.Core.Phases
{
    public static class AdaptationEffectProcessor
    {
        private const string AegisHyphaeCounterKey = "adaptation_aegis_hyphae_growths";

        public static void OnMutationPhaseStart(
            GameBoard board,
            List<Player> players,
            Random rng,
            ISimulationObserver observer)
        {
            if (board.CurrentRound != AdaptationGameBalance.RetrogradeBloomTriggerRound)
            {
                return;
            }

            foreach (var player in players)
            {
                var adaptation = player.GetAdaptation(AdaptationIds.RetrogradeBloom);
                if (adaptation == null || adaptation.HasTriggered)
                {
                    continue;
                }

                adaptation.MarkTriggered();
                TryApplyRetrogradeBloom(player, board, rng, observer);
            }
        }

        public static void OnCellColonized(
            int playerId,
            int tileId,
            GrowthSource source,
            GameBoard board,
            List<Player> players,
            ISimulationObserver observer)
        {
            var player = players.FirstOrDefault(candidate => candidate.PlayerId == playerId);
            if (player == null)
            {
                return;
            }

            var tile = board.GetTileById(tileId);
            var cell = tile?.FungalCell;
            if (cell == null || !cell.IsAlive || cell.OwnerPlayerId != playerId)
            {
                return;
            }

            TryApplyMarginalClamp(player, tileId, board, observer);
            TryApplyAegisHyphae(player, tileId, board);
        }

        private static void TryApplyAegisHyphae(
            Player player,
            int tileId,
            GameBoard board)
        {
            if (!player.HasAdaptation(AdaptationIds.AegisHyphae))
            {
                return;
            }

            int fortifiedThisRound = board.CurrentRoundContext.GetEffectCount(player.PlayerId, AegisHyphaeCounterKey);
            if (fortifiedThisRound >= AdaptationGameBalance.AegisHyphaeCellsPerRound)
            {
                return;
            }

            var tile = board.GetTileById(tileId);
            var cell = tile?.FungalCell;
            if (cell == null || !cell.IsAlive || cell.OwnerPlayerId != player.PlayerId || cell.IsResistant)
            {
                return;
            }

            cell.MakeResistant();
            board.CurrentRoundContext.IncrementEffectCount(player.PlayerId, AegisHyphaeCounterKey);
            board.OnResistanceAppliedBatch(player.PlayerId, GrowthSource.AegisHyphae, new List<int> { tileId });
        }

        private static void TryApplyMarginalClamp(
            Player player,
            int tileId,
            GameBoard board,
            ISimulationObserver observer)
        {
            if (!player.HasAdaptation(AdaptationIds.MarginalClamp))
            {
                return;
            }

            var borderThreatTiles = board.GetOrthogonalNeighbors(tileId)
                .Where(tile => BoardUtilities.IsOnBorder(tile, board.Width, board.Height))
                .Where(tile => tile.FungalCell != null)
                .ToList();

            var enemyLivingTargetTiles = borderThreatTiles
                .Where(tile => tile.FungalCell!.IsAlive)
                .Where(tile => tile.FungalCell!.OwnerPlayerId != player.PlayerId)
                .Where(tile => !tile.FungalCell!.IsResistant)
                .ToList();

            var toxinTargetTiles = borderThreatTiles
                .Where(tile => tile.FungalCell!.IsToxin)
                .ToList();

            if (enemyLivingTargetTiles.Count == 0 && toxinTargetTiles.Count == 0)
            {
                return;
            }

            foreach (var targetTile in enemyLivingTargetTiles)
            {
                board.KillFungalCell(targetTile.FungalCell!, DeathReason.MarginalClamp, player.PlayerId, tileId);
            }

            foreach (var toxinTile in toxinTargetTiles)
            {
                board.RemoveCellInternal(toxinTile.TileId, removeControl: true);
            }

            if (enemyLivingTargetTiles.Count > 0)
            {
                observer.RecordAttributedKill(player.PlayerId, DeathReason.MarginalClamp, enemyLivingTargetTiles.Count);
            }

            var affectedTileIds = enemyLivingTargetTiles
                .Select(tile => tile.TileId)
                .Concat(toxinTargetTiles.Select(tile => tile.TileId))
                .Distinct()
                .ToList();

            board.OnSpecialBoardEventTriggered(
                new SpecialBoardEventArgs(
                    SpecialBoardEventKind.MarginalClampTriggered,
                    player.PlayerId,
                    tileId,
                    affectedTileIds[0],
                    affectedTileIds));
        }

        public static bool TryConsumeSaprophageRingDeath(
            GameBoard board,
            Player owner,
            FungalCell cell,
            DeathCalculationResult deathResult)
        {
            if (owner == null || cell == null || !owner.HasAdaptation(AdaptationIds.SaprophageRing))
            {
                return false;
            }

            var anchorTile = board.GetOrthogonalNeighbors(cell.TileId)
                .FirstOrDefault(tile =>
                    tile.FungalCell != null
                    && tile.FungalCell.IsAlive
                    && tile.FungalCell.IsResistant
                    && tile.FungalCell.OwnerPlayerId == owner.PlayerId);
            if (anchorTile == null)
            {
                return false;
            }

            board.ConsumeFungalCell(cell, deathResult.Reason!.Value, deathResult.KillerPlayerId, deathResult.AttackerTileId);
            board.OnSpecialBoardEventTriggered(
                new SpecialBoardEventArgs(
                    SpecialBoardEventKind.SaprophageRingTriggered,
                    owner.PlayerId,
                    anchorTile.TileId,
                    cell.TileId,
                    new[] { cell.TileId }));
            return true;
        }

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

        private static bool TryApplyRetrogradeBloom(
            Player player,
            GameBoard board,
            Random rng,
            ISimulationObserver observer)
        {
            var tier1LevelPool = player.PlayerMutations.Values
                .Where(pm => pm.Mutation.Tier == MutationTier.Tier1 && pm.CurrentLevel > 0)
                .SelectMany(pm => Enumerable.Repeat(pm.MutationId, pm.CurrentLevel))
                .ToList();
            if (tier1LevelPool.Count < AdaptationGameBalance.RetrogradeBloomTier1LevelsLost)
            {
                return false;
            }

            var eligibleTier5Mutations = MutationRegistry.All.Values
                .Where(mutation => mutation.Tier == MutationTier.Tier5)
                .Where(mutation => player.GetMutationLevel(mutation.Id) < mutation.MaxLevel)
                .ToList();
            if (eligibleTier5Mutations.Count == 0)
            {
                return false;
            }

            var levelsLostByMutationId = new Dictionary<int, int>();
            for (int index = 0; index < AdaptationGameBalance.RetrogradeBloomTier1LevelsLost; index++)
            {
                int poolIndex = rng.Next(tier1LevelPool.Count);
                int mutationId = tier1LevelPool[poolIndex];
                tier1LevelPool.RemoveAt(poolIndex);

                if (!levelsLostByMutationId.ContainsKey(mutationId))
                {
                    levelsLostByMutationId[mutationId] = 0;
                }

                levelsLostByMutationId[mutationId]++;
            }

            foreach (var kvp in levelsLostByMutationId)
            {
                int currentLevel = player.GetMutationLevel(kvp.Key);
                player.SetMutationLevel(kvp.Key, currentLevel - kvp.Value, board.CurrentRound, observer);
            }

            string devolvedMutationSummary = BuildRetrogradeBloomDevolvedMutationSummary(levelsLostByMutationId);
            int devolvedPoints = CalculateRetrogradeBloomDevolvedPoints(levelsLostByMutationId);

            var targetMutation = eligibleTier5Mutations[rng.Next(eligibleTier5Mutations.Count)];
            int oldLevel = player.GetMutationLevel(targetMutation.Id);
            int newLevel = oldLevel + AdaptationGameBalance.RetrogradeBloomTier5LevelsGained;
            player.SetMutationLevel(targetMutation.Id, newLevel, board.CurrentRound, observer);
            observer.RecordMutationUpgradeEvent(
                playerId: player.PlayerId,
                mutationId: targetMutation.Id,
                mutationName: targetMutation.Name,
                mutationTier: targetMutation.Tier,
                oldLevel: oldLevel,
                newLevel: player.GetMutationLevel(targetMutation.Id),
                round: board.CurrentRound,
                mutationPointsBefore: player.MutationPoints,
                mutationPointsAfter: player.MutationPoints,
                pointsSpent: 0,
                upgradeSource: "adaptation");
            observer.RecordRetrogradeBloomUpgrade(player.PlayerId, targetMutation.Name, devolvedMutationSummary, devolvedPoints);

            int anchorTileId = player.StartingTileId ?? -1;
            board.OnSpecialBoardEventTriggered(
                new SpecialBoardEventArgs(
                    SpecialBoardEventKind.RetrogradeBloomTriggered,
                    player.PlayerId,
                    anchorTileId,
                    anchorTileId,
                    anchorTileId >= 0 ? new[] { anchorTileId } : Array.Empty<int>()));
            return true;
        }

        private static int CalculateRetrogradeBloomDevolvedPoints(Dictionary<int, int> levelsLostByMutationId)
        {
            int totalPoints = 0;

            foreach (var kvp in levelsLostByMutationId)
            {
                var mutation = MutationRegistry.GetById(kvp.Key);
                if (mutation == null)
                {
                    continue;
                }

                totalPoints += mutation.PointsPerUpgrade * kvp.Value;
            }

            return totalPoints;
        }

        private static string BuildRetrogradeBloomDevolvedMutationSummary(Dictionary<int, int> levelsLostByMutationId)
        {
            return string.Join(", ",
                levelsLostByMutationId
                    .Select(kvp => new
                    {
                        MutationName = MutationRegistry.GetById(kvp.Key)?.Name ?? $"Mutation {kvp.Key}",
                        LevelsLost = kvp.Value
                    })
                    .OrderByDescending(entry => entry.LevelsLost)
                    .ThenBy(entry => entry.MutationName)
                    .Select(entry => entry.LevelsLost == 1
                        ? entry.MutationName
                        : $"{entry.MutationName} x{entry.LevelsLost}"));
        }
    }
}