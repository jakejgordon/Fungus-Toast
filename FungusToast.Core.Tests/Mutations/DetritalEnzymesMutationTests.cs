using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Death;
using FungusToast.Core.Growth;
using FungusToast.Core.Mutations;
using FungusToast.Core.Phases;
using FungusToast.Core.Players;

namespace FungusToast.Core.Tests.Mutations;

public class DetritalEnzymesMutationTests
{
    [Fact]
    public void DetritalEnzymes_is_a_tier3_substrate_ecology_mutation_with_a_crustward_prerequisite()
    {
        var mutation = RequireMutation();

        Assert.Equal(MutationCategory.SubstrateEcology, mutation.Category);
        Assert.Equal(MutationTier.Tier3, mutation.Tier);
        Assert.Equal(MutationType.DetritalEnzymesGrowthChance, mutation.Type);
        Assert.Equal(GameBalance.DetritalEnzymesMaxLevel, mutation.MaxLevel);
        Assert.Equal(GameBalance.MutationCosts.GetUpgradeCostByTier(MutationTier.Tier3), mutation.PointsPerUpgrade);
        Assert.Contains(mutation.Prerequisites, prerequisite =>
            prerequisite.MutationId == MutationIds.CrustwardTropism && prerequisite.RequiredLevel == 1);
    }

    [Fact]
    public void DetritalEnzymes_counts_only_orthogonally_adjacent_non_toxin_dead_cells()
    {
        var (board, _, targetTile) = CreateDeadMatterTargetBoard();
        AddDeadCell(board, playerId: 1, tileId: 9); // Diagonal to target tile 13.

        Assert.Equal(2, SubstrateEcologyMutationProcessor.CountNonToxinDeadOrthogonalNeighbors(board, targetTile));

        ToxinHelper.ConvertToToxin(board, tileId: 18, GrowthSource.MycotoxinTracer);

        Assert.Equal(1, SubstrateEcologyMutationProcessor.CountNonToxinDeadOrthogonalNeighbors(board, targetTile));
        Assert.True(SubstrateEcologyMutationProcessor.QualifiesForDetritalEnzymes(board, targetTile));
    }

    [Fact]
    public void DetritalEnzymes_scales_from_one_dead_neighbor_and_adds_its_dense_bonus_only_at_max_level()
    {
        var (board, player, targetTile) = CreateDeadMatterTargetBoard();
        player.SetMutationLevel(MutationIds.DetritalEnzymes, 3, currentRound: 1);

        Assert.Equal(
            3 * GameBalance.DetritalEnzymesEffectPerLevel,
            SubstrateEcologyMutationProcessor.GetDetritalEnzymesGrowthBonus(player, board, targetTile),
            precision: 6);
        Assert.Equal(0f, SubstrateEcologyMutationProcessor.GetDetritalEnzymesDenseDeadMatterBonus(player, board, targetTile));

        player.SetMutationLevel(MutationIds.DetritalEnzymes, GameBalance.DetritalEnzymesMaxLevel, currentRound: 1);

        Assert.Equal(
            GameBalance.DetritalEnzymesMaxLevel * GameBalance.DetritalEnzymesEffectPerLevel
                + GameBalance.DetritalEnzymesDenseDeadMatterBonus,
            SubstrateEcologyMutationProcessor.GetDetritalEnzymesGrowthBonus(player, board, targetTile),
            precision: 6);
        Assert.Equal(
            GameBalance.DetritalEnzymesDenseDeadMatterBonus,
            SubstrateEcologyMutationProcessor.GetDetritalEnzymesDenseDeadMatterBonus(player, board, targetTile),
            precision: 6);
    }

    [Fact]
    public void DetritalEnzymes_dense_dead_matter_bonus_can_make_an_otherwise_failed_growth_succeed()
    {
        var (board, player, _) = CreateDeadMatterTargetBoard();
        player.SetMutationLevel(MutationIds.DetritalEnzymes, GameBalance.DetritalEnzymesMaxLevel, currentRound: 1);
        var observer = new DetritalEnzymesObserver();

        GrowthEngine.ExecuteGrowthCycle(
            board,
            board.Players,
            new FixedRollRandom(GameBalance.BaseGrowthChance + 0.055f),
            new RoundContext(),
            observer);

        Assert.Equal(2, board.GetAllCellsOwnedBy(player.PlayerId).Count(cell => cell.IsAlive));
        Assert.Equal(1, observer.QualifiedAttempts);
        Assert.Equal(1, observer.BonusGrowths);
        Assert.Equal(1, observer.DenseDeadMatterAttempts);
        Assert.Equal(1, observer.DenseDeadMatterBonusGrowths);
    }

    private static (GameBoard board, Player player, BoardTile targetTile) CreateDeadMatterTargetBoard()
    {
        var board = new GameBoard(width: 5, height: 5, playerCount: 2, permanentlyBlockedTileIds: new[] { 7, 11, 17 });
        var player = AddPlayer(board, 0);
        AddPlayer(board, 1);
        Assert.True(board.SpawnSporeForPlayer(player, tileId: 12, GrowthSource.InitialSpore));
        AddDeadCell(board, playerId: 1, tileId: 8);
        AddDeadCell(board, playerId: 1, tileId: 18);
        return (board, player, board.GetTileById(13)!);
    }

    private static void AddDeadCell(GameBoard board, int playerId, int tileId)
    {
        var owner = board.Players.Single(player => player.PlayerId == playerId);
        Assert.True(board.SpawnSporeForPlayer(owner, tileId, GrowthSource.InitialSpore));
        board.KillFungalCell(board.GetTileById(tileId)!.FungalCell!, DeathReason.Randomness);
    }

    private static Player AddPlayer(GameBoard board, int playerId)
    {
        var player = new Player(playerId, $"Player {playerId}", PlayerTypeEnum.AI);
        board.Players.Add(player);
        return player;
    }

    private static Mutation RequireMutation()
    {
        return Assert.IsType<Mutation>(MutationRegistry.GetById(MutationIds.DetritalEnzymes));
    }

    private sealed class FixedRollRandom : Random
    {
        private readonly double roll;

        public FixedRollRandom(double roll) => this.roll = roll;

        public override double NextDouble() => roll;
        public override int Next(int minValue, int maxValue) => minValue;
    }

    private sealed class DetritalEnzymesObserver : TestSimulationObserver
    {
        public int QualifiedAttempts { get; private set; }
        public int BonusGrowths { get; private set; }
        public int DenseDeadMatterAttempts { get; private set; }
        public int DenseDeadMatterBonusGrowths { get; private set; }

        public override void RecordDetritalEnzymesAttempt(int playerId) => QualifiedAttempts++;
        public override void RecordDetritalEnzymesBonusGrowth(int playerId) => BonusGrowths++;
        public override void RecordDetritalEnzymesDenseDeadMatterAttempt(int playerId) => DenseDeadMatterAttempts++;
        public override void RecordDetritalEnzymesDenseDeadMatterBonusGrowth(int playerId) => DenseDeadMatterBonusGrowths++;
    }
}
