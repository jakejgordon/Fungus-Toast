using FungusToast.Core.Board;
using FungusToast.Core.Death;
using FungusToast.Core.Events;
using FungusToast.Core.Growth;
using FungusToast.Core.Mycovariants;
using FungusToast.Core.Players;
using FungusToast.Core.Tests.Mutations;

namespace FungusToast.Core.Tests.Mycovariants;

public class SeptalAlarmMycovariantTests
{
    [Fact]
    public void OnCellDeath_SeptalAlarm_does_nothing_when_owner_has_no_mycovariant()
    {
        var board = CreateBoardWithPlayers(out var owner);
        var observer = new TestSimulationObserver();
        var dyingCell = PlaceOwnedLivingCell(board, owner, tileId: 6);
        PlaceOwnedLivingCell(board, owner, tileId: 1);

        MycovariantEffectProcessor.OnCellDeath_SeptalAlarm(
            board,
            new FungalCellDiedEventArgs(6, owner.PlayerId, DeathReason.Age, null, dyingCell),
            board.Players,
            new AlwaysZeroRandom(),
            observer);

        Assert.False(board.GetCell(1)!.IsResistant);
    }

    [Fact]
    public void OnCellDeath_SeptalAlarm_resists_adjacent_friendly_living_non_resistant_cells_when_roll_hits()
    {
        var board = CreateBoardWithPlayers(out var owner);
        var observer = new TestSimulationObserver();
        owner.AddMycovariant(new Mycovariant { Id = MycovariantIds.SeptalAlarmId, Name = "Septal Alarm", AutoMarkTriggered = true });
        var dyingCell = PlaceOwnedLivingCell(board, owner, tileId: 6);
        PlaceOwnedLivingCell(board, owner, tileId: 1);
        PlaceOwnedLivingCell(board, owner, tileId: 5);
        int? resistancePlayerId = null;
        GrowthSource? resistanceSource = null;
        IReadOnlyList<int>? resistanceTiles = null;
        board.ResistanceAppliedBatch += (playerId, source, tileIds) =>
        {
            resistancePlayerId = playerId;
            resistanceSource = source;
            resistanceTiles = tileIds;
        };

        MycovariantEffectProcessor.OnCellDeath_SeptalAlarm(
            board,
            new FungalCellDiedEventArgs(6, owner.PlayerId, DeathReason.Age, null, dyingCell),
            board.Players,
            new AlwaysZeroRandom(),
            observer);

        Assert.True(board.GetCell(1)!.IsResistant);
        Assert.True(board.GetCell(5)!.IsResistant);
        Assert.Equal(owner.PlayerId, resistancePlayerId);
        Assert.Equal(GrowthSource.SeptalAlarm, resistanceSource);
        Assert.Equal(new[] { 1, 5 }, resistanceTiles!.OrderBy(id => id).ToArray());
        Assert.Equal(2, owner.GetMycovariant(MycovariantIds.SeptalAlarmId)!.EffectCounts[MycovariantEffectType.SeptalAlarmResistances]);
    }

    [Fact]
    public void OnCellDeath_SeptalAlarm_ignores_non_friendly_dead_or_already_resistant_adjacent_cells()
    {
        var board = CreateBoardWithPlayers(out var owner);
        var enemy = new Player(1, "P1", PlayerTypeEnum.AI);
        board.Players.Add(enemy);
        var observer = new TestSimulationObserver();
        owner.AddMycovariant(new Mycovariant { Id = MycovariantIds.SeptalAlarmId, Name = "Septal Alarm", AutoMarkTriggered = true });
        var dyingCell = PlaceOwnedLivingCell(board, owner, tileId: 6);
        PlaceOwnedLivingCell(board, owner, tileId: 1).MakeResistant();
        var deadFriendly = PlaceOwnedLivingCell(board, owner, tileId: 5);
        deadFriendly.Kill(DeathReason.Age);
        PlaceOwnedLivingCell(board, enemy, tileId: 7);

        MycovariantEffectProcessor.OnCellDeath_SeptalAlarm(
            board,
            new FungalCellDiedEventArgs(6, owner.PlayerId, DeathReason.Age, null, dyingCell),
            board.Players,
            new AlwaysZeroRandom(),
            observer);

        Assert.True(board.GetCell(1)!.IsResistant);
        Assert.True(board.GetCell(5)!.IsDead);
        Assert.False(board.GetCell(7)!.IsResistant);
        Assert.False(owner.GetMycovariant(MycovariantIds.SeptalAlarmId)!.EffectCounts.ContainsKey(MycovariantEffectType.SeptalAlarmResistances));
    }

    private static GameBoard CreateBoardWithPlayers(out Player owner)
    {
        var board = new GameBoard(width: 5, height: 5, playerCount: 1);
        owner = new Player(0, "P0", PlayerTypeEnum.AI);
        board.Players.Add(owner);
        return board;
    }

    private static FungalCell PlaceOwnedLivingCell(GameBoard board, Player owner, int tileId)
    {
        var cell = new FungalCell(ownerPlayerId: owner.PlayerId, tileId: tileId, source: GrowthSource.InitialSpore, lastOwnerPlayerId: null);
        board.PlaceFungalCell(cell);
        owner.AddControlledTile(tileId);
        return cell;
    }

    private sealed class AlwaysZeroRandom : Random
    {
        protected override double Sample() => 0.0;
    }
}
