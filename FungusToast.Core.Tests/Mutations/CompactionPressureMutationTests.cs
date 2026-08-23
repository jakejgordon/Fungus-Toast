using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Growth;
using FungusToast.Core.Mutations;
using FungusToast.Core.Phases;
using FungusToast.Core.Players;

namespace FungusToast.Core.Tests.Mutations;

public class CompactionPressureMutationTests
{
    [Fact]
    public void CompactionPressure_is_tier2_and_requires_aerated_frontier_level_10()
    {
        var mutation = Assert.IsType<Mutation>(MutationRegistry.GetById(MutationIds.CompactionPressure));
        Assert.Equal(MutationTier.Tier2, mutation.Tier);
        Assert.Equal(MutationCategory.SubstrateEcology, mutation.Category);
        Assert.Contains(mutation.Prerequisites, prerequisite =>
            prerequisite.MutationId == MutationIds.AeratedFrontier && prerequisite.RequiredLevel == 10);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(3, false)]
    [InlineData(0, false)]
    public void CompactionPressure_only_qualifies_sources_with_one_or_two_legal_orthogonal_targets(int legalTargets, bool expected)
    {
        var board = new GameBoard(3, 3, 2);
        var player = new Player(0, "P0", PlayerTypeEnum.AI);
        var blocker = new Player(1, "P1", PlayerTypeEnum.AI);
        board.Players.Add(player);
        board.Players.Add(blocker);
        Assert.True(board.SpawnSporeForPlayer(player, 4, GrowthSource.InitialSpore));
        foreach (int tileId in new[] { 1, 3, 5, 7 }.Skip(legalTargets))
            Assert.True(board.SpawnSporeForPlayer(blocker, tileId, GrowthSource.InitialSpore));

        player.SetMutationLevel(MutationIds.CompactionPressure, 1, currentRound: 1);
        var source = board.GetTileById(4)!;

        Assert.Equal(legalTargets, SubstrateEcologyMutationProcessor.CountLegalOrthogonalGrowthTargets(board, source));
        Assert.Equal(expected, SubstrateEcologyMutationProcessor.QualifiesForCompactionPressure(board, source));
    }

    [Fact]
    public void DetritalEnzymes_unlocks_from_either_tier2_ecology_branch()
    {
        var detrital = Assert.IsType<Mutation>(MutationRegistry.GetById(MutationIds.DetritalEnzymes));
        Assert.Single(detrital.AnyPrerequisiteGroups);
        Assert.False(MutationPrerequisiteEvaluator.AreAllMet(detrital, new Player(0, "P0", PlayerTypeEnum.AI)));

        var compactionPlayer = new Player(0, "P0", PlayerTypeEnum.AI);
        compactionPlayer.SetMutationLevel(MutationIds.CompactionPressure, 1, currentRound: 1);
        Assert.True(MutationPrerequisiteEvaluator.AreAllMet(detrital, compactionPlayer));

        var crustwardPlayer = new Player(1, "P1", PlayerTypeEnum.AI);
        crustwardPlayer.SetMutationLevel(MutationIds.CrustwardTropism, 1, currentRound: 1);
        Assert.True(MutationPrerequisiteEvaluator.AreAllMet(detrital, crustwardPlayer));
    }
}
