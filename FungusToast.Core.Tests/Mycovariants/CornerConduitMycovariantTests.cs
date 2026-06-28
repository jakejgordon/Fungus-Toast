using FungusToast.Core.Board;
using FungusToast.Core.Growth;
using FungusToast.Core.Mycovariants;
using FungusToast.Core.Players;
using FungusToast.Core.Tests.Mutations;

namespace FungusToast.Core.Tests.Mycovariants;

public class CornerConduitMycovariantTests
{
    [Fact]
    public void OnPreGrowthPhase_CornerConduit_emits_projection_for_nearest_corner_and_resolved_tiles()
    {
        var board = new GameBoard(width: 5, height: 5, playerCount: 1);
        var owner = new Player(0, "P0", PlayerTypeEnum.AI);
        board.Players.Add(owner);
        board.PlaceInitialSpore(playerId: owner.PlayerId, x: 2, y: 2);
        owner.AddMycovariant(new Mycovariant { Id = MycovariantIds.CornerConduitIIId, Name = "Corner Conduit II" });

        GameBoard.ConduitProjectionEventArgs? projection = null;
        board.ConduitProjection += e => projection = e;

        MycovariantEffectProcessor.OnPreGrowthPhase_CornerConduit(
            board,
            board.Players,
            new Random(123),
            new TestSimulationObserver());

        Assert.NotNull(projection);
        Assert.Equal(owner.PlayerId, projection!.PlayerId);
        Assert.Equal(GrowthSource.CornerConduit, projection.Source);
        Assert.Equal(owner.StartingTileId, projection.OriginTileId);
        Assert.Equal(new[] { 12, 6, 0 }, projection.PathTileIds);
        Assert.Equal(new[] { 6, 0 }, projection.AffectedTileIds);
        Assert.Equal(0, projection.FinalLandingTileId);

        Assert.Equal(owner.PlayerId, board.GetCell(6)!.OwnerPlayerId);
        Assert.Equal(owner.PlayerId, board.GetCell(0)!.OwnerPlayerId);
        Assert.Equal(GrowthSource.CornerConduit, board.GetCell(6)!.SourceOfGrowth);
        Assert.Equal(GrowthSource.CornerConduit, board.GetCell(0)!.SourceOfGrowth);
    }

    [Fact]
    public void OnPreGrowthPhase_CornerConduit_adds_locked_in_bonus_tiles_from_draft_level()
    {
        var board = new GameBoard(width: 11, height: 11, playerCount: 1);
        var owner = new Player(0, "P0", PlayerTypeEnum.AI);
        board.Players.Add(owner);
        board.PlaceInitialSpore(playerId: owner.PlayerId, x: 5, y: 5);
        owner.AddMycovariant(new Mycovariant { Id = MycovariantIds.CornerConduitIIId, Name = "Corner Conduit II" });
        var myco = owner.GetMycovariant(MycovariantIds.CornerConduitIIId)!;
        myco.DraftedRound = 25;

        MycovariantEffectProcessor.OnPreGrowthPhase_CornerConduit(
            board,
            board.Players,
            new Random(123),
            new TestSimulationObserver());

        Assert.Equal(owner.PlayerId, board.GetCell(48)!.OwnerPlayerId);
        Assert.Equal(owner.PlayerId, board.GetCell(36)!.OwnerPlayerId);
        Assert.Equal(owner.PlayerId, board.GetCell(24)!.OwnerPlayerId);
        Assert.Equal(owner.PlayerId, board.GetCell(12)!.OwnerPlayerId);
        Assert.Equal(owner.PlayerId, board.GetCell(0)!.OwnerPlayerId);
    }

    [Fact]
    public void OnPreGrowthPhase_CornerConduit_targets_cached_corner_tile_on_irregular_board()
    {
        var board = new GameBoard(width: 5, height: 5, playerCount: 1, permanentlyBlockedTileIds: new[] { 0, 4, 20, 24 });
        var owner = new Player(0, "P0", PlayerTypeEnum.AI);
        board.Players.Add(owner);
        board.PlaceInitialSpore(playerId: owner.PlayerId, x: 2, y: 2);
        owner.AddMycovariant(new Mycovariant { Id = MycovariantIds.CornerConduitIIId, Name = "Corner Conduit II" });

        GameBoard.ConduitProjectionEventArgs? projection = null;
        board.ConduitProjection += e => projection = e;

        MycovariantEffectProcessor.OnPreGrowthPhase_CornerConduit(
            board,
            board.Players,
            new Random(123),
            new TestSimulationObserver());

        Assert.NotNull(projection);
        int expectedCornerTileId = board.GetCornerTileId(GameBoard.BoardCorner.TopLeft)!.Value;
        Assert.Equal(expectedCornerTileId, projection!.FinalLandingTileId);
        Assert.NotNull(board.GetCell(expectedCornerTileId));
        Assert.Equal(owner.PlayerId, board.GetCell(expectedCornerTileId)!.OwnerPlayerId);
        Assert.Null(board.GetCell(0));
    }
}
