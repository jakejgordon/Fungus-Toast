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

        public static void OnStartingSporesEstablished(
            GameBoard board,
            List<Player> players)
        {
            foreach (var player in players)
            {
                var adaptation = player.GetAdaptation(AdaptationIds.SporeSalvo);
                if (adaptation == null || adaptation.HasTriggered)
                {
                    continue;
                }

                TryApplySporeSalvo(player, board, players);
                adaptation.MarkTriggered();
            }
        }

        public static void OnMutationPhaseStart(
            GameBoard board,
            List<Player> players,
            Random rng,
            ISimulationObserver observer)
        {
            if (board.CurrentRound == AdaptationGameBalance.RetrogradeBloomTriggerRound)
            {
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

            if (board.CurrentRound == AdaptationGameBalance.DistalSporeTriggerRound)
            {
                foreach (var player in players)
                {
                    var adaptation = player.GetAdaptation(AdaptationIds.DistalSpore);
                    if (adaptation == null || adaptation.HasTriggered)
                    {
                        continue;
                    }

                    if (TryApplyDistalSpore(player, board))
                    {
                        adaptation.MarkTriggered();
                    }
                }
            }

            if (board.CurrentRound == AdaptationGameBalance.MycelialCrescendoFirstTriggerRound
                || board.CurrentRound == AdaptationGameBalance.MycelialCrescendoSecondTriggerRound)
            {
                foreach (var player in players)
                {
                    if (!player.HasAdaptation(AdaptationIds.MycelialCrescendo))
                    {
                        continue;
                    }

                    TryApplyMycelialCrescendo(player, board, rng, observer);
                }
            }

            if (board.CurrentRound == AdaptationGameBalance.ConidiaAscentTriggerRound)
            {
                foreach (var player in players)
                {
                    var adaptation = player.GetAdaptation(AdaptationIds.ConidiaAscent);
                    if (adaptation == null || adaptation.HasTriggered)
                    {
                        continue;
                    }

                    if (TryApplyConidiaAscent(player, board, rng))
                    {
                        adaptation.MarkTriggered();
                    }
                }
            }
        }

        public static void OnPostGrowthPhaseCompleted(
            GameBoard board,
            List<Player> players,
            Random rng,
            ISimulationObserver observer)
        {
            if (board.CurrentRound != AdaptationGameBalance.HyphalBridgeTriggerRound)
            {
                return;
            }

            foreach (var player in players)
            {
                var adaptation = player.GetAdaptation(AdaptationIds.HyphalBridge);
                if (adaptation == null || adaptation.HasTriggered)
                {
                    continue;
                }

                if (TryApplyHyphalBridge(player, board, players))
                {
                    adaptation.MarkTriggered();
                }
            }
        }

        public static void OnLivingCellEstablished(
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
            TryApplyCrustalCallus(player, tileId, board);
            TryApplyAegisHyphae(player, tileId, board);
        }

        public static void OnCellColonized(
            int playerId,
            int tileId,
            GrowthSource source,
            GameBoard board,
            List<Player> players,
            ISimulationObserver observer)
        {
            OnLivingCellEstablished(playerId, tileId, source, board, players, observer);
        }

        private static void TryApplyCrustalCallus(
            Player player,
            int tileId,
            GameBoard board)
        {
            if (!player.HasAdaptation(AdaptationIds.CrustalCallus))
            {
                return;
            }

            var tile = board.GetTileById(tileId);
            var cell = tile?.FungalCell;
            if (tile == null
                || cell == null
                || !cell.IsAlive
                || cell.OwnerPlayerId != player.PlayerId
                || cell.IsResistant
                || !BoardUtilities.IsWithinEdgeDistance(tile, board.Width, board.Height, AdaptationGameBalance.CrustalCallusEdgeDistance))
            {
                return;
            }

            cell.MakeResistant(GrowthSource.CrustalCallus);
            board.OnResistanceAppliedBatch(player.PlayerId, GrowthSource.CrustalCallus, new List<int> { tileId });
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

            cell.MakeResistant(GrowthSource.AegisHyphae);
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

        public static void OnToxinExpired(
            ToxinExpiredEventArgs eventArgs,
            GameBoard board,
            List<Player> players,
            Random rng,
            ISimulationObserver observer)
        {
            if (eventArgs?.ToxinOwnerPlayerId == null)
            {
                return;
            }

            var owner = players.FirstOrDefault(player => player.PlayerId == eventArgs.ToxinOwnerPlayerId.Value);
            if (owner == null || !owner.HasAdaptation(AdaptationIds.VesicleBurst))
            {
                return;
            }

            float popChance = Math.Clamp(AdaptationGameBalance.VesicleBurstExpiredToxinPopChance, 0f, 1f);
            if (popChance <= 0f || rng.NextDouble() >= popChance)
            {
                return;
            }

            TryApplyVesicleBurst(owner, eventArgs.TileId, board, observer);
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

        private static void TryApplyMycelialCrescendo(
            Player player,
            GameBoard board,
            Random rng,
            ISimulationObserver observer)
        {
            var eligibleSurges = MutationRegistry.All.Values
                .Where(mutation =>
                    mutation.IsSurge
                    && mutation.Category == MutationCategory.MycelialSurges
                    && !player.IsSurgeActive(mutation.Id)
                    && player.GetMutationLevel(mutation.Id) > 0
                    && player.GetMutationLevel(mutation.Id) < mutation.MaxLevel)
                .ToList();

            if (eligibleSurges.Count == 0)
            {
                return;
            }

            var chosenSurge = eligibleSurges[rng.Next(eligibleSurges.Count)];
            int cost = player.GetMutationPointCost(chosenSurge);
            player.MutationPoints += cost;

            if (!player.TryUpgradeMutation(chosenSurge, observer, board.CurrentRound))
            {
                player.MutationPoints -= cost;
                return;
            }

            observer.RecordMycelialCrescendoSurge(player.PlayerId, chosenSurge.Name);

            int sourceTileId = player.StartingTileId ?? -1;
            board.OnSpecialBoardEventTriggered(
                new SpecialBoardEventArgs(
                    SpecialBoardEventKind.MycelialCrescendoTriggered,
                    player.PlayerId,
                    sourceTileId,
                    sourceTileId,
                    surgeName: chosenSurge.Name));
        }

        private static bool TryApplyConidialRelay(Player player, GameBoard board, Random rng)
        {
            if (!player.StartingTileId.HasValue)
            {
                return false;
            }

            int sourceTileId = player.StartingTileId.Value;

            var candidates = board.AllTiles()
                .Where(tile => !tile.IsOccupiedForSporePlacement)
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

        private static bool TryApplyConidiaAscent(Player player, GameBoard board, Random rng)
        {
            if (board == null)
            {
                return false;
            }

            var sourceAnchor = FindFirstConidiaAscentSourceAnchor(board, player.PlayerId);
            if (sourceAnchor == null)
            {
                return false;
            }

            var destinationAnchors = GetConidiaAscentDestinationAnchors(board);
            if (destinationAnchors.Count == 0)
            {
                return false;
            }

            var destinationAnchor = destinationAnchors[rng.Next(destinationAnchors.Count)];
            var sourceTileIds = GetBlockTileIds(board, sourceAnchor.Value.x, sourceAnchor.Value.y, AdaptationGameBalance.ConidiaAscentSourceBlockSize);
            var destinationTileIds = GetBlockTileIds(board, destinationAnchor.x, destinationAnchor.y, AdaptationGameBalance.ConidiaAscentDestinationBlockSize);
            int sourceLaunchTileId = GetTileId(
                sourceAnchor.Value.x + AdaptationGameBalance.ConidiaAscentLaunchSubsquareOffset,
                sourceAnchor.Value.y + AdaptationGameBalance.ConidiaAscentLaunchSubsquareOffset,
                board.Width);
            int destinationTileId = GetTileId(destinationAnchor.x, destinationAnchor.y, board.Width);

            foreach (int tileId in sourceTileIds)
            {
                var cell = board.GetCell(tileId);
                if (cell == null || !cell.IsAlive || cell.OwnerPlayerId != player.PlayerId || cell.IsResistant)
                {
                    return false;
                }
            }

            foreach (int tileId in sourceTileIds)
            {
                var cell = board.GetCell(tileId);
                if (cell != null)
                {
                    board.KillFungalCell(cell, DeathReason.ConidiaAscent, player.PlayerId, sourceLaunchTileId);
                }
            }

            var placedTileIds = new List<int>(destinationTileIds.Count);
            foreach (int tileId in destinationTileIds)
            {
                if (!board.SpawnSporeForPlayer(player, tileId, GrowthSource.ConidiaAscent))
                {
                    foreach (int placedTileId in placedTileIds)
                    {
                        board.RemoveCellInternal(placedTileId, removeControl: true);
                    }

                    return false;
                }

                var placedCell = board.GetCell(tileId);
                if (placedCell?.IsAlive != true || placedCell.OwnerPlayerId != player.PlayerId)
                {
                    foreach (int placedTileId in placedTileIds)
                    {
                        board.RemoveCellInternal(placedTileId, removeControl: true);
                    }

                    board.RemoveCellInternal(tileId, removeControl: true);
                    return false;
                }

                placedCell.ClearNewlyGrownFlag();
                placedTileIds.Add(tileId);
            }

            board.OnSpecialBoardEventTriggered(
                new SpecialBoardEventArgs(
                    SpecialBoardEventKind.ConidiaAscentTriggered,
                    player.PlayerId,
                    sourceLaunchTileId,
                    destinationTileId,
                    sourceTileIds));
            return true;
        }

        private static bool TryApplyHyphalBridge(
            Player player,
            GameBoard board,
            IReadOnlyList<Player> players)
        {
            if (!player.StartingTileId.HasValue)
            {
                return false;
            }

            int sourceTileId = player.StartingTileId.Value;
            var sourcePosition = board.GetXYFromTileId(sourceTileId);
            var playerSummaries = BoardUtilities.GetPlayerBoardSummaries(players.ToList(), board);

            var nearestEnemy = players
                .Where(candidate => candidate.PlayerId != player.PlayerId && candidate.StartingTileId.HasValue)
                .OrderBy(candidate => SquaredDistance(board.GetXYFromTileId(candidate.StartingTileId!.Value), sourcePosition))
                .ThenByDescending(candidate => playerSummaries.TryGetValue(candidate.PlayerId, out var summary) ? summary.LivingCells : 0)
                .ThenBy(candidate => candidate.PlayerId)
                .FirstOrDefault();
            if (nearestEnemy?.StartingTileId == null)
            {
                return false;
            }

            var bridgeTileIds = GetHyphalBridgeTileIds(
                board,
                sourceTileId,
                nearestEnemy.StartingTileId.Value,
                AdaptationGameBalance.HyphalBridgeCellCount);
            if (bridgeTileIds.Count == 0)
            {
                return false;
            }

            var resolvedTileIds = new List<int>(bridgeTileIds.Count);
            foreach (int tileId in bridgeTileIds)
            {
                var tile = board.GetTileById(tileId);
                if (tile == null || tile.FungalCell?.IsResistant == true)
                {
                    continue;
                }

                var replacementCell = new FungalCell(
                    ownerPlayerId: player.PlayerId,
                    tileId: tileId,
                    source: GrowthSource.HyphalBridge,
                    lastOwnerPlayerId: tile.FungalCell?.OwnerPlayerId);
                board.PlaceFungalCell(replacementCell);

                var placedCell = board.GetCell(tileId);
                if (placedCell?.IsAlive == true && placedCell.OwnerPlayerId == player.PlayerId)
                {
                    placedCell.ClearNewlyGrownFlag();
                    resolvedTileIds.Add(tileId);
                }
            }

            if (resolvedTileIds.Count == 0)
            {
                return false;
            }

            board.OnSpecialBoardEventTriggered(
                new SpecialBoardEventArgs(
                    SpecialBoardEventKind.HyphalBridgeTriggered,
                    player.PlayerId,
                    sourceTileId,
                    resolvedTileIds[resolvedTileIds.Count - 1],
                    resolvedTileIds));
            return true;
        }

        private static void TryApplySporeSalvo(
            Player player,
            GameBoard board,
            IReadOnlyList<Player> players)
        {
            if (!player.StartingTileId.HasValue)
            {
                return;
            }

            int sourceTileId = player.StartingTileId.Value;
            var sourcePosition = board.GetXYFromTileId(sourceTileId);

            foreach (var enemy in players.Where(candidate => candidate.PlayerId != player.PlayerId && candidate.StartingTileId.HasValue))
            {
                int enemyStartingTileId = enemy.StartingTileId!.Value;
                var targetTile = board.GetOrthogonalNeighbors(enemyStartingTileId)
                    .Where(tile => !tile.IsOccupiedForSporePlacement)
                    .OrderBy(tile => SquaredDistance((tile.X, tile.Y), sourcePosition))
                    .ThenBy(tile => tile.TileId)
                    .FirstOrDefault();
                if (targetTile == null)
                {
                    continue;
                }

                ToxinHelper.ConvertToToxin(board, targetTile.TileId, GrowthSource.SporeSalvo, player);
                board.OnSpecialBoardEventTriggered(
                    new SpecialBoardEventArgs(
                        SpecialBoardEventKind.SporeSalvoTriggered,
                        player.PlayerId,
                        sourceTileId,
                        targetTile.TileId,
                        new[] { targetTile.TileId }));
            }
        }

        private static bool TryApplyDistalSpore(Player player, GameBoard board)
        {
            if (!player.StartingTileId.HasValue)
            {
                return false;
            }

            int sourceTileId = player.StartingTileId.Value;
            var (startX, startY) = board.GetXYFromTileId(sourceTileId);

            int[] cornerTileIds =
            {
                0,
                Math.Max(0, board.Width - 1),
                Math.Max(0, board.Height - 1) * board.Width,
                (Math.Max(0, board.Height - 1) * board.Width) + Math.Max(0, board.Width - 1)
            };

            int cornerTileId = cornerTileIds
                .Distinct()
                .OrderByDescending(tileId => SquaredDistance(board.GetXYFromTileId(tileId), (startX, startY)))
                .ThenBy(tileId => tileId)
                .First();

            var cornerTile = board.GetTileById(cornerTileId);
            if (cornerTile == null)
            {
                return false;
            }

            int targetTileId = cornerTileId;
            if (cornerTile.FungalCell?.IsResistant == true)
            {
                var fallbackTile = board.AllTiles()
                    .Where(tile => tile.FungalCell?.IsResistant != true)
                    .OrderBy(tile => SquaredDistance((tile.X, tile.Y), (cornerTile.X, cornerTile.Y)))
                    .ThenBy(tile => tile.TileId)
                    .FirstOrDefault();
                if (fallbackTile == null)
                {
                    return false;
                }

                targetTileId = fallbackTile.TileId;
            }

            var targetTile = board.GetTileById(targetTileId);
            if (targetTile == null)
            {
                return false;
            }

            var existingCell = targetTile.FungalCell;
            if (existingCell != null)
            {
                if (existingCell.IsResistant)
                {
                    return false;
                }

                if (existingCell.IsAlive)
                {
                    board.ConsumeFungalCell(existingCell, DeathReason.DistalSpore, player.PlayerId, sourceTileId);
                }
                else
                {
                    board.RemoveCellInternal(targetTileId, removeControl: true);
                }
            }

            if (!board.SpawnSporeForPlayer(player, targetTileId, GrowthSource.DistalSpore))
            {
                return false;
            }

            var spawnedCell = board.GetCell(targetTileId);
            if (spawnedCell == null || !spawnedCell.IsAlive || spawnedCell.OwnerPlayerId != player.PlayerId)
            {
                return false;
            }

            if (!spawnedCell.IsResistant)
            {
                spawnedCell.MakeResistant();
                board.OnResistanceAppliedBatch(player.PlayerId, GrowthSource.DistalSpore, new List<int> { targetTileId });
            }

            board.OnSpecialBoardEventTriggered(
                new SpecialBoardEventArgs(
                    SpecialBoardEventKind.DistalSporeTriggered,
                    player.PlayerId,
                    sourceTileId,
                    targetTileId,
                    new[] { targetTileId }));

            return true;
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

        private static void TryApplyVesicleBurst(
            Player owner,
            int sourceTileId,
            GameBoard board,
            ISimulationObserver observer)
        {
            int poisonedCells = 0;
            int toxifiedTiles = 0;
            var affectedTileIds = new List<int>();

            foreach (var targetTile in board.GetOrthogonalNeighbors(sourceTileId))
            {
                if (!IsEligibleVesicleBurstTarget(targetTile, owner.PlayerId))
                {
                    continue;
                }

                var targetCell = targetTile.FungalCell;
                if (targetCell?.IsAlive == true)
                {
                    ToxinHelper.KillAndToxify(
                        board,
                        targetTile.TileId,
                        ToxinHelper.GetToxinExpirationAge(owner),
                        DeathReason.Poisoned,
                        GrowthSource.VesicleBurst,
                        owner,
                        sourceTileId);
                    poisonedCells++;
                    affectedTileIds.Add(targetTile.TileId);
                    continue;
                }

                ToxinHelper.ConvertToToxin(board, targetTile.TileId, GrowthSource.VesicleBurst, owner);
                var placedToxin = board.GetCell(targetTile.TileId);
                if (placedToxin?.IsToxin == true && placedToxin.OwnerPlayerId == owner.PlayerId)
                {
                    toxifiedTiles++;
                    affectedTileIds.Add(targetTile.TileId);
                }
            }

            if (poisonedCells > 0)
            {
                observer.RecordAttributedKill(owner.PlayerId, DeathReason.Poisoned, poisonedCells);
            }

            if (poisonedCells > 0 || toxifiedTiles > 0)
            {
                observer.RecordVesicleBurstEffect(owner.PlayerId, poisonedCells, toxifiedTiles);
                board.OnSpecialBoardEventTriggered(
                    new SpecialBoardEventArgs(
                        SpecialBoardEventKind.VesicleBurstTriggered,
                        owner.PlayerId,
                        sourceTileId,
                        sourceTileId,
                        affectedTileIds.Distinct().ToList()));
            }
        }

        private static bool IsEligibleVesicleBurstTarget(BoardTile tile, int ownerPlayerId)
        {
            var cell = tile.FungalCell;
            if (cell == null)
            {
                return true;
            }

            if (cell.IsResistant)
            {
                return false;
            }

            return cell.OwnerPlayerId != ownerPlayerId;
        }

        private static (int x, int y)? FindFirstConidiaAscentSourceAnchor(GameBoard board, int playerId)
        {
            int blockSize = AdaptationGameBalance.ConidiaAscentSourceBlockSize;
            for (int y = 0; y <= board.Height - blockSize; y++)
            {
                for (int x = 0; x <= board.Width - blockSize; x++)
                {
                    if (IsValidConidiaAscentSourceBlock(board, playerId, x, y, blockSize))
                    {
                        return (x, y);
                    }
                }
            }

            return null;
        }

        private static bool IsValidConidiaAscentSourceBlock(GameBoard board, int playerId, int startX, int startY, int blockSize)
        {
            foreach (int tileId in GetBlockTileIds(board, startX, startY, blockSize))
            {
                var cell = board.GetCell(tileId);
                if (cell == null || !cell.IsAlive || cell.OwnerPlayerId != playerId || cell.IsResistant)
                {
                    return false;
                }
            }

            return true;
        }

        private static List<(int x, int y)> GetConidiaAscentDestinationAnchors(GameBoard board)
        {
            int blockSize = AdaptationGameBalance.ConidiaAscentDestinationBlockSize;
            var anchors = new List<(int x, int y)>();

            for (int y = 0; y <= board.Height - blockSize; y++)
            {
                for (int x = 0; x <= board.Width - blockSize; x++)
                {
                    if (IsConidiaAscentDestinationOpen(board, x, y, blockSize))
                    {
                        anchors.Add((x, y));
                    }
                }
            }

            return anchors;
        }

        private static bool IsConidiaAscentDestinationOpen(GameBoard board, int startX, int startY, int blockSize)
        {
            foreach (int tileId in GetBlockTileIds(board, startX, startY, blockSize))
            {
                var tile = board.GetTileById(tileId);
                if (tile == null || tile.IsOccupiedForSporePlacement)
                {
                    return false;
                }
            }

            return true;
        }

        private static List<int> GetBlockTileIds(GameBoard board, int startX, int startY, int blockSize)
        {
            var tileIds = new List<int>(blockSize * blockSize);

            for (int y = startY; y < startY + blockSize; y++)
            {
                for (int x = startX; x < startX + blockSize; x++)
                {
                    tileIds.Add(GetTileId(x, y, board.Width));
                }
            }

            return tileIds;
        }

        private static int GetTileId(int x, int y, int boardWidth)
        {
            return (y * boardWidth) + x;
        }

        private static List<int> GetHyphalBridgeTileIds(
            GameBoard board,
            int sourceTileId,
            int destinationTileId,
            int requestedStops)
        {
            var (startX, startY) = board.GetXYFromTileId(sourceTileId);
            var (endX, endY) = board.GetXYFromTileId(destinationTileId);
            var line = GenerateLine(startX, startY, endX, endY);
            if (line.Count < 3 || requestedStops <= 0)
            {
                return new List<int>();
            }

            int maxInteriorIndex = line.Count - 2;
            var selectedIndices = new List<int>(requestedStops);
            for (int stopNumber = 1; stopNumber <= requestedStops; stopNumber++)
            {
                int preferredIndex = (int)Math.Round(((line.Count - 1) * stopNumber) / (requestedStops + 1d), MidpointRounding.AwayFromZero);
                preferredIndex = Math.Clamp(preferredIndex, 1, maxInteriorIndex);

                int chosenIndex = FindNearestUnusedInteriorIndex(preferredIndex, maxInteriorIndex, selectedIndices);
                if (chosenIndex >= 1)
                {
                    selectedIndices.Add(chosenIndex);
                }
            }

            return selectedIndices
                .Distinct()
                .OrderBy(index => index)
                .Select(index =>
                {
                    var point = line[index];
                    return (point.y * board.Width) + point.x;
                })
                .Distinct()
                .ToList();
        }

        private static int FindNearestUnusedInteriorIndex(int preferredIndex, int maxInteriorIndex, IReadOnlyCollection<int> usedIndices)
        {
            if (!usedIndices.Contains(preferredIndex))
            {
                return preferredIndex;
            }

            for (int offset = 1; offset <= maxInteriorIndex; offset++)
            {
                int lower = preferredIndex - offset;
                if (lower >= 1 && !usedIndices.Contains(lower))
                {
                    return lower;
                }

                int upper = preferredIndex + offset;
                if (upper <= maxInteriorIndex && !usedIndices.Contains(upper))
                {
                    return upper;
                }
            }

            return -1;
        }

        private static List<(int x, int y)> GenerateLine(int x0, int y0, int x1, int y1)
        {
            var points = new List<(int x, int y)>();

            int dx = Math.Abs(x1 - x0);
            int dy = Math.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            int currentX = x0;
            int currentY = y0;
            while (true)
            {
                points.Add((currentX, currentY));
                if (currentX == x1 && currentY == y1)
                {
                    break;
                }

                int doubledError = 2 * err;
                if (doubledError > -dy)
                {
                    err -= dy;
                    currentX += sx;
                }

                if (doubledError < dx)
                {
                    err += dx;
                    currentY += sy;
                }
            }

            return points;
        }

        private static int SquaredDistance((int x, int y) a, (int x, int y) b)
        {
            int dx = a.x - b.x;
            int dy = a.y - b.y;
            return (dx * dx) + (dy * dy);
        }
    }
}