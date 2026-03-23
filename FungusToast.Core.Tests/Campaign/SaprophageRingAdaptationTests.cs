using FungusToast.Core.Board;
using FungusToast.Core.Campaign;
using FungusToast.Core.Death;
using FungusToast.Core.Events;
using FungusToast.Core.Growth;
using FungusToast.Core.Phases;
using FungusToast.Core.Players;

namespace FungusToast.Core.Tests.Campaign;

public class SaprophageRingAdaptationTests
{
    [Fact]
    public void TryConsumeSaprophageRingDeath_returns_false_when_owner_lacks_adaptation()
    {
        var board = CreateBoardWithPlayers(out var owner);
        var cell = PlaceOwnedCell(board, owner, tileId: 6);

        var consumed = AdaptationEffectProcessor.TryConsumeSaprophageRingDeath(
            board,
            owner,
            cell,
            DeathCalculationResult.Death(1f, DeathReason.Age));

        Assert.False(consumed);
        Assert.NotNull(board.GetCell(6));
    }

    [Fact]
    public void TryConsumeSaprophageRingDeath_returns_false_when_no_adjacent_resistant_anchor_exists()
    {
        var board = CreateBoardWithPlayers(out var owner);
        var cell = PlaceOwnedCell(board, owner, tileId: 6);
        owner.TryAddAdaptation(RequireAdaptation(AdaptationIds.SaprophageRing));

        var consumed = AdaptationEffectProcessor.TryConsumeSaprophageRingDeath(
            board,
            owner,
            cell,
            DeathCalculationResult.Death(1f, DeathReason.Age));

        Assert.False(consumed);
        Assert.NotNull(board.GetCell(6));
    }

    [Fact]
    public void TryConsumeSaprophageRingDeath_consumes_cell_when_adjacent_resistant_anchor_exists()
    {
        var board = CreateBoardWithPlayers(out var owner);
        owner.TryAddAdaptation(RequireAdaptation(AdaptationIds.SaprophageRing));
        PlaceOwnedCell(board, owner, tileId: 1).MakeResistant();
        var cell = PlaceOwnedCell(board, owner, tileId: 6);

        var consumed = AdaptationEffectProcessor.TryConsumeSaprophageRingDeath(
            board,
            owner,
            cell,
            DeathCalculationResult.Death(1f, DeathReason.Age));

        Assert.True(consumed);
        Assert.Null(board.GetCell(6));
        Assert.DoesNotContain(6, owner.ControlledTileIds);
    }

    [Fact]
    public void TryConsumeSaprophageRingDeath_raises_special_board_event_with_anchor_and_target_tiles()
    {
        var board = CreateBoardWithPlayers(out var owner);
        owner.TryAddAdaptation(RequireAdaptation(AdaptationIds.SaprophageRing));
        PlaceOwnedCell(board, owner, tileId: 1).MakeResistant();
        var cell = PlaceOwnedCell(board, owner, tileId: 6);
        SpecialBoardEventArgs? triggeredEvent = null;
        board.SpecialBoardEventTriggered += (_, e) => triggeredEvent = e;

        var consumed = AdaptationEffectProcessor.TryConsumeSaprophageRingDeath(
            board,
            owner,
            cell,
            DeathCalculationResult.Death(1f, DeathReason.Age));

        Assert.True(consumed);
        Assert.NotNull(triggeredEvent);
        Assert.Equal(SpecialBoardEventKind.SaprophageRingTriggered, triggeredEvent!.EventKind);
        Assert.Equal(owner.PlayerId, triggeredEvent.PlayerId);
        Assert.Equal(1, triggeredEvent.SourceTileId);
        Assert.Equal(6, triggeredEvent.DestinationTileId);
        Assert.Equal(new[] { 6 }, triggeredEvent.AffectedTileIds);
    }

    private static GameBoard CreateBoardWithPlayers(out Player owner)
    {
        var board = new GameBoard(width: 5, height: 5, playerCount: 1);
        owner = new Player(0, "P0", PlayerTypeEnum.AI);
        board.Players.Add(owner);
        return board;
    }

    private static FungalCell PlaceOwnedCell(GameBoard board, Player owner, int tileId)
    {
        var cell = new FungalCell(ownerPlayerId: owner.PlayerId, tileId: tileId, source: GrowthSource.InitialSpore, lastOwnerPlayerId: null);
        board.PlaceFungalCell(cell);
        owner.AddControlledTile(tileId);
        return cell;
    }

    private static AdaptationDefinition RequireAdaptation(string adaptationId)
    {
        var found = AdaptationRepository.TryGetById(adaptationId, out var adaptation);
        Assert.True(found, $"Expected adaptation {adaptationId} to exist in the adaptation repository.");
        return Assert.IsType<AdaptationDefinition>(adaptation);
    }
}
