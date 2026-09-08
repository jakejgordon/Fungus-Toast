using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Growth;
using FungusToast.Core.Mutations;
using FungusToast.Core.Phases;
using FungusToast.Core.Players;

namespace FungusToast.Core.Tests.Mutations;

public class NecroticClearanceMutationTests
{
    [Fact]
    public void NecroticClearance_is_tier2_surge_and_requires_homeostatic_harmony_five()
    {
        var mutation = Assert.IsType<Mutation>(MutationRegistry.GetById(MutationIds.NecroticClearance));

        Assert.Equal(MutationCategory.MycelialSurges, mutation.Category);
        Assert.Equal(MutationTier.Tier2, mutation.Tier);
        Assert.Equal(MutationType.NecroticClearance, mutation.Type);
        Assert.True(mutation.IsSurge);
        Assert.Equal(GameBalance.NecroticClearanceMaxLevel, mutation.MaxLevel);
        var prerequisite = Assert.Single(mutation.Prerequisites);
        Assert.Equal(MutationIds.HomeostaticHarmony, prerequisite.MutationId);
        Assert.Equal(5, prerequisite.RequiredLevel);
    }

    [Fact]
    public void OnPreGrowthPhase_clears_contested_adjacent_own_corpse_without_firing_another_death()
    {
        var board = new GameBoard(width: 3, height: 3, playerCount: 2);
        var player = new Player(0, "Owner", PlayerTypeEnum.AI);
        var enemy = new Player(1, "Enemy", PlayerTypeEnum.AI);
        board.Players.AddRange(new[] { player, enemy });
        player.SetMutationLevel(MutationIds.NecroticClearance, 3, currentRound: 1);
        player.ActiveSurges[MutationIds.NecroticClearance] = new Player.ActiveSurgeInfo(MutationIds.NecroticClearance, 3, 3);

        PlaceLiving(board, player, 0);
        var corpse = PlaceLiving(board, player, 1);
        PlaceLiving(board, enemy, 2);
        board.KillFungalCell(corpse, FungusToast.Core.Death.DeathReason.Randomness);
        int deathEvents = 0;
        board.CellDeath += (_, _) => deathEvents++;

        MycelialSurgeMutationProcessor.OnPreGrowthPhase_NecroticClearance(
            board, board.Players, new FixedRandom(0.0), new TestSimulationObserver());

        Assert.Null(board.GetTileById(1)!.FungalCell);
        Assert.Equal(0, deathEvents);
        Assert.DoesNotContain(1, player.ControlledTileIds);
    }

    [Fact]
    public void OnPreGrowthPhase_does_not_clear_without_active_surge()
    {
        var board = new GameBoard(width: 2, height: 2, playerCount: 1);
        var player = new Player(0, "Owner", PlayerTypeEnum.AI);
        board.Players.Add(player);
        player.SetMutationLevel(MutationIds.NecroticClearance, 3, currentRound: 1);
        PlaceLiving(board, player, 0);
        var corpse = PlaceLiving(board, player, 1);
        board.KillFungalCell(corpse, FungusToast.Core.Death.DeathReason.Randomness);

        MycelialSurgeMutationProcessor.OnPreGrowthPhase_NecroticClearance(
            board, board.Players, new FixedRandom(0.0), new TestSimulationObserver());

        Assert.NotNull(board.GetTileById(1)!.FungalCell);
    }

    private static FungalCell PlaceLiving(GameBoard board, Player player, int tileId)
    {
        var cell = new FungalCell(player.PlayerId, tileId, GrowthSource.InitialSpore, lastOwnerPlayerId: null);
        board.PlaceFungalCell(cell);
        player.AddControlledTile(tileId);
        return cell;
    }

    private sealed class FixedRandom : Random
    {
        private readonly double value;
        public FixedRandom(double value) => this.value = value;
        public override double NextDouble() => value;
    }
}
