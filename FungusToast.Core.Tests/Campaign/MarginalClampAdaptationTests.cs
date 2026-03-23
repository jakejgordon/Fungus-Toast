using FungusToast.Core.Board;
using FungusToast.Core.Campaign;
using FungusToast.Core.Death;
using FungusToast.Core.Events;
using FungusToast.Core.Growth;
using FungusToast.Core.Phases;
using FungusToast.Core.Players;
using FungusToast.Core.Tests.Mutations;

namespace FungusToast.Core.Tests.Campaign;

public class MarginalClampAdaptationTests
{
    [Fact]
    public void OnLivingCellEstablished_does_nothing_when_player_lacks_marginal_clamp()
    {
        var board = CreateBoardWithPlayers(out var player, out _);
        var observer = new TestSimulationObserver();
        SpecialBoardEventArgs? triggeredEvent = null;
        board.SpecialBoardEventTriggered += (_, e) => triggeredEvent = e;
        board.SpawnSporeForPlayer(player, tileId: 5, GrowthSource.HyphalOutgrowth);

        AdaptationEffectProcessor.OnLivingCellEstablished(player.PlayerId, 5, GrowthSource.HyphalOutgrowth, board, board.Players, observer);

        Assert.Null(triggeredEvent);
    }

    [Fact]
    public void OnLivingCellEstablished_kills_non_resistant_enemy_border_cells_and_removes_border_toxins()
    {
        var board = CreateBoardWithPlayers(out var player, out var enemy);
        var observer = new TestSimulationObserver();
        SpecialBoardEventArgs? triggeredEvent = null;
        board.SpecialBoardEventTriggered += (_, e) => triggeredEvent = e;
        player.TryAddAdaptation(RequireAdaptation(AdaptationIds.MarginalClamp));

        // New cell on tile 5 (x=0,y=1), with border threats on tiles 0 and 10.
        board.SpawnSporeForPlayer(player, tileId: 5, GrowthSource.HyphalOutgrowth);
        board.SpawnSporeForPlayer(enemy, tileId: 0, GrowthSource.HyphalOutgrowth);
        ToxinHelper.ConvertToToxin(board, tileId: 10, toxinLifespan: 5, growthSource: GrowthSource.CytolyticBurst, owner: enemy);

        AdaptationEffectProcessor.OnLivingCellEstablished(player.PlayerId, 5, GrowthSource.HyphalOutgrowth, board, board.Players, observer);

        Assert.True(board.GetCell(0)!.IsDead);
        Assert.Null(board.GetCell(10));
        Assert.NotNull(triggeredEvent);
        Assert.Equal(SpecialBoardEventKind.MarginalClampTriggered, triggeredEvent!.EventKind);
    }

    [Fact]
    public void OnLivingCellEstablished_does_not_kill_resistant_enemy_border_cells()
    {
        var board = CreateBoardWithPlayers(out var player, out var enemy);
        var observer = new TestSimulationObserver();
        SpecialBoardEventArgs? triggeredEvent = null;
        board.SpecialBoardEventTriggered += (_, e) => triggeredEvent = e;
        player.TryAddAdaptation(RequireAdaptation(AdaptationIds.MarginalClamp));

        board.SpawnSporeForPlayer(player, tileId: 5, GrowthSource.HyphalOutgrowth);
        board.SpawnSporeForPlayer(enemy, tileId: 0, GrowthSource.HyphalOutgrowth);
        board.GetCell(0)!.MakeResistant();

        AdaptationEffectProcessor.OnLivingCellEstablished(player.PlayerId, 5, GrowthSource.HyphalOutgrowth, board, board.Players, observer);

        Assert.True(board.GetCell(0)!.IsAlive);
        Assert.Equal(enemy.PlayerId, board.GetCell(0)!.OwnerPlayerId);
        Assert.Null(triggeredEvent);
    }

    [Fact]
    public void OnLivingCellEstablished_ignores_non_border_threats()
    {
        var board = CreateBoardWithPlayers(out var player, out var enemy);
        var observer = new TestSimulationObserver();
        SpecialBoardEventArgs? triggeredEvent = null;
        board.SpecialBoardEventTriggered += (_, e) => triggeredEvent = e;
        player.TryAddAdaptation(RequireAdaptation(AdaptationIds.MarginalClamp));

        // New cell at tile 6 (x=1,y=1); adjacent enemy at tile 7 is not on the border.
        board.SpawnSporeForPlayer(player, tileId: 6, GrowthSource.HyphalOutgrowth);
        board.SpawnSporeForPlayer(enemy, tileId: 7, GrowthSource.HyphalOutgrowth);

        AdaptationEffectProcessor.OnLivingCellEstablished(player.PlayerId, 6, GrowthSource.HyphalOutgrowth, board, board.Players, observer);

        Assert.True(board.GetCell(7)!.IsAlive);
        Assert.Equal(enemy.PlayerId, board.GetCell(7)!.OwnerPlayerId);
        Assert.Null(triggeredEvent);
    }

    private static GameBoard CreateBoardWithPlayers(out Player player, out Player enemy)
    {
        var board = new GameBoard(width: 5, height: 5, playerCount: 2);
        player = new Player(0, "P0", PlayerTypeEnum.AI);
        enemy = new Player(1, "P1", PlayerTypeEnum.AI);
        board.Players.Add(player);
        board.Players.Add(enemy);
        return board;
    }

    private static AdaptationDefinition RequireAdaptation(string adaptationId)
    {
        var found = AdaptationRepository.TryGetById(adaptationId, out var adaptation);
        Assert.True(found, $"Expected adaptation {adaptationId} to exist in the adaptation repository.");
        return Assert.IsType<AdaptationDefinition>(adaptation);
    }
}
