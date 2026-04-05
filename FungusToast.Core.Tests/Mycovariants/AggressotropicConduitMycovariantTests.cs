using FungusToast.Core.Board;
using FungusToast.Core.Growth;
using FungusToast.Core.Mycovariants;
using FungusToast.Core.Players;
using FungusToast.Core.Tests.Mutations;

namespace FungusToast.Core.Tests.Mycovariants;

public class AggressotropicConduitMycovariantTests
{
    [Fact]
    public void OnPreGrowthPhase_AggressotropicConduit_targets_enemy_start_with_most_living_cells_and_skips_blocked_tiles()
    {
        var board = new GameBoard(width: 7, height: 2, playerCount: 3);
        var owner = new Player(0, "P0", PlayerTypeEnum.AI);
        var targetEnemy = new Player(1, "P1", PlayerTypeEnum.AI);
        var otherEnemy = new Player(2, "P2", PlayerTypeEnum.AI);
        board.Players.Add(owner);
        board.Players.Add(targetEnemy);
        board.Players.Add(otherEnemy);

        board.PlaceInitialSpore(playerId: owner.PlayerId, x: 0, y: 0);
        board.PlaceInitialSpore(playerId: targetEnemy.PlayerId, x: 6, y: 0);
        board.PlaceInitialSpore(playerId: otherEnemy.PlayerId, x: 6, y: 1);

        owner.AddMycovariant(new Mycovariant { Id = MycovariantIds.AggressotropicConduitIIId, Name = "Aggressotropic Conduit II" });
        var ownerMyco = owner.GetMycovariant(MycovariantIds.AggressotropicConduitIIId)!;
    GameBoard.ConduitProjectionEventArgs? projection = null;
    board.ConduitProjection += e => projection = e;

        PlaceOwnedLivingCell(board, owner, tileId: 1);
        PlaceOwnedLivingCell(board, otherEnemy, tileId: 2).MakeResistant();
        PlaceOwnedLivingCell(board, targetEnemy, tileId: 8);
        PlaceOwnedLivingCell(board, targetEnemy, tileId: 12);

        MycovariantEffectProcessor.OnPreGrowthPhase_AggressotropicConduit(
            board,
            board.Players,
            new Random(123),
            new TestSimulationObserver());

        Assert.Equal(owner.PlayerId, board.GetCell(3)!.OwnerPlayerId);
        Assert.True(board.GetCell(3)!.IsAlive);
        Assert.Equal(GrowthSource.AggressotropicConduit, board.GetCell(3)!.SourceOfGrowth);

        Assert.Equal(owner.PlayerId, board.GetCell(4)!.OwnerPlayerId);
        Assert.True(board.GetCell(4)!.IsAlive);
        Assert.True(board.GetCell(4)!.IsResistant);
        Assert.Equal(GrowthSource.AggressotropicConduit, board.GetCell(4)!.SourceOfGrowth);

        Assert.Null(board.GetCell(10));
        Assert.Null(board.GetCell(11));
        Assert.True(board.GetCell(2)!.IsResistant);

        Assert.NotNull(projection);
        Assert.Equal(owner.PlayerId, projection!.PlayerId);
        Assert.Equal(GrowthSource.AggressotropicConduit, projection.Source);
        Assert.Equal(owner.StartingTileId, projection.OriginTileId);
        Assert.Equal(new[] { 0, 1, 2, 3, 4 }, projection.PathTileIds);
        Assert.Equal(new[] { 3, 4 }, projection.AffectedTileIds);
        Assert.Equal(4, projection.FinalLandingTileId);

        Assert.Equal(2, ownerMyco.EffectCounts[MycovariantEffectType.AggressotropicConduitColonizations]);
        Assert.Equal(1, ownerMyco.EffectCounts[MycovariantEffectType.AggressotropicConduitResistantPlacements]);
    }

    [Fact]
    public void OnPreGrowthPhase_AggressotropicConduit_stacks_tile_quota_but_only_makes_one_last_tile_resistant()
    {
        var board = new GameBoard(width: 6, height: 1, playerCount: 2);
        var owner = new Player(0, "P0", PlayerTypeEnum.AI);
        var enemy = new Player(1, "P1", PlayerTypeEnum.AI);
        board.Players.Add(owner);
        board.Players.Add(enemy);

        board.PlaceInitialSpore(playerId: owner.PlayerId, x: 0, y: 0);
        board.PlaceInitialSpore(playerId: enemy.PlayerId, x: 5, y: 0);

        owner.AddMycovariant(new Mycovariant { Id = MycovariantIds.AggressotropicConduitIId, Name = "Aggressotropic Conduit I" });
        owner.AddMycovariant(new Mycovariant { Id = MycovariantIds.AggressotropicConduitIIId, Name = "Aggressotropic Conduit II" });
        var tierI = owner.GetMycovariant(MycovariantIds.AggressotropicConduitIId)!;
        var tierII = owner.GetMycovariant(MycovariantIds.AggressotropicConduitIIId)!;
        GameBoard.ConduitProjectionEventArgs? projection = null;
        board.ConduitProjection += e => projection = e;

        MycovariantEffectProcessor.OnPreGrowthPhase_AggressotropicConduit(
            board,
            board.Players,
            new Random(123),
            new TestSimulationObserver());

        Assert.Equal(owner.PlayerId, board.GetCell(1)!.OwnerPlayerId);
        Assert.Equal(owner.PlayerId, board.GetCell(2)!.OwnerPlayerId);
        Assert.Equal(owner.PlayerId, board.GetCell(3)!.OwnerPlayerId);

        Assert.False(board.GetCell(1)!.IsResistant);
        Assert.False(board.GetCell(2)!.IsResistant);
        Assert.True(board.GetCell(3)!.IsResistant);

        Assert.NotNull(projection);
        Assert.Equal(new[] { 0, 1, 2, 3 }, projection!.PathTileIds);
        Assert.Equal(new[] { 1, 2, 3 }, projection.AffectedTileIds);
        Assert.Equal(3, projection.FinalLandingTileId);

        Assert.Empty(tierI.EffectCounts);
        Assert.Equal(3, tierII.EffectCounts[MycovariantEffectType.AggressotropicConduitColonizations]);
        Assert.Equal(1, tierII.EffectCounts[MycovariantEffectType.AggressotropicConduitResistantPlacements]);
    }

    private static FungalCell PlaceOwnedLivingCell(GameBoard board, Player owner, int tileId)
    {
        var cell = new FungalCell(ownerPlayerId: owner.PlayerId, tileId: tileId, source: GrowthSource.InitialSpore, lastOwnerPlayerId: null);
        board.PlaceFungalCell(cell);
        owner.AddControlledTile(tileId);
        return cell;
    }
}