using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Death;
using FungusToast.Core.Events;
using FungusToast.Core.Growth;
using FungusToast.Core.Mutations;
using FungusToast.Core.Phases;
using FungusToast.Core.Players;

namespace FungusToast.Core.Tests.Mutations;

public class NecrophyticBloomTests
{
    [Fact]
    public void CalculateNecrophyticBloomClusterThreshold_decreases_by_level()
    {
        Assert.Equal(10, GeneticDriftMutationProcessor.CalculateNecrophyticBloomClusterThreshold(level: 1));
        Assert.Equal(9, GeneticDriftMutationProcessor.CalculateNecrophyticBloomClusterThreshold(level: 2));
        Assert.Equal(6, GeneticDriftMutationProcessor.CalculateNecrophyticBloomClusterThreshold(level: GameBalance.NecrophyticBloomMaxLevel));
    }

    [Fact]
    public void CalculateNecrophyticBloomCompostChance_increases_by_level()
    {
        Assert.Equal(0.20f, GeneticDriftMutationProcessor.CalculateNecrophyticBloomCompostChance(level: 1));
        Assert.Equal(0.25f, GeneticDriftMutationProcessor.CalculateNecrophyticBloomCompostChance(level: 2));
        Assert.Equal(0.40f, GeneticDriftMutationProcessor.CalculateNecrophyticBloomCompostChance(level: GameBalance.NecrophyticBloomMaxLevel));
    }

    [Fact]
    public void CalculateNecrophyticBloomPatchTileCount_reduces_the_resulting_patch_size()
    {
        Assert.Equal(8, GeneticDriftMutationProcessor.CalculateNecrophyticBloomPatchTileCount(10));
        Assert.Equal(6, GeneticDriftMutationProcessor.CalculateNecrophyticBloomPatchTileCount(8));
        Assert.Equal(1, GeneticDriftMutationProcessor.CalculateNecrophyticBloomPatchTileCount(2));
    }

    [Fact]
    public void CalculateNecrophyticBloomPatchTileCount_caps_at_max_patch_size()
    {
        Assert.Equal(GameBalance.NecrophyticBloomMaxPatchSize, GeneticDriftMutationProcessor.CalculateNecrophyticBloomPatchTileCount(20));
        Assert.Equal(GameBalance.NecrophyticBloomMaxPatchSize, GeneticDriftMutationProcessor.CalculateNecrophyticBloomPatchTileCount(50));
        Assert.Equal(GameBalance.NecrophyticBloomMaxPatchSize, GeneticDriftMutationProcessor.CalculateNecrophyticBloomPatchTileCount(GameBalance.NecrophyticBloomMaxPatchSize + GameBalance.NecrophyticBloomPatchTileReduction + 1));
    }

    [Fact]
    public void GetNecrophyticBloomDeadClusters_groups_only_friendly_dead_non_toxin_regions()
    {
        var setup = CreateBoard(width: 4, height: 4);
        setup.player.SetMutationLevel(MutationIds.NecrophyticBloom, newLevel: 1, currentRound: 1);

        CreateDeadCell(setup.board, setup.player, tileId: 0);
        CreateDeadCell(setup.board, setup.player, tileId: 1);
        CreateDeadCell(setup.board, setup.player, tileId: 4);
        CreateDeadCell(setup.board, setup.player, tileId: 10);
        CreateDeadCell(setup.board, setup.enemy, tileId: 2);
        ToxinHelper.ConvertToToxin(setup.board, tileId: 3, GrowthSource.Manual, setup.enemy);

        var clusters = GeneticDriftMutationProcessor.GetNecrophyticBloomDeadClusters(setup.board, setup.player.PlayerId);

        Assert.Equal(2, clusters.Count);
        Assert.Contains(clusters, cluster => cluster.Count == 3 && cluster.Contains(0) && cluster.Contains(1) && cluster.Contains(4));
        Assert.Contains(clusters, cluster => cluster.Count == 1 && cluster.Contains(10));
    }

    [Fact]
    public void ResolveNecrophyticBloomComposting_converts_qualifying_dead_region_into_neutral_patch()
    {
        var setup = CreateBoard(width: 6, height: 2);
        setup.player.SetMutationLevel(MutationIds.NecrophyticBloom, newLevel: GameBalance.NecrophyticBloomMaxLevel, currentRound: 1);

        CreateDeadCell(setup.board, setup.player, tileId: 0);
        CreateDeadCell(setup.board, setup.player, tileId: 1);
        CreateDeadCell(setup.board, setup.player, tileId: 2);
        CreateDeadCell(setup.board, setup.player, tileId: 3);
        CreateDeadCell(setup.board, setup.player, tileId: 4);
        CreateDeadCell(setup.board, setup.player, tileId: 5);

        int createdPatchCount = GeneticDriftMutationProcessor.ResolveNecrophyticBloomComposting(
            setup.player,
            setup.board,
            new DeterministicLowRollRandom(),
            setup.observer);

        Assert.Equal(1, setup.observer.NecrophyticBloomReportCount);
        Assert.Equal(1, createdPatchCount);
        Assert.Equal(1, setup.observer.NecrophyticBloomPatchesByPlayer[setup.player.PlayerId]);
        Assert.All(new[] { 0, 1, 2, 3, 4, 5 }, tileId => Assert.Null(setup.board.GetCell(tileId)));
        Assert.Equal(4, setup.board.AllNutrientPatchTiles().Count());
        Assert.All(new[] { 0, 1, 2, 3 }, tileId => Assert.Equal(NutrientPatchSource.NecrophyticBloom, setup.board.GetTileById(tileId)?.NutrientPatch?.Source));
        Assert.All(new[] { 4, 5 }, tileId => Assert.Null(setup.board.GetTileById(tileId)?.NutrientPatch));
    }

    [Fact]
    public void ExecuteDeathCycle_composts_qualifying_clusters_without_activation_gate()
    {
        var setup = CreateSubscribedBoard(width: 5, height: 5);
        setup.player.SetMutationLevel(MutationIds.NecrophyticBloom, newLevel: GameBalance.NecrophyticBloomMaxLevel, currentRound: 1);

        setup.board.PlaceInitialSpore(setup.player.PlayerId, x: 0, y: 0);
        setup.board.PlaceInitialSpore(setup.enemy.PlayerId, x: 4, y: 4);
        CreateDeadCell(setup.board, setup.player, tileId: 1);
        CreateDeadCell(setup.board, setup.player, tileId: 2);
        CreateDeadCell(setup.board, setup.player, tileId: 3);
        CreateDeadCell(setup.board, setup.player, tileId: 4);
        CreateDeadCell(setup.board, setup.player, tileId: 6);
        CreateDeadCell(setup.board, setup.player, tileId: 7);

        DeathEngine.ExecuteDeathCycle(
            setup.board,
            failedGrowthsByPlayerId: new Dictionary<int, int>(),
            rng: new DeterministicLowRollRandom(),
            simulationObserver: setup.observer);

        Assert.Equal(1, setup.observer.NecrophyticBloomReportCount);
        Assert.Equal(1, setup.observer.NecrophyticBloomPatchesByPlayer[setup.player.PlayerId]);
        Assert.NotNull(setup.board.GetTileById(1)?.NutrientPatch);
        Assert.Equal(4, setup.board.AllNutrientPatchTiles().Count());
    }

    [Fact]
    public void ResolveNecrophyticBloomComposting_limits_patches_to_max_per_round()
    {
        // Create a board wide enough to hold 3 disconnected qualifying clusters
        var setup = CreateBoard(width: 20, height: 2);
        setup.player.SetMutationLevel(MutationIds.NecrophyticBloom, newLevel: GameBalance.NecrophyticBloomMaxLevel, currentRound: 1);

        // Cluster 1: tiles 0–5 (6 contiguous cells, row 0)
        for (int i = 0; i <= 5; i++)
            CreateDeadCell(setup.board, setup.player, tileId: i);

        // Cluster 2: tiles 7–12 (6 contiguous cells, row 0, separated from cluster 1 by tile 6)
        for (int i = 7; i <= 12; i++)
            CreateDeadCell(setup.board, setup.player, tileId: i);

        // Cluster 3: tiles 14–19 (6 contiguous cells, row 0, separated from cluster 2 by tile 13)
        for (int i = 14; i <= 19; i++)
            CreateDeadCell(setup.board, setup.player, tileId: i);

        int createdPatchCount = GeneticDriftMutationProcessor.ResolveNecrophyticBloomComposting(
            setup.player,
            setup.board,
            new DeterministicLowRollRandom(),
            setup.observer);

        Assert.Equal(GameBalance.NecrophyticBloomMaxPatchesPerRound, createdPatchCount);
        Assert.Equal(GameBalance.NecrophyticBloomMaxPatchesPerRound, setup.observer.NecrophyticBloomPatchesByPlayer[setup.player.PlayerId]);
    }

    private static (GameBoard board, List<Player> players, Player player, Player enemy, TestSimulationObserver observer) CreateBoard(int width, int height)
    {
        var board = new GameBoard(width, height, playerCount: 2);
        var player = new Player(playerId: 0, playerName: "P0", playerType: PlayerTypeEnum.AI);
        var enemy = new Player(playerId: 1, playerName: "P1", playerType: PlayerTypeEnum.AI);
        board.Players.Add(player);
        board.Players.Add(enemy);

        return (board, new List<Player> { player, enemy }, player, enemy, new TestSimulationObserver());
    }

    private static (GameBoard board, List<Player> players, Player player, Player enemy, TestSimulationObserver observer) CreateSubscribedBoard(int width, int height)
    {
        var setup = CreateBoard(width, height);
        GameRulesEventSubscriber.SubscribeAll(setup.board, setup.players, new DeterministicHighRollRandom(), setup.observer);
        return setup;
    }

    private static void CreateDeadCell(GameBoard board, Player player, int tileId)
    {
        board.SpawnSporeForPlayer(player, tileId, GrowthSource.Manual);
        var cell = Assert.IsType<FungalCell>(board.GetCell(tileId));
        board.KillFungalCell(cell, DeathReason.Unknown);
    }

    private sealed class DeterministicHighRollRandom : Random
    {
        private int nextIndex;

        public override double NextDouble() => 0.999999;

        public override int Next(int maxValue)
        {
            if (maxValue <= 1)
            {
                return 0;
            }

            int value = nextIndex % maxValue;
            nextIndex++;
            return value;
        }

        public override int Next(int minValue, int maxValue)
        {
            int span = maxValue - minValue;
            if (span <= 1)
            {
                return minValue;
            }

            return minValue + Next(span);
        }
    }

    private sealed class DeterministicMidRandom : Random
    {
        private int nextIndex;

        public override double NextDouble() => 0.5;

        public override int Next(int maxValue)
        {
            if (maxValue <= 1)
            {
                return 0;
            }

            int value = nextIndex % maxValue;
            nextIndex++;
            return value;
        }

        public override int Next(int minValue, int maxValue)
        {
            int span = maxValue - minValue;
            if (span <= 1)
            {
                return minValue;
            }

            return minValue + Next(span);
        }
    }

    private sealed class DeterministicLowRollRandom : Random
    {
        private int nextIndex;

        public override double NextDouble() => 0.0;

        public override int Next(int maxValue)
        {
            if (maxValue <= 1)
            {
                return 0;
            }

            int value = nextIndex % maxValue;
            nextIndex++;
            return value;
        }

        public override int Next(int minValue, int maxValue)
        {
            int span = maxValue - minValue;
            if (span <= 1)
            {
                return minValue;
            }

            return minValue + Next(span);
        }
    }
}