using FungusToast.Core.Board;
using FungusToast.Core.Campaign;
using FungusToast.Core.Config;
using FungusToast.Core.Growth;
using FungusToast.Core.Phases;
using FungusToast.Core.Players;
using FungusToast.Core.Tests.Mutations;

namespace FungusToast.Core.Tests.Campaign;

public class DistalSporeIrregularBoardTests
{
    [Fact]
    public void OnMutationPhaseStart_targets_cached_corner_tile_when_literal_corner_is_blocked()
    {
        var board = new GameBoard(
            width: 5,
            height: 5,
            playerCount: 1,
            permanentlyBlockedTileIds: new[] { 0, 4, 20, 24 });
        var player = new Player(0, "P0", PlayerTypeEnum.AI);
        board.Players.Add(player);
        board.PlaceInitialSpore(player.PlayerId, x: 1, y: 1);
        player.TryAddAdaptation(RequireAdaptation(AdaptationIds.DistalSpore));
        board.RestoreRoundState(AdaptationGameBalance.DistalSporeTriggerRound, currentGrowthCycle: 0, necrophyticBloomActivated: false, pendingHypervariationDraftPlayerIds: null);

        AdaptationEffectProcessor.OnMutationPhaseStart(board, board.Players, new Random(123), new TestSimulationObserver());

        Assert.Equal(19, board.GetCornerTileId(GameBoard.BoardCorner.BottomRight));
        Assert.NotNull(board.GetCell(19));
        Assert.Equal(player.PlayerId, board.GetCell(19)!.OwnerPlayerId);
        Assert.True(board.GetCell(19)!.IsResistant);
        Assert.Equal(GrowthSource.DistalSpore, board.GetCell(19)!.SourceOfGrowth);
        Assert.Null(board.GetCell(24));
    }

    private static AdaptationDefinition RequireAdaptation(string adaptationId)
    {
        var found = AdaptationRepository.TryGetById(adaptationId, out var adaptation);
        Assert.True(found, $"Expected adaptation {adaptationId} to exist in the adaptation repository.");
        return Assert.IsType<AdaptationDefinition>(adaptation);
    }
}
