using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Growth;
using FungusToast.Core.Mutations;
using FungusToast.Core.Phases;
using FungusToast.Core.Players;

namespace FungusToast.Core.Tests.Mutations;

public class AeratedFrontierMutationTests
{
    [Fact]
    public void AeratedFrontier_is_the_tier1_substrate_ecology_root()
    {
        var mutation = RequireMutation();

        Assert.Equal(MutationCategory.SubstrateEcology, mutation.Category);
        Assert.Equal(MutationTier.Tier1, mutation.Tier);
        Assert.Equal(MutationType.AeratedFrontierGrowthChance, mutation.Type);
        Assert.Equal(GameBalance.AeratedFrontierMaxLevel, mutation.MaxLevel);
        Assert.Empty(mutation.Prerequisites);
        Assert.Contains(mutation.Id, MutationRegistry.Roots.Keys);
    }

    [Fact]
    public void AeratedFrontier_bonus_scales_when_two_orthogonal_spaces_are_open()
    {
        var (board, player, sourceTile) = CreateCenterSourceBoard(blockedTileIds: new[] { 0, 2, 5, 6, 7, 8 });
        player.SetMutationLevel(MutationIds.AeratedFrontier, newLevel: 4, currentRound: 1);
        sourceTile.FungalCell!.SetGrowthCycleAge(6);

        int openSpaces = SubstrateEcologyMutationProcessor.CountOpenOrthogonalSpaces(board, sourceTile);
        float bonus = SubstrateEcologyMutationProcessor.GetAeratedFrontierGrowthBonus(player, board, sourceTile);

        Assert.Equal(2, openSpaces);
        Assert.Equal(4 * GameBalance.AeratedFrontierEffectPerLevel, bonus, precision: 6);
    }

    [Fact]
    public void AeratedFrontier_does_not_count_cells_nutrients_or_blocked_tiles_as_open()
    {
        var board = new GameBoard(width: 3, height: 3, playerCount: 2, permanentlyBlockedTileIds: new[] { 5 });
        var player = AddPlayer(board, 0);
        var enemy = AddPlayer(board, 1);
        Assert.True(board.SpawnSporeForPlayer(player, tileId: 4, GrowthSource.InitialSpore));
        Assert.True(board.SpawnSporeForPlayer(enemy, tileId: 1, GrowthSource.InitialSpore));
        Assert.True(board.PlaceNutrientPatch(3, NutrientPatch.CreateAdaptogenCluster(clusterId: 1, clusterTileCount: 1)));

        int openSpaces = SubstrateEcologyMutationProcessor.CountOpenOrthogonalSpaces(board, board.GetTileById(4)!);

        Assert.Equal(1, openSpaces);
    }

    [Fact]
    public void AeratedFrontier_qualifies_at_a_board_corner_with_two_open_neighbors()
    {
        var board = new GameBoard(width: 3, height: 3, playerCount: 1);
        var player = AddPlayer(board, 0);
        Assert.True(board.SpawnSporeForPlayer(player, tileId: 0, GrowthSource.InitialSpore));
        player.SetMutationLevel(MutationIds.AeratedFrontier, newLevel: 1, currentRound: 1);

        var sourceTile = board.GetTileById(0)!;
        sourceTile.FungalCell!.SetGrowthCycleAge(6);

        Assert.Equal(2, SubstrateEcologyMutationProcessor.CountOpenOrthogonalSpaces(board, sourceTile));
        Assert.True(SubstrateEcologyMutationProcessor.QualifiesForAeratedFrontier(board, sourceTile));
    }

    [Theory]
    [InlineData(5, false)]
    [InlineData(6, true)]
    public void AeratedFrontier_requires_a_source_older_than_five_growth_cycles(int growthCycleAge, bool expectedQualification)
    {
        var (board, player, sourceTile) = CreateCenterSourceBoard(blockedTileIds: new[] { 0, 2, 5, 6, 7, 8 });
        player.SetMutationLevel(MutationIds.AeratedFrontier, newLevel: 1, currentRound: 1);
        sourceTile.FungalCell!.SetGrowthCycleAge(growthCycleAge);

        Assert.Equal(expectedQualification, SubstrateEcologyMutationProcessor.QualifiesForAeratedFrontier(board, sourceTile));
        Assert.Equal(
            expectedQualification ? GameBalance.AeratedFrontierEffectPerLevel : 0f,
            SubstrateEcologyMutationProcessor.GetAeratedFrontierGrowthBonus(player, board, sourceTile),
            precision: 6);
    }

    [Fact]
    public void Growth_cycle_records_growth_that_only_succeeds_due_to_aerated_frontier()
    {
        var (board, player, sourceTile) = CreateCenterSourceBoard(blockedTileIds: new[] { 0, 2, 5, 6, 7, 8 });
        player.SetMutationLevel(MutationIds.AeratedFrontier, newLevel: 1, currentRound: 1);
        sourceTile.FungalCell!.SetGrowthCycleAge(6);
        var observer = new AeratedFrontierObserver();

        GrowthEngine.ExecuteGrowthCycle(
            board,
            board.Players,
            new FixedRollRandom(GameBalance.BaseGrowthChance + 0.002f),
            new RoundContext(),
            observer);

        Assert.Equal(2, board.GetAllCellsOwnedBy(player.PlayerId).Count(cell => cell.IsAlive));
        Assert.Equal(1, observer.QualifiedAttempts);
        Assert.Equal(1, observer.BonusGrowths);
    }

    [Fact]
    public void Growth_cycle_does_not_apply_aerated_frontier_with_only_one_open_space()
    {
        var (board, player, _) = CreateCenterSourceBoard(blockedTileIds: new[] { 0, 2, 3, 5, 6, 7, 8 });
        player.SetMutationLevel(MutationIds.AeratedFrontier, newLevel: GameBalance.AeratedFrontierMaxLevel, currentRound: 1);
        var observer = new AeratedFrontierObserver();

        GrowthEngine.ExecuteGrowthCycle(
            board,
            board.Players,
            new FixedRollRandom(GameBalance.BaseGrowthChance + 0.002f),
            new RoundContext(),
            observer);

        Assert.Single(board.GetAllCellsOwnedBy(player.PlayerId).Where(cell => cell.IsAlive));
        Assert.Equal(0, observer.QualifiedAttempts);
        Assert.Equal(0, observer.BonusGrowths);
    }

    private static (GameBoard board, Player player, BoardTile sourceTile) CreateCenterSourceBoard(IEnumerable<int> blockedTileIds)
    {
        var board = new GameBoard(width: 3, height: 3, playerCount: 1, permanentlyBlockedTileIds: blockedTileIds);
        var player = AddPlayer(board, 0);
        Assert.True(board.SpawnSporeForPlayer(player, tileId: 4, GrowthSource.InitialSpore));
        return (board, player, board.GetTileById(4)!);
    }

    private static Player AddPlayer(GameBoard board, int playerId)
    {
        var player = new Player(playerId, $"Player {playerId}", PlayerTypeEnum.AI);
        board.Players.Add(player);
        return player;
    }

    private static Mutation RequireMutation()
    {
        return Assert.IsType<Mutation>(MutationRegistry.GetById(MutationIds.AeratedFrontier));
    }

    private sealed class FixedRollRandom : Random
    {
        private readonly double roll;

        public FixedRollRandom(double roll)
        {
            this.roll = roll;
        }

        public override double NextDouble() => roll;
        public override int Next(int minValue, int maxValue) => minValue;
    }

    private sealed class AeratedFrontierObserver : TestSimulationObserver
    {
        public int QualifiedAttempts { get; private set; }
        public int BonusGrowths { get; private set; }

        public override void RecordAeratedFrontierAttempt(int playerId) => QualifiedAttempts++;
        public override void RecordAeratedFrontierBonusGrowth(int playerId) => BonusGrowths++;
    }
}
