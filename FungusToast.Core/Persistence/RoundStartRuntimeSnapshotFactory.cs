using System;
using System.Collections.Generic;
using System.Linq;
using FungusToast.Core.AI;
using FungusToast.Core.Board;
using FungusToast.Core.Campaign;
using FungusToast.Core.Growth;
using FungusToast.Core.Mutations;
using FungusToast.Core.Mycovariants;
using FungusToast.Core.Players;

namespace FungusToast.Core.Persistence;

public static class RoundStartRuntimeSnapshotFactory
{
    public static RoundStartRuntimeSnapshot Export(GameBoard board, MycovariantPoolManager? mycovariantPoolManager = null)
    {
        if (board == null)
        {
            throw new ArgumentNullException(nameof(board));
        }

        return new RoundStartRuntimeSnapshot
        {
            BoardWidth = board.Width,
            BoardHeight = board.Height,
            CurrentRound = board.CurrentRound,
            CurrentGrowthCycle = board.CurrentGrowthCycle,
            NecrophyticBloomActivated = board.NecrophyticBloomActivated,
            Players = board.Players
                .OrderBy(player => player.PlayerId)
                .Select(ExportPlayer)
                .ToList(),
            Cells = board.GetAllCells()
                .OrderBy(cell => cell.TileId)
                .Select(ExportCell)
                .ToList(),
            NutrientPatches = board.AllTiles()
                .Where(tile => tile.NutrientPatch != null)
                .OrderBy(tile => tile.TileId)
                .Select(tile => ExportNutrientPatch(tile.TileId, tile.NutrientPatch!))
                .ToList(),
            Chemobeacons = board.GetActiveChemobeacons()
                .OrderBy(marker => marker.PlayerId)
                .Select(marker => new ChemobeaconMarkerSnapshot
                {
                    PlayerId = marker.PlayerId,
                    MutationId = marker.MutationId,
                    TileId = marker.TileId,
                    TurnsRemaining = marker.TurnsRemaining,
                })
                .ToList(),
            PendingHypervariationDraftPlayerIds = board.GetPendingHypervariationDraftPlayerIds().ToList(),
            MycovariantPool = mycovariantPoolManager?.ExportRuntimeState(),
        };
    }

    public static (GameBoard Board, MycovariantPoolManager? MycovariantPoolManager) Restore(
        RoundStartRuntimeSnapshot snapshot,
        Func<PlayerRuntimeSnapshot, IMutationSpendingStrategy?>? mutationStrategyResolver = null,
        IReadOnlyCollection<Mycovariant>? allMycovariants = null)
    {
        if (snapshot == null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        var board = new GameBoard(snapshot.BoardWidth, snapshot.BoardHeight, snapshot.Players.Count);

        foreach (var playerSnapshot in snapshot.Players.OrderBy(player => player.PlayerId))
        {
            var player = RestorePlayer(playerSnapshot, mutationStrategyResolver);
            board.Players.Add(player);
        }

        var controlledTileOwners = board.Players
            .SelectMany(player => player.ControlledTileIds.Select(tileId => new { tileId, player.PlayerId }))
            .GroupBy(entry => entry.tileId)
            .ToDictionary(group => group.Key, group => group.First().PlayerId);

        board.RestoreRoundState(
            snapshot.CurrentRound,
            snapshot.CurrentGrowthCycle,
            snapshot.NecrophyticBloomActivated,
            snapshot.PendingHypervariationDraftPlayerIds);

        foreach (var nutrientPatchSnapshot in snapshot.NutrientPatches.OrderBy(patch => patch.TileId))
        {
            board.PlaceNutrientPatch(nutrientPatchSnapshot.TileId, RestoreNutrientPatch(nutrientPatchSnapshot));
        }

        foreach (var cellSnapshot in snapshot.Cells.OrderBy(cell => cell.TileId))
        {
            int? ownerPlayerId = ResolveRestoredOwnerPlayerId(cellSnapshot, controlledTileOwners, board.Players.Count);

            board.PlaceFungalCell(FungalCell.CreateRestored(
                cellSnapshot.OriginalOwnerPlayerId,
                ownerPlayerId,
                cellSnapshot.TileId,
                cellSnapshot.BirthRound,
                cellSnapshot.CellType,
                cellSnapshot.GrowthCycleAge,
                cellSnapshot.ToxinExpirationAge,
                cellSnapshot.IsNewlyGrown,
                cellSnapshot.IsDying,
                cellSnapshot.IsReceivingToxinDrop,
                cellSnapshot.CauseOfDeath,
                cellSnapshot.SourceOfGrowth,
                cellSnapshot.LastOwnerPlayerId,
                cellSnapshot.ReclaimCount,
                cellSnapshot.IsResistant,
                cellSnapshot.ResistanceSource));
        }

            RestoreMissingStartingTiles(board);

        foreach (var chemobeaconSnapshot in snapshot.Chemobeacons.OrderBy(marker => marker.PlayerId))
        {
            board.TryPlaceChemobeacon(
                chemobeaconSnapshot.PlayerId,
                chemobeaconSnapshot.TileId,
                chemobeaconSnapshot.MutationId,
                chemobeaconSnapshot.TurnsRemaining);
        }

        board.UpdateCachedOccupiedTileRatio();

        MycovariantPoolManager? restoredPoolManager = null;
        if (snapshot.MycovariantPool != null && allMycovariants != null)
        {
            restoredPoolManager = new MycovariantPoolManager();
            restoredPoolManager.RestoreRuntimeState(snapshot.MycovariantPool, allMycovariants);
        }

        return (board, restoredPoolManager);
    }

    private static PlayerRuntimeSnapshot ExportPlayer(Player player)
    {
        return new PlayerRuntimeSnapshot
        {
            PlayerId = player.PlayerId,
            PlayerName = player.PlayerName,
            PlayerType = player.PlayerType,
            AIType = player.AIType,
            MutationPoints = player.MutationPoints,
            IsActive = player.IsActive,
            Score = player.Score,
            WantsToBankPointsThisTurn = player.WantsToBankPointsThisTurn,
            IsLastAiMycovariantDrafterForCurrentDraft = player.IsLastAiMycovariantDrafterForCurrentDraft,
            BaseMutationPointIncome = player.GetBaseMutationPointIncome(),
            StartingTileId = player.StartingTileId,
            MutationStrategyName = player.MutationStrategy?.StrategyName,
            ControlledTileIds = player.ControlledTileIds.OrderBy(id => id).ToList(),
            Mutations = player.PlayerMutations.Values
                .OrderBy(mutation => mutation.MutationId)
                .Select(mutation => new PlayerMutationSnapshot
                {
                    MutationId = mutation.MutationId,
                    CurrentLevel = mutation.CurrentLevel,
                    FirstUpgradeRound = mutation.FirstUpgradeRound,
                    PrereqMetRound = mutation.PrereqMetRound,
                })
                .ToList(),
            Mycovariants = player.PlayerMycovariants
                .OrderBy(mycovariant => mycovariant.MycovariantId)
                .Select(mycovariant => new PlayerMycovariantSnapshot
                {
                    MycovariantId = mycovariant.MycovariantId,
                    HasTriggered = mycovariant.HasTriggered,
                    AIScoreAtDraft = mycovariant.AIScoreAtDraft,
                    EffectCounts = mycovariant.EffectCounts
                        .OrderBy(effect => effect.Key)
                        .Select(effect => new MycovariantEffectCountSnapshot
                        {
                            EffectType = effect.Key,
                            Count = effect.Value,
                        })
                        .ToList(),
                })
                .ToList(),
            Adaptations = player.PlayerAdaptations
                .OrderBy(adaptation => adaptation.Adaptation.Id, StringComparer.Ordinal)
                .Select(adaptation => new PlayerAdaptationSnapshot
                {
                    AdaptationId = adaptation.Adaptation.Id,
                    HasTriggered = adaptation.HasTriggered,
                    HasRuntimeValue = adaptation.HasRuntimeValue,
                    RuntimeValue = adaptation.RuntimeValue,
                })
                .ToList(),
            ActiveSurges = player.ActiveSurges.Values
                .OrderBy(surge => surge.MutationId)
                .Select(surge => new ActiveSurgeSnapshot
                {
                    MutationId = surge.MutationId,
                    Level = surge.Level,
                    TurnsRemaining = surge.TurnsRemaining,
                })
                .ToList(),
        };
    }

    private static FungalCellSnapshot ExportCell(FungalCell cell)
    {
        return new FungalCellSnapshot
        {
            OriginalOwnerPlayerId = cell.OriginalOwnerPlayerId,
            OwnerPlayerId = cell.OwnerPlayerId,
            TileId = cell.TileId,
            BirthRound = cell.BirthRound,
            CellType = cell.CellType,
            GrowthCycleAge = cell.GrowthCycleAge,
            ToxinExpirationAge = cell.ToxinExpirationAge,
            IsNewlyGrown = cell.IsNewlyGrown,
            IsDying = cell.IsDying,
            IsReceivingToxinDrop = cell.IsReceivingToxinDrop,
            CauseOfDeath = cell.CauseOfDeath,
            SourceOfGrowth = cell.SourceOfGrowth,
            LastOwnerPlayerId = cell.LastOwnerPlayerId,
            ReclaimCount = cell.ReclaimCount,
            IsResistant = cell.IsResistant,
            ResistanceSource = cell.ResistanceSource,
        };
    }

    private static NutrientPatchSnapshot ExportNutrientPatch(int tileId, NutrientPatch nutrientPatch)
    {
        return new NutrientPatchSnapshot
        {
            TileId = tileId,
            ClusterId = nutrientPatch.ClusterId,
            ClusterTileCount = nutrientPatch.ClusterTileCount,
            Source = nutrientPatch.Source,
            PatchType = nutrientPatch.PatchType,
            DisplayName = nutrientPatch.DisplayName,
            Description = nutrientPatch.Description,
            RewardType = nutrientPatch.RewardType,
            RewardAmount = nutrientPatch.RewardAmount,
        };
    }

    private static Player RestorePlayer(
        PlayerRuntimeSnapshot snapshot,
        Func<PlayerRuntimeSnapshot, IMutationSpendingStrategy?>? mutationStrategyResolver)
    {
        var player = new Player(snapshot.PlayerId, snapshot.PlayerName, snapshot.PlayerType, snapshot.AIType)
        {
            MutationPoints = snapshot.MutationPoints,
            IsActive = snapshot.IsActive,
            Score = snapshot.Score,
            WantsToBankPointsThisTurn = snapshot.WantsToBankPointsThisTurn,
            IsLastAiMycovariantDrafterForCurrentDraft = snapshot.IsLastAiMycovariantDrafterForCurrentDraft,
        };

        player.SetBaseMutationPoints(snapshot.BaseMutationPointIncome);
        player.SetMutationStrategy(mutationStrategyResolver?.Invoke(snapshot));

        if (snapshot.StartingTileId.HasValue)
        {
            player.SetStartingTile(snapshot.StartingTileId.Value);
        }

        foreach (var tileId in snapshot.ControlledTileIds.OrderBy(id => id))
        {
            player.AddControlledTile(tileId);
        }

        foreach (var mutationSnapshot in snapshot.Mutations.OrderBy(mutation => mutation.MutationId))
        {
            if (!MutationRegistry.All.TryGetValue(mutationSnapshot.MutationId, out var mutation))
            {
                continue;
            }

            player.SetMutationLevel(mutation.Id, mutationSnapshot.CurrentLevel);
            if (player.PlayerMutations.TryGetValue(mutation.Id, out var playerMutation))
            {
                playerMutation.RestoreBookkeeping(mutationSnapshot.FirstUpgradeRound, mutationSnapshot.PrereqMetRound);
            }
        }

        foreach (var mycovariantSnapshot in snapshot.Mycovariants.OrderBy(mycovariant => mycovariant.MycovariantId))
        {
            var mycovariant = MycovariantRepository.All.FirstOrDefault(candidate => candidate.Id == mycovariantSnapshot.MycovariantId);
            if (mycovariant == null)
            {
                continue;
            }

            var playerMycovariant = new PlayerMycovariant(player.PlayerId, mycovariant.Id, mycovariant)
            {
                AIScoreAtDraft = mycovariantSnapshot.AIScoreAtDraft,
            };

            if (mycovariantSnapshot.HasTriggered)
            {
                playerMycovariant.MarkTriggered();
            }

            foreach (var effectCount in mycovariantSnapshot.EffectCounts)
            {
                if (effectCount.Count > 0)
                {
                    playerMycovariant.IncrementEffectCount(effectCount.EffectType, effectCount.Count);
                }
            }

            player.PlayerMycovariants.Add(playerMycovariant);
        }

        foreach (var adaptationSnapshot in snapshot.Adaptations.OrderBy(adaptation => adaptation.AdaptationId, StringComparer.Ordinal))
        {
            if (!AdaptationRepository.TryGetById(adaptationSnapshot.AdaptationId, out var adaptation))
            {
                continue;
            }

            if (!player.TryAddAdaptation(adaptation))
            {
                continue;
            }

            var playerAdaptation = player.GetAdaptation(adaptation.Id);
            if (playerAdaptation == null)
            {
                continue;
            }

            if (adaptationSnapshot.HasTriggered)
            {
                playerAdaptation.MarkTriggered();
            }

            if (adaptationSnapshot.HasRuntimeValue)
            {
                playerAdaptation.SetRuntimeValue(adaptationSnapshot.RuntimeValue);
            }
        }

        foreach (var surgeSnapshot in snapshot.ActiveSurges.OrderBy(surge => surge.MutationId))
        {
            player.ActiveSurges[surgeSnapshot.MutationId] = new Player.ActiveSurgeInfo(
                surgeSnapshot.MutationId,
                surgeSnapshot.Level,
                surgeSnapshot.TurnsRemaining);
        }

        return player;
    }

    private static NutrientPatch RestoreNutrientPatch(NutrientPatchSnapshot snapshot)
    {
        return new NutrientPatch(
            snapshot.ClusterId,
            snapshot.ClusterTileCount,
            snapshot.Source,
            snapshot.PatchType,
            snapshot.DisplayName,
            snapshot.Description,
            snapshot.RewardType,
            snapshot.RewardAmount);
    }

    private static int? ResolveRestoredOwnerPlayerId(
        FungalCellSnapshot snapshot,
        IReadOnlyDictionary<int, int> controlledTileOwners,
        int playerCount)
    {
        if (snapshot.OwnerPlayerId.HasValue)
        {
            return snapshot.OwnerPlayerId;
        }

        if (controlledTileOwners.TryGetValue(snapshot.TileId, out int inferredOwnerId))
        {
            return inferredOwnerId;
        }

        if (snapshot.CellType == FungalCellType.Alive
            && snapshot.OriginalOwnerPlayerId >= 0
            && snapshot.OriginalOwnerPlayerId < playerCount)
        {
            return snapshot.OriginalOwnerPlayerId;
        }

        return null;
    }

    private static void RestoreMissingStartingTiles(GameBoard board)
    {
        foreach (var player in board.Players)
        {
            if (player.StartingTileId.HasValue)
            {
                continue;
            }

            var inferredStartingCell = board.GetAllCellsOwnedBy(player.PlayerId)
                .Where(cell => cell.IsAlive && cell.IsResistant)
                .OrderByDescending(cell => (cell.SourceOfGrowth ?? GrowthSource.Unknown) == GrowthSource.InitialSpore)
                .ThenByDescending(cell => cell.OriginalOwnerPlayerId == player.PlayerId)
                .ThenBy(cell => cell.BirthRound)
                .ThenBy(cell => cell.ReclaimCount)
                .FirstOrDefault();

            if (inferredStartingCell != null)
            {
                player.SetStartingTile(inferredStartingCell.TileId);
            }
        }
    }
}