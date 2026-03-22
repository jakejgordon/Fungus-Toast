using FungusToast.Core.Board;
using FungusToast.Core.Players;

namespace FungusToast.Core.Tests.Board;

public class GameBoardPhaseHelperTests
{
    [Fact]
    public void UpdateCachedOccupiedTileRatio_caches_current_ratio()
    {
        var board = new GameBoard(width: 5, height: 5, playerCount: 1);
        var player = new Player(playerId: 0, playerName: "Test Player", playerType: PlayerTypeEnum.AI);
        board.Players.Add(player);
        board.PlaceInitialSpore(playerId: 0, x: 1, y: 1);
        board.SpawnSporeForPlayer(player, tileId: 7, FungusToast.Core.Growth.GrowthSource.HyphalOutgrowth);

        board.UpdateCachedOccupiedTileRatio();

        Assert.Equal(2f / 25f, board.CachedOccupiedTileRatio, precision: 6);
    }

    [Fact]
    public void UpdateCachedDecayPhaseContext_creates_context_once_until_cleared()
    {
        var board = new GameBoard(width: 5, height: 5, playerCount: 0);

        board.UpdateCachedDecayPhaseContext();
        var first = board.CachedDecayPhaseContext;
        board.UpdateCachedDecayPhaseContext();
        var second = board.CachedDecayPhaseContext;

        Assert.NotNull(first);
        Assert.Same(first, second);

        board.ClearCachedDecayPhaseContext();
        Assert.Null(board.CachedDecayPhaseContext);

        board.UpdateCachedDecayPhaseContext();
        Assert.NotSame(first, board.CachedDecayPhaseContext);
    }

    [Fact]
    public void CountReclaimedCellsByPlayer_counts_only_alive_cells_reclaimed_by_the_same_player()
    {
        var board = new GameBoard(width: 5, height: 5, playerCount: 0);
        var deadSameOwner = new FungalCell(ownerPlayerId: 0, tileId: 6, source: FungusToast.Core.Growth.GrowthSource.InitialSpore, lastOwnerPlayerId: null);
        deadSameOwner.Kill(FungusToast.Core.Death.DeathReason.Age);
        deadSameOwner.Reclaim(newOwnerPlayerId: 0, source: FungusToast.Core.Growth.GrowthSource.RegenerativeHyphae);

        var normalLiving = new FungalCell(ownerPlayerId: 0, tileId: 7, source: FungusToast.Core.Growth.GrowthSource.InitialSpore, lastOwnerPlayerId: null);

        var reclaimedFromEnemy = new FungalCell(ownerPlayerId: 1, tileId: 8, source: FungusToast.Core.Growth.GrowthSource.InitialSpore, lastOwnerPlayerId: null);
        reclaimedFromEnemy.Kill(FungusToast.Core.Death.DeathReason.Age);
        reclaimedFromEnemy.Reclaim(newOwnerPlayerId: 0, source: FungusToast.Core.Growth.GrowthSource.RegenerativeHyphae);

        board.PlaceFungalCell(deadSameOwner);
        board.PlaceFungalCell(normalLiving);
        board.PlaceFungalCell(reclaimedFromEnemy);

        var count = board.CountReclaimedCellsByPlayer(0);

        Assert.Equal(1, count);
    }
}
