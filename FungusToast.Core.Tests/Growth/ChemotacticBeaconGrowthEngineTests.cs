using FungusToast.Core.Board;
using FungusToast.Core.Growth;
using FungusToast.Core.Mutations;
using FungusToast.Core.Phases;
using FungusToast.Core.Players;
using FungusToast.Core.Tests.Mutations;

namespace FungusToast.Core.Tests.Growth;

public class ChemotacticBeaconGrowthEngineTests
{
    [Fact]
    public void ExecuteGrowthCycle_with_active_beacon_biases_growth_toward_marker_over_many_seeds()
    {
        const int iterations = 1000;
        int towardWithBeacon = 0;
        int awayWithBeacon = 0;
        int towardWithoutBeacon = 0;
        int awayWithoutBeacon = 0;

        for (int seed = 0; seed < iterations; seed++)
        {
            var withBeaconBoard = CreateGrowthTestBoard(withBeacon: true);
            GrowthEngine.ExecuteGrowthCycle(withBeaconBoard.board, withBeaconBoard.players, new Random(seed), new RoundContext(), new TestSimulationObserver());
            CountOutcomes(withBeaconBoard.board, withBeaconBoard.player.PlayerId, ref towardWithBeacon, ref awayWithBeacon);

            var withoutBeaconBoard = CreateGrowthTestBoard(withBeacon: false);
            GrowthEngine.ExecuteGrowthCycle(withoutBeaconBoard.board, withoutBeaconBoard.players, new Random(seed), new RoundContext(), new TestSimulationObserver());
            CountOutcomes(withoutBeaconBoard.board, withoutBeaconBoard.player.PlayerId, ref towardWithoutBeacon, ref awayWithoutBeacon);
        }

        Assert.True(towardWithBeacon > towardWithoutBeacon,
            $"Expected Beacon to increase toward growth. With Beacon: {towardWithBeacon}, without Beacon: {towardWithoutBeacon}.");
        Assert.True(awayWithBeacon < awayWithoutBeacon,
            $"Expected Beacon to reduce away growth. With Beacon: {awayWithBeacon}, without Beacon: {awayWithoutBeacon}.");
    }

    private static (GameBoard board, List<Player> players, Player player) CreateGrowthTestBoard(bool withBeacon)
    {
        var board = new GameBoard(width: 5, height: 5, playerCount: 1);
        var player = new Player(playerId: 0, playerName: "Beacon Tester", playerType: PlayerTypeEnum.AI)
        {
            MutationPoints = 99
        };
        board.Players.Add(player);

        board.PlaceInitialSpore(player.PlayerId, x: 2, y: 2);
        board.PlaceNutrientPatch(tileId: 7, NutrientPatch.CreateAdaptogenCluster(clusterId: 1, clusterTileCount: 1));
        board.PlaceNutrientPatch(tileId: 17, NutrientPatch.CreateAdaptogenCluster(clusterId: 2, clusterTileCount: 1));

        player.SetMutationLevel(MutationIds.MycelialBloom, newLevel: 100, currentRound: 1);

        if (withBeacon)
        {
            player.SetMutationLevel(MutationIds.MycelialBloom, newLevel: 100, currentRound: 1);
            player.SetMutationLevel(MutationIds.ChemotacticBeacon, newLevel: 1, currentRound: 1);
            player.ActiveSurges[MutationIds.ChemotacticBeacon] = new Player.ActiveSurgeInfo(MutationIds.ChemotacticBeacon, level: 1, duration: 5);
            bool placed = board.TryPlaceChemobeacon(player.PlayerId, tileId: 14, mutationId: MutationIds.ChemotacticBeacon, turnsRemaining: 5);
            Assert.True(placed);
        }

        return (board, new List<Player> { player }, player);
    }

    private static void CountOutcomes(GameBoard board, int playerId, ref int towardCount, ref int awayCount)
    {
        var towardTile = Assert.IsType<BoardTile>(board.GetTile(3, 2));
        var awayTile = Assert.IsType<BoardTile>(board.GetTile(1, 2));

        if (towardTile.FungalCell?.OwnerPlayerId == playerId)
        {
            towardCount++;
        }

        if (awayTile.FungalCell?.OwnerPlayerId == playerId)
        {
            awayCount++;
        }
    }
}