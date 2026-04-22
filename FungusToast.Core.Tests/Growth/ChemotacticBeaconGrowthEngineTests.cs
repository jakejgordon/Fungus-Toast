using FungusToast.Core.Board;
using FungusToast.Core.Death;
using FungusToast.Core.Growth;
using FungusToast.Core.Mutations;
using FungusToast.Core.Phases;
using FungusToast.Core.Players;
using FungusToast.Core.Tests.Mutations;

namespace FungusToast.Core.Tests.Growth;

public class ChemotacticBeaconGrowthEngineTests
{
    [Fact]
    public void ProcessChemotacticBeacon_projects_hyphal_vectoring_length_toward_marker()
    {
        var setup = CreateBeaconBoard(level: 1, beaconTileId: 29);

        MycelialSurgeMutationProcessor.ProcessChemotacticBeacon(setup.board, setup.players, new Random(1), new TestSimulationObserver());

        AssertOwnedByPlayer(setup.board, setup.player.PlayerId, 2, 2);
        AssertOwnedByPlayer(setup.board, setup.player.PlayerId, 3, 2);
        AssertOwnedByPlayer(setup.board, setup.player.PlayerId, 4, 2);
        Assert.Null(setup.board.GetTile(5, 2)?.FungalCell);
        Assert.Equal(GrowthSource.ChemotacticBeacon, setup.board.GetTile(4, 2)?.FungalCell?.SourceOfGrowth);
    }

    [Fact]
    public void ProcessChemotacticBeacon_skips_friendly_living_cells_but_continues_the_line()
    {
        var setup = CreateBeaconBoard(level: 1, beaconTileId: 29);
        setup.board.PlaceFungalCell(new FungalCell(setup.player.PlayerId, setup.board.GetTile(3, 2)!.TileId, GrowthSource.HyphalSurge, lastOwnerPlayerId: null));

        MycelialSurgeMutationProcessor.ProcessChemotacticBeacon(setup.board, setup.players, new Random(1), new TestSimulationObserver());

        AssertOwnedByPlayer(setup.board, setup.player.PlayerId, 3, 2);
        AssertOwnedByPlayer(setup.board, setup.player.PlayerId, 2, 2);
        AssertOwnedByPlayer(setup.board, setup.player.PlayerId, 4, 2);
        AssertOwnedByPlayer(setup.board, setup.player.PlayerId, 5, 2);
        Assert.Null(setup.board.GetTile(6, 2)?.FungalCell);
        Assert.Equal(GrowthSource.HyphalSurge, setup.board.GetTile(3, 2)?.FungalCell?.SourceOfGrowth);
        Assert.Equal(GrowthSource.ChemotacticBeacon, setup.board.GetTile(4, 2)?.FungalCell?.SourceOfGrowth);
        Assert.Equal(GrowthSource.ChemotacticBeacon, setup.board.GetTile(5, 2)?.FungalCell?.SourceOfGrowth);
    }

    [Fact]
    public void ProcessChemotacticBeacon_uses_the_starting_spore_path_even_when_other_friendly_cells_are_closer()
    {
        var setup = CreateBeaconBoard(level: 1, beaconTileId: 29);
        var startingTile = Assert.IsType<BoardTile>(setup.board.GetTile(1, 2));
        var startingCell = Assert.IsType<FungalCell>(startingTile.FungalCell);
        setup.board.KillFungalCell(startingCell, FungusToast.Core.Death.DeathReason.Unknown, killerPlayerId: null);
        setup.board.PlaceFungalCell(new FungalCell(setup.player.PlayerId, setup.board.GetTile(4, 2)!.TileId, GrowthSource.HyphalSurge, lastOwnerPlayerId: null));

        MycelialSurgeMutationProcessor.ProcessChemotacticBeacon(setup.board, setup.players, new Random(1), new TestSimulationObserver());

        AssertOwnedByPlayer(setup.board, setup.player.PlayerId, 2, 2);
        AssertOwnedByPlayer(setup.board, setup.player.PlayerId, 3, 2);
        AssertOwnedByPlayer(setup.board, setup.player.PlayerId, 4, 2);
        AssertOwnedByPlayer(setup.board, setup.player.PlayerId, 5, 2);
        Assert.Null(setup.board.GetTile(6, 2)?.FungalCell);
        Assert.Null(setup.board.GetTile(7, 2)?.FungalCell);
    }

    [Fact]
    public void ProcessChemotacticBeacon_assigns_earliest_valid_targets_without_spending_quota_on_skipped_tiles()
    {
        var setup = CreateBeaconBoard(level: 1, beaconTileId: 29);
        setup.board.PlaceFungalCell(new FungalCell(setup.player.PlayerId, setup.board.GetTile(3, 2)!.TileId, GrowthSource.HyphalSurge, lastOwnerPlayerId: null));
        setup.board.PlaceFungalCell(new FungalCell(setup.player.PlayerId, setup.board.GetTile(4, 2)!.TileId, GrowthSource.HyphalSurge, lastOwnerPlayerId: null));

        var friendlyDeadCell = new FungalCell(setup.player.PlayerId, setup.board.GetTile(5, 2)!.TileId, GrowthSource.HyphalSurge, lastOwnerPlayerId: null);
        setup.board.PlaceFungalCell(friendlyDeadCell);
        setup.board.KillFungalCell(friendlyDeadCell, DeathReason.Unknown);

        setup.board.PlaceFungalCell(new FungalCell(ownerPlayerId: 99, tileId: setup.board.GetTile(6, 2)!.TileId, source: GrowthSource.HyphalSurge, lastOwnerPlayerId: null));
        setup.board.PlaceFungalCell(new FungalCell(ownerPlayerId: 98, tileId: setup.board.GetTile(7, 2)!.TileId, source: GrowthSource.Manual, toxinExpirationAge: 3, lastOwnerPlayerId: null));

        MycelialSurgeMutationProcessor.ProcessChemotacticBeacon(setup.board, setup.players, new Random(1), new TestSimulationObserver());

        AssertOwnedByPlayer(setup.board, setup.player.PlayerId, 2, 2);
        AssertOwnedByPlayer(setup.board, setup.player.PlayerId, 3, 2);
        AssertOwnedByPlayer(setup.board, setup.player.PlayerId, 4, 2);
        Assert.True(setup.board.GetTile(5, 2)?.FungalCell?.IsDead);
        AssertOwnedByPlayer(setup.board, setup.player.PlayerId, 6, 2);
        AssertOwnedByPlayer(setup.board, setup.player.PlayerId, 7, 2);
        Assert.Null(setup.board.GetTile(8, 2)?.FungalCell);
    }

    [Fact]
    public void ApplyDirectedVectorLine_skips_friendly_dead_cells_without_spending_quota()
    {
        var setup = CreateBeaconBoard(level: 1, beaconTileId: 29);
        var observer = new TestSimulationObserver();

        setup.board.PlaceFungalCell(new FungalCell(setup.player.PlayerId, setup.board.GetTile(3, 2)!.TileId, GrowthSource.HyphalSurge, lastOwnerPlayerId: null));
        var friendlyDeadCell = Assert.IsType<FungalCell>(setup.board.GetTile(3, 2)?.FungalCell);
        setup.board.KillFungalCell(friendlyDeadCell, DeathReason.Unknown);

        var outcome = DirectedVectorHelper.ApplyDirectedVectorLine(
            setup.player,
            setup.board,
            new Random(1),
            startX: 1,
            startY: 2,
            targetX: 9,
            targetY: 2,
            totalTiles: 5,
            observer,
            GrowthSource.ChemotacticBeacon,
            DeathReason.HyphalVectoring,
            stopAtTargetTile: false);

        Assert.True(setup.board.GetTile(3, 2)?.FungalCell?.IsDead);
        AssertOwnedByPlayer(setup.board, setup.player.PlayerId, 2, 2);
        AssertOwnedByPlayer(setup.board, setup.player.PlayerId, 4, 2);
        AssertOwnedByPlayer(setup.board, setup.player.PlayerId, 5, 2);
        AssertOwnedByPlayer(setup.board, setup.player.PlayerId, 6, 2);
        AssertOwnedByPlayer(setup.board, setup.player.PlayerId, 7, 2);
        Assert.Equal(1, outcome.Invalid);
        Assert.Equal(5, outcome.PlacedCount);
    }

    [Fact]
    public void ApplyDirectedVectorLine_can_colonize_nutrient_patch_tiles()
    {
        var setup = CreateBeaconBoard(level: 1, beaconTileId: 29);
        var observer = new TestSimulationObserver();
        bool placedPatch = setup.board.PlaceNutrientPatch(tileId: setup.board.GetTile(4, 2)!.TileId, NutrientPatch.CreateAdaptogenCluster(clusterId: 1, clusterTileCount: 1));
        Assert.True(placedPatch);

        var outcome = DirectedVectorHelper.ApplyDirectedVectorLine(
            setup.player,
            setup.board,
            new Random(1),
            startX: 1,
            startY: 2,
            targetX: 9,
            targetY: 2,
            totalTiles: 4,
            observer,
            GrowthSource.ChemotacticBeacon,
            DeathReason.HyphalVectoring,
            stopAtTargetTile: false);

        AssertOwnedByPlayer(setup.board, setup.player.PlayerId, 4, 2);
        Assert.False(setup.board.GetTile(4, 2)?.HasNutrientPatch);
        Assert.Contains(setup.board.GetTile(4, 2)!.TileId, outcome.AffectedTileIds);
    }

    private static (GameBoard board, List<Player> players, Player player) CreateBeaconBoard(int level, int beaconTileId)
    {
        var board = new GameBoard(width: 10, height: 5, playerCount: 1);
        var player = new Player(playerId: 0, playerName: "Beacon Tester", playerType: PlayerTypeEnum.AI)
        {
            MutationPoints = 99
        };
        board.Players.Add(player);

        board.PlaceInitialSpore(player.PlayerId, x: 1, y: 2);
        player.SetMutationLevel(MutationIds.ChemotacticBeacon, newLevel: level, currentRound: 1);
        player.ActiveSurges[MutationIds.ChemotacticBeacon] = new Player.ActiveSurgeInfo(MutationIds.ChemotacticBeacon, level: level, duration: FungusToast.Core.Config.GameBalance.ChemotacticBeaconSurgeDuration);
        bool placed = board.TryPlaceChemobeacon(player.PlayerId, tileId: beaconTileId, mutationId: MutationIds.ChemotacticBeacon, turnsRemaining: FungusToast.Core.Config.GameBalance.ChemotacticBeaconSurgeDuration);
        Assert.True(placed);

        return (board, new List<Player> { player }, player);
    }

    private static void AssertOwnedByPlayer(GameBoard board, int playerId, int x, int y)
    {
        Assert.Equal(playerId, board.GetTile(x, y)?.FungalCell?.OwnerPlayerId);
    }
}