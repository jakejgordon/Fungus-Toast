using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Death;
using FungusToast.Core.Growth;
using FungusToast.Core.Mutations;
using FungusToast.Core.Phases;
using FungusToast.Core.Players;

namespace FungusToast.Core.Tests.Mutations;

public class ToxinMarginMutationTests
{
    [Fact]
    public void ToxinMargin_is_a_tier3_ecology_mutation_with_the_approved_two_root_prerequisites()
    {
        var mutation = Assert.IsType<Mutation>(MutationRegistry.GetById(MutationIds.ToxinMargin));

        Assert.Equal(MutationCategory.SubstrateEcology, mutation.Category);
        Assert.Equal(MutationTier.Tier3, mutation.Tier);
        Assert.Equal(MutationType.ToxinMarginGrowthChance, mutation.Type);
        Assert.Equal(GameBalance.ToxinMarginMaxLevel, mutation.MaxLevel);
        Assert.Contains(mutation.Prerequisites, prerequisite =>
            prerequisite.MutationId == MutationIds.AeratedFrontier && prerequisite.RequiredLevel == 5);
        Assert.Contains(mutation.Prerequisites, prerequisite =>
            prerequisite.MutationId == MutationIds.HomeostaticHarmony && prerequisite.RequiredLevel == 5);
    }

    [Fact]
    public void ToxinMargin_qualifies_only_for_empty_targets_next_to_enemy_owned_toxins()
    {
        var (board, player, enemy, target) = CreateToxinMarginBoard();
        ToxinHelper.ConvertToToxin(board, tileId: 8, GrowthSource.MycotoxinTracer, enemy);

        Assert.Equal(1, SubstrateEcologyMutationProcessor.CountEnemyToxinOrthogonalNeighbors(player, board, target));
        Assert.True(SubstrateEcologyMutationProcessor.QualifiesForToxinMargin(player, board, target));

        ToxinHelper.ConvertToToxin(board, tileId: 8, GrowthSource.MycotoxinTracer, player);

        Assert.Equal(0, SubstrateEcologyMutationProcessor.CountEnemyToxinOrthogonalNeighbors(player, board, target));
        Assert.False(SubstrateEcologyMutationProcessor.QualifiesForToxinMargin(player, board, target));
    }

    [Fact]
    public void ToxinMargin_bonus_can_make_an_otherwise_failed_growth_succeed_and_records_attribution()
    {
        var (board, player, enemy, _) = CreateToxinMarginBoard();
        player.SetMutationLevel(MutationIds.ToxinMargin, GameBalance.ToxinMarginMaxLevel, currentRound: 1);
        ToxinHelper.ConvertToToxin(board, tileId: 8, GrowthSource.MycotoxinTracer, enemy);
        var observer = new ToxinMarginObserver();

        GrowthEngine.ExecuteGrowthCycle(
            board,
            board.Players,
            new FixedRollRandom(GameBalance.BaseGrowthChance + 0.05f),
            new RoundContext(),
            observer);

        Assert.Equal(2, board.GetAllCellsOwnedBy(player.PlayerId).Count(cell => cell.IsAlive));
        Assert.Equal(1, observer.QualifiedAttempts);
        Assert.Equal(1, observer.BonusGrowths);
    }

    private static (GameBoard board, Player player, Player enemy, BoardTile target) CreateToxinMarginBoard()
    {
        var board = new GameBoard(width: 5, height: 5, playerCount: 2, permanentlyBlockedTileIds: new[] { 7, 11, 17 });
        var player = new Player(0, "Player 0", PlayerTypeEnum.AI);
        var enemy = new Player(1, "Player 1", PlayerTypeEnum.AI);
        board.Players.Add(player);
        board.Players.Add(enemy);
        Assert.True(board.SpawnSporeForPlayer(player, tileId: 12, GrowthSource.InitialSpore));
        return (board, player, enemy, board.GetTileById(13)!);
    }

    private sealed class FixedRollRandom : Random
    {
        private readonly double roll;

        public FixedRollRandom(double roll) => this.roll = roll;

        public override double NextDouble() => roll;
        public override int Next(int minValue, int maxValue) => minValue;
    }

    private sealed class ToxinMarginObserver : TestSimulationObserver
    {
        public int QualifiedAttempts { get; private set; }
        public int BonusGrowths { get; private set; }

        public override void RecordToxinMarginAttempt(int playerId) => QualifiedAttempts++;
        public override void RecordToxinMarginBonusGrowth(int playerId) => BonusGrowths++;
    }
}
