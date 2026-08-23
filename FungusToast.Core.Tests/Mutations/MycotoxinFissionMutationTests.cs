using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Death;
using FungusToast.Core.Growth;
using FungusToast.Core.Mutations;
using FungusToast.Core.Phases;
using FungusToast.Core.Players;

namespace FungusToast.Core.Tests.Mutations;

public class MycotoxinFissionMutationTests
{
    [Fact]
    public void MycotoxinFission_is_a_tier5_ecology_mutation_with_the_approved_toxin_build_prerequisites()
    {
        var mutation = Assert.IsType<Mutation>(MutationRegistry.GetById(MutationIds.MycotoxinFission));

        Assert.Equal(MutationCategory.SubstrateEcology, mutation.Category);
        Assert.Equal(MutationTier.Tier5, mutation.Tier);
        Assert.Equal(MutationType.MycotoxinFissionGrowthChance, mutation.Type);
        Assert.Equal(GameBalance.MycotoxinFissionMaxLevel, mutation.MaxLevel);
        Assert.Contains(mutation.Prerequisites, prerequisite => prerequisite.MutationId == MutationIds.ToxinMargin && prerequisite.RequiredLevel == 5);
        Assert.Contains(mutation.Prerequisites, prerequisite => prerequisite.MutationId == MutationIds.MycotoxinPotentiation && prerequisite.RequiredLevel == 5);
        Assert.Contains(mutation.Prerequisites, prerequisite => prerequisite.MutationId == MutationIds.PutrefactiveMycotoxin && prerequisite.RequiredLevel == 2);
    }

    [Fact]
    public void MycotoxinFission_grants_three_percent_per_level_only_next_to_a_friendly_toxin()
    {
        var (board, player, enemy, colonizedTile) = CreateBoard();
        player.SetMutationLevel(MutationIds.MycotoxinFission, 2, currentRound: 1);
        ToxinHelper.ConvertToToxin(board, tileId: 8, GrowthSource.MycotoxinTracer, player);

        Assert.True(SubstrateEcologyMutationProcessor.QualifiesForMycotoxinFission(player, board, colonizedTile));
        Assert.Equal(0.06f, SubstrateEcologyMutationProcessor.GetMycotoxinFissionGrowthBonus(player, board, colonizedTile));

        ToxinHelper.ConvertToToxin(board, tileId: 8, GrowthSource.MycotoxinTracer, enemy);

        Assert.False(SubstrateEcologyMutationProcessor.QualifiesForMycotoxinFission(player, board, colonizedTile));
        Assert.Equal(0f, SubstrateEcologyMutationProcessor.GetMycotoxinFissionGrowthBonus(player, board, colonizedTile));
    }

    [Fact]
    public void MycotoxinFission_bonus_can_make_an_otherwise_failed_growth_succeed_and_trigger_a_split()
    {
        var board = new GameBoard(width: 5, height: 5, playerCount: 2, permanentlyBlockedTileIds: new[] { 7, 11, 17 });
        var player = new Player(0, "Player", PlayerTypeEnum.AI);
        var enemy = new Player(1, "Enemy", PlayerTypeEnum.AI);
        board.Players.Add(player);
        board.Players.Add(enemy);
        Assert.True(board.SpawnSporeForPlayer(player, tileId: 12, GrowthSource.InitialSpore));
        Assert.True(board.SpawnSporeForPlayer(enemy, tileId: 18, GrowthSource.InitialSpore));
        player.SetMutationLevel(MutationIds.MycotoxinFission, 2, currentRound: 1);
        ToxinHelper.ConvertToToxin(board, tileId: 8, GrowthSource.MycotoxinTracer, player);
        var observer = new FissionObserver();

        GrowthEngine.ExecuteGrowthCycle(
            board,
            board.Players,
            new FixedRollRandom(GameBalance.BaseGrowthChance + 0.05f),
            new RoundContext(),
            observer);

        Assert.True(board.GetTileById(13)!.FungalCell!.IsAlive);
        Assert.Null(board.GetTileById(8)!.FungalCell);
        Assert.Equal(1, observer.Attempts);
        Assert.Equal(1, observer.BonusGrowths);
        Assert.Equal(2, observer.ToxinsCreated);
        Assert.Equal(0, observer.BridgeGrowths);
    }

    [Fact]
    public void MycotoxinFission_at_max_level_splits_three_toxins_preserves_remaining_lifespan_and_grows_into_the_vacated_tile()
    {
        var (board, player, _, colonizedTile) = CreateBoard();
        player.SetMutationLevel(MutationIds.MycotoxinFission, GameBalance.MycotoxinFissionMaxLevel, currentRound: 1);
        ToxinHelper.ConvertToToxin(board, tileId: 8, GrowthSource.MycotoxinTracer, player);
        board.GetTileById(8)!.FungalCell!.SetGrowthCycleAge(2);
        var observer = new FissionObserver();

        MycotoxinFissionResult result = SubstrateEcologyMutationProcessor.TryResolveMycotoxinFission(
            board,
            player,
            colonizedTile.TileId,
            new LowestIndexRandom(),
            observer);

        Assert.Equal(GameBalance.MycotoxinFissionMaxLevel, result.ToxinsCreated);
        Assert.True(result.BridgeGrown);
        Assert.True(board.GetTileById(8)!.FungalCell!.IsAlive);
        Assert.Equal(GrowthSource.MycotoxinFission, board.GetTileById(8)!.FungalCell!.SourceOfGrowth);
        Assert.Equal(GameBalance.MycotoxinFissionMaxLevel, board.AllToxinFungalCells().Count());
        Assert.All(board.AllToxinFungalCells(), toxin => Assert.Equal(GameBalance.DefaultToxinDuration - 2, toxin.ToxinExpirationAge));
        Assert.Equal(GameBalance.MycotoxinFissionMaxLevel, observer.ToxinsCreated);
        Assert.Equal(1, observer.BridgeGrowths);
    }

    private static (GameBoard board, Player player, Player enemy, BoardTile colonizedTile) CreateBoard()
    {
        var board = new GameBoard(width: 5, height: 5, playerCount: 2);
        var player = new Player(0, "Player", PlayerTypeEnum.AI);
        var enemy = new Player(1, "Enemy", PlayerTypeEnum.AI);
        board.Players.Add(player);
        board.Players.Add(enemy);
        Assert.True(board.SpawnSporeForPlayer(player, tileId: 13, GrowthSource.InitialSpore));
        Assert.True(board.SpawnSporeForPlayer(enemy, tileId: 18, GrowthSource.InitialSpore));
        return (board, player, enemy, board.GetTileById(13)!);
    }

    private sealed class LowestIndexRandom : Random
    {
        public override int Next(int minValue, int maxValue) => minValue;
    }

    private sealed class FixedRollRandom : Random
    {
        private readonly double roll;

        public FixedRollRandom(double roll) => this.roll = roll;

        public override double NextDouble() => roll;
        public override int Next(int minValue, int maxValue) => minValue;
    }

    private sealed class FissionObserver : TestSimulationObserver
    {
        public int ToxinsCreated { get; private set; }
        public int BridgeGrowths { get; private set; }
        public int Attempts { get; private set; }
        public int BonusGrowths { get; private set; }

        public override void RecordMycotoxinFissionAttempt(int playerId) => Attempts++;
        public override void RecordMycotoxinFissionBonusGrowth(int playerId) => BonusGrowths++;

        public override void RecordMycotoxinFission(int playerId, int toxinsCreated, bool bridgeGrown)
        {
            ToxinsCreated += toxinsCreated;
            if (bridgeGrown)
            {
                BridgeGrowths++;
            }
        }
    }
}
