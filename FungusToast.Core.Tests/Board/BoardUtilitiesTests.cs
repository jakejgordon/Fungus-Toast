using FungusToast.Core.Board;
using FungusToast.Core.Growth;
using FungusToast.Core.Players;

namespace FungusToast.Core.Tests.Board;

public class BoardUtilitiesTests
{
    [Fact]
    public void IsWithinEdgeDistance_returns_true_only_for_tiles_within_requested_distance_of_edge()
    {
        var edgeTile = new BoardTile(x: 0, y: 2, boardWidth: 5);
        var nearEdgeTile = new BoardTile(x: 1, y: 2, boardWidth: 5);
        var centerTile = new BoardTile(x: 2, y: 2, boardWidth: 5);

        Assert.True(BoardUtilities.IsWithinEdgeDistance(edgeTile, width: 5, height: 5, distance: 1));
        Assert.False(BoardUtilities.IsWithinEdgeDistance(nearEdgeTile, width: 5, height: 5, distance: 1));
        Assert.True(BoardUtilities.IsWithinEdgeDistance(nearEdgeTile, width: 5, height: 5, distance: 2));
        Assert.False(BoardUtilities.IsWithinEdgeDistance(centerTile, width: 5, height: 5, distance: 2));
    }

    [Fact]
    public void IsOnBorder_returns_true_only_for_border_tiles()
    {
        var borderTile = new BoardTile(x: 0, y: 2, boardWidth: 5);
        var interiorTile = new BoardTile(x: 2, y: 2, boardWidth: 5);

        Assert.True(BoardUtilities.IsOnBorder(borderTile, width: 5, height: 5));
        Assert.False(BoardUtilities.IsOnBorder(interiorTile, width: 5, height: 5));
    }

    [Fact]
    public void CategorizePlayersByColonySize_splits_other_players_into_larger_and_smaller_or_equal_groups()
    {
        var board = CreateBoardWithPlayers(3);
        board.PlaceInitialSpore(playerId: 0, x: 1, y: 1); // 1 living
        board.PlaceInitialSpore(playerId: 1, x: 3, y: 1); // 2 living
        board.SpawnSporeForPlayer(board.Players[1], tileId: 9, GrowthSource.HyphalOutgrowth);
        board.PlaceInitialSpore(playerId: 2, x: 1, y: 3); // 1 living

        var (larger, smaller) = BoardUtilities.CategorizePlayersByColonySize(board.Players[0], board.Players, board);

        Assert.Equal(new[] { 1 }, larger.Select(player => player.PlayerId).ToArray());
        Assert.Equal(new[] { 2 }, smaller.Select(player => player.PlayerId).ToArray());
    }

    [Fact]
    public void GetPlayerColonySizes_counts_only_living_cells_for_each_player()
    {
        var board = CreateBoardWithPlayers(2);
        board.PlaceInitialSpore(playerId: 0, x: 1, y: 1);
        board.SpawnSporeForPlayer(board.Players[0], tileId: 7, GrowthSource.HyphalOutgrowth);
        board.PlaceInitialSpore(playerId: 1, x: 3, y: 3);

        var colonySizes = BoardUtilities.GetPlayerColonySizes(board.Players, board);

        Assert.Equal(2, colonySizes[0]);
        Assert.Equal(1, colonySizes[1]);
    }

    [Fact]
    public void BuildAllColonySizeCategorizations_returns_expected_relative_groups_for_all_players()
    {
        var board = CreateBoardWithPlayers(3);
        board.PlaceInitialSpore(playerId: 0, x: 1, y: 1); // 1 living
        board.PlaceInitialSpore(playerId: 1, x: 3, y: 1); // 2 living
        board.SpawnSporeForPlayer(board.Players[1], tileId: 9, GrowthSource.HyphalOutgrowth);
        board.PlaceInitialSpore(playerId: 2, x: 1, y: 3); // 1 living

        var categorizations = BoardUtilities.BuildAllColonySizeCategorizations(board.Players, board);

        Assert.Equal(new[] { 1 }, categorizations[0].largerColonies.Select(player => player.PlayerId).ToArray());
        Assert.Equal(new[] { 2 }, categorizations[0].smallerColonies.Select(player => player.PlayerId).ToArray());
        Assert.Empty(categorizations[1].largerColonies);
        Assert.Equal(new[] { 0, 2 }, categorizations[1].smallerColonies.Select(player => player.PlayerId).ToArray());
    }

    [Fact]
    public void GetPlayerBoardSummaries_counts_living_dead_and_toxin_cells_per_player()
    {
        var board = CreateBoardWithPlayers(2);
        board.PlaceInitialSpore(playerId: 0, x: 1, y: 1);
        board.PlaceInitialSpore(playerId: 1, x: 3, y: 3);

        var deadCell = new FungalCell(ownerPlayerId: 0, tileId: 7, source: GrowthSource.InitialSpore, lastOwnerPlayerId: null);
        deadCell.Kill(FungusToast.Core.Death.DeathReason.Age);
        board.PlaceFungalCell(deadCell);

        var toxinCell = new FungalCell(ownerPlayerId: 1, tileId: 8, source: GrowthSource.CytolyticBurst, toxinExpirationAge: 4, lastOwnerPlayerId: null);
        board.PlaceFungalCell(toxinCell);

        var summaries = BoardUtilities.GetPlayerBoardSummaries(board.Players, board);

        Assert.Equal(1, summaries[0].LivingCells);
        Assert.Equal(1, summaries[0].DeadCells);
        Assert.Equal(0, summaries[0].ToxinCells);
        Assert.Equal(1, summaries[1].LivingCells);
        Assert.Equal(0, summaries[1].DeadCells);
        Assert.Equal(1, summaries[1].ToxinCells);
    }

    private static GameBoard CreateBoardWithPlayers(int playerCount)
    {
        var board = new GameBoard(width: 5, height: 5, playerCount: playerCount);
        for (int playerId = 0; playerId < playerCount; playerId++)
        {
            board.Players.Add(new Player(playerId: playerId, playerName: $"P{playerId}", playerType: PlayerTypeEnum.AI));
        }

        return board;
    }
}
