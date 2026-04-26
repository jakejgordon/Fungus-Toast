using System;
using System.Linq;
using FungusToast.Core.AI;
using FungusToast.Core.Board;
using FungusToast.Core.Campaign;
using FungusToast.Core.Death;
using FungusToast.Core.Growth;
using FungusToast.Core.Mutations;
using FungusToast.Core.Mycovariants;
using FungusToast.Core.Persistence;
using FungusToast.Core.Players;

namespace FungusToast.Core.Tests.Persistence;

public class RoundStartRuntimeSnapshotFactoryTests
{
    [Fact]
    public void Export_and_restore_round_trips_representative_round_start_state()
    {
        var board = new GameBoard(width: 6, height: 6, playerCount: 2);

        var human = new Player(playerId: 0, playerName: "Human", playerType: PlayerTypeEnum.Human, aiType: AITypeEnum.Random)
        {
            MutationPoints = 7,
            IsActive = true,
            Score = 12,
            WantsToBankPointsThisTurn = true,
        };
        human.SetBaseMutationPoints(5);

        var strategy = new SnapshotTestMutationStrategy("snapshot-strategy");
        var ai = new Player(playerId: 1, playerName: "AI", playerType: PlayerTypeEnum.AI, aiType: AITypeEnum.Random)
        {
            MutationPoints = 11,
            IsActive = true,
            Score = 9,
            IsLastAiMycovariantDrafterForCurrentDraft = true,
        };
        ai.SetBaseMutationPoints(4);
        ai.SetMutationStrategy(strategy);

        board.Players.Add(human);
        board.Players.Add(ai);

        board.RestoreRoundState(currentRound: 4, currentGrowthCycle: 3, necrophyticBloomActivated: true, pendingHypervariationDraftPlayerIds: new[] { 1 });
        board.PlaceInitialSpore(playerId: 0, x: 1, y: 1);
        board.PlaceInitialSpore(playerId: 1, x: 4, y: 4);

        board.PlaceNutrientPatch(tileId: 2, NutrientPatch.CreateHypervariationCluster(clusterId: 99, clusterTileCount: 2));

        human.SetMutationLevel(MutationIds.CreepingMold, 2);
        human.PlayerMutations[MutationIds.CreepingMold].RestoreBookkeeping(firstUpgradeRound: 2, prereqMetRound: null);
        human.TryAddAdaptation(AdaptationRepository.All.First(adaptation => adaptation.Id == AdaptationIds.AegisHyphae));
        human.GetAdaptation(AdaptationIds.AegisHyphae)!.MarkTriggered();
        human.GetAdaptation(AdaptationIds.AegisHyphae)!.SetRuntimeValue(3);

        var mycovariant = MycovariantRepository.All.First();
        var playerMycovariant = new PlayerMycovariant(human.PlayerId, mycovariant.Id, mycovariant)
        {
            AIScoreAtDraft = 4.5f,
        };
        playerMycovariant.MarkTriggered();
        playerMycovariant.IncrementEffectCount(MycovariantEffectType.MpBonus, 2);
        human.PlayerMycovariants.Add(playerMycovariant);
        human.ActiveSurges[MutationIds.ChemotacticBeacon] = new Player.ActiveSurgeInfo(MutationIds.ChemotacticBeacon, level: 1, duration: 4);

        ai.SetMutationLevel(MutationIds.ChitinFortification, 1);
        ai.PlayerMutations[MutationIds.ChitinFortification].RestoreBookkeeping(firstUpgradeRound: 3, prereqMetRound: null);

        board.SpawnSporeForPlayer(human, tileId: 8, source: GrowthSource.HyphalOutgrowth);
        var reclaimedCell = new FungalCell(ownerPlayerId: human.PlayerId, tileId: 20, source: GrowthSource.Manual, lastOwnerPlayerId: null);
        reclaimedCell.Kill(DeathReason.Randomness);
        reclaimedCell.Reclaim(ai.PlayerId, GrowthSource.RegenerativeHyphae);
        reclaimedCell.SetBirthRound(board.CurrentRound);
        reclaimedCell.SetGrowthCycleAge(1);
        board.PlaceFungalCell(reclaimedCell);

        var toxinCell = new FungalCell(ownerPlayerId: human.PlayerId, tileId: 15, source: GrowthSource.ChemotacticBeacon, toxinExpirationAge: 6, lastOwnerPlayerId: ai.PlayerId);
        toxinCell.SetBirthRound(board.CurrentRound - 1);
        toxinCell.SetGrowthCycleAge(2);
        board.PlaceFungalCell(toxinCell);

        board.TryPlaceChemobeacon(playerId: human.PlayerId, tileId: 10, mutationId: MutationIds.ChemotacticBeacon, turnsRemaining: 4);

        var poolManager = new MycovariantPoolManager();
        poolManager.InitializePool(MycovariantRepository.All.ToList(), new Random(123));
        poolManager.DrawChoices(3, new Random(456));
        poolManager.TemporarilyRemoveFromPool(mycovariant.Id);

        var snapshot = RoundStartRuntimeSnapshotFactory.Export(board, poolManager);
        var restored = RoundStartRuntimeSnapshotFactory.Restore(
            snapshot,
            mutationStrategyResolver: playerSnapshot => string.Equals(playerSnapshot.MutationStrategyName, strategy.StrategyName, StringComparison.Ordinal)
                ? strategy
                : null,
            allMycovariants: MycovariantRepository.All.ToList());

        Assert.Equal(board.Width, restored.Board.Width);
        Assert.Equal(board.Height, restored.Board.Height);
        Assert.Equal(board.CurrentRound, restored.Board.CurrentRound);
        Assert.Equal(board.CurrentGrowthCycle, restored.Board.CurrentGrowthCycle);
        Assert.True(restored.Board.NecrophyticBloomActivated);
        Assert.Equal(snapshot.PendingHypervariationDraftPlayerIds, restored.Board.GetPendingHypervariationDraftPlayerIds());

        var restoredHuman = restored.Board.Players.Single(player => player.PlayerId == human.PlayerId);
        Assert.Equal(human.MutationPoints, restoredHuman.MutationPoints);
        Assert.True(restoredHuman.WantsToBankPointsThisTurn);
        Assert.Equal(human.GetBaseMutationPointIncome(), restoredHuman.GetBaseMutationPointIncome());
        Assert.Equal(human.StartingTileId, restoredHuman.StartingTileId);
        Assert.Equal(human.ControlledTileIds.OrderBy(id => id), restoredHuman.ControlledTileIds.OrderBy(id => id));
        Assert.Equal(2, restoredHuman.GetMutationLevel(MutationIds.CreepingMold));
        Assert.Equal(2, restoredHuman.PlayerMutations[MutationIds.CreepingMold].FirstUpgradeRound);
        Assert.True(restoredHuman.GetAdaptation(AdaptationIds.AegisHyphae)!.HasTriggered);
        Assert.True(restoredHuman.GetAdaptation(AdaptationIds.AegisHyphae)!.HasRuntimeValue);
        Assert.Equal(3, restoredHuman.GetAdaptation(AdaptationIds.AegisHyphae)!.RuntimeValue);
        Assert.Single(restoredHuman.PlayerMycovariants);
        Assert.True(restoredHuman.PlayerMycovariants[0].HasTriggered);
        Assert.Equal(2, restoredHuman.PlayerMycovariants[0].EffectCounts[MycovariantEffectType.MpBonus]);
        Assert.Equal(4, restoredHuman.ActiveSurges[MutationIds.ChemotacticBeacon].TurnsRemaining);

        var restoredAi = restored.Board.Players.Single(player => player.PlayerId == ai.PlayerId);
        Assert.Same(strategy, restoredAi.MutationStrategy);
        Assert.True(restoredAi.IsLastAiMycovariantDrafterForCurrentDraft);

        var restoredLivingCell = Assert.IsType<FungalCell>(restored.Board.GetCell(8));
        Assert.True(restoredLivingCell.IsAlive);
        Assert.Equal(GrowthSource.HyphalOutgrowth, restoredLivingCell.SourceOfGrowth);

        var restoredReclaimedCell = Assert.IsType<FungalCell>(restored.Board.GetCell(20));
        Assert.True(restoredReclaimedCell.IsAlive);
        Assert.Equal(1, restoredReclaimedCell.ReclaimCount);
        Assert.Equal(ai.PlayerId, restoredReclaimedCell.OwnerPlayerId);

        var restoredToxinCell = Assert.IsType<FungalCell>(restored.Board.GetCell(15));
        Assert.True(restoredToxinCell.IsToxin);
        Assert.Equal(6, restoredToxinCell.ToxinExpirationAge);
        Assert.Equal(2, restoredToxinCell.GrowthCycleAge);
        Assert.Equal(ai.PlayerId, restoredToxinCell.LastOwnerPlayerId);

        var restoredChemobeacon = Assert.IsType<GameBoard.ChemobeaconMarker>(restored.Board.GetChemobeacon(human.PlayerId));
        Assert.Equal(10, restoredChemobeacon.TileId);
        Assert.Equal(4, restoredChemobeacon.TurnsRemaining);

        var restoredPatchTile = restored.Board.GetTileById(2);
        Assert.NotNull(restoredPatchTile);
        Assert.Equal(NutrientPatchType.Hypervariation, restoredPatchTile!.NutrientPatch!.PatchType);
        Assert.Equal(99, restoredPatchTile.NutrientPatch.ClusterId);

        Assert.NotNull(restored.MycovariantPoolManager);
        Assert.Equal(poolManager.ExportRuntimeState().AvailablePoolIds, restored.MycovariantPoolManager!.ExportRuntimeState().AvailablePoolIds);
        Assert.Equal(poolManager.ExportRuntimeState().TemporarilyRemovedMycovariantIds, restored.MycovariantPoolManager.ExportRuntimeState().TemporarilyRemovedMycovariantIds);
    }

    private sealed class SnapshotTestMutationStrategy : IMutationSpendingStrategy
    {
        public SnapshotTestMutationStrategy(string strategyName)
        {
            StrategyName = strategyName;
        }

        public string StrategyName { get; }
        public MutationTier? MaxTier => null;
        public bool? PrioritizeHighTier => null;
        public bool? UsesGrowth => null;
        public bool? UsesCellularResilience => null;
        public bool? UsesFungicide => null;
        public bool? UsesGeneticDrift => null;

        public void SpendMutationPoints(Player player, System.Collections.Generic.List<Mutation> allMutations, GameBoard board, Random rnd, Metrics.ISimulationObserver simulationObserver)
        {
        }
    }
}