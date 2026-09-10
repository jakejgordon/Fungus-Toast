using FungusToast.Core.AI;
using FungusToast.Core.Mutations;
using FungusToast.Simulation.Calibration;
using Xunit;

namespace FungusToast.Simulation.Tests.Calibration;

public sealed class RosterBehaviorComparisonTests
{
    [Fact]
    public void Compare_UsesRawMutationsToDistinguishSameCategoryBuilds()
    {
        var first = Profile("first", new Dictionary<int, int> { [1] = 3 }, new Dictionary<MutationCategory, int> { [MutationCategory.Growth] = 3 });
        var second = Profile("second", new Dictionary<int, int> { [2] = 3 }, new Dictionary<MutationCategory, int> { [MutationCategory.Growth] = 3 });

        var pair = Assert.Single(RosterBehaviorComparison.Compare(new[] { first, second }));

        Assert.Equal(0d, pair.MutationSimilarity);
        Assert.Equal(1d, pair.CategorySimilarity);
    }

    [Fact]
    public void Compare_SortsMostSimilarPairsFirstWithStableTieBreaks()
    {
        var profiles = new[]
        {
            Profile("charlie", new Dictionary<int, int> { [1] = 2 }, new Dictionary<MutationCategory, int>()),
            Profile("alpha", new Dictionary<int, int> { [1] = 2 }, new Dictionary<MutationCategory, int>()),
            Profile("bravo", new Dictionary<int, int> { [2] = 2 }, new Dictionary<MutationCategory, int>())
        };

        var pairs = RosterBehaviorComparison.Compare(profiles);

        Assert.Equal(3, pairs.Count);
        Assert.Equal(("alpha", "charlie"), (pairs[0].FirstStrategyName, pairs[0].SecondStrategyName));
        Assert.Equal(1d, pairs[0].MutationSimilarity);
    }

    [Fact]
    public void ObserveProfiles_DescribesEveryProvenStrategyWithoutUsingAuthoredMetadata()
    {
        _ = AIRoster.ProvenStrategies.Count;

        var profiles = RosterBehaviorComparison.ObserveProfiles(AIRoster.ProvenStrategies);
        var pairs = RosterBehaviorComparison.Compare(profiles);

        Assert.Equal(AIRoster.ProvenStrategies.Count, profiles.Count);
        Assert.Equal(profiles.Count * (profiles.Count - 1) / 2, pairs.Count);
        Assert.All(profiles, profile => Assert.NotEmpty(profile.MutationLevels));
    }

    private static StrategyBehaviorProfile Profile(
        string name,
        IReadOnlyDictionary<int, int> mutations,
        IReadOnlyDictionary<MutationCategory, int> categories)
        => new(name, mutations, categories);
}
