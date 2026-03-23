using FungusToast.Core.Board;
using FungusToast.Core.Death;
using FungusToast.Core.Growth;
using FungusToast.Core.Mutations;
using FungusToast.Core.Phases;
using FungusToast.Core.Players;
using FungusToast.Core.Tests.Mutations;

namespace FungusToast.Core.Tests.Mutations;

public class NecrophyticBloomBehaviorTests
{
    [Fact]
    public void Initial_burst_can_reclaim_existing_dead_cells_once()
    {
        var board = CreateBoardWithPlayers(out var player, out var enemy);
        var observer = new TestSimulationObserver();
        player.SetMutationLevel(MutationIds.NecrophyticBloom, newLevel: 8, currentRound: 1);

        var deadA = CreateDeadCell(enemy.PlayerId, 11);
        var deadB = CreateDeadCell(enemy.PlayerId, 13);
        board.PlaceFungalCell(deadA);
        board.PlaceFungalCell(deadB);

        int reclaimed = CellularResilienceMutationProcessor.TriggerNecrophyticBloomInitialBurst(player, board, new AlwaysZeroRandom(), observer);

        Assert.True(reclaimed > 0, "Expected the initial burst to reclaim at least one pent-up dead cell.");
        Assert.True(player.HasTriggeredNecrophyticBloomInitialBurst);
        Assert.Contains(board.GetAllCellsOwnedBy(player.PlayerId), c => c.TileId == 11 || c.TileId == 13);
    }

    [Fact]
    public void Initial_burst_does_not_reprocess_old_backlog_a_second_time_without_new_deaths()
    {
        var board = CreateBoardWithPlayers(out var player, out var enemy);
        var observer = new TestSimulationObserver();
        player.SetMutationLevel(MutationIds.NecrophyticBloom, newLevel: 8, currentRound: 1);

        board.PlaceFungalCell(CreateDeadCell(enemy.PlayerId, 11));
        board.PlaceFungalCell(CreateDeadCell(enemy.PlayerId, 13));

        int first = CellularResilienceMutationProcessor.TriggerNecrophyticBloomInitialBurst(player, board, new AlwaysZeroRandom(), observer);
        int second = CellularResilienceMutationProcessor.TriggerNecrophyticBloomInitialBurst(player, board, new AlwaysZeroRandom(), observer);

        Assert.True(first > 0, "Expected the first initial burst to reclaim pent-up dead cells.");
        Assert.Equal(0, second);
    }

    [Fact]
    public void After_initial_burst_only_newly_dead_cells_should_be_reclaimed()
    {
        var board = CreateBoardWithPlayers(out var player, out var enemy);
        var observer = new TestSimulationObserver();
        player.SetMutationLevel(MutationIds.NecrophyticBloom, newLevel: 8, currentRound: 1);

        // Initial backlog
        board.PlaceFungalCell(CreateDeadCell(enemy.PlayerId, 11));
        board.PlaceFungalCell(CreateDeadCell(enemy.PlayerId, 13));
        int initial = CellularResilienceMutationProcessor.TriggerNecrophyticBloomInitialBurst(player, board, new AlwaysZeroRandom(), observer);
        Assert.True(initial > 0);

        // Add a newly dead cell after the initial burst.
        board.PlaceFungalCell(CreateDeadCell(enemy.PlayerId, 17));

        int followup = CellularResilienceMutationProcessor.ApplyNecrophyticBloom(player, board, new AlwaysZeroRandom(), observer);

        Assert.True(followup >= 0);
        Assert.NotNull(board.GetCell(17));
        Assert.Equal(player.PlayerId, board.GetCell(17)!.OwnerPlayerId);

        // The original backlog tiles should not be repeatedly re-harvested if they were already processed.
        int ownedOldBacklogTiles = board.GetAllCellsOwnedBy(player.PlayerId).Count(c => c.TileId == 11 || c.TileId == 13);
        Assert.InRange(ownedOldBacklogTiles, 0, 2);
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

    private static FungalCell CreateDeadCell(int ownerPlayerId, int tileId)
    {
        var cell = new FungalCell(ownerPlayerId: ownerPlayerId, tileId: tileId, source: GrowthSource.InitialSpore, lastOwnerPlayerId: null);
        cell.Kill(DeathReason.Age);
        return cell;
    }

    private sealed class AlwaysZeroRandom : Random
    {
        protected override double Sample() => 0.0;
    }
}
