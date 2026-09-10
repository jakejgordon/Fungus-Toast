using FungusToast.Core.AI;
using FungusToast.Core.Mutations;
using FungusToast.Simulation.Candidates;

namespace FungusToast.Simulation.Calibration;

/// <summary>
/// An observed, deterministic description of one strategy's mutation-spending behavior.
/// Authored theme and difficulty labels are deliberately absent: Phase 8 must reason from what
/// a strategy actually buys under the shared characterization script, not what its name claims.
/// </summary>
public sealed record StrategyBehaviorProfile(
    string StrategyName,
    IReadOnlyDictionary<int, int> MutationLevels,
    IReadOnlyDictionary<MutationCategory, int> CategoryLevels);

/// <summary>
/// A pairwise comparison of observed behavior. Raw-mutation similarity is the redundancy signal;
/// category similarity is retained as a coarser explanation of shared strategic direction.
/// </summary>
public sealed record StrategyBehaviorPair(
    string FirstStrategyName,
    string SecondStrategyName,
    double MutationSimilarity,
    double CategorySimilarity)
{
    public double MutationDistance => 1d - MutationSimilarity;
}

/// <summary>
/// Compares registered strategies from their deterministic scripted builds.
///
/// This is intentionally not a strength or counter classifier. Similar builds are candidates for
/// redundancy review; they still need measured matchup and contextual evidence before either can
/// be retired. Conversely, a distant build is behavioral coverage, not proof that players value it.
/// </summary>
public static class RosterBehaviorComparison
{
    public static IReadOnlyList<StrategyBehaviorProfile> ObserveProfiles(
        IEnumerable<IMutationSpendingStrategy> strategies,
        CandidateCharacterizationSettings? settings = null)
    {
        ArgumentNullException.ThrowIfNull(strategies);
        var effectiveSettings = settings ?? new CandidateCharacterizationSettings();
        return strategies
            .OrderBy(strategy => strategy.StrategyName, StringComparer.Ordinal)
            .Select(strategy =>
            {
                var build = CandidateCharacterizationGate.ObserveBuild(strategy, effectiveSettings);
                return new StrategyBehaviorProfile(
                    strategy.StrategyName,
                    new Dictionary<int, int>(build),
                    CandidateRanking.BuildCategoryProfile(build));
            })
            .ToList();
    }

    public static IReadOnlyList<StrategyBehaviorPair> Compare(
        IReadOnlyList<StrategyBehaviorProfile> profiles)
    {
        ArgumentNullException.ThrowIfNull(profiles);
        var duplicateNames = profiles
            .GroupBy(profile => profile.StrategyName, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();
        if (duplicateNames.Count > 0)
        {
            throw new ArgumentException(
                $"Strategy names must be unique: {string.Join(", ", duplicateNames)}.",
                nameof(profiles));
        }

        var orderedProfiles = profiles
            .OrderBy(profile => profile.StrategyName, StringComparer.Ordinal)
            .ToList();
        var pairs = new List<StrategyBehaviorPair>();
        for (var firstIndex = 0; firstIndex < orderedProfiles.Count; firstIndex++)
        for (var secondIndex = firstIndex + 1; secondIndex < orderedProfiles.Count; secondIndex++)
        {
            var first = orderedProfiles[firstIndex];
            var second = orderedProfiles[secondIndex];
            pairs.Add(new StrategyBehaviorPair(
                first.StrategyName,
                second.StrategyName,
                CosineSimilarity(first.MutationLevels, second.MutationLevels),
                CosineSimilarity(first.CategoryLevels, second.CategoryLevels)));
        }

        return pairs
            .OrderByDescending(pair => pair.MutationSimilarity)
            .ThenBy(pair => pair.FirstStrategyName, StringComparer.Ordinal)
            .ThenBy(pair => pair.SecondStrategyName, StringComparer.Ordinal)
            .ToList();
    }

    public static IReadOnlyList<StrategyBehaviorPair> Compare(
        IEnumerable<IMutationSpendingStrategy> strategies,
        CandidateCharacterizationSettings? settings = null)
        => Compare(ObserveProfiles(strategies, settings));

    private static double CosineSimilarity<TKey>(
        IReadOnlyDictionary<TKey, int> left,
        IReadOnlyDictionary<TKey, int> right)
        where TKey : notnull
    {
        double dot = 0;
        double leftMagnitude = 0;
        double rightMagnitude = 0;
        foreach (var key in left.Keys.Concat(right.Keys).Distinct())
        {
            var leftValue = left.TryGetValue(key, out var l) ? l : 0;
            var rightValue = right.TryGetValue(key, out var r) ? r : 0;
            dot += leftValue * rightValue;
            leftMagnitude += leftValue * leftValue;
            rightMagnitude += rightValue * rightValue;
        }

        return leftMagnitude <= 0 || rightMagnitude <= 0
            ? 0
            : dot / (Math.Sqrt(leftMagnitude) * Math.Sqrt(rightMagnitude));
    }
}
