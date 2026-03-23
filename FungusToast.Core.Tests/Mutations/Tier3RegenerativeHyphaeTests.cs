using FungusToast.Core.Board;
using FungusToast.Core.Death;
using FungusToast.Core.Growth;
using FungusToast.Core.Mutations;
using FungusToast.Core.Phases;
using FungusToast.Core.Players;
using FungusToast.Core.Tests.Mutations;

namespace FungusToast.Core.Tests.Mutations;

public class Tier3RegenerativeHyphaeTests
{
    [Fact]
    public void RegenerativeHyphae_is_tier3_cellular_resilience_and_requires_chronoresilient_cytoplasm_level_five()
    {
        var mutation = RequireMutation(MutationIds.RegenerativeHyphae);

        Assert.Equal(MutationCategory.CellularResilience, mutation.Category);
        Assert.Equal(MutationTier.Tier3, mutation.Tier);
        Assert.Equal(MutationType.ReclaimDeadCells, mutation.Type);
        var prereq = Assert.Single(mutation.Prerequisites);
        Assert.Equal(MutationIds.ChronoresilientCytoplasm, prereq.MutationId);
        Assert.Equal(5, prereq.RequiredLevel);
    }

    [Fact]
    public void TryApplyRegenerativeHyphae_returns_false_when_player_has_no_level()
    {
        var board = CreateBoardWithPlayers(out var player, out _);
        var observer = new TestSimulationObserver();
        var deadCell = CreateDeadCell(ownerPlayerId: 1, tileId: 12);
        board.PlaceFungalCell(deadCell);

        var reclaimed = CellularResilienceMutationProcessor.TryApplyRegenerativeHyphae(player, deadCell, board, observer);

        Assert.False(reclaimed);
        Assert.Equal(1, board.GetCell(12)!.OwnerPlayerId);
        Assert.True(board.GetCell(12)!.IsDead);
    }

    [Fact]
    public void TryApplyRegenerativeHyphae_reclaims_dead_cells_for_the_player()
    {
        var board = CreateBoardWithPlayers(out var player, out _);
        var observer = new TestSimulationObserver();
        player.SetMutationLevel(MutationIds.RegenerativeHyphae, newLevel: 1, currentRound: 1);
        var deadCell = CreateDeadCell(ownerPlayerId: 1, tileId: 12);
        board.PlaceFungalCell(deadCell);

        var reclaimed = CellularResilienceMutationProcessor.TryApplyRegenerativeHyphae(player, deadCell, board, observer);

        Assert.True(reclaimed);
        var cell = Assert.IsType<FungalCell>(board.GetCell(12));
        Assert.True(cell.IsAlive);
        Assert.Equal(player.PlayerId, cell.OwnerPlayerId);
        Assert.Equal(GrowthSource.RegenerativeHyphae, cell.SourceOfGrowth);
    }

    [Fact]
    public void TryApplyRegenerativeHyphae_records_reclaim_event_and_updates_control()
    {
        var board = CreateBoardWithPlayers(out var player, out _);
        var observer = new TestSimulationObserver();
        player.SetMutationLevel(MutationIds.RegenerativeHyphae, newLevel: 1, currentRound: 1);
        var deadCell = CreateDeadCell(ownerPlayerId: 1, tileId: 12);
        board.PlaceFungalCell(deadCell);

        var reclaimed = CellularResilienceMutationProcessor.TryApplyRegenerativeHyphae(player, deadCell, board, observer);

        Assert.True(reclaimed);
        Assert.Contains(12, player.ControlledTileIds);
    }

    [Fact]
    public void TryApplyRegenerativeHyphae_does_not_reclaim_non_reclaimable_cells()
    {
        var board = CreateBoardWithPlayers(out var player, out _);
        var observer = new TestSimulationObserver();
        player.SetMutationLevel(MutationIds.RegenerativeHyphae, newLevel: 1, currentRound: 1);
        var toxinCell = new FungalCell(ownerPlayerId: 1, tileId: 12, source: GrowthSource.CytolyticBurst, toxinExpirationAge: 5, lastOwnerPlayerId: null);
        board.PlaceFungalCell(toxinCell);

        var reclaimed = CellularResilienceMutationProcessor.TryApplyRegenerativeHyphae(player, toxinCell, board, observer);

        Assert.False(reclaimed);
        Assert.True(board.GetCell(12)!.IsToxin);
        Assert.DoesNotContain(12, player.ControlledTileIds);
    }

    private static Mutation RequireMutation(int mutationId)
    {
        var mutation = MutationRegistry.GetById(mutationId);
        return Assert.IsType<Mutation>(mutation);
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
        var deadCell = new FungalCell(ownerPlayerId: ownerPlayerId, tileId: tileId, source: GrowthSource.InitialSpore, lastOwnerPlayerId: null);
        deadCell.Kill(DeathReason.Age);
        return deadCell;
    }
}
