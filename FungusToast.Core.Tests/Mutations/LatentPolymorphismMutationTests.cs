using FungusToast.Core.Config;
using FungusToast.Core.Mutations;
using FungusToast.Core.Phases;
using FungusToast.Core.Players;

namespace FungusToast.Core.Tests.Mutations;

public class LatentPolymorphismMutationTests
{
    [Fact]
    public void LatentPolymorphism_is_a_tier4_genetic_drift_mutation_with_the_approved_prerequisites()
    {
        var mutation = Assert.IsType<Mutation>(MutationRegistry.GetById(MutationIds.LatentPolymorphism));

        Assert.Equal(MutationCategory.GeneticDrift, mutation.Category);
        Assert.Equal(MutationTier.Tier4, mutation.Tier);
        Assert.Equal(MutationType.LatentPolymorphismBankedInterest, mutation.Type);
        Assert.Equal(GameBalance.LatentPolymorphismMaxLevel, mutation.MaxLevel);
        Assert.Contains(mutation.Prerequisites, prerequisite =>
            prerequisite.MutationId == MutationIds.MutatorPhenotype && prerequisite.RequiredLevel == 7);
        Assert.Contains(mutation.Prerequisites, prerequisite =>
            prerequisite.MutationId == MutationIds.AdaptiveExpression && prerequisite.RequiredLevel == 5);
        Assert.Contains(mutation.Prerequisites, prerequisite =>
            prerequisite.MutationId == MutationIds.AnabolicInversion && prerequisite.RequiredLevel == 3);
    }

    [Theory]
    [InlineData(0, 50, 0)]
    [InlineData(1, 9, 0)]
    [InlineData(1, 10, 1)]
    [InlineData(3, 10, 3)]
    [InlineData(5, 10, 5)]
    [InlineData(5, 50, 5)]
    public void LatentPolymorphism_interest_uses_pre_interest_banked_points_and_caps_per_round(
        int level,
        int pointsBanked,
        int expectedInterest)
    {
        Assert.Equal(
            expectedInterest,
            GeneticDriftMutationProcessor.CalculateLatentPolymorphismInterest(level, pointsBanked));
    }

    [Fact]
    public void LatentPolymorphism_adds_interest_once_for_the_bank_action_and_records_income()
    {
        var player = new Player(0, "Player", PlayerTypeEnum.AI)
        {
            MutationPoints = 12
        };
        player.SetMutationLevel(MutationIds.LatentPolymorphism, 3, currentRound: 1);
        var observer = new InterestObserver();

        int interest = GeneticDriftMutationProcessor.OnMutationPointsBanked_LatentPolymorphism(player, pointsBanked: 12, observer);

        Assert.Equal(3, interest);
        Assert.Equal(15, player.MutationPoints);
        Assert.Equal(3, observer.Interest);
        Assert.Equal(3, observer.Income);
    }

    private sealed class InterestObserver : TestSimulationObserver
    {
        public int Interest { get; private set; }
        public int Income { get; private set; }

        public override void RecordLatentPolymorphismInterest(int playerId, int bonusPoints) => Interest += bonusPoints;
        public override void RecordMutationPointIncome(int playerId, int newMutationPoints) => Income += newMutationPoints;
    }
}
