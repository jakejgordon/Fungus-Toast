using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Growth;
using FungusToast.Core.Mutations;
using FungusToast.Core.Phases;
using FungusToast.Core.Players;

namespace FungusToast.Core.Tests.Mutations;

public class CrustwardTropismMutationTests
{
    [Fact]
    public void CrustwardTropism_is_a_tier2_substrate_ecology_mutation_with_aerated_frontier_prerequisite()
    {
        var mutation = RequireMutation();

        Assert.Equal(MutationCategory.SubstrateEcology, mutation.Category);
        Assert.Equal(MutationTier.Tier2, mutation.Tier);
        Assert.Equal(MutationType.CrustwardTropismGrowthChance, mutation.Type);
        Assert.Equal(GameBalance.CrustwardTropismMaxLevel, mutation.MaxLevel);
        Assert.Equal(GameBalance.MutationCosts.GetUpgradeCostByTier(MutationTier.Tier2), mutation.PointsPerUpgrade);
        Assert.Contains(mutation.Prerequisites, prerequisite =>
            prerequisite.MutationId == MutationIds.AeratedFrontier && prerequisite.RequiredLevel == 10);
    }

    [Fact]
    public void CrustwardTropism_only_bonuses_targets_strictly_closer_to_the_playable_crust()
    {
        var board = new GameBoard(width: 5, height: 5, playerCount: 1);
        var player = AddPlayer(board, 0);
        Assert.True(board.SpawnSporeForPlayer(player, tileId: 6, GrowthSource.InitialSpore));
        player.SetMutationLevel(MutationIds.CrustwardTropism, newLevel: 3, currentRound: 1);

        BoardTile sourceTile = board.GetTileById(6)!;
        BoardTile crustTarget = board.GetTileById(5)!;
        BoardTile sameDistanceTarget = board.GetTileById(11)!;

        Assert.Equal(2, board.GetPlayableEdgeDistance(sourceTile.TileId));
        Assert.Equal(1, board.GetPlayableEdgeDistance(crustTarget.TileId));
        Assert.True(SubstrateEcologyMutationProcessor.QualifiesForCrustwardTropism(board, sourceTile, crustTarget));
        Assert.False(SubstrateEcologyMutationProcessor.QualifiesForCrustwardTropism(board, sourceTile, sameDistanceTarget));
        Assert.Equal(
            3 * GameBalance.CrustwardTropismEffectPerLevel,
            SubstrateEcologyMutationProcessor.GetCrustwardTropismGrowthBonus(player, board, sourceTile, crustTarget),
            precision: 6);
    }

    [Fact]
    public void CrustwardTropism_bonus_can_make_an_otherwise_failed_crustward_attempt_succeed()
    {
        var (board, player, _, _) = CreateSingleCrustApproachBoard();
        player.SetMutationLevel(MutationIds.CrustwardTropism, newLevel: 3, currentRound: 1);
        var observer = new CrustwardTropismObserver();

        GrowthEngine.ExecuteGrowthCycle(
            board,
            board.Players,
            new FixedRollRandom(GameBalance.BaseGrowthChance + 0.02f),
            new RoundContext(),
            observer);

        Assert.Equal(2, board.GetAllCellsOwnedBy(player.PlayerId).Count(cell => cell.IsAlive));
        Assert.Equal(1, observer.QualifiedAttempts);
        Assert.Equal(1, observer.BonusGrowths);
        Assert.Equal(0, observer.AutomaticGrowths);
    }

    [Fact]
    public void Max_level_guarantees_only_one_crust_arrival_per_growth_cycle_and_resets_next_cycle()
    {
        var (board, player) = CreateTwoCrustApproachBoard();
        player.SetMutationLevel(MutationIds.CrustwardTropism, GameBalance.CrustwardTropismMaxLevel, currentRound: 1);
        var observer = new CrustwardTropismObserver();
        var failingRolls = new FixedRollRandom(0.99d);

        GrowthEngine.ExecuteGrowthCycle(board, board.Players, failingRolls, new RoundContext(), observer);

        Assert.Equal(3, board.GetAllCellsOwnedBy(player.PlayerId).Count(cell => cell.IsAlive));
        Assert.Equal(1, observer.AutomaticGrowths);

        GrowthEngine.ExecuteGrowthCycle(board, board.Players, failingRolls, new RoundContext(), observer);

        Assert.Equal(4, board.GetAllCellsOwnedBy(player.PlayerId).Count(cell => cell.IsAlive));
        Assert.Equal(2, observer.AutomaticGrowths);
    }

    [Fact]
    public void Max_level_automatic_crust_arrival_includes_an_enabled_diagonal_tendril()
    {
        var board = new GameBoard(width: 5, height: 5, playerCount: 1);
        var player = AddPlayer(board, 0);
        var blocker = AddPlayer(board, 1);
        Assert.True(board.SpawnSporeForPlayer(player, tileId: 6, GrowthSource.InitialSpore));
        foreach (int tileId in new[] { 1, 5, 7, 11 })
            Assert.True(board.SpawnSporeForPlayer(blocker, tileId, GrowthSource.InitialSpore));
        player.SetMutationLevel(MutationIds.CrustwardTropism, GameBalance.CrustwardTropismMaxLevel, currentRound: 1);
        player.SetMutationLevel(MutationIds.TendrilNorthwest, 1, currentRound: 1);
        var observer = new CrustwardTropismObserver();

        GrowthEngine.ExecuteGrowthCycle(
            board,
            board.Players,
            new FixedRollRandom(0.99d),
            new RoundContext(),
            observer);

        Assert.True(board.GetCell(10)?.IsAlive == true);
        Assert.Equal(1, observer.AutomaticGrowths);
        Assert.Equal(1, observer.DiagonalGrowths);
    }

    private static (GameBoard board, Player player, BoardTile sourceTile, BoardTile crustTarget) CreateSingleCrustApproachBoard()
    {
        var board = new GameBoard(width: 5, height: 5, playerCount: 1);
        var player = AddPlayer(board, 0);
        Assert.True(board.SpawnSporeForPlayer(player, tileId: 6, GrowthSource.InitialSpore));
        return (board, player, board.GetTileById(6)!, board.GetTileById(5)!);
    }

    private static (GameBoard board, Player player) CreateTwoCrustApproachBoard()
    {
        var board = new GameBoard(width: 5, height: 5, playerCount: 1);
        var player = AddPlayer(board, 0);
        Assert.True(board.SpawnSporeForPlayer(player, tileId: 6, GrowthSource.InitialSpore));
        Assert.True(board.SpawnSporeForPlayer(player, tileId: 16, GrowthSource.InitialSpore));
        return (board, player);
    }

    private static Player AddPlayer(GameBoard board, int playerId)
    {
        var player = new Player(playerId, $"Player {playerId}", PlayerTypeEnum.AI);
        board.Players.Add(player);
        return player;
    }

    private static Mutation RequireMutation()
    {
        return Assert.IsType<Mutation>(MutationRegistry.GetById(MutationIds.CrustwardTropism));
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

    private sealed class CrustwardTropismObserver : TestSimulationObserver
    {
        public int QualifiedAttempts { get; private set; }
        public int BonusGrowths { get; private set; }
        public int AutomaticGrowths { get; private set; }
        public int DiagonalGrowths { get; private set; }

        public override void RecordCrustwardTropismAttempt(int playerId) => QualifiedAttempts++;
        public override void RecordCrustwardTropismBonusGrowth(int playerId) => BonusGrowths++;
        public override void RecordCrustwardTropismAutomaticGrowth(int playerId) => AutomaticGrowths++;
        public override void RecordTendrilGrowth(int playerId, DiagonalDirection value) => DiagonalGrowths++;
    }
}
