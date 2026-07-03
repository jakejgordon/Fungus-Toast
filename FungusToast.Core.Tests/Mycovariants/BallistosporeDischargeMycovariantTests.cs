using FungusToast.Core.Board;
using FungusToast.Core.Growth;
using FungusToast.Core.Mycovariants;
using FungusToast.Core.Players;
using FungusToast.Core.Tests.Mutations;

namespace FungusToast.Core.Tests.Mycovariants;

public class BallistosporeDischargeMycovariantTests
{
    [Fact]
    public void ResolveBallistosporeDischarge_prefers_orthogonally_adjacent_empty_tiles()
    {
        var board = CreateBoardWithOwnerAndEnemy(out var owner, out var enemy);
        int enemyTileId = GetTileId(board, x: 4, y: 4);

        PlaceLivingCell(board, enemy, enemyTileId);

        int diagonalTileId = GetTileId(board, x: 3, y: 3);

        BallistosporeDischargeHelper.ResolveBallistosporeDischarge(
            CreatePlayerMyco(owner),
            board,
            sporesToDrop: 1,
            new Random(123),
            new TestSimulationObserver());

        var toxinTileIds = GetToxinTileIds(board, owner.PlayerId);

        Assert.Single(toxinTileIds);
        Assert.DoesNotContain(diagonalTileId, toxinTileIds);
        Assert.Contains(toxinTileIds.Single(), GetOrthogonalNeighborTileIds(board, enemyTileId));
    }

    [Fact]
    public void ResolveBallistosporeDischarge_targets_diagonal_tiles_when_no_orthogonal_empty_tiles_exist()
    {
        var board = CreateBoardWithOwnerAndEnemy(out var owner, out var enemy);
        int enemyTileId = GetTileId(board, x: 4, y: 4);

        PlaceLivingCell(board, enemy, enemyTileId);

        foreach (int orthogonalTileId in GetOrthogonalNeighborTileIds(board, enemyTileId))
        {
            PlaceLivingCell(board, owner, orthogonalTileId);
        }

        BallistosporeDischargeHelper.ResolveBallistosporeDischarge(
            CreatePlayerMyco(owner),
            board,
            sporesToDrop: 1,
            new Random(123),
            new TestSimulationObserver());

        var toxinTileIds = GetToxinTileIds(board, owner.PlayerId);

        Assert.Single(toxinTileIds);
        Assert.Contains(toxinTileIds.Single(), GetDiagonalNeighborTileIds(board, enemyTileId));
    }

    [Fact]
    public void ResolveBallistosporeDischarge_clusters_two_tiles_away_when_adjacent_tiles_are_unavailable()
    {
        var board = CreateBoardWithOwnerAndEnemy(out var owner, out var enemy);
        int enemyTileId = GetTileId(board, x: 4, y: 4);

        PlaceLivingCell(board, enemy, enemyTileId);

        foreach (int neighborTileId in board.GetAdjacentTileIds(enemyTileId))
        {
            PlaceLivingCell(board, owner, neighborTileId);
        }

        int farTileId = GetTileId(board, x: 0, y: 0);
        int oppositeFarTileId = GetTileId(board, x: 8, y: 8);

        BallistosporeDischargeHelper.ResolveBallistosporeDischarge(
            CreatePlayerMyco(owner),
            board,
            sporesToDrop: 1,
            new Random(123),
            new TestSimulationObserver());

        var toxinTileIds = GetToxinTileIds(board, owner.PlayerId);

        Assert.Single(toxinTileIds);
        Assert.DoesNotContain(farTileId, toxinTileIds);
        Assert.DoesNotContain(oppositeFarTileId, toxinTileIds);

        var toxinTile = board.GetTileById(toxinTileIds.Single())!;
        var enemyTile = board.GetTileById(enemyTileId)!;
        Assert.Equal(2, Math.Max(Math.Abs(toxinTile.X - enemyTile.X), Math.Abs(toxinTile.Y - enemyTile.Y)));
    }

    private static GameBoard CreateBoardWithOwnerAndEnemy(out Player owner, out Player enemy)
    {
        var board = new GameBoard(width: 9, height: 9, playerCount: 2);
        owner = new Player(0, "Owner", PlayerTypeEnum.AI);
        enemy = new Player(1, "Enemy", PlayerTypeEnum.AI);
        board.Players.Add(owner);
        board.Players.Add(enemy);
        return board;
    }

    private static PlayerMycovariant CreatePlayerMyco(Player owner)
    {
        return new PlayerMycovariant(
            owner.PlayerId,
            MycovariantIds.BallistosporeDischargeIId,
            new Mycovariant { Id = MycovariantIds.BallistosporeDischargeIId, Name = "Ballistospore Discharge I" });
    }

    private static void PlaceLivingCell(GameBoard board, Player owner, int tileId)
    {
        board.PlaceFungalCell(new FungalCell(owner.PlayerId, tileId, GrowthSource.InitialSpore, lastOwnerPlayerId: null));
    }

    private static List<int> GetToxinTileIds(GameBoard board, int ownerPlayerId)
    {
        return board.AllTiles()
            .Where(tile => tile.IsToxin && tile.FungalCell?.OwnerPlayerId == ownerPlayerId)
            .Select(tile => tile.TileId)
            .ToList();
    }

    private static IEnumerable<int> GetOrthogonalNeighborTileIds(GameBoard board, int tileId)
    {
        return board.GetOrthogonalNeighbors(tileId).Select(tile => tile.TileId);
    }

    private static IEnumerable<int> GetDiagonalNeighborTileIds(GameBoard board, int tileId)
    {
        return board.GetDiagonalNeighbors(tileId).Select(tile => tile.TileId);
    }

    private static int GetTileId(GameBoard board, int x, int y)
    {
        return y * board.Width + x;
    }
}
