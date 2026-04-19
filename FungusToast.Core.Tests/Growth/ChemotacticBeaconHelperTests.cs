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
    public void TrySelectAITargetTile_prefers_targets_with_more_expected_placements_before_reaching_the_beacon()
    {
        var board = new GameBoard(width: 50, height: 1, playerCount: 1);
        var player = CreatePlayer();
        board.Players.Add(player);
        board.PlaceInitialSpore(player.PlayerId, x: 1, y: 0);

        int shortTargetTileId = board.GetTile(16, 0)!.TileId;
        int longTargetTileId = board.GetTile(31, 0)!.TileId;
        BlockAllOpenTilesExcept(board, player.PlayerId, shortTargetTileId, longTargetTileId);

        int projectedLevel = 1;
        int? tileId = ChemotacticBeaconHelper.TrySelectAITargetTile(player, board, projectedLevel, GameBalance.ChemotacticBeaconSurgeDuration);

        Assert.Equal(longTargetTileId, tileId);
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
        PlaceNutrientsOnUniquePathTiles(board, startTile, emptyPathTarget, nutrientPathTarget, 2, 5);
        BlockAllOpenTilesExcept(board, player.PlayerId, emptyPathTarget.TileId, nutrientPathTarget.TileId);

        int? tileId = ChemotacticBeaconHelper.TrySelectAITargetTile(player, board, projectedLevel: 1, GameBalance.ChemotacticBeaconSurgeDuration);

        Assert.Equal(nutrientPathTarget.TileId, tileId);
    }

    [Fact]
    public void TrySelectAITargetTile_values_larger_nutrient_patches_more_than_smaller_ones()
    {
        var board = new GameBoard(width: 40, height: 20, playerCount: 1);
        var player = CreatePlayer();
        board.Players.Add(player);
        var startTile = Assert.IsType<BoardTile>(board.GetTile(1, 10));
        board.PlaceInitialSpore(player.PlayerId, startTile.X, startTile.Y);

        var smallPatchTarget = Assert.IsType<BoardTile>(board.GetTile(27, 6));
        var largePatchTarget = Assert.IsType<BoardTile>(board.GetTile(27, 14));
        PlaceNutrientsOnUniquePathTiles(board, startTile, smallPatchTarget, largePatchTarget, 1, 6);
        BlockAllOpenTilesExcept(board, player.PlayerId, smallPatchTarget.TileId, largePatchTarget.TileId);

        int? tileId = ChemotacticBeaconHelper.TrySelectAITargetTile(player, board, projectedLevel: 1, GameBalance.ChemotacticBeaconSurgeDuration);

        Assert.Equal(largePatchTarget.TileId, tileId);
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
    public void TrySelectAITargetTile_prefers_enemy_living_cells_over_enemy_toxins_when_other_value_is_similar()
    {
        var board = new GameBoard(width: 40, height: 20, playerCount: 2);
        var player = CreatePlayer();
        var enemy = CreatePlayer(playerId: 1, playerName: "Enemy");
        board.Players.Add(player);
        board.Players.Add(enemy);
        var startTile = Assert.IsType<BoardTile>(board.GetTile(1, 10));
        board.PlaceInitialSpore(player.PlayerId, startTile.X, startTile.Y);
        board.PlaceInitialSpore(enemy.PlayerId, x: 35, y: 10);

        var toxinPathTarget = Assert.IsType<BoardTile>(board.GetTile(29, 6));
        var livingPathTarget = Assert.IsType<BoardTile>(board.GetTile(29, 14));
        PlaceEnemyToxinsOnUniquePathTiles(board, enemy.PlayerId, startTile, toxinPathTarget, livingPathTarget, count: 4, toxinLifespan: 5);
        PlaceEnemyCellsOnUniquePathTiles(board, enemy.PlayerId, startTile, toxinPathTarget, livingPathTarget, count: 4);
        BlockAllOpenTilesExcept(board, player.PlayerId, toxinPathTarget.TileId, livingPathTarget.TileId);

        int? tileId = ChemotacticBeaconHelper.TrySelectAITargetTile(player, board, projectedLevel: 1, GameBalance.ChemotacticBeaconSurgeDuration);

        Assert.Equal(livingPathTarget.TileId, tileId);
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

    [Fact]
    public void GetProjectedGrowthTileIds_matches_chemotactic_beacon_growth_targets_and_skips_friendly_living_cells()
    {
        var board = new GameBoard(width: 10, height: 5, playerCount: 1);
        var player = CreatePlayer();
        board.Players.Add(player);
        board.PlaceInitialSpore(player.PlayerId, x: 1, y: 2);
        board.PlaceFungalCell(new FungalCell(player.PlayerId, board.GetTile(3, 2)!.TileId, GrowthSource.HyphalSurge, lastOwnerPlayerId: null));
        board.PlaceFungalCell(new FungalCell(ownerPlayerId: 99, tileId: board.GetTile(6, 2)!.TileId, source: GrowthSource.Manual, lastOwnerPlayerId: null));

        int targetTileId = board.GetTile(9, 2)!.TileId;

        var previewTileIds = ChemotacticBeaconHelper.GetProjectedGrowthTileIds(player, board, targetTileId, projectedLevel: 1);

        Assert.Equal(
            new[]
            {
                board.GetTile(2, 2)!.TileId,
                board.GetTile(4, 2)!.TileId,
                board.GetTile(5, 2)!.TileId,
                board.GetTile(6, 2)!.TileId,
                board.GetTile(7, 2)!.TileId,
            },
            previewTileIds);
        Assert.DoesNotContain(board.GetTile(3, 2)!.TileId, previewTileIds);
        Assert.DoesNotContain(targetTileId, previewTileIds);
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

    private static void PlaceNutrientsOnUniquePathTiles(GameBoard board, BoardTile startTile, BoardTile baselineTarget, BoardTile weightedTarget, params int[] clusterSizes)
    {
        var uniqueTileIds = GetUniqueIntermediateTileIds(board, startTile, baselineTarget, weightedTarget);
        for (int index = 0; index < clusterSizes.Length && index < uniqueTileIds.Count; index++)
        {
            bool placed = board.PlaceNutrientPatch(uniqueTileIds[index], NutrientPatch.CreateAdaptogenCluster(clusterId: index + 1, clusterTileCount: clusterSizes[index]));
            Assert.True(placed);
        }
    }

    private static void PlaceEnemyCellsOnUniquePathTiles(GameBoard board, int enemyPlayerId, BoardTile startTile, BoardTile baselineTarget, BoardTile weightedTarget, int count)
    {
        var uniqueTileIds = GetUniqueIntermediateTileIds(board, startTile, baselineTarget, weightedTarget);
        for (int index = 0; index < count && index < uniqueTileIds.Count; index++)
        {
            board.PlaceFungalCell(new FungalCell(enemyPlayerId, uniqueTileIds[index], GrowthSource.Manual, lastOwnerPlayerId: null));
        }
    }

    private static void PlaceEnemyToxinsOnUniquePathTiles(GameBoard board, int enemyPlayerId, BoardTile startTile, BoardTile baselineTarget, BoardTile weightedTarget, int count, int toxinLifespan)
    {
        var uniqueTileIds = GetUniqueIntermediateTileIds(board, startTile, baselineTarget, weightedTarget);
        for (int index = 0; index < count && index < uniqueTileIds.Count; index++)
        {
            board.PlaceFungalCell(new FungalCell(enemyPlayerId, uniqueTileIds[index], GrowthSource.Manual, toxinLifespan, lastOwnerPlayerId: null));
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