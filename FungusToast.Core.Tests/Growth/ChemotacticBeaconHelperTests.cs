using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Growth;
using FungusToast.Core.Mutations;
using FungusToast.Core.Players;
using FungusToast.Core.Tests.Mutations;

namespace FungusToast.Core.Tests.Growth;

public class ChemotacticBeaconHelperTests
{
    [Fact]
    public void TryGetActiveMarker_returns_false_when_beacon_is_inactive()
    {
        var board = new GameBoard(width: 5, height: 5, playerCount: 1);
        var player = CreatePlayer();
        board.Players.Add(player);

        bool found = ChemotacticBeaconHelper.TryGetActiveMarker(board, player, out var marker);

        Assert.False(found);
        Assert.Null(marker);
    }

    [Fact]
    public void TryGetActiveMarker_returns_true_when_beacon_is_active_and_marker_exists()
    {
        var board = new GameBoard(width: 5, height: 5, playerCount: 1);
        var player = CreatePlayer();
        board.Players.Add(player);
        player.SetMutationLevel(MutationIds.ChemotacticBeacon, newLevel: 1, currentRound: 1);
        player.ActiveSurges[MutationIds.ChemotacticBeacon] = new Player.ActiveSurgeInfo(MutationIds.ChemotacticBeacon, level: 1, duration: 5);
        bool placed = board.TryPlaceChemobeacon(player.PlayerId, tileId: 12, mutationId: MutationIds.ChemotacticBeacon, turnsRemaining: 5);

        bool found = ChemotacticBeaconHelper.TryGetActiveMarker(board, player, out var marker);

        Assert.True(placed);
        Assert.True(found);
        Assert.NotNull(marker);
        Assert.Equal(12, marker.TileId);
    }

    [Fact]
    public void TrySelectAITargetTile_returns_open_non_nutrient_tile()
    {
        var board = new GameBoard(width: 5, height: 5, playerCount: 1);
        var player = CreatePlayer();
        board.Players.Add(player);
        board.PlaceInitialSpore(player.PlayerId, x: 1, y: 1);
        board.PlaceNutrientPatch(tileId: 6, NutrientPatch.CreateAdaptogenCluster(clusterId: 1, clusterTileCount: 1));

        int? tileId = ChemotacticBeaconHelper.TrySelectAITargetTile(player, board);

        Assert.True(tileId.HasValue);
        Assert.True(board.IsTileOpenForChemobeacon(tileId.Value));
    }

    [Fact]
    public void TrySelectAITargetTile_prefers_tiles_near_the_ideal_distance()
    {
        var board = new GameBoard(width: 50, height: 1, playerCount: 1);
        var player = CreatePlayer();
        board.Players.Add(player);
        board.PlaceInitialSpore(player.PlayerId, x: 1, y: 0);

        int nearTileId = board.GetTile(16, 0)!.TileId;
        int idealTileId = board.GetTile(31, 0)!.TileId;
        BlockAllOpenTilesExcept(board, player.PlayerId, nearTileId, idealTileId);

        int projectedLevel = 1;
        int? tileId = ChemotacticBeaconHelper.TrySelectAITargetTile(player, board, projectedLevel, GameBalance.ChemotacticBeaconSurgeDuration);

        Assert.Equal(idealTileId, tileId);
    }

    [Fact]
    public void TrySelectAITargetTile_prefers_paths_with_nutrient_patches_when_distance_is_still_reasonable()
    {
        var board = new GameBoard(width: 40, height: 20, playerCount: 1);
        var player = CreatePlayer();
        board.Players.Add(player);
        var startTile = Assert.IsType<BoardTile>(board.GetTile(1, 10));
        board.PlaceInitialSpore(player.PlayerId, startTile.X, startTile.Y);

        var emptyPathTarget = Assert.IsType<BoardTile>(board.GetTile(31, 10));
        var nutrientPathTarget = Assert.IsType<BoardTile>(board.GetTile(27, 14));
        PlaceNutrientsOnUniquePathTiles(board, startTile, emptyPathTarget, nutrientPathTarget, count: 2);
        BlockAllOpenTilesExcept(board, player.PlayerId, emptyPathTarget.TileId, nutrientPathTarget.TileId);

        int? tileId = ChemotacticBeaconHelper.TrySelectAITargetTile(player, board, projectedLevel: 1, GameBalance.ChemotacticBeaconSurgeDuration);

        Assert.Equal(nutrientPathTarget.TileId, tileId);
    }

    [Fact]
    public void TrySelectAITargetTile_prefers_paths_with_enemy_living_cells_when_distance_is_similar()
    {
        var board = new GameBoard(width: 40, height: 20, playerCount: 2);
        var player = CreatePlayer();
        var enemy = CreatePlayer(playerId: 1, playerName: "Enemy");
        board.Players.Add(player);
        board.Players.Add(enemy);
        var startTile = Assert.IsType<BoardTile>(board.GetTile(1, 10));
        board.PlaceInitialSpore(player.PlayerId, startTile.X, startTile.Y);
        board.PlaceInitialSpore(enemy.PlayerId, x: 35, y: 10);

        var emptyPathTarget = Assert.IsType<BoardTile>(board.GetTile(31, 10));
        var enemyPathTarget = Assert.IsType<BoardTile>(board.GetTile(29, 6));
        PlaceEnemyCellsOnUniquePathTiles(board, enemy.PlayerId, startTile, emptyPathTarget, enemyPathTarget, count: 5);
        BlockAllOpenTilesExcept(board, player.PlayerId, emptyPathTarget.TileId, enemyPathTarget.TileId);

        int? tileId = ChemotacticBeaconHelper.TrySelectAITargetTile(player, board, projectedLevel: 1, GameBalance.ChemotacticBeaconSurgeDuration);

        Assert.Equal(enemyPathTarget.TileId, tileId);
    }

    [Fact]
    public void TrySelectAITargetTile_falls_back_to_the_best_available_distance_on_small_boards()
    {
        var board = new GameBoard(width: 5, height: 5, playerCount: 1);
        var player = CreatePlayer();
        board.Players.Add(player);
        board.PlaceInitialSpore(player.PlayerId, x: 1, y: 1);

        int shortTileId = board.GetTile(2, 1)!.TileId;
        int farTileId = board.GetTile(4, 4)!.TileId;
        BlockAllOpenTilesExcept(board, player.PlayerId, shortTileId, farTileId);

        int? tileId = ChemotacticBeaconHelper.TrySelectAITargetTile(player, board, projectedLevel: 1, GameBalance.ChemotacticBeaconSurgeDuration);

        Assert.Equal(farTileId, tileId);
    }

    private static void BlockAllOpenTilesExcept(GameBoard board, int ownerPlayerId, params int[] openTileIds)
    {
        var openTileIdSet = openTileIds.ToHashSet();
        foreach (var tile in board.AllTiles())
        {
            if (tile.FungalCell != null || tile.HasNutrientPatch || openTileIdSet.Contains(tile.TileId))
            {
                continue;
            }

            board.PlaceFungalCell(new FungalCell(ownerPlayerId, tile.TileId, GrowthSource.Manual, lastOwnerPlayerId: null));
        }
    }

    private static void PlaceNutrientsOnUniquePathTiles(GameBoard board, BoardTile startTile, BoardTile emptyPathTarget, BoardTile weightedTarget, int count)
    {
        var uniqueTileIds = GetUniqueIntermediateTileIds(board, startTile, emptyPathTarget, weightedTarget);
        for (int index = 0; index < count && index < uniqueTileIds.Count; index++)
        {
            bool placed = board.PlaceNutrientPatch(uniqueTileIds[index], NutrientPatch.CreateAdaptogenCluster(clusterId: index + 1, clusterTileCount: 1));
            Assert.True(placed);
        }
    }

    private static void PlaceEnemyCellsOnUniquePathTiles(GameBoard board, int enemyPlayerId, BoardTile startTile, BoardTile emptyPathTarget, BoardTile weightedTarget, int count)
    {
        var uniqueTileIds = GetUniqueIntermediateTileIds(board, startTile, emptyPathTarget, weightedTarget);
        for (int index = 0; index < count && index < uniqueTileIds.Count; index++)
        {
            board.PlaceFungalCell(new FungalCell(enemyPlayerId, uniqueTileIds[index], GrowthSource.Manual, lastOwnerPlayerId: null));
        }
    }

    private static List<int> GetUniqueIntermediateTileIds(GameBoard board, BoardTile startTile, BoardTile baselineTarget, BoardTile weightedTarget)
    {
        var baselinePath = DirectedVectorHelper
            .GetLineToTarget(startTile.X, startTile.Y, baselineTarget.X, baselineTarget.Y, Math.Max(Math.Abs(baselineTarget.X - startTile.X), Math.Abs(baselineTarget.Y - startTile.Y)))
            .Select(position => (position.x, position.y))
            .ToHashSet();

        return DirectedVectorHelper
            .GetLineToTarget(startTile.X, startTile.Y, weightedTarget.X, weightedTarget.Y, Math.Max(Math.Abs(weightedTarget.X - startTile.X), Math.Abs(weightedTarget.Y - startTile.Y)))
            .Take(Math.Max(0, Math.Max(Math.Abs(weightedTarget.X - startTile.X), Math.Abs(weightedTarget.Y - startTile.Y)) - 1))
            .Where(position => !baselinePath.Contains(position))
            .Select(position => (position.y * board.Width) + position.x)
            .ToList();
    }

    private static Player CreatePlayer(int playerId = 0, string playerName = "Test Player")
    {
        return new Player(playerId: playerId, playerName: playerName, playerType: PlayerTypeEnum.AI)
        {
            MutationPoints = 99
        };
    }
}