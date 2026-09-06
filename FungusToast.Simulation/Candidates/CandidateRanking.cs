using FungusToast.Core.AI;
using FungusToast.Core.Mutations;

namespace FungusToast.Simulation.Candidates;

/// <summary>
/// How strongly a candidate beat its parent, and how sure that is.
/// </summary>
public sealed class CandidateStrength
{
    /// <summary>The deepest stage with a measurement, since deeper stages carry more games.</summary>
    public required CandidateEvaluationStage MeasuredStage { get; init; }

    public required double Estimate { get; init; }

    public required double Ci95Low { get; init; }

    public required double Ci95High { get; init; }

    /// <summary>
    /// The conservative figure candidates are ordered by. Ranking on the point estimate would put
    /// a noisy candidate above a steady one that is better on the evidence.
    /// </summary>
    public double ConservativeScore => Ci95Low;
}

/// <summary>
/// Whether a candidate's advantage held up across the contexts it was measured in.
/// </summary>
public sealed class CandidateRobustness
{
    public required int ContextsMeasured { get; init; }

    /// <summary>The weakest lower bound across measured contexts; a candidate is only as good as its worst context.</summary>
    public required double WorstCi95Low { get; init; }

    /// <summary>False when the estimate changed sign between contexts, which means the advantage did not transfer.</summary>
    public required bool SignConsistent { get; init; }

    /// <summary>
    /// One context cannot demonstrate robustness. It is reported rather than scored so a
    /// single-context candidate is never mistaken for a proven one.
    /// </summary>
    public bool IsDemonstrated => ContextsMeasured > 1;
}

public sealed class CandidateRankingRow
{
    public required string CandidateId { get; init; }

    public required string DisplayName { get; init; }

    public required CandidateQueueStatus Status { get; init; }

    /// <summary>Null until a stage has produced an interval.</summary>
    public CandidateStrength? Strength { get; init; }

    public CandidateRobustness? Robustness { get; init; }

    /// <summary>
    /// Cosine similarity between the candidate's observed category profile and its parent's, in
    /// [0,1]. High means it still plays like its lineage; low means one gene change moved what it
    /// actually buys a long way.
    /// </summary>
    public required double LineageFidelity { get; init; }

    /// <summary>
    /// Mean cosine distance from the other ranked candidates' per-mutation builds, in [0,1]. High
    /// means this candidate explores a region of behavior the rest of the field does not.
    /// </summary>
    public required double BehavioralDiversity { get; init; }

    /// <summary>Levels bought per mutation category under the shared characterization script.</summary>
    public required IReadOnlyDictionary<MutationCategory, int> CategoryProfile { get; init; }
}

/// <summary>
/// Ranks a field of candidates on four independent measures.
///
/// There is deliberately no composite score. Strength, robustness, lineage fidelity, and
/// behavioral diversity answer different questions, and collapsing them into one number would let
/// a strong-but-fragile candidate outrank a steady one, or hide that a whole field converged on a
/// single behavior. Rows are ordered by conservative strength purely so the list reads sensibly;
/// the other three measures travel alongside rather than folding in.
///
/// Fidelity is measured against the parent's own observed behavior rather than an authored
/// archetype label. Generated candidates carry placeholder catalog metadata until Phase 7 assigns
/// bands from evidence, so scoring them against that label would be scoring them against a
/// fabrication. The parent's behavior is the only non-fabricated reference available.
/// </summary>
public static class CandidateRanking
{
    public static IReadOnlyList<CandidateRankingRow> Rank(
        CandidateEvaluationQueue queue,
        IReadOnlyDictionary<string, CandidateGenome> genomesByCandidateId,
        IMutationSpendingStrategy parentStrategy,
        CandidateCharacterizationSettings? settings = null)
    {
        ArgumentNullException.ThrowIfNull(queue);
        ArgumentNullException.ThrowIfNull(genomesByCandidateId);
        ArgumentNullException.ThrowIfNull(parentStrategy);

        var effectiveSettings = settings ?? new CandidateCharacterizationSettings();
        var parentProfile = BuildCategoryProfile(CandidateCharacterizationGate.ObserveBuild(parentStrategy, effectiveSettings));

        var profiles = new Dictionary<string, IReadOnlyDictionary<MutationCategory, int>>(StringComparer.Ordinal);
        var builds = new Dictionary<string, IReadOnlyDictionary<int, int>>(StringComparer.Ordinal);
        foreach (var entry in queue.Entries)
        {
            if (!genomesByCandidateId.TryGetValue(entry.CandidateId, out var genome))
                throw new ArgumentException(
                    $"Candidate '{entry.CandidateId}' has no genome; ranking needs one to observe its build.",
                    nameof(genomesByCandidateId));

            var strategy = CandidateGenomeFactory.Materialize(genome);
            var build = CandidateCharacterizationGate.ObserveBuild(strategy, effectiveSettings);
            builds[entry.CandidateId] = build;
            profiles[entry.CandidateId] = BuildCategoryProfile(build);
        }

        var rows = queue.Entries
            .Select(entry => new CandidateRankingRow
            {
                CandidateId = entry.CandidateId,
                DisplayName = entry.DisplayName,
                Status = entry.Status,
                Strength = BuildStrength(entry),
                Robustness = BuildRobustness(entry),
                LineageFidelity = CosineSimilarity(profiles[entry.CandidateId], parentProfile),
                BehavioralDiversity = MeanDistanceFromPeers(entry.CandidateId, builds),
                CategoryProfile = profiles[entry.CandidateId]
            })
            .ToList();

        // Ordering is presentation only: unmeasured candidates sink, and the rest sort by the
        // conservative bound so a noisy candidate cannot outrank a steadier, better-evidenced one.
        return rows
            .OrderByDescending(row => row.Strength?.ConservativeScore ?? double.NegativeInfinity)
            .ThenBy(row => row.DisplayName, StringComparer.Ordinal)
            .ToList();
    }

    private static CandidateStrength? BuildStrength(CandidateQueueEntry entry)
    {
        if (entry.Measurements.Count == 0) return null;

        // The deepest measured stage carries the most games, so it is the one to report.
        var deepest = entry.Measurements.OrderByDescending(pair => (int)pair.Key).First();
        return new CandidateStrength
        {
            MeasuredStage = deepest.Key,
            Estimate = deepest.Value.Estimate,
            Ci95Low = deepest.Value.Ci95Low,
            Ci95High = deepest.Value.Ci95High
        };
    }

    private static CandidateRobustness? BuildRobustness(CandidateQueueEntry entry)
    {
        if (entry.Measurements.Count == 0) return null;

        var measurements = entry.Measurements.Values.ToList();
        var signs = measurements.Select(measurement => Math.Sign(measurement.Estimate)).Distinct().ToList();
        return new CandidateRobustness
        {
            ContextsMeasured = measurements.Count,
            WorstCi95Low = measurements.Min(measurement => measurement.Ci95Low),
            // Zero estimates are not a sign change, so they are excluded before comparing.
            SignConsistent = signs.Count(sign => sign != 0) <= 1
        };
    }

    /// <summary>
    /// Total levels bought per category under the shared script. Categories are the natural unit
    /// here: two builds that buy different mutations toward the same end should read as similar.
    /// </summary>
    public static IReadOnlyDictionary<MutationCategory, int> BuildCategoryProfile(IReadOnlyDictionary<int, int> observedBuild)
    {
        ArgumentNullException.ThrowIfNull(observedBuild);
        var profile = Enum.GetValues(typeof(MutationCategory))
            .Cast<MutationCategory>()
            .ToDictionary(category => category, _ => 0);

        foreach (var (mutationId, level) in observedBuild)
        {
            var mutation = MutationRegistry.GetById(mutationId);
            if (mutation == null) continue;
            profile[mutation.Category] += level;
        }

        return profile;
    }

    /// <summary>
    /// Diversity is measured over the raw per-mutation build rather than the category profile.
    /// Categories are the right granularity for fidelity, where two builds pursuing the same end by
    /// different means should read as similar - but that same coarseness collapses genuinely
    /// different candidates onto identical vectors, which would understate how much of the space a
    /// field actually covers.
    /// </summary>
    private static double MeanDistanceFromPeers(
        string candidateId,
        IReadOnlyDictionary<string, IReadOnlyDictionary<int, int>> builds)
    {
        var peers = builds.Where(pair => !string.Equals(pair.Key, candidateId, StringComparison.Ordinal)).ToList();
        if (peers.Count == 0) return 0;

        var build = builds[candidateId];
        return peers.Average(peer => 1.0 - CosineSimilarity(build, peer.Value));
    }

    private static double CosineSimilarity(IReadOnlyDictionary<int, int> left, IReadOnlyDictionary<int, int> right)
    {
        double dot = 0, leftMagnitude = 0, rightMagnitude = 0;
        foreach (var key in left.Keys.Concat(right.Keys).Distinct())
        {
            double leftValue = left.TryGetValue(key, out var l) ? l : 0;
            double rightValue = right.TryGetValue(key, out var r) ? r : 0;
            dot += leftValue * rightValue;
            leftMagnitude += leftValue * leftValue;
            rightMagnitude += rightValue * rightValue;
        }

        if (leftMagnitude <= 0 || rightMagnitude <= 0) return 0;
        return dot / (Math.Sqrt(leftMagnitude) * Math.Sqrt(rightMagnitude));
    }

    private static double CosineSimilarity(
        IReadOnlyDictionary<MutationCategory, int> left,
        IReadOnlyDictionary<MutationCategory, int> right)
    {
        double dot = 0, leftMagnitude = 0, rightMagnitude = 0;
        foreach (var category in Enum.GetValues(typeof(MutationCategory)).Cast<MutationCategory>())
        {
            double leftValue = left.TryGetValue(category, out var l) ? l : 0;
            double rightValue = right.TryGetValue(category, out var r) ? r : 0;
            dot += leftValue * rightValue;
            leftMagnitude += leftValue * leftValue;
            rightMagnitude += rightValue * rightValue;
        }

        // An empty build has no direction, so similarity is undefined rather than perfect.
        if (leftMagnitude <= 0 || rightMagnitude <= 0) return 0;
        return dot / (Math.Sqrt(leftMagnitude) * Math.Sqrt(rightMagnitude));
    }
}
