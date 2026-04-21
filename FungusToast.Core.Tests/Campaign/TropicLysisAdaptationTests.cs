using FungusToast.Core.Board;
using FungusToast.Core.Campaign;
using FungusToast.Core.Death;
using FungusToast.Core.Growth;
using FungusToast.Core.Mutations;
using FungusToast.Core.Phases;
using FungusToast.Core.Players;
using FungusToast.Core.Tests.Mutations;

namespace FungusToast.Core.Tests.Campaign;

public class TropicLysisAdaptationTests
{
    [Fact]
    public void TryResolveTropicLysisAfterDraft_clears_enemy_cells_corpses_and_toxins_in_anchor_union()
    {
        var board = CreateBoardWithPlayers(out var player, out var enemy);
        var observer = new TestSimulationObserver();
        player.TryAddAdaptation(RequireAdaptation(AdaptationIds.TropicLysis));
        board.PlaceInitialSpore(player.PlayerId, x: 4, y: 4);
        ActivateBeacon(player, board, beaconTileId: board.GetTile(6, 4)!.TileId);

        PlaceLivingCell(board, enemy.PlayerId, board.GetTile(3, 4)!.TileId);
        PlaceCorpse(board, enemy.PlayerId, board.GetTile(4, 3)!.TileId);
        PlaceToxin(board, enemy.PlayerId, board.GetTile(6, 3)!.TileId);
        PlaceLivingCell(board, enemy.PlayerId, board.GetTile(7, 4)!.TileId);

        var result = AdaptationEffectProcessor.TryResolveTropicLysisAfterDraft(player, board, observer);

        Assert.True(result.AnyCleared);
        Assert.Equal(player.PlayerId, result.PlayerId);
        Assert.Equal(player.StartingTileId, result.StartingTileId);
        Assert.Equal(board.GetTile(6, 4)!.TileId, result.BeaconTileId);
        Assert.Equal(2, result.EnemyLivingCellsCleared);
        Assert.Equal(1, result.CorpsesCleared);
        Assert.Equal(1, result.ToxinsCleared);
        Assert.Equal(4, result.TotalCleared);
        Assert.Null(board.GetCell(board.GetTile(3, 4)!.TileId));
        Assert.Null(board.GetCell(board.GetTile(4, 3)!.TileId));
        Assert.Null(board.GetCell(board.GetTile(6, 3)!.TileId));
        Assert.Null(board.GetCell(board.GetTile(7, 4)!.TileId));
    }

    [Fact]
    public void TryResolveTropicLysisAfterDraft_leaves_friendly_cells_and_resistant_enemy_cells()
    {
        var board = CreateBoardWithPlayers(out var player, out var enemy);
        var observer = new TestSimulationObserver();
        player.TryAddAdaptation(RequireAdaptation(AdaptationIds.TropicLysis));
        board.PlaceInitialSpore(player.PlayerId, x: 4, y: 4);
        ActivateBeacon(player, board, beaconTileId: board.GetTile(6, 4)!.TileId);

        int friendlyLivingTileId = board.GetTile(4, 5)!.TileId;
        int friendlyCorpseTileId = board.GetTile(5, 5)!.TileId;
        int friendlyToxinTileId = board.GetTile(5, 4)!.TileId;
        int resistantEnemyTileId = board.GetTile(6, 5)!.TileId;

        PlaceLivingCell(board, player.PlayerId, friendlyLivingTileId);
        PlaceCorpse(board, player.PlayerId, friendlyCorpseTileId);
        PlaceToxin(board, player.PlayerId, friendlyToxinTileId);
        PlaceLivingCell(board, enemy.PlayerId, resistantEnemyTileId);
        board.GetCell(resistantEnemyTileId)!.MakeResistant();

        var result = AdaptationEffectProcessor.TryResolveTropicLysisAfterDraft(player, board, observer);

        Assert.False(result.AnyCleared);
        Assert.NotNull(board.GetCell(friendlyLivingTileId));
        Assert.True(board.GetCell(friendlyLivingTileId)!.IsAlive);
        Assert.NotNull(board.GetCell(friendlyCorpseTileId));
        Assert.True(board.GetCell(friendlyCorpseTileId)!.IsDead);
        Assert.NotNull(board.GetCell(friendlyToxinTileId));
        Assert.True(board.GetCell(friendlyToxinTileId)!.IsToxin);
        Assert.NotNull(board.GetCell(resistantEnemyTileId));
        Assert.True(board.GetCell(resistantEnemyTileId)!.IsAlive);
        Assert.True(board.GetCell(resistantEnemyTileId)!.IsResistant);
    }

    [Fact]
    public void TryResolveTropicLysisAfterDraft_falls_back_to_starting_spore_when_no_beacon_is_active()
    {
        var board = CreateBoardWithPlayers(out var player, out var enemy);
        var observer = new TestSimulationObserver();
        player.TryAddAdaptation(RequireAdaptation(AdaptationIds.TropicLysis));
        board.PlaceInitialSpore(player.PlayerId, x: 4, y: 4);

        int startRadiusTileId = board.GetTile(2, 4)!.TileId;
        int beaconOnlyTileId = board.GetTile(8, 4)!.TileId;
        PlaceLivingCell(board, enemy.PlayerId, startRadiusTileId);
        PlaceLivingCell(board, enemy.PlayerId, beaconOnlyTileId);

        var result = AdaptationEffectProcessor.TryResolveTropicLysisAfterDraft(player, board, observer);

        Assert.True(result.AnyCleared);
        Assert.Null(result.BeaconTileId);
        Assert.Null(board.GetCell(startRadiusTileId));
        Assert.NotNull(board.GetCell(beaconOnlyTileId));
        Assert.True(board.GetCell(beaconOnlyTileId)!.IsAlive);
    }

    [Fact]
    public void TryResolveTropicLysisAfterDraft_deduplicates_tiles_that_are_in_both_anchor_areas()
    {
        var board = CreateBoardWithPlayers(out var player, out var enemy);
        var observer = new TestSimulationObserver();
        player.TryAddAdaptation(RequireAdaptation(AdaptationIds.TropicLysis));
        board.PlaceInitialSpore(player.PlayerId, x: 4, y: 4);
        ActivateBeacon(player, board, beaconTileId: board.GetTile(5, 4)!.TileId);

        int overlapTileId = board.GetTile(4, 5)!.TileId;
        PlaceLivingCell(board, enemy.PlayerId, overlapTileId);

        var result = AdaptationEffectProcessor.TryResolveTropicLysisAfterDraft(player, board, observer);

        Assert.True(result.AnyCleared);
        Assert.Single(result.AffectedTileIds);
        Assert.Equal(overlapTileId, result.AffectedTileIds[0]);
        Assert.Equal(1, result.EnemyLivingCellsCleared);
    }

    [Fact]
    public void TryResolveTropicLysisAfterDraft_uses_a_circular_radius_instead_of_square_corners()
    {
        var board = CreateBoardWithPlayers(out var player, out var enemy);
        var observer = new TestSimulationObserver();
        player.TryAddAdaptation(RequireAdaptation(AdaptationIds.TropicLysis));
        board.PlaceInitialSpore(player.PlayerId, x: 4, y: 4);

        int circularEdgeTileId = board.GetTile(7, 4)!.TileId;
        int squareCornerTileId = board.GetTile(7, 7)!.TileId;
        PlaceLivingCell(board, enemy.PlayerId, circularEdgeTileId);
        PlaceLivingCell(board, enemy.PlayerId, squareCornerTileId);

        var result = AdaptationEffectProcessor.TryResolveTropicLysisAfterDraft(player, board, observer);

        Assert.True(result.AnyCleared);
        Assert.Contains(circularEdgeTileId, result.AffectedTileIds);
        Assert.DoesNotContain(squareCornerTileId, result.AffectedTileIds);
        Assert.Null(board.GetCell(circularEdgeTileId));
        Assert.NotNull(board.GetCell(squareCornerTileId));
        Assert.True(board.GetCell(squareCornerTileId)!.IsAlive);
    }

    [Fact]
    public void TryResolveTropicLysisAfterDraft_returns_none_when_player_lacks_adaptation()
    {
        var board = CreateBoardWithPlayers(out var player, out var enemy);
        var observer = new TestSimulationObserver();
        board.PlaceInitialSpore(player.PlayerId, x: 4, y: 4);
        PlaceLivingCell(board, enemy.PlayerId, board.GetTile(3, 4)!.TileId);

        var result = AdaptationEffectProcessor.TryResolveTropicLysisAfterDraft(player, board, observer);

        Assert.False(result.AnyCleared);
        Assert.NotNull(board.GetCell(board.GetTile(3, 4)!.TileId));
    }

    private static GameBoard CreateBoardWithPlayers(out Player player, out Player enemy)
    {
        var board = new GameBoard(width: 9, height: 9, playerCount: 2);
        player = new Player(0, "Player", PlayerTypeEnum.Human);
        enemy = new Player(1, "Enemy", PlayerTypeEnum.AI);
        board.Players.Add(player);
        board.Players.Add(enemy);
        return board;
    }

    private static void ActivateBeacon(Player player, GameBoard board, int beaconTileId)
    {
        player.ActiveSurges[MutationIds.ChemotacticBeacon] = new Player.ActiveSurgeInfo(MutationIds.ChemotacticBeacon, level: 1, duration: 5);
        bool placed = board.TryPlaceChemobeacon(player.PlayerId, beaconTileId, MutationIds.ChemotacticBeacon, turnsRemaining: 5);
        Assert.True(placed);
    }

    private static void PlaceLivingCell(GameBoard board, int ownerPlayerId, int tileId)
    {
        bool placed = board.SpawnSporeForPlayer(board.Players[ownerPlayerId], tileId, GrowthSource.Manual);
        Assert.True(placed);
    }

    private static void PlaceCorpse(GameBoard board, int ownerPlayerId, int tileId)
    {
        PlaceLivingCell(board, ownerPlayerId, tileId);
        board.KillFungalCell(Assert.IsType<FungalCell>(board.GetCell(tileId)), DeathReason.Unknown, ownerPlayerId);
    }

    private static void PlaceToxin(GameBoard board, int ownerPlayerId, int tileId)
    {
        ToxinHelper.ConvertToToxin(board, tileId, toxinLifespan: 5, growthSource: GrowthSource.CytolyticBurst, owner: board.Players[ownerPlayerId]);
    }

    private static AdaptationDefinition RequireAdaptation(string adaptationId)
    {
        var found = AdaptationRepository.TryGetById(adaptationId, out var adaptation);
        Assert.True(found, $"Expected adaptation {adaptationId} to exist in the adaptation repository.");
        return Assert.IsType<AdaptationDefinition>(adaptation);
    }
}