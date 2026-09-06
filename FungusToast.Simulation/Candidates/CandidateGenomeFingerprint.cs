using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using FungusToast.Core.Mutations;

namespace FungusToast.Simulation.Candidates;

/// <summary>
/// Deterministic identity for a candidate's behavior. The fingerprint covers the gene set only:
/// display name, candidate ID, lineage, and notes are excluded so two independently generated
/// candidates with identical behavior deduplicate to one fingerprint.
/// </summary>
public static class CandidateGenomeFingerprint
{
    /// <summary>Characters of the gene fingerprint embedded in a derived candidate ID.</summary>
    public const int CandidateIdFingerprintLength = 12;

    public static string Compute(CandidateGeneSet genes)
    {
        ArgumentNullException.ThrowIfNull(genes);
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(BuildCanonicalText(genes)));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>
    /// The exact bytes that are hashed. Exposed so a failing comparison can show what differed
    /// instead of only that two hashes disagreed.
    /// </summary>
    public static string BuildCanonicalText(CandidateGeneSet genes)
    {
        var fragments = BuildGeneFragments(genes);
        return string.Join("\n", new[] { CandidateGenome.CurrentSchemaVersion }
            .Concat(fragments.Select(fragment => fragment.Key.ToString() + "=" + fragment.Value)));
    }

    /// <summary>
    /// One canonical fragment per gene, in declaration order. This is the single source of truth
    /// for both the fingerprint and gene-level difference detection, so a gene can never be
    /// hashed but excluded from comparison (or the reverse).
    /// </summary>
    public static IReadOnlyList<KeyValuePair<CandidateGene, string>> BuildGeneFragments(CandidateGeneSet genes)
    {
        ArgumentNullException.ThrowIfNull(genes);
        return new[]
        {
            Fragment(CandidateGene.PrioritizeHighTier, genes.PrioritizeHighTier ? "true" : "false"),
            Fragment(CandidateGene.MaxTier, genes.MaxTier.ToString()),
            Fragment(CandidateGene.PriorityMutationCategories, FormatCategories(genes.PriorityMutationCategories)),
            Fragment(CandidateGene.TargetMutationGoals, FormatGoals(genes.TargetMutationGoals)),
            Fragment(CandidateGene.SurgePriorityIds, FormatIds(genes.SurgePriorityIds, sorted: false)),
            Fragment(CandidateGene.SurgeAttemptTurnFrequency, genes.SurgeAttemptTurnFrequency.ToString(CultureInfo.InvariantCulture)),
            Fragment(CandidateGene.EconomyBias, genes.EconomyBias.ToString()),
            Fragment(CandidateGene.MycovariantPreferences, FormatPreferences(genes.MycovariantPreferences)),
            Fragment(CandidateGene.ExcludedMutationIds, FormatIds(genes.ExcludedMutationIds, sorted: true)),
            Fragment(CandidateGene.StartingSporeEdgeOffset, genes.StartingSporeEdgeOffset.ToString(CultureInfo.InvariantCulture))
        };
    }

    /// <summary>
    /// Genes whose canonical value differs between two gene sets, in declaration order.
    /// </summary>
    public static IReadOnlyList<CandidateGene> FindDifferences(CandidateGeneSet left, CandidateGeneSet right)
    {
        var leftFragments = BuildGeneFragments(left);
        var rightFragments = BuildGeneFragments(right);
        return leftFragments
            .Zip(rightFragments, (leftFragment, rightFragment) => (leftFragment, rightFragment))
            .Where(pair => !string.Equals(pair.leftFragment.Value, pair.rightFragment.Value, StringComparison.Ordinal))
            .Select(pair => pair.leftFragment.Key)
            .ToList();
    }

    private static KeyValuePair<CandidateGene, string> Fragment(CandidateGene gene, string value)
        => new(gene, value);

    /// <summary>
    /// Derives the candidate ID from parent lineage plus behavior, so an edited ID cannot silently
    /// misattribute a candidate. Validation requires the stored ID to equal this value.
    /// </summary>
    public static string DeriveCandidateId(string parentStrategyId, CandidateGeneSet genes)
    {
        if (string.IsNullOrWhiteSpace(parentStrategyId))
            throw new ArgumentException("Parent strategy ID is required.", nameof(parentStrategyId));
        var fingerprint = Compute(genes);
        return CandidateGenome.CandidateIdPrefix
            + ExtractParentSlug(parentStrategyId)
            + "."
            + fingerprint[..CandidateIdFingerprintLength];
    }

    /// <summary>
    /// Reduces a stable strategy ID to its identity segment. <c>legacy.testing.tst-control.v1</c>
    /// yields <c>tst-control</c>; anything else is sanitized whole so a future ID scheme still
    /// produces a usable, collision-free slug.
    /// </summary>
    public static string ExtractParentSlug(string parentStrategyId)
    {
        if (string.IsNullOrWhiteSpace(parentStrategyId))
            throw new ArgumentException("Parent strategy ID is required.", nameof(parentStrategyId));

        var segments = parentStrategyId.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 4
            && segments[0].Equals("legacy", StringComparison.Ordinal)
            && segments[3].StartsWith("v", StringComparison.Ordinal))
        {
            return Sanitize(segments[2]);
        }

        return Sanitize(parentStrategyId);
    }

    private static string Sanitize(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var character in value.ToLowerInvariant())
            builder.Append(char.IsLetterOrDigit(character) ? character : '-');

        var slug = builder.ToString();
        while (slug.Contains("--", StringComparison.Ordinal))
            slug = slug.Replace("--", "-", StringComparison.Ordinal);
        slug = slug.Trim('-');

        return slug.Length == 0
            ? throw new ArgumentException($"Value '{value}' cannot produce a non-empty slug.", nameof(value))
            : slug;
    }

    private static string FormatCategories(IReadOnlyList<MutationCategory>? categories)
    {
        // Null and empty are different behaviors in Core: null expands to every category.
        if (categories == null) return "null";
        return "[" + string.Join(",", categories.Select(category => category.ToString())) + "]";
    }

    private static string FormatGoals(IReadOnlyList<CandidateMutationGoal> goals)
    {
        return string.Join(",", goals.Select(goal =>
            goal.MutationId.ToString(CultureInfo.InvariantCulture)
            + ":"
            + (goal.TargetLevel?.ToString(CultureInfo.InvariantCulture) ?? "max")));
    }

    private static string FormatIds(IReadOnlyList<int> ids, bool sorted)
    {
        var ordered = sorted ? ids.OrderBy(id => id) : ids.AsEnumerable();
        return string.Join(",", ordered.Select(id => id.ToString(CultureInfo.InvariantCulture)));
    }

    private static string FormatPreferences(IReadOnlyList<CandidateMycovariantPreference> preferences)
    {
        // Preference order is behavioral (Core evaluates the list top-down), so it is preserved.
        // IDs inside one preference are only membership-tested, so they are sorted.
        return string.Join(",", preferences.Select(preference =>
            preference.Priority.ToString(CultureInfo.InvariantCulture)
            + ":"
            + string.Join("+", preference.MycovariantIds.OrderBy(id => id).Select(id => id.ToString(CultureInfo.InvariantCulture)))));
    }
}
