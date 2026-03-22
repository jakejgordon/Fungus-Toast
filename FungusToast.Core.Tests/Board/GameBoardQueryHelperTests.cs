using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Growth;
using FungusToast.Core.Players;
using System.Reflection;

namespace FungusToast.Core.Tests.Board;

public class GameBoardQueryHelperTests
{
    [Fact]
    public void GetAllCells_and_GetAllTileIds_reflect_current_occupied_tiles()
    {
        var board = CreateBoard(width: 5, height: 5, playerCount: 2);

        board.PlaceInitialSpore(playerId: 0, x: 1, y: 1);
        board.PlaceInitialSpore(playerId: 1, x: 3, y: 3);

        var cells = board.GetAllCells();
        var tileIds = board.GetAllTileIds().OrderBy(id => id).ToArray();

        Assert.Equal(2, cells.Count);
        Assert.Equal(new[] { 6, 18 }, tileIds);
        Assert.Equal(tileIds, cells.Select(cell => cell.TileId).OrderBy(id => id).ToArray());
    }

    [Fact]
    public void GetAllCellsOwnedBy_returns_only_cells_owned_by_the_requested_player()
    {
        var board = CreateBoard(width: 5, height: 5, playerCount: 2);

        board.PlaceInitialSpore(playerId: 0, x: 1, y: 1);
        board.SpawnSporeForPlayer(board.Players[0], tileId: 7, GrowthSource.HyphalOutgrowth);
        board.PlaceInitialSpore(playerId: 1, x: 3, y: 3);

        var playerZeroCells = board.GetAllCellsOwnedBy(0).OrderBy(cell => cell.TileId).ToArray();
        var playerOneCells = board.GetAllCellsOwnedBy(1).OrderBy(cell => cell.TileId).ToArray();

        Assert.Equal(new[] { 6, 7 }, playerZeroCells.Select(cell => cell.TileId).ToArray());
        var playerOneCell = Assert.Single(playerOneCells);
        Assert.Equal(18, playerOneCell.TileId);
    }

    [Fact]
    public void GetOccupiedTileRatio_returns_zero_for_empty_board_and_fraction_for_occupied_board()
    {
        var board = CreateBoard(width: 5, height: 5, playerCount: 1);

        Assert.Equal(0f, board.GetOccupiedTileRatio());

        board.PlaceInitialSpore(playerId: 0, x: 1, y: 1);
        board.SpawnSporeForPlayer(board.Players[0], tileId: 7, GrowthSource.HyphalOutgrowth);
        board.SpawnSporeForPlayer(board.Players[0], tileId: 8, GrowthSource.HyphalOutgrowth);

        Assert.Equal(3f / 25f, board.GetOccupiedTileRatio(), precision: 6);
    }

    [Fact]
    public void ShouldTriggerEndgame_becomes_true_when_occupied_ratio_reaches_configured_threshold()
    {
        var board = CreateBoard(width: 10, height: 10, playerCount: 1);

        for (int tileId = 0; tileId < 89; tileId++)
        {
            board.SpawnSporeForPlayer(board.Players[0], tileId, GrowthSource.Manual);
        }

        Assert.Equal(89f / 100f, board.GetOccupiedTileRatio(), precision: 6);
        Assert.False(board.ShouldTriggerEndgame(), $"Expected endgame threshold of {GameBalance.GameEndTileOccupancyThreshold:P0} not to trigger at 89% occupancy.");

        var ninetiethPlaced = board.SpawnSporeForPlayer(board.Players[0], tileId: 89, GrowthSource.Manual);

        Assert.True(ninetiethPlaced, "Expected the 90th tile placement to succeed on an empty board.");
        Assert.Equal(GameBalance.GameEndTileOccupancyThreshold, board.GetOccupiedTileRatio(), precision: 6);
        Assert.True(board.ShouldTriggerEndgame(), $"Expected endgame threshold of {GameBalance.GameEndTileOccupancyThreshold:P0} to trigger at 90% occupancy.");
    }

    [Fact]
    public void AllLivingFungalCells_and_AllLivingFungalCellsWithTiles_return_only_alive_cells()
    {
        var board = CreateBoard(width: 5, height: 5, playerCount: 1);

        board.PlaceInitialSpore(playerId: 0, x: 1, y: 1);
        board.SpawnSporeForPlayer(board.Players[0], tileId: 7, GrowthSource.HyphalOutgrowth);

        var toxinCell = new FungalCell(ownerPlayerId: 0, tileId: 12, source: GrowthSource.CytolyticBurst, toxinExpirationAge: 3, lastOwnerPlayerId: null);
        PlaceCell(board, toxinCell);

        var livingCells = board.AllLivingFungalCells().OrderBy(cell => cell.TileId).ToArray();
        var livingCellsWithTiles = board.AllLivingFungalCellsWithTiles().OrderBy(pair => pair.cell.TileId).ToArray();

        Assert.Equal(new[] { 6, 7 }, livingCells.Select(cell => cell.TileId).ToArray());
        Assert.Equal(new[] { 6, 7 }, livingCellsWithTiles.Select(pair => pair.cell.TileId).ToArray());
        Assert.Equal(new[] { 6, 7 }, livingCellsWithTiles.Select(pair => pair.tile.TileId).ToArray());
    }

    [Fact]
    public void AllToxinTiles_and_AllToxinFungalCells_return_only_toxin_entries()
    {
        var board = CreateBoard(width: 5, height: 5, playerCount: 1);

        board.PlaceInitialSpore(playerId: 0, x: 1, y: 1);
        var toxinCell = new FungalCell(ownerPlayerId: 0, tileId: 12, source: GrowthSource.CytolyticBurst, toxinExpirationAge: 3, lastOwnerPlayerId: null);
        PlaceCell(board, toxinCell);

        var toxinTile = Assert.Single(board.AllToxinTiles());
        var toxinFungalCell = Assert.Single(board.AllToxinFungalCells());

        Assert.Equal(12, toxinTile.TileId);
        Assert.Equal(12, toxinFungalCell.TileId);
        Assert.Equal(FungalCellType.Toxin, toxinTile.CellType);
        Assert.Equal(FungalCellType.Toxin, toxinFungalCell.CellType);
    }

    [Fact]
    public void GetAdjacentLivingTiles_returns_only_alive_neighbors_and_respects_excluded_player()
    {
        var board = CreateBoard(width: 5, height: 5, playerCount: 2);

        board.PlaceInitialSpore(playerId: 0, x: 1, y: 1); // tile 6
        board.SpawnSporeForPlayer(board.Players[1], tileId: 7, GrowthSource.HyphalOutgrowth); // adjacent, alive
        board.SpawnSporeForPlayer(board.Players[0], tileId: 11, GrowthSource.HyphalOutgrowth); // adjacent, alive
        var toxinCell = new FungalCell(ownerPlayerId: 1, tileId: 12, source: GrowthSource.CytolyticBurst, toxinExpirationAge: 3, lastOwnerPlayerId: null); // adjacent, not alive
        PlaceCell(board, toxinCell);

        var allAdjacentLiving = board.GetAdjacentLivingTiles(6).Select(tile => tile.TileId).OrderBy(id => id).ToArray();
        var excludingPlayerZero = board.GetAdjacentLivingTiles(6, excludePlayerId: 0).Select(tile => tile.TileId).OrderBy(id => id).ToArray();

        Assert.Equal(new[] { 7, 11 }, allAdjacentLiving);
        Assert.Equal(new[] { 7 }, excludingPlayerZero);
    }

    private static void PlaceCell(GameBoard board, FungalCell cell)
    {
        var placeFungalCell = typeof(GameBoard).GetMethod("PlaceFungalCell", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(placeFungalCell);
        placeFungalCell!.Invoke(board, new object[] { cell });
    }

    private static GameBoard CreateBoard(int width, int height, int playerCount)
    {
        var board = new GameBoard(width, height, playerCount);
        for (int playerId = 0; playerId < playerCount; playerId++)
        {
            board.Players.Add(new Player(playerId, $"Player {playerId}", PlayerTypeEnum.AI));
        }

        return board;
    }
}
