using FungusToast.Core.Mutations;
using FungusToast.Core.Players;

namespace FungusToast.Core.Tests.Mutations;

public class MutationCategoryInvestmentPrerequisiteTests
{
    [Fact]
    public void Ontogenic_regression_requires_hyperadaptive_two_and_ten_tier_one_levels_in_three_categories()
    {
        var mutation = RequireMutation(MutationIds.OntogenicRegression);

        var namedPrerequisite = Assert.Single(mutation.Prerequisites);
        Assert.Equal(MutationIds.HyperadaptiveDrift, namedPrerequisite.MutationId);
        Assert.Equal(2, namedPrerequisite.RequiredLevel);

        var categoryPrerequisite = Assert.Single(mutation.CategoryInvestmentPrerequisites);
        Assert.Equal(MutationTier.Tier1, categoryPrerequisite.Tier);
        Assert.Equal(10, categoryPrerequisite.RequiredLevelsPerCategory);
        Assert.Equal(3, categoryPrerequisite.RequiredCategoryCount);
    }

    [Fact]
    public void Thirty_total_levels_in_only_two_categories_does_not_satisfy_the_gate()
    {
        var player = CreatePlayer();
        var prerequisite = RequireCategoryPrerequisite();

        player.SetMutationLevel(MutationIds.MycelialBloom, 20);
        player.SetMutationLevel(MutationIds.HomeostaticHarmony, 10);

        Assert.False(prerequisite.IsMet(player, MutationRegistry.Roots));
    }

    [Fact]
    public void Ten_levels_in_each_of_three_categories_satisfies_the_gate()
    {
        var player = CreatePlayer();
        var prerequisite = RequireCategoryPrerequisite();

        player.SetMutationLevel(MutationIds.MycelialBloom, 10);
        player.SetMutationLevel(MutationIds.HomeostaticHarmony, 10);
        player.SetMutationLevel(MutationIds.MycotoxinTracer, 10);

        Assert.True(prerequisite.IsMet(player, MutationRegistry.Roots));
    }

    [Fact]
    public void Non_root_mutation_levels_do_not_count_toward_category_foundations()
    {
        var player = CreatePlayer();
        var prerequisite = RequireCategoryPrerequisite();

        player.SetMutationLevel(MutationIds.AdaptiveExpression, 5);
        player.SetMutationLevel(MutationIds.ChronoresilientCytoplasm, 10);
        player.SetMutationLevel(MutationIds.MycotoxinPotentiation, 10);

        Assert.Empty(prerequisite.GetCategoryInvestments(player, MutationRegistry.Roots)
            .Where(investment => investment.Value >= 10));
        Assert.False(prerequisite.IsMet(player, MutationRegistry.Roots));
    }

    [Fact]
    public void Final_qualifying_root_records_unlock_round_and_enforces_one_round_delay()
    {
        var player = CreatePlayer();
        var ontogenicRegression = RequireMutation(MutationIds.OntogenicRegression);
        player.MutationPoints = ontogenicRegression.PointsPerUpgrade;

        player.SetMutationLevel(MutationIds.HyperadaptiveDrift, 2, currentRound: 1);
        player.SetMutationLevel(MutationIds.MycelialBloom, 10, currentRound: 1);
        player.SetMutationLevel(MutationIds.HomeostaticHarmony, 10, currentRound: 1);
        player.SetMutationLevel(MutationIds.MycotoxinTracer, 9, currentRound: 1);

        Assert.Null(player.PlayerMutations.GetValueOrDefault(ontogenicRegression.Id)?.PrereqMetRound);

        player.SetMutationLevel(MutationIds.MycotoxinTracer, 10, currentRound: 3);

        var playerMutation = Assert.IsType<PlayerMutation>(player.PlayerMutations[ontogenicRegression.Id]);
        Assert.Equal(3, playerMutation.PrereqMetRound);
        Assert.False(player.CanUpgrade(ontogenicRegression, currentRound: 3));
        Assert.True(player.CanUpgrade(ontogenicRegression, currentRound: 4));
    }

    [Fact]
    public void Progress_snapshot_exposes_each_category_and_grouped_completion()
    {
        var player = CreatePlayer();
        var ontogenicRegression = RequireMutation(MutationIds.OntogenicRegression);
        player.SetMutationLevel(MutationIds.MycelialBloom, 10);
        player.SetMutationLevel(MutationIds.HomeostaticHarmony, 10);
        player.SetMutationLevel(MutationIds.MycotoxinTracer, 9);

        var snapshot = MutationProgressSnapshot.Create(ontogenicRegression, player);

        var progress = Assert.Single(snapshot.CategoryInvestmentRequirements);
        Assert.Equal(2, progress.SatisfiedCategoryCount);
        Assert.Equal(3, progress.RequiredCategoryCount);
        Assert.False(progress.IsMet);
        Assert.True(snapshot.HasUnmetPrerequisites);
        Assert.Contains(progress.Categories, category =>
            category.Category == MutationCategory.Fungicide
            && category.CurrentLevel == 9
            && category.RequiredLevel == 10
            && !category.IsMet);
    }

    private static MutationCategoryInvestmentPrerequisite RequireCategoryPrerequisite()
    {
        return Assert.Single(RequireMutation(MutationIds.OntogenicRegression).CategoryInvestmentPrerequisites);
    }

    private static Mutation RequireMutation(int mutationId)
    {
        return Assert.IsType<Mutation>(MutationRegistry.GetById(mutationId));
    }

    private static Player CreatePlayer()
    {
        return new Player(0, "Category Investment Test", PlayerTypeEnum.AI);
    }
}
