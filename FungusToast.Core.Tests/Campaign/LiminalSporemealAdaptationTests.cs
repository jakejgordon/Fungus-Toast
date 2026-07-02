using FungusToast.Core.Board;
using FungusToast.Core.Campaign;
using FungusToast.Core.Phases;
using FungusToast.Core.Players;

namespace FungusToast.Core.Tests.Campaign;

public class LiminalSporemealAdaptationTests
{
    [Fact]
    public void OnStartingSporesEstablished_places_sporemeal_along_cached_playable_edge_on_irregular_board()
    {
        var board = new GameBoard(width: 5, height: 5, playerCount: 1, permanentlyBlockedTileIds: new[] { 1, 2, 3 });
        var player = new Player(0, "P0", PlayerTypeEnum.AI);
        board.Players.Add(player);
        player.TryAddAdaptation(RequireAdaptation(AdaptationIds.LiminalSporemeal));
        board.PlaceInitialSpore(player.PlayerId, x: 2, y: 1);

        AdaptationEffectProcessor.OnStartingSporesEstablished(board, board.Players, new Random(123));

        var patchTileIds = board.AllNutrientPatchTiles()
            .Select(tile => tile.TileId)
            .OrderBy(tileId => tileId)
            .ToArray();

        Assert.Equal(new[] { 5, 6, 8, 9 }, patchTileIds);
        Assert.All(patchTileIds, tileId => Assert.True(board.GetTileById(tileId)!.IsEdgeOfBoard));
    }

    private static AdaptationDefinition RequireAdaptation(string adaptationId)
    {
        var found = AdaptationRepository.TryGetById(adaptationId, out var adaptation);
        Assert.True(found, $"Expected adaptation {adaptationId} to exist in the adaptation repository.");
        return Assert.IsType<AdaptationDefinition>(adaptation);
    }
}
