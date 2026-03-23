using FungusToast.Core.Board;
using FungusToast.Core.Growth;
using FungusToast.Core.Mycovariants;
using FungusToast.Core.Players;
using FungusToast.Core.Tests.Mutations;

namespace FungusToast.Core.Tests.Mycovariants;

public class HyphalDrawMycovariantTests
{
    [Fact]
    public void ResolveHyphalDraw_returns_null_when_player_is_missing_from_board()
    {
        var board = new GameBoard(width: 8, height: 8, playerCount: 0);
        var myco = new PlayerMycovariant(
            playerId: 0,
            mycovariantId: MycovariantIds.HyphalDrawId,
            mycovariant: new Mycovariant { Id = MycovariantIds.HyphalDrawId, Name = "Hyphal Draw" });

        var result = MycovariantEffectProcessor.ResolveHyphalDraw(myco, board, new Random(123), new TestSimulationObserver());

        Assert.Null(result);
    }

    [Fact]
    public void ResolveHyphalDraw_returns_null_when_no_enemy_cells_exist()
    {
        var board = new GameBoard(width: 8, height: 8, playerCount: 1);
        var player = new Player(0, "P0", PlayerTypeEnum.AI);
        board.Players.Add(player);
        board.PlaceInitialSpore(playerId: player.PlayerId, x: 0, y: 1);
        SeedOwnedLivingCells(board, player, new[] { 9, 10, 11 });
        var myco = AddHyphalDraw(player);

        var result = MycovariantEffectProcessor.ResolveHyphalDraw(myco, board, new Random(123), new TestSimulationObserver());

        Assert.Null(result);
    }

    [Fact]
    public void ResolveHyphalDraw_relocates_owned_cells_and_tracks_relocation_count_when_plan_exists()
    {
        var board = new GameBoard(width: 8, height: 8, playerCount: 2);
        var player = new Player(0, "P0", PlayerTypeEnum.AI);
        var enemy = new Player(1, "P1", PlayerTypeEnum.AI);
        board.Players.Add(player);
        board.Players.Add(enemy);

        // Player and enemy cells in lines between their starting spores.
        board.PlaceInitialSpore(playerId: player.PlayerId, x: 0, y: 1);   // tile 8
        board.PlaceInitialSpore(playerId: enemy.PlayerId, x: 7, y: 1);    // tile 15
        SeedOwnedLivingCells(board, player, new[] { 9, 10, 11 });
        SeedOwnedLivingCells(board, enemy, new[] { 12, 13, 14 });
        var myco = AddHyphalDraw(player);

        var beforePlayerTileIds = board.GetAllCellsOwnedBy(player.PlayerId).Select(c => c.TileId).OrderBy(id => id).ToArray();

        var result = MycovariantEffectProcessor.ResolveHyphalDraw(myco, board, new Random(123), new TestSimulationObserver());

        Assert.NotNull(result);
        Assert.NotEmpty(result!.Moves);
        Assert.Equal(result.Moves.Count, myco.EffectCounts[MycovariantEffectType.Relocations]);

        var afterPlayerCells = board.GetAllCellsOwnedBy(player.PlayerId).Where(c => c.IsAlive).ToArray();
        var afterPlayerTileIds = afterPlayerCells.Select(c => c.TileId).OrderBy(id => id).ToArray();

        Assert.Equal(beforePlayerTileIds.Length, afterPlayerTileIds.Length);
        Assert.NotEqual(beforePlayerTileIds, afterPlayerTileIds);
        Assert.All(result.Moves, move =>
        {
            Assert.DoesNotContain(move.SourceTileId, afterPlayerTileIds);
            Assert.Equal(GrowthSource.HyphalDraw, Assert.IsType<FungalCell>(board.GetCell(move.DestinationTileId)).SourceOfGrowth);
        });
    }

    [Fact]
    public void ResolveHyphalDraw_preserves_resistance_on_relocated_cells()
    {
        var board = new GameBoard(width: 8, height: 8, playerCount: 2);
        var player = new Player(0, "P0", PlayerTypeEnum.AI);
        var enemy = new Player(1, "P1", PlayerTypeEnum.AI);
        board.Players.Add(player);
        board.Players.Add(enemy);

        board.PlaceInitialSpore(playerId: player.PlayerId, x: 0, y: 1);   // tile 8
        board.GetCell(8)!.MakeResistant();
        board.PlaceInitialSpore(playerId: enemy.PlayerId, x: 7, y: 1);    // tile 15
        SeedOwnedLivingCells(board, player, new[] { 9, 10, 11 });
        SeedOwnedLivingCells(board, enemy, new[] { 12, 13, 14 });
        var myco = AddHyphalDraw(player);

        var result = MycovariantEffectProcessor.ResolveHyphalDraw(myco, board, new Random(123), new TestSimulationObserver());

        Assert.NotNull(result);
        Assert.Contains(board.GetAllCellsOwnedBy(player.PlayerId), c => c.IsAlive && c.IsResistant);
    }

    private static PlayerMycovariant AddHyphalDraw(Player player)
    {
        var myco = new Mycovariant { Id = MycovariantIds.HyphalDrawId, Name = "Hyphal Draw" };
        player.AddMycovariant(myco);
        return player.GetMycovariant(MycovariantIds.HyphalDrawId)!;
    }

    private static void SeedOwnedLivingCells(GameBoard board, Player owner, IEnumerable<int> tileIds)
    {
        foreach (var tileId in tileIds)
        {
            var cell = new FungalCell(ownerPlayerId: owner.PlayerId, tileId: tileId, source: GrowthSource.InitialSpore, lastOwnerPlayerId: null);
            board.PlaceFungalCell(cell);
            owner.AddControlledTile(tileId);
        }
    }
}
