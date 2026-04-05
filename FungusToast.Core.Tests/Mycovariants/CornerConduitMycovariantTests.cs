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
}