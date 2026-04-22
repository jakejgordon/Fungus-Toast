using FungusToast.Core.Board;
using FungusToast.Core.Campaign;
using FungusToast.Core.Config;
using FungusToast.Core.Mutations;
using FungusToast.Core.Players;

namespace FungusToast.Core.Tests.Mutations;

public class PlayerTargetedSurgeTests
{
    [Fact]
    public void TryActivateTargetedSurge_returns_false_for_chemotactic_beacon_when_prerequisites_are_missing()
    {
        var player = CreatePlayer(mutationPoints: 99);
        var observer = new TestSimulationObserver();
        var board = new GameBoard(width: 5, height: 5, playerCount: 1);
        var mutation = RequireMutation(MutationIds.ChemotacticBeacon);

        var activated = player.TryActivateTargetedSurge(mutation, board, targetTileId: 12, observer, currentRound: 3);

        Assert.False(activated, $"Expected {mutation.Name} not to activate when prerequisites are missing.");
        Assert.False(player.IsSurgeActive(mutation.Id));
        Assert.False(board.HasChemobeacon(player.PlayerId));
    }

    [Fact]
    public void TryActivateTargetedSurge_places_chemobeacon_and_activates_surge_when_requirements_are_met()
    {
        var player = CreatePlayer(mutationPoints: 99);
        var observer = new TestSimulationObserver();
        var board = new GameBoard(width: 5, height: 5, playerCount: 1);
        var mutation = RequireMutation(MutationIds.ChemotacticBeacon);
        player.SetMutationLevel(MutationIds.MycelialBloom, newLevel: 7, currentRound: 1);
        int expectedCost = player.GetMutationPointCost(mutation);

        var activated = player.TryActivateTargetedSurge(mutation, board, targetTileId: 12, observer, currentRound: 2);

        Assert.True(activated, $"Expected {mutation.Name} to activate on an open tile when prerequisites are met.");
        Assert.True(player.IsSurgeActive(mutation.Id));
        Assert.Equal(1, player.GetMutationLevel(mutation.Id));
        Assert.Equal(99 - expectedCost, player.MutationPoints);

        var marker = Assert.IsType<GameBoard.ChemobeaconMarker>(board.GetChemobeacon(player.PlayerId));
        Assert.Equal(12, marker.TileId);
        Assert.Equal(mutation.Id, marker.MutationId);
        Assert.Equal(mutation.SurgeDuration, marker.TurnsRemaining);
    }

    [Fact]
    public void TryActivateTargetedSurge_with_hyphal_echo_extends_chemobeacon_duration()
    {
        var player = CreatePlayer(mutationPoints: 99);
        var observer = new TestSimulationObserver();
        var board = new GameBoard(width: 5, height: 5, playerCount: 1);
        var mutation = RequireMutation(MutationIds.ChemotacticBeacon);
        player.TryAddAdaptation(RequireAdaptation(AdaptationIds.HyphalEcho));
        player.SetMutationLevel(MutationIds.MycelialBloom, newLevel: 7, currentRound: 1);

        var activated = player.TryActivateTargetedSurge(mutation, board, targetTileId: 12, observer, currentRound: 2);

        Assert.True(activated);
        Assert.Equal(mutation.SurgeDuration + AdaptationGameBalance.HyphalEchoSurgeDurationBonus, player.GetSurgeTurnsRemaining(mutation.Id));

        var marker = Assert.IsType<GameBoard.ChemobeaconMarker>(board.GetChemobeacon(player.PlayerId));
        Assert.Equal(mutation.SurgeDuration + AdaptationGameBalance.HyphalEchoSurgeDurationBonus, marker.TurnsRemaining);
    }

    [Fact]
    public void TryActivateReservedTargetedSurge_uses_reserved_cost_and_spends_points_on_activation()
    {
        var player = CreatePlayer(mutationPoints: 50);
        var observer = new TestSimulationObserver();
        var board = new GameBoard(width: 5, height: 5, playerCount: 1);
        var mutation = RequireMutation(MutationIds.ChemotacticBeacon);
        player.SetMutationLevel(MutationIds.MycelialBloom, newLevel: 7, currentRound: 1);
        int reservedCost = GameBalance.ChemotacticBeaconPointsPerActivation;

        var activated = player.TryActivateReservedTargetedSurge(mutation, board, targetTileId: 6, observer, currentRound: 2, reservedActivationCost: reservedCost);

        Assert.True(activated, $"Expected reserved targeted surge activation to succeed on an open tile.");
        Assert.Equal(50 - reservedCost, player.MutationPoints);
        Assert.True(player.IsSurgeActive(mutation.Id));
        Assert.True(board.HasChemobeacon(player.PlayerId));
        Assert.Equal(reservedCost, observer.LastMutationPointsSpent);
    }

    [Fact]
    public void TryActivateReservedTargetedSurge_returns_false_without_spending_points_when_reserved_cost_cannot_be_paid()
    {
        var player = CreatePlayer(mutationPoints: 1);
        var observer = new TestSimulationObserver();
        var board = new GameBoard(width: 5, height: 5, playerCount: 1);
        var mutation = RequireMutation(MutationIds.ChemotacticBeacon);
        player.SetMutationLevel(MutationIds.MycelialBloom, newLevel: 7, currentRound: 1);
        int reservedCost = GameBalance.ChemotacticBeaconPointsPerActivation;

        var activated = player.TryActivateReservedTargetedSurge(mutation, board, targetTileId: 6, observer, currentRound: 2, reservedActivationCost: reservedCost);

        Assert.False(activated, $"Expected reserved targeted surge activation to fail when the reserved cost exceeds available mutation points.");
        Assert.Equal(1, player.MutationPoints);
        Assert.False(player.IsSurgeActive(mutation.Id));
        Assert.False(board.HasChemobeacon(player.PlayerId));
        Assert.Null(observer.LastMutationPointsSpent);
    }

    [Fact]
    public void TryActivateTargetedSurge_returns_false_when_target_tile_is_not_open_for_chemobeacon()
    {
        var player = CreatePlayer(mutationPoints: 99);
        var observer = new TestSimulationObserver();
        var board = new GameBoard(width: 5, height: 5, playerCount: 1);
        board.Players.Add(player);
        var mutation = RequireMutation(MutationIds.ChemotacticBeacon);
        player.SetMutationLevel(MutationIds.MycelialBloom, newLevel: 7, currentRound: 1);
        board.PlaceInitialSpore(playerId: 0, x: 2, y: 2);

        var activated = player.TryActivateTargetedSurge(mutation, board, targetTileId: 12, observer, currentRound: 2);

        Assert.False(activated, $"Expected {mutation.Name} not to activate on an occupied tile.");
        Assert.False(player.IsSurgeActive(mutation.Id));
        Assert.False(board.HasChemobeacon(player.PlayerId));
    }

    private static Player CreatePlayer(int mutationPoints)
    {
        return new Player(playerId: 0, playerName: "Test Player", playerType: PlayerTypeEnum.AI)
        {
            MutationPoints = mutationPoints
        };
    }

    private static Mutation RequireMutation(int mutationId)
    {
        var mutation = MutationRegistry.GetById(mutationId);
        return Assert.IsType<Mutation>(mutation);
    }

    private static AdaptationDefinition RequireAdaptation(string adaptationId)
    {
        var found = AdaptationRepository.TryGetById(adaptationId, out var adaptation);
        Assert.True(found, $"Expected adaptation {adaptationId} to exist in the adaptation repository.");
        return Assert.IsType<AdaptationDefinition>(adaptation);
    }
}
