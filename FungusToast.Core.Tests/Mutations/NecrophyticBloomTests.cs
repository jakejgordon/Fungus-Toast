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
    public void CalculateNecrophyticBloomSporesPerDeath_scales_with_board_size_and_applies_minimum_floor()
    {
        int defaultBoardTiles = GameBalance.BoardWidth * GameBalance.BoardHeight;

        Assert.Equal(40, GeneticDriftMutationProcessor.CalculateNecrophyticBloomSporesPerDeath(defaultBoardTiles, level: 1));
        Assert.Equal(80, GeneticDriftMutationProcessor.CalculateNecrophyticBloomSporesPerDeath(defaultBoardTiles, level: 2));
        Assert.Equal(
            GameBalance.NecrophyticBloomBaseSpores,
            GeneticDriftMutationProcessor.CalculateNecrophyticBloomSporesPerDeath(totalTiles: 25, level: 1));
    }

    [Fact]
    public void BuildNecrophyticBloomTargetPool_excludes_friendly_living_cells_and_toxins()
    {
        var setup = CreateBoard(width: 3, height: 3);
        setup.player.SetMutationLevel(MutationIds.NecrophyticBloom, newLevel: 1, currentRound: 1);

        setup.board.SpawnSporeForPlayer(setup.player, tileId: 0, GrowthSource.Manual);
        setup.board.SpawnSporeForPlayer(setup.player, tileId: 1, GrowthSource.Manual);
        var friendlyDeadCell = Assert.IsType<FungalCell>(setup.board.GetCell(1));
        setup.board.KillFungalCell(friendlyDeadCell, DeathReason.Unknown);
        setup.board.SpawnSporeForPlayer(setup.enemy, tileId: 2, GrowthSource.Manual);
        var enemyDeadCell = Assert.IsType<FungalCell>(setup.board.GetCell(2));
        setup.board.KillFungalCell(enemyDeadCell, DeathReason.Unknown);
        setup.board.SpawnSporeForPlayer(setup.enemy, tileId: 4, GrowthSource.Manual);
        ToxinHelper.ConvertToToxin(setup.board, tileId: 3, GrowthSource.Manual, setup.enemy);

        var pool = GeneticDriftMutationProcessor.BuildNecrophyticBloomTargetPool(
            setup.board,
            setup.player,
            level: 1,
            rng: new Random(1),
            decayPhaseContext: new DecayPhaseContext(setup.board, setup.players));

        Assert.DoesNotContain(0, pool);
        Assert.DoesNotContain(3, pool);
        Assert.Contains(1, pool);
        Assert.Contains(2, pool);
    }

    [Fact]
    public void BuildNecrophyticBloomTargetPool_reduces_invalid_targets_at_higher_levels()
    {
        var setup = CreateBoard(width: 4, height: 4);

        setup.board.SpawnSporeForPlayer(setup.player, tileId: 1, GrowthSource.Manual);
        var friendlyDeadCell = Assert.IsType<FungalCell>(setup.board.GetCell(1));
        setup.board.KillFungalCell(friendlyDeadCell, DeathReason.Unknown);

        setup.board.SpawnSporeForPlayer(setup.enemy, tileId: 2, GrowthSource.Manual);
        var enemyDeadCell = Assert.IsType<FungalCell>(setup.board.GetCell(2));
        setup.board.KillFungalCell(enemyDeadCell, DeathReason.Unknown);
        setup.board.SpawnSporeForPlayer(setup.player, tileId: 0, GrowthSource.Manual);
        ToxinHelper.ConvertToToxin(setup.board, tileId: 3, GrowthSource.Manual, setup.enemy);

        var lowLevelPool = GeneticDriftMutationProcessor.BuildNecrophyticBloomTargetPool(
            setup.board,
            setup.player,
            level: 1,
            rng: new Random(1),
            decayPhaseContext: new DecayPhaseContext(setup.board, setup.players));

        var maxLevelPool = GeneticDriftMutationProcessor.BuildNecrophyticBloomTargetPool(
            setup.board,
            setup.player,
            level: GameBalance.NecrophyticBloomMaxLevel,
            rng: new Random(1),
            decayPhaseContext: new DecayPhaseContext(setup.board, setup.players));

        Assert.Contains(1, lowLevelPool);
        Assert.Contains(2, lowLevelPool);
        Assert.Contains(1, maxLevelPool);
        Assert.Contains(2, maxLevelPool);
        Assert.True(maxLevelPool.Count < lowLevelPool.Count, "Expected higher Necrophytic Bloom levels to shrink the invalid target pool.");
    }

    [Fact]
    public void CalculateNecrophyticBloomBurstReclaims_matches_expected_ratio_without_variance()
    {
        int reclaims = GeneticDriftMutationProcessor.CalculateNecrophyticBloomBurstReclaims(
            sporesToRelease: 40,
            candidateTileCount: 20,
            successfulTargetCount: 6,
            rng: new DeterministicMidRandom());

        Assert.Equal(6, reclaims);
    }

    [Fact]
    public void TriggerNecrophyticBloomInitialBurst_counts_only_the_players_dead_non_toxin_cells_for_spore_generation()
    {
        var setup = CreateBoard(width: 3, height: 1);
        setup.player.SetMutationLevel(MutationIds.NecrophyticBloom, newLevel: GameBalance.NecrophyticBloomMaxLevel, currentRound: 1);

        CreateDeadCell(setup.board, setup.player, tileId: 1);
        CreateDeadCell(setup.board, setup.player, tileId: 0);
        CreateDeadCell(setup.board, setup.enemy, tileId: 2);

        GeneticDriftMutationProcessor.TriggerNecrophyticBloomInitialBurst(
            setup.player,
            setup.board,
            new DeterministicMidRandom(),
            setup.observer,
            new DecayPhaseContext(setup.board, setup.players));

        Assert.Equal(4, setup.observer.NecrophyticBloomSporesByPlayer[setup.player.PlayerId]);
        Assert.Equal(3, setup.observer.NecrophyticBloomReclaimsByPlayer[setup.player.PlayerId]);
        Assert.True(setup.board.GetCell(1)?.IsAlive);
        Assert.True(setup.board.GetCell(0)?.IsAlive);
        Assert.True(setup.board.GetCell(2)?.IsAlive);
        Assert.Equal(setup.player.PlayerId, setup.board.GetCell(2)?.OwnerPlayerId);
    }

    [Fact]
    public void TriggerNecrophyticBloomForNewDeaths_uses_board_scaled_spores_per_new_death()
    {
        var setup = CreateBoard(width: 5, height: 5);
        setup.player.SetMutationLevel(MutationIds.NecrophyticBloom, newLevel: 1, currentRound: 1);
        CreateDeadCell(setup.board, setup.player, tileId: 1);

        GeneticDriftMutationProcessor.TriggerNecrophyticBloomForNewDeaths(
            setup.player,
            setup.board,
            newlyDeadCellCount: 1,
            new DeterministicHighRollRandom(),
            setup.observer,
            new DecayPhaseContext(setup.board, setup.players));

        Assert.Equal(GameBalance.NecrophyticBloomBaseSpores, setup.observer.LastNecrophyticBloomSporesDropped);
    }

    [Fact]
    public void TriggerNecrophyticBloomForNewDeaths_batches_multiple_deaths_into_one_report()
    {
        var setup = CreateBoard(width: 3, height: 1);
        setup.player.SetMutationLevel(MutationIds.NecrophyticBloom, newLevel: GameBalance.NecrophyticBloomMaxLevel, currentRound: 1);

        CreateDeadCell(setup.board, setup.player, tileId: 0);
        CreateDeadCell(setup.board, setup.enemy, tileId: 1);
        CreateDeadCell(setup.board, setup.enemy, tileId: 2);

        GeneticDriftMutationProcessor.TriggerNecrophyticBloomForNewDeaths(
            setup.player,
            setup.board,
            newlyDeadCellCount: 2,
            new DeterministicMidRandom(),
            setup.observer,
            new DecayPhaseContext(setup.board, setup.players));

        Assert.Equal(1, setup.observer.NecrophyticBloomReportCount);
        Assert.Equal(GameBalance.NecrophyticBloomBaseSpores * 2, setup.observer.LastNecrophyticBloomSporesDropped);
        Assert.Equal(3, setup.observer.LastNecrophyticBloomSuccessfulReclaims);
    }

    [Fact]
    public void CellDeath_event_does_not_trigger_necrophytic_bloom_directly()
    {
        var setup = CreateSubscribedBoard(width: 3, height: 1);
        setup.player.SetMutationLevel(MutationIds.NecrophyticBloom, newLevel: 1, currentRound: 1);
        setup.board.NecrophyticBloomActivated = true;

        setup.board.SpawnSporeForPlayer(setup.player, tileId: 0, GrowthSource.Manual);
        CreateDeadCell(setup.board, setup.enemy, tileId: 1);

        var cell = Assert.IsType<FungalCell>(setup.board.GetCell(0));
        setup.board.KillFungalCell(cell, DeathReason.Unknown);

        Assert.Equal(0, setup.observer.NecrophyticBloomReportCount);
    }

    [Fact]
    public void ExecuteDeathCycle_does_not_activate_necrophytic_bloom_below_twenty_percent_occupancy()
    {
        var setup = CreateSubscribedBoard(width: 5, height: 5);
        setup.player.SetMutationLevel(MutationIds.NecrophyticBloom, newLevel: GameBalance.NecrophyticBloomMaxLevel, currentRound: 1);

        setup.board.PlaceInitialSpore(setup.player.PlayerId, x: 0, y: 0);
        setup.board.PlaceInitialSpore(setup.enemy.PlayerId, x: 4, y: 4);
        CreateDeadCell(setup.board, setup.player, tileId: 1);
        CreateDeadCell(setup.board, setup.enemy, tileId: 2);

        DeathEngine.ExecuteDeathCycle(
            setup.board,
            failedGrowthsByPlayerId: new Dictionary<int, int>(),
            rng: new DeterministicHighRollRandom(),
            simulationObserver: setup.observer);

        Assert.False(setup.board.NecrophyticBloomActivated);
        Assert.Equal(0, setup.observer.NecrophyticBloomReportCount);
        Assert.True(setup.board.GetCell(1)?.IsDead);
    }

    [Fact]
    public void ExecuteDeathCycle_triggers_the_initial_burst_once_when_threshold_is_reached()
    {
        var setup = CreateSubscribedBoard(width: 5, height: 5);
        setup.player.SetMutationLevel(MutationIds.NecrophyticBloom, newLevel: GameBalance.NecrophyticBloomMaxLevel, currentRound: 1);

        setup.board.PlaceInitialSpore(setup.player.PlayerId, x: 0, y: 0);
        setup.board.PlaceInitialSpore(setup.enemy.PlayerId, x: 4, y: 4);
        CreateDeadCell(setup.board, setup.player, tileId: 1);
        CreateDeadCell(setup.board, setup.enemy, tileId: 2);
        CreateDeadCell(setup.board, setup.enemy, tileId: 3);

        var rng = new DeterministicHighRollRandom();

        DeathEngine.ExecuteDeathCycle(
            setup.board,
            failedGrowthsByPlayerId: new Dictionary<int, int>(),
            rng: rng,
            simulationObserver: setup.observer);

        Assert.True(setup.board.NecrophyticBloomActivated);
        Assert.Equal(1, setup.observer.NecrophyticBloomReportCount);
        Assert.Equal(GameBalance.NecrophyticBloomBaseSpores, setup.observer.NecrophyticBloomSporesByPlayer[setup.player.PlayerId]);
        Assert.InRange(setup.observer.NecrophyticBloomReclaimsByPlayer[setup.player.PlayerId], 0, GameBalance.NecrophyticBloomBaseSpores);

        int reportCountAfterFirstCycle = setup.observer.NecrophyticBloomReportCount;
        int sporesAfterFirstCycle = setup.observer.NecrophyticBloomSporesByPlayer[setup.player.PlayerId];

        DeathEngine.ExecuteDeathCycle(
            setup.board,
            failedGrowthsByPlayerId: new Dictionary<int, int>(),
            rng: rng,
            simulationObserver: setup.observer);

        Assert.Equal(reportCountAfterFirstCycle, setup.observer.NecrophyticBloomReportCount);
        Assert.Equal(sporesAfterFirstCycle, setup.observer.NecrophyticBloomSporesByPlayer[setup.player.PlayerId]);
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
}