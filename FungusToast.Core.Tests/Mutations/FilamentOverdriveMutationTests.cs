using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Death;
using FungusToast.Core.Growth;
using FungusToast.Core.Mutations;
using FungusToast.Core.Phases;
using FungusToast.Core.Players;

namespace FungusToast.Core.Tests.Mutations;

public class FilamentOverdriveMutationTests
{
    [Fact]
    public void FilamentOverdrive_is_a_tier5_growth_capstone_with_expected_prerequisites()
    {
        Mutation mutation = Assert.IsType<Mutation>(MutationRegistry.GetById(MutationIds.FilamentOverdrive));

        Assert.Equal("Filament Overdrive", mutation.Name);
        Assert.Equal(MutationCategory.Growth, mutation.Category);
        Assert.Equal(MutationTier.Tier5, mutation.Tier);
        Assert.Equal(MutationType.FilamentOverdrive, mutation.Type);
        Assert.Equal(GameBalance.FilamentOverdriveMaxLevel, mutation.MaxLevel);
        Assert.Equal(3, mutation.Prerequisites.Count);
        Assert.Contains(mutation.Prerequisites, prerequisite => prerequisite.MutationId == MutationIds.CreepingMold && prerequisite.RequiredLevel == 3);
        Assert.Contains(mutation.Prerequisites, prerequisite => prerequisite.MutationId == MutationIds.HyphalSurge && prerequisite.RequiredLevel == 1);
        Assert.Contains(mutation.Prerequisites, prerequisite => prerequisite.MutationId == MutationIds.AeratedFrontier && prerequisite.RequiredLevel == 5);
        Assert.Contains("kill the source cell", mutation.Description);
        Assert.Contains("additional tiles instead", mutation.Description);
    }

    [Fact]
    public void Trigger_creates_two_bonus_cells_executes_source_and_emits_ordered_event()
    {
        var setup = CreateRunnerSetup(level: 1);
        GameBoard.FilamentOverdriveEventArgs? eventArgs = null;
        setup.Board.FilamentOverdriveTriggered += args => eventArgs = args;

        int bonusCells = GrowthMutationProcessor.TryTriggerFilamentOverdrive(
            setup.Board,
            setup.Player,
            setup.SourceTileId,
            setup.LandingTileId,
            DiagonalDirection.Northeast,
            new HashSet<int>(),
            new FixedRandom(0.0),
            setup.Observer);

        Assert.Equal(2, bonusCells);
        FungalCell deadSource = Assert.IsType<FungalCell>(setup.Board.GetCell(setup.SourceTileId));
        Assert.True(deadSource.IsDead);
        Assert.Equal(DeathReason.FilamentOverdrive, deadSource.CauseOfDeath);
        Assert.Equal(GrowthSource.FilamentOverdrive, Assert.IsType<FungalCell>(setup.Board.GetCell(TileId(setup.Board, 3, 3))).SourceOfGrowth);
        Assert.Equal(GrowthSource.FilamentOverdrive, Assert.IsType<FungalCell>(setup.Board.GetCell(TileId(setup.Board, 4, 4))).SourceOfGrowth);
        Assert.NotNull(eventArgs);
        Assert.Equal(setup.SourceTileId, eventArgs!.SourceTileId);
        Assert.Equal(new[] { setup.LandingTileId, TileId(setup.Board, 3, 3), TileId(setup.Board, 4, 4) }, eventArgs.RunnerTileIds);
        Assert.Equal(1, setup.Observer.Triggers);
        Assert.Equal(2, setup.Observer.BonusCells);
    }

    [Fact]
    public void Trigger_allows_partial_growth_and_stops_at_first_blocked_tile()
    {
        int blockedTileId = (4 * 7) + 4;
        var setup = CreateRunnerSetup(level: 1, blockedTileIds: new[] { blockedTileId });

        int bonusCells = GrowthMutationProcessor.TryTriggerFilamentOverdrive(
            setup.Board,
            setup.Player,
            setup.SourceTileId,
            setup.LandingTileId,
            DiagonalDirection.Northeast,
            new HashSet<int>(),
            new FixedRandom(0.0),
            setup.Observer);

        Assert.Equal(1, bonusCells);
        Assert.True(Assert.IsType<FungalCell>(setup.Board.GetCell(setup.SourceTileId)).IsDead);
        Assert.NotNull(setup.Board.GetCell(TileId(setup.Board, 3, 3)));
        Assert.Null(setup.Board.GetCell(blockedTileId));
    }

    [Fact]
    public void Max_level_creates_three_bonus_cells()
    {
        var setup = CreateRunnerSetup(GameBalance.FilamentOverdriveMaxLevel);

        int bonusCells = GrowthMutationProcessor.TryTriggerFilamentOverdrive(
            setup.Board,
            setup.Player,
            setup.SourceTileId,
            setup.LandingTileId,
            DiagonalDirection.Northeast,
            new HashSet<int>(),
            new FixedRandom(0.0),
            setup.Observer);

        Assert.Equal(3, bonusCells);
        Assert.Equal(GrowthSource.FilamentOverdrive, Assert.IsType<FungalCell>(setup.Board.GetCell(TileId(setup.Board, 5, 5))).SourceOfGrowth);
    }

    [Fact]
    public void Missed_roll_does_not_create_bonus_cells_or_execute_source()
    {
        var setup = CreateRunnerSetup(level: 1);

        int bonusCells = GrowthMutationProcessor.TryTriggerFilamentOverdrive(
            setup.Board,
            setup.Player,
            setup.SourceTileId,
            setup.LandingTileId,
            DiagonalDirection.Northeast,
            new HashSet<int>(),
            new FixedRandom(0.11),
            setup.Observer);

        Assert.Equal(0, bonusCells);
        Assert.True(Assert.IsType<FungalCell>(setup.Board.GetCell(setup.SourceTileId)).IsAlive);
        Assert.Null(setup.Board.GetCell(TileId(setup.Board, 3, 3)));
        Assert.Equal(0, setup.Observer.Triggers);
    }

    [Fact]
    public void Resistant_source_is_ineligible_and_does_not_consume_a_roll()
    {
        var setup = CreateRunnerSetup(level: GameBalance.FilamentOverdriveMaxLevel);
        Assert.IsType<FungalCell>(setup.Board.GetCell(setup.SourceTileId)).MakeResistant("Test");
        var rng = new CountingRandom();

        int bonusCells = GrowthMutationProcessor.TryTriggerFilamentOverdrive(
            setup.Board,
            setup.Player,
            setup.SourceTileId,
            setup.LandingTileId,
            DiagonalDirection.Northeast,
            new HashSet<int>(),
            rng,
            setup.Observer);

        Assert.Equal(0, bonusCells);
        Assert.Equal(0, rng.NextDoubleCalls);
        Assert.True(Assert.IsType<FungalCell>(setup.Board.GetCell(setup.SourceTileId)).IsAlive);
    }

    [Fact]
    public void Occupied_first_bonus_tile_prevents_trigger_without_consuming_a_roll()
    {
        var setup = CreateRunnerSetup(level: GameBalance.FilamentOverdriveMaxLevel);
        PlaceLivingCell(setup.Board, setup.Player, TileId(setup.Board, 3, 3), GrowthSource.Manual);
        var rng = new CountingRandom();

        int bonusCells = GrowthMutationProcessor.TryTriggerFilamentOverdrive(
            setup.Board,
            setup.Player,
            setup.SourceTileId,
            setup.LandingTileId,
            DiagonalDirection.Northeast,
            new HashSet<int>(),
            rng,
            setup.Observer);

        Assert.Equal(0, bonusCells);
        Assert.Equal(0, rng.NextDoubleCalls);
        Assert.True(Assert.IsType<FungalCell>(setup.Board.GetCell(setup.SourceTileId)).IsAlive);
    }

    [Fact]
    public void Source_execution_can_trigger_necrosporulation()
    {
        var setup = CreateRunnerSetup(level: 1);
        setup.Player.SetMutationLevel(MutationIds.Necrosporulation, GameBalance.NecrosporulationMaxLevel, currentRound: 1);

        GrowthMutationProcessor.TryTriggerFilamentOverdrive(
            setup.Board,
            setup.Player,
            setup.SourceTileId,
            setup.LandingTileId,
            DiagonalDirection.Northeast,
            new HashSet<int>(),
            new FixedRandom(0.0),
            setup.Observer);

        Assert.Contains(
            setup.Board.AllLivingFungalCells(),
            cell => cell.SourceOfGrowth == GrowthSource.Necrosporulation);
    }

    [Fact]
    public void Growth_engine_invokes_overdrive_after_successful_tendril_growth()
    {
        int width = 6;
        int sourceTileId = (1 * width) + 1;
        int[] blockedTileIds =
        {
            (0 * width) + 1,
            (1 * width) + 0,
            (1 * width) + 2,
            (2 * width) + 1
        };
        var board = new GameBoard(width, height: 6, playerCount: 1, permanentlyBlockedTileIds: blockedTileIds);
        var player = AddPlayer(board);
        PlaceLivingCell(board, player, sourceTileId, GrowthSource.Manual);
        player.SetMutationLevel(MutationIds.TendrilNortheast, GameBalance.TendrilDiagonalGrowthMaxLevel, currentRound: 1);
        player.SetMutationLevel(MutationIds.FilamentOverdrive, GameBalance.FilamentOverdriveMaxLevel, currentRound: 1);
        var observer = new FilamentObserver();

        GrowthEngine.ExecuteGrowthCycle(board, board.Players, new FixedRandom(0.0), new RoundContext(), observer);

        Assert.True(Assert.IsType<FungalCell>(board.GetCell(sourceTileId)).IsDead);
        Assert.Equal(1, observer.Triggers);
        Assert.Equal(3, observer.BonusCells);
        Assert.Equal(4, board.AllLivingFungalCells().Count());
    }

    private static RunnerSetup CreateRunnerSetup(int level, IEnumerable<int>? blockedTileIds = null)
    {
        var board = new GameBoard(width: 7, height: 7, playerCount: 1, permanentlyBlockedTileIds: blockedTileIds);
        Player player = AddPlayer(board);
        int sourceTileId = TileId(board, 1, 1);
        int landingTileId = TileId(board, 2, 2);
        PlaceLivingCell(board, player, sourceTileId, GrowthSource.Manual);
        PlaceLivingCell(board, player, landingTileId, GrowthSource.TendrilOutgrowth);
        player.SetMutationLevel(MutationIds.FilamentOverdrive, level, currentRound: 1);
        return new RunnerSetup(board, player, sourceTileId, landingTileId, new FilamentObserver());
    }

    private static Player AddPlayer(GameBoard board)
    {
        var player = new Player(0, "Player", PlayerTypeEnum.AI);
        board.Players.Add(player);
        return player;
    }

    private static FungalCell PlaceLivingCell(GameBoard board, Player player, int tileId, GrowthSource source)
    {
        var cell = new FungalCell(player.PlayerId, tileId, source, lastOwnerPlayerId: null);
        board.PlaceFungalCell(cell);
        return cell;
    }

    private static int TileId(GameBoard board, int x, int y) => (y * board.Width) + x;

    private sealed record RunnerSetup(
        GameBoard Board,
        Player Player,
        int SourceTileId,
        int LandingTileId,
        FilamentObserver Observer);

    private sealed class FixedRandom : Random
    {
        private readonly double value;

        public FixedRandom(double value)
        {
            this.value = value;
        }

        public override double NextDouble() => value;
        public override int Next(int maxValue) => 0;
        public override int Next(int minValue, int maxValue) => minValue;
    }

    private sealed class CountingRandom : Random
    {
        public int NextDoubleCalls { get; private set; }

        public override double NextDouble()
        {
            NextDoubleCalls++;
            return 0.0;
        }
    }

    private sealed class FilamentObserver : TestSimulationObserver
    {
        public int Triggers { get; private set; }
        public int BonusCells { get; private set; }

        public override void RecordFilamentOverdrive(int playerId, int bonusCellsCreated)
        {
            Triggers++;
            BonusCells += bonusCellsCreated;
        }
    }
}
