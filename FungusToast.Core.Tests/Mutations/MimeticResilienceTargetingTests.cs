using FungusToast.Core.Board;
using FungusToast.Core.Mutations;
using FungusToast.Core.Phases;
using FungusToast.Core.Players;
using System.Reflection;

namespace FungusToast.Core.Tests.Mutations;

public class MimeticResilienceTargetingTests
{
    [Fact]
    public void FindTargets_excludes_players_with_less_than_twenty_percent_more_living_cells()
    {
        var board = CreateBoardWithPlayers(out var actingPlayer, out var opponentA, out var opponentB);
        SeedLivingCells(board, actingPlayer, new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }); // 10
        SeedLivingCells(board, opponentA, new[] { 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30 }); // 11 (not enough)
        SeedLivingCells(board, opponentB, new[] { 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51 }); // 12 (exact threshold)

        var targets = InvokeFindTargets(actingPlayer, board.Players, board);

        Assert.DoesNotContain(targets, p => p.PlayerId == opponentA.PlayerId);
        Assert.Contains(targets, p => p.PlayerId == opponentB.PlayerId);
    }

    [Fact]
    public void FindTargets_excludes_players_below_minimum_board_control_threshold_even_if_they_have_more_living_cells()
    {
        var board = CreateBoardWithPlayers(out var actingPlayer, out var opponentA, out _);
        SeedLivingCells(board, actingPlayer, new[] { 0, 1, 2, 3, 4 });
        SeedLivingCells(board, opponentA, new[] { 20, 21, 22, 23, 24, 25 });

        // Use a tiny board-control footprint relative to a very large board.
        var giantBoard = new GameBoard(width: 100, height: 100, playerCount: 2);
        giantBoard.Players.Add(actingPlayer);
        giantBoard.Players.Add(opponentA);
        SeedLivingCells(giantBoard, actingPlayer, new[] { 0, 1, 2, 3, 4 });
        SeedLivingCells(giantBoard, opponentA, new[] { 20, 21, 22, 23, 24, 25 });

        var targets = InvokeFindTargets(actingPlayer, giantBoard.Players, giantBoard);

        Assert.DoesNotContain(targets, p => p.PlayerId == opponentA.PlayerId);
    }

    [Fact]
    public void OnPostGrowthPhase_mimetic_resilience_only_uses_eligible_players_as_source_targets()
    {
        var board = new GameBoard(width: 8, height: 8, playerCount: 3);
        var actingPlayer = new Player(0, "A", PlayerTypeEnum.AI);
        var ineligiblePlayer = new Player(1, "B", PlayerTypeEnum.AI);
        var eligiblePlayer = new Player(2, "C", PlayerTypeEnum.AI);
        board.Players.AddRange(new[] { actingPlayer, ineligiblePlayer, eligiblePlayer });

        // Acting player: 10 living cells and active surge.
        SeedLivingCells(board, actingPlayer, Enumerable.Range(0, 10));
        actingPlayer.SetMutationLevel(MutationIds.MimeticResilience, newLevel: 3, currentRound: 1);
        actingPlayer.ActiveSurges[MutationIds.MimeticResilience] = new Player.ActiveSurgeInfo(MutationIds.MimeticResilience, level: 3, duration: 2);

        // Ineligible player: 11 living cells, resistant source, but below the 20% threshold.
        SeedLivingCells(board, ineligiblePlayer, Enumerable.Range(16, 11));
        board.GetCell(16)!.MakeResistant();

        // Eligible player: 12 living cells, resistant source.
        SeedLivingCells(board, eligiblePlayer, Enumerable.Range(48, 12));
        board.GetCell(48)!.MakeResistant();

        // A contested enemy cell near the eligible player's resistant source.
        board.PlaceFungalCell(new FungalCell(ownerPlayerId: ineligiblePlayer.PlayerId, tileId: 41, source: FungusToast.Core.Growth.GrowthSource.InitialSpore, lastOwnerPlayerId: null));

        int actingBefore = board.GetAllCellsOwnedBy(actingPlayer.PlayerId).Count(c => c.IsAlive && c.IsResistant);
        int ineligibleBefore = board.GetAllCellsOwnedBy(ineligiblePlayer.PlayerId).Count(c => c.IsAlive && c.IsResistant);
        int eligibleBefore = board.GetAllCellsOwnedBy(eligiblePlayer.PlayerId).Count(c => c.IsAlive && c.IsResistant);

        var observer = new TestSimulationObserver();

        MycelialSurgeMutationProcessor.OnPostGrowthPhase_MimeticResilience(board, board.Players, new AlwaysZeroRandom(), observer);

        int actingAfter = board.GetAllCellsOwnedBy(actingPlayer.PlayerId).Count(c => c.IsAlive && c.IsResistant);
        int ineligibleAfter = board.GetAllCellsOwnedBy(ineligiblePlayer.PlayerId).Count(c => c.IsAlive && c.IsResistant);
        int eligibleAfter = board.GetAllCellsOwnedBy(eligiblePlayer.PlayerId).Count(c => c.IsAlive && c.IsResistant);

        Assert.True(actingAfter > actingBefore, "Expected the acting player to gain at least one resistant cell from an eligible Mimetic Resilience source.");
        Assert.Equal(ineligibleBefore, ineligibleAfter);
        Assert.True(eligibleAfter <= eligibleBefore, "Expected eligible source players not to gain additional resistant cells from the acting player's Mimetic Resilience effect.");
    }

    private static List<Player> InvokeFindTargets(Player actingPlayer, List<Player> players, GameBoard board)
    {
        var method = typeof(MycelialSurgeMutationProcessor).GetMethod("FindMimeticResilienceTargets_New", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(method);
        var result = method!.Invoke(null, new object[] { actingPlayer, players, board });
        return Assert.IsType<List<Player>>(result);
    }

    private static void SeedLivingCells(GameBoard board, Player player, IEnumerable<int> tileIds)
    {
        foreach (var tileId in tileIds)
        {
            var cell = new FungalCell(ownerPlayerId: player.PlayerId, tileId: tileId, source: FungusToast.Core.Growth.GrowthSource.InitialSpore, lastOwnerPlayerId: null);
            board.PlaceFungalCell(cell);
            player.AddControlledTile(tileId);
        }
    }

    private static GameBoard CreateBoardWithPlayers(out Player actingPlayer, out Player opponentA, out Player opponentB)
    {
        var board = new GameBoard(width: 8, height: 8, playerCount: 3);
        actingPlayer = new Player(0, "A", PlayerTypeEnum.AI);
        opponentA = new Player(1, "B", PlayerTypeEnum.AI);
        opponentB = new Player(2, "C", PlayerTypeEnum.AI);
        board.Players.AddRange(new[] { actingPlayer, opponentA, opponentB });
        return board;
    }

    private sealed class AlwaysZeroRandom : Random
    {
        protected override double Sample() => 0.0;
    }
}
